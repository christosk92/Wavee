using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using CommunityToolkit.WinUI.Media;
using LanguageExt.UnsafeValueAccess;
using Microsoft.UI;
using System.Threading.Tasks;
using System.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.WinUI.Controls;
using CommunityToolkit.WinUI.Helpers;
using LanguageExt;
using Microsoft.UI.Dispatching;
using Wavee.UI.Providers;
using Wavee.UI.ViewModels.NowPlaying;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Documents;
using Wavee.UI.ViewModels.Shell;
using Wavee.UI.WinUI.Converters;
using Wavee.UI.WinUI.Views.Common;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.NowPlaying
{
    public sealed partial class BottomPlaybackControll : UserControl
    {
        private DispatcherQueue _dispatcherQueue;
        private Guid _positionCallback;
        private NowPlayingViewModel _viewModel;

        public BottomPlaybackControll()
        {
            this.InitializeComponent();
            _dispatcherQueue = this.DispatcherQueue;
            this.DataContextChanged += (sender, args) =>
            {
                if (args.NewValue is NowPlayingViewModel playbackViewModel)
                {
                    _viewModel = playbackViewModel;
                    if (_positionCallback == Guid.Empty)
                    {
                        _positionCallback = playbackViewModel.RegisterTimerCallback(Callback);
                    }

                    _viewModel.VolumeChanged -= VolumeChanged;
                    _viewModel.VolumeChanged += VolumeChanged;
                    VolumeChanged(null, _viewModel.Volume);
                }
            };
        }

        private void VolumeChanged(object sender, double? e)
        {
            VolumeSliderHorizontal.Value = e ?? 100;
            VolumeSliderVertical.Value = e ?? 100;
        }

        public NowPlayingViewModel ViewModel => (NowPlayingViewModel)DataContext;
        public AttachedCardShadow CommonShadow => (AttachedCardShadow)this.Resources["CommonShadow"];


        private double _prevPosition;
        private void Callback()
        {
            if (_userIsSeeking) return;

            var position = _viewModel?.Position.TotalMilliseconds ?? 0;
            if (LargerThanHalfASecond(_prevPosition, position))
            {
                _prevPosition = position;
                _dispatcherQueue.TryEnqueue(DispatcherQueuePriority.Normal, () =>
                {
                    var pos = ViewModel?.Position;
                    PositionSlider.Value = pos?.TotalMilliseconds ?? 0;
                    var positionFormat = MilliSecondsToTimestampConverter.ConvertTo(pos);
                    var durationFormat = MilliSecondsToTimestampConverter.ConvertTo(ViewModel?.CurrentTrack.Item?.Duration);
                    DurationBlock.Inlines.Clear();
                    DurationBlock.Inlines.Add(new Run
                    {
                        Text = positionFormat,
                        FontWeight = FontWeights.SemiBold
                    });
                    DurationBlock.Inlines.Add(new Run
                    {
                        Text = " / ",
                        Foreground = (SolidColorBrush)Application.Current.Resources["ApplicationSecondaryForegroundThemeBrush"]
                    });
                    DurationBlock.Inlines.Add(new Run
                    {
                        Text = durationFormat,
                        Foreground = (SolidColorBrush)Application.Current.Resources["ApplicationSecondaryForegroundThemeBrush"]
                    });

                    // DurationBlock.Text = $"{positionFormat} / {durationFormat}";
                });
            }
        }


        private static bool LargerThanHalfASecond(double prevPosition, double position)
        {
            return Math.Abs(prevPosition - position) >= 500;
        }

        private bool _userIsSeeking;
        public Brush ExtractColorAndCreateAcrylicBrush((IWaveePlayableItem Item, IWaveeUIAuthenticatedProfile Profile) valueTuple)
        {
            var (waveePlayableItem, profile) = valueTuple;

            if (waveePlayableItem is null)
            {
                return (Brush)Application.Current.Resources["AcrylicInAppFillColorDefaultBrush"];
            }

            var image = waveePlayableItem.Images.HeadOrNone().Map(x => x.Url);
            if (image.IsNone)
            {
                return (Brush)Application.Current.Resources["AcrylicInAppFillColorDefaultBrush"];
            }

            var url = image.ValueUnsafe();
            var dark = this.ActualTheme is ElementTheme.Dark;
            var colorCode = Task.Run(async () => await GetColorFor(url, dark, profile)).ConfigureAwait(false)
                .GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(colorCode))
            {
                var br = (AcrylicBrush)this.Resources["AcrylicInAppFillColorDefaultBrushCopy"];
                br.TintColor = colorCode.ToColor();
                return br;
            }
            return (Brush)Application.Current.Resources["AcrylicInAppFillColorDefaultBrushCopy"];
        }

        public AttachedCardShadow ExtractColorAndCreateShadow((IWaveePlayableItem Item, IWaveeUIAuthenticatedProfile Profile) valueTuple, AttachedCardShadow attachedCardShadow)
        {
            var (waveePlayableItem, profile) = valueTuple;

            if (waveePlayableItem is null)
            {
                return attachedCardShadow;
            }

            var image = waveePlayableItem.Images.HeadOrNone().Map(x => x.Url);
            if (image.IsNone)
            {
                return attachedCardShadow;
            }

            var url = image.ValueUnsafe();
            var dark = this.ActualTheme is ElementTheme.Dark;
            var colorCode = Task.Run(async () => await GetColorFor(url, dark, profile)).ConfigureAwait(false)
                .GetAwaiter().GetResult();
            if (!string.IsNullOrEmpty(colorCode))
            {
                var br = this.CommonShadow;
                br.Color = colorCode.ToColor();
                return br;
            }
            return attachedCardShadow;
        }

        private static SemaphoreSlim _semaphore = new SemaphoreSlim(1, 1);
        private static Dictionary<string, (string, string)> _colorsCache = new();
        private static async Task<string> GetColorFor(string url, bool dark, IWaveeUIAuthenticatedProfile profile)
        {
            await _semaphore.WaitAsync();
            try
            {
                if (!_colorsCache.TryGetValue(url, out var colors))
                {
                    colors = await profile.ExtractColorFor(url);
                    _colorsCache[url] = colors;
                }

                if (dark) return colors.Item1;
                return colors.Item2;
            }
            catch (Exception ex)
            {
                return Colors.Black.ToHex();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public IEnumerable<MetadataItem> MapToMetadataItems(Seq<WaveePlayableItemDescription> waveePlayableItemDescriptions)
        {
            return waveePlayableItemDescriptions.Select(x => new MetadataItem
            {
                Label = x.Name
            });
        }


        public object PausedToIconConverter(bool b)
        {
            //       <FontIcon x:Name="PlayPauseIcon"
            //           Margin="0,-4,0,0"
            //           Glyph="&#xF5B0;" />
            if (b)
            {
                return new FontIcon
                {
                    FontFamily = SegoeFluentIcons,
                    Glyph = "\uF5B0",
                    Margin = new Thickness(0, -4, 0, 0)
                };
            }

            return new FontIcon
            {
                FontFamily = SegoeFluentIcons,
                Glyph = "\uF8AE",
                Margin = new Thickness(0, -4, 0, 0)
            };
        }

        private static FontFamily SegoeFluentIcons => (FontFamily)Microsoft.UI.Xaml.Application.Current.Resources["FluentIcons"]!;

        public RightSidebarViewModel RightSidebar
        {
            get => (RightSidebarViewModel)GetValue(RightSidebarProperty);
            set => SetValue(RightSidebarProperty, value);
        }

        // public string FormatStringForActiveDevice(WaveeRemoteDeviceViewModel waveeRemoteDeviceViewModel)
        // {
        //     //PLAYING ON DESKTOP
        //     return $"PLAYING ON {waveeRemoteDeviceViewModel?.Name}";
        // }


        public bool? IsRepeating(WaveeRepeatStateType waveeRepeatStateType)
        {
            return waveeRepeatStateType is WaveeRepeatStateType.Context or WaveeRepeatStateType.Track;
        }

        public IconElement RepeatStateToIcon(WaveeRepeatStateType waveeRepeatStateType)
        {
            return waveeRepeatStateType switch
            {
                WaveeRepeatStateType.Context or WaveeRepeatStateType.None => new SymbolIcon
                {
                    Symbol = Symbol.RepeatAll
                },
                WaveeRepeatStateType.Track => new SymbolIcon
                {
                    Symbol = Symbol.RepeatOne
                }
            };
        }

        public string FormatStringForActiveDevice(WaveeRemoteDeviceViewModel waveeRemoteDeviceViewModel)
        {
            return $"PLAYING ON {waveeRemoteDeviceViewModel?.Name}";

        }

        public Visibility IsNullThenCollapsed(WaveeRemoteDeviceViewModel? x)
        {
            return x is null ? Visibility.Collapsed : Visibility.Visible;
        }

        // private int itemsInOverflow = 0;
        private object _lock = new();

        private void MainMediaControlsCommandBar_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            lock (_lock)
            {
                // Debug.WriteLine($"OverflowAction: {args.Action}");
                // if (args.Action is CommandBarDynamicOverflowAction.AddingToOverflow)
                //     itemsInOverflow++;
                // else if(args.Action is CommandBarDynamicOverflowAction.RemovingFromOverflow)
                //     itemsInOverflow--;
                var itemsInOverflow = (sender as CommandBar)!.PrimaryCommands.Count(x => x.IsInOverflow);
                Debug.WriteLine($"ItemsInOverflow: {itemsInOverflow}");
                if (itemsInOverflow is 0)
                    (sender as CommandBar)!.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Collapsed;
                else (sender as CommandBar)!.OverflowButtonVisibility = CommandBarOverflowButtonVisibility.Visible;

                foreach (var item in (sender as CommandBar)!.PrimaryCommands)
                {
                    if (item is AppBarElementContainer x && x == VolumeButton)
                    {
                        NonOverflowingVolumeButton.Visibility = item.IsInOverflow ? Visibility.Collapsed : Visibility.Visible;
                        OverflowingVolumeGrid.Visibility =
                            item.IsInOverflow ? Visibility.Visible : Visibility.Collapsed;
                    }
                }
            }
        }


        private void VolumeFlyoutOpening(object sender, object e)
        {
            if (VolumeButton.IsInOverflow)
            {
                VolumeFlyout.Hide();
            }

        }

        private async void PositionSlider_OnSliderManipulationCompleted(object sender, EventArgs e)
        {
            Debug.WriteLine("Manipulation completed");
            var val = (sender as SliderEx)!.Value;
            await ViewModel.SeekCommand.ExecuteAsync(TimeSpan.FromMilliseconds(val));
            _userIsSeeking = false;
        }

        private void PositionSlider_OnSliderManipulationMoved(object sender, EventArgs e)
        {
            // _userIsSeeking = true;
            // Debug.WriteLine("Manipulation moved");
        }

        private void PositionSlider_OnSliderManipulationStarted(object sender, EventArgs e)
        {
            _userIsSeeking = true;
            Debug.WriteLine("Manipulatiuon started");
        }

        private async void VolumeSliderManipulationCompleted(object sender, EventArgs e)
        {
            var val = (sender as SliderEx).Value;
            await ViewModel.SetVolumeCommand.ExecuteAsync(val);
            _isSliding = false;
        }

        private void VolumeSliderManipulationStarted(object sender, EventArgs e)
        {
            _isSliding = true;
        }

        public string FormatVolume(double? d)
        {
            if (d is null) return "--";

            var x = (int)Math.Round(d.Value);
            return x.ToString();
        }

        private bool _isSliding;
        public static readonly DependencyProperty RightSidebarProperty = DependencyProperty.Register(nameof(RightSidebar), typeof(RightSidebarViewModel), typeof(BottomPlaybackControll), new PropertyMetadata(default(RightSidebarViewModel)));

        private void VolumeSliderVertical_OnSliderManipulationMoved(object sender, EventArgs e)
        {
            if (_isSliding)
            {
                ViewModel.Volume = (sender as Slider).Value;
            }
        }

        public bool? IsShowing(RightSidebarComponentViewModel? item, string typeName)
        {
            if (item is null) return false;

            var typeToCheckgainst = item.GetType();
            return typeToCheckgainst.FullName == typeName;
        }
    }
}
