using DynamicData;
using Wavee.ViewModels.Models.Users;

namespace Wavee.ViewModels.Interfaces;

public interface IUserRepository
{
    IObservableCache<UserModel, string> Users { get; }
    string DefaultUserId { get; }
    bool HasUser { get; }
    void StoreLastSelectedUser(UserModel user);
}