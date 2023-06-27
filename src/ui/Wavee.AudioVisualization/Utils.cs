using System;
using System.Collections.Generic;
using System.Text;

namespace AudioEffectsLib
{
    internal static class Extensions
    {
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }

    internal static class Util
    {
        public static double Clamp01(double x)
        {
            return Math.Max(0.0, Math.Min(1.0, (x)));
        }
    }
}
