using Wavee.UI.Identity.Users.Contracts;

namespace Wavee.UI.Identity.Users
{
    public class WaveeUserManagerFactory
    {
        private readonly WaveeUserManager[] _managers;

        public WaveeUserManagerFactory(IEnumerable<WaveeUserManager> managers)
        {
            _managers = managers.ToArray();
        }

        public WaveeUserManager GetManager(ServiceType serviceType)
        {
            return _managers.FirstOrDefault(a => a.ServiceType == serviceType)
                   ?? throw new KeyNotFoundException();
        }
    }
}
