using System.ComponentModel;
using System.Runtime.CompilerServices;
using Eum.UI.Helpers;
using Microsoft.Extensions.Hosting;
using Nito.AsyncEx;

namespace Eum.UI.Users;

public class EumUser : BackgroundService, IEumUser, INotifyPropertyChanged
{

    public EumUser(string dataDir, string userFullPath)
    {
        Guard.NotNullOrEmptyOrWhitespace(nameof(dataDir), dataDir);
        UserDetailProvider = new UserDetailProvider(userFullPath);
        RuntimeParams.SetDataDir(dataDir);
        HandleFiltersLock = new AsyncLock();
    }

    public UserDetailProvider UserDetailProvider { get; }

    private AsyncLock HandleFiltersLock { get; }
    public bool IsLoggedIn { get; set; }

    public string UserId => string.IsNullOrWhiteSpace(UserDetailProvider.FilePath) ? "" : Path.GetFileNameWithoutExtension(UserDetailProvider.FilePath);

    public string UserName => UserDetailProvider.ProfileName;

    public string? ProfilePicture
    {
        get => UserDetailProvider.Picture;
        set => UserDetailProvider.Picture = value;
    }
 
    public void Logout()
    {
        IsLoggedIn = false;
    }

    

    /// <inheritdoc/>
    public override async Task StartAsync(CancellationToken cancel)
    {
        try
        {
            await RuntimeParams.LoadAsync().ConfigureAwait(false);

            using (await HandleFiltersLock.LockAsync(cancel)
                       .ConfigureAwait(false))
            {
                await LoadUserStateAsync(cancel).ConfigureAwait(false);
            }

            await base.StartAsync(cancel).ConfigureAwait(false);
        }
        catch
        {
            throw;
        }
    }
    private Task LoadUserStateAsync(CancellationToken cancel)
    {
        return Task.CompletedTask;
    }
    /// <inheritdoc />
    protected override Task ExecuteAsync(CancellationToken stoppingToken) => Task.CompletedTask;

    public bool IsDefault
    {
        get
        {
            return UserDetailProvider.IsDefault ?? false;
        }
        set
        {
            if (UserDetailProvider.IsDefault != value)
            {
                UserDetailProvider.IsDefault = value;
                IsDefaultChanged?.Invoke(this, value);
                OnPropertyChanged(nameof(IsDefault));
            }
        }
    }

    public event EventHandler<bool> IsDefaultChanged;
    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}

