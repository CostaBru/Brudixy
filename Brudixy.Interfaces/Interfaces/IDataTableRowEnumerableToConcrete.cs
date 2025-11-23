using System;
using System.Collections.Generic;

namespace Brudixy.Interfaces
{
    public interface IDataTableRowEnumerableToConcrete<out T> : 
        IDataTableRowEnumerableReadOnly<T> where T : IDataTableReadOnlyRow
    {
        IDataTableRowEnumerableStringToConcrete<T> AsString();
        
        IDataTableRowEnumerableToConcrete<T> Not();

        IDataTableRowEnumerableReadOnly<T> Equals<V>(V? value) where V : struct, IComparable;

        IDataTableRowEnumerableReadOnly<T> Equals<V>(V value) where V :  IComparable;

        IDataTableRowEnumerableReadOnly<T> Null();

        IDataTableRowEnumerableReadOnly<T> Greater<V>(V value) where V : IComparable;

        IDataTableRowEnumerableReadOnly<T> Lesser<V>(V value) where V : IComparable;

        IDataTableRowEnumerableReadOnly<T> GreaterOrEquals<V>(V value) where V : IComparable;

        IDataTableRowEnumerableReadOnly<T> LesserOrEquals<V>(V value) where V : IComparable;

        IDataTableRowEnumerableReadOnly<T> In<V>(V value1) where V : IComparable;

        IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2) where V : IComparable;

        IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3) where V : IComparable;

        IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3, V value4) where V : IComparable;

        IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3, V value4, V value5) where V : IComparable;

        IDataTableRowEnumerableReadOnly<T> In<V>(IReadOnlyCollection<V> values) where V : IComparable;
    }
}