using System;
using System.ComponentModel;
using System.Windows.Forms;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Animation;
using Wavee.UI.ViewModels.Library;
using Wavee.UI.WinUI.Views.Artist;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;


namespace Wavee.UI.WinUI.Views.Library;

public sealed partial class LibraryArtistsView : UserControl
{
    public LibraryArtistsView(LibraryArtistsViewModel viewModel)
    {
        ViewModel = viewModel;
        this.InitializeComponent();
        viewModel.PropertyChanged += ViewModelOnPropertyChanged;
    }

    private void ViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(LibraryArtistsViewModel.SelectedItem))
        {
            if (ViewModel.SelectedItem is not null)
            {
                var stkcp = new Grid();
                stkcp.RowDefinitions.Add(new RowDefinition()
                {
                    Height = GridLength.Auto
                });
                stkcp.RowDefinitions.Add(new RowDefinition()
                {
                    Height = new GridLength(1, GridUnitType.Star)
                });
                stkcp.ChildrenTransitions = new TransitionCollection()
                {
                    new EntranceThemeTransition(),
                };
                Gr.Content = null;
                GC.Collect();
                Gr.Content = stkcp;
                var txtblock = new TextBlock
                {
                    Text = ViewModel.SelectedItem.Name,
                    FontSize = 24,
                    FontWeight = FontWeights.Bold,
                    Margin = new Thickness(24)
                };
                Grid.SetRow(txtblock, 0);
                stkcp.Children.Add(txtblock);
                var scroller = new ScrollView
                {
                    Content = new ArtistDiscographyComponent
                    {
                        Discography = ViewModel.SelectedItem.Discography
                    }
                };
                Grid.SetRow(scroller, 1);
                stkcp.Children.Add(scroller);
            }
        }
    }


    public LibraryArtistsViewModel ViewModel { get; }

    private void LibraryArtistsView_OnLoaded(object sender, RoutedEventArgs e)
    {
        this.Bindings.Update();
    }

    private void LibraryArtistsView_OnUnloaded(object sender, RoutedEventArgs e)
    {
        this.Bindings.StopTracking();
    }

    private void AnnotatedScrollBar_DetailLabelRequested(AnnotatedScrollBar sender, AnnotatedScrollBarDetailLabelRequestedEventArgs args)
    {

    }

}