using Eum.UI.Users;

namespace Eum.UI.Services.Login
{
    public class IdentityService
    {
        private static IdentityService _instance;
        public EumUser? CurrentUser { get; private set; }
        public static IdentityService Instance => _instance ??= new IdentityService();

        public event EventHandler<EumUser> UserLoggedIn; 
        public void LoginUser(EumUser user)
        {
            CurrentUser = user;
            UserLoggedIn?.Invoke(null, user);
        }
    }
}
