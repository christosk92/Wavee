using System.Reactive;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Items;
using Eum.UI.JsonConverters;
using Eum.UI.Services.Users;
using ReactiveUI;
using System.Reactive.Linq;
using Eum.UI.Bases;
using Nito.AsyncEx;
using Eum.Connections.Spotify.Models.Users;
using Eum.UI.Services;
using Eum.UI.ViewModels.Settings;

namespace Eum.UI.Users;

[INotifyPropertyChanged]
public partial class EumUser : ConfigBase, IEquatable<EumUser>
{
    [ObservableProperty]
    [JsonRequired]
    [property: JsonPropertyName("ProfileName")]
    private string _profileName = null!;

    [ObservableProperty]
    [JsonRequired]
    [property: JsonPropertyName("IsDefault")]
    private bool _isDefault;

    [ObservableProperty]
    [property: JsonPropertyName("ProfilePicture")]
    private string? _profilePicture;

    [ObservableProperty]
    [property: JsonPropertyName("Metadata")]
    private Dictionary<string, object> _metadata;

    [ObservableProperty]
    [property: JsonPropertyName("SidebarWidth")]
    private double _sidebarWidth;

    [ObservableProperty]
    [property: JsonPropertyName("WindowHeight")]
    private double _windowHeight;

    [ObservableProperty]
    [property: JsonPropertyName("windowWidth")]
    private double _windowWidth;

    [ObservableProperty]
    [property: JsonPropertyName("appTheme")]
    private AppTheme _appTheme;

    public EumUser()
    {
        this.WhenAnyValue(
                x => x.ProfileName,
                x => x.WindowHeight,
                x => x.WindowWidth,
                x => x.ProfilePicture,
                x => x.IsDefault,
                x => x.SidebarWidth,
                x => x.Metadata,
                x => x._appTheme,
                (_, _, _, _, _, _, _, _) => Unit.Default)
            .Throttle(TimeSpan.FromMilliseconds(500))
            //.Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ => { this.ToFile(); });

        ThemeService = Ioc.Default.GetRequiredService<IThemeSelectorServiceFactory>()
            .GetThemeSelectorService(this);
    }

    [JsonIgnore]
    public IThemeSelectorService ThemeService { get; }
    public void ReplaceMetadata(string key, object value)
    {
        Metadata[key] = value;
        this.ToFile();
    }
    [JsonConverter(typeof(ItemIdToJsonConverter))]
    public ItemId Id { get; init; }

    public bool Equals(EumUser? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((EumUser)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(EumUser? left, EumUser? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EumUser? left, EumUser? right)
    {
        return !Equals(left, right);
    }

    [JsonIgnore]
    public override object This => this;
}