using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Brudixy.Constraints;
using Brudixy.Converter;
using Brudixy.Interfaces;
using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy
{
    partial class CoreDataTable
    {
        public ICoreDataRowAccessor ImportRow(ICoreDataRowReadOnlyAccessor row)
        {
            var rows = new [] { row };

            var loadRows = LoadRows(rows, true);

            var dataRow = loadRows.FirstOrDefault();
            
            loadRows.Dispose();

            return dataRow;
        }
        
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]        
        void ICoreDataTable.LoadRows(IEnumerable<ICoreDataRowReadOnlyAccessor> dataRows, bool overrideExisting = false)
        {
            var loadRows = LoadRows(dataRows, overrideExisting);
            
            loadRows.Dispose();
        }

        public void ImportRows(IEnumerable<ICoreDataRowReadOnlyAccessor> dataRows)
        {
            var loadRows = ImportRowsCore(dataRows);
            loadRows.Dispose();
        }

        public Data<CoreDataRow> LoadRows(IEnumerable<ICoreDataRowReadOnlyAccessor> dataRows, bool overrideExisting = false)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot load new rows to the '{Name}' table because it is readonly.");
            }
            
            if(overrideExisting)
            {
                var rows = UpsertRows(dataRows);

                return rows;
            }

            if (StateInfo.RowStates.Storage.Count == 0 || DataColumnInfo.PrimaryKeyColumns.Length == 0)
            {
                return ImportRowsCore(dataRows);
              
            }
            else
            {
                return UpsertRows(dataRows);
            }
        }
        
        private Data<CoreDataRow> UpsertRows(IEnumerable<ICoreDataRowReadOnlyAccessor> dataRows)
        {
            var rows = new Data<CoreDataRow>();

            bool isFirst = true;
            int? multiIndex = null;
            bool useMap = true;
            string uniqueKey = null;

            Data<ICoreTableReadOnlyColumn> importedRowColumns = null;
            Data<string> columns = null;
            Data<string> indexedColumns = null;

            try
            {
                foreach (var rowToImport in dataRows)
                {
                    if (rowToImport.RowRecordState == RowState.Detached)
                    {
                        continue;
                    }

                    if (isFirst)
                    {
                        GetSchemaOptionsForImport(rowToImport, 
                            out importedRowColumns, 
                            out columns,
                            out indexedColumns,
                            out useMap);
                    }

                    IComparable keyValue = null;
                    IComparable[] multiColumnKeyValue = null;

                    GetKeysForImport(rowToImport,
                        ref uniqueKey,
                        ref keyValue,
                        ref multiIndex,
                        ref multiColumnKeyValue);

                    bool importNew = true;

                    if (uniqueKey != null || (multiColumnKeyValue != null && multiIndex.HasValue))
                    {
                        var existingOne = uniqueKey != null
                            ? GetRow(uniqueKey, keyValue, ignoreDeleted: false)
                            : GetRowByMultiIndex(multiIndex.Value, multiColumnKeyValue, ignoreDeleted: false);

                        if (existingOne != null)
                        {
                            importNew = false;

                            existingOne.CopyFrom(rowToImport);

                            SetupRowState(rowToImport.RowRecordState, existingOne.RowHandle);

                            rows.Add(existingOne);
                        }
                    }

                    if (importNew)
                    {
                        var newRow = AddRow();

                        if (newRow != null)
                        {
                            ImportNewRowCore(rowToImport, newRow);

                            rows.Add(newRow);
                        }
                    }

                    isFirst = false;
                }

                foreach (var dataRow in rows)
                {
                    OnNewRowAdded(dataRow.RowHandle);
                }
            }
            catch
            {
                foreach (var row in rows)
                {
                    RemoveRow(row.RowHandle);
                }

                throw;
            }
            finally
            {
                importedRowColumns?.Dispose();
                columns?.Dispose();
                indexedColumns?.Dispose();
            }

            return rows;
        }

        private void GetSchemaOptionsForImport(ICoreDataRowReadOnlyAccessor rowToImport, 
            out Data<ICoreTableReadOnlyColumn> importedRowColumns,
            out Data<string> columns,
            out Data<string> indexColumns,
            out bool useMap)
        {
            importedRowColumns = new (rowToImport.GetColumnCount());
            indexColumns = new ();

            foreach (var importRowColumn in rowToImport.GetColumns())
            {
                importedRowColumns.Add(importRowColumn);

                if (DataColumnInfo.ColumnMappings.TryGetValue(importRowColumn.ColumnName, out var thisTableColumnHandle))
                {
                    if (HasIndex(importRowColumn.ColumnName))
                    {
                        indexColumns.Add(importRowColumn.ColumnName);
                    }
                }
            }

            columns = new (importedRowColumns.Count);

            for (int i = 0; i < importedRowColumns.Count; i++)
            {
                columns.Add(importedRowColumns.ValueByRef(i).ColumnName);
            }

            var allColumnsAreCompatible = AllColumnsAreCompatible(columns.Count, importedRowColumns);

            useMap = allColumnsAreCompatible == false;
        }
        
        private bool AllColumnsAreCompatible(int columnsCount, Data<ICoreTableReadOnlyColumn> dataColumns)
        {
            bool allColumnsAreCompatible = true;

            for (int columnHandle = 0; columnHandle < ColumnCount && columnHandle < columnsCount; columnHandle++)
            {
                var importedRowColumn = dataColumns.ValueByRef(columnHandle);

                if (importedRowColumn.Type != DataColumnInfo.Columns[columnHandle].Type || string.Equals(importedRowColumn.ColumnName, DataColumnInfo.Columns[columnHandle].ColumnName, StringComparison.OrdinalIgnoreCase) == false)
                {
                    allColumnsAreCompatible = false;
                    break;
                }
            }
            return allColumnsAreCompatible;
        }

        private void GetKeysForImport(ICoreDataRowReadOnlyAccessor rowToImport,
            ref string uniqueKey, 
            ref IComparable keyValue,
            ref int? multiIndex,
            ref IComparable[] multiColumnKeyValue)
        {
            if (DataColumnInfo.PrimaryKeyColumns.Length == 1)
            {
                var fieldName = DataColumnInfo.PrimaryKeyColumns.First().ColumnName;

                if (uniqueKey == null || uniqueKey == fieldName)
                {
                    keyValue = (IComparable) rowToImport[fieldName];
                }

                if (uniqueKey == null)
                {
                    uniqueKey = fieldName;
                }
            }
            else if (DataColumnInfo.PrimaryKeyColumns.Length > 1)
            {
                var uniqueMultiColumnIndex = DataColumnInfo.PrimaryKeyColumns;

                var searchHandles = uniqueMultiColumnIndex.Select(s =>s.ColumnHandle).ToArray();
                
                multiIndex = MultiColumnIndexInfo.TryGetIndexIndex(searchHandles);

                multiColumnKeyValue = new IComparable[DataColumnInfo.PrimaryKeyColumns.Length];

                for (int i = 0; i < uniqueMultiColumnIndex.Length; i++)
                {
                    var indexesColumn = uniqueMultiColumnIndex[i];

                    multiColumnKeyValue[i] = (IComparable)rowToImport[indexesColumn.ColumnHandle];
                }
            }
        }

        private Data<CoreDataRow> ImportRowsCore(IEnumerable<ICoreDataRowReadOnlyAccessor> dataRows)
        {
            var rows = new Data<CoreDataRow>();

            try
            {
                Set<string> missingFields = null;

                bool first = true;

                foreach (var rowToImport in dataRows)
                {
                    if (rowToImport.RowRecordState == RowState.Detached)
                    {
                        continue;
                    }

                    if (first)
                    {
                        first = false;

                        foreach (var column in rowToImport.GetColumns())
                        {
                            if (DataColumnInfo.ColumnMappings.ContainsKey(column.ColumnName) == false)
                            {
                                missingFields ??= new (StringComparer.OrdinalIgnoreCase);

                                missingFields.Add(column.ColumnName);
                            }
                        }
                    }

                    var newRow = AddRow();

                    if (newRow != null)
                    {
                        ImportNewRowCore(rowToImport, newRow);

                        rows.Add(newRow);

                    }
                }

                missingFields?.Dispose();

                foreach (var row in rows)
                {
                    OnNewRowAdded(row.RowHandle);
                }
            }
            catch 
            {
                foreach (var row in rows)
                {
                    RemoveRow(row.RowHandle);
                }
                
                rows.Clear();

                throw;
            }

            return rows;
        }

        private CoreDataRow ImportNewRowCore(ICoreDataRowReadOnlyAccessor rowToImport, CoreDataRow newRow)
        {
            if (IsInitializing == false && GetIsInTransaction())
            {
                StateInfo.StartRowTransaction(newRow.RowHandleCore);
            }

            /*if (rowToImport["id"].GetInt32(0) == 19)
            {
                //
            }*/
            
            try
            {
                newRow.CopyAll(rowToImport);
            }
            catch (Exception e)
            {
                throw;
            }
            
            AddRowToIndexes(newRow);

            SetupRowState(rowToImport.RowRecordState, newRow.RowHandleCore);
            
            return newRow;
        }

        private void AddRowToIndexes(CoreDataRow newRow)
        {
            IndexBuilder initTableIndexBuilder = null;

            if (IndexInfo.HasAny)
            {
                if (IsInitializing)
                {
                    initTableIndexBuilder = new (this, newRow.RowHandleCore);
                }
                    
                foreach (var indexInfo in IndexInfo.Indexes)
                {
                    var dataColumn = DataColumnInfo.Columns[indexInfo.ColumnHandle];
                    
                    if (indexInfo.ReadyIndex.IsUnique)
                    {
                        if (newRow.IsNull(dataColumn))
                        {
                            throw new ConstraintException(
                                $"The unique indexed column {dataColumn.ColumnName} cell value can't be set null in {TableName} table.");
                        }
                    }

                    initTableIndexBuilder?
                        .SetValue(indexInfo.ColumnHandle, dataColumn.DataStorageLink.GetData(newRow.RowHandleCore, dataColumn));
                }
            }

            if (MultiColumnIndexInfo.HasAny)
            {
                if (IsInitializing)
                {
                    initTableIndexBuilder ??= new (this, newRow.RowHandleCore);
                }

                for (var index = 0; index < MultiColumnIndexInfo.Indexes.Count; index++)
                {
                    var indexInfo = MultiColumnIndexInfo.Indexes[index];
                    bool allNull = true;

                    foreach (var columnHandle in indexInfo.Columns)
                    {
                        var dataColumn = DataColumnInfo.Columns[columnHandle];
                        
                        if (newRow.IsNotNull(dataColumn))
                        {
                            allNull = false;
                        }

                        initTableIndexBuilder?.SetValue(columnHandle, dataColumn.DataStorageLink.GetData(newRow.RowHandleCore, dataColumn));
                    }

                    if (indexInfo.IsUnique)
                    {
                        if (allNull)
                        {
                            var cols = string.Join(";", indexInfo.Columns.Select(ch => DataColumnInfo.Columns[ch].ColumnName));

                            throw new ConstraintException(
                                $"The unique indexed '{cols}' columns can't be set null for {TableName} table. Multi value index at '{index}' index");
                        }
                    }
                }

                initTableIndexBuilder?.SetMultiColumnIndex();
            }
        }

        private int ImportRowCore(RowState rowState, object[] value, bool dirty = false)
        {
            OnNewRowAdding(out var isCancel);

            if (isCancel)
            {
                return -1;
            }

            var rowHandle = StateInfo.GetNewRowHandle(this);

            var count = DataColumnInfo.ColumnsCount;

            var acceptChanges = rowState == RowState.Unchanged;

            int skippedColumnCount = 0;

            var indexBuilder = new IndexBuilder(this, rowHandle);

            for (int columnHandle = 0; columnHandle < count; columnHandle++)
            {
                var dataColumn = GetColumn(columnHandle);
                
                if (IsReadOnlyColumn(dataColumn) == false)
                {
                    var cellVal = value[columnHandle - skippedColumnCount];

                    indexBuilder.SetValue(columnHandle, cellVal);

                    SetNewRowCellValue(dataColumn, cellVal, rowHandle, acceptChanges);
                }
                else if(dirty == false)
                {
                    skippedColumnCount++;
                }
            }

            indexBuilder.SetMultiColumnIndex();

            indexBuilder.Dispose();

            switch (rowState)
            {
                case RowState.Added:
                    StateInfo.SetAdded(rowHandle, GetTranId());
                    break;
                case RowState.Modified:
                    StateInfo.SetModified(rowHandle, GetTranId());
                    break;
                case RowState.Deleted:
                    StateInfo.SetDeleted(rowHandle, GetTranId());
                    break;
            }

            OnNewRowAdded(rowHandle);
            
            Interlocked.Increment(ref m_dataAge);

            if (GetIsInTransaction() == false)
            {
                CheckParentForeignKeyConstraints();
            }

            return rowHandle;
        }

        private void SetNewRowCellValue(CoreDataColumn column, object cellVal, int rowHandle, bool acceptChanges)
        {
            var dataItem = column.DataStorageLink;

            if (column.IsAutomaticValue)
            {
                if (cellVal == null)
                {
                    dataItem.AddNew(rowHandle, dataItem.NextAutoIncrementValue(column), column);
                }
                else
                {
                    dataItem.AddNew(rowHandle, cellVal, column);

                    UpdateMax(column, cellVal);
                }
            }
            else
            {
                dataItem.AddNew(rowHandle, cellVal, column);
            }

            if (acceptChanges)
            {
                dataItem.AcceptChanges(rowHandle, column);
            }
        }

        public int ImportRowDirty(RowState rowState, params object[] value) => ImportRowCore(rowState, value, true);

        public int ImportRow(RowState rowState, params object[] value) => ImportRowCore(rowState, value);
    }
}
