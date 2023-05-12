using Wavee.UI.Helpers;

namespace Wavee.UI.Daemon;

public class Config
{
    public PersistentConfig PersistentConfig { get; }
    private string[] Args { get; }
    public static string DataDir => GetString(
        EnvironmentHelpers.GetDataDir(Path.Combine("Wavee", "UI")),
        Environment.GetCommandLineArgs(),
        "datadir");

    public Config(PersistentConfig persistentConfig, string[] args)
    {
        PersistentConfig = persistentConfig;
        Args = args;
    }

    private bool GetEffectiveBool(bool valueInConfigFile, string key) =>
        GetEffectiveValue(
            valueInConfigFile,
            x =>
                bool.TryParse(x, out var value)
                    ? value
                    : throw new ArgumentException("must be 'true' or 'false'.", key),
            Args,
            key);

    private string GetEffectiveString(string valueInConfigFile, string key) =>
        GetEffectiveValue(valueInConfigFile, x => x, Args, key);

    private string? GetEffectiveOptionalString(string? valueInConfigFile, string key) =>
        GetEffectiveValue(valueInConfigFile, x => x, Args, key);

    private T GetEffectiveValue<T>(T valueInConfigFile, Func<string, T> converter, string key) =>
        GetEffectiveValue(valueInConfigFile, converter, Args, key);

    private static string GetString(string valueInConfigFile, string[] args, string key) =>
        GetEffectiveValue(valueInConfigFile, x => x, args, key);

    private static T GetEffectiveValue<T>(T valueInConfigFile, Func<string, T> converter, string[] args, string key)
    {
        if (ArgumentHelpers.TryGetValue(key, args, converter, out var cliArg))
        {
            return cliArg;
        }

        var envKey = "WAVEE-" + key.ToUpperInvariant();
        var envVars = Environment.GetEnvironmentVariables();
        if (envVars.Contains(envKey))
        {
            if (envVars[envKey] is string envVar)
            {
                return converter(envVar);
            }
        }

        return valueInConfigFile;
    }

}