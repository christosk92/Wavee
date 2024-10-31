// using Eum.Spotify.context;
// using Google.Protobuf;
// using Microsoft.Extensions.Logging;
// using Wavee.Models.Common;
// using Wavee.Playback.Contexts;
// using Wavee.Playback.Player;
//
// public class WaveeSimpleContext : WaveePlayerPlaybackContext
// {
//     private readonly SpotifyId OneId = SpotifyId.FromUri("spotify:track:4a7VgYvKmj4Q9dc3vRuoDC");
//     private readonly List<WaveePlayerPlaybackContextPage> _pages = new();
//     private readonly ILogger<WaveePlayer> _logger;
//
//     public WaveeSimpleContext(ILogger<WaveePlayer> logger)
//     {
//         _logger = logger;
//     }
//
//     public override Task<WaveePlayerPlaybackContextPage?> GetPage(int pageIndex, CancellationToken cancellationToken)
//     {
//         if (pageIndex < 0 || pageIndex >= _pages.Count)
//         {
//             return Task.FromResult<WaveePlayerPlaybackContextPage?>(null);
//         }
//         
//         return Task.FromResult<WaveePlayerPlaybackContextPage?>(_pages[pageIndex]);
//     }
//
//     public override string Id { get; } = "test";
//
//     public override Task<IReadOnlyCollection<WaveePlayerPlaybackContextPage>> InitializePages()
//     {
//         var ctxPage = new ContextPage();
//         int numberoftracks = 10;
//         for (int i = 0; i < numberoftracks; i++)
//         {
//             var uri = OneId.ToString();
//             ctxPage.Tracks.Add(new ContextTrack
//             {
//                 Uri = uri,
//                 Gid = ByteString.CopyFrom(OneId.ToRaw())
//             });
//         }
//         _pages.Add(new WaveePlayerPlaybackContextPage(0, ctxPage, null, _logger));
//         return Task.FromResult<IReadOnlyCollection<WaveePlayerPlaybackContextPage>>(_pages);
//     }
//
//     public override SpotifyId? GetTrackId(string mediaItemUid)
//     {
//         return OneId;
//     }
// }