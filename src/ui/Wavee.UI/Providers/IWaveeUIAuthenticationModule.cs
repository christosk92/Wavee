namespace Wavee.UI.Providers;

public sealed class WaveeUIAuthenticationModule
{
    internal WaveeUIAuthenticationModule(string oAuthUrl,
        Func<string, ValueTask<bool>> oAuthCallback,
        IWaveeUIProvider rootProvider)
    {
        IsOAuth = true;
        OAuthUrl = oAuthUrl;
        RootProvider = rootProvider;
        OAuthCallback = oAuthCallback;
    }

    internal WaveeUIAuthenticationModule(Task authenticationTask, IWaveeUIProvider rootProvider)
    {
        IsOAuth = false;
        OAuthUrl = null;
        AuthenticationTask = authenticationTask;
        RootProvider = rootProvider;
    }
    internal WaveeUIAuthenticationModule(Func<string, string, Task> userNamePasswordAuthentication, IWaveeUIProvider rootProvider)
    {
        IsOAuth = false;
        OAuthUrl = null;
        AuthenticationTask = null;
        UserNamePasswordAuthentication = userNamePasswordAuthentication;
        RootProvider = rootProvider;
    }

    public readonly bool IsOAuth;
    public readonly string? OAuthUrl;
    public readonly Func<string, ValueTask<bool>>? OAuthCallback;

    public readonly Task? AuthenticationTask;

    public readonly Func<string, string, Task>? UserNamePasswordAuthentication;
    public readonly IWaveeUIProvider RootProvider;
}