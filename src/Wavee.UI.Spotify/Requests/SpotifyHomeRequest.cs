using System;
using System.Globalization;
using System.Linq;
using NodaTime;
using NodaTime.TimeZones;
using Refit;
using Wavee.UI.Spotify.Responses;

namespace Wavee.UI.Spotify.Requests;

internal sealed class SpotifyHomeRequest : SpotifyQueryRequest<SpotifyHomeResponse>
{
    public SpotifyHomeRequest(TimeZoneInfo timeZoneInfo, CultureInfo country) : base(SpotifyUrls.Partner.Home.QueryName, SpotifyUrls.Partner.Home.Hash)
    {
        var windowsTimeZoneId = timeZoneInfo.Id;
        var x = DateTimeZoneProviders.Tzdb;
        var tzdbZoneId = TzdbDateTimeZoneSource.Default
            .WindowsMapping
            .MapZones
            .FirstOrDefault(x => x.WindowsId == windowsTimeZoneId)?
            .TzdbIds
            .FirstOrDefault();

        
        const string spT = "c2273f432bc9151ca8f87da4acc201aa";
        var countryName = country.TwoLetterISOLanguageName;
        Variables = $"{{\"timeZone\":\"{tzdbZoneId}\",\"sp_t\":\"{spT}\",\"country\":\"{countryName}\",\"facet\":null,\"sectionItemsLimit\":10}}";
    }
    
    [AliasAs("variables")]
    public string Variables { get; }
}