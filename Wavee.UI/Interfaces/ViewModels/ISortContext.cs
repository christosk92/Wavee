namespace Wavee.UI.Interfaces.ViewModels
{
    public interface ISortContext
    {
        string? SortBy { get; set; }
        bool SortAscending { get; set; }

        event EventHandler<(string SortBy, bool SortAscending)> SortChanged;
        void DefaultSort();
    }
}
