namespace Wavee.UI.ViewModels.Shell;

public abstract class SidebarItemViewModel
{
    public abstract string Title { get; }
    public abstract string Icon { get; }
    public abstract string GlyphFontFamily { get; }
    public bool IsEnabled { get; init; } = true;
    public abstract string Id { get; }

    public abstract void NavigateTo();
}

public class SidebarHeader : SidebarItemViewModel
{
    public SidebarHeader(string header)
    {
        Title = header;
    }
    public override string Title { get; }
    public override string Icon { get; }
    public override string GlyphFontFamily { get; }
    public override string Id { get; }
    public override void NavigateTo()
    {
        throw new NotSupportedException();
    }
}