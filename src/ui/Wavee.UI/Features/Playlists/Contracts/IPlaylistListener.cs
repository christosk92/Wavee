namespace Wavee.UI.Features.Playlists.Contracts;

public interface IPlaylistListener
{
    void OnPlaylistChanged_Dumb();
    string Id { get; }
}