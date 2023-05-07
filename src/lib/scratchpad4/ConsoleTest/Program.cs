using System.Diagnostics;
using System.Security.Cryptography;
using Eum.Spotify;
using Google.Protobuf;
using LanguageExt;
using Spotify.Metadata;
using Wavee.Spotify;
using Wavee.Spotify.Sys.Mercury;

var loginCredentials = new LoginCredentials
{
    Username = Environment.GetEnvironmentVariable("SPOTIFY_USERNAME"),
    AuthData = ByteString.CopyFromUtf8(Environment.GetEnvironmentVariable("SPOTIFY_PASSWORD")),
    Typ= AuthenticationType.AuthenticationUserPass
};

var info = await SpotifyClient.Authenticate(loginCredentials);
var countryCode = await info.CountryCode();
var productInfo = await info.ProductInfo();
var countryCodeAgain = await info.CountryCode();
//https://open.spotify.com/artist/0YLlTW9rW7ZCy2cA2u3RYk?si=DkVa5yDpQ_acn-yS_EM2Jw
//https://open.spotify.com/artist/0SfsnGyD8FpIN4U4WCkBZ5?si=rwHMbXOVSES-a2MJeUMuew
var k = "hm://artist/v1/0YLlTW9rW7ZCy2cA2u3RYk/desktop ?format=json&catalogue=premium&locale=en&cat=1";
var sw = Stopwatch.StartNew();
var r = await info.Get(k, Option<string>.None);
sw.Stop();
info.WelcomeChanged.Subscribe(option =>
{

});
Console.ReadLine();