using System;
using Windows.Foundation;
using DynamicData.Binding;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.ViewModels.ViewModels;

namespace Wavee.UI.WinUI.Views.Shell;

public sealed partial class Dialogs : UserControl
{
    private IDisposable? _currentPageSubscription;
    private IAsyncOperation<ContentDialogResult>? _task;
    private ContentDialog? _openDialog;

    public Dialogs()
    {
        this.InitializeComponent();
    }

    public MainViewModel ViewModel => (MainViewModel)DataContext;

    private void DialogOpened(object sender, RoutedEventArgs e)
    {
        var contentDialog = new ContentDialog();
        contentDialog.XamlRoot = ContentDialogHost.XamlRoot;
        contentDialog.FullSizeDesired = true;
        contentDialog.Resources["ContentDialogMaxWidth"] = (double)762;
        contentDialog.Resources["ContentDialogMaxHeight"] = (double)490;
        contentDialog.ContentTemplateSelector = (DataTemplateSelector)Resources["PageViewSelector"];
        // create a _currentPageSubscription that listens to the CurrentPage and sets the content accordingly
        _currentPageSubscription = ViewModel
            .DialogScreen
            .WhenPropertyChanged(x => x.CurrentPage)
            .Subscribe(x =>
            {
                if (x is { } page)
                {
                    contentDialog.Content = page;
                }
            });

        _task = contentDialog.ShowAsync();
    }

    private void DialogClosed(object sender, RoutedEventArgs e)
    {
        _openDialog?.Hide();
        _currentPageSubscription?.Dispose();
    }
}