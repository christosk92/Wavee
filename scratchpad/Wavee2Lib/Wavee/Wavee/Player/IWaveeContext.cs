namespace Wavee.Player;

public interface IWaveeContext : IEnumerable<FutureWaveeTrack>
{
    int IndexFromUid(string uid);
}