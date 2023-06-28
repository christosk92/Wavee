using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AudioEffectsLib.FastFourierTransform;

namespace AudioEffectsLib
{
    public sealed class AudioSpectrum : IDisposable
    {
        private double[] fftCircularBuffer = new double[8192];
        private int fftSize = 8192;
        private int fftBufferSize = 8192 * 4;
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

        double fftScalar;
        double bandScalar;
        double df;
        double[] m_bandFreq;
        double[] fftWindow;

        byte[] frameQueue;
        int frameQueueSize = 100000;
        int frameQueuePosition = 0;

        public int SampleRate { get; set; }
        public int BitsPerSample { get; set; }
        public int Channels { get; set; }

        public event EventHandler<AudioVisEventArgs> SpectrumDataAvailable;

        public AudioSpectrum(int sampleRate, int bitsPerSample, int channels)
        {
            SampleRate = sampleRate;
            BitsPerSample = bitsPerSample;
            Channels = channels;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            buffer = buffer.Skip(offset).ToArray();
            Write(buffer, count);
        }

        public void Write(byte[] buffer, int count)
        {
            //Task.Run(() => ProcessAudioData(buffer, count));
            ProcessAudioData(buffer, count);
        }


        Task frameProcTask;
        public IAudioWaveSource AudioClient { get; set; }
        bool processstate = false;

        private async void DoRecording()
        {
            int packetSize;

            while (processstate)
            {
                packetSize = AudioClient.GetNextPacketSize();
                //Debug.WriteLine(packetSize);

                if (packetSize == 0)
                {
                    /*
                    if (NativeMethods.WaitForSingleObjectEx(hEvent, 100, true) != 0)
                    {
                        throw new Exception("Capture event timeout");
                    }*/

                    continue;
                }

                var pData = AudioClient.GetBuffer(out var numFramesRead, out var flags);

                //Debug.WriteLine("Requested: " + packetSize + ", Read: " + numFramesRead);

                if (numFramesRead == 0 || flags == AudioWaveSourceBufferFlags.Silent) { continue; }

                ProcessAudioData(pData, numFramesRead);

                AudioClient.ReleaseBuffer(numFramesRead);
                await Task.Delay(5);
                GC.Collect();
            }

            AudioClient.ClearBuffer();

        }

        /// <summary>
        /// Call this before you write any data and after you set/changed any Property
        /// </summary>
        public void Start()
        {
            Stop();
            frameQueue = new byte[frameQueueSize];

            attack = Math.Exp(Math.Log10(0.01) / (SampleRate * 0.001 * Attack * 0.001));
            decay = Math.Exp(Math.Log10(0.01) / (SampleRate * 0.001 * Decay * 0.001));

            fftSize = FFTSize;

            fftCircularBuffer = new double[fftSize];

            switch (FFTEngine)
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

            fftScalar = (1.0 / Math.Sqrt(fftSize));

            if (Bands != 0)
            {
                m_bandFreq = new double[Bands];
                double step = (Math.Log(freqMax / freqMin) / Bands) / Math.Log(2.0);
                m_bandFreq[0] = freqMin * (Math.Pow(2.0, step / 2.0d));
                //initFreq = freqMin / (Math.Pow(2.0, step / 2.0d));

                df = (double)SampleRate / fftBufferSize;
                bandScalar = 2.0d / SampleRate;

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
                        fftWindow[i] = (0.5 * (1.0 - Math.Cos((Math.PI * 2) * i / (fftSize + 1))));
                    }
                    fftWindow[0] = 0d;
                    break;
                case WFWindow.HannWindow:
                    fftWindow = new double[fftSize];
                    for (int i = 0; i < fftSize; i++)
                    {
                        fftWindow[i] = 0.5 * (1 - Math.Cos((2 * Math.PI * i) / (fftSize - 1)));
                    }
                    break;
                case WFWindow.BlackmannHarrisWindow:
                    fftWindow = new double[fftSize];
                    for (int i = 0; i < fftSize; i++)
                    {
                        fftWindow[i] = 0.35875 - (0.48829 * Math.Cos((2 * Math.PI * i) / (fftSize - 1))) + (0.14128 * Math.Cos((4 * Math.PI * i) / (fftSize - 1))) - (0.01168 * Math.Cos((6 * Math.PI * i) / (fftSize - 1)));
                    }
                    break;
                default:
                    break;
            }

            AudioClient = new AudioStreamWaveSource(SampleRate, Channels, BitsPerSample);
            processstate = true;
            frameProcTask = new Task(DoRecording);
            frameProcTask.Start();
        }

        public void Stop()
        {
            processstate = false;
            frameProcTask?.Wait(100);
            frameProcTask = null;
        }

