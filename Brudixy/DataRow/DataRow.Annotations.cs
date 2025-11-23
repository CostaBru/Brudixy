using Brudixy.Converter;
using Brudixy.Exceptions;
using Brudixy.Interfaces;

namespace Brudixy;

public partial class DataRow
{
    public uint GetAnnotationAge()
    {
        if (table != null && RowRecordState != RowState.Detached)
        {
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetAnnotationAge();
            }
            
            return table.GetRowAnnotationAge(RowHandleCore);
        }
        
        if (m_detachedStorage != null)
        {
            return m_detachedStorage.GetAnnotationAge();
        }
        
        throw new DataDetachedException($"{DebugKeyValue}");
    }
    
    public T GetCellAnnotation<T>(string columnName, string type)
    {
        if (table != null && RowRecordState != RowState.Detached)
        {
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetCellAnnotation<T>(columnName, type);
            }
                
            if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
            {
                return table.GetRowCellAnnotation((DataColumn)column).ValueInfo.GetRowAnnotation<T>(RowHandleCore, type);
            }

            throw new MissingMetadataException($"Table {m_tableName} do not have {columnName} column.");
        }
            
        if (m_detachedStorage != null)
        {
            return m_detachedStorage.GetCellAnnotation<T>(columnName, type);
        }

        throw new DataDetachedException($"{DebugKeyValue}");
    }

    public IEnumerable<(string type, object value)> GetCellAnnotations(string columnName)
    {
        if (table != null && RowRecordState != RowState.Detached)
        {
            if (m_currentEditRow != null)
            {
                foreach (var cellAnnotation in m_currentEditRow.GetCellAnnotations(columnName))
                {
                    yield return cellAnnotation;
                }
                
                yield break;
            }
                
            if (table.DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
            {
                if (column.ColumnHandle >= table.RowCellAnnotationInitCount)
                {
                    yield break;
                }

                var rowAnnotations = table.GetRowCellAnnotation((DataColumn)column).ValueInfo.RowAnnotations;

                foreach (var kv in rowAnnotations)
                {
                    var data = (string)kv.Value.GetData(RowHandleCore);

                    if (string.IsNullOrEmpty(data) == false)
                    {
                        yield return (kv.Key, data);
                    }
                }
                
                yield break;
            }

            throw new MissingMetadataException($"Table {m_tableName} do not have {columnName} column.");
        }
            
        if (m_detachedStorage != null)
        {
            var cellAnnotations = m_detachedStorage.GetCellAnnotations(columnName);

            foreach (var cellAnnotation in cellAnnotations)
            {
                yield return cellAnnotation;
            }
            
            yield break;
        }

        throw new DataDetachedException($"{DebugKeyValue}");
    }

    public IEnumerable<(string column, string type, object value)> GetCellAnnotations()
    {
        if (table != null && RowRecordState != RowState.Detached)
        {
            if (m_currentEditRow != null)
            {
                foreach (var cellAnnotation in m_currentEditRow.GetCellAnnotations())
                {
                    yield return cellAnnotation;
                }
                
                yield break;
            }

            for (int columnHandle = 0; columnHandle < table.DataColumnInfo.ColumnsCount; columnHandle++)
            {
                if (columnHandle >= table.RowCellAnnotationInitCount)
                {
                    yield break;
                }

                var dataColumn = table.GetColumn(columnHandle);

                var rowAnnotations = table.GetRowCellAnnotation(dataColumn).ValueInfo.RowAnnotations;

                foreach (var kv in rowAnnotations)
                {
                    var data = (string)kv.Value.GetData(RowHandleCore);

                    if (string.IsNullOrEmpty(data) == false)
                    {
                        yield return (table.DataColumnInfo.Columns[columnHandle].ColumnName, kv.Key, data);
                    }
                }
            }
            
            yield break;
        }
            
        if (m_detachedStorage != null)
        {
            var cellAnnotations = m_detachedStorage.GetCellAnnotations();

            foreach (var cellAnnotation in cellAnnotations)
            {
                yield return cellAnnotation;
            }
            
            yield break;
        }

        throw new DataDetachedException($"{DebugKeyValue}");
    }

    public T GetRowAnnotation<T>(string type)
    {
        if (table != null && RowRecordState != RowState.Detached)
        {
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetRowAnnotation<T>(type);
            }

            return table.GetRowAnnotation<T>(RowHandleCore, type);
        }
            
        if (m_detachedStorage != null)
        {
            return m_detachedStorage.GetRowAnnotation<T>(type);
        }

        throw new DataDetachedException($"{DebugKeyValue}");
    }

    public IEnumerable<(string type, object value)> GetRowAnnotations()
    {
        if (table != null && RowRecordState != RowState.Detached)
        {
            if (m_currentEditRow != null)
            {
                return m_currentEditRow.GetRowAnnotations();
            }

            return table.GetRowAnnotations(RowHandleCore);
        }
            
        if (m_detachedStorage != null)
        {
            return m_detachedStorage.GetRowAnnotations();
        }

        throw new DataDetachedException($"{DebugKeyValue}");
    }

    public void SetCellAnnotation(string column, string type, object value)
    {
        if (table != null && RowRecordState != RowState.Detached)
        {
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup row cell annotation {type} for '{DebugKeyValue}' row of '{table.Name}' table because it is readonly.");
            }
            
            if (m_currentEditRow != null)
            {
                m_currentEditRow.SetCellAnnotation(column, type, value);
                    
                return;
            }
                
            table.SetCellAnnotation(column, value, RowHandleCore, table.GetTranId(), type);
        }
        else
        {
            throw new DataDetachedException($"{DebugKeyValue}");
        }
    }

    public void SetRowAnnotation(string type, object value)
    {
        if (table != null && RowRecordState != RowState.Detached)
        {
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup row annotation {type} for '{DebugKeyValue}' row of '{table.Name}' table because it is readonly.");
            }
            
            if (m_currentEditRow != null)
            {
                m_currentEditRow.SetRowAnnotation(type, value);
                    
                return;
            }
                
            table.SetRowAnnotation(RowHandleCore, value, table.GetTranId(), type);
        }
        else
        {
            throw new DataDetachedException($"{DebugKeyValue}");
        }
    }
}