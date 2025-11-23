using System;
using System.Collections.Concurrent;
using Brudixy.Interfaces;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        private static ConcurrentDictionary<(TableStorageType, TableStorageTypeModifier, bool, Type), Type> s_builtInCache = new();

        protected Type ComplexDataType = typeof(ComplexTypeDataItem<>);
        protected Type CommonDataType = typeof(DataItem<>);
        
        internal IDataItem CreateDataItem(CoreDataColumn column)
        {
            if (column.TypeModifier == TableStorageTypeModifier.Complex)
            {
                var dataType = column.DataType;

                if (dataType != null)
                {
                    Type repositoryType = ComplexDataType.MakeGenericType(dataType);

                    var instance = (IDataItem)Activator.CreateInstance(repositoryType);
                    
                    instance.Init(column.Type, column.TypeModifier, this);
                    
                    ConnectDataItem(instance, column);
                    
                    return instance;
                }
            }
            
            var valueType = GetDataTypeCore(column.Type, column.TypeModifier, column.AllowNull, column.DataType);

            if (valueType == null)
            {
                throw new ArgumentException(
                    $"Cannot create storage for '{column.ColumnName}' ({column.Type}/{column.TypeModifier}) of '{TableName}' table because DataType is not set.");
            }
            
            var itemType = CommonDataType.MakeGenericType(valueType);

            var dataItem = (IDataItem)Activator.CreateInstance(itemType);
            
            dataItem.Init(column.Type, column.TypeModifier, this);

            ConnectDataItem(dataItem, column);
         
            return dataItem;
        }

        protected virtual void ConnectDataItem(IDataItem dataItem, CoreDataColumn column)
        {
        }

        public static Type GetDataType(TableStorageType type, 
            TableStorageTypeModifier typeModifier, 
            bool columnAllowNull,
            Type columnDataType)
        {
            Type t;
            
            if (s_builtInCache.TryGetValue((type, typeModifier, columnAllowNull, columnDataType), out t))
            {
                return t;
            }

            s_builtInCache[(type, typeModifier, columnAllowNull, columnDataType)] = t = GetDataTypeCore(type, typeModifier, columnAllowNull, columnDataType);

            return t;
        }

        protected static Type GetDataTypeCore(TableStorageType type, 
            TableStorageTypeModifier typeModifier,
            bool columnAllowNull,
            Type dataType)
        {
            if (type == TableStorageType.UserType || typeModifier == TableStorageTypeModifier.Complex)
            {
                return dataType;
            }
            
            var columnType = TableStorageTypeMap.GetType(type);

            if (typeModifier == TableStorageTypeModifier.Array)
            {
                return columnType.MakeArrayType();
            }

            if (typeModifier == TableStorageTypeModifier.Range)
            {
                return typeof(Range<>).MakeGenericType(columnType);
            }
            
            if (columnAllowNull && columnType.IsValueType && columnType.IsEnum == false)
            {
                return typeof(Nullable<>).MakeGenericType(columnType);
            }

            return columnType;
        }
    }
}