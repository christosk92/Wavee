using Wavee.UI.ViewModels.Libray;

namespace Wavee.UI.Interfaces.ViewModels
{
    public interface ISortContext
    {
        SortOption SortBy { get; set; }
        bool SortAscending { get; set; }

        event EventHandler<(SortOption SortBy, bool SortAscending)>? SortChanged;
        void DefaultSort();
    }
}
