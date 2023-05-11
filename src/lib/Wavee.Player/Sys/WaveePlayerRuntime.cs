using System.Collections;
using System.Threading.Channels;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using NAudio.Wave;
using Wavee.Infrastructure.Sys.IO;
using Wavee.Infrastructure.Traits;
using Wavee.Player.Commanding;
using Wavee.Player.States;

namespace Wavee.Player.Sys;

internal static class WaveePlayerRuntime<RT> where RT : struct, HasAudioOutput<RT>
{
    private readonly record struct AudioSession(string PlaybackId,
        WaveStream Decoder);

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
                                            r.Decoder.Dispose();
                                            _ = AudioOutput<RT>.DiscardBuffer().Run(play.Runtime);
                                            return Option<AudioSession>.None;
                                        },
                                        None: () => Option<AudioSession>.None);
                                }));
                                await HandlePlay(play.Runtime, play.Stream, audioSession, state);
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
                                        AudioOutput<RT>.Stop();
                                        AudioOutput<RT>.DiscardBuffer().Run(seekCommand.runtime);
                                        p.Decoder.CurrentTime = seekCommand.To;
                                        if (!paused)
                                        {
                                            AudioOutput<RT>.Start().Run(seekCommand.runtime);
                                        }

                                        return p;
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
        IAudioStream playStream,
        Ref<Option<AudioSession>> audioSession,
        Ref<IWaveePlayerState> state)
    {
        var playbackId = Guid.NewGuid().ToString();
        var decoderMaybe =
            AudioDecoderRuntime.OpenAudioDecoder(playStream.AsStream(), playStream.TotalDuration)
                .Run();
        var decoder = decoderMaybe.ThrowIfFail();
        var audioSessionValue = new AudioSession(playbackId, decoder);
        atomic(() => audioSession.Swap(_ => audioSessionValue));
        _ = AudioOutput<RT>.Start().Run(runtime);
        await Task.Factory.StartNew(async () =>
        {
            Memory<byte> buffer = new byte[4096 * 2];
            atomic(() => state.Swap(_ => new WaveePlayingState(playbackId, decoder)));
            while (true)
            {
                var readMaybe = Try(() => decoder.Read(buffer.Span))();
                var read = readMaybe.Match(Succ: r => r, Fail: _ => 0);
                if (read == 0 && decoder.CurrentTime >= decoder.TotalTime)
                {
                    break;
                }

                var run = await AudioOutput<RT>.Write(buffer[..read]).Run(
                    runtime);
            }

            var alreadyGoingToNextTrack = audioSession.Value.IsSome
                                          && audioSession.Value.ValueUnsafe().PlaybackId != playbackId;
            atomic(() => state.Swap(_ => new WaveeEndOfTrackState(playbackId, decoder,
                alreadyGoingToNextTrack
            )));
        });
    }
}