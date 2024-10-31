using Wavee.Enums;

namespace Wavee.Services.Session;

internal record SpotifyTcpMessage(PacketType Type, byte[] Payload);