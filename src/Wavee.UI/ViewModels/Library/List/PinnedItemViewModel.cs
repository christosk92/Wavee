using System;
using Microsoft.UI.Xaml.Controls;
using ReactiveUI;
using Wavee.Contracts.Enums;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.ViewModels.Library.List;

public sealed class PinnedItemViewModel : ReactiveObject
{
    public PinnedItemViewModel(IPinnableItem item)
    {
        Item = item;

        Icon = item.Type switch
        {
            ItemType.Album => Wavee.UI.Icons.SegoeFluent("\uE93C"),
            ItemType.Artist => Wavee.UI.Icons.SegoeFluent("\uEBDA"),
            ItemType.Playlist => Icons.MediaPlayer("\uE93F"),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public IPinnableItem Item { get; }
    public IconElement Icon { get; }
}