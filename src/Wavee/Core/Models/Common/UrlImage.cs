namespace Wavee.Core.Models.Common;

public readonly struct UrlImage
{
    public required string Url { get; init; }
    public required uint? Width { get; init; }
    public required uint? Height { get; init; }
}