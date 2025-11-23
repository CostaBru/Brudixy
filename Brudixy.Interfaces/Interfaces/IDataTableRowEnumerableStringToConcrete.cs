namespace Brudixy.Interfaces
{
    public interface IDataTableRowEnumerableStringToConcrete<out T> : IDataTableRowEnumerableReadOnly<T> where T : IDataTableReadOnlyRow
    {
        IDataTableRowEnumerableReadOnly<T> StartsWith(string value, bool caseSensitive = true);

        IDataTableRowEnumerableReadOnly<T> EndsWith(string value, bool caseSensitive = true);

        IDataTableRowEnumerableReadOnly<T> Contains(string value, bool caseSensitive = true);

        IDataTableRowEnumerableReadOnly<T> Equals(string value, bool caseSensitive = true);

        IDataTableRowEnumerableStringToConcrete<T> Not();
        
        IDataTableRowEnumerableReadOnly<T> Null();
    }
}