// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Eum.UI.Items;
using Eum.UI.ViewModels.Search.SearchItems;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UWP.Views.Search
{
    public sealed partial class TopHitView : UserControl
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(ISearchItem), typeof(TopHitView), new PropertyMetadata(default(ISearchItem)));

        public TopHitView()
        {
            this.InitializeComponent();
        }

        public ISearchItem Item
        {
            get => (ISearchItem) GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }

        public string ToUpper(EumEntityType eumEntityType)
        {
            return eumEntityType.ToString().ToUpper();
        }
    }
}
