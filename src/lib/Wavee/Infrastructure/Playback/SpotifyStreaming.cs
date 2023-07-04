using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text;
using CommunityToolkit.HighPerformance;
using Eum.Spotify.storage;
using Google.Protobuf;
using LanguageExt;
using Serilog;
using Spotify.Metadata;
using Wavee.Cache;
using Wavee.Id;
using Wavee.Player.Ctx;

namespace Wavee.Infrastructure.Playback;

internal static class SpotifyStreaming
{
    public static Task<WaveeTrack> StreamTrack(
        Guid connectionId,
        SpotifyId id,
        HashMap<string, string> trackMetadata,
        CancellationToken ct)
    {
        switch (id.Type)
        {
            case AudioItemType.Track:
                return StreamTrackSpecifically(connectionId, id, trackMetadata, ct);
            case AudioItemType.PodcastEpisode:
                return StreamPodcastEpisodeSpecifically(connectionId, id, trackMetadata);
        }

        throw new NotImplementedException();
    }

    private static async Task<WaveeTrack> StreamTrackSpecifically(Guid connectionId, SpotifyId id,
        HashMap<string, string> trackMetadata, CancellationToken cancellationToken)
    {
        var sw = Stopwatch.StartNew();
        var client = SpotifyClient.Clients[connectionId];
        var country = await client.Country;
        var cache = client.Cache;
        var track = await cache.GetTrack(id)
            .MatchAsync(
                Some: x => x,
                None: async () =>
                {
                    var track = await client.Metadata.GetTrack(id, cancellationToken);
                    _ = Task.Run(async () => await cache.SetTrack(id, track));


                    return track;
                }
            );

        var metadataTime = sw.Elapsed;

        var playbackConfig = client.Config.Playback;
        var preferredQuality = playbackConfig.PreferedQuality;
        var canPlay = CanPlay(track, country);
        if (!canPlay)
        {
            throw new ContentNotPlayableException("Content has been restricted in your country.");
        }

        var format = GetBestFormat(track, preferredQuality)
            .IfNone(() => throw new ContentNotPlayableException("No suitable format found."));
        var audioKey = await client.AudioKeys.GetAudioKey(id, format, cancellationToken);
        var audioKeyTime = sw.Elapsed;

        var stream = await cache.File(format)
            .MatchAsync(
                Some: fileStream => StreamFromFile(fileStream, audioKey,
                    format.Format is AudioFile.Types.Format.OggVorbis96 or AudioFile.Types.Format.OggVorbis160
                        or AudioFile.Types.Format.OggVorbis320),
                None: async () => await StreamFromWeb(client, format, audioKey));
        Option<NormalisationData> normData = Option<NormalisationData>.None;
        if (stream.IsOgg)
        {
            normData = NormalisationData.ParseFromOgg(stream.UnoffsettedStream);
        }

        var streamTime = sw.Elapsed;
        Log.Debug("Metadata: {MetadataTime} AudioKey: {AudioKeyTime} Stream: {StreamTime}",
            metadataTime, audioKeyTime, streamTime);

        return new WaveeTrack(
            audioStream: stream,
            title: track.Name,
            id: id.ToString(),
            metadata: trackMetadata.ToDictionary(x => x.Key, x => (object)x.Value).ToHashMap(),
            duration: TimeSpan.FromMilliseconds(track.Duration),
            normalisationData: normData.Map(x => x.ToUniversal())
        );
    }

    private static Task<WaveeTrack> StreamPodcastEpisodeSpecifically(Guid connectionId, SpotifyId id,
        HashMap<string, string> trackMetadata)
    {
        throw new NotImplementedException();
    }

    private static Dictionary<string, string> _emptyMetadata = new();

