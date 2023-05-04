using System.Diagnostics;
using System.Text;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Common;
using Wavee.Spotify;
using Wavee.Spotify.Mercury;
using static LanguageExt.Prelude;

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var result = await SpotifyRuntime.Authenticate(loginCredentials, Option<ISpotifyListener>.Some(new SpotifyListener()));

while (true)
{
    var msg = Console.ReadLine();
    var sw = Stopwatch.StartNew();

    //format is [GET|SEND|] uri
    var method = msg[..3].Split(" ")[0] switch
    {
        "get" => MercuryMethod.Get,
        "send" => MercuryMethod.Send,
        _ => MercuryMethod.Get
    };
    var uri = msg[3..].Trim();
    var test = await MercuryRuntime.Send(
        method,
        uri,
        None, None);
    sw.Stop();
    Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}ms");
    Console.WriteLine($"{test.Header.StatusCode}");
    Console.WriteLine(Encoding.UTF8.GetString(test.Body.Span));
    GC.Collect();
}

Console.ReadLine();

public class SpotifyListener : ISpotifyListener
{
    public Unit OnConnected(Guid connectionId)
    {
        Debug.WriteLine($"Connected: {connectionId}");
        return unit;
    }

    public Unit OnDisconnected(Option<Error> error)
    {
        throw new NotImplementedException();
    }

    public Unit CountryCodeReceived(string countryCode)
    {
        Debug.WriteLine($"CountryCodeReceived: {countryCode}");
        return unit;
    }
}