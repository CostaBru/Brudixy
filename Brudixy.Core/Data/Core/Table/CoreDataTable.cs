using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading;
using System.Xml.Linq;
using Brudixy.Interfaces;
using Brudixy.Exceptions;
using Brudixy.Interfaces.Tools;
using JetBrains.Annotations;

using Konsarpoo.Collections;

namespace Brudixy
{
    [DebuggerDisplay("{Name}, Rows: {RowCount}, Columns: {ColumnCount}, PK: {PkDebug}, Thread: {SourceThread.Name}, Indexed: {IndexInfo.HasAny}, MI {MultiColumnIndexInfo.HasAny}, RO = {TableIsReadOnly}")]
    public partial class CoreDataTable : 
        ICoreDataTable,
        IDataEditTransaction, 
        IDataLoadState, 
        ICloneable,
        IReadonlySupported,
        IComparable<CoreDataTable>,
        IEquatable<CoreDataTable>,
        IJsonSerializable,
        IXmlSerializable,
        IComparable
    {
        public const string NoContextChange = "#No context";
        
        internal static readonly ConcurrentDictionary<string, Type> UserTypeRegistry = new();
        internal static readonly ConcurrentDictionary<Type, Func<object, object>> UserTypeCloneRegistry = new();

        public static void RegisterUserTypeStringMethods<T>(Func<T, string> convertToString, Func<string,T> convertFromString)
        {
            Func<object, string> ts = (c) => convertToString((T)c);
            Func<string, object> fs = (c) => (T)convertFromString(c);
            
            TableStorageStringHelper.StringParsingRegistry[typeof(T)] = (ts, fs);
        }

        public static void RegisterUserType<T>(Func<object, object> cloneFunc = null)
        {
            var type = typeof(T);

            UserTypeRegistry[type.AssemblyQualifiedName] = type;
            UserTypeCloneRegistry[type] = cloneFunc;
        }

        static CoreDataTable()
        {
            BuiltinDataItemFeatureSetup.Register();
            
            UserTypeCloneRegistry[typeof(JsonObject)] = (j) => JsonNode.Parse(j.ToString());
            UserTypeCloneRegistry[typeof(XElement)] = (x) => XElement.Parse(x.ToString());
            UserTypeCloneRegistry[typeof(Uri)] = (x) => new Uri(x.ToString());
            
            DataItemFeatureSetup<Guid>.AutomaticValueFuncRepository = (col) => NewGuid();
            DataItemFeatureSetup<Guid?>.AutomaticValueFuncRepository = (col) => NewGuid();
            
            DataItemFeatureSetup<DateTime>.AutomaticValueFuncRepository = (col) => UtcNow();
            DataItemFeatureSetup<DateTime?>.AutomaticValueFuncRepository = (col) => UtcNow();
            
            DataItemFeatureSetup<DateTimeOffset>.AutomaticValueFuncRepository = (col) => UtcNow();
            DataItemFeatureSetup<DateTimeOffset?>.AutomaticValueFuncRepository = (col) => UtcNow();
        }
        
        public CoreDataTable([NotNull] string tableName) : this()
        {
            Name = tableName;
        }

        public CoreDataTable([NotNull]CoreDataTable dataSet) : this()
        {
            SetUpDataSet(dataSet);
        }

        public void SetUpDataSet([NotNull]CoreDataTable dataSet)
        {
            if (dataSet == null)
            {
                throw new ArgumentNullException(nameof(dataSet));
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot set up a dataset reference '{dataSet.TableName}' for '{Name}' table because it is readonly.");
            }

            DataSetReference = new WeakReference<CoreDataTable>(dataSet);
        }

        public CoreDataTable()
        {
        }

        public RowState GetRowState(int rowHandle)
        {
            if (rowHandle >= StateInfo.RowStorageCount)
            {
                return RowState.Detached;
            }

            return StateInfo.GetRowState(rowHandle);
        }

        private bool RowIsNotDeletedOrRemoved(int rowHandle)
        {
            return StateInfo.IsNotDeletedAndRemoved(rowHandle);
        }

        public void RemoveRow(ICoreDataRowAccessor row)
        {
            if (row == null)
            {
                throw new ArgumentNullException(nameof(row));
            }

            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove given row from the '{Name}' table because it is readonly.");
            }
            
            var dataRow = (CoreDataRow)row;

            RemoveRow(dataRow.RowHandle);
        }

