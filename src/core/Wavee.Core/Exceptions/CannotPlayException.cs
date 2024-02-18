namespace Wavee.Core.Exceptions;

public sealed class CannotPlayException : Exception
{
    public CannotPlayException(string reason) : base(reason)
    {
        ReasonStr = reason;
    }

    public string ReasonStr { get; }

    public static class Reason
    {
        public const string UnsupportedMediaType = "UNSUPPORTED_MEDIA_TYPE";
        public const string NoAudioFiles = "NO_AUDIO_FILES";
        public const string RESTRICTED_MARKET = "RESTRICTED_MARKET";
        public const string CdnError = "CDN_ERROR";
    }
}