namespace Wavee.UI.Client.Previews;

public interface IWaveeUIPreviewClient
{
    Task<IEnumerable<Task<string>>> GetPreviewStreamsForContext(string id, CancellationToken ct = default);
}