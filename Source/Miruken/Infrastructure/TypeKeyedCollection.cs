namespace Miruken.Infrastructure;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

public class TypeKeyedCollection<TItem> : KeyedCollection<Type, TItem>
{
    public TypeKeyedCollection()
        : base(null, 4)
    {
    }

    public TypeKeyedCollection(IEnumerable<TItem> items)
        : base(null, 4)
    {
        if (items == null)
            throw new ArgumentNullException(nameof(items));

        foreach (var item in items) Add(item);
    }

    public T Find<T>()
    {
        return this.OfType<T>().FirstOrDefault();
    }

    public IEnumerable<T> FindAll<T>()
    {
        return this.OfType<T>();
    }

    protected override Type GetKeyForItem(TItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        return item.GetType();
    }

    protected override void InsertItem(int index, TItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));

        if (Contains(item.GetType()))
            throw new ArgumentException($"{item.GetType()} already exists");

        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, TItem item)
    {
        if (item == null)
            throw new ArgumentNullException(nameof(item));
        base.SetItem(index, item);
    }
}