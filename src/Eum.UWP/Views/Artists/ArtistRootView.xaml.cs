// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Threading.Tasks;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Eum.UI.ViewModels.Artists;
using Eum.UWP.XamlConverters;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UWP.Views.Artists
{
    public sealed partial class ArtistRootView : UserControl
    {
        public ArtistRootView(ArtistRootViewModel viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            this.DataContext = viewModel;
        }
        public ArtistRootViewModel ViewModel { get; }

        private void GridView_SizeCHanged(object sender, SizeChangedEventArgs e)
        {
            var s = (sender as ListView);

            var columns = Math.Clamp(Math.Floor(s.ActualWidth / 300), 1, 2);
            // if (Math.Abs(columns - 1) < 0.001)
            // {
            //     s.MaxHeight = 5 * ((ItemsWrapGrid) s.ItemsPanelRoot).ItemHeight;
            // }
            // else
            // {
            //     s.MaxHeight = Double.PositiveInfinity;
            // }
            ((ItemsWrapGrid)s.ItemsPanelRoot).ItemWidth = e.NewSize.Width / columns;
        }

        private async void ItemsREpeater_Loaded(object sender, RoutedEventArgs e)
        {
            var obj = (sender as ItemsRepeater);
            if (_listeners.TryRemove(obj, out var data))
            {
                data.Close();
            }

            _listeners[obj] = new TemplateChangedHolder(obj, (TemplateTypeTolayoutConverter)Application.Current.Resources["TemplateTypeToLayoutConverter"],
                (TemplateTypeTolayoutConverter)this.Resources["TemplateTypeToItemTemplateConverter"]);
            await Task.Delay(50);
            _listeners[obj].Start();
        }

        private ConcurrentDictionary<ItemsRepeater, TemplateChangedHolder> _listeners =
            new ConcurrentDictionary<ItemsRepeater, TemplateChangedHolder>();

        private void ArtistRootView_OnUnloaded(object sender, RoutedEventArgs e)
        {
            foreach (var templateChangedHolder in _listeners)
            {
                templateChangedHolder.Value.Close();
            }

            _listeners.Clear();
        }
    }

    internal class TemplateChangedHolder
    {
        private ItemsRepeater? _itemsRepeater;
        private TemplateTypeTolayoutConverter? _templateTypeTolayoutConverter;
        private TemplateTypeTolayoutConverter _templateTypeToItemTemplateConverter;

        private DiscographyGroup? dt;
        public TemplateChangedHolder(ItemsRepeater itemsRepeater,
            TemplateTypeTolayoutConverter layoutConverter,
            TemplateTypeTolayoutConverter itemTemplateConverter)
        {
            _itemsRepeater = itemsRepeater;
            _templateTypeTolayoutConverter = layoutConverter;
            _templateTypeToItemTemplateConverter = itemTemplateConverter;
        }

        public void Start()
        {
            dt = _itemsRepeater.Tag as DiscographyGroup;
            if (dt == null) return;
            Set();
            dt.PropertyChanged += DtOnPropertyChanged;
        }

        private void DtOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(DiscographyGroup.TemplateType):
                    Set();
                    break;
            }
        }

        public void Close()
        {
            dt.PropertyChanged -= DtOnPropertyChanged;
            _itemsRepeater = null;
            _templateTypeToItemTemplateConverter = null;
            _templateTypeTolayoutConverter = null;
            dt = null;
        }
        void Set()
        {
            if (_itemsRepeater != null)
            {
                _itemsRepeater.ItemTemplate =
                    _templateTypeToItemTemplateConverter
                        .Convert(dt.TemplateType, null, null, null);
                _itemsRepeater.Layout =
                    (Layout) (_templateTypeTolayoutConverter
                        .Convert(dt.TemplateType, null, null, null));
            }
        }
    }
}
