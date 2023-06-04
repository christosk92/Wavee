using Wavee.Spotify;
using Wavee.UI.Models.Common;
using Wavee.UI.Settings;

namespace Wavee.UI;

public sealed class State : IDisposable
{
    public readonly SpotifyClient Client;

    public State(SpotifyClient client,
        SpotifyConfig spotifyConfig,
        SpotifyUser user)
    {
        Instance?.Dispose();
        Instance = null;

        Config = spotifyConfig;
        User = user;
        Client = client;
        Instance = this;

        var configPath = PlatformSpecificServices.GetPersistentStoragePath();
        var settingsPath = Path.Combine(configPath, user.Id, "settings.json");
        Directory.CreateDirectory(Path.GetDirectoryName(settingsPath)!);
        Settings = new UserSettings(settingsPath);

        Config.Playback.CrossfadeDuration = Settings.CrossfadeDuration;
        Config.Playback.PreferedQuality = Settings.PreferedQuality;

        Config.Remote.DeviceName = Settings.DeviceName;
        Config.Remote.DeviceType = Settings.DeviceType;
        Task.Run(async() => await Client.Remote.RefreshState());
    }

    public SpotifyConfig Config { get; }
    public static State Instance { get; private set; } = null!;
    public SpotifyUser User { get; }

    public UserSettings Settings { get; }

    public void Dispose()
    {
        Client?.Dispose();
    }
}