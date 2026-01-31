using System;
using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using Brudixy.Converter;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

using DataException = Brudixy.Exceptions.DataException;

namespace Brudixy
{
    [DebuggerTypeProxy(typeof(CoreDataRowDebugView))]
    [DebuggerDisplay("Row container of {TableName}, State {RowRecordState}, #{DebugKeyValue}")]
    public partial class CoreDataRowContainer:
            ICloneable, 
            ICoreDataRowContainer,
            IComparable<ICoreDataRowContainer>, 
            IComparable<ICoreDataRowReadOnlyContainer>,
            IComparable<CoreDataRowContainer>, 
            IEquatable<CoreDataRowContainer>, 
            IEquatable<ICoreDataRowContainer>,
            IEquatable<ICoreDataRowReadOnlyContainer>,
            IXmlSerializable,
            IJsonSerializable,
            IReadonlySupported,
            IComparable
    {
        public string TableName
        {
            get => MetadataProps.TableName;
        }
        
        private CoreContainerDataProps Props
        {
            get => ContainerDataProps;
        }

        void ICoreDataRowAccessor.CopyFrom(ICoreDataRowAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            CopyFrom(rowAccessor, skipFields);
        }

        void ICoreDataRowAccessor.CopyChanges(ICoreDataRowAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            CopyChanges(rowAccessor, skipFields);
        }

        public void SetModified()
        {
            RowRecordState = RowState.Modified;

            Props.OriginalData?.Dispose();
            
            Props.OriginalData = CreateOriginalData();
        }

        string ICoreDataRowReadOnlyAccessor.ToString(ICoreTableReadOnlyColumn column)
        {
            return ToString(column);
        }

        public string ToString(string columnOrXProp)
        {
            return ToString(columnOrXProp, null, null);
        }

        IEnumerable<ICoreTableReadOnlyColumn> ICoreDataRowReadOnlyAccessor.GetColumns()
        {
            return Columns;
        }

        public RowState RowRecordState
        {
            get
            {
                return Props.DataRowState;
            }
            set
            {
                if (IsInitializing == false && IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup '{DebugKeyValue}' data row container of '{TableName}' table row record state because container is readonly.");
                }
                
                Props.DataRowState = value;
            }
        }

        public static T CreateFrom<T>([NotNull] IDataRowReadOnlyAccessor rowAccessor) where T : CoreDataRowContainer, new()
        {
            if (rowAccessor is CoreDataRow dataRow)
            {
                return (T)dataRow.ToContainer();
            }

            var container = new T();

            container.Init(rowAccessor);

            return container;
        }

        internal CoreContainerDataProps ContainerDataProps
        {
            get
            {
                if (m_containerDataProps == null)
                {
                    InitNew();
                }
                
                return m_containerDataProps;
            }
            set => m_containerDataProps = value;
        }

        internal CoreContainerMetadataProps MetadataProps
        {
            get
            {
                if (m_metadataProps == null)
                {
                    InitNew();
                }
                
                return m_metadataProps;
            }
            set => m_metadataProps = value;
        }

        protected virtual void InitNew()
        {
        }

        public virtual void Init(CoreContainerMetadataProps metadataProps, CoreContainerDataProps props, ICoreDataRowReadOnlyAccessor rowAccessor = null)
        {
            m_metadataProps = metadataProps;
            m_containerDataProps = props;
        }

        public virtual void Init(ICoreDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            m_containerDataProps = InitContainerDataProps(rowAccessor, skipFields);
            m_metadataProps = new CoreContainerMetadataProps(rowAccessor, CreateColumnContainer);
        }

        protected virtual CoreContainerDataProps InitContainerDataProps(ICoreDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields)
        {
            return new CoreContainerDataProps(rowAccessor, skipFields);
        }

        protected virtual CoreDataColumnContainer CreateColumnContainer(ICoreTableReadOnlyColumn column)
        {
            return new CoreDataColumnContainer(column);
        }
        
        protected virtual IEnumerable<CoreDataColumnContainerBuilder> GetDataColumnContainers()
        {
            return Array.Empty<CoreDataColumnContainerBuilder>();
        }

        private static readonly ConcurrentDictionary<Type, FrozenSet<string>> s_predefiendColumnCache = new ConcurrentDictionary<Type, FrozenSet<string>>();

        public FrozenSet<string> GetPredefinedColumns()
        {
            var type = this.GetType();
            
            if (s_predefiendColumnCache.TryGetValue(type, out FrozenSet<string> result))
            {
                return result;
            }

            var predefinedColumns = GetDataColumnContainers().Select(s => s.ColumnName).ToFrozenSet(StringComparer.OrdinalIgnoreCase);

            s_predefiendColumnCache[type] = predefinedColumns;
            
            return predefinedColumns;
        }

        private class DummyFieldContainer : IFieldContainer
        {
            public void Set(string col, object objValue)
            {
            }

            public object Get(string col)
            {
                return null;
            }
        }

        protected virtual void SetValue<T>(ref T value, string name, Action<string, T> updateField)
        {
            Map<int, object> cascadePrevValues = null;

            var column = GetColumn(name);
            
            var columnHandle = column.ColumnHandle;

            var oldValue = Field<T>(name);

            OnBeforeSet(name, columnHandle, ref value, oldValue, out var cancel, ref cascadePrevValues);
            if (cancel)
            {
                return;
            }

            OnDataChange();
            
            updateField(name, value);
            
            OnAfterSet(name, columnHandle, oldValue, value, cascadePrevValues);

            LogChange(name, value, oldValue);
        }

        protected virtual void OnAfterSet<T>(string s, int name, T oldValue, T value, Map<int, object> cascadePrevValues)
        {
        }

        protected virtual void OnBeforeSet<T>(string s, int name, ref T value, T oldValue, out bool cancel, ref Map<int, object> cascadePrevValues)
        {
            cancel = false;
        }

        protected virtual void RejectChangesCore()
        {
            Props.Data.Dispose();

            Props.Data = Props.OriginalData?.ToData();

            EnsureData();
        }

        private void EnsureData()
        {
            if (Props.Data == null)
            {
                Props.Data = new ();
                Props.Data.Ensure(MetadataProps.ColumnMap.Count);
            }
        }

        protected virtual object GetData(CoreDataColumnContainer reference)
        {
            var value = Props.Data[reference.ColumnHandle];

            if (reference.Type is TableStorageType.Object or TableStorageType.UserType || reference.TypeModifier == TableStorageTypeModifier.Complex)
            {
                if (value is not IReadonlySupported or IReadonlySupported { IsReadOnly: false })
                {
                   return ((ICloneable)value).Clone();
                }
            }

            if (value is Array arr)
            {
                return arr.Clone();
            }
            
            return value;
        }

        protected virtual void SetData(CoreDataColumnContainer column, object value)
        {
            OnDataChange();
            
            if (value == null)
            {
                WriteValue(column, null);
            }
            else
            {
                var conversionType = CoreDataTable.GetColumnType(column.Type, column.TypeModifier, column.AllowNull, column.DataType);

                DoCheckAndFix(ref value, column);
            
                CopyIfNeededBoxed(ref value);

                if (value is IConvertible)
                {
                    object convertedValue;

                    if (value is string str)
                    {
                        convertedValue = CoreDataTable.ConvertStringToObject(column.Type,
                            column.TypeModifier,
                            str,
                            conversionType);
                    }
                    else
                    {
                        convertedValue = Tool.ConvertBoxed(value, conversionType);
                    }

                    WriteValue(column, convertedValue);
                }
                else
                {
                    WriteValue(column, value);
                }
            }
        }

        private void OnDataChange()
        {
            if (IsInitializing == false)
            {
                Interlocked.Increment(ref Props.Age); 
            }

            if (RowRecordState == RowState.Unchanged)
            {
                Props.OriginalData = CreateOriginalData();
                
                RowRecordState = RowState.Modified;
            }
        }

        private void DoCheckAndFix(ref object value, CoreDataColumnContainer columnContainer)
        {
            if (value is string newValueS)
            {
                var containerMaxLength = columnContainer.MaxLength;

                if (containerMaxLength > 0)
                {
                    if (string.IsNullOrEmpty(newValueS) == false && newValueS.Length > containerMaxLength)
                    {
                        throw new DataException(GetMessage(TableName, columnContainer.ColumnName, $"The maximum string value length ({newValueS.Length} > {newValueS}) constraint error."));
                    }
                }
            }
            else if(value is Array arr)
            {
                var containerMaxLength = columnContainer.MaxLength;

                if (containerMaxLength > 0 && arr.Length > containerMaxLength)
                {
                    throw new DataException(GetMessage(TableName, columnContainer.ColumnName, $"The maximum array value length ({arr.Length} > {columnContainer.MaxLength}) constraint error."));
                }
            }
            else if(value is DateTime dt && dt.Kind != DateTimeKind.Utc)
            {
                value = DateTimeKindForceSet(dt);
            }
        }

        private void WriteValue(CoreDataColumnContainer column, object value)
        {
            Props.Data[column.ColumnHandle] = value;

            if (IsInitializing == false)
            {
                Interlocked.Increment(ref Props.Age); 
            }
        }

        public static T ConvertBoxed<T>(string tableName, string column, object x, uint? maxLen = null)
        {
            if (x is null)
            {
                return default;
            }
            
            var tableStorageType = CoreDataTable.GetColumnType(x.GetType());
            
            return TypeConvertor.ConvertValue<T>(x, column, tableName, tableStorageType.type, tableStorageType.typeModifier, "Container set");
        }
        
        public static T? ConvertBoxedDateTime<T>(string tableName, string column, object x, uint? maxLen = null)
        {
            var dt = Tool.ConvertBoxed<DateTime?>(x);
            
            if (dt.HasValue && dt.Value.Kind != DateTimeKind.Utc)
            {
                dt = DateTimeKindForceSet(dt);
            }
            
            return (T?)(object)dt;
        }
        
        public static T ConvertBoxedArray<T>(string tableName, string column, object x, uint? maxLen = null)
        {
            if (x is Array arr)
            {
                if (arr.Length > maxLen)
                {
                    throw new DataException(GetMessage(tableName, column, $"The maximum array value length ({arr.Length} > {maxLen}) constraint error."));
                }
            }

            return (T)x;
        }
        
        public static string ConvertBoxedString<T>(string tableName, string column, object x, uint? maxLen = null)
        {
            var s = Brudixy.TypeConvertor.ConvertValue<string>(x, column, tableName, TableStorageType.String, TableStorageTypeModifier.Simple, "Row container convert");
            
            if (string.IsNullOrEmpty(s) == false && s.Length > maxLen)
            {
                throw new DataException(GetMessage(tableName, column, $"The maximum value length ({s.Length} > {maxLen}) constraint error."));
            }
            return s;
        }
        
        public static string GetMessage(string tableName, string column, string reason)
        {
            return $"The changing '{column}' container of the '{tableName}' table was canceled. Reason: {reason}";
        }

        protected virtual void AcceptChangesCore()
        {
            Props.OriginalData?.Dispose();

            Props.OriginalData = null;
        }

        protected Data<object> CreateOriginalData()
        {
            var data = new Data<object>(GetColumnCount());

            var columnCount = GetColumnCount();
            
            data.Ensure(columnCount);

            foreach (var column in MetadataProps.ColumnMap.Values)
            {
                data[column.ColumnHandle] = GetData(column);
            }

            return data;
        }

        string ICoreDataRowReadOnlyAccessor.GetTableName() => TableName;

        IEnumerable<ICoreTableReadOnlyColumn> ICoreDataRowReadOnlyAccessor.PrimaryKeyColumn => this.MetadataProps.PrimaryKeyColumn;

        internal WeakReference<IDataOwner> m_tableOwnerReference;

        public void SetOwner(IDataOwner value)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{DebugKeyValue}' data row container of '{TableName}' table owner because container is readonly.");
            }
            
