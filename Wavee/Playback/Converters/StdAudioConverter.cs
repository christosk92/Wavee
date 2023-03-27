namespace Wavee.Playback.Converters;

internal sealed class StdAudioConverter : IAudioConverter
{
    private readonly IDitherer? _ditherer;

    private const double ScaleS32 = 2147483648;
    private const double ScaleS24 = 8388608;
    private const double ScaleS16 = 32768;

    public StdAudioConverter(Func<IDitherer>? dithererBuilder = null)
    {
        _ditherer = dithererBuilder?.Invoke();
        if (_ditherer != null)
        {
            Console.WriteLine($"Converting with ditherer: {_ditherer.Name}");
        }
    }

    private double Scale(double sample, double factor)
    {
        return _ditherer != null
            ? Math.Round((sample * factor + _ditherer.Noise()))
            : Math.Round((sample * factor));
    }

    private double ClampingScale(double sample, double factor)
    {
        double intValue = Scale(sample, factor);
        double min = -factor;
        double max = factor - 1;

        return Math.Clamp(intValue, min, max);
    }

    public ReadOnlySpan<float> F64ToF32(ReadOnlySpan<double> samples)
    {
        Span<float> result = new float[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            result[i] = (float)samples[i];
        }

        return result;
    }

    public ReadOnlySpan<int> F64ToS32(ReadOnlySpan<double> samples)
    {
        Span<int> result = new int[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            result[i] = (int)Scale(samples[i], ScaleS32);
        }

        return result;
    }

    public ReadOnlySpan<int> F64ToS24(ReadOnlySpan<double> samples)
    {
        Span<int> result = new int[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            result[i] = (int)ClampingScale(samples[i], ScaleS24);
        }
        return result;
        // return samples.Select(sample => (int)ClampingScale(sample, ScaleS24)).ToList();
    }

    public ReadOnlySpan<int> F64ToS24_3(ReadOnlySpan<double> samples)
    {
        // You need to implement the i24 class for this conversion.
        throw new NotImplementedException();
    }

    public ReadOnlySpan<short> F64ToS16(ReadOnlySpan<double> samples)
    {
        Span<short> result = new short[samples.Length];
        for (int i = 0; i < samples.Length; i++)
        {
            result[i] = (short)Scale(samples[i], ScaleS16);
        }
        return result;
    }
}

internal interface IDitherer
{
    string Name
    {
        get;
    }
    double Noise();
}