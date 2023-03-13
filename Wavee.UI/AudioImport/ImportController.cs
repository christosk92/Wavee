using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;
using TagLib;
using Wavee.UI.AudioImport.Database;

namespace Wavee.UI.AudioImport;

public class ImportController : ObservableObject
{
    // private int _processed;
    // private int _total;
    // private readonly ChannelWriter<ImportingProgressEventArgs> _progressWriter;

    private readonly IList<string> _paths;
    private readonly ILogger<ImportController> _logger;

    private readonly IAudioDb _db;

    // internal ChannelReader<ImportingProgressEventArgs> EventsReader { get; }

    public int Total { get; private set; }

    public Guid Id { get; }

    internal ImportController(IAudioDb db,
        IList<string> paths,
        ILogger<ImportController> logger)
    {
        // var channels = Channel.CreateUnbounded<ImportingProgressEventArgs>();
        // EventsReader = channels.Reader;
        // _progressWriter = channels.Writer;

        _logger = logger;
        _paths = paths;
        _db = db;
        Id = Guid.NewGuid();
    }

    public async IAsyncEnumerable<(LocalAudioFile? Imported, bool Existing)> Process(CancellationToken ct = default)
    {
        var filesExist = _db.CheckIfAudioFilesExist(_paths);

        int i = 0;
        await foreach (var (tag, path) in EnumerateTags(_paths)
                           .WithCancellation(ct))
        {
            if (ct.IsCancellationRequested)
                break;
            LocalAudioFile? imported = null;
            bool existing = false;
            try
            {
                if (filesExist[i])
                {
                    imported = _db.GetAudioFile(path);
                    existing = true;
                }
                else
                {
                    imported = await _db.ImportAudioFile(new ImportAudioRequest(tag, path));
                }
            }
            catch (Exception x)
            {
                _logger.LogError(x, "Error importing audio file {Path}", path);
                Errors.TryAdd(path, x);
            }

            yield return (imported, existing);
        }
        // await _progressWriter.WriteAsync(new ImportingProgressEventArgs
        // {
        //     Errors = Errors,
        //     Progress = _processed,
        //     Total = _total,
        //     IsDone = true
        // }, ct);
        // _progressWriter.Complete();
    }

    private async IAsyncEnumerable<(Tag Tag, string Path)> EnumerateTags(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            var tag = await OpenTag(path);
            if (tag is not null)
            {
                yield return (tag, path);
            }
        }
    }

    private async Task<Tag?> OpenTag(string path)
    {
        try
        {
            var file = await Task.Run(() => TagLib.File.Create(path));
            Total++;
            return file.Tag;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error opening tag for {Path}", path);
            Errors.TryAdd(path, e);
            return null;
        }
    }

    public ConcurrentDictionary<string, Exception> Errors { get; } = new();
}