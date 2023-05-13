using System.Reactive.Concurrency;
using LanguageExt;
using Microsoft.Reactive.Testing;
using Moq;
using Wavee.Core.Contracts;
using Wavee.Core.Id;
using Wavee.Core.Infrastructure.Traits;
using Wavee.Player.States;
using static LanguageExt.Prelude;

namespace Wavee.Player.Tests;

public class WaveePlayerBehaviorTests
{
    [Fact]
    public async Task Pause_SetsStateToPausedState()
    {
        _ = WaveeCore.Runtime;
        atomic(() => WaveeCore.AudioOutput.Swap(_ => Some(BuildMockAudioOutput())));

        // Arrange
        var scheduler = new TestScheduler();
        var stateChanged = WaveePlayer.StateChanged;
        var stateChangedObserver = scheduler.CreateObserver<WaveePlayerState>();

        stateChanged.Subscribe(stateChangedObserver);

        var contextId = new AudioId("ContextId", AudioItemType.Playlist, "Test Context");
        var futureTrack = new FutureTrack(new AudioId("TrackId", AudioItemType.Track, "test"),
            () => Task.FromResult<IAudioStream>(new Mock<IAudioStream>().Object));
        var context = new WaveeContext(Option<IShuffleProvider>.None,
            contextId,
            "Test Context",
            new List<FutureTrack>
            {
                futureTrack
            });
        WaveePlayer.PlayContext(context, TimeSpan.Zero, 0, false);
        await Task.Delay(100); // wait for PlayContext command to be processed
        // Act
        scheduler.AdvanceBy(1); // advance the virtual time to allow the PlayContext command to be processed

        WaveePlayer.Pause();
        await Task.Delay(100); // wait for PlayContext command to be processed
        var expectedStateTypes = new[]
        {
            typeof(WaveeNothingState),
            typeof(WaveeLoadingState),
            typeof(WaveePlayingState),
            typeof(WaveePausedState)
        };
        var actualStateTypes = stateChangedObserver.Messages.Select(m => m.Value.Value.State.GetType()).ToArray();

        Assert.Equal(expectedStateTypes, actualStateTypes);
    }

    private AudioOutputIO BuildMockAudioOutput()
    {
        var moc = new Mock<AudioOutputIO>();
        moc.Setup(x => x.PlayStream(It.IsAny<Stream>(), It.IsAny<Action<TimeSpan>>(), It.IsAny<bool>()))
            .Returns(Task.Delay(TimeSpan.FromHours(1)));

        moc.Setup(x => x.Pause())
            .Returns(unit);
        moc.Setup(x => x.Start())
            .Returns(unit);
        return moc.Object;
    }
}