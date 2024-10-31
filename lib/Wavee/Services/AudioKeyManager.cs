using System.Collections.Concurrent;
using Eum.Spotify.playplay;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Wavee.Enums;
using Wavee.Exceptions;
using Wavee.Interfaces;
using Wavee.Models.Common;
using Wavee.Playback.Streaming;

namespace Wavee.Services;

internal readonly record struct AudioKey(byte[]? Key)
{
    public bool HasKey => Key is not null;
}

internal sealed class PlayPlayAudioKeyManager : IAudioKeyManager
{
    private static readonly ByteString Token = ByteString.CopyFrom(new byte[]
        { 0x01, 0xe1, 0x32, 0xca, 0xe5, 0x27, 0xbd, 0x21, 0x62, 0x0e, 0x82, 0x2f, 0x58, 0x51, 0x49, 0x32 });

    private readonly ISpotifyApiClient _spotifyApiClient;
    private readonly ILogger<IAudioKeyManager> _logger;
    
    public PlayPlayAudioKeyManager(ISpotifyApiClient spotifyApiClient, ILogger<IAudioKeyManager> logger)
    {
        _spotifyApiClient = spotifyApiClient;
        _logger = logger;
    }

    public async Task<AudioKey> RequestAsync(SpotifyId _, FileId file)
    {
        /*
         *    playplay_license_request = PlayPlayLicenseRequest(
                version=2,
                token=bytes.fromhex("01e132cae527bd21620e822f58514932"),
                interactivity=Interactivity.DOWNLOAD,
                content_type=AUDIO_TRACK
            )
         */
        _logger.LogInformation("Requesting key for {File}", file);
        var playPlayLicenseRequest = new PlayPlayLicenseRequest();
        //{ "version": 3, "token": "AfZnN5r+RLmNcUZ2O7gdjQ==", "interactivity": "INTERACTIVE", "contentType": "AUDIO_TRACK", "timestamp": "1729361421" }
        //015c8801577620d7d46de9d696bb9574
        const string tokenbase16 = "015c8801577620d7d46de9d696bb9574";
        var f = new byte[tokenbase16.Length / 2];
        for (var i = 0; i < f.Length; i++)
        {
            f[i] = byte.Parse(tokenbase16.Substring(i * 2, 2), System.Globalization.NumberStyles.HexNumber);
        }
        playPlayLicenseRequest.Version = 2;
        playPlayLicenseRequest.Token = ByteString.CopyFrom(f);
        playPlayLicenseRequest.Timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        playPlayLicenseRequest.CacheId = ByteString.Empty;
        playPlayLicenseRequest.Interactivity = Interactivity.Interactive;
        playPlayLicenseRequest.ContentType = ContentType.AudioTrack;
        var playPlayLicenseResponse =
            await _spotifyApiClient.GetPlayPlayLicenseAsync(playPlayLicenseRequest, file, CancellationToken.None);
        var obfuscatedKey = playPlayLicenseResponse.ObfuscatedKey;
        _logger.LogInformation("Received obfuscated key for {File}", file);
        var unobfuscated = UnplayplayWrapper.DecryptAndBindKey(
            obfuscatedKey.ToByteArray(),
            file.ToBase16());
        _logger.LogInformation("Unobfuscated key for {File}", file);
        return new AudioKey(unobfuscated);
    }

    public void Dispatch(PacketType cmd, byte[] data)
    {
        throw new NotSupportedException("This implementation does not support dispatching audio keys");
    }
}

[Obsolete("Legacy implementation, use PlayPlayAudioKeyManager instead")]
internal sealed class Legacy_AudioKeyManager : IAudioKeyManager
{
    private readonly ConcurrentDictionary<uint, TaskCompletionSource<AudioKey>> _pending;
    private uint _sequence;
    private readonly IPacketDispatcher _dispatcher;
    private readonly ILogger<Legacy_AudioKeyManager> _logger;

    public Legacy_AudioKeyManager(IPacketDispatcher dispatcher, ILogger<Legacy_AudioKeyManager> logger)
    {
        _dispatcher = dispatcher;
        _logger = logger;
        _pending = new ConcurrentDictionary<uint, TaskCompletionSource<AudioKey>>();
        _sequence = 0;
        _dispatcher.SetAudioKeyDispatcher(this);
    }

    public void Dispatch(PacketType cmd, byte[] data)
    {
        uint seq = BitConverter.ToUInt32(data, 0);

        if (!_pending.TryRemove(seq, out var tcs))
        {
            throw new AudioKeyException($"Sequence {seq} not pending");
        }

        switch (cmd)
        {
            case PacketType.AesKey:
                _logger.LogInformation("Received AES key for sequence {Sequence}", seq);
                var key = new byte[16];
                Array.Copy(data, 4, key, 0, 16);
                tcs.SetResult(new AudioKey(key));
                break;

            case PacketType.AesKeyError:
                _logger.LogError("Received AES key error for sequence {Sequence}: {Error:X2} {Error:X2}", seq, data[4],
                    data[5]);
                tcs.SetException(new AudioKeyException($"AES key error: {data[4]:X2} {data[5]:X2}"));
                break;

            default:
                _logger.LogError("Received unexpected packet type {PacketType} for sequence {Sequence}", cmd, seq);
                tcs.SetException(new AudioKeyException($"Unexpected packet type {cmd}"));
                break;
        }
    }

    public async Task<AudioKey> RequestAsync(SpotifyId track, FileId file)
    {
        var tcs = new TaskCompletionSource<AudioKey>();
        uint seq = GetNextSequence();

        _pending[seq] = tcs;
        _logger.LogInformation("Requesting key for {Track} {File}", track, file);
        try
        {
            await SendKeyRequestAsync(seq, track, file);
            return await tcs.Task;
        }
        catch
        {
            _pending.TryRemove(seq, out _);
            throw;
        }
    }

    private async Task SendKeyRequestAsync(uint seq, SpotifyId track, FileId file)
    {
        using (var ms = new MemoryStream())
        using (var writer = new BinaryWriter(ms))
        {
            writer.Write(file.ToByteArray());
            writer.Write(track.ToRaw());
            writer.Write(seq);
            writer.Write((ushort)0);

            await _dispatcher.SendPacketAsync(PacketType.RequestKey, ms.ToArray());
        }
    }

    private uint GetNextSequence()
    {
        return System.Threading.Interlocked.Increment(ref _sequence);
    }
}