        private void ProcessAudioData(byte[] buffer, int count)
        {
            try
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();
                if (count == 0)
                {
                    return;
                }

                int nBytesPerInt = BitsPerSample / 8;
                int nFrames = count / nBytesPerInt;
                //Debug.WriteLine("Frames: " + nFrames*2);

                // ##############
                // # Parse Data #
                // ##############

                #region ParseArray

                double[] totalFrames = new double[nFrames];

                if (BitsPerSample == 16)
                {
                    //Convert bytes to int16: Every 2 bytes = 1 int16
                    for (int i = 0; i < nFrames; i++)
                    {
                        totalFrames[i] = Convert.ToDouble(BitConverter.ToInt16(buffer, i * nBytesPerInt));
                        //Debug.WriteLine("D: " + totalFrames[i]);
                    }
                }
                else if (BitsPerSample == 32)
                {
                    //Convert bytes to floats: Every 4 bytes = 1 float
                    for (int i = 0; i < nFrames; i++)
                    {
                        totalFrames[i] =
                            Convert.ToDouble(BitConverter.ToSingle(buffer, i * nBytesPerInt) * short.MaxValue);
                        //Debug.WriteLine("D: " + totalFrames[i]);
                    }

                    //Debug.WriteLine("D: " + totalFrames[0]);
                }

                for (int iFrame = 0; iFrame < nFrames; iFrame++)
                {
                    for (int iChan = 0; iChan < Channels; iChan++)
                    {
                        if (Channel == Channels)
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

                //Debug.WriteLine("YEYE: " + fftCircularBuffer[fftBufIndex]);

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
                    SpectrumDataAvailable?.Invoke(this,
                        new AudioVisEventArgs(audioData, audioData.Length, watch.ElapsedTicks));
                    return;
                }

                double[] m_bandOut = new double[Bands];

                //skip first band
                //otherwise:
                int iBin = (int)((freqMin / df) - 0.5);
                int iBand = 0;
                double f0 = freqMin;

                while (iBin <= (fftBufferSize * 0.5) && iBand < Bands)
                {
                    double fLin1 = (iBin + 0.5d) * df; //linear frequency
                    double fLog1 = m_bandFreq[iBand]; //logarythmic frequency

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

                for (int i = 0; i < m_bandOut.Length; i++)
                {
                    //Debug.WriteLine("ad" +audioData[i]);
                    m_bandOut[i] = Math.Max(0, sensitivity * Math.Log10(Util.Clamp01(m_bandOut[i])) + 1.0);
                    //Debug.WriteLine("Log10(" + m_bandOut[i] + ") = " + sensitivity * Math.Log10(m_bandOut[i]) + ", BendLogValue: " + blv);
                }

                watch.Stop();

                SpectrumDataAvailable?.Invoke(this,
                    new AudioVisEventArgs(m_bandOut, m_bandOut.Length, watch.Elapsed.TotalMilliseconds));
                return;
                NoFFT:

                if (!UseFFT)
                {
                    int nFramesPerBand = fftSize / Bands;
                    int unassignedFrames = fftSize % Bands;

                    //Debug.WriteLine(audioData.Length);
                    //Debug.WriteLine("Frames per Band: " + nFramesPerBand + ", Unassigned Frames: " + unassignedFrames);

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
                    }

                    watch.Stop();

                    SpectrumDataAvailable?.Invoke(this,
                        new AudioVisEventArgs(bandValues, bandValues.Length, watch.Elapsed.TotalMilliseconds));
                }

                #endregion
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        #region Properties

        public int Bands { get; set; }

        /// <summary>
        /// Zero Based Channel number
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

        public int FFTSize { get; set; } = 8192;

        public int FFTBufferSize
        {
            get
            {
                return fftBufferSize;
            }
            set
            {
                //Check if the value is a power of 2
                //int logFFT = (int)Math.Log(value, 2);
                //fftBufferSize = (int)Math.Pow(logFFT, 2);
                //Debug.WriteLine("fftBufferSize=" + fftBufferSize);
                fftBufferSize = value;
            }
        }

        public double Attack { get; set; }

        public double Decay { get; set; }

        public WFWindow Window { get; set; } = WFWindow.HammingWindow;

        public FFTEngine FFTEngine { get; set; } = FFTEngine.KissFFT;

        #endregion

        public void Dispose()
        {
            Stop();
            AudioClient.Dispose();
        }
    }

    public class AudioVisEventArgs : EventArgs
    {
        public double[] AudioData { get; }
        public int nFrames { get; }
        public double elapsedTime { get; }

        public AudioVisEventArgs(double[] audioData, int nFrames, double elapsedTime)
        {
            AudioData = audioData;
            this.nFrames = nFrames;
            this.elapsedTime = elapsedTime;
        }
    }
}
