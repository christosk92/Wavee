using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Linq;

namespace AudioEffectsLib.FastFourierTransform
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
            double[] tempin = new double[dout.Length * 2];
            din.CopyTo(tempin, 0);
            fft.RealFFT(tempin, true);

            for (int i = 0; i < dout.Length * 2; i += 2)
            {
                //Debug.WriteLine("ffttemp: " + ffttemp[i*2] + ", log: " + Math.Log(ffttemp[i*2]));
                System.Numerics.Complex c = new System.Numerics.Complex(tempin[i], tempin[i + 1]);
                dout[i / 2] = c.Magnitude * 100;

                dout[i] = (tempin[i] * tempin[i] + tempin[i + 1] * tempin[i + 1]) * 100;
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

        public void FFT(double[] din,double[] dout)
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

        public void FFT(double[] din,double[] dout)
        {
            float[] fin = din.Select(x => (float)x).ToArray();
            UnsafeKissFFT.FFT(config, fin, dout);
        }
    }

    unsafe internal class UnsafeKissFFT
    {

#if X64
        [DllImport(@"KissFFT64.dll", EntryPoint = "KISS_Alloc", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Alloc(int nfft, int inverse_fft);

        [DllImport(@"KissFFT64.dll", EntryPoint = "KISS_AllocR", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AllocReal(int nfft, int inverse_fft);

        [DllImport(@"KissFFT64.dll", EntryPoint = "KISS_FFT", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        static extern void Kiss_FFT(IntPtr cfg, Complex* fin, Complex* fout);

        [DllImport(@"KissFFT64.dll", EntryPoint = "KISS_FFTR", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        static extern void Kiss_FFTR(IntPtr cfg, float* fin, Complex* fout);

        [DllImport(@"KissFFT64.dll", EntryPoint = "KISS_Cleanup", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Cleanup();

        [DllImport(@"KissFFT64.dll", EntryPoint = "KISS_NextFastSize", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int NextFastSize(int n);
#else
        [DllImport(@"KissFFT.dll", EntryPoint = "KISS_Alloc", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr Alloc(int nfft, int inverse_fft);

        [DllImport(@"KissFFT.dll", EntryPoint = "KISS_AllocR", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AllocReal(int nfft, int inverse_fft);

        [DllImport(@"KissFFT.dll", EntryPoint = "KISS_FFT", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        static extern void Kiss_FFT(IntPtr cfg, Complex* fin, Complex* fout);

        [DllImport(@"KissFFT.dll", EntryPoint = "KISS_FFTR", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        static extern void Kiss_FFTR(IntPtr cfg, float* fin, Complex* fout);

        [DllImport(@"KissFFT.dll", EntryPoint = "KISS_Cleanup", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern void Cleanup();

        [DllImport(@"KissFFT.dll", EntryPoint = "KISS_NextFastSize", SetLastError = false, CallingConvention = CallingConvention.Cdecl)]
        public static extern int NextFastSize(int n);
#endif

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
                //if nan; set to 0
                if (double.IsNaN(fout[i].Real))
                {
                    fout[i] = new Complex(0, fout[i].Imag);
                }

                if (double.IsNaN(fout[i].Imag))
                {
                    fout[i] = new Complex(fout[i].Real, 0);
                }
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
                //System.Numerics.Complex complex = new System.Numerics.Complex(Convert.ToDouble(fout[i].Real), Convert.ToDouble(fout[i].Imag));
                //dfout[i] = complex.Magnitude;
                dfout[i] = (fout[i].Real * fout[i].Real + fout[i].Imag * fout[i].Imag);
            }
        }
    }

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    unsafe internal struct Complex
    {
        public float Real;
        public float Imag;

        public Complex(float real, float imag)
        {
            Real = real;
            Imag = imag;
        }

        public static Complex operator -(Complex c1)
        {
            return new Complex(-c1.Real, -c1.Imag);
        }

        public static Complex operator +(Complex c1)
        {
            return new Complex(c1.Real, c1.Imag);
        }

        public static Complex operator +(Complex c1, Complex c2)
        {
            return new Complex(c1.Real + c2.Real, c1.Imag + c2.Imag);
        }

        public static Complex operator +(Complex c1, float c2)
        {
            return new Complex(c1.Real + c2, c1.Imag);
        }

        public static Complex operator +(Complex c1, int c2)
        {
            return new Complex(c1.Real + c2, c1.Imag);
        }

        public static Complex operator -(Complex c1, Complex c2)
        {
            return new Complex(c1.Real - c2.Real, c1.Imag - c2.Imag);
        }

        public static Complex operator *(Complex c1, Complex c2)
        {
            var r = c1.Real * c2.Real - c1.Imag * c2.Imag;
            var i = c1.Real * c2.Imag + c1.Imag * c2.Real;
            return new Complex(r, i);
        }

        public static Complex operator *(Complex c1, float c2)
        {
            return new Complex(c1.Real * c2, c1.Imag * c2);
        }

        public static Complex operator *(Complex c1, int c2)
        {
            return new Complex(c1.Real * c2, c1.Imag * c2);
        }

        public static Complex operator /(Complex c1, float c2)
        {
            return new Complex(c1.Real / c2, c1.Imag / c2);
        }

        public static Complex operator /(Complex c1, int c2)
        {
            return new Complex(c1.Real / c2, c1.Imag / c2);
        }

        public static implicit operator Complex(float rhs)
        {
            return new Complex(rhs, 0);
        }

        public override string ToString()
        {
            var r = Real;
            var i = Imag;
            if (Math.Abs(r) % 1.0 < 0.000000000001)
                r = (float)Math.Round(r);
            if (Math.Abs(i) % 1.0 < 0.000000000001)
                i = (float)Math.Round(i);

            if (i == 0)
                return r.ToString();
            else
                return "(" + r + ", " + i + ")";
        }

        public static Complex I = new Complex(0, 1);

        public static Complex CExp(float phase)
        {
            var x = Math.Cos(phase);
            var y = Math.Sin(phase);
            return new Complex((float)x, (float)y);
        }

        // ------------- fast operations -------------

        public static void Multiply(ref Complex dest, ref Complex c1, ref Complex c2)
        {
            var r = c1.Real * c2.Real - c1.Imag * c2.Imag;
            var i = c1.Real * c2.Imag + c1.Imag * c2.Real;
            dest.Real = r;
            dest.Imag = i;
        }

        public static void Add(ref Complex dest, ref Complex c1, ref Complex c2)
        {
            dest.Real = c1.Real + c2.Real;
            dest.Imag = c1.Imag + c2.Imag;
        }

        public static void Subtract(ref Complex dest, ref Complex c1, ref Complex c2)
        {
            dest.Real = c1.Real - c2.Real;
            dest.Imag = c1.Imag - c2.Imag;
        }


        public static void Multiply(ref Complex dest, ref Complex c1)
        {
            var r = dest.Real * c1.Real - dest.Imag * c1.Imag;
            dest.Imag = dest.Real * c1.Imag + dest.Imag * c1.Real;
            dest.Real = r;
        }

        public static void Add(ref Complex dest, ref Complex c1)
        {
            dest.Real += c1.Real;
            dest.Imag += c1.Imag;
        }

        public static void Subtract(ref Complex dest, ref Complex c1)
        {
            dest.Real -= c1.Real;
            dest.Imag -= c1.Imag;
        }
    }

    public class LomontFFT
    {
        /// <summary>                                                                                            
        /// Compute the forward or inverse Fourier Transform of data, with                                       
        /// data containing complex valued data as alternating real and                                          
        /// imaginary parts. The length must be a power of 2. The data is                                        
        /// modified in place.                                                                                   
        /// </summary>                                                                                           
        /// <param name="data">The complex data stored as alternating real                                       
        /// and imaginary parts</param>                                                                          
        /// <param name="forward">true for a forward transform, false for                                        
        /// inverse transform</param>                                                                            
        public void FFT(double[] data, bool forward)
        {
            var n = data.Length;
            // checks n is a power of 2 in 2's complement format                                                 
            if ((n & (n - 1)) != 0)
                throw new ArgumentException(
                    "data length " + n + " in FFT is not a power of 2");
            n /= 2;    // n is the number of samples                                                             

            Reverse(data, n); // bit index data reversal                                                         

            // do transform: so single point transforms, then doubles, etc.                                      
            double sign = forward ? B : -B;
            var mmax = 1;
            while (n > mmax)
            {
                var istep = 2 * mmax;
                var theta = sign * Math.PI / mmax;
                double wr = 1, wi = 0;
                var wpr = Math.Cos(theta);
                var wpi = Math.Sin(theta);
                for (var m = 0; m < istep; m += 2)
                {
                    for (var k = m; k < 2 * n; k += 2 * istep)
                    {
                        var j = k + istep;
                        var tempr = wr * data[j] - wi * data[j + 1];
                        var tempi = wi * data[j] + wr * data[j + 1];
                        data[j] = data[k] - tempr;
                        data[j + 1] = data[k + 1] - tempi;
                        data[k] = data[k] + tempr;
                        data[k + 1] = data[k + 1] + tempi;
                    }
                    var t = wr; // trig recurrence                                                               
                    wr = wr * wpr - wi * wpi;
                    wi = wi * wpr + t * wpi;
                }
                mmax = istep;
            }

            // perform data scaling as needed                                                                    
            Scale(data, n, forward);
        }

        /// <summary>                                                                                            
        /// Compute the forward or inverse Fourier Transform of data, with data                                  
        /// containing complex valued data as alternating real and imaginary                                     
        /// parts. The length must be a power of 2. This method caches values                                    
        /// and should be slightly faster on than the FFT method for repeated uses.                              
        /// It is also slightly more accurate. Data is transformed in place.                                     
        /// </summary>                                                                                           
        /// <param name="data">The complex data stored as alternating real                                       
        /// and imaginary parts</param>                                                                          
        /// <param name="forward">true for a forward transform, false for                                        
        /// inverse transform</param>                                                                            
        public void TableFFT(double[] data, bool forward)
        {
            var n = data.Length;
            // checks n is a power of 2 in 2's complement format                                                 
            if ((n & (n - 1)) != 0)
                throw new ArgumentException(
                    "data length " + n + " in FFT is not a power of 2"
                    );
            n /= 2;    // n is the number of samples                                                             

            Reverse(data, n); // bit index data reversal                                                         

            // make table if needed                                                                              
            if ((cosTable == null) || (cosTable.Length != n))
                Initialize(n);

            // do transform: so single point transforms, then doubles, etc.                                      
            double sign = forward ? B : -B;
            var mmax = 1;
            var tptr = 0;
            while (n > mmax)
            {
                var istep = 2 * mmax;
                for (var m = 0; m < istep; m += 2)
                {
                    var wr = cosTable[tptr];
                    var wi = sign * sinTable[tptr++];
                    for (var k = m; k < 2 * n; k += 2 * istep)
                    {
                        var j = k + istep;
                        var tempr = wr * data[j] - wi * data[j + 1];
                        var tempi = wi * data[j] + wr * data[j + 1];
                        data[j] = data[k] - tempr;
                        data[j + 1] = data[k + 1] - tempi;
                        data[k] = data[k] + tempr;
                        data[k + 1] = data[k + 1] + tempi;
                    }
                }
                mmax = istep;
            }


            // perform data scaling as needed                                                                    
            Scale(data, n, forward);
        }

        /// <summary>                                                                                            
        /// Compute the forward or inverse Fourier Transform of data, with                                       
        /// data containing real valued data only. The output is complex                                         
        /// valued after the first two entries, stored in alternating real                                       
        /// and imaginary parts. The first two returned entries are the real                                     
        /// parts of the first and last value from the conjugate symmetric                                       
        /// output, which are necessarily real. The length must be a power                                       
        /// of 2.                                                                                                
        /// </summary>                                                                                           
        /// <param name="data">The complex data stored as alternating real                                       
        /// and imaginary parts</param>                                                                          
        /// <param name="forward">true for a forward transform, false for                                        
        /// inverse transform</param>                                                                            
        public void RealFFT(double[] data, bool forward)
        {

            var n = data.Length; // # of real inputs, 1/2 the complex length                                     
            // checks n is a power of 2 in 2's complement format                                                 
            if ((n & (n - 1)) != 0)
                throw new ArgumentException(
                    "data length " + n + " in FFT is not a power of 2"
                    );

            var sign = -1.0; // assume inverse FFT, this controls how algebra below works                        
            if (forward)
            { // do packed FFT. This can be changed to FFT to save memory                                        
                TableFFT(data, true);
                sign = 1.0;
                // scaling - divide by scaling for N/2, then mult by scaling for N                               
                if (A != 1)
                {
                    var scale = Math.Pow(2.0, (A - 1) / 2.0);
                    for (var i = 0; i < data.Length; ++i)
                        data[i] *= scale;
                }
            }

            var theta = B * sign * 2 * Math.PI / n;
            var wpr = Math.Cos(theta);
            var wpi = Math.Sin(theta);
            var wjr = wpr;
            var wji = wpi;

            for (var j = 1; j <= n / 4; ++j)
            {
                var k = n / 2 - j;
                var tkr = data[2 * k];    // real and imaginary parts of t_k  = t_(n/2 - j)                      
                var tki = data[2 * k + 1];
                var tjr = data[2 * j];    // real and imaginary parts of t_j                                     
                var tji = data[2 * j + 1];

                var a = (tjr - tkr) * wji;
                var b = (tji + tki) * wjr;
                var c = (tjr - tkr) * wjr;
                var d = (tji + tki) * wji;
                var e = (tjr + tkr);
                var f = (tji - tki);

                // compute entry y[j]                                                                            
                data[2 * j] = 0.5 * (e + sign * (a + b));
                data[2 * j + 1] = 0.5 * (f + sign * (d - c));

                // compute entry y[k]                                                                            
                data[2 * k] = 0.5 * (e - sign * (b + a));
                data[2 * k + 1] = 0.5 * (sign * (d - c) - f);

                var temp = wjr;
                // todo - allow more accurate version here? make option?                                         
                wjr = wjr * wpr - wji * wpi;
                wji = temp * wpi + wji * wpr;
            }

            if (forward)
            {
                // compute final y0 and y_{N/2}, store in data[0], data[1]                                       
                var temp = data[0];
                data[0] += data[1];
                data[1] = temp - data[1];
            }
            else
            {
                var temp = data[0]; // unpack the y0 and y_{N/2}, then invert FFT                                
                data[0] = 0.5 * (temp + data[1]);
                data[1] = 0.5 * (temp - data[1]);
                // do packed inverse (table based) FFT. This can be changed to regular inverse FFT to save memory
                TableFFT(data, false);
                // scaling - divide by scaling for N, then mult by scaling for N/2                               
                //if (A != -1) // todo - off by factor of 2? this works, but something seems weird               
                {
                    var scale = Math.Pow(2.0, -(A + 1) / 2.0) * 2;
                    for (var i = 0; i < data.Length; ++i)
                        data[i] *= scale;
                }
            }
        }

        /// <summary>                                                                                            
        /// Determine how scaling works on the forward and inverse transforms.                                   
        /// For size N=2^n transforms, the forward transform gets divided by                                     
        /// N^((1-a)/2) and the inverse gets divided by N^((1+a)/2). Common                                      
        /// values for (A,B) are                                                                                 
        ///     ( 0, 1)  - default                                                                               
        ///     (-1, 1)  - data processing                                                                       
        ///     ( 1,-1)  - signal processing                                                                     
        /// Usual values for A are 1, 0, or -1                                                                   
        /// </summary>                                                                                           
        public int A { get; set; }

        /// <summary>                                                                                            
        /// Determine how phase works on the forward and inverse transforms.                                     
        /// For size N=2^n transforms, the forward transform uses an                                             
        /// exp(B*2*pi/N) term and the inverse uses an exp(-B*2*pi/N) term.                                      
        /// Common values for (A,B) are                                                                          
        ///     ( 0, 1)  - default                                                                               
        ///     (-1, 1)  - data processing                                                                       
        ///     ( 1,-1)  - signal processing                                                                     
        /// Abs(B) should be relatively prime to N.                                                              
        /// Setting B=-1 effectively corresponds to conjugating both input and                                   
        /// output data.                                                                                         
        /// Usual values for B are 1 or -1.                                                                      
        /// </summary>                                                                                           
        public int B { get; set; }

        public LomontFFT()
        {
            A = 0;
            B = 1;
        }

#region Internals                                                                                        

        /// <summary>                                                                                            
        /// Scale data using n samples for forward and inverse transforms as needed                              
        /// </summary>                                                                                           
        /// <param name="data"></param>                                                                          
        /// <param name="n"></param>                                                                             
        /// <param name="forward"></param>                                                                       
        void Scale(double[] data, int n, bool forward)
        {
            // forward scaling if needed                                                                         
            if ((forward) && (A != 1))
            {
                var scale = Math.Pow(n, (A - 1) / 2.0);
                for (var i = 0; i < data.Length; ++i)
                    data[i] *= scale;
            }

            // inverse scaling if needed                                                                         
            if ((!forward) && (A != -1))
            {
                var scale = Math.Pow(n, -(A + 1) / 2.0);
                for (var i = 0; i < data.Length; ++i)
                    data[i] *= scale;
            }
        }

        /// <summary>                                                                                            
        /// Call this with the size before using the TableFFT version                                            
        /// Fills in tables for speed. Done automatically in TableFFT                                            
        /// </summary>                                                                                           
        /// <param name="size">The size of the FFT in samples</param>                                            
        void Initialize(int size)
        {
            // NOTE: if you port to non garbage collected languages                                              
            // like C# or Java be sure to free these correctly                                                   
            cosTable = new double[size];
            sinTable = new double[size];

            // forward pass                                                                                      
            var n = size;
            int mmax = 1, pos = 0;
            while (n > mmax)
            {
                var istep = 2 * mmax;
                var theta = Math.PI / mmax;
                double wr = 1, wi = 0;
                var wpi = Math.Sin(theta);
                // compute in a slightly slower yet more accurate manner                                         
                var wpr = Math.Sin(theta / 2);
                wpr = -2 * wpr * wpr;
                for (var m = 0; m < istep; m += 2)
                {
                    cosTable[pos] = wr;
                    sinTable[pos++] = wi;
                    var t = wr;
                    wr = wr * wpr - wi * wpi + wr;
                    wi = wi * wpr + t * wpi + wi;
                }
                mmax = istep;
            }
        }

        /// <summary>                                                                                            
        /// Swap data indices whenever index i has binary                                                        
        /// digits reversed from index j, where data is                                                          
        /// two doubles per index.                                                                               
        /// </summary>                                                                                           
        /// <param name="data"></param>                                                                          
        /// <param name="n"></param>                                                                             
        static void Reverse(double[] data, int n)
        {
            // bit reverse the indices. This is exercise 5 in section                                            
            // 7.2.1.1 of Knuth's TAOCP the idea is a binary counter                                             
            // in k and one with bits reversed in j                                                              
            int j = 0, k = 0; // Knuth R1: initialize                                                            
            var top = n / 2;  // this is Knuth's 2^(n-1)                                                         
            while (true)
            {
                // Knuth R2: swap - swap j+1 and k+2^(n-1), 2 entries each                                       
                var t = data[j + 2];
                data[j + 2] = data[k + n];
                data[k + n] = t;
                t = data[j + 3];
                data[j + 3] = data[k + n + 1];
                data[k + n + 1] = t;
                if (j > k)
                { // swap two more                                                                               
                    // j and k                                                                                   
                    t = data[j];
                    data[j] = data[k];
                    data[k] = t;
                    t = data[j + 1];
                    data[j + 1] = data[k + 1];
                    data[k + 1] = t;
                    // j + top + 1 and k+top + 1                                                                 
                    t = data[j + n + 2];
                    data[j + n + 2] = data[k + n + 2];
                    data[k + n + 2] = t;
                    t = data[j + n + 3];
                    data[j + n + 3] = data[k + n + 3];
                    data[k + n + 3] = t;
                }
                // Knuth R3: advance k                                                                           
                k += 4;
                if (k >= n)
                    break;
                // Knuth R4: advance j                                                                           
                var h = top;
                while (j >= h)
                {
                    j -= h;
                    h /= 2;
                }
                j += h;
            } // bit reverse loop                                                                                
        }

        /// <summary>                                                                                            
        /// Pre-computed sine/cosine tables for speed                                                            
        /// </summary>                                                                                           
        double[] cosTable;
        double[] sinTable;

#endregion

    }
}
