namespace Wavee.UI.Test;

public interface IUIDispatcher
{
    void Invoke(Action action);
    Task InvokeAsync(Func<Task> func);
}