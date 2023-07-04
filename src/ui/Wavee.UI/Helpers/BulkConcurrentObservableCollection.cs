// Copyright (c) 2023 Files Community
// Licensed under the MIT License. See the LICENSE.

using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using Wavee.UI.ViewModel.Shell.Sidebar;

namespace Wavee.UI.Helpers
{
    [DebuggerTypeProxy(typeof(CollectionDebugView<>))]
    [DebuggerDisplay("Count = {Count}")]
    public class BulkConcurrentObservableCollection<T> : INotifyCollectionChanged, INotifyPropertyChanged, ICollection<T>, IList<T>, ICollection, IList
    {
        protected bool IsBulkOperationStarted;
        private readonly object _syncRoot = new object();
        private readonly List<T> _collection = new List<T>();

        // When 'GroupOption' is set to 'None' or when a folder is opened, 'GroupedCollection' is assigned 'null' by 'ItemGroupKeySelector'
        public BulkConcurrentObservableCollection<GroupedCollection<T>>? GroupedCollection { get; private set; }
        public bool IsSorted { get; set; }

        public int Count
        {
            get
            {
                lock (_syncRoot)
                {
                    return _collection.Count;
                }
            }
        }

        public bool IsReadOnly => false;

        public bool IsFixedSize => false;

        public bool IsSynchronized => true;

        public object SyncRoot => _syncRoot;

        public bool IsGrouped => ItemGroupKeySelector is not null;

        object? IList.this[int index]
        {
            get
            {
                return this[index];
            }
            set
            {
                if (value is not null)
                    this[index] = (T)value;
            }
        }

        public T this[int index]
        {
            get
            {
                lock (_syncRoot)
                {
                    return _collection[index];
                }
            }
            set
            {
                T item;
                lock (_syncRoot)
                {
                    item = _collection[index];
                    _collection[index] = value;
                }

                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, value, item, index), false);
            }
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public event PropertyChangedEventHandler? PropertyChanged;

        private Func<T, string>? itemGroupKeySelector;

        public Func<T, string>? ItemGroupKeySelector
        {
            get => itemGroupKeySelector;
            set
            {
                itemGroupKeySelector = value;
                if (value is not null)
                    GroupedCollection ??= new BulkConcurrentObservableCollection<GroupedCollection<T>>();
                else
                    GroupedCollection = null;
            }
        }

        private Func<T, object>? itemSortKeySelector;

        public Func<T, object>? ItemSortKeySelector
        {
            get => itemSortKeySelector;
            set => itemSortKeySelector = value;
        }

        public Action<GroupedCollection<T>>? GetGroupHeaderInfo { get; set; }
        public Action<GroupedCollection<T>>? GetExtendedGroupHeaderInfo { get; set; }

        public BulkConcurrentObservableCollection()
        {
        }

        public BulkConcurrentObservableCollection(IEnumerable<T> items)
        {
            AddRange(items);
        }

        public virtual void BeginBulkOperation()
        {
            IsBulkOperationStarted = true;
            GroupedCollection?.ForEach(gp => gp.BeginBulkOperation());
            GroupedCollection?.BeginBulkOperation();
        }

        protected void OnCollectionChanged(NotifyCollectionChangedEventArgs e, bool countChanged = true)
        {
            if (!IsBulkOperationStarted)
            {
                if (countChanged)
                    PropertyChanged?.Invoke(this, EventArgsCache.CountPropertyChanged);

                PropertyChanged?.Invoke(this, EventArgsCache.IndexerPropertyChanged);
                CollectionChanged?.Invoke(this, e);
            }

            if (IsGrouped)
            {
                if (e.NewItems is not null)
                    AddItemsToGroup(e.NewItems.Cast<T>());

                if (e.OldItems is not null)
                    RemoveItemsFromGroup(e.OldItems.Cast<T>());
            }
        }

        public void ResetGroups(CancellationToken token = default)
        {
            if (!IsGrouped)
                return;

            // Prevents any unwanted errors caused by bindings updating
            GroupedCollection?.ForEach(x => x.Model?.PausePropertyChangedNotifications());
            GroupedCollection?.Clear();
            AddItemsToGroup(_collection, token);
        }

        private void AddItemsToGroup(IEnumerable<T> items, CancellationToken token = default)
        {
            if (GroupedCollection is null)
                return;

            foreach (var item in items)
            {
                if (token.IsCancellationRequested)
                    return;

                var key = GetGroupKeyForItem(item);
                if (key is null)
                    continue;

                var groups = GroupedCollection?.Where(x => x.Model?.Key == key);
                if (item is IGroupableItem groupable)
                    groupable.Key = key;

                if (groups is not null &&
                    groups.Any())
                {
                    var gp = groups.First();
                    gp.Add(item);
                    gp.IsSorted = false;
                }
                else
                {
                    var group = new GroupedCollection<T>(key)
                    {
                        item
                    };

                    group.GetExtendedGroupHeaderInfo = GetExtendedGroupHeaderInfo;
                    if (GetGroupHeaderInfo is not null)
                        GetGroupHeaderInfo.Invoke(group);

                    GroupedCollection?.Add(group);
                    GroupedCollection!.IsSorted = false;
                }
            }
        }

