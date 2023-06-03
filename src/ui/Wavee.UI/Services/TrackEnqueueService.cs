using System.Collections.Concurrent;
using System.Diagnostics;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.Services;

public static class TrackEnqueueService<R> where R : struct, HasSpotify<R>
{
    private static readonly ConcurrentQueue<FetchItemsInBulk> _queue = new();
    public static R Runtime { get; set; }
    private static ManualResetEvent _waitForAnything = new ManualResetEvent(false);
    static TrackEnqueueService()
    {
        //fetch as many tracks as possible
        //so we need a sliding window of tracks.
        //if no tracks are added to the queue within 50ms, fetch the queue
        Task.Run(async () =>
        {
            while (true)
            {
                try
                {
                    _waitForAnything.WaitOne();
                    if (_queue.Count > 0)
                    {
                        //var fetchedTracks = await App.Runtime.Library.FetchTracks(tracks);
                        //we can fetch 1000 tracks at a time, so we need to split the array into chunks of 1000
                        _queue.TryDequeue(out var item);

                        var output = new Dictionary<AudioId, Option<TrackOrEpisode>>();
                        //check for items cached
                        var cachedItems = Spotify<R>
                            .GetFromCache(item.Request)
                            .Run(Runtime)
                            .ThrowIfFail();
                        foreach (var maybe in cachedItems.Where(x=> x.Value.IsSome))
                        {
                            output[maybe.Key] = maybe.Value.ValueUnsafe();
                        }
                        var toFetchItems = item.Request.Where(x => cachedItems[x].IsNone);
                        var batches = toFetchItems.Chunk(2000).Select(c => c.ToSeq());
                        foreach (var batch in batches)
                        {
                            var fetchedTracksResut = await Spotify<R>
                                .FetchBatchOfTracks(batch)
                                .Run(Runtime);

                            if (fetchedTracksResut.IsFail)
                            {
                                Debugger.Break();
                                continue;
                            }

                            var fetchedTracks = fetchedTracksResut.Match(Succ: x => x,
                                Fail: _ => throw new NotSupportedException());


                            foreach (var originalTrack in batch)
                            {
                                output[originalTrack] = fetchedTracks[originalTrack];
                            }
                            // foreach (var (originalRequest, fetchedTrack) in batch.Zip(fetchedTracks))
                            // {
                            //     originalRequest.CompletionSource.SetResult(fetchedTrack);
                            // }
                        }

                        item.Result.SetResult(output);
                        if (_queue.Count == 0)
                        {
                            _waitForAnything.Reset();
                        }
                    }
                }
                catch (Exception x)
                {
                    Debug.WriteLine(x);
                }
            }
        });
    }

    public static async Task<Dictionary<AudioId, Option<TrackOrEpisode>>> GetTracks(Seq<AudioId> ids)
    {
        var tcs = new TaskCompletionSource<Dictionary<AudioId, Option<TrackOrEpisode>>>();
        _queue.Enqueue(new FetchItemsInBulk
        {
            Request = ids,
            Result = tcs
        });
        _waitForAnything.Set();
        return await tcs.Task;
    }
    // public static async Task<TrackOrEpisode> GetTrack(AudioId id)
    // {
    //     var tcs = new TaskCompletionSource<TrackOrEpisode>();
    //     _queue.Enqueue(new TrackQueueItem(id, tcs));
    //     _waitForAnything.Set();
    //     return await tcs.Task;
    // }
}

internal sealed class FetchItemsInBulk
{
    public TaskCompletionSource<Dictionary<AudioId, Option<TrackOrEpisode>>> Result { get; set; }
    public Seq<AudioId> Request { get; set; }
}

// internal record TrackQueueItem(AudioId Id,
//     TaskCompletionSource<TrackOrEpisode> CompletionSource);