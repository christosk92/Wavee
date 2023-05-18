using System.Drawing;
using System.Text;
using System.Text.Json;
using LanguageExt;
using Wavee.UI.Infrastructure.Sys.IO;
using Wavee.UI.Infrastructure.Traits;

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
        new(new WindowConfig(800, 600));

    private readonly record struct UiConfigStruct(WindowConfig Window)
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

        // public UiConfigStruct SetSetupCompleted(bool setupCompleted)
        // {
        //     return this with
        //     {
        //         Setup = Setup with
        //         {
        //             SetupCompleted = setupCompleted
        //         }
        //     };
        // }
        //
        // public UiConfigStruct SetSetupProgress(int setupProgress)
        // {
        //     return this with
        //     {
        //         Setup = Setup with
        //         {
        //             SetupProgress = setupProgress
        //         }
        //     };
        // }
    }

    private readonly record struct WindowConfig(uint Width, uint Height);
    // private readonly record struct SetupConfig(bool SetupCompleted, int SetupProgress);
}