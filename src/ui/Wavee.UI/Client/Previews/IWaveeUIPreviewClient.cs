using LanguageExt;

namespace Wavee.UI.Client.Previews;

public interface IWaveeUIPreviewClient
{
    Task<Option<string>> GetPreviewStreamsForContext(string id, CancellationToken ct = default);
}