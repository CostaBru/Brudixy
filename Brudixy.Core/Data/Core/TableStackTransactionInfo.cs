using System.Collections.Generic;
using Konsarpoo.Collections;

namespace Brudixy
{
    internal class TableStackTransactionInfo : StackTransactionInfo
    {
        private readonly Set<int> m_detachedRows = new ();

        public void AddDetachedRow(int rowHandle)
        {
            m_detachedRows.Add(rowHandle);
        }

        public IEnumerable<int> DetachedRows => m_detachedRows;

        public void ClearDetachedRows()
        {
            m_detachedRows.Clear();
        }
            
        public bool RemoveDetachedRow(int rowHandle)
        {
            return m_detachedRows.Remove(rowHandle);
        }
    }
}