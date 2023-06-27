using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Runtime.CompilerServices;

namespace AudioEffectsLib
{
    [Guid("905a0fef-bc53-11df-8c49-001e4fc686da"),
             InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IBufferByteAccess
    {
        unsafe void Buffer(out byte* pByte);
    }

    [Flags]
    public enum AudioWaveSourceBufferFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// AUDWS_BUFFERFLAGS_DATA_DISCONTINUITY
        /// </summary>
        DataDiscontinuity = 0x1,
        /// <summary>
        /// AUDWS_BUFFERFLAGS_SILENT
        /// </summary>
        Silent = 0x2,
        /// <summary>
        /// AUDWS_BUFFERFLAGS_TIMESTAMP_ERROR
        /// </summary>
        TimestampError = 0x4

    }

    public enum AudioClientErrors
    {
        /// <summary>
        /// AUDCLNT_E_NOT_INITIALIZED
        /// </summary>
        NotInitialized = unchecked((int)0x88890001),
        /// <summary>
        /// AUDCLNT_E_UNSUPPORTED_FORMAT
        /// </summary>
        UnsupportedFormat = unchecked((int)0x88890008),
        /// <summary>
        /// AUDCLNT_E_DEVICE_IN_USE
        /// </summary>
        DeviceInUse = unchecked((int)0x8889000A),
        /// <summary>
        /// AUDCLNT_E_RESOURCES_INVALIDATED
        /// </summary>
        ResourcesInvalidated = unchecked((int)0x88890026),
    }

    /// <summary>
    /// AUDCLNT_SHAREMODE
    /// </summary>
    public enum AudioClientShareMode
    {
        /// <summary>
        /// AUDCLNT_SHAREMODE_SHARED,
        /// </summary>
        Shared,
        /// <summary>
        /// AUDCLNT_SHAREMODE_EXCLUSIVE
        /// </summary>
        Exclusive,
    }

    [Guid("C8ADBD64-E71E-48a0-A4DE-185C395CD317"),
     InterfaceType(ComInterfaceType.InterfaceIsIUnknown), ComImport]
    interface IAudioCaptureClient
    {
        /*HRESULT GetBuffer(
            BYTE** ppData,
            UINT32* pNumFramesToRead,
            DWORD* pdwFlags,
            UINT64* pu64DevicePosition,
            UINT64* pu64QPCPosition
            );*/

        int GetBuffer(
            out IntPtr dataBuffer,
            out int numFramesToRead,
            out AudioWaveSourceBufferFlags bufferFlags,
            out long devicePosition,
            out long qpcPosition);

        int ReleaseBuffer(int numFramesRead);

        int GetNextPacketSize(out int numFramesInNextPacket);

    }

    /// <summary>
    /// Audio Capture Client
    /// </summary>
    public class AudioCaptureClient : IDisposable
    {
        IAudioCaptureClient audioCaptureClientInterface;

        internal AudioCaptureClient(IAudioCaptureClient audioCaptureClientInterface)
        {
            this.audioCaptureClientInterface = audioCaptureClientInterface;
        }

        /// <summary>
        /// Gets a pointer to the buffer
        /// </summary>
        /// <returns>Pointer to the buffer</returns>
        public IntPtr GetBuffer(
            out int numFramesToRead,
            out AudioWaveSourceBufferFlags bufferFlags,
            out long devicePosition,
            out long qpcPosition)
        {
            Marshal.ThrowExceptionForHR(audioCaptureClientInterface.GetBuffer(out var bufferPointer, out numFramesToRead, out bufferFlags, out devicePosition, out qpcPosition));
            return bufferPointer;
        }

        /// <summary>
        /// Gets a pointer to the buffer
        /// </summary>
        /// <param name="numFramesToRead">Number of frames to read</param>
        /// <param name="bufferFlags">Buffer flags</param>
        /// <returns>Pointer to the buffer</returns>
        public IntPtr GetBuffer(
            out int numFramesToRead,
            out AudioWaveSourceBufferFlags bufferFlags)
        {
            Marshal.ThrowExceptionForHR(audioCaptureClientInterface.GetBuffer(out var bufferPointer, out numFramesToRead, out bufferFlags, out _, out _));
            return bufferPointer;
        }

