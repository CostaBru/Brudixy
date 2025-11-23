using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces;
using Brudixy.Index;
using Konsarpoo.Collections;

namespace Brudixy.Constraints
{
    public partial class ForeignKeyConstraint
    {
        internal void CheckRowConstraints(CoreDataTable rowTable, int rowHandle)
        {
            var childTable = m_childKey.Table;

            if (ReferenceEquals(childTable, rowTable) == false)
            {
                return;
            }

            var childColumns = new int[m_childKey.Columns.Count];
            var values = new object[m_childKey.Columns.Count];

            bool allNull = true;

            for (var index = 0; index < m_childKey.Columns.Count; index++)
            {
                var column = m_childKey.Columns[index];
                
                childColumns[index] = column;

                var dataColumn = rowTable.GetColumn(column);
                
                values[index] = rowTable.GetRowFieldValue(rowHandle, dataColumn, DefaultValueType.ColumnBased, null);

                if (rowTable.GetIsRowColumnNull(rowHandle, dataColumn) == false)
                {
                    allNull = false;
                }
            }

            if (allNull == false)
            {
                CheckConstraint(rowTable, rowHandle, DataRowAction.Change, childColumns, values);
            }
        }

        internal void CheckConstraint(Map<int, string> parentConstraintViolationRowHandles)
        {
            GetErrorConstraintChildRows(parentConstraintViolationRowHandles);
        }

        
        public bool CheckConstraint(CoreDataTable childTable, int childRowHandle, 
            DataRowAction change,
            ColumnHandle columnHandle, 
            object proposedValues)
        {
            return CheckConstraint(childTable, childRowHandle, change, new int [] { columnHandle.Handle }, new object[] { proposedValues });
        }

        internal bool CheckConstraint(CoreDataTable childTable,
            int childRowHandle, 
            DataRowAction action, 
            IReadOnlyList<int> childFields,
            IReadOnlyList<object> proposedValues)
        {
            var parentTable = m_parentKey.Table;

            var canCheck = (action == DataRowAction.Change ||
                            action == DataRowAction.Add ||
                            action == DataRowAction.Rollback);

            if (canCheck && (parentTable?.GetEnforceConstraints() ?? false))
            {
                // now check to see if someone exists... it will have to be in a parent row's current, not a proposed. 
                if (m_childKey.Columns.Count == 1)
                {
                    var comp = (IComparable)proposedValues[0];

                    var parentKeyColumn = m_parentKey.Columns[0];

                    if (parentTable.IndexInfo.IndexMappings.TryGetValue(parentKeyColumn, out var parentIndexIndex) == false)
                    {
                        throw new InvalidOperationException(
                            $"FK constraint check {constraintName} violation occurred. No parent '{parentTable.Name}' with column '{parentTable.DataColumnInfo.Columns[parentKeyColumn].ColumnName}' is not indexed.");
                    }

                    var rowHandles = parentTable.IndexInfo.GetRowHandles(parentIndexIndex, comp);
                    
                    if (rowHandles.Any(r => parentTable.StateInfo.IsNotDeletedAndRemoved(r)) == false)
                    {
                        throw new ConstraintException(
                            $"FK constraint check {constraintName} violation occurred. No parent '{parentTable.Name}' with key '{parentTable.DataColumnInfo.Columns[parentKeyColumn].ColumnName}' and '{comp}' value exists.");
                    }
                }
                else
                {
                    var (searchKey, parentColumns, multiIndex) = 
                        GetParentTableSearchInfo(m_childKey, childTable, childRowHandle, childFields, m_parentKey, parentTable, proposedValues);

                    if (multiIndex == null)
                    {
                        var strCol = string.Join(",", parentColumns);

                        throw new ConstraintException($"FK constraint check {constraintName} violation occurred. Cannot find multi column index for '{strCol}' columns in {parentTable.Name} parent table defined.");
                    }

                    var searchResult = multiIndex.GetRowHandles(searchKey);

                    if (searchResult.Any() == false)
                    {
                        var strCol = string.Join(",", parentColumns);
                        var strKey =  MultiColumnBisectIndex.ValueToString(searchKey, parentColumns, parentTable);

                        throw new ConstraintException($"FK multi column constraint check {constraintName} violation occurred. No parent '{parentTable.Name}' with  '{strCol}' keys fields and '{strKey}' values are exists.");
                    }
                }

                return true;
            }

            return true;
        }
        
