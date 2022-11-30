namespace Eum.UI.Users;

public interface IEumUser
{
    string UserId { get; }
    string UserName { get; }
    string? ProfilePicture { get; set; }
}