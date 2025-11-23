using System.ComponentModel;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy;

public partial class DataRowContainer
{
    void IEditableObject.BeginEdit() => this.BeginEdit();

    void IEditableObject.CancelEdit() => this.CancelEdit();

    void IEditableObject.EndEdit() => this.EndEdit();

    [CanBeNull]
    protected DataRowContainer m_currentEditRow;
        
    public IDataRowEdit BeginEdit()
    {
        if (m_currentEditRow != null)
        {
            return m_currentEditRow.BeginEdit();
        }
            
        m_currentEditRow = Clone();

        return this;
    }

    public IDataRowContainer Editing => m_currentEditRow;

    public bool RowInEdit() => m_currentEditRow != null;

    public void EndEdit()
    {
        if (m_currentEditRow != null)
        {
            m_currentEditRow.EndEdit();

            var sourceOfChanges = m_currentEditRow;
            
            m_currentEditRow = null;

            CopyChanges(sourceOfChanges);
        }
    }

    public bool CancelEdit()
    {
        var cancelEdit = m_currentEditRow?.CancelEdit();

        m_currentEditRow = null;

        return cancelEdit ?? false;
    }

    protected override void SetValue<T>(ref T value, ref T field, ref T prefField, string name)
    {
        if (m_currentEditRow != null)
        {
            m_currentEditRow.SetValue(ref value, ref field, ref prefField, name);
            
            return;
        }
        
        base.SetValue(ref value, ref field, ref prefField, name);
    }

    protected override void SetData(CoreDataColumnContainer reference, object value)
    {
        if (m_currentEditRow != null)
        {
            m_currentEditRow.SetData(reference, value);
            
            return;
        }
        
        base.SetData(reference, value);
    }

    public override object this[string columnOrXProp]
    {
        get
        {
            if (m_currentEditRow != null)
            {
                return m_currentEditRow[columnOrXProp];
            }
            
            return base[columnOrXProp];
        }
        set
        {
            if (m_currentEditRow != null)
            {
                m_currentEditRow[columnOrXProp] = value;
                
                return;
            }
            
            base[columnOrXProp] = value;
        }
    }

    protected override void SetCore(string column, CoreDataColumnContainer colObj, object newValue, bool setData)
    {
        if (m_currentEditRow != null)
        {
            m_currentEditRow.SetCore(column, colObj, newValue, setData);
            
            return;
        }
        
        base.SetCore(column, colObj, newValue, setData);
    }

    public override object GetOriginalValue(string columnOrXProp)
    {
        if (m_currentEditRow != null)
        {
            return m_currentEditRow.GetOriginalValue(columnOrXProp);
        }
        
        return base.GetOriginalValue(columnOrXProp);
    }

    public override IEnumerable<string> GetXProperties()
    {
        if (m_currentEditRow != null)
        {
            return m_currentEditRow.GetXProperties();
        }
        
        return base.GetXProperties();
    }

    public override bool HasXProperty(string xPropertyName)
    {
        if (m_currentEditRow != null)
        {
            return m_currentEditRow.HasXProperty(xPropertyName);
        }
        
        return base.HasXProperty(xPropertyName);
    }

    public override T GetXProperty<T>(string xPropertyName, bool original = false)
    {
        if (m_currentEditRow != null)
        {
            return m_currentEditRow.GetXProperty<T>(xPropertyName, original);
        }
        
        return base.GetXProperty<T>(xPropertyName, original);
    }

    protected override bool TryGetOriginalValue(CoreDataColumnContainer column, out object value)
    {
        if (m_currentEditRow != null)
        {
            return m_currentEditRow.TryGetOriginalValue(column, out value);
        }
        
        return base.TryGetOriginalValue(column, out value);
    }

    public override T GetFieldValue<T>(string columnOrXProp, T defaultIfNull, DefaultValueType defaultValueType)
    {
        if (m_currentEditRow != null)
        {
            return m_currentEditRow.GetFieldValue<T>(columnOrXProp, defaultIfNull, defaultValueType);
        }
        
        return base.GetFieldValue(columnOrXProp, defaultIfNull, defaultValueType);
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
}