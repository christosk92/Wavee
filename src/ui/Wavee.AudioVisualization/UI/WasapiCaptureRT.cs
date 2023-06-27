using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NAudio.CoreAudioApi;
using NAudio.CoreAudioApi.Interfaces;
using System.Threading;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;
using System.Runtime.CompilerServices;
using AudioVisualizerUWP;
using System.Linq;
using System.Numerics;

namespace NAudio.Wave
{
    enum WasapiCaptureState
    {
        Uninitialized,
        Stopped,
        Recording,
        Disposed
    }

    /// <summary>
    /// Audio Capture using Wasapi
    /// See http://msdn.microsoft.com/en-us/library/dd370800%28VS.85%29.aspx
    /// </summary>
    public class WasapiCaptureRT : IWaveIn
    {
        static readonly Guid IID_IAudioClient2 = new Guid("726778CD-F60A-4eda-82DE-E47610CD78AA");
        private const long REFTIMES_PER_SEC = 20000000;
        private const long REFTIMES_PER_MILLISEC = 20000;
        private volatile WasapiCaptureState captureState;
        private byte[] recordBuffer;
        private readonly string device;
        private int bytesPerFrame;
        private WaveFormat waveFormat;
        private AudioClient audioClient;
        private IntPtr hEvent;
        private Task captureTask;
        private readonly SynchronizationContext syncContext;

        /// <summary>
        /// Indicates recorded data is available 
        /// </summary>
        public event EventHandler<WaveInEventArgs> DataAvailable;

        public event EventHandler<AudioVisEventArgs> AudioDataAvailable;

        /// <summary>
        /// Indicates that all recorded data has now been received.
        /// </summary>
        public event EventHandler<StoppedEventArgs> RecordingStopped;

        /// <summary>
        /// The effective latency in milliseconds
        /// </summary>
        public int LatencyMilliseconds { get; private set; }

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        public WasapiCaptureRT() :
            this(GetDefaultCaptureDevice())
        {
        }

        /// <summary>
        /// Initialises a new instance of the WASAPI capture class
        /// </summary>
        /// <param name="device">Capture device to use</param>
        public WasapiCaptureRT(string device)
        {
            this.device = device;
            syncContext = SynchronizationContext.Current;
            //this.waveFormat = audioClient.MixFormat;
        }

        /// <summary>
        /// Recording wave format
        /// </summary>
        public virtual WaveFormat WaveFormat
        {
            get
            {
                // for convenience, return a WAVEFORMATEX, instead of the real
                // WAVEFORMATEXTENSIBLE being used
                if (waveFormat is WaveFormatExtensible wfe)
                {
                    try
                    {
                        return wfe.ToStandardWaveFormat();
                    }
                    catch (InvalidOperationException)
                    {
                        // couldn't convert to a standard format
                    }
                }
                return waveFormat;
            }
            set => waveFormat = value;
        }

        /// <summary>
        /// Way of enumerating all the audio capture devices available on the system
        /// </summary>
        /// <returns></returns>
        public static async Task<IEnumerable<DeviceInformation>> GetCaptureDevices()
        {
            var audioCaptureSelector = MediaDevice.GetAudioCaptureSelector();

            // (a PropertyKey)
            var supportsEventDrivenMode = "{1da5d803-d492-4edd-8c23-e0c0ffee7f0e} 7";

            var captureDevices = await DeviceInformation.FindAllAsync(audioCaptureSelector, new[] { supportsEventDrivenMode });
            return captureDevices;
        }

        /// <summary>
        /// Gets the default audio capture device
        /// </summary>
        /// <returns>The default audio capture device</returns>
        public static string GetDefaultCaptureDevice()
        {
            var defaultCaptureDeviceId = MediaDevice.GetDefaultAudioRenderId(AudioDeviceRole.Default);
            return defaultCaptureDeviceId;
        }

        /// <summary>
        /// Initializes the capture device. Must be called on the UI (STA) thread.
        /// If not called manually then StartRecording() will call it internally.
        /// </summary>
        public async Task InitAsync()
        {
            if (captureState == WasapiCaptureState.Disposed) throw new ObjectDisposedException(nameof(WasapiCaptureRT));
            if (captureState != WasapiCaptureState.Uninitialized) throw new InvalidOperationException("Already initialized");

            var icbh = new ActivateAudioInterfaceCompletionHandler(ac2 => InitializeCaptureDevice((IAudioClient)ac2));
            // must be called on UI thread
            NativeMethods.ActivateAudioInterfaceAsync(device, IID_IAudioClient2, IntPtr.Zero, icbh, out var activationOperation);
            audioClient = new AudioClient((IAudioClient)(await icbh));

            hEvent = NativeMethods.CreateEventExW(IntPtr.Zero, IntPtr.Zero, 0, EventAccess.EVENT_ALL_ACCESS);
            audioClient.SetEventHandle(hEvent);

            captureState = WasapiCaptureState.Stopped;
        }

