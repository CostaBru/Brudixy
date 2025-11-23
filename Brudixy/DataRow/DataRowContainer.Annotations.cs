using Brudixy.Converter;
using Brudixy.EventArgs;
using Brudixy.Exceptions;
using Brudixy.Interfaces;

namespace Brudixy;

public partial class DataRowContainer
{
    public T GetCellAnnotation<T>(string column, string type)
    {
        if (m_currentEditRow != null)
        {
            return m_currentEditRow.GetCellAnnotation<T>(column, type);
        }
        
        if (ContainerDataProps.CellAnnotations == null)
        {
            return TypeConvertor.ReturnDefault<T>();
        }

        if (ContainerDataProps.CellAnnotations.TryGetValue(column, out var columnInfo))
        {
            if (columnInfo.TryGetValue(type, out var value))
            {
                return XPropertyValueConverter.TryConvert<T>(column, type, value);
            }
        }
        
        return TypeConvertor.ReturnDefault<T>();
    }

    public IEnumerable<(string type, object value)> GetCellAnnotations(string columnName)
    {
        if (m_currentEditRow != null)
        {
            var cellAnnotations = m_currentEditRow.GetCellAnnotations(columnName);

            foreach (var annotation in cellAnnotations)
            {
                yield return annotation;
            }
            
            yield break;
        }
        
        if (ContainerDataProps.CellAnnotations == null)
        {
            yield break;
        }

        if (ContainerDataProps.CellAnnotations.TryGetValue(columnName, out var columnInfo))
        {
            foreach (var kv in columnInfo)
            {
                yield return (kv.Key, kv.Value);
            }
        }
    }

    public IEnumerable<(string column, string type, object value)> GetCellAnnotations()
    {
        if (m_currentEditRow != null)
        {
            var cellAnnotations = m_currentEditRow.GetCellAnnotations();

            foreach (var annotation in cellAnnotations)
            {
                yield return annotation;
            }
            
            yield break;
        }
        
        if (ContainerDataProps.CellAnnotations == null)
        {
            yield break;
        }

        foreach (var kv in ContainerDataProps.CellAnnotations)
        {
            foreach (var annotation in kv.Value)
            {
                yield return (kv.Key, annotation.Key, annotation.Value);
            }
        }
    }

    public T GetRowAnnotation<T>(string type)
    {
        if (m_currentEditRow != null)
        {
            return m_currentEditRow.GetRowAnnotation<T>(type);
        }

        if (ContainerDataProps.RowAnnotations == null)
        {
            return TypeConvertor.ReturnDefault<T>();
        }

        if (ContainerDataProps.RowAnnotations.TryGetValue(type, out var value))
        {
            return XPropertyValueConverter.TryConvert<T>("ContainerAnnotation", type, value);
        }
        
        return TypeConvertor.ReturnDefault<T>();
    }

    public IEnumerable<(string type, object value)> GetRowAnnotations()
    {
        if (m_currentEditRow != null)
        {
            var rowAnnotations = m_currentEditRow.GetRowAnnotations();

            foreach (var rowAnnotation in rowAnnotations)
            {
                yield return rowAnnotation;
            }

            yield break;
        }

        if (ContainerDataProps.RowAnnotations == null)
        {
            yield break;
        }

        foreach (var type in ContainerDataProps.RowAnnotations)
        {
            yield return (type.Key, type.Value);
        }
    }

    public void SetRowAnnotation(string type, object value)
    {
        if (IsInitializing == false && IsReadOnly)
        {
            throw new ReadOnlyAccessViolationException(
                $"Cannot setup the {type} for '{DebugKeyValue}' data row container of '{TableName}' table because it is readonly.");
        }

        if (m_currentEditRow != null)
        {
            m_currentEditRow.SetRowAnnotation(type, value);
                
            return;
        }
            
        if (ContainerDataProps.RowAnnotations == null)
        {
            ContainerDataProps.RowAnnotations = new();
        }

        ContainerDataProps.RowAnnotations[type] = value;

        if (IsLocked == false)
        {
            ContainerDataProps.AnnotationAge++;
        }
        
        // no aggregate handler        
        if (m_metaDataChangedEvent != null && m_metaDataChangedEvent.HasAny())
        {
            var changingArgs = new DataContainerMetaDataChangedArgs(this, string.Empty, type, value, RowMetadataType.Row);

            m_metaDataChangedEvent.Raise(changingArgs);
        }
    }

