using LanguageExt;
using Wavee.Spotify;

namespace Wavee.UI.Core;

public static class Global
{
    public static bool IsMock { get; set; } = true;
    public static IAppState AppState { get; set; } = null!;
    public static Func<Option<string>>? RetrieveDefaultUsername { get; set; }
    public static RetrievePasswordFromVaultForUser? RetrievePasswordFromVaultForUserFunc { get; set; }
    public static SavePasswordToVaultForUser? SavePasswordToVaultForUserAction { get; set; }
    public static Func<string>? GetPersistentStoragePath { get; set; }
    public static SpotifyConfig SpotifyConfig { get; set; }
}
public delegate Option<string> RetrievePasswordFromVaultForUser(string username);
public delegate void SavePasswordToVaultForUser(string username, string password);