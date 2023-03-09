using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.DependencyInjection;
using CommunityToolkit.Mvvm.Messaging;
using Wavee.UI.Identity.Messaging;
using Wavee.UI.Identity.Users;

namespace Wavee.UI.ViewModels.Identity.User
{
    public class WaveeUserViewModel
    {
        public static WaveeUserViewModel Create(WaveeUser messageValue)
        {
            return new WaveeUserViewModel
            {
                User = messageValue,
                IsLoggedIn = false,
            };
        }

        public WaveeUser User { get; init; }
        public bool IsLoggedIn { get; set; }
        public string DisplayName => User.UserData.DisplayName ?? User.UserData.Username;

        public void SignIn()
        {
            IsLoggedIn = true;
            WeakReferenceMessenger.Default.Send(new LoggedInUserChangedMessage(User));
        }

        public void SignOut()
        {
            IsLoggedIn = false;
            WeakReferenceMessenger.Default.Send(new LoggedOutUserChangedMessage(User));
        }
    }
}
