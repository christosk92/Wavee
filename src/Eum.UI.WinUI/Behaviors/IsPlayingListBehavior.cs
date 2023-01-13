using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.WinUI.UI;
using CommunityToolkit.WinUI.UI.Behaviors;
using Eum.UI.Items;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Playlists;
using Microsoft.UI.Composition;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Eum.UI.WinUI.Behaviors;

public class IsPlayingListBehavior : BehaviorBase<ListViewBase>
{
    public List<Control> PreviousPlayingContainers = new List<Control>();


    protected override void OnAttached()
    {
        base.OnAttached();
        Ioc.Default.GetRequiredService<MainViewModel>()
            .PlaybackViewModel!.PlayingItemChanged += PlaybackViewModelOnPlayingItemChanged;
        // AssociatedObject.ChoosingItemContainer += AssociatedObjectOnChoosingItemContainer;
        // AssociatedObject.ContainerContentChanging += AssociatedObjectOnContainerContentChanging;
    }

    // private void HandleCointainerEvent(object argItem, object? container = null)
    // {
    //     if (container == null) return;
    //     if (argItem is IIsPlaying isplaying)
    //     {
    //         //   VisualStateManager.GoToState(userCtrl, "Playing", true);
    //         if (Ioc.Default.GetRequiredService<MainViewModel>()
    //                 .PlaybackViewModel!.Id == isplaying.Id)
    //         {
    //             if ((container is ListViewItem lvItem))
    //             {
    //                 if (lvItem.ContentTemplateRoot is UserControl ct)
    //                 {
    //                     VisualStateManager.GoToState(ct, "Playing", true);
    //                     PreviousPlayingContainers.Add(ct);
    //                 }
    //             }
    //         }
    //     }
    // }
    // private void AssociatedObjectOnChoosingItemContainer(ListViewBase sender, ChoosingItemContainerEventArgs args)
    // {
    //     HandleCointainerEvent(args.Item, args.ItemContainer);
    //     // if (args.Item is IIsPlaying isplaying)
    //     // {
    //     //     //   VisualStateManager.GoToState(userCtrl, "Playing", true);
    //     //     if (Ioc.Default.GetRequiredService<MainViewModel>()
    //     //             .PlaybackViewModel!.Id == isplaying.Id)
    //     //     {
    //     //         var item = sender.ContainerFromIndex(args.ItemIndex);
    //     //         if ((item is ListViewItem lvItem))
    //     //         {
    //     //             if (lvItem.ContentTemplateRoot is UserControl ct)
    //     //             {
    //     //                 VisualStateManager.GoToState(ct, "Playing", true);
    //     //                 _previousPlayingContainers.Add(ct);
    //     //             }
    //     //         }
    //     //     }
    //     // }
    // }
    //
    // private void AssociatedObjectOnContainerContentChanging(ListViewBase sender, ContainerContentChangingEventArgs args)
    // {
    //     HandleCointainerEvent(args.Item, args.ItemContainer);
    //     // if (args.Item is IIsPlaying isplaying)
    //     // {
    //     //     //   VisualStateManager.GoToState(userCtrl, "Playing", true);
    //     //     if (Ioc.Default.GetRequiredService<MainViewModel>()
    //     //             .PlaybackViewModel!.Id == isplaying.Id)
    //     //     {
    //     //         var item = sender.ContainerFromIndex(args.ItemIndex);
    //     //         if ((item is ListViewItem lvItem))
    //     //         {
    //     //             if (lvItem.ContentTemplateRoot is UserControl ct)
    //     //             {
    //     //                 VisualStateManager.GoToState(ct, "Playing", true);
    //     //                 _previousPlayingContainers.Add(ct);
    //     //             }
    //     //         }
    //     //     }
    //     // }
    // }

