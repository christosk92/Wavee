using CommunityToolkit.Labs.WinUI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Views.Artist
{
    public sealed partial class ArtistDiscographyView : UserControl
    {
        public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ArtistDiscographyView), new PropertyMetadata(default(string)));
        public static readonly DependencyProperty CanSwitchTemplatesProperty = DependencyProperty.Register(nameof(CanSwitchTemplates), 
            typeof(bool), typeof(ArtistDiscographyView), new PropertyMetadata(default(bool)));

        public static readonly DependencyProperty ViewsProperty =
            DependencyProperty.Register(nameof(Views),
                typeof(List<ArtistDiscographyItem>),
                typeof(ArtistDiscographyView),
                new PropertyMetadata(default(List<ArtistDiscographyItem>)));

        public ArtistDiscographyView()
        {
            this.InitializeComponent();
        }

        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public bool CanSwitchTemplates
        {
            get => (bool)GetValue(CanSwitchTemplatesProperty);
            set => SetValue(CanSwitchTemplatesProperty, value);
        }

        public List<ArtistDiscographyItem> Views
        {
            get => (List<ArtistDiscographyItem>)GetValue(ViewsProperty);
            set
            {
                SetValue(ViewsProperty, value);
                if (value.Count > 0)
                {
                    _waitForViews.TrySetResult();
                    if (!CanSwitchTemplates && !AlwaysHorizontal)
                    {
                        CurrentView = new ArtistDiscographyGridView(Views);
                    }
                }
            }
        }

        public object CurrentView
        {
            get => (object)GetValue(CurrentViewProperty);
            set => SetValue(CurrentViewProperty, value);
        }

        public bool AlwaysHorizontal
        {
            get => (bool)GetValue(AlwaysHorizontalProperty);
            set => SetValue(AlwaysHorizontalProperty, value);
        }

        private readonly TaskCompletionSource _waitForViews = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        private async void SwitchTemplatesControl_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            await _waitForViews.Task;
            var items = e.AddedItems;
            if (items.Count > 0)
            {
                var item = (SegmentedItem)items[0];

                var key = item.Tag.ToString()!;
                if (!_pages.TryGetValue(Title, out var pages))
                {
                    pages = new Dictionary<string, object>
                    {
                        [key] = item.Tag switch
                        {
                            "grid" => new ArtistDiscographyGridView(Views),
                            "list" => new ArtistDiscographyListView(Views) as UIElement
                        }
                    };

                    _pages[Title] = pages;
                    CurrentView = pages[key];
                }
                else
                {
                    if (!pages.TryGetValue(key, out var page))
                    {
                        pages[key] = item.Tag switch
                        {
                            "grid" => new ArtistDiscographyGridView(Views),
                            "list" => new ArtistDiscographyListView(Views) as UIElement
                        };
                        CurrentView = pages[key];
                    }
                    else
                    {
                        try
                        {
                            CurrentView = page;
                        }
                        catch (Exception exception)
                        {
                            Console.WriteLine(exception);
                        }
                    }
                }
            }

            //GC.Collect();
        }

        private readonly ConcurrentDictionary<string, Dictionary<string, object>> _pages = new();
        public static readonly DependencyProperty CurrentViewProperty = DependencyProperty.Register(nameof(CurrentView), typeof(object), typeof(ArtistDiscographyView), new PropertyMetadata(default(object)));
        public static readonly DependencyProperty AlwaysHorizontalProperty = DependencyProperty.Register(nameof(AlwaysHorizontal), typeof(bool), typeof(ArtistDiscographyView),
            new PropertyMetadata(default(bool), AlwaysHorizontalChanged));

        private static async void AlwaysHorizontalChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var x = (ArtistDiscographyView)d;
            if (e.NewValue is true)
            {
                await x._waitForViews.Task;
                x.CurrentView = new ArtistDiscographyHorizontalView(x.Views);
            }
            else if(!x.CanSwitchTemplates)
            {
                x.CurrentView = new ArtistDiscographyGridView(x.Views);
            }
        }

        public void ClearAll()
        {
            foreach (var (key, value) in _pages)
            {
                foreach (var (key1, value1) in value)
                {
                    if (value1 is IDisposable disposable)
                    {
                        disposable.Dispose();
                    }
                }
                value.Clear();
            }
            _pages.Clear();

        }
    }
}
