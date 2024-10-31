// using NAudio.Wave;
//
// namespace Wavee.Playback.Player;
//
// public class CrossfadeSampleProvider : ISampleProvider
// {
//     private readonly ISampleProvider sourceA;
//     private readonly ISampleProvider sourceB;
//     private WaveFormat waveFormat;
//     private readonly int crossfadeSampleCount;
//     private int samplePosition = 0;
//
//     public bool CrossfadeComplete { get; private set; } = false;
//
//     public CrossfadeSampleProvider(ISampleProvider sourceA, ISampleProvider sourceB, TimeSpan crossfadeDuration)
//     {
//         this.sourceA = sourceA;
//         this.sourceB = sourceB;
//         this.waveFormat = sourceA.WaveFormat;
//         this.crossfadeSampleCount = (int)(waveFormat.SampleRate * crossfadeDuration.TotalSeconds) * waveFormat.Channels;
//     }
//
//     public WaveFormat WaveFormat
//     {
//         get { return waveFormat; }
//         set
//         {
//             waveFormat = value;
//         }
//     }
//
//     public int Read(float[] buffer, int offset, int count)
//     {
//         if (CrossfadeComplete)
//         {
//             // Continue reading from sourceB
//             return sourceB.Read(buffer, offset, count);
//         }
//
//         int samplesReadA = sourceA.Read(buffer, offset, count);
//         float[] bufferB = new float[count];
//         int samplesReadB = sourceB.Read(bufferB, 0, count);
//
//         int samplesRead = Math.Max(samplesReadA, samplesReadB);
//
//         for (int n = 0; n < samplesRead; n++)
//         {
//             float crossfadeFactor = 0f;
//             if (samplePosition < crossfadeSampleCount)
//             {
//                 crossfadeFactor = (float)samplePosition / crossfadeSampleCount;
//                 samplePosition++;
//             }
//             else
//             {
//                 crossfadeFactor = 1f;
//                 CrossfadeComplete = true;
//             }
//
//             buffer[offset + n] = buffer[offset + n] * (1 - crossfadeFactor) + bufferB[n] * crossfadeFactor;
//         }
//
//         return samplesRead;
//     }
// }