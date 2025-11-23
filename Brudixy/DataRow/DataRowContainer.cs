using System.Collections.Frozen;
using System.Diagnostics;
using Brudixy.Delegates;
using Brudixy.Interfaces;
using Brudixy.Interfaces.Delegates;
using Brudixy.EventArgs;
using Brudixy.Exceptions;
using Brudixy.Expressions;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    [DebuggerTypeProxy(typeof(DataRowDebugView))]
    [DebuggerDisplay("Row container of {TableName}, State {RowRecordState}")]
    public partial class DataRowContainer : CoreDataRowContainer,
        IDisposable, 
        ICloneable, 
        IDataRowContainer,
        IDataLockEventState, 
        IComparable<IDataRowContainer>, 
        IComparable<IDataRowReadOnlyContainer>,
        IComparable<DataRowContainer>, 
        IEquatable<DataRowContainer>, 
        IEquatable<IDataRowContainer>,
        IEquatable<IDataRowReadOnlyContainer>,
        IXmlSerializable,
        IJsonSerializable,
        IDataLoadState,
        IReadonlySupported,
        IComparable
    {
        internal DataEvent<IDataContainerFieldValueChangingArgs> m_dataFieldChangingEvent;
        internal DataEvent<IDataContainerFieldValueChangedArgs> m_dataFieldChangedEvent;
        internal DataEvent<IDataContainerXPropertyChangedArgs> m_dataXPropertyChangedEvent;
        internal DataEvent<IDataContainerXPropertyChangingArgs> m_dataXPropertyChangingEvent;
        internal DataEvent<IDataContainerMetaDataChangedArgs> m_metaDataChangedEvent;

        [CanBeNull]
        private DataExpressionCache m_expressionCache;

        private readonly DisposableCollection m_eventsReferenceHolder = new();

        [NotNull]
        protected DataExpressionCache DataExpressions => m_expressionCache ??= new (this);

        [CanBeNull]
        private Map<string, DataTable.ColumnChange> m_columnChangedAggregated;

        [CanBeNull]
        private Map<string, DataTable.XPropertyChange> m_xPropertyChangedAggregated;
      

        protected virtual List<string> GetPrimaryKey() => new();

        protected virtual string GetDefaultTableName() => string.Empty;

        protected virtual DataColumnContainerBuilder CreateDataColumnContainerBuilder(string columnName, TableStorageType type,
            TableStorageTypeModifier modifier, Type dataType, IReadOnlyDictionary<string, object> xProps = null) =>
            new DataColumnContainerBuilder()
            {
                ColumnName = columnName, 
                Type = type, 
                TypeModifier = modifier, 
                DataType = dataType,
                TableName = GetDefaultTableName()
            };

        protected virtual DataColumnContainer CreateDataColumnContainer(DataColumnObj columnObj) => new DataColumnContainer(columnObj);
        
        public void InitNew()
        {
            var dataColumnContainers = this
                .GetDataColumnContainers()
                .Select((s, i) =>
                {
                    s.ColumnHandle = i;
                    return (CoreDataColumnContainer)CreateDataColumnContainer((DataColumnObj)s.ToImmutable());
                })
                .ToArray();

            IReadOnlyDictionary<string, CoreDataColumnContainer> colMap = dataColumnContainers.ToFrozenDictionary(
                c => c.ColumnName,
                c => c, 
                StringComparer.OrdinalIgnoreCase);

            var containerMetadataProps = new CoreContainerMetadataProps(GetDefaultTableName(), dataColumnContainers, colMap, this.GetPrimaryKey(), 0);

            var data = new Data<object>();
            data.Ensure(colMap.Count);
            
            var containerDataProps = new ContainerDataProps(-1, data, RowState.Detached);

            this.Init(containerMetadataProps, containerDataProps);
        }

        public bool CheckFilter(string expression, IReadOnlyDictionary<string, object> testing = null)
        {
            var select = new Select(new DataRowExpressionSource(this, testing), expression);
            return select.SelectRows<DataRowContainer>().Any();
        }

        public void SetXPropertyAnnotation([CanBeNull] string propertyCode, string key, object value)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{propertyCode}' xProperty '{key}' annotation of '{DebugKeyValue}' data row container of '{TableName}' because it is readonly.");
            }
            
            if (m_currentEditRow != null)
            {
                m_currentEditRow.SetXPropertyAnnotation(propertyCode, key, value);
                
                return;
            }
            
            ContainerDataProps.SetXPropertyInfo(propertyCode, key, value);
            
            if (IsLocked == false)
            {
                ContainerDataProps.AnnotationAge++;
            }
            
            //no aggregate handler
            if (m_metaDataChangedEvent != null && m_metaDataChangedEvent.HasAny())
            {
                var changingArgs = new DataContainerMetaDataChangedArgs(this, propertyCode, key, value, RowMetadataType.XProperty);

                m_metaDataChangedEvent.Raise(changingArgs);
            }
        }

        public IReadOnlyDictionary<string, object> GetXPropertyAnnotationValues(string propertyCode)
        {
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetXPropertyAnnotationValues(propertyCode);
            }
            
            return ContainerDataProps.GetXPropertyInfoValues(propertyCode);
        }

        public T GetXPropertyAnnotation<T>([CanBeNull] string propertyCode, string key)
        {
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetXPropertyAnnotation<T>(propertyCode, key);
            }

            var value = ContainerDataProps.GetXPropertyInfo(propertyCode, key);
            
            return XPropertyValueConverter.TryConvert<T>(propertyCode, key, value);
        }
        
        public IEnumerable<string> XPropertyAnnotations
        {
            get
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.XPropertyAnnotations;
                }
                
                if (ContainerDataProps.XPropInfo != null)
                {
                    return ContainerDataProps.XPropInfo.Keys;
                }

                return Array.Empty<string>();
            }
        }

        void IDataRowAccessor.CopyFrom(IDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            CopyFrom(rowAccessor, skipFields);
        }

        void IDataRowAccessor.CopyChanges(IDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            CopyChanges(rowAccessor, skipFields);
        }

        string ICoreDataRowReadOnlyAccessor.ToString(ICoreTableReadOnlyColumn column)
        {
            return ToString(column);
        }

        string IDataRowReadOnlyAccessor.ToString(IDataTableReadOnlyColumn column, string format = null, IFormatProvider formatProvider = null)
        {
            return ToString(column, format, formatProvider);
        }

        IEnumerable<ICoreTableReadOnlyColumn> ICoreDataRowReadOnlyAccessor.GetColumns()
        {
            return MetadataProps.Columns;
        }
     
        public new static T CreateFrom<T>([NotNull] IDataRowReadOnlyAccessor rowAccessor) where T : DataRowContainer, new()
        {
            if (rowAccessor is DataRow dataRow)
            {
                return (T)dataRow.ToContainer();
            }

            var container = new T();

            container.Init(rowAccessor);

            return container;
        }

        internal new ContainerDataProps ContainerDataProps => (ContainerDataProps)base.ContainerDataProps;
      
        protected override CoreContainerDataProps InitContainerDataProps(ICoreDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields)
        {
            return new ContainerDataProps(rowAccessor, skipFields);
        }

        protected override CoreDataColumnContainer CreateColumnContainer(ICoreTableReadOnlyColumn column)
        {
            return new DataColumnContainer(column);
        }

        protected override void RejectChangesCore()
        {
            m_currentEditRow?.CancelEdit();
            
            m_currentEditRow = null;

            base.RejectChangesCore();
            
            ContainerDataProps.Data.Dispose();

            ContainerDataProps.Data = ContainerDataProps.OriginalData?.ToData();

            EnsureData();
        }

        private void EnsureData()
        {
            if (ContainerDataProps.Data == null)
            {
                ContainerDataProps.Data = new ();
                ContainerDataProps.Data.Ensure(MetadataProps.ColumnMap.Count);
            }
        }

        protected override object GetData(CoreDataColumnContainer column)
        {
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetData(column);
            }

            var coreDataColumnContainer = (DataColumnContainer)column;
            
            if (string.IsNullOrEmpty(coreDataColumnContainer.Expression))
            {
                return ContainerDataProps.Data[column.ColumnHandle];
            }

            return GetExpressionValue(coreDataColumnContainer);
        }

        public uint GetAnnotationAge() => (uint)ContainerDataProps.AnnotationAge;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        protected object GetExpressionValue(string column)
        {
            return GetExpressionValue(MetadataProps.ColumnMap[column]);
        }

        protected virtual object GetExpressionValue(CoreDataColumnContainer column)
        {
            return DataExpressions.GetExpressionValue(column.ColumnHandle);
        }

        protected override void AcceptChangesCore()
        {
            m_currentEditRow?.EndEdit();
            
            base.AcceptChangesCore();
            
            ContainerDataProps.OriginalData?.Dispose();

            ContainerDataProps.OriginalData = null;
        }

        string IDataRowReadOnlyAccessor.GetTableName() => TableName;

        IEnumerable<ICoreTableReadOnlyColumn> ICoreDataRowReadOnlyAccessor.PrimaryKeyColumn => this.MetadataProps.PrimaryKeyColumn;


        [DebuggerHidden]
        public IDataEvent<IDataContainerFieldValueChangingArgs> FieldValueChangingEvent => m_dataFieldChangingEvent ??= new DataEvent<IDataContainerFieldValueChangingArgs>(this.m_eventsReferenceHolder);

        [DebuggerHidden]
        public IDataEvent<IDataContainerFieldValueChangedArgs> FieldValueChangedEvent => m_dataFieldChangedEvent ??= new DataEvent<IDataContainerFieldValueChangedArgs>(this.m_eventsReferenceHolder);

        [DebuggerHidden]
        public IDataEvent<IDataContainerXPropertyChangedArgs> XPropertyChangedEvent => m_dataXPropertyChangedEvent ??= new DataEvent<IDataContainerXPropertyChangedArgs>(this.m_eventsReferenceHolder);

        [DebuggerHidden]
        public IDataEvent<IDataContainerXPropertyChangingArgs> XPropertyChangingEvent => m_dataXPropertyChangingEvent ??= new DataEvent<IDataContainerXPropertyChangingArgs>(this.m_eventsReferenceHolder);
       
        [DebuggerHidden]
        public IDataEvent<IDataContainerMetaDataChangedArgs> MetaDataChangedEvent => m_metaDataChangedEvent ??= new DataEvent<IDataContainerMetaDataChangedArgs>(this.m_eventsReferenceHolder);

        string ICoreDataRowReadOnlyAccessor.GetTableName() => TableName;

        public new IEnumerable<IDataTableReadOnlyColumn> PrimaryKeyColumn => MetadataProps.PrimaryKeyColumn.OfType<IDataTableReadOnlyColumn>();

        ICoreTableReadOnlyColumn ICoreDataRowReadOnlyAccessor.GetColumn(int columnHandle) => GetColumnCore(columnHandle);

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableReadOnlyColumn IDataRowReadOnlyAccessor.TryGetColumn(string columnName)
        {
            return TryGetColumn(columnName);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableReadOnlyColumn IDataRowReadOnlyAccessor.GetColumn(string columnOrXProperty)
        {
            return GetColumn(columnOrXProperty);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreTableReadOnlyColumn ICoreDataRowReadOnlyAccessor.GetColumn(string columnOrXProperty)
        {
            return GetColumn(columnOrXProperty);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreTableReadOnlyColumn ICoreDataRowReadOnlyAccessor.TryGetColumn(string columnName)
        {
            return TryGetColumn(columnName);
        }

        [CanBeNull]
        public new DataColumnContainer TryGetColumn(string columnName)
        {
            return (DataColumnContainer)MetadataProps.TryGetColumn(columnName);
        }

        [NotNull]
        public new DataColumnContainer GetColumn(string columnOrXProperty)
        {
            return (DataColumnContainer)MetadataProps.GetColumn(columnOrXProperty);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTableReadOnlyColumn> IDataRowReadOnlyAccessor.GetColumns() => MetadataProps.Columns.OfType<IDataTableReadOnlyColumn>();

        T ICoreDataRowReadOnlyAccessor.Field<T>(ICoreTableReadOnlyColumn column)
        {
            return Field<T>(column.ColumnName);
        }

        T ICoreDataRowReadOnlyAccessor.Field<T>(ICoreTableReadOnlyColumn column, T defaultIfNull)
        {
            return Field<T>(column.ColumnName);
        }

        public override IReadOnlyList<T> FieldArray<T>(string columnOrXProperty, IReadOnlyList<T> defaultIfNull)
        {
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.FieldArray<T>(columnOrXProperty, defaultIfNull);
            }
            
            return base.FieldArray(columnOrXProperty, defaultIfNull);
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

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        protected string GetColumn(int reference)
        {
            return GetColumnCore(reference).ColumnName;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableReadOnlyColumn IDataRowReadOnlyAccessor.GetColumn(int reference)
        {
            return (IDataTableReadOnlyColumn)MetadataProps.GetColumn(reference);
        }
    
        public new T Field<T>(IDataTableReadOnlyColumn column, T defaultIfNull)
        {
            return GetFieldValue(column.ColumnName, defaultIfNull, DefaultValueType.Passed);
        }
        
        public IReadOnlyList<T> FieldArray<T>([NotNull] IDataTableReadOnlyColumn dataColumn, IReadOnlyList<T> defaultIfNull)
        {
            if (dataColumn == null)
            {
                throw new ArgumentNullException(nameof(dataColumn));
            }
            
            var columnName = dataColumn.ColumnName;

            return FieldArray(columnName, defaultIfNull);
        }

        public new bool IsNull(IDataTableReadOnlyColumn column)
        {
            return IsNull(column.ColumnName);
        }

        public new bool IsNotNull(IDataTableReadOnlyColumn column)
        {
            return IsNotNull(column.ColumnName);
        }

        public new object this[IDataTableReadOnlyColumn column]
        {
            get => this[column.ColumnName];
            set => this[column.ColumnName] = value;
        }

        public new T Field<T>(IDataTableReadOnlyColumn column)
        {
            return GetFieldValue(column.ColumnName, default(T), DefaultValueType.ColumnBased);
        }

        public new bool IsNull(ICoreTableReadOnlyColumn column)
        {
            var data = GetData(GetColumn(column.ColumnName));

            return data == null;
        }

        public DataRowContainer Set<T>(DataColumnContainer columnContainer, T value)
        {
             Set(columnContainer.ColumnHandle, value);
             
             return this;
        }
        
        public new DataRowContainer Set<T>(int columnHandle, T value)
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

        public new DataRowContainer SetNull(int columnHandle)
        {
            this[columnHandle] = null;

            return this;
        }

        IDataRowAccessor IDataRowAccessor.Set<T>(string columnName, T value)
        {
            this[columnName] = value;

            return this;
        }

        public IDataRowContainer Set<T>(IDataTableReadOnlyColumn column, T value)
        {
            return (IDataRowContainer)this.Set(column.ColumnName, value);
        }

        IDataRowContainer IDataRowContainer.SetNull(IDataTableReadOnlyColumn column)
        {
            return this.SetNull(column);
        }

        IDataRowAccessor IDataRowAccessor.Set<T>(IDataTableReadOnlyColumn column, T value)  
        {
            this[column.ColumnName] = value;
            
            return this;
        }

        IDataRowAccessor IDataRowAccessor.Set<T>(string columnName, T? value)
        {
            this[columnName] = value;
            
            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IDataRowAccessor Set<T>(IDataTableReadOnlyColumn column, T? value) where T : struct, IComparable, IComparable<T>
        {
            this[column] = value;

            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowAccessor IDataRowAccessor.SetNull(string columnName)
        {
            return SetNull(columnName);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowAccessor IDataRowAccessor.SetNull(IDataTableReadOnlyColumn column)
        {
            return SetNull(column);
        }

        private DataRowContainer SetCore<T>(ICoreTableReadOnlyColumn column, T value)
        {
            if (TypeConvertor.CanBeTreatedAsNull<T>(value))
            {
                SetNull(column);

                return this;
            }

            if (value == null)
            {
                SetNull(column);
            }
            else
            {
                this[column] = value;
            }

            return this;
        }

        public new IDataRowContainer SetNull(ICoreTableReadOnlyColumn column)
        {
            this[column] = null;

            return this;
        }

        [CanBeNull]
        public object GetOriginalValue([NotNull] IDataTableReadOnlyColumn column)
        {
            return GetOriginalValue(column.ColumnName);
        }

        public new T GetOriginalValue<T>(IDataTableReadOnlyColumn column)
        {
            return GetOriginalValue<T>(column.ColumnName);
        }

        object IDataRowReadOnlyAccessor.GetOriginalValue(IDataTableReadOnlyColumn column)
        {
            return GetOriginalValue(column);
        }

        void IDataRowAccessor.SilentlySetValue(IDataTableReadOnlyColumn columnName, object value)
        {
            SilentlySetValue(columnName, value);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowReadOnlyContainer IDataRowReadOnlyContainer.Clone()
        {
            return Clone();
        }

        private int m_lockCount;

        public bool IsLocked
        {
            get
            {
                if (m_lockCount > 0 || IsInitializing)
                {
                    return true;
                }

                if (m_lockCount <= 0)
                {
                    m_lockCount = 0;
                }
               
                return false;
            }
        }

        public IEnumerable<DataColumnContainer> Columns => MetadataProps.Columns.OfType<DataColumnContainer>();

        public int ColumnsCount => MetadataProps.ColumnMap.Count;
   
        public IDataLoadState BeginLoad()
        {
            LockEvents();

            m_initLockCount++;

            return this;
        }

        public IDataLockEventState LockEvents()
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot lock event for '{DebugKeyValue}' data row container'{TableName}' table because it is readonly.");
            }
            
            m_lockCount++;

            return this;
        }

        void IDataLockEventState.ResetAggregatedEvents()
        {
            ResetEvents();
        }

        void IDataLockEventState.UnlockEvents()
        {
            m_lockCount--;
            
            if (IsLocked == false)
            {
                Interlocked.Increment(ref ContainerDataProps.Age); 

                RaiseMultiColumnChanged();

                RaiseMultiXPropertyChanged();
            }
        }

        void IDataLoadState.EndLoad()
        {
            m_initLockCount--;

            if (IsInitializing == false)
            {
                ((IDataLockEventState)this).UnlockEvents();
            }
        }
        
        protected override void OnBeforeSet<T>(string column, int columHandle, ref T newValue, T oldValue, out bool cancel, ref Map<int, object> cascadePrevValues)
        {
            cancel = false;

            var columnContainer = (DataColumnContainer)MetadataProps.GetColumn(columHandle);
            
            if (columnContainer.IsReadOnly)
            {
                cancel = true;
                
                return;
            }

            if ((m_dataFieldChangingEvent?.HasAny() ?? false) || (m_dataFieldChangedEvent?.HasAny() ?? false))
            {
                foreach (var expressionColumnHandle in DataExpressions.GetDependantExpressionColumnHandles(columnContainer.ColumnHandle))
                {
                    var dependentColumn = (DataColumnContainer)MetadataProps.GetColumn(expressionColumnHandle);

                    var expressionOldValue = GetExpressionValue(dependentColumn);

                    if (cascadePrevValues == null)
                    {
                        cascadePrevValues = new();
                    }

                    cascadePrevValues[expressionColumnHandle] = expressionOldValue;
                }
            }

            var isNotLocked = IsLocked == false;
            
            if(isNotLocked)
            {
                try
                {
                    OnPropertyChanging(column);
                }
                catch (DataChangeCancelException)
                {
                    cancel = true;
                
                    return;
                }
            }
            
            if (isNotLocked && m_dataFieldChangingEvent != null && m_dataFieldChangingEvent.HasAny())
            {
                var changingArgs = new DataContainerFieldValueChangingArgs(this)
                {
                    ColumnName = column,
                    OldValue = oldValue,
                    NewValue = newValue
                };

                m_dataFieldChangingEvent.Raise(changingArgs);

                if (changingArgs.IsCancel)
                {
                    cancel = true;
                }
                else
                {
                    if (changingArgs.NewValue != null)
                    {
                        newValue = Brudixy.TypeConvertor.ConvertValue<T>(changingArgs.NewValue, column, TableName, columnContainer.Type, columnContainer.TypeModifier,"Row container before set");
                    }
                }

                if (cancel == false)
                {
                    OnDependantExpressionChanging(column, newValue, cascadePrevValues, columnContainer, ref cancel);
                }
            }
        }

        private void OnDependantExpressionChanging<T>(string column, T newValue, Map<int, object> cascadePrevValues, DataColumnContainer columnContainer, ref bool cancel)
        {
            foreach (var expressionColumnHandle in DataExpressions.GetDependantExpressionColumnHandles(columnContainer.ColumnHandle))
            {
                var exprColumnName = GetColumn(expressionColumnHandle);

                var expressionOldValue = cascadePrevValues[expressionColumnHandle];
                var expressionNewValue = GetExpressionValue(expressionColumnHandle, column, newValue);

                var expressionChangingArgs = new DataContainerFieldValueChangingArgs(this)
                {
                    ColumnName = exprColumnName,
                    OldValue = expressionOldValue,
                    NewValue = expressionNewValue
                };

                m_dataFieldChangingEvent.Raise(expressionChangingArgs);

                if (expressionChangingArgs.IsCancel)
                {
                    cancel = true;

                    return;
                }
            }
        }

        private object GetExpressionValue<T>(int expressionColumn, string column, T newValue)
        {
            return this.DataExpressions.GetExpressionValue(expressionColumn, column, newValue);
        }

        protected override void OnAfterSet<T>(string columnName, int columnHandle, T prevValue, T value,
            Map<int, object> cascadePrevValues)
        {
            if (IsInitializing)
            {
                return;
            }

            ContainerDataProps.ChangedFields ??= (ContainerDataProps.ChangedFields = new (StringComparer.OrdinalIgnoreCase));

            ContainerDataProps.ChangedFields.Add(columnName);
            
            var hasAnyChangedSubscriber = m_dataFieldChangedEvent?.HasAny() ?? false;

            if (hasAnyChangedSubscriber)
            {
                if (IsLocked)
                {
                    AggregateColumnChangedEvent(columnName, prevValue, value);
                }
                else
                {
                    var columnChangedArgs = new DataContainerFieldValueChangedArgs(this, columnName, prevValue, value);

                    m_dataFieldChangedEvent.Raise(columnChangedArgs);

                    columnChangedArgs.Dispose();
                }

                if(cascadePrevValues != null)
                {
                    foreach (var cascadeKv in cascadePrevValues)
                    {
                        var cascadeColumn = GetColumn(cascadeKv.Key);

                        if (IsLocked)
                        {
                            AggregateColumnChangedEvent(cascadeColumn, cascadeKv.Value, this[cascadeKv.Key]);
                        }
                        else
                        {
                            var columnChangedArgs = new DataContainerFieldValueChangedArgs(this, cascadeColumn, cascadeKv.Value, this[cascadeKv.Key]);

                            m_dataFieldChangedEvent.Raise(columnChangedArgs);

                            columnChangedArgs.Dispose();
                        }
                    }
                }
            }
           

            if (IsLocked == false)
            {
                Interlocked.Increment(ref ContainerDataProps.Age); 

                OnPropertyChanged(columnName);
            }
        }

        protected override void OnBeforeXPropSet<T>(string propertyCode, T oldValue, ref T valueStr, out bool cancel)
        {
            cancel = false;

            var isNotLocked = IsLocked == false;

            if(isNotLocked)
            {
                try
                {
                    OnPropertyChanging(propertyCode);
                }
                catch (DataChangeCancelException)
                {
                    cancel = true;
                    return;
                }
            }
            
            if (isNotLocked && m_dataXPropertyChangingEvent != null && m_dataXPropertyChangingEvent.HasAny())
            {
                var changingArgs = new DataContainerXPropertyChangingArgs(this, propertyCode, oldValue, valueStr);

                m_dataXPropertyChangingEvent.Raise(changingArgs);

                cancel = changingArgs.IsCancel;

                valueStr = changingArgs.GetNewValue<T>();
            }
        }
        
        protected override void OnAfterXPropSet(string propertyCode, object prevValue, object newValue)
        {
            if (IsInitializing)
            {
                return;
            }

            if (m_dataXPropertyChangedEvent != null && m_dataXPropertyChangedEvent.HasAny())
            {
                if (IsLocked)
                {
                    AggregateXPropertyChangedEvent(propertyCode, prevValue, newValue);
                }
                else
                {
                    var columnChangedArgs = new DataContainerXPropertyChangedArgs(this, propertyCode, prevValue, newValue);

                    m_dataXPropertyChangedEvent.Raise(columnChangedArgs);
                }
            }

            if (IsLocked == false)
            {
                OnPropertyChanged(propertyCode);

                Interlocked.Increment(ref ContainerDataProps.Age); 
            }
        }

        private void AggregateXPropertyChangedEvent(string propertyCode, object prevValue, object value)
        {
            if (m_xPropertyChangedAggregated == null)
            {
                m_xPropertyChangedAggregated = new ();
            }

            if (m_xPropertyChangedAggregated.TryGetValue(propertyCode, out var change))
            {
                prevValue = change.OldValue;
            }

            m_xPropertyChangedAggregated[propertyCode] = new () { NewValue = value, OldValue = prevValue };
        }

        private void AggregateColumnChangedEvent<T>(string columnName, object prevValue, T value)
        {
            if (m_columnChangedAggregated == null)
            {
                m_columnChangedAggregated = new ();
            }

            if (m_columnChangedAggregated.TryGetValue(columnName, out var change))
            {
                prevValue = change.OldValue;
            }

            m_columnChangedAggregated[columnName] = new () { NewValue = value, OldValue = prevValue };
        }

        private void RaiseMultiColumnChanged()
        {
            if (m_columnChangedAggregated?.Count > 0 && m_dataFieldChangedEvent != null && m_dataFieldChangedEvent.HasAny())
            {
                var columnChanges = new Map<string, DataTable.ColumnChange>(m_columnChangedAggregated);

                m_columnChangedAggregated = null;

                var columnChangedArgs = new DataContainerFieldValueChangedArgs(this, columnChanges);

                m_dataFieldChangedEvent.Raise(columnChangedArgs);
            }
        }

        private void RaiseMultiXPropertyChanged()
        {
            if (m_xPropertyChangedAggregated?.Count > 0 && m_dataXPropertyChangedEvent != null && m_dataXPropertyChangedEvent.HasAny())
            {
                var propertyChanges = new Map<string, DataTable.XPropertyChange>(m_xPropertyChangedAggregated);

                m_xPropertyChangedAggregated = null;

                var xPropertyChangedArgs = new DataContainerXPropertyChangedArgs(this, propertyChanges);

                m_dataXPropertyChangedEvent.Raise(xPropertyChangedArgs);
                
                xPropertyChangedArgs.Dispose();
            }
        }

        protected void ResetEvents()
        {
            m_xPropertyChangedAggregated = null;
            m_columnChangedAggregated = null;
        }

        IDataRowContainer IDataRowContainer.Set<T>(string columnName, T value)
        {
            if (TypeConvertor.CanBeTreatedAsNull<T>(value))
            {
                SetNull(columnName);

                return this;
            }
            
            if (value == null)
            {
                SetNull(columnName);
            }
            else
            {
                this[columnName] = value;
            }

            return this;
        }

        public new IDataRowContainer SetNull(string columnName)
        {
            this[columnName] = null;

            return this;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        object IDataRowContainer.this[int handle]
        {
            get => this[handle];
            set => this[handle] = value;
        }
        
        public override void CopyAll(ICoreDataRowReadOnlyAccessor rowAccessor)
        {
            base.CopyAll(rowAccessor);
            
            if (rowAccessor is IDataRowAccessor ra)
            {
                CopyDataMetaData(ra);
            }
        }

        public override void CopyFrom(ICoreDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null)
        {
            base.CopyFrom(rowAccessor, skipFields);

            if (rowAccessor is IDataRowAccessor ra)
            {
                CopyDataMetaData(ra);
            }
        }

        private void CopyDataMetaData(IDataRowAccessor ra)
        {
            var fault = ra.GetRowFault();

            if (GetRowFault() != fault)
            {
                SetRowFault(fault);
            }
                
            var error = ra.GetRowError();

            if (GetRowError() != error)
            {
                SetRowError(error);
            }

            var warning = ra.GetRowWarning();

            if (GetRowWarning() != warning)
            {
                SetRowWarning(warning);
            }

            var info = ra.GetRowInfo();

            if (GetRowInfo() != info)
            {
                SetRowInfo(info);
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        object ICloneable.Clone()
        {
            return Clone();
        }

        public new DataRowContainer Clone()
        {
            return (DataRowContainer)base.Clone();
        }

        protected override CoreDataRowContainer CloneCore()
        {
            var newObj = (DataRowContainer)MemberwiseClone();
            
            newObj.Init(MetadataProps, ContainerDataProps.Clone());

            if (m_currentEditRow != null)
            {
                newObj.CopyChanges(m_currentEditRow);
            }

            return newObj;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowContainer IDataRowContainer.Clone()
        {
            return Clone();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public IDataRowReadOnlyContainer ToReadOnly()
        {
            return Clone();
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowReadOnlyContainer IDataRowContainer.ToReadOnly()
        {
            return ToReadOnly();
        }

        void ICoreDataRowAccessor.CopyFrom(ICoreDataRowAccessor rowAccessor, IReadOnlyCollection<string> skipFields)
        {
            CopyFrom(rowAccessor, skipFields);
        }

        void ICoreDataRowAccessor.CopyChanges(ICoreDataRowAccessor rowAccessor, IReadOnlyCollection<string> skipFields)
        {
            CopyChanges(rowAccessor, skipFields);
        }

        protected override void DisposeCore()
        {
            base.DisposeCore();
            
            m_expressionCache?.Dispose();
            m_dataFieldChangedEvent?.Dispose();
            m_dataFieldChangingEvent?.Dispose();
            m_dataXPropertyChangedEvent?.Dispose();
            m_dataXPropertyChangingEvent?.Dispose();
            m_metaDataChangedEvent?.Dispose();
            m_eventsReferenceHolder.Dispose();
            m_currentEditRow?.Dispose();

            m_currentEditRow = null;
        }
        
        ~DataRowContainer()
        {
            DisposeCore();
        }

        protected class DataExpressionCache : IDisposable
        {
            private ulong? m_lastRowAge;

            private Map<int, object> m_expressionValue;

            private readonly DataRowContainer m_rowAccessor;

            private Map<int, DataExpression> m_columExpressions;
            
            private Map<int, Set<int>> m_expressionDependOnColumn;

            public DataExpressionCache(DataRowContainer rowAccessor)
            {
                m_rowAccessor = rowAccessor;
            }

            public void Dispose()
            {
                m_lastRowAge = null;

                if (m_expressionValue != null)
                {
                    m_expressionValue.Dispose();

                    m_expressionValue = null;
                }

                m_columExpressions?.Dispose();
                
                m_columExpressions = null;

                if (m_expressionDependOnColumn != null)
                {
                    var keyValuePairs = m_expressionDependOnColumn.ToArray();

                    foreach (var keyValuePair in keyValuePairs)
                    {
                        keyValuePair.Value?.Dispose();
                    }
                    
                    m_expressionDependOnColumn.Dispose();

                    m_expressionDependOnColumn = null;
                }
            }

            public object GetExpressionValue(int columnHandle)
            {
                var column = (DataColumnContainer)m_rowAccessor.GetColumnCore(columnHandle);
                var expression = column.Expression;

                if (expression != null)
                {
                    return GetExpressionValue(columnHandle, expression);
                }

                return m_rowAccessor[columnHandle];
            }

            private object GetExpressionValue(int columnHandle,  string expression)
            {
                m_expressionValue ??= new ();

                var rowAge = m_rowAccessor.GetRowAge();

                bool evaluate = true;

                var savedRowAge = m_lastRowAge;

                if (savedRowAge == rowAge)
                {
                    evaluate = false;
                }

                if (evaluate || m_expressionValue.ContainsKey(columnHandle) == false)
                {
                    return EvaluateNew(expression, columnHandle);
                }

                return m_expressionValue[columnHandle];
            }

            private object EvaluateNew(string expression, int columnHandle)
            {
                var dataExpression = GetExpression(expression, columnHandle);

                var value = dataExpression.Evaluate(m_rowAccessor.RowHandle);

                m_expressionValue[columnHandle] = value;

                m_lastRowAge = m_rowAccessor.GetRowAge();

                return value;
            }

            private DataExpression GetExpression(string expression, int columnHandle)
            {
                if (m_columExpressions == null)
                {
                    m_columExpressions = new();
                }
                
                if (m_columExpressions.TryGetValue(columnHandle, out var dataExpression) == false)
                {
                    m_columExpressions[columnHandle] = dataExpression = new DataExpression(new DataRowExpressionSource(this.m_rowAccessor), expression);
                }

                return dataExpression;
            }

            public IEnumerable<int> GetDependantExpressionColumnHandles(int column)
            {
                if (m_expressionDependOnColumn == null)
                {
                    var expressionDependOnColumn = BuildColumnInExpressionIndex();

                    m_expressionDependOnColumn = expressionDependOnColumn;
                }

                if (m_expressionDependOnColumn?.TryGetValue(column, out var dependantExpr) ?? false)
                {
                    return dependantExpr;
                }

                return Array.Empty<int>();
            }

            private Map<int, Set<int>> BuildColumnInExpressionIndex()
            {
                Map<int, Set<int>> expressionDependOnColumn = null;

                foreach (DataColumnContainer columnContainer in m_rowAccessor.MetadataProps.Columns)
                {
                    var expression = columnContainer.Expression;

                    if (string.IsNullOrEmpty(expression))
                    {
                        continue;
                    }

                    var dataExpression = GetExpression(expression, columnContainer.ColumnHandle);

                    var dependency = dataExpression.GetDependency();

                    foreach (var dependCol in dependency)
                    {
                        var dependant = m_rowAccessor.GetColumn(dependCol);

                        if (expressionDependOnColumn == null)
                        {
                            expressionDependOnColumn = new();
                        }
                        
                        expressionDependOnColumn
                            .GetOrAdd(dependant.ColumnHandle, () => new Set<int>())
                            .Add(columnContainer.ColumnHandle);
                    }
                }

                return expressionDependOnColumn;
            }

            public object GetExpressionValue(int expressionColumn, string column, object newValue)
            {
                var columnContainer = (DataColumnContainer)m_rowAccessor.GetColumnCore(expressionColumn);
                
                var expression = columnContainer.Expression;

                if (string.IsNullOrEmpty(expression))
                {
                    return m_rowAccessor[expressionColumn];
                }
                
                var dataExpression = GetExpression(expression, expressionColumn);

                return dataExpression.Evaluate(m_rowAccessor.RowHandle, _.MapObj((column, newValue)));
            }
        }

        public int CompareTo(IDataRowContainer other)
        {
            var compareDataRows = CompareDataRows(this, other);
            
            return compareDataRows.cmp;
        }
        
        public int CompareTo(IDataRowReadOnlyContainer other)
        {
            var compareDataRows = CompareDataRows(this, other);
            
            return compareDataRows.cmp;
        }
        
        public int CompareTo(DataRowContainer other)
        {
            var compareDataRows = CompareDataRows(this, other);
            
            return compareDataRows.cmp;
        }
        
        public int CompareTo(object obj)
        {
            if (obj is DataRowContainer dc)
            {
                return CompareTo(dc);
            }
            if (obj is IDataRowReadOnlyContainer drc)
            {
                return CompareTo(drc);
            }

            return -1;
        }

        public override bool Equals(object obj)
        {
            if (obj is IDataRowContainer other1)
            {
                return Equals(other1);
            }
            
            if (obj is IDataRowReadOnlyContainer other2)
            {
                return Equals(other2);
            }
            
            return ReferenceEquals(this, obj);
        }

        public bool Equals(DataRowContainer other)
        {
            var tuple = EqualsDataRowContainers(this, other);
            
            return tuple.value;
        }
        
        public (bool value, string name, string type) EqualsCore(DataRowContainer other)
        {
            return EqualsDataRowContainers(this, other);
        }
        
        public bool Equals(IDataRowReadOnlyContainer other)
        {
            var tuple = EqualsDataRowContainers(this, other);
            
            return tuple.value;
        }
        
        public (bool value, string name, string type) EqualsCore(IDataRowReadOnlyContainer other)
        {
            return EqualsDataRowContainers(this, other);
        }

        public bool Equals(IDataRowContainer other)
        {
            var tuple = EqualsDataRowContainers(this, other);
            
            return tuple.value;
        }
        
        public (bool value, string name, string type) EqualsCore(IDataRowContainer other)
        {
            return EqualsDataRowContainers(this, other);
        }

        public override int GetHashCode()
        {
            return ContainerHashCode(this);
        }

        protected override CoreRowContainerSerializer<T, V> CreateRowContainerSerializer<T, V>(SerializerAdapter<T, V> serializer)
        {
            return new RowContainerSerializer<T, V>(this, serializer);
        }
    }
}