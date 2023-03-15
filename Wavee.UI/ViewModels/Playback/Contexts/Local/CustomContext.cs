using CommunityToolkit.Mvvm.DependencyInjection;
using Wavee.UI.AudioImport;
using Wavee.UI.AudioImport.Database;
using Wavee.UI.ViewModels.Album;
using Wavee.UI.ViewModels.Playback.Impl;

namespace Wavee.UI.ViewModels.Playback.Contexts.Local;

public class CustomContext : ILocalContext
{
    private string[]? _paths;
    private readonly IEnumerable<IPlayableItem> _contexts;
    public CustomContext(IEnumerable<IPlayableItem> contexts)
    {
        _contexts = contexts;
    }

    public LocalAudioFile? GetTrack(int index)
    {
        BuildIfNeccesary();

        var db = Ioc.Default.GetRequiredService<IAudioDb>();
        var find = db.AudioFiles.FindById(_paths![index]);
        return find;
    }

    public int Length
    {
        get
        {
            BuildIfNeccesary();

            return _paths!.Length;
        }
    }

    private void BuildIfNeccesary()
    {
        if (_paths != null) return;
        var ids = _contexts.SelectMany(a => a.GetPlaybackIds());
        _paths = ids.ToArray();
    }
}