using LanguageExt.Common;
using LanguageExt.Effects.Traits;
using Spotify.Metadata;
using Wavee.Spotify.Clients.Info;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Clients.Mercury.Metadata;

namespace Wavee.Spotify.Infrastructure.Sys;

internal static class SpotifyPlayback<RT> where RT : struct, HasCancel<RT>
{
    public static Aff<RT, SpotifyStream> LoadTrack(SpotifyId id, Guid mainConnectionId,
        Func<ValueTask<string>> getBearer, IMercuryClient mercury, CancellationToken ct) =>
        from metadata in id.Type switch
        {
            AudioItemType.Track => mercury.GetTrack(id, ct).ToAff()
                .Map(x => new TrackOrEpisode(Right(x))),
            AudioItemType.Episode => mercury.GetEpisode(id, ct).ToAff()
                .Map(x => new TrackOrEpisode(Left(x))),
            _ => FailAff<RT, TrackOrEpisode>(Error.New("Unsupported type"))
        }
        select new SpotifyStream
        {
            Metadata = metadata
        };

}

internal class SpotifyStream : Stream
{
    public required TrackOrEpisode Metadata { get; init; }

    public override void Flush()
    {
        throw new NotImplementedException();
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        throw new NotImplementedException();
    }

    public override void SetLength(long value)
    {
        throw new NotImplementedException();
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        throw new NotImplementedException();
    }

    public override bool CanRead { get; }
    public override bool CanSeek { get; }
    public override bool CanWrite { get; }
    public override long Length { get; }
    public override long Position { get; set; }
}