using CommunityToolkit.Mvvm.ComponentModel;
using Wavee.UI.Identity.Users.Contracts;

namespace Wavee.UI.Identity.Users
{
    public class WaveeUser : ObservableObject, IWaveeUser, IEquatable<WaveeUser>
    {
        public WaveeUser(ServiceType serviceType, string userFullpath) : this(serviceType, UserData.FromFile(userFullpath)) { }

        public WaveeUser(
            ServiceType serviceType,
            UserData data)
        {
            ServiceType = serviceType;
            UserData = data;
        }


        public string Id => UserData.Username;
        public ServiceType ServiceType
        {
            get;
        }
        public UserData UserData
        {
            get; private set;
        }

        public void UpdateUserData(UserData newUserData)
        {
            ArgumentNullException.ThrowIfNull(UserData);
            UserData = newUserData;
            UserData.ToFile();
        }


        public bool Equals(WaveeUser? other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return ServiceType == other.ServiceType && Id.Equals(other.Id);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((WaveeUser)obj);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)ServiceType, Id);
        }
        public static bool operator ==(WaveeUser? left, WaveeUser? right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(WaveeUser? left, WaveeUser? right)
        {
            return !Equals(left, right);
        }
    }
}
