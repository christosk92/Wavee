using DynamicData;
using System.Reactive;
using System.Reactive.Linq;

namespace Wavee.UI.Common;

public static class ObservableExtensions
{
    public static IDisposable SubscribeAsync<T>(this IObservable<T> source, Func<T, Task> onNextAsync) =>
        source
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Concat()
            .Subscribe();

    public static IObservable<Unit> DoAsync<T>(this IObservable<T> source, Func<T, Task> onNextAsync) =>
        source
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Concat();

    public static IObservable<Unit> ToSignal<T>(this IObservable<T> source) => source.Select(_ => Unit.Default);

    public static IObservable<T> ReplayLastActive<T>(this IObservable<T> observable)
    {
        return observable.Replay(1).RefCount();
    }

    public static IDisposable RefillFrom<TObject, TKey>(this ISourceCache<TObject, TKey> sourceCache, IObservable<IEnumerable<TObject>> contents) where TKey : notnull
    {
        return contents.Subscribe(list => sourceCache.Edit(updater => updater.Load(list)));
    }
}