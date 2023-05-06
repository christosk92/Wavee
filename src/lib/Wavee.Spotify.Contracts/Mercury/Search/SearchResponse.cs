using LanguageExt;

namespace Wavee.Spotify.Contracts.Mercury.Search;

public readonly record struct SearchResponse(Seq<SearchCategory> Categories);