        protected void RemoveRow(int rowHandle)
        {
            StateInfo.RemoveRow(rowHandle);

            RemoveRowFromIndexes(rowHandle);

            StateInfo.UpdateRowAge(rowHandle);

            Interlocked.Increment(ref m_dataAge);

            OnRowRemoved(rowHandle);
        }

        protected virtual void OnRowRemoved(int rowHandle)
        {
        }

        internal bool GetEnforceConstraints()
        {
            if (EnforceConstraints)
            {
                return true;
            }

            return Parent is { EnforceConstraints: true };
        }

        internal void UpdateMax(CoreDataColumn column, object currentValue)
        {
            column.DataStorageLink.UpdateLastAutomaticValue(currentValue, column);
        }

        internal void UpdateMax<T>(int columnHandle, T currentValue) where T : struct
        {
            var valueByRef = GetColumn(columnHandle).DataStorageLink as DataItem<T>;

            valueByRef?.UpdateLastAutomaticValue(currentValue);
        }

        public void DeleteRow(int rowHandle)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot delete '{GetRowDebugKey(rowHandle)}' row from the '{Name}' table because it is readonly.");
            }
            
            DeleteRows(new int[] { rowHandle });
        }

        protected virtual bool OnDeleteRows(IReadOnlyList<int> rowHandles)
        {
            return false;
        }

        public void DeleteRows(IReadOnlyList<int> rowHandles)
        {
            if (IsInitializing == false)
            {
                var isCancel = OnDeleteRows(rowHandles);

                if (isCancel)
                {
                    return;
                }
            }

            var childRelationsMap = ChildRelationsMap;

            var isInTransaction = GetIsInTransaction();
            
            if (childRelationsMap != null && GetEnforceConstraints())
            {
                foreach (var rel in childRelationsMap.Values)
                {
                    if (rel.ChildKeyConstraint is { AcceptRejectRule: AcceptRejectRule.Cascade })
                    {
                        foreach (var rowHandle in rowHandles)
                        {
                            if (isInTransaction)
                            {
                                var childRowTransaction = rel
                                    .ChildKeyConstraint
                                    .CascadeDelete(this, rowHandle, inTransaction: true);

                                if (childRowTransaction != null)
                                {
                                    StateInfo.AddDependantTransactions(rowHandle, childRowTransaction);
                                }
                            }
                            else
                            {
                                rel.ChildKeyConstraint.CascadeDelete(this, rowHandle, inTransaction: false);
                            }
                        }
                    }
                }
            }
            
            bool anyWasDeleted = false;

            foreach (var rowHandle in rowHandles)
            {
                var tranId = GetTranId();
                
                var isDeleted = StateInfo.DeleteRow(rowHandle, tranId);

                anyWasDeleted = anyWasDeleted || isDeleted;

                if (isDeleted)
                {
                    if (isInTransaction)
                    {
                        StateInfo.SetTransactionRowChanged(rowHandle);
                    }
                    else
                    {
                        ClearRowIndexes(rowHandle);
                    }

                    StateInfo.UpdateRowAge(rowHandle);
                }
            }

            if (anyWasDeleted )
            {
                OnDeletedRows(rowHandles);
            }

            if (anyWasDeleted)
            {
                Interlocked.Increment(ref m_dataAge);
            }
        }

        protected virtual void OnDeletedRows(IReadOnlyList<int> rowHandles)
        {
        }
        
        internal void SilentlySetRowValue(int rowHandle, object value, CoreDataColumn column)
        {
            var dataItem = column.DataStorageLink;

            dataItem.SilentlySetValue(rowHandle, value, column);

            var isServiceColumn = column.IsServiceColumn;

            if (isServiceColumn == false)
            {
                Interlocked.Increment(ref m_dataAge);
            }
        }

        public void SilentlySetValue(string columnOrXProp, object value)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var dataColumn))
            {
                SilentlySetValue(dataColumn, value);
            }
            else
            {
                if (IsInitializing == false && IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup '{columnOrXProp}' column or x property for the '{Name}' table because it is readonly.");
                }
                
                var type = typeof(object);

                if (value is not null)
                {
                    type = value.GetType();
                }
                
                foreach (var rowHandle in RowsHandles)
                {
                    if (StateInfo.RowXProps.Storage[rowHandle]?.ContainsKey(columnOrXProp) ?? false)
                    {
                        var columnType = GetColumnType(type);
                        
                        StateInfo.SilentlySetRowExtendedProperty(rowHandle, columnOrXProp, ConvertObjectToString(columnType.type, columnType.typeModifier, value, type));
                    }
                }
            }
        }

        void ICoreDataTable.SilentlySetValue(int columnHandle, object value)
        {
            var dataColumn = GetColumn(columnHandle);

            SilentlySetValue(dataColumn, value);
        }

        public void SilentlySetValue(CoreDataColumn column, object value)
        {
            SilentlySetValue(value, column);
        }
        
        public void SilentlySetValue(ColumnHandle columnHandle, object value)
        {
            SilentlySetValue(value, DataColumnInfo.Columns[columnHandle.Handle]);
        }

        public T GetRowFieldValue<T>(int rowHandle, CoreDataColumn column, DefaultValueType defaultValueType, T defaultIfNull)
        {
            var dataItem = column.DataStorageLink;

            if (dataItem.IsNull(rowHandle, column))
            {
                return defaultValueType == DefaultValueType.ColumnBased
                    ? GetDefaultNullValue<T>(column)
                    : defaultIfNull;
            }

            if (dataItem is DataItem<T> typedDataItem && typedDataItem.IsArray == false)
            {
                var root = (typedDataItem.Storage as Data<T>)?.GetRoot();
                
                if (root?.Storage != null && rowHandle < root.Size)
                {
                    return root.Storage[rowHandle];
                }

                return typedDataItem.Storage[rowHandle];
            }

            if (dataItem is ITypedDataItem<T> di)
            {
                return di.GetDataTyped(rowHandle, column);
            }

            var dataValue = dataItem.GetData(rowHandle, column);

            if(dataValue is T tv)
            {
                return tv;
            }
            else
            {
                return CoreDataRow.TryConvertValue<T>(this, column, dataValue, "table row value getter");
            }
        }
        
        public IReadOnlyList<T> GetRowFieldArrayValue<T>(int rowHandle, CoreDataColumn column, DefaultValueType defaultValueType, IReadOnlyList<T> defaultIfNull)
        {
            var dataItem = column.DataStorageLink;

            if (dataItem.IsNull(rowHandle, column))
            {
                return defaultValueType == DefaultValueType.ColumnBased
                    ? Array.Empty<T>()
                    : defaultIfNull ?? Array.Empty<T>();
            }

            var rawDataValue = dataItem.GetRawData(rowHandle, column);

            if (rawDataValue is IReadOnlyList<T> rt)
            {
                return rt;
            }

            throw GetInvalidArrayCastException<T>(rowHandle, column);
        }

        internal InvalidCastException GetInvalidArrayCastException<T>(int rowHandle, CoreDataColumn column)
        {
           return new InvalidCastException($"The '{column.ColumnName}' column of the row '{GetRowDebugKey(rowHandle)}' of the '{TableName}' table cannot be casted to readonly list.");
        }

        internal void SetRowDefaultValue(int rowHandle, CoreDataColumn column)
        {
            var defaultValue = column.DefaultValue;

            var prevValue = GetRowFieldValue(rowHandle, column, DefaultValueType.ColumnBased, null);

            SetRowColumnValue(rowHandle, column, defaultValue, prevValue, null);
        }
        
        internal void SetRowNullValue(int rowHandle, CoreDataColumn column)
        {
            var defaultValue = column.DefaultValue;

            var prevValue = GetRowFieldValue(rowHandle, column, DefaultValueType.ColumnBased, null);

            SetRowColumnValue(rowHandle, column, defaultValue, prevValue, null);
        }

        public object GetRowFieldValue(int rowHandle, CoreDataColumn column, DefaultValueType defaultValueType, object defaultIfNull)
        {
            var dataItem = column.DataStorageLink;

            if (dataItem.IsNull(rowHandle, column))
            {
                return defaultValueType == DefaultValueType.ColumnBased
                    ? GetDefaultNullValue<object>(column)
                    : defaultIfNull;
            }

            return dataItem.GetData(rowHandle, column);
        }
        
        public object GetRowRawFieldValue(int rowHandle, CoreDataColumn column, DefaultValueType defaultValueType, object defaultIfNull)
        {
            var dataItem = column.DataStorageLink;

            if (dataItem.IsNull(rowHandle, column))
            {
                return defaultValueType == DefaultValueType.ColumnBased
                    ? GetDefaultNullValue<object>(column)
                    : defaultIfNull;
            }

            return dataItem.GetRawData(rowHandle, column);
        }

        internal bool GetIsRowColumnNull(int rowHandle, CoreDataColumn column)
        {
            return column.DataStorageLink.IsNull(rowHandle, column);
        }

        private void SilentlySetValue(object value, CoreDataColumn column)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{column.ColumnName}' column for the '{Name}' table because it is readonly.");
            }

            var dataItem = column.DataStorageLink;

            var stateInfoRowCount = StateInfo.RowCount;

            for (int rowHandle = 0; rowHandle < stateInfoRowCount; rowHandle++)
            {
                dataItem.SilentlySetValue(rowHandle, value, column);
            }

            if (column.HasIndex)
            {
                IndexInfo.RebuildIndex(this, column.ColumnHandle);
            }

            MultiColumnIndexInfo.GetColumnFirstIndex(column.ColumnHandle);

            //todo rebuild multi column index

            var isServiceColumn = column.IsServiceColumn;

            if (isServiceColumn == false)
            {
                Interlocked.Increment(ref m_dataAge);
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        object ICloneable.Clone() => Clone(null);

        public CoreDataTable Clone() => Clone(null);

        public uint GetRowAge(int rowHandle) => StateInfo.GetRowAge(rowHandle);

        protected IEnumerable<string> PrimaryKeyColumns()
        {
            foreach (var container in DataColumnInfo.PrimaryKeyColumns)
            {
                yield return container.ColumnName;
            }
        }
        
        protected internal object GetOriginalData(int rowHandle, CoreDataColumn column)
        {
            var dataItem = column.DataStorageLink;

            if (StateInfo.GetRowState(rowHandle) == RowState.Added || IsReadOnlyColumn(column))
            {
                return dataItem.GetDefaultValue(column);
            }
            
            return dataItem.GetOriginalValue(rowHandle, column);
        }

        public virtual bool IsReadOnlyColumn(CoreDataColumn column) => false;

        public virtual bool IsReadOnlyField(string column) => IsReadOnlyColumn(GetColumn(column));

        protected internal T GetOriginalData<T>(int rowHandle, CoreDataColumn column)
        {
            var dataItem = column.DataStorageLink;

            var returnDefault = StateInfo.GetRowState(rowHandle) == RowState.Added/* || DataColumnInfo.ColumnHasExpression(columnHandle)*/;

            if (dataItem is DataItem<T> dt)
            {
                if (returnDefault)
                {
                    var nullValue = dt.DefaultNullValue;

                    if (nullValue == null)
                    {
                        return TypeConvertor.ReturnDefault<T>();
                    }
                    
                    return nullValue;
                }
                        
                return dt.GetOriginalTypedValue(rowHandle, column);
            }

            if (returnDefault)
            {
                if(dataItem.GetDefaultValue(column) is T tv)
                {
                    return tv;
                }
                else
                {
                    return CoreDataRow.TryConvertValue<T>(this, column, dataItem.GetDefaultValue(column), "original field value getter: default null");
                }
            }

            var originalValue = dataItem.GetOriginalValue(rowHandle, column);

            if(originalValue is T tvo)
            {
                return tvo;
            }
            else
            {
                return CoreDataRow.TryConvertValue<T>(this, column, originalValue, "original field value getter");
            }
        }

        public bool IsNotChanged() => HasChanges() == false;

        public bool HasChanges() => m_baseAge != m_dataAge;

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataTable ICoreDataTable.GetChanges() => GetChanges();

        [NotNull]
        public CoreDataTable GetChanges()
        {
            var table = Clone();

            var rows = table.AllRows.Where(r => r.IsModified);

            table.ImportRows(rows);

            return table;
        }

        public void SetRowXProperty(int rowHandle, string propertyCode, object value)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{propertyCode}' extended property for '{GetRowDebugKey(rowHandle)}' row of '{Name}' table because it is readonly.");
            }
            
            if (rowHandle > StateInfo.RowStorageCount)
            {
                throw new ArgumentOutOfRangeException(
                    $"Given {rowHandle} row handle is out of expected range {StateInfo.RowStorageCount}.");
            }
            
            var prevValue = StateInfo.GetExtendedProperty(rowHandle, propertyCode);

            BeforeXPropertySet(rowHandle, propertyCode, ref value, prevValue, out var canContinue);

            if (canContinue)
            {
                SetXRowXProperty(rowHandle, propertyCode, value, prevValue);
            }
        }

        private void SetXRowXProperty(int rowHandle, string propertyCode, object value, object prevValue)
        {
            StateInfo.SetRowExtendedProperty(rowHandle, propertyCode, value, prevValue, GetTranId());

            OnRowXPropertySet(rowHandle, propertyCode, value, prevValue);

            StateInfo.UpdateRowAge(rowHandle);

            Interlocked.Increment(ref m_dataAge);
        }

        protected virtual void OnRowXPropertySet(int rowHandle, string propertyCode, object value, object prevValue) => LogChange(propertyCode, value, prevValue, rowHandle);

        protected virtual void BeforeXPropertySet(int rowHandle,
            string propertyCode,
            ref object value,
            object prevValue,
            out bool canContinue)
        {
            canContinue = true;
        }

        public void SetXProperty<T>(string propertyName, [CanBeNull] T value) 
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{propertyName}' extended property for the '{Name}' table because  is readonly.");
            }
            
            if (ExtProperties == null)
            {
                ExtProperties = new Map<string, ExtPropertyValue>(StringComparer.OrdinalIgnoreCase);
            }

            if (OnBeforeSetXProperty(propertyName, ref value))
            {
                if (ExtProperties.ContainsKey(propertyName))
                {
                    ExtProperties.ValueByRef(propertyName, out var _).Current = value;
                }
                else
                {
                    ExtProperties[propertyName] = new ExtPropertyValue { Original = value };
                }

                OnSetXProperty(propertyName, value);
            }
        }

        [CanBeNull]
        public T GetXProperty<T>(string xPropertyName, bool original = false) 
        {
            if (ExtProperties == null)
            {
                return TypeConvertor.ReturnDefault<T>();
            }

            ExtProperties.TryGetValue(xPropertyName, out var value);

            if (original)
            {
                return XPropertyValueConverter.TryConvert<T>("DataTable original", xPropertyName, value.Original);
            }
            
            if (value.Current is not null)
            {
                return XPropertyValueConverter.TryConvert<T>("DataTable", xPropertyName, value.Current);
            }
        
            return XPropertyValueConverter.TryConvert<T>("DataTable original", xPropertyName, value.Original);
        }

        public void SetModifiedRowState(int rowHandle)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot set up '{GetRowDebugKey(rowHandle)}' row of '{Name}' as modified for table because it is readonly.");
            }
            
            StateInfo.SetModified(rowHandle, GetTranId());
        }
        
        public void SetUnchangedRowState(int rowHandle)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot set up '{GetRowDebugKey(rowHandle)}' row of '{Name}' as unchanged for table because it is readonly.");
            }
            
            StateInfo.SetUnchanged(rowHandle, GetTranId());
        }

        public void SetAddedRowState(int rowHandle)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot set up '{GetRowDebugKey(rowHandle)}' row of '{Name}' as added for table because it is readonly.");
            }
            
            StateInfo.SetAdded(rowHandle, GetTranId());
        }

        public bool HasXProperty(string propertyName) => ExtProperties != null && ExtProperties.ContainsKey(propertyName);

        protected void SetDataColumnInfo([NotNull] CoreDataColumnInfo info, [NotNull] Data<CoreDataColumn> columns)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            if (columns == null)
            {
                throw new ArgumentNullException(nameof(columns));
            }

            m_dataColumnInfo?.Dispose();

            m_dataColumnInfo = info;

            m_areColumnsReadonly = true;
        }

        public bool ContainsColumn(string column) => DataColumnInfo.ColumnMappings.ContainsKey(column);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataTable ICoreDataTable.Copy() => Copy();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreReadOnlyDataTable ICoreReadOnlyDataTable.Copy() => Copy();

        public CoreDataTable Copy(Thread thread = null) => CloneCore(thread);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataTable ICoreDataTable.Clone() => Clone();

        public CoreDataTable Clone(Thread thread = null) => CloneCore(thread, false);

        public void SetServiceColumn(int columnHandle, bool value)
        {
            if (AreColumnsReadonly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DataColumnInfo.Columns[columnHandle].ColumnName}' column of '{Name}' table because columns are readonly."); 
            }
                    
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{DataColumnInfo.Columns[columnHandle].ColumnName}' column of the '{Name}' table column service flag because it is readonly.");
            }
                    
            DataColumnInfo.SetServiceColumn(columnHandle, value);
        }
    }
}