        /// <summary>
        /// Gets the size of the next packet
        /// </summary>
        public int GetNextPacketSize()
        {
            Marshal.ThrowExceptionForHR(audioCaptureClientInterface.GetNextPacketSize(out var numFramesInNextPacket));
            return numFramesInNextPacket;
        }

        /// <summary>
        /// Release buffer
        /// </summary>
        /// <param name="numFramesWritten">Number of frames written</param>
        public void ReleaseBuffer(int numFramesWritten)
        {
            Marshal.ThrowExceptionForHR(audioCaptureClientInterface.ReleaseBuffer(numFramesWritten));
        }

        /// <summary>
        /// Release the COM object
        /// </summary>
        public void Dispose()
        {
            if (audioCaptureClientInterface != null)
            {
                // althugh GC would do this for us, we want it done now
                // to let us reopen WASAPI
                Marshal.ReleaseComObject(audioCaptureClientInterface);
                audioCaptureClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }
    }


    /// <summary>
    /// Windows CoreAudio IAudioClient interface
    /// Defined in AudioClient.h
    /// </summary>
    [Guid("1CB9AD4C-DBFA-4c32-B178-C2F568A703B2"),
        InterfaceType(ComInterfaceType.InterfaceIsIUnknown),
        ComImport]
    internal interface IAudioClient
    {
        [PreserveSig]
        int Initialize(AudioClientShareMode shareMode,
            AudioClientStreamFlags streamFlags,
            long hnsBufferDuration, // REFERENCE_TIME
            long hnsPeriodicity, // REFERENCE_TIME
            [In] WAVEFORMATEX pFormat,
            [In] ref Guid audioSessionGuid);

        /// <summary>
        /// The GetBufferSize method retrieves the size (maximum capacity) of the endpoint buffer.
        /// </summary>
        int GetBufferSize(out uint bufferSize);

        [return: MarshalAs(UnmanagedType.I8)]
        long GetStreamLatency();

        int GetCurrentPadding(out int currentPadding);

        [PreserveSig]
        int IsFormatSupported(
            AudioClientShareMode shareMode,
            [In] WAVEFORMATEX pFormat,
            IntPtr closestMatchFormat);

        int GetMixFormat(out IntPtr deviceFormatPointer);

        // REFERENCE_TIME is 64 bit int        
        int GetDevicePeriod(out long defaultDevicePeriod, out long minimumDevicePeriod);

        int Start();

        int Stop();

        int Reset();

        int SetEventHandle(IntPtr eventHandle);

        /// <summary>
        /// The GetService method accesses additional services from the audio client object.
        /// </summary>
        /// <param name="interfaceId">The interface ID for the requested service.</param>
        /// <param name="interfacePointer">Pointer to a pointer variable into which the method writes the address of an instance of the requested interface. </param>
        [PreserveSig]
        int GetService([In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId, [Out, MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);
    }

    /// <summary>
    /// Windows CoreAudio AudioClient
    /// </summary>
    public class AudioClient : IDisposable
    {
        private IAudioClient audioClientInterface;
        private WAVEFORMATEX mixFormat;
        private AudioCaptureClient audioCaptureClient;
        private AudioClientShareMode shareMode;

        internal AudioClient(IAudioClient audioClientInterface)
        {
            this.audioClientInterface = audioClientInterface;
        }

        /// <summary>
        /// Retrieves the stream format that the audio engine uses for its internal processing of shared-mode streams.
        /// Can be called before initialize
        /// </summary>
        public WAVEFORMATEX MixFormat
        {
            get
            {
                if (true)
                {
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetMixFormat(out var waveFormatPointer));
                    var waveFormat = Marshal.PtrToStructure<WAVEFORMATEX>(waveFormatPointer);
                    Marshal.FreeCoTaskMem(waveFormatPointer);
                    mixFormat = waveFormat;
                }
                return mixFormat;
            }
        }

        /// <summary>
        /// Initializes the Audio Client
        /// </summary>
        /// <param name="shareMode">Share Mode</param>
        /// <param name="streamFlags">Stream Flags</param>
        /// <param name="bufferDuration">Buffer Duration</param>
        /// <param name="periodicity">Periodicity</param>
        /// <param name="waveFormat">Wave Format</param>
        /// <param name="audioSessionGuid">Audio Session GUID (can be null)</param>
        public void Initialize(AudioClientShareMode shareMode,
            AudioClientStreamFlags streamFlags,
            long bufferDuration,
            long periodicity,
            WAVEFORMATEX waveFormat,
            Guid audioSessionGuid)
        {
            this.shareMode = shareMode;
            int hresult = audioClientInterface.Initialize(shareMode, streamFlags, bufferDuration, periodicity, waveFormat, ref audioSessionGuid);
            Marshal.ThrowExceptionForHR(hresult);
            // may have changed the mix format so reset it
        }

        /// <summary>
        /// Retrieves the size (maximum capacity) of the audio buffer associated with the endpoint. (must initialize first)
        /// </summary>
        public int BufferSize
        {
            get
            {
                uint bufferSize;
                Marshal.ThrowExceptionForHR(audioClientInterface.GetBufferSize(out bufferSize));
                return (int)bufferSize;
            }
        }

        /// <summary>
        /// Retrieves the maximum latency for the current stream and can be called any time after the stream has been initialized.
        /// </summary>
        public long StreamLatency => audioClientInterface.GetStreamLatency();

        /// <summary>
        /// Retrieves the number of frames of padding in the endpoint buffer (must initialize first)
        /// </summary>
        public int CurrentPadding
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioClientInterface.GetCurrentPadding(out var currentPadding));
                return currentPadding;
            }
        }

