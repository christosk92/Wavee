using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wavee.UI.Features.Search.ViewModels;

public sealed class SearchGroupViewModel
{
    public  string Title { get; set; }
    public IReadOnlyCollection<SearchItemViewModel> Items { get; set; }
    public  ulong Total { get; set; }
    public  SearchGroupRenderingType RenderingType { get; set; }
}

public enum SearchGroupRenderingType
{
    TopResults,
    Horizontal,
    Tracks
}

public sealed class SearchItemViewModel : INotifyPropertyChanged
{
    public  string Id { get; set; }
    public  string Title { get; set; }
    public  string LargeImageUrl { get; set; }
    public  string SmallImageUrl { get; set; }
    public  string MediumImageUrl { get; set; }
    public  bool IsArtist { get; set; }
    public  string Description { get; set; }
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}