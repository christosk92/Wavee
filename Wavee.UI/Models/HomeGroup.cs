using System.Collections.ObjectModel;

namespace Wavee.UI.Models
{
    public class HomeGroup : ObservableCollection<object>
    {
        public HomeGroup(IEnumerable<object> items) : base(items)
        {

        }
        public string Title { get; init; }
        public string TagLine { get; init; }
        public GroupRenderType RenderType { get; init; }
    }
}
