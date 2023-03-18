namespace Wavee.UI.ViewModels.Shell.Sidebar
{
    public interface ISidebarItem
    {
        string Content { get; }
        Type NavigateTo { get; }
    }
}
