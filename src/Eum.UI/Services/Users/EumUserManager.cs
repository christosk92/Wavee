using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.Connections.Spotify;
using Eum.Helpers;
using Eum.Logging;
using Eum.UI.Items;
using Eum.UI.JsonConverters;
using Eum.UI.Playlists;
using Eum.UI.Services.Directories;
using Eum.UI.Services.Playlists;
using Eum.UI.Users;
using Eum.Users;
using Nito.AsyncEx;

namespace Eum.UI.Services.Users
{
    public class EumUserManager : IEumUserManager
    {
        private readonly IUsersDirectory _usersDirectory;

        private object Lock = new object();
        public EumUserManager(ICommonDirectoriesProvider dirProvider)
        {
            var dictionary = new Dictionary<ServiceType, string>();
            foreach (ServiceType i in Enum.GetValues(typeof(ServiceType)))
            {
                var path = Path.Combine(dirProvider.WorkDir,
                    dirProvider.UsersDirectory.UsersDirName(i).Path);
                Directory.CreateDirectory(path);

                dictionary[i] = path;
            }

            WorkDirs = dictionary;
            _usersDirectory = dirProvider.UsersDirectory;

            AsyncContext.Run(RefreshUserList);
        }
        public async ValueTask<IEnumerable<EumUser>> GetUsers(bool refreshList)
        {
            if (refreshList)
            {
                await RefreshUserList();
            }

            lock (Lock)
            {
                return Users.ToList();
            }
        }

        public async ValueTask<EumUser> AddUser(string profileName, string id, string? profilePicture,
            ServiceType serviceType,
            Dictionary<string, object> metadata)
        {
            var newUser = await AddUser(serviceType, id);
            newUser.ProfileName = profileName;
            newUser.ProfilePicture = profilePicture;
            newUser.Metadata = metadata;


            return newUser;
        }
        public IReadOnlyDictionary<ServiceType, string> WorkDirs { get; }

        /// <summary>
        /// Triggered if a user added to the Users collection. The sender of the event will be the UserManager and the argument is the added User.
        /// </summary>
        public event EventHandler<EumUser>? UserUpdated;
        /// <summary>
        /// Triggered if a user added to the Users collection. The sender of the event will be the UserManager and the argument is the added User.
        /// </summary>
        public event EventHandler<EumUser>? UserAdded;
        /// <summary>
        /// Triggered if a user removed from the Users collection. The sender of the event will be the UserManager and the argument is the removed User.
        /// </summary>
        public event EventHandler<EumUser>? UserRemoved;

        public EumUser GetUser(ItemId user)
        {
            lock (Lock)
            {
                if (Users.Any(a => a.Id == user))
                    return Users.First(a => a.Id == user);
            }

            //fetch..
            switch (user.Service)
            {
                case ServiceType.Spotify:
                    var spotifyClient = Ioc.Default.GetRequiredService<ISpotifyClient>();
                    var fetchedPublicUser = AsyncContext.Run(async () => await spotifyClient.Users.GetUserOnId(user.Id));

                    var newUser = AsyncContext.Run(async () => await AddUser(fetchedPublicUser.Name, fetchedPublicUser.Id,
                        fetchedPublicUser.Avatar.FirstOrDefault()?.Url, ServiceType.Spotify,
                        new Dictionary<string, object>()));
                    return newUser;
                    break;
            }

            return default;
        }


        /// <remarks>All access must be guarded by <see cref="Lock"/> object.</remarks>
        private HashSet<EumUser> Users { get; } = new();


        private async Task RefreshUserList()
        {
            foreach (var service in _usersDirectory.EnumerateUserFiles())
            {
                foreach (var fileInfo in service.Value)
                {
                    try
                    {
                        var userId = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                        lock (Lock)
                        {
                            if (Users.Any(w => w.Id.Id == userId && w.Id.Service == service.Key))
                            {
                                continue;
                            }
                        }

                        await AddUser(service.Key, userId);
                    }
                    catch (Exception ex)
                    {
                        S_Log.Instance.LogWarning(ex);
                    }
                }
            }
        }

        private async ValueTask<EumUser> AddUser(ServiceType serviceType, string userId)
        {
            (string userFullPath, string userBackupFullPath) = _usersDirectory.GetUserFilePaths(serviceType, userId);
            EumUser user;
            try
            {
                if (File.Exists(userFullPath))
                {
                    await using var fs = File.OpenRead(userFullPath);
                    user = await JsonSerializer.DeserializeAsync<EumUser>(fs,
                        SystemTextJsonSerializationOptions.Default);
                }
                else
                {
                    user = new EumUser
                    {
                        FilePath = userFullPath,
                        Id = new ItemId($"{serviceType.ToString().ToLower()}:user:{userId}")
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

                user = new EumUser
                {
                    FilePath = userFullPath,
                    Id = new ItemId($"{serviceType.ToString().ToLower()}:user:{userId}")
                };
            }

            AddUser(user);

            return user;
        }

        private void AddUser(EumUser user)
        {
            bool didUpdate = false;
            lock (Lock)
            {
                if (Users.Any(w => w.Id == user.Id))
                {
                    //update
                    //update
                    Users.Remove(user);
                    Users.Add(user);
                    didUpdate = true;
                }

                Users.Add(user);
            }
            if (string.IsNullOrEmpty(user.FilePath))
            {
                (string userFullPath, string userBackupFullPath) =
                    _usersDirectory.GetUserFilePaths(user.Id.Service, user.Id.Id);
                user.FilePath = userFullPath;
            }
            user.ToFile();
            //user.IsDefaultChanged += UserOnIsDefaultChanged;
            // wallet.StateChanged += Wallet_StateChanged;

            if (!didUpdate)
                UserAdded?.Invoke(this, user);
            else UserUpdated?.Invoke(this, user);
        }
    }
}
