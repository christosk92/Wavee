using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Wavee.UI.Generators
{
    [Generator]
    public class NavigationMetaDataGenerator : IIncrementalGenerator
    {
        public const string NavigationMetaDataAttributeDisplayString =
            "Wavee.ViewModels.ViewModels.NavBar.NavigationMetaDataAttribute";

        private const string NavigationMetaDataDisplayString = "Wavee.ViewModels.ViewModels.NavBar.NavigationMetaData";

        private const string RoutableViewModelDisplayString =
            "Wavee.ViewModels.ViewModels.Navigation.RoutableViewModel";

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            // Register a syntax provider that selects classes with the target attribute
            var classDeclarations = context.SyntaxProvider
                .CreateSyntaxProvider(
                    predicate: static (s, _) => IsClassWithAttributes(s),
                    transform: static (ctx, _) => GetSemanticTarget(ctx))
                .Where(static m => m != null);

            // Combine with the compilation
            var compilationAndClasses = context.CompilationProvider.Combine(classDeclarations.Collect());

            // Register the source output
            context.RegisterSourceOutput(compilationAndClasses, (spc, source) =>
            {
                var (compilation, classes) = source;
                Execute(compilation, classes, spc);
            });
        }

        private static bool IsClassWithAttributes(SyntaxNode node)
        {
            return node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0;
        }

        private static INamedTypeSymbol? GetSemanticTarget(GeneratorSyntaxContext context)
        {
            var classDeclarationSyntax = (ClassDeclarationSyntax)context.Node;

            var symbol = ModelExtensions.GetDeclaredSymbol(context.SemanticModel, classDeclarationSyntax);
            if (symbol == null)
                return null;

            foreach (var attribute in symbol.GetAttributes())
            {
                var attrClass = attribute.AttributeClass?.ToDisplayString();
                if (attrClass == NavigationMetaDataAttributeDisplayString)
                {
                    return symbol as INamedTypeSymbol;
                }
            }

            return null;
        }

        private static void Execute(Compilation compilation, ImmutableArray<INamedTypeSymbol?> classes,
            SourceProductionContext context)
        {
            if (classes.IsDefaultOrEmpty)
                return;

            var distinctClasses = classes.Distinct(SymbolEqualityComparer.Default);

            var attributeSymbol = compilation.GetTypeByMetadataName(NavigationMetaDataAttributeDisplayString);
            if (attributeSymbol is null)
                return;

            var metadataSymbol = compilation.GetTypeByMetadataName(NavigationMetaDataDisplayString);
            if (metadataSymbol is null)
                return;

            foreach (var classSymbol in distinctClasses)
            {
                if (classSymbol is null)
                    continue;

                var classSource = ProcessClass(compilation, classSymbol as INamedTypeSymbol, attributeSymbol,
                    metadataSymbol);
                if (classSource is not null)
                {
                    context.AddSource($"{classSymbol.Name}_NavigationMetaData.cs",
                        SourceText.From(classSource, Encoding.UTF8));
                }
            }
        }

        private static string? ProcessClass(Compilation compilation, INamedTypeSymbol classSymbol,
            INamedTypeSymbol attributeSymbol, INamedTypeSymbol metadataSymbol)
        {
            if (!classSymbol.ContainingSymbol.Equals(classSymbol.ContainingNamespace, SymbolEqualityComparer.Default))
            {
                return null;
            }

            string namespaceName = classSymbol.ContainingNamespace.ToDisplayString();

            var format = new SymbolDisplayFormat(
                typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypes,
                genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters |
                                 SymbolDisplayGenericsOptions.IncludeTypeConstraints |
                                 SymbolDisplayGenericsOptions.IncludeVariance);

            var attributeData = classSymbol
                .GetAttributes()
                .Single(ad => ad.AttributeClass?.Equals(attributeSymbol, SymbolEqualityComparer.Default) ?? false);

            var isNavBarItem =
                attributeData.NamedArguments.Any(x => x.Key == "NavBarPosition") &&
                attributeData.NamedArguments.Any(x => x.Key == "NavBarSelectionMode");

            var implementedInterfaces = new List<string>();

            if (isNavBarItem)
            {
                var navBarSelectionMode =
                    attributeData.NamedArguments.First(x => x.Key == "NavBarSelectionMode").Value.Value;
                if (navBarSelectionMode is int s)
                {
                    if (s == 1)
                    {
                        implementedInterfaces.Add("INavBarButton");
                    }
                    else if (s == 2)
                    {
                        implementedInterfaces.Add("INavBarToggle");
                    }
                }
            }

            var implementedInterfacesString =
                implementedInterfaces.Count != 0
                    ? ": " + string.Join(", ", implementedInterfaces)
                    : "";

            var source = new StringBuilder(
                $$"""
                  // <auto-generated />
                  #nullable enable
                  using System;
                  using System.Threading.Tasks;
                  using Wavee.ViewModels.ViewModels.Navigation;

                  namespace {{namespaceName}};

                  public partial class {{classSymbol.ToDisplayString(format)}}{{implementedInterfacesString}}
                  {

                  """);

            source.Append(
                $$"""
                      public static {{metadataSymbol.ToDisplayString()}} MetaData { get; } = new()
                      {

                  """);
            var length = attributeData.NamedArguments.Length;
            for (int i = 0; i < length; i++)
            {
                var namedArgument = attributeData.NamedArguments[i];

                source.AppendLine($"\t\t{namedArgument.Key} = " +
                                  $"{(namedArgument.Value.Kind == TypedConstantKind.Array ? "new[] " : "")}" +
                                  $"{namedArgument.Value.ToCSharpString()}{(i < length - 1 ? "," : "")}");
            }

            source.Append(
                """
                    };

                """);

            source.AppendLine(
                """
                    public static void RegisterAsyncLazy(Func<Task<RoutableViewModel?>> createInstance) => NavigationManager.RegisterAsyncLazy(MetaData, createInstance);
                """);
            source.AppendLine(
                """
                    public static void RegisterLazy(Func<RoutableViewModel?> createInstance) => NavigationManager.RegisterLazy(MetaData, createInstance);
                """);
            source.AppendLine(
                """
                    public static void Register(RoutableViewModel createInstance) => NavigationManager.Register(MetaData, createInstance);
                """);

            var routableClass = compilation.GetTypeByMetadataName(RoutableViewModelDisplayString);

            if (routableClass is { })
            {
                bool addRoutableMetaData = false;
                var baseType = classSymbol.BaseType;
                while (true)
                {
                    if (baseType is null)
                    {
                        break;
                    }

                    if (SymbolEqualityComparer.Default.Equals(baseType, routableClass))
                    {
                        addRoutableMetaData = true;
                        break;
                    }

                    baseType = baseType.BaseType;
                }

                if (addRoutableMetaData)
                {
                    if (attributeData.NamedArguments.Any(x => x.Key == "NavigationTarget"))
                    {
                        source.AppendLine(
                            """
                                public override NavigationTarget DefaultTarget => MetaData.NavigationTarget;
                            """);
                    }

                    if (attributeData.NamedArguments.Any(x => x.Key == "Title"))
                    {
                        source.AppendLine(
                            """
                                public override string Title { get => MetaData.Title!; protected set {} }
                            """);
                    }
                }

                if (attributeData.NamedArguments.Any(x => x.Key == "IconName"))
                {
                    source.AppendLine(
                        """
                            public string IconName => MetaData.IconName!;
                        """);
                }

                if (attributeData.NamedArguments.Any(x => x.Key == "IconNameFocused"))
                {
                    source.AppendLine(
                        """
                            public string IconNameFocused => MetaData.IconNameFocused!;
                        """);
                }
            }

            source.Append(
                """
                }
                """);

            return source.ToString();
        }
    }
}