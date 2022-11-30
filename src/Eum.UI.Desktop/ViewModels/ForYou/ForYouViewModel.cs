using Eum.UI.ViewModels.Sidebar;

namespace Eum.UI.ViewModels.ForYou
{
    internal class ForYouViewModel : SidebarItemViewModel
    {
        public override string Title { get; protected set; } = "Feed";
        public override string Glyph => "\uE794";
    }
}
