﻿using CommunityToolkit.Mvvm.Input;
using Wavee.UI.ViewModels.Track;

namespace Wavee.UI.ViewModels.Libray
{
    public class LibraryArtistsViewModel : AbsLibraryViewModel<object>
    {
        public override Task Initialize()
        {
            return Task.CompletedTask;
        }

        public override AsyncRelayCommand<TrackViewModel> PlayCommand
        {
            get;
        }
    }
}
