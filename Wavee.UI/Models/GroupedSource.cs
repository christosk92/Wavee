using System.Collections.ObjectModel;

namespace Wavee.UI.Models;

public sealed class GroupedSource : ObservableCollection<object>
{
    public GroupedSource(IEnumerable<object> items) : base(items)
    {
        
    }

    public string Key
    {
        get;
        init;
    }

    public string? Title
    {
        get;
        init;
    }
}