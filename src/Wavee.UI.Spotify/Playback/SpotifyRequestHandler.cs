using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Eum.Spotify.context;
using Eum.Spotify.transfer;
using Wavee.Contracts.Enums;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Playback;
using Wavee.UI.Spotify.Clients;
using Wavee.UI.Spotify.Common;
using Wavee.UI.Spotify.Interfaces;

namespace Wavee.UI.Spotify.Playback;

internal sealed class SpotifyRequestHandler : ISpotifyRequestHandler
{
    private readonly IWaveePlayer _player;
    private readonly SpotifyPlaybackClient _client;

    public SpotifyRequestHandler(IWaveePlayer player, SpotifyPlaybackClient client)
    {
        _player = player;
        _client = client;
    }

    public void HandleRequest(string identity, JsonElement main, Dictionary<string, string> messageHeaders)
    {
        var payload = SpotifyWsUtils.ReadPayload(main, messageHeaders);
        var reader = new Utf8JsonReader(payload);
        using var jsonDocument = JsonDocument.ParseValue(ref reader);
        var root = jsonDocument.RootElement;
        var messageId = root.GetProperty("message_id").GetInt32();
        var setnBy = root.GetProperty("sent_by_device_id").GetString();
        var command = root.GetProperty("command");

        var endpoint = command.GetProperty("endpoint").GetString();
        switch (endpoint)
        {
            case "transfer":
            {
                var transferRequest =
                    TransferState.Parser.ParseFrom(command.GetProperty("data").GetBytesFromBase64().AsSpan());
                HandleTransferRequest(transferRequest);
                break;
            }
        }
    }

    private void HandleTransferRequest(TransferState command)
    {
        using var playerlock = _player.Lock();
        //Order is important here
        //Each of these operations will occur in sequence
        _player.Clear();
        _player.SetShuffle(command.Options.ShufflingContext);
        _player.SetRepeat(command.Options.RepeatingTrack
            ? RepeatMode.RepeatTrack
            : (command.Options.RepeatingContext ? RepeatMode.RepeatContext : RepeatMode.None));


        Task<IMediaSource> trackTask = null;
        TimeSpan position = TimeSpan.Zero;
        bool startPlayback = false;
        if (command.Playback?.CurrentTrack is not null)
        {
            var itemId =
                RegularSpotifyId.FromRaw(command.Playback.CurrentTrack.Gid.Span,
                    SpotifyIdItemType.Track); //TODO: Episodes
            trackTask = _client.CreateMediaSource(itemId, false, CancellationToken.None);

            var timestamp = command.Playback.Timestamp;
            var posSinceTs = command.Playback.PositionAsOfTimestamp;
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var diff = now - timestamp;
            position = TimeSpan.FromMilliseconds(diff + posSinceTs);
            startPlayback = !command.Playback.IsPaused;
        }

        _player.Play(trackTask, position, startPlayback);

        var queue = CreateQueue(command.Queue);
        _player.SetQueue(queue);

        var context = CreateContext(command.CurrentSession, command.Playback?.CurrentTrack);
        _player.SetContext(context);
    }

    private ISpotifyPlayQueue CreateQueue(Queue commandQueue)
    {
        //TODO:
        return default;
    }

    private ISpotifyPlayContext CreateContext(Session commandCurrentSession, ContextTrack playbackCurrentTrack)
    {
        //TODO:
        return default;
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }
}