using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using Microsoft.Win32;
using Serilog;

namespace Wavee.ViewModels.Service;

public class TerminateService
{
	private const long TerminateStatusNotStarted = 0;
	private const long TerminateStatusInProgress = 1;
	private const long TerminateStatusFinished = 2;
	private readonly Func<Task> _terminateApplicationAsync;
	private readonly Action _terminateApplication;
	private long _terminateStatus;

	public TerminateService(Func<Task> terminateApplicationAsync, Action terminateApplication)
	{
		_terminateApplicationAsync = terminateApplicationAsync;
		_terminateApplication = terminateApplication;
		IsSystemEventsSubscribed = false;
		CancellationToken = _terminationCts.Token;
		Instance = this;
	}

	public static TerminateService? Instance { get; private set; }

	/// <summary>Completion source that is completed once we receive a request to terminate the application in a graceful way.</summary>
	/// <remarks>Currently, we handle CTRL+C this way. However, for example, an RPC command might use this API too.</remarks>
	private readonly TaskCompletionSource _forcefulTerminationRequested = new(TaskCreationOptions.RunContinuationsAsynchronously);

	/// <summary>Task is set, if user requested the application to stop in a "forceful" way (e.g. CTRL+C or by the stop RPC request).</summary>
	public Task ForcefulTerminationRequestedTask => _forcefulTerminationRequested.Task;

	/// <summary>Cancellation token source cancelled once <see cref="_forcefulTerminationRequested"/> is assigned a result.</summary>
	private readonly CancellationTokenSource _terminationCts = new();

	/// <summary>Cancellation token that denotes that user requested to stop the application.</summary>
	/// <remarks>Assigned once so that there are no issues with <see cref="_terminationCts"/> being disposed.</remarks>
	public CancellationToken CancellationToken { get; }

	private bool IsSystemEventsSubscribed { get; set; }

	/// <summary>In case of an unrecoverable exception, SignalGracefulCrash will store here the exception to pass down to the CrashReporter.</summary>
	public Exception? GracefulCrashException { get; private set; }

	public void Activate()
	{
		AppDomain.CurrentDomain.ProcessExit += CurrentDomain_ProcessExit;
		Console.CancelKeyPress += Console_CancelKeyPress;
		AssemblyLoadContext.Default.Unloading += Default_Unloading;
		AppDomain.CurrentDomain.DomainUnload += CurrentDomain_DomainUnload;

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && !Debugger.IsAttached)
		{
			// If the debugger is attached and you subscribe to SystemEvents, then on quit Wavee gracefully stops but never returns from console.
			Log.Debug($"{nameof(TerminateService)} subscribed to SystemEvents");
			SystemEvents.SessionEnding += Windows_SystemEvents_SessionEnding;
			IsSystemEventsSubscribed = true;
		}
	}

	private void CurrentDomain_DomainUnload(object? sender, EventArgs e)
	{
		Log.Information("Process domain unloading requested by the OS.");
		Terminate();
	}

	private void Default_Unloading(AssemblyLoadContext obj)
	{
		Log.Information("Process context unloading requested by the OS.");
		Terminate();
	}

	private void Windows_SystemEvents_SessionEnding(object sender, SessionEndingEventArgs e)
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			// This event will only be triggered if you run Wavee from the published package.
			Log.Information($"Process termination was requested by the OS, reason '{e.Reason}'.");
			e.Cancel = true;
		}

		// This must be a blocking call because after this the OS will terminate the Wavee process if it exists.
		// The process will be killed by the OS after ~7 seconds, even with e.Cancel = true.
		Terminate();
	}

	private void CurrentDomain_ProcessExit(object? sender, EventArgs e)
	{
		Log.Debug("ProcessExit was called.");

		// This must be a blocking call because after this the OS will terminate Wavee process if exists.
		Terminate();
	}

	private void Console_CancelKeyPress(object? sender, ConsoleCancelEventArgs e)
	{
		if (_forcefulTerminationRequested.Task.IsCompleted)
		{
			Log.Warning("Multiple requests to terminate registered. Stopping the application non-gracefully.");
			e.Cancel = false;
		}
		else
		{
			Log.Warning($"Process termination was requested using '{e.SpecialKey}' keyboard shortcut.");

			// Do not kill the process ...
			e.Cancel = true;

			// ... instead signal back that the app should terminate.
			SignalForceTerminate();
		}
	}

	public void SignalGracefulCrash(Exception ex)
	{
		GracefulCrashException = ex;
		SignalForceTerminate();
	}

	public void SignalForceTerminate()
	{
		if (_forcefulTerminationRequested.TrySetResult())
		{
			_terminationCts.Cancel();
			_terminationCts.Dispose();

			// Run this callback just once.
			_terminateApplication();
		}
	}

	/// <summary>
	/// Terminates the application.
	/// </summary>
	/// <remark>This is a blocking method.</remark>
	public void Terminate()
	{
		var prevValue = Interlocked.CompareExchange(ref _terminateStatus, TerminateStatusInProgress, TerminateStatusNotStarted);
		Log.Debug($"Terminate was called from ThreadId: {Environment.CurrentManagedThreadId}");
		if (prevValue != TerminateStatusNotStarted)
		{
			// Secondary callers will be blocked until the end of the termination.
			while (Interlocked.Read(ref _terminateStatus) != TerminateStatusFinished)
			{
				Thread.Sleep(50);
			}
			return;
		}

		// First caller starts the terminate procedure.
		Log.Debug("Start shutting down the application.");

		// We want to call the callback once. Not multiple times.
		if (!_forcefulTerminationRequested.Task.IsCompleted)
		{
			_terminateApplication();
		}

		// Async termination has to be started on another thread otherwise there is a possibility of deadlock.
		// We still need to block the caller so Wait applied.
		Task.Run(async () =>
		{
			try
			{
				await _terminateApplicationAsync().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				Log.Error(ex, "Error during termination.");
			}
		}).Wait();

		AppDomain.CurrentDomain.ProcessExit -= CurrentDomain_ProcessExit;
		Console.CancelKeyPress -= Console_CancelKeyPress;
		AssemblyLoadContext.Default.Unloading -= Default_Unloading;
		AppDomain.CurrentDomain.DomainUnload -= CurrentDomain_DomainUnload;

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && IsSystemEventsSubscribed)
		{
			SystemEvents.SessionEnding -= Windows_SystemEvents_SessionEnding;
		}

		// Indicate that the termination procedure finished. So other callers can return.
		Interlocked.Exchange(ref _terminateStatus, TerminateStatusFinished);

		Log.Information("Stopped the application.");
	}
}
