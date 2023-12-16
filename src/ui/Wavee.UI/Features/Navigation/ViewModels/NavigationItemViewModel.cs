using CommunityToolkit.Mvvm.ComponentModel;

namespace Wavee.UI.Features.Navigation.ViewModels;

public abstract class NavigationItemViewModel : ObservableObject
{
    private NavigationItemViewModel? _selectedItem;
    private SharedThickness _childrenThickness;
    public virtual NavigationItemViewModel[] Children { get; } = Array.Empty<NavigationItemViewModel>();

    public NavigationItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set => SetProperty(ref _selectedItem, value);
    }

    public SharedThickness ChildrenThickness
    {
        get => _childrenThickness;
        set => SetProperty(ref _childrenThickness, value);
    }
}

public struct SharedThickness
{
    private double _Left;
    private double _Top;
    private double _Right;
    private double _Bottom;

    public SharedThickness(double uniformLength) => this._Left = this._Top = this._Right = this._Bottom = uniformLength;

    public SharedThickness(double left, double top, double right, double bottom)
    {
        this._Left = left;
        this._Top = top;
        this._Right = right;
        this._Bottom = bottom;
    }

    public double Left
    {
        get => this._Left;
        set => this._Left = value;
    }

    public double Top
    {
        get => this._Top;
        set => this._Top = value;
    }

    public double Right
    {
        get => this._Right;
        set => this._Right = value;
    }

    public double Bottom
    {
        get => this._Bottom;
        set => this._Bottom = value;
    }
}