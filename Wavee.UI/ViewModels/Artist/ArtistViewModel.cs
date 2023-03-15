using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Models.AudioItems;
using Wavee.UI.Navigation;
using Wavee.UI.ViewModels.Album;

namespace Wavee.UI.ViewModels.Artist;

public partial class
    ArtistViewModel : ObservableRecipient,
    IPlayableItem,
    IAddableItem,
    IEditableItem,
    IDescribeable
{
    public ArtistViewModel(IArtist artist)
    {
        Artist = artist;
    }
    public IArtist Artist
    {
        get;
    }

    public bool CanEdit => Artist.ServiceType == ServiceType.Local;

    public string Describe() => Artist.Name;

    public IEnumerable<string> GetPlaybackIds() => throw new NotImplementedException();
}
