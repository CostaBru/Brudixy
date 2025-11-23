using System;
using System.Collections.Generic;
using System.Linq;
using Konsarpoo.Collections;

namespace Brudixy
{
    internal class DataKey
    {
        private readonly WeakReference<CoreDataTable> m_dataTableReference;

        internal IReadOnlyList<int> Columns;

        public int Count => Columns.Count;

        internal IEnumerable<CoreDataColumn> ColumnsReference
        {
            get
            {
                var table = Table;

                foreach (var column in Columns)
                {
                    yield return table.GetColumn(column);
                }
            }
        }

        internal CoreDataTable Table
        {
            get
            {
                if (m_dataTableReference == null)
                {
                    return null;
                }

                m_dataTableReference.TryGetTarget(out var tbl);
                
                return tbl;
            }
        }
        
        internal DataKey(CoreDataTable dataTable, IReadOnlyList<int> columns)
        {
            if (dataTable == null)
            {
                throw new ArgumentNullException(nameof(dataTable));
            }

            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            if (columns.Count == 0)
            {
                throw new ArgumentException($"No columns provided for '{dataTable.Name}' table key.");
            }

            m_dataTableReference = new WeakReference<CoreDataTable>(dataTable);

            for (int index = 0; index < columns.Count; ++index)
            {
                if (columns[index] < 0)
                {
                    throw new ArgumentNullException(
                        $"Null column reference provided at '{index}' index for '{dataTable.Name}' table key.");
                }
            }

            for (int index1 = 0; index1 < columns.Count; ++index1)
            {
                for (int index2 = 0; index2 < index1; ++index2)
                {
                    if (columns[index1] == columns[index2])
                    {
                        throw new ArgumentNullException(
                            $"Same column reference provided at '{index1}','{index2}' indexes for '{dataTable.Name}' table key.");
                    }
                }
            }

            Columns = columns;
        }
      

        internal bool ColumnsEqual(DataKey key)
        {
            return ReferenceEquals(this.Table, key.Table) && ColumnsEqual(Columns, key.Columns);
        }

        internal static bool ColumnsEqual(IReadOnlyList<int> column1, IReadOnlyList<int> column2)
        {
            if (ReferenceEquals(column1, column2))
            {
                return true;
            }

            if (column1 == null || column2 == null || column1.Count != column2.Count)
            {
                return false;
            }

            for (int index1 = 0; index1 < column1.Count; ++index1)
            {
                bool flag = false;
                for (int index2 = 0; index2 < column2.Count; ++index2)
                {
                    if (column1[index1].Equals(column2[index2]))
                    {
                        flag = true;
                        break;
                    }
                }
                if (!flag)
                {
                    return false;
                }
            }
            return true;
        }
        
        internal bool ContainsColumn(int columnHandle)
        {
            for (int index = 0; index < Columns.Count; ++index)
            {
                if (columnHandle == Columns[index])
                {
                    return true;
                }
            }
            return false;
        }

        internal IEnumerable<string> GetColumnNames()
        {
            var table = Table;

            foreach (var col in Columns)
            {
                yield return table.DataColumnInfo.Columns[col].ColumnName;
            }
        }

        public void RemapColumnHandles(Map<int,int> oldToNewMap)
        {
            var newColumns = Columns.ToArray();

            foreach (var kv in oldToNewMap)
            {
                var oldColumnHandle = kv.Key;
                var newColumnHandle = kv.Value;

                if (oldColumnHandle < newColumns.Length && newColumnHandle < newColumns.Length)
                {
                    newColumns[newColumnHandle] = Columns[oldColumnHandle];
                }
            }

            Columns = newColumns;
        }
    }
}