        private void InitializeCaptureDevice(IAudioClient audioClientInterface)
        {
            var audioClient = new AudioClient((IAudioClient)audioClientInterface);
            if (waveFormat == null)
            {
                waveFormat = audioClient.MixFormat;
            }

            long requestedDuration = 0;//REFTIMES_PER_MILLISEC * 100;

            if (!audioClient.IsFormatSupported(AudioClientShareMode.Shared, waveFormat))
            {
                throw new ArgumentException("Unsupported Wave Format");
            }

            var streamFlags = GetAudioClientStreamFlags();

            audioClient.Initialize(AudioClientShareMode.Shared,
                streamFlags,
                requestedDuration,
                0,
                waveFormat,
                Guid.Empty);

            int bufferFrameCount = audioClient.BufferSize;
            bytesPerFrame = waveFormat.Channels * waveFormat.BitsPerSample / 8;
            recordBuffer = new byte[bufferFrameCount * bytesPerFrame];
            //Debug.WriteLine("record buffer size = {0}", this.recordBuffer.Length);

            // Get back the effective latency from AudioClient
            LatencyMilliseconds = (int)(audioClient.StreamLatency / 10000);
        }

        /// <summary>
        /// To allow overrides to specify different flags (e.g. loopback)
        /// </summary>
        protected virtual AudioClientStreamFlags GetAudioClientStreamFlags()
        {
            return AudioClientStreamFlags.EventCallback | AudioClientStreamFlags.Loopback;
        }

        /// <summary>
        /// Start Recording
        /// </summary>
        public async void StartRecording()
        {
            try
            {
                if (captureState == WasapiCaptureState.Disposed) throw new ObjectDisposedException(nameof(WasapiCaptureRT));
                if (captureState == WasapiCaptureState.Uninitialized) await InitAsync();
            }catch (Exception ex){
                Debug.WriteLine(ex.Message);
            }

            PrepareRecording();

            captureState = WasapiCaptureState.Recording;

            captureTask = Task.Run(() => DoRecording());

            Debug.WriteLine("Recording...");
        }


        /// <summary>
        /// Stop Recording
        /// </summary>
        public void StopRecording()
        {
            if (captureState == WasapiCaptureState.Disposed) throw new ObjectDisposedException(nameof(WasapiCaptureRT));
            if (captureState != WasapiCaptureState.Recording) return;

            captureState = WasapiCaptureState.Stopped;
            captureTask?.Wait(5000);
            //Debug.WriteLine("WasapiCaptureRT stopped");
        }

        private void DoRecording()
        {
            Debug.WriteLine("Recording buffer size: " + audioClient.BufferSize);

            var buf = new byte[audioClient.BufferSize * bytesPerFrame];

            int bufLength = 0;
            int minPacketSize = waveFormat.AverageBytesPerSecond / 100; //100ms

            try
            {
                AudioCaptureClient capture = audioClient.AudioCaptureClient;
                audioClient.Start();

                int packetSize = capture.GetNextPacketSize();

                while (captureState == WasapiCaptureState.Recording)
                {
                    if (packetSize == 0)
                    {
                        if (NativeMethods.WaitForSingleObjectEx(hEvent, 100, true) != 0)
                        {
                            throw new Exception("Capture event timeout");
                        }
                    }

                    var pData = capture.GetBuffer(out var numFramesToRead, out var dwFlags);

                    if ((int)(dwFlags & AudioClientBufferFlags.Silent) > 0)
                    {
                        pData = IntPtr.Zero;
                    }

                    if (numFramesToRead == 0) { continue; }

                    int capturedBytes = numFramesToRead * bytesPerFrame;

                    if (pData == IntPtr.Zero)
                    {
                        Array.Clear(buf, bufLength, capturedBytes);
                    }
                    else
                    {
                        Marshal.Copy(pData, buf, bufLength, capturedBytes);
                    }

                    bufLength += capturedBytes;

                    capture.ReleaseBuffer(numFramesToRead);

                    if (bufLength >= minPacketSize)
                    {
                        ProcessAudioData(buf, bufLength);
                        bufLength = 0;
                    }

                    packetSize = capture.GetNextPacketSize();
                }
            }
            catch (Exception ex)
            {
                RaiseRecordingStopped(ex);
                Debug.WriteLine("stop wasapi");
            }
            finally
            {
                RaiseRecordingStopped(null);

                audioClient.Stop();
            }
            Debug.WriteLine("stop wasapi");
        }

        double initFreq;
        double step;

