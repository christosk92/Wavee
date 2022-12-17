using System.Collections.Generic;
using System.Runtime.InteropServices;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;

namespace WinRTLibrary
{
    public sealed class GainAudioEffect : IBasicAudioEffect
    {
        private AudioEncodingProperties currentEncodingProperties;
        IPropertySet configuration;

        public void SetProperties(IPropertySet configuration)
        {
            this.configuration = configuration;
        }

        public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
        {
            currentEncodingProperties = encodingProperties;
        }

        unsafe public void ProcessFrame(ProcessAudioFrameContext context)
        {
            AudioFrame inputFrame = context.InputFrame;
            AudioFrame outputFrame = context.OutputFrame;

            using (AudioBuffer inputBuffer = inputFrame.LockBuffer(AudioBufferAccessMode.Read),
                   outputBuffer = outputFrame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference inputReference = inputBuffer.CreateReference(),
                   outputReference = outputBuffer.CreateReference())
            {
                byte* inputDataInBytes;
                byte* outputDataInBytes;
                uint inputCapacity;
                uint outputCapacity;

                ((IMemoryBufferByteAccess) inputReference).GetBuffer(out inputDataInBytes, out inputCapacity);
                ((IMemoryBufferByteAccess) outputReference).GetBuffer(out outputDataInBytes, out outputCapacity);

                float* inputDataInFloat = (float*) inputDataInBytes;
                float* outputDataInFloat = (float*) outputDataInBytes;

                float inputData;

                // Process audio data
                int dataInFloatLength = (int) inputBuffer.Length / sizeof(float);

                for (int i = 0; i < dataInFloatLength; i++)
                {
                    inputData = inputDataInFloat[i] * Gain;
                    outputDataInFloat[i] = inputData;
                }
            }
        }

        public void Close(MediaEffectClosedReason reason)
        {
            // Dispose of effect resources
          
        }

        public void DiscardQueuedFrames()
        {

        }

        public IReadOnlyList<AudioEncodingProperties> SupportedEncodingProperties
        {
            get
            {
                var supportedEncodingProperties = new List<AudioEncodingProperties>();

                AudioEncodingProperties encodingProps1 = AudioEncodingProperties.CreatePcm(44100, 1, 32);
                encodingProps1.Subtype = MediaEncodingSubtypes.Float;
                AudioEncodingProperties encodingProps2 = AudioEncodingProperties.CreatePcm(48000, 1, 32);
                encodingProps2.Subtype = MediaEncodingSubtypes.Float;

                AudioEncodingProperties encodingProps3 = AudioEncodingProperties.CreatePcm(44100, 2, 32);
                encodingProps3.Subtype = MediaEncodingSubtypes.Float;
                AudioEncodingProperties encodingProps4 = AudioEncodingProperties.CreatePcm(48000, 2, 32);
                encodingProps4.Subtype = MediaEncodingSubtypes.Float;

                AudioEncodingProperties encodingProps5 = AudioEncodingProperties.CreatePcm(96000, 2, 32);
                encodingProps5.Subtype = MediaEncodingSubtypes.Float;
                AudioEncodingProperties encodingProps6 = AudioEncodingProperties.CreatePcm(192000, 2, 32);
                encodingProps6.Subtype = MediaEncodingSubtypes.Float;

                supportedEncodingProperties.Add(encodingProps1);
                supportedEncodingProperties.Add(encodingProps2);
                supportedEncodingProperties.Add(encodingProps3);
                supportedEncodingProperties.Add(encodingProps4);
                supportedEncodingProperties.Add(encodingProps5);
                supportedEncodingProperties.Add(encodingProps6);

                return supportedEncodingProperties;
            }
            set
            {

            }
        }

        public bool UseInputFrameForOutput => true;


        public float Gain
        {
            get
            {
                object val;
                if (configuration != null && configuration.TryGetValue("Gain", out val))
                {
                    return (float) val;
                }

                return 1f;
            }
        }
    }
}
