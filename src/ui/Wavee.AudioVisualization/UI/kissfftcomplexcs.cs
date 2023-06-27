using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace AudioVisualizerUWP.Complexs
{

    [StructLayoutAttribute(LayoutKind.Sequential, Pack = 1)]
    public unsafe struct Complex
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
}

