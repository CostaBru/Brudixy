using Brudixy.Interfaces;

namespace Brudixy.EventArgs
{
    public class DataColumnChangingArgs : IDataColumnChangingEventArgs
    {
        public WeakReference<DataTable> Table;

        public string ColumnName;

        public int RowHandle;

        public int ColumnHandle;

        public object NewValue;

        public object PrevValue;

        public bool IsCancel;

        public string ExceptionMessage = string.Empty;

        string IDataColumnChangingEventArgs.ColumnName => ColumnName;

        string IDataColumnChangingEventArgs.ErrorMessage
        {
            get => ExceptionMessage;
            set => ExceptionMessage = value ?? string.Empty;
        }

        object IDataColumnChangingEventArgs.NewValue
        {
            get => NewValue;
            set => NewValue = value;
        }

        object IDataColumnChangingEventArgs.OldValue => PrevValue;

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

        IDataTableRow IDataColumnChangingEventArgs.Row
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

        bool IDataColumnChangingEventArgs.IsCancel
        {
            get => IsCancel;
            set => IsCancel = true;
        }
    }
}