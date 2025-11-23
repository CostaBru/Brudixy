using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreDataRowAccessor> ICoreDataTable.AllRows => GetAllRows();

        public IEnumerable<CoreDataRow> AllRows => GetAllRows();
        
        [CanBeNull]
        public CoreDataRow GetRowByHandle(int rowHandle)
        {
            if (rowHandle < 0 || rowHandle >= StateInfo.RowCount)
            {
                return null;
            }

            if (StateInfo.GetRowState(rowHandle) == RowState.Detached)
            {
                return null;
            }

            return GetRowInstance(rowHandle);
        }
        
        internal IEnumerable<CoreDataRow> GetAllRows()
        {
            var stateInfoRowStorageCount = StateInfo.RowStorageCount;

            for (int i = 0; i < stateInfoRowStorageCount; i++)
            {
                if (StateInfo.IsNotRemoved(i))
                {
                    var row = GetRowInstance(i);

                    yield return row;
                }
            }
        }

        public CoreDataRow this[int index]
        {
            get
            {
                var rowHandle = StateInfo.RowHandles[index];

                return GetRowInstance(rowHandle);
            }
        }
        
        public int GetRowHandleIndex(int rowHandle)
        {
           return StateInfo.RowHandles.BinarySearch(rowHandle, 0, StateInfo.RowHandles.Count);
        }

        [NotNull]
        protected CoreDataRow GetRowInstance(int rowHandle) 
        {
            if (m_rowReferences == null)
            {
                m_rowReferences = new Data<CoreDataRow>(Capacity);
            }

            if (rowHandle < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(rowHandle));
            }

            if (rowHandle >= StateInfo.RowStorageCount)
            {
                throw new IndexOutOfRangeException(
                    $"Row index '{rowHandle}' is greater or equal than '{Name}' table row storage count '{StateInfo.RowStorageCount}'.");
            }

            m_rowReferences.Ensure(Math.Max(Capacity, StateInfo.RowStorageCount));

            var instance = GetReadyInstance(rowHandle);

            if (instance == null)
            {
                var row = CreateRowInstance();

                row.Init(rowHandle, this);

                m_rowReferences[rowHandle] = row;

                return row;
            }

            return instance;
        }

        protected virtual CoreDataRow GetReadyInstance(int rowHandle)
        {
            return m_rowReferences[rowHandle];
        }

        protected virtual CoreDataRow CreateRowInstance() 
        {
            return new CoreDataRow();
        }

        public IEnumerable<int> RowsHandles => StateInfo.RowHandles;

        public IEnumerable<int> AllRowsHandles
        {
            get
            {
                var stateInfoRowStorageCount = StateInfo.RowStorageCount;

                for (int i = 0; i < stateInfoRowStorageCount; i++)
                {
                    if (StateInfo.IsNotRemoved(i))
                    {
                        yield return i;
                    }
                }
            }
        }
        
        IEnumerable<ICoreDataRowAccessor> ICoreDataTable.Rows  => this.GetRows();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreDataRowReadOnlyAccessor> ICoreReadOnlyDataTable.AllRows => this.GetAllRows();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreDataRowReadOnlyAccessor> ICoreReadOnlyDataTable.Rows => this.GetRows();

        public IEnumerable<CoreDataRow> Rows => this.GetRows();

        internal IEnumerable<CoreDataRow> GetRows()
        {
            foreach (var rowsHandle in RowsHandles)
            {
                yield return GetRowInstance(rowsHandle);
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowAccessor ICoreDataTable.GetRowBy<T>(T value) => GetRowBy(value);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowAccessor ICoreDataTable.GetRow(int rowHandle) => GetRowInstance(rowHandle);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowReadOnlyAccessor ICoreReadOnlyDataTable.GetRow(int rowHandle) => GetRowInstance(rowHandle);

        public CoreDataRow GetRow(int rowHandle) => GetRowInstance(rowHandle);

        [CanBeNull]
        public CoreDataRow GetRowBy<T>(T value) where T : IComparable
        {
            if (DataColumnInfo.PrimaryKeyColumns.Length > 0)
            {
                var dataColumnContainer = DataColumnInfo.PrimaryKeyColumns.First();

                if (DataColumnInfo.PrimaryKeyColumns.Length == 1)
                {
                    return GetRowBySinglePk(value);
                }
                else
                {
                    var index = MultiColumnIndexInfo.GetColumnFirstIndex(dataColumnContainer.ColumnHandle);

                    if (index >= 0)
                    {
                        var key = new IComparable[] {value};
                        
                        var rowHandle = MultiColumnIndexInfo.Indexes[index].GetRowHandle(this, key);

                        if (rowHandle >= 0)
                        {
                            if (StateInfo.IsNotDeletedAndRemoved(rowHandle))
                            {
                                return GetRowInstance(rowHandle);
                            }
                        }
                    }
                }
            }
            else
            {
                var rowHandle = IndexInfo.GetRowHandle(this, value);

                if (rowHandle >= 0)
                {
                    return GetRowInstance(rowHandle);
                }
            }

            return null;
        }

        public CoreDataRow GetRowBySinglePk<T>(T value) where T : IComparable
        {
            var primaryKeyColumnHandle = DataColumnInfo.PrimaryKeyColumns.First();
            
            if (IndexInfo.IndexMappings.TryGetValue(primaryKeyColumnHandle.ColumnHandle, out var index))
            {
                var rowHandle = IndexInfo.Indexes[index].GetRowHandle(value);

                if (rowHandle >= 0)
                {
                    if (StateInfo.IsNotDeletedAndRemoved(rowHandle))
                    {
                        return GetRowInstance(rowHandle);
                    }
                }
            }

            return null;
        }

        [CanBeNull]
        public CoreDataRow GetRowByArray([NotNull] IComparable[] value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            
            if (DataColumnInfo.PrimaryKeyColumns.Length > 1)
            {
                return GetRowByMultiColPk(value);
            }
            
            var indexesOfMany = MultiColumnIndexInfo.Indexes.FirstOrDefault();

            if (indexesOfMany == null)
            {
                throw new DataException(
                    $"Cannot get any row for given items because multi column index is not set for {TableName} table.");
            }
            
            var rowHandle = indexesOfMany.GetRowHandle(this, value);

            if (rowHandle >= 0)
            {
                if (StateInfo.IsNotDeletedAndRemoved(rowHandle))
                {
                    return GetRowInstance(rowHandle);
                }
            }

            return null;
        }
        
        [CanBeNull]
        public IEnumerable<TRow> GetRowsByArray<TRow>([NotNull] string[] columns, [NotNull] IComparable[] value) where TRow: CoreDataRow
        {
            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }
            if (columns.Length != value.Length)
            {
                throw new ArgumentOutOfRangeException($"columns and value array lengths are different {columns.Length} != {value.Length}.");
            }
            
            var columnHandles = columns.Select(c => DataColumnInfo.ColumnMappings[c].ColumnHandle).ToArray();

            var index = MultiColumnIndexInfo.TryGetIndex(columnHandles);

            if (index == null)
            {
                throw new DataException(
                    $"Cannot get any row for given [{string.Join(",", columns)}] because multi column index is not set for {TableName} table.");
            }
            
            var rowHandles = index.GetRowHandles(value);

            foreach (var rowHandle in rowHandles)
            {
                if (StateInfo.IsNotDeletedAndRemoved(rowHandle))
                {
                    yield return (TRow)GetRowInstance(rowHandle);
                }
            }
        }
        
        [CanBeNull]
        public CoreDataRow GetRowByMultiColPk(IComparable[] value)
        {
            var indexesOfMany = MultiColumnIndexInfo.Indexes.FirstOrDefault();
            
            if (indexesOfMany == null)
            {
                throw new DataException(
                    $"Cannot get any row for given items because primary key index is not set for {TableName} table.");
            }
            
            var rowHandle = indexesOfMany.GetRowHandle(this, value);

            if (rowHandle >= 0)
            {
                if (StateInfo.IsNotDeletedAndRemoved(rowHandle))
                {
                    return GetRowInstance(rowHandle);
                }
            }

            return null;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowReadOnlyAccessor ICoreReadOnlyDataTable.GetRow<T>(string column, T value) => GetRow(column, value);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public ICoreDataRowAccessor GetRow<T>(int columnHandle, T value) where T : IComparable => GetRow(new ColumnHandle(columnHandle), value);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowAccessor ICoreDataTable.GetRow<T>(string column, T value) => GetRow(column, value);

        [CanBeNull]
        public CoreDataRow GetRow<T>(string column, T value, bool ignoreDeleted = true) where T : IComparable
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(column, out var dataColumn))
            {
                return GetRowCore(dataColumn, value, ignoreDeleted);
            }

            return null;
        }

        public CoreDataRow GetRow<T>(ColumnHandle columnHandle, T value, bool ignoreDeleted = true) where T : IComparable
        {
            var dataColumn = DataColumnInfo.Columns[columnHandle.Handle];

            return GetRowCore(dataColumn, value, ignoreDeleted);
        }

        public CoreDataRow GetRow<T>(CoreDataColumn dataColumn, T value, bool ignoreDeleted)
            where T : IComparable
        {
            if (ReferenceEquals(dataColumn.DataTable, this) == false)
            {
                return GetRowCore(GetColumn(dataColumn.ColumnName), value, ignoreDeleted);
            }
            
            return GetRowCore(dataColumn, value, ignoreDeleted);
        }

        protected CoreDataRow GetRowCore<T>(CoreDataColumn dataColumn, T value, bool ignoreDeleted) where T : IComparable
        {
            if (IndexInfo.IndexMappings.TryGetValue(dataColumn.ColumnHandle, out var indexMapping))
            {
                var storageType = dataColumn.Type;

                int rowHandle = IndexInfo.GetRowHandle(this, storageType, indexMapping, value, ignoreDeleted);

                if (rowHandle < 0)
                {
                    return null;
                }

                return GetRowInstance(rowHandle);
            }

            return TryGetRow(value, ignoreDeleted, dataColumn);
        }

        [CanBeNull]
        public CoreDataRow GetRow<T>(string column, ref T value, bool ignoreDeleted = true) where T : struct, IComparable
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(column, out var dataColumn))
            {
                if (IndexInfo.IndexMappings.TryGetValue(dataColumn.ColumnHandle, out var indexMapping))
                {
                    var storageType = dataColumn.Type;

                    int rowHandle = -1;

                    rowHandle = IndexInfo.GetRowHandleStruct(this, storageType, indexMapping, ref value, ignoreDeleted);

                    if (rowHandle < 0)
                    {
                        return null;
                    }
                    
                    return GetRowInstance(rowHandle);
                }
                
                return TryGetRow(value, true, dataColumn);
            }

            return null;
        }

        public int GetRowHandle<T>(T value) where T : IComparable
        {
            var handle = IndexInfo.GetRowHandle(this, value);

            if (handle < 0 && MultiColumnIndexInfo.HasAny)
            {
                return MultiColumnIndexInfo.GetRowHandle(this, value);
            }

            return handle;
        }

        public int GetRowHandle<T>(string column, T value) where T : IComparable
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(column, out var dataColumn))
            {
                return GetRowHandle(dataColumn, value);
            }

            return -1;
        }

        public int GetRowHandle<T>(CoreDataColumn column, T value) where T : IComparable
        {
            var columnHandle = column.ColumnHandle;
            
            if (IndexInfo.IndexMappings.TryGetValue(columnHandle, out var indexMapping))
            {
                var storageType = DataColumnInfo.Columns[columnHandle].Type;

                return IndexInfo.GetRowHandle(this, storageType, indexMapping, value, false);
            }

            if (MultiColumnIndexInfo.HasAny)
            {
                var indexes = MultiColumnIndexInfo.GetColumnFirstIndex(columnHandle);

                if (indexes >= 0)
                {
                    var key = new IComparable[] { value };

                    var rowHandles = MultiColumnIndexInfo.GetRowHandles(indexes, key);

                    if (rowHandles.Count > 0)
                    {
                        var rowHandle = rowHandles[0];

                        rowHandles.Dispose();

                        return rowHandle;
                    }

                    rowHandles.Dispose();
                }
            }

            var dataItem = column.DataStorageLink;

            var ints = FullScanHandles(value, dataItem, columnHandle).Take(1).ToData();

            if (ints.Count == 1)
            {
                var rowHandle = ints[0];

                ints.Dispose();

                return rowHandle;
            }

            ints.Dispose();
            
            return -1;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IEnumerable<ICoreDataRowAccessor> GetRows<TVal>(int columnHandle, TVal value) where TVal : IComparable => GetRows<CoreDataRow, TVal>(GetColumn(columnHandle), value);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreDataRowReadOnlyAccessor> ICoreReadOnlyDataTable.GetRowsWhereNull(string column) => GetRowsWhereNull(column);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IEnumerable<ICoreDataRowAccessor> GetRowsWhereNull(int columnHandle) => GetRows<CoreDataRow, IComparable>(GetColumn(columnHandle), (IComparable)null);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreDataRowAccessor> ICoreDataTable.GetRowsWhereNull(string column) => GetRowsWhereNull(column);

        [NotNull]
        public IEnumerable<CoreDataRow> GetRowsWhereNull(string column) => GetRows<CoreDataRow, IComparable>(column, (IComparable)null);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreDataRowReadOnlyAccessor> ICoreReadOnlyDataTable.GetRows<TVal>(string column, TVal value) => GetRows<CoreDataRow, TVal>(column, value);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreDataRowAccessor> ICoreDataTable.GetRows<TVal>(string column, TVal value) => GetRows<CoreDataRow, TVal>(column, value);

        [NotNull]
        public IEnumerable<TRow> GetRows<TRow, TVal>(string column, TVal value) where TRow: CoreDataRow where TVal : IComparable 
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(column, out var dataColumn))
            {
                foreach (var row in GetRows<TRow, TVal>(dataColumn, value))
                {
                    yield return row;
                }
            }
        }

        public IEnumerable<TRow> GetRows<TRow, TVal>(CoreDataColumn column, TVal value) where TRow: CoreDataRow where TVal : IComparable 
        {
            if (column.HasIndex)
            {
                if (IndexInfo.IndexMappings.TryGetValue(column.ColumnHandle, out var indexMapping))
                {
                    foreach (var dataRow in GetRowsBySimpleIndex<TRow, TVal>(indexMapping, value))
                    {
                        yield return dataRow;
                    }
                }
            }
            else
            {
                if (MultiColumnIndexInfo.HasAny)
                {
                    var indexes = MultiColumnIndexInfo.GetColumnFirstIndex(column.ColumnHandle);

                    if (indexes >= 0)
                    {
                        var key = new TVal[] { value };

                        var rowHandles = MultiColumnIndexInfo.GetRowHandles(indexes, key);

                        foreach (var rowHandle in rowHandles)
                        {
                            if (StateInfo.GetRowState(rowHandle) != RowState.Detached)
                            {
                                yield return (TRow)GetRowInstance(rowHandle);
                            }
                        }

                        rowHandles.Dispose();

                        yield break;
                    }
                }

                var dataItem = column.DataStorageLink;

                foreach (var dataRow in FullScan<TRow, TVal>(value, dataItem, column.ColumnHandle))
                {
                    yield return dataRow;
                }
            }
        }

        [NotNull]
        public IEnumerable<int> FindManyHandles<T>(int columnHandle, T value, bool ignoreDeleted = true) where T : IComparable
        {
            var dataColumn = DataColumnInfo.Columns[columnHandle];
            
            if (dataColumn.HasIndex)
            {
                if (IndexInfo.IndexMappings.TryGetValue(columnHandle, out var indexMapping))
                {
                    var rowHandles = IndexInfo.GetRowHandles(indexMapping, value);

                    foreach (var rowHandle in rowHandles)
                    {
                        if (ignoreDeleted && (StateInfo.GetRowState(rowHandle) == RowState.Deleted))
                        {
                            continue;
                        }

                        if (StateInfo.IsNotRemoved(rowHandle))
                        {
                            yield return rowHandle;
                        }
                    }
                }
            }
            else
            {
                if (MultiColumnIndexInfo.HasAny)
                {
                    var indexes = MultiColumnIndexInfo.GetColumnFirstIndex(columnHandle);

                    if (indexes >= 0)
                    {
                        var key = new IComparable[1] { value };

                        var rowHandles = MultiColumnIndexInfo.GetRowHandles(indexes, key);

                        foreach (var rowHandle in rowHandles)
                        {
                            if (ignoreDeleted && (StateInfo.GetRowState(rowHandle) == RowState.Deleted))
                            {
                                continue;
                            }

                            if (StateInfo.IsNotRemoved(rowHandle))
                            {
                                yield return rowHandle;
                            }
                        }

                        rowHandles.Dispose();

                        yield break;
                    }
                }

                var dataItem = dataColumn.DataStorageLink;

                foreach (var rowHandle in FullScanHandles(value, dataItem, columnHandle))
                {
                    if (ignoreDeleted && StateInfo.GetRowState(rowHandle) == RowState.Deleted)
                    {
                        continue;
                    }

                    yield return rowHandle;
                }
            }
        }
        
        [NotNull]
        public IEnumerable<int> FindManyHandles(ColumnHandle[] columnsHandles, IComparable[] value, bool ignoreDeleted = true)
        {
            var indexesOfMany = MultiColumnIndexInfo.TryGetIndex(columnsHandles);
            
            if (indexesOfMany != null)
            {
                foreach (var rowHandle in indexesOfMany.GetRowHandles(value))
                {
                    if (ignoreDeleted && (StateInfo.GetRowState(rowHandle) == RowState.Deleted))
                    {
                        continue;
                    }

                    if (StateInfo.IsNotRemoved(rowHandle))
                    {
                        yield return rowHandle;
                    }
                }
            }
            else
            {
                //tod unit test
                
                Data<int> result = null;
                
                for (var i = 0; i < columnsHandles.Length; i++)
                {
                    var columnHandle = columnsHandles[i];
                    var dataColumn = DataColumnInfo.Columns[columnHandle.Handle];
                    
                    var val = value[i];
                    
                    if (result is not null)
                    {
                        var dataItem = dataColumn.DataStorageLink;

                        var subset = result;

                        result = FullScanHandles(val, dataItem, columnHandle.Handle, subset)
                            .Where(r => (ignoreDeleted && StateInfo.GetRowState(r) == RowState.Deleted) == false)
                            .ToData();
                        
                        subset.Dispose();
                    }
                    else
                    {
                        result = FindManyHandles(columnHandle.Handle, val, ignoreDeleted).ToData();
                    }

                    if (result.Any() == false)
                    {
                        break;
                    }
                }

                if (result != null)
                {
                    foreach (var rowHandle in result)
                    {
                        yield return rowHandle;
                    }
                }
            }
        }

        internal IEnumerable<TRow> FullScan<TRow, TVal>(TVal value, IDataItem dataItem, int columnHandle) where TRow: CoreDataRow where TVal : IComparable 
        {
            foreach (var rowHandle in FullScanHandles(value, dataItem, columnHandle))
            {
                if (StateInfo.IsNotDeleted(rowHandle))
                {
                    yield return (TRow)GetRowInstance(rowHandle);
                }
            }
        }

        protected virtual IEnumerable<int> FullScanHandles<T>(T value, IDataItem dataItem, int columnHandle)
            where T : IComparable
        {
            var dataColumn = GetColumn(columnHandle);

            if (value is null || value is "")
            {
                var stateInfoRowCount = StateInfo.RowCount;

                for (int rowHandle = 0; rowHandle < stateInfoRowCount; rowHandle++)
                {
                    if (dataItem.IsNull(rowHandle, dataColumn) && StateInfo.IsNotRemoved(rowHandle))
                    {
                        yield return rowHandle;
                    }
                }
            }
            else
            {
                foreach (var rowHandle in dataItem.Filter(value, dataColumn))
                {
                    if (StateInfo.IsNotRemoved(rowHandle))
                    {
                        yield return rowHandle;
                    }
                }
            }
        }
        
        protected virtual IEnumerable<int> FullScanHandles<T>(T value, IDataItem dataItem, int columnHandle, IEnumerable<int> rowHandles)
            where T : IComparable
        {
            var dataColumn = GetColumn(columnHandle);

            if (value is null || value is "")
            {
                foreach (var rowHandle in rowHandles)
                {
                    if (dataItem.IsNull(rowHandle, dataColumn) && StateInfo.IsNotRemoved(rowHandle))
                    {
                        yield return rowHandle;
                    }
                }
            }
            else
            {
                foreach (var rowHandle in rowHandles)
                {
                    var data = dataItem.GetData(rowHandle, dataColumn);

                    if (data != null && data.Equals(value))
                    {
                        if (StateInfo.IsNotRemoved(rowHandle))
                        {
                            yield return rowHandle;
                        }
                    }
                }
            }
        }

        [NotNull]
        internal IEnumerable<TRow> GetRowsBySimpleIndex<TRow, TVal>(int indexIndex, TVal value) where TRow: CoreDataRow where TVal : IComparable 
        {
            var rowHandles = IndexInfo.GetRowHandles(indexIndex, value);

            foreach (var rowHandle in rowHandles)
            {
                if (StateInfo.IsNotDeletedAndRemoved(rowHandle))
                {
                    yield return (TRow)GetRowInstance(rowHandle);
                }
            }
        }
        
        public enum StringIndexLookupType
        {
            Equals,
            StartsWith,
            EndsWith,
            Contains,
        }
        
        [NotNull]
        internal IEnumerable<TRow> GetRowsByStringIndex<TRow>(int indexIndex, string value, StringIndexLookupType type) where TRow: CoreDataRow
        {
            var rowHandles = IndexInfo.GetRowHandlesByStringIndex(indexIndex, value, type);

            foreach (var rowHandle in rowHandles)
            {
                if (StateInfo.IsNotDeletedAndRemoved(rowHandle))
                {
                    yield return (TRow)GetRowInstance(rowHandle);
                }
            }
        }

        [NotNull]
        internal IEnumerable<TRow> GetRowsByMultiIndex<TRow, TVal>(int indexIndex, Data<TVal> values) where TRow: CoreDataRow where TVal : IComparable 
        {
            var rowHandles = MultiColumnIndexInfo.GetRowHandles(indexIndex, values);

            foreach (var rowHandle in rowHandles)
            {
                if (StateInfo.IsNotDeletedAndRemoved(rowHandle))
                {
                    yield return (TRow)GetRowInstance(rowHandle);
                }
            }
            
            rowHandles.Dispose();
        }

        [CanBeNull]
        internal CoreDataRow GetRowByMultiIndex<TVal>(int indexIndex, TVal[] values, bool ignoreDeleted = true) where TVal : IComparable
        {
            var rowHandles = MultiColumnIndexInfo.GetRowHandles(indexIndex, values);

            if (rowHandles.Count == 0)
            {
                rowHandles.Dispose();
                
                return null;
            }

            foreach (var rowHandle in rowHandles)
            {
                if (rowHandle >= 0)
                {
                    if (ignoreDeleted && StateInfo.IsNotDeleted(rowHandle) == false)
                    {
                        continue;
                    }
                    
                    return GetRowInstance(rowHandle);
                }
            }
            
            rowHandles.Dispose();
            
            return null;
        }
        
        private CoreDataRow TryGetRow<TVal>(TVal value, bool ignoreDeleted, CoreDataColumn column) where TVal : IComparable
        {
            if (MultiColumnIndexInfo.HasAny)
            {
                var indexes = MultiColumnIndexInfo.GetColumnFirstIndex(column.ColumnHandle);

                if (indexes >= 0)
                {
                    var key = new IComparable[1] { value };

                    var rowHandles = MultiColumnIndexInfo.GetRowHandles(indexes, key);

                    if (rowHandles.Count > 0)
                    {
                        var rowHandle = rowHandles[0];

                        rowHandles.Dispose();

                        return GetRowInstance(rowHandle);
                    }

                    rowHandles.Dispose();
                }
            }

            var dataItem = column.DataStorageLink;

            foreach (var rowHandle in FullScanHandles(value, dataItem, column.ColumnHandle))
            {
                if (ignoreDeleted && StateInfo.IsNotDeleted(rowHandle) == false)
                {
                    continue;
                }

                return GetRowInstance(rowHandle);
            }

            return null;
        }
    }
}