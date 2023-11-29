using Mediator;

namespace Wavee.Spotify.Application.GraphQL.Queries;

public sealed class GetSpotifyGraphQLQuery : IQuery<HttpResponseMessage>
{
    public required string OperationName { get; init; }
    public required IReadOnlyDictionary<string, object> Variables { get; init; }
    public required string Hash { get; init; }
}