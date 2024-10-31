using System.ComponentModel;
using System.Reactive;
using System.Reactive.Linq;
using Newtonsoft.Json;
using ReactiveUI;
using Wavee.ViewModels.Bases;
using Wavee.ViewModels.Converters;
using Wavee.ViewModels.Enums;

namespace Wavee.ViewModels;

[JsonObject(MemberSerialization.OptIn)]
public class UiConfig : ConfigBase
{
	private bool _darkModeEnabled;
	private string? _lastSelectedUser;
	private string _windowState = "Normal";
	private bool _runOnSystemStartup;
	private bool _oobe;
	private bool _hideOnClose;
	private double? _windowWidth;
	private double? _windowHeight;

    public UiConfig() : base()
    {
    }

    public UiConfig(string filePath) : base(filePath)
    {
        this.WhenAnyValue(
                x => x.DarkModeEnabled,
                x => x.LastSelectedUser,
                x => x.WindowState,
                x => x.Oobe,
                x => x.RunOnSystemStartup,
                x => x.HideOnClose,
                (_, _, _, _, _, _) => Unit.Default)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ToFile());

        this.WhenAnyValue(
                x => x.WindowWidth,
                x => x.WindowHeight)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.TaskpoolScheduler)
            .Subscribe(_ => ToFile());
    }

    [JsonProperty(PropertyName = "WindowWidth")]
    public double? WindowWidth
    {
        get => _windowWidth;
        internal set => base.SetField(ref _windowWidth, value);
    }

    [JsonProperty(PropertyName = "WindowHeight")]
    public double? WindowHeight
    {
        get => _windowHeight;
        internal set => SetField(ref _windowHeight, value);
    }

    [DefaultValue(false)]
    [JsonProperty(PropertyName = "HideOnClose", DefaultValueHandling = DefaultValueHandling.Populate)]
    public bool HideOnClose
    {
        get => _hideOnClose;
        set => SetField(ref _hideOnClose, value);
    }

    [JsonProperty(PropertyName = "Oobe", DefaultValueHandling = DefaultValueHandling.Populate)]
    [DefaultValue(true)]
    public bool Oobe
    {
        get => _oobe;
        set => SetField(ref _oobe, value);
    }

    [JsonProperty(PropertyName = "WindowState")]
    [JsonConverter(typeof(WindowStateAfterStartJsonConverter))]
    public string WindowState
    {
        get => _windowState;
        internal set => SetField(ref _windowState, value);
    }
    [JsonProperty(PropertyName = "DarkModeEnabled", DefaultValueHandling = DefaultValueHandling.Populate)]
    public bool DarkModeEnabled
    {
        get => _darkModeEnabled;
        set => SetField(ref _darkModeEnabled, value);
    }

    [DefaultValue(null)]
    [JsonProperty(PropertyName = "    public string? LastSelectedUser\r\n", DefaultValueHandling = DefaultValueHandling.Populate)]
    public string? LastSelectedUser
    {
        get => _lastSelectedUser;
        set => SetField(ref _lastSelectedUser, value);
    }

    // OnDeserialized changes this default on Linux.
    [DefaultValue(false)]
    [JsonProperty(PropertyName = "RunOnSystemStartup", DefaultValueHandling = DefaultValueHandling.Populate)]
    public bool RunOnSystemStartup
    {
        get => _runOnSystemStartup;
        set => SetField(ref _runOnSystemStartup, value);
    }
}