        private void PrepareRecording()
        {
            fftScalar = (1.0 / Math.Sqrt(fftSize));

            if (Bands != 0)
            {
                m_bandFreq = new double[Bands];
                step = (Math.Log(freqMax / freqMin) / Bands) / Math.Log(2.0);
                m_bandFreq[0] = freqMin * (Math.Pow(2.0, step)); //(Math.Pow(2.0, step / 2.0d));
                initFreq = freqMin / (Math.Pow(2.0, step));

                df = (double)WaveFormat.SampleRate / fftBufferSize;
                bandScalar = 2.0d / WaveFormat.SampleRate;

                for (int iBand = 1; iBand < Bands; ++iBand)
                {
                    m_bandFreq[iBand] = (float)(m_bandFreq[iBand - 1] * Math.Pow(2.0, step));
                }

                //m_bandOut = (float*)calloc(m_nBands * sizeof(float), 1);
            }

            switch (Window)
            {
                case WFWindow.HammingWindow:
                    fftWindow = new double[fftSize];
                    for (int i = 0; i < fftSize; i++)
                    {
                        //fftWindow[i] = NAudio.Dsp.FastFourierTransform.HammingWindow(i, fftSize);
                        fftWindow[i] = (0.5 * (1.0 - Math.Cos((Math.PI*2) * i / (fftSize + 1))));
                    }
                    fftWindow[0] = 0d;
                    break;
                case WFWindow.HannWindow:
                    fftWindow = new double[fftSize];
                    for (int i = 0; i < fftSize; i++)
                    {
                        fftWindow[i] = NAudio.Dsp.FastFourierTransform.HannWindow(i, fftSize);
                    }
                    break;
                case WFWindow.BlackmannHarrisWindow:
                    fftWindow = new double[fftSize];
                    for (int i = 0; i < fftSize; i++)
                    {
                        fftWindow[i] = NAudio.Dsp.FastFourierTransform.BlackmannHarrisWindow(i, fftSize);
                    }
                    break;
                default:
                    break;
            }
        }

        double fftScalar;
        double bandScalar;
        double df;
        double[] m_bandFreq;
        double[] fftWindow;
        bool animationDone = false;
        int offset = 0;
        double dOffset = 0;
        double freqOffset = 0;

