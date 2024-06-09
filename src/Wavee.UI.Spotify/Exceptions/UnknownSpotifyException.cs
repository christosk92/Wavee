using System;

namespace Wavee.UI.Spotify.Exceptions;

public sealed class UnknownSpotifyException : Exception
{
    public UnknownSpotifyException(Exception inner) : base(UnknownMessage, inner)
    {
    }
    public UnknownSpotifyException(string message) : base(message)
    {
    }

    public const string UnknownMessage = "An unknown error occurred while communicating with Spotify.";
}