using System.Diagnostics;
using System.Xml.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    [DebuggerTypeProxy(typeof(DataRowDebugView))]
    [DebuggerDisplay("Row of {m_tableName}, State {RowRecordState}, Age {GetRowAge()}, # {DebugKeyValue} ")]
    public partial class DataRow : CoreDataRow, IDataTableRow, IDataLockEventState, IComparable<DataRow>
    {
        protected internal DataTable table
        {
            get { return (DataTable)base.table; }
            set { base.table = value; }
        }

        ICoreDataRowReadOnlyContainer IDataTableReadOnlyRow.ToContainer() => ToContainerCore();

        [CanBeNull] 
        protected internal DataRowContainer m_detachedStorage;

        internal override object DebugKeyValue
        {
            get
            {
                if (base.table != null && RowRecordState != RowState.Detached)
                {
                    return base.DebugKeyValue;
                }

                if (m_detachedStorage != null)
                {
                    var pk = m_detachedStorage.PrimaryKeyColumn.ToArray();

                    if (pk.Length > 0)
                    {
                        return string.Join(", ", pk.Select(k =>
                        {
                            var value = m_detachedStorage[k.ColumnName];

                            return $"{k.ColumnName} = {(value ?? "{NULL}")}, Row handle: {RowHandleCore}. Row is detached.";
                        }));
                    }
                }

                if (base.table is null)
                {
                    return $"Row {RowHandleCore} is disposed.";
                }

                return $"Row {RowHandleCore} is detached.";
            }
        }

        public int CompareTo(DataRow row)
        {
            var tuple = CompareToExt((CoreDataRow)row);
            
            return tuple.cmp;
        }

        public override bool Equals(object obj)
        {
            if (obj is not DataRow row)
            {
                return false;
            }

            if (row.table == null && this.table == null)
            {
                var valueTuple = CoreDataRowContainer.EqualsDataRowContainers(row.m_detachedStorage, this.m_detachedStorage);
                
                return valueTuple.value;
            }

            if (row.table == null)
            {
                return false;
            }
            
            var tuple = EqualsExt(row);
            
            return tuple.value;
        }

        protected override int CalcPrimaryKeyHashCode()
        {
            if (table is null)
            {
                if (m_detachedStorage is not null)
                {
                    return m_detachedStorage.GetHashCode();
                }

                return 0;
            }

            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetHashCode();
            }

            return base.CalcPrimaryKeyHashCode();
        }

        public DataRow()
        {
        }

        public new void Init(int rowHandle, [NotNull] CoreDataTable dataTable)
        {
            if (table != null)
            {
                throw new InvalidOperationException("Row was already initialized.");
            }

            RowHandleCore = rowHandle;
            m_tableName = dataTable.Name;

            base.table = dataTable;

            m_detachedStorage?.Dispose();
            m_detachedStorage = null;

            m_currentEditRow?.CancelEdit();
            m_currentEditRow = null;
        }

        public DataRow(int rowHandle, CoreDataTable dataTable)
        {
            Init(rowHandle, dataTable);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyColumn> IDataTableReadOnlyRow.GetTableColumns() => GetColumns();

        public IDataRowReadOnlyContainer ToReadOnlyDataContainer()
        {
            return ToContainer();
        }

        [NotNull]
        public override IReadOnlyList<string> GetTableColumnNames()
        {
            if (table != null)
            {
                return table.DataColumnInfo.Columns.Select(c => c.ColumnName).ToArray();
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.Columns.Select(c => c.ColumnName).ToArray();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyColumn> IDataRowReadOnlyAccessor.PrimaryKeyColumn => GetPrimaryKeyColumns();

        public IEnumerable<IDataTableReadOnlyColumn> GetPrimaryKeyColumns()
        {
            if (table != null)
            {
                return table.PrimaryKey.OfType<DataColumn>();
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.PrimaryKeyColumn;
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [CanBeNull]
        protected override ICoreDataTableColumn TryGetColumnCore(string columnName)
        {
            if (table == null && m_detachedStorage != null)
            {
                return m_detachedStorage.TryGetColumn(columnName);
            }
            
            if (table != null)
            {
                return table.TryGetColumn(columnName);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }
        
        public override object this[CoreDataColumn column]
        {
            get
            {
                if (m_detachedStorage != null && column.ColumnHandle < m_detachedStorage.ColumnsCount)
                {
                    var columnContainer = m_detachedStorage.GetColumn(column.ColumnHandle);

                    return m_detachedStorage.GetFieldValue(columnContainer.ColumnName, DefaultValueType.ColumnBased, default);
                }

                return base[column];
            }
            set
            {
                SetFieldCore(column, value);
            }
        }

        public IDataTableReadOnlyColumn GetColumn(string columnOrXProperty)
        {
            if (table == null && m_detachedStorage != null)
            {
                return m_detachedStorage.GetColumn(columnOrXProperty);
            }
            
            var column = (DataColumn)TryGetColumnCore(columnOrXProperty);

            if (column == null)
            {
                throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessage(columnOrXProperty));
            }

            return column;
        }
        
        public IDataTableReadOnlyColumn TryGetColumn(string columnOrXProperty)
        {
            if (table == null && m_detachedStorage != null)
            {
                return m_detachedStorage.TryGetColumn(columnOrXProperty);
            }
            
            var column = (DataColumn)TryGetColumnCore(columnOrXProperty);

            return column;
        }

        IDataTableReadOnlyColumn IDataRowReadOnlyAccessor.GetColumn(int columnHandle)
        {
            if (table == null && m_detachedStorage != null)
            {
                return (DataColumnContainer)m_detachedStorage.GetColumn(columnHandle);
            }
            
            return (IDataTableReadOnlyColumn)GetColumnCore(columnHandle);
        }
        
        protected override IEnumerable<ICoreTableReadOnlyColumn> GetPrimaryKeyColumn()
        {
            if (table == null)
            {
                if (m_detachedStorage != null)
                {
                    return m_detachedStorage.PrimaryKeyColumn;
                }
            }

            return base.GetPrimaryKeyColumn();
        }

        public override IComparable[] GetRowKeyValue()
        {
            if (table == null)
            {
                if (m_detachedStorage != null)
                {
                    return m_detachedStorage.GetRowKeyValue();
                }
            }
            
            return base.GetRowKeyValue();
        }

        protected override ICoreTableReadOnlyColumn GetColumnCore(int columnHandle)
        {
            if (table == null && m_detachedStorage != null)
            {
                return ((IDataRowReadOnlyAccessor)m_detachedStorage).GetColumn(columnHandle);
            }

            if (table != null)
            {
                return table.GetColumn(columnHandle);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableReadOnlyColumn IDataRowReadOnlyAccessor.TryGetColumn(string columnName) => (DataColumn)TryGetColumnCore(columnName);

        public bool IsNull(IDataTableReadOnlyColumn column)
        {
            if (column is CoreDataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, this.table))
            {
                return IsNull(dataColumn);
            }

            return IsNull(column.ColumnName);
        }

        public bool IsNotNull(IDataTableReadOnlyColumn column)
        {
            if (column is CoreDataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, this.table))
            {
                return IsNull(dataColumn) == false;
            }

            return IsNull(column.ColumnName) == false;
        }

        public object this[IDataTableReadOnlyColumn column]
        {
            get
            {
                if (column is CoreDataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, this.table))
                {
                    return this[dataColumn];
                }

                return this[column.ColumnName];
            }
            set
            {
                if (column is CoreDataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, this.table))
                {
                    this[dataColumn] = value;
                }
                else
                {
                    this[column.ColumnName] = value;
                }
            }
        }

        object IDataRowReadOnlyAccessor.this[IDataTableReadOnlyColumn column]
        {
            get
            {
                if (column is CoreDataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, this.table))
                {
                    return this[dataColumn];
                }

                return this[column.ColumnName];
            }
        }

        public void SetColumnError(string column, string error)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    m_currentEditRow.SetColumnError(column, error);
                    
                    return;
                }
                
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup '{column}' column '{DebugKeyValue}' row from '{table.Name}' table error because it is readonly.");
                }
                
                table.SetRowColumnError(column, error, RowHandleCore, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public string GetCellError(string columnName)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetCellError(columnName);
                }
                
                if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
                {
                    if (column.ColumnHandle >= table.RowCellAnnotationInitCount)
                    {
                        return string.Empty;
                    }

                    var dataColumn = (DataColumn)column;
                    
                    return table.GetRowCellAnnotation(dataColumn).GetCellError(RowHandleCore);
                }

                throw new MissingMetadataException($"Table {m_tableName} do not have {columnName} column.");
            }
            
            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetCellError(columnName);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public bool HasAnyColumnError()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    foreach (var column in m_currentEditRow.Columns)
                    {
                        var error = m_currentEditRow.GetCellError(column.ColumnName);

                        if (string.IsNullOrEmpty(error) == false)
                        {
                            return true;
                        }
                    }

                    return false;
                }
                
                for (int columnHandle = 0; columnHandle < table.DataColumnInfo.ColumnsCount; columnHandle++)
                {
                    var dataColumn = table.GetColumn(columnHandle);

                    var error = table.GetRowCellAnnotation(dataColumn).GetCellError(RowHandleCore);

                    if (string.IsNullOrEmpty(error) == false)
                    {
                        return true;
                    }
                }

                return false;
            }
            
            if (m_detachedStorage != null)
            {
                foreach (var column in m_detachedStorage.Columns)
                {
                    var error = m_detachedStorage.GetCellError(column.ColumnName);

                    if (string.IsNullOrEmpty(error) == false)
                    {
                        return true;
                    }
                }

                return false;
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public void SetColumnWarning(string column, string warning)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    m_currentEditRow.SetColumnWarning(column, warning);
                    
                    return;
                }
                
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup '{column}' column '{DebugKeyValue}' row from '{table.Name}' table warning because it is readonly.");
                }
                
                table.SetRowColumnWarning(column, warning, RowHandleCore, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public string GetCellWarning(string columnName)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetCellWarning(columnName);
                }
                
                if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
                {
                    if (column.ColumnHandle >= table.RowCellAnnotationInitCount)
                    {
                        return string.Empty;
                    }

                    var dataColumn = (DataColumn)column;
                    
                    return table.GetRowCellAnnotation(dataColumn).GetCellWarning(RowHandleCore);
                }

                throw new MissingMetadataException($"Table {m_tableName} do not have {columnName} column.");
            }
            
            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetCellWarning(columnName);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public void SetColumnInfo(string column, string info)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    m_currentEditRow.SetColumnInfo(column, info);
                    
                    return;
                }
                
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup '{column}' column '{DebugKeyValue}' row from '{table.Name}' table info because it is readonly.");
                }
                
                table.SetRowColumnInfo(column, info, RowHandleCore, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public string GetCellInfo(string columnName)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetCellInfo(columnName);
                }
                
                if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
                {
                    if (column.ColumnHandle >= table.RowCellAnnotationInitCount)
                    {
                        return string.Empty;
                    }
                    
                    var dataColumn = (DataColumn)column;

                    return table.GetRowCellAnnotation(dataColumn).GetCellInfo(RowHandleCore);
                }
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetCellInfo(columnName);
            }

            throw new MissingMetadataException($"Table {m_tableName} do not have {columnName} column.");

        }

        public void SetRowError([CanBeNull] string value)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    m_currentEditRow.SetRowError(value);
                    
                    return;
                }
                
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup error for '{DebugKeyValue}' row of '{table.Name}' table because it is readonly.");
                }
                
                table.SetRowError(RowHandleCore, value, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        [CanBeNull]
        public string GetRowError()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetRowError();
                }
                
                return table.GetRowError(RowHandleCore);
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetRowError();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public void SetRowFault([CanBeNull] string value)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    m_currentEditRow.SetRowFault(value);
                    
                    return;
                }
                
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup fault for '{DebugKeyValue}' row of '{table.Name}' table because it is readonly.");
                }
                
                table.SetRowFault(RowHandleCore, value, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        [CanBeNull]
        public string GetRowFault()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetRowFault();
                }
                
                return table.GetRowFault(RowHandleCore);
            }
            
            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetRowFault();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public void SetRowWarning([CanBeNull] string value)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    m_currentEditRow.SetRowWarning(value);
                    
                    return;
                }
                
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup warning for '{DebugKeyValue}' row of '{table.Name}' table because it is readonly.");
                }
                
                table.SetRowWarning(RowHandleCore, value, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        [CanBeNull]
        public string GetRowWarning()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetRowWarning();
                }
                
                return table.GetRowWarning(RowHandleCore);
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetRowWarning();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public void SetRowInfo([CanBeNull] string value)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    m_currentEditRow.SetRowInfo(value);
                    
                    return;
                }
                
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup info for '{DebugKeyValue}' row of '{table.Name}' table because it is readonly.");
                }
                
                table.SetRowInfo(RowHandleCore, value, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }
        
        public void SetXPropertyAnnotation([CanBeNull] string propertyCode, string key, object value)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup XProperty info for '{DebugKeyValue}' row of '{table.Name}' table because it is readonly.");
                }
                
                if (m_currentEditRow != null)
                {
                    m_currentEditRow.SetXPropertyAnnotation(propertyCode, key, value);
                    
                    return;
                }
                
                table.SetXPropertyAnnotation(RowHandleCore, propertyCode, key, value, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }


        public IReadOnlyDictionary<string, object> GetXPropertyAnnotationValues(string propertyCode)
        {
            if (table != null)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetXPropertyAnnotationValues(propertyCode);
                }
                
                return table.GetXPropertyAnnotationValues(RowHandleCore, propertyCode);
            }
            else
            {
                if (m_detachedStorage != null)
                {
                    return m_detachedStorage.GetXPropertyAnnotationValues(propertyCode);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public T GetXPropertyAnnotation<T>([CanBeNull] string propertyCode, string key)
        {
            if (table != null)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetXPropertyAnnotation<T>(propertyCode, key);
                }
                
                return table.GetXPropertyAnnotation<T>(RowHandleCore, propertyCode, key);
            }
            else
            {
                if (m_detachedStorage != null)
                {
                    return m_detachedStorage.GetXPropertyAnnotation<T>(propertyCode, key);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public IEnumerable<string> XPropertyAnnotations
        {
            get
            {
                if (m_detachedStorage != null)
                {
                    return m_detachedStorage.XPropertyAnnotations;
                }

                if (table != null)
                {
                    return table.GetXPropertyInfos(RowHandleCore);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        void IDataRowAccessor.CopyFrom(IDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            CopyFrom(rowAccessor, skipFields);
        }

        [CanBeNull]
        public string GetRowInfo()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetRowInfo();
                }
                
                return table.GetRowInfo(RowHandleCore);
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetRowInfo();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public IEnumerable<string> GetErrorColumns()
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                if (m_detachedStorage != null)
                {
                    foreach (var column in m_detachedStorage.GetErrorColumns())
                    {
                        yield return column;
                    }

                    yield break;
                }

                throw new DataDetachedException($"{DebugKeyValue}");
            }

            if (m_currentEditRow != null)
            {
                foreach (var column in m_currentEditRow.GetErrorColumns())
                {
                    yield return column;
                }

                yield break;
            }

            for (var columnHandle = 0; columnHandle < table.RowCellAnnotationInitCount; columnHandle++)
            {
                var dataColumn = table.GetColumn(columnHandle);

                if (table.GetRowCellAnnotation(dataColumn).HasCellError(RowHandleCore))
                {
                    yield return dataColumn.ColumnName;
                }
            }
        }

        public IEnumerable<string> GetWarningColumns()
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                if (m_detachedStorage != null)
                {
                    foreach (var column in m_detachedStorage.GetWarningColumns())
                    {
                        yield return column;
                    }

                    yield break;
                }

                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (m_currentEditRow != null)
            {
                foreach (var column in m_currentEditRow.GetWarningColumns())
                {
                    yield return column;
                }

                yield break;
            }

            for (var columnHandle = 0; columnHandle < table.RowCellAnnotationInitCount; columnHandle++)
            {
                var dataColumn = table.GetColumn(columnHandle);
                
                if (table.GetRowCellAnnotation(dataColumn).HasCellWarning(RowHandleCore))
                {
                    yield return dataColumn.ColumnName;
                }
            }
        }

        public IEnumerable<string> GetInfoColumns()
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                if (m_detachedStorage != null)
                {
                    foreach (var column in m_detachedStorage.GetInfoColumns())
                    {
                        yield return column;
                    }

                    yield break;
                }

                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (m_currentEditRow != null)
            {
                foreach (var column in m_currentEditRow.GetInfoColumns())
                {
                    yield return column;
                }

                yield break;
            }

            for (var columnHandle = 0; columnHandle < table.RowCellAnnotationInitCount; columnHandle++)
            {
                var dataColumn = table.GetColumn(columnHandle);
                
                if (table.GetRowCellAnnotation(dataColumn).HasCellInfo(RowHandleCore))
                {
                    yield return dataColumn.ColumnName;
                }
            }
        }

        public T Field<T>(IDataTableReadOnlyColumn column, T defaultIfNull)
        {
            if (column is DataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, this.table))
            {
                return Field(dataColumn, defaultIfNull);
            }

            return Field(column.ColumnName, defaultIfNull);
        }

        public T Field<T>(IDataTableReadOnlyColumn column)
        {
            if (column is DataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, this.table))
            {
                return Field<T>(dataColumn);
            }

            return Field<T>(column.ColumnName);
        }

        public void SilentlySetValue(IDataTableReadOnlyColumn column, object value)
        {
            if (column is DataColumn dataColumn)
            {
                SilentlySetValue(dataColumn, value);
            }

            SilentlySetValue(column.ColumnName, value);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowAccessor IDataRowAccessor.SetNull(string columnName)
        {
            return SetNull(columnName);
        }

        public IDataRowAccessor SetNull(IDataTableReadOnlyColumn column)
        {
            if (column is DataColumn dataColumn)
            {
                SetNull(dataColumn);
            }
            else
            {
                SetNull(column.ColumnName);
            }

            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        object IDataRowAccessor.this[int handle]
        {
            get
            {
                return this[new ColumnHandle(handle)];
            }
            set
            {
                this[new ColumnHandle(handle)] = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowAccessor IDataRowAccessor.Set<T>(IDataTableReadOnlyColumn column, T value)
        {
            this[column.ColumnName] = value;

            return this;
        }

        [NotNull]
        public new DataRow SetNull(string column)
        {
            this[column] = null;

            return this;
        }

        public override bool IsExistsField(string columnName)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var _);
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.IsExistsField(columnName);
            }
            
            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public override int GetColumnCount()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.ColumnCount;
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetColumnCount();
            }

            return 0;
        }

        public object GetOriginalValue(IDataTableReadOnlyColumn column)
        {
            if (column is DataColumn dc && ReferenceEquals(table, dc.DataTable))
            {
                return GetOriginalValue(dc);
            }
            
            return GetOriginalValue(column.ColumnName);
        }

        public T GetOriginalValue<T>(IDataTableReadOnlyColumn column)
        {
            if (column is DataColumn dc)
            {
                return GetOriginalValue<T>(dc);
            }
            
            return GetOriginalValue<T>(column.ColumnName);
        }
     
        public void DetachRow()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                m_detachedStorage = ToContainer();

                base.table = null;
            }
            else
            {
                base.table = null;
            }
        }

        public void AttachRow([NotNull] DataTable dataTable, int rowHandle)
        {
            if (rowHandle < 0 || rowHandle >= dataTable.RowCount)
            {
                throw new ArgumentOutOfRangeException(nameof(rowHandle));
            }

            RowHandleCore = rowHandle;
            
            base.table = dataTable ?? throw new ArgumentNullException(nameof(dataTable));

            base.table.AttachRow(this, DebugKeyValue);

            m_currentEditRow?.CancelEdit();
            m_currentEditRow = null;
            
            m_detachedStorage?.Dispose();
            m_detachedStorage = null;
        }

        public override ulong GetColumnAge(string columnName)
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                if (m_detachedStorage != null)
                {
                    return m_detachedStorage.GetRowAge();
                }
                
                return 0;
            }
            
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetRowAge();
            }

            if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column) == false)
            {
                return 0;
            }
            
            if (column.ColumnHandle >= table.ColumnCount)
            {
                return ulong.MinValue;
            }
            
            var dataColumn = (DataColumn)column;

            if (dataColumn.FixType == DataColumnType.Expression)
            {
                return table.DataAge;
            }

            return dataColumn.DataStorageLink.GetAge(RowHandleCore, column);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableRow IDataTableRow.Set(string columnOrXProp, string value)
        {
            return Set(columnOrXProp, value);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableRow IDataTableRow.Set(string columnOrXProp, XElement value)
        {
            return Set(columnOrXProp, value);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableRow IDataTableRow.Set(string columnOrXProp, byte[] value)
        {
            return Set(columnOrXProp, value);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableRow IDataTableRow.Set(string columnOrXProp, char[] value)
        {
            return Set(columnOrXProp, value);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableRow IDataTableRow.Set(string columnOrXProp, Uri value)
        {
            return Set(columnOrXProp, value);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableRow IDataTableRow.Set(string columnOrXProp, Type value)
        {
            return Set(columnOrXProp, value);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableRow IDataTableRow.Set<T>(string columnOrXProp, T? value) 
        {
            return Set(columnOrXProp, value);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableRow IDataTableRow.Set<T>(string columnOrXProp, T value)
        {
            return Set(columnOrXProp, value);
        }

        [NotNull]
        IDataTableRow IDataTableRow.Set(IDataTableReadOnlyColumn column, string value)
        {
            if (column is DataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, table))
            {
                return Set(dataColumn, value);
            }

            return Set(column.ColumnName, value);
        }

        [NotNull]
        IDataTableRow IDataTableRow.Set(IDataTableReadOnlyColumn column, XElement value)
        {
            if (column is DataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, table))
            {
                return Set(dataColumn, value);
            }

            return Set(column.ColumnName, value);
        }

        [NotNull]
        IDataTableRow IDataTableRow.Set(IDataTableReadOnlyColumn column, byte[] value)
        {
            if (column is DataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, table))
            {
                return Set(dataColumn, value);
            }

            return Set(column.ColumnName, value);
        }

        [NotNull]
        IDataTableRow IDataTableRow.Set(IDataTableReadOnlyColumn column, char[] value)
        {
            if (column is DataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, table))
            {
                return Set(dataColumn, value);
            }

            return Set(column.ColumnName, value);
        }

        [NotNull]
        IDataTableRow IDataTableRow.Set(IDataTableReadOnlyColumn column, Uri value)
        {
            if (column is DataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, table))
            {
                return Set(dataColumn, value);
            }

            return Set(column.ColumnName, value);
        }

        [NotNull]
        IDataTableRow IDataTableRow.Set(IDataTableReadOnlyColumn column, Type value)
        {
            if (column is DataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, table))
            {
                return Set(dataColumn, value);
            }

            return Set(column.ColumnName, value);
        }

        [NotNull]
        IDataTableRow IDataTableRow.Set<T>(IDataTableReadOnlyColumn column, T? value)
        {
            if (column is DataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, table))
            {
                return Set(dataColumn, value);
            }

            return Set(column.ColumnName, value);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowAccessor IDataRowAccessor.Set<T>(string columnName, T value)
        {
            this[columnName] = value;

            return this;
        }

        [NotNull]
        IDataTableRow IDataTableRow.Set<T>(IDataTableReadOnlyColumn column, T value)
        {
            if (column is DataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, table))
            {
                return Set(dataColumn, value);
            }

            return Set(column.ColumnName, value);
        }
     

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowAccessor IDataRowAccessor.Set<T>(string columnOrXProp, T? value) 
        {
            return (IDataRowAccessor)SetValueCore(columnOrXProp, value);
        }

        [NotNull]
        IDataRowAccessor IDataRowAccessor.Set<T>(IDataTableReadOnlyColumn column, T? value)
        {
            if (column is DataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, table))
            {
                return Set(dataColumn, value);
            }

            return Set(column.ColumnName, value);
        }
     

        public new IEnumerable<IDataTableReadOnlyColumn> GetColumns()
        {
            return GetColumnsCore().OfType<IDataTableReadOnlyColumn>();
        }
     
        protected override IEnumerable<ICoreTableReadOnlyColumn> GetColumnsCore()
        {
            if (table != null)
            {
                return table.GetColumns();
            }
            
            if (m_detachedStorage != null)
            {
                return m_detachedStorage.Columns;
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public override IEnumerable<string> GetChangedFields()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetChangedFields();
                }
                
                return ChangedFieldsCore(table.GetColumns());
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetChangedFields();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public override IEnumerable<string> GetChangedXProperties()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetChangedXProperties();
                }
                
                var dataRow = this;

                var rowState = dataRow.RowRecordState;

                if (rowState == RowState.Unchanged)
                {
                    return Enumerable.Empty<string>();
                }

                return table.StateInfo.GetChangedXProperties(RowHandleCore);
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetChangedXProperties();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public IEnumerable<string> GetChangedFields(IEnumerable<DataColumn> columns)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    var changedFields = m_currentEditRow.GetChangedFields().ToSet();

                    var fields = columns.Where(c => changedFields.Contains(c.ColumnName)).Select(c => c.ColumnName).ToData();
                
                    changedFields.Dispose();
                
                    return fields;
                }
                
                return ChangedFieldsCore(columns);
            }

            if (m_detachedStorage != null)
            {
                var changedFields = m_detachedStorage.GetChangedFields().ToSet();

                var fields = columns.Where(c => changedFields.Contains(c.ColumnName)).Select(c => c.ColumnName).ToData();
                
                changedFields.Dispose();
                
                return fields;
            }

            return Enumerable.Empty<string>();
        }

        private IEnumerable<string> ChangedFieldsCore(IEnumerable<DataColumn> columns)
        {
            var dataRow = this;

            var rowState = dataRow.RowRecordState;

            if (rowState == RowState.Unchanged)
            {
                yield break;
            }

            foreach (var dataColumn in columns)
            {
                if (ReferenceEquals(dataColumn.DataTable, this.table))
                {
                    if (dataColumn.FixType == DataColumnType.Common && rowState == RowState.Added
                            ? dataColumn.DataStorageLink.IsNull(RowHandleCore, dataColumn) == false
                            : dataColumn.DataStorageLink.IsCellChanged(RowHandleCore, dataColumn))
                    {
                        yield return dataColumn.ColumnName;
                    }
                }
                else
                {
                    var column = (DataColumn)GetColumnCore(dataColumn.ColumnName);

                    if (column.FixType == DataColumnType.Common && rowState == RowState.Added
                            ? column.DataStorageLink.IsNull(RowHandleCore, column) == false
                            : column.DataStorageLink.IsCellChanged(RowHandleCore, column))
                    {
                        yield return column.ColumnName;
                    }
                }
            }
        }

        public override RowState RowRecordState
        {
            get
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.RowRecordState;
                }
                
                if (table != null)
                {
                    return table.GetRowState(RowHandleCore);
                }

                if (m_detachedStorage != null)
                {
                    return RowState.Detached;
                }

                return RowState.Disposed;
            }
        }

        [CanBeNull]
        public override T GetXProperty<T>(string xPropertyName, bool original = false) 
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetXProperty<T>(xPropertyName, original);
                }
                
                var value = table.StateInfo.GetExtendedProperty(RowHandleCore, xPropertyName, original);

                return XPropertyValueConverter.TryConvert<T>("DataRow", xPropertyName, value);
            }
            
            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetXProperty<T>(xPropertyName, original);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public override IEnumerable<string> GetXProperties()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetXProperties();
                }
                
                return base.GetXProperties();
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetXProperties();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public new DataRowContainer ToContainer()
        {
            return (DataRowContainer)base.ToContainer();
        }
        
        IDataRowContainer IDataTableRow.ToContainer()
        {
            return (DataRowContainer)base.ToContainer();
        }
        
        protected override CoreDataRowContainer ToContainerCore()
        {
            if (table == null && m_detachedStorage is not null)
            {
                return m_detachedStorage.Clone();
            }
            
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.Clone();
            }

            return base.ToContainerCore();
        }

        public override ulong GetRowAge()
        {
            if (table is null && m_detachedStorage != null)
            {
                return m_detachedStorage.GetRowAge();
            }
            
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetRowAge();
            }

            return base.GetRowAge();
        }

        public IDataLockEventState LockEvents()
        {
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot lock events for '{DebugKeyValue}' row of '{table.Name}' table because it is readonly.");
            }
            
            if (table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            table.StateInfo.LockRowEvents(RowHandleCore);

            return this;
        }
        
        void IDataLockEventState.ResetAggregatedEvents()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                table.ResetAggregatedEvents(RowHandleCore);
            }
        }

        void IDataLockEventState.UnlockEvents()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                table.UnlockRowEvents(this);
            }
        }

        public void CopyChanges(IDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            if (m_currentEditRow != null)
            {
                m_currentEditRow.CopyChanges(rowAccessor, skipFields);
                
                return;
            }
            
            base.CopyChanges(rowAccessor, skipFields);
        }

        public override void CopyAll([NotNull] ICoreDataRowReadOnlyAccessor rowAccessor)
        {
            if (m_currentEditRow != null)
            {
                m_currentEditRow.CopyAll(rowAccessor);
                
                return;
            }
            
            base.CopyAll(rowAccessor);

            if (rowAccessor is IDataRowAccessor dra)
            {
                CopyDataMetaData(dra);
            }
        }

        private void CopyDataMetaData(IDataRowAccessor dra)
        {
            foreach (var annotation in dra.GetRowAnnotations())
            {
                SetRowAnnotation(annotation.type, annotation.value);
            }

            foreach (var annotation in dra.GetCellAnnotations())
            {
                SetCellAnnotation(annotation.column, annotation.type, annotation.value);
            }

            foreach (var xPropertyInfo in dra.XPropertyAnnotations)
            {
                var xPropertyInfoValues = dra.GetXPropertyAnnotationValues(xPropertyInfo);

                if (xPropertyInfoValues != null)
                {
                    foreach (var kValue in xPropertyInfoValues)
                    {
                        SetXPropertyAnnotation(xPropertyInfo, kValue.Key, kValue.Value);
                    }
                }
            }
        }

        public override void CopyFrom([NotNull] ICoreDataRowReadOnlyAccessor rowAccessor,
            IReadOnlyCollection<string> skipFields = null)
        {
            if (m_currentEditRow != null)
            {
                m_currentEditRow.CopyFrom(rowAccessor, skipFields);
                
                return;
            }
            
            base.CopyFrom(rowAccessor, skipFields);

            if (rowAccessor is IDataRowAccessor dra)
            {
                CopyDataMetaData(dra);
            }
        }
    }
}