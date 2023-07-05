using LanguageExt;
using Wavee.Id;

namespace Wavee.UI.Spotify;

public readonly record struct SpotifyPlayParameter(int Index, SpotifyId Id, Option<string> Uid, SpotifyId Contextid) : IPlayParameter;