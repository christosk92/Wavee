namespace Wavee.Vorbis.Format.Tags;

/// <summary>
/// `VendorData` is any binary metadata that is proprietary to a certain application or vendor.
/// </summary>
/// <param name="Ident"></param>
/// <param name="Data"></param>
public readonly record struct VendorData(string Ident, byte[] Data);