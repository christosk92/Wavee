using System.Collections.Concurrent;
using System.Diagnostics;
using Wavee.Core.Contracts;
using Wavee.Core.Ids;
using Wavee.Spotify.Infrastructure.Playback;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.Services;

public static class TrackEnqueueService<R> where R : struct, HasSpotify<R>
{
    private static readonly ConcurrentQueue<TrackQueueItem> _queue = new();
    public static R Runtime { get; set; }
    static TrackEnqueueService()
    {
        //fetch as many tracks as possible
        //so we need a sliding window of tracks.
        //if no tracks are added to the queue within 50ms, fetch the queue
        Task.Run(async () =>
        {
            var currentBuffer = new List<TrackQueueItem>();
            while (true)
            {
                try
                {
                    await Task.Delay(50);
                    var currentCount = _queue.Count;
                    if (currentCount == 0 && currentBuffer.Count > 0)
                    {
                        //var fetchedTracks = await App.Runtime.Library.FetchTracks(tracks);
                        //we can fetch 1000 tracks at a time, so we need to split the array into chunks of 1000
                        var batches = currentBuffer.Chunk(1000);
                        foreach (var batch in batches)
                        {
                            var fetchedTracksResut = await Spotify<R>
                                .FetchBatchOfTracks(batch.Select(x => x.Id).ToSeq())
                                .Run(Runtime);

                            if (fetchedTracksResut.IsFail)
                            {
                                Debugger.Break();
                                continue;
                            }

                            var fetchedTracks = fetchedTracksResut.Match(Succ: x => x,
                                Fail: _ => throw new NotSupportedException());

                            for (int i = 0; i < batch.Length; i++)
                            {
                                var originalTrack = batch[i];
                                var potentialTrack = fetchedTracks
                                    .FirstOrDefault(x => x.Id == originalTrack.Id);
                                if (potentialTrack.Id.Id.IsZero)
                                {
                                    Debugger.Break();
                                }
                                else
                                {
                                    if (!originalTrack.CompletionSource.TrySetResult(potentialTrack))
                                    {
                                        Debugger.Break();
                                    }
                                }
                            }
                            // foreach (var (originalRequest, fetchedTrack) in batch.Zip(fetchedTracks))
                            // {
                            //     originalRequest.CompletionSource.SetResult(fetchedTrack);
                            // }
                        }

                        currentBuffer.Clear();
                    }
                    else
                    {
                        for (var i = 0; i < currentCount; i++)
                        {
                            _queue.TryDequeue(out var item);
                            currentBuffer.Add(item);
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

    public static async Task<TrackOrEpisode> GetTrack(AudioId id)
    {
        var tcs = new TaskCompletionSource<TrackOrEpisode>();
        _queue.Enqueue(new TrackQueueItem(id, tcs));
        return await tcs.Task;
    }
}

internal record TrackQueueItem(AudioId Id,
    TaskCompletionSource<TrackOrEpisode> CompletionSource);