using Eum.UI.ViewModels.Sidebar;

namespace Eum.UI.ViewModels.ForYou
{
    public sealed class HomeViewModel : SidebarItemViewModel
    {
        public override string Title { get; protected set; } = "Home";
        public override string Glyph { get; } = "\uE10F";
    }
}
