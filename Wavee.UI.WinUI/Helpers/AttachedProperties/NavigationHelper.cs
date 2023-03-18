using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Wavee.UI.WinUI.Helpers.AttachedProperties;

// NavigationHelper.SetNavigateTo(navigationViewItem, typeof(MainViewModel).FullName);
// public static class NavigationHelper
// {
//     public static string GetNavigateTo(NavigationViewItem item) => (string)item.GetValue(NavigateToProperty);
//
//     public static void SetNavigateTo(NavigationViewItem item, string value) => item.SetValue(NavigateToProperty, value);
//
//     public static readonly DependencyProperty NavigateToProperty =
//         DependencyProperty.RegisterAttached("NavigateTo", typeof(string), typeof(NavigationHelper), new PropertyMetadata(null));
//
//     public static NavigationViewItem NavigateTo<T>(this NavigationViewItem item)
//     {
//         NavigationHelper.SetNavigateTo(item, typeof(T).FullName);
//         return item;
//     }
// }
