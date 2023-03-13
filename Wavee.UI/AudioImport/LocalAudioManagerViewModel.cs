using System.Collections.Concurrent;
using System.Linq.Expressions;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using Wavee.UI.AudioImport.Database;
using Wavee.UI.AudioImport.Messages;

namespace Wavee.UI.AudioImport;

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
public partial class LocalAudioManagerViewModel : ObservableRecipient, IRecipient<ImportTracksMessage>
{
    // private readonly SourceList<WaveeUserViewModel> _latestImportsSourceList = new();
    // private readonly ObservableCollectionExtended<LocalAudioFile> _latestImports = new();

    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<LocalAudioManagerViewModel> _logger;
    private readonly IAudioDb _db;
    [ObservableProperty]
    private MusicImportProgressController? _progressController;

    private readonly ConcurrentDictionary<Guid, ImportController> _controllers = new();



    public static readonly string[] AcceptedAudioFormats = new[]
    {
        ".mp3", ".ogg", ".wav", ".flac", ".m4a", ".aac", ".wma", ".alac"
    };

    public LocalAudioManagerViewModel(
        IAudioDb db,
        ILogger<LocalAudioManagerViewModel> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger;
        _loggerFactory = loggerFactory;
        _db = db;
        this.IsActive = true;
    }

    public IReadOnlyCollection<LocalAudioFile>
        GetLatestAudioFiles<TK>(Expression<Func<LocalAudioFile, TK>> order,
            bool ascending, int offset = 0, int limit = 20) => _db.GetLatestAudioFiles(order, ascending, offset, limit);


    public void Receive(ImportTracksMessage message)
    {
        _logger.LogInformation("Received message");
        _progressController ??= new MusicImportProgressController();
        //scan for audio files and each folder, and its subdirs.
        //if it's a folder, scan for audio files
        //if it's a file, check if it's an audio file
        //then we check if it's already in the db 
        //if it's not, we add it to the db

        var paths = new List<string>();
        foreach (var (path, isFolder) in message.Value)
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

        var controller = new ImportController(_db, paths, _loggerFactory.CreateLogger<ImportController>());
        _controllers[controller.Id] = controller;
        _progressController.Total += controller.Total;
        _progressController.TotalTasks += 1;

        Task.Factory.StartNew(async () =>
        {
            var prevError = 0;
            var processedCount = 0;
            try
            {
                await foreach (var processed in controller.Process())
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
                    //notify UI
                    if (processed is { Imported: { }, Existing: false })
                        WeakReferenceMessenger.Default.Send(
                            new TrackImportCompleteMessage(processed.Imported));
                }
            }
            catch (Exception x)
            {
                _logger?.LogWarning(x, "An error occured while reading events.");
            }
            finally
            {
                _controllers.TryRemove(controller.Id, out var processedController);
                if (_progressController != null)
                {
                    _progressController.TotalTasks -= 1;
                }

                _logger?.LogDebug("Finished: {processed}/{total} error: {error}",
                        processedCount, controller.Total, controller.Errors.Count);
            }
        });
    }

    public int Count()
    {
        return _db.Count();
    }

    public IEnumerable<GroupedAlbum> GetLatestAlbums<TK>(Expression<Func<LocalAudioFile, TK>> order,
        bool ascending,
        int offset = 0,
        int limit = 20)
    {
        return _db.GetLatestAlbums(order, ascending, offset, limit);
    }
}