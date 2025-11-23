using System;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataRow
    {
        public virtual bool HasXProperty(string xPropertyName)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
               return table.StateInfo.RowXProps.Storage[RowHandleCore]?.ContainsKey(xPropertyName) ?? false;
            }

            return false;
        }
        
        public virtual bool CanChangeTo(string column, object value, out string reason)
        {
            reason = string.Empty;
            
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    reason = $"Cannot change '{column}' field of '{DebugKeyValue}' row of '{table.Name}' because it is readonly.";

                    return false;
                }

                var hasColumn = table.ColumnMapping.TryGetValue(column, out var dataColumn);
                var hasXProperty = table.StateInfo.RowXProps.Storage[RowHandleCore]?.ContainsKey(column) ?? false;

                if (hasColumn == false)
                {
                    if (hasXProperty == false)
                    {
                        reason = GetMissingColumnOrXPropErrorMessageOnEdit(column);

                        return false;
                    }

                    return true;
                }

                if (table.IsReadOnlyColumn(dataColumn))
                {
                    reason = $"'{column}' column of {GetTableName()} data table is readonly.";

                    return false;
                }

                var dataItem = dataColumn.DataStorageLink;

                var prevValue = dataItem.IsNull(RowHandleCore, dataColumn)
                    ? null
                    : dataItem.GetData(RowHandleCore, dataColumn);

                try
                {
                    dataItem.GetValidValue(ref value, RowHandleCore, dataColumn);
                    
                    table.CheckNewValue(dataColumn, prevValue, value, this.RowHandleCore);
                }
                catch (Exception e)
                {
                    reason = $"Row '{DebugKeyValue}' of {GetTableName()} data table cannot be changed. Reason: {e.Message}";
                    
                    return false;
                }

                if (value is not null)
                {
                    try
                    {
                        dataItem.CheckValueIsCompatibleType(value, dataColumn);
                    }
                    catch (Exception e)
                    {
                        reason = $"'{column}' column of {GetTableName()} data table has incompatible type. Reason: {e.Message}";

                        return false;
                    }
                }

                return true;
            }

            reason = $"Row '{DebugKeyValue}' of {GetTableName()} data table is detached.";

            return false;
        }

        private string GetMissingColumnOrXPropErrorMessageOnEdit(string column)
        {
            return $"Cannot change the '{DebugKeyValue}' row of '{table.Name}' because neither column or extended property with '{column}' name is exist.";
        }
        
        private string GetMissingColumnOrXPropErrorMessage(string column)
        {
            return $"Cannot get the '{DebugKeyValue}' row of '{table.Name}' because neither column or extended property with '{column}' name is exist.";
        }

        protected virtual CoreDataRow SetCore<T>(CoreDataColumn column, T value) 
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var dataItem = column.DataStorageLink;

                if (dataItem is ITypedDataItem<T> typedDataItem)
                {
                    return SetTypedNullableFieldValue(column, value, typedDataItem);
                }
                
                return SetFieldCore(column, value);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        protected virtual CoreDataRow SetFieldCore(CoreDataColumn column, object value)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var dataItem = column.DataStorageLink;
                
                dataItem.GetValidValue(ref value, RowHandleCore, column);

                var prevValue = dataItem.TryGetData(RowHandleCore, column);

                Map<int, object> cascadePrevValues = null;

                table.BeforeSetRowColumnBoxed(this.RowHandleCore, column, ref value, prevValue, out var canContinue, out var cancelExceptionMessage, ref cascadePrevValues);

                if (canContinue)
                {
                     table.SetRowColumnValue(RowHandleCore, column, value, prevValue, cascadePrevValues);
                }
                else if(string.IsNullOrEmpty(cancelExceptionMessage) == false)
                {
                    throw new DataChangeCancelException(this, column.ColumnName, cancelExceptionMessage);
                }
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            return this;
        }

        private CoreDataRow SetTypedNullableFieldValue<T>(CoreDataColumn column, T value, ITypedDataItem<T> dataItem) 
        {
            if (value is null)
            {
                dataItem.GetValidValue(ref value, RowHandleCore, column);
            }
            
            var prevValue = dataItem.GetDataTyped(RowHandleCore, column);
            
            Map<int, object> cascadePrevValues = null;

            table.BeforeSetRowColumn(this.RowHandleCore, column, ref value, ref prevValue, out var canContinue, out var cancelExceptionMessage, ref cascadePrevValues);

            if (canContinue)
            {
                table.SetRowColumnValue(this.RowHandleCore, dataItem, column, ref value, ref prevValue, cascadePrevValues);
            }
            else if (string.IsNullOrEmpty(cancelExceptionMessage) == false)
            {
                throw new DataChangeCancelException(this, column.ColumnName, cancelExceptionMessage);
            }

            return this;
        }
    }
}