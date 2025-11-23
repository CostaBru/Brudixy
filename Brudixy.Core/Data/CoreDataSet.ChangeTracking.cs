using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataSet
    {
        protected ImmutableDictionary<string, ImmutableList<IDataLogEntry>> m_changes = ImmutableDictionary<string, ImmutableList<IDataLogEntry>>.Empty;
        protected ImmutableStack<object> m_changesContext = ImmutableStack<object>.Empty;
            
        protected StopwatchSlim? m_stopwatch;
        protected DateTime? m_utsStopWatchStart;

        public void StartTrackingChangeTimes(DateTime utcTime)
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot start logging change times for '{DataSetName}' data set because it is readonly.");
            }
            
            m_utsStopWatchStart = utcTime;
            m_stopwatch = StopwatchSlim.StartNew();
        }
        
        public void StopTrackingChangeTimes()
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot stop logging change times for '{DataSetName}' data set because it is readonly.");
            }
            
            m_utsStopWatchStart = null;
            m_stopwatch = null;
        }

        public IDisposable StartLoggingChanges(object context)
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot start logging changes for '{DataSetName}' data set because it is readonly.");
            }

            if (context == null)
            {
                context = CoreDataTable.NoContextChange;
            }

            m_changesContext = m_changesContext.Push(context);

            return new StopLoggingDisposable(this);
        }

        private class StopLoggingDisposable : IDisposable
        {
            private readonly CoreDataSet m_container;

            public StopLoggingDisposable(CoreDataSet container)
            {
                m_container = container;
            }

            public void Dispose()
            {
                m_container.StopLoggingChanges();
            }
        }
        
        public void StopLoggingChanges()
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot stop logging changes for '{DataSetName}' data set because it is readonly.");
            }
            
            if (m_changesContext.IsEmpty)
            {
                return;
            }
            
            m_changesContext = m_changesContext.Pop();
        }

        public IReadOnlyList<IDataLogEntry> GetTableLoggedChanges(string tableName)
        {
            if (m_changes.TryGetValue(tableName, out var tableChanges))
            {
                return tableChanges;
            }
            return ImmutableList<IDataLogEntry>.Empty;
        }
        
        public void ClearTableLoggedChanges()
        {
            m_changes = ImmutableDictionary<string, ImmutableList<IDataLogEntry>>.Empty;
        }
        
        public void ClearTableLoggedChanges(string tableName)
        {
            if (m_changes.ContainsKey(tableName))
            {
                m_changes = m_changes.Remove(tableName);
            }
        }

        internal void ClearTableTransactionChanges(string tableName, int transactionId)
        {
            if(m_changes.TryGetValue(tableName, out var tableChanges))
            {
                m_changes= m_changes.SetItem(tableName, CoreDataTable.ClearAllChangesStartingTransaction(tableChanges, transactionId));
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
                    $"Cannot log change for '{DataSetName}' data set because it is readonly.");
            }

            if (TablesMap.MissingKey(tableName))
            {
                throw new ArgumentNullException($"Given '{tableName}' table name is missing in '{DataSetName}' data set.");
            }
            
            if (m_changesContext.IsEmpty)
            {
                return false;
            }

            ImmutableList<IDataLogEntry> tableChanges;
            
            if (m_changes.TryGetValue(tableName, out tableChanges) == false)
            {
                tableChanges = ImmutableList<IDataLogEntry>.Empty;
            }

            tableChanges = tableChanges.Add(change);
            
            m_changes = m_changes.SetItem(tableName, tableChanges);

            return true;
        }

        public virtual DateTime? GetChangeUtcTimestamp()
        {
            return CoreDataTable.GetChangeTimeStamp(m_stopwatch, m_utsStopWatchStart);
        }
    }
}