using System.Collections.Generic;
using System.Text.Json.Serialization;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.Spotify.Responses;

internal sealed class SpotifyHomeResponse
{
    public required SpotifyHomeData Data { get; init; }
}

internal sealed class SpotifyHomeData
{
    public required SpotifyHomeHome Home { get; init; }
}

internal sealed class SpotifyHomeHome
{
    [JsonPropertyName("__typename")] public required string Typename { get; init; }
    public required SpotifyHomeGreeting Greeting { get; init; }
    public required SpotifyHomeSectionContainer SectionContainer { get; init; }
}

internal sealed class SpotifyHomeGreeting
{
    public required string Text { get; init; }
}

internal sealed class SpotifyHomeSectionContainer
{
    public required SpotifyHomeSections Sections { get; init; }
}

internal sealed class SpotifyHomeSections
{
    public required IReadOnlyCollection<SpotifyHomeSection> Items { get; init; }
}

internal sealed class SpotifyHomeSection
{
    public required ISpotifyHomeSectionData Data { get; init; }
    public required SpotifyHomeSectionItems SectionItems { get; init; }
    public required string Uri { get; init; }
}

internal sealed class SpotifyHomeSectionItems
{
    public required IReadOnlyCollection<IHomeItem> Items { get; init; }
    public required int TotalCount { get; init; }
    public required SpotifyHomeSectionPagingInfo PagingInfo { get; init; }
}

internal sealed class SpotifyHomeSectionPagingInfo
{
    public required int? NextOffset { get; init; }
}

internal interface ISpotifyHomeSectionData
{
    string Typename { get; }
    string TitleText { get; }
}

internal sealed class HomeShortsSectionData : ISpotifyHomeSectionData
{
    [JsonPropertyName("__typename")] public required string Typename { get; init; }
    public string TitleText => "HomeShortsSectionData";
}

internal sealed class HomeRecentlyPlayedSectionData : ISpotifyHomeSectionData
{
    [JsonPropertyName("__typename")] public required string Typename { get; init; }
    public string TitleText => Title.Text;
    public required SpotifyHomeSectionTitle Title { get; init; }
}

internal sealed class HomeSpotlightSectionData : ISpotifyHomeSectionData
{
    [JsonPropertyName("__typename")] public required string Typename { get; init; }
    public string TitleText => Title.TransformedLabel;
    public required HomeSpotlightSectionTitle Title { get; init; }
}

internal sealed class HomeSpotlightSectionTitle
{
    public required string TransformedLabel { get; init; }
}

internal sealed class HomeGenericSectionData : ISpotifyHomeSectionData
{
    [JsonPropertyName("__typename")] public required string Typename { get; init; }

    public string TitleText => Title.Text;
    public required SpotifyHomeSectionTitle Title { get; init; }
}

internal sealed class SpotifyHomeSectionTitle
{
    public required string Text { get; init; }
}