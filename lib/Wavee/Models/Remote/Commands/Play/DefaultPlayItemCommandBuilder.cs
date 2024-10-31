namespace Wavee.Models.Remote.Commands.Play;

/// <summary>
/// Builder for creating <see cref="PlayItemCommand"/> with an arbitrary context ID.
/// </summary>
public class DefaultPlayItemCommandBuilder : PlayItemCommandBuilderBase<DefaultPlayItemCommandBuilder>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DefaultPlayItemCommandBuilder"/> class with the required context ID.
    /// </summary>
    /// <param name="contextId">The arbitrary context ID.</param>
    public DefaultPlayItemCommandBuilder(string contextId)
    {
        if (string.IsNullOrWhiteSpace(contextId))
            throw new ArgumentException("Context ID cannot be null or empty.", nameof(contextId));

        _contextId = contextId;
    }
}