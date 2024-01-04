using Microsoft.UI.Xaml;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.Profile;

namespace Wavee.UI.WinUI;

public partial class App : Application
{
    public App()
    {
        this.InitializeComponent();
        
        var viewFactory = WinUIViewFactory.Create()
            .Add<ProfileViewModel, ProfileViewModel>(ViewType.Dialog)
            .Build();
    }

    /// <summary>
    /// Invoked when the application is launched.
    /// </summary>
    /// <param name="args">Details about the launch request and process.</param>
    protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
    {
        m_window = new MainWindow();
        m_window.Activate();
    }

    private Window m_window;
}