namespace Wavee.Context;

public interface IWaveeContext
{
    string Id { get; }

    ValueTask<int> Length { get; }
    ValueTask<string> GetIdAt(Option<int> index);
}