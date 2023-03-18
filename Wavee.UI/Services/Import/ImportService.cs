using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Wavee.UI.Interfaces.Services;
using CommunityToolkit.Mvvm.Messaging;
using Nito.AsyncEx;

namespace Wavee.UI.Services.Import
{
    public partial class ImportService : ObservableObject
    {
        [ObservableProperty]
        private MusicImportProgressController? _progressController;

        public event EventHandler? ImportCompleted;

        private readonly ConcurrentDictionary<Guid, ImportController> _controllers = new();

        private readonly ILocalAudioDb _db;
        private readonly ILogger<ImportService>? _logger;
        private readonly ILoggerFactory? _loggerFactory;
        private readonly IAppDataProvider _appDataProvider;
        public ImportService(
            ILocalAudioDb db, IAppDataProvider appDataProvider, ILoggerFactory? loggerFactory = null)
        {
            _logger = loggerFactory?.CreateLogger<ImportService>();
            _loggerFactory = loggerFactory;
            _db = db;
            _appDataProvider = appDataProvider;
            _ = Task.Run(async () => await Refresh());
        }

        private async Task Refresh()
        {

            //try and fetch new metadata
            var all =
                await _db.GetAllForUpdateCheck();
            var updatePaths = new List<string>();
            foreach (var data in all)
            {
                var path = data.Id;
                var lastUpdatedAt = data.LastChanged;
                if (!string.IsNullOrEmpty(path) && File.Exists(path))
                {
                    //try and update
                    var lastWriteTime = new FileInfo(path).LastWriteTimeUtc;
                    if (Math.Abs(lastWriteTime.Ticks - lastUpdatedAt.Ticks) > TimeSpan.TicksPerMillisecond)
                    {
                        //update metadata
                        updatePaths.Add(path);
                    }
                }
                else
                {
                    //File deleted... remove from db
                    await _db.Remove(path);
                }
            }

            var controller = await Import(updatePaths
                .Select(a => (a, false)))
                .Process();
        }
        public ImportController Import(IEnumerable<(string Path, bool Folder)> message)
        {
            _logger?.LogInformation("Received message");
            _progressController ??= new MusicImportProgressController();
            //scan for audio files and each folder, and its subdirs.
            //if it's a folder, scan for audio files
            //if it's a file, check if it's an audio file
            //then we check if it's already in the db 
            //if it's not, we add it to the db

            var paths = new List<string>();
            foreach (var (path, isFolder) in message)
            {
                if (isFolder)
                {
                    paths.AddRange(Directory.EnumerateFiles(path, "*.*",
                            SearchOption.AllDirectories)
                        .Where(x => AcceptedAudioFormats.Contains(Path.GetExtension(x))));
                }
                else
                {
                    paths.Add(path);
                }
            }

            var controller = new ImportController(_db, paths,
                _appDataProvider,
                _loggerFactory?.CreateLogger<ImportController>());
            _controllers[controller.Id] = controller;
            _progressController.Total += controller.Total;
            _progressController.TotalTasks += 1;

            Task.Factory.StartNew(async () =>
            {
                var prevError = 0;
                var processedCount = 0;
                try
                {
                    var tracks = await controller.Process(() =>
                    {
                        processedCount++;
                        _progressController.Processed++;
                        if (prevError != controller.Errors.Count)
                        {
                            _progressController.Error += (controller.Errors.Count - prevError);
                            prevError = controller.Errors.Count;
                        }

                        _logger?.LogDebug("{processed}/{total} error: {error}",
                            processedCount, controller.Total, controller.Errors.Count);
                    });
                    controller.Set(tracks.ToList());
                }
                catch (Exception x)
                {
                    _logger?.LogWarning(x, "An error occured while reading events.");
                    controller.SetError(x);
                }
                finally
                {
                    _controllers.TryRemove(controller.Id, out var processedController);
                    if (_progressController != null)
                    {
                        _progressController.TotalTasks -= 1;
                    }

                    Debug.Assert(processedController != null, nameof(processedController) + " != null");
                    _logger?.LogDebug("Finished: {processed}/{total} error: {error}",
                        processedCount, controller.Total, controller.Errors.Count);
                    ImportCompleted?.Invoke(this, EventArgs.Empty);
                }
            });
            return controller;
        }

        public static readonly string[] AcceptedAudioFormats = new[]
        {
            ".mp3", ".ogg", ".wav", ".flac", ".m4a", ".aac", ".wma", ".alac"
        };
    }

    public partial class MusicImportProgressController : ObservableRecipient
    {
        [ObservableProperty]
        private int _processed;
        [ObservableProperty]
        private int _total;
        [ObservableProperty]
        private int _error;

        [ObservableProperty]
        private int _totalTasks;

    }
}
