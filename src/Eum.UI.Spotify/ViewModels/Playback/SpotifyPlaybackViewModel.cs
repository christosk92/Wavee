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
using Eum.Connections.Spotify.Clients.Contracts;
using Eum.Connections.Spotify.Clients;
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
using Eum.UI.ViewModels.Settings;
using Flurl;
using Flurl.Http;
using LiteDB;
using MoreLinq.Extensions;
using Org.BouncyCastle.Utilities.Encoders;
using ReactiveUI;
using Color = System.Drawing.Color;
using Eum.UI.Services.Library;

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
            IsSaved = false;
            OnPlayingItemChanged(default);
            return;
        }
        var playingUri = new SpotifyId(e.Cluster.PlayerState.Track.Uri);


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
            OnSeeked(initial);
            PlayingOnExternalDevice = !string.IsNullOrEmpty(e.Cluster.ActiveDeviceId) && e.Cluster.ActiveDeviceId != _spotifyClient
                .Config.DeviceId;
            if (PlayingOnExternalDevice)
            {
                RemoteDevices.Clear();

                var currentDevice = e.Cluster.Device[_spotifyClient.Config.DeviceId];
                RemoteDevices.Add(new RemoteDevice(new ItemId($"spotify:device:{currentDevice.DeviceId}"), currentDevice.Name, currentDevice.DeviceType));
                foreach (var deviceInfo in e.Cluster.Device)
                {
                    if (deviceInfo.Key == currentDevice.DeviceId) continue;

                    RemoteDevices.Add(new RemoteDevice(new ItemId($"spotify:device:{deviceInfo.Key}"), deviceInfo.Value.Name, deviceInfo.Value.DeviceType));
                }

                ExternalDevice = RemoteDevices.First(a => a.DeviceId.Id == e.Cluster.ActiveDeviceId);
                ActiveDeviceId = ExternalDevice.DeviceId;
            }
            else
            {
                ExternalDevice = null;
                ActiveDeviceId = new ItemId($"spotify:device:{_spotifyClient.Config.DeviceId}");
            }

            if (Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User.ThemeService
                    .Glaze == "Playback Dependent")
            {
                Ioc.Default.GetRequiredService<MainViewModel>()
                    .Glaze = await GetColorFromAlbumArt(Ioc.Default.GetRequiredService<MainViewModel>().CurrentUser.User.ThemeService.ActualTheme);
            }
            async Task<string> GetColorFromAlbumArt(AppTheme theme)
            {
                if (Item?.BigImage != null)
                {
                    return await Task.Run(async () =>
                    {
                        try
                        {
                            var colorsClient = Ioc.Default.GetRequiredService<IExtractedColorsClient>();
                            var color = await
                                colorsClient.GetColors(Item.BigImageUrl.ToString());
                            switch (theme)
                            {
                                case AppTheme.Dark:
                                    var f = color[ColorTheme.Dark].ToColor();
                                    return (Color.FromArgb(25, f.R, f.G, f.B)).ToHex();
                                case AppTheme.Light:
                                    var f2 = color[ColorTheme.Light].ToColor();
                                    return (Color.FromArgb(25, f2.R, f2.G, f2.B)).ToHex();
                                default:
                                    throw new ArgumentOutOfRangeException(nameof(theme), theme, null);
                            }
                        }
                        catch (Exception ex)
                        {
                            using var fs = await Ioc.Default.GetRequiredService<IFileHelper>()
                                .GetStreamForString(obj.item.Images.FirstOrDefault().Id, default);
                            using var bmp = new Bitmap(fs);
                            var colorThief = new ColorThief();
                            var c = colorThief.GetPalette(bmp);

                            var a =
                                c[0].Color.ToHexString();

                            var f = a.ToColor();
                            return (Color.FromArgb(25, f.R, f.G, f.B)).ToHex();
                        }
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

    public override async Task SwitchRemoteDevice(ItemId? deviceId)
    {
        if (deviceId is null)
        {
            //current device
            deviceId = new ItemId($"spotify:device:{_spotifyClient.Config.DeviceId}");
        }
        using var _ = await "https://gae2-spclient.spotify.com"
            .AppendPathSegments("connect-state", "v1", "connect", "transfer", "from", _spotifyClient.Config.DeviceId,
                "to", deviceId.Value.Id)
            .WithOAuthBearerToken((await _spotifyClient.BearerClient.GetBearerTokenAsync()))
            .PostAsync();
    }


    public override ServiceType Service => ServiceType.Spotify;
}