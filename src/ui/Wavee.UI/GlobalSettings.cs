
using Newtonsoft.Json;
using ReactiveUI;
using Wavee.UI.Bases;
using System.Reactive.Linq;
using Wavee.Player;

namespace Wavee.UI;

[JsonObject(MemberSerialization.OptIn)]
public class GlobalSettings : ConfigBase
{
    private string? _defaultUser;

    public GlobalSettings() : base()
    {
    }

    public GlobalSettings(string filePath) : base(filePath)
    {
        this.WhenAnyValue(x => x.DefaultUser)
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => ToFile());
    }

    [JsonProperty(PropertyName = "DefaultUser", DefaultValueHandling = DefaultValueHandling.Populate)]
    public string DefaultUser
    {
        get => _defaultUser;
        set => this.RaiseAndSetIfChanged(ref _defaultUser, value);
    }
}
