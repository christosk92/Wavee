using System.Reactive;
using System.Reactive.Linq;
using Eum.UI.Bases;
using Eum.UI.Io;
using Newtonsoft.Json;
using ReactiveUI;

namespace Eum.UI.Users;
[JsonObject(MemberSerialization.OptIn)]
public class UserDetailProvider : ConfigBase
{
    private int? _wWidth;
    private string? _picture;
    private int? _wHeight;
    private bool? _isDefault;
    private double? _sidebarWidth;
    private string? _profileName;
    private ServiceType _serviceType;
    private EumPlaylist[]? _playlists;

    public UserDetailProvider(string userFilepath) : base(userFilepath)
    {
        this.WhenAnyValue(
                x => x.Picture,
                x => x.WindowHeight,
                x => x.WindowWidth,
                x => x.IsDefault,
                x => x.Service,
                x => x.Playlists,
                x => x.SidebarWidth,
                (_, _, _, _, _, _, _) => Unit.Default)
            .Throttle(TimeSpan.FromMilliseconds(500))
            //.Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                ToFile();
            });
        this.LoadOrCreateDefaultFile();
    }

    [JsonProperty(PropertyName = "UserPicture")]
    public string? Picture
    {
        get => _picture;
        set => RaiseAndSetIfChanged(ref _picture, value);
    }

    [JsonProperty(PropertyName = "WindowWidth")]
    public int? WindowWidth
    {
        get => _wWidth;
        set => RaiseAndSetIfChanged(ref _wWidth, value);
    }
    [JsonProperty(PropertyName = "WindowHeight")]
    public int? WindowHeight
    {
        get => _wHeight;
        set => RaiseAndSetIfChanged(ref _wHeight, value);
    }
    [JsonProperty(PropertyName = "IsDefault")]
    public bool? IsDefault
    {
        get => _isDefault;
        set => RaiseAndSetIfChanged(ref _isDefault, value);
    }

    [JsonProperty(PropertyName = "SidebarWidth")]
    public double? SidebarWidth
    {
        get => _sidebarWidth;
        set => RaiseAndSetIfChanged(ref _sidebarWidth, value);
    }

    [JsonProperty(PropertyName = "ProfileName")]
    public string? ProfileName
    {
        get => _profileName;
        set => RaiseAndSetIfChanged(ref _profileName, value);
    }

    [JsonProperty(PropertyName = "ServiceType")]
    public ServiceType Service
    {
        get => _serviceType;
        set => this.RaiseAndSetIfChanged(ref _serviceType, value);
    }
    [JsonProperty(PropertyName = "Playlists")]
    public EumPlaylist[]? Playlists
    {
        get => _playlists;
        set => this.RaiseAndSetIfChanged(ref _playlists, value);
    }

    public void Delete()
    {
        var safeio = new SafeIoManager(FilePath);
        safeio.DeleteMe();
    }
}
[JsonObject(MemberSerialization.OptIn)]
public class EumPlaylist : ReactiveObject
{
    private string _name;
    private string? _imagePath;
    private Guid[]? _tracks;

    public EumPlaylist()
    {
        this.WhenAnyValue(
                x => x.Name,
                x=> x.ImagePath,
                x => x.LinkedTo,
                x=> x.Tracks,
                (_, _, _, _) => Unit.Default)
            .Throttle(TimeSpan.FromMilliseconds(100))
            //.Skip(1) // Won't save on UiConfig creation.
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(_ =>
            {
                User.UserDetailProvider.ToFile();
            });

    }
    [JsonProperty(PropertyName = "Id")]
    public Guid Id { get; init; }
    [JsonIgnore]
    public EumUser User { get; set; }
    [JsonProperty(PropertyName = "Name")]
    public string Name
    {
        get => _name;
        set => this.RaiseAndSetIfChanged(ref _name, value);
    }
    [JsonProperty(PropertyName = "LinkedTo")]
    public IReadOnlyDictionary<ServiceType, string> LinkedTo { get; set; } = new Dictionary<ServiceType, string>();
    [JsonProperty(PropertyName = "ImagePath")]
    public string? ImagePath
    {
        get => _imagePath;
        set => this.RaiseAndSetIfChanged(ref _imagePath, value);
    }
    [JsonProperty(PropertyName = "Tracks")]
    public Guid[]? Tracks
    {
        get => _tracks;
        set => this.RaiseAndSetIfChanged(ref _tracks, value);
    }
}