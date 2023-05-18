using Eum.Spotify;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.Infrastructure.Sys;

public static class Spotify<R> where R : struct, HasSpotify<R>
{
    public static Aff<R, APWelcome> Authenticate(LoginCredentials credentials, CancellationToken ct = default) =>
        from aff in default(R).SpotifyEff.MapAsync(x => x.Authenticate(credentials, ct))
        from apwelcome in default(R).SpotifyEff.Map(x=> x.WelcomeMessage())
        select apwelcome.ValueUnsafe();
}