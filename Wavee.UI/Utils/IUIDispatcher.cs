namespace Wavee.UI.Utils
{
    public interface IUiDispatcher
    {
        bool Dispatch(DispatcherQueuePriority priority, Action callback);
    }

    public enum DispatcherQueuePriority
    {
        /// <summary>**Low** priority work will be scheduled when there isn't any other work to process. Work at **Low** priority can be preempted by new incoming **High** and **Normal** priority tasks.</summary>
        Low = -10, // 0xFFFFFFF6

        /// <summary>Work will be dispatched once all **High** priority tasks are dispatched. If a new **High** priority work is scheduled, all new **High** priority tasks are processed before resuming **Normal** tasks. This is the default priority.</summary>
        Normal = 0,

        /// <summary>Work scheduled at **High** priority will be dispatched first, along with other **High** priority System tasks, before processing **Normal** or **Low** priority work.</summary>
        High = 10, // 0x0000000A
    }
}
