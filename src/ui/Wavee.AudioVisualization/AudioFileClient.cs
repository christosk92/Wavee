using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;

namespace AudioEffectsLib
{
    public interface IAudioWaveSource : IDisposable
    {
        int GetNextPacketSize();
        byte[] GetBuffer(out int numBytesRead, out AudioWaveSourceBufferFlags flags);
        void ReleaseBuffer(int num);
        void ClearBuffer();
    }


    public class AudioStreamWaveSource : IAudioWaveSource
    {
        public int SampleRate { get; set; }
        public int Channels { get; set; }
        public int BitsPerSample { get; set; }
        DateTime lastFrame = DateTime.Now;
        int lastpSize;

        Stream stream;
        private bool disposed;

        int minpacketsize;
        int flushBytes = 0;

        public AudioStreamWaveSource(int samplerate, int channels, int bitsPerSample)
        {
            stream = new MemoryStream();
            // new UnmanagedMemoryStream().
            SampleRate = samplerate;
            Channels = channels;
            BitsPerSample = bitsPerSample;

            minpacketsize = (samplerate / 100) * channels * (bitsPerSample / 8);
            Debug.WriteLine("(" + samplerate + "/ 100) * " + channels + " * (" + bitsPerSample + "/8) = " + minpacketsize);
        }

        public unsafe void SetStreamPointer(byte* ptr, uint capacity)
        {
            stream = new UnmanagedMemoryStream(ptr, capacity);
        }

