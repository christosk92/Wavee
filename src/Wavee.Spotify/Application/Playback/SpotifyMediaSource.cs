using System.Diagnostics;
using System.Net.Http.Headers;
using Google.Protobuf;
using Google.Protobuf.Collections;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Domain.Playback.Player;
using Wavee.Spotify.Application.AudioKeys.QueryHandlers;
using Wavee.Spotify.Application.Decrypt;
using Wavee.Spotify.Common;
using Wavee.Spotify.Common.Contracts;

namespace Wavee.Spotify.Application.Playback;

public sealed class SpotifyMediaSource : IWaveeMediaSource
{
    private SpotifyStream? _stream;
    private readonly string _uri;
    private ISpotifyClient? _client;
    public IReadOnlyDictionary<string, string> Metadata { get; }

    private SpotifyMediaSource(string uri, ISpotifyClient client)
    {
        _uri = uri;
        _client = client;
    }

    public Either<Episode, Track> TrackOrEpisode { get; private set; }
    public Option<NormalisationData> NormData { get; private set; }

    public async ValueTask<Stream> CreateStream()
    {
        if (_stream is not null)
            return _stream;

        var id = SpotifyId.FromUri(_uri);
        var track = await _client.Tracks.GetTrack(id, CancellationToken.None);
        var preferedQuality = _client.Config.Playback.PreferedQuality;
        var file = FindFile(track, preferedQuality);
        var audioKeyTask = _client.AudioKeys.GetAudioKey(id, file.FileId, CancellationToken.None).AsTask();
        var streamingFileTask = _client.StorageResolver.GetStorageFile(file.FileId, CancellationToken.None).AsTask();
        await Task.WhenAll(audioKeyTask, streamingFileTask);

        var audioKey = audioKeyTask.Result;
        var streamingFile = streamingFileTask.Result;

        var stream = new SpotifyStream(
            file: streamingFile,
            audioKey: audioKey,
            isOgg: IsVorbis(file)
        );
        var normData = NormalisationData.ParseFromOgg(stream.UnoffsettedStream);

        _stream = stream;
        NormData = normData;
        TrackOrEpisode = track;
        Duration = TimeSpan.FromSeconds(track.Duration);
        return stream;
    }

    public TimeSpan Duration { get; private set; }

    public static SpotifyMediaSource CreateFromUri(
        ISpotifyClient client,
        string uri,
        CancellationToken cancellationToken = default)
    {
        return new SpotifyMediaSource(
            uri,
            client
        );
    }

    public void Dispose()
    {
        // TODO release managed resources here
    }

    private static AudioFile? FindFile(Track track, SpotifyAudioQuality? preferedQuality)
    {
        var matched = FindFile(track.File, preferedQuality);
        if (matched is not null)
            return matched;
        foreach (var alternative in track.Alternative)
        {
            matched = FindFile(alternative.File, preferedQuality);
            if (matched is not null)
            {
                return matched;
            }
        }

        if (preferedQuality is null)
        {
            //give up
            return null;
        }

        return FindFile(track, null);
    }

    private static AudioFile? FindFile(RepeatedField<AudioFile> files, SpotifyAudioQuality? preferedQuality)
    {
        if (preferedQuality is null)
        {
            return files.FirstOrDefault(x => IsVorbis(x));
        }

        var quality = preferedQuality.Value;
        return files.FirstOrDefault(x => IsVorbis(x) && GetQuality(x.Format) == quality);
    }

    private static bool IsVorbis(AudioFile audioFile)
    {
        return audioFile.Format is AudioFile.Types.Format.OggVorbis96
            or AudioFile.Types.Format.OggVorbis160
            or AudioFile.Types.Format.OggVorbis320;
    }

    private static SpotifyAudioQuality GetQuality(AudioFile.Types.Format format)
    {
        switch (format)
        {
            case AudioFile.Types.Format.Mp396:
            case AudioFile.Types.Format.OggVorbis96:
            case AudioFile.Types.Format.Mp3160:
            case AudioFile.Types.Format.Mp3160Enc:
            case AudioFile.Types.Format.OggVorbis160:
            case AudioFile.Types.Format.Aac24:
                return SpotifyAudioQuality.High;
            case AudioFile.Types.Format.Mp3320:
            case AudioFile.Types.Format.Mp3256:
            case AudioFile.Types.Format.OggVorbis320:
            case AudioFile.Types.Format.Aac48:
                return SpotifyAudioQuality.VeryHigh;
            default:
                return SpotifyAudioQuality.Normal;
        }
    }
}