    protected override void OnDetaching()
    {
        base.OnDetaching();
        Ioc.Default.GetRequiredService<MainViewModel>()
            .PlaybackViewModel!.PlayingItemChanged -= PlaybackViewModelOnPlayingItemChanged;
        // AssociatedObject.ChoosingItemContainer -= AssociatedObjectOnChoosingItemContainer;
        // AssociatedObject.ContainerContentChanging -= AssociatedObjectOnContainerContentChanging;
    }

    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();
        PlaybackViewModelOnPlayingItemChanged(this, Ioc.Default.GetRequiredService<MainViewModel>()
            .PlaybackViewModel!.Id);
    }

    protected override void OnAssociatedObjectUnloaded()
    {
        base.OnAssociatedObjectUnloaded();
        PreviousPlayingContainers.Clear();
    }

    private void PlaybackViewModelOnPlayingItemChanged(object sender, ItemId e)
    {
        AssociatedObject.DispatcherQueue.TryEnqueue(DispatcherQueuePriority.High, () =>
        {
            //if the item is different, try and find the item in the list
            //if we cant find the id in the list, then 
            //find item in list
            var interesetingItems =
                AssociatedObject.Items
                .Cast<IIsPlaying>()
                .Select((a, i) => (a, i))
                .Where(a => a.a.Id == e)
                .ToArray();
            var ctrls = new Control[interesetingItems.Length];
            int i = 0;

            foreach (var previousPlayingContainer in PreviousPlayingContainers)
            {
                VisualStateManager.GoToState(previousPlayingContainer, "Normal", true);
            }

            foreach (var interesetingItem in interesetingItems)
            {
                var itemContainer = AssociatedObject.ContainerFromIndex(interesetingItem.i);
                if (itemContainer is ListViewItem lv)
                {
                    if (lv.ContentTemplateRoot is UserControl userCtrl)
                    {
                        ctrls[i] = userCtrl;
                        i++;

                        //set is playing
                        VisualStateManager.GoToState(userCtrl, "Playing", true);
                    }
                }
            }

            PreviousPlayingContainers = ctrls.Where(a => a != null).ToList();
        });
    }

    // public void RegisterEvents()
    // {
    //     Ioc.Default.GetRequiredService<MainViewModel>()
    //         .PlaybackViewModel.PlayingItemChanged += PlaybackViewModelOnPlayingItemChanged;
    // }
    //
    // public void UnregisterEvents()
    // {
    //     Ioc.Default.GetRequiredService<MainViewModel>()
    //         .PlaybackViewModel.PlayingItemChanged -= PlaybackViewModelOnPlayingItemChanged;
    //     _previousPlaying = null;
    // }
    //
    // private void PlaybackViewModelOnPlayingItemChanged(object sender, ItemId e)
    // {
    //     //Find the currently playing item in the list for listview
    //     if (AssociatedObject.Items == null) return;
    //
    //     // var oldPlayingItem = AssociatedObject.Items
    //     //     .Cast<IIsPlaying>()
    //     //     .FirstOrDefault(a => a.IsPlaying());
    //     if (_previousPlaying != null && e != _previousPlaying.Id)
    //     {
    //         _previousPlaying.ChangeIsPlaying(false);
    //     }
    //     
    //     if (_previousPlaying == null)
    //     {
    //         //find currently playing item and set false anyways
    //         _previousPlaying = AssociatedObject.Items
    //             .Cast<IIsPlaying>()
    //             .FirstOrDefault(a => a.WasPlaying);
    //         _previousPlaying?.ChangeIsPlaying(false);
    //     }
    //     
    //     var item = AssociatedObject.Items
    //         .Cast<IIsPlaying>()
    //         .FirstOrDefault(x => x.Id == e);
    //     
    //     if (item == null) return;
    //     _previousPlaying = item;
    //     item.ChangeIsPlaying(true);
    //
    //     // if (_wasPlaying)
    //     // {
    //     //     if (e != Track.Id)
    //     //     {
    //     //         IsPlayingChanged?.Invoke(this, false);
    //     //         _wasPlaying = false;
    //     //     }
    //     // }
    //     // else
    //     // {
    //     //     if (e == Track.Id)
    //     //     {
    //     //         IsPlayingChanged?.Invoke(this, true);
    //     //         _wasPlaying = true;
    //     //     }
    //     // }
    // }


}