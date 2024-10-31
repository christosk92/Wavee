using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Wavee.Helpers;

public class BatchObservableCollection<T> : ObservableCollection<T>
{
    private bool _suppressNotification = false;

    /// <summary>
    /// Adds a range of items to the collection and raises a single Reset event.
    /// </summary>
    /// <param name="items">The items to add.</param>
    public void AddRange(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;
        foreach (var item in items)
        {
            Add(item);
        }
        _suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Removes a range of items from the collection and raises a single Reset event.
    /// </summary>
    /// <param name="items">The items to remove.</param>
    public void RemoveRange(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;
        foreach (var item in items)
        {
            Remove(item);
        }
        _suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Clears the collection and raises a single Reset event.
    /// </summary>
    public new void Clear()
    {
        _suppressNotification = true;
        base.Clear();
        _suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    /// <summary>
    /// Replaces the entire collection with the provided items and raises a single Reset event.
    /// </summary>
    /// <param name="items">The new items to set.</param>
    public void ReplaceRange(IEnumerable<T> items)
    {
        if (items == null) throw new ArgumentNullException(nameof(items));

        _suppressNotification = true;
        base.Clear();
        foreach (var item in items)
        {
            base.Add(item);
        }
        _suppressNotification = false;
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        if (!_suppressNotification)
            base.OnCollectionChanged(e);
    }
}