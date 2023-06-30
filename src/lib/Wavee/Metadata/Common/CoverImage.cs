using LanguageExt;

namespace Wavee.Metadata.Common;

public readonly record struct CoverImage(string Url, Option<ushort> Width, Option<ushort> Height) : ICoverImage;

public interface ICoverImage
{
    string Url { get; }
    Option<ushort> Width { get; }
    Option<ushort> Height { get; }
}