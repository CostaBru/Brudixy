using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class DataRowAddingDataEvent : DataEvent<IDataRowAddingArgs>, IDataRowAddingDataEvent
    {
        public DataRowAddingDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
        {
        }
    }

    public class DataRowAddingArgs : IDataRowAddingArgs
    {
        internal WeakReference<DataTable> Table;

        public int RowHandle;

        public bool IsCancel;

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

        bool IDataRowAddingArgs.IsCancel
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
    }
}