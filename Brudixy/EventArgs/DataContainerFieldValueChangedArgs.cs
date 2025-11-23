using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy.EventArgs
{
    public class DataContainerFieldValueChangedArgs : IDataContainerFieldValueChangedArgs
    {
        private Map<string, DataTable.ColumnChange> m_dict;

        public DataContainerFieldValueChangedArgs(IDataRowContainer rowContainer, 
            string columnName, 
            object prevValue,
            object value)
        {
            Row = rowContainer;

            m_dict = new Map<string, DataTable.ColumnChange>
            {
                { columnName, new DataTable.ColumnChange { NewValue = value, OldValue = prevValue } }
            };
        }

        internal DataContainerFieldValueChangedArgs(IDataRowContainer rowContainer, Map<string, DataTable.ColumnChange> dict)
        {
            m_dict = dict;
            Row = rowContainer;
        }

        public IDataRowContainer Row { get; }

        public IEnumerable<string> ChangedColumnNames
        {
            get
            {
                return m_dict.Keys;
            }
        }

        public bool IsColumnChanged(string columnName)
        {
            return GetColumnChange(columnName) != null;
        }

        public object GetNewValue(string columnName)
        {
            return GetColumnChange(columnName)?.NewValue;
        }

        public object GetOldValue(string columnName)
        {
            return GetColumnChange(columnName)?.OldValue;
        }

        [CanBeNull]
        private DataTable.ColumnChange GetColumnChange(string columnName)
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