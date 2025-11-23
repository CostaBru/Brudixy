using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class DataRowAddedDataEvent : DataEvent<IDataTableRowAddedArgs>, IDataRowAddedDataEvent
    {
        public DataRowAddedDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
        {
        }
    }

    public class DataRowAddedArgs : IDataTableRowAddedArgs, IDisposable
    {
        internal WeakReference<DataTable> Table;
        private readonly Data<int> m_rowHandles;

        public int RowHandle
        {
            get
            {
                return RowHandles.FirstOrDefault();
            }
        }

        public IReadOnlyList<int> RowHandles => m_rowHandles;

        internal DataRowAddedArgs(DataTable table, Data<int> newRowAddedHandles)
        {
            Table = new WeakReference<DataTable>(table);
            m_rowHandles = newRowAddedHandles;
        }

        internal DataRowAddedArgs(DataTable table, int newRowAddedHandle)
        {
            Table = new WeakReference<DataTable>(table);
            m_rowHandles = new Data<int> { newRowAddedHandle };
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

        IDataTableRow IDataTableRowAddedArgs.Row
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
                    return table.GetRowByHandle(RowHandle);
                }

                return null;
            }
        }

        IEnumerable<IDataTableRow> IDataTableRowAddedArgs.Rows
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
                    if (m_rowHandles != null)
                    {
                        foreach (var rowHandle in m_rowHandles)
                        {
                            yield return table.GetRowByHandle(rowHandle);
                        }
                    }
                }
            }
        }

        public bool IsMultipleRow
        {
            get
            {
                return m_rowHandles?.Count > 1;
            }
        }

        public void Dispose()
        {
            m_rowHandles?.Dispose();
        }
    }
}