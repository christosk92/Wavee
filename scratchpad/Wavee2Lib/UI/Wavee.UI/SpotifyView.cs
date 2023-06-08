using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Text.Json;
using Eum.Spotify;
using Eum.Spotify.connectstate;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Ids;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;
using Wavee.Spotify.Infrastructure.Remote.Contracts;
using Wavee.UI.Models.Common;
using Wavee.UI.Models.Home;
using static LanguageExt.Prelude;
namespace Wavee.UI;

public static class SpotifyView
{
    public static async Task<State> LoginAsync(SpotifyConfig config, LoginCredentials credentials, CancellationToken ct)
    {
        var client = await SpotifyClient.CreateAsync(config, credentials);

        if (PlatformSpecificServices.SavePasswordToVaultForUserAction is not null)
            PlatformSpecificServices.SavePasswordToVaultForUserAction(
                client.WelcomeMessage.CanonicalUsername,
                client.WelcomeMessage.ReusableAuthCredentials.ToBase64()
            );

        return new State(client, config,
            user: new SpotifyUser(
                Id: client.WelcomeMessage.CanonicalUsername,
                DisplayName: client.WelcomeMessage.CanonicalUsername,
                ImageUrl: Option<string>.None
            )
        );
    }

    public static Aff<IReadOnlyCollection<HomeGroup>> GetHomeView(CancellationToken ct = default)
    {
        var state = State.Instance;
        const string types = "track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album";

        static IReadOnlyCollection<HomeGroup> ParseHome(JsonDocument home)
        {
            var groupResults = new List<HomeGroup>();
            if (home.RootElement.TryGetProperty("content", out var ct)
                && ct.TryGetProperty("items", out var items))
            {
                using var itemsArr = items.EnumerateArray();
                foreach (var group in itemsArr)
                {
                    var content = group.GetProperty("content");
                    using var itemsInGroup = content.GetProperty("items").EnumerateArray();
                    var result = new List<CardViewItem>();
                    foreach (var item in itemsInGroup)
                    {
                        var type = item.GetProperty("type").GetString();
                        string? image = null; //TODO: Total violation of FP
                        if (item.TryGetProperty("images", out var imgs))
                        {
                            using var images = imgs.EnumerateArray();
                            var artwork = LanguageExt.Seq<Artwork>.Empty;
                            foreach (var jsonImage in images)
                            {
                                var h = jsonImage.TryGetProperty("height", out var height)
                                        && height.ValueKind is JsonValueKind.Number
                                    ? height.GetInt32()
                                    : Option<int>.None;
                                var w = jsonImage.TryGetProperty("width", out var width)
                                        && width.ValueKind is JsonValueKind.Number
                                    ? width.GetInt32()
                                    : Option<int>.None;

                                var url = jsonImage.GetProperty("url").GetString();
                                var relativeSize =
                                    h.Map(x =>
                                        x switch
                                        {
                                            < 100 => ArtworkSizeType.Small,
                                            < 400 => ArtworkSizeType.Default,
                                            _ => ArtworkSizeType.Large
                                        }).IfNone(ArtworkSizeType.Default);

                                artwork = artwork.Add(new Artwork(url, w, h, relativeSize));
                            }

                            // we may have 3 images (large, medium, small)
                            //or 1 image (large)
                            //get medium if it exists, otherwise get large
                            image = artwork.Find(x => x.Size == ArtworkSizeType.Default)
                                .Match(x => x.Url, () => artwork.Find(x => x.Size == ArtworkSizeType.Default)
                                    .Match(x => x.Url,
                                        () => artwork.HeadOrNone().Map(x => x.Url)))
                                .IfNone(string.Empty);
                        }

                        switch (type)
                        {
                            case "playlist":
                                result.Add(new CardViewItem
                                {
                                    Id = AudioId.FromUri(item.GetProperty("uri").GetString()),
                                    Title = item.GetProperty("name").GetString()!,
                                    ImageUrl = image,
                                    Subtitle = item.GetProperty("description").GetString()
                                });
                                break;
                            case "album":
                                result.Add(new CardViewItem
                                {
                                    Id = AudioId.FromUri(item.GetProperty("uri").GetString()),
                                    Title = item.GetProperty("name").GetString()!,
                                    ImageUrl = image,
                                    Subtitle = $"{item.GetProperty("total_tracks").GetInt32()} tracks"
                                });
                                break;
                            case "artist":
                                var uri = AudioId.FromUri(item.GetProperty("uri").GetString());
                                result.Add(new CardViewItem
                                {
                                    Id = uri,
                                    Title = item.GetProperty("name").GetString()!,
                                    ImageUrl = image,
                                    Subtitle = item.GetProperty("followers").GetProperty("total").GetInt32().ToString()
                                });
                                break;
                            default:
                                break;
                        }
                    }

                    var title = group.GetProperty("name").GetString();
                    var tagline = group.TryGetProperty("tag_line", out var t) ? t.GetString() : null;
                    var groupResult = new HomeGroup
                    {
                        Items = result,
                        Title = title,
                        Subtitle = tagline
                    };
                    groupResults.Add(groupResult);
                }
            }

            home.Dispose();
            return groupResults;
        }

        return
            from result in state.Client.GetHomeAsync(
                types: types,
                limit: 20,
                offset: 0,
                contentLimit: 10,
                contentOffset: 0, ct)
            from parsed in Eff(() => ParseHome(result))
            select parsed;
    }

    public static IObservable<SpotifyRemoteState> ObserveRemoteState() =>
        State.Instance.Client.Remote.StateUpdates
            .Where(c => c.IsSome)
            .Select(c => c.ValueUnsafe());

    public static Aff<TrackOrEpisode> GetTrack(AudioId id, CancellationToken ct = default)
    {
        if (id.Type is not AudioItemType.Track and not AudioItemType.PodcastEpisode)
            return FailEff<TrackOrEpisode>(Error.New("Item is not a track or episode."));

        var country = State.Instance.Client.CountryCode.IfNone("US");
        return State.Instance.Client.Mercury.GetMetadata(id, country, ct).ToAff();
    }

    public static Aff<SpotifyColors> GetColorForImage(string imageUrl)
    {
        var response = State.Instance.Client.PrivateApi.FetchColorFor(Seq1(imageUrl));
        return response.ToAff();
    }
}