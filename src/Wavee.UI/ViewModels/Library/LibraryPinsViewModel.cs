using System;
using System.Reactive.Linq;
using DynamicData;
using ReactiveUI;
using Wavee.Contracts.Interfaces;
using Wavee.Contracts.Interfaces.Contracts;
using Wavee.UI.ViewModels.Library.List;

namespace Wavee.UI.ViewModels.Library;

public sealed class LibraryPinsViewModel : LibraryCategoryViewModel
{
    public LibraryPinsViewModel(IObservable<IChangeSet<IPinnableItem, string>> pins) : base("Pins",
        Icons.SegoeFluent("\uE718"),
        Icons.SegoeFluent("\uE718"))
    {
        pins
            .Transform(x => CreateViewModel(x))
            .ObserveOn(RxApp.MainThreadScheduler)
            .Bind(out var pinsCollection)
            .Subscribe();

        SubItems = pinsCollection;
    }

    private object CreateViewModel(IPinnableItem pinnableItem)
    {
        //TODO:
        return new PinnedItemViewModel(pinnableItem);
    }
}