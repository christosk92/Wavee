namespace Wavee.Vorbis.Decoder;

internal sealed class Windows
{
    public float[] Short { get; }
    public float[] Long { get; }

    public Windows(int blockSize0, int blockSize1)
    {
        Short = GenerateWindow(blockSize0);
        Long = GenerateWindow(blockSize1);
    }

    /// <summary>
    /// For a given window size, generates the curve of the left-half of the window
    /// </summary>
    /// <param name="blockSize0"></param>
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    private static float[] GenerateWindow(int bs)
    {
        int len = bs / 2;
        double denom = Convert.ToDouble(len);

        var slope = new float[len];

        for (int i = 0; i < len; i++)
        {
            double num = Convert.ToDouble(i) + 0.5;
            double frac = Math.PI / 2 * (num / denom);
            slope[i] = (float)Math.Sin(Math.PI / 2 * Math.Pow(Math.Sin(frac), 2));
        }

        return slope;
    }
}