          private bool GetBothIndexes(CoreDataTable childTable, out Indexes childIndex, out Indexes parentIndex,
            out CoreDataTable parentTable)
        {
            childIndex = null;
            parentIndex = null;
            parentTable = null;

            var dataColumn = m_childKey.Columns[0];

            if (childTable.IndexInfo.IndexMappings.TryGetValue(dataColumn, out var childIndexIndex) == false)
            {
                return false;
            }

            childIndex = childTable.IndexInfo.Indexes[childIndexIndex];

            var parentKeyColumn = m_parentKey.Columns[0];

            parentTable = m_parentKey.Table;
            
            if (parentTable == null)
            {
                return false;
            }

            if (parentTable.IndexInfo.IndexMappings.TryGetValue(parentKeyColumn, out var parentIndexIndex) == false)
            {
                return false;
            }

            parentIndex = parentTable.IndexInfo.Indexes[parentIndexIndex];

            return true;
        }

        private bool GetBothMultiIndexes(CoreDataTable childTable, out IndexesOfMany childIndex,
            out IndexesOfMany parentIndex, out CoreDataTable parentTable)
        {
            childIndex = null;
            parentIndex = null;
            parentTable = null;

            childIndex = childTable.MultiColumnIndexInfo
                .TryGetIndex(m_childKey.ColumnsReference.Select(a => a.ColumnHandle)
                    .ToArray());

            if (childIndex == null)
            {
                return false;
            }

            parentTable = m_parentKey.Table;
            
            if (parentTable == null)
            {
                return false;
            }

            parentIndex = parentTable.MultiColumnIndexInfo
                .TryGetIndex(m_parentKey.ColumnsReference.Select(a => a.ColumnHandle)
                    .ToArray());

            return true;
        }

        internal void GetErrorConstraintChildRows(Map<int, string> parentConstraintViolationRowHandles)
        {
            var childTable = m_childKey.Table;
            
            if (childTable == null || childTable.GetEnforceConstraints() == false)
            {
                return;
            }

            if (m_parentKey.Columns.Count == 1)
            {
                if (!GetBothIndexes(childTable, out var childIndex, out var parentIndex, out var parentTable))
                {
                    return;
                }

                CheckParentIndex(parentIndex.ReadyIndex, childIndex, parentTable, parentConstraintViolationRowHandles, childTable);
            }
            else
            {
                if (!GetBothMultiIndexes(childTable, out var childIndex, out var parentIndex, out var parentTable))
                {
                    return;
                }

                CheckParentIndex(parentIndex.ReadyIndex, childIndex, parentTable, parentConstraintViolationRowHandles, childTable);
            }
        }

        private void CheckParentIndex(IMultiValueIndex parentIndex, 
            IndexesOfMany childIndex, 
            CoreDataTable parentTable,
            Map<int, string> parentConstraintViolationRowHandles, 
            CoreDataTable childTable)
        {
            if (parentIndex.Count > 0)
            {
                Func<int, bool> validCheck = parentRowHandle =>
                {
                    var rowState = parentTable.GetRowState(parentRowHandle);

                    return rowState != RowState.Deleted && rowState != RowState.Detached;
                };
                Func<int, bool> childValidCheck = childRowHandle =>
                {
                    var rowState = childTable.GetRowState(childRowHandle);

                    return rowState != RowState.Deleted && rowState != RowState.Detached;
                };

                var errorList1 = parentIndex.CheckAllKeys(childIndex.ReadyIndex, validCheck, childValidCheck);

                foreach (var rowHandle in errorList1)
                {
                    parentConstraintViolationRowHandles[rowHandle] = GetParentError();
                }
            }
        }

        private string GetParentError()
        {
            return  constraintName;
        }

        private void CheckParentIndex(IIndexStorage parentIndex,
            Indexes childIndex,
            CoreDataTable parentTable,
            Map<int, string> parentConstraintViolationRowHandles,
            CoreDataTable childTable)
        {
            if (parentIndex.Count > 0)
            {
                Func<int, bool> validCheck = parentRowHandle =>
                {
                    return true;
                };
                Func<int, bool> childValidCheck = childRowHandle =>
                {
                    var rowState = childTable.GetRowState(childRowHandle);

                    return rowState != RowState.Deleted && rowState != RowState.Detached;
                };

                var errorList1 = parentIndex.CheckAllKeys(childIndex.ReadyIndex, validCheck, childValidCheck);

                foreach (var rowHandle in errorList1)
                {
                    if (parentTable.GetRowState(rowHandle) != RowState.Deleted)
                    {
                        parentConstraintViolationRowHandles[rowHandle] = GetParentError();
                    }
                }
            }
        }
    }
}