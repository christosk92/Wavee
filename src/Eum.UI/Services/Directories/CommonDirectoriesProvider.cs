using Eum.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eum.UI.Items;
using Eum.Users;

namespace Eum.UI.Services.Directories;
public sealed class CommonDirectoriesProvider : ICommonDirectoriesProvider
{
    public CommonDirectoriesProvider(string workDir)
    {
        WorkDir = workDir;
        UsersDirectory = new UsersDirectory(WorkDir);
    }
    public IUsersDirectory UsersDirectory { get; }
    public string WorkDir { get; }
}

internal class AnyDirectory
{
    public readonly string CoreDirname;
    public readonly string CoreBackupDirName;
    private readonly string _fileExtension;
    private readonly string _workDir;

    public AnyDirectory(string coreDirname, string coreBackupDirName, string workDir, string fileExtension)
    {
        CoreDirname = coreDirname;
        CoreBackupDirName = coreBackupDirName;
        _workDir = workDir;
        _fileExtension = fileExtension;
    }

    public (string Path, string BackupPath) DirName(ServiceType service)
    {

        var dir = Path.Combine(_workDir, CoreDirname, service.ToString());
        var usersBackupDir = Path.Combine(_workDir, CoreBackupDirName,
            service.ToString());

        Directory.CreateDirectory(dir);
        Directory.CreateDirectory(usersBackupDir);

        return (dir, usersBackupDir);
    }


    public (string fullPath, string backupFullPath) GetFilePaths(ServiceType service, string userId)
    {
        if (!userId.EndsWith($".{_fileExtension}", StringComparison.OrdinalIgnoreCase))
        {
            userId = $"{userId}.{_fileExtension}";
        }

        var (main, backup) = DirName(service);
        return (Path.Combine(main, userId), Path.Combine(backup, userId));
    }

    //UsersDirName
    public Dictionary<ServiceType, IEnumerable<FileInfo>> EnumerateFiles()
    {
        IEnumerable<FileInfo> GetFiles(ServiceType service)
        {
            var (main, backup) = DirName(service);
            var usersDirInfo = new DirectoryInfo(main);
            var usersDirExists = usersDirInfo.Exists;
            string searchPattern = $"*.{_fileExtension}";
            const SearchOption searchOption = SearchOption.TopDirectoryOnly;
            IEnumerable<FileInfo> result;

            var backupsDirInfo = new DirectoryInfo(backup);
            if (!usersDirExists && !backupsDirInfo.Exists)
            {
                return Enumerable.Empty<FileInfo>();
            }

            result = usersDirInfo
                .EnumerateFiles(searchPattern, searchOption)
                .Concat(backupsDirInfo.EnumerateFiles(searchPattern, searchOption));

            return result.OrderByDescending(t => t.LastAccessTimeUtc);
        }

        var dictionary = new Dictionary<ServiceType, IEnumerable<FileInfo>>();
        foreach (ServiceType service in Enum.GetValues(typeof(ServiceType)))
        {
            dictionary[service] = GetFiles(service);
        }
        return dictionary;
    }
}
public class UsersDirectory : IUsersDirectory
{
    private readonly AnyDirectory _playlists;
    private readonly AnyDirectory _users;
    public UsersDirectory(string workDir)
    {
        _users = new AnyDirectory("Users", "UsersBackup", workDir, "json");
        _playlists = new AnyDirectory("Playlists", "PlaylistsBackup", workDir, "json");
    }



    public (string Path, string BackupPath) UsersDirName(ServiceType service) => _users.DirName(service);

    public Dictionary<ServiceType, IEnumerable<FileInfo>> EnumerateUserFiles() => _users.EnumerateFiles();

    public (string userFullPath, string userBackupFullPath) GetUserFilePaths(ServiceType service, string id) =>
        _users.GetFilePaths(service, id);

    public (string Path, string BackupPath) PlaylistsDirName(ServiceType service)

        => _playlists.DirName(service);

    public Dictionary<ServiceType, IEnumerable<FileInfo>> EnumeratePlaylistFiles()
        => _playlists.EnumerateFiles();

    public (string playlistFullPath, string playlistBackupFullPath) GetPlaylistFilePaths(ServiceType service, string id)
    => _playlists.GetFilePaths(service, id);
}