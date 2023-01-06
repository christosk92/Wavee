using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eum.Connections.Spotify.Models.Users;
using Eum.Connections.Spotify.Playback;
using Eum.UI.Items;
using Eum.UI.ViewModels.Playback;

namespace Eum.UI.Services
{
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
        Task PlayOnDevice(ItemId contextId, ItemId trackId, ItemId? deviceId = null);
        Task PlayOnDevice(ItemId contextId, int trackIndex, ItemId? deviceId = null);
    }

    public class PlaybackService : IPlaybackService
    {
        private readonly PlaybackViewModel _playbackViewModel;
        private readonly ISpotifyPlaybackClient _spotifyPlaybackClient;

        public PlaybackService(ISpotifyPlaybackClient spotifyPlaybackClient, PlaybackViewModel playbackViewModel)
        {
            _spotifyPlaybackClient = spotifyPlaybackClient;
            _playbackViewModel = playbackViewModel;
        }

        public async Task PlayOnDevice(ItemId contextId, ItemId trackId, ItemId? deviceId = null)
        {
            switch (contextId.Service)
            {
                case ServiceType.Local:
                    //if the active device is spotify, we can't obviously so we switch to a local device
                    await _playbackViewModel.SwitchRemoteDevice(null);
                    //TODO: play for local
                    break;
                case ServiceType.Spotify:

                    if (_playbackViewModel.PlayingOnExternalDevice)
                        await _spotifyPlaybackClient.PlayOnDevice(new SpotifyId(contextId.Uri), new SpotifyId(trackId.Uri),
                            null, deviceId?.Id ?? _playbackViewModel.ActiveDeviceId.Id);
                    else 
                        await _spotifyPlaybackClient.PlayOnDevice(new SpotifyId(contextId.Uri), new SpotifyId(trackId.Uri),
                        null, deviceId?.Id);
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

        public Task PlayOnDevice(ItemId contextId, int trackIndex, ItemId? deviceId = null)
        {
            throw new NotImplementedException();
        }
    }
}
