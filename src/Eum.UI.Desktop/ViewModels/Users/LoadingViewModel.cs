using System.Diagnostics;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using Eum.UI.Users;
using Eum.Users;
using ReactiveUI;

namespace Eum.UI.ViewModels.Users;

public partial class LoadingViewModel : ActivatableViewModel
{
	private readonly EumUser _user;
	
	private Stopwatch? _stopwatch;
	private volatile bool _isLoading;

	public LoadingViewModel(EumUser user)
	{
		_user = user;
		//
		// Services.Synchronizer.WhenAnyValue(x => x.BackendStatus)
		// 	.Where(status => status == BackendStatus.Connected)
		// 	.SubscribeAsync(async _ => await LoadWalletAsync(isBackendAvailable: true).ConfigureAwait(false));

		RxApp.TaskpoolScheduler.Schedule(async () =>
		{
			await LoadUserAsync(isBackendAvailable: false).ConfigureAwait(false);
		});

		/*Observable.FromEventPattern<bool>(Services.Synchronizer, nameof(Services.Synchronizer.ResponseArrivedIsGenSocksServFail))
			.SubscribeAsync(async _ =>
			{
				if (Services.Synchronizer.BackendStatus == BackendStatus.Connected)
				{
					return;
				}

				await LoadWalletAsync(isBackendAvailable: false).ConfigureAwait(false);
			});*/
	}

	public string UserId => _user.UserId;
	
	protected override void OnActivated(CompositeDisposable disposables)
	{
		base.OnActivated(disposables);
		
		_stopwatch ??= Stopwatch.StartNew();

		/*
		Observable.Interval(TimeSpan.FromSeconds(1))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(_ =>
			{
				UpdateStatus();
			})
			.DisposeWith(disposables);*/
	}
	

	private async Task LoadUserAsync(bool isBackendAvailable)
	{
		if (_isLoading)
		{
			return;
		}

		_isLoading = true;
		

		//TODO:
		//await UiServices.WalletManager.LoadWalletAsync(_wallet).ConfigureAwait(false);
	}

}
