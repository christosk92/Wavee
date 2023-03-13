using System.Linq.Expressions;
using CommunityToolkit.Mvvm.DependencyInjection;
using LiteDB;
using Wavee.Spotify.Player;
using Wavee.UI.AudioImport;
using Wavee.UI.AudioImport.Database;
using Wavee.UI.ViewModels.Playback.Impl;

namespace Wavee.UI.ViewModels.Playback.Contexts.Local;
public class LocalLibraryContext : ILocalContext
{
    private readonly IAudioDb _db;
    private readonly int _offset;
    private readonly PlayOrderType _order;
    private readonly bool _descending;
    private readonly LibraryViewType _libraryViewType;
    private LocalLibraryContext(IAudioDb db, PlayOrderType order, int offset, LibraryViewType libraryViewType, bool descending)
    {
        _db = db;
        _offset = offset;
        _libraryViewType = libraryViewType;
        _descending = descending;
        _order = order;
    }

    public LocalAudioFile? GetTrack(int index) => BuildQuery().Skip(index)
        .FirstOrDefault();

    private IEnumerable<LocalAudioFile?> BuildQuery()
    {
        var orderedQuery =
            _descending
                ? _db.AudioFiles.Query()
                    .OrderByDescending<object>(_order switch
                    {
                        PlayOrderType.Name => a => a.Name,
                        PlayOrderType.Imported => a => a.CreatedAt,
                        _ => throw new ArgumentOutOfRangeException()
                    })
                : _db.AudioFiles.Query()
                    .OrderBy<object>(_order switch
                    {
                        PlayOrderType.Name => a => a.Name,
                        PlayOrderType.Imported => a => a.CreatedAt,
                        _ => throw new ArgumentOutOfRangeException()
                    });

        return _libraryViewType switch
        {
            LibraryViewType.Songs =>
                orderedQuery
                .Offset(_offset)
                .ToEnumerable(),
            LibraryViewType.Albums =>
                orderedQuery
                .ToEnumerable()
                .GroupBy(a => a.Album)
                .Skip(_offset)
                .SelectMany(a => a
                    .OrderBy(j => j.Track)),
            LibraryViewType.Artists => throw new NotImplementedException(),
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public int Length => BuildQuery().Count();

    public static LocalLibraryContext Create(
        PlayOrderType order,
        bool descending,
        int offset,
        LibraryViewType viewType)
    {
        return new LocalLibraryContext(Ioc.Default.GetRequiredService<IAudioDb>(),
            order,
            offset,
            viewType,
            descending);
    }
}

public enum PlayOrderType
{
    Name,
    Imported
}