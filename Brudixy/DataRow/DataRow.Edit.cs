using JetBrains.Annotations;

namespace Brudixy;

public partial class DataRow
{
    [CanBeNull]
    protected DataRowContainer m_currentEditRow;
        
    internal void BeginEdit()
    {
        if (table.IsReadOnly)
        {
            return;
        }
            
        if (m_currentEditRow != null)
        {
            m_currentEditRow.BeginEdit();
                
            return;
        }
            
        m_currentEditRow = (DataRowContainer)ToContainerCore();
    }

    public bool DataRowRecordIsInEdit() => m_currentEditRow != null;

    internal void EndEdit()
    {
        var currentEditRow = m_currentEditRow;
        
        if (currentEditRow != null)
        {
            currentEditRow.EndEdit();

            m_currentEditRow = null;

            this.CopyChanges(currentEditRow);
        }
    }

    internal bool CancelEdit()
    {
        var cancelEdit = m_currentEditRow?.CancelEdit();

        m_currentEditRow = null;

        return cancelEdit ?? false;
    }

    protected override CoreDataRow SetValueCore<T>(string columnOrXProp, T value)
    {
        if (m_currentEditRow != null)
        {
            m_currentEditRow.Set(columnOrXProp, value);

            return this;
        }
        
        return base.SetValueCore(columnOrXProp, value);
    }

    protected override CoreDataRow SetFieldValueCore(string columnOrXProp, object value)
    {
        if (m_currentEditRow != null)
        {
            m_currentEditRow.Set(columnOrXProp, value);

            return this;
        }
        
        return base.SetFieldValueCore(columnOrXProp, value);
    }

    public override void SetXProperty<T>(string propertyCode, T value)
    {
        if (m_currentEditRow != null)
        {
            m_currentEditRow.SetXProperty<T>(propertyCode, value);

            return;
        }
        
        base.SetXProperty(propertyCode, value);
    }

    protected override CoreDataRow SetCore<T>(CoreDataColumn column, T value)
    {
        if (m_currentEditRow != null)
        {
            m_currentEditRow.Set<T>(column, value);

            return this;
        }
        
        return base.SetCore(column, value);
    }

    protected override CoreDataRow SetFieldCore(CoreDataColumn columnHandle, object value)
    {
        if (m_currentEditRow != null)
        {
            m_currentEditRow.Set(columnHandle, value);

            return this;
        }
        
        return base.SetFieldCore(columnHandle, value);
    }
}