using Brudixy.EventArgs;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy;

public partial class DataTable
{
    [CanBeNull]
    internal Data<RowCellAnnotation> RowCellAnnotations;

    private DisposableCollection m_disposables = new ();

    [CanBeNull]
    internal RowXPropertyInfoDataItem m_rowXPropertyAnnotations;

    [CanBeNull]
    internal ValueInfo m_rowAnnotations;
    
    [CanBeNull]
    private CoreContainerMetadataProps m_containerMetaDataPropsAge;
    
    [CanBeNull]
    private Map<int, DataRow> m_editingRows;
    
    [CanBeNull]
    internal DataExpressionCache ExpressionValuesCache;

    protected override void OnDisposed()
    {
        base.OnDisposed();
        
        DisposeEvents();

        if (m_editingRows != null)
        {
            foreach (var row in m_editingRows.Values)
            {
                row.EndEdit();
            }
            m_editingRows.Dispose();
            m_editingRows = null;
        }
            
        RowCellAnnotations?.Dispose();
        m_rowAnnotations?.Dispose();
        ExpressionValuesCache?.Dispose();
        m_rowXPropertyAnnotations?.Dispose(null);
        m_disposables?.Dispose();
    }

    protected override void ResetState()
    {
        base.ResetState();
        
        ResetEvents();

        m_editingRows = null;
        RowCellAnnotations = null;
        m_rowXPropertyAnnotations = null;
        m_rowAnnotations = null;
        ExpressionValuesCache = new DataExpressionCache(this);
        m_disposables = new ();
        m_containerMetaDataPropsAge = null;
    }
}