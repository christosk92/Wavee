using Wavee.UI.Models;
using Wavee.UI.Navigation;

namespace Wavee.UI.ViewModels.ForYou.Recommended;

public class LocalRecommendedViewModel : INavigatable
{
    public LocalRecommendedViewModel()
    {
        Items = new List<HomeGroup>
        {
            new HomeGroup(new List<object>())
            {
                Title = "You recently listened to",
                TagLine = "Pick off where you left off.",
                RenderType = GroupRenderType.HorizontalFlow
            },
            new HomeGroup(new List<object>())
            {
                Title = "Your #1 lately",
                TagLine = "You've been on a streak lately with this album!",
                RenderType = GroupRenderType.SingleView
            },
            new HomeGroup(new List<object>())
            {
                Title = "Your #1 artist",
                TagLine = "You've listened to this artist more than any other!",
                RenderType = GroupRenderType.SingleView
            },
            new HomeGroup(new List<object>())
            {
                Title = "Your top tracks",
                TagLine = "You've listened to these tracks more often than others!",
                RenderType = GroupRenderType.TrackList
            },
            // new HomeGroup(new List<object>())
            // {
            //     Title = "Your #1 artist",
            //     TagLine = "You've listened to this artist more than any other!",
            //     RenderType = GroupRenderType.SingleView
            // },
        };
    }

    public void OnNavigatedTo(object parameter)
    {

    }

    public void OnNavigatedFrom()
    {

    }

    public int MaxDepth { get; }
    public IEnumerable<HomeGroup> Items { get; }
}