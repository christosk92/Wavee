using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using Wavee.Contracts.Common;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.Contracts.Models;

namespace Wavee.UI.Spotify.Responses;

internal sealed class SpotifyHomeItem : IHomeItem, INotifyPropertyChanged
{
    private string _color;

    public SpotifyHomeItem(IItem item,
        string color,
        string descriptionText)
    {
        _color = color;
        Item = item;
        DescriptionText = descriptionText;
        MediumImageUrl = item.Images
                .OrderByDescending(x => x.Width ?? 0)
                .Skip(1)
                .FirstOrDefault()
                .Url ?? item.Images.FirstOrDefault().Url
            ?? SpotifyConstants.AlbumPlaceholderImageId;
    }

    public IItem Item { get; }
    public ComposedKey Key { get; set; }
    public string MediumImageUrl { get; }
    public string DescriptionText { get; }
    public HomeGroup Group { get; set; }
    public int Order { get; set; }

    public string Color
    {
        get => _color;
        set => SetField(ref _color, value);
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
}