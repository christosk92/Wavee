using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Wavee.UI.AudioImport.Messages;

public class ImportTracksMessage : ValueChangedMessage<IEnumerable<(string Path, bool IsFolder)>>
{
    public ImportTracksMessage(IEnumerable<(string Path, bool IsFolder)> tracks) : base(tracks)
    {
    }
    
}