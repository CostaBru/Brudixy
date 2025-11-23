using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class CoreDataRowContainer :  IDataChangeTracking
    {
        private ImmutableList<IDataLogEntry> m_changes = ImmutableList<IDataLogEntry>.Empty;
        private ImmutableStack<object> m_changesContext = ImmutableStack<object>.Empty;
            
        private StopwatchSlim? m_stopwatch;
        private DateTime? m_utsStopWatchStart;

        public void StartTrackingChangeTimes(DateTime utcTime)
        {
            m_utsStopWatchStart = utcTime;
            m_stopwatch = StopwatchSlim.StartNew();
        }
        
        public void StopTrackingChangeTimes()
        {
            m_utsStopWatchStart = null;
            m_stopwatch = null;
        }

        public IDisposable StartLoggingChanges(object context)
        {
            if (context == null)
            {
                context = CoreDataTable.NoContextChange;
            }

            m_changesContext = m_changesContext.Push(context);

            return new StopLoggingDisposable(this);
        }

        public bool GetIsLoggingChanges() => m_changesContext.IsEmpty == false;

        [CanBeNull]
        public object CurrentChangingContext() => m_changesContext.IsEmpty ? null : m_changesContext.Peek();
        
        private class StopLoggingDisposable : IDisposable
        {
            private readonly CoreDataRowContainer m_container;

            public StopLoggingDisposable(CoreDataRowContainer container)
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
            if (m_changesContext.IsEmpty)
            {
                return;
            }
            
            m_changesContext = m_changesContext.Pop();
        }

        public void ClearLoggedChanges()
        {
            m_changes = ImmutableList<IDataLogEntry>.Empty;
        }

        public IReadOnlyList<IDataLogEntry> GetLoggedChanges()
        {
            return m_changes;
        }

        private bool LogChange(string columnOrXProperty, object value, object prevValue)
        {
            if (m_changesContext.IsEmpty)
            {
                return false;
            }

            m_changes = m_changes.Add(CreateDataLogEntry(columnOrXProperty, value, prevValue));

            return true;
        }

        protected virtual DataLogEntry CreateDataLogEntry(string columnOrXProperty, object value, object prevValue)
        {
            return new DataLogEntry(Props.RowHandle, m_changesContext.Peek(), columnOrXProperty, value, prevValue, ContainerDataProps.Age, GetChangeTimeStamp(), null);
        }

        protected DateTime? GetChangeTimeStamp()
        {
            return CoreDataTable.GetChangeTimeStamp(m_stopwatch, m_utsStopWatchStart);
        }
    }
}