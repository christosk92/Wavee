using System.Collections;
using System.Threading.Channels;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using LibVLCSharp.Shared;
using NAudio.Wave;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Player.Commanding;
using Wavee.Player.States;

namespace Wavee.Player.Sys;

internal static class WaveePlayerRuntime<RT> where RT : struct, HasAudioOutput<RT>
{
    private readonly record struct AudioSession(string PlaybackId,
        Stream Decoder);

    public static Aff<RT, Unit> Start(ChannelReader<IInternalPlayerCommand> reader,
        Ref<IWaveePlayerState> state)
    {
        return Aff<RT, Unit>(async (rt) =>
        {
            await Task.Factory.StartNew(async () =>
            {
                Ref<Option<AudioSession>> audioSession = Ref(Option<AudioSession>.None);
                await foreach (var command in reader.ReadAllAsync())
                {
                    try
                    {
                        switch (command)
                        {
                            case InternalPlayCommand<RT> play:
                                atomic(() => audioSession.Swap(f =>
                                {
                                    return f.Match(
                                        Some: r =>
                                        {
                                            AudioOutput<RT>.Stop().Run(rt);
                                            return Option<AudioSession>.None;
                                        },
                                        None: () => Option<AudioSession>.None);
                                }));

                                await HandlePlay(play.Runtime,
                                    play.SourceId,
                                    play.Stream, audioSession, state);
                                break;
                            case InternalPauseCommand<RT> pause:
                                _ = AudioOutput<RT>.Stop().Run(pause.Runtime);
                                atomic(() => state.Swap(f =>
                                {
                                    if (f is WaveePlayingState p)
                                        return p.ToPaused();
                                    return f;
                                }));
                                break;
                            case InternalSeekCommand<RT> seekCommand:
                                atomic(() => state.Swap(f =>
                                {
                                    if (f is IWaveePlayerInPlaybackState p)
                                    {
                                        var paused = p is WaveePausedState;
                                        AudioOutput<RT>.Seek(seekCommand.To).Run(seekCommand.runtime);
                                        // AudioOutput<RT>.Stop();
                                        //  AudioOutput<RT>.DiscardBuffer().Run(seekCommand.runtime);
                                        // if (!paused)
                                        // {
                                        //     AudioOutput<RT>.Start().Run(seekCommand.runtime);
                                        // }
                                        return p switch
                                        {
                                            WaveePlayingState pl => pl with
                                            {
                                                PositionAsOfTimestamp = seekCommand.To,
                                                Timestamp = DateTimeOffset.Now
                                            },
                                            WaveePausedState pl => pl with
                                            {
                                                Position = seekCommand.To
                                            },
                                        };
                                    }

                                    return f;
                                }));
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                    }
                }
            }, TaskCreationOptions.LongRunning);

            return unit;
        });
    }

    private static async Task HandlePlay(
        RT runtime,
        string SourceId,
        IAudioStream playStream,
        Ref<Option<AudioSession>> audioSession,
        Ref<IWaveePlayerState> state)
    {
        var playbackId = Guid.NewGuid().ToString();
        var stream = playStream.AsStream();
        // var decoderMaybe =
        //     AudioDecoderRuntime.OpenAudioDecoder(playStream.AsStream(), playStream.TotalDuration)
        //         .Run();
        // var decoder = decoderMaybe.ThrowIfFail();
        var audioSessionValue = new AudioSession(playbackId, stream);
        atomic(() => audioSession.Swap(_ => audioSessionValue));
        _ = AudioOutput<RT>.Start().Run(runtime);
        await Task.Factory.StartNew(async () =>
        {
            atomic(() => state.Swap(_ => new WaveePlayingState(playbackId,
                SourceId,
                stream)
            {
                Timestamp = DateTimeOffset.UtcNow,
                PositionAsOfTimestamp = TimeSpan.Zero
            }));

            var r = AudioOutput<RT>.PlayStream(stream, true).Run(runtime);
            var t = r.ThrowIfFail();
            await t;
        });
    }
}