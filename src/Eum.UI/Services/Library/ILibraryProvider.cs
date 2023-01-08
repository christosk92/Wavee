using Eum.Connections.Spotify.Connection;
using Eum.Enums;
using Eum.UI.Items;
using Eum.UI.ViewModels.Artists;

namespace Eum.UI.Services.Library
{
    public interface ILibraryProvider
    {
        ValueTask InitializeLibrary(CancellationToken ct = default);
        bool IsSaved(ItemId id);
        int LibraryCount(EntityType type);
        int TotalLibraryCount { get; }
        bool IsInitializing { get; }
        event EventHandler<(EntityType Type, IReadOnlyList<CollectionUpdate> Ids)>? CollectionUpdated; 
        void Deconstruct();
        void SaveItem(ItemId id);
        void UnsaveItem(ItemId id);
    }
}
