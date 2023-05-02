using LanguageExt;
using LanguageExt.Effects.Traits;

namespace Wavee.Spotify.Playback.Context;

public struct GenericSpotifyContext<RT> : ISpotifyContext where RT : struct, HasCancel<RT>
{
    private readonly Option<string> _firstUid;
    private readonly Option<string> _firstId;

    private readonly RT _rt;

    public GenericSpotifyContext(string uri, Option<string> firstId, Option<string> firstUid, RT rt)
    {
        _firstId = firstId;
        _firstUid = firstUid;
        _rt = rt;
        Id = uri;
    }

    public string Id { get; }
    public ValueTask<int> Length { get; }

    public async ValueTask<string> GetIdAt(Option<int> index)
    {
        if (index.IsNone)
            return _firstId.Match(
                Some: id => id,
                None: () => throw new Exception("No id found"));

        var run = await GetAt(Id, index.IfNone(0)).Run(_rt);
        return run.Match(
            Succ: id => id,
            Fail: e => throw new Exception("Failed to get id"));
    }

    private static Aff<RT, string> GetAt(string uri, int index) =>
        throw new NotImplementedException();
}