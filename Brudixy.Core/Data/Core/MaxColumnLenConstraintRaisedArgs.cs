using System;
using System.Diagnostics;
using Brudixy.Converter;
using Brudixy.Delegates;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class MaxColumnLenConstraintDataEvent : DataEvent<IMaxColumnLenConstraintRaisedArgs>, IMaxColumnLenConstraintDataEvent
    {
        public MaxColumnLenConstraintDataEvent(IDisposableCollection referenceHolder) : base(referenceHolder)
        {
        }
    }

    public class MaxColumnLenConstraintRaisedArgs : System.EventArgs, IMaxColumnLenConstraintRaisedArgs
    {
        internal WeakReference<CoreDataTable> Table;

        public MaxColumnLenConstraintRaisedArgs(int columnHandle, string columnName, object value, CoreDataRow row, bool preValidating, WeakReference<CoreDataTable> table)
        {
            ColumnHandle = columnHandle;
            ColumnName = columnName;
            Row = row;
            PreValidating = preValidating;
            Value = value;
            Table = table;
        }

        public string ColumnName { get; }
        
        public int ColumnHandle { get;  }

        public CoreDataRow Row { get;  }

        public object Value { get; }
        
        public bool PreValidating { get;  }

        public T GetValue<T>()
        {
            if (Value is T tv)
            {
                return tv;
            }
            
            var table = GetDataTable();

            if (table != null)
            {
                if (Tool.IsString<T>())
                {
                    var type = Value.GetType();

                    var columnType = CoreDataTable.GetColumnType(type);
                    
                    return (T)(object)CoreDataTable.ConvertObjectToString(columnType.type, columnType.typeModifier, Value, type);
                }

                return Tool.ConvertBoxed<T>(Value);
            }

            return Tool.ConvertBoxed<T>(Value);
        }
        
        public bool RaiseError { get; set; } = true;

        ICoreDataTable IMaxColumnLenConstraintRaisedArgs.Table => GetDataTable();

        private CoreDataTable GetDataTable()
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

        ICoreDataRowReadOnlyAccessor IMaxColumnLenConstraintRaisedArgs.Row => Row;

        string IMaxColumnLenConstraintRaisedArgs.ColumnName => ColumnName;

        object IMaxColumnLenConstraintRaisedArgs.Value => Value;
    }
}