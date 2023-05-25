using System;
using System.Globalization;
using System.Linq;
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
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
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
    public void RemovedFromCache()
    {
        ViewModel.Clear();
    }

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
        AlbumType.Text = ViewModel.Type.ToUpper();
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
        var totalSum = ViewModel.Discs
            .Sum(x => x.Tracks.Sum(f => f.Duration.TotalMilliseconds));
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

    /*
     *                <StackPanel Spacing="4" Orientation="Horizontal">
                                <FontIcon FontFamily="Segoe Fluent Icons" Glyph="&#xEB52;" />
                                <TextBlock Text="Save"/>
                            </StackPanel>
     */
    private void SaveButton_OnTapped(object sender, TappedRoutedEventArgs e)
    {
        ViewModel.SaveCommand.Execute(new ModifyLibraryCommand(Seq1(ViewModel.Id), !ViewModel.IsSaved));
    }

    public object SavedToContent(bool b)
    {
        var stckp = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8
        };
        if (b)
        {
            stckp.Children.Add(new FontIcon
            {
                FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"),
                Glyph = "\uEB52"
            });
            stckp.Children.Add(new TextBlock
            {
                Text = "Remove"
            });
        }
        else
        {
            stckp.Children.Add(new FontIcon { FontFamily = new Microsoft.UI.Xaml.Media.FontFamily("Segoe Fluent Icons"), Glyph = "\uE006" });
            stckp.Children.Add(new TextBlock { Text = "Save" });
        }

        return stckp;
    }
}