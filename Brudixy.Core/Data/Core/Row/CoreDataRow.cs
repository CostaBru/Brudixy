using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    [DebuggerTypeProxy(typeof(CoreDataRowDebugView))]
    [DebuggerDisplay("Row of {m_tableName}, State {RowRecordState}, Age {GetRowAge()}, # {DebugKeyValue} ")]
    public partial class CoreDataRow : ICoreDataRowAccessor, IComparable<CoreDataRow>
    {
        protected internal CoreDataTable table;

        protected string m_tableName;

        internal int RowHandleCore;

        public string GetTableName() => m_tableName;

        public int RowHandle => RowHandleCore;

        internal virtual object DebugKeyValue
        {
            get
            {
                if (table != null && RowRecordState != RowState.Detached)
                {
                    return table.GetRowDebugKey(RowHandleCore);
                }

                return $"Row {RowHandleCore} is detached";
            }
        }

        public int CompareTo(CoreDataRow row)
        {
            return CompareToExt(row).cmp;
        }

        internal (int cmp, string name, string type) CompareToExt(CoreDataRow row)
        {
            if (row == null)
            {
                return (-1, "other row is null", string.Empty);
            }

            var sameTableReference = ReferenceEquals(table, row.table);

            if (sameTableReference)
            {
                var dataRow = table.GetRow(row.RowHandleCore);

                if (ReferenceEquals(row, dataRow))
                {
                    return (0, "Same reference", string.Empty);
                }

                var compareToCore = RowHandleCore.CompareTo(row.RowHandleCore);
                
                return (compareToCore, "Same row handle", string.Empty);
            }

            return CoreDataRowContainer.CompareDataRows(this, row);
        }

        public override bool Equals(object obj)
        {
            var valueTuple = EqualsExt(obj);
            return valueTuple.value;
        }

        public virtual (bool value, string name, string type) EqualsExt(object obj)
        {
            if (obj is not CoreDataRow row)
            {
                return (false, $"Is not {nameof(CoreDataRow)} type", "Type mismatch");
            }

            if (table != null)
            {
                var sameTableReference = ReferenceEquals(table, row.table);

                if (sameTableReference)
                {
                    var dataRow = table.GetRow(row.RowHandleCore);

                    if (ReferenceEquals(this, dataRow))
                    {
                        return (true, "Same row reference", "Same Reference");
                    }

                    if (RowHandleCore == row.RowHandleCore)
                    {
                        return (true, "Same row reference handle", "Same Row Handle");
                    }
                }

                if (sameTableReference == false && GetColumnCount() != row.GetColumnCount())
                {
                    return (false, "Different column count", "Different table");
                }
            }

            var colOrXProp = DeepEquals(row);

            return colOrXProp;
        }

        private (bool value, string name, string type)  DeepEquals(CoreDataRow row)
        {
            if (this.table != null)
            {
                var pkColumns = this.table.PrimaryKey.ToArray();

                if (pkColumns.Length > 0)
                {
                    bool all = false;

                    int i = 0;

                    foreach (var otherKeyColumn in ((ICoreDataRowAccessor)row).PrimaryKeyColumn)
                    {
                        var thisKeyColumn = pkColumns[i];

                        var compCol = string.CompareOrdinal(thisKeyColumn.ColumnName, otherKeyColumn.ColumnName);

                        if (compCol != 0)
                        {
                            return (false, thisKeyColumn.ColumnName, "PK column name");
                        }

                        var thisKeyVal = (IComparable)this[thisKeyColumn];
                        var otherKeyVal = (IComparable)row[thisKeyColumn];

                        if (thisKeyVal is null && otherKeyVal is null)
                        {
                            continue;
                        }

                        if (otherKeyVal is null)
                        {
                            return (false, thisKeyColumn.ColumnName, "PK column value is null");
                        }

                        var keyComp = thisKeyVal?.CompareTo(otherKeyVal);

                        all = keyComp == 0;

                        if (all)
                        {
                            continue;
                        }

                        return (false, thisKeyColumn.ColumnName, "PK column value");
                    }
                }
            }

            return CoreDataRowContainer.ContentEquals(this, row);
        }

        public override int GetHashCode()
        {
            var hashCode = CalcPrimaryKeyHashCode();

            if (hashCode == 0)
            {
                return RuntimeHelpers.GetHashCode(this);
            }
            
            return hashCode;
        }

        protected virtual int CalcPrimaryKeyHashCode()
        {
            if (table is null)
            {
                return 0;
            }

            var pkLen = table.DataColumnInfo.PrimaryKeyColumns.Length;
            
            if (pkLen > 0)
            {
                if (pkLen == 1)
                {
                    var PkColHandle = table.DataColumnInfo.PrimaryKeyColumns.First().ColumnHandle;
                    
                    return this[new ColumnHandle(PkColHandle)].GetHashCode();
                }

                var hashCode = 0;
                foreach (var container in table.DataColumnInfo.PrimaryKeyColumns)
                {
                    var value = this[new ColumnHandle(container.ColumnHandle)];

                    if (value != null)
                    {
                        hashCode ^= value.GetHashCode();
                    }
                    else
                    {
                        hashCode ^= 314159;
                    }
                }

                return hashCode;
            }

            return 0;
        }

        public CoreDataRow()
        {
        }

        public void Init(int rowHandle, [NotNull] CoreDataTable dataTable)
        {
            if (table != null)
            {
                throw new InvalidOperationException("Row was already initialized.");
            }

            RowHandleCore = rowHandle;
            m_tableName = dataTable.Name;

            table = dataTable;
        }

        public CoreDataRow(int rowHandle, CoreDataTable dataTable)
        {
            Init(rowHandle, dataTable);
        }

        public bool IsChangedRow
        {
            get
            {
                var rowState = RowRecordState;

                return rowState is RowState.Added or RowState.Modified or RowState.Deleted;
            }
        }

        public void SetModified()
        {
            if (table != null)
            {
                table.SetModifiedRowState(RowHandleCore);
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public void AcceptChanges()
        {
            if (table != null)
            {
                table.SetUnchangedRowState(RowHandleCore);
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public void SetAdded()
        {
            if (table != null)
            {
                table.SetAddedRowState(RowHandleCore);
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        string ICoreDataRowReadOnlyAccessor.ToString(ICoreTableReadOnlyColumn column)
        {
            return ToString(column.ColumnName);
        }

        string ICoreDataRowReadOnlyAccessor.ToString(string columnOrXProp)
        {
            return ToString(columnOrXProp);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreTableReadOnlyColumn> ICoreDataRowReadOnlyAccessor.GetColumns() => GetColumns();


        [NotNull]
        public virtual IReadOnlyList<string> GetTableColumnNames()
        {
            if (table != null)
            {
                return table.DataColumnInfo.Columns.Select(s => s.ColumnName).ToArray();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreTableReadOnlyColumn> ICoreDataRowReadOnlyAccessor.PrimaryKeyColumn => GetPrimaryKeyColumn();

        protected virtual IEnumerable<ICoreTableReadOnlyColumn> GetPrimaryKeyColumn()
        {
            if (table != null)
            {
                return table.PrimaryKey;
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public virtual IComparable[] GetRowKeyValue()
        {
            if (table != null)
            {
                var primaryKey = table.DataColumnInfo.PrimaryKeyColumns;

                var res = new IComparable[primaryKey.Length];

                for (int i = 0; i < res.Length; i++)
                {
                    res[i] = (IComparable)this[new ColumnHandle(primaryKey[i].ColumnHandle)];
                }

                return res;
            }

            return null;
        }

        protected virtual ICoreDataTableColumn GetColumnCore(string columnName)
        {
            var column = TryGetColumnCore(columnName);

            if (column == null)
            {
                throw new MissingMetadataException($"Column {columnName} doesn't exist in {m_tableName} table.");
            }

            return column;
        }

        ICoreTableReadOnlyColumn ICoreDataRowReadOnlyAccessor.GetColumn(string columnName)
        {
            return GetColumnCore(columnName);
        }

        ICoreTableReadOnlyColumn ICoreDataRowReadOnlyAccessor.GetColumn(int columnHandle)
        {
            return GetColumnCore(columnHandle);
        }

        protected virtual ICoreTableReadOnlyColumn GetColumnCore(int columnHandle)
        {
            if (table != null)
            {
                return (CoreDataColumn)table.GetColumn(columnHandle);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreTableReadOnlyColumn ICoreDataRowReadOnlyAccessor.TryGetColumn(string columnName) => TryGetColumnCore(columnName);

        public bool IsNull(ICoreTableReadOnlyColumn column)
        {
            if (column is CoreDataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, this.table))
            {
                return IsNull(dataColumn);
            }

            return IsNull(column.ColumnName);
        }

        public bool IsNotNull(ICoreTableReadOnlyColumn column)
        {
            if (column is CoreDataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, this.table))
            {
                return IsNull(dataColumn) == false;
            }

            return IsNull(column.ColumnName) == false;
        }

        public object this[ICoreTableReadOnlyColumn column]
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

        object ICoreDataRowReadOnlyAccessor.this[ICoreTableReadOnlyColumn column]
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

        [CanBeNull]
        protected virtual ICoreDataTableColumn TryGetColumnCore(string columnName)
        {
            if (table != null)
            {
                return table.TryGetColumn(columnName);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public T Field<T>(ICoreTableReadOnlyColumn column, T defaultIfNull)
        {
            if (column is CoreDataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, this.table))
            {
                return Field(dataColumn, defaultIfNull);
            }

            return Field(column.ColumnName, defaultIfNull);
        }

        public T Field<T>(ICoreTableReadOnlyColumn column)
        {
            if (column is CoreDataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, this.table))
            {
                return Field<T>(dataColumn);
            }

            return Field<T>(column.ColumnName);
        }

        public void SilentlySetValue(string columnOrXProp, object value)
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                return;
            }
            
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DebugKeyValue}' row from '{table.Name}' table default because it is readonly.");
            }

            if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var dataColumn) == false)
            {
                if (HasXProperty(columnOrXProp))
                {
                    SetXProperty(columnOrXProp, (IComparable)value);
                }
                
                return;
            }
            
            table.SilentlySetRowValue(RowHandleCore, value, dataColumn);
        }

        public void SilentlySetValue(ICoreTableReadOnlyColumn column, object value)
        {
            if (column is CoreDataColumn dataColumn)
            {
                SilentlySetValue(dataColumn, value);
            }

            SilentlySetValue(column.ColumnName, value);
        }

        public ICoreDataRowAccessor Clone()
        {
            return ToContainer();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowAccessor ICoreDataRowAccessor.SetNull(string columnOrXProp)
        {
            return SetNull(columnOrXProp);
        }

        public ICoreDataRowAccessor SetNull(ICoreTableReadOnlyColumn column)
        {
            if (column is CoreDataColumn dataColumn)
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
        object ICoreDataRowAccessor.this[int handle]
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
        public ICoreDataRowAccessor Set<T>(ICoreTableReadOnlyColumn column, T value)
        {
            this[column.ColumnName] = value;

            return this;
        }

        [NotNull]
        public CoreDataRow SetNull(string column)
        {
            this[column] = null;

            return this;
        }

        public virtual bool IsExistsField(string columnName)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var _);

            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public bool IsChanged(string field)
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (RowRecordState == RowState.Unchanged)
            {
                return false;
            }

            if (table.DataColumnInfo.ColumnMappings.TryGetValue(field, out var dataColumn))
            {
                return dataColumn.DataStorageLink.IsCellChanged(RowHandle, dataColumn);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public object GetOriginalValue(ICoreTableReadOnlyColumn column)
        {
            if (column is CoreDataColumn dc && ReferenceEquals(table, dc.DataTable))
            {
                return GetOriginalValue(dc);
            }
            
            return GetOriginalValue(column.ColumnName);
        }

        public T GetOriginalValue<T>(ICoreTableReadOnlyColumn column)
        {
            if (column is CoreDataColumn dc)
            {
                return GetOriginalValue<T>(dc);
            }
            
            return GetOriginalValue<T>(column.ColumnName);
        }

        public virtual ulong GetColumnAge(string columnName)
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                return 0;
            }

            if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var dataColumn) == false)
            {
                return 0;
            }

            return dataColumn.DataStorageLink.GetAge(RowHandleCore, dataColumn);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowAccessor ICoreDataRowAccessor.Set<T>(string columnOrXProp, T value) 
        {
            return SetValueCore(columnOrXProp, value);
        }

        public IEnumerable<ICoreTableReadOnlyColumn> GetColumns()
        {
            return GetColumnsCore();
        }

        protected virtual IEnumerable<ICoreTableReadOnlyColumn> GetColumnsCore()
        {
            if (table != null)
            {
                return table.GetColumns();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public virtual IEnumerable<string> GetChangedFields()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return ChangedFieldsCore(table, table.GetColumns());
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public virtual IEnumerable<string> GetChangedXProperties()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return ChangedXPropertiesCore(table);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public IEnumerable<string> GetChangedFields(IEnumerable<CoreDataColumn> columns)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return ChangedFieldsCore(table, columns);
            }

            return Enumerable.Empty<string>();
        }

        protected virtual IEnumerable<string> ChangedFieldsCore(CoreDataTable table, IEnumerable<CoreDataColumn> columns)
        {
            var dataRow = this;

            var rowState = dataRow.RowRecordState;

            if (rowState == RowState.Unchanged)
            {
                yield break;
            }

            foreach (var c in columns)
            {
                if (ReferenceEquals(c.DataTable, this.table))
                {
                    if ((table.IsReadOnlyColumn(c) == false) &&
                        rowState == RowState.Added
                            ? c.DataStorageLink.IsNull(RowHandleCore, c) == false
                            : c.DataStorageLink.IsCellChanged(RowHandleCore, c))
                    {
                        yield return c.ColumnName;
                    }
                }
                else
                {
                    var column = (CoreDataColumn)GetColumnCore(c.ColumnName);

                    if ((table.IsReadOnlyField(c.ColumnName) == false) &&
                        rowState == RowState.Added
                            ? c.DataStorageLink.IsNull(RowHandleCore, c) == false
                            : c.DataStorageLink.IsCellChanged(RowHandleCore, c))
                    {
                        yield return c.ColumnName;
                    }
                }
            }
        }

        private IEnumerable<string> ChangedXPropertiesCore(CoreDataTable table)
        {
            var dataRow = this;

            var rowState = dataRow.RowRecordState;

            if (rowState == RowState.Unchanged)
            {
                return Enumerable.Empty<string>();
            }

            return table.StateInfo.GetChangedXProperties(RowHandleCore);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        object ICoreDataRowReadOnlyAccessor.this[int handle] => this[new ColumnHandle(handle)];

        public virtual ulong GetRowAge()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.GetRowAge(RowHandleCore);
            }

            return ulong.MinValue;
        }

        public virtual int GetColumnCount()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.ColumnCount;
            }

            return 0;
        }

        public void Delete()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                table.DeleteRow(RowHandleCore);
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public bool IsModified
        {
            get
            {
                if (table != null)
                {
                    return table.StateInfo.GetRowState(RowHandleCore) == RowState.Modified;
                }

                return false;
            }
        }

        public bool IsModifiedOrAdded
        {
            get
            {
                if (table != null)
                {
                    var state = table.StateInfo.GetRowState(RowHandleCore);

                    return state == RowState.Modified || state == RowState.Added;
                }

                return false;
            }
        }

        public bool HasChanges
        {
            get
            {
                if (table != null)
                {
                    return table.StateInfo.GetRowState(RowHandleCore) != RowState.Unchanged;
                }

                return false;
            }
        }

        public bool IsUnchangedRow
        {
            get
            {
                if (table != null)
                {
                    return table.StateInfo.GetRowState(RowHandleCore) == RowState.Unchanged;
                }

                return false;
            }
        }

        public bool IsAddedRow
        {
            get
            {
                if (table != null)
                {
                    return table.StateInfo.GetRowState(RowHandleCore) == RowState.Added;
                }

                return false;
            }
        }

        public bool IsDeletedRow
        {
            get
            {
                if (table != null)
                {
                    return table.StateInfo.GetRowState(RowHandleCore) == RowState.Deleted;
                }

                return false;
            }
        }

        public bool IsDetachedRow
        {
            get
            {
                if (table != null)
                {
                    return table.StateInfo.GetRowState(RowHandleCore) == RowState.Detached;
                }

                return true;
            }
        }

        public bool IsValidRowState
        {
            get
            {
                if (table != null)
                {
                    var state = table.StateInfo.GetRowState(RowHandleCore);

                    return state != RowState.Deleted && state != RowState.Detached;
                }

                return false;
            }
        }

        public virtual RowState RowRecordState
        {
            get
            {
                if (table != null)
                {
                    return table.GetRowState(RowHandleCore);
                }
               
                return RowState.Disposed;
            }
        }

        [CanBeNull]
        public virtual T GetXProperty<T>(string xPropertyName, bool original = false)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var value = table.StateInfo.GetExtendedProperty(RowHandleCore, xPropertyName, original);
             
                return XPropertyValueConverter.TryConvert<T>("Row", xPropertyName, value);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }
        
        public virtual object GetXProperty(string xPropertyName, bool original = false) 
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.StateInfo.GetExtendedProperty(RowHandleCore, xPropertyName, original);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public virtual void SetXProperty<T>(string propertyCode, T value) 
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                table.SetRowXProperty(RowHandleCore, propertyCode, value);
                
                return;
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public virtual IEnumerable<string> GetXProperties()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.StateInfo.GetExtendedProperties(RowHandleCore)?.Keys ?? Array.Empty<string>();
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public CoreDataRowContainer ToContainer()
        {
            return ToContainerCore();
        }

        protected virtual CoreDataRowContainer ToContainerCore()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var container = table.CreateDataRowContainer(RowHandleCore);

                if (container == null)
                {
                    throw new InvalidOperationException($"Cannot create row container for {DebugKeyValue} row.");
                }

                return container;
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public static KeyValuePair<string, string>[] GetItemKeyStringValuePairArray(CoreDataRow row, IReadOnlyDictionary<int, string> columnFormat = null)
        {
            var objects = new KeyValuePair<string, string>[row.table.DataColumnInfo.ColumnsCount];

            var colCount =  row.table.DataColumnInfo.ColumnsCount;

            for (int columnHandle = 0; columnHandle < colCount; columnHandle++)
            {
                var column = row.table.GetColumn(columnHandle);

                var format = columnFormat?.GetOrDefault(columnHandle);

                objects[columnHandle] = new KeyValuePair<string, string>(column.ColumnName, row.ToString(column, format));
            }

            return objects;
        }

        public static object[] GetItemArray(CoreDataRow row)
        {
            var objects = new object[row.table.DataColumnInfo.ColumnsCount];

            for (int columnHandle = 0; columnHandle < row.table.DataColumnInfo.ColumnsCount; columnHandle++)
            {
                objects[columnHandle] = row[new ColumnHandle(columnHandle)];
            }

            return objects;
        }
        
        public IDataEditTransaction StartTransaction()
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.StartRowTransaction(RowHandleCore);
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }


        public bool IsRowInTransaction
        {
            get
            {
                if (table != null)
                {
                    return table.GetIsInTransaction();
                }

                return false;
            }
        }

        public void EditRow<T>(Action<T> action) where T : IDataRowAccessor
        {
            var transaction = StartTransaction();

            try
            {
                action((T)(IDataRowAccessor)this);

                transaction.Commit();
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        void ICoreDataRowAccessor.CopyChanges(ICoreDataRowAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            CopyChanges(rowAccessor, skipFields);
        }

        public void CopyChanges(ICoreDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot copy changes to '{DebugKeyValue}' row of '{table.Name}' table because it is readonly.");
            }
            
            var changedFields = rowAccessor.GetChangedFields().ToHashSet();

            var rowAccessorColumns = rowAccessor.GetColumns();

            var unchangedFields = new HashSet<string>();

            foreach (var rowAccessorColumn in rowAccessorColumns)
            {
                if (!(changedFields.Contains(rowAccessorColumn.ColumnName)))
                {
                    unchangedFields.Add(rowAccessorColumn.ColumnName);
                }
            }

            var skipF = unchangedFields.TryCombine(skipFields);

            CopyFrom(rowAccessor, skipF);
        }

        void ICoreDataRowAccessor.CopyFrom([NotNull] ICoreDataRowAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            CopyFrom(rowAccessor, skipFields);
        }
        
        public virtual void CopyFrom([NotNull] ICoreDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            if (rowAccessor == null)
            {
                throw new ArgumentNullException(nameof(rowAccessor));
            }
            if (table == null)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot copy data to '{DebugKeyValue}' row of '{table.Name}' table because it is readonly.");
            }
            
            if (RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
          
            foreach (var column in rowAccessor.GetColumns())
            {
                if (skipFields.ListNullOrItemAbsent(column.ColumnName))
                {
                    var dataColumn = table.TryGetColumn(column.ColumnName);

                    if (dataColumn != null && table.IsReadOnlyColumn(dataColumn) == false)
                    {
                        this[dataColumn] = rowAccessor[column];
                    }
                }
            }

            foreach (var xProperty in rowAccessor.GetXProperties())
            {
                if (skipFields.ListNullOrItemAbsent(xProperty))
                {
                    table.SetRowXProperty(RowHandleCore, xProperty, rowAccessor.GetXProperty<object>(xProperty));
                }
            }
        }
        
        public virtual void CopyAll([NotNull] ICoreDataRowReadOnlyAccessor rowAccessor)
        {
            if (rowAccessor == null)
            {
                throw new ArgumentNullException(nameof(rowAccessor));
            }
            if (table == null)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot copy all data to '{DebugKeyValue}' row of '{table.Name}' table because it is readonly.");
            }
            
            if (RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
           
            foreach (var column in rowAccessor.GetColumns())
            {
                var dataColumn = table.TryGetColumn(column.ColumnName);
                    
                if (dataColumn != null && table.IsReadOnlyColumn(dataColumn) == false)
                {
                    this[dataColumn] = rowAccessor[column];
                }
            }

            foreach (var xProperty in rowAccessor.GetXProperties())
            {
                table.SetRowXProperty(RowHandleCore, xProperty, rowAccessor.GetXProperty<object>(xProperty));
            }
        }

        public bool HasVersion(DataRowVersion version)
        {
            switch (version)
            {
                case DataRowVersion.Current: return true;
                case DataRowVersion.Original: return RowRecordState == RowState.Modified;
            }

            return false;
        }

        public void SetParentRow([NotNull] string relationName, CoreDataRow parentRow = null)
        {
            if (this.table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup parent row reference for '{DebugKeyValue}' row of '{table.Name}' table because table is readonly.");
            }

            var dataRelation = table.ParentRelationsMap?.GetOrDefault(relationName);

            if (dataRelation == null)
            {
                throw new ArgumentNullException($"Cannot setup parent row reference for '{DebugKeyValue}' row of '{table.Name}' table because the given relation '{relationName}' is missing.");
            }

            SetParentRow(dataRelation, parentRow);
        }

        public void SetParentRow([NotNull] DataRelation relation, CoreDataRow parentRow = null)
        {
            if (this.table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup parent row reference for '{DebugKeyValue}' row of '{table.Name}' table because table is readonly.");
            }
            
            if (relation == null)
            {
                throw new ArgumentNullException(nameof(relation));
            }
            
            SetParentRowCore(relation, parentRow);
        }
        
        public void SetParentRow(IDataRelation relation, IDataRowReadOnlyAccessor parentRow = null)
        {
            if (this.table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup parent row reference for '{DebugKeyValue}' row of '{table.Name}' table because table is readonly.");
            }
            
            if (relation == null)
            {
                throw new ArgumentNullException(nameof(relation));
            }

            if (relation is DataRelation dr)
            {
                SetParentRowCore(dr, parentRow);
                
                return;
            }
            
            if (parentRow is not null)
            {
                var parentColumns = relation.ParentColumns.ToArray();
                var childColumns = relation.ChildColumns.ToArray();

                if (parentColumns.Length == 1)
                {
                    this[parentColumns[0]] = parentRow[childColumns[0]];
                }
                else
                {
                    var transaction = this.StartTransaction();
                    
                    try
                    {
                        for (var index = 0; index < parentColumns.Length; index++)
                        {
                            this[childColumns[index]] = parentRow[parentColumns[index]];
                        }
                        
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
            else
            {
                var childColumns = relation.ChildColumns.ToArray();

                if (childColumns.Length == 1)
                {
                    this.SetNull(childColumns[0]);
                }
                else
                {
                    var transaction = this.StartTransaction();
                    
                    try
                    {
                        foreach (var childColumn in childColumns)
                        {
                            this.SetNull(childColumn);
                        }
                        
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }

        protected void SetParentRowCore(DataRelation relation, ICoreDataRowReadOnlyAccessor parentRow)
        {
            if (parentRow is not null)
            {
                if (relation.ChildKeyRef.Columns.Count == 1)
                {
                    this[new ColumnHandle(relation.ChildKeyRef.Columns[0])] = parentRow[relation.ParentKeyRef.Columns[0]];
                }
                else
                {
                    var parentColumns = relation.ParentKeyRef.Columns;
                    var childColumns = relation.ChildKeyRef.Columns;

                    var transaction = this.StartTransaction();

                    try
                    {
                        for (var index = 0; index < parentColumns.Count; index++)
                        {
                            this[new ColumnHandle(childColumns[index])] = parentRow[parentColumns[index]];
                        }
                        
                        transaction.Commit();
                    }
                    catch 
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
             
            }
            else
            {
                if (relation.ChildKeyRef.Columns.Count == 1)
                {
                    this.SetNull(new ColumnHandle(relation.ChildKeyRef.Columns[0]));
                }
                else
                {
                    var transaction = this.StartTransaction();
                    
                    try
                    {
                        foreach (var childColumn in relation.ChildKeyRef.Columns)
                        {
                            this.SetNull(new ColumnHandle(childColumn));
                        }
                        
                        transaction.Commit();
                    }
                    catch 
                    {
                        transaction.Rollback();
                        throw;
                    }
                }
            }
        }
    }
}