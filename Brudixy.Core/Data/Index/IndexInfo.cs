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
    internal partial class IndexInfo
    {
        private static Type s_rangeType = typeof(RangeIndex<>);
        private static Type s_coreStructHashIndexType = typeof(CoreStructHashIndex<>);
        private static Type s_coreHashIndexType = typeof(CoreHashIndex<>);

        [NotNull]
        internal Data<Indexes> Indexes = new ();

        [NotNull]
        internal Map<int, int> IndexMappings = new();

        private bool m_isDisposed;

        public bool HasAny => m_isDisposed == false && Indexes.Count > 0;

        public bool ColumnHasIndex(int columnHandle)
        {
            return IndexMappings.ContainsKey(columnHandle);
        }
        
        public void RemapColumnHandles(Map<int, int> oldToNew)
        {
            foreach (var kb in oldToNew)
            {
                var oldColumnHandle = kb.Key;
                var newColumnHandle = kb.Value;

                if (oldColumnHandle != newColumnHandle)
                {
                    if (IndexMappings.ContainsKey(oldColumnHandle))
                    {
                        var mapping = IndexMappings[oldColumnHandle];
                        
                        IndexMappings[newColumnHandle] = mapping;

                        var indexes = Indexes[mapping];
                        
                        indexes.ColumnHandle = newColumnHandle;

                        IndexMappings.Remove(oldColumnHandle);
                    }
                }
            }
        }
        
        public void UpdateIndexValue<T>(CoreDataTable table, int columnHandle, T value, T newValue, int rowHandle) where T : IComparable
        {
            if (m_isDisposed)
            {
                return;
            }

            if (IndexMappings.TryGetValue(columnHandle, out var indexIndex))
            {
                var indexes = Indexes[indexIndex];

                indexes.UpdateIndexHandle(value, newValue, rowHandle);
            }
        }
        
        public void AddIndexValue<T>(CoreDataTable table, int columnHandle, T newValue, int rowHandle, bool addToIndex) where T : IComparable
        {
            if (m_isDisposed)
            {
                return;
            }

            if (IndexMappings.TryGetValue(columnHandle, out var indexIndex))
            {
                var indexes = Indexes[indexIndex];

                indexes.AddIndex(newValue, rowHandle);
            }
        }

        public int GetRowHandle<T>(CoreDataTable table, int columnHandle, T value) where T : IComparable
        {
            if (m_isDisposed)
            {
                return -1;
            }

            if (IndexMappings.TryGetValue(columnHandle, out var indexIndex))
            {
                var columnType = table.DataColumnInfo.Columns[columnHandle].Type;

                return GetRowHandle(table, columnType, indexIndex, value, true);
            }
            return -1;
        }

        public int GetRowHandle<T>(CoreDataTable table, T value) where T : IComparable
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

                return GetRowHandle(table, storageType.Value, null, value, true);
            }

            return -1;
        }

        public int GetRowHandle<T>(CoreDataTable table, 
            TableStorageType storageType,
            int? indexIndex, 
            T value,
            bool ignoreDeleted) where T : IComparable
        {
            if (m_isDisposed)
            {
                return -1;
            }

            var rowHandle = -1;

            Indexes suitableIndex = null;

            if (indexIndex.HasValue)
            {
                suitableIndex = Indexes[indexIndex.Value];
            }
            else
            {
                var findIndex = Indexes.FindIndex(storageType, c => c.ReadyIndex.StorageType.type);

                if (findIndex >= 0)
                {
                    suitableIndex = Indexes[findIndex];
                }
            }

            if (suitableIndex != null)
            {
                if (suitableIndex.ReadyIndex.IsUnique)
                {
                    rowHandle = suitableIndex.GetRowHandle(value);

                    if (rowHandle >= 0)
                    {
                        var rowState = table.StateInfo.GetRowState(rowHandle);
                        
                        if (rowState == RowState.Detached || ignoreDeleted && (rowState == RowState.Deleted))
                        {
                            return -1;
                        }
                    }
                }
                else
                {
                    foreach (var handle in suitableIndex.GetRowHandles(value))
                    {
                        var rowState = table.StateInfo.GetRowState(handle);
                        
                        if (rowState == RowState.Detached || ignoreDeleted && (rowState == RowState.Deleted))
                        {
                            continue;
                        }

                        return handle;
                    }
                }
            }

            return rowHandle;
        }
        
         public int GetRowHandleStruct<T>(CoreDataTable table, 
            TableStorageType storageType,
            int? indexIndex, 
            ref T value,
            bool ignoreDeleted) where T : struct, IComparable
        {
            if (m_isDisposed)
            {
                return -1;
            }

            var rowHandle = -1;

            Indexes suitableIndex = null;

            if (indexIndex.HasValue)
            {
                suitableIndex = Indexes[indexIndex.Value];
            }
            else
            {
                var findIndex = Indexes.FindIndex(storageType, c => c.ReadyIndex.StorageType.type);

                if (findIndex >= 0)
                {
                    suitableIndex = Indexes[findIndex];
                }
            }

            if (suitableIndex != null)
            {
                if (suitableIndex.ReadyIndex.IsUnique)
                {
                    rowHandle = suitableIndex.GetRowHandleStruct(ref value);

                    if (rowHandle >= 0)
                    {
                        var rowState = table.StateInfo.GetRowState(rowHandle);
                        
                        if (rowState == RowState.Detached || ignoreDeleted && (rowState == RowState.Deleted))
                        {
                            return -1;
                        }
                    }
                }
                else
                {
                    foreach (var handle in suitableIndex.GetRowHandlesStruct(ref value))
                    {
                        var rowState = table.StateInfo.GetRowState(rowHandle);
                        
                        if (rowState == RowState.Detached || ignoreDeleted && (rowState == RowState.Deleted))
                        {
                            continue;
                        }

                        return handle;
                    }
                }
            }

            return rowHandle;
        }

        public Data<int> GetRowHandles<T>(int indexIndex, T value) where T : IComparable
        {
            if (m_isDisposed)
            {
                return new Data<int>();
            }

            var indexes = Indexes[indexIndex];

            var rowHandles = indexes.GetRowHandles(value);

            return rowHandles;
        }

        public void RebuildIndex(CoreDataTable table, int columnHandle)
        {
            if (IndexMappings.TryGetValue(columnHandle, out var indexHandle))
            {
                var indexes = Indexes[indexHandle];
                
                indexes.ClearValues();
                
                ConstructIndex(table, columnHandle);
            }
        }

        public void TryAddValueToIndex(CoreDataTable table, int columnHandle, object cellVal, int rowHandle)
        {
            var indexInfo = this;

            if (indexInfo.IndexMappings.TryGetValue(columnHandle, out var indexIndex))
            {
                indexInfo.Indexes[indexIndex].AddIndex((IComparable)cellVal, rowHandle);
            }
        }
        
        public void AddNew(CoreDataTable table, CoreDataColumn column, ref int addedIndexHandle, bool unique)
        {
            if (IndexMappings.ContainsKey(column.ColumnHandle))
            {
                throw new ConstraintException($"Index on column {column} already exists.");
            }

            AddNew(column, ref addedIndexHandle, ref Indexes, unique);

            IndexMappings[column.ColumnHandle] = addedIndexHandle;
        }

        private static void AddNew(CoreDataColumn column, ref int addedIndexHandle, ref Data<Indexes> indexes, bool unique)
        {
            if (indexes == null)
            {
                indexes = new Data<Indexes>();
            }

            addedIndexHandle = indexes.Count;

            IIndexStorage readyIndex;

            if (column.TypeModifier == TableStorageTypeModifier.Range)
            {
                var dataType = CoreDataTable.GetDataType(column.Type, TableStorageTypeModifier.Simple, false, column.DataType);

                var rangeType = s_rangeType.MakeGenericType(dataType);

                readyIndex =  (IIndexStorage)Activator.CreateInstance(rangeType);
            }
            else if (column.TypeModifier == TableStorageTypeModifier.Complex)
            {
                readyIndex = new CoreHashIndex<IComparable>(unique); 
            }
            else
            {
                if (column.Type == TableStorageType.String)
                {
                    var fullText = column.GetXProperty<bool?>(CoreDataTable.StringIndexFullTextXProp);
                    
                    var caseSensitive = column.GetXProperty<bool?>(CoreDataTable.StringIndexCaseSensitiveXProp);
                    
                    var stringIndex = new StringIndex(caseSensitive ?? false, unique, fullText ?? false);

                    readyIndex = stringIndex;
                }
                else
                {
                    var dataType = CoreDataTable.GetDataType(column.Type, column.TypeModifier, false, column.DataType);

                    var rangeType = dataType.IsValueType ? s_coreStructHashIndexType : s_coreHashIndexType;

                    var instance = (IHashIndexInit)Activator.CreateInstance(rangeType.MakeGenericType(dataType));
                    
                    instance.Init(unique);
                    
                    readyIndex =  (IIndexStorage)instance;
                }
            }

            var newIndex = new Indexes(column.ColumnHandle, readyIndex);

            indexes.Add(newIndex);
        }

        internal void ConstructIndex(CoreDataTable table, int columnHandle)
        {
            if (table.RowCount == 0)
            {
                //do not need construct because we have no data.
                return;
            }

            var dataColumn = table.DataColumnInfo.Columns[columnHandle];
            
            var storageType = dataColumn.Type;

            var dataItem = dataColumn.DataStorageLink;

            if (IndexMappings.TryGetValue(columnHandle, out var indexIndex))
            {
                if (storageType < TableStorageType.BigInteger && storageType != TableStorageType.String)
                {
                    ConstructStructType(table, columnHandle, storageType, dataItem, dataColumn.ColumnName, indexIndex);
                }
                else
                {
                    var isUnique = dataColumn.IsUnique;

                    for (int rowHandle = 0; rowHandle < table.StateInfo.RowStorageCount; rowHandle++)
                    {
                        var item = dataItem;

                        if (item.IsNull(rowHandle, dataColumn))
                        {
                            if (isUnique)
                            {
                                throw new ConstraintException(
                                    $"Unique indexed column '{dataColumn.ColumnName}' can't be null.");
                            }

                            var indexes = Indexes[indexIndex];

                            indexes.AddIndex(null, rowHandle);
                        }
                        else
                        {
                            var cellVal = (IComparable)item.GetData(rowHandle, dataColumn);

                            var indexes = Indexes[indexIndex];

                            indexes.AddIndex(cellVal, rowHandle);
                        }
                    }
                }
            }
        }

        private void ConstructSimpleStructIndex<W>(CoreDataTable table, IDataItem dataItem, string column, int indexIndex, Data<Indexes> indexes, int columnHandle)
            where W : struct, IComparable
        {
            var item = dataItem as ITypedDataItem<W>;

            var dataColumn = table.DataColumnInfo.Columns[columnHandle];
            
            var isUnique = dataColumn.IsUnique;

            for (int rowHandle = 0; rowHandle < table.StateInfo.RowStorageCount; rowHandle++)
            {
                if (table.GetRowState(rowHandle) == RowState.Detached)
                {
                    continue;
                }

                if (dataItem.IsNull(rowHandle, dataColumn))
                {
                    if (isUnique)
                    {
                        throw new ConstraintException($"Unique indexed column {column} can't be null.");
                    }

                    var index = indexes[indexIndex];

                    index.AddIndex(null, rowHandle);
                }
                else
                {
                    var index = indexes[indexIndex];

                    if (item != null)
                    {
                        W? comparable = item.GetDataTyped(rowHandle, dataColumn);
                        
                        index.AddIndexStruct(ref comparable, rowHandle);
                    }
                    else
                    {
                        try
                        {
                            W? value = (W)dataItem.GetData(rowHandle, dataColumn);
                            
                            index.AddIndexStruct(ref value, rowHandle);
                        }
                        catch (InvalidCastException)
                        {
                            W?  comparable = (W)dataItem.CheckValueIsCompatibleType(dataItem.GetData(rowHandle, dataColumn), dataColumn);
                            
                            index.AddIndexStruct(ref comparable, rowHandle);
                        }
                    }
                }
            }
        }

        public void CreateFrom(IndexInfo source, bool withData)
        {
            Indexes = new(source.Indexes.Count);

            foreach (var sourceIndex in source.Indexes)
            {
                Indexes.Add(sourceIndex.Clone(withData));
            }

            IndexMappings = new(source.IndexMappings);
        }

        public void Merge(CoreDataTable targetTable, CoreDataTable sourceTable)
        {
            var newIndexes = new Data<ColumnnIndex>();

            var source = sourceTable.IndexInfo;

            MergeIndex(targetTable, sourceTable, source.Indexes, Indexes, newIndexes);
            
            newIndexes.Clear();
        }

        private void MergeIndex(CoreDataTable targetTable, CoreDataTable sourceTable, Data<Indexes> sourceIndex, Data<Indexes> targetIndexed, Data<ColumnnIndex> newIndexesHanldes)
        {
            if (sourceIndex != null)
            {
                foreach (var indexese in sourceIndex)
                {
                    var sourceColumn = sourceTable.DataColumnInfo.Columns[indexese.ColumnHandle];
                    var indexColumn = sourceColumn.ColumnName;

                    if (targetTable.DataColumnInfo.ColumnMappings.TryGetValue(indexColumn, out var targetColumn))
                    {
                        var targetColumnType = targetColumn.Type;
                        var sourceColumnType = sourceColumn.Type;

                        if (targetColumnType == sourceColumnType)
                        {
                            if (targetIndexed == null || targetIndexed.Any(c => c.ColumnHandle == targetColumn.ColumnHandle) == false)
                            {
                                if (IndexMappings.TryGetValue(indexese.ColumnHandle, out var value))
                                {
                                    if (value != targetColumn.ColumnHandle)
                                    {
                                        IndexMappings[indexese.ColumnHandle] = targetColumn.ColumnHandle;
                                    }
                                }
                                else
                                {
                                    int addedNewIndexHandle = 0;

                                    AddNew(targetTable, targetColumn, ref addedNewIndexHandle, targetColumn.IsUnique);

                                    ConstructIndex(targetTable, targetColumn.ColumnHandle);

                                    newIndexesHanldes.Add(new ColumnnIndex { ColumnHandle = targetColumn.ColumnHandle, IndexHandle = addedNewIndexHandle });
                                }
                            }
                        }
                    }
                }
            }
        }

        private struct ColumnnIndex
        {
            public int ColumnHandle;

            public int IndexHandle;
        }

        private void DisposeIndex(Data<Indexes> indexeses)
        {
            if (indexeses != null)
            {
                foreach (var indexese in indexeses)
                {
                    indexese.ReadyIndex?.Dispose();
                }

                indexeses.Dispose();
            }
        }

        public void Dispose()
        {
            m_isDisposed = true;

            DisposeIndex(Indexes);

            IndexMappings?.Dispose();

            Indexes = null;
            IndexMappings = null;
        }

        public IComparable GetMinMaxValue(CoreDataTable table, int columnHandle, bool calcMax)
        {
            bool Check(int rowHandle)
            {
                var rowState = table.StateInfo.GetRowState(rowHandle);

                return rowState != RowState.Deleted && rowState != RowState.Detached;
            }

            var indexMapping = IndexMappings[columnHandle];

            var indexStorage = Indexes[indexMapping].ReadyIndex;

            return calcMax ? indexStorage?.GetMaxNotNullValue(Check) : indexStorage?.GetMinNotNullValue(Check);
        }

        public bool CheckNewValue(CoreDataTable dataTable, int columnHandle, object prevValue, object value)
        {
            if (value == null)
            {
                return false;
            }

            return GetRowHandle(dataTable, columnHandle, (IComparable)value) == -1;
        }

        public bool Remove(int columnHandle)
        {
            if (IndexMappings.TryGetValue(columnHandle, out var indexIndex))
            {
                var indexes = Indexes[indexIndex];

                indexes.ReadyIndex?.Dispose();

                Indexes.RemoveAt(indexIndex);
                
                IndexMappings.Remove(columnHandle);

                var list = IndexMappings.ToData();

                foreach (var columnMapping in list)
                {
                    if (columnMapping.Value > indexIndex)
                    {
                        IndexMappings[columnMapping.Key] = columnMapping.Value - 1;
                    }
                }

                list.Dispose();

                return true;
            }

            return false;
        }

        public IEnumerable<int> GetRowHandlesByStringIndex(int indexIndex, string value, CoreDataTable.StringIndexLookupType type)
        {
            if (m_isDisposed)
            {
                return new Data<int>();
            }

            var indexes = Indexes[indexIndex];

            var rowHandles = indexes.GetRowHandlesByStringIndex(value, type);

            return rowHandles;
        }

        public void RebuildIndex(CoreDataTable table)
        {
            foreach (var dataColumn in table.GetColumns())
            {
                if (dataColumn.HasIndex)
                {
                    RebuildIndex(table, dataColumn.ColumnHandle);
                }
            }
        }
    }
}