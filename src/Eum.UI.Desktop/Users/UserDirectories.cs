using Eum.UI.Helpers;

namespace Eum.UI.Users;

public class UserDirectories
{
    public const string UsersDirName = "Users";
    public const string UsersBackupDirName = "UsersBackups";
    private const string UsersFileExtension = "json";

    public UserDirectories(string workDir)
    {
        var correctedWorkDir = Guard.NotNullOrEmptyOrWhitespace(nameof(workDir), workDir, true);
        UsersDir = Path.Combine(correctedWorkDir, UsersDirName);
        UsersBackupDir = Path.Combine(correctedWorkDir, UsersBackupDirName);

        Directory.CreateDirectory(UsersDir);
        Directory.CreateDirectory(UsersBackupDir);
    }
    
    public string UsersDir { get; }
    public string UsersBackupDir { get; }

    
    public (string userFilePath, string userBckupFilePath) GetUserFilePaths(string userId)
    {
        if (!userId.EndsWith($".{UsersFileExtension}", StringComparison.OrdinalIgnoreCase))
        {
            userId = $"{userId}.{UsersFileExtension}";
        }
        return (Path.Combine(UsersDir, userId), Path.Combine(UsersBackupDirName, userId));
    }
    public IEnumerable<FileInfo> EnumearteUsersFiles(bool includeBackupDir = false)
    {
        var usersDirInfo = new DirectoryInfo(UsersDir);
        var usersDirExists = usersDirInfo.Exists;
        const string searchPattern = $"*.{UsersFileExtension}";
        const SearchOption searchOption = SearchOption.TopDirectoryOnly;
        IEnumerable<FileInfo> result;

        if (includeBackupDir)
        {
            var backupsDirInfo = new DirectoryInfo(UsersBackupDirName);
            if (!usersDirExists && !backupsDirInfo.Exists)
            {
                return Enumerable.Empty<FileInfo>();
            }

            result = usersDirInfo
                .EnumerateFiles(searchPattern, searchOption)
                .Concat(backupsDirInfo.EnumerateFiles(searchPattern, searchOption));
        }
        else
        {
            if (!usersDirExists)
            {
                return Enumerable.Empty<FileInfo>();
            }

            result = usersDirInfo.EnumerateFiles(searchPattern, searchOption);
        }

        return result
            .Where(a=> !a.FullName.EndsWith("_info.json")).OrderByDescending(t => t.LastAccessTimeUtc);
    }

    public string GetNextUserId(string prefix = "Random Wallet")
    {
        int i = 1;
        var userIds = EnumearteUsersFiles().Select(x => Path.GetFileNameWithoutExtension(x.Name));
        while (true)
        {
            var userId = i == 1 ? prefix : $"{prefix} {i}";

            if (!userIds.Contains(userId))
            {
                return userId;
            }

            i++;
        }
    }
}