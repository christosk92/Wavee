﻿using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using LanguageExt.UnsafeValueAccess;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;
using DynamicData;
using System.Collections.ObjectModel;
using System.Text.Json;
using LanguageExt;
using Wavee.Core.Ids;
using Wavee.UI.Models;
using Eum.Spotify.playlist4;

namespace Wavee.UI.ViewModels;

public sealed class LibraryViewModel<R> : ReactiveObject where R : struct, HasSpotify<R>
{
    //private readonly ReadOnlyObservableCollection<SpotifyLibaryItem> _libraryItemsView;
    private readonly string _userId;
    private readonly SourceCache<SpotifyLibaryItem, AudioId> _items = new(s => s.Id);
    public LibraryViewModel(R runtime,
        Action<Seq<AudioId>> onLibraryItemAdded,
        Action<Seq<AudioId>> onLibraryItemRemoved, string userId)
    {
        _userId = userId;
        var libraryObservable = Spotify<R>.ObserveLibrary()
            .Run(runtime)
            .ThrowIfFail()
            .ValueUnsafe()
            //.Throttle(TimeSpan.FromMilliseconds(50))
            .SelectMany(async c =>
            {
                if (c.Initial)
                {
                    var items = await FetchLibraryInitial(
                        runtime,
                        _userId,
                        CancellationToken.None);
                    return (items, true);
                }

                if (c.Removed)
                {
                    return (Seq1(new SpotifyLibaryItem(
                        Id: c.Item,
                        AddedAt: DateTimeOffset.MinValue
                    )), false);
                }

                var newItem = new SpotifyLibaryItem(
                    Id: c.Item,
                    AddedAt: c.AddedAt.ValueUnsafe()
                );
                return (Seq1(newItem), true);
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(itemsToAddOrRemove =>
            {
                _items.Edit(innerList =>
                {
                    innerList.Clear();
                    if (itemsToAddOrRemove.Item2)
                    {
                        innerList.AddOrUpdate(itemsToAddOrRemove.Item1);
                        onLibraryItemAdded(itemsToAddOrRemove.Item1.Select(c => c.Id));
                    }
                    else
                    {
                        innerList.Remove(itemsToAddOrRemove.Item1);
                        onLibraryItemRemoved(itemsToAddOrRemove.Item1.Select(c => c.Id));
                    }
                });
            });
    }

    private static async Task<Seq<SpotifyLibaryItem>> FetchLibraryInitial(R runtime,
        string userId,
        CancellationToken ct = default)
    {
        //hm://collection/collection/{id}?format=json
        var tracksAndAlbums = FetchLibraryComponent(runtime, "collection", userId, ct);
        var artists = FetchLibraryComponent(runtime, "artist", userId, ct);
        var results = await Task.WhenAll(tracksAndAlbums, artists);
        return results.SelectMany(c => c).ToSeq();
    }

    private static async Task<Seq<SpotifyLibaryItem>> FetchLibraryComponent(
        R runtime,
        string key, string userId, CancellationToken ct)
    {
        var aff =
            from mercury in Spotify<R>.Mercury()
            from tracksAndAlbums in mercury.Get(
                $"hm://collection/{key}/{userId}?allowonlytracks=false&format=json&",
                ct).ToAff()
            select tracksAndAlbums;

        var response = await aff.Run(runtime: runtime);
        var k = response.ThrowIfFail();
        /*
         * {
  "item": [{
    "type": "TRACK",
    "identifier": "AI+JEIUXSo+5Kt1X/ht3ow==",
    "added_at": 1672030273
  }, {
    "type": "TRACK",
    "identifier": "ARTcvj+fR+iFonI/WRYiEA==",
    "added_at": 1655035551
  }, {
         */
        using var doc = JsonDocument.Parse(k.Payload);
        using var items = doc.RootElement.GetProperty("item").EnumerateArray();
        var res = LanguageExt.Seq<SpotifyLibaryItem>.Empty;
        foreach (var item in items)
        {
            var type = item.GetProperty("type").GetString();
            // var id = item.GetProperty("identifier").GetString();
            //id is base64 encoded and needs to be decoded. 
            var audioId = AudioId.FromRaw(
                id: item.GetProperty("identifier").GetBytesFromBase64(),
                type: type switch
                {
                    "TRACK" => AudioItemType.Track,
                    "ALBUM" => AudioItemType.Album,
                    "ARTIST" => AudioItemType.Artist,
                    _ => throw new Exception("Unknown type")
                },
                ServiceType.Spotify
            );

            var addedAt = item.GetProperty("added_at").GetInt64();
            var spotifyLibaryItem = new SpotifyLibaryItem(audioId, DateTimeOffset.FromUnixTimeSeconds(addedAt));
            res = res.Add(spotifyLibaryItem);
        }


        return res;
    }

    public bool InLibrary(AudioId id)
    {
        return _items.Lookup(id).HasValue;
    }
}

public enum LibraryItemType
{
    Songs,
    Albums,
    Artists
}

public readonly record struct SpotifyLibaryItem(AudioId Id, DateTimeOffset AddedAt);