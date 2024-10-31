using System.Net;
using System;
using Serilog;
using Wavee.ViewModels.Interfaces;

namespace Wavee.ViewModels.Models.Users;

public class UserManager
{
    /// <remarks>All access must be guarded by <see cref="_lock"/> object.</remarks>
    private volatile bool _disposedValue = false;
    private readonly object _lock = new();


    private readonly UserFactory _userFactory;
    private readonly string _workDir;

    /// <summary>Cancels initialization of wallets.</summary>
    private readonly CancellationTokenSource _cancelAllTasks = new();

    /// <summary>Token from <see cref="_cancelAllTasks"/>.</summary>
    /// <remarks>Accessing the token of <see cref="_cancelAllTasks"/> can lead to <see cref="ObjectDisposedException"/>. So we copy the token and no exception can be thrown.</remarks>
    private readonly CancellationToken _cancelAllTasksToken;

    /// <remarks>All access must be guarded by <see cref="_lock"/> object.</remarks>
    private readonly HashSet<User> _users = new();
    public UserManager(
        string workDir,
        UserDirectories userDirectories,
        UserFactory userFactory)
    {
        _workDir = workDir;
        Directory.CreateDirectory(_workDir);
        UserDirectories = userDirectories;
        _userFactory = userFactory;
        _cancelAllTasksToken = _cancelAllTasks.Token;

        LoadUserListFromFileSystem();
    }

    /// <summary>
    /// Triggered if a user added to the User collection. The sender of the event will be the UserManager and the argument is the added User.
    /// </summary>
    public event EventHandler<User>? UserAdded;
    /// <summary>
    /// Triggered if any of the _users changes its state. The sender of the event will be the User.
    /// </summary>
    public event EventHandler<UserState>? UserStateChanged;


    private void LoadUserListFromFileSystem()
    {
        var userFileNames = UserDirectories
            .EnumerateUserFiles()
            .Select(fi => Path.GetFileNameWithoutExtension(fi.FullName));

        string[]? userIdsToLoad = null;
        lock (_lock)
        {
            userIdsToLoad = userFileNames
                 .Where(userFileName => !_users.Any(user => user.Id == userFileName))
                 .ToArray();
        }

        if (userIdsToLoad.Length == 0)
        {
            return;
        }

        List<Task<User>> userLoadTasks = userIdsToLoad
            .Select(walletName => Task.Run(() => LoadUserByIdFromDisk(walletName), _cancelAllTasksToken))
            .ToList();

        while (userLoadTasks.Count > 0)
        {
            var tasksArray = userLoadTasks.ToArray();
            var finishedTaskIndex = Task.WaitAny(tasksArray, _cancelAllTasksToken);
            var finishedTask = tasksArray[finishedTaskIndex];
            userLoadTasks.Remove(finishedTask);
            try
            {
                var user = finishedTask.Result;
                AddUser(user);
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Error");
            }
        }
    }
    private void AddUser(User user)
    {
        lock (_lock)
        {
            if (_users.Any(w => w.Id == user.Id))
            {
                throw new InvalidOperationException($"Wallet with the same ID was already added: {user.Id}.");
            }
            _users.Add(user);
        }

        if (!File.Exists(UserDirectories.GetUserFilePaths(user.Id).UserFilePath))
        {
            user.UserAuthenticator.ToFile();
        }

        user.StateChanged += User_StateChanged;

        UserAdded?.Invoke(this, user);
    }
    private void User_StateChanged(object? sender, UserState e)
    {
        UserStateChanged?.Invoke(sender, e);
    }

    private User LoadUserByIdFromDisk(string userId)
    {
        (string userFullPath, string userBackupFullPath) = UserDirectories.GetUserFilePaths(userId);
        User user;
        try
        {
            user = _userFactory.Create(IUserAuthenticator.FromFile(userFullPath));
        }
        catch (Exception ex)
        {
            if (!File.Exists(userBackupFullPath))
            {
                throw;
            }

            Log.Warning($"User got corrupted.\n" +
                              $"User file path: {userFullPath}\n" +
                              $"Trying to recover it from backup.\n" +
                              $"Backup path: {userBackupFullPath}\n" +
                              $"userFullPath: {ex}");
            if (File.Exists(userFullPath))
            {
                string corruptedUserBackupPath = $"{userBackupFullPath}_CorruptedBackup";
                if (File.Exists(corruptedUserBackupPath))
                {
                    File.Delete(corruptedUserBackupPath);
                    Log.Information($"Deleted previous corrupted wallet file backup from `{corruptedUserBackupPath}`.");
                }
                File.Move(userFullPath, corruptedUserBackupPath);
                Log.Information($"Backed up corrupted wallet file to `{corruptedUserBackupPath}`.");
            }
            File.Copy(userBackupFullPath, userFullPath);

            user = _userFactory.Create(IUserAuthenticator.FromFile(userFullPath));
        }

        return user;
    }
    public UserDirectories UserDirectories { get; }

    public User? GetUserById(string userModelId)
    {
        lock (_lock)
        {
            return _users.Single(x => x.Id == userModelId);
        }
    }

    public IEnumerable<User> GetUsers()
    {
        lock (_lock)
        {
            return _users.ToList();
        }
    }

    public bool HasUser()
    {
        lock (_lock)
        {
            return _users.Count > 0;
        }
    }
}