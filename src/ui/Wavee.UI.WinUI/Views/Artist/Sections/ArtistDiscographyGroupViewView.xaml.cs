using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Labs.WinUI;
using LanguageExt;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Artist;
using Wavee.UI.WinUI.Views.Artist.Sections.Grid;
using Wavee.UI.WinUI.Views.Artist.Sections.List;

namespace Wavee.UI.WinUI.Views.Artist.Sections;

public partial class ArtistDiscographyGroupViewView
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(nameof(Title), typeof(string), typeof(ArtistDiscographyGroupViewView), new PropertyMetadata(default(string)));
    public static readonly DependencyProperty CanSwitchTemplatesProperty = DependencyProperty.Register(nameof(CanSwitchTemplates), typeof(bool), typeof(ArtistDiscographyGroupViewView), new PropertyMetadata(default(bool)));
    public static readonly DependencyProperty ViewsProperty = 
        DependencyProperty.Register(nameof(Views), 
            typeof(List<ArtistDiscographyView>), 
            typeof(ArtistDiscographyGroupViewView), 
            new PropertyMetadata(default(List<ArtistDiscographyView>)));

    public ArtistDiscographyGroupViewView()
    {
        InitializeComponent();
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

    public List<ArtistDiscographyView> Views
    {
        get => (List<ArtistDiscographyView>)GetValue(ViewsProperty);
        set
        {
            SetValue(ViewsProperty, value);
            if (value.Count > 0)
            {
                _waitForViews.TrySetResult();
            }
        }
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
                pages = new Dictionary<string, object>();

                pages[key] = item.Tag switch
                {
                    "grid" => new ArtistDiscographyGridView(Views),
                    "list" => new ArtistDiscographyListView(Views) as UIElement
                };
                _pages[Title] = pages;
                ItemsView.Content = pages[key];
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
                    ItemsView.Content = pages[key];
                }
                else
                {
                    try
                    {
                        ItemsView.Content = page;
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

    private static readonly ConcurrentDictionary<string, Dictionary<string, object>> _pages = new();

    public static void ClearAll()
    {
        foreach (var (key, value) in _pages)
        {
            value.Clear();
        }
        _pages.Clear();
    }
}