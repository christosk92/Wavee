using LanguageExt;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;

namespace Wavee.Spotify.Infrastructure.PrivateApi.Contracts;

public interface ISpotifyPrivateApi
{
    Task<SpotifyColors> FetchColorFor(Seq<string> artwork, CancellationToken ct = default);
}