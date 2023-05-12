namespace Wavee.UI.Users;

public sealed class User
{
    public string DispayName { get; }
    public UserProductType ProductType { get; }
    public string Id { get; }
    public bool IsLoggedIn { get; }
}

public enum UserProductType
{
    Guest,
    Local,
    SpotifyPremium
}