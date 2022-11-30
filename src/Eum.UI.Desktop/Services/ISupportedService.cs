namespace Eum.UI.Services;

/// <summary>
/// A specific service may not be supported by a music service. Use this to check to avoid unexpected exceptions.
/// </summary>
public interface ISupportedService
{
    bool IsSupported { get; }
    string? NotSupportedReason { get; }
}