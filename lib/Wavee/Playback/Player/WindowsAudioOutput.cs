using System.Runtime.InteropServices;

namespace Wavee.Playback.Player;

public class WindowsAudioOutput : IDisposable
{
    private const int CALLBACK_FUNCTION = 0x00030000;
    private const int WAVE_FORMAT_IEEE_FLOAT = 3;
    private const int SAMPLES_PER_BUFFER = 4096;

    private IntPtr hWaveOut;
    private WaveOutBuffer[] buffers;
    private int bufferSize;
    private int currentBuffer;
    private bool disposed;
    private AutoResetEvent writeEvent = new AutoResetEvent(false);

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveOutBuffer
    {
        public IntPtr dataBuffer;
        public int bufferLength;
        public IntPtr userData;
        public int flags;
        public IntPtr reserved;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = SAMPLES_PER_BUFFER)]
        public float[] buffer;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct WaveFormat
    {
        public ushort wFormatTag;
        public ushort nChannels;
        public uint nSamplesPerSec;
        public uint nAvgBytesPerSec;
        public ushort nBlockAlign;
        public ushort wBitsPerSample;
        public ushort cbSize;
    }

    private delegate void WaveOutProc(IntPtr hwo, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2);

    [DllImport("winmm.dll")]
    private static extern uint waveOutOpen(out IntPtr hWaveOut, uint uDeviceID, WaveFormat lpFormat, WaveOutProc dwCallback, IntPtr dwInstance, uint dwFlags);

    [DllImport("winmm.dll")]
    private static extern uint waveOutPrepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern uint waveOutWrite(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern uint waveOutUnprepareHeader(IntPtr hWaveOut, IntPtr lpWaveOutHdr, int uSize);

    [DllImport("winmm.dll")]
    private static extern uint waveOutReset(IntPtr hWaveOut);

    [DllImport("winmm.dll")]
    private static extern uint waveOutClose(IntPtr hWaveOut);

    [DllImport("winmm.dll")]
    private static extern uint waveOutPause(IntPtr hWaveOut);

    [DllImport("winmm.dll")]
    private static extern uint waveOutRestart(IntPtr hWaveOut);

    public WindowsAudioOutput()
    {
        WaveFormat waveFormat = new WaveFormat
        {
            wFormatTag = WAVE_FORMAT_IEEE_FLOAT,
            nChannels = 2,
            nSamplesPerSec = 44100,
            nAvgBytesPerSec = 44100 * 2 * 4,
            nBlockAlign = (ushort)(2 * 4),
            wBitsPerSample = 32,
            cbSize = 0
        };

        bufferSize = SAMPLES_PER_BUFFER * 4 * 2;

        waveOutOpen(out hWaveOut, 0xFFFFFFFF, waveFormat, CallbackFunction, IntPtr.Zero, CALLBACK_FUNCTION);

        buffers = new WaveOutBuffer[2];
        for (int i = 0; i < buffers.Length; i++)
        {
            buffers[i] = new WaveOutBuffer
            {
                dataBuffer = Marshal.AllocHGlobal(bufferSize),
                bufferLength = bufferSize,
                userData = GCHandle.ToIntPtr(GCHandle.Alloc(i, GCHandleType.Pinned))
            };

            buffers[i].buffer = new float[SAMPLES_PER_BUFFER * 2];

            IntPtr headerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(buffers[i]));
            Marshal.StructureToPtr(buffers[i], headerPtr, false);
            waveOutPrepareHeader(hWaveOut, headerPtr, Marshal.SizeOf(buffers[i]));
            Marshal.FreeHGlobal(headerPtr);
        }
    }

    private void CallbackFunction(IntPtr hwo, uint uMsg, IntPtr dwInstance, IntPtr dwParam1, IntPtr dwParam2)
    {
        if (uMsg == 955) // WOM_DONE
        {
            writeEvent.Set();
        }
    }

    public void Write(float[] samples)
    {
        WaveOutBuffer buffer = buffers[currentBuffer];
        currentBuffer = (currentBuffer + 1) % buffers.Length;

        Marshal.Copy(samples, 0, buffer.dataBuffer, samples.Length);

        writeEvent.WaitOne();

        IntPtr headerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(buffer));
        Marshal.StructureToPtr(buffer, headerPtr, false);
        var res = waveOutWrite(hWaveOut, headerPtr, Marshal.SizeOf(buffer));
        Marshal.FreeHGlobal(headerPtr);
    }

    public void Pause()
    {
        waveOutPause(hWaveOut);
    }

    public void Resume()
    {
        waveOutRestart(hWaveOut);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!disposed)
        {
            if (disposing)
            {
                writeEvent.Dispose();
            }

            waveOutReset(hWaveOut);

            for (int i = 0; i < buffers.Length; i++)
            {
                IntPtr headerPtr = Marshal.AllocHGlobal(Marshal.SizeOf(buffers[i]));
                Marshal.StructureToPtr(buffers[i], headerPtr, false);
                waveOutUnprepareHeader(hWaveOut, headerPtr, Marshal.SizeOf(buffers[i]));
                Marshal.FreeHGlobal(headerPtr);

                Marshal.FreeHGlobal(buffers[i].dataBuffer);
                GCHandle.FromIntPtr(buffers[i].userData).Free();
            }

            waveOutClose(hWaveOut);

            disposed = true;
        }
    }

    ~WindowsAudioOutput()
    {
        Dispose(false);
    }

    public void Clear()
    {
        
    }
}