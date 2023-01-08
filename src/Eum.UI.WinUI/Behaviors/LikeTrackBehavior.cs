using CommunityToolkit.WinUI.UI.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Services.Library;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Artists;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;

namespace Eum.UI.WinUI.Behaviors
{
    public class LikeTrackBehavior : BehaviorBase<ToggleButton>
    {
        protected override void OnAssociatedObjectLoaded()
        {
            base.OnAssociatedObjectLoaded();
            AssociatedObject.Tapped += AssociatedObjectOnTapped;
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();
            AssociatedObject.Tapped -= AssociatedObjectOnTapped;
        }


        private void AssociatedObjectOnTapped(object sender, TappedRoutedEventArgs e)
        {
            if (AssociatedObject.DataContext is IIsSaved s)
            {
                if (AssociatedObject.IsChecked ?? false)
                {
                    Ioc.Default.GetRequiredService<MainViewModel>()
                        .CurrentUser.User.LibraryProvider.SaveItem(s.Id);
                }
                else
                {
                    Ioc.Default.GetRequiredService<MainViewModel>()
                        .CurrentUser.User.LibraryProvider.UnsaveItem(s.Id);
                }
            }
        }
    }
}