        private void ProcessAudioData(byte[] buffer, int nBytesRecorded)
        {
            var watch = System.Diagnostics.Stopwatch.StartNew();
            if (nBytesRecorded == 0)
            {
                return;
            }

            int nChannels = WaveFormat.Channels;
            int nBitsPerSample = WaveFormat.BitsPerSample;
            int sampleRate = WaveFormat.SampleRate;
            int nBytesPerInt = nBitsPerSample / 8;
            int nFrames = nBytesRecorded / nBytesPerInt;
            //int nFramesPerChannel = nFrames / nBytesPerInt;

            // ##############
            // # Parse Data #
            // ##############
            #region ParseArray

            double[] totalFrames = new double[nFrames];

            if (nBitsPerSample == 16)
            {
                //Debug.WriteLine(16);
                //Convert byte array to int array: Every 2 bytes = 1 int16
                for (int i = 0; i < nFrames; i++)
                {
                    totalFrames[i] = Convert.ToDouble(BitConverter.ToInt16(buffer, i * nBytesPerInt));
                }
            }
            else if (nBitsPerSample == 32)
            {
                for (int i = 0; i < nFrames; i++)
                {
                    totalFrames[i] = Convert.ToDouble(BitConverter.ToSingle(buffer, i * nBytesPerInt) * short.MaxValue);
                    //Debug.WriteLine("Val: " + val + ", TD: " + Convert.ToDouble(val));
                }
                /*
                byte[] newArray16Bit = new byte[nBytesRecorded / 2];
                short two;
                float value;
                for (int i = 0, j = 0; i < nBytesRecorded; i += 4, j += 2)
                {
                    value = (BitConverter.ToSingle(buffer, i));
                    two = (short)(value * short.MaxValue);

                    newArray16Bit[j] = (byte)(two & 0xFF);
                    newArray16Bit[j + 1] = (byte)((two >> 8) & 0xFF);
                }


                //Debug.WriteLine(32);
                //Convert byte array to int array: Every 4 bytes = 1 float
                for (int i = 0; i < nBytesRecorded / 2; i += 2)
                {
                    totalFrames[i / nBytesPerInt] = Convert.ToDouble(BitConverter.ToInt16(newArray16Bit, i));
                }*/
            }

            /*
            double[][] lstChannelFrames = new double[nChannels][];

            for (int i = 0; i < nChannels - 1; i++)
            {
                lstChannelFrames[i] = new double[nFramesPerChannel];
            }

            // assign each frame to its channel. Each frame of a channel has a spacing nChannels-1 form the next frame of that channel: 
            // int[] totalframes = { frame1Channel1, frame1Channel2, frame2Channel1, frame2Channel2... }
            for (int i = 0; i < nFrames; i += nChannels)
            {
                for (int channel = 0; channel < nChannels - 1; channel++)
                {
                    lstChannelFrames[channel][i / nChannels] = totalFrames[i];
                }
            }*/

            for (int iFrame = 0; iFrame < nFrames; iFrame++)
            {
                for (int iChan = 0; iChan < nChannels; iChan++)
                {
                    if (Channel == nChannels)
                    {
                        if (iChan == 0)
                        {
                            // cannot increment before evaluation
                            double L = totalFrames[iFrame];
                            double R = totalFrames[iFrame + 1];

                            fftCircularBuffer[fftBufIndex] = (L + R) / 2;
                        }
                        else
                        {
                            iFrame++;
                        }
                    }
                    else if (Channel == iChan)
                    {
                        fftCircularBuffer[fftBufIndex] = totalFrames[iFrame];
                    }
                    else
                    {
                        iFrame++;
                    }
                }
                fftBufIndex = (fftBufIndex + 1) % fftSize;

                // move along the data-to-process buffer
            }

            double[] fftFrames = new double[fftBufferSize];
            //rearrage array
            Array.Copy(fftCircularBuffer, fftBufIndex, fftFrames, 0, fftSize - fftBufIndex);
            Array.Copy(fftCircularBuffer, 0, fftFrames, fftSize - fftBufIndex, fftBufIndex);

            #endregion

            // ########################
            // # Apply Hamming Window #
            // ########################
            #region ApplyWindow
            if (Window != WFWindow.None)
            {
                for (int i = 0; i < fftSize; i++)
                {
                    fftFrames[i] *= fftWindow[i];
                }
            }
            #endregion

            // #############
            // # Apply FFT #
            // #############
            #region FFT
            double[] audioData;

            // find smallest power of 2 that is bigger than channel0.Length
            //double arrlen = Math.Pow(2, Math.Ceiling(Math.Log(channel0.Length, 2)));
            if (!UseFFT)
            {
                audioData = fftFrames.Select(x => ((x / (short.MaxValue * 2))) + 0.5).ToArray();
                goto NoFFT;
            }


            audioData = new double[fftBufferSize];
            double[] adc = fftFrames.Select(x => x / short.MaxValue).ToArray();
            fft.FFT(adc, audioData);


                for (int i = 0; i < audioData.Length; i++)
                {
                //Debug.WriteLine(audioData[i]);
                audioData[i] = audioData[i] * fftScalar;
                }

            //Debug.WriteLine(o);
            //Debug.WriteLine("LSTF: " + lstChannelFrames[0][0] + ", audioData: " + audioData[0] + ", adc: " + adc[0]);
            //fft.RealFFT(ffttemp, false);

            //FastFourierTransform.FFT(true, (int)Math.Log(fftBufferSize, 2), cffttemp);
            #endregion

            // ###########################
            // # Extract Frequency Range #
            // ###########################
            /*
            #region FrquencyRange
            int fs = WaveFormat.SampleRate; //Wave freq
            int fn = fs / 2; //Max freq
            int bl = fftBufferSize;
            double df = fs / bl;

            int endidx = (int)(freqMax / df);
            int startidx = (int)(freqMin / df);

            audioData = audioData.SubArray(startidx, endidx);
            #endregion*/

            // ################
            // # Attack/Decay #
            // ################
            #region AttackDecay

            if (audioDataOld == null)
            {
                audioDataOld = audioData;
                goto Bands;
            }
            else if (audioDataOld.Length != audioData.Length)
            {
                audioDataOld = audioData;
                goto Bands;
            }

            for (int i = 0; i < audioData.Length; i++)
            {
                double oldVal = audioDataOld[i];
                double newVal = audioData[i];

                if (newVal < oldVal)
                {
                    //Decay
                    audioData[i] = newVal + attack * (oldVal - newVal);
                }
                else if (newVal > oldVal)
                {
                    //Attack
                    audioData[i] = newVal + decay * (oldVal - newVal);
                }
            }

            audioDataOld = audioData;


        #endregion

            // #########
            // # Bands #
            // #########
            #region Bands
            Bands:

            if (Bands == 0)
            {
                watch.Stop();
                AudioDataAvailable?.Invoke(this, new AudioVisEventArgs(audioData, audioData.Length,watch.ElapsedTicks));
                return;
            }

            double[] m_bandOut = new double[Bands];
            double[] m_bandFreqTemp = new double[Bands];

            double frqAni = 1000;

            //Array.Copy(m_bandFreq, m_bandFreqTemp, Bands);

            //m_bandFreq[Bands - 1] = m_bandFreqTemp[0];
            //for (int i = 0; i < Bands-1; i++)
            //{
            //    m_bandFreq[i] = m_bandFreqTemp[i + 1];
            //}
            double offsetspeed = 1;

            dOffset += offsetspeed;
            offset = (int)dOffset;
            if (offset >= Bands)
            {
                offset = 0;
                dOffset = 0;
            }

            /*
            if (!animationDone)
            {
                freqMin++;
                for (int i = 0; i < Bands; i++)
                {
                    m_bandFreq[i] += 1;
                }

                if (m_bandFreq[Bands - 1] > frqAni)
                {
                    animationDone = true;
                }
            }
            else
            {
                freqMin--;
                for (int i = 0; i < Bands; i++)
                {
                    m_bandFreq[i] -= 1;
                }

                if (m_bandFreq[0] <= 20)
                {
                    animationDone = false;
                }
            }*/

            int iBin = (int)((freqMin / df) - 0.5);
            int iBand = 0;
            double f0 = freqMin;

            //LinToLog(m_bandOut, audioData, logFreqs, ref f0, bandoffset);


            while (iBin <= (fftBufferSize * 0.5) && iBand < Bands)
            {
                //Debug.WriteLine("YE" + iBand + ", ofb:" + (iBand + offset));
                double fLin1 = (iBin + 0.5) * df;   //linear freq.
                double fLog1 = m_bandFreq[iBand]; //logarythmic freq.

                if (fLin1 <= fLog1)
                {
                    m_bandOut[iBand] += (fLin1 - f0) * audioData[iBin] * bandScalar;
                    f0 = fLin1;
                    iBin += 1;
                }
                else
                {
                    m_bandOut[iBand] += (fLog1 - f0) * audioData[iBin] * bandScalar;
                    f0 = fLog1;
                    iBand += 1;
                }
            }

            /*
            freqOffset += 2;
            if (freqOffset >= freqMax)
            {
                freqOffset = 1;
            }


            double logfreqMax = freqMax;//m_bandFreq[Bands - 1];
            double offsetfreqmin = freqMax - freqOffset;

            int nOffBands = 1;
            m_bandFreq[0] = offsetfreqmin * Math.Pow(2.0, step);
            for (int nOffBandst = 1; m_bandFreq[nOffBandst - 1] < logfreqMax; nOffBandst++)
            {
                if (nOffBands > Bands - 1)
                {
                    freqOffset = 0;
                    break;
                }
                m_bandFreq[nOffBandst] = m_bandFreq[nOffBandst - 1] * Math.Pow(2.0, step);
                nOffBands++;
            }
            


            Debug.WriteLine("FreqMax: " + m_bandFreq[Bands-1] + ", FreqMin: " + m_bandFreq[0] + "off: " + offsetfreqmin+ ", nOffBands: " + nOffBands);

            //skip first band
            //otherwise:
            int iBin = (int)((offsetfreqmin / df)-0.5);
            int iBand = 0;
            double f0 = offsetfreqmin;

            while (iBin <= (fftBufferSize * 0.5) && iBand < nOffBands)
            {
                //Debug.WriteLine("YE"+ iBand + ", freq:" + m_bandFreq[iBand] + ", iBin: " + iBin);
                double fLin1 = (iBin + 0.5) * df;   //linear freq.
                double fLog1 = m_bandFreq[iBand]; //logarythmic freq.

                if (fLin1 <= fLog1)
                {
                    m_bandOut[iBand] += (fLin1 - f0) * audioData[iBin] * bandScalar;
                    f0 = fLin1;
                    iBin += 1;
                }
                else
                {
                    m_bandOut[iBand] += (fLog1 - f0) * audioData[iBin] * bandScalar;
                    f0 = fLog1;
                    iBand += 1;
                }
            }

            iBand--;  

            m_bandFreq[nOffBands-1] = freqMin * (Math.Pow(2.0, step)); //(Math.Pow(2.0, step / 2.0d));

            for (int iBandx = nOffBands; iBandx < (Bands - nOffBands); ++iBandx)
            {
                m_bandFreq[iBandx] = (float)(m_bandFreq[iBandx - 1] * Math.Pow(2.0, step));
            }

            iBin = (int)((freqMin / df) - 0.5);
            f0 = freqMin;

            while (iBin <= (fftBufferSize * 0.5) && iBand < Bands)
            {
                //Debug.WriteLine("YE"+ iBand + ", freq:" + m_bandFreq[iBand] + ", iBin: " + iBin);
                double fLin1 = (iBin + 0.5) * df;   //linear freq.
                double fLog1 = m_bandFreq[iBand]; //logarythmic freq.

                if (fLin1 <= fLog1)
                {
                    m_bandOut[iBand] += (fLin1 - f0) * audioData[iBin] * bandScalar;
                    f0 = fLin1;
                    iBin += 1;
                }
                else
                {
                    m_bandOut[iBand] += (fLog1 - f0) * audioData[iBin] * bandScalar;
                    f0 = fLog1;
                    iBand += 1;
                }
            }*/

            for (int i = 0; i < m_bandOut.Length; i++)
            {
                //Debug.WriteLine("ad" +audioData[i]);
                m_bandOut[i] = Math.Max(0, sensitivity * Math.Log10(Clamp01(m_bandOut[i])) + 1.0);
                //Debug.WriteLine("Log10(" + m_bandOut[i] + ") = " + sensitivity * Math.Log10(m_bandOut[i]) + ", BendLogValue: " + blv);
            }

            watch.Stop();
            //Debug.WriteLine(fftBufIndex + ", " + m_bandOut.Length);
            AudioDataAvailable?.Invoke(this, new AudioVisEventArgs(m_bandOut, m_bandOut.Length,watch.Elapsed.TotalMilliseconds));
            return;
            NoFFT:
            
            if (!UseFFT)
            {
                /*
                int nFramesPerBand = fftSize / Bands;
                int unassignedFrames = fftSize % Bands;

                //Debug.WriteLine(audioData.Length);
                Debug.WriteLine("Frames per Band: " + nFramesPerBand + ", Unassigned Frames: " + unassignedFrames);

                int audioDataPosition = 0;

                double[] bandValues = new double[Bands];

                for (int i = 0; i < Bands; i++)
                {
                    int framesPerBand = nFramesPerBand;
                    if (i < unassignedFrames)
                    {
                        framesPerBand++;
                    }

                    double bandValue;
                    if (audioDataPosition < fftSize)
                    {
                        bandValue = audioData.SubArray(audioDataPosition, framesPerBand).Average();
                    }
                    else
                    {
                        bandValue = 0;
                    }
                    bandValues[i] = bandValue;

                    audioDataPosition += framesPerBand;
                }*/

                double[] bandValues = new double[Bands];

                int iBinn = 0;
                int iBandn = 0;
                float f0n = 0;
                float vPerBand = (fftSize / Bands);

                while (iBinn <= fftSize && iBandn < Bands)
                {
                    float fLin1 = iBinn;
                    float fLog1 = vPerBand * iBandn;

                    if (fLin1 < fLog1)
                    {
                        bandValues[iBandn] += (fLin1 - f0n) * audioData[iBinn];
                        f0n = fLin1;
                        iBinn += 1;
                    }
                    else
                    {
                        bandValues[iBandn] += (fLog1 - f0n) * audioData[iBinn];
                        bandValues[iBandn] /= vPerBand;
                        f0n = fLog1;
                        iBandn += 1;
                    }
                }

                watch.Stop();

                AudioDataAvailable?.Invoke(this, new AudioVisEventArgs(bandValues, bandValues.Length, watch.Elapsed.TotalMilliseconds));
            }
            
            #endregion
        }

