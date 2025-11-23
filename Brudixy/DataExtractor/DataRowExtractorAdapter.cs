using System.Collections;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    internal class DataRowExtractorAdapter<T> :
        IDataTableRowEnumerable<T>, 
        IDataTableRowEnumerableToConcrete<T>, 
        IDataTableRowEnumerableStringToConcrete<T> where T : IDataTableReadOnlyRow
    {
        private readonly WeakReference<IDataTable> m_table;
        private readonly string m_tableName;

        [NotNull] 
        protected internal IDataTableRowEnumerable<IDataTableRow> m_rowEnumerable;

        public DataRowExtractorAdapter(IDataTable table)
        {
            m_table = new WeakReference<IDataTable>(table);
            m_tableName = table.TableName;
            m_rowEnumerable = table.Rows;
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var row in m_rowEnumerable.OfType<T>())
            {
                yield return row;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataTableRowEnumerableToConcrete<T> Where(string column)
        {
            m_rowEnumerable.Where(column);

            return this;
        }

        public IDataTableRowEnumerableStringToConcrete<T> AsString()
        {
            return this;
        }

        protected ICoreDataTable Table
        {
            get
            {
                m_table.TryGetTarget(out var tbl);

                if (tbl == null)
                {
                    throw new DataDetachedException($"{this} detached from {m_tableName} data table.");
                }

                return tbl;
            }
        }

        public IDataTableRowEnumerable<T> Add(IDataRowReadOnlyAccessor newRow)
        {
            Table.ImportRow(newRow);

            return this;
        }

        public IDataTableRowEnumerable<T> AddRange(IEnumerable<IDataRowReadOnlyAccessor> newRows)
        {
            var table = Table;

            foreach (var newRow in newRows)
            {
                table.ImportRow(newRow);
            }
            
            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> StartsWith(string value, bool caseSensitive = true)
        {
            if (m_rowEnumerable is IDataTableRowEnumerableStringToConcrete<IDataTableRow> s1)
            {
                s1.StartsWith(value, caseSensitive);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> EndsWith(string value, bool caseSensitive = true)
        {
            if (m_rowEnumerable is IDataTableRowEnumerableStringToConcrete<IDataTableRow> s1)
            {
                s1.EndsWith(value, caseSensitive);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Contains(string value, bool caseSensitive = true)
        {
            if (m_rowEnumerable is IDataTableRowEnumerableStringToConcrete<IDataTableRow> s1)
            {
                s1.Contains(value, caseSensitive);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Equals(string value, bool caseSensitive = true)
        {
            if (m_rowEnumerable is IDataTableRowEnumerableStringToConcrete<IDataTableRow> s1)
            {
                s1.Equals(value, caseSensitive);
            }

            return this;
        }

        IDataTableRowEnumerableStringToConcrete<T> IDataTableRowEnumerableStringToConcrete<T>.Not()
        {
            if (m_rowEnumerable is IDataTableRowEnumerableStringToConcrete<IDataTableRow> s1)
            {
                s1.Not();
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Equals<V>(V? value) where V : struct, IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.Equals(value);
            }

            return this;
        }

        IDataTableRowEnumerableToConcrete<T> IDataTableRowEnumerableToConcrete<T>.Not()
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.Not();
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Equals<V>(V value) where V : IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.Equals(value);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Null()
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.Null();
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Greater<V>(V value) where V : IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.Greater(value);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> Lesser<V>(V value) where V : IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.Lesser(value);
            }
            
            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> GreaterOrEquals<V>(V value) where V : IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.GreaterOrEquals(value);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> LesserOrEquals<V>(V value) where V : IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.LesserOrEquals(value);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(V value1) where V : IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.In(value1);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2) where V : IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.In(value1, value2);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3) where V : IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.In(value1, value2, value3);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3, V value4) where V : IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.In(value1, value2, value3, value4);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(V value1, V value2, V value3, V value4, V value5) where V : IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.In(value1, value2, value3, value4, value5);
            }

            return this;
        }

        public IDataTableRowEnumerableReadOnly<T> In<V>(IReadOnlyCollection<V> values) where V : IComparable
        {
            if (m_rowEnumerable is IDataTableRowEnumerableToConcrete<IDataTableRow> s1)
            {
                s1.In(values);
            }

            return this;
        }
    }
}

