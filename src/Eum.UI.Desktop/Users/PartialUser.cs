namespace Eum.UI.Users;

public record PartialUser(string Id, string ProfileName,
    string? ProfilePicture,
    ServiceType ServiceType);