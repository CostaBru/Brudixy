using System.Collections.Generic;
using Brudixy.Converter;
using Brudixy.Exceptions;

namespace Brudixy
{
    public partial class CoreDataRow
    {
        protected virtual T GetFieldValue<T>(CoreDataTable table, 
            CoreDataColumn column, 
            DefaultValueType defaultValueType,
            T defaultIfNull)
        {
            if (table == null)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            var dataColumn = GetThisColumn(column);

            return table.GetRowFieldValue<T>(RowHandleCore, dataColumn, defaultValueType, defaultIfNull);
        }
        
        protected virtual IReadOnlyList<T> GetFieldArrayValue<T>(CoreDataTable table,
            CoreDataColumn column,
            DefaultValueType defaultValueType,
            IReadOnlyList<T> defaultIfNull)
        {
            if (table == null)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            var dataColumn = GetThisColumn(column);

            return table.GetRowFieldArrayValue<T>(RowHandleCore, dataColumn, defaultValueType, defaultIfNull);
        }


        protected virtual object GetFieldValue(CoreDataTable table, 
            CoreDataColumn column,
            DefaultValueType defaultValueType,
            object defaultIfNull)
        {
            if (table == null)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            var dataColumn = GetThisColumn(column);
            
            return table.GetRowFieldValue(RowHandleCore, dataColumn, defaultValueType, defaultIfNull);
        }

        protected virtual T GetFieldValueNotNull<T>(CoreDataTable table,
            CoreDataColumn column, 
            DefaultValueType defaultValueType,
            T defaultIfNull) 
        {
            if (table == null)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            var dataColumn = GetThisColumn(column);
            
            if (Tool.IsObject<T>() || Tool.IsArray<T>() || Tool.IsString<T>())
            {
                var fieldValue = GetFieldValue(table, dataColumn, defaultValueType, defaultIfNull);

                if (fieldValue == null)
                {
                    return TypeConvertor.ReturnDefault<T>();
                }
            }

            return table.GetRowFieldValue<T>(RowHandleCore, dataColumn, defaultValueType, defaultIfNull);
        }

        internal static T TryConvertValue<T>(CoreDataTable table, CoreDataColumn column, object dataValue, string source)
        {
            return TypeConvertor.ConvertValue<T>(dataValue, column.ColumnName, table.TableName, column.Type, column.TypeModifier, source);
        }
    }
}