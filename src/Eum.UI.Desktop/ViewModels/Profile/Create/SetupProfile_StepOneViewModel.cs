using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.DependencyInjection;
using Eum.UI.Stage;
using Eum.UI.ViewModels.Login;
using ReactiveUI;
using DynamicData;
using DynamicData.Binding;
using Eum.UI.Users;
using Eum.Users;
using Nito.AsyncEx;
using StringComparison = System.StringComparison;

namespace Eum.UI.ViewModels.Profile.Create;

public partial class SetupProfile_StepOneViewModel : ReactiveObject, IStage, IDisposable
{
    public int StageIndex => 0;
    private bool _canGoNext;
    [AutoNotify]
    private string? _profileName;
    [AutoNotify]
    private PictureItem? _selectedProfilePicture;

    [AutoNotify] private string _searchTerm;

    public bool CanGoNext
    {
        get => _canGoNext;
        set => this.RaiseAndSetIfChanged(ref _canGoNext, value);
    }

    private readonly SourceList<PictureItem> _picturesSL = new();

    private readonly IDisposable _cleanUp;
    public SetupProfile_StepOneViewModel()
    {
        foreach (var pic in Ioc.Default.GetService<IList<GroupedprofilePictures>>()
                     .SelectMany(a => a.Items))
        {
            _picturesSL.Add(pic);
        }
        var observableFilter = this
            .WhenAnyValue(viewModel => viewModel.SearchTerm)
            .Select(BuildFilter);

        Disposables[0] = _picturesSL.Connect()
            .Filter(observableFilter)
            .Bind(Pictures)
            .Subscribe();

        Disposables[1] = this.WhenAnyValue(a => a.ProfileName, a => a.SelectedProfilePicture, ((arg1, arg2) =>
        {
            return !string.IsNullOrEmpty(arg1) && arg2?.Src != null;
        })).BindTo(this, x=> x.CanGoNext);
    }

    public (IStage? Stage, object? Result) NextStage()
    {
        //return (null, null);
        var userGenerator = new UserGenerator(
            Ioc.Default.GetRequiredService<UserDirectories>().UsersDir, ServiceType.Local);
        var newUser = userGenerator.GenerateUser(_profileName);
        newUser.ProfilePicture
            = _selectedProfilePicture.Src;
        Ioc.Default.GetRequiredService<UserManager>()
            .AddUser(newUser);
        return (null, newUser);
    }

    public string Title => "Setup your profile";
    public string Description => "Pick a name and an image. You can also optionally secure your profile.";

    public IDisposable[] Disposables { get; } = new IDisposable[2];


    public void Dispose()
    {
        _picturesSL.Dispose();
        foreach (var disposable in Disposables)
        {
            disposable.Dispose();
        }
        _cleanUp?.Dispose();
    }
    public ObservableCollectionExtended<PictureItem> Pictures { get; } = new ObservableCollectionExtended<PictureItem>();

    private Func<PictureItem, bool> BuildFilter(string searchText)
    {
        if (string.IsNullOrEmpty(searchText)) return trade => true;
        return t => t.Name.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }
}

public class GroupedprofilePictures
{
    [JsonPropertyName("items")]
    public IList<PictureItem> Items { get; init; }
}