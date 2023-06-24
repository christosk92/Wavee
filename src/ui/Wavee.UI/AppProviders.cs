using LanguageExt;

namespace Wavee.UI.WinUI.Providers;
public static class AppProviders
{
    public static Func<string> GetPersistentStoragePath { get; set; }
    public static Func<string, Option<string>> GetCredentialsFor { get; set; }
}
