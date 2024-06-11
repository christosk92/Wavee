using System.Reactive.Linq;
using Eum.Spotify.connectstate;
using Wavee;
using Wavee.UI.Spotify;
using Wavee.UI.Spotify.Auth;

var player = new WaveePlayer();
var client = new SpotifyClient(new SpotifyConfig(new SpotifyOAuthConfiguration(OpenBrowserRequest), player));

ValueTask OpenBrowserRequest(string url)
{
    Console.WriteLine(url);
    return new ValueTask();
}

var result = await client.Playback.Connect("Wavee", DeviceType.Computer, new CancellationToken());
result.ConnectionStatus
    .Subscribe(x => Console.WriteLine($"Connection status: {x}"));
result.State
    .Subscribe(xy =>
    {
        if (xy.IsT0)
        {
            // StateError
            Console.WriteLine($"Error: {xy.AsT0}");
            return;
        }
        var x = xy.AsT1;
        
        // Position: 00:00:00 - 00:00:00
        if (x.CurrentItem is null)
        {
            Console.WriteLine("No item playing");
            return;
        }

        var pos = x.Position.Value;
        var duration = x.CurrentItem?.Duration;

        Console.WriteLine($"{x.CurrentItem.Name} - {x.CurrentItem.MainContributor.Name}");
        Console.WriteLine(
            $"Position: {pos.Hours:D2}:{pos.Minutes:D2}:{pos.Seconds:D2} - {duration?.Hours:D2}:{duration?.Minutes:D2}:{duration?.Seconds:D2}");
    });
//
// var test = await client.Home.GetItems(CancellationToken.None);
// Console.WriteLine(test.Count);


Console.ReadLine();