        public void MoveLin(double[] bandOut, double[] audioData, double[] logFreqs, double linfreq0, int bandoffset)
        {
            int iBin = (int)((freqMin / df) - 0.5);
            int iBand = offset;
            double f0 = freqMin;

            LinToLog(bandOut, audioData, logFreqs, ref f0, bandoffset);

            iBand = 0;

            while (iBin <= (fftBufferSize * 0.5) && iBand < offset)
            {
                //Debug.WriteLine("YE" + iBand + ", ofb:" + (iBand + offset));
                double fLin1 = (iBin + 0.5) * df;   //linear freq.
                double fLog1 = m_bandFreq[Bands - offset + iBand]; //logarythmic freq.

                if (fLin1 <= fLog1)
                {
                    bandOut[iBand] += (fLin1 - f0) * audioData[iBin] * bandScalar;
                    f0 = fLin1;
                    iBin += 1;
                }
                else
                {
                    bandOut[iBand] += (fLog1 - f0) * audioData[iBin] * bandScalar;
                    f0 = fLog1;
                    iBand += 1;
                }
            }
        }

        public void LinToLog(double[] bandOut, double[] audioData, double[] logFreqs, ref double linfreq0, int bandoffset)
        {
            int iBin = (int)((linfreq0 / df) - 0.5);
            int iBand = bandoffset;
            double f0 = linfreq0;

            while (iBin <= (fftBufferSize * 0.5) && iBand < Bands)
            {
                //Debug.WriteLine("YE"+ iBand + ", ofb:" + (iBand + offset));
                double fLin1 = (iBin + 0.5) * df;   //linear freq.
                double fLog1 = logFreqs[iBand - bandoffset]; //logarythmic freq.

                if (fLin1 <= fLog1)
                {
                    //bandout          deltaFreq
                    bandOut[iBand] += (fLin1 - f0) * audioData[iBin] * bandScalar;
                    f0 = fLin1;
                    iBin += 1;
                }
                else
                {
                    bandOut[iBand] += (fLog1 - f0) * audioData[iBin] * bandScalar;
                    f0 = fLog1;
                    iBand += 1;
                }
            }

            linfreq0 = f0;
        }

