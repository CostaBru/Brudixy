using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class NewRowCellValueRequestingDataEvent : DataEvent<INewRowCellValueRequestingArgs>, INewRowCellValueRequestingDataEvent
    {
        public NewRowCellValueRequestingDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
        {
        }
    }

    public class EmptyRowCellValueRequestingArgs : INewRowCellValueRequestingArgs
    {
        internal WeakReference<DataTable> Table; 

        public string ColumnName;

        public object Value;

        string INewRowCellValueRequestingArgs.ColumnName => ColumnName;

        object INewRowCellValueRequestingArgs.Value
        {
            get => Value;
            set => Value = value;
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
    }
}
