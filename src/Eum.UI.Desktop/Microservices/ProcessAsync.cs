using System.Diagnostics;
using Eum.Logging;

namespace Eum.UI.Microservices;

/// <summary>
/// Async wrapper class for <see cref="System.Diagnostics.Process"/> class that implements <see cref="WaitForExitAsync(CancellationToken, bool)"/>
/// to asynchronously wait for a process to exit.
/// </summary>
public class ProcessAsync : IDisposable
{
	/// <summary>
	/// To detect redundant calls.
	/// </summary>
	private bool _disposed = false;

	public ProcessAsync(ProcessStartInfo startInfo) : this(new Process() { StartInfo = startInfo })
	{
	}

	internal ProcessAsync(Process process)
	{
		Process = process;
	}

	private Process Process { get; }

	/// <inheritdoc cref="Process.StartInfo"/>
	public ProcessStartInfo StartInfo => Process.StartInfo;

	/// <inheritdoc cref="Process.ExitCode"/>
	public int ExitCode => Process.ExitCode;

	/// <inheritdoc cref="Process.HasExited"/>
	public virtual bool HasExited => Process.HasExited;

	/// <inheritdoc cref="Process.Id"/>
	public int Id => Process.Id;

	/// <inheritdoc cref="Process.StandardInput"/>
	public StreamWriter StandardInput => Process.StandardInput;

	/// <inheritdoc cref="Process.StandardOutput"/>
	public StreamReader StandardOutput => Process.StandardOutput;

	/// <inheritdoc cref="Process.Start()"/>
	public void Start()
	{
		try
		{
			Process.Start();
		}
		catch (Exception ex)
		{
			S_Log.Instance.LogError(ex);

			S_Log.Instance.LogInfo($"{nameof(Process.StartInfo.FileName)}: {Process.StartInfo.FileName}.");
			S_Log.Instance.LogInfo($"{nameof(Process.StartInfo.Arguments)}: {Process.StartInfo.Arguments}.");
			S_Log.Instance.LogInfo($"{nameof(Process.StartInfo.RedirectStandardOutput)}: {Process.StartInfo.RedirectStandardOutput}.");
			S_Log.Instance.LogInfo($"{nameof(Process.StartInfo.UseShellExecute)}: {Process.StartInfo.UseShellExecute}.");
			S_Log.Instance.LogInfo($"{nameof(Process.StartInfo.CreateNoWindow)}: {Process.StartInfo.CreateNoWindow}.");
			S_Log.Instance.LogInfo($"{nameof(Process.StartInfo.WindowStyle)}: {Process.StartInfo.WindowStyle}.");
			throw;
		}
	}

	/// <inheritdoc cref="Process.Kill()"/>
	public virtual void Kill()
	{
		Process.Kill();
	}

	/// <summary>
	/// Waits until the process either finishes on its own or when user cancels the action.
	/// </summary>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <param name="killOnCancel">If <c>true</c> the process will be killed (with entire process tree) when this asynchronous action is canceled via <paramref name="cancellationToken"/> token.</param>
	/// <returns><see cref="Task"/>.</returns>
	public virtual async Task WaitForExitAsync(CancellationToken cancellationToken, bool killOnCancel = false)
	{
		if (Process.HasExited)
		{
			S_Log.Instance.LogTrace("Process has already exited.");
			return;
		}

		try
		{
			S_Log.Instance.LogTrace($"Wait for the process to exit: '{Process.Id}'");
			await Process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

			S_Log.Instance.LogTrace("Process has exited.");
		}
		catch (OperationCanceledException ex)
		{
			S_Log.Instance.LogTrace("User canceled waiting for process exit.");

			if (killOnCancel)
			{
				if (!Process.HasExited)
				{
					try
					{
						S_Log.Instance.LogTrace("Kill process.");
						Process.Kill(entireProcessTree: true);
					}
					catch (Exception e)
					{
						S_Log.Instance.LogError($"Could not kill process: {e}.");
					}
				}
			}

			throw new TaskCanceledException("Waiting for process exiting was canceled.", ex, cancellationToken);
		}
	}

	// Protected implementation of Dispose pattern.
	protected virtual void Dispose(bool disposing)
	{
		if (_disposed)
		{
			return;
		}

		if (disposing)
		{
			// Dispose managed state (managed objects).
			Process.Dispose();
		}

		_disposed = true;
	}

	public virtual void Dispose()
	{
		// Dispose of unmanaged resources.
		Dispose(true);

		// Suppress finalization.
		GC.SuppressFinalize(this);
	}
}
