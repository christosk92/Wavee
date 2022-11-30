using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Reactive.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using DynamicData;
using DynamicData.Binding;
using Eum.UI.Users;
using Eum.UI.ViewModels.NavBar;
using Eum.UI.ViewModels.Users;
using Eum.Users;
using ReactiveUI;

namespace Eum.UI.ViewModels;

[INotifyPropertyChanged]
public partial class UserManagerViewModel
{
    private readonly SourceList<UserViewModelBase> _usersSourceList = new();
    private readonly ObservableCollectionExtended<UserViewModelBase> _users = new();

    private NavBarItemViewModel? _currentSelection;
    [ObservableProperty] private UserViewModelBase? _selectedUser;

    private UserManager _usersManager;
    //[AutoNotify(SetterModifier = AccessModifier.Private)] private bool _isLoadingWallet;

    public UserManagerViewModel(UserManager usersManager)
    {
        _usersManager = usersManager;
        _usersSourceList
            .Connect()
            .Sort(SortExpressionComparer<UserViewModelBase>.Descending(i => i.IsLoggedIn)
                .ThenByAscending(i => i.Title))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(_users)
            .Subscribe();
        Observable
            .FromEventPattern<bool>(usersManager, nameof(UserManager.IsDefaultChanged))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Select(x => x.Sender as EumUser)
            .WhereNotNull()
            .Subscribe(user =>
            {
                if (!TryGetUserViewModel(user, out var userViewModel))
                {
                    return;
                }

                if (user.IsDefault)
                {
                    foreach (var eumUser in _usersManager.GetUsers(false))
                    {
                        if (eumUser.UserId != user.UserId)
                        {
                            eumUser.IsDefault = false;
                        }
                    }
                }
            });


        /*// Observable
		// 	.FromEventPattern<WalletState>(Services.WalletManager, nameof(WalletManager.WalletStateChanged))
		// 	.ObserveOn(RxApp.MainThreadScheduler)
		// 	.Select(x => x.Sender as Wallet)
		// 	.WhereNotNull()
		// 	.Subscribe(wallet =>
		// 	{
		// 		if (!TryGetWalletViewModel(wallet, out var walletViewModel))
		// 		{
		// 			return;
		// 		}
		//
		// 		if (wallet.State == WalletState.Stopping)
		// 		{
		// 			RemoveWallet(walletViewModel);
		// 		}
		// 		else if (walletViewModel is ClosedWalletViewModel { IsLoggedIn: true } cwvm &&
		// 				 ((cwvm.Wallet.KeyManager.SkipSynchronization && cwvm.Wallet.State == WalletState.Starting) ||
		// 				  cwvm.Wallet.State == WalletState.Started))
		// 		{
		// 			OpenClosedWallet(cwvm);
		// 		}
		// 	});
		*/

        Observable
            .FromEventPattern<EumUser>(_usersManager, nameof(UserManager.UserAdded))
            .Select(x => x.EventArgs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(wallet =>
            {
                UserViewModelBase vm = UserViewModelBase.Create(wallet);

                _usersSourceList.Add(vm);
            });

        Observable
            .FromEventPattern<EumUser>(_usersManager, nameof(UserManager.UserRemoved))
            .Select(x => x.EventArgs)
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe(wallet =>
            {
                if (TryGetUserViewModel(wallet, out var toRemoveUser))
                {
                    _usersSourceList.Remove(toRemoveUser);
                }
            });

        // Observable
        // 	.FromEventPattern<ProcessedResult>(Services.WalletManager, nameof(Services.WalletManager.WalletRelevantTransactionProcessed))
        // 	.ObserveOn(RxApp.MainThreadScheduler)
        // 	.SubscribeAsync(async arg =>
        // 	{
        // 		var (sender, e) = arg;
        //
        // 		if (Services.UiConfig.PrivacyMode ||
        // 			!e.IsNews ||
        // 			sender is not Wallet { IsLoggedIn: true, State: WalletState.Started } wallet)
        // 		{
        // 			return;
        // 		}
        //
        // 		if (TryGetWalletViewModel(wallet, out var walletViewModel) && walletViewModel is WalletViewModel wvm)
        // 		{
        // 			if (!e.IsOwnCoinJoin)
        // 			{
        // 				NotificationHelpers.Show(wallet.WalletName, e, onClick: () =>
        // 				{
        // 					if (MainViewModel.Instance.IsBusy)
        // 					{
        // 						return;
        // 					}
        //
        // 					wvm.NavigateAndHighlight(e.Transaction.GetHash());
        // 				});
        // 			}
        //
        // 			if (wvm.IsSelected && (e.NewlyReceivedCoins.Any() || e.NewlyConfirmedReceivedCoins.Any()))
        // 			{
        // 				await Task.Delay(200);
        // 				wvm.History.SelectTransaction(e.Transaction.GetHash());
        // 			}
        // 		}
        // 	});

        EnumerateUsers();
    }

    public event EventHandler<UserViewModelBase?> UserSelected;
    public ObservableCollection<UserViewModelBase> Users => _users;

    // public UserViewModelBase GetWalletViewModel(Wallet wallet)
    // {
    // 	if (TryGetWalletViewModel(wallet, out var walletViewModel) && walletViewModel is WalletViewModel result)
    // 	{
    // 		return result;
    // 	}
    //
    // 	throw new Exception("Wallet not found, invalid api usage");
    // }
    //
    // public async Task LoadWalletAsync(Wallet wallet)
    // {
    // 	if (wallet.State != WalletState.Uninitialized)
    // 	{
    // 		throw new Exception("Wallet is already being logged in.");
    // 	}
    //
    // 	try
    // 	{
    // 		await Task.Run(async () => await Services.WalletManager.StartWalletAsync(wallet));
    // 	}
    // 	catch (OperationCanceledException ex)
    // 	{
    // 		S_Log.Instance.LogTrace(ex);
    // 	}
    // 	catch (Exception ex)
    // 	{
    // 		S_Log.Instance.LogError(ex);
    // 	}
    // }
    //
    // private void OpenClosedWallet(ClosedWalletViewModel closedWalletViewModel)
    // {
    // 	IsLoadingWallet = true;
    //
    // 	RemoveWallet(closedWalletViewModel);
    //
    // 	var walletViewModelItem = OpenWallet(closedWalletViewModel.Wallet);
    //
    // 	if (_currentSelection == closedWalletViewModel)
    // 	{
    // 		SelectedWallet = walletViewModelItem;
    // 	}
    //
    // 	IsLoadingWallet = false;
    // }
    //
    // private WalletViewModel OpenWallet(Wallet wallet)
    // {
    // 	if (Wallets.Any(x => x.Title == wallet.WalletName))
    // 	{
    // 		throw new Exception("Wallet already opened.");
    // 	}
    //
    // 	var walletViewModel = WalletViewModel.Create(wallet);
    //
    // 	InsertWallet(walletViewModel);
    //
    // 	return walletViewModel;
    // }
    //
    // private void InsertWallet(WalletViewModelBase wallet)
    // {
    // 	_walletsSourceList.Add(wallet);
    // }
    //
    // private void RemoveWallet(WalletViewModelBase walletViewModel)
    // {
    // 	_walletsSourceList.Remove(walletViewModel);
    // }
    //
    private void EnumerateUsers()
    {
        foreach (var wallet in _usersManager.GetUsers())
        {
            _usersSourceList.Add(UserViewModelBase.Create(wallet));
        }
    }

    public void SetSelectedUser(UserViewModelBase item)
    {
        if (item == null)
        {
            SelectedUser = null;
            UserSelected?.Invoke(this, null);
        }

        var result = default(UserViewModelBase);
        if (item is UserViewModelBase { IsLoggedIn: true } userViewModelBase)
        {
            SelectedUser = userViewModelBase;
            result = item;
            UserSelected?.Invoke(this, result);
        }
    }

    //
    // public NavBarItemViewModel? SelectionChanged(NavBarItemViewModel item)
    // {
    // 	if (item.SelectionMode == NavBarItemSelectionMode.Selected)
    // 	{
    // 		_currentSelection = item;
    // 	}
    //
    // 	if (IsLoadingWallet || SelectedWallet == item)
    // 	{
    // 		return default;
    // 	}
    //
    // 	var result = default(NavBarItemViewModel);
    //
    // 	if (SelectedWallet is { IsLoggedIn: true } && item is WalletViewModelBase && SelectedWallet != item)
    // 	{
    // 		SelectedWallet = null;
    // 		result = item;
    // 	}
    //
    // 	if (item is WalletViewModel { IsLoggedIn: true } walletViewModelItem)
    // 	{
    // 		SelectedWallet = walletViewModelItem;
    // 		result = item;
    // 	}
    //
    // 	return result;
    // }
    //
    private bool TryGetUserViewModel(EumUser user, [NotNullWhen(true)] out UserViewModelBase? userViewModel)
    {
        userViewModel = Users.FirstOrDefault(x => x.UserId == user.UserId);
        return userViewModel is { };
    }
}
