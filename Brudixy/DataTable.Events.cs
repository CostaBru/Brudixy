using Brudixy.Interfaces;
using Brudixy.EventArgs;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    partial class DataTable
    {
        private RowXPropertyChangingDataEvent m_rowXPropertyChanging;
        private RowXPropertyChangedDataEvent m_rowXPropertyChanged;
        internal DataColumnChangedDataEvent m_columnChanged;
        internal DataColumnChangingDataEvent m_columnChanging;
        private DataRowAddedDataEvent m_rowAdded;
        private DataRowAddingDataEvent m_rowAdding;
        private DataRowDeletedDataEvent m_rowDeleted;
        private DataRowDeletingDataEvent m_rowDeleting;
        internal DataRowChangedDataEvent m_dataRowChanged;
        private DataTableDisposedDataEvent m_disposed;
        private DataTableXPropertyChangedDataEvent m_tableXPropertyChanged;
        private DataTableXPropertyChangingDataEvent m_tableXPropertyChanging;
        private DataRowMetaDataChangedEvent m_rowMetaDataChangedEvent;
        private DataTableTransactionCommitEvent m_dataTableCommitEvent;
        private DataTableTransactionRollbackEvent m_dataTableRollbackEvent;
        
        [CanBeNull]
        internal Map<int, IColumnChangingInfo> m_columnChangingHandlers;

        [CanBeNull]
        internal Map<int, ColumnChangedInfo> m_columnChangedHandlers;

        [CanBeNull]
        private Map<int, CellValueRequestingHandlerInfo> m_emptyRowCellValueRequestingHandlers;
        
        private Map<int, Map<int, Data<ColumnChange>>> m_columnChangedAggregated;
        private Map<int, Map<string,  Data<XPropertyChange>>> m_rowXPropertyChangedAggregated;
        
        private Data<(int rowHandle, int? tran)> m_rowChangedAggregated;
        private Data<(int rowHandle, int? tran)> m_addedRowHandlesAggregated;
        private Data<(int rowHandle, int? tran)> m_rowDeletedHandlesAggregated;

        protected virtual void ResetEvents()
        {
            m_rowXPropertyChanging = null;
            m_rowXPropertyChanged = null;
            m_columnChanged = null;
            m_columnChanging = null;
            m_rowAdded = null;
            m_rowAdding = null;
            m_rowDeleted = null;
            m_rowDeleting = null;
            m_dataRowChanged = null;
            m_disposed = null;
            m_tableXPropertyChanged = null;
            m_tableXPropertyChanging = null;
            m_rowMetaDataChangedEvent = null;
            m_dataTableCommitEvent = null;
            m_dataTableRollbackEvent = null;

            m_columnChangingHandlers = null;
            m_columnChangedHandlers = null;
            m_emptyRowCellValueRequestingHandlers = null;
            m_columnChangedAggregated = null;
            m_rowXPropertyChangedAggregated = null;
            m_rowChangedAggregated = null;
            m_addedRowHandlesAggregated = null;
            m_rowDeletedHandlesAggregated = null;
        }

        protected virtual void DisposeEvents()
        {
            if (m_columnChangingHandlers != null)
            {
                foreach (var columnChangingHandler in m_columnChangingHandlers)
                {
                    columnChangingHandler.Value.Dispose();
                }

                m_columnChangingHandlers.Dispose();
            }

            if (m_columnChangedHandlers != null)
            {
                foreach (var changedHandler in m_columnChangedHandlers)
                {
                    changedHandler.Value.SubscribeCount = 0;
                    changedHandler.Value.Event = null;
                }

                m_columnChangedHandlers.Dispose();
            }

            if (m_emptyRowCellValueRequestingHandlers != null)
            {
                foreach (var changedHandler in m_emptyRowCellValueRequestingHandlers)
                {
                    changedHandler.Value.SubscribeCount = 0;
                    changedHandler.Value.Event = null;
                }

                m_emptyRowCellValueRequestingHandlers.Dispose();
            }

            if (m_columnChangedAggregated != null)
            {
                foreach (var value in m_columnChangedAggregated.Values)
                {
                    value?.Dispose();
                }

                m_columnChangedAggregated.Dispose();
            }

            
            m_rowXPropertyChanging?.Dispose();
            m_rowXPropertyChanged?.Dispose();
            m_columnChanged?.Dispose();
            m_columnChanging?.Dispose();
            m_rowAdded?.Dispose();
            m_rowAdding?.Dispose();
            m_rowDeleted?.Dispose();
            m_rowDeleting?.Dispose();
            m_dataRowChanged?.Dispose();
            m_tableXPropertyChanged?.Dispose();
            m_tableXPropertyChanging?.Dispose();
            m_rowMetaDataChangedEvent ?.Dispose();
            m_dataTableCommitEvent?.Dispose();
            m_dataTableRollbackEvent?.Dispose();

            m_rowXPropertyChangedAggregated ?.Dispose();
            m_rowChangedAggregated?.Dispose();
            m_addedRowHandlesAggregated?.Dispose();
            m_rowDeletedHandlesAggregated?.Dispose();
            
            if (HasDisposedHandler)
            {
                Disposed.Raise(new DataTableDisposedEventArgs {TableName = Name});
            }
            
            m_disposed?.Dispose();
        }

        public void AttachEventHandlersTo(DataTable target, IDataOwner dataOwner = null)
        {
            AttachDatasetEventHandlersTo(target, dataOwner);
            
            if (m_columnChanged != null)
            {
                if (target.m_columnChanged == null)
                {
                    target.m_columnChanged = new (m_disposables);
                }

                m_columnChanged.CopyTo(target.m_columnChanged);
            }

            if (m_columnChanging != null)
            {
                if (target.m_columnChanging == null)
                {
                    target.m_columnChanging = new (m_disposables);
                }

                m_columnChanging.CopyTo(target.m_columnChanging);
            }

            if (m_rowAdded != null)
            {
                if (target.m_rowAdded == null)
                {
                    target.m_rowAdded = new (m_disposables);
                }

                m_rowAdded.CopyTo(target.m_rowAdded);
            }

            if (m_rowAdding != null)
            {
                if (target.m_rowAdding == null)
                {
                    target.m_rowAdding = new (m_disposables);
                }

                m_rowAdding.CopyTo(target.m_rowAdding);
            }
         
            if (m_rowDeleted != null)
            {
                if (target.m_rowDeleted == null)
                {
                    target.m_rowDeleted = new (m_disposables);
                }

                m_rowDeleted.CopyTo(target.m_rowDeleted);
            }

            if (m_rowDeleting != null)
            {
                if (target.m_rowDeleting == null)
                {
                    target.m_rowDeleting = new (m_disposables);
                }

                m_rowDeleting.CopyTo(target.m_rowDeleting);
            }

            if (m_maxColumnLenConstraint != null)
            {
                if (target.m_maxColumnLenConstraint == null)
                {
                    target.m_maxColumnLenConstraint = new (m_disposables);
                }

                m_maxColumnLenConstraint.CopyTo(target.m_maxColumnLenConstraint);
            }

            if (m_dataRowChanged != null)
            {
                if (target.m_dataRowChanged == null)
                {
                    target.m_dataRowChanged = new (m_disposables);
                }

                m_dataRowChanged.CopyTo(target.m_dataRowChanged);
            }

            if (m_columnChangedHandlers != null)
            {
                if (target.m_columnChangedHandlers == null)
                {
                    target.m_columnChangedHandlers = new ();
                }

                foreach (var sourceHandler in m_columnChangedHandlers)
                {
                    if (target.m_columnChangedHandlers.TryGetValue(sourceHandler.Key, out var targetHandler))
                    {
                        sourceHandler.Value.CopyTo(targetHandler);
                    }
                    else
                    {
                        target.m_columnChangedHandlers[sourceHandler.Key] = sourceHandler.Value.Clone(m_disposables);
                    }
                }
            }

            if (m_columnChangingHandlers != null)
            {
                if (target.m_columnChangingHandlers == null)
                {
                    target.m_columnChangingHandlers = new ();
                }

                foreach (var sourceHandler in m_columnChangingHandlers)
                {
                    if (target.m_columnChangingHandlers.TryGetValue(sourceHandler.Key, out var targetHandler))
                    {
                        sourceHandler.Value.CopyTo(targetHandler);
                    }
                    else
                    {
                        target.m_columnChangingHandlers[sourceHandler.Key] = sourceHandler.Value.Clone(m_disposables);
                    }
                }
            }

            if (m_emptyRowCellValueRequestingHandlers != null)
            {
                if (target.m_emptyRowCellValueRequestingHandlers == null)
                {
                    target.m_emptyRowCellValueRequestingHandlers = new ();
                }

                foreach (var sourceHandler in m_emptyRowCellValueRequestingHandlers)
                {
                    if (target.m_emptyRowCellValueRequestingHandlers.TryGetValue(sourceHandler.Key, out var targetHandler))
                    {
                        sourceHandler.Value.CopyTo(targetHandler);
                    }
                    else
                    {
                        target.m_emptyRowCellValueRequestingHandlers[sourceHandler.Key] = sourceHandler.Value.Clone(m_disposables);
                    }
                }
            }

            if (m_rowXPropertyChanging != null)
            {
                if (target.m_rowXPropertyChanging == null)
                {
                    target.m_rowXPropertyChanging = new (m_disposables);
                }

                m_rowXPropertyChanging.CopyTo(target.m_rowXPropertyChanging);
            }

            if (m_rowXPropertyChanged != null)
            {
                if (target.m_rowXPropertyChanged == null)
                {
                    target.m_rowXPropertyChanged = new (m_disposables);
                }

                m_rowXPropertyChanged.CopyTo(target.m_rowXPropertyChanged);
            }
            
            if (m_tableXPropertyChanging != null)
            {
                if (target.m_tableXPropertyChanging == null)
                {
                    target.m_tableXPropertyChanging = new (m_disposables);
                }

                m_tableXPropertyChanging.CopyTo(target.m_tableXPropertyChanging);
            }
            
            if (m_tableXPropertyChanged != null)
            {
                if (target.m_tableXPropertyChanged == null)
                {
                    target.m_tableXPropertyChanged = new (m_disposables);
                }

                m_tableXPropertyChanged.CopyTo(target.m_tableXPropertyChanged);
            }
            
            if (m_rowMetaDataChangedEvent != null)
            {
                if (target.m_rowMetaDataChangedEvent == null)
                {
                    target.m_rowMetaDataChangedEvent = new (m_disposables);
                }

                m_rowMetaDataChangedEvent.CopyTo(target.m_rowMetaDataChangedEvent);
            }
            
            if (m_dataTableCommitEvent != null)
            {
                if (target.m_dataTableCommitEvent == null)
                {
                    target.m_dataTableCommitEvent = new (m_disposables);
                }

                m_dataTableCommitEvent.CopyTo(target.m_dataTableCommitEvent);
            }
            
            if (m_dataTableRollbackEvent != null)
            {
                if (target.m_dataTableRollbackEvent == null)
                {
                    target.m_dataTableRollbackEvent = new (m_disposables);
                }

                m_dataTableRollbackEvent.CopyTo(target.m_dataTableRollbackEvent);
            }

            if (dataOwner != null)
            {
                Owner = dataOwner;
            }
        }

        private void TryRaiseRowDeleted(IReadOnlyList<int> rowHandles)
        {
            if (IsInitializing)
            {
                return;
            }

            if (AreEventsLocked)
            {
                if (m_rowDeletedHandlesAggregated == null)
                {
                    m_rowDeletedHandlesAggregated = new ();
                }

                var tranId = GetTranId();
                
                foreach (var rowHandle in rowHandles)
                {
                    m_rowDeletedHandlesAggregated.Add((rowHandle, tranId));
                }
            }
            else
            {
                var deletedArgs = new DataRowDeletedArgs(this, rowHandles.ToData());

                RaiseRowDeleted(deletedArgs);
            }
        }

        internal void TryRaiseRowXPropertyChanged(int rowHandle, string propertyCode, object prevValue, object value)
        {
            if (IsInitializing)
            {
                return;
            }

            if (AreEventsLocked || StateInfo.IsRowEventLocked(rowHandle))
            {
                AggregateRowXPropertyChangedEvent(rowHandle, propertyCode, prevValue, value);
            }
            else
            {
                var args = new DataRowXPropertyChangedArgs(this, rowHandle, propertyCode, prevValue, value, GetTranId());
                
                RaiseRowXPropertyChanged(args);
                
                args.Dispose();
            }
        }

        private void RaiseRowXPropertyChanged(DataRowXPropertyChangedArgs columnChangedArgs)
        {
            RowXPropertyChanged.Raise(columnChangedArgs);
        }

        internal void TryRaiseColumnChanged(int rowHandle, CoreDataColumn column, object prevValue, object value)
        {
            if (IsInitializing)
            {
                return;
            }

            if (AreEventsLocked || StateInfo.IsRowEventLocked(rowHandle))
            {
                AggregateColumnChangedEvent(rowHandle, column, prevValue, value);
            }
            else
            {
                RaiseColumnChanged(new DataColumnChangedArgs(this, rowHandle, column.ColumnHandle, null, prevValue, value));
            }
        }

        private void RaiseColumnChanged(DataColumnChangedArgs columnChangedArgs)
        {
            ColumnChanged.Raise(columnChangedArgs);
        }

        internal bool TryRaiseCustomColumnChanged(int rowHandle, CoreDataColumn column,object prevValue, object value)
        {
            if (m_columnChangedHandlers == null)
            {
                return false;
            }

            if (IsInitializing)
            {
                return false;
            }

            if (AreEventsLocked || StateInfo.IsRowEventLocked(rowHandle))
            {
                AggregateColumnChangedEvent(rowHandle, column, prevValue, value);
            }
            else
            {
                if (m_columnChangedHandlers.TryGetValue(column.ColumnHandle, out var changigInfo))
                {
                    if (changigInfo.SubscribeCount > 0)
                    {
                        changigInfo.Event.Raise(new DataColumnChangedArgs(this, rowHandle, column.ColumnHandle, null, prevValue, value));

                        return true;
                    }
                }
            }
            return false;
        }

        internal class XPropertyChange : ColumnChange
        {
        }

        internal class ColumnChange
        {
            public int? TranId;
            
            public object NewValue;

            public object OldValue;
        }

        private void AggregateRowXPropertyChangedEvent(int rowHandle, string propertyCode, object prevValue, object value)
        {
            if (m_rowXPropertyChangedAggregated == null)
            {
                m_rowXPropertyChangedAggregated = new ();
            }

            if (m_rowXPropertyChangedAggregated.TryGetValue(rowHandle, out var list) == false)
            {
                list = new ();

                m_rowXPropertyChangedAggregated[rowHandle] = list;
            }

            if (list.TryGetValue(propertyCode, out var changes) == false)
            {
                list[propertyCode] = changes = new Data<XPropertyChange>();
            }

            changes.Add(new XPropertyChange { NewValue = value, OldValue = prevValue, TranId = GetTranId()});
        }

        private void AggregateColumnChangedEvent(int rowHandle, CoreDataColumn column, object prevValue, object value)
        {
            if (m_columnChangedAggregated == null)
            {
                m_columnChangedAggregated = new ();
            }

            if (m_columnChangedAggregated.TryGetValue(rowHandle, out var list) == false)
            {
                list = new Map<int, Data<ColumnChange>>();

                m_columnChangedAggregated[rowHandle] = list;
            }

            if (list.TryGetValue(column.ColumnHandle, out var changes) == false)
            {
                list[column.ColumnHandle] = changes = new Data<ColumnChange>();
            }

            changes.Add(new ColumnChange { NewValue = value, OldValue = prevValue, TranId = GetTranId()});
        }

        internal bool HasRowXPropertyChangedEvent => m_rowXPropertyChanged?.HasAny() ?? false;

        internal bool HasRowXPropertyChangingEvent => m_rowXPropertyChanging?.HasAny() ?? false;

        internal void RaiseRowXPropertyChanging(DataRowXPropertyChangingArgs args)
        {
            args.Table = new WeakReference<DataTable>(this);

            RowXPropertyChanging.Raise(args);
        }

        internal void RaiseColumnChanging(DataColumnChangingArgs args)
        {
            args.Table = new WeakReference<DataTable>(this);

            ColumnChanging.Raise(args);
        }

        internal void RaiseRowAdded(DataRowAddedArgs args)
        {
            RowAdded.Raise(args);
            
            args.Dispose();
        }

        internal void RaiseRowAdding(DataRowAddingArgs args)
        {
            RowAdding.Raise(args);
        }

        internal void RaiseRowDeleted(DataRowDeletedArgs args)
        {
            RowDeleted.Raise(args);
            
            args.Dispose();
        }

        internal void RaiseRowDeleting(DataRowDeletingArgs args)
        {
            RowDeleting.Raise(args);
            
            args.Dispose();
        }

        internal void TryRaiseDataRowChanged(int rowHandle)
        {
            if (IsInitializing)
            {
                return;
            }

            if (AreEventsLocked || StateInfo.IsRowEventLocked(rowHandle))
            {
                if (m_rowChangedAggregated == null)
                {
                    m_rowChangedAggregated = new ();
                }

                m_rowChangedAggregated.Add((rowHandle, GetTranId()));
            }
            else
            {
                var dataRowChangedArgs = new DataRowChangedArgs(this, rowHandle);
                
                RaiseRowChanged(dataRowChangedArgs);
                
                dataRowChangedArgs.Dispose();
            }
        }

        private void RaiseRowChanged(DataRowChangedArgs changedArgs)
        {
            DataRowChanged.Raise(changedArgs);
        }

        internal bool HasDisposedHandler => m_disposed?.HasAny() ?? false;
        
        internal bool HasXPropertyChangedHandler => m_tableXPropertyChanged?.HasAny() ?? false;
        
        internal bool HasXPropertyChangingHandler => m_tableXPropertyChanged?.HasAny() ?? false;
        internal bool HasRowMetaDataChangedHandler => m_rowMetaDataChangedEvent?.HasAny() ?? false;
        internal bool HasRollbackHandler => m_dataTableRollbackEvent?.HasAny() ?? false;
        internal bool HasCommitHandler => m_dataTableCommitEvent?.HasAny() ?? false;

        internal bool HasDataRowDeletingHandler => m_rowDeleting?.HasAny() ?? false;

        internal bool HasDataRowDeletedHandler => m_rowDeleted?.HasAny() ?? false;

        internal bool HasDataRowAddingHandler => m_rowAdding?.HasAny() ?? false;

        internal bool HasDataRowAddedHandler => m_rowAdded?.HasAny() ?? false;

        internal bool HasDataColumnChangingHandler => m_columnChanging?.HasAny() ?? false;

        internal bool HasDataColumnChangedHandler => m_columnChanged?.HasAny() ?? false;

        internal bool HasDataRowChangedHandler => m_dataRowChanged?.HasAny() ?? false;
      
        internal interface IColumnChangingInfo
        {
            int SubscribeCount { get;  }
            
            void CopyTo(IColumnChangingInfo targetHandler);
            
            IColumnChangingInfo Clone(DisposableCollection disposables);
            
            void Dispose();

            (bool isCancel, string exceptionMessage, object newValue) Raise(DataTable table, int rowHandleCore, CoreDataColumn column, string columnName, object value, object prevValue);

            (bool isCancel, string exceptionMessage, T? newValue) Raise<T>(DataTable table, int rowHandleCore, CoreDataColumn column, string columnName, T? value, T? prevValue) where T : struct, IComparable, IComparable<T>;
        }

        internal class ColumnChangingInfoTyped<T> : IColumnChangingInfo
        {
            public DataColumnChangingDataEventTyped<T> Event;

            public int SubscribeCount;

            public ColumnChangingInfoTyped(DisposableCollection eventsReferenceHolder)
            {
                Event = new(eventsReferenceHolder);
            }

            void IColumnChangingInfo.CopyTo(IColumnChangingInfo info)
            {
                var columnChangingInfoTyped = (ColumnChangingInfoTyped<T>)info;

                Event.CopyTo(columnChangingInfoTyped.Event);

                columnChangingInfoTyped.SubscribeCount += SubscribeCount;
            }

            IColumnChangingInfo IColumnChangingInfo.Clone(DisposableCollection disposables)
            {
                return Clone(disposables);
            }

            public void Dispose()
            {
                Event = null;
                SubscribeCount = 0;
            }

            public (bool isCancel, string exceptionMessage, object newValue) Raise(DataTable table,
                int rowHandleCore, 
                CoreDataColumn column, 
                string columnName,
                object value,
                object prevValue)
            {
                T convertedValue = default;
                T convertedPrevValue = default;

                var newValueNull = value is null;
                
                if (newValueNull == false)
                {
                    if(value is T tv)
                    {
                        convertedValue = tv;
                    }
                    else
                    {
                        convertedValue = DataRow.TryConvertValue<T>(table, column, value,
                            "Particular column changing handler on converting proposed value");
                    }
                }

                var prevValueNull = prevValue is null;
                if (prevValueNull == false)
                {
                    if(prevValue is T tv)
                    {
                        convertedPrevValue = tv;
                    }
                    else
                    {
                        convertedPrevValue = DataRow.TryConvertValue<T>(table, column, value, "Particular column changing handler on converting previous value");
                    }
                }

                var args = new DataColumnChangingArgsTyped<T>()
                {
                    Table = new WeakReference<DataTable>(table), 
                    NewValue = convertedValue, 
                    PrevValue = convertedPrevValue,
                    ColumnHandle = column.ColumnHandle, 
                    ColumnName = columnName, 
                    RowHandle = rowHandleCore,
                    NewValueIsNull = newValueNull,
                    PrevValueIsNull = prevValueNull
                };
                
                Event.Raise(args);
                
                var cancelEdit = args.IsCancel || string.IsNullOrEmpty(args.ExceptionMessage) == false;

                var newVal = value;

                if (cancelEdit == false && (args.NewValueIsNull != newValueNull || EqualityComparer<T>.Default.Equals(args.NewValue, convertedValue) == false))
                {
                    if (args.NewValueIsNull)
                    {
                        newVal = null;
                    }
                    else
                    {
                        newVal = args.NewValue;
                    }
                }

                return (cancelEdit, args.ExceptionMessage, newVal);
            }

            public (bool isCancel, string exceptionMessage, V? newValue) Raise<V>(DataTable table,
                int rowHandleCore,
                CoreDataColumn column,
                string columnName, 
                V? value,
                V? prevValue) where V : struct, IComparable, IComparable<V>
            {
                T? convertedValue = default;

                if (value.HasValue)
                {
                    V val = value.Value;
                    
                    GenericConverter.ConvertTo(ref val, ref convertedValue);
                }
                
                T? convertedPrevValue = default;

                if (prevValue.HasValue)
                {
                    V val = prevValue.Value;
                    
                    GenericConverter.ConvertTo(ref val, ref convertedPrevValue);
                }
                
                var args = new DataColumnChangingArgsTyped<T>()
                {
                    Table = new WeakReference<DataTable>(table), 
                    NewValue = convertedValue,
                    PrevValue = convertedPrevValue,
                    ColumnHandle = column.ColumnHandle, 
                    ColumnName = columnName, 
                    RowHandle = rowHandleCore,
                    NewValueIsNull = value.HasValue == false,
                    PrevValueIsNull = prevValue.HasValue == false
                };
                
                Event.Raise(args);
                
                var cancelEdit = args.IsCancel || string.IsNullOrEmpty(args.ExceptionMessage) == false;
                
                V? convertedNewValue = value;
                T newValue = args.NewValue;

                if (cancelEdit == false && (args.NewValueIsNull != (value.HasValue == false) || EqualityComparer<T>.Default.Equals(args.NewValue, convertedValue) == false))
                {
                    if (args.NewValueIsNull)
                    {
                        convertedNewValue = default;
                    }
                    else
                    {
                        GenericConverter.ConvertTo(ref newValue, ref convertedNewValue);
                    }
                }

                return (cancelEdit, args.ExceptionMessage, convertedNewValue);
            }

            public ColumnChangingInfoTyped<T> Clone(DisposableCollection eventsReferenceHolder)
            {
                return new ColumnChangingInfoTyped<T>(eventsReferenceHolder) { SubscribeCount = SubscribeCount, Event = (DataColumnChangingDataEventTyped<T>)Event.Clone(eventsReferenceHolder) };
            }

            int IColumnChangingInfo.SubscribeCount => SubscribeCount;
        }

        internal class ColumnChangedInfo
        {
            public DataColumnChangedDataEvent Event;

            public int SubscribeCount;

            public ColumnChangedInfo(DisposableCollection eventsReferenceHolder)
            {
                Event = new (eventsReferenceHolder);
            }

            public ColumnChangedInfo Clone(DisposableCollection eventsReferenceHolder)
            {
                return new ColumnChangedInfo(eventsReferenceHolder) {SubscribeCount = SubscribeCount, Event = (DataColumnChangedDataEvent)Event.Clone(eventsReferenceHolder)};
            }

            public void CopyTo(ColumnChangedInfo info)
            {
                Event.CopyTo(info.Event);

                info.SubscribeCount += SubscribeCount;
            }
        }

        private class CellValueRequestingHandlerInfo
        {
            public NewRowCellValueRequestingDataEvent Event;

            public int SubscribeCount;

            public CellValueRequestingHandlerInfo(DisposableCollection eventsReferenceHolder)
            {
                Event = new (eventsReferenceHolder);
            }

            public CellValueRequestingHandlerInfo Clone(DisposableCollection eventsReferenceHolder)
            {
                return new CellValueRequestingHandlerInfo(eventsReferenceHolder) { SubscribeCount = SubscribeCount, Event = (NewRowCellValueRequestingDataEvent)Event.Clone(eventsReferenceHolder) };
            }

            public void CopyTo(CellValueRequestingHandlerInfo info)
            {
                Event.CopyTo(info.Event);

                info.SubscribeCount += SubscribeCount;
            }
        }

        internal  (bool isCancel, string exceptionMessage, object newValue)? RaiseColumnChangingHandler(CoreDataColumn column, int rowHandle,
            string columnName,
            object value,
            object prevValue)
        {
            if (m_columnChangingHandlers == null)
            {
                return null;
            }

            if (m_columnChangingHandlers.TryGetValue(column.ColumnHandle, out var changingInfo))
            {
                if (changingInfo.SubscribeCount > 0)
                {
                    return changingInfo.Raise(this, rowHandle, column, columnName, value, prevValue);
                }
            }

            return null;
        }

        internal  (bool isCancel, string exceptionMessage, T? newValue)? RaiseColumnChangingHandler<T>(CoreDataColumn column, int rowHandle,
            string columnName,
            T? value,
            T? prevValue) where T : struct, IComparable, IComparable<T>
        {
            if (m_columnChangingHandlers == null)
            {
                return null;
            }

            if (m_columnChangingHandlers.TryGetValue(column.ColumnHandle, out var changingInfo))
            {
                if (changingInfo.SubscribeCount > 0)
                {
                    return changingInfo.Raise(this, rowHandle, column, columnName, value, prevValue);
                }
            }

            return null;
        }

        internal bool HasCustomDataColumnColumnChangingHandler(int columnHandle)
        {
            return m_columnChangingHandlers?.GetOrDefault(columnHandle)?.SubscribeCount > 0;
        }

        internal bool HasCustomDataColumnChangedHandler(int columnHandle)
        {
            return m_columnChangedHandlers?.GetOrDefault(columnHandle)?.SubscribeCount > 0;
        }

        public bool SubscribeColumnChanging<T>(string columnName, Action<IDataColumnChangingTypedEventArgs<T>, string> eventHandler, string context = null)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
            {
                if (m_columnChangingHandlers == null)
                {
                    m_columnChangingHandlers = new Map<int, IColumnChangingInfo>();
                }

                if (m_columnChangingHandlers.TryGetValue(column.ColumnHandle, out var handler) == false)
                {
                    var columnChangingHandler = new ColumnChangingInfoTyped<T>(m_disposables) { SubscribeCount = 1 };

                    columnChangingHandler.Event.Subscribe(eventHandler, context);

                    m_columnChangingHandlers[column.ColumnHandle] = columnChangingHandler;
                }
                else
                {
                    var columnChangingInfoTyped = (ColumnChangingInfoTyped<T>)handler;

                    columnChangingInfoTyped.Event.Subscribe(eventHandler);
                    columnChangingInfoTyped.SubscribeCount++;
                }

                return true;
            }

            return false;
        }

        public bool UnsubscribeColumnChanging<T>(string columnName, Action<IDataColumnChangingTypedEventArgs<T>, string> eventHandler)
        {
            if (m_columnChangingHandlers == null)
            {
                return false;
            }

            if (DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
            {
                if (m_columnChangingHandlers.TryGetValue(column.ColumnHandle, out var handler))
                {
                    var columnChangingInfoTyped = (ColumnChangingInfoTyped<T>)handler;
                    
                    if (columnChangingInfoTyped.SubscribeCount > 0)
                    {
                        columnChangingInfoTyped.SubscribeCount--;
                        columnChangingInfoTyped.Event.Unsubscribe(eventHandler);

                        return true;
                    }
                }
            }

            return false;
        }

        public bool SubscribeColumnChanged(string columnName, Action<IDataColumnChangedEventArgs, string> eventHandler, string context = null)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
            {
                if (m_columnChangedHandlers == null)
                {
                    m_columnChangedHandlers = new Map<int, ColumnChangedInfo>();
                }

                if (m_columnChangedHandlers.TryGetValue(column.ColumnHandle, out var handler) == false)
                {
                    var columnChangedHandler = new ColumnChangedInfo(m_disposables) {  SubscribeCount = 1 };

                    columnChangedHandler.Event.Subscribe(eventHandler, context);

                    m_columnChangedHandlers[column.ColumnHandle] = columnChangedHandler;
                }
                else
                {
                    handler.Event.Subscribe(eventHandler);
                    handler.SubscribeCount++;
                }

                return true;
            }

            return false;
        }

        public bool UnsubscribeColumnChanged(string columnName, Action<IDataColumnChangedEventArgs, string> eventHandler)
        {
            if (m_columnChangedHandlers == null)
            {
                return false;
            }

            if (DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
            {
                if (m_columnChangedHandlers.TryGetValue(column.ColumnHandle, out var handler))
                {
                    if (handler.SubscribeCount > 0)
                    {
                        handler.SubscribeCount--;
                        handler.Event.Unsubscribe(eventHandler);

                        return true;
                    }
                }
            }

            return false;
        }

        internal bool HasCellValueRequestingHandler(int columnHandle)
        {
            return m_emptyRowCellValueRequestingHandlers?.GetOrDefault(columnHandle)?.SubscribeCount > 0;
        }

        internal bool RaiseCellValueRequesting(int columnHandle, EmptyRowCellValueRequestingArgs args)
        {
            if (m_emptyRowCellValueRequestingHandlers == null)
            {
                return false;
            }

            if (m_emptyRowCellValueRequestingHandlers.TryGetValue(columnHandle, out var changigInfo))
            {
                if (changigInfo.SubscribeCount > 0)
                {
                    changigInfo.Event.Raise(args);
                }
            }

            return false;
        }

        public bool SubscribeCellValueRequesting(string columnName, Func<INewRowCellValueRequestingArgs, string, bool> eventHandler, string context = null)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
            {
                if (m_emptyRowCellValueRequestingHandlers == null)
                {
                    m_emptyRowCellValueRequestingHandlers = new Map<int, CellValueRequestingHandlerInfo>();
                }

                if (m_emptyRowCellValueRequestingHandlers.TryGetValue(column.ColumnHandle, out var handler) == false)
                {
                    var requestinHandlerInfo = new CellValueRequestingHandlerInfo(m_disposables) { SubscribeCount = 1 };

                    requestinHandlerInfo.Event.Subscribe(eventHandler, context);

                    m_emptyRowCellValueRequestingHandlers[column.ColumnHandle] = requestinHandlerInfo;
                }
                else
                {
                    handler.Event.Subscribe(eventHandler);
                    handler.SubscribeCount++;
                }

                return true;
            }

            return false;
        }

        public bool UnsubscribeCellValueRequesting(string columnName, Func<INewRowCellValueRequestingArgs, string, bool> eventHandler)
        {
            if (m_emptyRowCellValueRequestingHandlers == null)
            {
                return false;
            }

            if (DataColumnInfo.ColumnMappings.TryGetValue(columnName, out var column))
            {
                if (m_emptyRowCellValueRequestingHandlers.TryGetValue(column.ColumnHandle, out var handler))
                {
                    if (handler.SubscribeCount > 0)
                    {
                        handler.SubscribeCount--;
                        handler.Event.Unsubscribe(eventHandler);

                        return true;
                    }
                }
            }

            return false;
        }

        public DataTableTransactionCommitEvent TransactionCommit => IsDisposed ? null : m_dataTableCommitEvent ??= new (m_disposables);
        public DataTableTransactionRollbackEvent TransactionRollback => IsDisposed ? null : m_dataTableRollbackEvent ??= new (m_disposables);
        public RowXPropertyChangedDataEvent RowXPropertyChanged => IsDisposed ? null : m_rowXPropertyChanged ??= new (m_disposables);
        public DataRowMetaDataChangedEvent RowMetadataChangedEvent => IsDisposed ? null : m_rowMetaDataChangedEvent ??= new (m_disposables);
        public DataTableXPropertyChangedDataEvent XPropertyChanged => IsDisposed ? null : m_tableXPropertyChanged ??= new (m_disposables);
        public DataTableXPropertyChangingDataEvent XPropertyChanging => IsDisposed ? null : m_tableXPropertyChanging ??= new (m_disposables);
        public RowXPropertyChangingDataEvent RowXPropertyChanging => IsDisposed ? null : m_rowXPropertyChanging ??= new (m_disposables);
        public DataColumnChangedDataEvent ColumnChanged => IsDisposed ? null : m_columnChanged ??= new (m_disposables);
        public DataColumnChangingDataEvent ColumnChanging => IsDisposed ? null : m_columnChanging ??= new (m_disposables);
        public DataRowAddedDataEvent RowAdded => IsDisposed ? null : m_rowAdded ??= new (m_disposables);
        public DataRowAddingDataEvent RowAdding => IsDisposed ? null : m_rowAdding ??= new (m_disposables);
        public DataRowDeletedDataEvent RowDeleted => IsDisposed ? null : m_rowDeleted ??= new (m_disposables);
        public DataRowDeletingDataEvent RowDeleting => IsDisposed ? null : m_rowDeleting ??= new (m_disposables);
        public DataRowChangedDataEvent DataRowChanged => IsDisposed ? null : m_dataRowChanged ??= new (m_disposables);
        public DataTableDisposedDataEvent Disposed => m_disposed ??= new (m_disposables);
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowMetaDataChangedEvent IDataTable.RowMetaDataChangedEvent => RowMetadataChangedEvent;
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableXPropertyChangingDataEvent IDataTable.XPropertyChanging => XPropertyChanging;
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableXPropertyChangedDataEvent IDataTable.XPropertyChanged => XPropertyChanged;
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowXPropertyChangedDataEvent IDataTable.RowXPropertyChanged => RowXPropertyChanged;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowXPropertyChangingDataEvent IDataTable.RowXPropertyChanging => RowXPropertyChanging;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataColumnChangedDataEvent IDataTable.ColumnChanged => ColumnChanged;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataColumnChangingDataEvent IDataTable.ColumnChanging => ColumnChanging;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowAddedDataEvent IDataTable.RowAdded => RowAdded;
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowAddingDataEvent IDataTable.RowAdding => RowAdding;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowDeletedDataEvent IDataTable.RowDeleted => RowDeleted;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowDeletingDataEvent IDataTable.RowDeleting => RowDeleting;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRowChangedDataEvent IDataTable.DataRowChanged => DataRowChanged;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IMaxColumnLenConstraintDataEvent IDataTable.MaxColumnLenConstraint => MaxColumnLenConstraint;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTableDisposedDataEvent IDataTable.Disposed => Disposed;
        
        
        private void RaiseMultiRowsDeleted()
        {
            if (m_rowDeletedHandlesAggregated?.Count > 0 && HasDataRowDeletedHandler)
            {
                var rowHandles = new Data<int>();
                
                rowHandles.Ensure(m_rowDeletedHandlesAggregated.Count);

                for (int i = 0; i < m_rowDeletedHandlesAggregated.Count; i++)
                {
                    rowHandles[i] = m_rowDeletedHandlesAggregated[i].rowHandle;
                }
                
                var deletedArgs = new DataRowDeletedArgs(this, rowHandles);

                m_rowDeletedHandlesAggregated.Dispose();
                
                m_rowDeletedHandlesAggregated = null;

                RaiseRowDeleted(deletedArgs);
            }
        }

        private void RaiseMultiRowsAdded()
        {
            if (m_addedRowHandlesAggregated?.Count > 0 && HasDataRowAddedHandler)
            {
                var rowHandles = new Data<int>();
                
                rowHandles.Ensure(m_addedRowHandlesAggregated.Count);

                for (int i = 0; i < m_addedRowHandlesAggregated.Count; i++)
                {
                    rowHandles[i] = m_addedRowHandlesAggregated[i].rowHandle;
                }

                var rowAddingArgs = new DataRowAddedArgs(this, rowHandles);

                m_addedRowHandlesAggregated.Dispose();
                
                m_addedRowHandlesAggregated = null;

                RaiseRowAdded(rowAddingArgs);
            }
        }

        private void RaiseMultiRowsChanged()
        {
            if (m_rowChangedAggregated?.Count > 0 && (m_dataRowChanged?.HasAny() ?? false))
            {
                var rowHandles = new Data<int>();
                
                rowHandles.Ensure(m_rowChangedAggregated.Count);

                for (int i = 0; i < m_rowChangedAggregated.Count; i++)
                {
                    rowHandles[i] = m_rowChangedAggregated[i].rowHandle;
                }
                
                var changedArgs = new DataRowChangedArgs(this, rowHandles);

                m_rowChangedAggregated.Dispose();
                m_rowChangedAggregated = null;

                RaiseRowChanged(changedArgs);
                
                changedArgs.Dispose();
            }
        }

        private void RaiseMultiColumnChanged()
        {
            if (m_columnChangedAggregated?.Count > 0 && (m_columnChanged?.HasAny() ?? false))
            {
                var runToColumnChanges = m_columnChangedAggregated.ToData();
                
                m_columnChangedAggregated.Dispose();
                m_columnChangedAggregated = null;

                foreach (var runColumnChange in runToColumnChanges)
                {
                    var columnChangedArgs = new DataColumnChangedArgs(this, rowHandle: runColumnChange.Key, dict: runColumnChange.Value);

                    RaiseColumnChanged(columnChangedArgs);
                }

                runToColumnChanges.Dispose();
            }
        }

        private void RaiseMultiRowXPropertyChanged()
        {
            if (m_rowXPropertyChangedAggregated?.Count > 0 && HasRowXPropertyChangedEvent)
            {
                var runToXPropertyChanges = m_rowXPropertyChangedAggregated.ToData();
                m_rowXPropertyChangedAggregated.Dispose();
                m_rowXPropertyChangedAggregated = null;

                foreach (var runColumnChange in runToXPropertyChanges)
                {
                    var rowXPropertyChangedArgs = new DataRowXPropertyChangedArgs(this, rowHandle: runColumnChange.Key, dict: runColumnChange.Value);

                    RaiseRowXPropertyChanged(rowXPropertyChangedArgs);
                }

                runToXPropertyChanges.Dispose();
            }
        }
        
        internal void RaiseMultiRowXPropertyChanged(int rowHandle)
        {
            if (m_rowXPropertyChangedAggregated?.Count > 0 && HasRowXPropertyChangedEvent)
            {
                if (m_rowXPropertyChangedAggregated.TryGetValue(rowHandle, out var runToXPropertyChanges))
                {
                    m_rowXPropertyChangedAggregated.Remove(rowHandle);
                    
                    var rowXPropertyChangedArgs = new DataRowXPropertyChangedArgs(this, rowHandle, dict: runToXPropertyChanges);

                    RaiseRowXPropertyChanged(rowXPropertyChangedArgs);
                    
                    rowXPropertyChangedArgs.Dispose();
                }
            }
        }

        internal void RaiseMultiColumnChanged(int rowHandleCore)
        {
            if (m_columnChangedAggregated?.Count > 0 && ((m_columnChanged?.HasAny() ?? false) || m_columnChangedHandlers?.Count > 0))
            {
                if (m_columnChangedAggregated.TryGetValue(rowHandleCore, out var runToColumnChanges))
                {
                    m_columnChangedAggregated.Remove(rowHandleCore);

                    var columnChangedHandlers = m_columnChangedHandlers;
                    
                    if (columnChangedHandlers is not null)
                    {
                        foreach (var change in runToColumnChanges)
                        {
                            var columnHandle = change.Key;
                            
                            if (columnChangedHandlers.TryGetValue(columnHandle, out var changigInfo))
                            {
                                if (changigInfo.SubscribeCount > 0)
                                {
                                    foreach (var colChange in change.Value)
                                    {
                                        changigInfo.Event.Raise(new DataColumnChangedArgs(this, rowHandleCore, columnHandle, GetTranId(), colChange.OldValue, colChange.NewValue));
                                    }
                                }
                            }
                        }
                    }
                    
                    RaiseColumnChanged(new DataColumnChangedArgs(this, rowHandle: rowHandleCore, dict: runToColumnChanges));

                    runToColumnChanges.Dispose();
                }
            }
        }

        internal void RaiseMultiRowsChanged(int rowHandleCore)
        {
            if (m_rowChangedAggregated?.Count > 0 && (m_dataRowChanged?.HasAny() ?? false))
            {
                var index = m_rowChangedAggregated.FindIndex(rowHandleCore, (r) => r.rowHandle);
                
                if (index >= 0)
                {
                    m_rowChangedAggregated.RemoveAll(rowHandleCore, (r) => r.rowHandle);

                    var rowHandles = new Data<int> { rowHandleCore };
                    
                    var changedArgs = new DataRowChangedArgs(this, rowHandles);

                    RaiseRowChanged(changedArgs);
                    
                    rowHandles.Dispose();
                }
            }
        }
    }
}
