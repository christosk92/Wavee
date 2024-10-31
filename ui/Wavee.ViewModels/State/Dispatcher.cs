using System.Reactive.Concurrency;
using ReactiveUI;

namespace Wavee.ViewModels.State;

public static class Dispatcher
{
    public static class UIThread
    {
        public static void Post(Action action, DispatcherPriority background)
        {
            RxApp.MainThreadScheduler.Schedule(action);
            //action();
        }
    }
}

public enum DispatcherPriority
{
    Background
}