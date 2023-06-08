using System.Windows.Input;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.ViewModels.Playback;

namespace Wavee.UI.ViewModels.Artist;

public sealed class ArtistViewModel : ReactiveObject
{
    private bool _isFollowing;

    public ArtistViewModel()
    {
        PlayArtistCommand = ReactiveCommand.CreateFromTask((ct) =>
        {
            return Task.CompletedTask;
        });
    }

    public bool IsFollowing
    {
        get => _isFollowing;
        set => this.RaiseAndSetIfChanged(ref _isFollowing, value);
    }

    public ICommand PlayArtistCommand { get;  }

    public void Create(AudioId artistId)
    {
        IsFollowing = LibrariesViewModel.Instance.InLibrary(artistId);

        //create listener
    }
    public void Destroy()
    {
        
    }
}