        public double Clamp01(double x)
        {
            return Math.Max(0.0, Math.Min(1.0, (x)));
        }

        private double[] fftCircularBuffer = new double[8192];
        private int fftSize = 8192;
        private int fftBufferSize = 8192*4;
        private int fftBufIndex = 0;
        private int bandNum = 0;
        private double freqMin = 20;
        private double freqMax = 200;

        private double sensitivity = 0.2;
        private double attack = 0;
        private double decay = 0;
        private double[] audioDataOld;
        private FFT fft;


        public bool UseFFT { get; set; } = true;
        public bool UseLogScale { get; set; } = false;

        public int Bands
        {
            get
            {
                return bandNum;
            }
            set
            {
                bandNum = value;
            }
        }

        //LomontFFT fft = new LomontFFT();
        //IntPtr kfftconfig;

        /// <summary>
        /// Zero Based Recording Channel number
        /// Default: All
        /// </summary>
        public int Channel { get; set; } = 2;

        public double Sensitivity
        {
            get
            {
                return 10d / sensitivity;
            }
            set
            {
                sensitivity = 10d / Math.Max(1d, value);
                Debug.WriteLine("hhhhhh:"+sensitivity);
            }
        }

        public double FreqMin
        {
            get
            {
                return freqMin;
            }
            set
            {
                if (value < 20d || value > 20000d)
                {
                    throw new ArgumentOutOfRangeException("FreqMin", value, "The min frequency has to be between 20hz and 20000hz");
                }
                else
                {
                    freqMin = value;
                }
            }
        }

