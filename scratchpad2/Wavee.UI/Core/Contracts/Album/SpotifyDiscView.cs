using Wavee.UI.Core.Contracts.Artist;

namespace Wavee.UI.Core.Contracts.Album;

public class SpotifyDiscView
{
    public ushort Number { get; set; }
    public ArtistDiscographyTrack[] Tracks { get; set; }
    public bool HasMultipleDiscs { get; set; }

    public string FormatDiscName(ushort numb)
    {
        return $"Disc {Number}";
    }
}