using System.Collections;
using Brudixy.Exceptions;
using Brudixy.Interfaces;

namespace Brudixy
{
    internal class DataRowExtractorStub<T> : DataRowExtractorReadOnlyStub<T>, IDataTableRowEnumerable<T>, IDataTableRowEnumerableToConcrete<T>,
        IDataTableRowEnumerableStringToConcrete<T> where T : IDataTableReadOnlyRow
    {
        private readonly WeakReference<ICoreDataTable> m_table;
        private readonly string m_tableName;

        public DataRowExtractorStub(ICoreDataTable table)
        {
            m_tableName = table.TableName;
            m_table = new WeakReference<ICoreDataTable>(table);
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

        public IEnumerator<T> GetEnumerator()
        {
            yield break;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public IDataTableRowEnumerableToConcrete<T> Where(string column) => this;

        public IDataTableRowEnumerableStringToConcrete<T> WhereString(string column) => this;
    }
}