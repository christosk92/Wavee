using System.Collections;
using System.ComponentModel;
using ReactiveUI;
using Wavee.ViewModels.Models.UI;

namespace Wavee.ViewModels.ViewModels;

public class ViewModelBase : ReactiveObject, INotifyDataErrorInfo
{
    protected UiContext UiContext { get; set; } = UiContext.Default;
    
    //TODO: Error handling
    public IEnumerable GetErrors(string? propertyName)
    {
        throw new NotImplementedException();
    }

    public bool HasErrors { get; }
    public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;
}