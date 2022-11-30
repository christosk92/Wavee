using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Eum.UI.Services;

namespace Eum.UI.Spotify
{
    internal class ServiceFetcherSpotify_Impl : IServiceFetcher
    {
        public IAuthenticationService AuthService { get; }
    }
}
