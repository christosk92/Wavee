using System.Collections.ObjectModel;
using System.Reactive.Disposables;
using DynamicData;
using DynamicData.Aggregation;
using DynamicData.Binding;
using ReactiveUI;
using Wavee.ViewModels.Enums;
using Wavee.ViewModels.Extensions;
using Wavee.ViewModels.Interfaces;
using Wavee.ViewModels.Models;
using Wavee.ViewModels.ViewModels.Library.Items;
using Wavee.ViewModels.ViewModels.Navigation;

namespace Wavee.ViewModels.ViewModels.Library;

public abstract partial class AbsLibraryComponentViewModel<TItemViewmodel> : RoutableViewModel where TItemViewmodel : LibraryItemViewModel
{
    private readonly ReadOnlyObservableCollection<TItemViewmodel> _items;
    //[AutoNotify] private int _count;
    private readonly CompositeDisposable _disposables = new();
    private int _count;

    protected AbsLibraryComponentViewModel(LibraryItemType type, ILibraryService libraryService)
    {
        var baseAggregate = libraryService
            .Library
            .Filter(x => x.Type == type);

        Type = type;
        baseAggregate
            .TransformWithInlineUpdate(Create, (model, item) => model.Update(item))
            .SortAndBind(
                out _items,
                SortExpressionComparer<LibraryItemViewModel>.Descending(i => i.AddedAt))
            .Subscribe()
            .DisposeWith(_disposables);

        // subscribe to count
        baseAggregate
            .Count()
            .Subscribe(count =>
            {
                Count = count;
            })
            .DisposeWith(_disposables);
    }

    public int Count
    {
        get => _count;
        set => this.RaiseAndSetIfChanged(ref _count, value);
    }

    public ReadOnlyObservableCollection<TItemViewmodel> Items => _items;
    
    public LibraryItemType Type { get; }
    
    protected abstract TItemViewmodel Create(LibraryItem item);
    
}