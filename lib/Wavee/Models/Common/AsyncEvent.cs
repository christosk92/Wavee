namespace Wavee.Models.Common;

public class AsyncEvent<TEventArgs> where TEventArgs : EventArgs
{
    private readonly List<Func<object, TEventArgs, Task>> invocationList;
    private readonly object locker;

    private AsyncEvent()
    {
        invocationList = new List<Func<object, TEventArgs, Task>>();
        locker = new object();
    }

    public static AsyncEvent<TEventArgs> operator +(
        AsyncEvent<TEventArgs> e, Func<object, TEventArgs, Task> callback)
    {
        if (callback == null) throw new NullReferenceException("callback is null");

        //Note: Thread safety issue- if two threads register to the same event (on the first time, i.e when it is null)
        //they could get a different instance, so whoever was first will be overridden.
        //A solution for that would be to switch to a public constructor and use it, but then we'll 'lose' the similar syntax to c# events             
        if (e == null) e = new AsyncEvent<TEventArgs>();

        lock (e.locker)
        {
            e.invocationList.Add(callback);
        }
        return e;
    }

    public static AsyncEvent<TEventArgs> operator -(
        AsyncEvent<TEventArgs> e, Func<object, TEventArgs, Task> callback)
    {
        if (callback == null) throw new NullReferenceException("callback is null");
        if (e == null) return null;

        lock (e.locker)
        {
            e.invocationList.Remove(callback);
        }
        return e;
    }

    public async Task InvokeAsync(object sender, TEventArgs eventArgs)
    {
        List<Func<object, TEventArgs, Task>> tmpInvocationList;
        lock (locker)
        {
            tmpInvocationList = new List<Func<object, TEventArgs, Task>>(invocationList);
        }

        foreach (var callback in tmpInvocationList)
        {
            //Assuming we want a serial invocation, for a parallel invocation we can use Task.WhenAll instead
            await callback(sender, eventArgs);
        }
    }
}