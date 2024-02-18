using System.Data;
using System.Diagnostics;
using Eum.Spotify.connectstate;
using Wavee.Core;
using Wavee.Spotify;
using Wavee.Spotify.Auth.OAuth;

var config = SpotifyClientConfig.CreateDefault().WithAuthenticator(new OAuthAuthenticator(OpenBrowserRequestDelegate));

var client = new SpotifyClient(config);
var me = await client.Player.Connect("Wavee", DeviceType.Computer);
me.CurrentlyPlaying.Subscribe(x =>
{
    var y = x;
    var pos = y.Position.ToString("g");
    Console.WriteLine($"Position: {pos}");
});
//await me.Transfer(true, CancellationToken.None);
Console.ReadKey();


ValueTask OpenBrowserRequestDelegate(string url)
{
    var uriStr = url.ToString().Replace("&", "^&");
    Process.Start(new ProcessStartInfo($"cmd", $"/c start {uriStr}"));
    return ValueTask.CompletedTask;
}