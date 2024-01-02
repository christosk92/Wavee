using Eum.Spotify.context;
using Eum.Spotify.transfer;
using Google.Protobuf;
using Wavee.Core.Enums;
using Wavee.Core.Playback;
using Wavee.Interfaces;
using Wavee.Spotify.Core.Exceptions;
using Wavee.Spotify.Core.Models.Common;

namespace Wavee.Spotify.Core.Playback;

public class SpotifyContextBuilder
{
    internal delegate Func<CancellationToken, Task<Context>> ContextFactory(IWaveeSpotifyClient client);

    private ContextFactory? _contextFactory;

    public static SpotifyContextBuilder New()
    {
        return new();
    }

    public SpotifyArtistContextBuilder FromArtist(SpotifyId id)
    {
        if (id.Type is not AudioItemType.Artist)
            throw new ArgumentException("Id must be of type artist", nameof(id));

        _contextFactory = (client) =>
        {
            return async (ct) =>
            {
                var context = await client.Context.ResolveContext(id.ToString(), ct);
                return context;
            };
        };

        return new(_contextFactory);
    }

    public sealed class SpotifyArtistContextBuilder
    {
        internal delegate Func<Task<ContextPage?>> Page(IWaveeSpotifyClient client);

        private readonly ContextFactory _contextFactory;
        private Page? _page;

        internal SpotifyArtistContextBuilder(ContextFactory contextFactory)
        {
            _contextFactory = contextFactory;
        }

        public TrackSpecificBuilder FromTopTracks()
        {
            _page = (client) =>
            {
                return async () =>
                {
                    var context = await _contextFactory(client)(CancellationToken.None);
                    // top tracks is the first page
                    var page = context.Pages.FirstOrDefault();
                    return page;
                };
            };

            return new TrackSpecificBuilder(_page);
        }
        
        public TrackSpecificBuilder FromAlbum(SpotifyId id)
        {
            if (id.Type is not AudioItemType.Album)
                throw new ArgumentException("Id must be of type album", nameof(id));

            _page = (client) =>
            {
                return async () =>
                {
                    var context = await _contextFactory(client)(CancellationToken.None);
                    //hm://artistplaycontext/v1/page/spotify/album/1cBT3tlQ0YlRIp7nCIb8qT/km_artist
                    var uri = $"hm://artistplaycontext/v1/page/spotify/album/{id.ToBase62()}/km_artist";
                    var page = context.Pages.FirstOrDefault(p => p.PageUrl == uri || p.Tracks.Any(f=> IsInAlbumContext(f, id)));
                    if (page is null)
                    {
                        var innerException = new Exception("Could not find album in context with id " + id);
                        throw new SpotifyCannotPlayContentException(SpotifyCannotPlayReason.InvalidContext, innerException);
                    }

                    if (page.Tracks.Count > 0)
                    {
                        var old = page.Tracks.ToList();
                        page.Tracks.Clear();
                        page.Tracks.AddRange(old.Where(f=> IsInAlbumContext(f, id)));
                    }
                    
                    if (page.Tracks.Count is 0 && !string.IsNullOrEmpty(page.PageUrl)) 
                    {
                        // fetch tracks
                        var pageUrl = page.PageUrl;
                        //replace hm:// with empty
                        pageUrl = pageUrl.Replace("hm://", "");
                        var tracks = await client.Context.ResolveContextRaw(pageUrl, CancellationToken.None);
                        return tracks;
                    }

                    return page;
                };
            };

            return new TrackSpecificBuilder(_page);
        }

        private static bool IsInAlbumContext(ContextTrack contextTrack, SpotifyId albumUri)
        {
            //uid: albumIdtrackId
            var uid = contextTrack.Uid;
            var albumId = albumUri.ToBase62();
            var trackId = SpotifyId.FromUri(contextTrack.Uri).ToBase62();

            return string.Equals(uid, $"{albumId}{trackId}", StringComparison.OrdinalIgnoreCase);
        }
    }

    public sealed class TrackSpecificBuilder
    {
        private readonly SpotifyArtistContextBuilder.Page _page;
        private Func<IWaveeSpotifyClient, Task<(WaveePlaybackItem[], int)>>? _items;

        internal TrackSpecificBuilder(SpotifyArtistContextBuilder.Page page)
        {
            _page = page;
        }

        public FinalizedBuilder StartFromIndex(int index)
        {
            // TODO: Adapt to MediaSource factory
            _items = async (client) =>
            {
                var page = await _page(client)();
                var items = page!.Tracks.Select(item =>
                {
                    var metadata = item.Metadata;
                    metadata["uid"] = item.Uid;
                    return new WaveePlaybackItem
                    {
                        Factory = async () => await client.Playback.CreateStream(SpotifyId.FromUri(item.Uri)),
                        Id = item.Uri,
                        Metadata = metadata
                    };
                }).ToArray();
                return (items ?? Array.Empty<WaveePlaybackItem>(), index);
            };
            return new FinalizedBuilder(_items);
        }
    }

    public FinalizedBuilder FromTransferState(TransferState stateToTransfer)
    {
        var ctx = stateToTransfer.CurrentSession.Context;
        var items = new System.Func<IWaveeSpotifyClient, Task<(WaveePlaybackItem[], int)>>(async (client) =>
        {
            var (allTracks, trackIndex, pageIndex, absoluteTrackIndex) =
                await createTrackList(client, ctx, stateToTransfer);

            return (allTracks.ToArray(), absoluteTrackIndex);
        });

        return new(items);
    }
    