        public async void Write(byte[] buffer, int offset, int count)
        {
            if (disposed) return;
            lock (stream)
            {
                try
                {
                    long pos = stream.Position;
                    flushBytes = (int)(stream.Length - pos);
                    //Debug.WriteLine("Fbytes: " + flushBytes + ", L: " + (stream.Length));
                    stream.Seek(0, SeekOrigin.End);
                    stream.Write(buffer, offset, count);
                    stream.Position = pos;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
            }

        }

        public int GetNextPacketSize()
        {
            TimeSpan r = DateTime.Now - lastFrame;
            double pasttime = r.TotalSeconds;

            int samplesAvailable = (int)(pasttime * SampleRate);
            int bytesAvailable = samplesAvailable * Channels * (BitsPerSample / 8);

            if (flushBytes > 0)
            {
                bytesAvailable += flushBytes;
            }

            if (bytesAvailable >= minpacketsize)
            {
                lastpSize = bytesAvailable;
                flushBytes = 0;
                return bytesAvailable;
            }

            return 0;
        }

        public unsafe byte[] GetBuffer(out int numBytesRead, out AudioWaveSourceBufferFlags flags)
        {
            int psize = lastpSize;
            byte[] buffer = new byte[psize];

            try
            {
                lock (stream)
                {
                    numBytesRead = stream.Read(buffer, 0, psize);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
                numBytesRead = 0;
                Debug.WriteLine("OutOfRange: " + psize + ", Pos: " + stream.Position + ", len: " + stream.Length);
            }


            if (numBytesRead == 0)
            {
                flags = AudioWaveSourceBufferFlags.Silent;
            }
            else
            {
                flags = AudioWaveSourceBufferFlags.None;
            }

            lastFrame = DateTime.Now;
            return buffer;
        }

        public void ReleaseBuffer(int num)
        {
            try
            {
                stream.Position = stream.Position - num;
                if ((stream as MemoryStream).TryGetBuffer(out ArraySegment<byte> buf))
                {
                    Buffer.BlockCopy(buf.Array, buf.Offset + num, buf.Array, buf.Offset + 0, (int)stream.Length - num);
                    stream.SetLength(stream.Length - num);
                }
            }
            catch
            {

            }
        }

        public void ReleaseBuffer()
        {
            try
            {
                int num = (int)stream.Position;
                stream.Position = 0;
                if ((stream as MemoryStream).TryGetBuffer(out ArraySegment<byte> buf))
                {
                    Buffer.BlockCopy(buf.Array, buf.Offset + num, buf.Array, buf.Offset + 0, (int)stream.Length - num);
                    stream.SetLength(stream.Length - num);
                }
            }
            catch
            {

            }
        }

        public void ClearBuffer()
        {
            stream.Dispose();
            stream = new MemoryStream();
        }

        public void Dispose()
        {
            disposed = true;
            stream?.Dispose();
        }
    }


    public class SystemAudioWaveSource : IAudioWaveSource
    {
        private AudioClient audioClient { get; set; }
        private AudioCaptureClient audioCaptureClient { get; set; }
        private IntPtr hEvent { get; set; }

        public WAVEFORMATEX WaveFormat { get; set; }

        public SystemAudioWaveSource()
        {
            Guid iid_audioClient = new Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2");
            string deviceId = Windows.Media.Devices.MediaDevice.GetDefaultAudioRenderId(Windows.Media.Devices.AudioDeviceRole.Default);
            var icbh = new ActivateAudioInterfaceCompletionHandler(ac2 => InitializeCaptureDevice((IAudioClient)ac2));

            NativeMethods.ActivateAudioInterfaceAsync(deviceId, iid_audioClient, IntPtr.Zero, icbh, out var actOp);

            hEvent = NativeMethods.CreateEventExW(IntPtr.Zero, IntPtr.Zero, 0, EventAccess.EVENT_ALL_ACCESS);
            audioClient.SetEventHandle(hEvent);
        }

        private void InitializeCaptureDevice(IAudioClient audioClientInterface)
        {
            audioClient = new AudioClient((IAudioClient)audioClientInterface);

            WaveFormat = audioClient.MixFormat;


            long requestedDuration = 0;//REFTIMES_PER_MILLISEC * 100;

            if (!audioClient.IsFormatSupported(AudioClientShareMode.Shared, WaveFormat))
            {
                throw new ArgumentException("Unsupported Wave Format");
            }

            var streamFlags = AudioClientStreamFlags.Loopback;

            audioClient.Initialize(AudioClientShareMode.Shared,
                streamFlags,
                requestedDuration,
                0,
                WaveFormat,
                Guid.Empty);

            int bufferFrameCount = audioClient.BufferSize;

            hEvent = NativeMethods.CreateEventExW(IntPtr.Zero, IntPtr.Zero, 0, EventAccess.EVENT_ALL_ACCESS);
            audioClient.SetEventHandle(hEvent);
            audioCaptureClient = audioClient.AudioCaptureClient;

            audioClient.Start();
            //Debug.WriteLine("record buffer size = {0}", this.recordBuffer.Length);

            // Get back the effective latency from AudioClient
            //LatencyMilliseconds = (int)(audioClient.StreamLatency / 10000);
        }

        public int GetNextPacketSize()
        {
            return audioCaptureClient.GetNextPacketSize();
        }

        public byte[] GetBuffer(out int numBytesRead, out AudioWaveSourceBufferFlags flags)
        {
            var pData = audioCaptureClient.GetBuffer(out var numFramesToRead, out var dwFlags);
            flags = dwFlags;

            if ((int)(dwFlags & AudioWaveSourceBufferFlags.Silent) > 0)
            {
                pData = IntPtr.Zero;
            }

            numBytesRead = numFramesToRead * WaveFormat.wBitsPerSample;
            byte[] buf = new byte[numFramesToRead];

            if (numFramesToRead == 0) { return null; }


            if (pData == IntPtr.Zero)
            {
                Array.Clear(buf, 0, numBytesRead);
            }
            else
            {
                Marshal.Copy(pData, buf, 0, numBytesRead);
            }

            return buf;
        }

        public void ReleaseBuffer(int num)
        {
            audioCaptureClient.ReleaseBuffer(num);
        }

        public void ClearBuffer()
        {

        }

        public void Dispose()
        {
            audioClient?.Dispose();
            audioCaptureClient?.Dispose();
        }
    }
}
