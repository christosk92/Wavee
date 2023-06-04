using LanguageExt;

namespace Wavee.UI;

public static class PlatformSpecificServices
{
    public static Func<Option<string>>? RetrieveDefaultUsername { get; set; }
    public static RetrievePasswordFromVaultForUser? RetrievePasswordFromVaultForUserFunc { get; set; }
    public static SavePasswordToVaultForUser? SavePasswordToVaultForUserAction { get; set; }

    public static Func<string>? GetPersistentStoragePath { get; set; }
}
public delegate Option<string> RetrievePasswordFromVaultForUser(string username);
public delegate void SavePasswordToVaultForUser(string username, string password);