        private void RemoveItemsFromGroup(IEnumerable<T> items)
        {
            foreach (var item in items)
            {
                var key = GetGroupKeyForItem(item);

                var group = GroupedCollection?.Where(x => x.Model?.Key == key).FirstOrDefault();
                if (group is not null)
                {
                    group.Remove(item);
                    if (group.Count == 0)
                        GroupedCollection?.Remove(group);
                }
            }
        }

        private string? GetGroupKeyForItem(T item)
        {
            return ItemGroupKeySelector?.Invoke(item);
        }

        public virtual void EndBulkOperation()
        {
            if (!IsBulkOperationStarted)
                return;

            IsBulkOperationStarted = false;
            GroupedCollection?.ForEach(gp => gp.EndBulkOperation());
            GroupedCollection?.EndBulkOperation();

            OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
            PropertyChanged?.Invoke(this, EventArgsCache.CountPropertyChanged);
            PropertyChanged?.Invoke(this, EventArgsCache.IndexerPropertyChanged);
        }

        public void Add(T? item)
        {
            if (item is null)
                return;

            lock (_syncRoot)
            {
                _collection.Add(item);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, _collection.Count - 1));
        }

        public void Clear()
        {
            lock (_syncRoot)
            {
                _collection.Clear();
            }
            GroupedCollection?.Clear();

            OnCollectionChanged(EventArgsCache.ResetCollectionChanged);
        }

        public bool Contains(T? item)
        {
            if (item is null)
                return false;

            lock (_syncRoot)
            {
                return _collection.Contains(item);
            }
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            lock (_syncRoot)
            {
                _collection.CopyTo(array, arrayIndex);
            }
        }

        public bool Remove(T? item)
        {
            if (item is null)
                return false;

            int index;

            lock (_syncRoot)
            {
                index = _collection.IndexOf(item);

                if (index == -1)
                    return false;

                _collection.RemoveAt(index);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
            return true;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return new BlockingListEnumerator<T>(_collection, _syncRoot);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public int IndexOf(T? item)
        {
            if (item is null)
                return -1;

            lock (_syncRoot)
            {
                return _collection.IndexOf(item);
            }
        }

        public void Insert(int index, T? item)
        {
            if (item is null)
                return;

            lock (_syncRoot)
            {
                _collection.Insert(index, item);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
        }

        public void RemoveAt(int index)
        {
            T item;

            lock (_syncRoot)
            {
                item = _collection[index];
                _collection.RemoveAt(index);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (!items.Any())
                return;

            lock (_syncRoot)
            {
                _collection.AddRange(items);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList(), _collection.Count - items.Count()));
        }

        public void InsertRange(int index, IEnumerable<T> items)
        {
            if (!items.Any())
                return;

            lock (_syncRoot)
            {
                _collection.InsertRange(index, items);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items.ToList(), index));
        }

        public void RemoveRange(int index, int count)
        {
            if (count <= 0)
                return;

            List<T> items;

            lock (_syncRoot)
            {
                items = _collection.Skip(index).Take(count).ToList();
                _collection.RemoveRange(index, count);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items, index));
        }

        public void ReplaceRange(int index, IEnumerable<T> items)
        {
            var count = items.Count();

            if (count == 0)
                return;

            List<T> oldItems;
            List<T> newItems;

            lock (_syncRoot)
            {
                oldItems = _collection.Skip(index).Take(count).ToList();
                newItems = items.ToList();
                _collection.InsertRange(index, newItems);
                _collection.RemoveRange(index + count, count);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, newItems, oldItems, index), false);
        }

        public void Sort()
        {
            lock (_syncRoot)
            {
                _collection.Sort();
            }
        }

        public void Sort(Comparison<T> comparison)
        {
            lock (_syncRoot)
            {
                _collection.Sort(comparison);
            }
        }

        public void Order(Func<List<T>, IEnumerable<T>> func)
        {
            IEnumerable<T> result;
            lock (_syncRoot)
            {
                result = func.Invoke(_collection);
            }

            ReplaceRange(0, result);
        }

        public void OrderOne(Func<List<T>, IEnumerable<T>> func, T item)
        {
            IList<T> result;
            lock (_syncRoot)
            {
                result = func.Invoke(_collection).ToList();
            }

            Remove(item);
            var index = result.IndexOf(item);
            if (index != -1)
                Insert(index, item);
        }

        int IList.Add(object? value)
        {
            if (value is null)
                return -1;

            int index;

            lock (_syncRoot)
            {
                index = ((IList)_collection).Add((T)value);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, value, _collection.Count - 1));
            return index;
        }

        bool IList.Contains(object? value) => Contains((T?)value);

        int IList.IndexOf(object? value) => IndexOf((T?)value);

        void IList.Insert(int index, object? value) => Insert(index, (T?)value);

        void IList.Remove(object? value) => Remove((T?)value);

        void ICollection.CopyTo(Array array, int index) => CopyTo((T[])array, index);

        private static class EventArgsCache
        {
            internal static readonly PropertyChangedEventArgs CountPropertyChanged = new PropertyChangedEventArgs("Count");
            internal static readonly PropertyChangedEventArgs IndexerPropertyChanged = new PropertyChangedEventArgs("Item[]");
            internal static readonly NotifyCollectionChangedEventArgs ResetCollectionChanged = new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset);
        }
    }
}
