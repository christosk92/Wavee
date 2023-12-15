namespace Wavee.Domain.Playback;

public class RemoteDevice
{
    public required string Id { get; init; }
    public required RemoteDeviceType Type { get; init; }
    public required string Name { get; init; }
    public required double? Volume { get; init; }
    public required IReadOnlyDictionary<string, string> Metadata { get; init; }
}

public enum RemoteDeviceType
{
    Unknown = 0,
    Computer = 1,
    Tablet = 2,
    Smartphone = 3,
    Speaker = 4,
    Tv = 5,
    Avr = 6,
    Stb = 7,
    AudioDongle = 8,
    GameConsole = 9,
    CastVideo = 10,
    CastAudio = 11,
    Automobile = 12,
    Smartwatch = 13,
    Chromebook = 14,
    UnknownSpotify = 100,
    CarThing = 101,
    Observer = 102,
    HomeThing = 103,
}