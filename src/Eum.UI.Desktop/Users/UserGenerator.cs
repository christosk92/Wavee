using System;

namespace Eum.UI.Users;

public class UserGenerator
{
    private static readonly string[] ReservedFileNames = new string[]
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9",
    };

    public UserGenerator(string usersDir, ServiceType serviceType)
    {
        UsersDir = usersDir;
        Network = serviceType;
    }

    public string UsersDir { get; private set; }
    public ServiceType Network { get; private set; }

    public EumUser GenerateUser(string profileName,
        string? id = null,
        ServiceType serviceType = ServiceType.Local)
    {
        id ??= (Guid.NewGuid()).ToString();
        string userFilePath = GetUserFilePath(id, UsersDir);

        var newUser = new EumUser(UsersDir,  userFilePath);

        newUser.UserDetailProvider.ProfileName = profileName;
        //newUser.UserDetailProvider.ser
        return newUser;
    }

    public static string GetUserFilePath(string profileId, string usersDir)
    {
        if (!ValidateWalletName(profileId))
        {
            throw new ArgumentException("Invalid user name.");
        }

        string walletFilePath = Path.Combine(usersDir, $"{profileId}.json");
        if (File.Exists(walletFilePath))
        {
            throw new ArgumentException("UserId is already taken.");
        }

        return walletFilePath;
    }

    public static bool ValidateWalletName(string walletName)
    {
        if (string.IsNullOrWhiteSpace(walletName))
        {
            return false;
        }

        var invalidChars = Path.GetInvalidFileNameChars();
        var isValid = !walletName.Any(c => invalidChars.Contains(c)) && !walletName.EndsWith(".");
        var isReserved =
            ReservedFileNames.Any(w => walletName.ToUpper() == w || walletName.ToUpper().StartsWith(w + "."));
        return isValid && !isReserved;
    }
}