using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.Core.Ids;

namespace Wavee.UI.ViewModel.Artist;

public sealed class ArtistViewModel : ObservableObject
{
    private bool _isFollowing;
    private string? _header;
    private string? _name;
    private string? _monthlyListenersText;

    public bool IsFollowing
    {
        get => _isFollowing;
        set => SetProperty(ref _isFollowing, value);
    }

    public string? Header
    {
        get => _header;
        set => SetProperty(ref _header, value);
    }

    public string? Name
    {
        get => _name;
        set => SetProperty(ref _name, value);
    }

    public string? MonthlyListenersText
    {
        get => _monthlyListenersText;
        set => SetProperty(ref _monthlyListenersText, value);
    }

    public void Create(AudioId id)
    {
        
    }

    public void Destroy()
    {
        
    }
}