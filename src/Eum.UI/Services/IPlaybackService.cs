using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback;
using Eum.Logging;
using Eum.UI.Items;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Playback;

namespace Eum.UI.Services
{
    public enum SortField
    {
        Name,
        ArtistName,
        AlbumName,
        Added
    }

    public record PagedTrack(ItemId Id, Dictionary<string, string> Metadata);
    public abstract record ContextPlayObject(ItemId ContextId, int Index, Dictionary<string, string>? ContextMetadata = null);

    public record PlainContextPlayCommand(ItemId ContextId, 
        int TrackIndex, Dictionary<string, string>? ContextMetadata) : ContextPlayObject(ContextId, TrackIndex, ContextMetadata);
    public record SortedContextPlayCommand(ItemId ContextId, int TrackIndex, SortField SortField, bool Ascending, Dictionary<string, string>? ContextMetadata = null) : ContextPlayObject(ContextId, TrackIndex, ContextMetadata);
    public record PagedContextPlayCommand(ItemId ContextId, PagedTrack[] Tracks, int TrackIndex, Dictionary<string, string>? ContextMetadata = null) : ContextPlayObject(ContextId, TrackIndex, ContextMetadata);
    public interface IPlaybackService
    {
        /// <summary>
        /// Attempts to play an item. If no deviceid is provided, the currently active device is fetched and played on there.
        /// If no active device is found, playback will resume on local device.
        /// </summary>
        /// <param name="contextId"></param>
        /// <param name="trackId"></param>
        /// <param name="deviceId">Override the playing device id. If this is null, the currently active device is used. If no external device is found, playback will resume on this device.</param>
        /// <returns></returns>
        Task PlayOnDevice(ContextPlayObject contextId,  ItemId? deviceId = null);
        Task SetPosition(double val);
    }

    public class PlaybackService : IPlaybackService
    {
        private readonly PlaybackViewModel _playbackViewModel;
        private readonly ISpotifyPlaybackClient _spotifyPlaybackClient;

        public PlaybackService(ISpotifyPlaybackClient spotifyPlaybackClient)
        {
            _spotifyPlaybackClient = spotifyPlaybackClient;
            _playbackViewModel = Ioc.Default.GetRequiredService<MainViewModel>()
                .PlaybackViewModel;
        }

        public async Task PlayOnDevice(ContextPlayObject contextId, ItemId? deviceId = null)
        {
            switch (contextId.ContextId.Service)
            {
                case ServiceType.Local:
                    //if the active device is spotify, we can't obviously so we switch to a local device
                    await _playbackViewModel.SwitchRemoteDevice(null);
                    //TODO: play for local
                    break;
                case ServiceType.Spotify:
                    var deviceIdPlay = _playbackViewModel.PlayingOnExternalDevice
                        ? (deviceId?.Id ?? _playbackViewModel.ActiveDeviceId.Id)
                        : deviceId?.Id;
                    switch (contextId)
                    {
                        case SortedContextPlayCommand sortedSpotify:
                            if (sortedSpotify.ContextMetadata == null)
                                throw new ArgumentException("Context Metadata cannot be zero.");
                            //list_util_sort
                            //sorting.criteria
                            sortedSpotify.ContextMetadata["list_util_sort"]
                                 = sortedSpotify.SortField switch
                                 {
                                     SortField.Name => "name {0}",
                                     SortField.ArtistName => "artist.name {0},album.name,discNumber,trackNumber",
                                     SortField.AlbumName => "album.name {0},discNumber,trackNumber",
                                     SortField.Added => "addTime {0},album.name,album.artist.name,discNumber,trackNumber",
                                     _ => throw new ArgumentOutOfRangeException()
                                 };
                            sortedSpotify.ContextMetadata["list_util_sort"] =
                                string.Format(sortedSpotify.ContextMetadata["list_util_sort"],
                                    sortedSpotify.Ascending ? "ASC" : "DESC");

                            sortedSpotify.ContextMetadata["sorting.criteria"]
                                = sortedSpotify.SortField switch
                                {
                                    SortField.Name => "title{0}",
                                    SortField.ArtistName => "artist_name{0},album_title,album_disc_number,album_track_number",
                                    SortField.AlbumName => "album_title{0},album_disc_number,album_track_number",
                                    SortField.Added => "added_at{0},album_title,album_artist_name,album_disc_number,album_track_number",
                                    _ => throw new ArgumentOutOfRangeException()
                                };
                            sortedSpotify.ContextMetadata["sorting.criteria"] =
                                string.Format(sortedSpotify.ContextMetadata["sorting.criteria"],
                                    sortedSpotify.Ascending ? string.Empty : " DESC");

                            await _spotifyPlaybackClient.PlayOnDevice(
                                new SpotifyId(sortedSpotify.ContextId.Uri),
                                sortedSpotify.TrackIndex,
                                sortedSpotify.ContextMetadata ??
                                throw new ArgumentException("Context Metadata cannot be zero."), deviceIdPlay);
                            break;
                        case PagedContextPlayCommand pagedContext:
                            throw new NotImplementedException();
                            break;
                        case PlainContextPlayCommand plainContext:
                            await _spotifyPlaybackClient.PlayOnDevice(
                                new SpotifyId(plainContext.ContextId.Uri), plainContext.TrackIndex,
                                plainContext.ContextMetadata, deviceIdPlay);
                            break;
                    }
                    break;
                case ServiceType.Apple:
                    //Apple does not support remote playback, so default to local device as well
                    await _playbackViewModel.SwitchRemoteDevice(null);
                    //TODO: play for apple
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

        }

        public Task PlayOnDevice(ContextPlayObject contextId, int trackIndex, ItemId? deviceId = null)
        {
            throw new NotImplementedException();
        }

        public async Task SetPosition(double val)
        {
            try
            {
                switch (_playbackViewModel.Service)
                {
                    case ServiceType.Local:
                        break;
                    case ServiceType.Spotify:
                        await _spotifyPlaybackClient.Seek(val, _playbackViewModel.PlayingOnExternalDevice);
                        break;
                    case ServiceType.Apple:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            catch (Exception ex)
            {
                S_Log.Instance.LogError($"An error occured while trying to seek to {val}..", ex);
            }
        }
    }
}
