using NAudio.SoundFont;
using Wavee.Id;

namespace Wavee.UI.User;

public sealed class UserInfo
{
    public required UserId Id { get; init; }
    public required string? DisplayName { get; init; }
    public required string? Image { get; init; }
}
public class UserViewModel : IDisposable
{
    private readonly Action _dispose;
    public UserViewModel(UserId id, string displayName, string? image, Action dispose)
    {
        _dispose = dispose;
        Settings = new UserSettings();
        Info = new UserInfo
        {
            Id = id,
            DisplayName = displayName,
            Image = image
        };
    }
    public UserId Id => Info.Id;
    public UserSettings Settings { get; }
    public UserInfo Info { get; }
    public string? ReusableCredentials { get; init; }
    public void Dispose()
    {
        _dispose();
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