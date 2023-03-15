using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Models.AudioItems;
using Wavee.UI.ViewModels.Playback.Impl;

namespace Wavee.UI.ViewModels.Album;

public record AlbumViewModel(IAlbum Album, ICommand PlayCommand, IPlayContext PlayContext) :
    IPlayableItemWrapper,
    IAddableItem,
    IEditableItem,
    IDescribeable, INotifyPropertyChanged
{
    private bool _isSelected;

    public bool IsSelected
    {
        get => _isSelected;
        set => SetField(ref _isSelected, value);
    }

    public bool IsNull(string? s, bool ifNull)
    {
        return string.IsNullOrEmpty(s) ? ifNull : !ifNull;
    }


    public bool CanEdit => Album.ServiceType is ServiceType.Local;

    public string Describe() => Album.Name;


    public IPlayableItem PlayableItem => Album;
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

public interface IPlayableItemWrapper
{
    IPlayableItem PlayableItem
    {
        get;
    }
}

public interface IEditableItem
{
    bool CanEdit
    {
        get;
    }
}

public interface IPlayableItem
{
    IEnumerable<string> GetPlaybackIds();
}

public interface IAddableItem
{

}

public interface IDescribeable
{
    string Describe();
}