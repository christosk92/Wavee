using Eum.UI.Helpers;

namespace Eum.UI.Items;

public readonly struct ItemId : IComparable<ItemId>, IEquatable<ItemId>
{
    public override bool Equals(object? obj)
    {
        return obj is ItemId other && Equals(other);
    }

    public ItemId(string uri)
    {
        Uri = uri;
        var s =
            uri.SplitLines();

        var i = 0;
        while (s.MoveNext())
        {
            switch (i)
            {
                case 0:
                    Service = (ServiceType) GetServiceType(s.Current.Line);
                    break;
                case 1:
                    Type = (EumEntityType) GetType(s.Current.Line);
                    break;
            }

            i++;
        }

        Id = s.Current.Line.ToString();
    }
    public ServiceType Service { get; }
    public string Uri { get; }
    public EumEntityType Type { get; }
    public string? Id { get; }


    private static int GetType(ReadOnlySpan<char> r)
    {
        return r switch
        {
            "station" => 2,
            "track" => 3,
            "artist" => 1,
            "album" => 4,
            "show" => 5,
            "episode" => 6,
            "playlist" => 7,
            "collection" => 8,
            "user" => 9,
            "local" => 10,
            _ => 0,
        };
    }

    private static int GetServiceType(ReadOnlySpan<char> r)
    {
        return r switch
        {
            "local" => 0,
            "spotify" => 1,
            _ => 0,
        };
    }
    public int Compare(ItemId x, ItemId y)
    {
        return string.Compare(x.Uri, y.Uri, StringComparison.Ordinal);
    }

    public int CompareTo(ItemId other)
    {
        return string.Compare(Uri, other.Uri,
            StringComparison.Ordinal);
    }

    public bool Equals(ItemId other)
    {
        return Uri == other.Uri;
    }


    public static bool operator ==(ItemId left, ItemId right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(ItemId left, ItemId right)
    {
        return !left.Equals(right);
    }

    public override int GetHashCode()
    {
        return Uri?.GetHashCode() ?? 0;
    }
}