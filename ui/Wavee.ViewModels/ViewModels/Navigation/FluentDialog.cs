using Wavee.ViewModels.ViewModels.Dialogs.Base;

namespace Wavee.ViewModels.ViewModels.Navigation;

public class FluentDialog<TResult>
{
    private readonly Task<DialogResult<TResult>> _resultTask;

    public FluentDialog(Task<DialogResult<TResult>> resultTask)
    {
        _resultTask = resultTask;
    }

    public async Task<TResult?> GetResultAsync()
    {
        var result = await _resultTask;

        return result.Result;
    }
}