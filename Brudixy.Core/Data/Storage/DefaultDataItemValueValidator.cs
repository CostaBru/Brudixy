using System;
using Brudixy.Exceptions;

namespace Brudixy.Storage
{
    public static class DefaultDataItemValueValidator
    {
        public static T StringValidator<T>(CoreDataTable table, ColumnHandle columnHandle, T val, int rowHandle, bool preValidating)
        {
            var dataColumn = table.DataColumnInfo.Columns[columnHandle.Handle];
            
            var maxLen = dataColumn.MaxLength;

            if (maxLen > 0)
            {
                var strVal = (string)(object)val;

                if (strVal != null && strVal.Length > maxLen)
                {
                    var columnName = dataColumn.ColumnName;
                    
                    var raiseError = table.MaxLenConstraintHandler(columnHandle, columnName, rowHandle, val, preValidating);

                    if (raiseError)
                    {
                        var dataRow = table.GetRowByHandle(rowHandle);
                        
                        throw new DataChangeCancelException(dataRow,
                            columnName,
                            $"The maximum string value length ({maxLen}) constraint error.");
                    }

                    return (T)(object)strVal.Substring(0, (int)maxLen);
                }
            }

            return val;
        }
        
        public static T DateTimeValidator<T>(CoreDataTable table, ColumnHandle columnHandle, T val, int rowHandle, bool preValidating)
        {
            if (val is null)
            {
                return val;
            }
            
            DateTime rez = default;
            GenericConverter.ConvertTo(ref val, ref rez);

            var validDateTime = DateTime.SpecifyKind(rez, table.StorageTimeKind);

            return (T)(object)validDateTime;
        }
        
        public static T ArrayValidator<T>(CoreDataTable table, ColumnHandle columnHandle, T val, int rowHandle, bool preValidating)
        {
            if (val is Array array)
            {
                var maxLen = table.DataColumnInfo.Columns[columnHandle.Handle].MaxLength;
                
                var storageType = table.DataColumnInfo.Columns[columnHandle.Handle].Type;

                if (maxLen > 0)
                {
                    if (array.Length > maxLen)
                    {
                        var columnName = table.DataColumnInfo.Columns[columnHandle.Handle].ColumnName;

                        var raiseError = table.MaxLenConstraintHandler(columnHandle, columnName, rowHandle, val, preValidating);

                        if (raiseError)
                        {
                            throw new DataChangeCancelException(table.GetRowByHandle(rowHandle),
                                columnName,
                                $"The maximum array value length ({maxLen}) constraint error.");
                        }

                        var length = maxLen.Value;


                        return CloneArray<T>(storageType, (int)length, array);
                    }
                }

                return CloneArray<T>(storageType, array.Length, array);
            }

            return default;
        }
        
        private static T CloneArray<T>(TableStorageType storageType, int length, Array array)
        {
            var type = CoreDataTable.GetDataType(storageType, TableStorageTypeModifier.Simple, false, typeof(T));

            var instance = Array.CreateInstance(type, length);

            Array.Copy(array, 0, instance, 0, instance.Length);

            return (T)(object)instance;
        }
    }
}