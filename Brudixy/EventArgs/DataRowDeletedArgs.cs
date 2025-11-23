using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class DataRowDeletedDataEvent : DataEvent<IDataRowDeletedArgs>, IDataRowDeletedDataEvent
    {
        public DataRowDeletedDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
        {
        }
    }

    public class DataRowDeletedArgs : IDataRowDeletedArgs, IDisposable
    {
        private readonly Data<int> m_rowHandles;
        
        internal WeakReference<DataTable> Table { get; }

        public IReadOnlyList<int> RowHandles => m_rowHandles;

        internal DataRowDeletedArgs(DataTable table, Data<int> rowDeletedHandles)
        {
            Table = new WeakReference<DataTable>(table);
            m_rowHandles = rowDeletedHandles;
        }

        IDataTable IDataTableEventArgs.Table
        {
            get
            {
                var reference = Table;

                if (reference == null)
                {
                    return null;
                }

                if (reference.TryGetTarget(out var table))
                {
                    return table;
                }

                return null;
            }
        }

        IEnumerable<IDataTableRow> IDataRowDeletedArgs.DeletedRows
        {
            get
            {
                var reference = Table;

                if (reference == null)
                {
                    yield break;
                }

                if (reference.TryGetTarget(out var table))
                {
                    foreach (var rowHandle in RowHandles)
                    {
                        yield return table.GetRowByHandle(rowHandle);
                    }
                }
            }
        }

        public void Dispose()
        {
            m_rowHandles?.Dispose();
        }
    }
}