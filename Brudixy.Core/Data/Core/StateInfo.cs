using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class StateInfo
    {
        internal readonly CoreDataTable DataTable;

        public StateInfo(CoreDataTable table)
        {
            DataTable = table;
        }

        public void OnRowsAnnotationChange(int row)
        {
            RowStates.IncRowAnnotationAge(row);
        }

        private RowStateInfoDataItem m_rowStates;
        
        [NotNull]
        public RowStateInfoDataItem RowStates => m_rowStates ??= CreateRowStatesStorage();

        protected virtual RowStateInfoDataItem CreateRowStatesStorage()
        {
            return new (DataTable);
        }

        private RowXPropDataItem m_rowXProps;
        
        [NotNull]
        public RowXPropDataItem RowXProps => m_rowXProps ??= CreateRowXPropsStorage();

        protected virtual RowXPropDataItem CreateRowXPropsStorage()
        {
            return new(DataTable);
        }

        private ICollection<int> m_emptyRowSlotsQueue;
        
        [NotNull]
        public ICollection<int> EmptyRowSlotsQueue => m_emptyRowSlotsQueue ??= CreateEmptyRowSlotsQueueStorage();
        
        protected virtual ICollection<int> CreateEmptyRowSlotsQueueStorage(IEnumerable<int> source = null)
        {
            if(source != null)
            {
                return new Set<int>(source);
            }
            
            return new Set<int>();
        }

        private IRandomAccessData<int> m_rowHandles;
        
        public IRandomAccessData<int> RowHandles => m_rowHandles ??= CreateRowHandlesStorage();

        protected virtual IRandomAccessData<int> CreateRowHandlesStorage(IEnumerable<int> source = null)
        {
            if(source != null)
            {
                return new Data<int>(source);
            }
            
            return new Data<int>();
        }

        public int RowStorageCount => RowStates.Storage.Count;

        public bool IsDisposed;

        public int RowCount => RowHandles.Count;
        
        [CanBeNull]
        internal Map<int, StackTransactionInfo> RowTransactionMap;
        
        
        [CanBeNull]
        internal TableStackTransactionInfo TableTransactionInfo;
        
        internal int MaxTranId;

        public void LockRowEvents(int rowHandle)
        {
            RowStates.LockRowEvents(rowHandle);
        }

        public void UnlockRowEvents(int rowHandle)
        {
            RowStates.UnlockRowEvents(rowHandle);
        }

        public bool IsRowEventLocked(int rowHandle)
        {
            return RowStates.RowEventLocked(rowHandle);
        }

        public void SetAdded(int rowHandle, int? tranId)
        {
            RowStates.SetState(rowHandle, RowState.Added, tranId);
        }

        public void SetModified(int rowHandle, int? tranId)
        {
            RowStates.SetState(rowHandle, RowState.Modified, tranId);
        }
        
        public void SetUnchanged(int rowHandle, int? tranId)
        {
            RowStates.SetState(rowHandle, RowState.Unchanged, tranId);
        }

        public bool SetDeleted(int rowHandle, int? tranId)
        {
            var wasSet = RowStates.SetState(rowHandle, RowState.Deleted, tranId);

            if (wasSet)
            {
                MakeRowDeleted(rowHandle);

                return true;
            }

            return false;
        }

        private void MakeRowDeleted(int rowHandle)
        {
            var index = RowHandles.BinarySearch(rowHandle, 0, RowHandles.Count);

            RowHandles.RemoveAt(index);

            EmptyRowSlotsQueue.Remove(rowHandle);
        }
        
        private void MakeRowDetached(int rowHandle)
        {
            var index = RowHandles.BinarySearch(rowHandle, 0, RowHandles.Count);

            if (index >= 0)
            {
                RowHandles.RemoveAt(index);
            }

            EmptyRowSlotsQueue.Add(rowHandle);
        }

        public int GetNewRowHandle(CoreDataTable table)
        {
            var rowHandle = RowStorageCount;

            var rowHandles = RowHandles;
            var rowStates = RowStates;
            
            if (m_emptyRowSlotsQueue is { Count: > 0 })
            {
                var freeRowHandle = m_emptyRowSlotsQueue.Last();

                rowHandle = freeRowHandle;

                m_emptyRowSlotsQueue.Remove(freeRowHandle);

                var insertPos = rowHandles.BinarySearch(freeRowHandle,0, rowHandles.Count);
                
                rowHandles.Insert(~insertPos, freeRowHandle);
                rowStates.Storage[freeRowHandle] = RowStateInfo.New();
            }
            else
            {
                rowHandles.Add(rowHandle);
                rowStates.Storage.Add(RowStateInfo.New());
            }

            return rowHandle;
        }

        public void RemoveAllRows()
        {
            EmptyRowSlotsQueue.Clear();
            RowStates.Clear();
            RowXProps.Clear();
            RowHandles.Clear();
        }

        public void RemoveRow(int rowHandle)
        {
            MakeRowDetached(rowHandle);

            if(RowTransactionMap is not null && RowTransactionMap.TryGetValue(rowHandle, out var lockInfo))
            {
                lockInfo.Clear();
            }

            RowStates.SetState(rowHandle, RowState.Unchanged, null);

            var rowXProps = RowXProps;
            
            if (rowHandle < rowXProps.Storage.Count)
            {
                var props = rowXProps.Storage[rowHandle];
                
                props?.Dispose();

                rowXProps.Storage[rowHandle] = null;
            }
        }

        public bool DeleteRow(int rowHandle, int? tranId)
        {
            var rowStates = RowStates;
            
            rowStates.Ensure(rowHandle + 1);
            
            var rowState = rowStates.Storage[rowHandle]?.RowState ?? RowState.Disposed;

            if (rowState == RowState.Added)
            {
                if (tranId == null)
                {
                    RemoveRow(rowHandle);

                    return true;
                }

                TableTransactionInfo?.AddDetachedRow(rowHandle);
                
                var wasSet = rowStates.SetState(rowHandle, RowState.Detached, tranId);

                if (wasSet)
                {
                    MakeRowDetached(rowHandle);

                    return true;
                }
            }
            else
            {
                var wasDeleted = SetDeleted(rowHandle, tranId);

                if (wasDeleted)
                {
                    return true;
                }
            }

            return false;
        }

        public RowState GetRowState(int rowHandle)
        {
            var rowStates = RowStates;
            
            if (rowStates.Storage.Count == 0)
            {
                return RowState.Unchanged;
            }

            if (m_emptyRowSlotsQueue?.Contains(rowHandle) ?? false)
            {
                return RowState.Detached;
            }

            if (rowHandle >= rowStates.Storage.Count)
            {
                return RowState.Unchanged;
            }
         
            return rowStates.Storage[rowHandle]?.RowState ?? RowState.Disposed;
        }

        public virtual void Merge(StateInfo source)
        {
            if (source.RowStates.Storage.Count > 0)
            {
                m_rowStates = source.RowStates.Copy(DataTable);
            }

            m_emptyRowSlotsQueue = CreateEmptyRowSlotsQueueStorage(source.EmptyRowSlotsQueue);

            m_rowHandles = CreateRowHandlesStorage(source.RowHandles);

            MaxTranId = source.MaxTranId;

            m_rowXProps = source.RowXProps.Copy(DataTable);
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            m_rowXProps?.Dispose();

            if (m_emptyRowSlotsQueue is IDisposable ed)
            {
                ed.Dispose();
            }

            m_rowStates?.Dispose();

            if (m_rowHandles is IDisposable d)
            {
                d.Dispose();
            }

            IsDisposed = true;
        }

        public bool IsNotDeleted(int rowHandle)
        {
            var rowStates = RowStates;
            
            if (rowStates.Storage.Count == 0 || rowHandle >= rowStates.Storage.Count)
            {
                return true;
            }

            return rowStates.Storage[rowHandle]?.RowState != RowState.Deleted;
        }

        public bool IsNotDeletedAndRemoved(int rowHandle)
        {
            var rowStates = RowStates;
            
            if (rowStates.Storage.Count == 0 || rowHandle >= rowStates.Storage.Count)
            {
                return true;
            }

            var state = rowStates.Storage[rowHandle]?.RowState ?? RowState.Detached;
            
            return state != RowState.Deleted && state != RowState.Detached && (EmptyRowSlotsQueue?.Contains(rowHandle) ?? false) == false;
        }

        public bool IsNotRemoved(int rowHandle)
        {
            if (m_emptyRowSlotsQueue == null)
            {
                return true;
            }

            return m_emptyRowSlotsQueue.Contains(rowHandle) == false;
        }

        public void RejectChanges(CoreDataTable table)
        {
            var rowStates = RowStates;
            
            if (rowStates.Storage.Count == 0)
            {
                return;
            }

            for (int rowHandle = 0; rowHandle < RowStorageCount; rowHandle++)
            {
                var recordState = rowStates.Storage[rowHandle]?.RowState ?? RowState.Detached;

                if (recordState == RowState.Added)
                {
                    var wasSet = rowStates.SetState(rowHandle, RowState.Deleted, null);

                    AcceptChangesCore(table, rowHandle, null);

                    if (wasSet)
                    {
                        MakeRowDetached(rowHandle);
                    }
                }
                else if(recordState != RowState.Detached)
                {
                    rowStates.SetState(rowHandle, RowState.Unchanged, null);
                    
                    RejectChangesExtendedProperties(rowHandle);
                }
            }

            IncAllRowAges();
        }

        public void IncAllRowAges()
        {
            RowStates.IncAllRowAges(RowHandles);
        }

        public bool AcceptChangesRow(CoreDataTable table, int rowHandle, int? tranId)
        {
            return AcceptChangesCore(table, rowHandle, tranId);
        }

        public bool FlushDetached()
        {
            bool any = false;

            var transactionInfo = TableTransactionInfo;
            
            if (transactionInfo != null)
            {
                foreach (var rowHandle in transactionInfo.DetachedRows)
                {
                    RemoveRow(rowHandle);

                    any = true;
                }
                
                transactionInfo.ClearDetachedRows();
            }

            return any;
        }

        public bool AcceptChangesAll(CoreDataTable table)
        {
            bool any = false;

            for (int rowHandle = 0; rowHandle < RowStorageCount; rowHandle++)
            {
                any = AcceptChangesCore(table, rowHandle, null) || any;
            }

            return any;
        }

        private bool AcceptChangesCore(CoreDataTable table, int rowHandle, int? tranId)
        {
            var rowStates = RowStates;
            
            if (rowHandle >= rowStates.Storage.Count)
            {
                return false;
            }

            var rowRecordState = rowStates.Storage[rowHandle]?.RowState ?? RowState.Detached;
            
            var deleted = rowRecordState == RowState.Deleted;

            if (deleted)
            {
                table.RemoveRowFromIndexes(rowHandle);

                RemoveRow(rowHandle);

                return rowStates.SetState(rowHandle, RowState.Detached, tranId);
            }

            if (rowRecordState != RowState.Detached)
            {
                var ok = rowStates.SetState(rowHandle, RowState.Unchanged, tranId);

                AcceptChangesExtendedProperties(rowHandle);

                return ok;
            }

            return false;
        }

        private void RejectChangesExtendedProperties(int rowHandle)
        {
            RowXProps.RejectChanges(rowHandle);
        }

        private void AcceptChangesExtendedProperties(int rowHandle)
        {
            RowXProps.AcceptChanges(rowHandle);
        }

        public void SetRowExtendedProperty(int rowHandle,
            string propertyName, 
            object value, 
            object prevValue,
            int? tranId)
        {
            var wasSet = RowXProps.SetValue(rowHandle, propertyName, value, prevValue, tranId);

            if (wasSet)
            {
                if (GetRowState(rowHandle) == RowState.Unchanged)
                {
                    SetModified(rowHandle, tranId);
                }

                if (tranId.HasValue)
                {
                    SetTransactionRowChanged(rowHandle);

                    SetRowTransactionXPropChanged(rowHandle, propertyName);
                }
            }
        }
        
        public void SilentlySetRowExtendedProperty(int rowHandle, string propertyName, string value)
        {
            RowXProps.SilentlySetValue(rowHandle, propertyName, value);
        }

        public IReadOnlyDictionary<string, object> GetExtendedProperties(int rowHandle)
        {
            var rowXProps = RowXProps;
            
            if (rowHandle >= rowXProps.Storage.Count)
            {
               return null;
            }

            var props = rowXProps.Storage[rowHandle];

            if (props == null)
            {
                return null;
            }

            return props;
        }
        
        public bool HasXProperty(int rowHandle, string property)
        {
            var rowXProps = RowXProps;
            
            if (rowHandle >= rowXProps.Storage.Count)
            {
                return false;
            }

            var props = rowXProps.Storage[rowHandle];

            if (props == null)
            {
                return false;
            }

            return props.ContainsKey(property);
        }

        [CanBeNull]
        public object GetExtendedProperty(int rowHandle, string property, bool original = false)
        {
            if (original)
            {
                return RowXProps.GetOriginalValue(rowHandle, property);
            }

            return RowXProps.GetData(rowHandle, property);
        }

        public IEnumerable<string> GetChangedXProperties(int rowHandle)
        {
            var rowXProps = RowXProps;
            
            if (rowHandle >= rowXProps.Storage.Count)
            {
                yield break;
            }

            var props = rowXProps.Storage[rowHandle];

            if (props == null)
            {
                yield break;
            }

            foreach (var changedPropertyName in rowXProps.GetChangedPropertyNames(rowHandle))
            {
                yield return changedPropertyName;
            }
        }

        public StackTransactionInfo StartRowTransaction(int rowHandleCore)
        {
            if (RowTransactionMap is null)
            {
                RowTransactionMap = new();
            }

            if (RowTransactionMap.TryGetValue(rowHandleCore, out var lockInfo) == false)
            {
                RowTransactionMap[rowHandleCore] = lockInfo = new StackTransactionInfo();
            }

            lockInfo.PushTransaction(MaxTranId);

            return lockInfo;
        }

        public int StartTableTransaction(int parentTransaction = 0)
        {
            if (TableTransactionInfo is null)
            {
                TableTransactionInfo = new ();
            }

            var max = Math.Max(MaxTranId, parentTransaction) + 1;
            
            TableTransactionInfo.PushTransaction(max);

            MaxTranId = max;
            
            return max;
        }

        public void CommitTableTransaction()
        {
            var transactionInfo = TableTransactionInfo;
            
            transactionInfo?.PopTransaction();

            if (transactionInfo != null && transactionInfo.HasAny() == false)
            {
                FlushDetached();
                
                transactionInfo.Clear();

                if (RowTransactionMap != null)
                {
                    foreach (var stackTransactionInfo in RowTransactionMap)
                    {
                        stackTransactionInfo.Value.Clear();
                    }

                    RowTransactionMap.Clear();
                }
            }
        }

        public IEnumerable<int> GetRowsInTransaction()
        {
            if (RowTransactionMap == null)
            {
               yield break;
            }

            foreach (var transactionInfo in RowTransactionMap)
            {
                if (transactionInfo.Value.HasAny())
                {
                    yield return transactionInfo.Key;
                }
            }
        }
        
        public bool IsGoingToCommitRowSourceTransaction(int rowHandle)
        {
            if (RowTransactionMap == null)
            {
                return true;
            }

            if (RowTransactionMap.TryGetValue(rowHandle, out var lockInfo))
            {
                return lockInfo.Last();
            }

            return true;
        }

        public void RollbackRowTransaction(int rowHandle)
        {
            if (RowTransactionMap == null)
            {
                return;
            }

            if (RowTransactionMap.TryGetValue(rowHandle, out var lockInfo))
            {
                lockInfo.RollbackTransaction();
            }
        }

        public CoreDataTable.IndexAction RollbackStateInfoTransaction(int rowHandle)
        {
            CoreDataTable.IndexAction indexAction = CoreDataTable.IndexAction.None;
            
            var tranId = GetCurrentTableTranId();

            if (tranId.HasValue)
            {
                var rowStates = RowStates;
                
                var valueByRef = rowStates.Storage[rowHandle]?.RowState;

                var wasDeleted = RowState.Deleted == valueByRef;
                var wasDetached = (m_emptyRowSlotsQueue?.Contains(rowHandle) ?? false);
                var wasAdded = RowState.Added == valueByRef;

                var rejectEditRow = rowStates.RollbackRowTransaction(rowHandle, tranId.Value);

                if (rejectEditRow)
                {
                    if (wasDeleted || wasDetached)
                    {
                        var rowHandles = RowHandles;
                        
                        var insertPos = rowHandles.BinarySearch(rowHandle, 0, rowHandles.Count);

                        rowHandles.Insert(~insertPos, rowHandle);

                        TableTransactionInfo?.RemoveDetachedRow(rowHandle);

                        indexAction = CoreDataTable.IndexAction.Restore;
                    }
                    else if (wasAdded)
                    {
                        MakeRowDeleted(rowHandle);

                        rowStates.SetState(rowHandle, RowState.Detached, null);

                        TableTransactionInfo?.AddDetachedRow(rowHandle);

                        indexAction = CoreDataTable.IndexAction.Clear;
                    }
                }
            }

            return indexAction;
        }

        public void CommitRowTransaction(int rowHandle, bool stopLoggingTran)
        {
            if (RowTransactionMap == null)
            {
                return;
            }

            if (RowTransactionMap.TryGetValue(rowHandle, out var lockInfo) == false)
            {
                return;
            }
            
            if (stopLoggingTran)
            {
                RowStates.StopLoggingTransactionChanges(rowHandle);
                RowXProps.StopLoggingTransactionChanges(rowHandle);
            }

            lockInfo.PopTransaction();
        }

        public void SetRowTransactionColumnChanged(int rowHandle, int columnHandle)
        {
            StackTransactionInfo lockInfo = null;
            if ((RowTransactionMap?.TryGetValue(rowHandle, out lockInfo) ?? false) && lockInfo.HasStarted)
            {
                lockInfo.AddChangedItem(columnHandle);
            }
            else
            {
                lockInfo = StartRowTransaction(rowHandle);
                
                lockInfo.AddChangedItem(columnHandle);
            }
        }
        
        public void SetRowTransactionXPropChanged(int rowHandle, string xProperty)
        {
            StackTransactionInfo lockInfo = null;
            if ((RowTransactionMap?.TryGetValue(rowHandle, out lockInfo) ?? false) && lockInfo.HasStarted)
            {
                lockInfo.AddChangedXProp(xProperty);
            }
            else
            {
                lockInfo = StartRowTransaction(rowHandle);
                
                lockInfo.AddChangedXProp(xProperty);
            }
        }

        public int? GetCurrentTableTranId()
        {
            if (TableTransactionInfo?.HasAny() ?? false)
            {
                return TableTransactionInfo.CurrentTranId;
            }

            return null;
        }

        public IReadOnlyCollection<int> GetRowTransactionChanges(int rowHandle)
        {
            var editLockInfo = RowTransactionMap?.GetOrDefault(rowHandle);

            if (editLockInfo != null)
            {
                return editLockInfo.GetChangedItems().ToSet();
            }

            return Array.Empty<int>();
        }
        
        public IReadOnlyCollection<string> GetRowTransactionXPropChanges(int rowHandle)
        {
            var editLockInfo = RowTransactionMap?.GetOrDefault(rowHandle);

            if (editLockInfo != null)
            {
                var allChangedColumns = editLockInfo.GetChangedXProps().ToSet();

                return allChangedColumns;
            }

            return Array.Empty<string>();
        }
        
        public IReadOnlyCollection<IDataEditTransaction> GetRowDependantTransactions(int rowHandle)
        {
            var editLockInfo = RowTransactionMap?.GetOrDefault(rowHandle);

            if (editLockInfo != null)
            {
                var dataEditTransactions = editLockInfo.GetTransactions().ToData();

                return  dataEditTransactions;
            }

            return Array.Empty<IDataEditTransaction>();
        }

        public  IReadOnlyCollection<int>  GetTransactionChangedRowList()
        {
            if (TableTransactionInfo == null || TableTransactionInfo.HasAny() == false)
            {
                return Array.Empty<int>();
            }

            var changedRows = TableTransactionInfo.GetChangedItems().ToSet();

            return changedRows;
        }

        public void SetTransactionRowChanged(int rowHandle)
        {
            TableTransactionInfo?.AddChangedItem(rowHandle);
        }

        public bool IsTableInTransaction => TableTransactionInfo?.HasAny() ?? false;
        
        public bool IsGoingToCommitSourceTableTransaction()
        {
            if (TableTransactionInfo == null)
            {
                return false;
            }

            return TableTransactionInfo.Last();
        }

        public void RollbackTableTransaction()
        {
            TableTransactionInfo?.RollbackTransaction();

            if (TableTransactionInfo != null && TableTransactionInfo.HasAny() == false)
            {
                TableTransactionInfo.ClearDetachedRows();
            }
        }

        public void AddDependantTransactions(int rowHandle, IDataEditTransaction dataEditTransactions)
        {
            StackTransactionInfo lockInfo = null;
            if (RowTransactionMap?.TryGetValue(rowHandle, out lockInfo) == false || lockInfo == null)
            {
                return;
            }
            
            lockInfo.AddDependantTransactions(dataEditTransactions);
        }

        public void AddTableDependantTransactions(IDataEditTransaction childRowTransactions)
        {
            if (TableTransactionInfo != null)
            {
                TableTransactionInfo.AddDependantTransactions(childRowTransactions);
            }
        }

        public IEnumerable<IDataEditTransaction> GetTableDependantTransactions()
        {
            if (TableTransactionInfo != null)
            {
                return TableTransactionInfo.GetTransactions();
            }
            
            return Enumerable.Empty<IDataEditTransaction>();
        }

        public uint GetRowAnnotationAge(int rowHandle)
        {
            return m_rowStates?.Storage[rowHandle]?.AnnAge ?? 0;
        }

        public void UpdateRowAge(int rowHandle)
        {
            RowStates.IncRowAge(rowHandle);
        }

        public uint GetRowAge(int rowHandle)
        {
            return RowStates.Storage[rowHandle]?.Age ?? 0;
        }
    }
}