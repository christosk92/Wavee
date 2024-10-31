// using System.Buffers;
// using System.Runtime.InteropServices;
// using NAudio.Wave;
//
// namespace Wavee.Playback.Player;
//
// public sealed class NAudioAudioOutput : AudioOutput, IDisposable
// {
//     private readonly WaveOutEvent _wavePlayer;
//     private readonly BufferedWaveProvider _bufferedWaveProvider;
//     private readonly WaveFormat _waveFormat;
//
//     public NAudioAudioOutput()
//     {
//         _wavePlayer = new WaveOutEvent();
//         _waveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate: 44100, channels: 2);
//         _bufferedWaveProvider = new BufferedWaveProvider(_waveFormat)
//         {
//             BufferDuration = TimeSpan.FromSeconds(1)
//         };
//         _wavePlayer.Init(_bufferedWaveProvider);
//         _wavePlayer.Play();
//     }
//
//     private bool _stopReadingImmediately;
//
//     public override void Write(Span<float> samplesFloats)
//     {
//         var samplesSpan = MemoryMarshal.Cast<float, byte>(samplesFloats);
//
//
//         var samples = ArrayPool<byte>.Shared.Rent(samplesSpan.Length);
//         samplesSpan.CopyTo(samples);
//         _bufferedWaveProvider.AddSamples(samples, 0, samples.Length);
//
//         // Optional: Handle buffer overflow
//         try
//         {
//             while ((_bufferedWaveProvider.BufferLength - _bufferedWaveProvider.BufferedBytes < samples.Length))
//             {
//                 Task.Delay(1).Wait();
//             }
//         }
//         catch (Exception x)
//         {
//             // ignored
//         }
//         finally
//         {
//             ArrayPool<byte>.Shared.Return(samples);
//         }
//     }
//
//     public override void Consume()
//     {
//         while (_bufferedWaveProvider.BufferedBytes > 0)
//         {
//             Task.Delay(1).Wait();
//         }
//     }
//
//     public void Write(ReadOnlySpan<byte> samples)
//     {
//         if (samples.Length == 0)
//             return;
//
//         var samplesArr = samples.ToArray();
//         _bufferedWaveProvider.AddSamples(samplesArr, 0, samples.Length);
//
//         // Optional: Handle buffer overflow
//         while ((_bufferedWaveProvider.BufferLength - _bufferedWaveProvider.BufferedBytes < samples.Length))
//         {
//             // if (_stopReadingImmediately)
//             // {
//             //     _bufferedWaveProvider.ClearBuffer();
//             //     _stopReadingImmediately = false;
//             //     break;
//             // }
//
//             Thread.Sleep(1);
//         }
//     }
//
//     public void Dispose()
//     {
//         _wavePlayer.Dispose();
//     }
//
//     public override void Pause()
//     {
//         _wavePlayer.Pause();
//     }
//
//     public override void Resume()
//     {
//         _wavePlayer.Play();
//     }
//
//     public override void Stop()
//     {
//         _wavePlayer.Stop();
//         _bufferedWaveProvider.ClearBuffer();
//     }
//
//     public void Play()
//     {
//         _wavePlayer.Play();
//     }
//
//     public override void Clear()
//     {
//         //_wavePlayer.Stop();
//         _bufferedWaveProvider.ClearBuffer();
//         //_wavePlayer.Play();
//     }
// }