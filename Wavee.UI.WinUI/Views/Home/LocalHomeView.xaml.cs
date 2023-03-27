using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Wavee.UI.ViewModels.Home;
using Wavee.UI.WinUI.Helpers.DragDropHelpers;
using Windows.Storage;
using CommunityToolkit.Mvvm.Collections;
using CommunityToolkit.Mvvm.Input;
using Wavee.UI.Helpers.Extensions;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Playback.Contexts;
using Wavee.UI.Services.Import;
using Wavee.UI.ViewModels.Playback;
using Wavee.UI.ViewModels.Track;

namespace Wavee.UI.WinUI.Views.Home
{
    public sealed partial class LocalHomeView : Page
    {
        public LocalHomeView()
        {
            ViewModel = Ioc.Default.GetRequiredService<LocalHomeViewModel>();
            this.InitializeComponent();
        }

        public LocalHomeViewModel ViewModel
        {
            get;
        }

        public Visibility HasItems(int i)
        {
            return i == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void LocalHomeView_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (e.NewSize.Width < 1300)
                VisualStateManager.GoToState(this, "SmallState", false);
            else
                VisualStateManager.GoToState(this, "DefaultState", false);
        }

        private void ImportDragOver(object sender, DragEventArgs e)
        {
            e.AllowDrag("Import files", DataPackageOperation.Link);
        }

        private async void ImportDrop(object sender, DragEventArgs e)
        {
            var items = await e.GetStorageItemsAsync();
            if (items.Count == 0) return;

            static bool IsAudioFile(IStorageItem item)
            {
                var isFile = item.IsOfType(StorageItemTypes.File);
                var contains = ImportService.AcceptedAudioFormats.Contains(Path.GetExtension(item.Path));
                return isFile && contains;
            }

            static bool IsFolder(IStorageItem item)
            {
                return item.IsOfType(StorageItemTypes.Folder);
            }

            var audioFiles = items.Where(a => IsAudioFile(a) || IsFolder(a))
                .Select(a => (a.Path, IsFolder(a)));
            var tracks = await Task.Run(async () => await Ioc.Default.GetRequiredService<ImportService>()
                .Import(audioFiles)
                .WaitForFinish());
            var stringLocalizer = Ioc.Default.GetRequiredService<IStringLocalizer>();

            // var addTracks = await Task.Run(async () =>
            //  {
            //      var tracks = await controller.Process();
            //      return tracks.Where(a => a.Existing).Select(a => a.Imported.Value);
            //  });

            foreach (var track in tracks.Where(a => !a.Existing).Select(a => a.Imported.Value))
            {
                const double minSecondsDiff = 60 * 60 * 24;

                var groupKey = track.DateImported.CalculateRelativeDateString(minSecondsDiff, stringLocalizer);
                if (ViewModel.LatestFiles.FirstGroupByKeyOrDefault(groupKey) is null)
                {
                    ViewModel.LatestFiles.InsertGroup(groupKey);
                }

                int i = 1;
                foreach (var trackViewModel in ViewModel.LatestFiles.FirstGroupByKey(groupKey))
                {
                    trackViewModel.Index = i;
                    i++;
                }

                ViewModel.LatestFiles.InsertItem(groupKey,
                    new TrackViewModel(track,
                        0));
            }
        }
    }
}