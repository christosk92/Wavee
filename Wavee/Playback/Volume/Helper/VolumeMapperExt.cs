using Microsoft.Extensions.Logging;
using Wavee.Playback.Volume.VolumeControl;

namespace Wavee.Playback.Volume.Helper;

internal static class VolumeMapperExt
{
    public static double ToMapped<TCaller>(this IVolumeCtrl volumeGetter, ushort volume,
        ILogger<TCaller> logger)
    {
        // More than just an optimization, this ensures that zero volume is
        // really mute (both the log and cubic equations would otherwise not
        // reach zero).
        if (volume == 0)
        {
            return 0;
        }

        if (volume == MAX_VOLUME)
            // And limit in case of rounding errors (as is the case for log).
        {
            return 1.0;
        }

        var normalizedVolume = (double)volume / MAX_VOLUME;

        double mappedVolume = 0;

        if (volumeGetter.RangeOk())
        {
            switch (volumeGetter)
            {
                case CubicVolumeCtrl cubicCtrl:
                    mappedVolume = CubicMapping.LinearToMapped(normalizedVolume, cubicCtrl.Val);
                    break;
                case LogVolumeCtrl logCtrl:
                    mappedVolume = LogMapping.LinearToMapped(normalizedVolume, logCtrl.Val);
                    break;
                default:
                    mappedVolume = normalizedVolume;
                    break;
            }
        }
        else
        {
            // Ensure not to return -inf or NaN due to division by zero.
            logger.LogWarning("{this} does not work with 0 dB range, using linear mapping instead",
                volumeGetter);
            mappedVolume = normalizedVolume;
        }

        logger.LogDebug("Mapped volume {volume} to {mappedVolume}", volume, mappedVolume);
        return mappedVolume;
    }

    public static ushort AsUnmapped<TCaller>(this IVolumeCtrl volumeCtrl, double mappedVolume, ILogger<TCaller> logger)
    {
        // More than just an optimization, this ensures that zero mapped volume
        // is unmapped to non-negative real numbers (otherwise the log and cubic
        // equations would respectively return -inf and -1/9.)
        if (Math.Abs(mappedVolume - 0.0) <= double.Epsilon)
        {
            return 0;
        }
        else if (Math.Abs(mappedVolume - 1.0) <= double.Epsilon)
        {
            return MAX_VOLUME;
        }

        double unmappedVolume;
        if (volumeCtrl.RangeOk())
        {
            switch (volumeCtrl)
            {
                case CubicVolumeCtrl cubic:
                    unmappedVolume = CubicMapping.MappedToLinear(mappedVolume, cubic.Val);
                    break;
                case LogVolumeCtrl log:
                    unmappedVolume = LogMapping.MappedToLinear(mappedVolume, log.Val);
                    break;
                default:
                    unmappedVolume = mappedVolume;
                    break;
            }
        }
        else
        {
            // Ensure not to return -inf or NaN due to division by zero.
            logger.LogWarning("{this} does not work with 0 dB range, using linear mapping instead",
                volumeCtrl);
            unmappedVolume = mappedVolume;
        }

        return (ushort)(unmappedVolume * MAX_VOLUME);
    }

    private static bool RangeOk(this IVolumeCtrl volumeGetter)
    {
        return volumeGetter switch
        {
            CubicVolumeCtrl cubicCtrl => cubicCtrl.Val > 0,
            LogVolumeCtrl logCtrl => logCtrl.Val > 0,
            FixedVolumeCtrl => true,
            LinearVolumeCtrl => true
        };
    }

    public const ushort MAX_VOLUME = ushort.MaxValue;
}

// Ported from: https://github.com/alsa-project/alsa-utils/blob/master/alsamixer/volume_mapping.c
// which in turn was inspired by: https://www.robotplanet.dk/audio/audio_gui_design/
//
// Though this mapping is computationally less expensive than the logarithmic
// mapping, it really does not matter as librespot memoizes the mapped value.
// Use this mapping if you have some reason to mimic Alsa's native mixer or
// prefer a more granular control in the upper volume range.
//
// Note: https://www.dr-lex.be/info-stuff/volumecontrols.html#ideal3 shows
// better approximations to the logarithmic curve but because we only intend
// to mimic Alsa here, we do not implement them. If your desire is to use a
// logarithmic mapping, then use that volume control.
internal static class CubicMapping
{
    public static double LinearToMapped(double normalizedVolume, double dbRange)
    {
        double minNorm = MinNorm(dbRange);
        return Math.Pow(normalizedVolume * (1.0 - minNorm) + minNorm, 3);
    }

    public static double MappedToLinear(double mappedVolume, double dbRange)
    {
        double minNorm = MinNorm(dbRange);
        return (Math.Pow(mappedVolume, 1.0 / 3.0) - minNorm) / (1.0 - minNorm);
    }

    private static double MinNorm(double dbRange)
    {
        // Note that this 60.0 is unrelated to DEFAULT_DB_RANGE.
        // Instead, it's the cubic voltage to dB ratio.
        return Math.Pow(10.0, -1.0 * dbRange / 60.0);
    }
}
// Volume conversion taken from: https://www.dr-lex.be/info-stuff/volumecontrols.html#ideal2
//
// As the human auditory system has a logarithmic sensitivity curve, this
// mapping results in a near linear loudness experience with the listener.

internal static class LogMapping
{
    public static double LinearToMapped(double normalizedVolume, double dbRange)
    {
        (double dbRatio, double idealFactor) = Coefficients(dbRange);
        return Math.Exp(idealFactor * normalizedVolume) / dbRatio;
    }

    public static double MappedToLinear(double mappedVolume, double dbRange)
    {
        (double dbRatio, double idealFactor) = Coefficients(dbRange);
        return Math.Log(dbRatio * mappedVolume) / idealFactor;
    }

    private static (double, double) Coefficients(double dbRange)
    {
        double dbRatio = DbToRatio(dbRange);
        double idealFactor = Math.Log(dbRatio);
        return (dbRatio, idealFactor);
    }

    private static double DbToRatio(double dbRange)
    {
        // Assuming you have a function to convert dB to ratio
        // Placeholder implementation
        return Math.Pow(10.0, dbRange / 20.0);
    }
}