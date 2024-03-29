﻿using Wavee.Core.Decoders.VorbisDecoder.Format;

namespace Wavee.Core.Decoders.VorbisDecoder.Mapper;

internal interface IMapResult
{
    
}
internal readonly record struct UnknownMapResult : IMapResult;
internal readonly record struct SetupMapResult : IMapResult;
internal readonly record struct StreamDataMapResult(ulong Dur) : IMapResult;
internal readonly record struct SideDataMapResult(SideData SideData) : IMapResult;

public record SideData(MetadataRevision Revision) : IMapResult;