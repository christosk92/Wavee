using Eum.UI.Items;

namespace Eum.UI.Services.Directories
{
    public interface IUsersDirectory
    {
        (string Path, string BackupPath) UsersDirName(ServiceType service);
        Dictionary<ServiceType, IEnumerable<FileInfo>> EnumerateUserFiles();
        (string userFullPath, string userBackupFullPath) GetUserFilePaths(ServiceType service, string id);


        (string Path, string BackupPath) PlaylistsDirName(ServiceType service);
        Dictionary<ServiceType, IEnumerable<FileInfo>> EnumeratePlaylistFiles();
        (string playlistFullPath, string playlistBackupFullPath) GetPlaylistFilePaths(ServiceType service, string id);

    }
}
