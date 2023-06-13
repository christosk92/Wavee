using System.Net.Http.Headers;
using System.Reactive.Linq;
using System.Text.Json;
using Eum.Spotify;
using Eum.Spotify.extendedmetadata;
using Eum.Spotify.playlist4;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Common;
using LanguageExt.UnsafeValueAccess;
using Spotify.Metadata;
using Wavee.Core.Ids;
using Wavee.Infrastructure.IO;
using Wavee.Spotify;
using Wavee.Spotify.Infrastructure.ApResolve;
using Wavee.Spotify.Infrastructure.Mercury;
using Wavee.Spotify.Infrastructure.Mercury.Models;
using Wavee.Spotify.Infrastructure.PrivateApi.Contracts.Response;
using Wavee.Spotify.Infrastructure.Remote.Contracts;
using Wavee.UI.Client.Artist;
using Wavee.UI.Models.Common;
using Wavee.UI.Models.Home;
using static LanguageExt.Prelude;
namespace Wavee.UI.Client;

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

    /// <summary>
    /// Fetches a playlist, potentially from a cache.
    /// If the playlist is found in the cache, it will be returned immediately, but a diff needs to happen.
    /// </summary>
    /// <param name="id"></param>
    /// <param name="ct"></param>
    /// <returns></returns>
    public static Aff<(SelectedListContent Item, bool FromCache)> FetchPlaylist(AudioId id, CancellationToken ct)
    {
        var spClient = ApResolver.SpClient.ValueUnsafe();

        var apiUrl = $"https://{spClient}/playlist/v2/playlist/{id.ToBase62()}?market=from_token";
        var client = State.Instance.Client;

        static Aff<(SelectedListContent result, bool)> CreateAff(SpotifyClient client, string apiurl,
            CancellationToken ct)
        {
            return from bearer in client.Mercury.GetAccessToken(ct).ToAff()
                    .Map(x => new AuthenticationHeaderValue("Bearer", x))
                   from result in HttpIO.Get(apiurl, bearer, LanguageExt.HashMap<string, string>.Empty, ct)
                       .ToAff().MapAsync(async r =>
                       {
                           r.EnsureSuccessStatusCode();
                           await using var stream = await r.Content.ReadAsStreamAsync(ct);
                           return SelectedListContent.Parser.ParseFrom(stream);
                       })
                   select (result, false);
        }

        var idAsStr = id.ToString();
        return
            from cache in client.Cache.GetRawEntity(idAsStr)
                .Match(
                    Some: playlist => SuccessAff((SelectedListContent.Parser.ParseFrom(playlist.Span), true)),
                    None: () =>
                        from result in CreateAff(client, apiUrl, ct)
                        from _ in Eff(() => client.Cache.SaveRawEntity(idAsStr, result.result.Attributes.Name, result.result.ToByteArray(), DateTimeOffset.MaxValue))
                        select result
                )
            select cache;

    }


    public static Aff<IReadOnlyCollection<HomeGroup>> GetHomeView(CancellationToken ct = default)
    {
        var state = State.Instance;
        const string types = "track%2Calbum%2Cplaylist%2Cplaylist_v2%2Cartist%2Ccollection_artist%2Ccollection_album";
        const int limit = 20;
        const int offset = 0;
        const int contentLimit = 10;
        const int contentOffset = 0;

        var apiurl =
            $"https://api.spotify.com/v1/views/desktop-home?types={types}&offset={offset}&limit={limit}&content_limit={contentLimit}&content_offset={contentOffset}";

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
        var client = state.Client;
        return
            from bearer in client.Mercury.GetAccessToken(ct).ToAff()
                .Map(x => new AuthenticationHeaderValue("Bearer", x))
            from result in HttpIO.Get(apiurl, bearer, LanguageExt.HashMap<string, string>.Empty, ct)
                .ToAff().MapAsync(async r =>
                {
                    await using var stream = await r.Content.ReadAsStreamAsync(ct);
                    return await JsonDocument.ParseAsync(stream, default, ct);
                })
            from parsed in Eff(() => ParseHome(result))
            select parsed;
    }

    public static Aff<MercuryPacket> GetLibaryComponent(string key, string userId,
        CancellationToken ct)
    {
        var state = State.Instance;
        return from mercury in SuccessEff(state.Client.Mercury)
               from tracksAndAlbums in mercury.Get(
                   $"hm://collection/{key}/{userId}?allowonlytracks=false&format=json&", ct).ToAff()
               select tracksAndAlbums;
    }

    public static Task<SpotifyArtistView> FetchArtist(AudioId artistId) => State.Instance.Client.FetchArtist(artistId);

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

    public static Eff<ISpotifyMercuryClient> Mercury => SuccessEff(State.Instance.Client.Mercury);
}