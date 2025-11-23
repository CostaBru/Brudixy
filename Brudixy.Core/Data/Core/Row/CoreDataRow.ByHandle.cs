using System;
using Brudixy.Exceptions;
using Brudixy.Interfaces;

namespace Brudixy
{
    public partial class CoreDataRow
    {
        public virtual bool IsNull(ColumnHandle column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (column.Handle >= table.ColumnCount)
                {
                    throw new ArgumentOutOfRangeException($"Table name doesn't have column at {column.Handle} index.");
                }

                var dataColumn = table.GetColumn(column.Handle);
                return dataColumn.DataStorageLink.IsNull(RowHandleCore, dataColumn);
            }
            
            return false;
        }

        public bool IsNotNull(ColumnHandle column)
        {
            return IsNull(column) == false;
        }
        
        public virtual string ToString(CoreDataColumn column, string format = null, IFormatProvider formatProvider = null)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var value = table.GetRowRawFieldValue(RowHandleCore, column, DefaultValueType.ColumnBased, null);

                return CoreDataRowContainer.ValueToStringFormat(format, formatProvider, value, table.DisplayDateTimeUtcOffsetTicks);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public void SilentlySetValue(ColumnHandle column, object value)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot change '{DebugKeyValue}' row from '{table.Name}' table default because it is readonly.");
                }
                
                if (column.Handle >= table.ColumnCount)
                {
                    return;
                }

                table.SilentlySetRowValue(RowHandleCore, value, table.GetColumn(column.Handle));
            }
        }

        public CoreDataRow SetNull(ColumnHandle column)
        {
            this[column] = null;

            return this;
        }

        public virtual bool IsChanged(ColumnHandle column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (RowRecordState == RowState.Unchanged)
                {
                    return false;
                }
                
                if (column.Handle >= table.ColumnCount)
                {
                    throw new ArgumentOutOfRangeException($"Table name doesn't have column at {column.Handle} index.");
                }

                var dataColumn = table.GetColumn(column.Handle);
                
                return dataColumn.DataStorageLink.IsCellChanged(RowHandle, dataColumn);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public virtual object GetOriginalValue(ColumnHandle column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (column.Handle >= table.ColumnCount)
                {
                    throw new ArgumentOutOfRangeException($"Table name doesn't have column at {column.Handle} index.");
                }

                var dataColumn = table.GetColumn(column.Handle);
                
                var dataItem = dataColumn.DataStorageLink;

                if (RowRecordState == RowState.Added)
                {
                    return dataItem.GetDefaultValue(dataColumn);
                }
                
                return dataItem.GetOriginalValue(RowHandleCore, dataColumn);
            }
            
            throw new DataDetachedException($"{DebugKeyValue}");
        }
        
        public virtual T GetOriginalValue<T>(ColumnHandle column)
        {
            var rowState = RowRecordState;
            
            if (table != null && rowState != RowState.Detached)
            {
                if (column.Handle >= table.ColumnCount)
                {
                    throw new ArgumentOutOfRangeException($"Table name doesn't have column at {column.Handle} index.");
                }

                return table.GetOriginalData<T>(RowHandleCore, table.GetColumn(column.Handle));
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public virtual T FieldNotNull<T>(ColumnHandle column)
        {
            return GetFieldValueNotNull(table, table.GetColumn(column.Handle), DefaultValueType.ColumnBased, default(T));
        }

        public virtual ulong GetColumnAge(ColumnHandle column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (column.Handle >= table.ColumnCount)
                {
                    throw new ArgumentOutOfRangeException($"Table name doesn't have column at {column.Handle} index.");
                }

                var dataColumn = table.GetColumn(column.Handle);
                
                if (table.IsReadOnlyColumn(dataColumn))
                {
                    return table.DataAge;
                }

                return dataColumn.DataStorageLink.GetAge(RowHandleCore, dataColumn);
            }
            
            return 0;
        }

        public virtual CoreDataRow Set(ColumnHandle column, string value)
        {
            this[column] = value;

            return this;
        }

        public virtual CoreDataRow Set<T>(ColumnHandle column, T? value) where T : struct, IComparable, IComparable<T>
        {
            return SetCore(table.GetColumn(column.Handle), value);
        }

        public virtual CoreDataRow Set<T>(ColumnHandle column, T value) where T : struct, IComparable, IComparable<T>
        {
            return SetCore(table.GetColumn(column.Handle), new T?(value));
        }

        public virtual T Field<T>(ColumnHandle column, T defaultIfNull)
        {
            return GetFieldValue(table, table.GetColumn(column.Handle), DefaultValueType.Passed, defaultIfNull);
        }

        public virtual T Field<T>(ColumnHandle column)
        {
            return GetFieldValue(table, table?.GetColumn(column.Handle), DefaultValueType.ColumnBased, default(T));
        }

        public virtual object this[ColumnHandle column]
        {
            get
            {
                return GetFieldValue(table, table.GetColumn(column.Handle), DefaultValueType.ColumnBased, null);
            }
            set
            {
                SetFieldCore(table.GetColumn(column.Handle), value);
            }
        }

        public object this[ColumnHandle column, DataRowVersion version = DataRowVersion.Current]
        {
            get
            {
                if (version == DataRowVersion.Original)
                {
                    return GetOriginalValue(column);
                }

                return this[column];
            }
        }
    }
}