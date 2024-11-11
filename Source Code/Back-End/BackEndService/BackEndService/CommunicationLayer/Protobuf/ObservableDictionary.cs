using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

public class ObservableDictionary<TKey, TValue> : ObservableCollection<KeyValuePair<TKey, TValue>>, INotifyCollectionChanged, INotifyPropertyChanged where TKey : notnull
{
    private readonly Dictionary<TKey, TValue> _dictionary = new();

    // Event to track value changes specifically
    public event EventHandler<KeyValueChangedEventArgs<TKey, TValue>>? ValueChanged;

    public TValue this[TKey key]
    {
        get => _dictionary[key];
        set
        {
            if (_dictionary.ContainsKey(key))
            {
                var oldValue = _dictionary[key];
                if (!EqualityComparer<TValue>.Default.Equals(oldValue, value))
                {
                    _dictionary[key] = value;
                    OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, new KeyValuePair<TKey, TValue>(key, value)));
                    ValueChanged?.Invoke(this, new KeyValueChangedEventArgs<TKey, TValue>(key, oldValue, value));
                }
            }
            else
            {
                Add(key, value);
            }
        }
    }

    public void Add(TKey key, TValue value)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));
        if (value == null) throw new ArgumentNullException(nameof(value));

        if (_dictionary.ContainsKey(key))
            throw new ArgumentException("Key already exists in the dictionary.");

        _dictionary.Add(key, value);
        var kvp = new KeyValuePair<TKey, TValue>(key, value);
        base.Add(kvp);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, kvp));
    }

    public bool Remove(TKey key)
    {
        if (key == null) throw new ArgumentNullException(nameof(key));

        if (_dictionary.TryGetValue(key, out var value) && _dictionary.Remove(key))
        {
            var kvp = new KeyValuePair<TKey, TValue>(key, value);
            int index = IndexOf(kvp);
            if (index >= 0)
            {
                base.RemoveAt(index);
            }
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, kvp));
            return true;
        }

        return false;
    }

    public bool ContainsKey(TKey key) => key != null && _dictionary.ContainsKey(key);

    public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);

    public new void Clear()
    {
        _dictionary.Clear();
        base.Clear();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }

    protected override void InsertItem(int index, KeyValuePair<TKey, TValue> item)
    {
        if (item.Key == null) throw new ArgumentNullException(nameof(item.Key));
        if (item.Value == null) throw new ArgumentNullException(nameof(item.Value));

        _dictionary.Add(item.Key, item.Value);
        base.InsertItem(index, item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, item, index));
    }

    protected override void RemoveItem(int index)
    {
        var item = this[index];
        _dictionary.Remove(item.Key);
        base.RemoveItem(index);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, item, index));
    }

    protected override void SetItem(int index, KeyValuePair<TKey, TValue> item)
    {
        if (item.Key == null) throw new ArgumentNullException(nameof(item.Key));
        if (item.Value == null) throw new ArgumentNullException(nameof(item.Value));

        var oldItem = this[index];
        _dictionary[oldItem.Key] = item.Value;
        base.SetItem(index, item);
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, oldItem, index));
    }

    protected override void ClearItems()
    {
        _dictionary.Clear();
        base.ClearItems();
        OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}

// Custom EventArgs to hold the old and new values for value changes
public class KeyValueChangedEventArgs<TKey, TValue> : EventArgs
{
    public TKey Key { get; }
    public TValue OldValue { get; }
    public TValue NewValue { get; }

    public KeyValueChangedEventArgs(TKey key, TValue oldValue, TValue newValue)
    {
        Key = key;
        OldValue = oldValue;
        NewValue = newValue;
    }
}
