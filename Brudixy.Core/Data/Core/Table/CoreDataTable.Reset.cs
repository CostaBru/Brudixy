using System.Threading;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        public virtual void ClearRows()
        {
            ClearIndexValues();

            StateInfo.RemoveAllRows();
            
            Interlocked.Increment(ref m_dataAge);

            ClearRowReferencesOnClearRows();
        }

        protected virtual void ClearRowReferencesOnClearRows()
        {
            m_rowReferences?.Clear();
        }

        public void ClearTableXProperties()
        {
            ExtProperties?.Clear();

            OnMetadataChanged();
        }

        public virtual void ClearRowsXProperties()
        {
            StateInfo.RowXProps.Clear();

            Interlocked.Increment(ref m_dataAge);
        }
    }
}