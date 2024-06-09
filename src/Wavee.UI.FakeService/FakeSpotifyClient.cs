using DynamicData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Clients;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.Contracts.Models;
using Wavee.UI.Spotify;
using Wavee.UI.Spotify.Responses.Parsers;
using Wavee.UI.ViewModels.Home;

namespace Wavee.UI.FakeService;

public sealed class FakeSpotifyClient : IAccountClient
{
    public FakeSpotifyClient()
    {
        Home = new FakeHomeClient();
        Color = new FakeColorClient();
    }

    public IHomeClient Home { get; }
    public IColorClient Color { get; }
}

internal sealed class FakeColorClient : IColorClient
{
    public Task FetchColors(Dictionary<string, string?> output, CancellationToken cancellation)
    {
        // Return random colors
        var random = new Random();
        foreach (var key in output.Keys.ToList())
        {
            if (!string.IsNullOrEmpty(output[key])) continue;
            output[key] = $"#{random.Next(0x1000000):X6}";
        }

        return Task.CompletedTask;
        // var toFetchKeys = output.Where(x=> string.IsNullOrEmpty(x.Value))
        //     .Select(x=> x.Key)
        //     .ToList();
        //
        // var 
    }
}

internal sealed class FakeHomeClient : IHomeClient
{
    public async Task<IReadOnlyCollection<IHomeItem>> GetItems(CancellationToken cancellation)
    {
        var file = Path.Combine(AppContext.BaseDirectory, "Responses", "HomeResponse.json");
        await using var stream = File.OpenRead(file);
        using var jsonDocument = await JsonDocument.ParseAsync(stream, default, cancellation);
        var root = jsonDocument.RootElement;
        //data -> home -> sectionContainer -> sections -> items
        var items = root.GetProperty("data").GetProperty("home").GetProperty("sectionContainer").GetProperty("sections")
            .GetProperty("items");
        return ParseItems(items);
    }

    public async Task<IReadOnlyCollection<IHomeItem>> GetRecentlyPlayed(HomeGroup group, CancellationToken cancellation)
    {
        var fileOne = Path.Combine(AppContext.BaseDirectory, "Responses", "RecentlyPlayedOne.json");
        await using var streamOne = File.OpenRead(fileOne);
        using var jsonDocumentOne = await JsonDocument.ParseAsync(streamOne, default, cancellation);
        var rootOne = jsonDocumentOne.RootElement;
        var items = rootOne.GetProperty("playContexts").ParseSpotifyPlayContexts(50);

        var fileTwo = Path.Combine(AppContext.BaseDirectory, "Responses", "RecentlyPlayedTwo.json");
        await using var streamTwo = File.OpenRead(fileTwo);
        using var jsonDocumentTwo = await JsonDocument.ParseAsync(streamTwo, default, cancellation);
        var rootTwo = jsonDocumentTwo.RootElement;
        using var parsedItems = rootTwo.GetProperty("data").GetProperty("lookup").EnumerateArray();
        var output = new List<(IHomeItem, DateTimeOffset)>();
        while (parsedItems.MoveNext())
        {
            var rootItem = parsedItems.Current;
            var item = rootItem.GetProperty("data");
            var uri = item.GetProperty("uri").GetString();
            var key = new ComposedKey("HomeRecentlyPlayedSectionData", uri);
            var parsed = item.ParseHomeItem(group, key);
            if (parsed is null) continue;
            var contextItem = items.FirstOrDefault(x => x.Uri == uri);
            if (contextItem == default) continue;
            output.Add((parsed, contextItem.PlayedAt));
        }

        return output
            .OrderByDescending(x => x.Item2)
            .Select((x, i) =>
            {
                var item = x.Item1;
                item.Order = i;
                return x.Item1;
            })
            .ToList();
    }

    private IReadOnlyCollection<IHomeItem> ParseItems(JsonElement items)
    {
        var itemsOutput = new List<IHomeItem>();
        using var enumerator = items.EnumerateArray();
        int groupIndex = 0;
        while (enumerator.MoveNext())
        {
            var sectionRoot = enumerator.Current;

            var uri = sectionRoot.GetProperty("uri").GetString();
            var sectionName = sectionRoot
                .GetProperty("data")
                .GetProperty("__typename")
                .GetString();

            bool isLazySection = false;
            if (sectionName is "HomeShortsSectionData") continue; // TODO
            if (sectionName is "HomeSpotlightSectionData") continue; // TODO
            if (sectionName is "HomeRecentlyPlayedSectionData")
            {
                uri = sectionName;
                isLazySection = true;
            }

            var itemsArray = sectionRoot.GetProperty("sectionItems").GetProperty("items");
            using var itemsEnumerator = itemsArray.EnumerateArray();
            HomeGroup group = new HomeGroup(Id: uri,
                Title: sectionRoot.GetProperty("data").GetProperty("title").GetProperty("text").GetString(),
                Pinned: false,
                Order: groupIndex,
                IsLazySection: isLazySection);

            int index = 0;
            while (itemsEnumerator.MoveNext())
            {
                var itemElement = itemsEnumerator.Current;
                var content = itemElement.GetProperty("content").GetProperty("data");
                var itemUri = itemElement.GetProperty("uri").GetString();
                var key = new ComposedKey(uri, itemUri);
                var parsed = content.ParseHomeItem(group, key);
                if (parsed is null) continue;
                itemsOutput.Add(parsed);
                index++;
            }

            groupIndex++;
        }

        return itemsOutput;
    }
}