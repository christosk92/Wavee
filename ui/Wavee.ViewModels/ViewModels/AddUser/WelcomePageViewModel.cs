using Eum.Spotify.context;
using ReactiveUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using System.Threading.Tasks;
using Wavee.ViewModels.ViewModels.Dialogs.Base;
using Wavee.ViewModels.ViewModels.NavBar;

namespace Wavee.ViewModels.ViewModels.AddUser;

[NavigationMetaData(Title = "Welcome")]
public partial class WelcomePageViewModel : DialogViewModelBase<Unit>
{
    private const int NumberOfPages = 1;
    [AutoNotify] private int _selectedIndex;
    [AutoNotify] private string? _nextLabel;
    [AutoNotify] private bool _enableNextKey = true;

    private WelcomePageViewModel()
    {
        SetupCancel(enableCancel: false, enableCancelOnEscape: false, enableCancelOnPressed: false);

        SelectedIndex = 0;
        NextCommand = ReactiveCommand.Create(OnNext);
        CanGoBack = this.WhenAnyValue(x => x.SelectedIndex, i => i > 0);
        BackCommand = ReactiveCommand.Create(() => SelectedIndex--, CanGoBack);

        this.WhenAnyValue(x => x.SelectedIndex)
            .Subscribe(
                x =>
                {
                    NextLabel = x < NumberOfPages - 1 ? "Continue" : "Get Started";
                    EnableNextKey = x < NumberOfPages - 1;
                });

        this.WhenAnyValue(x => x.IsActive)
            .Skip(1)
            .Where(x => !x)
            .Subscribe(x => EnableNextKey = false);
    }

    public IObservable<bool> CanGoBack { get; }

    private void OnNext()
    {
        if (SelectedIndex < NumberOfPages - 1)
        {
            SelectedIndex++;
        }
        else if (!UiContext.UserRepository.HasUser)
        {
            Navigate().To().AddUserPage();
        }
        else
        {
            Close();
        }
    }
}