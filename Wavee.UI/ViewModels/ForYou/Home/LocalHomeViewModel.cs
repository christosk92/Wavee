using CommunityToolkit.Mvvm.Messaging;
using System.Collections.ObjectModel;
using System.Reactive.Concurrency;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wavee.UI.AudioImport.Messages;
using Wavee.UI.Navigation;
using ReactiveUI;
using Wavee.UI.AudioImport;
using Wavee.UI.ViewModels.AudioItems;

namespace Wavee.UI.ViewModels.ForYou.Home
{
    public class LocalHomeViewModel : ObservableRecipient, INavigatable,
        IRecipient<TrackImportCompleteMessage>
    {
        public ObservableCollection<TrackViewModel> LatestFiles { get; } = new ObservableCollection<TrackViewModel>();

        public LocalHomeViewModel(LocalAudioManagerViewModel managerViewModel)
        {
            var latest =
                managerViewModel.GetLatestAudioFiles(file => file.CreatedAt, false, 0, 25);
            int i = 0;
            foreach (var localAudioFile in latest)
            {
                LatestFiles.Add(new TrackViewModel(i, localAudioFile));
                i++;
            }
        }

        public void OnNavigatedTo(object parameter)
        {
            this.IsActive = true;
        }

        public void OnNavigatedFrom()
        {
            this.IsActive = false;
        }

        public int MaxDepth
        {
            get;
        }


        public void Receive(TrackImportCompleteMessage message)
        {
            RxApp.MainThreadScheduler.Schedule(() =>
            {
                try
                {
                    LatestFiles.Insert(0, new TrackViewModel(0, message.Value));
                    //trim all after 10
                    while (LatestFiles.Count > 25)
                    {
                        LatestFiles.RemoveAt(LatestFiles.Count - 1);
                    }

                    int i = 0;
                    foreach (var trackViewModel in LatestFiles)
                    {
                        trackViewModel.Index = i;
                        i++;
                    }
                }
                catch (Exception ex)
                {

                }
                finally
                {

                }
            });
        }
    }
}