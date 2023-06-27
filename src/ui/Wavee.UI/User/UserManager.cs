using System.Text.Json;
using LanguageExt;

namespace Wavee.UI.User;

public sealed class UserManager : IUserManager
{
    private readonly string _path;

    public UserManager(string persistentPath)
    {
        _path = Path.Combine(persistentPath, "Wavee", "Users");
        Directory.CreateDirectory(_path);
    }

    public Option<UserViewModel> GetUser(UserId id)
    {
        //enumerate all files in the directory and find the one with the id
        //files end with .json
        var files = Directory.EnumerateFiles(_path, "*.json");
        foreach (var file in files)
        {
            var json = File.ReadAllText(file);
            var user = JsonSerializer.Deserialize<UserInfo>(json);
            if (user.Id == id)
            {
                return new UserViewModel(
                    id: user.Id,
                    displayName: user.DisplayName,
                    image: user.Image,
                    client: null
                )
                {
                    ReusableCredentials = AppProviders.GetCredentialsFor(user.Id.ToString()).IfNone(string.Empty)
                };
            }
        }

        return Option<UserViewModel>.None;
    }

    public void SaveUser(UserViewModel user, bool setDefault)
    {
        var json = JsonSerializer.Serialize(user.Info);
        var path = Path.Combine(_path, $"{user.Id}.json");
        File.WriteAllText(path, json);

        //Check if we have more than 1 user
        var files = Directory.EnumerateFiles(_path, "*.json");
        if (files.Count() is 1)
        {
            setDefault = true;
        }

        if (setDefault)
        {
            Shared.GlobalSettings.DefaultUser = user.Id.ToString();
        }

        if (!string.IsNullOrEmpty(user.ReusableCredentials))
        {
            AppProviders.SecurePasswordInVault(user.Id.ToString(), user.ReusableCredentials);
        }
    }
}

public interface IUserManager
{
    Option<UserViewModel> GetUser(UserId id);
    void SaveUser(UserViewModel user, bool setDefault);
}