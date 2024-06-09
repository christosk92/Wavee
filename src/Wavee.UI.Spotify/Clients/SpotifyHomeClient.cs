using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualBasic;
using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Clients;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.Contracts.Models;
using Wavee.UI.Spotify.Interfaces.Api;
using Wavee.UI.Spotify.Requests;
using Wavee.UI.Spotify.Responses;

namespace Wavee.UI.Spotify.Clients;

internal sealed class SpotifyHomeClient : IHomeClient
{
    private readonly ISpotifyPartnerApi _partnerApi;

    public SpotifyHomeClient(ISpotifyPartnerApi partnerApi)
    {
        _partnerApi = partnerApi;
    }

    public async Task<IReadOnlyCollection<IHomeItem>> GetItems(CancellationToken cancellation)
    {
        var timeZone = TimeZoneInfo.Local;
        var country = new System.Globalization.CultureInfo("nl-NL");

        var query = new SpotifyHomeRequest(timeZone, country);
        var response = await _partnerApi.Query(query);
        var result = response
            .Data.Home.SectionContainer.Sections.Items
            .Where(x =>
            {
                var sectionName = x.Data.Typename;
                if (sectionName is "HomeShortsSectionData") return false; // TODO
                if (sectionName is "HomeSpotlightSectionData") return false; // TODO

                return true;
            })
            .SelectMany((x, i) =>
            {
                var sectionName = x.Data.Typename;
                var uri = x.Uri;

                bool isLazySection = false;
                if (x.Data is HomeRecentlyPlayedSectionData)
                {
                    uri = sectionName;
                    isLazySection = true;
                }

                HomeGroup group = new HomeGroup(Id: uri,
                    Title: x.Data.TitleText,
                    Pinned: false,
                    Order: i,
                    IsLazySection: isLazySection);

                var items = x.SectionItems.Items;
                int index = 0;
                foreach (var item in items)
                {
                    if (item is null) continue;
                    item.Group = group;
                    item.Order = index;
                    item.Key = new ComposedKey(uri, item.Item.Id);
                    index++;
                }

                return items;
            });

        return result.ToList();
    }

    public Task<IReadOnlyCollection<IHomeItem>> GetRecentlyPlayed(HomeGroup group, CancellationToken cancellation)
    {
        throw new System.NotImplementedException();
    }
}