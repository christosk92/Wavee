using System.Text.Json;
using Eum.Logging;
using Eum.UI.Items;
using Eum.UI.JsonConverters;
using Eum.UI.Playlists;
using Eum.UI.Services.Directories;
using Eum.UI.Users;
using Medialoc.Shared.Helpers;
using Nito.AsyncEx;

namespace Eum.UI.Services.Playlists
{
    public class EumPlaylistManager : IEumPlaylistManager
    {
        private readonly IUsersDirectory _usersDirectory;

        private object Lock = new object();
        public EumPlaylistManager(ICommonDirectoriesProvider dirProvider)
        {
            var dictionary = new Dictionary<ServiceType, string>();
            foreach (ServiceType i in Enum.GetValues(typeof(ServiceType)))
            {
                var path = Path.Combine(dirProvider.WorkDir,
                    dirProvider.UsersDirectory.PlaylistsDirName(i).Path);
                Directory.CreateDirectory(path);

                dictionary[i] = path;
            }

            WorkDirs = dictionary;
            _usersDirectory = dirProvider.UsersDirectory;

            AsyncContext.Run(RefreshUserList);
        }
        public async ValueTask<IEnumerable<EumPlaylist>> GetPlaylists(ItemId user, bool refreshList)
        {
            if (refreshList)
            {
                await RefreshUserList();
            }

            lock (Lock)
            {
                return Playlists.ToList();
            }
        }

        public async ValueTask<EumPlaylist> AddPlaylist(string name,
            string id,
            string? picture,
            ServiceType serviceType,
            ItemId forUser,
            Dictionary<ServiceType, ItemId> linkedWith)
        {
            var newUser = await AddPlaylist(serviceType, id);
            newUser.Name = name;
            newUser.ImagePath = picture;
            newUser.Tracks= Array.Empty<ItemId>();
            newUser.LinkedTo = linkedWith;
            newUser.User = forUser;


            return newUser;
        }
        public IReadOnlyDictionary<ServiceType, string> WorkDirs { get; }

        /// <summary>
        /// Triggered if a user added to the Users collection. The sender of the event will be the UserManager and the argument is the added User.
        /// </summary>
        public event EventHandler<EumPlaylist>? PlaylistUpdated;
        /// <summary>
        /// Triggered if a user added to the Users collection. The sender of the event will be the UserManager and the argument is the added User.
        /// </summary>
        public event EventHandler<EumPlaylist>? PlaylistAdded;
        /// <summary>
        /// Triggered if a user removed from the Users collection. The sender of the event will be the UserManager and the argument is the removed User.
        /// </summary>
        public event EventHandler<EumPlaylist>? PlaylistRemoved;

        public void RemovePlaylist(EumPlaylist playlists)
        {
            lock (Lock)
            {
                Playlists.Remove(playlists);
            }

            var safeIO = new SafeIoManager(playlists.FilePath);
            safeIO.DeleteMe();

            PlaylistRemoved?.Invoke(this, playlists);
        }


        /// <remarks>All access must be guarded by <see cref="Lock"/> object.</remarks>
        private HashSet<EumPlaylist> Playlists { get; } = new();


        private async Task RefreshUserList()
        {
            foreach (var service in _usersDirectory.EnumeratePlaylistFiles())
            {
                foreach (var fileInfo in service.Value)
                {
                    try
                    {
                        var userId = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                        lock (Lock)
                        {
                            if (Playlists.Any(w => w.Id.Id == userId && w.Id.Service == service.Key))
                            {
                                continue;
                            }
                        }

                        await AddPlaylist(service.Key, userId);
                    }
                    catch (Exception ex)
                    {
                        S_Log.Instance.LogWarning(ex);
                    }
                }
            }
        }

        private async ValueTask<EumPlaylist> AddPlaylist(ServiceType serviceType, string playlistId)
        {
            (string userFullPath, string userBackupFullPath) = _usersDirectory.GetPlaylistFilePaths(serviceType, playlistId);
            EumPlaylist playlist;
            try
            {
                if (File.Exists(userFullPath))
                {
                    await using (var fs = File.Open(userFullPath, FileMode.OpenOrCreate, FileAccess.Read, FileShare.ReadWrite))
                    {
                        playlist = await JsonSerializer.DeserializeAsync<EumPlaylist>(fs,
                            SystemTextJsonSerializationOptions.Default);
                    }
                }
                else
                {
                    playlist = new EumPlaylist
                    {
                        FilePath = userFullPath,
                        Id = new ItemId($"{serviceType.ToString().ToLower()}:playlist:{playlistId}"),
                        Order = Playlists.Max(a => a.Order) + 1
                    };
                }
                // user = new EumUser(new ItemId($"{serviceType.ToString().ToLower()}:user:{userId}"), userFullPath);
            }
            catch (Exception ex)
            {
                if (!File.Exists(userBackupFullPath))
                {
                    throw;
                }

                S_Log.Instance.LogWarning($"User got corrupted.\n" +
                                          $"User Filepath: {userFullPath}\n" +
                                          $"Trying to recover it from backup.\n" +
                                          $"Backup path: {userBackupFullPath}\n" +
                                          $"Exception: {ex}");
                if (File.Exists(userFullPath))
                {
                    string corruptedWalletBackupPath = $"{userBackupFullPath}_CorruptedBackup";
                    if (File.Exists(corruptedWalletBackupPath))
                    {
                        File.Delete(corruptedWalletBackupPath);
                        S_Log.Instance.LogInfo(
                            $"Deleted previous corrupted User file backup from `{corruptedWalletBackupPath}`.");
                    }

                    File.Move(userFullPath, corruptedWalletBackupPath);
                    S_Log.Instance.LogInfo($"Backed up corrupted User file to `{corruptedWalletBackupPath}`.");
                }

                File.Copy(userBackupFullPath, userFullPath);

                playlist = new EumPlaylist
                {
                    FilePath = userFullPath,
                    Id = new ItemId($"{serviceType.ToString().ToLower()}:playlist:{playlistId}"),
                    Order = Playlists.Max(a => a.Order) + 1
                };
            }

            AddPlaylist(playlist);

            return playlist;
        }

        public void AddPlaylist(EumPlaylist user)
        {
            bool didUpdate = false;
            lock (Lock)
            {
                if (Playlists.Any(w => w.Id == user.Id))
                {
                    //update
                    Playlists.Remove(user);
                    Playlists.Add(user);
                    didUpdate = true;
                }
                else
                {
                    Playlists.Add(user);
                }
            }

            if (string.IsNullOrEmpty(user.FilePath))
            {
                (string userFullPath, string userBackupFullPath) =
                    _usersDirectory.GetPlaylistFilePaths(user.Id.Service, user.Id.Id);
                user.FilePath = userFullPath;
            }

            user.ToFile();

            //user.IsDefaultChanged += UserOnIsDefaultChanged;
            // wallet.StateChanged += Wallet_StateChanged;

            if (!didUpdate)
                PlaylistAdded?.Invoke(this, user);
            else PlaylistUpdated?.Invoke(this, user);
        }
    }
}
