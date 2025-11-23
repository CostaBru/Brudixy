using System.Text.Json.Nodes;
using System.Xml.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class DataRow
    {
        public void SetColumnError(ColumnHandle column, string error)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (column.Handle >= table.ColumnCount)
                {
                    throw new InvalidCastException($"Table name doesn't have column at {column.Handle} index.");
                }
                
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup '{table.DataColumnInfo.Columns[column.Handle].ColumnName}' column '{DebugKeyValue}' row from '{table.Name}' table error because it is readonly.");
                }
                
                var dataColumn = table.GetColumn(column.Handle);

                table.GetRowCellAnnotation(dataColumn).SetCellError(RowHandleCore, column.Handle, error, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public string GetColumnError(ColumnHandle column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (column.Handle >= table.ColumnCount)
                {
                    throw new InvalidCastException($"Table name doesn't have column at {column.Handle} index.");
                }
                
                if (column.Handle >= table.RowCellAnnotationInitCount)
                {
                    return string.Empty;
                }
                
                var dataColumn = table.GetColumn(column.Handle);

                return table.GetRowCellAnnotation(dataColumn).GetCellError(RowHandleCore);
            }
            
            if (m_detachedStorage != null && column.Handle < m_detachedStorage.GetColumnCount())
            {
                var container = m_detachedStorage.GetColumn(column.Handle);

                return m_detachedStorage.GetCellError(container.ColumnName);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public void SetColumnWarning(ColumnHandle column, string warning)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (column.Handle >= table.ColumnCount)
                {
                    throw new ArgumentOutOfRangeException($"Table name doesn't have column at {column.Handle} index.");
                }
                
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup '{table.DataColumnInfo.Columns[column.Handle].ColumnName}' column '{DebugKeyValue}' row from '{table.Name}' table warning because it is readonly.");
                }
                
                var dataColumn = table.GetColumn(column.Handle);

                table.GetRowCellAnnotation(dataColumn).SetCellWarning(RowHandleCore, column.Handle, warning, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public string GetColumnWarning(ColumnHandle column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (column.Handle >= table.ColumnCount)
                {
                    throw new ArgumentOutOfRangeException($"Table name doesn't have column at {column.Handle} index.");
                }
                
                if (column.Handle >= table.RowCellAnnotationInitCount)
                {
                    return string.Empty;
                }
                
                var dataColumn = table.GetColumn(column.Handle);

                return table.GetRowCellAnnotation(dataColumn).GetCellWarning(RowHandleCore);
            }

            if (m_detachedStorage != null && column.Handle < m_detachedStorage.GetColumnCount())
            {
                var container = m_detachedStorage.GetColumn(column.Handle);
                return m_detachedStorage.GetCellWarning(container.ColumnName);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public void SetColumnInfo(ColumnHandle column, string info)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (column.Handle >= table.ColumnCount)
                {
                    throw new ArgumentOutOfRangeException($"Table name doesn't have column at {column.Handle} index.");
                }
                
                if (table.IsInitializing == false && table.IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot setup '{table.DataColumnInfo.Columns[column.Handle].ColumnName}' column '{DebugKeyValue}' row from '{table.Name}' table info because it is readonly.");
                }
                
                var dataColumn = table.GetColumn(column.Handle);

                table.GetRowCellAnnotation(dataColumn).SetCellInfo(RowHandleCore, column.Handle, info, table.GetTranId());
            }
            else
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
        }

        public string GetColumnInfo(ColumnHandle column)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                if (column.Handle >= table.ColumnCount)
                {
                    throw new ArgumentOutOfRangeException($"Table name doesn't have column at {column.Handle} index.");
                }
                
                if (column.Handle >= table.RowCellAnnotationInitCount)
                {
                    return string.Empty;
                }
                
                var dataColumn = table.GetColumn(column.Handle);

                return table.GetRowCellAnnotation(dataColumn).GetCellInfo(RowHandleCore);
            }
            
            if (m_detachedStorage != null && column.Handle < m_detachedStorage.GetColumnCount())
            {
                var container = m_detachedStorage.GetColumn(column.Handle);
                return m_detachedStorage.GetCellInfo(container.ColumnName);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public override bool IsNull(ColumnHandle column)
        {
            if (table is null && m_detachedStorage != null && column.Handle < m_detachedStorage.GetColumnCount())
            {
                var container = m_detachedStorage.GetColumn(column.Handle);
                
                return m_detachedStorage.IsNull(container);
            }
            
            return base.IsNull(column);
        }

        public new DataRow SetNull(ColumnHandle column)
        {
            this[column] = null;

            return this;
        }

        public override bool IsChanged(ColumnHandle column)
        {
            if (table is null && m_detachedStorage != null && column.Handle < m_detachedStorage.GetColumnCount())
            {
                var dataColumnContainer = m_detachedStorage.GetColumn(column.Handle);

                return m_detachedStorage.GetChangedFields().Contains(dataColumnContainer.ColumnName);
            }

            return base.IsChanged(column);
        }

        public override object GetOriginalValue(ColumnHandle column)
        {
            if (table is null && m_detachedStorage != null && column.Handle < m_detachedStorage.GetColumnCount())
            {
                var dataColumnContainer = m_detachedStorage.GetColumn(column.Handle);
                
                return m_detachedStorage.GetOriginalValue(dataColumnContainer);
            }

            return base.GetOriginalValue(column);
        }
        
        public override T GetOriginalValue<T>(ColumnHandle column)
        {
            if (table is null && m_detachedStorage != null && column.Handle < m_detachedStorage.GetColumnCount())
            {
                var dataColumnContainer = m_detachedStorage.GetColumn(column.Handle);
                
                return m_detachedStorage.GetOriginalValue<T>(dataColumnContainer);
            }

            return base.GetOriginalValue<T>(column);
        }

        public override ulong GetColumnAge(ColumnHandle column)
        {
            if (table is null && m_detachedStorage != null)
            {
                return m_detachedStorage.GetRowAge();
            }
            
            return base.GetColumnAge(column);
        }

        public override DataRow Set(ColumnHandle column, string value)
        {
            this[column] = value;

            return this;
        }

        public DataRow Set(ColumnHandle column, XElement value)
        {
            this[column] = value;

            return this;
        }
        
        public DataRow Set(ColumnHandle column, JsonObject value)
        {
            this[column] = value;

            return this;
        }

        [NotNull]
        public DataRow Set(ColumnHandle column, Uri value)
        {
            this[column] = value;

            return this;
        }
        
        [NotNull]
        public DataRow Set(ColumnHandle column, Type value)
        {
            this[column] = value;

            return this;
        }

        public DataRow Set(ColumnHandle column, byte[] value)
        {
            this[column] = value;

            return this;
        }

        public DataRow Set(ColumnHandle column, char[] value)
        {
            this[column] = value;

            return this;
        }

        public override DataRow Set<T>(ColumnHandle column, T? value) 
        {
            if (table == null)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            var dataColumn = table.GetColumn(column.Handle);

            if (m_currentEditRow != null)
            {
                m_currentEditRow.Set<T>(dataColumn, value);

                return this;
            }
            
            SetCore(dataColumn, value);
            
            return this;
        }

        public override DataRow Set<T>(ColumnHandle column, T value) 
        {
            if (table == null)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            var dataColumn = table.GetColumn(column.Handle);

            if (m_currentEditRow != null)
            {
                m_currentEditRow.Set<T>(dataColumn, value);

                return this;
            }
            
            SetCore(dataColumn, new T?(value));

            return this;
        }
    }
}