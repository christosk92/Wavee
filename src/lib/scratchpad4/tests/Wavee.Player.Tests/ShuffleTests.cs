using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Enums;
using Wavee.Core.Id;
using Wavee.Player.States;

namespace Wavee.Player.Tests;

public class ShuffleTests
{
    [Fact]
    public void Shuffle_WithMultipleItems_ShufflesAsExpected()
    {
        // Arrange
        var tracks = Enumerable.Range(1, 10)
            .Select(i => new FutureTrack(new AudioId(i.ToString(), AudioItemType.Track, "test"),
                () => Task.FromResult<IAudioStream>(null)))
            .ToList();
        var rng = new MockRandomNumberGenerator(new[] { 1, 3, 2, 0, 9, 7, 8, 5, 6, 4 }); // mock "random" sequence
        var context = new WaveeContext(
            rng,
            new AudioId("ContextId", AudioItemType.Playlist, "Test Context"), "Test Context",
            tracks);
        var state = new WaveePlayerState(
            new WaveeLoadingState(0, new AudioId("0", AudioItemType.Track, "test"), false, TimeSpan.Zero, false)
            {
                Stream = null
            },
            context, RepeatStateType.None, false, new Que<FutureTrack>());

        // Act
        var newState = state.Shuffle(true);
        var nextTrackState = newState.SkipNext();
        // Assert
        // Based on the mock "random" sequence, the first track after shuffling should be the one at index 1 in the original list
        Assert.Equal(tracks[1].Id, ((WaveeLoadingState)nextTrackState.State).TrackId.ValueUnsafe());
    }

    private class MockRandomNumberGenerator : IShuffleProvider
    {
        private readonly Queue<int> _numbers;

        public MockRandomNumberGenerator(IEnumerable<int> numbers)
        {
            _numbers = new Queue<int>(numbers);
        }

        public int GetNextIndex(int currentIndex, int maxIndex)
        {
            return _numbers.Dequeue();
        }
    }
}