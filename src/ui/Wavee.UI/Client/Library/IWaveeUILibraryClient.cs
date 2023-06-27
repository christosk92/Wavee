namespace Wavee.UI.Client.Library;

public interface IWaveeUILibraryClient
{
    IObservable<WaveeUILibraryNotification> CreateListener();

    Task<WaveeUILibraryNotification> InitializeLibraryAsync(CancellationToken cancellationToken);
}