// using LibVLCSharp.Shared;
// using Wavee.Config;
// using Wavee.Playback.Player;
//
// namespace ConsoleApp1.Player;
//
// internal sealed class VlcOutput : IAudioOutput,IDisposable
// {
//     private record MediaHolder(Media Media, StreamMediaInput StreamMediaInput, Stream Stream) : IDisposable
//     {
//         public void Dispose()
//         {
//             Media.Dispose();
//             Stream.Dispose();
//             StreamMediaInput.Dispose();
//         }
//     }
//
//     private readonly LibVLC _libVlc;
//     private readonly MediaPlayer _mediaPlayer;
//     private readonly Dictionary<WaveePlayerMediaItem, MediaHolder> _mediaItems = new();
//     private readonly object _lock = new object();
//
//     // Track the currently playing playback stream
//     private WaveePlaybackStream? _currentPlaybackStream;
//
//     // Event to notify when media playback has ended
//     public event EventHandler<WaveePlaybackStream>? MediaEnded;
//
//     private readonly SpotifyConfig _config;
//
//     // Additional structures for cache eviction
//     private readonly LinkedList<WaveePlayerMediaItem> _cacheOrder = new();
//     private readonly Dictionary<WaveePlayerMediaItem, LinkedListNode<WaveePlayerMediaItem>> _cacheNodes = new();
//
//     public VlcOutput(SpotifyConfig config)
//     {
//         _config = config ?? throw new ArgumentNullException(nameof(config));
//
//         // Initialize LibVLC (ensure LibVLCSharp is properly initialized in your project)
//         Core.Initialize();
//         _libVlc = new LibVLC();
//         _mediaPlayer = new MediaPlayer(_libVlc);
//
//         // Subscribe to the EndReached event
//         _mediaPlayer.EndReached += OnMediaEnded;
//     }
//
//     public int MaxCacheEntries => _config.Playback.MaxActiveNodes;
//
//     /// <summary>
//     /// Plays the specified WaveePlaybackStream.
//     /// </summary>
//     /// <param name="playbackStream">The playback stream to play.</param>
//     /// <param name="cancellationToken">Cancellation token.</param>
//     public async Task Play(WaveePlaybackStream playbackStream, CancellationToken cancellationToken)
//     {
//         if (playbackStream == null) throw new ArgumentNullException(nameof(playbackStream));
//
//         MediaHolder? mediaHolder;
//
//         lock (_lock)
//         {
//             // Stop any currently playing media
//             if (_mediaPlayer.IsPlaying)
//             {
//                 _mediaPlayer.Stop();
//             }
//
//             // Set the current playback stream
//             _currentPlaybackStream = playbackStream;
//
//             // Attempt to retrieve the MediaHolder from the dictionary
//             if (_mediaItems.TryGetValue(playbackStream.MediaItem, out mediaHolder))
//             {
//                 _mediaPlayer.Media = mediaHolder.Media;
//
//                 // Move the accessed media item to the end to mark it as recently used
//                 if (_cacheNodes.TryGetValue(playbackStream.MediaItem, out var node))
//                 {
//                     _cacheOrder.Remove(node);
//                     _cacheOrder.AddLast(node);
//                 }
//             }
//         }
//
//         // If MediaHolder doesn't exist, create it
//         if (mediaHolder == null)
//         {
//             // Open the stream asynchronously
//             var stream = await playbackStream.Open(cancellationToken).ConfigureAwait(false);
//             var streamMediaInput = new StreamMediaInput(stream);
//             // make sure we only fetch required data, and not the whole stream
//             var newMedia = new Media(_libVlc, streamMediaInput);
//             await newMedia.Parse(MediaParseOptions.ParseNetwork, cancellationToken: cancellationToken);
//             mediaHolder = new MediaHolder(newMedia, streamMediaInput, stream);
//
//             lock (_lock)
//             {
//                 // Add the new MediaHolder to the dictionary
//                 _mediaItems[playbackStream.MediaItem] = mediaHolder;
//
//                 // Add the media item to the cache order
//                 var node = _cacheOrder.AddLast(playbackStream.MediaItem);
//                 _cacheNodes[playbackStream.MediaItem] = node;
//
//                 // Set the media to the media player
//                 _mediaPlayer.Play(mediaHolder.Media);
//
//                 // Check if cache exceeds the maximum allowed entries
//                 if (_mediaItems.Count > MaxCacheEntries)
//                 {
//                     EvictOldestCacheEntry();
//                 }
//             }
//         }
//
//         // // Start playback
//         // _mediaPlayer.Play();
//     }
//
//     public void Stop()
//     {
//         throw new NotImplementedException();
//     }
//
//     /// <summary>
//     /// Handles the EndReached event from LibVLC's MediaPlayer.
//     /// </summary>
//     private void OnMediaEnded(object? sender, EventArgs e)
//     {
//         WaveePlaybackStream? endedStream = null;
//
//         // Safely retrieve and clear the current playback stream
//         lock (_lock)
//         {
//             endedStream = _currentPlaybackStream;
//             _currentPlaybackStream = null;
//         }
//
//         // Invoke the MediaEnded event if there was an active playback stream
//         if (endedStream != null)
//         {
//             // Optionally, ensure this is invoked on the UI thread or appropriate synchronization context
//             MediaEnded?.Invoke(this, endedStream);
//         }
//     }
//
//     /// <summary>
//     /// Evicts the oldest media item from the cache.
//     /// </summary>
//     private void EvictOldestCacheEntry()
//     {
//         if (_cacheOrder.First is null)
//             return;
//
//         var oldestMediaItem = _cacheOrder.First.Value;
//
//         // Remove from the cache order and cache nodes
//         _cacheOrder.RemoveFirst();
//         _cacheNodes.Remove(oldestMediaItem);
//
//         // Remove from the media items dictionary and dispose the media holder
//         if (_mediaItems.TryGetValue(oldestMediaItem, out var mediaHolder))
//         {
//             _mediaItems.Remove(oldestMediaItem);
//             mediaHolder.Dispose();
//         }
//     }
//
//     /// <summary>
//     /// Disposes the VlcOutput and its resources.
//     /// </summary>
//     public void Dispose()
//     {
//         // Unsubscribe from the EndReached event
//         _mediaPlayer.EndReached -= OnMediaEnded;
//
//         // Stop playback and dispose MediaPlayer and LibVLC
//         _mediaPlayer.Stop();
//         _mediaPlayer.Dispose();
//         _libVlc.Dispose();
//
//         // Dispose all MediaHolder instances
//         lock (_lock)
//         {
//             foreach (var holder in _mediaItems.Values)
//             {
//                 holder.Dispose();
//             }
//
//             _mediaItems.Clear();
//             _cacheOrder.Clear();
//             _cacheNodes.Clear();
//         }
//     }
// }