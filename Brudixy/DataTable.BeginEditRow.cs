using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy;

public partial class DataTable
{
    public void EndEdit()
    {
        if (m_editingRows == null || m_editingRows.Count <= 0)
        {
            return;
        }

        var dataRows = m_editingRows.Values.ToData();

        foreach (var row in dataRows)
        {
            EndEditRow(row);
        }

        dataRows.Dispose();
    }

    IDataTableRow IDataTable.BeginEditRow(IDataTableRow row) => BeginEditingRow(row.RowHandle);

    IDataTableRow IDataTable.EndEditRow(IDataTableRow row) => EndEditRow(row.RowHandle);

    IDataTableRow IDataTable.CancelEditRow(IDataTableRow row) => CancelEditRow(row.RowHandle);

    public void CancelEdit()
    {
        if (m_editingRows == null || m_editingRows.Count <= 0)
        {
            return;
        }
            
        foreach (var row in m_editingRows.Values)
        {
            row.CancelEdit();
        }

        m_editingRows.Clear();
    }
    
    public DataRow BeginEditRow(DataRow row) => BeginEditingRow(row.RowHandleCore);

    private DataRow BeginEditingRow(int rowRowHandleCore)
    {
        var editingRow = m_editingRows?.GetValueOrDefault(rowRowHandleCore);

        if (editingRow != null)
        {
            editingRow.BeginEdit();

            return editingRow;
        }

        if (m_editingRows == null)
        {
            m_editingRows = new Map<int, DataRow>();
        }

        var dataRow = (DataRow)CreateRowInstance();

        dataRow.Init(rowRowHandleCore, this);

        dataRow.BeginEdit();

        m_editingRows[rowRowHandleCore] = dataRow;

        return dataRow;
    }

    public DataRow CancelEditRow(DataRow row) => CancelEditRow(row.RowHandleCore);

    private DataRow CancelEditRow(int rowRowHandleCore)
    {
        if (m_editingRows == null || m_editingRows.TryGetValue(rowRowHandleCore, out var rowInstance) == false)
        {
            return (DataRow)GetReadyInstance(rowRowHandleCore);
        }

        m_editingRows.Remove(rowRowHandleCore);

        rowInstance.CancelEdit();

        rowInstance.table = null;

        return (DataRow)GetReadyInstance(rowRowHandleCore);
    }

    public DataRow EndEditRow(DataRow row) => EndEditRow(row.RowHandleCore);

    private DataRow EndEditRow(int rowRowHandleCore)
    {
        if (m_editingRows == null || m_editingRows.TryGetValue(rowRowHandleCore, out var rowInstance) == false)
        {
            return (DataRow)GetReadyInstance(rowRowHandleCore);
        }

        m_editingRows.Remove(rowRowHandleCore);

        rowInstance.EndEdit();

        rowInstance.table = null;

        return (DataRow)GetReadyInstance(rowRowHandleCore);
    }
}