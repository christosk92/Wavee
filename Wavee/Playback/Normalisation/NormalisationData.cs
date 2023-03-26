using Microsoft.Extensions.Logging;

namespace Wavee.Playback.Normalisation;

public class NormalisationData
{
    public static double GetFactor<TCaller>(WaveePlayerConfig config, NormalisationData? data,
        ILogger<TCaller>? logger)

    {
        if (!config.Normalisation || data is null) return 1d;

        (double gainDb, double gainPeak) = config.NormalisationType == WaveeNormalisationType.Album
            ? (data.AlbumGainDb, data.AlbumPeak)
            : (data.TrackGainDb, data.TrackPeak);

        const double PcmAt0Dbfs = 1.0;
        double normalisationFactor;


        // As per the ReplayGain 1.0 & 2.0 (proposed) spec:
        // https://wiki.hydrogenaud.io/index.php?title=ReplayGain_1.0_specification#Clipping_prevention
        // https://wiki.hydrogenaud.io/index.php?title=ReplayGain_2.0_specification#Clipping_prevention
        if (config.NormalisationMethod == WaveeNormalisationMethod.Basic)
        {
            // For Basic Normalisation, factor = min(ratio of (ReplayGain + PreGain), 1.0 / peak level).
            // https://wiki.hydrogenaud.io/index.php?title=ReplayGain_1.0_specification#Peak_amplitude
            // https://wiki.hydrogenaud.io/index.php?title=ReplayGain_2.0_specification#Peak_amplitude
            // We then limit that to 1.0 as not to exceed dBFS (0.0 dB).

            double factor = Math.Min(
                DbToRatio(gainDb + config.NormalisationPregainDb),
                PcmAt0Dbfs / gainPeak
            );

            if (factor > PcmAt0Dbfs)
            {
                // Log as info
                double loweredGainDb = RatioToDb(factor);
                logger?.LogInformation(
                    "Lowering gain by {gainDb} dB for the duration of this track to avoid potentially exceeding dBFS.",
                    loweredGainDb);
                normalisationFactor = PcmAt0Dbfs;
            }
            else
            {
                normalisationFactor = factor;
            }
        }
        else
        {
            double factor = DbToRatio(gainDb + config.NormalisationPregainDb);
            double thresholdRatio = DbToRatio(config.NormalisationThresholdDbfs);

            if (factor > PcmAt0Dbfs)
            {
                var factorDb = gainDb + config.NormalisationPregainDb;
                var limitingDb = factorDb + Math.Abs(config.NormalisationThresholdDbfs);
                // Log as warning
                logger?.LogWarning(
                    "This track may exceed dBFS by {:.2} dB and be subject to {:.2} dB of dynamic limiting at it's peak.",
                    factorDb, limitingDb);
            }
            else if (factor > thresholdRatio)
            {
                var limitingDb = gainDb
                                 + config.NormalisationPregainDb
                                 + Math.Abs(config.NormalisationThresholdDbfs);
                // Log as info
                logger?.LogInformation("This track may be subject to {:.2} dB of dynamic limiting at it's peak.",
                    limitingDb);
            }

            normalisationFactor = factor;
        }

        logger?.LogDebug(
            "Calculated Normalisation Factor for {normalisationType}: {normalisationFactor:P2}",
            config.NormalisationType, normalisationFactor);
        return normalisationFactor;
    }

    private const double DB_VOLTAGE_RATIO = 20.0;

    private static double DbToRatio(double db)
    {
        return Math.Pow(10.0, db / 20.0);
    }

    private static double RatioToDb(double ratio)
    {
        return 20.0 * Math.Log10(ratio);
    }

    public double AlbumGainDb
    {
        get;
        init;
    }

    public double AlbumPeak
    {
        get;
        init;
    }

    public double TrackGainDb
    {
        get;
        init;
    }

    public double TrackPeak
    {
        get;
        init;
    }

    public static NormalisationData Default = new NormalisationData
    {
        TrackGainDb = 0,
        AlbumPeak = 1,
        TrackPeak = 1,
        AlbumGainDb = 0
    };
}