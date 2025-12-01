using System.Collections.Frozen;
using System.Diagnostics;
using Brudixy.Converter;
using Brudixy.EventArgs;
using Brudixy.Exceptions;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    [DebuggerDisplay("{Name}, Rows: {RowCount}, Columns: {ColumnCount}, PK: {PkDebug}, Thread: {SourceThread.Name}, Indexed: {IndexInfo.HasAny}, MI {MultiColumnIndexInfo.HasAny}, RO = {TableIsReadOnly}")]
    public partial class DataTable : CoreDataTable, IDataTable,  IEquatable<DataTable>,  IDataLockEventState
    {
        internal ValueInfo RowAnnotations => m_rowAnnotations ??= new ValueInfo();
        
        internal RowXPropertyInfoDataItem RowXPropertyAnnotations => m_rowXPropertyAnnotations ??= new RowXPropertyInfoDataItem(this);

        internal int RowCellAnnotationInitCount => RowCellAnnotations?.Count ?? 0;
        
        public IEnumerable<string> GetXPropertyInfos(int rowHandle)
        {
            if (rowHandle >= RowXPropertyAnnotations.Storage.Count)
            {
                return Array.Empty<string>();
            }

            var xPropertyInfos = RowXPropertyAnnotations.Storage[rowHandle];

            if (xPropertyInfos == null)
            {
                return Array.Empty<string>();
            }

            return xPropertyInfos.Keys;
        }
        
        [NotNull]
        public new IEnumerable<IDataTableRow> GetRowsWhereNull(string column)
        {
            return GetRows(column, (IComparable)null);
        }
        
        internal new IEnumerable<DataRow> GetAllRows()
        {
            var stateInfoRowStorageCount = StateInfo.RowStorageCount;

            for (int i = 0; i < stateInfoRowStorageCount; i++)
            {
                if (StateInfo.IsNotRemoved(i))
                {
                    var row = (DataRow)GetRowInstance(i);

                    yield return row;
                }
            }
        }
        
        internal new IEnumerable<DataRow> GetRows()
        {
            foreach (var rowsHandle in RowsHandles)
            {
                yield return (DataRow)GetRowInstance(rowsHandle);
            }
        }
        
        public T GetXPropertyAnnotation<T>(int rowHandle, string propCode, string key)
        {
            if (rowHandle >= RowXPropertyAnnotations.Storage.Count)
            {
                return TypeConvertor.ReturnDefault<T>();
            }

            var value = RowXPropertyAnnotations.GetData(rowHandle, propCode, key);
            
            return XPropertyValueConverter.TryConvert<T>(propCode, key, value);
        }
        
        public IReadOnlyDictionary<string, object> GetXPropertyAnnotationValues(int rowHandle, string propCode)
        {
            if (rowHandle >= RowXPropertyAnnotations.Storage.Count)
            {
                return null;
            }
            
            return RowXPropertyAnnotations.GetDataInfo(rowHandle, propCode);
        }

        public bool SetXPropertyAnnotation(int rowHandle, string propCode, string key, object value, int? tranId)
        {
            var prevValue = RowXPropertyAnnotations.GetData(rowHandle, propCode, key);

            var wasSet = RowXPropertyAnnotations.SetValue(rowHandle, propCode, key, prevValue, value, tranId);

            if (wasSet)
            {
                if (tranId.HasValue)
                {
                    StateInfo.SetTransactionRowChanged(rowHandle);
                }

                StateInfo.OnRowsAnnotationChange(rowHandle);
                
                if (m_rowMetaDataChangedEvent?.HasAny() ?? false)
                {
                    m_rowMetaDataChangedEvent.Raise(new DataRowMetaDataChangedArgs(this, rowHandle, propCode, key, value, RowMetadataType.XProperty));
                }
            }
            
            return wasSet;
        }

        public void ClearRowsAnnotations(string type)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot clear {type} rows' metadata for the '{Name}' table because is readonly.");
            }
            
            m_rowAnnotations?.ClearMetaInfo(type);
        }
        
        public void ClearRowsAnnotations()
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot clear rows' metadata for the '{Name}' table because is readonly.");
            }
            
            m_rowAnnotations?.Clear();
        }
        
        public void SetRowInfo(int rowHandle, string info, int? tranId)
        {
            SetRowAnnotation(rowHandle, info, tranId, ValueInfo.Info);
        }

        public void SetRowError(int rowHandle, string error, int? tranId)
        {
            SetRowAnnotation(rowHandle, error, tranId, ValueInfo.Error);
        }

        public void SetRowWarning(int rowHandle, string warning, int? tranId)
        {
            SetRowAnnotation(rowHandle, warning, tranId, ValueInfo.Warning);
        }
        
        public void SetRowFault(int rowHandle, string fault, int? tranId)
        {
            SetRowAnnotation(rowHandle, fault, tranId, ValueInfo.Fault);
        }

        public void SetRowAnnotation(int rowHandle, object value, int? tranId, string type)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup row {type} for the '{Name}' table because is readonly.");
            }

            if (rowHandle < 0 || (StateInfo.GetRowState(rowHandle) == RowState.Detached))
            {
                return;
            }

            var wasSet = RowAnnotations.SetRowAnnotation(this, rowHandle, value, tranId, type);

            if (wasSet)
            {
                if (tranId.HasValue)
                {
                    StateInfo.SetTransactionRowChanged(rowHandle);
                }
                
                StateInfo.OnRowsAnnotationChange(rowHandle);

                if (m_rowMetaDataChangedEvent?.HasAny() ?? false)
                {
                    m_rowMetaDataChangedEvent.Raise(new DataRowMetaDataChangedArgs(this, rowHandle, string.Empty, type, value, RowMetadataType.Row));
                }
            }
        }

        public T GetRowAnnotation<T>(int rowHandle, string key)
        {
            if (m_rowAnnotations == null || rowHandle < 0 || (StateInfo.GetRowState(rowHandle) == RowState.Detached))
            {
                return TypeConvertor.ReturnDefault<T>();
            }

            return m_rowAnnotations.GetRowAnnotation<T>(rowHandle, key);
        }

        public IEnumerable<(string type, object value)> GetRowAnnotations(int rowHandle)
        {
            if (m_rowAnnotations == null || rowHandle < 0 || (StateInfo.GetRowState(rowHandle) == RowState.Detached))
            {
                yield break;
            }

            foreach (var kv in m_rowAnnotations.RowAnnotations)
            {
                var value = (string)kv.Value.GetData(rowHandle);
                
                if(string.IsNullOrEmpty(value) == false)
                {
                    yield return (kv.Key, value);
                }
            }
        }
        
        public string GetRowInfo(int rowHandle) => GetRowAnnotation<string>(rowHandle, ValueInfo.Info);

        public string GetRowError(int rowHandle) => GetRowAnnotation<string>(rowHandle, ValueInfo.Error);

        public string GetRowFault(int rowHandle) => GetRowAnnotation<string>(rowHandle, ValueInfo.Fault);

        public string GetRowWarning(int rowHandle) => GetRowAnnotation<string>(rowHandle, ValueInfo.Warning);

        protected override bool OnBeforeSetXProperty<T>(string propertyName, ref T value)
        {
            if (HasXPropertyChangingHandler)
            {
                var oldValue = this.ExtProperties?.GetOrDefault(propertyName);

                var changingArgs = new DataTableXPropertyChangingArgs(this, propertyName, value, oldValue?.Original);
                
                XPropertyChanging.Raise(changingArgs);

                if (changingArgs.Cancel)
                {
                    return false;
                }
                
                value = changingArgs.GetNewValue<T>();

                return true;
            }

            return base.OnBeforeSetXProperty(propertyName, ref value);
        }

        protected override void OnSetXProperty<T>(string propertyName, T value)
        {
            base.OnSetXProperty(propertyName, value);

            if (HasXPropertyChangedHandler)
            {
                XPropertyChanged.Raise(new DataTableXPropertyChangedArgs(this, propertyName, value));
            }
        }

        protected override void RemapColumnHandles(Map<int, int> oldToNewMap)
        {
            base.RemapColumnHandles(oldToNewMap);
            
            CoreDataColumnInfo.ChangeHandles(oldToNewMap, RowCellAnnotations);
        }
        
        protected override void StopLoggingTransactionChanges(int rowHandle)
        {
            base.StopLoggingTransactionChanges(rowHandle);

            var rowCellAnnotations = RowCellAnnotations;

            if (rowCellAnnotations != null)
            {
                foreach (var annotation in rowCellAnnotations)
                {
                    annotation.StopLoggingTransactionChanges(rowHandle);
                }
            }
            
            m_rowXPropertyAnnotations?.StopLoggingTransactionChanges(rowHandle);
            m_rowAnnotations?.StopLoggingTransactionChanges(rowHandle);
        }
        
        protected override void OnRollbackRowTransaction(int rowHandle, int transaction, IReadOnlyCollection<int> columnChangedInfo)
        {
            base.OnRollbackRowTransaction(rowHandle, transaction, columnChangedInfo);
            
            var rowCellAnnotations = RowCellAnnotations;

            if (rowCellAnnotations != null)
            {
                foreach (var annotation in rowCellAnnotations)
                {
                    annotation.RollbackRowTransaction(rowHandle, transaction);
                }
            }

            CancelEditRowsOnRollbackTransaction();

            m_rowXPropertyAnnotations?.RollbackRowTransaction(rowHandle, transaction);
            
            m_rowAnnotations?.RollbackRowTransaction(rowHandle, transaction);
        }

        private void CancelEditRowsOnRollbackTransaction()
        {
            CancelEdit();
        }

        protected override CoreDataTableSerializer<T, V> CreateTableSerializer<T, V>(SerializerAdapter<T, V> serializer)
        {
            return new DataTableSerializer<T, V>(this, serializer);
        }

        protected override void OnRowRemoved(int rowHandle)
        {
            if (m_rowAnnotations?.HasData() ?? false)
            {
                m_rowAnnotations.OnRowRemoved(rowHandle);
            }
        }
        
        protected override void OnColumnRemoved(int columnHandle)
        {
            if (RowCellAnnotations != null)
            {
                var rowCellAnnotation = RowCellAnnotations[columnHandle];
                
                rowCellAnnotation.Dispose();

                RowCellAnnotations.RemoveAt(columnHandle);
            }
        }
        
        protected override CoreDataRow CreateRowInstance() 
        {
            return new DataRow();
        }

        internal RowCellAnnotation GetRowCellAnnotation(DataColumn column)
        {
            if (RowCellAnnotations is null)
            {
                RowCellAnnotations = new();
            }
            
            var dataCount = RowCellAnnotations.Count;

            RowCellAnnotations.Ensure(column.ColumnHandle + 1);

            for (int index = dataCount; index < RowCellAnnotations.Length; index++)
            {
                RowCellAnnotations[index] = new RowCellAnnotation(this);
            }

            return RowCellAnnotations[column.ColumnHandle];
        }
        
        public new IDataTableRowEnumerable<DataRow> Rows => new DataExtractor<DataRow>(this);
        
        public new IDataTableRowEnumerable<DataRow> AllRows => new DataExtractor<DataRow>(this, true);
        
        IDataTableRowEnumerable<IDataTableRow> IDataTable.Rows => new DataExtractor<IDataTableRow>(this);
        
        IDataTableRowEnumerable<IDataTableRow> IDataTable.AllRows => new DataExtractor<IDataTableRow>(this);

        [CanBeNull]
        public new DataRow GetRowByHandle(int rowHandle)
        {
            return (DataRow)base.GetRowInstance(rowHandle);
        }

        public new DataRow GetRow(int rowHandle)
        {
            return (DataRow)base.GetRow(rowHandle);
        }

        public new DataRow GetRowBy<T>(T value) where T : IComparable
        {
            return (DataRow)base.GetRowBy(value);
        }

        IDataTableRow IDataTable.GetRowBy<T>(T value)
        {
            return (IDataTableRow)base.GetRowBy(value);
        }

        public DataRow GetRow<T>(string column, T value) where T : IComparable
        {
            return (DataRow)base.GetRow(column, value);
        }
        
        public DataRow GetRow<T>(DataColumn dataColumn, T value, bool ignoreDeleted)
            where T : IComparable
        {
            return (DataRow)base.GetRow(dataColumn, value, ignoreDeleted);
        }

        IDataTableRow IDataTable.GetRow<T>(string column, T value)
        {
            return (IDataTableRow)base.GetRow(column, value);
        }

        [NotNull]
        public IEnumerable<DataRow> GetRows<T>(string column, T value) where T : IComparable
        {
            return base.GetRows<DataRow, T>(column, value);
        }

        public IEnumerable<DataRow> GetRows<TVal>(DataColumn column, TVal value) where TVal : IComparable
        {
            return base.GetRows<DataRow, TVal>(column, value);
        }

        [NotNull]
        IEnumerable<IDataTableReadOnlyRow> IReadOnlyDataTable.GetRows<T>(string column, T value) 
        {
            return base.GetRows<DataRow, T>(column, value);
        }
        
        [NotNull]
        IEnumerable<IDataTableRow> IDataTable.GetRows<T>(string column, T value)
        {
            return base.GetRows<DataRow, T>(column, value).OfType<IDataTableRow>();
        }

        [CanBeNull]
        public new DataRow GetRow<T>(string column, T value, bool ignoreDeleted = true) where T : IComparable
        {
            return (DataRow)base.GetRow(column, value, ignoreDeleted);
        }

        public new DataRow AddRow(IReadOnlyDictionary<string, object> values)
        {
            return (DataRow)base.AddRow(values);
        }

        public DataRow AddRow(IDataRowReadOnlyAccessor rowAccessor)
        {
            return (DataRow)base.AddRow(rowAccessor);
        }

        IDataTableRow IDataTable.AddRow(IDataRowReadOnlyAccessor rowAccessor)
        {
            return (DataRow)base.AddRow(rowAccessor);
        }

        public IDataTableRow ImportRow(IDataRowReadOnlyAccessor row)
        {
            return (IDataTableRow)base.ImportRow(row);
        }
        
        public new IDataTableRow ImportRow(ICoreDataRowReadOnlyAccessor row)
        {
            return (IDataTableRow)base.ImportRow(row);
        }

        [NotNull]
        public new DataRowContainer NewRow(IReadOnlyDictionary<string, object> values = null)
        {
            return (DataRowContainer)base.NewRow(values);
        }
        
        [NotNull]
        IDataRowContainer IDataTable.NewRow(IReadOnlyDictionary<string, object> values = null)
        {
            return (DataRowContainer)base.NewRow(values);
        }
        
        protected override CoreDataRowContainer CreateContainerInstance()
        {
            return new DataRowContainer();
        }

        protected override CoreContainerDataPropsBuilder CreateContainerDataPropsBuilderInstance()
        {
            return new ContainerDataPropsBuilder();
        }

        [DebuggerStepThrough]
        [NotNull]
        public new DataColumn GetColumn(int columnHandle)
        {
            return GetDataColumnInstance(columnHandle);
        }

        public override void ClearRowsXProperties()
        {
            base.ClearRowsXProperties();
            
            m_rowXPropertyAnnotations?.Clear(null);
        }
      
        [DebuggerStepThrough]
        [NotNull]
        public new DataColumn GetColumn(string name)
        {
            return (DataColumn)base.GetColumn(name);
        }
        
        protected override CoreDataColumn CreateColumnInstance(CoreDataColumnObj columnContainer)
        {
            return new DataColumn(this, (DataColumnObj)columnContainer);
        }

        protected new DataColumn GetDataColumnInstance(int columnHandle)
        {
            return (DataColumn)base.GetDataColumnInstance(columnHandle);
        }
        
        //do not copy
        internal int TableEventLockCount;

        public bool AreEventsLocked => TableEventLockCount > 0;

        public IDataLockEventState LockEvents()
        {
            LockDatasetEvents();
            
            TableEventLockCount++;

            return this;
        }

        void IDataLockEventState.UnlockEvents()
        {
            UnlockDatasetEvents();
            
            TableEventLockCount--;

            if (AreEventsLocked == false)
            {
                OnEventsUnlocked();
            }
        }
      

        protected override IEnumerable<int> FullScanHandles<T>(T value, IDataItem dataItem, int columnHandle)
        {
            var dataColumn = DataColumnInfo.GetColumn(columnHandle);
            
            if (dataColumn.ExpressionLink != null && dataColumn.FixType == DataColumnType.Expression)
            {
                for (int rowHandle = 0; rowHandle < StateInfo.RowCount; rowHandle++)
                {
                    if (StateInfo.IsNotRemoved(rowHandle))
                    {
                        var expressionValue = ExpressionValuesCache?.GetExpressionValue(columnHandle, rowHandle) as IComparable;

                        if (value is null && expressionValue is null || (value is not null && value.CompareTo(expressionValue) == 0))
                        {
                            yield return rowHandle;
                        }
                    }
                }
            }
            else
            {
                foreach (var rowHandle in base.FullScanHandles<T>(value, dataItem, columnHandle))
                {
                    yield return rowHandle;
                }
            }
        }
        
        protected override IEnumerable<int> FullScanHandles<T>(T value, IDataItem dataItem, int columnHandle, IEnumerable<int> rowHandles)
        {
            var dataColumn = DataColumnInfo.GetColumn(columnHandle);
            
            if (dataColumn.ExpressionLink != null && dataColumn.FixType == DataColumnType.Expression)
            {
                foreach (var rowHandle in rowHandles)
                {
                    if (StateInfo.IsNotRemoved(rowHandle))
                    {
                        var expressionValue = ExpressionValuesCache?.GetExpressionValue(columnHandle, rowHandle) as IComparable;

                        if (value is null && expressionValue is null || (value is not null && value.CompareTo(expressionValue) == 0))
                        {
                            yield return rowHandle;
                        }
                    }
                }
            }
            else
            {
                foreach (var rowHandle in base.FullScanHandles<T>(value, dataItem, columnHandle, rowHandles))
                {
                    yield return rowHandle;
                }
            }
        }

        public IEnumerable<IDataTableColumn> PrimaryKey => base.PrimaryKey.OfType<IDataTableColumn>();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableRow> IDataTable.Select(string filter)
        {
            return Select<IDataTableRow>(filter);
        }

        IDataTableRow IDataTable.GetRow<T>(int columnHandle, T value)
        {
            return (DataRow)base.GetRow(new ColumnHandle(columnHandle), value);
        }

        IEnumerable<IDataTableRow> IDataTable.GetRows<T>(int columnHandle, T value)
        {
            return base.GetRows(columnHandle, value).OfType<DataRow>();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]

        IEnumerable<IDataTableReadOnlyRow> IReadOnlyDataTable.Select(string filter)
        {
            return Select<IDataTableReadOnlyRow>(filter);
        }

        public IEnumerable<T> Select<T>(string filter) where T: IDataRowReadOnlyAccessor
        {
            if (string.IsNullOrEmpty(filter))
            {
                return Rows.OfType<T>();
            }

            var sl = new Select(this, filter);

            return sl.SelectRows<T>();
        }

        protected override void OnClearColumns()
        {
            base.OnClearColumns();
            
            ClearExpressionsCache();
        }
        
        public DataTable()
        {
            ExpressionValuesCache = new DataExpressionCache(this);
            
            DataRowType = typeof(DataRow);

            DataTableType = typeof(DataTable);
        }
        
        public DataTable(string tableName) : base(tableName)
        {
            ExpressionValuesCache = new DataExpressionCache(this);
            
            DataRowType = typeof(DataRow);
            
            DataTableType = typeof(DataTable);
        }
        
        public DataTable(DataTable ds) : base(ds)
        {
            ExpressionValuesCache = new DataExpressionCache(this);

            DataRowType = typeof(DataRow);
            
            DataTableType = typeof(DataTable);
        }

        protected override CoreDataColumnInfo CreateDataColumnInfo() => new DataColumnInfo();

        internal new DataColumnInfo DataColumnInfo => (DataColumnInfo)base.DataColumnInfo;

        public void SetColumnCaption(int columnHandle, string value)
        {
            if (AreColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column of '{Name}' table because columns are readonly."); 
            }
                    
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{DataColumnInfo.Columns[columnHandle].ColumnName}' column of the '{Name}' table caption because it is readonly.");
            }
            
            DataColumnInfo.SetCaption(columnHandle, value);

            OnMetadataChanged();
        }
        
        public override bool IsReadOnlyColumn(CoreDataColumn column) => ((DataColumn)column).IsReadOnly;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTable IDataTable.Copy() => Copy();

        IDataTableReadOnlyColumn IReadOnlyDataTable.GetColumn(string name) => GetColumn(name);

        IDataTableReadOnlyColumn IReadOnlyDataTable.TryGetColumn(string name) => (IDataTableReadOnlyColumn)TryGetColumn(name);

        IDataTableColumn IDataTable.NewColumn() => (IDataTableColumn)NewColumn();

        IReadOnlyDataTable IReadOnlyDataTable.Copy() => (DataTable)base.Copy();

        IDataTableColumn IDataTable.GetColumn(string name) => (DataColumn)base.GetColumn(name);

        IDataTableColumn IDataTable.TryGetColumn(string name) => (DataColumn)base.TryGetColumn(name);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTable IDataTable.Clone() => (DataTable)CloneCore(null, withData: false);

        public new IDataTable GetChanges() => (IDataTable)base.GetChanges();

        public DataTable Copy(Thread thread = null) => (DataTable)CloneCore(thread);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataTable ICoreDataTable.Clone() => Clone();
        
        public new DataTable Clone() => (DataTable)CloneCore(null, withData: false);
        
        public new DataTable CreatePartitionTable(Thread thread = null) => (DataTable)base.CreatePartitionTable(thread);

        protected override void ResetTransactionAggregatedEvents(int rowHandle,
            int transaction,
            IReadOnlyCollection<int> columnChangedInfo,
            IReadOnlyCollection<string> xPropChangedInfo)  
        {
            if (m_columnChangedAggregated is not null)
            {
                if (m_columnChangedAggregated.TryGetValue(rowHandle, out var changes))
                {
                    foreach (var columnHandle in columnChangedInfo)
                    {
                        if (changes.TryGetValue(columnHandle, out var columnChanges))
                        {
                            columnChanges.RemoveAll(transaction, c => c.TranId);

                            if (columnChanges.Count == 0)
                            {
                                columnChanges.Dispose();

                                changes.Remove(columnHandle);
                            }
                        }
                    }

                    if (changes.Count == 0)
                    {
                        changes.Dispose();

                        m_columnChangedAggregated.Remove(rowHandle);
                    }
                }
            }

            if (m_rowXPropertyChangedAggregated is not null)
            {
                if (m_rowXPropertyChangedAggregated.TryGetValue(rowHandle, out var changes))
                {
                    foreach (var xProp in xPropChangedInfo)
                    {
                        if (changes.TryGetValue(xProp, out var xPropertyChanges))
                        {
                            xPropertyChanges.RemoveAll(transaction, c => c.TranId);
                            
                            if (xPropertyChanges.Count == 0)
                            {
                                xPropertyChanges.Dispose();

                                changes.Remove(xProp);
                            }
                        }
                    }
                    
                    if (changes.Count == 0)
                    {
                        changes.Dispose();

                        m_rowXPropertyChangedAggregated.Remove(rowHandle);
                    }
                }
            }

            if (m_rowChangedAggregated is not null)
            {
                m_rowChangedAggregated.RemoveAll(transaction, c => c.tran);
            }
            
            if (m_rowDeletedHandlesAggregated is not null)
            {
                m_rowDeletedHandlesAggregated.RemoveAll(transaction, c => c.tran);
            }
            
            if (m_addedRowHandlesAggregated is not null)
            {
                m_addedRowHandlesAggregated.RemoveAll(transaction, c => c.tran);
            }
        }
        
        protected override void OnEventsUnlocked()
        {
            RaiseMultiRowsAdded();

            RaiseMultiRowsDeleted();

            RaiseMultiColumnChanged();

            RaiseMultiRowXPropertyChanged();

            RaiseMultiRowsChanged();
        }

        IEnumerable<IDataTableRow> IDataTable.GetRowsWhereNull(int columnHandle)
        {
            return base.GetRowsWhereNull(columnHandle).OfType<DataRow>();
        }

        IDataTableReadOnlyRow IReadOnlyDataTable.GetRow<T>(int columnHandle, T value)
        {
            return (DataRow)base.GetRow<T>(new ColumnHandle(columnHandle), value);
        }

        IEnumerable<IDataTableReadOnlyRow> IReadOnlyDataTable.GetRows<T>(int columnHandle, T value)
        {
            return base.GetRows<T>(columnHandle, value).OfType<IDataTableReadOnlyRow>();
        }

        IEnumerable<IDataTableReadOnlyRow> IReadOnlyDataTable.GetRowsWhereNull(int columnHandle)
        {
            return base.GetRowsWhereNull(columnHandle).OfType<DataRow>();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyColumn> IReadOnlyDataTable.Columns => GetColumns();
        
        public void ResetAggregatedEvents()
        {
            m_columnChangedAggregated?.Dispose();
            m_rowXPropertyChangedAggregated?.Dispose();

            m_columnChangedAggregated = null;
            m_rowXPropertyChangedAggregated = null;
        }
        
        public void ResetAggregatedEvents(int rowHandle)
        {
            if (m_columnChangedAggregated != null)
            {
                m_columnChangedAggregated.GetOrDefault(rowHandle)?.Dispose();
                m_columnChangedAggregated.Remove(rowHandle);
            }
            
            if (m_rowXPropertyChangedAggregated != null)
            {
                m_rowXPropertyChangedAggregated.GetOrDefault(rowHandle)?.Dispose();
                m_rowXPropertyChangedAggregated.Remove(rowHandle);
            }
        }

        public void UnlockRowEvents(DataRow row)
        {
            StateInfo.UnlockRowEvents(row.RowHandleCore);
                
            if (AreEventsLocked == false || StateInfo.IsRowEventLocked(row.RowHandleCore) == false)
            {
                RaiseMultiColumnChanged(row.RowHandleCore);
                RaiseMultiRowXPropertyChanged(row.RowHandleCore);
                RaiseMultiRowsChanged(row.RowHandleCore);
            }
        }
        
        protected override void OnBeforeNewRowValueSet(int columnHandle, Data<object> data, ref bool canContinue)
        {
            if (HasCellValueRequestingHandler(columnHandle))
            {
                var requestingArgs = new EmptyRowCellValueRequestingArgs
                {
                    ColumnName = DataColumnInfo.Columns[columnHandle].ColumnName,
                    Table = new WeakReference<DataTable>(this)
                };

                RaiseCellValueRequesting(columnHandle, requestingArgs);

                data[columnHandle] = requestingArgs.Value;

                canContinue = false;
            }
        }
        
        protected override void OnNewRowAdding(out bool isCancel)
        {
            isCancel = false;

            if (HasDataRowAddingHandler)
            {
                var rowAddingArgs = new DataRowAddingArgs { RowHandle = -1, Table = new WeakReference<DataTable>(this) };

                RaiseRowAdding(rowAddingArgs);

                isCancel = rowAddingArgs.IsCancel;
            }
        }
        
        protected override void OnNewRowAdded(int rowHandle)
        {
            if (AreEventsLocked == false)
            {
                if (HasDataRowAddedHandler)
                {
                    var rowAddingArgs = new DataRowAddedArgs(this, rowHandle);

                    RaiseRowAdded(rowAddingArgs);
                }

                EvaluateExpressions(rowHandle);
            }
            else
            {
                m_addedRowHandlesAggregated ??= new();

                m_addedRowHandlesAggregated.Add((rowHandle, GetTranId()));
            }
        }
        
        protected override void BeforeXPropertySet(int rowHandle, 
            string propertyCode, 
            ref object value, 
            object prevValue,
            out bool canContinue)
        {
            canContinue = true;

            if (HasRowXPropertyChangingEvent && AreEventsLocked == false && StateInfo.IsRowEventLocked(rowHandle) == false)
            {
                var changingArgs = new DataRowXPropertyChangingArgs
                {
                    Value = value,
                    PrevValue = prevValue,
                    PropertyCode = propertyCode,
                    RowHandle = rowHandle
                };

                RaiseRowXPropertyChanging(changingArgs);

                canContinue = changingArgs.IsCancel == false;

                value = changingArgs.Value;
            }
        }
        
        protected override void OnRowXPropertySet(int rowHandle, string propertyCode, object value, object prevValue)
        {
            base.OnRowXPropertySet(rowHandle, propertyCode, value, prevValue);
            
            if (HasRowXPropertyChangedEvent)
            {
                TryRaiseRowXPropertyChanged(rowHandle, propertyCode, prevValue, value);
            }

            if (this.m_dataRowChanged?.HasAny() ?? false)
            {
                this.TryRaiseDataRowChanged(rowHandle);
            }
        }
        
        protected override void OnDeletedRows(IReadOnlyList<int> rowHandles)
        {
            if (HasDataRowDeletedHandler)
            {
                TryRaiseRowDeleted(rowHandles);
            }
        }
        
        protected override bool OnDeleteRows(IReadOnlyList<int> rowHandles)
        {
            if (HasDataRowDeletingHandler)
            {
                var deletingArgs = new DataRowDeletingArgs(this, rowHandles.ToData());

                RaiseRowDeleting(deletingArgs);

                return deletingArgs.IsCancel;
            }

            return false;
        }

        protected override void OnBeforeRowColumnSet(
             int rowHandle, 
             CoreDataColumn column, 
             ref object value, 
             object prevValue,
             ref bool canContinue, 
             ref string cancelExceptionMessage, 
             ref Map<int, object> cascadePrevValues,
             bool isNotInit)
        {
            if (isNotInit)
            {
                if (value != prevValue)
                {
                    FillCascadePrevValues(rowHandle, column, ref cascadePrevValues);
                }
            }

            if (isNotInit && TableEventLockCount == 0 && StateInfo.IsRowEventLocked(rowHandle) == false)
            {
                var columnName = column.ColumnName;
                
                var hasCustomDataColumnColumnChangingHandler =
                    m_columnChangingHandlers?.GetOrDefault(column.ColumnHandle)?.SubscribeCount > 0;
                
                if (hasCustomDataColumnColumnChangingHandler)
                {
                    var result = RaiseColumnChangingHandler(column,
                        rowHandle,
                        columnName,
                        value,
                        prevValue);

                    if (result.HasValue)
                    {
                        canContinue = result.Value.isCancel == false && string.IsNullOrEmpty(result.Value.exceptionMessage);
                        cancelExceptionMessage = result.Value.exceptionMessage;
                        value = result.Value.newValue;
                    }
                }

                if (canContinue && (m_columnChanging?.HasAny() ?? false))
                {
                    var changingArgs = new DataColumnChangingArgs
                    {
                        RowHandle = rowHandle,
                        ColumnHandle = column.ColumnHandle,
                        ColumnName = columnName,
                        NewValue = value,
                        PrevValue = prevValue
                    };

                    RaiseColumnChanging(changingArgs);

                    value = changingArgs.NewValue;
                    canContinue = changingArgs.IsCancel == false && string.IsNullOrEmpty(changingArgs.ExceptionMessage);
                    cancelExceptionMessage = changingArgs.ExceptionMessage;
                }
                
                if (canContinue)
                {
                    OnDependantExpressionColumnChanging(rowHandle, columnName, value, ref canContinue, ref cancelExceptionMessage, ref cascadePrevValues);
                }
            }
        }

        private void FillCascadePrevValues(int rowHandle, CoreDataColumn column, ref Map<int, object> cascadePrevValues)
        {
            var columnName = column.ColumnName;

            if (DataColumnInfo.ColumnContainsInExpressionMap?.TryGetValue(columnName, out var expressionColHandles) ?? false)
            {
                foreach (var expressionColHandle in expressionColHandles)
                {
                    var hasExpressionCustomDataColumnColumnChangingHandler = m_columnChangingHandlers?
                        .GetOrDefault(expressionColHandle)?
                        .SubscribeCount > 0;
                    
                    var hasExpressionCustomDataColumnColumnChangedHandler = m_columnChangedHandlers?
                        .GetOrDefault(expressionColHandle)?
                        .SubscribeCount > 0;
                    
                    var hasAnyChangingSubscribers = m_columnChanging?.HasAny() ?? false;
                    var hasAnyChangedSubscribers = m_columnChanged?.HasAny() ?? false;

                    if (hasExpressionCustomDataColumnColumnChangingHandler || hasExpressionCustomDataColumnColumnChangedHandler|| hasAnyChangingSubscribers || hasAnyChangedSubscribers)
                    {
                        var prevExpressionValue = this.GetRowByHandle(rowHandle)?[new ColumnHandle(expressionColHandle)];

                        if (cascadePrevValues == null)
                        {
                            cascadePrevValues = new();
                        }
                        
                        cascadePrevValues[expressionColHandle] = prevExpressionValue;
                    }
                }
            }
        }

        private void OnDependantExpressionColumnChanging(int rowHandle,
            string columnName,
            object columnNewValue,
            ref bool canContinue,
            ref string cancelExceptionMessage,
            ref Map<int, object> cascadePrevValues)
        {
            if (DataColumnInfo.ColumnContainsInExpressionMap?.TryGetValue(columnName, out var expressionColHandles) ?? false)
            {
                var testValues = _.Map((columnName, columnNewValue));

                foreach (var expressionColHandle in expressionColHandles)
                {
                    var hasExpressionCustomDataColumnColumnChangingHandler = m_columnChangingHandlers?.GetOrDefault(expressionColHandle)?.SubscribeCount > 0;
                    var hasAnyChangingSubscribers = m_columnChanging?.HasAny() ?? false;

                    if (hasExpressionCustomDataColumnColumnChangingHandler || hasAnyChangingSubscribers)
                    {
                        var dataColumn = DataColumnInfo.GetColumn(expressionColHandle);
                        
                        var expressionColumnName = dataColumn.ColumnName;

                        var prevExpressionValue = cascadePrevValues?.GetOrDefault(expressionColHandle);

                        var newExpressionValue = dataColumn.ExpressionLink?.Evaluate(rowHandle, testValues);

                        if (newExpressionValue == prevExpressionValue)
                        {
                            continue;
                        }

                        if (hasExpressionCustomDataColumnColumnChangingHandler)
                        {
                            var result = RaiseColumnChangingHandler(dataColumn,
                                rowHandle,
                                expressionColumnName,
                                newExpressionValue,
                                prevExpressionValue);

                            if (result.HasValue)
                            {
                                canContinue = result.Value.isCancel == false;
                                cancelExceptionMessage = result.Value.exceptionMessage;
                            }
                        }

                        if (canContinue && hasAnyChangingSubscribers)
                        {
                            var changingArgs = new DataColumnChangingArgs
                            {
                                RowHandle = rowHandle,
                                ColumnHandle = expressionColHandle,
                                ColumnName = expressionColumnName,
                                NewValue = newExpressionValue,
                                PrevValue = prevExpressionValue
                            };

                            RaiseColumnChanging(changingArgs);

                            canContinue = changingArgs.IsCancel == false && string.IsNullOrEmpty(changingArgs.ExceptionMessage);
                            cancelExceptionMessage = changingArgs.ExceptionMessage;
                        }

                        if (canContinue == false)
                        {
                            return;
                        }
                    }
                }
            }
        }

        protected override void OnBeforeRowColumnSet<T>(int rowHandle,
            CoreDataColumn column,
            ref T value,
            T prevValue,
            ref bool canContinue,
            ref string cancelExceptionMessage,
            ref Map<int, object> cascadePrevValues,
            bool isNotInit)
        {
            if (isNotInit)
            {
                FillCascadePrevValues(rowHandle, column, ref cascadePrevValues);
            }
            
            if (isNotInit && TableEventLockCount == 0 && StateInfo.IsRowEventLocked(rowHandle) == false)
            {
                var columnName = column.ColumnName;
                
                var hasCustomDataColumnColumnChangingHandler =
                    m_columnChangingHandlers?.GetOrDefault(column.ColumnHandle)?.SubscribeCount > 0;

                if (hasCustomDataColumnColumnChangingHandler)
                {
                    var result = RaiseColumnChangingHandler(column, rowHandle, columnName, value, prevValue);
                    
                    if (result.HasValue)
                    {
                        value = Brudixy.TypeConvertor.ConvertValue<T>(result.Value.newValue, columnName, TableName, column.Type, column.TypeModifier,nameof(OnBeforeRowColumnSet));
                        canContinue = result.Value.isCancel == false;
                        cancelExceptionMessage = result.Value.exceptionMessage;
                    }
                }

                if (canContinue && (m_columnChanging?.HasAny() ?? false))
                {
                    var changingArgs = new DataColumnChangingArgs
                    {
                        RowHandle = rowHandle,
                        ColumnHandle = column.ColumnHandle,
                        ColumnName = columnName,
                        NewValue = value,
                        PrevValue = prevValue
                    };

                    RaiseColumnChanging(changingArgs);

                    value = Brudixy.TypeConvertor.ConvertValue<T?>(changingArgs.NewValue, columnName, TableName, column.Type, column.TypeModifier, nameof(OnBeforeRowColumnSet));
                    canContinue = changingArgs.IsCancel == false && string.IsNullOrEmpty(changingArgs.ExceptionMessage);
                    cancelExceptionMessage = changingArgs.ExceptionMessage;
                }

                if (canContinue)
                {
                    OnDependantExpressionColumnChanging(rowHandle, columnName, value, ref canContinue, ref cancelExceptionMessage, ref cascadePrevValues);
                }
            }
        }
        
        protected override void OnRowChanged(CoreDataColumn column, int rowHandle, object value, object prevValue,
            Map<int, object> cascadePrevValues)
        {
            base.OnRowChanged(column, rowHandle, value, prevValue, cascadePrevValues);
            
            EvaluateExpressions(rowHandle);
            
            if (m_columnChangedHandlers?.GetOrDefault(column.ColumnHandle)?.SubscribeCount > 0)
            {
                TryRaiseCustomColumnChanged(rowHandle, column, prevValue, value);
            }

            var hasColumnChangedHandlers = m_columnChanged?.HasAny() ?? false;
            
            if (hasColumnChangedHandlers)
            {
                TryRaiseColumnChanged(rowHandle, column, prevValue, value);
            }
            
            if (cascadePrevValues != null && cascadePrevValues.Count > 0)
            {
                TryRaiseCascadeColumChanged(rowHandle, cascadePrevValues, hasColumnChangedHandlers);
            }

            if (m_dataRowChanged?.HasAny() ?? false)
            {
                TryRaiseDataRowChanged(rowHandle);
            }
        }
        
        protected override void OnRowChanged<T>(CoreDataColumn column, int rowHandle, T? value, T? prevValue,
            Map<int, object> cascadePrevValues)
        {
            base.OnRowChanged(column, rowHandle, value, prevValue, cascadePrevValues);
            
            EvaluateExpressions(rowHandle);

            if (m_columnChangedHandlers?.GetOrDefault(column.ColumnHandle)?.SubscribeCount > 0)
            {
                TryRaiseCustomColumnChanged(rowHandle, column, prevValue, value);
            }

            var hasColumnChangedHandlers = m_columnChanged?.HasAny() ?? false;
            
            if (hasColumnChangedHandlers)
            {
                TryRaiseColumnChanged(rowHandle, column, prevValue, value);
            }

            if (cascadePrevValues != null && cascadePrevValues.Count > 0)
            {
                TryRaiseCascadeColumChanged(rowHandle, cascadePrevValues, hasColumnChangedHandlers);
            }

            if (m_dataRowChanged?.HasAny() ?? false)
            {
                TryRaiseDataRowChanged(rowHandle);
            }
        }

        private void TryRaiseCascadeColumChanged(int rowHandle,  Map<int, object> cascadePrevValues, bool hasColumnChangedHandlers)
        {
            var dataRow = GetRowInstance(rowHandle);

            foreach (var kValue in cascadePrevValues)
            {
                var hasCustomHandler = m_columnChangedHandlers?.GetOrDefault(kValue.Key)?.SubscribeCount > 0;

                if (hasCustomHandler || hasColumnChangedHandlers)
                {
                    var dataColumn = GetColumn(kValue.Key);

                    var newValue = dataRow[dataColumn];

                    if (hasCustomHandler)
                    {
                        TryRaiseCustomColumnChanged(rowHandle, dataColumn, kValue.Value, newValue);
                    }

                    if (hasColumnChangedHandlers)
                    {
                        TryRaiseColumnChanged(rowHandle, dataColumn, kValue.Value, newValue);
                    }
                }
            }
            
            cascadePrevValues.Dispose();
        }

        public void SetColumnReadOnly(int columnHandle, bool value)
        {
            if (AreColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column of '{Name}' table because columns are readonly."); 
            }
                    
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{DataColumnInfo.Columns[columnHandle].ColumnName}' column of the '{Name}' table column readonly flag because table is readonly.");
            }
            
            if (IsReadOnly)
            {
                return;
            }
            
            DataColumnInfo.SetReadOnlyColumn(columnHandle, value);

            OnMetadataChanged();
        }

        protected void ClearExpressionsCache()
        {
            ExpressionValuesCache?.Dispose();

            ExpressionValuesCache = new DataExpressionCache(this);
        }

        public DataColumn AddColumn([NotNull]DataColumnContainer column)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new  '{column.ColumnName}' column to the '{Name}' table because it is readonly.");
            }

            return AddColumn(column.ColumnName,
                column.Type,
                column.TypeModifier,
                column.DataType,
                column.Caption,
                column.IsAutomaticValue ? true : new bool?(),
                column.IsReadOnly ? true : new bool?(),
                column.IsUnique ? true : new bool?(),
                column.Expression,
                column.MaxLength,
                column.DefaultValue,
                column.IsBuiltin,
                column.IsServiceColumn,
                column.AllowNull,
                column.GetXProperties());
        }
        
        public IDataTableColumn AddColumn([NotNull]IDataTableReadOnlyColumn column)
        {
            if (column is DataColumnContainer dataColumn)
            {
                return AddColumn(dataColumn);
            }

            if (column is CoreDataColumnContainer codeDataColumn)
            {
                return (IDataTableColumn)AddColumn(codeDataColumn);
            }

            IReadOnlyDictionary<string, object> xProps = null;

            if (column.XProperties.Any())
            {
                var x = new Map<string, object>(StringComparer.OrdinalIgnoreCase);
                
                foreach (var property in column.XProperties)
                {
                    x[property] = column.GetXProperty<object>(property);
                }

                xProps = x;
            }

            return AddColumn(
                column.ColumnName,
                column.Type,
                column.TypeModifier,
                column.DataType,
                displayName: column.Caption,
                auto: column.IsAutomaticValue,
                readOnly: column.IsReadOnly,
                unique: column.IsUnique,
                dataExpression: column.Expression,
                columnMaxLength: column.MaxLength,
                defaultValue: column.DefaultValue,
                xProps: xProps);
        }

        IDataTableColumn IDataTable.AddColumn(
            string columnName,
            TableStorageType valueType = TableStorageType.String,
            TableStorageTypeModifier valueTypeModifier = TableStorageTypeModifier.Simple,
            Type type = null,
            string displayName = null,
            bool? auto = null,
            bool? readOnly = null,
            bool? unique = null,
            string dataExpression = null,
            uint? columnMaxLength = null,
            object defaultValue = null,
            bool builtin = false,
            bool serviceColumn = false,
            bool allowNull = true,
            IReadOnlyDictionary<string, object> xProps = null)
        {
            return AddColumn(columnName,
                valueType,
                valueTypeModifier,
                type,
                displayName,
                auto,
                readOnly,
                unique,
                dataExpression,
                columnMaxLength,
                defaultValue,
                builtin, 
                serviceColumn,
                allowNull,
                xProps);
        }

        public DataColumn AddTypedColumn(
            string columnName,
            Type dataType = null,
            string displayName = null,
            bool? auto = null,
            bool? readOnly = null,
            bool? unique = null,
            string dataExpression = null,
            uint? columnMaxLength = null,
            object defaultValue = null,
            bool builtin = false,
            bool serviceColumn = false,
            bool allowNull = true,
            IReadOnlyDictionary<string, object> xProps = null)
        {
            var (valueTypeModifier, valueType) = GetColType(dataType);

            return AddColumn(columnName,
                valueType,
                valueTypeModifier,
                dataType,
                displayName,
                auto,
                readOnly,
                unique,
                dataExpression,
                columnMaxLength,
                defaultValue,
                builtin,
                serviceColumn,
                allowNull,
                xProps);
        }

        public DataColumn AddColumn(
            string columnName,
            TableStorageType valueType = TableStorageType.String,
            TableStorageTypeModifier valueTypeModifier = TableStorageTypeModifier.Simple,
            Type dataType = null,
            string displayName = null,
            bool? auto = null,
            bool? readOnly = null,
            bool? unique = null,
            string dataExpression = null,
            uint? columnMaxLength = null,
            object defaultValue = null,
            bool builtin = false,
            bool serviceColumn = false,
            bool allowNull = true,
            IReadOnlyCollection<KeyValuePair<string, object>> xProps = null)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add columns to the {TableName} table because it is readonly.");
            }
            
            var hasExpression = string.IsNullOrEmpty(dataExpression) == false;

            if (hasExpression)
            {
                if (valueType == TableStorageType.BigInteger)
                {
                    throw new ArgumentException($"'{Name}' table '{columnName}' cannot have expression because of unsupported '{valueType}' data type.");
                }

                if (unique ?? false)
                {
                    throw new InvalidOperationException($"Cannot set unique constraint for expression '{columnName}' column.");
                }

                if (auto ?? false)
                {
                    throw new InvalidOperationException($"Cannot set auto increment for expression '{columnName}' column.");
                }
            }

            var dataColumn = (DataColumn)base.AddColumn(columnName,
                valueType, 
                valueTypeModifier,
                dataType, 
                auto,
                unique, 
                columnMaxLength,
                defaultValue, 
                builtin, 
                serviceColumn,
                allowNull, 
                xProps);

            var readonlyValue = false;
            
            if (readOnly.HasValue || hasExpression)
            {
                readonlyValue = readOnly ?? false || hasExpression;
            }

            var dataColumnContainer = DataColumnInfo.GetColumnContainer(dataColumn.ColumnHandle);
            
            dataColumn.ColumnObj = dataColumnContainer.WithCaptionExpressionReadOnly(displayName, dataExpression, readonlyValue);
            
            if (hasExpression)
            {
                var expression = new DataExpression(this, dataExpression);

                dataColumn.ExpressionLink = expression;

                var dependantColumns = expression.GetDependency();

                if (IsInitializing == false)
                {
                    foreach (var dc in dependantColumns)
                    {
                        if (DataColumnInfo.ColumnMappings.ContainsKey(dc) == false)
                        {
                            ThrowMissingColumnInExpression(columnName, dc); 
                        }
                    }
                }
                    
                foreach (var dependantColumn in dependantColumns)
                {
                    DataColumnInfo.AddDependentColumn(dataColumn.ColumnHandle, dependantColumn);
                }
            }

            return dataColumn;
        }
        
        public DataColumn AddColumn<T>(string columnName, string displayName = null, bool? readOnly = null, T defaultValue = default, bool builtin = false, bool serviceColumn = false, IReadOnlyDictionary<string, object> xProps = null)
            where T :   class, 
            IComparable<T>, 
            IComparable,
            IEquatable<T>, 
            ICloneable, 
            IXmlSerializable, 
            IJsonSerializable, 
            new()
        {
            var dataColumn = (DataColumn)base.AddColumn<T>(columnName, defaultValue: defaultValue);

            dataColumn.ColumnObj = dataColumn.ColObj.WithCaptionExpressionReadOnly(displayName, string.Empty, readOnly ?? false);

            return dataColumn;
        }
        
        public void ChangeColumnExpression(string fieldName, string expr)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(fieldName, out var column))
            {
                ChangeColumnExpressionCore((DataColumn)column, expr);
            }
        }

        public void ChangeColumnExpression(DataColumn column, string expr)
        {
            if (AreColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{column.ColumnName}' column of '{Name}' table because columns are readonly."); 
            }
                    
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{column.ColumnName}' column of the '{Name}' table expression because it is readonly.");
            }

            var tableStorageType = column.Type;
            
            if (tableStorageType == TableStorageType.BigInteger)
            {
                throw new ArgumentException($"{TableName} table {column.ColumnName} cannot have expression because of unsupported {tableStorageType} data type.");
            }
            
            ChangeColumnExpressionCore(column, expr);
        }
        
        public override void ClearRows()
        {
            DetachRows();
            
            ExpressionValuesCache.Dispose();

            ExpressionValuesCache = new DataExpressionCache(this);
            
            ClearAllAnnotations();

            base.ClearRows();
        }

        protected override void ClearRowReferencesOnClearRows()
        {
        }

        protected override CoreDataRow GetReadyInstance(int rowHandle)
        {
            var dataRow = (DataRow)base.GetReadyInstance(rowHandle);

            if (dataRow is { table: null })
            {
                dataRow.table = this;
                dataRow.RowHandleCore = rowHandle;
                
                dataRow.m_detachedStorage?.Dispose();
                dataRow.m_detachedStorage = null;
                
                StateInfo.UpdateRowAge(rowHandle);
            }
            
            return dataRow;
        }

        protected override void OnTransactionFullCommit()
        {
            EvaluateExpressions();

            if (m_dataTableCommitEvent?.HasAny() ?? false)
            {
                m_dataTableCommitEvent.Raise(new DataTableTransactionCommitEventArgs(this));
            }
        }

        protected override CoreDataTable CloneCore(Thread thread, bool withData = true, bool cloneColumns = true)
        {
            var clone = (DataTable)base.CloneCore(thread, withData);

            if (m_rowAnnotations?.HasData() ?? false)
            {
                clone.m_rowAnnotations = m_rowAnnotations?.Clone(clone);
            }

            if (withData)
            {
                if (m_rowXPropertyAnnotations != null)
                {
                    clone.m_rowXPropertyAnnotations = (RowXPropertyInfoDataItem)m_rowXPropertyAnnotations?.Copy(clone);
                }
            }

            var rowCellAnnotations = RowCellAnnotations;
            
            if (rowCellAnnotations != null)
            {
                clone.RowCellAnnotations = new();
                
                clone.RowCellAnnotations.Ensure(rowCellAnnotations.Count);

                var count = rowCellAnnotations.Count;

                if (withData)
                {
                    for (var index = 0; index < count; index++)
                    {
                        clone.RowCellAnnotations[index] = rowCellAnnotations[index].Copy(clone);
                    }
                }
                else
                {
                    for (var index = 0; index < count; index++)
                    {
                        clone.RowCellAnnotations[index] = rowCellAnnotations[index].Clone();
                    }
                }
            }
            
            if (withData)
            {
                clone.ExpressionValuesCache?.MergeFrom(ExpressionValuesCache);
            }
            
            return clone;
        }

        protected override CoreDataColumn MergeColumn(CoreDataTable sourceTable,
            CoreDataColumn sourceColumn,
            CoreDataColumnInfo sourceColumnInfo)
        {
            var targetColumn = (DataColumn)base.MergeColumn(sourceTable, sourceColumn, sourceColumnInfo);

            if (sourceColumnInfo is DataColumnInfo sdc)
            {
                var sourceCol = (DataColumn)sourceColumn;
                
                targetColumn.Caption  = sourceCol.Caption;
                targetColumn.Expression  = sourceCol.Expression;
                targetColumn.IsReadOnly  = sourceCol.IsReadOnly;

                var defaultValue = sourceCol.DefaultValue;
                
                CoreDataRowContainer.CopyIfNeededBoxed(ref defaultValue);

                targetColumn.DefaultValue = defaultValue;

                var dataExpression = targetColumn.Expression;
                var sourceExpression = sourceCol.Expression;
                
                if (dataExpression != sourceExpression)
                {
                    ChangeColumnExpressionCore(targetColumn, sourceExpression);
                }
            }

            return targetColumn;
        }
        
        public new DataRow this[int index] => (DataRow)GetRowInstance(StateInfo.RowHandles[index]);

        protected override void OnBeforeRejectChanges()
        {
            base.OnBeforeRejectChanges();
            
            m_rowXPropertyAnnotations?.RejectAllChanges();
            
            m_rowAnnotations?.RejectChanges();
         
            var rowCellAnnotations = RowCellAnnotations;

            if (rowCellAnnotations != null)
            {
                var count = rowCellAnnotations.Count;

                for (var index = 0; index < count; index++)
                {
                    var valueInfo = rowCellAnnotations[index]?.ValueInfo;

                    valueInfo?.RejectChanges();
                }
            }
        }

        protected override void OnRollbackTransaction(int tranId)
        {
            EvaluateExpressions();
            
            base.OnRollbackTransaction(tranId);
            
            if(m_dataTableRollbackEvent?.HasAny() ?? false)
            {
                m_dataTableRollbackEvent.Raise(new DataTableTransactionRollbackEventArgs(this));
            }
        }
        
        protected override void OnEndLoad()
        {
            var cols = GetDependentColumns();

            cols.Dispose();
            
            EvaluateExpressions();
        }

        private void ChangeColumnExpressionCore(DataColumn column, string expr)
        {
            var columnContainer = column;

            //todo check that we do not create storage for expression column
            if (string.IsNullOrEmpty(expr))
            {
                columnContainer.ColumnObj = columnContainer.ColObj.WithExpressionReadOnly(string.Empty, false);
                
                column.ExpressionLink?.Dispose();
                column.ExpressionLink = null;
            }
            else
            {
                columnContainer.ColumnObj = columnContainer.ColObj.WithExpressionReadOnly(expr, true);
                column.ExpressionLink = new DataExpression(this, expr);
            }

            ExpressionValuesCache?.Dispose();
            ExpressionValuesCache = new DataExpressionCache(this);
        }

        protected void DetachRows()
        {
            if (base.DataColumnInfo.ColumnsCount <= 0)
            {
                return;
            }
            if (m_rowReferences != null)
            {
                foreach (var reference in m_rowReferences)
                {
                    ((DataRow)reference)?.DetachRow();
                }
            }
            if (m_editingRows != null)
            {
                foreach (var reference in m_editingRows.Values)
                {
                    ((DataRow)reference)?.DetachRow();
                }
            }
        }

        protected override CoreContainerMetadataProps GetContainerMetaProps()
        {
            if (m_containerMetaDataPropsAge?.Age == MetaAge)
            {
                return m_containerMetaDataPropsAge;
            }

            m_containerMetaDataPropsAge = base.GetContainerMetaProps();
            
            return m_containerMetaDataPropsAge;
        }

        protected override CoreContainerDataPropsBuilder CreateContainerProps(int rowHandle, IReadOnlyCollection<string> skipColumns)
        {
            var containerDataProps = (ContainerDataPropsBuilder)base.CreateContainerProps(rowHandle, skipColumns);

            var xPropertyInfos = m_rowXPropertyAnnotations?.Storage?.ElementAtOrDefault(rowHandle);

            Map<string, Map<string, object>> containerXPropInfo = null;
            
            if (xPropertyInfos != null)
            {
                containerXPropInfo = new (StringComparer.OrdinalIgnoreCase);

                foreach (var kv in xPropertyInfos)
                {
                    if (kv.Value != null)
                    {
                        containerXPropInfo[kv.Key] = new Map<string, object>(kv.Value);
                    }
                }
            }

            var columnInfos = GetColumnInfos(rowHandle, skipColumns);

            containerDataProps.CellAnnotations = columnInfos;
            containerDataProps.RowAnnotations = GetRowAnnotations(rowHandle);
            containerDataProps.XPropInfo = containerXPropInfo;

            return containerDataProps;
        }
        
        private Map<string, Map<string, object>> GetColumnInfos(int rowHandle,
            IReadOnlyCollection<string> skipColumns)
        {
            var rowCellAnnotations = RowCellAnnotations;
            
            if (rowCellAnnotations == null)
            {
                return null;
            }
            
            var colAnnotations = new Map<string, Map<string, object>>();

            for (int columnHandle = 0; columnHandle < DataColumnInfo.ColumnsCount; columnHandle++)
            {
                var columnName = DataColumnInfo.Columns[columnHandle].ColumnName;
                
                if (skipColumns != null && skipColumns.Contains(columnName))
                {
                    continue;
                }
                
                if(columnHandle >= rowCellAnnotations.Count)
                {
                    break;
                }
                
                var dataItem = rowCellAnnotations[columnHandle];

                var cellAnnotations = dataItem.GetCellAnnotations(rowHandle);

                foreach (var cellAnnotation in cellAnnotations)
                {
                    if (colAnnotations.TryGetValue(columnName, out var columnAnnotations) == false)
                    {
                        columnAnnotations = new();
                        colAnnotations[columnName] = columnAnnotations;
                    }

                    columnAnnotations[cellAnnotation.type] = cellAnnotation.value;
                }
            }

            return colAnnotations;
        }

        internal void SetRowColumnError(string column, string error, int rowHandle, int? tranId)
        {
            SetCellAnnotation(column, error, rowHandle, tranId, ValueInfo.Error);
        }

        public void SetCellAnnotation(string columnName, object value, int rowHandle, int? tranId, string type)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup row cell annotation {type} for '{GetRowDebugKey(rowHandle)}' row of '{Name}' table because it is readonly.");
            }
            
            if (base.DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
            {
                var dataColumn = (DataColumn)column;

                var wasSet = GetRowCellAnnotation(dataColumn).SetCellAnnotation(rowHandle, value, tranId, type);

                if (wasSet)
                {
                    StateInfo.OnRowsAnnotationChange(rowHandle);
                    
                    if (m_rowMetaDataChangedEvent?.HasAny() ?? false)
                    {
                        m_rowMetaDataChangedEvent.Raise(new DataRowMetaDataChangedArgs(this, rowHandle, columnName, type, value, RowMetadataType.Column));
                    }
                }
            }
            else
            {
                throw new MissingMetadataException($"Table '{Name}' do not have '{columnName}' column or xProperty.");
            }
        }

        public uint GetRowAnnotationAge(int row)
        {
            return StateInfo.GetRowAnnotationAge(row);
        }

        internal void SetRowColumnWarning(string column, string warning, int rowHandle, int? tranId)
        {
            SetCellAnnotation(column, warning, rowHandle, tranId, ValueInfo.Warning);
        }

        internal void SetRowColumnInfo(string column, string info, int rowHandle, int? tranId)
        {
            SetCellAnnotation(column, info, rowHandle, tranId, ValueInfo.Info);
        }

        public void ClearCellAnnotations()
        {
            var rowCellAnnotations = RowCellAnnotations;
            
            if (rowCellAnnotations != null)
            {
                foreach (var item in rowCellAnnotations)
                {
                    item.ClearAnnotations();
                }
            }
        }

        public void ClearAllAnnotations()
        {
            m_rowAnnotations?.Clear();
            m_rowXPropertyAnnotations?.Clear(null);
            ClearCellAnnotations();
        }

        public void ClearRowOnlyAnnotations()
        {
            m_rowAnnotations?.Clear();
            m_rowXPropertyAnnotations?.Clear(null);
        }

        public bool Equals(DataTable other)
        {
            return this.Equals((CoreDataTable)other);
        }
        
        public void AttachDatasetEventHandlersTo(DataTable targetDataset, IDataOwner dataOwner = null)
        {
            foreach (var sourceTable in Tables)
            {
                var targetTable = targetDataset.TryGetTable(sourceTable.Name);

                if(targetTable != null)
                {
                    sourceTable.AttachEventHandlersTo(targetTable, dataOwner);
                }
            }
        }
        
        public new IEnumerable<DataTable> Tables => base.Tables.OfType<DataTable>();
        
        [NotNull]
        public new DataTable GetTable([NotNull] string tableName)
        {
            return (DataTable)base.GetTable(tableName);
        }
        protected override void MergeTableMetadata(CoreDataTable currentTable, CoreDataTable sourceTable)
        {
            var lockEvents = ((DataTable)currentTable).LockEvents();

            try
            {
                currentTable.MergeMetaOnly(sourceTable);
            }
            finally
            {
                lockEvents.ResetAggregatedEvents();

                lockEvents.UnlockEvents();
            }
        }
        
        protected override void MergeTable(bool overrideExisting, CoreDataTable table, CoreDataTable sourceTable)
        {
            var lockEvents = ((DataTable)table).LockEvents();

            try
            {
                base.MergeTable(overrideExisting, table, sourceTable);
            }
            finally
            {
                lockEvents.ResetAggregatedEvents();

                lockEvents.UnlockEvents();
            }
        }

        private void LockDatasetEvents()
        {
            foreach (var table in Tables)
            {
                table.LockEvents();
            }
        }

        public void ResetDatasetAggregatedEvents()
        {
            foreach (var table in Tables)
            {
                ((IDataLockEventState)table).ResetAggregatedEvents();
            }
        }

        private void UnlockDatasetEvents()
        {
            foreach (var table in Tables)
            {
                ((IDataLockEventState)table).UnlockEvents();
            }
        }
    }
}