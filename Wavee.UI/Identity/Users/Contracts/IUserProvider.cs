namespace Wavee.UI.Identity.Users.Contracts
{
    public interface IUserProvider
    {
        Task<IReadOnlyCollection<WaveeUser>> GetUsersAsync();
    }
}
