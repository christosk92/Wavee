using LanguageExt;
using Wavee.Spotify.Contracts.Common;

namespace Wavee.Spotify.Contracts.Mercury.Search;

public readonly record struct TrackSearchHit(SpotifyId Id, string Name, Seq<NameUriCombo> Artists, NameUriCombo Album,
    uint Duration, string Image) : ISearchHit;