using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class DataRowDeletingDataEvent : DataEvent<IDataRowDeletingArgs>, IDataRowDeletingDataEvent
    {
        public DataRowDeletingDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
        {
        }
    }

    public class DataRowDeletingArgs : IDataRowDeletingArgs
    {
        internal WeakReference<DataTable> Table;

        public IEnumerable<int> RowHandles => m_rowHandles;

        public bool IsCancel;
        
        private readonly Data<int> m_rowHandles;

        public DataRowDeletingArgs(DataTable table, Data<int> rowHandles)
        {
            Table = new WeakReference<DataTable>(table);
            m_rowHandles = rowHandles;
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

        IEnumerable<IDataTableRow> IDataRowDeletingArgs.Rows
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

        bool IDataRowDeletingArgs.IsCancel
        {
            get
            {
                return IsCancel;
            }
            set
            {
                IsCancel = value;
            }
        }

        public void Dispose()
        {
            m_rowHandles.Dispose();
        }
    }
}