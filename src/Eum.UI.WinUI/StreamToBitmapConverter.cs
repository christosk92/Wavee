using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI.Core;
using CommunityToolkit.WinUI;
using Microsoft.UI.Dispatching;

namespace Eum.UI.WinUI;

public class StreamToBitmapConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is Stream input)
        {
            var task = Task.Run(async () =>
            {
                BitmapImage img = default;
                using var mem = new MemoryStream();
                await input.CopyToAsync(mem);
                mem.Position = 0;
                await App.MWindow.DispatcherQueue.EnqueueAsync(async () =>
                {
                    img = new BitmapImage();
                    await img.SetSourceAsync(mem.AsRandomAccessStream());
                }, DispatcherQueuePriority.High);
                return img;
            });
            return new TaskCompletionNotifier<BitmapImage>(task);
        }

        return null;
    }

    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
public interface ITaskCompletionNotifier : INotifyPropertyChanged
{
    /// <summary>
    /// Gets the task being watched. This property never changes and is never <c>null</c>.
    /// </summary>
    Task Task { get; }

    /// <summary>
    /// Gets the current task status. This property raises a notification when the task completes.
    /// </summary>
    TaskStatus Status { get; }

    /// <summary>
    /// Gets whether the task has completed. This property raises a notification when the value changes to <c>true</c>.
    /// </summary>
    bool IsCompleted { get; }

    /// <summary>
    /// Gets whether the task has completed successfully. This property raises a notification when the value changes to <c>true</c>.
    /// </summary>
    bool IsSuccessfullyCompleted { get; }

    /// <summary>
    /// Gets whether the task has been canceled. This property raises a notification only if the task is canceled (i.e., if the value changes to <c>true</c>).
    /// </summary>
    bool IsCanceled { get; }

    /// <summary>
    /// Gets whether the task has faulted. This property raises a notification only if the task faults (i.e., if the value changes to <c>true</c>).
    /// </summary>
    bool IsFaulted { get; }

    /// <summary>
    /// Gets the wrapped faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
    /// </summary>
    AggregateException Exception { get; }

    /// <summary>
    /// Gets the original faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
    /// </summary>
    Exception InnerException { get; }

    /// <summary>
    /// Gets the error message for the original faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
    /// </summary>
    string ErrorMessage { get; }
}

public interface ITaskCompletionNotifier<TResult> : ITaskCompletionNotifier
{
    /// <summary>
    /// Gets the task being watched. This property never changes and is never <c>null</c>.
    /// </summary>
    new Task<TResult> Task { get; }

    /// <summary>
    /// Gets the result of the task. Returns the default value of <typeparamref name="TResult"/> if the task has not completed successfully. This property raises a notification when the task completes successfully.
    /// </summary>
    TResult Result { get; }
}

/// <summary>
/// Watches a task and raises property-changed notifications when the task completes.
/// </summary>
/// <typeparam name="TResult">The type of the task result.</typeparam>
public sealed class TaskCompletionNotifier<TResult> : ITaskCompletionNotifier<TResult>
{
    /// <summary>
    /// Initializes a task notifier watching the specified task.
    /// </summary>
    /// <param name="task">The task to watch.</param>
    public TaskCompletionNotifier(Task<TResult> task)
    {
        Task = task;
        if (!task.IsCompleted)
        {
            var scheduler = (SynchronizationContext.Current == null) ? TaskScheduler.Current : TaskScheduler.FromCurrentSynchronizationContext();
            task.ContinueWith(t =>
            {
                var propertyChanged = PropertyChanged;
                if (propertyChanged != null)
                {
                    propertyChanged(this, new PropertyChangedEventArgs("Status"));
                    propertyChanged(this, new PropertyChangedEventArgs("IsCompleted"));
                    if (t.IsCanceled)
                    {
                        propertyChanged(this, new PropertyChangedEventArgs("IsCanceled"));
                    }
                    else if (t.IsFaulted)
                    {
                        propertyChanged(this, new PropertyChangedEventArgs("IsFaulted"));
                        propertyChanged(this, new PropertyChangedEventArgs("Exception"));
                        propertyChanged(this, new PropertyChangedEventArgs("InnerException"));
                        propertyChanged(this, new PropertyChangedEventArgs("ErrorMessage"));
                    }
                    else
                    {
                        propertyChanged(this, new PropertyChangedEventArgs("IsSuccessfullyCompleted"));
                        propertyChanged(this, new PropertyChangedEventArgs("Result"));
                    }
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            scheduler);
        }
    }

    /// <summary>
    /// Gets the task being watched. This property never changes and is never <c>null</c>.
    /// </summary>
    public Task<TResult> Task { get; private set; }

    Task ITaskCompletionNotifier.Task
    {
        get { return Task; }
    }

    /// <summary>
    /// Gets the result of the task. Returns the default value of <typeparamref name="TResult"/> if the task has not completed successfully. This property raises a notification when the task completes successfully.
    /// </summary>
    public TResult Result { get { return (Task.Status == TaskStatus.RanToCompletion) ? Task.Result : default(TResult); } }

    /// <summary>
    /// Gets the current task status. This property raises a notification when the task completes.
    /// </summary>
    public TaskStatus Status { get { return Task.Status; } }

    /// <summary>
    /// Gets whether the task has completed. This property raises a notification when the value changes to <c>true</c>.
    /// </summary>
    public bool IsCompleted { get { return Task.IsCompleted; } }

    /// <summary>
    /// Gets whether the task has completed successfully. This property raises a notification when the value changes to <c>true</c>.
    /// </summary>
    public bool IsSuccessfullyCompleted { get { return Task.Status == TaskStatus.RanToCompletion; } }

    /// <summary>
    /// Gets whether the task has been canceled. This property raises a notification only if the task is canceled (i.e., if the value changes to <c>true</c>).
    /// </summary>
    public bool IsCanceled { get { return Task.IsCanceled; } }

    /// <summary>
    /// Gets whether the task has faulted. This property raises a notification only if the task faults (i.e., if the value changes to <c>true</c>).
    /// </summary>
    public bool IsFaulted { get { return Task.IsFaulted; } }

    /// <summary>
    /// Gets the wrapped faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
    /// </summary>
    public AggregateException Exception { get { return Task.Exception; } }

    /// <summary>
    /// Gets the original faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
    /// </summary>
    public Exception InnerException { get { return (Exception == null) ? null : Exception.InnerException; } }

    /// <summary>
    /// Gets the error message for the original faulting exception for the task. Returns <c>null</c> if the task is not faulted. This property raises a notification only if the task faults (i.e., if the value changes to non-<c>null</c>).
    /// </summary>
    public string ErrorMessage { get { return (InnerException == null) ? null : InnerException.Message; } }

    /// <summary>
    /// Occurs when property values change due to task completion.
    /// </summary>
    public event PropertyChangedEventHandler PropertyChanged;
}