using LanguageExt;
using Spotify.Collection.Proto.V2;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;

namespace Wavee.Spotify.Infrastructure.PrivateApi.Contracts;

public interface ISpotifyPrivateApi
{
    Task<SpotifyColors> FetchColorFor(Seq<string> artwork, CancellationToken ct = default);
    Task<Unit> WriteLibrary(WriteRequest writeRequest, CancellationToken ct = default);
}