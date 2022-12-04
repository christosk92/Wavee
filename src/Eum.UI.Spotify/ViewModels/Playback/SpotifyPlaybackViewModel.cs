using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback;
using Eum.Enums;
using Eum.Spotify.connectstate;
using Eum.Spotify.metadata;
using Eum.UI.Items;
using Eum.UI.Playlists;
using Eum.UI.Services.Directories;
using Eum.UI.Services.Playlists;
using Eum.UI.ViewModels.Playback;
using Eum.UI.ViewModels.Playlists;
using Flurl;
using ReactiveUI;

namespace Eum.UI.Spotify.ViewModels.Playback;
public class SpotifyPlaybackViewModel : PlaybackViewModel
{
    private readonly ISpotifyClient _spotifyClient;
    private readonly ISpotifyPlaybackClient _spotifyPlaybackClient;
    private readonly IDisposable _disposable;
    public SpotifyPlaybackViewModel(ISpotifyPlaybackClient spotifyPlaybackClient, ISpotifyClient spotifyClient)
    {
        _spotifyPlaybackClient = spotifyPlaybackClient;
        _spotifyClient = spotifyClient;

        _disposable = Observable
            .FromEventPattern<ClusterUpdate>(_spotifyPlaybackClient, nameof(ISpotifyPlaybackClient.ClusterChanged))
            .SelectMany(async x =>
            {
                if (x.EventArgs.Cluster?.PlayerState?.Track?.Uri?.StartsWith("spotify:track") ?? false)
                {
                    var uri = new SpotifyId(x.EventArgs.Cluster.PlayerState.Track.Uri);
                    var original =
                        _tracksCache.TryGetValue(uri.HexId(), out var data)
                            ? data
                            : await _spotifyClient.Tracks.MercuryTracks.GetTrack(uri.HexId());
                    _tracksCache[uri.HexId()] = original;
                    return (x.EventArgs, original, null as Episode);
                }
                else if (x.EventArgs.Cluster?.PlayerState?.Track?.Uri?.StartsWith("spotify:local") ?? false)
                {
                    //local track:
                    //spotify:local:{artist}:{album_title}:{track_title}:{duration_in_seconds}


                    var uri = new SpotifyId(x.EventArgs.Cluster.PlayerState.Track.Uri);
                    var trackData = x.EventArgs.Cluster.PlayerState.Track.Uri.Split(":")
                        .Skip(2).ToArray();

                    var artist = Url.Decode(trackData[0], true);
                    var album = Url.Decode(trackData[1], true);
                    var title = Url.Decode(trackData[2], true);
                    var duration = double.Parse(trackData[3]);

                    return (x.EventArgs, new Track
                    {
                        Name = title,
                        Album = new Album
                        {
                            Name = album,

                        },
                        Artist =
                        {
                            new Artist
                            {
                                Name = artist
                            }
                        },
                        Duration = (int)duration
                    }, null);
                }
                return (null, null, null)!;
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(clusterChanged);
    }
    private static string MakeValidFileName(string name)
    {
        string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
    }
    private void clusterChanged((ClusterUpdate? EventArgs, Track? original, Episode? episode) obj)
    {
        var e = obj.EventArgs;
        if (e?.Cluster == null)
        {
            //clear playback state
            return;
        }

        var playingUri = new SpotifyId(e.Cluster.PlayerState.Track.Uri);
        if (!e.Cluster.PlayerState.Track.Metadata.Any())
        {
            //if not we fetch!
            throw new NotImplementedException();
        }

        var original = obj.original;

        string smallImage = default;
        string bigImage = default;
        if (playingUri.Type != EntityType.Local)
        {
            smallImage = e.Cluster.PlayerState.Track.Metadata["image_small_url"].Split(":").Last();
            bigImage = e.Cluster.PlayerState.Track.Metadata["image_large_url"].Split(":").Last();

            bigImage = $"https://i.scdn.co/image/{bigImage}";
            smallImage = $"https://i.scdn.co/image/{smallImage}";
        }
        else
        {
            //try fetch the file on disk
            var smallImagePath
                = Url.Decode(e.Cluster.PlayerState.Track.Metadata["image_small_url"].Split(":").Last(), true);
            var bigImagePath
                = Url.Decode(e.Cluster.PlayerState.Track.Metadata["image_large_url"].Split(":").Last(), true);

            if (File.Exists(bigImagePath))
            {
                var cleanFileName = $"{MakeValidFileName(original.Album?.Name ?? original.Name)}-big";
                var saveto = Path.Combine(Ioc.Default.GetRequiredService<ICommonDirectoriesProvider>()
                    .WorkDir, cleanFileName);
                if (File.Exists(saveto))
                {
                    bigImage = saveto;
                }
                else
                {
                    using var tfile = TagLib.File.Create(bigImagePath);
                    if (tfile.Tag.Pictures.Length > 0)
                    {
                        TagLib.IPicture pic = tfile.Tag.Pictures[0];
                        using var ms = new MemoryStream(pic.Data.Data);
                        ms.Seek(0, SeekOrigin.Begin);

                        using var fs = File.Open(saveto, FileMode.OpenOrCreate);
                        ms.CopyTo(fs);
                        bigImage = saveto;
                    }
                }
            }
            if (File.Exists(smallImagePath))
            {
                var cleanFileName = $"{MakeValidFileName(original.Album?.Name ?? original.Name)}-small";
                var saveto = Path.Combine(Ioc.Default.GetRequiredService<ICommonDirectoriesProvider>()
                    .WorkDir, cleanFileName);
                if (File.Exists(saveto))
                {
                    smallImage = saveto;
                }
                else
                {
                    using var tfile = TagLib.File.Create(smallImagePath);
                    if (tfile.Tag.Pictures.Length > 0)
                    {
                        TagLib.IPicture pic = tfile.Tag.Pictures[0];
                        using var ms = new MemoryStream(pic.Data.Data);
                        ms.Seek(0, SeekOrigin.Begin);


                        using var fs = File.Open(saveto, FileMode.OpenOrCreate);
                        ms.CopyTo(fs);
                        smallImage = saveto;
                    }
                }
            }
        }

        var albumName = e.Cluster.PlayerState.Track.Metadata["album_title"];
        var albumId =
            e.Cluster.PlayerState.Track.Metadata.ContainsKey("album_uri") ?
            new ItemId(e.Cluster.PlayerState.Track.Metadata["album_uri"]) : default;
        var contextId = new ItemId(e.Cluster.PlayerState.ContextUri);
        var duration = e.Cluster.PlayerState.Duration;


        Item = new CurrentlyPlayingHolder
        {
            Title = new IdWithTitle
            {
                Id = albumId,
                Title = original.Name
            },
            Artists = original.Artist.Select(a => new IdWithTitle
            {
                Title = a.Name
            }).ToArray(),
            BigImage = bigImage,
            SmallImage =smallImage,
            Duration = duration,
            Context = contextId
        };
        int diff = (int)(_spotifyClient.TimeProvider.CurrentTimeMillis() - e.Cluster.PlayerState.Timestamp);
        var initial = Math.Max(0, (int)(e.Cluster.PlayerState.PositionAsOfTimestamp + diff));
        StartTimer(initial);
    }

    private static readonly ConcurrentDictionary<string, Track> _tracksCache = new ConcurrentDictionary<string, Track>();


    public override void Deconstruct()
    {
        _disposable.Dispose();
    }

    public override ServiceType Service => ServiceType.Spotify;
}