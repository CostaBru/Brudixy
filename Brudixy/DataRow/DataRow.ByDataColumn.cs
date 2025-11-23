using System.Text.Json.Nodes;
using System.Xml.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class DataRow 
    {
        public void SetColumnError(DataColumn column, string error)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup '{column.ColumnName}' column '{DebugKeyValue}' row from '{table.Name}' table error because it is readonly.");
                }

                table.GetRowCellAnnotation(column).SetCellError(RowHandleCore, column.ColumnHandle, error, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public string GetColumnError(DataColumn column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var dataColumn = GetThisColumn(column);

                return table.GetRowCellAnnotation(dataColumn).GetCellError(RowHandleCore);
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetCellError(column.ColumnName);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }


        public void SetColumnWarning(DataColumn column, string warning)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup '{column.ColumnName}' column '{DebugKeyValue}' row from '{table.Name}' table warning because it is readonly.");
                }
                
                table.GetRowCellAnnotation(column).SetCellWarning(RowHandleCore, column.ColumnHandle, warning, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public string GetColumnWarning(DataColumn column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var dataColumn = GetThisColumn(column);
                
                return table.GetRowCellAnnotation(dataColumn).GetCellWarning(RowHandleCore);
            }
            
            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetCellWarning(column.ColumnName);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public void SetColumnInfo(DataColumn column, string info)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup '{column.ColumnName}' column '{DebugKeyValue}' row from '{table.Name}' table info because it is readonly.");
                }
                
                table.GetRowCellAnnotation(column).SetCellInfo(RowHandleCore, column.ColumnHandle, info, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        private DataColumn GetThisColumn(DataColumn column)
        {
            if (ReferenceEquals(column.DataTable, this.table))
            {
                return column;
            }

            return table.GetColumn(column.ColumnName);
        }

        public string GetColumnInfo(DataColumn column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var columnHandle = column.ColumnHandle;

                if (columnHandle >= table.RowCellAnnotationInitCount)
                {
                    return string.Empty;
                }
                
                return table.GetRowCellAnnotation(GetThisColumn(column)).GetCellInfo(RowHandleCore);
            }
            
            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetCellInfo(column.ColumnName);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public bool IsNull(DataColumn column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.IsNull(column);
                }

                var dataStorageLink = GetThisColumn(column).DataStorageLink;

                return dataStorageLink.IsNull(RowHandleCore, column);
            }

            if (m_detachedStorage != null)
            {
                return m_detachedStorage.IsNull(column);
            }

            return false;
        }
        
        public override bool IsNull(CoreDataColumn column)
        {
            if (column is DataColumn dc)
            {
                return IsNull(dc);
            }
            
            if (table == null && m_detachedStorage != null)
            {
                return m_detachedStorage.IsNull(column);
            }

            return IsNull(column.ColumnName);
        }

        public bool IsNotNull(DataColumn column)
        {
            return IsNull(column) == false;
        }

        public DataRow SilentlySetValue(DataColumn column, object value)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    m_currentEditRow.SilentlySetValue(column, value);

                    return this;
                }
                
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot change '{DebugKeyValue}' row from '{table.Name}' table default because it is readonly.");
                }
                
                table.SilentlySetRowValue(RowHandleCore, value, GetThisColumn(column));
            }

            return this;
        }

        public DataRow SetNull(DataColumn column)
        {
            this[column] = null;

            return this;
        }

        public bool IsChanged(DataColumn column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (RowRecordState == RowState.Unchanged)
                {
                    return false;
                }

                var thisColumn = GetThisColumn(column);

                return thisColumn.DataStorageLink.IsCellChanged(RowHandle, thisColumn);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public object GetOriginalValue(DataColumn column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var thisColumn = GetThisColumn(column);

                if (RowRecordState == RowState.Added)
                {
                    return thisColumn.DataStorageLink.GetDefaultValue(thisColumn);
                }
                
                return thisColumn.DataStorageLink.GetOriginalValue(RowHandleCore, thisColumn);
            }
            
            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetOriginalValue(column.ColumnName);
            }
            
            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public T GetOriginalValue<T>(DataColumn column)
        {
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetOriginalValue<T>(column.ColumnName);
            }
            
            var rowState = RowRecordState;
            
            if (table != null && rowState != RowState.Detached)
            {
                var thisColumn = GetThisColumn(column);
                return table.GetOriginalData<T>(RowHandleCore, thisColumn);
            }
            
            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetOriginalValue<T>(column.ColumnName);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public T FieldNotNull<T>(DataColumn column) where T : struct
        {
            return GetFieldValueNotNull(table, column, DefaultValueType.ColumnBased, default(T));
        }

        public ulong GetColumnAge(DataColumn column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (m_currentEditRow != null)
                {
                    return m_currentEditRow.GetRowAge();
                }
                
                var thisColumn = GetThisColumn(column);
                
                if (table.IsReadOnlyColumn(thisColumn))
                {
                    return table.DataAge;
                }

                return thisColumn.DataStorageLink.GetAge(RowHandleCore, thisColumn);
            }
            
            if (m_detachedStorage != null)
            {
                return m_detachedStorage.GetRowAge();
            }
          
            return 0;
        }

        public DataRow Set(DataColumn column, string value)
        {
            this[column] = value;

            return this;
        }

        public DataRow Set(DataColumn column, XElement value)
        {
            this[column] = value;

            return this;
        }
        
        public DataRow Set(DataColumn column, JsonObject value)
        {
            this[column] = value;

            return this;
        }

        [NotNull]
        public DataRow Set(DataColumn column, Uri value)
        {
            this[column] = value;

            return this;
        }

        [NotNull]
        public DataRow Set(DataColumn column, Type value)
        {
            this[column] = value;

            return this;
        }

        public DataRow Set(DataColumn column, byte[] value)
        {
            this[column] = value;

            return this;
        }

        public DataRow Set(DataColumn column, char[] value)
        {
            this[column] = value;

            return this;
        }

        public DataRow Set<T>(DataColumn column, T? value) where T : struct, IComparable, IComparable<T>
        {
            SetCore(column, value);
            return this;
        }

        public DataRow Set<T>(DataColumn column, T value) where T : struct, IComparable, IComparable<T>
        {
            if (table == null)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }

            SetCore(column, new T?(value));
            
            return this;
        }

        public DataRow Set(DataColumn column, object value)
        {
            this[column] = value;

            return this;
        }

        public T Field<T>(DataColumn column, T defaultIfNull)
        {
            return GetFieldValue(column, defaultIfNull);
        }

        public T Field<T>(DataColumn column)
        {
            return GetFieldValue(table, column, DefaultValueType.ColumnBased, default(T));
        }

        private T GetFieldValue<T>(DataColumn column, T defaultIfNull)
        {
            return GetFieldValue(table, column, DefaultValueType.Passed, defaultIfNull);
        }

        public object this[DataColumn column]
        {
            get
            {
                if (table == null)
                {
                    if (m_detachedStorage != null)
                    {
                        return m_detachedStorage[column.ColumnName];
                    }
                    
                    throw new DataDetachedException($"{DebugKeyValue}");
                }
                
                return GetFieldValue(table, column, DefaultValueType.ColumnBased, null);
            }
            set
            {
                SetFieldCore(column, value);
            }
        }
        
        private string GetMissingColumnOrXPropErrorMessage(string column)
        {
            return $"Cannot change the '{DebugKeyValue}' row of '{table.Name}' because neither column or extended property with '{column}' name exists.";
        }

        private static object GetDefaultValueAsObject(CoreDataTable table, int columnHandle)
        {
            return table.GetDefaultNullValue<object>(columnHandle);
        }

        private static T GetDefaultValue<T>(CoreDataTable table, int columnHandle)
        {
            return table.GetDefaultNullValue<T>(columnHandle);
        }

        public object this[DataColumn column, DataRowVersion version]
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
    }
}