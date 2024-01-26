using Wavee.UI.ViewModels.Library;

namespace Wavee.UI.Providers;

public interface IWaveeUIProvider
{
    IWaveeUIAuthenticationProvider Authentication { get; }

    ValueTask Initialize();
    ValueTask InitializeOnAuthenticated();
}

public interface IWaveeUIAuthenticationProvider
{
    IWaveeUIProvider RootProvider { get; }
    IWaveeUIAuthenticatedProfile? AuthenticatedProfile { get; }
    event EventHandler<WaveeUIAuthenticationModule> AuthenticationRequested;
    event EventHandler AuthenticationDone;
}

public interface IWaveeUIAuthenticatedProfile
{
    IWaveeUIProvider Provider { get; }

    Task<IReadOnlyCollection<LibraryItemViewModel>> GetLibraryArtists(KnownLibraryComponentFilterType sort, bool sortAscending, string? filter, CancellationToken cancellation);
    Task<IReadOnlyCollection<LibraryItemViewModel>> GetLibraryAlbums(KnownLibraryComponentFilterType sort, bool sortAscending, string? filter, CancellationToken cancellation);
    Task<IReadOnlyCollection<LibraryItemViewModel>> GetLibraryTracks(KnownLibraryComponentFilterType sort, bool sortAscending, string? filter, CancellationToken cancellation);
}