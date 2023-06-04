using System;
using System.Threading.Tasks;
using Eum.Spotify;
using LanguageExt.Common;
using Microsoft.UI.Xaml.Controls;
using Wavee.Spotify;


namespace Wavee.UI.WinUI.Views.SignIn;

public sealed partial class SigninInView : UserControl
{
    private SpotifyConfig _config;
    private LoginCredentials _credentials;
    private Action<Exception> _onError;
    private Action<State> _onDone;
    public SigninInView(
        LoginCredentials credentials,
        Action<Exception> onError,
        Action<State> onDone,
        SpotifyConfig config)
    {
        _credentials = credentials;
        _onError = onError;
        _onDone = onDone;
        _config = config;
        this.InitializeComponent();

        _ = Task.Run(async () => await SpinTask());
    }

    private async Task SpinTask()
    {
        try
        {
            var state = await SpotifyView.LoginAsync(_config, _credentials, default);
            _onDone(state);
        }
        catch (Exception e)
        {
            _onError(e);
        }
        finally
        {
            _onError = null;
            _onDone = null;
            _credentials = null;
            _config = null;
        }
    }
}