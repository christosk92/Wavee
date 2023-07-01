using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using Wavee.UI.User;
using Wavee.UI.ViewModel.Shell;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.View.Shell
{
    public sealed partial class RightSidebarControl : UserControl
    {
        public static readonly DependencyProperty ViewProperty = DependencyProperty.Register(nameof(View), typeof(RightSidebarView), typeof(RightSidebarControl), new PropertyMetadata(default(RightSidebarView)));
        public static readonly DependencyProperty ViewModelProperty = DependencyProperty.Register(nameof(ViewModel), typeof(RightSidebarViewModel), typeof(RightSidebarControl), new PropertyMetadata(default(RightSidebarViewModel), ViewModelChanged));

        private static void ViewModelChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var x = (RightSidebarControl)d;
            x.UpdateView(e);
        }

        public RightSidebarControl()
        {
            this.InitializeComponent();
        }

        public RightSidebarView View
        {
            get => (RightSidebarView)GetValue(ViewProperty);
            set => SetValue(ViewProperty, value);
        }

        public RightSidebarViewModel ViewModel
        {
            get => (RightSidebarViewModel)GetValue(ViewModelProperty);
            set => SetValue(ViewModelProperty, value);
        }

        private void UpdateView(DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is RightSidebarViewModel r)
            {
                r.PropertyChanged -= ViewModel_PropertyChanged;
            }

            if (e.NewValue is RightSidebarViewModel r2)
            {
                r2.PropertyChanged += ViewModel_PropertyChanged;
            }
        }

        private void ViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(RightSidebarViewModel.View))
            {
                switch (ViewModel.View)
                {
                    case RightSidebarView.Lyrics:
                        MainPivot.SelectedIndex = 0;
                        LyricsCtrl.ViewModel.Create();
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            if (e.PropertyName == nameof(RightSidebarViewModel.Show))
            {
                if (!ViewModel.Show)
                {
                    LyricsCtrl.ViewModel.Destroy();
                }
                else if (ViewModel.View == RightSidebarView.Lyrics)
                {
                    LyricsCtrl.ViewModel.Create();
                }
            }
        }
    }
}
