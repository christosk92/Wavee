using Eum.Spotify.context;
using LanguageExt;

namespace Wavee.ContextResolve;

public readonly record struct SpotifyContext(string Url, HashMap<string, string> Metadata, Seq<ContextPage> Pages, HashMap<string, Seq<string>> Restrictions);