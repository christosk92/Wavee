using Eum.UI.Items;
using ReactiveUI;
using System.Reactive;
using System.Reactive.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Newtonsoft.Json;
using Eum.UI.Bases;
using Eum.UI.JsonConverters;
using Eum.UI.Users;

namespace Eum.UI.Playlists;
[INotifyPropertyChanged]
public partial class EumPlaylist : ConfigBase, IEquatable<EumPlaylist>
{
    private string _name;
    private string? _imagePath;
    private ItemId[]? _tracks;
    private ItemId[] _alsoUnder;
    private string? _description;

    public EumPlaylist()
    {
        this.WhenAnyValue(
                x => x.Name,
                x => x.ImagePath,
                x => x.LinkedTo,
                x => x.Tracks,
                x=> x.Metadata,
                x=> x.Description,
                (_, _, _, _, _, _) => Unit.Default)
            .Throttle(TimeSpan.FromMilliseconds(100))
            //.Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                ToFile();
            });
    }
    [JsonPropertyName("Id")]
    public ItemId Id { get; init; }

    [JsonPropertyName("User")]
    public ItemId User { get; set; }
    [JsonPropertyName("Metadata")]
    public IReadOnlyDictionary<string, string>? Metadata { get; set; }

    [JsonPropertyName("Name")]
    public string Name
    {
        get => _name;
        set => this.SetProperty(ref _name, value);
    }
    [JsonPropertyName("LinkedTo")]
    public IReadOnlyDictionary<ServiceType, ItemId> LinkedTo { get; set; } = new Dictionary<ServiceType, ItemId>();
    [JsonPropertyName("ImagePath")]
    public string? ImagePath
    {
        get => _imagePath;
        set => this.SetProperty(ref _imagePath, value);
    }
    [JsonPropertyName("Tracks")]
    public ItemId[]? Tracks
    {
        get => _tracks;
        set => this.SetProperty(ref _tracks, value);
    }

    [System.Text.Json.Serialization.JsonIgnore]
    public override object This => this;

    [JsonPropertyName("AlsoUnder")]
    public ItemId[] AlsoUnder
    {
        get => _alsoUnder;
        set => this.SetProperty(ref _alsoUnder, value);
    }
    [JsonPropertyName("Description")]
    public string? Description
    {
        get => _description;
        set => this.SetProperty(ref _description, value);
    }
    public int Order { get; set; }

    public bool Equals(EumPlaylist? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id.Equals(other.Id);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        return Equals((EumPlaylist)obj);
    }

    public override int GetHashCode()
    {
        return Id.GetHashCode();
    }

    public static bool operator ==(EumPlaylist? left, EumPlaylist? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(EumPlaylist? left, EumPlaylist? right)
    {
        return !Equals(left, right);
    }
}