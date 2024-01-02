using System.Runtime.Serialization;
using Wavee.Spotify.Core.Attributes;

namespace Wavee.Spotify.Core.Exceptions;

public sealed class SpotifyCannotPlayContentException : Exception
{
    internal SpotifyCannotPlayContentException(SpotifyCannotPlayReason code, Exception? inner = null) : base(
        MessageToCode(code, inner), inner)
    {
        Reason = code;
    }
    internal SpotifyCannotPlayContentException(Exception? inner = null) : base(
        MessageToCode(SpotifyCannotPlayReason.Unknown, inner), inner)
    {
        Reason = SpotifyCannotPlayReason.Unknown;
    }

    public SpotifyCannotPlayReason Reason { get; }

    private static string? MessageToCode(SpotifyCannotPlayReason code, Exception? exception)
    {
        var c = code.ToStringValue();
        if (string.IsNullOrEmpty(c))
        {
            if (exception != null)
            {
                //Unkown error: {exception.Message}
                return "Unkown error: " + exception.Message;
            }
            else
            {
                //Totally unkown error
                return "Totally unkown error";
            }
        }

        // {code}: {c}
        return code + ": " + c;
    }
}

public enum SpotifyCannotPlayReason
{
    [StringValue("Cannot play content that is not a track or episode")]
    NotTrackOrEpisode,
    [StringValue("Wavee only supports tracks at the moment")]
    NotYetImplemented,
    [StringValue("The track or episode could not be found. Please make sure the id is correct. If it is, the track or episode might have been removed from Spotify.")]
    InvalidTrack,
    [StringValue("The supplied context could not be build. Please check the inner exception for more details.")]
    InvalidContext,
    Unknown
}