    public sealed class FinalizedBuilder
    {
        private readonly Func<IWaveeSpotifyClient, Task<(WaveePlaybackItem[], int)>>? _items;

        public FinalizedBuilder(Func<IWaveeSpotifyClient, Task<(WaveePlaybackItem[], int)>>? items)
        {
            _items = items;
        }

        public WaveePlaybackList Build(IWaveeSpotifyClient client)
        {
            return WaveePlaybackList.Create(() => _items!(client));
        }
    }
    
     private async Task<(IReadOnlyCollection<WaveePlaybackItem> allTracks, int trackIndex, int pageIndex, int
            absoluteTrackIndex)>
        createTrackList(IWaveeSpotifyClient client, Context ctx, TransferState stateToTransfer)
    {
        static int FindIndex(ContextPage page, ByteString? trackGid, string? trackUid, string? trackUri)
        {
            var index = 0;
            foreach (var track in page.Tracks)
            {
                if (trackGid is not null && track.Gid == trackGid)
                    return index;
                else if (trackUid is not null && track.Uid == trackUid)
                    return index;
                else if (trackUri is not null && track.Uri == trackUri)
                    return index;
                index++;
            }

            return -1;
        }

        static async Task<(int PageIndex, int IndexInPage, int absoluteTrackIndex)> findPage(Context context,
            IWaveeSpotifyClient client,
            TransferState statetotransfer1,
            int seenTracks)
        {
            if (context.Pages.Count is 0)
            {
                if (context.Url.StartsWith("context://spotify:track:"))
                {
                    //for sake, add the track to the context
                    var ctxTrakc = new ContextTrack
                    {
                        Uid = string.Empty,
                        Uri = context.Uri,
                        Gid = ByteString.CopyFrom(SpotifyId.FromUri(context.Uri).ToRaw()),
                    };
                    foreach (var track in statetotransfer1.Playback.CurrentTrack.Metadata)
                    {
                        ctxTrakc.Metadata.Add(track.Key, track.Value);
                    }

                    ctxTrakc.Uid = statetotransfer1.CurrentSession.CurrentUid;
                    ctxTrakc.Metadata["uid"] = statetotransfer1.CurrentSession.CurrentUid;
                    context.Pages.Add(new ContextPage
                    {
                        Tracks =
                        {
                            ctxTrakc
                        }
                    });
                    return (0, 0, seenTracks);
                }
                else
                {
                    //not good, no pages , so we need to fetch them
                    var pages = await client.Context.ResolveContext(context.Uri, CancellationToken.None);
                    foreach (var page in pages.Pages)
                    {
                        context.Pages.Add(page);
                    }

                    // retry
                    return await findPage(context, client, statetotransfer1, seenTracks);
                }
            }
            else
            {
                for (var index = 0; index < context.Pages.Count; index++)
                {
                    var page = context.Pages[index];

                    //check if we have a track in this page
                    var trackGid = statetotransfer1.Playback.CurrentTrack.Gid;
                    var trackUid = statetotransfer1.Playback.CurrentTrack.Uid;
                    var trackUri = statetotransfer1.Playback.CurrentTrack.Uri;
                    var x = FindIndex(page, null, trackUid, null);
                    if (x is -1)
                    {
                        x = FindIndex(page, trackGid, null, null);
                        if (x is -1)
                        {
                            x = FindIndex(page, null, null,
                                SpotifyId.FromRaw(trackGid.Span, AudioItemType.Track).ToString());
                        }
                    }

                    if (x is not -1)
                    {
                        //Good, we found it!
                        return (index, x, seenTracks + x);
                    }

                    //check if this page needs to be fetched
                    if (!string.IsNullOrEmpty(page.NextPageUrl))
                    {
                        var tracks =
                            await client.Context.ResolveContextRaw(page.NextPageUrl, CancellationToken.None);
                        page.Tracks.AddRange(tracks.Tracks);
                        page.NextPageUrl = tracks.NextPageUrl;

                        // Try again
                        return await findPage(context, client, statetotransfer1, seenTracks + page.Tracks.Count);
                    }
                    else if (!string.IsNullOrEmpty(page.PageUrl))
                    {
                        
                        var pageUrl = page.PageUrl.Replace("hm://", "");
                        var tracks =
                            await client.Context.ResolveContextRaw(pageUrl, CancellationToken.None);
                        page.Tracks.AddRange(tracks.Tracks);
                        page.PageUrl = string.Empty;
                        page.NextPageUrl = tracks.NextPageUrl;

                        // Try again
                        return await findPage(context, client, statetotransfer1, seenTracks + page.Tracks.Count);
                    }
                }
            }

            // Give up
            throw new Exception("Could not find track in context");
        }

        var (pageIndex, trackIndex, absoluteTrackIndex) = await findPage(ctx, client, stateToTransfer, 0);
        var allTracks = ctx.Pages.SelectMany(p => p.Tracks).Select(t =>
        {
            var id = !string.IsNullOrEmpty(t.Uri)
                ? SpotifyId.FromUri(t.Uri)
                : SpotifyId.FromRaw(t.Gid.Span, AudioItemType.Track);
            if (t.HasUid && !string.IsNullOrEmpty(t.Uid))
            {
                t.Metadata["uid"] = t.Uid;
            }

            return new WaveePlaybackItem
            {
                Factory =
                    async () => await client.Playback.CreateStream(id),
                Id = id.ToString(),
                Metadata = t.Metadata
            };
        }).ToList();

        return (allTracks, trackIndex, pageIndex, absoluteTrackIndex);
    }
    
}