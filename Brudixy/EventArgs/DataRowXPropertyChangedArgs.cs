using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class DataRowXPropertyChangedArgs : IDataRowXPropertyChangedEventArgs
    {
        private readonly Map<string, Data<DataTable.XPropertyChange>> m_dict;

        internal DataRowXPropertyChangedArgs(DataTable table, int rowHandle, Map<string, Data<DataTable.XPropertyChange>> dict)
        {
            m_dict = dict;
            Table = new WeakReference<DataTable>(table);
            RowHandle = rowHandle;
        }

        internal WeakReference<DataTable> Table;

        public int RowHandle { get;  }

        public DataRowXPropertyChangedArgs(DataTable table, int rowHandle, string propertyCode, object prevValue, object value, int? trandId)
        {
            m_dict = new Map<string, Data<DataTable.XPropertyChange>>
            {
                {
                    propertyCode,
                    new Data<DataTable.XPropertyChange>() { new () { NewValue = value, OldValue = prevValue, TranId = trandId } }
                }
            };

            Table = new WeakReference<DataTable>(table);
            RowHandle = rowHandle;
        }

        public IEnumerable<string> ChangedPropertyCodes => m_dict.Keys;

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

        public bool IsPropertyChanged(string propertyCode)
        {
            return GetXPropChange(propertyCode) != null;
        }

        public T GetNewValue<T>(string propertyCode)
        {
            var newValue = GetXPropChange(propertyCode)?.LastOrDefault()?.NewValue;
            
            return XPropertyValueConverter.TryConvert<T>("DataRow xProp new value", propertyCode, newValue);
        }

        public T GetOldValue<T>(string propertyCode)
        {
            var oldValue = GetXPropChange(propertyCode)?.FirstOrDefault()?.OldValue;
            
            return XPropertyValueConverter.TryConvert<T>("DataRow xProp old value", propertyCode, oldValue);
        }

        [CanBeNull]
        private Data<DataTable.XPropertyChange> GetXPropChange(string propertyCode)
        {
            m_dict.TryGetValue(propertyCode, out var value);

            return value;
        }

        IDataTableRow IDataRowXPropertyChangedEventArgs.Row
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
            m_dict.Dispose();
        }
    }
}