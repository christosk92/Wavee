using System.Text.Json;
using LanguageExt;
using ReactiveUI;
using Wavee.Core.Ids;
using Wavee.UI.Infrastructure.Sys;
using Wavee.UI.Infrastructure.Traits;

namespace Wavee.UI.ViewModels;

public sealed class ArtistViewModel<R> : ReactiveObject, INavigableViewModel
    where R : struct, HasSpotify<R>
{
    private readonly R _runtime;

    public ArtistViewModel(R runtime)
    {
        _runtime = runtime;
    }

    public async void OnNavigatedTo(object? parameter)
    {
        if (parameter is not AudioId artistId)
            return;

        var id = artistId.ToBase62();
        const string fetch_uri = "hm://artist/v1/{0}/desktop?format=json&catalogue=premium&locale={1}&cat=1";
        var url = string.Format(fetch_uri, id, "en");
        var aff =
            from mercuryClient in Spotify<R>.Mercury().Map(x=> x)
            from response in mercuryClient.Get(url, CancellationToken.None).ToAff()
            select response;
        var result = await aff.Run(runtime: _runtime);
        var r = result.ThrowIfFail();
        using var jsonDoc = JsonDocument.Parse(r.Payload);
        var k = "";
    }

    public void OnNavigatedFrom()
    {

    }
}