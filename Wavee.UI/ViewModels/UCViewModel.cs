using Wavee.UI.ViewModels.Shell;

namespace Wavee.UI.ViewModels
{
    public class UCViewModel : SidebarItemViewModel
    {
        public override string Title { get; }
        public override string Icon { get; }
        public override string GlyphFontFamily { get; }
        public override string Id { get; }

        public override void NavigateTo()
        {
            throw new NotImplementedException();
        }
    }
}
