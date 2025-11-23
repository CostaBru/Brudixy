using System;
using System.Collections.Generic;
using Brudixy.Interfaces;
using Brudixy.Exceptions;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class CoreDataRow
    {
        public virtual bool IsNull(CoreDataColumn column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var dataColumn = GetThisColumn(column);
                
                return dataColumn.DataStorageLink.IsNull(RowHandleCore, dataColumn);
            }
            
            return false;
        }

        public bool IsNotNull(CoreDataColumn column)
        {
            return IsNull(column) == false;
        }

        public CoreDataRow SilentlySetValue(CoreDataColumn column, object value)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot change '{DebugKeyValue}' row from '{table.Name}' table default because it is readonly.");
                }
                
                var dataColumn = GetThisColumn(column);
                
                table.SilentlySetRowValue(RowHandleCore, value, dataColumn);
            }

            return this;
        }

        public CoreDataRow SetNull(CoreDataColumn column)
        {
            this[column] = null;

            return this;
        }

        public bool IsChanged(CoreDataColumn column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (RowRecordState == RowState.Unchanged)
                {
                    return false;
                }
                
                var dataColumn = GetThisColumn(column);

                return dataColumn.DataStorageLink.IsCellChanged(RowHandle, dataColumn);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public virtual object GetOriginalValue(CoreDataColumn column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var dataColumn = GetThisColumn(column);

                var dataItem = dataColumn.DataStorageLink;

                if (RowRecordState == RowState.Added)
                {
                    return dataItem.GetDefaultValue(dataColumn);
                }
                
                return dataItem.GetOriginalValue(RowHandleCore, dataColumn);
            }
            
            throw new DataDetachedException($"{DebugKeyValue}");
        }
        
        [CanBeNull]
        public IReadOnlyList<T> FieldArray<T>(CoreDataColumn column)
        {
            return GetFieldArrayValue(table, column, DefaultValueType.ColumnBased, Array.Empty<T>());
        }

        public virtual T GetOriginalValue<T>(CoreDataColumn column)
        {
            if (ReferenceEquals(column.DataTable, this.table) == false)
            {
                return GetOriginalValue<T>(column.ColumnName);
            }
            
            var rowState = RowRecordState;
            
            if (table != null && rowState != RowState.Detached)
            {
                var dataColumn = GetThisColumn(column);

                return table.GetOriginalData<T>(RowHandleCore, dataColumn);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public T FieldNotNull<T>(CoreDataColumn column) 
        {
            return GetFieldValueNotNull(table, column, DefaultValueType.ColumnBased, default(T));
        }

        public virtual ulong GetColumnAge(CoreDataColumn column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (table.IsReadOnlyColumn(column))
                {
                    return table.DataAge;
                }

                return column.DataStorageLink.GetAge(RowHandleCore, column);
            }
          
            return 0;
        }

        public CoreDataRow Set(CoreDataColumn column, string value)
        {
            this[column] = value;

            return this;
        }
      
        public CoreDataRow Set<T>(CoreDataColumn column, T value)
        {
            if (table == null)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            return SetCore(column, value);
        }

        public CoreDataRow Set(CoreDataColumn column, object value)
        {
            this[column] = value;

            return this;
        }

        public T Field<T>(CoreDataColumn column, T defaultIfNull)
        {
            return GetFieldValue(column, defaultIfNull);
        }

        public T Field<T>(CoreDataColumn column)
        {
            return GetFieldValue(table, column, DefaultValueType.ColumnBased, default(T));
        }

        private T GetFieldValue<T>(CoreDataColumn column, T defaultIfNull)
        {
            return GetFieldValue(table, column, DefaultValueType.Passed, defaultIfNull);
        }

        public virtual object this[CoreDataColumn column]
        {
            get
            {
                if (table == null)
                {
                    throw new DataDetachedException($"{DebugKeyValue}");
                }
                
                var dataColumn = GetThisColumn(column);
                
                return GetFieldValue(table, dataColumn, DefaultValueType.ColumnBased, null);
            }
            set
            {
                SetFieldCore(column, value);
            }
        }

        private static object GetDefaultValueAsObject(CoreDataTable table, int columnHandle)
        {
            return table.GetDefaultNullValue<object>(columnHandle);
        }

        private static T GetDefaultValue<T>(CoreDataTable table, int columnHandle)
        {
            return table.GetDefaultNullValue<T>(columnHandle);
        }

        public object this[CoreDataColumn column, DataRowVersion version]
        {
            get
            {
                if (version == DataRowVersion.Original)
                {
                    return GetOriginalValue(column);
                }

                return GetFieldValue(table, column, DefaultValueType.ColumnBased, null);
            }
        }

        private CoreDataColumn GetThisColumn(CoreDataColumn column)
        {
            if (ReferenceEquals(column.DataTable, this.table))
            {
                return column;
            }

            return table.GetColumn(column.ColumnName);
        }
    }
}