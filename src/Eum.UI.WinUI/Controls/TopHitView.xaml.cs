// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Eum.UI.ViewModels.Search.SearchItems;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Controls
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
    }
}
