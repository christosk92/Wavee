using LanguageExt;

namespace Wavee.Metadata.Common;

public readonly record struct CoverImage(string Url, Option<ushort> Width, Option<ushort> Height);