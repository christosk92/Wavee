namespace Wavee.Spotify.Interfaces;

internal interface IApResolverService
{
    ValueTask<(string Host, ushort Port)> GetAccessPoint(CancellationToken cancellationToken);
    ValueTask<(string Host, ushort Port)> GetDealer(CancellationToken cancellationToken);
    Task<string> GetSpClient(CancellationToken cancellationToken);
}