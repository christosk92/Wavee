using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Wavee.UI.ViewModels.Shell.Sidebar;

public record CountedSidebarItem(string Id,
    string Content,
    string Icon,
    int Count,
    Type NavigateTo,
    object? NavigateToParameter) : ISidebarItem, INotifyPropertyChanged
{
    private int _count = Count;

    public int Count
    {
        get => _count;
        set => SetField(ref _count, value);
    }

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