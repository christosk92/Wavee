using Mediator;

namespace Wavee.UI.Features.Dialog.Queries;

public sealed class PromptDeviceSelectionQuery : IQuery<PromptDeviceSelectionResult>
{

}
public record PromptDeviceSelectionResult(
    PromptDeviceSelectionResultType ResultType,
    string? DeviceId, 
    bool AlwaysDoThis);

public enum PromptDeviceSelectionResultType
{
    PlayOnDevice,
    PlayOnThisDevice,
    Nothing,
}