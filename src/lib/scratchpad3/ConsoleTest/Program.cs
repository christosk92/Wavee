using System.Diagnostics;
using System.Text;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using LanguageExt.Common;
using Wavee.Spotify;
using Wavee.Spotify.Clients.Mercury;
using Wavee.Spotify.Infrastructure.Sys;
using static LanguageExt.Prelude;

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ = AuthenticationType.AuthenticationUserPass
};

var client = await SpotifyRuntime.Authenticate(loginCredentials);

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
    var test = await client.Mercury.Send(
        method,
        uri,
        None);
    sw.Stop();
    Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds}ms");
    Console.WriteLine($"{test.Header.StatusCode}");
    Console.WriteLine(Encoding.UTF8.GetString(test.Body.Span));
    GC.Collect();
}

Console.ReadLine();