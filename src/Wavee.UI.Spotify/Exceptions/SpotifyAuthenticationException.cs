using System;
using System.Runtime.Serialization;

namespace Wavee.UI.Spotify.Exceptions;

public sealed class SpotifyException : Exception
{
    public SpotifyException(SpotifyFailureReason reason) : base(ToMessage(reason))
    {
        Reason = reason;
    }

    public SpotifyException(SpotifyFailureReason reason, string message) : base(ToMessage(reason, message))
    {
        Reason = reason;
        ExtraMessage = message;
    }

    public string? ExtraMessage { get; set; }

    private static string ToMessage(SpotifyFailureReason reason, string message)
    {
        var baseMessage = ToMessage(reason);
        if (string.IsNullOrWhiteSpace(message))
        {
            return baseMessage;
        }

        return $"{message} {baseMessage}";
    }

    public SpotifyFailureReason Reason { get; }

    private static string ToMessage(SpotifyFailureReason reason)
    {
        return reason switch
        {
            SpotifyFailureReason.TokenExpired => "The Spotify token has expired.",
            SpotifyFailureReason.RateLimited => "The Spotify API is rate limited.",
            SpotifyFailureReason.AuthFailure => "The Spotify API authentication failed.",
            SpotifyFailureReason.InvalidId => "The Spotify ID is invalid.",
            SpotifyFailureReason.AudioKeyError => "The Spotify audio key could not be retrieved.",
            _ => throw new ArgumentOutOfRangeException(nameof(reason), reason, null)
        };
    }
}

public enum SpotifyFailureReason
{
    TokenExpired,
    RateLimited,
    AuthFailure,
    InvalidId,
    AudioKeyError
}