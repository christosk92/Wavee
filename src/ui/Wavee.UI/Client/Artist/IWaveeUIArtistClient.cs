namespace Wavee.UI.Client.Artist;

public interface IWaveeUIArtistClient
{
    Task<WaveeUIArtistView> GetArtist(string id, CancellationToken ct);
}

