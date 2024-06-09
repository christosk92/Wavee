namespace Wavee.Contracts.Interfaces.Clients;

public interface IColorClient
{
    Task FetchColors(Dictionary<string, string?> output, CancellationToken cancellation);
}