using System.ComponentModel;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using DynamicData.Binding;
using ReactiveUI;
using Serilog;
using Wavee.ViewModels.Enums;
using Wavee.ViewModels.Extensions;
using Wavee.ViewModels.Helpers;
using Wavee.ViewModels.Infrastructure;
using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.Models.EventArgs;
using Wavee.ViewModels.Models.UI;
using Wavee.ViewModels.Providers;
using Wavee.ViewModels.Service;
using Wavee.ViewModels.State;
using Wavee.ViewModels.ViewModels;

namespace Wavee.ViewModels;

[AppLifetime]
public class ApplicationStateManager : IMainWindowService
{
	private readonly StateMachine<State, Trigger> _stateMachine;
	private readonly IActivatableApplicationLifetime _lifetime;
	private CompositeDisposable? _compositeDisposable;
	private bool _hideRequest;
	private bool _isShuttingDown;
	private bool _restartRequest;
	private IActivatableApplicationLifetime? _activatable;
	private readonly IMainWindowFactory _mainWindowFactory;

	public ApplicationStateManager(
	    IMainWindowFactory mainWindowFactory,
        IActivatableApplicationLifetime lifetime, 
        UiContext uiContext,
        bool startInBg)
    {
        _lifetime = lifetime;
		_stateMachine = new StateMachine<State, Trigger>(State.InitialState);

		if (_lifetime is { } activatableLifetime)
		{
			if (startInBg)
			{
				Dispatcher.UIThread.Post(
					() =>
					{
						_activatable = activatableLifetime;
						activatableLifetime.TryEnterBackground();
						activatableLifetime.Activated += ActivatableLifetimeOnActivated;
						activatableLifetime.Deactivated += ActivatableLifetimeOnDeactivated;
					},
					DispatcherPriority.Background);
			}
			else
			{
				_activatable = activatableLifetime;
				activatableLifetime.Activated += ActivatableLifetimeOnActivated;
				activatableLifetime.Deactivated += ActivatableLifetimeOnDeactivated;
			}
		}

		UiContext = uiContext;
		_mainWindowFactory = mainWindowFactory;
		ApplicationViewModel = new ApplicationViewModel(UiContext, this);
		State initTransitionState = startInBg ? State.Closed : State.Open;

		Observable
			.FromEventPattern(Services.SingleInstanceChecker, nameof(SingleInstanceChecker.OtherInstanceStarted))
			.ObserveOn(RxApp.MainThreadScheduler)
			.Subscribe(_ => _stateMachine.Fire(Trigger.Show));

		_stateMachine.Configure(State.InitialState)
			.InitialTransition(initTransitionState)
			.OnTrigger(
				Trigger.ShutdownRequested,
				() =>
				{
					if (_restartRequest)
					{
						AppLifetimeHelper.StartAppWithArgs();
					}

					_lifetime.Shutdown();
				})
			.OnTrigger(
				Trigger.ShutdownPrevented,
				() =>
				{
					_lifetime.MainWindow.BringToFront();
					ApplicationViewModel.OnShutdownPrevented(_restartRequest);
					_restartRequest = false; // reset the value.
				});

		_stateMachine.Configure(State.Closed)
			.SubstateOf(State.InitialState)
			.OnEntry(() =>
			{
				_lifetime.MainWindow?.Close();
				_lifetime.MainWindow = null;
				ApplicationViewModel.IsMainWindowShown = false;
				if (_activatable is { })
				{
					_activatable.TryEnterBackground();
				}
			})
			.Permit(Trigger.Show, State.Open)
			.Permit(Trigger.ShutdownPrevented, State.Open);

		_stateMachine.Configure(State.Open)
			.SubstateOf(State.InitialState)
			.OnEntry(CreateAndShowMainWindow)
			.Permit(Trigger.Hide, State.Closed)
			.Permit(Trigger.MainWindowClosed, State.Closed)
			.OnTrigger(Trigger.Show, MainViewModel.Instance.ApplyUiConfigWindowState);

		_lifetime.ShutdownRequested += LifetimeOnShutdownRequested;

		_stateMachine.Start();
    }
    
    private enum Trigger
    {
	    Invalid = 0,
	    Hide,
	    Show,
	    ShutdownPrevented,
	    ShutdownRequested,
	    MainWindowClosed,
    }

    private enum State
    {
	    Invalid = 0,
	    InitialState,
	    Closed,
	    Open,
    }

    internal UiContext UiContext { get; }
    internal ApplicationViewModel ApplicationViewModel { get; }
    public IMainWindow ActualWindow => _lifetime.MainWindow;

    private void LifetimeOnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
	{
		// Shutdown prevention will only work if you directly run the executable.
		bool shouldShutdown = ApplicationViewModel.CanShutdown(_restartRequest, out bool isShutdownEnforced) || isShutdownEnforced;

		e.Cancel = !shouldShutdown;
		Log.Debug($"Cancellation of the shutdown set to: {e.Cancel}.");

		_stateMachine.Fire(shouldShutdown ? Trigger.ShutdownRequested : Trigger.ShutdownPrevented);
	}

