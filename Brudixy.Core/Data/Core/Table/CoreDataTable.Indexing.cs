using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        public bool RemoveIndex(string column)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove index for '{column}' column from the '{Name}' table because it is readonly.");
            }

            if (DataColumnInfo.ColumnMappings.TryGetValue(column, out var dataColumn))
            {
                return RemoveIndexCore(dataColumn.ColumnHandle);
            }

            return false;
        }

        public bool RemoveIndex(int columnHandle)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove index for '{columnHandle}' column from the '{Name}' table because it is readonly.");
            }

            return RemoveIndexCore(columnHandle);
        }

        private bool RemoveIndexCore(int columnHandle)
        {
            var removeIndex = IndexInfo.Remove(columnHandle) & ColumnIsPresentInMultiIndex(columnHandle) == false;

            if (removeIndex)
            {
                DataColumnInfo.SetHasIndex(columnHandle, false);
            }

            return removeIndex;
        }

        public void RebuildIndex([NotNull] string column)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            var dataColumn = GetColumn(column);

            IndexInfo.RebuildIndex(this, dataColumn.ColumnHandle);
        }
        
        private void ClearIndexValues()
        {
            foreach (var indexInfoIndex in IndexInfo.Indexes)
            {
                indexInfoIndex.ClearValues();
            }

            foreach (var indexInfoIndex in MultiColumnIndexInfo.Indexes)
            {
                indexInfoIndex.ClearValues();
            }
        }
        
        public bool HasIndex(string column)
        {
            var dataColumn = GetColumn(column);
            
            return HasIndex(dataColumn.ColumnHandle);
        }

        public bool HasIndex(int columnHandle)
        {
            return IndexInfo.ColumnHasIndex(columnHandle) || MultiColumnIndexInfo.ContainsColumn(this, columnHandle);
        }

        public void AddIndex(string column, bool unique = false)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(column, out var dataColumn))
            {
                AddIndex(unique, dataColumn);
            }
        }

        public void AddIndex(int columnHandle, bool unique = false)
        {
            var coreDataColumn = GetColumn(columnHandle);

            AddIndex(coreDataColumn, unique);
        }

        public void AddIndex([NotNull] CoreDataColumn column, bool unique = false)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }
            
            if (ReferenceEquals(column.DataTable, this))
            {
                AddIndex(unique, column);
            }
            else
            {
                AddIndex(column.ColumnName, unique);
            }
        }

        private void AddIndex(bool unique, CoreDataColumn column)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new index for '{column.ColumnName}' column of the '{Name}' table because it is readonly.");
            }

            var newColumnHandle = column.ColumnHandle;
            
            if (IsReadOnlyColumn(column))
            {
                throw new ReadOnlyAccessViolationException($"Cannot add index to readonly '{column.ColumnName}' column.");
            }

            if (IndexInfo.ColumnHasIndex(newColumnHandle))
            {
                if (unique)
                {
                    DataColumnInfo.SetUniqueColumn(newColumnHandle, true);
                }
            }
            else
            {
                DataColumnInfo.SetIndexedColumn(newColumnHandle, true);

                if (unique)
                {
                    DataColumnInfo.SetUniqueColumn(newColumnHandle, true);
                }

                int addedIndexHandle = 0;

                IndexInfo.AddNew(this, column, ref addedIndexHandle, unique);

                IndexInfo.ConstructIndex(this, newColumnHandle);
            }

            OnMetadataChanged();
        }

        public void AddMultiColumnIndex(IEnumerable<string> columns, bool unique = false)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new multi column index for '{string.Join(",", columns)}' columns of the '{Name}' table because it is readonly.");
            }
            
            var list = columns.ToArray();

            var columnsHandles = new int[list.Length];

            for (var i = 0; i < list.Length; i++)
            {
                var column = list[i];
                
                if (DataColumnInfo.ColumnMappings.TryGetValue(column, out var dataColumn))
                {
                    columnsHandles[i] = dataColumn.ColumnHandle;
                    
                    if (IsReadOnlyColumn(dataColumn))
                    {
                        throw new ReadOnlyAccessViolationException($"Cannot add multiple index to readonly '{dataColumn.ColumnName}' column.");
                    }
                }
            }

            var index = MultiColumnIndexInfo.GetColumnFirstIndex(columnsHandles);

            if (index < 0)
            {
                AddNewMultiColumnIndex(columnsHandles, unique);
            }
            else
            {
                if (unique)
                {
                    MultiColumnIndexInfo.Indexes[index].IsUnique = true;
                }
            }
            
            OnMetadataChanged();
        }

        private void AddNewMultiColumnIndex(int[] columnHandles, bool unique)
        {
            int addedIndexHandle = 0;

            MultiColumnIndexInfo.AddNew(this, columnHandles, ref addedIndexHandle, unique);

            //Construct index if data are present already
            if (StateInfo.RowCount > 0)
            {
                MultiColumnIndexInfo.ConstructIndex(this, addedIndexHandle);
            }
        }

        public void RemoveMultiColumnIndex(IEnumerable<string> columns)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove multi index for '{string.Join(";", columns)}' columns from the '{Name}' table because it is readonly.");
            }
            
            var list = columns.ToArray();

            var columnHandles = new int[list.Length];

            for (int i = 0; i < list.Length; i++)
            {
                columnHandles[i] = GetColumn(list[i]).ColumnHandle;
            }
            
            RemoveMultiColumnIndexCore(columnHandles);
            
            OnMetadataChanged();
        }

        protected virtual void OnMetadataChanged()
        {
            m_metaAge++;
        }

        private void RemoveMultiColumnIndexCore(int[] columnHandles)
        {
            var index = MultiColumnIndexInfo.GetColumnFirstIndex(columnHandles);

            if (index >= 0)
            {
                MultiColumnIndexInfo.Indexes[index].ClearValues();

                MultiColumnIndexInfo.Indexes.RemoveAt(index);
            }
            
            OnMetadataChanged();
        }

        internal void RemoveRowFromIndexes(int rowHandle)
        {
            if (IndexInfo.HasAny)
            {
                foreach (var indexese in IndexInfo.Indexes)
                {
                    var dataColumn = DataColumnInfo.Columns[indexese.ColumnHandle];

                    var dataItem = dataColumn.DataStorageLink;

                    if (dataItem.IsNull(rowHandle, dataColumn))
                    {
                        indexese.RemoveIndex(rowHandle, null);
                    }
                    else
                    {
                        var indexKey = (IComparable)dataItem.GetData(rowHandle, dataColumn);

                        indexese.RemoveIndex(rowHandle, indexKey);
                    }
                }

                foreach (var indexese in MultiColumnIndexInfo.Indexes)
                {
                    var key = new IComparable[indexese.Columns.Length];

                    for (var i = 0; i < indexese.Columns.Length; i++)
                    {
                        var columnHandle = indexese.Columns[i];

                        var dataColumn = DataColumnInfo.Columns[columnHandle];
                        
                        var dataItem = dataColumn.DataStorageLink;

                        if (dataItem.IsNull(rowHandle, dataColumn))
                        {
                            key[i] = null;
                        }
                        else
                        {
                            var indexKey = (IComparable)dataItem.GetData(rowHandle, dataColumn);

                            key[i] = indexKey;
                        }
                    }

                    indexese.RemoveIndex(rowHandle, key);
                }
            }
        }
    }
}