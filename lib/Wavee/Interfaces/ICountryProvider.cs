namespace Wavee.Interfaces;

public interface ICountryProvider
{
    ValueTask<string> GetCountryCode(CancellationToken cancellationToken = default);
    ValueTask<string> UserId(CancellationToken cancellationToken = default);
}