        /// <summary>
        /// Retrieves the length of the periodic interval separating successive processing passes by the audio engine on the data in the endpoint buffer.
        /// (can be called before initialize)
        /// </summary>
        public long DefaultDevicePeriod
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioClientInterface.GetDevicePeriod(out var defaultDevicePeriod, out _));
                return defaultDevicePeriod;
            }
        }

        /// <summary>
        /// Gets the minimum device period 
        /// (can be called before initialize)
        /// </summary>
        public long MinimumDevicePeriod
        {
            get
            {
                Marshal.ThrowExceptionForHR(audioClientInterface.GetDevicePeriod(out _, out var minimumDevicePeriod));
                return minimumDevicePeriod;
            }
        }

        // TODO: GetService:
        // IID_IAudioSessionControl
        // IID_IChannelAudioVolume
        // IID_ISimpleAudioVolume

        /// <summary>
        /// Gets the AudioCaptureClient service
        /// </summary>
        public AudioCaptureClient AudioCaptureClient
        {
            get
            {
                if (audioCaptureClient == null)
                {
                    var audioCaptureClientGuid = new Guid("c8adbd64-e71e-48a0-a4de-185c395cd317");
                    Marshal.ThrowExceptionForHR(audioClientInterface.GetService(audioCaptureClientGuid, out var audioCaptureClientInterface));
                    audioCaptureClient = new AudioCaptureClient((IAudioCaptureClient)audioCaptureClientInterface);
                }
                return audioCaptureClient;
            }
        }

        /// <summary>
        /// Determines whether if the specified output format is supported
        /// </summary>
        /// <param name="shareMode">The share mode.</param>
        /// <param name="desiredFormat">The desired format.</param>
        /// <returns>True if the format is supported</returns>
        public bool IsFormatSupported(AudioClientShareMode shareMode,
            WAVEFORMATEX desiredFormat)
        {
            return IsFormatSupported(shareMode, desiredFormat, out _);
        }

        private IntPtr GetPointerToPointer()
        {
            return Marshal.AllocHGlobal(MarshalHelpers.SizeOf<IntPtr>());
        }

        /// <summary>
        /// Determines if the specified output format is supported in shared mode
        /// </summary>
        /// <param name="shareMode">Share Mode</param>
        /// <param name="desiredFormat">Desired Format</param>
        /// <param name="closestMatchFormat">Output The closest match format.</param>
        /// <returns>True if the format is supported</returns>
        public bool IsFormatSupported(AudioClientShareMode shareMode, WAVEFORMATEX desiredFormat, out WAVEFORMATEX closestMatchFormat)
        {
            IntPtr pointerToPtr = GetPointerToPointer(); // IntPtr.Zero; // Marshal.AllocHGlobal(Marshal.SizeOf<WaveFormatExtensible>());
            int hresult = audioClientInterface.IsFormatSupported(shareMode, desiredFormat, pointerToPtr);
            closestMatchFormat = new WAVEFORMATEX();
            var closestMatchPtr = MarshalHelpers.PtrToStructure<IntPtr>(pointerToPtr);

            if (closestMatchPtr != IntPtr.Zero)
            {
                closestMatchFormat = MarshalHelpers.PtrToStructure<WAVEFORMATEX>(closestMatchPtr);
                Marshal.FreeCoTaskMem(closestMatchPtr);
            }
            Marshal.FreeHGlobal(pointerToPtr);
            // S_OK is 0, S_FALSE = 1
            if (hresult == 0)
            {

                // directly supported
                return true;
            }
            if (hresult == 1)
            {
                return false;
            }
            if (hresult == (int)AudioClientErrors.UnsupportedFormat)
            {
                // documentation is confusing as to what this flag means
                // https://docs.microsoft.com/en-us/windows/desktop/api/audioclient/nf-audioclient-iaudioclient-isformatsupported
                // "Succeeded but the specified format is not supported in exclusive mode."
                return false; // shareMode != AudioClientShareMode.Exclusive;
            }
            Marshal.ThrowExceptionForHR(hresult);
            // shouldn't get here
            throw new NotSupportedException("Unknown hresult " + hresult);
        }

        /// <summary>
        /// Starts the audio stream
        /// </summary>
        public void Start()
        {
            audioClientInterface.Start();
        }

        /// <summary>
        /// Stops the audio stream.
        /// </summary>
        public void Stop()
        {
            audioClientInterface.Stop();
        }

        /// <summary>
        /// Set the Event Handle for buffer synchro.
        /// </summary>
        /// <param name="eventWaitHandle">The Wait Handle to setup</param>
        public void SetEventHandle(IntPtr eventWaitHandle)
        {
            audioClientInterface.SetEventHandle(eventWaitHandle);
        }

        /// <summary>
        /// Resets the audio stream
        /// Reset is a control method that the client calls to reset a stopped audio stream. 
        /// Resetting the stream flushes all pending data and resets the audio clock stream 
        /// position to 0. This method fails if it is called on a stream that is not stopped
        /// </summary>
        public void Reset()
        {
            audioClientInterface.Reset();
        }

        #region IDisposable Members

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (audioClientInterface != null)
            {
                if (audioCaptureClient != null)
                {
                    audioCaptureClient.Dispose();
                    audioCaptureClient = null;
                }
                Marshal.ReleaseComObject(audioClientInterface);
                audioClientInterface = null;
                GC.SuppressFinalize(this);
            }
        }

        #endregion
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct WAVEFORMATEX
    {
        public ushort wFormatTag;		// format type
        public ushort nChannels;		// number of channels (i.e. mono, stereo...)
        public uint nSamplesPerSec;     // sample rate
        public uint nAvgBytesPerSec;	// for buffer estimation
        public ushort nBlockAlign;      // block size of data
        public ushort wBitsPerSample;	// number of bits per sample of mono data
        public ushort cbSize;			// the count in bytes of
    }

    /// <summary>
    /// Support for Marshal Methods in both UWP and .NET 3.5
    /// </summary>
    public static class MarshalHelpers
    {
        /// <summary>
        /// SizeOf a structure
        /// </summary>
        public static int SizeOf<T>()
        {
            return Marshal.SizeOf<T>();
        }

        /// <summary>
        /// Offset of a field in a structure
        /// </summary>
        public static IntPtr OffsetOf<T>(string fieldName)
        {
            return Marshal.OffsetOf<T>(fieldName);
        }

        /// <summary>
        /// Pointer to Structure
        /// </summary>
        public static T PtrToStructure<T>(IntPtr pointer)
        {
            return Marshal.PtrToStructure<T>(pointer);
        }
    }

    /// <summary>
    /// Some useful native methods for Windows 8/10 support ( https://msdn.microsoft.com/en-us/library/windows/desktop/hh802935(v=vs.85).aspx )
    /// </summary>
    class NativeMethods
    {
        [DllImport("api-ms-win-core-synch-l1-2-0.dll", CharSet = CharSet.Unicode, ExactSpelling = false,
            PreserveSig = true, SetLastError = true)]
        internal static extern IntPtr CreateEventExW(IntPtr lpEventAttributes, IntPtr lpName, int dwFlags,
                                                    EventAccess dwDesiredAccess);


        [DllImport("api-ms-win-core-handle-l1-1-0.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        public static extern bool CloseHandle(IntPtr hObject);

        [DllImport("api-ms-win-core-synch-l1-2-0.dll", ExactSpelling = true, PreserveSig = true, SetLastError = true)]
        public static extern int WaitForSingleObjectEx(IntPtr hEvent, int milliseconds, bool bAlertable);

        /// <summary>
        /// Enables Windows Store apps to access preexisting Component Object Model (COM) interfaces in the WASAPI family.
        /// </summary>
        /// <param name="deviceInterfacePath">A device interface ID for an audio device. This is normally retrieved from a DeviceInformation object or one of the methods of the MediaDevice class.</param>
        /// <param name="riid">The IID of a COM interface in the WASAPI family, such as IAudioClient.</param>
        /// <param name="activationParams">Interface-specific activation parameters. For more information, see the pActivationParams parameter in IMMDevice::Activate. </param>
        /// <param name="completionHandler"></param>
        /// <param name="activationOperation"></param>
        [DllImport("Mmdevapi.dll", ExactSpelling = true, PreserveSig = false)]
        public static extern void ActivateAudioInterfaceAsync(
            [In, MarshalAs(UnmanagedType.LPWStr)] string deviceInterfacePath,
            [In, MarshalAs(UnmanagedType.LPStruct)] Guid riid,
            [In] IntPtr activationParams, // n.b. is actually a pointer to a PropVariant, but we never need to pass anything but null
            [In] IActivateAudioInterfaceCompletionHandler completionHandler,
            out IActivateAudioInterfaceAsyncOperation activationOperation);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("41D949AB-9862-444A-80F6-C261334DA5EB")]
    public interface IActivateAudioInterfaceCompletionHandler
    {
        //virtual HRESULT STDMETHODCALLTYPE ActivateCompleted(/*[in]*/ _In_  
        //   IActivateAudioInterfaceAsyncOperation *activateOperation) = 0;
        void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation);
    }


    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D")]
    public interface IActivateAudioInterfaceAsyncOperation
    {
        //virtual HRESULT STDMETHODCALLTYPE GetActivateResult(/*[out]*/ _Out_  
        //  HRESULT *activateResult, /*[out]*/ _Outptr_result_maybenull_  IUnknown **activatedInterface) = 0;
        void GetActivateResult([Out] out int activateResult,
                               [Out, MarshalAs(UnmanagedType.IUnknown)] out object activateInterface);
    }

    [Flags]
    internal enum EventAccess
    {
        STANDARD_RIGHTS_REQUIRED = 0xF0000,
        SYNCHRONIZE = 0x100000,
        EVENT_ALL_ACCESS = STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x3
    }

    internal class ActivateAudioInterfaceCompletionHandler :
        IActivateAudioInterfaceCompletionHandler, IAgileObject
    {
        private Action<IAudioClient> initializeAction;
        private TaskCompletionSource<IAudioClient> tcs = new TaskCompletionSource<IAudioClient>();

        public ActivateAudioInterfaceCompletionHandler(
            Action<IAudioClient> initializeAction)
        {
            this.initializeAction = initializeAction;
        }

        public void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation)
        {
            // First get the activation results, and see if anything bad happened then
            int hr = 0;
            object unk = null;
            activateOperation.GetActivateResult(out hr, out unk);
            if (hr != 0)
            {
                tcs.TrySetException(Marshal.GetExceptionForHR(hr, new IntPtr(-1)));
                return;
            }

            var pAudioClient = (IAudioClient)unk;

            // Next try to call the client's (synchronous, blocking) initialization method.
            try
            {
                initializeAction(pAudioClient);
                tcs.SetResult(pAudioClient);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }


        }


        public TaskAwaiter<IAudioClient> GetAwaiter()
        {
            return tcs.Task.GetAwaiter();
        }
    }

    /// <summary>
    /// AUDCLNT_STREAMFLAGS
    /// </summary>
    [Flags]
    public enum AudioClientStreamFlags
    {
        /// <summary>
        /// None
        /// </summary>
        None,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_CROSSPROCESS
        /// </summary>
        CrossProcess = 0x00010000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_LOOPBACK
        /// </summary>
        Loopback = 0x00020000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_EVENTCALLBACK 
        /// </summary>
        EventCallback = 0x00040000,
        /// <summary>
        /// AUDCLNT_STREAMFLAGS_NOPERSIST     
        /// </summary>
        NoPersist = 0x00080000,
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90")]
    interface IAgileObject
    {

    }
}
