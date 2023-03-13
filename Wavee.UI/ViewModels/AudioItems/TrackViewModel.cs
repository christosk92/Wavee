using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Models;

namespace Wavee.UI.ViewModels.AudioItems
{
    public partial class TrackViewModel : ObservableRecipient
    {
        [ObservableProperty]
        private int _index;
        public TrackViewModel(int index, ITrack localAudioFile)
        {
            OriginalIndex = index;
            _index = index;
            Track = localAudioFile;
        }
        public int OriginalIndex
        {
            get; set;
        }
        public ITrack Track
        {
            get;
        }

        public bool IsNull(string? s, bool ifNull)
        {
            return string.IsNullOrEmpty(s) ? ifNull : !ifNull;
        }
    }
}
