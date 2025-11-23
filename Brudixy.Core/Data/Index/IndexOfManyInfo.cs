using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Constraints;
using Brudixy.Index;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    internal class IndexOfManyInfo
    {
        [NotNull]
        internal Data<IndexesOfMany> Indexes = new ();

        private bool m_isDisposed;

        public bool HasAny => m_isDisposed == false && Indexes.Count > 0;

        public bool HasAnyUnique
        {
            get
            {
                return HasAny && Indexes.Any(i => i.IsUnique);
            }
        }

        public IndexesOfMany TryGetIndex(IEnumerable<int> columnsHandles)
        {
            var index = TryGetIndexIndex(columnsHandles);

            if (index < 0)
            {
                return null;
            }

            return Indexes[index];
        }
        
        public IndexesOfMany TryGetIndex(ColumnHandle[] columnsHandles)
        {
            var index = TryGetIndexIndex(columnsHandles.Select(c => c.Handle).ToArray());

            if (index < 0)
            {
                return null;
            }

            return Indexes[index];
        }
        
        public int TryGetIndexIndex(IEnumerable<int> columnsHandles)
        {
            return Indexes.FindIndex(columnsHandles, (ind, cols) => ind.Columns.SequenceEqual(cols));
        }

        public void UpdateIndexValue(CoreDataTable table, int columnHandle, IComparable value, IComparable newValue, int rowHandle) 
        {
            if (m_isDisposed)
            {
                return;
            }

            var indices = Indexes.Where((i, col) => i.Columns.Contains(col), columnHandle);

            foreach (var index in indices)
            {
                var newValueList = new IComparable[index.Columns.Length];
                var oldValueList = new IComparable[index.Columns.Length];

                bool anyNull = false;

                for (int i = 0; i < index.Columns.Length; i++)
                {
                    var indexColumnHandle = index.Columns[i];

                    IComparable rowData = null;

                    if (indexColumnHandle != columnHandle)
                    {
                        var indexDataColumn = table.GetColumn(indexColumnHandle);
                        
                        if (indexDataColumn.DataStorageLink.IsNull(rowHandle, indexDataColumn) == false)
                        {
                            rowData = (IComparable) indexDataColumn.DataStorageLink.GetData(rowHandle, indexDataColumn);
                        }
                        else
                        {
                            anyNull = true;
                        }
                    }

                    oldValueList[i] = indexColumnHandle == columnHandle ? value : rowData;
                    newValueList[i] = indexColumnHandle == columnHandle ? newValue : rowData;
                }

                if (anyNull == false)
                {
                    index.UpdateIndexHandle(oldValueList, newValueList, rowHandle);
                }
            }
        }
        
        public void UpdateIndexValueStruct<T>(CoreDataTable table, int columnHandle, T? value, T? newValue, int rowHandle) where T : struct, IComparable, IComparable<T>
        {
            if (m_isDisposed)
            {
                return;
            }

            var indices = Indexes.Where((i, col) => i.Columns.Contains(col), columnHandle);

            foreach (var index in indices)
            {
                var newValueList = new IComparable[index.Columns.Length];
                var oldValueList = new IComparable[index.Columns.Length];

                bool allNull = true;

                for (int i = 0; i < index.Columns.Length; i++)
                {
                    var indexColumnHandle = index.Columns[i];

                    IComparable rowData = null;

                    if (indexColumnHandle != columnHandle)
                    {
                        var indexDataColumn = table.GetColumn(indexColumnHandle);
                        
                        if (indexDataColumn.DataStorageLink.IsNull(rowHandle, indexDataColumn) == false)
                        {
                            rowData = (IComparable) indexDataColumn.DataStorageLink.GetData(rowHandle, indexDataColumn);

                            allNull = false;
                        }
                    }

                    oldValueList[i] = indexColumnHandle == columnHandle ? value : rowData;
                    newValueList[i] = indexColumnHandle == columnHandle ? newValue : rowData;
                }

                if (allNull == false)
                {
                    index.UpdateIndexHandle(oldValueList, newValueList, rowHandle);
                }
            }
        }

        public int GetRowHandle<T>(CoreDataTable table, int columnHandle, T value) where T : IComparable
        {
            if (m_isDisposed)
            {
                return -1;
            }

            var index = GetColumnFirstIndex(columnHandle);

            if (index >= 0)
            {
                var valueList = new IComparable[1] { value };

                return Indexes[index].GetRowHandle(table, valueList);
            }

            return -1;
        }

        public int GetColumnFirstIndex(int columnHandle)
        {
            return Indexes.FindIndex(columnHandle, (col, target) => col.Columns.Contains(target));
        }

        public int GetColumnFirstIndex(IEnumerable<int> columnHandles)
        {
            return Indexes.FindIndex(columnHandles, (i, col) => i.Columns.SequenceEqual(col));
        }

        public Data<int> GetRowHandles<V>(int indexIndex, IEnumerable<V> value) where V : IComparable
        {
            var rowHandles = GetRowHandles(indexIndex, value.Cast<IComparable>().ToArray());
            
            return rowHandles;
        }

        public Data<int> GetRowHandles(int indexIndex, IComparable[] value)
        {
            if (m_isDisposed)
            {
                return new Data<int>();
            }

            var index = Indexes[indexIndex];

            var rowHandles = index.GetRowHandles(value);

            return rowHandles;
        }
        
        public int GetRowHandle(CoreDataTable dataTable, int indexIndex, IComparable[] value)
        {
            if (m_isDisposed)
            {
                return -1;
            }

            var index = Indexes[indexIndex];

            return index.GetRowHandle(dataTable, value);
        }

        public void AddToIndex(CoreDataTable table, int index, IComparable[] cellVals, int rowHandle)
        {
            var indexesOfMany = Indexes[index];

            bool isUnique = indexesOfMany.IsUnique;

            if (isUnique)
            {
                if (cellVals.Any(val => val == null))
                {
                    var columns = string.Join(",", indexesOfMany.Columns);

                    throw new ConstraintException($"Unique indexed columns {columns} value can't be null.");
                }
            }

            indexesOfMany.AddIndex(cellVals, rowHandle);
        }

        public void AddNew(CoreDataTable table, int[] columnHandles, ref int addedIndexHandle, bool unique)
        {
            var index = Indexes.FirstOrDefault(c => c.Columns.SequenceEqual(columnHandles));

            if (index != null)
            {
                throw new InvalidOperationException($"Index on columns [{string.Join(",", columnHandles)}] is already exists.");
            }

            AddNew(table, columnHandles,  ref addedIndexHandle, ref Indexes, unique);
        }

        private static void AddNew(CoreDataTable table, 
            int[] columnHandles,
            ref int addedIndexHandle,
            ref Data<IndexesOfMany> indexes, 
            bool unique)
        {
            if (indexes == null)
            {
                indexes = new Data<IndexesOfMany>();
            }

            addedIndexHandle = indexes.Count;

            IMultiValueIndex readyIndex = new MultiColumnHashIndex(unique, columnHandles.Length);

            var newIndex = new IndexesOfMany(columnHandles, readyIndex) {IsUnique = unique, HashIndex = true};

            indexes.Add(newIndex);
        }
        
        public void RebuildIndexes(CoreDataTable table)
        {
            for (var index = 0; index < Indexes.Count; index++)
            {
                var ofMany = Indexes[index];
                ConstructIndex(table, index);
            }
        }

        internal void ConstructIndex(CoreDataTable table, int indexHandle)
        {
            if (table.RowCount == 0)
            {
                //do not need construct because we have no data.
                return;
            }

            var index = Indexes[indexHandle];

            for (int rowHandle = 0; rowHandle < table.StateInfo.RowStorageCount; rowHandle++)
            {
                if (table.StateInfo.GetRowState(rowHandle) == RowState.Detached)
                {
                    continue;
                }

                var value = new IComparable[index.Columns.Length];

                bool anyUnique = false;

                for (var i = 0; i < index.Columns.Length; i++)
                {
                    var columnHandle = index.Columns[i];

                    var dataColumn = table.DataColumnInfo.Columns[columnHandle];

                    var item = dataColumn.DataStorageLink;
                    
                    var isUnique = dataColumn.IsUnique;

                    if (item.IsNull(rowHandle, dataColumn))
                    {
                        if (isUnique)
                        {
                            throw new ConstraintException($"Unique indexed  '{dataColumn.ColumnName}' column can't be null.");
                        }

                        value[i] = null;
                    }
                    else
                    {
                        value[i] = (IComparable)item.GetData(rowHandle, dataColumn);
                    }
                }

                index.AddIndex(value, rowHandle);
            }
        }

        public void CreateFrom(IndexOfManyInfo source, bool withData)
        {
            Indexes = new(source.Indexes.Count);

            foreach (var sourceIndex in source.Indexes)
            {
                Indexes.Add(sourceIndex.Clone(withData));
            }
        }

        public void Merge(CoreDataTable targetTable, CoreDataTable sourceTable)
        {
            var source = sourceTable.MultiColumnIndexInfo;

            MergeIndex(targetTable, sourceTable, source.Indexes, Indexes);
        }

        private void MergeIndex(CoreDataTable targetTable,
            CoreDataTable sourceTable,
            Data<IndexesOfMany> sourceIndexes,
            Data<IndexesOfMany> targetIndexes)
        {
            if (sourceIndexes != null)
            {
                foreach (var sourceIndex in sourceIndexes)
                {
                    var targetIndex = FindIndex(targetIndexes, sourceIndex);

                    bool allTypesCompatible = true;

                    var columns = new int[sourceIndex.Columns.Length];

                    for (var index = 0; index < sourceIndex.Columns.Length; index++)
                    {
                        var sourceColumnHandle = sourceIndex.Columns[index];

                        var sourceColumn = sourceTable.DataColumnInfo.Columns[sourceColumnHandle];

                        if (targetTable.DataColumnInfo.ColumnMappings.TryGetValue(sourceColumn.ColumnName, out var targetColumn))
                        {
                            var targetColumnType = targetColumn.Type;
                            var sourceColumnType = sourceColumn.Type;

                            allTypesCompatible = targetColumnType == sourceColumnType;

                            if (allTypesCompatible == false)
                            {
                                break;
                            }
                        }

                        columns[index] = sourceColumnHandle;
                    }

                    if (targetIndex == null && allTypesCompatible)
                    {
                        int addedNewIndexHandle = 0;

                        AddNew(targetTable, columns, ref addedNewIndexHandle, sourceIndex.IsUnique);

                        ConstructIndex(targetTable, addedNewIndexHandle);
                    }
                }
            }
        }

        private static IndexesOfMany FindIndex(
            Data<IndexesOfMany> targetIndexes,
            IndexesOfMany sourceIndex)
        {
            foreach (var targetIndex in targetIndexes)
            {
                bool allColumnsCovered = true;

                foreach (var targetColumnName in targetIndex.Columns)
                {
                    allColumnsCovered = sourceIndex.Columns.Contains(targetColumnName);

                    if (allColumnsCovered == false)
                    {
                        break;
                    }
                }

                if (allColumnsCovered)
                {
                    return targetIndex;
                }
            }

            return null;
        }

        private void DisposeIndex(Data<IndexesOfMany> indexeses)
        {
            if (indexeses != null)
            {
                foreach (var indexese in indexeses)
                {
                    indexese.ReadyIndex.Dispose();
                }

                indexeses.Clear();
            }
        }

        public void Dispose()
        {
            m_isDisposed = true;

            DisposeIndex(Indexes);
        }

        public bool CheckNewValue(CoreDataTable dataTable, int columnHandle, object prevValue, object value, int rowHandle)
        {
            var columnFirstIndex = GetColumnFirstIndex(columnHandle);

            if (columnFirstIndex < 0)
            {
                return true;
            }

            var indexesOfMany = Indexes[columnFirstIndex];

            if (indexesOfMany.IsUnique)
            {
                var valueToCompare = new IComparable[indexesOfMany.Columns.Length];

                for (var i = 0; i < indexesOfMany.Columns.Length; i++)
                {
                    var indexColumn = indexesOfMany.Columns[i];
                    if (columnHandle == indexColumn)
                    {
                        valueToCompare[i] = value as IComparable;
                    }
                    else
                    {
                        var dataColumn = dataTable.GetColumn(indexColumn);

                        var rowValue = dataColumn.DataStorageLink.GetData(rowHandle, dataColumn);

                        valueToCompare[i] = rowValue as IComparable;
                    }
                }

                var checkNewValue = indexesOfMany.GetRowHandle(dataTable, valueToCompare) == -1;
                
                return checkNewValue;
            }

            return true;
        }

        public bool Remove(int indexIndex)
        {
            var indexes = Indexes[indexIndex];

            indexes.ReadyIndex.Clear();

            Indexes.RemoveAt(indexIndex);

            return true;
        }

        public bool ContainsColumn(CoreDataTable dataTable, int columnHandle)
        {
            return Indexes.FindIndex(columnHandle, (col, target) => col.Columns.Contains(target)) >= 0;
        }
        
        internal int GetRowHandle<T>(CoreDataTable table, T value) where T : IComparable
        {
            if (m_isDisposed)
            {
                return -1;
            }

            if (Indexes.Count > 0 && table.DataColumnInfo.ColumnsCount > 0)
            {
                var type = typeof(T);

                var typeCode = Type.GetTypeCode(type);

                var storageType = IndexInfo.GetTableStorageType(typeCode);

                if (storageType == null && type == typeof(Guid))
                {
                    storageType = TableStorageType.Guid;
                }

                if (storageType == null)
                {
                    throw new NotSupportedException($"Indexing for '${type.Name}' type isn't supported yet.");
                }

                return GetRowHandle(table, storageType.Value, null, value);
            }

            return -1;
        }


        public int GetRowHandle<T>(CoreDataTable table, TableStorageType storageType, int? indexIndex, T value)
            where T : IComparable
        {
            if (m_isDisposed)
            {
                return -1;
            }

            var rowHandle = -1;

            var suitableIndex = indexIndex.HasValue
                ? Indexes[indexIndex.Value]
                : Indexes.FirstOrDefault(i => table.GetColumn(i.Columns.First()).Type == storageType);

            if (suitableIndex != null)
            {
                rowHandle = suitableIndex.GetRowHandle(table, new IComparable[] { value });
            }

            if (rowHandle >= 0 && table.StateInfo.IsNotDeletedAndRemoved(rowHandle))
            {
                return rowHandle;
            }
            
            return -1;
        }

        public void RemapColumnHandles(Map<int,int> oldToNewMap)
        {
            foreach (var indexesOfMany in Indexes)
            {
                var oldOnes = indexesOfMany.Columns.ToArray();

                for (int i = 0; i < oldOnes.Length; i++)
                {
                    var oldHandle = oldOnes[i];

                    if(oldToNewMap.TryGetValue(oldHandle, out var newHandle))
                    {
                        indexesOfMany.Columns[i] = newHandle;
                    }
                }
            }
        }
    }
}
