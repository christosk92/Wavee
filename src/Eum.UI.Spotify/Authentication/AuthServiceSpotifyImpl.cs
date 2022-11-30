using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eum.Connections.Spotify;
using Eum.Connections.Spotify.Connection.Authentication;
using Eum.UI.Services;
using Eum.UI.Users;

namespace Eum.UI.Spotify.Authentication
{
    internal class AuthServiceSpotifyImpl : IAuthenticationService
    {
        public bool IsSupported => true;
        public string? NotSupportedReason => null;

        private readonly ISpotifyClient _spotifyClient;

        public AuthServiceSpotifyImpl(ISpotifyClient spotifyClient)
        {
            _spotifyClient = spotifyClient;
        }
        public async Task<PartialUser> AuthenticateUsernamePassword(string username, string password, CancellationToken ct = default)
        {
            var module = new SpotifyUserPassAuthenticator(username, password);
            var authenticate = await
                _spotifyClient.AuthenticateAsync(module);

            var apiUser = await _spotifyClient.Users.GetCurrentUser(ct);
            //
            // var userGenerator = new UserGenerator(_userDirectories.UsersDir, ServiceType.Spotify);
            // var user = userGenerator.GenerateUser(apiUser.Name, authenticate.Username, ServiceType.Spotify);

            return new PartialUser(authenticate!.Username, apiUser!.Name, apiUser.Avatar.FirstOrDefault()?.Url,
                ServiceType.Spotify);
        }
    }
}
