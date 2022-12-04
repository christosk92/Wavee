// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Services.Users;
using UserControl = Microsoft.UI.Xaml.Controls.UserControl;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Shell
{
    public sealed partial class CurrentUserProfileCardView : UserControl
    {
        public CurrentUserProfileCardView()
        {
            UserViewModel = Ioc.Default.GetRequiredService<IEumUserViewModelManager>();
            this.InitializeComponent();
            this.DataContext = UserViewModel;
        }
        public IEumUserViewModelManager UserViewModel { get; }
    }
}
