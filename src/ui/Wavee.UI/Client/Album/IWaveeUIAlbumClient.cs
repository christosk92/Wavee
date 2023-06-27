namespace Wavee.UI.Client.Album;

public interface IWaveeUIAlbumClient
{
    Task<WaveeUIAlbumView> GetAlbum(string id, CancellationToken cancellationToken);
}