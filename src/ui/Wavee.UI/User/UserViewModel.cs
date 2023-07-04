using Wavee.Id;
using Wavee.UI.Client;

namespace Wavee.UI.User;

public sealed class UserInfo
{
    public required UserId Id { get; init; }
    public required string? DisplayName { get; init; }
    public required string? Image { get; init; }
}
public class UserViewModel : IDisposable
{
    public UserViewModel(UserId id, string displayName, string? image, IWaveeUIClient client)
    {
        Client = client;
        var persistentPath = Path.Combine(AppProviders.GetPersistentStoragePath(), "Wavee", "UserSettings", $"{id.ToString()}.json");
        Directory.CreateDirectory(Path.GetDirectoryName(persistentPath)!);
        Settings = new UserSettings(persistentPath);
        Settings.LoadFile(true);
        Info = new UserInfo
        {
            Id = id,
            DisplayName = displayName,
            Image = image
        };
    }
    public IWaveeUIClient Client { get; }
    public UserId Id => Info.Id;
    public UserSettings Settings { get; }
    public UserInfo Info { get; }
    public string? ReusableCredentials { get; init; }
    public void Dispose()
    {
        Client?.Dispose();
        Settings.Dispose();
    }
}

public readonly record struct UserId(ServiceType Source, string Id)
{
    public override string ToString()
    {
        return $"{(int)Source}.{Id}";
    }
}