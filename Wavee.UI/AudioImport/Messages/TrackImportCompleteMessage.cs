using CommunityToolkit.Mvvm.Messaging.Messages;

namespace Wavee.UI.AudioImport.Messages
{
    public class TrackImportCompleteMessage : ValueChangedMessage<LocalAudioFile>
    {
        public TrackImportCompleteMessage(LocalAudioFile value) : base(value)
        {
        }
    }
}