        public double FreqMax
        {
            get
            {
                return freqMax;
            }
            set
            {
                if (value < 20d || value > 20000d)
                {
                    throw new ArgumentOutOfRangeException("FreqMax", value, "The max frequency has to be between 20hz and 20000hz");
                }
                else
                {
                    freqMax = value;
                }
            }
        }

        public int FFTSize
        {
            get
            {
                return fftSize;
            }
            set
            {
                fftSize = value;
                fftCircularBuffer = new double[value];
            }
        }

        public int FFTBufferSize
        {
            get
            {
                return fftBufferSize;
            }
            set
            {
                //Check if the value is a power of 2
                decimal logFFT = (decimal)Math.Log(value, 2);
                Debug.WriteLine(logFFT + ", " + (logFFT % 1));

                if ((logFFT % 1) == 0)
                {
                    Debug.WriteLine("SET FFTBS1: " + value);
                    fftBufferSize = value;
                }
                else
                {
                    Debug.WriteLine("SET FFTBS2: " + (int)Math.Pow((double)logFFT, 2));
                    fftBufferSize = (int)Math.Pow((double)logFFT, 2);
                }

                if (FFTEngine == FFTEngine.KissFFT)
                {
                    fft = new KissFFTR(fftBufferSize);
                }
            }
        }

        public double Attack
        {
            set
            {
                attack = Math.Exp(Math.Log10(0.01) / (WaveFormat.SampleRate * 0.001 * value * 0.001));
            }
        }

        public double Decay
        {
            set
            {
                decay = Math.Exp(Math.Log10(0.01) / (WaveFormat.SampleRate * 0.001 * value * 0.001));
            }
        }

        public WFWindow Window { get; set; } = WFWindow.HammingWindow;

        public FFTEngine FFTEngine
        {
            get
            {
                if (fft == null)
                {
                    return FFTEngine.LomontFFT; // Default FFT engine
                }

                if (fft.GetType() == typeof(KissFFTR))
                {
                    return FFTEngine.KissFFT;
                }
                else if (fft.GetType() == typeof(LomFFT))
                {
                    return FFTEngine.LomontFFT;
                }
                else
                {
                    return FFTEngine.NAudioFFT;
                }
            }
            set
            {
                switch (value)
                {
                    case FFTEngine.KissFFT:
                        fft = new KissFFTR(fftBufferSize);
                        break;
                    case FFTEngine.LomontFFT:
                        fft = new LomFFT();
                        break;
                    case FFTEngine.NAudioFFT:
                        break;
                }
            }
        }

        private void RaiseRecordingStopped(Exception exception)
        {
            var handler = RecordingStopped;
            if (handler != null)
            {
                if (syncContext == null)
                {
                    handler(this, new StoppedEventArgs(exception));
                }
                else
                {
                    syncContext.Post(state => handler(this, new StoppedEventArgs(exception)), null);
                }
            }
        }

