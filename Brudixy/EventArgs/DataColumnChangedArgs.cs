using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class DataColumnChangedArgs : IDataColumnChangedEventArgs, IDisposable
    {
        private readonly Map<int, Data<DataTable.ColumnChange>> m_dict;

        internal DataColumnChangedArgs(DataTable table, int rowHandle, Map<int, Data<DataTable.ColumnChange>> dict)
        {
            m_dict = dict;
            Table = new WeakReference<DataTable>(table);
            RowHandle = rowHandle;
        }

        public WeakReference<DataTable> Table;

        public int RowHandle { get; }

        public DataColumnChangedArgs(DataTable table, int rowHandle, int columnHandle, int? tranId, object prevValue, object value)
        {
            m_dict = new Map<int, Data<DataTable.ColumnChange>>
            {
                         { columnHandle, new () { new DataTable.ColumnChange { NewValue = value, OldValue = prevValue, TranId = tranId }  } }
                     };

            Table = new WeakReference<DataTable>(table);
            RowHandle = rowHandle;
        }

        public IEnumerable<string> ChangedColumnNames
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
                    foreach (var columnHandle in m_dict.Keys)
                    {
                        if (columnHandle < table.ColumnCount)
                        {
                            yield return table.DataColumnInfo.Columns[columnHandle].ColumnName;
                        }
                    }
                }
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

        public bool IsColumnChanged(string columnName)
        {
            return GetColumnChange(columnName) != null;
        }

        [CanBeNull]
        public object GetNewValue(string columnName)
        {
            return GetColumnChange(columnName)?.LastOrDefault()?.NewValue;
        }

        [CanBeNull]
        public object GetOldValue(string columnName)
        {
            return GetColumnChange(columnName)?.FirstOrDefault()?.OldValue;
        }

        [CanBeNull]
        private Data<DataTable.ColumnChange> GetColumnChange(string columnName)
        {
            var reference = Table;

            if (reference == null)
            {
                return null;
            }

            if (reference.TryGetTarget(out var table))
            {
                if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
                {
                    m_dict.TryGetValue(column.ColumnHandle, out var value);

                    return value;
                }
            }

            return null;
        }

        IDataTableRow IDataColumnChangedEventArgs.Row
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

        public void Dispose()
        {
            m_dict?.Dispose();
        }
    }
}