    public void SetCellAnnotation(string column, string type, object value)
    {
        if (IsInitializing == false && IsReadOnly)
        {
            throw new ReadOnlyAccessViolationException(
                $"Cannot setup '{column}' column '{type}' annotation of '{DebugKeyValue}' data row container of '{TableName}' table because it is readonly.");
        }

        if (m_currentEditRow != null)
        {
            m_currentEditRow.SetCellAnnotation(column, type, value);
                
            return;
        }
            
        if (ContainerDataProps.CellAnnotations == null)
        {
            ContainerDataProps.CellAnnotations = new();
        }

        if (ContainerDataProps.CellAnnotations.TryGetValue(column, out var columnInfo) == false)
        {
            ContainerDataProps.CellAnnotations[column] = columnInfo = new();
        }

        columnInfo[type] = value;

        if (IsLocked == false)
        {
            ContainerDataProps.AnnotationAge++;
        }
        
        //no aggregate handler
        if (m_metaDataChangedEvent != null && m_metaDataChangedEvent.HasAny())
        {
            var changingArgs = new DataContainerMetaDataChangedArgs(this, column, type, value, RowMetadataType.Column);

            m_metaDataChangedEvent.Raise(changingArgs);
        }
    }

    public void SetRowError(string value)
    {
        SetRowAnnotation(ValueInfo.Error, value);

        OnErrorsChanged(string.Empty);
    }

    public string GetRowFault() => GetRowAnnotation<string>(ValueInfo.Fault) as string ?? string.Empty;

    public string GetRowError() => GetRowAnnotation<string>(ValueInfo.Error) as string ?? string.Empty;

    public string GetRowWarning() => GetRowAnnotation<string>(ValueInfo.Warning) as string ?? string.Empty;

    public string GetRowInfo() => GetRowAnnotation<string>(ValueInfo.Info) as string ?? string.Empty;

    public void SetRowFault(string value)
    {
        SetRowAnnotation(ValueInfo.Fault, value);
    }

    public void SetRowWarning(string value)
    {
        SetRowAnnotation(ValueInfo.Warning, value);
    }

    public void SetRowInfo(string value)
    {
        SetRowAnnotation(ValueInfo.Info, value);
    }

    public void SetColumnError(string column, string error)
    {
        SetCellAnnotation(column, ValueInfo.Error, error);

        if (IsLocked == false)
        {
            OnErrorsChanged(column);
        }
    }

    public void SetColumnWarning(string column, string warning)
    {
        SetCellAnnotation(column, ValueInfo.Warning, warning);
    }

    public void SetColumnInfo(string column, string info)
    {
        SetCellAnnotation(column, ValueInfo.Info, info);
    }

    protected IEnumerable<string> GetCellWithAnnotationType(string type)
    {
        if (m_currentEditRow != null)
        {
            var errorColumns = m_currentEditRow.GetCellWithAnnotationType(type);

            foreach (var column in errorColumns)
            {
                yield return column;
            }

            yield break;
        }

        if (ContainerDataProps.CellAnnotations == null)
        {
            yield break;
        }

        foreach (var colAnn in ContainerDataProps.CellAnnotations)
        {
            if (colAnn.Value.TryGetValue(type, out var _))
            {
                yield return colAnn.Key;
            }
        }
    }

    public IEnumerable<string> GetErrorColumns() => GetCellWithAnnotationType(ValueInfo.Error);

    public IEnumerable<string> GetWarningColumns() => GetCellWithAnnotationType(ValueInfo.Warning);

    public IEnumerable<string> GetInfoColumns() => GetCellWithAnnotationType(ValueInfo.Info);

    public string GetCellError(string columnName) => GetCellAnnotation<string>(columnName, ValueInfo.Error) as string ?? string.Empty;

    public string GetCellWarning(string columnName) => GetCellAnnotation<string>(columnName, ValueInfo.Warning) as string ?? string.Empty;

    public string GetCellInfo(string columnName) => GetCellAnnotation<string>(columnName, ValueInfo.Info) as string ?? string.Empty;

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    string IDataRowReadOnlyAccessor.GetRowError() => GetRowError();

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    string IDataRowReadOnlyAccessor.GetRowWarning() => GetRowWarning();

    [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
    string IDataRowReadOnlyAccessor.GetRowInfo() => GetRowInfo();
}