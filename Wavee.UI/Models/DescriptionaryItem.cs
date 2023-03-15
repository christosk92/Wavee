using Wavee.UI.ViewModels.Artist;

namespace Wavee.UI.Models;

public readonly record struct ArtistDescriptionaryItem(string Title, object Value) : IDescriptionaryItem
{
    public Type NavigateTo => typeof(ArtistRootViewModel);
}

public interface IDescriptionaryItem
{
    string Title
    {
        get;
    }
    Type NavigateTo
    {
        get;
    }
    object Value
    {
        get;
    }
}