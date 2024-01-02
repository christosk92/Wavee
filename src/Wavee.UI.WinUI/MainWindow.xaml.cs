using Microsoft.UI.Xaml;
using System;
using Microsoft.Extensions.DependencyInjection;

namespace Wavee.UI.WinUI;

public sealed partial class MainWindow : Window
{
    public MainWindow()
    {
        var sp = BuildServiceProvider();
        this.InitializeComponent();
    }

    public MainWindowViewModel ViewModel { get; }

    private static IServiceProvider BuildServiceProvider()
    {
        var coll = new ServiceCollection();
        coll.AddWavee();
        coll.AddMediator(x =>
        {
            x.ServiceLifetime = ServiceLifetime.Transient;
        });

        return coll;
    }

}