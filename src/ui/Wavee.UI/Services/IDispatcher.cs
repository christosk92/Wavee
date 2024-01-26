namespace Wavee.UI.Services;

public interface IDispatcher
{
    void Dispatch(Action action, bool highPriority = false);
}