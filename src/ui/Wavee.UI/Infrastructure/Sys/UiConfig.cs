using System.Drawing;
using System.Text;
using System.Text.Json;
using LanguageExt;
using Wavee.UI.Infrastructure.Sys.IO;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.Infrastructure.Sys;

public static class UiConfig<RT> where RT : struct, HasFile<RT>, HasDirectory<RT>, HasLocalPath<RT>
{
    public static Eff<RT, string> ConfigPath =>
        from appData in Local<RT>.localDir
        let configPath = Path.Combine(appData, "configs", "ui_config.json")
        from _ in Directory<RT>.createContainingDirectory(configPath)
        select configPath;

    public static Aff<RT, Unit> CreateDefaultIfNotExists =>
        from configPath in ConfigPath
        from exists in File<RT>.exists(configPath)
        where !exists
        from _ in SerializeAndWrite(configPath, CreateDefaultConfig())
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

    private static Aff<RT, Unit> SerializeAndWrite(string file, UiConfigStruct str) =>
        from bytes in Serialize(str)
        from __ in File<RT>.writeAllBytes(file, bytes)
        select unit;

    private static Eff<RT, byte[]> Serialize(UiConfigStruct str) =>
        Eff(() => JsonSerializer.SerializeToUtf8Bytes(str));

    private static Eff<RT, UiConfigStruct> Deserialize(string file) =>
        from bytes in File<RT>.readAllBytesSync(file)
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
    }

    private readonly record struct WindowConfig(uint Width, uint Height);
}