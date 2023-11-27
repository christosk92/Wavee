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
    public IReadOnlyDictionary<string, string> Metadata { get; }

    public ValueTask<Stream> CreateStream()
    {
        throw new NotImplementedException();
    }

    public TimeSpan Duration { get; }

    public static async Task<SpotifyMediaSource> CreateFromUri(
        ISpotifyClient client,
        string uri,
        CancellationToken cancellationToken = default)
    {
        var id = SpotifyId.FromUri(uri);
        var track = await client.Tracks.GetTrack(id, cancellationToken);
        var preferedQuality = client.Config.Playback.PreferedQuality;
        var file = FindFile(track, preferedQuality);
        var audioKeyTask = client.AudioKeys.GetAudioKey(id, file.FileId, cancellationToken).AsTask();
        var streamingFileTask = client.StorageResolver.GetStorageFile(file.FileId, cancellationToken).AsTask();
        await Task.WhenAll(audioKeyTask, streamingFileTask);

        var audioKey = audioKeyTask.Result;
        var streamingFile = streamingFileTask.Result;

        var stream = new SpotifyUnoffsettedStream(
            streamingFile,
            audioKey: audioKey,
            offset: 0xa7
        );
        var normData = NormalisationData.ParseFromOgg(stream);


        // //download first 1MB of the file as test
        // using var httpClient = new HttpClient();
        // using var request = new HttpRequestMessage(HttpMethod.Get, streamingurl);
        // request.Headers.Range = new RangeHeaderValue(0, 1024 * 1024);
        // using var response = await httpClient.SendAsync(request, cancellationToken);
        // response.EnsureSuccessStatusCode();
        // var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
        // var stream = new SpotifyUnoffsettedStream(
        //     totalSize: response.Content.Headers.ContentLength.Value,
        //     getChunkFunc: (i) => { return new ValueTask<byte[]>(bytes); },
        //     audioKey: audioKey,
        //     offset: 0xa7
        // );
        // var normData = NormalisationData.ParseFromOgg(stream);

        return null;
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