        private void ReadNextPacket(AudioCaptureClient capture)
        {
            IntPtr buffer;
            int framesAvailable;
            AudioClientBufferFlags flags;
            int packetSize = capture.GetNextPacketSize();
            int recordBufferOffset = 0;
            //Debug.WriteLine(string.Format("packet size: {0} samples", packetSize / 4));

            while (packetSize != 0)
            {
                buffer = capture.GetBuffer(out framesAvailable, out flags);

                int bytesAvailable = framesAvailable * bytesPerFrame;

                // apparently it is sometimes possible to read more frames than we were expecting?
                // fix suggested by Michael Feld:
                int spaceRemaining = Math.Max(0, recordBuffer.Length - recordBufferOffset);
                if (spaceRemaining < bytesAvailable && recordBufferOffset > 0)
                {
                    if (DataAvailable != null) DataAvailable(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
                    recordBufferOffset = 0;
                }

                // if not silence...
                if ((flags & AudioClientBufferFlags.Silent) != AudioClientBufferFlags.Silent)
                {
                    Marshal.Copy(buffer, recordBuffer, recordBufferOffset, bytesAvailable);
                }
                else
                {
                    Array.Clear(recordBuffer, recordBufferOffset, bytesAvailable);
                }
                recordBufferOffset += bytesAvailable;
                capture.ReleaseBuffer(framesAvailable);
                packetSize = capture.GetNextPacketSize();
            }
            if (DataAvailable != null)
            {
                DataAvailable(this, new WaveInEventArgs(recordBuffer, recordBufferOffset));
            }
        }

        /// <summary>
        /// Dispose
        /// </summary>
        public void Dispose()
        {
            if (captureState == WasapiCaptureState.Disposed) return;

            try
            {
                StopRecording();

                NativeMethods.CloseHandle(hEvent);
                audioClient?.Dispose();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Exception disposing WasapiCaptureRT: " + ex.ToString());
            }

            hEvent = IntPtr.Zero;
            audioClient = null;

            captureState = WasapiCaptureState.Disposed;
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

    // trying some ideas from Lucian Wischik (ljw1004):
    // http://www.codeproject.com/Articles/460145/Recording-and-playing-PCM-audio-on-Windows-8-VB

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
        private Action<IAudioClient2> initializeAction;
        private TaskCompletionSource<IAudioClient2> tcs = new TaskCompletionSource<IAudioClient2>();

        public ActivateAudioInterfaceCompletionHandler(
            Action<IAudioClient2> initializeAction)
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

            var pAudioClient = (IAudioClient2)unk;

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


        public TaskAwaiter<IAudioClient2> GetAwaiter()
        {
            return tcs.Task.GetAwaiter();
        }
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("41D949AB-9862-444A-80F6-C261334DA5EB")]
    interface IActivateAudioInterfaceCompletionHandler
    {
        //virtual HRESULT STDMETHODCALLTYPE ActivateCompleted(/*[in]*/ _In_  
        //   IActivateAudioInterfaceAsyncOperation *activateOperation) = 0;
        void ActivateCompleted(IActivateAudioInterfaceAsyncOperation activateOperation);
    }


    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("72A22D78-CDE4-431D-B8CC-843A71199B6D")]
    interface IActivateAudioInterfaceAsyncOperation
    {
        //virtual HRESULT STDMETHODCALLTYPE GetActivateResult(/*[out]*/ _Out_  
        //  HRESULT *activateResult, /*[out]*/ _Outptr_result_maybenull_  IUnknown **activatedInterface) = 0;
        void GetActivateResult([Out] out int activateResult,
                               [Out, MarshalAs(UnmanagedType.IUnknown)] out object activateInterface);
    }


    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("726778CD-F60A-4eda-82DE-E47610CD78AA")]
    interface IAudioClient2
    {
        [PreserveSig]
        int Initialize(AudioClientShareMode shareMode,
                       AudioClientStreamFlags streamFlags,
                       long hnsBufferDuration, // REFERENCE_TIME
                       long hnsPeriodicity, // REFERENCE_TIME
                       [In] WaveFormat pFormat,
                       [In] IntPtr audioSessionGuid);

        // ref Guid AudioSessionGuid

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
            [In] WaveFormat pFormat,
            out IntPtr closestMatchFormat);

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
        int GetService([In, MarshalAs(UnmanagedType.LPStruct)] Guid interfaceId,
                       [Out, MarshalAs(UnmanagedType.IUnknown)] out object interfacePointer);

        //virtual HRESULT STDMETHODCALLTYPE IsOffloadCapable(/*[in]*/ _In_  
        //   AUDIO_STREAM_CATEGORY Category, /*[in]*/ _Out_  BOOL *pbOffloadCapable) = 0;
        void IsOffloadCapable(int category, out bool pbOffloadCapable);
        //virtual HRESULT STDMETHODCALLTYPE SetClientProperties(/*[in]*/ _In_  
        //  const AudioClientProperties *pProperties) = 0;
        void SetClientProperties([In] IntPtr pProperties);
        // TODO: try this: void SetClientProperties([In, MarshalAs(UnmanagedType.LPStruct)] AudioClientProperties pProperties);
        //virtual HRESULT STDMETHODCALLTYPE GetBufferSizeLimits(/*[in]*/ _In_  
        //   const WAVEFORMATEX *pFormat, /*[in]*/ _In_  BOOL bEventDriven, /*[in]*/ 
        //  _Out_  REFERENCE_TIME *phnsMinBufferDuration, /*[in]*/ _Out_  
        //  REFERENCE_TIME *phnsMaxBufferDuration) = 0;
        void GetBufferSizeLimits(IntPtr pFormat, bool bEventDriven,
                                 out long phnsMinBufferDuration, out long phnsMaxBufferDuration);
    }

    [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("94ea2b94-e9cc-49e0-c0ff-ee64ca8f5b90")]
    interface IAgileObject
    {

    }
}