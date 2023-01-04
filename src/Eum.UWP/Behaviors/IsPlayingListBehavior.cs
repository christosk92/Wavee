using System.Linq;
using Windows.UI.Xaml.Controls;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Items;
using Eum.UI.ViewModels;
using Eum.UI.ViewModels.Playlists;
using Microsoft.Toolkit.Uwp.UI.Behaviors;

namespace Eum.UWP.Behaviors;

public class IsPlayingListBehavior : BehaviorBase<ListViewBase>
{
    private IIsPlaying? _previousPlaying;
    protected override void OnAssociatedObjectLoaded()
    {
        base.OnAssociatedObjectLoaded();
        
        RegisterEvents();
        PlaybackViewModelOnPlayingItemChanged(null, Ioc.Default.GetRequiredService<MainViewModel>()
            .PlaybackViewModel?.Item?.Id ?? new ItemId());
    }


    protected override void OnAssociatedObjectUnloaded()
    {
        base.OnAssociatedObjectUnloaded();
        
        UnregisterEvents();
    }

    public void RegisterEvents()
    {
        Ioc.Default.GetRequiredService<MainViewModel>()
            .PlaybackViewModel.PlayingItemChanged += PlaybackViewModelOnPlayingItemChanged;
    }
    
    public void UnregisterEvents()
    {
        Ioc.Default.GetRequiredService<MainViewModel>()
            .PlaybackViewModel.PlayingItemChanged -= PlaybackViewModelOnPlayingItemChanged;
        _previousPlaying = null;
    }

    private void PlaybackViewModelOnPlayingItemChanged(object sender, ItemId e)
    {
        //Find the currently playing item in the list for listview
        if (AssociatedObject.Items == null) return;

        // var oldPlayingItem = AssociatedObject.Items
        //     .Cast<IIsPlaying>()
        //     .FirstOrDefault(a => a.IsPlaying());
        if (_previousPlaying != null && e != _previousPlaying.Id)
        {
            _previousPlaying.ChangeIsPlaying(false);
        }
        
        if (_previousPlaying == null)
        {
            //find currently playing item and set false anyways
            _previousPlaying = AssociatedObject.Items
                .Cast<IIsPlaying>()
                .FirstOrDefault(a => a.WasPlaying);
            _previousPlaying?.ChangeIsPlaying(false);
        }
        
        var item = AssociatedObject.Items
            .Cast<IIsPlaying>()
            .FirstOrDefault(x => x.Id == e);
        
        if (item == null) return;
        _previousPlaying = item;
        item.ChangeIsPlaying(true);

        // if (_wasPlaying)
        // {
        //     if (e != Track.Id)
        //     {
        //         IsPlayingChanged?.Invoke(this, false);
        //         _wasPlaying = false;
        //     }
        // }
        // else
        // {
        //     if (e == Track.Id)
        //     {
        //         IsPlayingChanged?.Invoke(this, true);
        //         _wasPlaying = true;
        //     }
        // }
    }

}
