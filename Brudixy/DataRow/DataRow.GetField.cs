using System.Reflection.Metadata.Ecma335;
using Brudixy.Exceptions;
using Brudixy.Interfaces;

namespace Brudixy
{
    public partial class DataRow
    {
        public override bool HasXProperty(string xPropertyName)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.HasXProperty(xPropertyName);
                }

                return (table.StateInfo.RowXProps.Storage.ElementAtOrDefault(RowHandleCore)?.ContainsKey(xPropertyName) ?? false) || 
                       (table.m_rowXPropertyAnnotations?.Storage.ElementAtOrDefault(RowHandleCore)?.ContainsKey(xPropertyName) ?? false);
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.HasXProperty(xPropertyName);
            }

            return false;
        }
        
        protected override object GetFieldValue(CoreDataTable dataTable, CoreDataColumn column, DefaultValueType defaultValueType, object defaultIfNull)
        {
            var dt = (DataTable)dataTable;
            
            if (dt == null)
            {
                if (m_detachedStorage != null && column.ColumnHandle < m_detachedStorage.ColumnsCount)
                {
                    var columnContainer = m_detachedStorage.GetColumn(column.ColumnHandle);

                    return m_detachedStorage.GetFieldValue(columnContainer.ColumnName, defaultIfNull, defaultValueType);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            var dataColumn = (DataColumn)column;

            if (m_currentEditRow != null)
            {
                if (defaultValueType == DefaultValueType.Passed)
                {
                    return m_currentEditRow.Field<object>(dataColumn, defaultValueType);
                }
                
                return m_currentEditRow[column];
            }
            
            var thisColumn = GetThisColumn(dataColumn);

            if (thisColumn.FixType == DataColumnType.Expression)
            {
                var expressionValue = dt.ExpressionValuesCache?.GetExpressionValue(thisColumn.ColumnHandle, RowHandleCore);

                if (expressionValue == null)
                {
                    return defaultValueType == DefaultValueType.ColumnBased
                        ? dt.GetDefaultNullValue<object>(thisColumn)
                        : defaultIfNull;
                }

                return expressionValue;
            }
            
            return base.GetFieldValue(dt, thisColumn, defaultValueType, defaultIfNull);
        }
        
        public override object this[ColumnHandle column]
        {
            get
            {
                var dt = (DataTable)table;

                if (dt == null)
                {
                    if (m_detachedStorage != null && column.Handle < m_detachedStorage.ColumnsCount)
                    {
                        var columnContainer = (DataColumnContainer)m_detachedStorage.GetColumn(column.Handle);
                    
                        return m_detachedStorage[columnContainer];
                    }
                
                    throw new DataDetachedException($"{DebugKeyValue}");
                }

                var dataColumn = dt.GetColumn(column.Handle);
                
                return GetFieldValue(table, dataColumn, DefaultValueType.ColumnBased, null);
            }
            set
            {
                if (table == null)
                {
                    throw new DataDetachedException($"{DebugKeyValue}");
                } 
                
                SetFieldCore(table.GetColumn(column.Handle), value);
            }
        }
        
        public override T Field<T>(ColumnHandle column)
        {
            var dt = (DataTable)table;

            if (dt == null)
            {
                if (m_detachedStorage != null && column.Handle < m_detachedStorage.ColumnsCount)
                {
                    var columnContainer = (DataColumnContainer)m_detachedStorage.GetColumn(column.Handle);
                    
                    return m_detachedStorage.Field<T>(columnContainer);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            return GetFieldValue(table, table.GetColumn(column.Handle), DefaultValueType.ColumnBased, default(T));
        }

        public override T Field<T>(ColumnHandle column, T defaultIfNull)
        {
            var dt = (DataTable)table;

            if (dt == null)
            {
                if (m_detachedStorage != null && column.Handle < m_detachedStorage.ColumnsCount)
                {
                    var columnContainer = (DataColumnContainer)m_detachedStorage.GetColumn(column.Handle);
                    
                    return m_detachedStorage.Field<T>(columnContainer);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            var dataColumn = dt.GetColumn(column.Handle);

            return GetFieldValue(dt, dataColumn, DefaultValueType.Passed, defaultIfNull);
        }

        protected override T GetFieldValue<T>(CoreDataTable dataTable, CoreDataColumn column, DefaultValueType defaultValueType, T defaultIfNull)
        {
            var dt = (DataTable)dataTable;

            if (dt == null)
            {
                if (m_detachedStorage != null && column.ColumnHandle < m_detachedStorage.ColumnsCount)
                {
                    return m_detachedStorage.GetFieldValue(column.ColumnName, defaultIfNull, defaultValueType);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            DataColumn dataColumn = (DataColumn)column;
            
            if (m_currentEditRow != null)
            {
                if (defaultValueType == DefaultValueType.Passed)
                {
                    if (m_currentEditRow.IsNull(dataColumn))
                    {
                        return defaultIfNull;
                    }
                }
                
                return m_currentEditRow.Field<T>(dataColumn);
            }
            
            var thisColumn = GetThisColumn(dataColumn);

            if (thisColumn.FixType == DataColumnType.Expression)
            {
                var expressionValue = dt.ExpressionValuesCache?.GetExpressionValue(thisColumn.ColumnHandle, RowHandleCore);

                if(expressionValue is T tv)
                {
                    return tv;
                }
                else
                {
                    return TryConvertValue<T>(dt, thisColumn, expressionValue, "field value expression getter");
                }
            }
            
            return base.GetFieldValue(dt, thisColumn, defaultValueType, defaultIfNull);
        }
        
        protected override IReadOnlyList<T> GetFieldArrayValue<T>(CoreDataTable dataTable, CoreDataColumn column,
            DefaultValueType defaultValueType, IReadOnlyList<T> defaultIfNull)
        {
            var dt = (DataTable)dataTable;
            
            if (dt == null)
            {
                if (m_detachedStorage != null && column.ColumnHandle < m_detachedStorage.ColumnsCount)
                {
                    var columnContainer = (DataColumnContainer)m_detachedStorage.GetColumn(column.ColumnHandle);

                    return m_detachedStorage.FieldArray<T>(columnContainer, defaultIfNull);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            var dataColumn = (DataColumn)column;

            if (m_currentEditRow != null)
            {
                return m_currentEditRow.FieldArray<T>(dataColumn, defaultIfNull);
            }
            
            var thisColumn = GetThisColumn(dataColumn);

            if (thisColumn.FixType == DataColumnType.Expression)
            {
                var expressionValue = dt.ExpressionValuesCache?.GetExpressionValue(thisColumn.ColumnHandle, RowHandleCore);

                if(expressionValue is IReadOnlyList<T> tv)
                {
                    return tv;
                }
                else
                {
                    throw dt.GetInvalidArrayCastException<T>(this.RowHandleCore, thisColumn);
                }
            }
            
            return base.GetFieldArrayValue(dt, thisColumn, defaultValueType, defaultIfNull);
        }
        
        public override T FieldNotNull<T>(ColumnHandle columnHandle)
        {
            var dt = this.table;
            
            if (dt == null)
            {
                var dataRowContainer = m_detachedStorage;
                
                if (dataRowContainer != null)
                {
                    var column = dataRowContainer.GetColumn(columnHandle.Handle);

                    var defVal = column.DefaultValue;

                    if (defVal == null)
                    {
                        return TypeConvertor.ReturnDefault<T>();
                    }

                    return TypeConvertor.ConvertValue<T>(defVal, column.ColumnName, dataRowContainer.TableName, column.Type, column.TypeModifier,"FieldNotNull");
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            return GetFieldValueNotNull(table, table.GetColumn(columnHandle.Handle), DefaultValueType.ColumnBased, default(T));
        }

        public override T FieldNotNull<T>(string columnOrXProp)
        {
            var dt = this.table;
            
            if (dt == null)
            {
                var dataRowContainer = m_detachedStorage;
                
                if (dataRowContainer != null)
                {
                    return GetColumnOrXPropNotNullValue<T>(columnOrXProp, dataRowContainer);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (m_currentEditRow != null)
            {
                return GetColumnOrXPropNotNullValue<T>(columnOrXProp, m_currentEditRow);
            }

            var dataColumn = dt.TryGetColumn(columnOrXProp);

            if (dataColumn != null)
            {
                return GetFieldValueNotNull<T>(dt, dataColumn, DefaultValueType.ColumnBased, default);
            }

            var hasXProperty = HasXProperty(columnOrXProp);

            if (hasXProperty)
            {
                var val = GetXProperty<T>(columnOrXProp);

                if (val == null)
                {
                    return TypeConvertor.ReturnDefault<T>();
                }

                return val;
            }
            
            throw new MissingMetadataException($"Table {m_tableName} does not have '{columnOrXProp}' column.");
        }

        private static T GetColumnOrXPropNotNullValue<T>(string columnOrXProp, DataRowContainer dataRowContainer)
        {
            var val = dataRowContainer.Field<T>(columnOrXProp);

            if (val is null)
            {
                var column = dataRowContainer.TryGetColumn(columnOrXProp);

                if (column != null)
                {
                    var defVal = column.DefaultValue;

                    if (defVal == null)
                    {
                        return TypeConvertor.ReturnDefault<T>();
                    }

                    return TypeConvertor.ConvertValue<T>(defVal, column.ColumnName, dataRowContainer.TableName, column.Type, column.TypeModifier, "FieldNotNull");
                }

                if (dataRowContainer.HasXProperty(columnOrXProp))
                {
                    var propValue = dataRowContainer.GetXProperty<T>(columnOrXProp);

                    if (propValue != null)
                    {
                        return propValue;
                    }
                }
                    
                return TypeConvertor.ReturnDefault<T>();
            }
                    
            return val;
        }

        protected override T GetFieldValueNotNull<T>(CoreDataTable dataTable, CoreDataColumn column, DefaultValueType defaultValueType, T defaultIfNull)
        {
            var dt = (DataTable)dataTable;
            
            if (dt == null)
            {
                if (m_detachedStorage != null && column.ColumnHandle < m_detachedStorage.ColumnsCount)
                {
                    var columnContainer = (DataColumnContainer)m_detachedStorage.GetColumn(column.ColumnHandle);

                    if (m_detachedStorage.IsNull(columnContainer))
                    {
                        return defaultIfNull;
                    }
                    
                    return m_detachedStorage.Field<T>(columnContainer);
                }
                
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            var dataColumn = (DataColumn)column;
            
            if (m_currentEditRow != null)
            {
                if (m_currentEditRow.IsNull(column))
                {
                    return defaultIfNull;
                }
                    
                return m_currentEditRow.Field<T>(dataColumn);
            }
            
            var thisColumn = GetThisColumn(dataColumn);
            
            if (thisColumn.FixType == DataColumnType.Expression)
            {
                var expressionValue = dt.ExpressionValuesCache?.GetExpressionValue(thisColumn.ColumnHandle, RowHandleCore);

                if (expressionValue == null)
                {
                    return defaultValueType == DefaultValueType.ColumnBased
                        ? dt.GetDefaultNullValue<T>(thisColumn)
                        : defaultIfNull;
                }

                if(expressionValue is T tve)
                {
                    return tve;
                }
                else
                {
                    return TryConvertValue<T>(dt, thisColumn, expressionValue, "field value expression getter");
                }
            }
            
            return base.GetFieldValueNotNull(dt, thisColumn, defaultValueType, defaultIfNull);
        }
    }
}