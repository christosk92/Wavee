namespace Wavee.Playback;

public record WaveePlayerConfig
{
    public bool Gapless
    {
        get;
        init;
    }

    public bool Passthrough
    {
        get;
        init;
    }

    public bool Normalisation
    {
        get;
        init;
    }

    public WaveeNormalisationType NormalisationType
    {
        get;
        init;
    }

    public WaveeNormalisationMethod NormalisationMethod
    {
        get;
        init;
    }

    public double NormalisationPregainDb
    {
        get;
        init;
    }

    public double NormalisationThresholdDbfs
    {
        get;
        init;
    }

    public double NormalisationAttackCf
    {
        get;
        init;
    }

    public double NormalisationReleaseCf
    {
        get;
        init;
    }

    public double NormalisationKneeDb
    {
        get;
        init;
    }

    public WaveePlayerConfig Copy()
    {
        return this with { };
    }
}

public enum WaveeNormalisationMethod
{
    Basic,
    Dynamic
}

public enum WaveeNormalisationType
{
    Album,
    Track,
    Auto
}