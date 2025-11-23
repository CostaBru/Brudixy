using System.Text.Json.Nodes;
using System.Xml.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class DataRow
    {
        public override bool IsNull(string columnOrXProp)
        {
            if (table is null)
            {
                if (m_detachedStorage != null)
                {
                    return m_detachedStorage.IsNull(columnOrXProp);
                }
            }
            else
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.IsNull(columnOrXProp);
                }
                
                if (RowRecordState != RowState.Detached && table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column))
                {
                    var dataColumn = (DataColumn)column;

                    if (dataColumn.FixType == DataColumnType.Expression)
                    {
                        var expressionValue = table.ExpressionValuesCache?.GetExpressionValue(dataColumn.ColumnHandle, RowHandleCore);

                        if (expressionValue == null)
                        {
                            expressionValue = table.GetDefaultNullValue<object>(dataColumn);
                        }

                        return expressionValue is null or "";
                    }

                    return base.IsNull(dataColumn);
                }
            }

            return base.IsNull(columnOrXProp);
        }

        protected override T GetFieldCore<T>(string columnOrXProp, 
            DefaultValueType defaultValueType,
            T defaultIfNull)
        {
            if (table is null)
            {
                if (m_detachedStorage != null)
                {
                    return m_detachedStorage.GetFieldValue<T>(columnOrXProp, defaultIfNull, defaultValueType);
                }
            }
            else
            {
                if (m_currentEditRow != null)
                {
                    if (defaultValueType == DefaultValueType.Passed)
                    {
                        if (m_currentEditRow.IsNull(columnOrXProp))
                        {
                            return defaultIfNull;
                        }
                    }
                    
                    return m_currentEditRow.Field<T>(columnOrXProp);
                }
                
                if (RowRecordState != RowState.Detached && table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column))
                {
                    var dataColumn = (DataColumn)column;
                    
                    if (dataColumn.FixType == DataColumnType.Expression)
                    {
                        var expressionValue =
                            table.ExpressionValuesCache?.GetExpressionValue(dataColumn.ColumnHandle, RowHandleCore);

                        if (expressionValue is null)
                        {
                            if (defaultValueType == DefaultValueType.ColumnBased)
                            {
                                return table.GetDefaultNullValue<T>(dataColumn);
                            }

                            return defaultIfNull;
                        }
                        else
                        {
                            if (expressionValue is T tv)
                            {
                                return tv;
                            }
                            else
                            {
                                return TryConvertValue<T>(table, dataColumn, expressionValue,
                                    "field value expression getter");
                            }
                        }
                    }
                    
                    return base.GetFieldValue(table, dataColumn, defaultValueType, defaultIfNull);
                }
            }

            return base.GetFieldCore(columnOrXProp, defaultValueType, defaultIfNull);
        }

        protected override object GetFieldCore(string columnOrXProp,
            DefaultValueType defaultValueType, 
            object defaultIfNull)
        {
            if (table is null)
            {
                if (m_detachedStorage != null)
                {
                    return m_detachedStorage.GetFieldValue(columnOrXProp, defaultIfNull, defaultValueType);
                }
            }
            else
            {
                if (m_currentEditRow != null)
                {
                    if (defaultValueType == DefaultValueType.Passed)
                    {
                        return m_currentEditRow.Field<object>(columnOrXProp, defaultValueType);
                    }
                
                    return m_currentEditRow[columnOrXProp];
                }

                if (RowRecordState != RowState.Detached && table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column))
                {
                    var dataColumn = (DataColumn)column;
                    
                    if (dataColumn.FixType == DataColumnType.Expression)
                    {
                        var expressionValue = table.ExpressionValuesCache?.GetExpressionValue(dataColumn.ColumnHandle, RowHandleCore);

                        if (expressionValue == null)
                        {
                            if (defaultValueType == DefaultValueType.ColumnBased)
                            {
                                return table.GetDefaultNullValue<object>(dataColumn);
                            }

                            return defaultIfNull;
                        }

                        return expressionValue;
                    }

                    return base.GetFieldValue(this.table, dataColumn, defaultValueType, defaultIfNull);
                }
            }

            return base.GetFieldCore(columnOrXProp, defaultValueType, defaultIfNull);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        object IDataRowReadOnlyAccessor.SilentlyGetValue([NotNull] string columnOrXProp) => SilentlyGetValue(columnOrXProp);

        [CanBeNull]
        public object SilentlyGetValue([NotNull] string columnOrXProp)
        {
            if (columnOrXProp == null)
            {
                throw new ArgumentNullException(nameof(columnOrXProp));
            }

            if (table == null || RowRecordState == RowState.Detached)
            {
                if (m_detachedStorage != null)
                {
                    return m_detachedStorage[columnOrXProp];
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (m_currentEditRow != null)
            {
                return m_currentEditRow[columnOrXProp];
            }
            
            if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column) == false)
            {
                if (HasXProperty(columnOrXProp))
                {
                    return GetXProperty<string>(columnOrXProp);
                }
                
                return null;
            }
            
            var dataColumn = (DataColumn)column;
            
            var dataItem = dataColumn.DataStorageLink;

            if (dataItem.IsNull(RowHandleCore, dataColumn))
            {
                return table.GetDefaultNullValue<object>(dataColumn);
            }

            return dataItem.GetData(RowHandleCore, column);
        }
        
        [CanBeNull]
        public override object GetOriginalValue(string columnOrXProp)
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                if (m_detachedStorage != null)
                {
                    return m_detachedStorage.GetOriginalValue(columnOrXProp);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetOriginalValue(columnOrXProp);
            }

            return base.GetOriginalValue(columnOrXProp);
        }

        [CanBeNull]
        public override T GetOriginalValue<T>(string columnOrXProp)
        {
            if (table == null || RowRecordState == RowState.Detached)
            {
                if (m_detachedStorage != null)
                {
                    return  m_detachedStorage.GetOriginalValue<T>(columnOrXProp);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetOriginalValue<T>(columnOrXProp);
            }

            return base.GetOriginalValue<T>(columnOrXProp);
        }

      
        [CanBeNull]
        public override T GetValueOrDefault<T>(string columnOrXProp, T defaultValue)
        {
            if (table is null)
            {
                if (m_detachedStorage is not null)
                {
                    if (m_detachedStorage.IsExistsField(columnOrXProp))
                    {
                        return m_detachedStorage.Field<T>(columnOrXProp);
                    }

                    if (m_detachedStorage.HasXProperty(columnOrXProp))
                    {
                        return m_detachedStorage.GetXProperty<T>(columnOrXProp);
                    }

                    return defaultValue;
                }
            }
            else
            {
                if (m_currentEditRow != null)
                {
                    if(m_currentEditRow.IsNull(columnOrXProp))
                    {
                        return defaultValue;
                    }
                    
                    return m_currentEditRow.Field<T>(columnOrXProp);
                }
                
                if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnOrXProp, out var column))
                {
                    var dataColumn = (DataColumn)column;
                    if (dataColumn.FixType == DataColumnType.Expression)
                    {
                        var expressionValue = table.ExpressionValuesCache?.GetExpressionValue(dataColumn.ColumnHandle, RowHandleCore);

                        if (expressionValue is null)
                        {
                            return defaultValue;
                        }
                        else
                        {
                            if (expressionValue is T tv)
                            {
                                return tv;
                            }
                            else
                            {
                                return TryConvertValue<T>(table, dataColumn, expressionValue, "field value expression getter");
                            }
                        }
                    }
                    
                    return base.GetFieldValue(table, dataColumn, DefaultValueType.Passed, defaultValue);
                }
            }

            return base.GetValueOrDefault(columnOrXProp, defaultValue);
        }

        public new DataRow SetField(string column, object value)
        {
            SetFieldValueCore(column, value);

            return this;
        }
        
        public string ToString(IDataTableReadOnlyColumn column, string format = null, IFormatProvider formatProvider = null)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.ToString(column, format, formatProvider);
                }
                
                if (column is CoreDataColumn dataColumn && ReferenceEquals(dataColumn.DataTable, table))
                {
                    return this.ToString(dataColumn, format, formatProvider);
                }

                return this.ToString(column.ColumnName, format, formatProvider);
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.ToString(column.ColumnName, format, formatProvider);
            }
            
            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public override string ToString(string columnOrXProperty, string format = null, IFormatProvider formatProvider = null)
        {
            if (table is null && m_detachedStorage != null)
            {
                return m_detachedStorage.ToString(columnOrXProperty, format, formatProvider);
            }
            
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.ToString(columnOrXProperty, format, formatProvider);
            }

            if (table != null && table.ColumnMapping.TryGetValue(columnOrXProperty, out var column))
            {
                var dataColumn = (DataColumn)column;
                if (dataColumn.FixType == DataColumnType.Expression)
                {
                    var fieldValue = this.GetFieldValue(table, dataColumn, DefaultValueType.ColumnBased, null);
                    
                    return CoreDataRowContainer.ValueToStringFormat(format, formatProvider, fieldValue, table.DisplayDateTimeUtcOffsetTicks);
                }

                return base.ToString(dataColumn, format, formatProvider);
            }

            return base.ToString(columnOrXProperty, format, formatProvider);
        }
        
        public override string ToString(CoreDataColumn column, string format = null, IFormatProvider formatProvider = null)
        {
            if (table is null && m_detachedStorage != null)
            {
                return m_detachedStorage.ToString(column, format, formatProvider);
            }
            
            if (table != null)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.ToString(column, format, formatProvider);
                }
                
                var dataColumn = (DataColumn)column;
                
                if (dataColumn.FixType == DataColumnType.Expression)
                {
                    return CoreDataRowContainer.ValueToStringFormat(format, formatProvider, this.GetFieldValue(table, column, DefaultValueType.ColumnBased, null), table.DisplayDateTimeUtcOffsetTicks);
                }
            }

            return base.ToString(column, format, formatProvider);
        }
        
        [NotNull]
        public new DataRow Set<T>(string column, T value)
        {
            return (DataRow)SetValueCore(column, value);
        }
        
        [NotNull]
        public new DataRow Set(string column, string value)
        {
            this.SetFieldValueCore(column, value);

            return this;
        }
        
        [NotNull]
        public new DataRow Set<T>(string column, T[] value)
        {
            this.SetFieldValueCore(column, value);

            return this;
        }

        [NotNull]
        public new DataRow Set(string column, XElement value)
        {
            this.SetFieldValueCore(column, value);

            return this;
        }
        
        [NotNull]
        public DataRow Set(string column, JsonObject value)
        {
            this.SetFieldValueCore(column, value);
            
            return this;
        }

        [NotNull]
        public DataRow Set(string column, byte[] value)
        {
            this.SetFieldValueCore(column, value);

            return this;
        }

        [NotNull]
        public DataRow Set(string column, char[] value)
        {
            this.SetFieldValueCore(column, value);

            return this;
        }

        [NotNull]
        public DataRow Set(string column, Uri value)
        {
            this.SetFieldValueCore(column, value);

            return this;
        }

        [NotNull]
        public DataRow Set(string column, Type value)
        {
            this.SetFieldValueCore(column, value);

            return this;
        }

        public override CoreDataRow SetDefault(string columnOrXProperty)
        {
            if (table != null && table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{columnOrXProperty}' column or x property of '{DebugKeyValue}' row from '{table.Name}' table the default value because it is readonly.");
            }
            
            if (m_currentEditRow != null)
            {
                if (m_currentEditRow.HasXProperty(columnOrXProperty))
                {
                    m_currentEditRow.SetXProperty<object>(columnOrXProperty, null);
                }
                else
                {
                    var column = GetColumn(columnOrXProperty);

                    m_currentEditRow.Set(column.ColumnHandle, column.DefaultValue);
                }

                return this;
            }
            
            base.SetDefault(columnOrXProperty);

            return this;
        }
        
        public override CoreDataRow SetDefault(ColumnHandle columnHandle)
        {
            if (table != null && table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{GetColumnCore(columnHandle.Handle)}' column '{DebugKeyValue}' row from '{table.Name}' table the default value because it is readonly.");
            }
            
            if (m_currentEditRow != null)
            {
                var column = GetColumnCore(columnHandle.Handle);

                m_currentEditRow.Set(column.ColumnHandle, column.DefaultValue);

                return this;
            }

            base.SetDefault(columnHandle);
            
            return this;
        }
    }
}