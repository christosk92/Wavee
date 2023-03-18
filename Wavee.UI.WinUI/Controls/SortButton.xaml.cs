using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Xml.Linq;
using Wavee.UI.Interfaces.ViewModels;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml.Markup;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Wavee.UI.WinUI.Controls
{
    [ContentProperty(Name = "MainContent")]
    public sealed partial class SortButton : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty SortContextProperty = DependencyProperty.Register(nameof(SortContext),
            typeof(ISortContext),
            typeof(SortButton), new PropertyMetadata(default(ISortContext), PropertyChangedCallback));


        public static readonly DependencyProperty SortByProperty = DependencyProperty.Register(nameof(SortBy), typeof(string),
            typeof(SortButton), new PropertyMetadata(default(string), PropertyChangedCallback));

        public static DependencyProperty MainContentProperty =
            DependencyProperty.Register("MainContent", typeof(object), typeof(SortButton), null);

        private static void PropertyChangedCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var s = (SortButton)d;
            if (e.OldValue is ISortContext sctxOld)
            {
                s.UnregisterEvents(sctxOld);
            }
            if (e.NewValue is ISortContext sctx)
            {
                s.RegisterEvents(sctx);
            }
            s.UpdateSort();
        }

        private void UnregisterEvents(ISortContext sctxOld)
        {
            sctxOld.SortChanged -= SctxOldOnSortChanged;
        }


        private void RegisterEvents(ISortContext sctx)
        {
            sctx.SortChanged
                 += SctxOldOnSortChanged;
        }

        private bool _isSorting;
        public static readonly DependencyProperty IsSpecialSortProperty = DependencyProperty.Register(nameof(IsSpecialSort), typeof(bool), typeof(SortButton), new PropertyMetadata(default(bool)));

        public SortButton()
        {
            this.InitializeComponent();
        }
        public ISortContext SortContext
        {
            get => (ISortContext)GetValue(SortContextProperty);
            set => SetValue(SortContextProperty, value);
        }

        public string? SortBy
        {
            get => (string?)GetValue(SortByProperty);
            set => SetValue(SortByProperty, value);
        }
        public object MainContent
        {
            get => GetValue(MainContentProperty);
            set => SetValue(MainContentProperty, value);
        }

        public bool IsSorting
        {
            get => _isSorting;
            set => SetField(ref _isSorting, value);
        }

        public bool IsSpecialSort
        {
            get => (bool)GetValue(IsSpecialSortProperty);
            set => SetValue(IsSpecialSortProperty, value);
        }

        private void UpdateSort()
        {
            if (SortContext?.SortBy == SortBy)
            {
                IsSorting = true;
                //check ascending/descending and flip chevron based on that.
                VisualStateManager.GoToState(this,
                    SortContext.SortAscending ? "SortingAscending" : "SortingDescending", true);
            }
            else
            {
                IsSorting = false;
                VisualStateManager.GoToState(this,
                    "Nothing", true);
            }
        }
        private void SctxOldOnSortChanged(object sender, (string SortBy, bool SortAscending) e)
        {
            UpdateSort();
        }
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        private void Btn_OnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (SortContext != null)
            {
                //three states:
                // descending -> ascending
                // ascending -> default 
                // default -> descending
                if (SortBy == null)
                {
                    //flip sort direction
                    SortContext.SortAscending = !SortContext.SortAscending;
                }
                else
                {
                    if (SortContext.SortBy == SortBy)
                    {
                        switch (SortContext.SortAscending)
                        {
                            case true:
                                if (!IsSpecialSort)
                                {
                                    SortContext.DefaultSort();
                                }
                                else
                                {
                                    SortContext.SortAscending = false;
                                }
                                break;
                            case false:
                                SortContext.SortAscending = true;
                                break;
                        }
                    }
                    else
                    {
                        SortContext.SortBy = SortBy;
                        SortContext.SortAscending = false;
                    }
                }
            }
        }
    }
}
