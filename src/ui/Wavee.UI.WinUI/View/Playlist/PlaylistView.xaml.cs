using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic.Logging;
using Wavee.UI.ViewModel.Playlist;
using Wavee.UI.ViewModel.Shell;
using Wavee.UI.WinUI.Navigation;
using Log = Serilog.Log;

namespace Wavee.UI.WinUI.View.Playlist;

public sealed partial class PlaylistView : UserControl, INavigable, ICacheablePage
{
    private readonly CancellationTokenSource _cts = new();
    public PlaylistView()
    {
        ViewModel = new PlaylistViewModel(ShellViewModel.Instance.User);
        this.InitializeComponent();
    }

    public PlaylistViewModel ViewModel { get; }
    public async void NavigatedTo(object parameter)
    {
        if (parameter is string id)
        {
            try
            {
                await ViewModel.Initialize(id, _cts.Token);
            }
            catch (TaskCanceledException)
            {
            }
            catch (OperationCanceledException)
            {
            }
            catch (ObjectDisposedException disposed)
            {

            }
            catch (Exception e)
            {
                Log.Error(e, "An error occurred while initializing the playlist.");
            }
            finally
            {
                _cts.Dispose();
            }
        }
    }

    public void NavigatedFrom(NavigationMode mode)
    {
        try
        {
            _cts.Cancel();
        }
        catch (Exception e)
        {

        }
    }

    public bool ShouldKeepInCache(int currentDepth)
    {
        return currentDepth <= 4;
    }

    public void RemovedFromCache()
    {
    }
}