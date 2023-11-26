using System.Reflection.Metadata;
using Eum.Spotify;
using Eum.Spotify.login5v3;
using Google.Protobuf;
using Microsoft.Extensions.DependencyInjection;
using Wavee.Spotify;
using Wavee.Spotify.Application.Authentication.Modules;
using Wavee.Spotify.Common.Contracts;

var storagePath = "/d/";
Directory.CreateDirectory(storagePath);
var sp = new ServiceCollection()
    .AddSpotify(new SpotifyClientConfig
    {
        Storage = new StorageSettings
        {
            Path = storagePath
        },
        Remote = new SpotifyRemoteConfig()
    })
    .WithStoredOrOAuthModule(OpenBrowser)
    .AddMediator()
    .BuildServiceProvider();

var client = sp.GetRequiredService<ISpotifyClient>();
var me = await client.Test();
var x = "";


static Task<string> OpenBrowser(string url, CancellationToken ct)
{
    Console.WriteLine($"Open browser: {url} and return redirect url");

    var redirect = Console.ReadLine();
    return Task.FromResult(redirect);
}