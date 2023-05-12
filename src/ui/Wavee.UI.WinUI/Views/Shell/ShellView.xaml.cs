using System;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using Wavee.UI.Helpers;
using Wavee.UI.ViewModels;

namespace Wavee.UI.WinUI.Views.Shell
{
    public sealed partial class ShellView : UserControl, IViewFor<MainViewModel>
    {
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty
            .Register(nameof(ViewModel), typeof(MainViewModel), typeof(ShellView), new PropertyMetadata(null));

        public ShellView()
        {
            this.InitializeComponent();
            ViewModel = new MainViewModel();
            SidebarControl.SetSidebarWidth(Services.UiConfig.SidebarWidth ?? Constants.DefaultSidebarWidth);
            this.WhenActivated(disposable =>
            {
                this.SidebarControl.SidebarWidthChanged
                    .Throttle(TimeSpan.FromMilliseconds(500))
                    .Skip(1) // Won't save on UiConfig creation.
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(x =>
                    {
                        Services.UiConfig.SidebarWidth = x;
                        return Unit.Default;
                    })
                    .Subscribe()
                    .DisposeWith(disposable);

                this.OneWayBind(ViewModel,
                    vmProperty: viewModel => viewModel.SidebarItems,
                    viewProperty: view => view.SidebarControl.SidebarItems);
            });
        }

        public MainViewModel ViewModel
        {
            get => (MainViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        object IViewFor.ViewModel
        {
            get => ViewModel;
            set => ViewModel = (MainViewModel)value;
        }
    }
}
