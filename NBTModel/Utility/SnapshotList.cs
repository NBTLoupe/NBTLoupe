using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NBTModel.Utility;

public class SnapshotList<T>(IList<T> snapshot) : Collection<T>(new ProxyList<T>())
{
    private IList<T>? _recycled;

    private new ProxyList<T> Items => (ProxyList<T>)base.Items;

    private void Modified()
    {
        if (!Equals(snapshot, Items.InnerList))
            return;

        // Snapshot is in use, copy backing array to recycled array or create new backing array
        if (_recycled != null)
        {
            for (var i = 0; i < Count; i++)
                _recycled.Add(Items[i]);
            Items.InnerList = _recycled;
            _recycled = null;
        }
        else
        {
            Resize(Items.Count);
        }
    }

    private void Resize(int newSize)
    {
        var oldList = Items.InnerList;
        var newList = new List<T>(newSize);
        for (int i = 0, n = oldList.Count; i < n; i++)
            newList.Add(oldList[i]);

        Items.InnerList = newList;
    }

    protected override void InsertItem(int index, T item)
    {
        Modified();
        base.InsertItem(index, item);
    }

    protected override void SetItem(int index, T item)
    {
        Modified();
        base.SetItem(index, item);
    }

    protected override void RemoveItem(int index)
    {
        Modified();
        base.RemoveItem(index);
    }

    protected override void ClearItems()
    {
        Modified();
        base.ClearItems();
    }

    private class ProxyList<TK>(IList<TK> list) : IList<TK>
    {
        public ProxyList() : this(new List<TK>())
        {
        }

        public IList<TK> InnerList { get; set; } = list;

        public int IndexOf(TK item)
        {
            return InnerList.IndexOf(item);
        }

        public void Insert(int index, TK item)
        {
            InnerList.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            InnerList.RemoveAt(index);
        }

        public TK this[int index]
        {
            get => InnerList[index];
            set => InnerList[index] = value;
        }

        public void Add(TK item)
        {
            InnerList.Add(item);
        }

        public void Clear()
        {
            InnerList.Clear();
        }

        public bool Contains(TK item)
        {
            return InnerList.Contains(item);
        }

        public void CopyTo(TK[] array, int arrayIndex)
        {
            InnerList.CopyTo(array, arrayIndex);
        }

        public int Count => InnerList.Count;

        public bool IsReadOnly => InnerList.IsReadOnly;

        public bool Remove(TK item)
        {
            return InnerList.Remove(item);
        }

        public IEnumerator<TK> GetEnumerator()
        {
            return InnerList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return InnerList.GetEnumerator();
        }
    }
}
