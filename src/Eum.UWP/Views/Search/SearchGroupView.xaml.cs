// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Windows.UI.Xaml.Controls;
using Eum.UI.ViewModels.Search.Sources;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UWP.Views.Search
{
    public sealed partial class SearchGroupView : UserControl
    {
        public SearchGroupView(ISearchGroup viewModel)
        {
            ViewModel = viewModel;
            this.InitializeComponent();
            this.DataContext = ViewModel;
        }
        public ISearchGroup ViewModel { get; }
    }
}
