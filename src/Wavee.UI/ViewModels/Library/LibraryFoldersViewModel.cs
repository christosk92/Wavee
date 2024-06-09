using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryFoldersViewModel : LibraryCategoryViewModel
{
    public LibraryFoldersViewModel(IObservable<IChangeSet<IFolder, string>> connect) : base("Folders",
        Icons.MediaPlayer("\uE8B7"),
        Icons.SegoeFluent("\uE8D5"))
    {
        connect
            .Transform(folder => CreateFolderViewModel(folder))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var items)
            .Subscribe();

        Items = items;
    }

    private object CreateFolderViewModel(IFolder folder)
    {
        //TODO:
        return new object();
    }

    public ReadOnlyObservableCollection<object> Items { get; }
}