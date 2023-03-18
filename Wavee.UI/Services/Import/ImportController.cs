using System.Collections.Concurrent;
using CommunityToolkit.Mvvm.ComponentModel;
using ImageMagick;
using Microsoft.Extensions.Logging;
using Nito.AsyncEx;
using TagLib;
using Wavee.UI.Interfaces.Services;
using Wavee.UI.Models.Local;
using File = TagLib.File;

namespace Wavee.UI.Services.Import;

public class ImportController : ObservableObject
{
    private static AsyncLock _importLock = new AsyncLock();
    private readonly IList<string> _paths;
    private readonly ILogger<ImportController>? _logger;

    private readonly ILocalAudioDb _db;

    public int Total { get; private set; }

    public Guid Id { get; }

    private readonly IAppDataProvider _appDataProvider;

    internal ImportController(
        ILocalAudioDb db,
        IList<string> paths,
        IAppDataProvider appDataProvider,
        ILogger<ImportController>? logger = null)
    {
        _logger = logger;
        _paths = paths;
        _appDataProvider = appDataProvider;
        _db = db;
        Id = Guid.NewGuid();
    }

    internal async Task<IEnumerable<(LocalTrack? Imported, bool Existing)>> Process(
        Action? Processed = null,
        CancellationToken ct = default)
    {
        var results = new List<(LocalTrack?, bool existing)>();
        using (await _importLock.LockAsync())
        {
            var filesExist = _db.CheckIfAudioFilesExist(_paths);

            int i = 0;
            foreach (var path in _paths)
            {
                if (ct.IsCancellationRequested)
                    break;
                LocalTrack? imported = null;
                bool existing = false;
                try
                {
                    var tag = await OpenTag(path);


                    imported = await CreateFileFromPath(tag, path);

                    if (filesExist[i])
                    {
                        await _db.UpdateTrackAsync(imported.Value, ct);
                        existing = true;
                    }
                    else
                    {
                        await _db.InsertTrackAsync(imported.Value, ct);
                    }
                }
                catch (Exception x)
                {
                    _logger?.LogError(x, "Error importing audio file {Path}", path);
                    Errors.TryAdd(path, x);
                }
                finally
                {
                    Processed?.Invoke();
                }

                i++;
                results.Add((imported, existing));
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

        return results;
    }

    private async Task<LocalTrack> CreateFileFromPath(File file, string path)
    {
        var tag = file.Tag;

        //check if image exists, based on album name
        var album = string.Join("_", tag.Album.Split(Path.GetInvalidFileNameChars()));
        var albumArtPath = Path.Combine(_appDataProvider.GetAppDataRoot(), "images");
        Directory.CreateDirectory(albumArtPath);
        var imagePath = Path.Combine(albumArtPath, album);
        var image = tag.Pictures.FirstOrDefault();

        if (!System.IO.File.Exists(imagePath) && image != null)
        {
            await using var mem = ResizeImage(image.Data.Data, 500, 500);
            mem.Position = 0;
            await using var fs = System.IO.File.Create(imagePath);
            await mem.CopyToAsync(fs);
        }
        else if (image == null)
        {
            imagePath = null;
        }

        return new LocalTrack
        {
            Id = path,
            Album = tag.Album,
            AlbumArtists = tag.AlbumArtists,
            AlbumArtistsSort = tag.AlbumArtistsSort,
            AlbumSort = tag.AlbumSort,
            BeatsPerMinute = tag.BeatsPerMinute,
            Comment = tag.Comment,
            Composers = tag.Composers,
            ComposersSort = tag.ComposersSort,
            Conductor = tag.Conductor,
            Disc = tag.Disc,
            DiscCount = tag.DiscCount,
            Copyright = tag.Copyright,
            Description = tag.Description,
            Duration = file.Properties.Duration.TotalMilliseconds,
            Genres = tag.Genres,
            DateTagged = tag.DateTagged,
            Grouping = tag.Grouping,
            Lyrics = tag.Lyrics,
            Track = tag.Track,
            Performers = tag.Performers,
            PerformersRole = tag.PerformersRole,
            PerformersSort = tag.PerformersSort,
            ISRC = tag.ISRC,
            TrackCount = tag.TrackCount,
            Year = tag.Year,
            Title = !string.IsNullOrEmpty(tag.Title)
                ? tag.Title
                : Path.GetFileNameWithoutExtension(path),
            TitleSort = tag.TitleSort,
            Publisher = tag.Publisher,
            Subtitle = tag.Subtitle,
            LastChanged = new FileInfo(path).LastWriteTimeUtc,
            DateImported = DateTime.UtcNow,
            Image = imagePath
        };
    }


    private async Task<File> OpenTag(string path)
    {
        try
        {
            var file = await Task.Run(() => TagLib.File.Create(path));
            Total++;
            return file;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Error opening tag for {Path}", path);
            Errors.TryAdd(path, e);
            return null;
        }
    }

    public ConcurrentDictionary<string, Exception> Errors { get; } = new();

    private static Stream ResizeImage(byte[] imageStream, int width, int height)
    {
        var settings = new MagickReadSettings
        {
            Width = width,
            Height = height
        };

        var memStream = new MemoryStream();
        using var image = new MagickImage(imageStream);
        var size = new MagickGeometry(width, height)
        {
            // This will resize the image to a fixed size without maintaining the aspect ratio.
            // Normally an image will be resized to fit inside the specified size.
            IgnoreAspectRatio = true
        };

        image.Format = MagickFormat.Png;
        image.Resize(size);

        image.Write(memStream);

        return memStream;
    }

    private readonly TaskCompletionSource<IReadOnlyCollection<(LocalTrack? Imported, bool Existing)>> _waitForFinish = new();

    public Task<IReadOnlyCollection<(LocalTrack? Imported, bool Existing)>> WaitForFinish()
    {
        return _waitForFinish.Task;
    }
    public void Set(IReadOnlyCollection<(LocalTrack? Imported, bool Existing)> tracks)
    {
        _waitForFinish.SetResult(tracks);
    }

    public void SetError(Exception? error)
    {
        _waitForFinish.SetException(error);
    }
}