	private void ActivatableLifetimeOnActivated(object? sender, ActivatedEventArgs e)
	{
		switch (e.Kind)
		{
			case ActivationKind.Background:
			case ActivationKind.Reopen:
				if (this is IMainWindowService service)
				{
					service.Show();
				}
				break;
		}
	}

	private void ActivatableLifetimeOnDeactivated(object? sender, ActivatedEventArgs e)
	{
		switch (e.Kind)
		{
			case ActivationKind.Background:
				if (this is IMainWindowService service)
				{
					if (_lifetime.MainWindow is not null)
					{
						service.Hide();
					}
				}
				break;
		}
	}

	private void CreateAndShowMainWindow()
	{
		if (_lifetime.MainWindow is { })
		{
			return;
		}

		MainViewModel.Instance.ApplyUiConfigWindowState();

		if (_lifetime is { } activatable)
		{
			activatable.TryLeaveBackground();
		}

		var result = _mainWindowFactory.Create();
		result.DataContext = MainViewModel.Instance;
		
		_compositeDisposable?.Dispose();
		_compositeDisposable = new();

		Observable.FromEventPattern<CancelEventArgs>(result, nameof(result.Closing))
			.Select(args => (args.EventArgs, !ApplicationViewModel.CanShutdown(false, out bool isShutdownEnforced), isShutdownEnforced))
			.TakeWhile(_ => !_isShuttingDown) // Prevents stack overflow.
			.Subscribe(tup =>
			{
				var (e, preventShutdown, isShutdownEnforced) = tup;

				// Check if Ctrl-C was used to forcefully terminate the app.
				if (isShutdownEnforced)
				{
					_isShuttingDown = true;
					tup.EventArgs.Cancel = false;
					_stateMachine.Fire(Trigger.ShutdownRequested);
				}

				// _hideRequest flag is used to distinguish what is the user's intent.
				// It is only true when the request comes from the Tray.
				if (Services.UiConfig.HideOnClose || _hideRequest)
				{
					_hideRequest = false; // request processed, set it back to the default.
					return;
				}

				_isShuttingDown = !preventShutdown;
				e.Cancel = preventShutdown;

				_stateMachine.Fire(preventShutdown ? Trigger.ShutdownPrevented : Trigger.ShutdownRequested);
			})
			.DisposeWith(_compositeDisposable);

		Observable.FromEventPattern(result, nameof(result.Closed))
			.Take(1)
			.Subscribe(_ =>
			{
				_compositeDisposable?.Dispose();
				_compositeDisposable = null;
				_stateMachine.Fire(Trigger.MainWindowClosed);
			})
			.DisposeWith(_compositeDisposable);

		_lifetime.MainWindow = result;

		if (result.WindowState != WindowState.Maximized)
		{
			SetWindowSize(result);
		}

		ObserveWindowSize(result, _compositeDisposable);

		result.Show();

		ApplicationViewModel.IsMainWindowShown = true;
	}

	private void SetWindowSize(IMainWindow window)
	{
		var configWidth = Services.UiConfig.WindowWidth;
		var configHeight = Services.UiConfig.WindowHeight;
		var currentScreen = window;

		if (configWidth is null || configHeight is null || currentScreen is null)
		{
			return;
		}

		var isValidWidth = configWidth <= currentScreen.Width && configWidth >= window.MinWidth;
		var isValidHeight = configHeight <= currentScreen.Height && configHeight >= window.MinHeight;

        window.Width = configWidth.Value;
        window.Height = configHeight.Value;
    }

	private void ObserveWindowSize(IMainWindow window, CompositeDisposable disposables)
	{
		window
			.WhenPropertyChanged(x=> x.Bounds)
			.Skip(1)
			.Where(b => b != default 
			            && window.WindowState == WindowState.Normal)
			.Subscribe(b =>
			{
				Services.UiConfig.WindowWidth = b.Value.Width;
				Services.UiConfig.WindowHeight = b.Value.Height;
			})
			.DisposeWith(disposables);
	}

	void IMainWindowService.Show()
	{
		_stateMachine.Fire(Trigger.Show);
	}

	void IMainWindowService.Hide()
	{
		_hideRequest = true;
		_stateMachine.Fire(Trigger.Hide);
	}

	void IMainWindowService.Shutdown(bool restart)
	{
		_restartRequest = restart;

		bool shouldShutdown = ApplicationViewModel.CanShutdown(_restartRequest, out bool isShutdownEnforced) || isShutdownEnforced;
		_stateMachine.Fire(shouldShutdown ? Trigger.ShutdownRequested : Trigger.ShutdownPrevented);
	}
}