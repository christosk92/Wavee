using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Wavee.UI.Avalonia.ViewModels;
using Wavee.UI.Avalonia.Views;
using Wavee.UI.Core;

namespace Wavee.UI.Avalonia;

public partial class App : Application
{
    public App()
    {
        Global.GetPersistentStoragePath = () =>
        {
            var c = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            var p = System.IO.Path.Combine(c, "Wavee");
            if (!System.IO.Directory.Exists(p))
            {
                System.IO.Directory.CreateDirectory(p);
            }
            
            return p;
        };
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }
}