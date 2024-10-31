namespace Wavee.ViewModels.Interfaces;

public interface IUserAuthenticator
{
    event EventHandler<WaveeUserInfo> Authenticated;

    void ToFile()
    {
        //TODO:
        throw new NotImplementedException();
    }

    static IUserAuthenticator FromFile(string userFullPath)
    {
        //TODO:
        throw new NotImplementedException();
    }
}

public record WaveeUserInfo(string Id, string Name);