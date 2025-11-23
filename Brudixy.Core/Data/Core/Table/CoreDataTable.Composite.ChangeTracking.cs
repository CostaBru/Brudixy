using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        public IReadOnlyList<IDataLogEntry> GetTableLoggedChanges(string tableName)
        {
            if (m_dsChanges.TryGetValue(tableName, out var tableChanges))
            {
                return tableChanges;
            }
            return ImmutableList<IDataLogEntry>.Empty;
        }
        
        public void ClearTableLoggedChanges()
        {
            m_dsChanges = ImmutableDictionary<string, ImmutableList<IDataLogEntry>>.Empty;
        }
        
        public void ClearTableLoggedChanges(string tableName)
        {
            if (m_dsChanges.ContainsKey(tableName))
            {
                m_dsChanges = m_dsChanges.Remove(tableName);
            }
        }

        internal void ClearTableTransactionChanges(string tableName, int transactionId)
        {
            if(m_dsChanges.TryGetValue(tableName, out var tableChanges))
            {
                m_dsChanges= m_dsChanges.SetItem(tableName, CoreDataTable.ClearAllChangesStartingTransaction(tableChanges, transactionId));
            }
        }

        public bool GetIsLoggingTableChanges() => m_changesContext.IsEmpty == false;

        [CanBeNull]
        public object CurrentTableChangingContext() => m_changesContext.IsEmpty ? null : m_changesContext.Peek();

        public virtual bool LogTableChange([NotNull] IDataLogEntry change, [NotNull] string tableName)
        {
            if (change == null)
            {
                throw new ArgumentNullException(nameof(change));
            }

            if (string.IsNullOrEmpty(tableName))
            {
                throw new ArgumentNullException(nameof(tableName));
            }

            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot log change for '{TableName}' data set because it is readonly.");
            }

            if (TablesMap.MissingKey(tableName))
            {
                throw new ArgumentNullException($"Given '{tableName}' table name is missing in '{TableName}' data set.");
            }
            
            if (m_changesContext.IsEmpty)
            {
                return false;
            }

            ImmutableList<IDataLogEntry> tableChanges;
            
            if (m_dsChanges.TryGetValue(tableName, out tableChanges) == false)
            {
                tableChanges = ImmutableList<IDataLogEntry>.Empty;
            }

            tableChanges = tableChanges.Add(change);
            
            m_dsChanges = m_dsChanges.SetItem(tableName, tableChanges);

            return true;
        }
    }
}