    private static async Task<SpotifyStream> StreamFromWeb(SpotifyClient client, AudioFile format,
        Option<byte[]> audioKey)
    {
        var bearer = await client.Token.GetToken();
        var storageResolve = await StorageResolve(format.FileId, bearer, CancellationToken.None);
        if (storageResolve.Result is not StorageResolveResponse.Types.Result.Cdn)
        {
            throw new ContentNotPlayableException("Cannot play this track for some reason.. Cdn is not available.");
        }

        var cdnUrl = storageResolve.Cdnurl.First();
        //TODO: check expiration
        const int firstChunkStart = 0;
        const int chunkSize = SpotifyUnoffsettedStream.ChunkSize;
        const int firstChunkEnd = firstChunkStart + chunkSize - 1;

        using var firstChunk = await HttpIO.GetWithContentRange(
            cdnUrl,
            firstChunkStart,
            firstChunkEnd,
            _emptyMetadata,
            null);
        var firstChunkBytes = await firstChunk.Content.ReadAsByteArrayAsync();
        var numberOfChunks =
            (int)Math.Ceiling((double)firstChunk.Content.Headers.ContentRange!.Length!.Value / chunkSize);

        var requested = new TaskCompletionSource<byte[]>[numberOfChunks];
        requested[0] = new TaskCompletionSource<byte[]>();
        requested[0].SetResult(firstChunkBytes);

        ValueTask<byte[]> GetChunkFunc(int index)
        {
            if (requested[index] is { Task.IsCompleted: true })
            {
                return new ValueTask<byte[]>(requested[index].Task.Result);
            }

            if (requested[index] is null)
            {
                var start = index * chunkSize;
                var end = start + chunkSize - 1;
                requested[index] = new TaskCompletionSource<byte[]>();
                return new ValueTask<byte[]>(HttpIO.GetWithContentRange(
                        cdnUrl,
                        start,
                        end,
                        _emptyMetadata,
                        null)
                    .MapAsync(x => x.Content.ReadAsByteArrayAsync())
                    .ContinueWith(x =>
                    {
                        requested[index].TrySetResult(x.Result);
                        return x.Result;
                    }));
            }

            return new ValueTask<byte[]>(requested[index].Task);
        }

        var stream = new SpotifyStream(
            totalSize: firstChunk.Content.Headers.ContentRange.Length.Value,
            getChunkFunc: GetChunkFunc,
            audioKey: audioKey,
            format.Format is AudioFile.Types.Format.OggVorbis96 or AudioFile.Types.Format.OggVorbis160
                or AudioFile.Types.Format.OggVorbis320
        );
        return stream;
    }

    private static SpotifyStream StreamFromFile(FileStream fileStream, Option<byte[]> audioKey, bool isOgg)
    {
        async ValueTask<byte[]> GetChunkFunc(int index)
        {
            //since x is a filestream, we can just seek to the correct position and read the bytes
            const int chunkSize = SpotifyUnoffsettedStream.ChunkSize;
            int start = index * chunkSize;
            fileStream.Seek(start, SeekOrigin.Begin);
            Memory<byte> chunk = new byte[chunkSize];
            var readAsync = await fileStream.ReadAsync(chunk);
            //return subarray
            return chunk.Slice(0, readAsync).ToArray();
        }

        return new SpotifyStream(
            totalSize: fileStream.Length,
            GetChunkFunc,
            audioKey,
            isOgg);
    }

    private static async Task<StorageResolveResponse> StorageResolve(ByteString file, string jwt, CancellationToken ct)
    {
        const string spclient = "gae2-spclient.spotify.com:443";
        var query = $"https://{spclient}/storage-resolve/files/audio/interactive/{{0}}";

        static string ToBase16(ByteString data)
        {
            var sp = data.Span;
            var hex = new StringBuilder(sp.Length * 2);
            foreach (var b in sp)
            {
                hex.AppendFormat("{0:x2}", b);
            }

            return hex.ToString();
        }

        var finalUri = string.Format(query, ToBase16(file));

        using var resp = await HttpIO.Get(finalUri,
            _emptyMetadata, new AuthenticationHeaderValue("Bearer", jwt), ct);
        resp.EnsureSuccessStatusCode();
        using var stream = await resp.Content.ReadAsStreamAsync();
        return StorageResolveResponse.Parser.ParseFrom(stream);
    }

    private static bool CanPlay(Track track, string country)
    {
        //TODO:
        return true;
    }

    private static Option<AudioFile> GetBestFormat(Track track, PreferedQuality preferredQuality)
    {
        foreach (var file in track.File)
        {
            switch (file.Format)
            {
                case AudioFile.Types.Format.OggVorbis96:
                    if (preferredQuality is PreferedQuality.Normal)
                        return file;
                    break;
                case AudioFile.Types.Format.OggVorbis160:
                    if (preferredQuality is PreferedQuality.High)
                        return file;
                    break;
                case AudioFile.Types.Format.OggVorbis320:
                    if (preferredQuality is PreferedQuality.Highest)
                        return file;
                    break;
            }
        }

        //if no format is found, return the first one
        var firstOne = track.File.FirstOrDefault(c => c.Format is AudioFile.Types.Format.OggVorbis96
            or AudioFile.Types.Format.OggVorbis160 or AudioFile.Types.Format.OggVorbis320);
        if (firstOne is null)
        {
            foreach (var alternative in track.Alternative)
            {
                var altItem = GetBestFormat(alternative, preferredQuality);
                if (altItem.IsSome)
                {
                    return altItem;
                }
            }
        }

        return firstOne is null ? Option<AudioFile>.None : firstOne;
    }
}