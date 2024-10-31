namespace Wavee.ViewModels.Models.Users;

public enum UserState
{
    Uninitialized,
    WaitingForInit,
    Initialized,
    Starting,
    Started,
    Stopping,
    Stopped
}