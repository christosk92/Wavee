using LanguageExt;

namespace Wavee.Spotify.Contracts.Mercury.Search;

public readonly record struct SearchCategory(string Category, int Total, Seq<ISearchHit> Hits);