namespace Wavee.Core.Contracts;

public readonly record struct Artwork(string Url, 
    Option<int> Width,
    Option<int> Height,
    Option<ArtworkSizeType> Size);