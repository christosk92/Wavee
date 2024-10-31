using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Wavee.ViewModels.Interfaces;

namespace Wavee.ViewModels.Models.Users;

public record UserFactory(
    string DataDir)
{
    public User Create(IUserAuthenticator authenticator)
    {
        return new(DataDir, "New User", authenticator);
    }

    public User CreateAndInitialize(IUserAuthenticator userManager)
    {
        User user= Create(userManager);
        user.Initialize();

        return user;
    }
}
