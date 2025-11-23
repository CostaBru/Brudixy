using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Brudixy.Constraints;
using Brudixy.Converter;
using Brudixy.EventArgs;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        protected MaxColumnLenConstraintDataEvent m_maxColumnLenConstraint;
        
        internal bool HasMaxColumnLenConstraintHandler => m_maxColumnLenConstraint?.HasAny() ?? false;

        public object GetRowDebugKey(int rowHandle)
        {
            var pk = DataColumnInfo.PrimaryKeyColumns.ToArray();

            if (pk.Length > 0)
            {
                return string.Join(", ", pk.Select(k =>
                {
                    var value = GetRowFieldValue(rowHandle, k, DefaultValueType.ColumnBased, null);

                    return $"{k.ColumnName} = {(value ?? "{NULL}")}, Row handle: {rowHandle}";
                }));
            }

            if (IndexInfo.HasAny)
            {
                var columnHandle = TryGetUniqueIndex()?.ColumnHandle ?? IndexInfo.Indexes.First().ColumnHandle;

                var value = GetRowFieldValue(rowHandle, GetColumn(columnHandle), DefaultValueType.ColumnBased, null);
                
                return $"{DataColumnInfo.Columns[columnHandle].ColumnName} = {(value ?? "{NULL}")}, Row handle: {rowHandle}";
            }

            return "Row handle: " + rowHandle;
        }
        
        internal void BeforeSetRowColumnBoxed(int rowHandle, 
            CoreDataColumn column,
            ref object value, 
            object prevValue,
            out bool canContinue,
            out string cancelExceptionMessage,
            ref Map<int, object> cascadePrevValues)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{GetRowDebugKey(rowHandle)}' from '{Name}' table because it is readonly.");
            }
            
            cancelExceptionMessage = string.Empty;
                
            if (column.IsServiceColumn)
            {
                SilentlySetRowValue(rowHandle, new ColumnHandle(column.ColumnHandle), value);
                    
                canContinue = false;
                
                return;
            }
                
            if (IsReadOnlyColumn(column))
            {
                canContinue = false;
                
                return;
            }
            
            canContinue = true;

            cancelExceptionMessage = string.Empty;

            var isNotInit = IsInitializing == false;
            //todo remove transaction check
            var isNotTransaction = isNotInit && GetIsInTransaction() == false;

            if (isNotTransaction && GetRowState(rowHandle) != RowState.New)
            {
                CheckNewValue(column, prevValue, value, rowHandle);
            }

            OnBeforeRowColumnSet(rowHandle, column, ref value, prevValue, ref canContinue, ref cancelExceptionMessage, ref cascadePrevValues, isNotInit);
        }

        internal void BeforeSetRowColumn<T>(int rowHandle,
            CoreDataColumn column,
            ref T value,
            ref T prevValue,
            out bool canContinue,
            out string cancelExceptionMessage, 
            ref Map<int, object> cascadePrevValues) 
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change '{GetRowDebugKey(rowHandle)}' from '{Name}' table because it is readonly.");
            }
            
            cancelExceptionMessage = string.Empty;
                
            if (column.IsServiceColumn)
            {
                SilentlySetRowValue(rowHandle, new ColumnHandle(column.ColumnHandle), value);
                    
                canContinue = false;
                
                return;
            }
                
            if (IsReadOnlyColumn(column))
            {
                canContinue = false;
                
                return;
            }
            
            var isNotInit = IsInitializing == false;
            var isNotInTransaction = isNotInit && GetIsInTransaction() == false;
            
            if (isNotInTransaction && GetRowState(rowHandle) != RowState.New)
            {
                CheckNewValue(column, prevValue, value, rowHandle);
            }

            canContinue = true;

            OnBeforeRowColumnSet(rowHandle, column, ref value, prevValue, ref canContinue, ref cancelExceptionMessage, ref cascadePrevValues, isNotInit);
        }
       
        private void SilentlySetRowValue(int rowHandle, ColumnHandle column, object value)
        {
            var dataColumn = DataColumnInfo.Columns[column.Handle];
            
            var dataItem = dataColumn.DataStorageLink;

            dataItem.SilentlySetValue(rowHandle, value, dataColumn);
        }
        
        internal bool SetRowColumnValue(int rowHandle, CoreDataColumn column, object value, object prevValue, Map<int, object> cascadePrevValues)
        {
            var tranId = GetTranId();

            var wasChanged = column.DataStorageLink.SetValue(rowHandle, value, tranId, column);

            if (wasChanged == false)
            {
                return false;
            }

            if (value is IComparable && column.IsAutomaticValue)
            {
                UpdateMax(column, value);
            }

            if (IsInitializing)
            {
                return true;
            }

            var isInTransaction = GetIsInTransaction();

            //if (isInTransaction == false)
            {
                UpdateIndexAfterColumnChange(column, 
                    rowHandle,
                    value,
                    prevValue);

                var transactions = UpdateChildrenByForeignKey(rowHandle, value, prevValue, column);

                foreach (var transaction in transactions)
                {
                    StateInfo.AddDependantTransactions(rowHandle, transaction);
                }
            }

            AfterRowChanged(rowHandle, column, tranId, isInTransaction);

            OnRowChanged(column, rowHandle, value, prevValue, cascadePrevValues);

            return true;
        }

        internal void SetRowColumnValue<T>(int rowHandle, 
            ITypedDataItem<T> typedDataItem,
            CoreDataColumn column,
            ref T value, 
            ref T prevValue, 
            Map<int, object> cascadePrevValues) 
        {
            var tranId = GetTranId();

            var wasChanged = typedDataItem.SetValue(rowHandle, value, tranId, column);

            if (wasChanged == false)
            {
                return;
            }
            
            var newValueNull = value == null;

            if (newValueNull == false && (column.IsAutomaticValue))
            {
                UpdateMax(column, value);
            }

            if (IsInitializing)
            {
                return;
            }

            var isInTransaction = GetIsInTransaction();

            //if (isInTransaction == false)
            {
                UpdateIndexAfterColumnChange(column, rowHandle, value, prevValue);

                UpdateChildrenByForeignKey(rowHandle, value, prevValue, column);
            }

            AfterRowChanged(rowHandle, column, tranId, isInTransaction);

            OnRowChanged(column, rowHandle, value, prevValue, cascadePrevValues);
        }


        internal void AfterRowChanged(int rowHandle, CoreDataColumn column, int? tranId, bool isInTransaction)
        {
            StateInfo.UpdateRowAge(rowHandle);

            Interlocked.Increment(ref m_dataAge);

            if (StateInfo.GetRowState(rowHandle) == RowState.Unchanged)
            {
                StateInfo.SetModified(rowHandle, tranId);
            }

            if (isInTransaction)
            {
                StateInfo.SetRowTransactionColumnChanged(rowHandle, column.ColumnHandle);
                StateInfo.SetTransactionRowChanged(rowHandle);
            }
        }

        private void UpdateIndexAfterColumnChange(CoreDataColumn column,
            int rowHandle,
            object value,
            object prevValue)
        {
            if (IndexInfo.ColumnHasIndex(column.ColumnHandle))
            {
                IndexInfo.UpdateIndexValue(this, column.ColumnHandle, (IComparable)prevValue, (IComparable)value, rowHandle);
            }

            if (MultiColumnIndexInfo.HasAny)
            {
                MultiColumnIndexInfo.UpdateIndexValue(this, column.ColumnHandle, (IComparable)prevValue, (IComparable)value, rowHandle);
            }
        }

        private IReadOnlyList<IDataEditTransaction> UpdateChildrenByForeignKey(int rowHandle, object value, object prevValue, CoreDataColumn column)
        {
            var relationNames = m_columnChildRelations?.GetOrDefault(column.ColumnHandle);

            var dataEditTransactions = new List<IDataEditTransaction>();

            if (value != null && prevValue != null && relationNames != null && GetEnforceConstraints())
            {
                foreach (var relationName in relationNames)
                {
                    var relation = ChildRelationsMap?.GetOrDefault(relationName);

                    if (relation == null)
                    {
                        relation = Parent?.RelationsMap?.GetOrDefault(relationName);
                    }

                    var constraint = relation?.ChildKeyConstraint;

                    if (constraint != null && constraint.UpdateRule != Rule.None)
                    {
                        var transaction = CascadeUpdateChildren(rowHandle, value, prevValue, column,  relation.ChildKeyConstraint);

                        if (transaction != null)
                        {
                            dataEditTransactions.Add(transaction);
                        }
                    }
                }
            }

            return dataEditTransactions;
        }

        private IDataEditTransaction CascadeUpdateChildren(int rowHandle,
            object value,
            object prevValue,
            CoreDataColumn column,
            ForeignKeyConstraint relationChildKeyConstraint)
        {
            return relationChildKeyConstraint.CascadeUpdate(this,
                rowHandle,
                column.ColumnHandle,
                (IComparable)value,
                (IComparable)prevValue,
                inTransaction: GetIsInTransaction());
        }

        private IReadOnlyList<IDataEditTransaction> CascadeUpdateChildrenMultiIndex(int rowHandleCore, 
             IndexesOfMany index, 
             IComparable[] newKey,
             IComparable[] oldKey)
        {
            var dataEditTransactions = new List<IDataEditTransaction>();

            foreach (var columnHandle in index.Columns)
             {
                 var relationNames = m_columnChildRelations.GetOrDefault(columnHandle);

                 bool wasUpdated = false;

                 if (relationNames is not null)
                 {
                     foreach (var relationName in relationNames)
                     {
                         if (ChildRelationsMap != null
                             && ChildRelationsMap.TryGetValue(relationName, out var relation)
                             && relation.ChildKeyConstraint?.UpdateRule == Rule.Cascade
                             && relation.ChildKeyConstraint?.ParentKey.Columns.Count ==
                             newKey.Length)
                         {
                             var dataEditTransaction = relation.ChildKeyConstraint.CascadeUpdate(this, rowHandleCore,
                                 -1,
                                 null,
                                 null,
                                 newKey,
                                 oldKey,
                                 inTransaction:  GetIsInTransaction());

                             if (dataEditTransaction != null)
                             {
                                 dataEditTransactions.Add(dataEditTransaction);
                             }

                             wasUpdated = true;
                         }
                     }
                 }

                 if (wasUpdated)
                 {
                     break;
                 }
             }

            return dataEditTransactions;
        }

        public void CheckParentForeignKeyConstraints(int rowHandle, CoreDataColumn column, object proposedValue) 
        {
            if (m_columnChildRelations != null && GetEnforceConstraints())
            {
                var relationNames = m_columnChildRelations.GetOrDefault(column.ColumnHandle);

                if (relationNames is not null && ParentRelationsMap != null)
                {
                    foreach (var relationName in relationNames)
                    {
                        var relation = ParentRelationsMap.GetOrDefault(relationName);

                        relation?.ParentKeyConstraint?.CheckConstraint(this, rowHandle, DataRowAction.Change, new ColumnHandle(column.ColumnHandle), proposedValue);
                    }
                }
            }
        }
        
        public void CheckParentForeignKeyConstraints<T>(int rowHandle, CoreDataColumn column, ref T? proposedValue) where T : IComparable, IComparable<T>
        {
            if (m_columnChildRelations != null && GetEnforceConstraints())
            {
                var relationNames = m_columnChildRelations.GetOrDefault(column.ColumnHandle);

                if (relationNames is not null && ParentRelationsMap != null)
                {
                    foreach (var relationName in relationNames)
                    {
                        DataRelation relation = ParentRelationsMap.GetOrDefault(relationName);
                        
                        if (relation?.ParentKeyConstraint != null)
                        {
                            if (relation.ParentKeyConstraint is IComparableForeignKeyConstraint<T> str)
                            {
                                str.CheckCurrentConstraint(this, rowHandle, DataRowAction.Change, new ColumnHandle(column.ColumnHandle), ref proposedValue);
                            }
                            else
                            {
                                relation.ParentKeyConstraint.CheckConstraint(this, rowHandle, DataRowAction.Change, new ColumnHandle(column.ColumnHandle), proposedValue);
                            }
                        }
                    }
                }
            }
        }

        internal bool NeedCheckUnique(CoreDataColumn columnHandle)
        {
            if (columnHandle.IsUnique)
            {
                return IndexInfo.ColumnHasIndex(columnHandle.ColumnHandle);
            }

            if (MultiColumnIndexInfo.HasAny)
            {
                var columnFirstIndex = MultiColumnIndexInfo.GetColumnFirstIndex(columnHandle.ColumnHandle);

                return columnFirstIndex >= 0 && MultiColumnIndexInfo.Indexes[columnFirstIndex].IsUnique;
            }

            return false;
        }

        internal void CheckNewValue(CoreDataColumn column, object prevValue, object value, int rowHandle)
        {
            CheckNewValueCore(column, prevValue, value, rowHandle);
        }

        private void CheckNewValueCore(CoreDataColumn column, object prevValue, object value, int rowHandle)
        {
            if (NeedCheckUnique(column))
            {
                CheckNewUniqueValueFull(column, prevValue, value, rowHandle);
            }

            CheckParentForeignKeyConstraints(rowHandle, column, value);
        }

        private void CheckNewUniqueValueFull(CoreDataColumn column, object prevValue, object value, int rowHandle)
        {
            if (value == null)
            {
                throw new ConstraintException(
                    $"The unique indexed column '{column.ColumnName}' cell value of the '{TableName}' table can't be set null.");
            }

            if (value.Equals(prevValue))
            {
                return;
            }

            if (IndexInfo.CheckNewValue(this, column.ColumnHandle, prevValue, value) == false)
            {
                throw new ConstraintException(
                    $"The unique indexed column '{column.ColumnName}' cell value of the '{TableName}' table can't be set to '{value}' because of unique constraint.");
            }

            if (MultiColumnIndexInfo.HasAny)
            {
                if (MultiColumnIndexInfo.CheckNewValue(this, column.ColumnHandle, prevValue, value, rowHandle) == false)
                {
                    throw new ConstraintException(
                        $"The unique indexed column '{column.ColumnName}' cell value  of the '{TableName}' table can't be set to '{value}' because of unique constraint.");
                }
            }
        }
        
        private void CheckNewUniqueValue(int columnHandle, object prevValue, object value, int rowHandle)
        {
            if (value == null)
            {
                throw new ConstraintException(
                    $"The unique indexed column '{DataColumnInfo.Columns[columnHandle].ColumnName}' cell value of the '{TableName}' table can't be set null.");
            }

            if (value.Equals(prevValue))
            {
                return;
            }

            if (IndexInfo.CheckNewValue(this, columnHandle, prevValue, value) == false)
            {
                throw new ConstraintException(
                    $"The unique indexed column '{DataColumnInfo.Columns[columnHandle].ColumnName}' cell value of the '{TableName}' table can't be set to '{value}' because of unique constraint.");
            }
        }

        internal void CheckNewValue<T>(CoreDataColumn column, T prevValue, T value, int rowHandle) where T : IComparable, IComparable<T>
        {
            if (NeedCheckUnique(column))
            {
                if (value == null)
                {
                    throw new ConstraintException(
                        $"The unique indexed column ${column.ColumnName} cell value  of the '{TableName}' table can't be set null.");
                }

                if (prevValue != null && EqualityComparer<T>.Default.Equals(prevValue, value))
                {
                    return;
                }

                if (IndexInfo.CheckNewValue(this, column.ColumnHandle, prevValue, value) == false)
                {
                    throw new ConstraintException(
                        $"The unique indexed column ${column.ColumnName} cell value  of the '{TableName}' table can't be set to {value} because of unique constraint.");
                }

                if (MultiColumnIndexInfo.HasAny)
                {
                    if (MultiColumnIndexInfo.CheckNewValue(this, column.ColumnHandle, prevValue, value, rowHandle) == false)
                    {
                        throw new ConstraintException(
                            $"The unique multi key indexed column ${column.ColumnName} cell value of the '{TableName}' table can't be set to {value} because of unique constraint.");
                    }
                }

                CheckParentForeignKeyConstraints(rowHandle, column, ref value);
            }
        }
    }
}