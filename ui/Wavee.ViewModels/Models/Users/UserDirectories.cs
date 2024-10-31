namespace Wavee.ViewModels.Models.Users;

public class UserDirectories
{
    public const string UsersDirName = "Users";
    public const string UsersBackupDirName = "UserBackups";
    public const string UserFileExtension = "json";

    public UserDirectories(string workDir)
    {
        var correctedWorkDir = workDir;
        UsersDir = Path.Combine(correctedWorkDir, UsersDirName);
        UsersBackupDir = Path.Combine(correctedWorkDir, UsersBackupDirName);

        Directory.CreateDirectory(UsersDir);
        Directory.CreateDirectory(UsersBackupDir);
    }

    public string UsersDir { get; }
    public string UsersBackupDir { get; }

    public (string UserFilePath, string UserBackupFilePath) GetUserFilePaths(string UserName)
    {
        if (!UserName.EndsWith($".{UserFileExtension}", StringComparison.OrdinalIgnoreCase))
        {
            UserName = $"{UserName}.{UserFileExtension}";
        }
        return (Path.Combine(UsersDir, UserName), Path.Combine(UsersBackupDir, UserName));
    }

    public IEnumerable<FileInfo> EnumerateUserFiles(bool includeBackupDir = false)
    {
        var UsersDirInfo = new DirectoryInfo(UsersDir);
        var UsersDirExists = UsersDirInfo.Exists;
        var searchPattern = $"*.{UserFileExtension}";
        var searchOption = SearchOption.TopDirectoryOnly;
        IEnumerable<FileInfo> result;

        if (includeBackupDir)
        {
            var backupsDirInfo = new DirectoryInfo(UsersBackupDir);
            if (!UsersDirExists && !backupsDirInfo.Exists)
            {
                return Enumerable.Empty<FileInfo>();
            }

            result = UsersDirInfo
                .EnumerateFiles(searchPattern, searchOption)
                .Concat(backupsDirInfo.EnumerateFiles(searchPattern, searchOption));
        }
        else
        {
            if (!UsersDirExists)
            {
                return Enumerable.Empty<FileInfo>();
            }

            result = UsersDirInfo.EnumerateFiles(searchPattern, searchOption);
        }

        return result.OrderByDescending(t => t.LastAccessTimeUtc);
    }

    public string GetNextUserName(string prefix = "Random User")
    {
        int i = 1;
        var UserNames = EnumerateUserFiles().Select(x => Path.GetFileNameWithoutExtension(x.Name));
        while (true)
        {
            var UserName = i == 1 ? prefix : $"{prefix} {i}";

            if (!UserNames.Contains(UserName))
            {
                return UserName;
            }

            i++;
        }
    }
}
