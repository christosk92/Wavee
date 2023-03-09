using Wavee.UI.Identity.Users.Contracts;

namespace Wavee.UI.Identity.Users.Directories
{
    public class UserDirectories
    {
        public const string UsersDirName = "Users";
        private const string UserFileExtension = "json";

        public UserDirectories(ServiceType serviceType, string workDir)
        {
            ServiceType = serviceType;
            UsersDir = Path.Combine(workDir, serviceType.ToString().ToLowerInvariant());
            Directory.CreateDirectory(UsersDir);
        }

        public string UsersDir { get; }
        public ServiceType ServiceType { get; }

        public IEnumerable<FileInfo> EnumerateUserFiles()
        {
            var usersDirInfo = new DirectoryInfo(UsersDirName);
            var userDirExists = usersDirInfo.Exists;
            const string searchPattern = $"*.{UserFileExtension}";
            const SearchOption searchOption = SearchOption.TopDirectoryOnly;
            if (!userDirExists)
            {
                return Enumerable.Empty<FileInfo>();
            }
            return usersDirInfo.EnumerateFiles(searchPattern, searchOption);
        }

        public string GetUserFilePath(string username)
        {
            if (!username.EndsWith($".{UserFileExtension}", StringComparison.OrdinalIgnoreCase))
            {
                username = $"{username}.{UserFileExtension}";
            }

            return Path.Combine(UsersDir, username);
        }
    }
}
