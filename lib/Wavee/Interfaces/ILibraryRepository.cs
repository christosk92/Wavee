// ILibraryRepository.cs

using Wavee.Models.Library;

namespace Wavee.Interfaces;

/// <summary>
/// Provides methods to interact with the library repository.
/// </summary>
public interface ILibraryRepository
{
    /// <summary>
    /// Adds or updates multiple library items in the repository.
    /// </summary>
    /// <param name="items">The collection of library items to add or update.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous add or update operation.</returns>
    Task AddOrUpdateItemsAsync(IEnumerable<SpotifyLibraryItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes multiple library items from the repository.
    /// </summary>
    /// <param name="items">The collection of library items to delete.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous delete operation.</returns>
    Task DeleteItemsAsync(IEnumerable<SpotifyLibraryItem> items, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all library items from the repository.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous retrieval operation. The task result contains the list of library items.</returns>
    Task<List<SpotifyLibraryItem>> GetAllItemsAsync(CancellationToken cancellationToken = default);
}