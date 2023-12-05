using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using CommunityToolkit.WinUI.UI;
using Mediator;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using NeoSmart.AsyncLock;
using Wavee.UI.Features.Artist.Queries;
using Wavee.UI.Features.Artist.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Controls
{
    public sealed partial class ArtistDiscographyGroupComponent : UserControl
    {
        public static readonly DependencyProperty ItemProperty = DependencyProperty.Register(nameof(Item), typeof(ArtistViewDiscographyGroupViewModel), typeof(ArtistDiscographyGroupComponent), new PropertyMetadata(default(ArtistViewDiscographyGroupViewModel)));
        public ArtistDiscographyGroupComponent()
        {
            this.InitializeComponent();
        }

        public ArtistViewDiscographyGroupViewModel Item
        {
            get => (ArtistViewDiscographyGroupViewModel)GetValue(ItemProperty);
            set => SetValue(ItemProperty, value);
        }
    }
}
