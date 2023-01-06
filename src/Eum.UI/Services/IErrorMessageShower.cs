namespace Eum.UI.Services
{
    public interface IErrorMessageShower
    {
        Task ShowErrorAsync(Exception notImplementedException, string title,
            string description);
    }
}
