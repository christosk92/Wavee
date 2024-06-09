using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Wavee.UI.Generators.Abstractions;

internal abstract class GeneratorStep
{
	private readonly object _lock = new();

	public GeneratorStepContext Context { get; private set; }

	public void Initialize(GeneratorExecutionContext context, Compilation compilation)
	{
		Context = new GeneratorStepContext(context, compilation);
	}

	public virtual void OnInitialize(Compilation compilation, GeneratorStep[] steps)
	{
	}

	public abstract void Execute();

	protected SyntaxTree AddSource(string name, string source)
	{
		var syntaxTree = SyntaxFactory.ParseSyntaxTree(source, Context.Context.ParseOptions);
		Context.Context.AddSource(name, SourceText.From(source, Encoding.UTF8));

		lock (_lock)
		{
			Context = Context with { Compilation = Context.Compilation.AddSyntaxTrees(syntaxTree) };
		}

		return syntaxTree;
	}

	protected void ReportDiagnostic(DiagnosticDescriptor diagnosticDescriptor, Location? location)
	{
		Context.Context.ReportDiagnostic(Diagnostic.Create(diagnosticDescriptor, location));
	}

	protected SemanticModel GetSemanticModel(SyntaxTree syntaxTree) => Context.Compilation.GetSemanticModel(syntaxTree);
}
