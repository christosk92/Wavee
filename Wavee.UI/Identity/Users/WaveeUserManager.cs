using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Wavee.UI.Identity.Messaging;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.Identity.Users.Directories;

namespace Wavee.UI.Identity.Users
{
    public class WaveeUserManager : ObservableRecipient, IUserProvider
    {
        /// <remarks>All access must be guarded by <see cref="Lock"/> object.</remarks>
        private volatile bool _disposedValue = false;

        private readonly string _workDir;
        private readonly UserDirectories _userDirectories;
        private readonly ILogger<WaveeUserManager>? _logger;
        public WaveeUserManager(ServiceType service, string workdir,
            UserDirectories userDirectories,
            ILogger<WaveeUserManager>? logger = null)
        {
            ServiceType = service;
            _workDir = workdir;
            Directory.CreateDirectory(_workDir);
            _userDirectories = userDirectories;
            _logger = logger;

            RefreshUserList();
        }

        public ServiceType ServiceType { get; }

        /// <remarks>All access must be guarded by <see cref="Lock"/> object.</remarks>
        private HashSet<WaveeUser> Users { get; } = new();
        private object Lock { get; } = new();

        private void RefreshUserList()
        {
            foreach (var fileInfo in _userDirectories.EnumerateUserFiles())
            {
                try
                {
                    string userId = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                    lock (Lock)
                    {
                        if (Users.Any(w => w.Id == userId && w.ServiceType == ServiceType))
                        {
                            continue;
                        }
                    }
                    AddUser(userId);
                }
                catch (Exception ex)
                {
                    _logger?.LogWarning(ex, "An error occurred while retrieving users.");
                }
            }
        }

        public IReadOnlyCollection<WaveeUser> GetUsers(bool refreshUserList = true)
        {
            if (refreshUserList)
            {
                RefreshUserList();
            }

            lock (Lock)
            {
                return Users.ToList();
            }
        }

        public Task<IReadOnlyCollection<WaveeUser>> GetUsersAsync()
            => Task.FromResult(GetUsers(refreshUserList: true));

        public IWaveeUser AddUser(string username,
            string? displayName,
            string? profilePicture,
            Dictionary<string, string> metadata)
        {
            var userData = new UserData(
                _workDir,
                ServiceType,
                Username: username,
                DisplayName: displayName,
                ProfilePicture: profilePicture,
                Metadata: metadata
            );
            var user = new WaveeUser(
                ServiceType, userData);
            AddUser(user);
            return user;
        }
        private void AddUser(string userId)
        {
            var userFullpath = _userDirectories.GetUserFilePath(userId);
            var user = new WaveeUser(ServiceType, userFullpath);
            AddUser(user);

        }

        private void AddUser(WaveeUser user)
        {
            lock (Lock)
            {
                if (Users.Any(u => u.Id == user.Id && u.ServiceType == user.ServiceType))
                {
                    throw new InvalidOperationException($"User with the same id was already added: {user.Id}");
                }
                Users.Add(user);
            }

            if (!File.Exists(_userDirectories.GetUserFilePath(user.Id)))
            {
                user.UserData.ToFile();
            }


            WeakReferenceMessenger.Default.Send(new UserAddedMessage(user));
        }
    }
}
