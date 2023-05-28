using System.Drawing;
using System.Text;
using System.Text.Json;
using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.Common;
using Wavee.UI.Infrastructure.Sys.IO;
using Wavee.UI.Infrastructure.Traits;
using Wavee.UI.ViewModels;

namespace Wavee.UI.Infrastructure.Sys;

public static class UiConfig<RT> where RT : struct, HasFile<RT>, HasDirectory<RT>, HasLocalPath<RT>
{
    //semaphore for file access
    private static readonly SemaphoreSlim FileLock = new(1, 1);

    private static Eff<Unit> Lock() =>
        Eff(() =>
        {
            FileLock.Wait();
            return unit;
        });

    private static Eff<Unit> Release() =>
        Eff(() =>
        {
            FileLock.Release();
            return unit;
        });


    public static Eff<RT, string> ConfigPath =>
        from appData in Local<RT>.localDir
        let configPath = Path.Combine(appData, "configs", "ui_config.json")
        from _ in Directory<RT>.createContainingDirectory(configPath)
        select configPath;

    public static Aff<RT, Unit> CreateDefaultIfNotExists =>
        from configPath in ConfigPath
        from exists in File<RT>.exists(configPath)
        where !exists
        from __ in SerializeAndWrite(configPath, CreateDefaultConfig())
        select unit;

    public static Aff<RT, uint> WindowWidth =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        select config.Window.Width;

    public static Aff<RT, uint> WindowHeight =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        select config.Window.Height;

    public static Eff<RT, AppTheme> Theme =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        select (AppTheme)config.Personalization.Theme;

    public static Eff<RT, string> Locale =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        select config.Personalization.Locale;

    public static Eff<RT, string> DeviceName =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        select config.Remote.DeviceName;

    public static Eff<RT, DeviceType> DeviceType =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        select (DeviceType)config.Remote.DeviceType;


    public static Aff<RT, Unit> SetWindowWidth(uint width) =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        from newConfig in SuccessEff(config.SetWindowWidth(width))
        from serialized in SerializeAndWrite(configPath, newConfig)
        select unit;

    public static Aff<RT, Unit> SetWindowHeight(uint height) =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        from newConfig in SuccessEff(config.SetWindowHeight(height))
        from serialized in SerializeAndWrite(configPath, newConfig)
        select unit;

    public static Aff<RT, Unit> SetTheme(AppTheme currentTheme) =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        from newConfig in SuccessEff(config.SetTheme(currentTheme))
        from serialized in SerializeAndWrite(configPath, newConfig)
        select unit;

    public static Aff<RT, Unit> SetLocale(string cultureName) =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        from newConfig in SuccessEff(config.SetLocale(cultureName))
        from serialized in SerializeAndWrite(configPath, newConfig)
        select unit;

    public static Aff<RT, Unit> SetDeviceType(DeviceType deviceType) =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        from newConfig in SuccessEff(config.SetDeviceType(deviceType))
        from serialized in SerializeAndWrite(configPath, newConfig)
        select unit;

    //device name
    public static Aff<RT, Unit> SetDeviceName(string deviceName) =>
        from configPath in ConfigPath
        from config in Deserialize(configPath)
        from newConfig in SuccessEff(config.SetDeviceName(deviceName))
        from serialized in SerializeAndWrite(configPath, newConfig)
        select unit;


    // public static Aff<RT, Unit> SetSetupCompleted(bool setupCompleted) =>
    //     from configPath in ConfigPath
    //     from config in Deserialize(configPath)
    //     from newConfig in SuccessEff(config.SetSetupCompleted(setupCompleted))
    //     from serialized in SerializeAndWrite(configPath, newConfig)
    //     select unit;
    //
    // public static Aff<RT, Unit> SetSetupProgress(int setupProgress) =>
    //     from configPath in ConfigPath
    //     from config in Deserialize(configPath)
    //     from newConfig in SuccessEff(config.SetSetupProgress(setupProgress))
    //     from serialized in SerializeAndWrite(configPath, newConfig)
    //     select unit;

    private static Aff<RT, Unit> SerializeAndWrite(string file, UiConfigStruct str) =>
        from bytes in Serialize(str)
        from _ in Lock()
        from __ in File<RT>.writeAllBytes(file, bytes)
        from ___ in Release()
        select unit;

    private static Eff<RT, byte[]> Serialize(UiConfigStruct str) =>
        Eff(() => JsonSerializer.SerializeToUtf8Bytes(str));

    private static Eff<RT, UiConfigStruct> Deserialize(string file) =>
        from _ in Lock()
        from bytes in File<RT>.readAllBytesSync(file)
        from __ in Release()
        select JsonSerializer.Deserialize<UiConfigStruct>(bytes.Span);

    private static UiConfigStruct CreateDefaultConfig() =>
        new(new WindowConfig(800, 600),
            new Personalization(0, null),
            new Remote((int)Eum.Spotify.connectstate.DeviceType.Computer, "Wavee"));

    private readonly record struct UiConfigStruct(WindowConfig Window, Personalization Personalization, Remote Remote)
    {
        public UiConfigStruct SetWindowWidth(uint width)
        {
            return this with
            {
                Window = Window with
                {
                    Width = width
                }
            };
        }

        public UiConfigStruct SetWindowHeight(uint height)
        {
            return this with
            {
                Window = Window with
                {
                    Height = height
                }
            };
        }

        public UiConfigStruct SetTheme(AppTheme currentTheme)
        {
            return this with
            {
                Personalization = Personalization with
                {
                    Theme = (int)currentTheme
                }
            };
        }

        public UiConfigStruct SetLocale(string cultureName)
        {
            return this with
            {
                Personalization = Personalization with
                {
                    Locale = cultureName
                }
            };
        }

        public UiConfigStruct SetDeviceType(DeviceType deviceType)
        {
            return this with
            {
                Remote = Remote with
                {
                    DeviceType = (int)deviceType
                }
            };
        }

        public UiConfigStruct SetDeviceName(string deviceName)
        {
            if(string.IsNullOrEmpty(deviceName))
                deviceName = "Wavee";
            return this with
            {
                Remote = Remote with
                {
                    DeviceName = deviceName
                }
            };
        }
    }

    private readonly record struct Remote(int DeviceType, string DeviceName);
    private readonly record struct Personalization(int Theme, string? Locale);
    private readonly record struct WindowConfig(uint Width, uint Height);
    // private readonly record struct SetupConfig(bool SetupCompleted, int SetupProgress);
}