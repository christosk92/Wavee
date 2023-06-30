using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.Client.Artist;

namespace Wavee.UI.WinUI.View.Artist.Views.Discography;
public sealed partial class DiscographyPageView : UserControl
{
    //public static readonly DependencyProperty CurrentViewProperty = DependencyProperty.Register(nameof(CurrentView), typeof(ViewType), typeof(DiscographyPageView), new PropertyMetadata(default(ViewType)));
    public static readonly DependencyProperty GetReleasesFuncProperty = DependencyProperty.Register(nameof(GetReleasesFunc), typeof(GetReleases), typeof(DiscographyPageView), new PropertyMetadata(default(GetReleases), CurrentViewChanged));
    public static readonly DependencyProperty CurrentViewIdxProperty = DependencyProperty.Register(nameof(CurrentViewIdx), typeof(int), typeof(DiscographyPageView), new PropertyMetadata(default(int), CurrentViewChanged));
    private DiscographyPageGridView _gridView;
    private DiscographyPageListView _listView;

    public DiscographyPageView()
    {
        this.InitializeComponent();
    }

    public GetReleases GetReleasesFunc
    {
        get => (GetReleases)GetValue(GetReleasesFuncProperty);
        set => SetValue(GetReleasesFuncProperty, value);
    }

    public int CurrentViewIdx
    {
        get => (int)GetValue(CurrentViewIdxProperty);
        set => SetValue(CurrentViewIdxProperty, value);
    }

    public ViewType CurrentView
    {
        get => (ViewType)CurrentViewIdx;
        set => CurrentViewIdx = (int)value;
    }

    private static void CurrentViewChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var x = (DiscographyPageView)d;
        bool forceRerender = e.Property == GetReleasesFuncProperty;
        x.UpdateUI(forceRerender);
    }

    private void UpdateUI(bool forceRerender)
    {
        if (GetReleasesFunc is null)
        {
            return;
        }

        if (forceRerender)
        {
            _gridView = null;
            _listView = null;
        }
        switch (CurrentView)
        {
            case ViewType.Grid:
                _gridView ??= new DiscographyPageGridView(GetReleasesFunc);
                Presenter.Content = _gridView;
                break;
            case ViewType.List:
                _listView ??= new DiscographyPageListView(GetReleasesFunc);
                Presenter.Content = _listView;
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }
}
public enum ViewType
{
    Grid,
    List
}
