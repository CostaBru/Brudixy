using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        protected void EnsureMaxTranId(int maxTranId)
        {
            StateInfo.MaxTranId = Math.Max(maxTranId, StateInfo.MaxTranId);

            var parent = Parent;

            if (parent != null)
            {
                parent.EnsureMaxTranId(StateInfo.MaxTranId);
            }
        }
        
        internal int? GetTranId()
        {
            if (IsInitializing)
            {
                return null;
            }

            var parentTranId = Parent?.GetTranId();
            var currentTableTranId = StateInfo.GetCurrentTableTranId();

            if(parentTranId.HasValue && currentTableTranId.HasValue)
            {
                if (currentTableTranId.Value < parentTranId.Value)
                {
                    StartTransaction();
                    
                    return StateInfo.MaxTranId;
                }
                
                return currentTableTranId;
            }

            if(parentTranId.HasValue)
            {
                StartTransaction();
                
                return StateInfo.MaxTranId;
            }

            return currentTableTranId;
        }

        [Pure]
        public IDataLoadState BeginLoad()
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot start loading mode for the '{Name}' table because it is readonly.");
            }
            
            return BeginLoadCore();
        }
        
        
        void IDataLoadState.EndLoad()
        {
            m_initLockCount--;

            if (IsInitializing == false)
            {
                OnEndLoad();

                UpdateAutoIncrement();
                
                CheckParentForeignKeyConstraints();
            }
        }

        public void UpdateAutoIncrement()
        {
            if (IsDisposed)
            {
                return;
            }

            foreach (var column in DataColumnInfo.Columns.Where(c => c.IsAutomaticValue))
            {
                var dataItem = column.DataStorageLink;

                var aggregateValue = dataItem.GetAggregateValue(RowsHandles, AggregateType.Max, column);

                if (aggregateValue != null)
                {
                    dataItem.UpdateMax(aggregateValue, column);
                }
            }
        }

        internal IDataLoadState BeginLoadCore()
        {
            m_initLockCount++;

            return this;
        }

        public IDataEditTransaction StartTransaction()
        {
            if (IsInitializing)
            {
                return this;
            }
            
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot start new transaction for the '{Name}' table because it is readonly.");
            }
            
            StateInfo.StartTableTransaction();

            EnsureMaxTranId(StateInfo.MaxTranId);

            return this;
        }

        bool IDataEditTransaction.Rollback()
        {
            if (IsInitializing)
            {
                var anyRows = RowCount > 0;

                ClearRows();

                return anyRows;
            }
            
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot rollback a transaction for the '{Name}' table because it is readonly.");
            }

            if (GetIsInTransaction() == false)
            {
                throw new InvalidOperationException("Cannot rollback transaction because it didn't started.");
            }
            
            foreach (IDataEditTransaction table in Tables.Where(t => t.GetIsInTransaction()))
            {
                table.Rollback();
            }

            bool any = false;

            var changedRowList = StateInfo.GetTransactionChangedRowList();

            var tranId = StateInfo.TableTransactionInfo.CurrentTranId;

            foreach (var rowHandle in changedRowList.Reverse())
            {
                any = RollbackEditRowTransaction(rowHandle, tranId) || any;
            }

            ClearAllChangesStartingTransaction(tranId);

            foreach (var transaction in StateInfo.GetTableDependantTransactions())
            {
                transaction.Rollback();
            }

            UpdateAutoIncrement();

            StateInfo.RollbackTableTransaction();

            CheckParentForeignKeyConstraints();

            OnRollbackTransaction(tranId);

            return any;
        }

        void IDataEditTransaction.Commit()
        {
            if (IsInitializing)
            {
                return;
            }
            
            if (GetIsInTransaction() == false)
            {
                throw new InvalidOperationException("Cannot commit transaction because it wasn't started.");
            }
            
            foreach (IDataEditTransaction table in Tables)
            {
                table.Commit();
            }
            
            var changedRowList = StateInfo.GetTransactionChangedRowList();

            foreach (var rowHandle in changedRowList)
            {
                CommitRowTransaction(rowHandle);
            }

            CheckParentForeignKeyConstraints();

            var unlockTable = StateInfo.IsGoingToCommitSourceTableTransaction();

            if (unlockTable)
            {
                foreach (var transaction in StateInfo.GetTableDependantTransactions())
                {
                    transaction.Commit();
                }

                StateInfo.CommitTableTransaction();

                OnTransactionFullCommit();
            }
            else
            {
                StateInfo.TableTransactionInfo?.PopTransaction();
            }
        }

        internal IDataEditTransaction StartRowTransaction(int rowHandleCore)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot start transaction for '{GetRowDebugKey(rowHandleCore)}' row of '{Name}' table because it is readonly.");
            }
            
            if (IsInitializing)
            {
                return null;
            }

            var transaction = StartTransaction();

            StateInfo.StartRowTransaction(rowHandleCore);

            return transaction;
        }

        internal bool RollbackEditRowTransaction(int rowHandle, int transaction)
        {
            var rollbackRowTransaction = StateInfo.RollbackStateInfoTransaction(rowHandle);

            var columnChangedInfo = StateInfo.GetRowTransactionChanges(rowHandle);
            var xPropChangedInfo = StateInfo.GetRowTransactionXPropChanges(rowHandle);

            var indexUpdate = new Dictionary<int, (object oldV, object newV)>();
            
            bool anyRolledBack = RollbackRowDataStorage(rowHandle, transaction, columnChangedInfo, indexUpdate);

            anyRolledBack = RollBackRowXProperties(rowHandle, transaction, xPropChangedInfo) || anyRolledBack;

            ResetTransactionAggregatedEvents(rowHandle, transaction, columnChangedInfo, xPropChangedInfo);
            
            RestoreRowIndexes(rowHandle, indexUpdate);

            var transactions = StateInfo.GetRowDependantTransactions(rowHandle);

            foreach (var tr in transactions)
            {
                tr.Rollback();
            }

            StateInfo.RollbackRowTransaction(rowHandle);
            
            OnRollbackRowTransaction(rowHandle, transaction, columnChangedInfo);

            return anyRolledBack;
        }

        private bool RollbackRowDataStorage(int rowHandle, int transaction, IReadOnlyCollection<int> columnChangedInfo, Dictionary<int, (object oldV, object newV)> indexUpdate)
        {
            var anyRolledBack = false;

            foreach (var columnHandle in columnChangedInfo)
            {
                var dataColumn = DataColumnInfo.Columns[columnHandle];
                
                var dataItem = dataColumn.DataStorageLink;

                var prevValue = dataItem.GetData(rowHandle, dataColumn);

                var rolledBack = false;

                rolledBack = dataItem.RollbackRowTransaction(rowHandle, transaction, dataColumn);
              
                var newValue = dataItem.GetData(rowHandle, dataColumn);

                OnRowChanged(GetColumn(columnHandle), rowHandle, newValue,  prevValue, null);

                anyRolledBack = rolledBack || anyRolledBack;

                indexUpdate[columnHandle] = (prevValue, newValue);
            }

            return anyRolledBack;
        }

        private bool RollBackRowXProperties(int rowHandle, int transaction, IReadOnlyCollection<string> xPropChangedInfo)
        {
            var rolledBackXProps = false;
            
            var oldValues = new Map<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var xPropChanged in xPropChangedInfo)
            {
                oldValues[xPropChanged] = StateInfo.GetExtendedProperty(rowHandle, xPropChanged, false);
            }

            rolledBackXProps = StateInfo.RowXProps.RollbackRowTransaction(rowHandle, transaction);

            var newValues = new Map<string, object>(StringComparer.OrdinalIgnoreCase);

            foreach (var xPropChanged in xPropChangedInfo)
            {
                newValues[xPropChanged] = StateInfo.GetExtendedProperty(rowHandle, xPropChanged, false);
            }

            foreach (var xPropChanged in xPropChangedInfo)
            {
                OnRowXPropertySet(rowHandle, xPropChanged, oldValues[xPropChanged], newValues[xPropChanged]);
            }

            return rolledBackXProps;
        }

        public enum IndexAction
        {
            None,
            Clear,
            Restore
        }

        internal void CommitRowTransaction(int rowHandle)
        {
            if (IsInitializing)
            {
                return;
            }
            
            var unlockRow = StateInfo.IsGoingToCommitRowSourceTransaction(rowHandle);

            var canEndEdit = unlockRow && GetIsInTransaction() == false;
            
            if (canEndEdit)
            {
                CheckRowConstraints(rowHandle);
            }
            
            var tranId = StateInfo.GetCurrentTableTranId();

            if (tranId.HasValue)
            {
                var deletedRow  = StateInfo.GetRowState(rowHandle) == RowState.Deleted;

                if (deletedRow)
                {
                    if (unlockRow && StateInfo.IsGoingToCommitSourceTableTransaction())
                    {
                        ClearRowIndexes(rowHandle);
                    }
                }
                
                var transactions = StateInfo.GetRowDependantTransactions(rowHandle);

                foreach (var transaction in transactions)
                {
                    transaction.Commit();
                }
            }

            if (canEndEdit)
            {
                var rowChanges = StateInfo.GetRowTransactionChanges(rowHandle);

                foreach (var columnHandle in rowChanges)
                {
                    var dataColumn = DataColumnInfo.Columns[columnHandle];
                    
                    var newValue = dataColumn.DataStorageLink.GetData(rowHandle, dataColumn);

                    CheckParentForeignKeyConstraints(rowHandle, GetColumn(columnHandle), newValue);
                }

                foreach (var dataItemIndex in rowChanges)
                {
                    var dataColumn = DataColumnInfo.Columns[dataItemIndex];

                    dataColumn.DataStorageLink.StopLoggingTransactionChanges(rowHandle, dataColumn);
                }
            }

            if (canEndEdit)
            {
                StopLoggingTransactionChanges(rowHandle);
            }
        }

        private void CheckRowConstraints(int rowHandle)
        {
            if (StateInfo.IsNotDeletedAndRemoved(rowHandle))
            {
                if (ParentRelationsMap is not null)
                {
                    foreach (var value in ParentRelationsMap.Values)
                    {
                        value.ParentKeyConstraint?.CheckRowConstraints(this, rowHandle);
                    }
                }
            }
        }
        
        private void ClearRowIndexes(int rowHandleCore)
        {
            foreach (var index in IndexInfo.Indexes)
            {
                var dataColumn = DataColumnInfo.Columns[index.ColumnHandle];
                
                var dataItem = dataColumn.DataStorageLink;

                if (dataItem.IsNull(rowHandleCore, dataColumn) == false)
                {
                    IComparable prevValue = (IComparable)dataItem.GetData(rowHandleCore, dataColumn);

                    index.UpdateIndexHandle(prevValue, null, rowHandleCore);
                }
            }

            foreach (var index in MultiColumnIndexInfo.Indexes)
            {
                var indexColumns = index.Columns;
                
                var oldKey = new IComparable[indexColumns.Length];

                bool allNotNull = true;
                
                for (var i = 0; i < indexColumns.Length; i++)
                {
                    var columnHandle = indexColumns[i];

                    var dataColumn = DataColumnInfo.Columns[columnHandle];
                    
                    var dataItem = dataColumn.DataStorageLink;

                    if (dataItem.IsNull(rowHandleCore, dataColumn) == false)
                    {
                        oldKey[i] = (IComparable)dataItem.GetData(rowHandleCore, dataColumn);
                    }
                    else
                    {
                        allNotNull = false;
                    }
                }

                if (allNotNull)
                {
                    index.RemoveIndex(rowHandleCore, oldKey);
                }
                else
                {
                    index.RemoveIndex(rowHandleCore, null);
                }
            }
        }
        
        private void RestoreRowIndexes(int rowHandleCore, Dictionary<int, (object oldV, object newV)> indexUpdate)
        {
            RestoreSingleIndex(rowHandleCore, indexUpdate);

            RestoreMultiColumnIndex(rowHandleCore, indexUpdate);
        }

        private void RestoreMultiColumnIndex(int rowHandle, Dictionary<int, (object oldV, object newV)> indexUpdate)
        {
            foreach (var index in MultiColumnIndexInfo.Indexes)
            {
                var indexColumns = index.Columns;

                var newKey = new Lazy<IComparable[]>(() => new IComparable[indexColumns.Length]);
                var oldKey = new Lazy<IComparable[]>(() => new IComparable[indexColumns.Length]);

                int newSet = 0;
                int oldSet = 0;

                for (var i = 0; i < indexColumns.Length; i++)
                {
                    var columnHandle = indexColumns[i];

                    if (indexUpdate.TryGetValue(columnHandle, out var change))
                    {
                        if (change.oldV != null)
                        {
                            oldKey.Value[i] = (IComparable)change.oldV;
                            oldSet++;
                        }
                        
                        if (change.newV != null)
                        {
                            newKey.Value[i] = (IComparable)change.newV;
                            newSet++;
                        }
                    }
                }

                var os = oldSet == indexColumns.Length;
                var ns = newSet == indexColumns.Length;
                
                if (os && ns)
                {
                    index.UpdateIndexHandle(oldKey.Value, newKey.Value, rowHandle);
                }
                else if(os)
                {
                    index.RemoveIndex(rowHandle, oldKey.Value);
                }
                else if(ns)
                {
                    index.AddIndex(newKey.Value, rowHandle);
                }
            }
        }

        private void RestoreSingleIndex(int rowHandleCore, Dictionary<int, (object oldV, object newV)> indexUpdate)
        {
            foreach (var index in IndexInfo.Indexes)
            {
                if (indexUpdate.TryGetValue(index.ColumnHandle, out var val))
                {
                    if (val.oldV != null && val.newV != null)
                    {
                        var oldValue = (IComparable)val.oldV;
                        var newValue = (IComparable)val.newV;

                        index.UpdateIndexHandle(oldValue, newValue, rowHandleCore);
                    }
                    else if(val.oldV != null)
                    {
                        var oldValue = (IComparable)val.oldV;
                        index.RemoveIndex(rowHandleCore, oldValue);
                    }
                    else if(val.newV != null)
                    {
                        var newValue = (IComparable)val.newV;
                        index.AddIndex(newValue, rowHandleCore);
                    }
                }
            }
        }

        public bool GetIsInTransaction() => StateInfo.IsTableInTransaction;
    }
}