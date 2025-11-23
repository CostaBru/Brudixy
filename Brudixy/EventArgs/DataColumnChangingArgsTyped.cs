using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class DataColumnChangingDataEventTyped<T> : DataEvent<IDataColumnChangingTypedEventArgs<T>>
    {
        public DataColumnChangingDataEventTyped(IDisposableCollection referenceHolder) : base(referenceHolder)
        {
        }
    }
    
    public class DataColumnChangingArgsTyped<T> : IDataColumnChangingTypedEventArgs<T>
    {
        public WeakReference<DataTable> Table;

        public string ColumnName;

        public int RowHandle;

        public int ColumnHandle;

        public T NewValue;

        public T PrevValue;

        public bool NewValueIsNull;
        
        public bool PrevValueIsNull;
        
        public bool IsCancel;

        public string ExceptionMessage = string.Empty;

        string IDataColumnChangingEventArgs.ColumnName => ColumnName;

        T IDataColumnChangingTypedEventArgs<T>.NewValue
        {
            get => NewValue;
            set => NewValue = value;
        }

        string IDataColumnChangingEventArgs.ErrorMessage
        {
            get => ExceptionMessage;
            set => ExceptionMessage = value ?? string.Empty;
        }

        object IDataColumnChangingEventArgs.NewValue
        {
            get
            {
                return NewValue;
            }
            set
            {
                if(value is T tv)
                {
                    NewValue = tv;
                }
                else
                {
                    var dataTable = GetTable();

                    var dataColumn = dataTable.GetColumn(ColumnHandle);

                    NewValue = CoreDataRow.TryConvertValue<T>(dataTable, dataColumn, value, "DataColumnChanging typed");
                }
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        object IDataColumnChangingEventArgs.OldValue => PrevValue;

        bool IDataColumnChangingTypedEventArgs<T>.NewValueIsNull
        {
            get => NewValueIsNull;
            set => NewValueIsNull = value;
        }

        bool IDataColumnChangingTypedEventArgs<T>.PrevValueIsNull => PrevValueIsNull;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTable IDataTableEventArgs.Table => GetTable();

        private DataTable GetTable()
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

        public T OldValue => PrevValue;

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

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        bool IDataColumnChangingEventArgs.IsCancel
        {
            get => IsCancel;
            set => IsCancel = value;
        }
    }
}