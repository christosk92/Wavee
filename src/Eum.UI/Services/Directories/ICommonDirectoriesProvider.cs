namespace Eum.UI.Services.Directories
{
    public interface ICommonDirectoriesProvider
    {
        IUsersDirectory UsersDirectory { get; }
        string WorkDir { get; }
    }
}
