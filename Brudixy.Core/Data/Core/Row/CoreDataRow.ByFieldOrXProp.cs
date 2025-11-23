using System;
using System.Collections.Generic;
using Brudixy.Converter;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class CoreDataRow
    {
        public virtual bool IsNull(string columnOrXProp)
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                return false;
            }

            if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column))
            {
                return table.GetIsRowColumnNull(RowHandleCore, column);
            }

            if (HasXProperty(columnOrXProp))
            {
                var xProperty = GetXProperty<object>(columnOrXProp);
                
                return xProperty == null || (xProperty is string s && string.IsNullOrEmpty(s));
            }

            throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessageOnEdit(columnOrXProp));
        }

        public bool IsNotNull(string columnOrXProp)
        {
            return IsNull(columnOrXProp) == false;
        }
        
        protected virtual T GetFieldCore<T>(string columnOrXProp, 
            DefaultValueType defaultValueType,
            T defaultIfNull) 
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column))
                {
                    return table.GetRowFieldValue<T>(RowHandleCore, column, defaultValueType, defaultIfNull);
                }

                if (HasXProperty(columnOrXProp))
                {
                    var val = GetXProperty(columnOrXProp);
                    
                    return XPropertyValueConverter.TryConvert<T>("Row", columnOrXProp, val);
                }

                throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessage(columnOrXProp));
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }
        
        protected virtual IReadOnlyList<T> GetFieldArrayCore<T>(string columnOrXProp, 
            DefaultValueType defaultValueType,
            IReadOnlyList<T> defaultIfNull) 
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column))
                {
                    return GetFieldArrayValue(table, column, defaultValueType, defaultIfNull);
                }

                if (HasXProperty(columnOrXProp))
                {
                    var val = GetXProperty(columnOrXProp);

                    if (val is null)
                    {
                        return Array.Empty<T>();
                    }

                    if (val is IReadOnlyList<T> rt)
                    {
                        return rt;
                    }
                    
                    throw new InvalidCastException(
                        $"The '{columnOrXProp}' XProperty of the row '{DebugKeyValue}' of the '{table.TableName}' table cannot be casted to readonly list.");
                }

                throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessage(columnOrXProp));
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        protected virtual object GetFieldCore(string columnOrXProp,
            DefaultValueType defaultValueType, 
            object defaultIfNull)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var columnHandle))
                {
                    return table.GetRowFieldValue(RowHandleCore, columnHandle, defaultValueType, defaultIfNull);
                }

                if (HasXProperty(columnOrXProp))
                {
                    return GetXProperty<object>(columnOrXProp);
                }

                throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessage(columnOrXProp));
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [CanBeNull]
        public virtual object GetOriginalValue(string columnOrXProp)
        {
            if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column))
            {
                return table.GetOriginalData(RowHandle, column);
            }

            if (HasXProperty(columnOrXProp))
            {
                return GetXProperty<object>(columnOrXProp, original: true);
            }

            throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessage(columnOrXProp));
        }

        [CanBeNull]
        public virtual T GetOriginalValue<T>(string columnOrXProp) 
        {
            var rowState = RowRecordState;

            if (table == null || rowState == RowState.Detached)
            {
                return TypeConvertor.ReturnDefault<T>();
            }

            if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column))
            {
                return table.GetOriginalData<T>(RowHandleCore, column);
            }

            if (HasXProperty(columnOrXProp))
            {
                var originalValue = GetXProperty(columnOrXProp, original: true);
                
                return  XPropertyValueConverter.TryConvert<T>("Row original", columnOrXProp, originalValue);
            }

            throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessage(columnOrXProp));
        }

        [NotNull]
        public virtual T FieldNotNull<T>(string columnOrXProp)
        {
            if (table != null)
            {
                var column = table.TryGetColumn(columnOrXProp);

                if (column != null)
                {
                    return GetFieldValueNotNull<T>(table, column, DefaultValueType.ColumnBased, default);
                }

                if (HasXProperty(columnOrXProp))
                {
                    var property = GetXProperty<T>(columnOrXProp);

                    if (property == null)
                    {
                        return TypeConvertor.ReturnDefault<T>();
                    }
                    
                    return property;
                }
            }

            throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessage(columnOrXProp));
        }

        [CanBeNull]
        public virtual T GetValueOrDefault<T>(string columnOrXProp, T defaultValue)
        {
		    if (IsExistsField(columnOrXProp))
		    {
		        return Field<T>(columnOrXProp);
		    }

            if (HasXProperty(columnOrXProp))
            {
                var val = GetXProperty(columnOrXProp);
                
                return XPropertyValueConverter.TryConvert<T>("Row", columnOrXProp, val);
            }

            return defaultValue;
        }
        
        [CanBeNull]
        public object this[string columnOrXProp, DataRowVersion version]
        {
            get
            {
                if (version == DataRowVersion.Original)
                {
                    return GetOriginalValue(columnOrXProp);
                }

                return GetFieldCore(columnOrXProp, DefaultValueType.ColumnBased, null);
            }
        }
        
        [CanBeNull]
        public object this[string columnOrXProp]
        {
            get
            {
                return GetFieldCore(columnOrXProp, DefaultValueType.ColumnBased, null);
            }
            set
            {
                SetFieldValueCore(columnOrXProp, value);
            }
        }

        public CoreDataRow SetField(string columnOrXProp, object value)
        {
            SetFieldValueCore(columnOrXProp, value);

            return this;
        }
        
        [CanBeNull]
        public T Field<T>(string columnOrXProp)
        {
            return GetFieldCore(columnOrXProp, DefaultValueType.ColumnBased, default(T));
        }

        [CanBeNull]
        public IReadOnlyList<T> FieldArray<T>(string columnOrXProp)
        {
            return GetFieldArrayCore(columnOrXProp, DefaultValueType.ColumnBased, Array.Empty<T>());
        }

        [CanBeNull]
        public IReadOnlyList<T> FieldArray<T>(string columnOrXProp, IReadOnlyList<T> defaultIfNull)
        {
            return GetFieldArrayCore(columnOrXProp, DefaultValueType.Passed, defaultIfNull);
        }

        [CanBeNull]
        public T Field<T>(string columnOrXProp, T defaultIfNull)
        {
            return GetFieldCore(columnOrXProp, DefaultValueType.Passed, defaultIfNull);
        }
        
        public virtual string ToString(string columnOrXProp, string format = null, IFormatProvider formatProvider = null)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                object value;

                if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column))
                {
                    value = table.GetRowRawFieldValue(RowHandleCore, column, DefaultValueType.ColumnBased, null);
                }
                else if (HasXProperty(columnOrXProp))
                {
                    value = GetXProperty<object>(columnOrXProp);
                }
                else
                {
                    throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessage(columnOrXProp));
                }

                return CoreDataRowContainer.ValueToStringFormat(format, formatProvider, value, table.DisplayDateTimeUtcOffsetTicks);
            }
          
            throw new DataDetachedException($"{DebugKeyValue}");
        }


        public ICoreDataRowAccessor Set<T>(string columnOrXProp, T value)
        {
            return SetFieldValueCore(columnOrXProp, value);
        }

        [NotNull]
        public CoreDataRow Set(string columnOrXProp, string value)
        {
            this[columnOrXProp] = value;

            return this;
        }

        protected virtual CoreDataRow SetValueCore<T>(string columnOrXProp, T value) 
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (table.ColumnMapping.TryGetValue(columnOrXProp, out var column))
            {
                return SetCore(column, value);
            }
            
            if (HasXProperty(columnOrXProp))
            {
                SetXProperty(columnOrXProp, value);

                return this;
            }

            throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessageOnEdit(columnOrXProp));
        }

    
        
        public virtual CoreDataRow SetDefault(string columnOrXProp)
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{columnOrXProp}' column or x property of '{DebugKeyValue}' row from '{table.Name}' table the default value because it is readonly.");
            }
            
            if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column) == false)
            {
                if (HasXProperty(columnOrXProp))
                {
                    SetXProperty<object>(columnOrXProp, null);
                    
                    return this;
                }
                
                throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessageOnEdit(columnOrXProp));
            }
                
            return SetDefaultCore(column);
        }
        
        public virtual CoreDataRow SetDefault(ColumnHandle columnHandle)
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{table.DataColumnInfo.Columns[columnHandle.Handle].ColumnName}' column of '{DebugKeyValue}' row from '{table.Name}' table the default value because it is readonly.");
            }
            
            return SetDefaultCore(table.DataColumnInfo.Columns[columnHandle.Handle]);
        }

        private CoreDataRow SetDefaultCore(CoreDataColumn column)
        {
            if (table.IsReadOnlyColumn(column))
            {
                return this;
            }
         
            var defaultValue = column.DefaultValue;

            return SetFieldCore(column, defaultValue);
        }

        protected virtual CoreDataRow SetFieldValueCore(string columnOrXProp, object value)
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            if (table.ColumnMapping.TryGetValue(columnOrXProp, out var columnHandle))
            {
                return SetFieldCore(columnHandle, value);
            }
            
            if (HasXProperty(columnOrXProp))
            {
                SetXProperty(columnOrXProp, value);

                return this;
            }

            throw new MissingMetadataException(GetMissingColumnOrXPropErrorMessageOnEdit(columnOrXProp));
        }
    }
}