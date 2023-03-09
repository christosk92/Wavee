using CommunityToolkit.Mvvm.Messaging.Messages;
using Wavee.UI.Identity.Users;
using Wavee.UI.Identity.Users.Contracts;
using Wavee.UI.ViewModels.Identity.User;

namespace Wavee.UI.Identity.Messaging
{
    public class LoggedInUserChangedMessage : ValueChangedMessage<WaveeUser?>
    {
        public LoggedInUserChangedMessage(WaveeUser? user) : base(user)
        {
        }
    }

    public class LoggedOutUserChangedMessage : ValueChangedMessage<WaveeUser>
    {
        public LoggedOutUserChangedMessage(WaveeUser? user) : base(user)
        {
        }
    }

    public class UserAddedMessage : ValueChangedMessage<WaveeUser>
    {
        public UserAddedMessage(WaveeUser value) : base(value)
        {
        }
    }

    public class RequestViewModelForUser : AsyncRequestMessage<WaveeUserViewModel>
    {
        public string UserId { get; init; }
        public ServiceType ForService { get; init; }
        public CancellationToken CancellatonToken { get; init; }
    }
}
