// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using Eum.UI.Stage;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Eum.UI.WinUI.Views.Profile.Create
{
    public sealed partial class PipsStageView : UserControl
    {
        public StageManager StageManager { get; }
        public PipsStageView(StageManager stageManager)
        {
            StageManager = stageManager;
            this.InitializeComponent();
            this.DataContext = StageManager;
        }
    }
}
