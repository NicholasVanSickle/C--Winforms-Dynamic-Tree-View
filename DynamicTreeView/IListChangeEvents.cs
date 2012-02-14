using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DynamicTreeView
{
    public interface IListChangeEvents<T>
    {
        event Action<T> ChildAdded;
        event Action<T> ChildRemoved;
        event Action<int, T> ChildChanged;
        event Action<int, T> ChildInserted;
    }
}
