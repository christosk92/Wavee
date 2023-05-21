using LanguageExt;
using Wavee.Core.Ids;

namespace Wavee.Spotify.Infrastructure.Remote.Messaging;

public readonly record struct SpotifyLibraryUpdateNotification(
    bool Initial,
    AudioId Item,
    bool Removed,
    Option<DateTimeOffset> AddedAt);