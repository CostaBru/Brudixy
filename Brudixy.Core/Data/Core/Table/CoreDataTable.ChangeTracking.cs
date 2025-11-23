using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataTable : IDataChangeTracking
    {
        protected ImmutableList<IDataLogEntry> m_changes = ImmutableList<IDataLogEntry>.Empty;
        protected ImmutableStack<object> m_changesContext = ImmutableStack<object>.Empty;
        
        protected StopwatchSlim? m_stopwatch;
        protected DateTime? m_utsStopWatchStart;

        public void StartTrackingChangeTimes(DateTime utcTime)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot start table changes time tracking because it is readonly.");
            }
            
            m_utsStopWatchStart = utcTime;
            m_stopwatch = StopwatchSlim.StartNew();
        }
        
        public void StopTrackingChangeTimes()
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot stop table changes time tracking because it is readonly.");
            }
            
            m_utsStopWatchStart = null;
            m_stopwatch = null;
        }

        [NotNull]
        public IDisposable StartLoggingChanges(object context)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot start table changes tracking because it is readonly.");
            }
            
            if (context == null)
            {
                context = NoContextChange;
            }

            m_changesContext = m_changesContext.Push(context);

            return new StopLoggingDisposable(this);
        }
        
        private class StopLoggingDisposable : IDisposable
        {
            private readonly CoreDataTable m_container;

            public StopLoggingDisposable(CoreDataTable container)
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
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot stop table changes tracking because it is readonly.");
            }
            
            if (m_changesContext.IsEmpty)
            {
                return;
            }
            
            m_changesContext = m_changesContext.Pop();
        }

        [NotNull]
        public IReadOnlyList<IDataLogEntry> GetLoggedChanges()
        {
            return m_changes;
        }

        public bool GetIsLoggingChanges() => m_changesContext.IsEmpty == false;

        [CanBeNull]
        public object CurrentChangingContext() => m_changesContext.IsEmpty ? null : m_changesContext.Peek();
        
        public void ClearLoggedChanges()
        {
            m_changes = ImmutableList<IDataLogEntry>.Empty;
        }

        private class ChangesComparer: IComparer<IDataLogEntry>
        {
            public int Compare(IDataLogEntry x, IDataLogEntry y)
            {
                if (ReferenceEquals(x, y)) return 0;
                if (ReferenceEquals(null, y)) return 1;
                if (ReferenceEquals(null, x)) return -1;
                return Nullable.Compare(x.TranId, y.TranId);
            }
        }

        protected void ClearAllChangesStartingTransaction(int transactionId)
        {
            m_changes = ClearAllChangesStartingTransaction(m_changes, transactionId);
        }
        
        internal static ImmutableList<IDataLogEntry> ClearAllChangesStartingTransaction(ImmutableList<IDataLogEntry> changes, int transactionId)
        {
            if (changes.IsEmpty)
            {
                return ImmutableList<IDataLogEntry>.Empty;
            }
            
            var index = changes.BinarySearch(new DataLogEntry(transactionId), new ChangesComparer());

            if (index < 0)
            {
                index = ~index;
            }
            else
            {
                index = changes.BinarySearchLeft(transactionId, 0, index + 1, (i, tr) => Nullable.Compare(i.TranId, tr));
            }

            if (index >= changes.Count)
            {
                return changes;
            }
                
            var changesCount = changes.Count - (index);

            if (changesCount > 0)
            {
                return changes.RemoveRange(index, changesCount);
            }

            return changes;
        }

        protected bool LogChange(string columnOrXProperty, object value, object prevValue, int rowHandle)
        {
            if (m_changesContext.IsEmpty == false)
            {
                m_changes = m_changes.Add(CreateLogEntry(m_changesContext.Peek(), columnOrXProperty, value, prevValue, rowHandle, GetChangeUtcTimestamp()));
            }
            
            CoreDataTable ds = null;

            if ((DataSetReference?.TryGetTarget(out ds) ?? false) && ds.GetIsLoggingTableChanges())
            {
                var context = ds.CurrentTableChangingContext() ?? NoContextChange;

                var changeUtcTimestamp = GetChangeUtcTimestamp() ?? ds.GetChangeUtcTimestamp();
                
                return ds.LogTableChange(CreateLogEntry(context, columnOrXProperty, value, prevValue, rowHandle, changeUtcTimestamp), TableName);
            }

            return m_changes.Count > 0;
        }
    
        internal static DateTime? GetChangeTimeStamp(StopwatchSlim? stopWatch, DateTime? startTime)
        {
            if (stopWatch == null || startTime == null)
            {
                return null;
            }

            return startTime.Value + stopWatch.Value.Elapsed;
        }
    }
}