            if (value != null)
            {
                m_tableOwnerReference = new WeakReference<IDataOwner>(value);
            }
            else
            {
                m_tableOwnerReference = null;
            }
        }

        public IDataOwner GetOwner()
        {
            if (m_tableOwnerReference == null)
            {
                return null;
            }

            m_tableOwnerReference.TryGetTarget(out var owner);

            return owner;
        }
      
        public ulong GetRowAge() => Props.Age;
        
        public int GetMetaAge() => MetadataProps.Age;

        public int GetColumnCount() => MetadataProps.ColumnMap.Count;

        public int RowHandle => Props.RowHandle;
     

        public IEnumerable<ICoreTableReadOnlyColumn> PrimaryKeyColumn => MetadataProps.PrimaryKeyColumn;

        ICoreTableReadOnlyColumn ICoreDataRowReadOnlyAccessor.GetColumn(int columnHandle)
        {
            return GetColumnCore(columnHandle);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreTableReadOnlyColumn ICoreDataRowReadOnlyAccessor.TryGetColumn(string columnName)
        {
            return TryGetColumn(columnName);
        }

        public bool IsExistsField(string columnName)
        {
            return TryGetColumn(columnName) != null;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreTableReadOnlyColumn ICoreDataRowReadOnlyAccessor.GetColumn(string columnName)
        {
            return GetColumn(columnName);
        }

        [CanBeNull]
        public CoreDataColumnContainer TryGetColumn(string columnName)
        {
            return MetadataProps.TryGetColumn(columnName);
        }

        [NotNull]
        public CoreDataColumnContainer GetColumn(string columnName)
        {
            return MetadataProps.GetColumn(columnName);
        }
        
        [NotNull]
        public CoreDataColumnContainer GetColumn(int columnHandle)
        {
            return GetColumnCore(columnHandle);
        }

        public string ToString(string columnOrXProp, string format = null, IFormatProvider formatProvider = null)
        {
            return ValueToStringFormat(format, formatProvider, this[columnOrXProp], this.ContainerDataProps.DisplayDateTimeUtcOffsetTicks);
        }

        public static string DefaultDateTimeDisplayFormat = "dd/MMM/yyyy HH:mm:ss.fff K";
        public static string UtcDefaultDateTimeDisplayFormat = "dd/MMM/yyyy HH:mm:ss.fff UTC";

        public static string ValueToStringFormat(string format, IFormatProvider formatProvider, object value, int? displayDateTimeUtcOffsetTicks)
        {
            string FormatDateTime(int? displayOffset, DateTime dateTime, string ftm, IFormatProvider frmPrvd)
            {
                if (displayOffset.HasValue)
                {
                    if (ftm != null || frmPrvd != null)
                    {
                        return new DateTime(dateTime.Ticks + displayOffset.Value).ToString(ftm, frmPrvd);
                    }
                    
                    return $"{new DateTime(dateTime.Ticks + displayOffset.Value).ToString(DefaultDateTimeDisplayFormat)} ({dateTime.ToString(UtcDefaultDateTimeDisplayFormat)})";
                }

                var localTime = dateTime.ToLocalTime();
                
                if (ftm != null && frmPrvd != null)
                {
                    return localTime.ToString(ftm, frmPrvd);
                }

                if (localTime != dateTime)
                {
                    return $"{localTime.ToString(DefaultDateTimeDisplayFormat)} ({dateTime.ToString(UtcDefaultDateTimeDisplayFormat)})";
                }

                return dateTime.ToString(DefaultDateTimeDisplayFormat);
            }

            if (value == null)
            {
                return string.Empty;
            }

            if (value is DateTime dt)
            {
                return FormatDateTime(displayDateTimeUtcOffsetTicks, dt, format, formatProvider);
            }
            
            if (value is Range<DateTime> rd)
            {
                if (displayDateTimeUtcOffsetTicks.HasValue)
                {
                    var range = new Range<DateTime>(new DateTime(rd.Minimum.Ticks + displayDateTimeUtcOffsetTicks.Value), new DateTime(rd.Maximum.Ticks + displayDateTimeUtcOffsetTicks.Value));
                    
                    return $"{range.ToString((v) => FormatDateTime(displayDateTimeUtcOffsetTicks, v, format, formatProvider))}";
                }

                return rd.ToString((v) => FormatDateTime(null, v, format, formatProvider));
            }
            
            if ((format != null || formatProvider != null) && value is IFormattable fm)
            {
                return fm.ToString(format, formatProvider);
            }
            
            if (value is Array arr)
            {
                var builder = new StringBuilder();

                var i = 0;
                var count = arr.Length;

                builder.Append("[");
                
                foreach (var val in arr)
                {
                    if ((format != null || formatProvider != null) && val is IFormattable fmv)
                    {
                        builder.Append(fmv.ToString(format, formatProvider));;
                    }
                    else
                    {
                        builder.Append(val);
                    }

                    if (i + 1 < count)
                    {
                        builder.Append(";");
                    }
                    
                    i++;
                }
                
                builder.Append("]");
                
                return builder.ToString();
            }

            return value.ToString();
        }

        public string ToString(ICoreTableReadOnlyColumn column, string format = null, IFormatProvider formatProvider = null)
        {
            return ValueToStringFormat(format, formatProvider, this[column], Props.DisplayDateTimeUtcOffsetTicks);
        }

        T ICoreDataRowReadOnlyAccessor.Field<T>(ICoreTableReadOnlyColumn column)
        {
            return Field<T>(column.ColumnName);
        }

        public T Field<T>(string columnOrXProp, T defaultIfValue)
        {
            return GetFieldValue(columnOrXProp, defaultIfValue, DefaultValueType.Passed);
        }

        T ICoreDataRowReadOnlyAccessor.Field<T>(ICoreTableReadOnlyColumn column, T defaultIfNull)
        {
            return Field<T>(column.ColumnName);
        }

        public T Field<T>(string columnOrXProp)
        {
            return GetFieldValue(columnOrXProp, default(T), DefaultValueType.ColumnBased);
        }

        public bool IsNull(string columnOrXProp)
        {
            var column = TryGetColumn(columnOrXProp);
            if (column != null)
            {
                return IsNull(column);
            }
            
            return HasXProperty(columnOrXProp) == false;
        }

        public bool IsNotNull(string columnOrXProp)
        {
            return IsNull(columnOrXProp) == false;
        }

        bool ICoreDataRowReadOnlyAccessor.IsNull(ICoreTableReadOnlyColumn column)
        {
            return IsNull(column);
        }

        bool ICoreDataRowReadOnlyAccessor.IsNotNull(ICoreTableReadOnlyColumn column)
        {
            return IsNotNull(column);
        }

        object ICoreDataRowAccessor.this[ICoreTableReadOnlyColumn column]
        {
            get => this[column];
            set => this[column] = value;
        }

        object ICoreDataRowReadOnlyAccessor.this[ICoreTableReadOnlyColumn column] => this[column];

        protected CoreDataColumnContainer GetColumnCore(int reference)
        {
            return MetadataProps.GetColumn(reference);
        }
        
        [CanBeNull]
        public virtual object this[string columnOrXProp]
        {
            get
            {
                var hasColumn = MetadataProps.ColumnMap.TryGetValue(columnOrXProp, out var column);

                if (hasColumn == false)
                {
                    if (HasXProperty(columnOrXProp))
                    {
                        return GetXProperty<object>(columnOrXProp);
                    }

                    throw new MissingMetadataException($"Data row container created from '{MetadataProps.TableName}' does not have {columnOrXProp} column or xproperty.");
                }

                return GetData(column);
            }
            set
            {
                if (IsInitializing == false && IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot change '{DebugKeyValue}' data row container of '{TableName}' table because it is readonly.");
                }
                
                if (!(MetadataProps.ColumnMap.TryGetValue(columnOrXProp, out var column)))
                {
                    if (HasXProperty(columnOrXProp))
                    {
                        SetXProperty(columnOrXProp, value);
                        
                        return;
                    }
                    
                    throw new MissingMetadataException($"Data row container created from '{MetadataProps.TableName}' does not have '{columnOrXProp}' column or XProperty.");
                }
             
                SetColValue(column, value);
            }
        }

        private void SetColValue(CoreDataColumnContainer column, object value)
        {
            if (value != null)
            {
                var columnType = CoreDataTable.GetColumnType(column.Type, column.TypeModifier, column.AllowNull, column.DataType);

                if (value is IConvertible)
                {
                    var newValue = Tool.ConvertBoxed(value, columnType);
                    SetCore(column.ColumnName, column, newValue);
                }
                else
                {
                    SetCore(column.ColumnName, column, value);
                }
            }
            else
            {
                SetCore(column.ColumnName, column, null);
            }
        }

        protected virtual void SetCore(string column, CoreDataColumnContainer colObj, object newValue)
        {
            var oldValue = GetData(colObj);

            Map<int, object> cascadePrevValues = null;

            var columnHandle = colObj.ColumnHandle;

            OnBeforeSet(column, columnHandle, ref newValue, oldValue, out var cancel, ref cascadePrevValues);

            if (cancel)
            {
                return;
            }

            SetData(colObj, newValue);

            OnAfterSet(column, columnHandle, oldValue, newValue, cascadePrevValues);
            
            LogChange(column, newValue, oldValue);
        }
        
        private static T DateTimeKindForceSet<T>(T val)
        {
            DateTime rez = default;
            GenericConverter.ConvertTo(ref val, ref rez);

            var validDateTime = DateTime.SpecifyKind(rez, DateTimeKind.Utc);

            T rezConv = default;
            GenericConverter.ConvertTo(ref validDateTime, ref rezConv);

            return rezConv;
        }

        private void EnsureOriginalData()
        {
            if (Props.OriginalData == null)
            {
                Props.OriginalData = CreateOriginalData();
            }
        }

        public T Field<T>(IDataTableReadOnlyColumn column, T defaultIfNull)
        {
            return GetFieldValue(column.ColumnName, defaultIfNull, DefaultValueType.Passed);
        }

        public bool IsNull(IDataTableReadOnlyColumn column)
        {
            return IsNull(column.ColumnName);
        }

        public bool IsNotNull(IDataTableReadOnlyColumn column)
        {
            return IsNotNull(column.ColumnName);
        }

        public object this[IDataTableReadOnlyColumn column]
        {
            get => this[column.ColumnName];
            set => this[column.ColumnName] = value;
        }

        public T Field<T>(IDataTableReadOnlyColumn column)
        {
            return GetFieldValue(column.ColumnName, default(T), DefaultValueType.ColumnBased);
        }

        public bool IsNull(ICoreTableReadOnlyColumn column)
        {
            var data = GetData(GetColumn(column.ColumnName));

            return data == null;
        }

        public bool IsNotNull(ICoreTableReadOnlyColumn column)
        {
            return IsNull(column) == false;
        }

        public CoreDataRowContainer Set<T>(CoreDataColumnContainer columnContainer, T value)
        {
             Set(columnContainer.ColumnHandle, value);
             
             return this;
        }
        
        public ICoreDataRowAccessor Set<T>(string columnOrXProp, T value)
        {
            if (TypeConvertor.CanBeTreatedAsNull<T>(value))
            {
                SetNull(columnOrXProp);

                return this;
            }
            
            if (value == null)
            {
                SetNull(columnOrXProp);
            }
            else
            {
                this[columnOrXProp] = value;
            }
            
            return this;
        }

        ICoreDataRowAccessor ICoreDataRowAccessor.Set<T>(ICoreTableReadOnlyColumn column, T value)
        {
            return Set(column, value);
        }

        public CoreDataRowContainer Set<T>(int columnHandle, T value)
        {
            if (TypeConvertor.CanBeTreatedAsNull<T>(value))
            {
                SetNull(columnHandle);

                return this;
            }
            
            if (value == null)
            {
                SetNull(columnHandle);
            }
            else
            {
                this[columnHandle] = value;
            }
            
            return this;
        }

        public CoreDataRowContainer SetNull(int columnHandle)
        {
            this[columnHandle] = null;

            return this;
        }

        ICoreDataRowAccessor ICoreDataRowAccessor.Set<T>(string columnOrXProp, T value)
        {
            if (value is null)
            {
                return SetNull(columnOrXProp);
            }
            
            return Set<T>(columnOrXProp, value);
        }
        
       

        public ICoreDataRowContainer Set<T>(ICoreTableReadOnlyColumn column, T value)
        {
            return (IDataRowContainer)this.Set(column.ColumnName, value);
        }

        ICoreDataRowContainer ICoreDataRowContainer.SetNull(ICoreTableReadOnlyColumn column)
        {
            return this.SetNull(column);
        }

        ICoreDataRowAccessor ICoreDataRowAccessor.SetNull(string columnOrXProp)  
        {
            this[columnOrXProp] = null;
            
            return this;
        }
        ICoreDataRowAccessor ICoreDataRowAccessor.SetNull(ICoreTableReadOnlyColumn column)
        {
            this[column.ColumnName] = null;
            
            return this;
        }

        public ICoreDataRowContainer SetNull(ICoreTableReadOnlyColumn column)
        {
            this[column] = null;

            return this;
        }


        [CanBeNull]
        public T GetOriginalValue<T>([NotNull] ICoreTableReadOnlyColumn column)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }
            
            var originalValue = GetOriginalValue(column.ColumnName);
            
            if (originalValue is null)
            {
                return default;
            }

            if(originalValue is T tv)
            {
                return tv;
            }
            else
            {
                return TypeConvertor.ConvertValue<T>(originalValue, column.ColumnName, TableName, column.Type, column.TypeModifier, "original field value getter");
            }
        }

        [CanBeNull]
        public T GetOriginalValue<T>([NotNull] string columnOrXProp)
        {
            var originalValue = GetOriginalValue(columnOrXProp);

            if (originalValue is null)
            {
                return TypeConvertor.ReturnDefault<T>();
            }

            if(originalValue is T tv)
            {
                return tv;
            }
            else
            {
                TableStorageType tableStorageType = TableStorageType.String;
                TableStorageTypeModifier tableStorageTypeModifier = TableStorageTypeModifier.Simple;

                var column = MetadataProps.TryGetColumn(columnOrXProp);

                if (column != null)
                {
                    tableStorageType = column.Type;
                    tableStorageTypeModifier = column.TypeModifier;
                }

                return TypeConvertor.ConvertValue<T>(originalValue, columnOrXProp, TableName, tableStorageType, tableStorageTypeModifier, "core original field value getter");
            }
        }

        T ICoreDataRowReadOnlyAccessor.GetOriginalValue<T>(ICoreTableReadOnlyColumn column)
        {
            return GetOriginalValue<T>(column);
        }

        object ICoreDataRowReadOnlyAccessor.GetOriginalValue(ICoreTableReadOnlyColumn column)
        {
            return GetOriginalValue(column);
        }

        public object GetOriginalValue(ICoreTableReadOnlyColumn column)
        {
            return GetOriginalValue(column.ColumnName);
        }

        [CanBeNull]
        public virtual object GetOriginalValue([NotNull] string columnOrXProp)
        {
            if (!MetadataProps.ColumnMap.TryGetValue(columnOrXProp, out var column))
            {
                throw new MissingMetadataException($"DataRowContainer created from {TableName} does not have {columnOrXProp} column.");
            }

            if (RowRecordState == RowState.Added)
            {
                return null;
            }

            if (TryGetOriginalValue(column, out var value))
            {
                return value;
            }

            return GetData(column);
        }

        protected virtual bool TryGetOriginalValue(CoreDataColumnContainer column, out object value)
        {
            value = null;

            if (Props.OriginalData != null)
            {
                value = Props.OriginalData[column.ColumnHandle];
                return true;
            }

            return false;
        }

        public void SilentlySetValue(ICoreTableReadOnlyColumn column, object value)
        {
            if (column == null)
            {
                throw new ArgumentNullException(nameof(column));
            }
            
            SilentlySetValue(column.ColumnName, value);
        }

        public void SilentlySetValue(string columnOrXProp, object value)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{DebugKeyValue}' data row container of '{TableName}' table because it is readonly.");
            }
            
            var hasColumn = MetadataProps.ColumnMap.TryGetValue(columnOrXProp, out var idx);

            if (hasColumn == false)
            {
                return;
            }

            SetData(idx, value);
        }

        void ICoreDataRowAccessor.SilentlySetValue(ICoreTableReadOnlyColumn columnName, object value)
        {
            SilentlySetValue(columnName, value);
        }

        public IEnumerable<string> GetChangedFields()
        {
            var dataRow = this;

            var rowState = dataRow.RowRecordState;

            if (rowState == RowState.Unchanged)
            {
                yield break;
            }

            if (rowState is RowState.Added or RowState.New)
            {
                foreach (var column in MetadataProps.Columns)
                {
                    yield return column.ColumnName;
                }
            }
            else
            {
                if (Props.ChangedFields != null)
                {
                    foreach (var changedField in Props.ChangedFields)
                    {
                        yield return changedField;
                    }
                }
            }
        }

        [NotNull]
        public IEnumerable<string> GetChangedXProperties()
        {
            var dataRow = this;

            var rowState = dataRow.RowRecordState;

            if (rowState == RowState.Unchanged || Props.ExtProperties == null)
            {
                yield break;
            }

            if (rowState is RowState.Added or RowState.New)
            {
                foreach (var xProperty in Props.ExtProperties.Keys)
                {
                    yield return xProperty;
                }
            }

            foreach (var extendedProperty in Props.ExtProperties)
            {
                if (extendedProperty.Value.Current != null)
                {
                    yield return extendedProperty.Key;
                }
            }
        }

        public void AcceptChanges()
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot accept changes for '{DebugKeyValue}' data row container of '{TableName}' table because it is readonly.");
            }
            
            RowRecordState = RowState.Unchanged;

            AcceptChangesCore();

            ClearChangedFields();

            AcceptChangesExtendedProperties();
        }

        public void SetAdded()
        {
            RowRecordState = RowState.Added;
            
            Props.OriginalData?.Dispose();
            Props.OriginalData = null;
        }

        private void ClearChangedFields()
        {
            Props.ChangedFields?.Dispose();
            Props.ChangedFields = null;
        }

        public virtual bool RejectChanges()
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot reject changes for '{DebugKeyValue}' data row container of '{TableName}' table because it is readonly.");
            }
            
            RowRecordState = RowState.Unchanged;

            RejectChangesCore();

            ClearChangedFields();

            RejectChangesExtendedProperties();
            
            ClearLoggedChanges();

            return true;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowReadOnlyContainer ICoreDataRowReadOnlyContainer.Clone()
        {
            return Clone();
        }

        void ICoreDataRowContainer.SetColumnXProperty<T>(string column, string property, T value)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{DebugKeyValue}' data row container of '{TableName}' table {column} extended property because container is readonly.");
            }
            
            var columnContainer = GetColumn(column);

            columnContainer?.SetXProperty(property, value);

            if (IsInitializing == false)
            {
                Interlocked.Increment(ref Props.Age); 
            }
        }

        public bool IsInitializing
        {
            get
            {
                if (m_initLockCount > 0)
                {
                    return true;
                }

                if (m_initLockCount <= 0)
                {
                    m_initLockCount = 0;
                }

                return false;
            }
        }

        protected int m_initLockCount;
        private CoreContainerMetadataProps m_metadataProps;
        private CoreContainerDataProps m_containerDataProps;

        protected object DebugKeyValue
        {
            get
            {
                if (RowRecordState != RowState.Detached)
                {
                    var pk = PrimaryKeyColumn.ToArray();

                    if (pk.Length > 0)
                    {
                        return string.Join(", ", pk.Select(k =>
                        {
                            var value = this[k.ColumnName];

                            return $"{k.ColumnName} = {(value ?? "{NULL}")}";
                        }));
                    }

                    return "Row container handle: " + RowHandle;
                }

                return $"Row container {RowHandle} is detached";
            }
        }

        ICoreDataRowContainer ICoreDataRowContainer.Set<T>(string columnOrXProp, T value)
        {
            if (TypeConvertor.CanBeTreatedAsNull<T>(value))
            {
                SetNull(columnOrXProp);

                return this;
            }
            
            if (value == null)
            {
                SetNull(columnOrXProp);
            }
            else
            {
                this[columnOrXProp] = value;
            }

            return this;
        }

        public ICoreDataRowContainer SetNull(string columnOrXProp)
        {
            this[columnOrXProp] = null;

            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public object this[ICoreTableReadOnlyColumn column]
        {
            get => this[column.ColumnName];
            set => this[column.ColumnName] = value;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        object ICoreDataRowContainer.this[int handle]
        {
            get => this[handle];
            set => this[handle] = value;
        }

        public object this[int handle]
        {
            get
            {
                return GetData(MetadataProps.GetColumn(handle));
            }
            set
            {
                if (IsInitializing == false && IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot change '{DebugKeyValue}' data row container of '{TableName}' table because it is readonly.");
                }

                var rowState = RowRecordState;

                EnsureOriginalData();

                var container = MetadataProps.GetColumn(handle);

                var isNull = TypeConvertor.CanBeTreatedAsNullBoxed(container.Type, container.TypeModifier, value);

                if (isNull)
                {
                    SetCore(container.ColumnName, container, null);
                }
                else
                {
                    var columnType = CoreDataTable.GetColumnType(container.Type, container.TypeModifier, container.AllowNull, container.DataType);

                    if (value is IConvertible)
                    {
                        var newValue = Tool.ConvertBoxed(value, columnType);
                        SetCore(container.ColumnName, container, newValue);
                    }
                    else
                    {
                        SetCore(container.ColumnName, container, value);
                    }
                }
            }
        }

        public void CopyChanges(ICoreDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot copy changes to '{DebugKeyValue}' data row container of '{TableName}' table because it is readonly.");
            }
            
            var changedFields = rowAccessor.GetChangedFields().ToSet();

            var rowAccessorColumns = rowAccessor.GetColumns();

            var unchangedFields = new Set<string>();

            foreach (var rowAccessorColumn in rowAccessorColumns)
            {
                if (!(changedFields.Contains(rowAccessorColumn.ColumnName)))
                {
                    unchangedFields.Add(rowAccessorColumn.ColumnName);
                }
            }
            
            changedFields.Dispose();

            var skipF = unchangedFields.TryCombine(skipFields);

            CopyFrom(rowAccessor, skipF);
            
            unchangedFields.Dispose();
        }

        public virtual void CopyAll(ICoreDataRowReadOnlyAccessor rowAccessor)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot copy data to '{DebugKeyValue}' data row container of '{TableName}' table because it is readonly.");
            }
            
            if (RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            if (MetadataProps == null || ContainerDataProps == null)
            {
                Init(rowAccessor);
            }

            var predefinedColumns = GetPredefinedColumns();
           
            foreach (var column in rowAccessor.GetColumns())
            {
                if (predefinedColumns.Contains(column.ColumnName))
                {
                    continue;
                }

                var dataColumn = TryGetColumn(column.ColumnName);

                if (dataColumn != null)
                {
                    SetColValue(dataColumn, rowAccessor[column]);
                }
            }

            foreach (var xProperty in rowAccessor.GetXProperties())
            {
                SetXProperty(xProperty, rowAccessor.GetXProperty<object>(xProperty));
            }
        }

        public virtual void CopyFrom(ICoreDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot copy data to '{DebugKeyValue}' data row container of '{TableName}' table because it is readonly.");
            }
            
            if (RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            if (MetadataProps == null || ContainerDataProps == null)
            {
                Init(rowAccessor, skipFields);
            }

            foreach (var column in rowAccessor.GetColumns())
            {
                if (skipFields != null && skipFields.Contains(column.ColumnName))
                {
                    continue;
                }

                this[column.ColumnName] = rowAccessor[column];
            }

            foreach (var xProperty in rowAccessor.GetXProperties())
            {
                if (skipFields != null && skipFields.Contains(xProperty))
                {
                    continue;
                }

                SetXProperty(xProperty, rowAccessor.GetXProperty<object>(xProperty));
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        object ICloneable.Clone()
        {
            return Clone();
        }

        public CoreDataRowContainer Clone()
        {
            return CloneCore();
        }

        protected virtual CoreDataRowContainer CloneCore()
        {
            var newObj = (CoreDataRowContainer)MemberwiseClone();

            newObj.ContainerDataProps = ContainerDataProps.Clone();

            return newObj;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowContainer ICoreDataRowContainer.Clone()
        {
            return Clone();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public ICoreDataRowReadOnlyContainer ToReadOnly()
        {
            return Clone();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowReadOnlyContainer ICoreDataRowContainer.ToReadOnly()
        {
            return ToReadOnly();
        }

        public virtual T GetFieldValue<T>(string columnOrXProp, T defaultIfNull, DefaultValueType defaultValueType)
        {
            var value = this[columnOrXProp];

            var isNull = value == null;
            
            if (value != null)
            {
                var (type, typeModifier) = GetValueType<T>(columnOrXProp);

                isNull = TypeConvertor.CanBeTreatedAsNullBoxed(type.Value, typeModifier.Value, value);
            }

            if (isNull)
            {
                if (defaultValueType == DefaultValueType.ColumnBased)
                {
                    if (MetadataProps.ColumnMap.TryGetValue(columnOrXProp, out var column))
                    {
                        value = column.DefaultValue;

                        if (value == null)
                        {
                            value = TypeConvertor.ReturnDefault<T>();
                        }
                    }
                }
                else if (defaultIfNull == null)
                {
                    value = TypeConvertor.ReturnDefault<T>();
                }
                else
                {
                    return defaultIfNull;
                }
            }

            if(value is T tv)
            {
                return tv;
            }
            else
            {
                var (type, typeModifier) = GetValueType<T>(columnOrXProp);
                
                return TypeConvertor.ConvertValue<T>(value, columnOrXProp, TableName, type, typeModifier, "field value getter");
            }
        }

        private (TableStorageType? type, TableStorageTypeModifier? typeModifier) GetValueType<T>(string columnOrXProp)
        {
            var columnContainer = MetadataProps.TryGetColumn(columnOrXProp);

            var type = columnContainer?.Type;
            var typeModifier = columnContainer?.TypeModifier;

            if (columnContainer == null)
            {
                var valueTuple = CoreDataTable.GetColumnType(typeof(T));

                type = valueTuple.type;
                typeModifier = valueTuple.typeModifier;
            }

            return (type, typeModifier);
        }

        public IReadOnlyList<T> FieldArray<T>(string columnOrXProperty)
        {
            return FieldArray<T>(columnOrXProperty, Array.Empty<T>());
        }
        
        public virtual IReadOnlyList<T> FieldArray<T>(string columnOrXProperty, IReadOnlyList<T> defaultIfNull)
        {
            var hasColumn = MetadataProps.ColumnMap.TryGetValue(columnOrXProperty, out var column);

            object value;
            
            if (hasColumn == false)
            {
                if (Props.ExtProperties?.ContainsKey(columnOrXProperty) ?? false)
                {
                    value = GetXPropertyCore<T>(columnOrXProperty);
                }
                else
                {
                    throw new MissingMetadataException($"Data row container created from '{MetadataProps.TableName}' does not have {columnOrXProperty} column or xproperty.");
                }
            }
            else
            {
                value = Props.Data[column.ColumnHandle];
            }

            if (value == null)
            {
                return defaultIfNull ?? Array.Empty<T>();
            }

            if (value is IReadOnlyList<T> tv)
            {
                return tv;
            }

            throw new InvalidCastException($"The '{columnOrXProperty}' column or XProperty of the row '{DebugKeyValue}' container of the '{TableName}' table cannot be casted to readonly list.");
        }

        private void RejectChangesExtendedProperties()
        {
            if (Props.ExtProperties != null)
            {
                var data = Props.ExtProperties.ToData();
                
                foreach (var tuple in data)
                {
                    Props.ExtProperties[tuple.Key] = new ExtPropertyValue { Original = tuple.Value.Original };
                }
                
                data.Dispose();
            }
        }

        private void AcceptChangesExtendedProperties()
        {
            if (Props.ExtProperties != null)
            {
                var data = Props.ExtProperties.ToData();
                
                foreach (var tuple in data)
                {
                    Props.ExtProperties[tuple.Key] = new ExtPropertyValue { Original = tuple.Value.Current ?? tuple.Value.Original, Current = null };
                }
                
                data.Dispose();
            }
        }

        public virtual void SetXProperty<T>(string propertyCode, T value) 
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{propertyCode}' extended property for '{DebugKeyValue}' data row container of '{TableName}' table because it is readonly.");
            }
            
            if (Props.ExtProperties == null)
            {
                Props.ExtProperties = new Map<string, ExtPropertyValue>(StringComparer.OrdinalIgnoreCase);
            }

            var oldValue = GetXProperty<T>(propertyCode);

            var newValue = value;
            
            OnBeforeXPropSet<T>(propertyCode, oldValue, ref newValue, out var cancel);

            if (cancel)
            {
                return;
            }

            CopyIfNeeded(ref newValue);
            
            OnDataChange();

            if (Props.ExtProperties.ContainsKey(propertyCode))
            {
                ref var propertyValue = ref Props.ExtProperties.ValueByRef(propertyCode, out var _);
                
                propertyValue.Current = newValue;
                propertyValue.HasCurrent = true;
            }
            else
            {
                Props.ExtProperties[propertyCode] = new ExtPropertyValue { Original = newValue };
            }

            OnAfterXPropSet(propertyCode, oldValue: oldValue, newValue: newValue);

            LogChange(propertyCode, value: newValue, prevValue: oldValue);
        }

        private static readonly ConcurrentDictionary<Type, bool> s_immutableTypeCache = new();
        
        public static void CopyIfNeededBoxed(ref object val)
        {
            if (val is null)
            {
                return;
            }

            var type = val.GetType();

            if (type.IsValueType)
            {
                return;
            }

            if (s_immutableTypeCache.TryGetValue(type, out var immutable) == false)
            {
                s_immutableTypeCache[type] = immutable = Tool.IsImmutable(type);
            }
            
            if (immutable)
            {
                return;
            }

            if (val is IReadonlySupported ro && ro.IsReadOnly)
            {
                return;
            }
            
            if (val is ICloneable cl)
            {
                val = cl.Clone();
                
                return;
            }
            
            if (CoreDataTable.UserTypeCloneRegistry.TryGetValue(type, out var cloneFunc))
            {
                val = cloneFunc(val);
                
                return;
            }
            
            throw new NotSupportedException($"Cloning user type {type.FullName} does not supported. Please use CoreDataTable.RegisterUserType<T>.");
        }

        public static void CopyIfNeeded<T>(ref T val)
        {
            if (Tool.IsImmutable<T>())
            {
                return;
            }

            if (Tool.IsObject<T>())
            {
                object boxedVal = val;
                
                CopyIfNeededBoxed(ref boxedVal);

                val = (T)boxedVal;
                
                return;
            }

            CopyIfNeededCore(ref val);
        }

        private static void CopyIfNeededCore<T>(ref T val)
        {
            if (val == null)
            {
                return;
            }
            
            if (Tool.IsImmutable<T>() || (Tool.IsReadonlySupported<T>() && ((IReadonlySupported)val).IsReadOnly))
            {
                return;
            }
            
            if (val is ICloneable cl)
            {
                val = (T)cl.Clone();
                
                return;
            }

            var type = typeof(T);
            
            if (type.IsValueType)
            {
                return;
            }
            
            if (CoreDataTable.UserTypeCloneRegistry.TryGetValue(type, out var cloneFunc))
            {
                val = (T)cloneFunc(val);
                
                return;
            }
            
            throw new NotSupportedException($"Cloning user type {type.FullName} does not supported. Please use CoreDataTable.RegisterUserType<T>.");
        }

        protected virtual void OnAfterXPropSet(string propertyCode, object oldValue, object newValue)
        {
        }

        protected virtual void OnBeforeXPropSet<T>(string propertyCode, T oldValue, ref T valueStr, out bool cancel)
        {
            cancel = false;
        }

        [CanBeNull]
        public object SilentlyGetValue(string columnName)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change the '{DebugKeyValue}' data row container of '{TableName}' table because container is readonly.");
            }
            
            var hasColumn = MetadataProps.ColumnMap.TryGetValue(columnName, out var idx);

            if (hasColumn == false)
            {
                return null;
            }

            return GetData(idx);
        }

        public virtual IEnumerable<string> GetXProperties()
        {
            if (Props.ExtProperties == null)
            {
                return Enumerable.Empty<string>();
            }

            return Props.ExtProperties.Keys;
        }

        [CanBeNull]
        public virtual T GetXProperty<T>(string xPropertyName, bool original = false)
        {
            if (Props.ExtProperties == null)
            {
                return TypeConvertor.ReturnDefault<T>();
            }

            var value = GetXPropertyCore<T>(xPropertyName);

            T storageValue;

            if (original)
            {
                storageValue = XPropertyValueConverter.TryConvert<T>("Row original", xPropertyName, value.Original);
            }
            else
            {
                if (value.HasCurrent && value.Current == null)
                {
                    return default;
                }
                
                storageValue = XPropertyValueConverter.TryConvert<T>("Row", xPropertyName, value.Current ?? value.Original);
            }

            CoreDataRowContainer.CopyIfNeeded(ref storageValue);

            return storageValue;
        }

        private ExtPropertyValue GetXPropertyCore<T>(string xPropertyName)
        {
            Props.ExtProperties.TryGetValue(xPropertyName, out var value);
            return value;
        }

        public virtual bool HasXProperty(string xPropertyName)
        {
            return Props.ExtProperties?.ContainsKey(xPropertyName) ?? false;
        }

        protected virtual void DisposeCore()
        {
            try
            {
                if (ContainerDataProps != null)
                {
                    ContainerDataProps.DataRowState = RowState.Disposed;
                    ContainerDataProps.Dispose();
                }
            }
            catch
            {
            }
        }

        public void Dispose()
        {
            DisposeCore();
            GC.SuppressFinalize(this);
        }
        
        ~CoreDataRowContainer()
        {
            DisposeCore();
        }

        public int CompareTo(ICoreDataRowContainer other)
        {
            var tuple = CompareDataRows(this, other);
            
            return tuple.cmp;
        }
        
        public int CompareTo(ICoreDataRowReadOnlyContainer other)
        {
            var tuple = CompareDataRows(this, other);
            
            return tuple.cmp;
        }
        
        public int CompareTo(CoreDataRowContainer other)
        {
            var tuple = CompareDataRows(this, other);
            
            return tuple.cmp;
        }
        
        public int CompareTo(object obj)
        {
            if (obj is CoreDataRowContainer dc)
            {
                return CompareTo(dc);
            }
            if (obj is ICoreDataRowReadOnlyContainer drc)
            {
                return CompareTo(drc);
            }

            return -1;
        }

        public override bool Equals(object obj)
        {
            if (obj is ICoreDataRowContainer other1)
            {
                return Equals(other1);
            }
            
            if (obj is ICoreDataRowReadOnlyContainer other2)
            {
                return Equals(other2);
            }
            
            return ReferenceEquals(this, obj);
        }

        public bool Equals(CoreDataRowContainer other)
        {
            var tuple = EqualsDataRowContainers(this, other);
            
            return tuple.value;
        }
        
        public (bool value, string name, string type) EqualsExt(CoreDataRowContainer other)
        {
            return EqualsDataRowContainers(this, other);
        }
        
        public (bool value, string name, string type) EqualsExt(ICoreDataRowReadOnlyContainer other)
        {
            return EqualsDataRowContainers(this, other);
        }
        
        public bool Equals(ICoreDataRowReadOnlyContainer other)
        {
            var tuple = EqualsDataRowContainers(this, other);
            
            return tuple.value;
        }

        public bool Equals(ICoreDataRowContainer other)
        {
            var tuple = EqualsDataRowContainers(this, other);
            
            return tuple.value;
        }
        
        public (bool value, string name, string type) EqualsExt(ICoreDataRowContainer other)
        {
            return EqualsDataRowContainers(this, other);
        }

        public static (int cmp, string name, string type) CompareDataRows(ICoreDataRowReadOnlyAccessor contX, ICoreDataRowReadOnlyAccessor contY)
        {
            var tableName = contX.GetTableName();
            
            var compareTo = string.CompareOrdinal(tableName, contY.GetTableName());

            if (compareTo != 0)
            {
                return (compareTo, tableName, "table");
            }

            var pkColumns = contX.PrimaryKeyColumn.ToArray();

            if (pkColumns.Length > 0)
            {
                bool all = false;

                int i = 0;
                foreach (var otherKeyColumn in contY.PrimaryKeyColumn)
                {
                    var thisKeyColumn = pkColumns[i];

                    var compCol = string.CompareOrdinal(thisKeyColumn.ColumnName, otherKeyColumn.ColumnName);

                    if (compCol != 0)
                    {
                        return (compCol, thisKeyColumn.ColumnName, "primary key column name");
                    }

                    var thisKeyVal = (IComparable)contX[thisKeyColumn];
                    var otherKeyVal = (IComparable)contY[thisKeyColumn];

                    if (thisKeyVal is null && otherKeyVal is null)
                    {
                        continue;
                    }

                    if (otherKeyVal is null)
                    {
                        return (-1, thisKeyColumn.ColumnName, "primary key value null");
                    }

                    var keyComp = thisKeyVal?.CompareTo(otherKeyVal);

                    all = keyComp == 0;

                    if (all)
                    {
                        continue;
                    }

                    return (keyComp ?? -1, thisKeyColumn.ColumnName, "primary key value");
                }
            }
            
            {
                bool all = false;

                foreach (var column in contX.GetColumns())
                {
                    if (contY.IsExistsField(column.ColumnName) == false)
                    {
                        continue;
                    }
                    
                    var thisO = contX[column];
                    var rowO = contY[column.ColumnName];

                    if (thisO is IComparable thisComp)
                    {
                        if (contY.IsExistsField(column.ColumnName) == false)
                        {
                            continue;
                        }

                        var otherIsNull = contY.IsNull(column.ColumnName);

                        if (contX.IsNull(column) && otherIsNull)
                        {
                            continue;
                        }

                        if (otherIsNull)
                        {
                            return (-1, column.ColumnName, "column value is null");
                        }

                        if (rowO is IComparable otherComp)
                        {
                            var keyComp = thisComp.CompareTo(otherComp);

                            all = keyComp == 0;

                            if (all)
                            {
                                continue;
                            }

                            return (keyComp, column.ColumnName, "column value");
                        }
                    }
                    else
                    {
                        if (thisO is null != rowO is null)
                        {
                            if (rowO is null)
                            {
                                return (-1, "<Row <> Null>", "column value is null");
                            }
                            
                            return (1, "<Row <> Null>", "column value");
                        }

                        if (thisO is not null)
                        {
                            var type = thisO.GetType();

                            var tableStorageType = CoreDataTable.GetColumnType(type);

                            var str = ValuesAreEqual(thisO, rowO, column.ColumnName, tableStorageType.type, tableStorageType.typeModifier, type);

                            all = str == null;

                            if (all)
                            {
                                continue;
                            }

                            return (-1, str, "column values not equal");
                        }
                    }
                }

                if (all == false)
                {
                    return (-1, "-Row-", "Should not happen");
                }
            }

            return CompareXProperties(contX, contY);
        }
        
        private static (int cmp, string name, string type) CompareXProperties(ICoreDataRowReadOnlyAccessor contX, ICoreDataRowReadOnlyAccessor contY)
        {
            int compareTo;
            
            var xc1 = contX.GetXProperties().OrderBy(r => r).ToArray();
            var xc2 = contY.GetXProperties().OrderBy(r => r).ToArray();

            compareTo = xc1.Length.CompareTo(xc2.Length);

            if (compareTo != 0)
            {
                return (compareTo, "-XPropLen-", "XProp lengths are different");
            }

            var x1 = xc1.GetEnumerator();
            var x2 = xc2.GetEnumerator();

            while (x1.MoveNext() && x2.MoveNext())
            {
                var x1Property = (string)x1.Current;
                var x2Property = (string)x2.Current;

                compareTo = x1Property.CompareTo(x2Property);

                if (compareTo != 0)
                {
                    return (compareTo, x1Property, "XProp names are different");
                }

                var xp1 = contX.GetXProperty<object>(x1Property);
                var xp2 = contY.GetXProperty<object>(x1Property);

                if (xp1 is null != xp2 is null)
                {
                    if (xp2 is null)
                    {
                        return (-1, x1Property, "XProps values are different. Some is null.");
                    }

                    return (1, x2Property, "XProps values are different.");
                }

                if (xp1 is IComparable cxp1 && xp2 is IComparable cxp2)
                {
                    compareTo = cxp1.CompareTo(cxp2);
                    
                    if (compareTo != 0)
                    {
                        return (compareTo, x1Property, "XProps values are not equal.");
                    }
                }
                else if(xp1 is not null)
                {
                    var type = xp1.GetType();

                    var tableStorageType = CoreDataTable.GetColumnType(type);

                    var str = ValuesAreEqual(xp1, xp2, x1Property, tableStorageType.type, tableStorageType.typeModifier, type);

                    var deepEquals = str == null;

                    if (deepEquals == false)
                    {
                        return (-1, str, "XProps values are not the same.");
                    }
                }
            }

            return (0, string.Empty, string.Empty);
        }

        public static (bool value, string name, string type) ContentEquals(ICoreDataRowReadOnlyAccessor rowX, ICoreDataRowReadOnlyAccessor rowY)
        {
            foreach (var column in rowX.GetColumns())
            {
                var columnOrXProp = column.ColumnName;
                
                if (rowY.IsExistsField(columnOrXProp) == false)
                {
                    return (false, columnOrXProp, "Column does not exist");
                }

                //todo check contents without copying
                if (column.TypeModifier == TableStorageTypeModifier.Array)
                {
                }

                var thisO = rowX[column];
                var rowO = rowY[columnOrXProp];

                var columnName = ValuesAreEqual(thisO, rowO, columnOrXProp, column.Type, column.TypeModifier, column.DataType);
                if (columnName != null)
                {
                    return (false, columnName, "Column values are different");
                }
            }

            var processedX = new Set<string>();

            foreach (var propertyName in rowX.GetXProperties())
            {
                var thisO = rowX.GetXProperty<object>(propertyName);
                var rowO = rowY.GetXProperty<object>(propertyName);

                processedX.Add(propertyName);
                
                var type = thisO?.GetType();
                
                var tableStorageType = type == null
                    ? (TableStorageType.Empty, TableStorageTypeModifier.Simple, true)
                    : CoreDataTable.GetColumnType(type);

                var xPropName = ValuesAreEqual(thisO, rowO, propertyName, tableStorageType.Item1, tableStorageType.Item2, type);
                if (xPropName != null)
                {
                    return (false, xPropName, "XProp1 values are different");
                }
            }
            
            foreach (var propertyName in rowY.GetXProperties())
            {
                if (processedX.Contains(propertyName))
                {
                    continue;
                }
                
                var thisO = rowX.GetXProperty<object>(propertyName);
                var rowO = rowY.GetXProperty<object>(propertyName);

                processedX.Add(propertyName);

                var type = thisO?.GetType();
                
                var tableStorageType = type == null
                    ? (TableStorageType.Empty, TableStorageTypeModifier.Simple, true)
                    : CoreDataTable.GetColumnType(type);

                var xPropName = ValuesAreEqual(thisO, rowO, propertyName, tableStorageType.Item1, tableStorageType.Item2, type);
                if (xPropName != null)
                {
                    return (false, xPropName, "XProp2 values are different");
                }
            }
            
            processedX.Dispose();

            return (true, string.Empty, string.Empty);
        }

        public static bool DeepEquals(object thisO, object rowO)
        {
            var type1 = thisO?.GetType();
            var type2 = rowO?.GetType();

            if (type1 != type2)
            {
                return false;
            }
            
            var tableStorageType = type1 == null
                ? (TableStorageType.Empty, TableStorageTypeModifier.Simple, true)
                : CoreDataTable.GetColumnType(type1);

            var valuesAreEqual = ValuesAreEqual(thisO, rowO, "temp", tableStorageType.Item1, tableStorageType.Item2, type1);

            return valuesAreEqual == null;
        }
        
        private static string ValuesAreEqual(object thisO,
            object rowO,
            string columnOrXProp,
            TableStorageType tableStorageType,
            TableStorageTypeModifier columnTypeModifier,
            Type columnDataType)
        {
            var thisIsNull = TypeConvertor.CanBeTreatedAsNullBoxed(tableStorageType, columnTypeModifier, thisO);
            var rowOIsNull = TypeConvertor.CanBeTreatedAsNullBoxed(tableStorageType, columnTypeModifier, rowO);
                
            if (thisIsNull != rowOIsNull)
            {
                return columnOrXProp;
            }

            if (thisIsNull == false)
            {
                if (columnTypeModifier == TableStorageTypeModifier.Array)
                {
                    if (Tool.ArrayDeepEquals((Array)thisO, (Array)rowO) == false)
                    {
                        return columnOrXProp;
                    }
                }
                else if(columnTypeModifier == TableStorageTypeModifier.Range)
                {
                    if (thisO.Equals(rowO) == false)
                    {
                        return columnOrXProp;
                    }
                }
                else
                {
                    if (tableStorageType == TableStorageType.UserType)
                    {
                        if (thisO.Equals(rowO) == false)
                        {
                            return columnOrXProp;
                        }
                    }
                    else
                    {
                        var deepEquals = TableStorageDeepEquals.DeepEquals(tableStorageType, thisO, rowO);

                        if (deepEquals == false)
                        {
                            return columnOrXProp;
                        }
                    }
                }
            }

            return null;
        }

        public override int GetHashCode()
        {
            return ContainerHashCode(this);
        }

        public static int ContainerHashCode(ICoreDataRowReadOnlyAccessor accessor)
        {
            var hashCode = 0;
            foreach (var column in accessor.PrimaryKeyColumn)
            {
                var value = accessor[column];

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

        public static (bool value, string name, string type) EqualsDataRowContainers(ICoreDataRowReadOnlyContainer contX, ICoreDataRowReadOnlyContainer contY)
        {
            if (contX.GetTableName() != contY.GetTableName())
            {
                return (false, "Table name", nameof(CoreDataRowContainer));
            }
            
            var pkColumns = contX.PrimaryKeyColumn.ToArray();

            if (pkColumns.Length > 0)
            {
                int i = 0;
                foreach (var otherKeyColumn in contY.PrimaryKeyColumn)
                {
                    var thisKeyColumn = pkColumns[i];

                    var compCol = string.Equals(thisKeyColumn.ColumnName, otherKeyColumn.ColumnName);

                    if (compCol == false)
                    {
                        return (false, thisKeyColumn.ColumnName, "PK column name is different");
                    }

                    var thisKeyVal = (IComparable)contX[thisKeyColumn];
                    var otherKeyVal = (IComparable)contY[thisKeyColumn];

                    if (thisKeyVal is null && otherKeyVal is null)
                    {
                        i++;
                        continue;
                    }

                    if (otherKeyVal is null)
                    {
                        return (false, thisKeyColumn.ColumnName, "Other PK value is null");
                    }

                    var keyComp = thisKeyVal?.CompareTo(otherKeyVal) ?? -1;

                    if (keyComp != 0)
                    {
                        return (false, thisKeyColumn.ColumnName, "PK values are different");
                    }

                    i++;
                }
            }

            return ContentEquals(contX, contY);
        }

        public bool IsReadOnly { get; set; }

        public IEnumerable<CoreDataColumnContainer> Columns => MetadataProps.Columns;

        protected virtual CoreRowContainerSerializer<T, V> CreateRowContainerSerializer<T, V>(SerializerAdapter<T, V> serializer)
        {
            return new CoreRowContainerSerializer<T, V>(this, serializer);
        }

        public XElement ToXml(SerializationMode mode = SerializationMode.Full)
        {
            var serializer = CreateRowContainerSerializer(new XmlSerializer());

            var root = new XElement("DataRowContainer", new XAttribute("TableName", TableName), new XAttribute("IsReadOnly", IsReadOnly));

            if (mode == SerializationMode.Full || mode == SerializationMode.SchemaOnly)
            {
                root.Add(serializer.SerializeSchema());
            }

            if (mode == SerializationMode.Full || mode == SerializationMode.DataOnly)
            {
                serializer.WriteDataTo(root);
            }

            return root;
        }

        XElement IXmlSerializable.ToXml()
        {
            return ToXml(SerializationMode.Full);
        }

        public void FromXml(XElement element)
        {
            var serializer = CreateRowContainerSerializer(new XmlSerializer());

            serializer.Deserialize(element);
        }

        public JElement ToJson(SerializationMode mode = SerializationMode.Full)
        {
            var root = new JElement("DataRowContainer");

            root.AddAttribute(new JAttribute("TableName", TableName));
            root.AddAttribute(new JAttribute("IsReadOnly", IsReadOnly));
                
            var serializer = CreateRowContainerSerializer(new JsonSerializer());

            if (mode == SerializationMode.Full || mode == SerializationMode.SchemaOnly)
            {
                var simpleSchema = serializer.SerializeSchema();

                root.AddElement(new JElement("Metadata", simpleSchema));
            }

            if (mode == SerializationMode.Full || mode == SerializationMode.DataOnly)
            {
                serializer.WriteDataTo(root);
            }

            return root;
        }

        JElement IJsonSerializable.ToJson()
        {
            return ToJson(SerializationMode.Full);
        }

        public void FromJson(JElement element)
        {
            var serializer = CreateRowContainerSerializer(new JsonSerializer());

            serializer.Deserialize(element);
        }
        
        public IComparable[] GetRowKeyValue()
        {
            var handles = MetadataProps.PrimaryKeyColumn;
            
            var res = new IComparable[handles.Count];

            for (int i = 0; i < res.Length; i++)
            {
                res[i] = (IComparable)this[handles[i]];
            }

            return res;
        }
    }
}