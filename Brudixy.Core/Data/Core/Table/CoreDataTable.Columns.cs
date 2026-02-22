using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Brudixy.Constraints;
using Brudixy.Converter;
using Brudixy.EventArgs;
using Brudixy.Exceptions;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataTable 
    {
        [DebuggerStepThrough]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        [NotNull]
        ICoreTableReadOnlyColumn ICoreReadOnlyDataTable.GetColumn(string name) => GetColumn(name);

        [DebuggerStepThrough]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        [NotNull]
        ICoreDataTableColumn ICoreDataTable.GetColumn(string name) => GetColumn(name);

        [DebuggerStepThrough]
        [NotNull]
        public CoreDataColumn GetColumn(string name)
        {
            if (ColumnMapping.TryGetValue(name, out var dataColumn))
            {
                return dataColumn;
            }

            throw new MissingMetadataException($"Column {name} doesn't exists in table '{Name}'");
        }

        [DebuggerStepThrough]
        [NotNull]
        public CoreDataColumn GetColumn(int columnHandle) => GetDataColumnInstance(columnHandle);

        [DebuggerStepThrough]
        [NotNull]
        public ColumnHandle GetColumnHandle(string name)
        {
            if (ColumnMapping.TryGetValue(name, out var column))
            {
                return new ColumnHandle(column.ColumnHandle);
            }

            throw new MissingMetadataException($"Column {name} doesn't exists in table '{Name}'");
        }
        
        ICoreTableReadOnlyColumn ICoreReadOnlyDataTable.TryGetColumn(string name) => TryGetColumn(name);

        ICoreDataTableColumn ICoreDataTable.TryGetColumn(string name) => TryGetColumn(name);

        public CoreDataColumn TryGetColumn(string name)
        {
            return ColumnMapping.GetValueOrDefault(name);
        }
        
        public T GetDefaultNullValue<T>(string column)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(column, out var dataColumn))
            {
                var defaultValue = dataColumn.DefaultValue;

                if (defaultValue == null)
                {
                    return TypeConvertor.ReturnDefault<T>();
                }

                return (T)defaultValue;
            }

            return TypeConvertor.ReturnDefault<T>();
        }

        public T GetDefaultNullValue<T>(int columnHandle)
        {
            if (columnHandle < 0 || columnHandle >= DataColumnInfo.ColumnsCount)
            {
                return TypeConvertor.ReturnDefault<T>();
            }

            var dataColumn = DataColumnInfo.Columns[columnHandle];
            
            object defaultValueFromMetadata = dataColumn.DefaultValue;

            if (defaultValueFromMetadata != null)
            {
                if(defaultValueFromMetadata is T tv)
                {
                    return tv;
                }
                else
                {
                    return CoreDataRow.TryConvertValue<T>(this, dataColumn, defaultValueFromMetadata, "default null value");
                }
            }

            return TypeConvertor.ReturnDefault<T>();
        }

        public T GetDefaultNullValue<T>(CoreDataColumn column)
        {
            var columnHandle = column.ColumnHandle;

            var dataColumn = DataColumnInfo.Columns[columnHandle];
            
            object defaultValueFromMetadata = dataColumn.DefaultValue;

            if (defaultValueFromMetadata != null)
            {
                if(defaultValueFromMetadata is T tv)
                {
                    return tv;
                }
                else
                {
                    return CoreDataRow.TryConvertValue<T>(this, dataColumn, defaultValueFromMetadata, "default null value");
                }
            }

            return TypeConvertor.ReturnDefault<T>();
        }
        
        public T NextAutoIncrementValue<T>([NotNull] CoreDataColumn column)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }
            
            var dataItem = column.DataStorageLink;

            if (dataItem is ITypedDataItem<T> dt)
            {
                return dt.GetAutomaticValueTyped(column);
            }
            
            var nextAutoIncrementValue = dataItem.GetAutomaticValue(column);
            
            return Tool.ConvertBoxed<T>(nextAutoIncrementValue);
        }
        
        public IComparable GetAggregatedValue([NotNull] CoreDataColumn column, AggregateType type)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }

            if (StateInfo.RowStorageCount == 0)
            {
                return null;
            }
            
            var dataItem = column.DataStorageLink;

            return (IComparable)dataItem.GetAggregateValue(StateInfo.RowHandles, type, column);
        }
        
        public void SetupColumnAutomaticValue(int columnHandle, bool value)
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column from '{Name}' table because columns are readonly."); 
            }
                
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column from '{Name}' table because it is readonly.");
            }
            
            DataColumnInfo.SetAutomaticValueColumn(columnHandle, value);

            OnMetadataChanged();
        }

        public void SetupColumnDefaultValue(int columnHandle, object value)
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column from '{Name}' table because columns are readonly."); 
            }
                
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column from '{Name}' table because it is readonly.");
            }
            
            DataColumnInfo.SetDefaultValue(columnHandle, value);

            OnMetadataChanged();
        }

        public void SetupColumnMaxLen(int columnHandle, uint? value)
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column from '{Name}' table because columns are readonly."); 
            }
                
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column from '{Name}' table because it is readonly.");
            }
            
            DataColumnInfo.ColumnSetMaxLength(columnHandle, value);

            OnMetadataChanged();
        }

        public void ChangeColumnUnique(int columnHandle, bool value)
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column from '{Name}' table because columns are readonly."); 
            }
                
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column from '{Name}' table because it is readonly.");
            }
            
            DataColumnInfo.SetUniqueColumn(columnHandle, value);

            if (value)
            {
                AddIndex(DataColumnInfo.Columns[columnHandle].ColumnName, true);
            }
            else
            {
                RemoveIndex(DataColumnInfo.Columns[columnHandle].ColumnName);
            }
            OnMetadataChanged();
        }
        
        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreTableReadOnlyColumn> ICoreReadOnlyDataTable.Columns => GetColumns();

        [NotNull]
        public IEnumerable<CoreDataColumn> GetColumns()
        {
            var columnsCount = DataColumnInfo.ColumnsCount;

            for (int i = 0; i < columnsCount; i++)
            {
                yield return GetDataColumnInstance(i);
            }
        }

        public bool RemoveColumn(string column)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(column, out var dataColumn))
            {
                return RemoveColumnCore(dataColumn);
            }

            return false;
        }

        protected bool RemoveColumnCore(CoreDataColumn column)
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove '{column.ColumnName}' column from '{Name}' table because columns are readonly.");
            }

            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove '{column.ColumnName}' column from '{Name}' table because it is readonly.");
            }

            var canRemove = CanRemove(column, true);

            if (canRemove == false)
            {
                return false;
            }

            return RemoveColumnCoreCore(column);
        }

        private bool RemoveColumnCoreCore(CoreDataColumn column)
        {
            var oldToNewMap = new Map<int, int>();
            oldToNewMap[column.ColumnHandle] = ColumnCount - 1;

            for (int i = column.ColumnHandle + 1; i < ColumnCount; i++)
            {
                oldToNewMap[i] = i - 1;
            }

            if (IndexInfo.HasAny)
            {
                IndexInfo.Remove(column.ColumnHandle);
            }

            if (oldToNewMap.Count > 0)
            {
                RemapColumnHandles(oldToNewMap);
            }
            
            OnColumnRemoved(column.ColumnHandle);

            DataColumnInfo.Remove(column);
            
            oldToNewMap.Dispose();

            return true;
        }

        public bool CanRemoveColumn(string columnName)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var columnHandle) == false)
            {
                return false;
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                return false;
            }
                
            return CanRemove(columnHandle, false);
        }

        protected bool CanRemove(CoreDataColumn column, bool isThrowEx)
        {
            if (m_areColumnsReadonly)
            {
                if (isThrowEx)
                {
                    throw new ReadOnlyAccessViolationException($"Cannot remove {column.ColumnName} column from '{Name}' data table, because columns are readonly.");
                }

                return false;
            }
            
            if(DataColumnInfo.CanRemove(column, isThrowEx, Name) == false)
            {
                return false;
            }
            
            if (ParentRelationsMap != null)
            {
                foreach (var parentRelation in ParentRelationsMap)
                {
                    var relation = parentRelation.Value;
                    var relationName = parentRelation.Key;

                    var dataKey = relation.ChildKey;
                    
                    if (dataKey.ContainsColumn(column.ColumnHandle))
                    {
                        if (!isThrowEx)
                        {
                            return false;
                        }
                        throw new ConstraintException($"Cannot remove {column.ColumnName} column from '{Name}' data table, because it is exists in parent key of {relationName} relation.");
                    }
                }
            }

            if (ChildRelationsMap != null)
            {
                foreach (var childRelation in ChildRelationsMap)
                {
                    var relation = childRelation.Value;
                    var relationName = childRelation.Key;
                    
                    var dataKey = relation.ChildKey;
                    if (dataKey.ContainsColumn(column.ColumnHandle))
                    {
                        if (!isThrowEx)
                        {
                            return false;
                        }
                        throw new ConstraintException($"Cannot remove {column.ColumnName} column because it is exists in child key of {relationName} relation.");
                    }
                }
            }

            if (MultiColumnIndexInfo.HasAny)
            {
                if (ColumnIsPresentInMultiIndex(column.ColumnHandle))
                {
                    return false;
                }
            }
            
            return true;
        }

        private bool ColumnIsPresentInMultiIndex(int columnHandle)
        {
            foreach (var indexesOfMany in MultiColumnIndexInfo.Indexes)
            {
                if (indexesOfMany.Columns.Contains(columnHandle))
                {
                    return true;
                }
            }

            return false;
        }

        public bool HasColumn(string columnName) => DataColumnInfo.ColumnMappings.ContainsKey(columnName);

        public void ClearColumns()
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot clear columns from '{Name}' table because columns are readonly."); 
            }
            
            OnClearColumns();
            
            ClearRows();
        
            var columns = System.Linq.Enumerable.Reverse(DataColumnInfo.Columns)
                .ToArray();
            
            foreach (var column in columns)
            {
                if (CanRemove(column, false))
                {
                    RemoveColumnCoreCore(column);
                }
            }

            OnMetadataChanged();
        }

        public bool RemoveColumn(int columnHandle)
        {
            var column = DataColumnInfo.Columns[columnHandle];

            return RemoveColumnCore(column);
        }

        public void SetPrimaryKeyColumn(string column)
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change primary key of the '{Name}' table because columns are readonly.");
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup primary key for '{column}' for the '{Name}' table because  is readonly.");
            }
            
            if (string.IsNullOrEmpty(column))
            {
                DropPrimaryKey();
                
                return;
            }
            
            var dataColumn = GetColumn(column);

            SetPrimaryKeyColumnCore(dataColumn);
        }

        protected void SetPrimaryKeyColumnCore(CoreDataColumn dataColumn)
        {
            DropPrimaryKey();

            DataColumnInfo.SetPrimaryKeyColumn(dataColumn);

            AddIndex(dataColumn, true);
        }

        public void SetPrimaryKeyColumns(IReadOnlyList<string> primaryKeyColumns)
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change primary key of the '{Name}' table because columns are readonly.");
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup primary key for '{string.Join(";", primaryKeyColumns)}' for the '{Name}' table because  is readonly.");
            }
            
            var dataColumns = new Data<CoreDataColumn>();

            for (var index = 0; index < primaryKeyColumns.Count; index++)
            {
                var column = primaryKeyColumns[index];
                var dataColumn = GetColumn(column);

                dataColumns.Add(dataColumn);
            }

            SetPrimaryKeyColumnsCore(dataColumns);
            
            dataColumns.Dispose();
        }
        
        protected void SetPrimaryKeyColumnsCore(IReadOnlyList<CoreDataColumn> primaryKeyColumns)
        {
            DropPrimaryKey();

            DataColumnInfo.SetPrimaryKeyColumns(primaryKeyColumns);

            if (primaryKeyColumns.Count > 0)
            {
                if (primaryKeyColumns.Count == 1)
                {
                    AddIndex(primaryKeyColumns[0], true);
                }
                else
                {
                    AddNewMultiColumnIndex(primaryKeyColumns.Select(c => c.ColumnHandle).ToArray(), true);
                }
            }
        }

        private void DropPrimaryKey()
        {
            if (DataColumnInfo.PrimaryKeyColumns.Length > 0)
            {
                if (DataColumnInfo.PrimaryKeyColumns.Length == 1)
                {
                    RemoveIndexCore(DataColumnInfo.PrimaryKeyColumns.First().ColumnHandle);
                }
                else
                {
                    RemoveMultiColumnIndexCore(DataColumnInfo.PrimaryKeyColumns.Select(c => c.ColumnHandle).ToArray());
                }
            }

            DataColumnInfo.DropPrimaryKey();
        }

        public IEnumerable<ICoreDataTableColumn> PrimaryKey
        {
            get
            {
                foreach (var columnHandle in DataColumnInfo.PrimaryKeyColumns)
                {
                    yield return GetColumn(columnHandle.ColumnHandle);
                }
            }
        }

        public int PrimaryKeyColumnCount => DataColumnInfo.PrimaryKeyColumns.Length;

        public MaxColumnLenConstraintDataEvent MaxColumnLenConstraint => IsDisposed ? null : m_maxColumnLenConstraint ??= new (m_disposables);

        [CanBeNull]
        public CoreDataColumn TryGetUniqueIndex()
        {
            var uniqueIndexHandle = GetUniqueIndexColumnHandle();

            if (uniqueIndexHandle >= 0)
            {
                return GetDataColumnInstance(uniqueIndexHandle);
            }

            return null;
        }

        protected CoreDataColumn GetDataColumnInstance(int columnHandle)
        {
            return DataColumnInfo.Columns[columnHandle];
        }

        protected virtual CoreDataColumn CreateColumnInstance(CoreDataColumnObj columnObj) => new(this, columnObj);
        protected virtual CoreDataColumnContainer CreateColumnContainerInstance(CoreDataColumnObj columnObj) => new(columnObj);

        private int GetUniqueIndexColumnHandle()
        {
           return DataColumnInfo.Columns.FindIndex(c => c.IsUnique);
        }

        public ICoreDataTableColumn AddColumn([NotNull]ICoreTableReadOnlyColumn column)
        {
            if (column is CoreDataColumnContainer dataColumn)
            {
                return AddColumn(dataColumn);
            }

            return AddColumn(
                column.ColumnName,
                column.Type,
                auto: column.IsAutomaticValue,
                unique: column.IsUnique,
                columnMaxLength: column.MaxLength,
                defaultValue: column.DefaultValue);
        }

        public CoreDataColumn AddColumn([NotNull]CoreDataColumnContainer column)
        {
            var dc = AddColumn(
                column.ColumnName,
                column.Type,
                column.TypeModifier,
                column.DataType,
                column.IsAutomaticValue ? true : new bool?(),
                column.IsUnique ? true : new bool?(),
                column.MaxLength,
                column.DefaultValue,
                xProps: column.GetXProperties());

            return dc;
        }

        public virtual CoreDataColumn AddColumn<T>(string columnName,
            T defaultValue = default,
            bool builtin = false,
            bool serviceColumn = false,
            IReadOnlyDictionary<string, object> xProps = null)
            where T : class,
            ICloneable,
            IXmlSerializable,
            IJsonSerializable,
            new()
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new  '{columnName}' column to the '{Name}' table because columns are readonly.");
            }

            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new  '{columnName}' column to the '{Name}' table because it is readonly.");
            }
            
            if (DataColumnInfo.ColumnMappings.ContainsKey(columnName))
            {
                throw new ArgumentException(
                    $"Cannot add new '{columnName}' column to the '{Name}' table because it exists.");
            }

            var columnHandle = DataColumnInfo.ColumnsCount;

            var safeDafaultValue = (defaultValue as ICloneable)?.Clone() ?? defaultValue;

            var dataColumnContainer = CreateDataColumnContainerBuilder(columnName,
                TableStorageType.UserType, 
                TableStorageTypeModifier.Complex,
                typeof(T),
                false,
                false,
                null,
                safeDafaultValue,
                builtin,
                serviceColumn,
                true,
                columnHandle);

            var dataColumn = CreateColumnInstance(dataColumnContainer.ToImmutable());
            
            DataColumnInfo.Columns.Add(dataColumn);
            DataColumnInfo.ColumnMappings[columnName] = dataColumn;

            if (xProps != null)
            {
                foreach (var xProp in xProps)
                {
                    dataColumnContainer.SetXProperty(xProp.Key, xProp.Value);
                }
            }

            if (StateInfo.RowCount > 0)
            {
                var item = dataColumn.DataStorageLink;

                item.CreateEmptyRows(StateInfo.RowCount, dataColumn);
            }

            OnMetadataChanged();

            return dataColumn;
        }

        public virtual CoreDataColumn AddColumn(string columnName,
            TableStorageType valueType = TableStorageType.String,
            TableStorageTypeModifier valueTypeModifier = TableStorageTypeModifier.Simple,
            Type type = null,
            bool? auto = null,
            bool? unique = null,
            uint? columnMaxLength = null,
            object defaultValue = null,
            bool builtin = false,
            bool serviceColumn = false,
            bool allowNull = true,
            IReadOnlyCollection<KeyValuePair<string, object>> xProps = null)
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new  '{columnName}' column to the '{Name}' table because columns are readonly.");
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new  '{columnName}' column to the '{Name}' table because it is readonly.");
            }

            if (DataColumnInfo.ColumnMappings.ContainsKey(columnName))
            {
                throw new ArgumentException(
                    $"Cannot add new '{columnName}' column to the '{Name}' table because it exists.");
            }
            
            var columnHandle = DataColumnInfo.ColumnsCount;

            var dataColumnContainer = CreateDataColumnContainerBuilder(columnName,
                valueType, 
                valueTypeModifier, 
                type,
                auto ?? false,
                unique ?? false, 
                columnMaxLength, 
                (defaultValue as ICloneable)?.Clone() ?? defaultValue,
                builtin, 
                serviceColumn,
                allowNull, 
                columnHandle);

            var dataColumn = CreateColumnInstance(dataColumnContainer.ToImmutable());
            
            DataColumnInfo.Columns.Add(dataColumn);
            DataColumnInfo.ColumnMappings[columnName] = dataColumn;

            if (xProps != null)
            {
                foreach (var xProp in xProps)
                {
                    dataColumnContainer.SetXProperty(xProp.Key, xProp.Value);
                }
            }

            if (unique ?? false)
            {
                AddIndex(columnName, true);
            }

            if (StateInfo.RowCount > 0)
            {
                var item = dataColumn.DataStorageLink;
                
                item.CreateEmptyRows(StateInfo.RowCount, dataColumn);
            }

            OnColumnAdded();
            
            OnMetadataChanged();

            return dataColumn;
        }

        protected virtual void OnColumnAdded()
        {
        }

        public virtual CoreDataColumn AddColumn(string columnName,
            Type type = null,
            bool? auto = null,
            bool? unique = null,
            uint? columnMaxLength = null,
            object defaultValue = null,
            bool builtin = false,
            bool serviceColumn = false,
            bool allowNull = true,
            IReadOnlyDictionary<string, object> xProps = null)
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new  '{columnName}' column to the '{Name}' table because columns are readonly.");
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new  '{columnName}' column to the '{Name}' table because it is readonly.");
            }
            
            if (DataColumnInfo.ColumnMappings.ContainsKey(columnName))
            {
                throw new ArgumentException(
                    $"Cannot add new '{columnName}' column to the '{Name}' table because it exists.");
            }

            var (valueTypeModifier, valueType) = GetColType(type);

            var columnHandle = DataColumnInfo.ColumnsCount;

            var dataColumnContainer = CreateDataColumnContainerBuilder(columnName,
                valueType, 
                valueTypeModifier, 
                type,
                auto ?? false,
                unique ?? false, 
                columnMaxLength, 
                (defaultValue as ICloneable)?.Clone() ?? defaultValue,
                builtin, 
                serviceColumn,
                allowNull, 
                columnHandle);

            var dataColumn = CreateColumnInstance(dataColumnContainer.ToImmutable());
            
            DataColumnInfo.Columns.Add(dataColumn);
            DataColumnInfo.ColumnMappings[columnName] = dataColumn;

            if (xProps != null)
            {
                foreach (var xProp in xProps)
                {
                    dataColumnContainer.SetXProperty(xProp.Key, xProp.Value);
                }
            }

            if (unique ?? false)
            {
                AddIndex(columnName, true);
            }

            if (StateInfo.RowCount > 0)
            {
                var item = dataColumn.DataStorageLink;
                
                item.CreateEmptyRows(StateInfo.RowCount, dataColumn);
                item.SetAllNull(dataColumn);
            }
            
            OnColumnAdded();

            OnMetadataChanged();

            return dataColumn;
        }

        protected static (TableStorageTypeModifier valueTypeModifier, TableStorageType valueType) GetColType(Type type)
        {
            TableStorageTypeModifier valueTypeModifier;
            TableStorageType valueType;
            
            if (type == null)
            {
                valueType = TableStorageType.String;
                valueTypeModifier = TableStorageTypeModifier.Simple;
            }
            else
            {
                var columnType = CoreDataTable.GetColumnType(type);
                
                valueType = columnType.type;
                valueTypeModifier = columnType.typeModifier;
            }

            return (valueTypeModifier, valueType);
        }

        protected virtual CoreDataColumnContainerBuilder CreateDataColumnContainerBuilder(string columnName, 
            TableStorageType valueType,
            TableStorageTypeModifier valueTypeModifier,
            Type type,
            bool autoIncrement, 
            bool unique, 
            uint? columnMaxLength,
            object defaultValue,
            bool builtin, 
            bool serviceColumn,
            bool allowNull, 
            int columnHandle)
        {
            var dataColumnContainer = new CoreDataColumnContainerBuilder()
            {
                ColumnName = columnName,
                Type = valueType,
                TypeModifier = valueTypeModifier,
                ColumnHandle = columnHandle,
                DataType = type,

                AllowNull = allowNull,
                IsAutomaticValue = autoIncrement,
                DefaultValue = defaultValue,
                HasIndex = unique,
                IsBuiltin = builtin,
                IsServiceColumn = serviceColumn,
                MaxLength = columnMaxLength,
            };
            return dataColumnContainer;
        }

        public ICoreDataTableColumn NewColumn() => new CoreDataColumnContainer {TableName = Name};

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataTableColumn ICoreDataTable.AddColumn([NotNull] string columnName,
            TableStorageType valueType,
            TableStorageTypeModifier valueTypeModifier,
            Type userType,
            bool? auto = null,
            bool? unique = null,
            uint? columnMaxLength = null,
            object defaultValue = null)
        {
            return AddColumn(columnName,
                valueType,
                valueTypeModifier,
                userType,
                auto,
                unique,
                columnMaxLength,
                defaultValue);
        }

        public void ChangeColumnType(CoreDataColumn column, TableStorageType type)
        {
            if (AreColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change column '{column.ColumnName}' column of the '{Name}' table because columns are readonly.");
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change column '{column.ColumnName}' column of the '{Name}' table because it is readonly.");
            }
            
            if (RowCount > 0)
            {
                throw new ReadOnlyAccessViolationException($"Changing column type is not supported when table already has rows. Column {column}; Table {Name}.");
            }
            
            if (column.HasDataLink)
            {
                column.DataStorageLink.Dispose(column);
                column.DataStorageLink = null;
            }

            column.ColumnObj = column.ColumnObj.WithType(type);
        }

        public void ChangeColumnTypeModifier(CoreDataColumn column, TableStorageTypeModifier value)
        {
            if (AreColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change column '{column.ColumnName}' column of the '{Name}' table because columns are readonly.");
            }

            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change column '{column.ColumnName}' column of the '{Name}' table because it is readonly.");
            }

            if (RowCount > 0)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Changing column type is not supported when table already has rows. Column {column}; Table {Name}.");
            }

            if (column.HasDataLink)
            {
                column.DataStorageLink.Dispose(column);
                column.DataStorageLink = null;
            }

            column.ColumnObj.TypeModifier = value;
        }

        public void ChangeColumnName(string columnName, string newName)
        {
            if (m_areColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change column '{columnName}' column of the '{Name}' table because columns are readonly.");
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change column '{columnName}' column of the '{Name}' table because it is readonly.");
            }
            
            if (DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var dataColumn) == false)
            {
                throw new ArgumentException(
                    $"Can't change column name. The table {Name} does not have column with name {columnName}.");
            }

            if (DataColumnInfo.ColumnMappings.ContainsKey(newName))
            {
                throw new ArgumentException(
                    $"Can't change column name. The table {Name} already has column with name {newName}.");
            }

            DataColumnInfo.ColumnMappings.Remove(columnName);
            DataColumnInfo.ColumnMappings[newName] = dataColumn;

            dataColumn.ColumnName = newName;
            
            OnMetadataChanged();
        }

        internal void SetColumnXProperty<T>(int columnHandle, string propertyName, T value)
        {
            if (AreColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column of '{Name}' table because columns are readonly."); 
            }
                    
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column of the '{Name}' table because it is readonly.");
            }

            DataColumnInfo.SetExtProperty(columnHandle, propertyName, value);

            OnMetadataChanged();
        }

        internal T GetColumnXProperty<T>(int columnHandle, string propertyName)
        {
            var value = this.DataColumnInfo.GetExtProperty(columnHandle, propertyName);

            return XPropertyValueConverter.TryConvert<T>("Column", propertyName, value);
        }

        public bool MaxLenConstraintHandler(ColumnHandle columnHandle, string columnName, int rowHandle, object value, bool preValidating)
        {
            if (HasMaxColumnLenConstraintHandler)
            {
                var row = GetRowInstance(rowHandle);

                var args = new MaxColumnLenConstraintRaisedArgs(columnHandle.Handle, columnName, value, row, preValidating, new WeakReference<CoreDataTable>(this));
                
                RaiseMaxLenConstraint(args);

                return args.RaiseError;
            }

            return true;
        }
        
        internal void RaiseMaxLenConstraint(MaxColumnLenConstraintRaisedArgs args)
        {
            MaxColumnLenConstraint.Raise(args);
        }
    }
}