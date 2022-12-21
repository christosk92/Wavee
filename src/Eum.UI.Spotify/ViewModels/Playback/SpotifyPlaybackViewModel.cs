using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Compression;
using System.Linq;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using ColorThiefDotNet;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback;
using Eum.Enums;
using Eum.Logging;
using Eum.Spotify.connectstate;
using Eum.Spotify.metadata;
using Eum.UI.Helpers;
using Eum.UI.Items;
using Eum.UI.Playlists;
using Eum.UI.Services.Directories;
using Eum.UI.Services.Playlists;
using Eum.UI.Services.Tracks;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Playback;
using Eum.UI.ViewModels.Playlists;
using Flurl;
using Flurl.Http;
using LiteDB;
using MoreLinq.Extensions;
using Org.BouncyCastle.Utilities.Encoders;
using ReactiveUI;
using Color = System.Drawing.Color;

namespace Eum.UI.Spotify.ViewModels.Playback;
public class SpotifyPlaybackViewModel : PlaybackViewModel
{
    private readonly ISpotifyClient _spotifyClient;
    private readonly ISpotifyPlaybackClient _spotifyPlaybackClient;
    private readonly IDisposable _disposable;
    public SpotifyPlaybackViewModel(ISpotifyPlaybackClient spotifyPlaybackClient, ISpotifyClient spotifyClient,
        ITrackAggregator trackAggregator)
    {
        _spotifyPlaybackClient = spotifyPlaybackClient;
        _spotifyClient = spotifyClient;

        _disposable = Observable
            .FromEventPattern<ClusterUpdate>(_spotifyPlaybackClient, nameof(ISpotifyPlaybackClient.ClusterChanged))
            .SelectMany(async x =>
            {
                if (x.EventArgs.Cluster.PlayerState.Track == null)
                {
                    return (x.EventArgs, null);
                }
                try
                {
                    var track = await Task.Run(async () => await trackAggregator.GetTrack(new ItemId(x.EventArgs.Cluster.PlayerState.Track.Uri)));
                    return (x.EventArgs, track);
                }
                catch (Exception ex)
                {
                    S_Log.Instance.LogError(ex);
                    return (x.EventArgs, null);
                }
            })
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(clusterChanged)
            .Subscribe();
    }
    private static string MakeValidFileName(string name)
    {
        string invalidChars = System.Text.RegularExpressions.Regex.Escape(new string(System.IO.Path.GetInvalidFileNameChars()));
        string invalidRegStr = string.Format(@"([{0}]*\.+$)|([{0}]+)", invalidChars);

        return System.Text.RegularExpressions.Regex.Replace(name, invalidRegStr, "_");
    }
    private async Task clusterChanged((ClusterUpdate? EventArgs, EumTrack item) obj)
    {
        var e = obj.EventArgs;
        if (e?.Cluster == null)
        {
            //clear playback state
            return;
        }

        if (obj.item == null)
        {
            Item?.Dispose();
            Item = null;
            OnPlayingItemChanged(default);
            return;
        }
        var playingUri = new SpotifyId(e.Cluster.PlayerState.Track.Uri);
        // if (!e.Cluster.PlayerState.Track.Metadata.Any())
        // {
        //     //if not we fetch!
        //     throw new NotImplementedException();
        // }
        //
        // var original = obj.original;
        //
        // string smallImage = default;
        // string bigImage = default;
        // if (playingUri.Type != EntityType.Local)
        // {
        //     smallImage = e.Cluster.PlayerState.Track.Metadata["image_small_url"].Split(":").Last();
        //     bigImage = e.Cluster.PlayerState.Track.Metadata["image_large_url"].Split(":").Last();
        //
        //     bigImage = $"https://i.scdn.co/image/{bigImage}";
        //     smallImage = $"https://i.scdn.co/image/{smallImage}";
        // }
        // else
        // {
        //     //try fetch the file on disk
        //     var smallImagePath
        //         = Url.Decode(e.Cluster.PlayerState.Track.Metadata["image_small_url"].Split(":").Last(), true);
        //     var bigImagePath
        //         = Url.Decode(e.Cluster.PlayerState.Track.Metadata["image_large_url"].Split(":").Last(), true);
        //
        //     if (File.Exists(bigImagePath))
        //     {
        //         var cleanFileName = $"{MakeValidFileName(original.Album?.Name ?? original.Name)}-big";
        //         var saveto = Path.Combine(Ioc.Default.GetRequiredService<ICommonDirectoriesProvider>()
        //             .WorkDir, cleanFileName);
        //         if (File.Exists(saveto))
        //         {
        //             bigImage = saveto;
        //         }
        //         else
        //         {
        //             using var tfile = TagLib.File.Create(bigImagePath);
        //             if (tfile.Tag.Pictures.Length > 0)
        //             {
        //                 TagLib.IPicture pic = tfile.Tag.Pictures[0];
        //                 using var ms = new MemoryStream(pic.Data.Data);
        //                 ms.Seek(0, SeekOrigin.Begin);
        //
        //                 using var fs = File.Open(saveto, FileMode.OpenOrCreate);
        //                 ms.CopyTo(fs);
        //                 bigImage = saveto;
        //             }
        //         }
        //     }
        //     if (File.Exists(smallImagePath))
        //     {
        //         var cleanFileName = $"{MakeValidFileName(original.Album?.Name ?? original.Name)}-small";
        //         var saveto = Path.Combine(Ioc.Default.GetRequiredService<ICommonDirectoriesProvider>()
        //             .WorkDir, cleanFileName);
        //         if (File.Exists(saveto))
        //         {
        //             smallImage = saveto;
        //         }
        //         else
        //         {
        //             using var tfile = TagLib.File.Create(smallImagePath);
        //             if (tfile.Tag.Pictures.Length > 0)
        //             {
        //                 TagLib.IPicture pic = tfile.Tag.Pictures[0];
        //                 using var ms = new MemoryStream(pic.Data.Data);
        //                 ms.Seek(0, SeekOrigin.Begin);
        //
        //
        //                 using var fs = File.Open(saveto, FileMode.OpenOrCreate);
        //                 ms.CopyTo(fs);
        //                 smallImage = saveto;
        //             }
        //         }
        //     }
        // }
        //
        // var albumName = e.Cluster.PlayerState.Track.Metadata["album_title"];
        // var albumId =
        //     e.Cluster.PlayerState.Track.Metadata.ContainsKey("album_uri") ?
        //     new ItemId(e.Cluster.PlayerState.Track.Metadata["album_uri"]) : default;
        // var contextId = new ItemId(e.Cluster.PlayerState.ContextUri);
        // var duration = e.Cluster.PlayerState.Duration;
        //
        //
        try
        {
            var contextId = new ItemId(e.Cluster.PlayerState.ContextUri);
            var duration = e.Cluster.PlayerState.Duration;
            Item?.Dispose();
            Item = new CurrentlyPlayingHolder
            {
                Title = new IdWithTitle
                {
                    Id = obj.item.Group.Id,
                    Title = obj.item.Name
                },
                Artists = obj.item.Artists,
                Id = obj.item.Id,
                BigImage = (await obj.item.Images.OrderByDescending(a => a.Height ?? 0).First().ImageStream),
                SmallImage = (await obj.item.Images.OrderBy(a => a.Height ?? 0).First().ImageStream),
                BigImageUrl = new Uri($"https://i.scdn.co/image/{obj.item.Images.OrderByDescending(a => a.Height ?? 0).First().Id.ToLower()}"),
                Duration = obj.item.Duration,
                Context = contextId
            };
            OnPlayingItemChanged(Item.Id);
            int diff = (int)(_spotifyClient.TimeProvider.CurrentTimeMillis() - e.Cluster.PlayerState.Timestamp);
            var initial = Math.Max(0, (int)(e.Cluster.PlayerState.PositionAsOfTimestamp + diff));
            StartTimer(initial);


            PlayingOnExternalDevice = e.Cluster.ActiveDeviceId != _spotifyClient
                .Config.DeviceId;
            if (PlayingOnExternalDevice)
            {
                RemoteDevices.Clear();

                var currentDevice = e.Cluster.Device[_spotifyClient.Config.DeviceId];
                RemoteDevices.Add(new RemoteDevice(currentDevice.DeviceId, currentDevice.Name, currentDevice.DeviceType, Service));
                foreach (var deviceInfo in e.Cluster.Device)
                {
                    if (deviceInfo.Key == currentDevice.DeviceId) continue;

                    RemoteDevices.Add(new RemoteDevice(deviceInfo.Key, deviceInfo.Value.Name, deviceInfo.Value.DeviceType, Service));
                }

                ExternalDevice = RemoteDevices.First(a => a.DeviceId == e.Cluster.ActiveDeviceId);
            }
            else
            {
                ExternalDevice = null;
            }

            if (Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User.ThemeService
                    .Glaze == "Playback Dependent")
            {
                Ioc.Default.GetRequiredService<MainViewModel>()
                    .Glaze = await GetColorFromAlbumArt();
            }
            async Task<string> GetColorFromAlbumArt()
            {
                if (Item?.BigImage != null)
                {
                    return await Task.Run(async () =>
                    {
                        using var fs = await Ioc.Default.GetRequiredService<IFileHelper>()
                            .GetStreamForString(Item?.BigImageUrl.ToString(), default);
                        using var bmp = new Bitmap(fs);
                        var colorThief = new ColorThief();
                        var c = colorThief.GetPalette(bmp);

                        var a =
                            c[0].Color.ToHexString();

                        var f = a.ToColor();
                        return (Color.FromArgb(25, f.R, f.G, f.B)).ToHex();
                    });
                }

                return "#00000000";
            }
        }
        catch (Exception ex)
        {
            var db = Ioc.Default.GetRequiredService<ILiteDatabase>();

            try
            {
                var rebuild = db.Rebuild();
            }
            catch (Exception j)
            {
                //"Cannot insert duplicate key in unique index '_id'. The duplicate value is '{\"f\":\"ab67616d0000b273be7757567d0c3dc3c98c8ba2\",\"n\":0}'."
                if (j.Message.StartsWith("Cannot insert duplicate key"))
                {
                    var findAll = db.FileStorage.FindAll();
                    foreach (var liteFileInfo in findAll)
                    {
                        db.FileStorage.Delete(liteFileInfo.Id);
                    }
                }
            }
            S_Log.Instance.LogError(ex);
        }
    }

    private static readonly ConcurrentDictionary<string, Track> _tracksCache = new ConcurrentDictionary<string, Track>();


    public override void Deconstruct()
    {
        _disposable.Dispose();
    }

    public override async Task SwitchRemoteDevice(string deviceId)
    {
        //https://gae2-spclient.spotify.com/connect-state/v1/connect/transfer/from/1ef2aa4cf31c7af8bf20b5d5776a54ce8930f9fe/to/1ef2aa4cf31c7af8bf20b5d5776a54ce8930f9fe
        //TODO: ApResolver
        //gae2-spclient.spotify.com:443
        using var _ = await "https://gae2-spclient.spotify.com"
            .AppendPathSegments("connect-state", "v1", "connect", "transfer", "from", _spotifyClient.Config.DeviceId,
                "to", deviceId)
            .WithOAuthBearerToken((await _spotifyClient.BearerClient.GetBearerTokenAsync()))
            .PostAsync();
    }

    public override ServiceType Service => ServiceType.Spotify;
}