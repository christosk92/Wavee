using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using AudioVisualizerUWP.Complexs;
using System.Diagnostics;

namespace AudioVisualizerUWP
{
    public enum FFTEngine
    {
        KissFFT,
        LomontFFT,
        NAudioFFT
    }

    public enum WFWindow
    {
        None,
        HammingWindow,
        HannWindow,
        BlackmannHarrisWindow
    }

    public interface FFT
    {
        void FFT(double[] din, double[] dout);
    }

    public class LomFFT : FFT
    {
        private LomontFFT fft;

        public LomFFT()
        {
            fft = new LomontFFT();
        }

        public void FFT(double[] din, double[] dout)
        {
            double[] tempin = new double[dout.Length*2];
            din.CopyTo(tempin, 0);
            fft.RealFFT(tempin, true);

            for (int i = 0; i < dout.Length*2; i += 2)
            {
                //Debug.WriteLine("ffttemp: " + ffttemp[i*2] + ", log: " + Math.Log(ffttemp[i*2]));
                System.Numerics.Complex c = new System.Numerics.Complex(tempin[i], tempin[i + 1]);
                dout[i / 2] = c.Magnitude*100;
            }
        }
    }

    public class KissFFTR : FFT
    {
        private IntPtr config;

        public KissFFTR(int fftbuffersize)
        {
            config = UnsafeKissFFT.AllocReal(fftbuffersize, 0);
        }

        ~KissFFTR()
        {
            UnsafeKissFFT.Cleanup();
        }

        public void FFT(double[] din, double[] dout)
        {
            float[] fin = new float[dout.Length];
            din.Select(x => (float)x).ToArray().CopyTo(fin, 0);

            // Zero padding
            for (int i = din.Length; i < dout.Length; i++)
            {
                fin[i] = 0;
            }

            UnsafeKissFFT.FFTR(config, fin, dout);
        }
    }

    public class KissFFT : FFT
    {
        private IntPtr config;

        public KissFFT(int fftbuffersize)
        {
            config = UnsafeKissFFT.Alloc(fftbuffersize, 0);
        }

        ~KissFFT()
        {
            UnsafeKissFFT.Cleanup();
        }

        public void FFT(double[] din, double[] dout)
        {
            float[] fin = din.Select(x => (float)x).ToArray();
            UnsafeKissFFT.FFT(config, fin, dout);
        }
    }

    unsafe class UnsafeKissFFT
    {
        [DllImport("KissFFT.dll", EntryPoint = "KISS_Alloc", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Alloc(int nfft, int inverse_fft);

        [DllImport("KissFFT.dll", EntryPoint = "KISS_AllocR", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AllocReal(int nfft, int inverse_fft);

        [DllImport("KissFFT.dll", EntryPoint = "KISS_FFT", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        static extern void Kiss_FFT(IntPtr cfg, Complex* fin, Complex* fout);

        [DllImport("KissFFT.dll", EntryPoint = "KISS_FFTR", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        static extern void Kiss_FFTR(IntPtr cfg, float* fin, Complex* fout);

        [DllImport("KissFFT.dll", EntryPoint = "KISS_Cleanup", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Cleanup();

        [DllImport("KissFFT.dll", EntryPoint = "KISS_NextFastSize", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int NextFastSize(int n);

        public static void FFT(IntPtr cfg, Complex[] fin, Complex[] fout)
        {
            fixed (Complex* inp = fin, outp = fout)
            {
                Kiss_FFT(cfg, inp, outp);
            }
        }

        public static void FFTR(IntPtr cfg, float[] fin, Complex[] fout)
        {
            fixed (float* inp = fin)
            {
                fixed (Complex* outp = fout)
                {
                    Kiss_FFTR(cfg, inp, outp);
                }
            }
        }

        public static void FFTR(IntPtr cfg, float[] fin, double[] dfout)
        {
            Complex[] fout = new Complex[dfout.Length];

            fixed (float* inp = fin)
            {
                fixed (Complex* outp = fout)
                {
                    Kiss_FFTR(cfg, inp, outp);
                }
            }

            for (int i = 0; i < dfout.Length; i++)
            {
                //System.Numerics.Complex complex = new System.Numerics.Complex(fout[i].Real, fout[i].Imag);
                //Debug.WriteLine("C1: " + fout[i].Real + ", C2: " + complex.Real);
                //dfout[i] = complex.Magnitude;
                dfout[i] = (fout[i].Real * fout[i].Real + fout[i].Imag * fout[i].Imag);
            }
        }

        public static void FFT(IntPtr cfg, float[] dfin, double[] dfout)
        {


            Complex[] fin = dfin.Select(x => new Complex(x, 1)).ToArray();
            Complex[] fout = new Complex[dfout.Length];

            fixed (Complex* inp = fin, outp = fout)
            {
                Kiss_FFT(cfg, inp, outp);
            }

            for (int i = 0; i < dfout.Length; i++)
            {
                System.Numerics.Complex complex = new System.Numerics.Complex(Convert.ToDouble(fout[i].Real), Convert.ToDouble(fout[i].Imag));
                Debug.WriteLine("C1: " + fout[i].Real + ", C2: " + complex.Real);
                dfout[i] = complex.Magnitude;
            }
        }
    }
}
