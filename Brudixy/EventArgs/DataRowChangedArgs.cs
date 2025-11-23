using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class DataRowChangedDataEvent : DataEvent<IDataRowChangedArgs>, IDataRowChangedDataEvent
    {
        public DataRowChangedDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
        {
        }
    }

    public class DataRowChangedArgs : IDataRowChangedArgs, IDisposable
    {
        internal WeakReference<DataTable> Table;
        
        private readonly Data<int> m_rowHandles;

        public int RowHandle
        {
            get
            {
                if (RowHandles != null)
                {
                    return RowHandles[0];
                }

                return -1;
            }
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

        IDataTableRow IDataRowChangedArgs.Row
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

        public IReadOnlyList<int> RowHandles => m_rowHandles;

        internal DataRowChangedArgs(DataTable table, Data<int> rowHandles)
        {
            Table = new WeakReference<DataTable>(table);
            m_rowHandles = rowHandles.ToData();
        }

        internal DataRowChangedArgs(DataTable table, int rowHandle)
        {
            Table = new WeakReference<DataTable>(table);
            m_rowHandles = new Data<int> { rowHandle };
        }

        public void Dispose()
        {
            m_rowHandles?.Dispose();
        }
    }
}