using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using Wavee.Core.Contracts;
using Wavee.Core.Enums;
using Wavee.Core.Id;
using Wavee.Player.States;
using static LanguageExt.Prelude;

namespace Wavee.Player.Tests;

public class PlayerStateTests
{
    [Fact]
    public void SkipNext_WithQueue_PlaysNextInQueue()
    {
        // Arrange
        const string firstTrackId = "0";
        const string secondTrackId = "1";
        const string source = "test";

        var firstTrackAudioId = new AudioId(firstTrackId, AudioItemType.Track, source);
        var secondT = new AudioId(secondTrackId, AudioItemType.Track, source);

        var queue = new Que<FutureTrack>().Enqueue(
            new FutureTrack(secondT, () => CreateFakeStream(secondT)));

        var state = new WaveePlayerState(
            new WaveeLoadingState(0, 
                firstTrackAudioId, 
                false, TimeSpan.Zero, false, true)
            {
                Stream = CreateFakeStream(firstTrackAudioId)
            }, Option<WaveeContext>.None,
            RepeatStateType.None, false, queue);

        // Act
        var newState = state.SkipNext(true);

        // Assert
        Assert.True(newState.Queue.IsEmpty);
        Assert.True(newState.State is WaveeLoadingState);
        Assert.Equal(secondT, ((WaveeLoadingState)newState.State).TrackId.ValueUnsafe());
    }

    [Fact]
    public void SkipNext_WithRepeatTrack_RepeatsSameTrack()
    {
        // Arrange
        const string firstTrackId = "0";
        var firstTrackAudioId = new AudioId(firstTrackId, AudioItemType.Track, "test");
        var state = new WaveePlayerState(
            new WaveeLoadingState(0,
                firstTrackAudioId,
                false,
                TimeSpan.Zero, false, true)
            {
                Stream = CreateFakeStream(firstTrackAudioId)
            }, Option<WaveeContext>.None,
            RepeatStateType.RepeatTrack, false, new Que<FutureTrack>());

        // Act
        var newState = state.SkipNext(true);

        // Assert
        Assert.True(newState.State is WaveeLoadingState);
        Assert.Equal(firstTrackAudioId, ((WaveeLoadingState)newState.State).TrackId.ValueUnsafe());
    }

    [Fact]
    public Task SetContext_AddToQueue_CheckContextAndQueue()
    {
        // Arrange
        const string contextName = "Test Context";
        const string trackId = "1";
        var trackAudioId = new AudioId(trackId, AudioItemType.Track, "test");
        var context = new WaveeContext(
            Option<IShuffleProvider>.None,
            "ContextId",
            contextName,
            new List<FutureTrack> { new FutureTrack(trackAudioId, () => CreateFakeStream(trackAudioId)) }
        );
        var queueTrack = new FutureTrack(trackAudioId, () => CreateFakeStream(trackAudioId));
        var state = new WaveePlayerState(
            new WaveeLoadingState(0,
                trackAudioId,
                false,
                TimeSpan.Zero, false, true)
            {
                Stream = CreateFakeStream(trackAudioId)
            },
            Option<WaveeContext>.None,
            RepeatStateType.None,
            false,
            new Que<FutureTrack>()
        );

        // Act: Set context and add to queue
        var stateWithContext = state.PlayContext(context, Some(0), Some(TimeSpan.Zero), Some(false));
        var newState = stateWithContext.AddToQueue(queueTrack);

        // Assert
        Assert.True(newState.Context.IsSome);
        Assert.Equal(contextName, newState.Context.ValueUnsafe().Name);
        Assert.Single(newState.Queue);
        Assert.Equal(trackAudioId, newState.Queue.Head().Id);
        return Task.CompletedTask;
    }

    private Task<IAudioStream> CreateFakeStream(AudioId id)
    {
        return Task.FromResult<IAudioStream>(new FakeStream(new FakeTrack(id)));
    }

    private readonly struct FakeStream : IAudioStream
    {
        public FakeStream(ITrack track)
        {
            Track = track;
        }

        public ITrack Track { get; }
        public Option<string> Uid { get; }

        public Stream AsStream()
        {
            throw new NotImplementedException();
        }

        public Task<Stream> GetStreamAsync()
        {
            throw new NotImplementedException();
        }
    }
}

internal class FakeTrack : ITrack
{
    public FakeTrack(AudioId id)
    {
        Id = id;
    }

    public AudioId Id { get; }
    public string Title { get; }
    public Seq<ITrackArtist> Artists { get; }
    public ITrackAlbum Album { get; }
    public TimeSpan Duration { get; }
    public bool CanPlay { get; }
}