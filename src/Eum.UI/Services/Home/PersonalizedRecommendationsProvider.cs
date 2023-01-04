using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Models;
using Eum.Connections.Spotify.Models.Views;
using System.Net.NetworkInformation;
using Eum.Logging;

namespace Eum.UI.Services.Home
{
    public class PersonalizedRecommendationsProvider
    {
        private View<View<ISpotifyItem>>? _previousItems;
        private readonly ISpotifyClient _client;

        public PersonalizedRecommendationsProvider(ISpotifyClient client)
        {
            _client = client;
        }

        public async Task<IEnumerable<GroupedHomeItem>?> GetRecommendations(
            CancellationToken ct = default(CancellationToken))
        {
            var items = await GetItems(ct);

            return items?.Content.Items?
                .Select(a => new GroupedHomeItem(a.Content.Items ?? Enumerable.Empty<ISpotifyItem>())
                {
                    Key = a.Id,
                    Title = a.Name,
                    TagLine = a.TagLine
                });
        }

        private async ValueTask<View<View<ISpotifyItem>>?> GetItems(CancellationToken ct = default)
        {
            //check if internet is available
            if (!IsConnected())
            {
                return _previousItems;
            }

            using var cancelToken = new CancellationTokenSource();
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct, cancelToken.Token);
            cancelToken.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                var data = await _client.ViewsClient.GetHomeAsync(new HomeRequest(DateTimeOffset.UtcNow, "en",
                    _client.AuthenticatedUser!.CountryCode), cts.Token);

                _previousItems = data;

                return data;
            }
            catch (TaskCanceledException _)
            {
                return _previousItems ?? null;
            }
        }

        private static bool IsConnected()
        {
            try
            {
                Ping myPing = new Ping();
                var host = "8.8.8.8";
                byte[] buffer = new byte[32];
                int timeout = 1000;
                PingOptions pingOptions = new PingOptions();
                var reply = myPing.Send(host, timeout, buffer, pingOptions);
                if (reply is
                    {
                        Status: IPStatus.Success
                    })
                {
                    return true;
                }
            }
            catch (Exception ex)
            {
                S_Log.Instance.LogError(ex);
            }
            return false;
        }
    }
}