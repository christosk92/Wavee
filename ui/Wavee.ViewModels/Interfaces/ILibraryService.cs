using DynamicData;
using Wavee.Models.Common;
using Wavee.ViewModels.Models;

namespace Wavee.ViewModels.Interfaces;

public interface ILibraryService
{
    Task InitializeAsync();
    IObservable<IChangeSet<LibraryItem, SpotifyId>> Library { get; }
}