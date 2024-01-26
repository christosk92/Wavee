using System.Collections.Immutable;
using Wavee.Spfy;
using Wavee.Spfy.Items;

namespace Wavee.UI.Providers.Spotify;

public static class SpotifyLibrary
{
    public static async Task<Dictionary<WaveeSpotifyLibraryItem, ISpotifyItem>> GetAlbumAndTracks(
        WaveeSpotifyClient client)
    {

        {
            var idOutput = new Dictionary<WaveeSpotifyLibraryItem, ISpotifyItem?>();
            var results = client.Library.GetCollection();
            await foreach (var page in results)
            {
                foreach (var item in page.Items)
                {
                    idOutput[item] = null;
                }
                //idOutput.AddRange(page.Items);
            }

            var country = await client.CountryCode;
            await client.Metadata.FillBatched(idOutput, country, static x => x.Id);
            return idOutput;
        }
    }

    public static async Task<Dictionary<WaveeSpotifyLibraryItem, ISpotifyItem>> GetArtists(WaveeSpotifyClient client)
    {

        var idOutput = new Dictionary<WaveeSpotifyLibraryItem, ISpotifyItem?>();
        var results = client.Library.GetArtists();
        await foreach (var page in results)
        {
            foreach (var item in page.Items)
            {
                idOutput[item] = null;
            }
            //idOutput.AddRange(page.Items);
        }

        var country = await client.CountryCode;
        await client.Metadata.FillBatched(idOutput, country, static x => x.Id);
        return idOutput;
    }
}