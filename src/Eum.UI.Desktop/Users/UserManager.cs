using Eum.Logging;
using Eum.UI.Euum.Client;
using Eum.UI.Helpers;

namespace Eum.UI.Users;

public class UserManager : IUserProvider
{

    /// <remarks>All access must be guarded by <see cref="Lock"/> object.</remarks>
    private volatile bool _disposedValue = false;

    public UserManager(string workDir, UserDirectories userDirectories)
    {
        WorkDir = Guard.NotNullOrEmptyOrWhitespace(nameof(workDir), workDir, true);
        WorkDir = Path.Combine(WorkDir, UserDirectories.UsersDirName);
        Directory.CreateDirectory(WorkDir);
        UserDirectories = Guard.NotNull(nameof(userDirectories), userDirectories);

        RefreshUserList();
    }

    /// <summary>
    /// Triggered if a user added to the Users collection. The sender of the event will be the UserManager and the argument is the added User.
    /// </summary>
    public event EventHandler<EumUser>? UserAdded;
    /// <summary>
    /// Triggered if a user removed from the Users collection. The sender of the event will be the UserManager and the argument is the removed User.
    /// </summary>
    public event EventHandler<EumUser>? UserRemoved;

    public event EventHandler<bool> IsDefaultChanged; 
    private CancellationTokenSource CancelAllInitialization { get; } = new();

    /// <remarks>All access must be guarded by <see cref="Lock"/> object.</remarks>
    private HashSet<EumUser> Users { get; } = new();

    private object Lock { get; } = new();
    public UserDirectories UserDirectories { get; }
    public string WorkDir { get; }

    private void RefreshUserList()
    {
        foreach (var fileInfo in UserDirectories.EnumearteUsersFiles())
        {
            try
            {
                var userId = Path.GetFileNameWithoutExtension(fileInfo.FullName);
                lock (Lock)
                {
                    if (Users.Any(w => w.UserId == userId))
                    {
                        continue;
                    }
                }

                AddUser(userId);
            }
            catch (Exception ex)
            {
                S_Log.Instance.LogWarning(ex);
            }
        }
    }

    public Task<IEnumerable<IEumUser>> GetUsersAsync()
        => Task.FromResult<IEnumerable<IEumUser>>(GetUsers(refreshUsersList: true));

    public IEnumerable<EumUser> GetUsers(bool refreshUsersList = true)
    {
        if (refreshUsersList)
        {
            RefreshUserList();
        }

        lock (Lock)
        {
            return Users.ToList();
        }
    }
    // public EumUser AddUser(KeyManager keyManager, ServiceType service)
    // {
    //     EumUser wallet = new(WorkDir, keyManager);
    //     AddUser(wallet);
    //     return wallet;
    // }
    private void AddUser(string userId)
    {
        (string userFullPath, string userBackupFullPath) = UserDirectories.GetUserFilePaths(userId);
        EumUser user;
        try
        {
            user = new EumUser(WorkDir, userFullPath);
        }
        catch (Exception ex)
        {
            if (!File.Exists(userBackupFullPath))
            {
                throw;
            }

            S_Log.Instance.LogWarning($"Wallet got corrupted.\n" +
                              $"Wallet Filepath: {userFullPath}\n" +
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
                        $"Deleted previous corrupted wallet file backup from `{corruptedWalletBackupPath}`.");
                }

                File.Move(userFullPath, corruptedWalletBackupPath);
                S_Log.Instance.LogInfo($"Backed up corrupted wallet file to `{corruptedWalletBackupPath}`.");
            }

            File.Copy(userBackupFullPath, userFullPath);

            user = new EumUser(WorkDir, userFullPath);
        }

        AddUser(user);
    }

    public void AddUser(EumUser user)
    {
        lock (Lock)
        {
            if (Users.Any(w => w.UserId == user.UserId))
            {
                throw new InvalidOperationException($"User the same ID was already added: {user.UserId}.");
            }

            Users.Add(user);
        }

        if (!File.Exists(UserDirectories.GetUserFilePaths(user.UserId).userFilePath))
        {
            user.UserDetailProvider.ToFile();
        }

        user.IsDefaultChanged += UserOnIsDefaultChanged;
        // wallet.StateChanged += Wallet_StateChanged;

        UserAdded?.Invoke(this, user);
    }

    private void UserOnIsDefaultChanged(object? sender, bool e)
    {
        IsDefaultChanged?.Invoke(sender, e);
    }

    public bool HasUser() => AnyUser(_ => true);
    public bool AnyUser(Func<EumUser, bool> predicate)
    {
        lock (Lock)
        {
            return Users.Any(predicate);
        }
    }

    public void RemoveUser(EumUser user)
    {
        lock (Lock)
        {
            if (Users.All(w => w.UserId != user.UserId))
            {
                throw new InvalidOperationException($"User does not exist with id: {user.UserId}.");
            }

            Users.Remove(user);
        }

        user.UserDetailProvider.Delete();

        user.IsDefaultChanged -= UserOnIsDefaultChanged;

        // wallet.StateChanged += Wallet_StateChanged;

        UserRemoved?.Invoke(this, user);
    }
}