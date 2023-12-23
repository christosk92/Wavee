using ConsoleApp1;
using LiteDB;
using Wavee.Spotify;

var db = new LiteDatabase("credentials.db");
var repo = new LiteDbCredentialsStorage(db);
var config = new WaveeSpotifyConfig
{
    CredentialsStorage = new WaveeSpotifyCredentialsStorageConfig
    {
        GetDefaultUsername = () => repo.GetDefaultUserName(),
        OpenCredentials = (name, type) =>
        {
            var credentials = repo.GetFor(name, type);
            return credentials;
        },
        SaveCredentials = (name, type, data) =>
        {
            repo.Store(name, type, data);
        }
    }
};
var client = WaveeSpotifyClient.Create(config, OpenBrowserAndReturnCallback);
var track = await client.Track.GetTrackAsync("4o8YocVCmZZimcqyY1Z5rO", false,
    cancellationToken: CancellationToken.None);
//var token = await client.Token.GetAccessToken();
var newConnectionMade = await client.Remote.Connect();
if (newConnectionMade)
{
    Console.WriteLine("Connected!");
}
else
{
    Console.WriteLine("Already connected!");
}
var test = "";
Task<string> OpenBrowserAndReturnCallback(string url)
{
    // Console.WriteLine($"Please open this url in your browser: {url}");
    // Console.WriteLine("Please enter the callback url:");
    // return Task.FromResult(Console.ReadLine());
    
    Console.WriteLine($"Please open this url in your browser: {url}");
    
    Console.WriteLine("Please enter the callback url:");
    var callbackUrl = Console.ReadLine();
    return Task.FromResult(callbackUrl);
}