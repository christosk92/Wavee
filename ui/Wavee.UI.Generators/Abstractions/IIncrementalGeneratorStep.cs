using Microsoft.CodeAnalysis;

namespace Wavee.UI.Generators.Abstractions;

/// <summary>
/// Interface that defines methods for incremental generator steps.
/// </summary>
internal interface IIncrementalGeneratorStep
{
    /// <summary>
    /// Registers the syntax provider and source output for the generator step.
    /// </summary>
    /// <param name="context">The incremental generator initialization context.</param>
    /// <param name="combinedGenerator">Reference to the combined generator for shared resources.</param>
    void Register(IncrementalGeneratorInitializationContext context, CombinedGenerator combinedGenerator);
}