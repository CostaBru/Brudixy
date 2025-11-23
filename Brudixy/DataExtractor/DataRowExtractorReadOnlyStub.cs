using System.Collections;
using Brudixy.Interfaces;

namespace Brudixy;

internal class DataRowExtractorReadOnlyStub<T> : IDataTableRowEnumerableReadOnly<T>, 
    IDataTableRowEnumerableToConcrete<T>,
    IDataTableRowEnumerableStringToConcrete<T> where T : IDataTableReadOnlyRow
{
    public IEnumerator<T> GetEnumerator()
    {
        yield break;
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IDataTableRowEnumerableToConcrete<T> Where(string column) => this;

    public IDataTableRowEnumerableStringToConcrete<T> AsString() => this;

    public IDataTableRowEnumerableReadOnly<T> StartsWith(string value, bool caseSensitive = true) => this;

    public IDataTableRowEnumerableReadOnly<T> EndsWith(string value, bool caseSensitive = true) => this;

    public IDataTableRowEnumerableReadOnly<T> Contains(string value, bool caseSensitive = true) => this;

    public IDataTableRowEnumerableReadOnly<T> Equals(string value, bool caseSensitive = true) => this;

    IDataTableRowEnumerableStringToConcrete<T> IDataTableRowEnumerableStringToConcrete<T>.Not() => this;

    public IDataTableRowEnumerableReadOnly<T> Equals<V>(V? value) where V : struct, IComparable => this;

    IDataTableRowEnumerableToConcrete<T> IDataTableRowEnumerableToConcrete<T>.Not() => this;

    public IDataTableRowEnumerableReadOnly<T> Equals<V>(V value) where V : IComparable => this;

    public IDataTableRowEnumerableReadOnly<T> Null() => this;

    public IDataTableRowEnumerableReadOnly<T> Greater<V>(V value) where V : IComparable => this;

    public IDataTableRowEnumerableReadOnly<T> Lesser<V>(V value) where V : IComparable => this;

    public IDataTableRowEnumerableReadOnly<T> GreaterOrEquals<V>(V value) where V : IComparable => this;

    public IDataTableRowEnumerableReadOnly<T> LesserOrEquals<V>(V value) where V : IComparable => this;

    public IDataTableRowEnumerableReadOnly<T> In<V>(V value1) where V : IComparable => this;

    public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2) where V : IComparable => this;

    public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3) where V : IComparable => this;

    public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3, V value4) where V : IComparable => this;

    public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3, V value4, V value5) where V : IComparable => this;

    public IDataTableRowEnumerableReadOnly<T> In<V>(IReadOnlyCollection<V> values) where V : IComparable => this;
}