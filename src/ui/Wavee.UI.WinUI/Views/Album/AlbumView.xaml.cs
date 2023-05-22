using System;
using System.Globalization;
using System.Windows.Forms;
using CommunityToolkit.WinUI.UI;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Imaging;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Live;
using Wavee.UI.ViewModels;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

namespace Wavee.UI.WinUI.Views.Album;

public sealed partial class AlbumView : UserControl, INavigablePage
{
    public AlbumView()
    {
        ViewModel = new AlbumViewModel<WaveeUIRuntime>(App.Runtime);
        this.InitializeComponent();
    }

    public bool ShouldKeepInCache(int _)
    {
        //never keep in cache
        return false;
    }

    Option<INavigableViewModel> INavigablePage.ViewModel => ViewModel;

    public AlbumViewModel<WaveeUIRuntime> ViewModel { get; }

    private async void AlbumView_OnLoaded(object sender, RoutedEventArgs e)
    {
        await ViewModel.AlbumFetched.Task;
        UpdateBindings();
    }

    private void UIElement_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        UpdateBindings();
    }

    private void UpdateBindings()
    {
        this.Bindings.Update();
        MainImage.Source = new BitmapImage(new Uri(ViewModel.Image));
        TotalDuration.Text = TotalSum().ToString(@"mm\:ss");

        if (ViewModel.Month.IsSome)
        {
            var month = ViewModel.Month.ValueUnsafe();
            var dateOnly = new DateOnly(
                year: ViewModel.Year,
                month: month,
                day: 1
            );

            string fullMonthName =
                dateOnly.ToString("MMMM");

            if (ViewModel.Day.IsSome)
            {
                var day = ViewModel.Day.ValueUnsafe();
                MoreDescription.Text = $"{fullMonthName} {day}, {dateOnly.Year}";
            }
            else
            {
                MoreDescription.Text = $"{fullMonthName}, {dateOnly.Year}";
            }
        }
        else
        {
            MoreDescription.Text = ViewModel.Year.ToString(CultureInfo.InvariantCulture);
        }
    }
    private TimeSpan TotalSum()
    {
        var totalSum = ViewModel.Discs.Sum(x =>
            x.Tracks.Sum(f => f.Duration.TotalMilliseconds));
        return TimeSpan.FromMilliseconds(totalSum);
    }

    private void RelatedAlbumTapped(object sender, TappedRoutedEventArgs e)
    {
        var tag = (sender as FrameworkElement)?.Tag;
        if (tag is not AudioId id)
        {
            return;
        }

        //if the originalSource contains ButtonsPanel, we tapped on a button and we don't want to navigate
        if (e.OriginalSource is FrameworkElement originalSource
            && originalSource.FindAscendantOrSelf<StackPanel>(x => x.Name is "ButtonsPanel") is { })
        {
            return;
        }

        UICommands.NavigateTo.Execute(id);
    }
}