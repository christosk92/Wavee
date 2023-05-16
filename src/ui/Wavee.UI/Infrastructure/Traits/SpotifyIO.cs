using LanguageExt;
using LanguageExt.Attributes;
using LanguageExt.Effects.Traits;

namespace Wavee.UI.Infrastructure.Traits;

public interface SpotifyIO
{
    
}

/// <summary>
/// Type-class giving a struct the trait of supporting spotify IO
/// </summary>
/// <typeparam name="RT">Runtime</typeparam>
[Typeclass("*")]
public interface HasSpotify<RT> : HasCancel<RT>
    where RT : struct, HasCancel<RT>
{
    /// <summary>
    /// Access the spotify synchronous effect environment
    /// </summary>
    /// <returns>Spotify synchronous effect environment</returns>
    Eff<RT, SpotifyIO> SpotifyEff { get; }
}
