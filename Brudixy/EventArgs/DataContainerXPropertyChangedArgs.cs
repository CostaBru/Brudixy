using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class DataContainerXPropertyChangedArgs : IDataContainerXPropertyChangedArgs
    {
        private Map<string, DataTable.XPropertyChange> m_dict;

        public DataContainerXPropertyChangedArgs(IDataRowContainer rowContainer, string propertyCode, object prevValue, object value)
        {
            Row = rowContainer;

            m_dict = new Map<string, DataTable.XPropertyChange>
            {
                         { propertyCode, new DataTable.XPropertyChange { NewValue = value, OldValue = prevValue } }
                     };
        }

        internal DataContainerXPropertyChangedArgs(IDataRowContainer rowContainer, Map<string, DataTable.XPropertyChange> dict)
        {
            m_dict = dict;
            Row = rowContainer;
        }

        public IDataRowContainer Row { get; }

        public IEnumerable<string> ChangedPropertyCodes
        {
            get
            {
                return m_dict.Keys;
            }
        }

        public bool IsPropertyChanged(string columnName)
        {
            return GetPropertyChange(columnName) != null;
        }

        public T GetNewValue<T>(string columnName)
        {
            var newValue = GetPropertyChange(columnName)?.NewValue;
            
            return XPropertyValueConverter.TryConvert<T>("Row container xProp new value", columnName, newValue);
        }

        public T GetOldValue<T>(string columnName)
        {
            var oldValue = GetPropertyChange(columnName)?.OldValue;
            
            return XPropertyValueConverter.TryConvert<T>("Row container xProp old value", columnName,oldValue);
        }

        [CanBeNull]
        private DataTable.XPropertyChange GetPropertyChange(string columnName)
        {
            m_dict.TryGetValue(columnName, out var value);

            return value;
        }

        public void Dispose()
        {
            m_dict.Dispose();
        }
    }
}