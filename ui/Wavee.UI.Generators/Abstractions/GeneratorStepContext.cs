using Microsoft.CodeAnalysis;

namespace Wavee.UI.Generators.Abstractions;

internal record GeneratorStepContext(GeneratorExecutionContext Context, Compilation Compilation);