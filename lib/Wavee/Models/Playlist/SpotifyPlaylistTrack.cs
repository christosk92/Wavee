using System.ComponentModel;
using System.Runtime.CompilerServices;
using Wavee.Models.Common;

namespace Wavee.Models.Playlist;

public sealed class SpotifyPlaylistTrack : INotifyPropertyChanged
{
    private int _index;
    private SpotifyId _id;
    private string? _addedBy;
    private DateTimeOffset? _addedAt;
    private bool _initialized;
    private int _originalIndex;
    private string? _uid;
    private SpotifyItem? _item;

    public Guid InstanceId { get; } = Guid.NewGuid();

    public int Index
    {
        get => _index;
        set => SetField(ref _index, value);
    }

    public int OriginalIndex
    {
        get => _originalIndex;
        set => SetField(ref _originalIndex, value);
    }

    public bool Initialized
    {
        get => _initialized;
        set => SetField(ref _initialized, value);
    }

    public SpotifyId Id
    {
        get => _id;
        set => SetField(ref _id, value);
    }

    public DateTimeOffset? AddedAt
    {
        get => _addedAt;
        set => SetField(ref _addedAt, value);
    }

    public string? AddedBy
    {
        get => _addedBy;
        set => SetField(ref _addedBy, value);
    }

    public string? Uid
    {
        get => _uid;
        set => SetField(ref _uid, value);
    }
    
    public SpotifyItem? Item
    {
        get => _item;
        set => SetField(ref _item, value);
    }
    
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private bool SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    public Dictionary<string, string> CreateMetadataDictionary()
    {
        var dictionary = new Dictionary<string, string>();
        if (AddedAt.HasValue)
            dictionary.Add("added_at", AddedAt.Value.ToUnixTimeMilliseconds().ToString());
        if (!string.IsNullOrWhiteSpace(AddedBy))
            dictionary.Add("added_by", AddedBy);
        if (!string.IsNullOrWhiteSpace(Uid))
            dictionary.Add("uid", Uid);
        return dictionary;
    }
}