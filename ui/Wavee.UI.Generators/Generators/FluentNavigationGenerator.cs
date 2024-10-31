﻿using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Wavee.UI.Generators.Abstractions;

namespace Wavee.UI.Generators.Generators;

internal class FluentNavigationGenerator : GeneratorStep
{
    public List<ConstructorDeclarationSyntax> Constructors { get; } = new();

    public override void OnInitialize(Compilation compilation, GeneratorStep[] steps)
    {
        var uiContextStep = steps.OfType<UiContextConstructorGenerator>().First();

        Constructors.AddRange(uiContextStep.Constructors);
    }

    public override void Execute()
    {
        var namespaces = new List<string>();
        var methods = new List<string>();

        foreach (var constructor in Constructors)
        {
            var semanticModel = GetSemanticModel(constructor.SyntaxTree);

            if (constructor.Parent is not ClassDeclarationSyntax cls)
            {
                continue;
            }

            if (!cls.IsRoutableViewModel(semanticModel))
            {
                continue;
            }

            if (cls.IsAbstractClass(semanticModel))
            {
                continue;
            }

            var viewModelTypeInfo = semanticModel.GetDeclaredSymbol(cls);

            if (viewModelTypeInfo == null)
            {
                continue;
            }

            var className = cls.Identifier.ValueText;

            var constructorNamespaces = constructor.ParameterList.Parameters
                .Where(p => p.Type is not null)
                .Select(p => semanticModel.GetTypeInfo(p.Type!))
                .Where(t => t.Type is not null)
                .SelectMany(t => t.Type.GetNamespaces())
                .ToArray();

            var uiContextParam = constructor.ParameterList.Parameters.FirstOrDefault(x => x.Type.IsUiContextType(semanticModel));

            var methodParams = constructor.ParameterList;

            if (uiContextParam != null)
            {
                methodParams = SyntaxFactory.ParameterList(methodParams.Parameters.Remove(uiContextParam));
            }

            var navigationMetadata = viewModelTypeInfo
                .GetAttributes()
                .FirstOrDefault(x => x.AttributeClass?.ToDisplayString() == NavigationMetaDataGenerator.NavigationMetaDataAttributeDisplayString);

            var defaultNavigationTarget = "DialogScreen";

            if (navigationMetadata != null)
            {
                var navigationArgument = navigationMetadata.NamedArguments
                    .FirstOrDefault(x => x.Key == "NavigationTarget");

                if (navigationArgument.Value.Type is INamedTypeSymbol navigationTargetEnum)
                {
                    var enumValue = navigationTargetEnum
                        .GetMembers()
                        .OfType<IFieldSymbol>()
                        .FirstOrDefault(x => x.ConstantValue?.Equals(navigationArgument.Value.Value) == true);

                    if (enumValue != null)
                    {
                        defaultNavigationTarget = enumValue.Name;
                    }
                }
            }

            var additionalMethodParams =
                $"NavigationTarget navigationTarget = NavigationTarget.{defaultNavigationTarget}, NavigationMode navigationMode = NavigationMode.Normal";

            methodParams = methodParams.AddParameters(SyntaxFactory.ParseParameterList(additionalMethodParams).Parameters.ToArray());

            var constructorArgs =
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SeparatedList(
                        constructor.ParameterList
                            .Parameters
                            .Select(x => x.Type.IsUiContextType(semanticModel) ? "UiContext" : x.Identifier.ValueText) // replace uiContext argument for UiContext property reference
                            .Select(x => SyntaxFactory.ParseExpression(x))
                            .Select(SyntaxFactory.Argument),
                        constructor.ParameterList
                            .Parameters
                            .Skip(1)
                            .Select(x => SyntaxFactory.Token(SyntaxKind.CommaToken))));

            namespaces.Add(viewModelTypeInfo.ContainingNamespace.ToDisplayString());
            namespaces.AddRange(constructorNamespaces);

            var methodName = className.Replace("ViewModel", "");

            var (dialogReturnType, dialogReturnTypeNamespace) = cls.GetDialogResultType(semanticModel);

            foreach (var ns in dialogReturnTypeNamespace)
            {
                namespaces.Add(ns);
            }

            if (dialogReturnType is { })
            {
                var dialogString =
                    $$"""
                      	public FluentDialog<{{dialogReturnType}}> {{methodName}}{{methodParams}}
                      	{
                      	    var dialog = new {{className}}{{constructorArgs.ToFullString()}};
                      		var target = UiContext.Navigate(navigationTarget);
                      		target.To(dialog, navigationMode);
                      
                      		return new FluentDialog<{{dialogReturnType}}>(target.NavigateDialogAsync(dialog, navigationMode));
                      	}

                      """;
                methods.Add(dialogString);
            }
            else
            {
                var methodString =
                    $$"""
                      	public void {{methodName}}{{methodParams}}
                      	{
                      		UiContext.Navigate(navigationTarget).To(new {{className}}{{constructorArgs.ToFullString()}}, navigationMode);
                      	}

                      """;
                methods.Add(methodString);
            }
        }

        var usings = namespaces
            .Distinct()
            .OrderBy(x => x)
            .Select(n => $"using {n};")
            .ToArray();

        var usingsString = string.Join("\r\n", usings);

        var methodsString = string.Join("\r\n", methods);

        var sourceText =
            $$"""
              // <auto-generated />
              #nullable enable

              {{usingsString}}

              namespace Wavee.ViewModels.ViewModels.Navigation;

              public partial class FluentNavigate
              {
              {{methodsString}}
              }

              """;

        AddSource("FluentNavigate.g.cs", sourceText);
    }
}