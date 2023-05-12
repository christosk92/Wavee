using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Windows.Forms;
using LanguageExt;
using Microsoft.UI.Xaml;
using ReactiveUI;
using Wavee.UI.Helpers;
using Wavee.UI.ViewModels;
using Unit = System.Reactive.Unit;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

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
            SidebarControl.PlaylistFilters = new Seq<PlaylistSourceFilter>(new[] { PlaylistSourceFilter.Yours, PlaylistSourceFilter.Others });
            SidebarControl.PlaylistSort = ViewModel.PlaylistSorts;

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


                this.SidebarControl.WhenAnyValue(x => x.PlaylistSort)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(x =>
                    {
                        ViewModel.PlaylistSorts = x;
                        return Unit.Default;
                    })
                    .Subscribe()
                    .DisposeWith(disposable);

                this.SidebarControl.WhenAnyValue(x => x.PlaylistFilters)
                    .ObserveOn(RxApp.MainThreadScheduler)
                    .Select(x =>
                    {
                        ViewModel.PlaylistSourceFilters = x;
                        return Unit.Default;
                    })
                    .Subscribe()
                    .DisposeWith(disposable);


                this.OneWayBind(ViewModel,
                    vmProperty: viewModel => viewModel.SidebarItems,
                    viewProperty: view => view.SidebarControl.SidebarItems);

                ViewModel.HasPlaylists
                    .Select(c => c ? Visibility.Collapsed : Visibility.Visible)
                    .BindTo(this, x => x.SidebarControl.NoPlaylistsView.Visibility);

                // this.OneWayBind(ViewModel,
                //     vmProperty: vm => vm.PlaylistSourceFilters,
                //     viewProperty: v => v.SidebarControl.PlaylistFilterTokenView.SelectedItems,
                //     vmToViewConverter: filters =>
                //     {
                //         var items = new List<object>(filters.Length);
                //         foreach (var filter in filters)
                //         {
                //             switch (filter)
                //             {
                //                 case PlaylistSourceFilter.Yours:
                //                     items.Add(this.SidebarControl.PlaylistFilterTokenView.Items[0]);
                //                     break;
                //                 case PlaylistSourceFilter.Others:
                //                     items.Add(this.SidebarControl.PlaylistFilterTokenView.Items[0]);
                //                     break;
                //                 default:
                //                     throw new ArgumentOutOfRangeException();
                //             }
                //         }
                //
                //         return items;
                //     },
                //     viewToVmConverter: selectedItems =>
                //     {
                //         var filters = new List<PlaylistSourceFilter>(selectedItems.Count);
                //         foreach (var selectedItem in selectedItems)
                //         {
                //             switch (selectedItem)
                //             {
                //                 case TextBlock textBlock when textBlock.Text == "Yours":
                //                     filters.Add(PlaylistSourceFilter.Yours);
                //                     break;
                //                 case TextBlock textBlock when textBlock.Text == "Others":
                //                     filters.Add(PlaylistSourceFilter.Others);
                //                     break;
                //                 default:
                //                     throw new ArgumentOutOfRangeException();
                //             }
                //         }
                //
                //         return filters.ToArray();
                //     }
                // );
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
