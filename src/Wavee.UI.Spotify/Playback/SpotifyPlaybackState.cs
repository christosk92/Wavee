using System;
using Wavee.Contracts.Common;
using Wavee.Contracts.Enums;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.Spotify.Remote;

internal sealed class SpotifyPlaybackState : IPlaybackState
{
    public SpotifyPlaybackState(IPlayableItem currentItem, RealTimePosition position, RemotePlaybackStateType state)
    {
        CurrentItem = currentItem;
        Position = position;
        State = state;
    }

    public IPlayableItem CurrentItem { get; }
    public RealTimePosition Position { get; }
    public RemotePlaybackStateType State { get; }
}