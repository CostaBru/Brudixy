using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Brudixy.Index;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy.Constraints
{
    public partial class ForeignKeyConstraint
    {
        internal virtual IDataEditTransaction CascadeDelete(CoreDataTable rowTable, int rowHandle, bool inTransaction)
        {
            var (values, keyValue, hasValue) = GetKeyValues(rowTable, rowHandle, m_parentKey);

            if (hasValue == false)
            {
               return null;
            }

            var childTable = m_childKey.Table;
            
            if (childTable == null || childTable.GetEnforceConstraints() == false)
            {
                return null;
            }

            if (rowTable.RowInCascadeUpdate == null)
            {
                rowTable.RowInCascadeUpdate = new Set<int>();
            }

            if (rowTable.RowInCascadeUpdate.Contains(rowHandle) || rowTable.GetRowState(rowHandle) == RowState.Detached)
            {
                return null;
            }

            rowTable.RowInCascadeUpdate.Add(rowHandle);

            try
            {
                switch (DeleteRule)
                {
                    case Rule.None:
                    {
                        CheckDeleteNonRule(rowTable, rowHandle, childTable, keyValue, values);

                        break;
                    }

                    case Rule.Cascade:
                    {
                        var transaction = CascadeUpdateChildRows(rowTable,
                            rowHandle,
                            -1,
                            keyValue, 
                            keyValue,
                            values,
                            values,
                            childTable,
                            (chTable, chRowHandle, col, newVal) => chTable.DeleteRow(chRowHandle),
                            (chTable, childRowHandle, cols, newVals) => chTable.DeleteRow(childRowHandle),
                            inTransaction: inTransaction);

                        return transaction;
                    }

                    case Rule.SetNull:
                    {
                        Action<CoreDataTable, int, IReadOnlyList<int>, IReadOnlyList<object>> setNullN =
                            (chTable, childRowHandle, cols, newVals) =>
                            {
                                foreach (var col in cols)
                                {
                                    chTable.SetRowNullValue(childRowHandle, chTable.GetColumn(col));
                                }
                            };

                        var transaction = CascadeUpdateChildRows(rowTable,
                            rowHandle,
                            -1,
                            keyValue,
                            keyValue,
                            values,
                            values,
                            childTable,
                            (chTable, childRowHandle, col, newVal) => chTable.SetRowNullValue(childRowHandle, chTable.GetColumn(col)),
                            setNullN,
                            inTransaction: inTransaction);

                        return transaction;
                    }
                    case Rule.SetDefault:
                    {
                        Action<CoreDataTable, int, IReadOnlyList<int>, IReadOnlyList<object>> setDefaultN =
                            (chTable, childRowHandle, cols, newVals) =>
                            {
                                foreach (var col in cols)
                                {
                                    chTable.SetRowDefaultValue(childRowHandle, chTable.GetColumn(col));
                                }
                            };

                        var transaction = CascadeUpdateChildRows(rowTable,
                            rowHandle,
                            -1,
                            keyValue,
                            keyValue,
                            values,
                            values,
                            childTable,
                            (chTable, childRowHandle, col, newVal) => chTable.SetRowDefaultValue(childRowHandle, chTable.GetColumn(col)),
                            setDefaultN,
                            inTransaction: inTransaction);

                        return transaction;
                    }
                    default:
                    {
                        Debug.Assert(false, "Unknown Rule value");
                        break;
                    }
                }
            }
            finally
            {
                rowTable.RowInCascadeUpdate.Remove(rowHandle);
            }
            
            return null;
        }

        internal IDataEditTransaction CascadeUpdate(CoreDataTable table, int rowHandle, int columnHandle, IComparable proposedValues, IComparable currentValues, bool inTransaction = true)
        {
            return CascadeUpdate(table, rowHandle, columnHandle, proposedValues, currentValues, null, null, inTransaction);
        }

        internal IDataEditTransaction CascadeUpdate(CoreDataTable table,
            int rowHandle, 
            int columnHandle,
            IComparable proposedValue, IComparable currentValue,
            IComparable[] proposedValues, IComparable[] currentValues, 
            bool inTransaction = true)
        {
            if (currentValues == null && currentValue is null)
            {
                return null;
            }

            var childTable = m_childKey.Table;
            
            if (childTable == null)
            {
                return null;
            }

            if (table.RowInCascadeUpdate == null)
            {
                table.RowInCascadeUpdate = new Set<int>();
            }

            if (table.RowInCascadeUpdate.Contains(rowHandle) || table.GetRowState(rowHandle) == RowState.Detached)
            {
                return null;
            }

            table.RowInCascadeUpdate.Add(rowHandle);

            try
            {
                switch (UpdateRule)
                {
                    case Rule.None:
                    {
                        CheckNonUpdateRule(table, rowHandle, currentValues, childTable);

                        break;
                    }

                    case Rule.Cascade:
                    {
                        Action<CoreDataTable, int, int, IComparable> updateRow1 = (chTable, childRowHandle, col, newVal) =>
                        {
                            var chPrev = chTable.GetRowFieldValue(childRowHandle, chTable.GetColumn(col), DefaultValueType.ColumnBased, null);

                            chTable.SetRowColumnValue(childRowHandle, chTable.GetColumn(col), newVal, chPrev, null);
                        };

                        Action<CoreDataTable, int, IReadOnlyList<int>, IReadOnlyList<IComparable>> updateRowN =
                            (chTable, childRowHandle, cols, newVals) =>
                            {
                                if (newVals == null)
                                {
                                    for (int i = 0; i < cols.Count; i++)
                                    {
                                        chTable.SetRowNullValue(childRowHandle, chTable.GetColumn(cols[i]));
                                    }
                                }
                                else
                                {
                                    for (int i = 0; i < cols.Count; i++)
                                    {
                                        var ch = cols[i];

                                        var chPrev = chTable.GetRowFieldValue(childRowHandle, chTable.GetColumn(ch), DefaultValueType.ColumnBased, null);

                                        chTable.SetRowColumnValue(childRowHandle, chTable.GetColumn(ch), newVals[i], chPrev, null);
                                    }
                                }
                            };

                        var transaction = CascadeUpdateChildRows(table, rowHandle, columnHandle, proposedValue, currentValue, proposedValues, currentValues, childTable, updateRow1, updateRowN, inTransaction: inTransaction);

                        return transaction;
                    }

                    case Rule.SetNull:
                    {
                        Action<CoreDataTable, int, int, IComparable> setNull1 = (chTable, childRowHandle, col, newVal) =>
                        {
                            chTable.SetRowNullValue(childRowHandle, chTable.GetColumn(col));
                        };

                        Action<CoreDataTable, int, IReadOnlyList<int>, IReadOnlyList<object>> setNullN =
                            (chTable, childRowHandle, cols, newVals) =>
                            {
                                foreach (var col in cols)
                                {
                                    chTable.SetRowNullValue(childRowHandle, chTable.GetColumn(col));
                                }
                            };

                        var transaction = CascadeUpdateChildRows(table, rowHandle, columnHandle, proposedValue, currentValue, proposedValues, currentValues, childTable, setNull1, setNullN, inTransaction: inTransaction);

                        return transaction;

                    }
                    case Rule.SetDefault:
                    {
                        Action<CoreDataTable, int, int, IComparable> setNull1 = (chTable, childRowHandle, col, newVal) =>
                        {
                            chTable.SetRowDefaultValue(rowHandle, chTable.GetColumn(col));
                        };

                        Action<CoreDataTable, int, IReadOnlyList<int>, IReadOnlyList<IComparable>> setNullN =
                            (chTable, childRowHandle, cols, newVals) =>
                            {
                                foreach (var col in cols)
                                {
                                    chTable.SetRowDefaultValue(childRowHandle, chTable.GetColumn(col));
                                }
                            };

                        var transaction = CascadeUpdateChildRows(table, rowHandle, columnHandle, proposedValue, currentValue, proposedValues, currentValues, childTable, setNull1, setNullN, inTransaction: inTransaction);

                        return transaction;
                    }
                    default:
                    {
                        Debug.Assert(false, "Unknown Rule value");
                        break;
                    }
                }
            }
            finally
            {
                table.RowInCascadeUpdate.Remove(rowHandle);
            }
            
            return null;
        }

        private void CheckDeleteNonRule(CoreDataTable rowTable, int rowHandle, CoreDataTable childTable, IComparable keyValue, IComparable[] values)
        {
            if (m_childKey.Columns.Count == 1)
            {
                var failedCascadeDelete =
                    childTable.FindManyHandles(m_childKey.Columns[0], keyValue).Take(2)
                        .Count() == 2;

                if (failedCascadeDelete)
                {
                    throw new InvalidOperationException(
                        $"FK constraint {constraintName} cascade delete error.  Child table {childTable.Name} has more than one item for FK {constraintName}.");
                }
            }
            else
            {
                var parentFields = m_parentKey.Columns;

                var (searchKey, childFields, multiIndex) = GetChildTableSearchInfo(
                    m_parentKey,
                    rowTable,
                    rowHandle,
                    m_childKey,
                    parentFields,
                    childTable,
                    null,
                    values);

                if (multiIndex == null)
                {
                    var strCol = string.Join(",", childFields);

                    throw new ConstraintException(
                        $"FK constraint {constraintName} cascade update error. Cannot find multi column index for '{strCol}' columns in {childTable.Name} child table defined. Rule {UpdateRule}");
                }

                var searchResult = multiIndex.GetRowHandles(searchKey);

                var validRows = new Data<int>();

                foreach (var foundRowHandles in searchResult)
                {
                    if (childTable.StateInfo.IsNotDeletedAndRemoved(foundRowHandles))
                    {
                        validRows.Add(foundRowHandles);
                    }
                }
                
                if (validRows.Count > 1)
                {
                    var strCol = string.Join(",", childFields);
                    var strKey = MultiColumnBisectIndex.ValueToString(searchKey, childFields, childTable);

                    throw new ConstraintException(
                        $"FK multi column constraint {constraintName} cascade delete error occurred. There is an existing record in '{childTable.Name}' with  '{strCol}' keys fields and '{strKey}' values. Rule {UpdateRule}");
                }
            }
        }

        private void CheckNonUpdateRule(CoreDataTable table, int rowHandle, IComparable[] currentValues, CoreDataTable childTable)
        {
            if (childTable.GetEnforceConstraints())
            {
                if (m_childKey.Columns.Count == 1)
                {
                    var keyValue = currentValues[0];

                    // if we're not cascading deletes, we should throw if we're going to strand a child row under enforceConstraints.
                    var failedCascadeUpdate = childTable
                        .FindManyHandles(m_childKey.Columns[0], keyValue).Any();

                    if (failedCascadeUpdate)
                    {
                        throw new ConstraintException(
                            $"Failed cascade update for {constraintName}. Rule {UpdateRule}");
                    }
                }
                else
                {
                    var parentFields = m_parentKey.Columns;

                    var (searchKey, childColumns, multiIndex) = 
                        GetChildTableSearchInfo(m_parentKey,
                            table,
                            rowHandle,
                        m_childKey, 
                            parentFields,
                            childTable,
                            null,
                            currentValues);

                    if (multiIndex == null)
                    {
                        var strCol = string.Join(",", childColumns);

                        throw new ConstraintException(
                            $"FK constraint {constraintName} cascade update error occurred. Cannot find multi column index for '{strCol}' columns in {childTable.Name} child table defined. Rule {UpdateRule}");
                    }

                    var searchResult = multiIndex.GetRowHandles(searchKey);
                    
                    foreach (var searchRowHandle in searchResult)
                    {
                        if (childTable.StateInfo.IsNotDeletedAndRemoved(searchRowHandle))
                        {
                            var strCol = string.Join(",", childColumns);
                            var strKey =  MultiColumnBisectIndex.ValueToString(searchKey, childColumns, childTable);

                            throw new ConstraintException(
                                $"FK multi column constraint {constraintName} cascade update error occurred. There is an existing record in '{childTable.Name}' with  '{strCol}' keys fields and '{strKey}' values. Rule {UpdateRule}");
                        }
                    }
                }
            }
        }

        internal static (IComparable[] values, IComparable keyValue, bool hasValue) 
            GetKeyValues(
            CoreDataTable rowTable,
            int rowHandle,
            DataKey key)
        {
            IComparable[] values = null;
            IComparable keyValue = null;

            if (key.Columns.Count == 1)
            {
                if (rowTable.GetIsRowColumnNull(rowHandle, rowTable.GetColumn(key.Columns[0])))
                {
                    return (null, null, false);
                }

                keyValue = (IComparable)rowTable.GetRowFieldValue(rowHandle, rowTable.GetColumn(key.Columns[0]), DefaultValueType.ColumnBased, null);
            }
            else
            {
                bool allNull = true;

                values = new IComparable[key.Columns.Count];

                for (var i = 0; i < key.Columns.Count; i++)
                {
                    var column = key.Columns[i];
                    if (rowTable.GetIsRowColumnNull(rowHandle, rowTable.GetColumn(column)))
                    {
                        values[i] = null;
                    }
                    else
                    {
                        values[i] = (IComparable)rowTable.GetRowFieldValue(rowHandle, rowTable.GetColumn(column), DefaultValueType.ColumnBased, null);

                        allNull = false;
                    }
                }

                if (allNull)
                {
                    return (null, null, false);
                }
            }

            return (values, keyValue, true);
        }

        private IDataEditTransaction CascadeUpdateChildRows(CoreDataTable rowTable,
            int rowHandle,
            [CanBeNull] int columnHandle,
            IComparable proposedValue,
            IComparable currentValue,
            IComparable[] proposedValues,
            IComparable[] currentValues,
            CoreDataTable childTable,
            Action<CoreDataTable, int, int, IComparable> updateRowAct1,
            Action<CoreDataTable, int, IReadOnlyList<int>, IReadOnlyList<IComparable>> updateRowActN,
            bool ignoreDeleted = true,
            bool inTransaction = true
        )
        {
            IDataEditTransaction startTransaction = null;
            
            if (inTransaction && ReferenceEquals(childTable, rowTable) == false)
            {
                startTransaction = childTable.StartTransaction();
            }

            if (m_childKey.Columns.Count == 1)
            {
                var keyValue = currentValue;

                var keyColumn = m_childKey.Columns[0];
                
                var childRowHandles = childTable.FindManyHandles(keyColumn, keyValue, ignoreDeleted: ignoreDeleted).ToData();
                
                if (childRowHandles.Length > 0)
                {
                    var proposedKey = proposedValue;

                    foreach (var childRowHandle in childRowHandles)
                    {
                        if (childTable.RowInCascadeUpdate?.Contains(childRowHandle) ?? false)
                        {
                            continue;
                        }

                        updateRowAct1(childTable, childRowHandle, keyColumn, proposedKey);
                    }
                }
                
                childRowHandles.Dispose();
            }
            else
            {
                var singleColumnChange = proposedValues == null && proposedValue is not null;
                
                var parentFields = m_parentKey.Columns;

                var (searchKey, childFields, multiIndex) =
                    GetChildTableSearchInfo(m_parentKey, 
                        rowTable, 
                        rowHandle, 
                        m_childKey
                        , parentFields,
                        childTable, 
                        (currentValue, columnHandle),
                        currentValues);

                if (multiIndex == null)
                {
                    var strCol = string.Join(",", childFields);

                    throw new ConstraintException($"FK constraint {constraintName} cascade update error. Cannot find multi column index for '{strCol}' columns in {rowTable.TableName} child table defined. Rule {UpdateRule}");
                }

                var searchResult = multiIndex.GetRowHandles(searchKey);

                if (searchResult.Length > 0)
                {
                    if (singleColumnChange)
                    {
                        var columnsCount = m_parentKey.Columns.Count;

                        proposedValues = new IComparable[columnsCount];

                        for (var i = 0; i < columnsCount; i++)
                        {
                            var column = m_parentKey.Columns[i];

                            if (column == columnHandle)
                            {
                                proposedValues[i] = proposedValue;
                            }
                            else
                            {
                                proposedValues[i] = rowTable.GetRowFieldValue(rowHandle, rowTable.GetColumn(column), DefaultValueType.ColumnBased, null) as IComparable;
                            }
                        }
                    }
                    
                    foreach (var childRowHandle in searchResult)
                    {
                        if (childTable.RowInCascadeUpdate?.Contains(childRowHandle) ?? false)
                        {
                            continue;
                        }

                        if (ignoreDeleted && childTable.StateInfo.IsNotDeleted(childRowHandle) == false)
                        {
                            continue;
                        }

                        updateRowActN(childTable, childRowHandle, childFields, proposedValues);
                    }
                }
                
                searchResult.Dispose();
            }

            return startTransaction;
        }

        internal static (IComparable[] searchKey, int[] parentColumnHandles, IndexesOfMany parentIndex)
            GetParentTableSearchInfo(DataKey childKey,
                CoreDataTable childRowTable,
                int childRowHandle,
                IReadOnlyList<int> childFields,
                DataKey parentKey,
                CoreDataTable parentTable,
                IReadOnlyList<object> proposedValues = null)
        {
            var searchKey = new IComparable[childKey.Columns.Count];
            var parentColumnHandles = new int[childKey.Columns.Count];

            if (proposedValues == null)
            {
                for (var i = 0; i < childKey.Columns.Count; i++)
                {
                    var column = childKey.Columns[i];

                    searchKey[i] = childRowTable.GetRowFieldValue(childRowHandle, childRowTable.GetColumn(column), DefaultValueType.ColumnBased, null) as IComparable;

                    parentColumnHandles[i] = parentKey.Columns[i];
                }
            }
            else
            {
                for (var i = 0; i < childKey.Columns.Count; i++)
                {
                    var column = childKey.Columns[i];
                    var index = childFields.FindIndex(column, (field, col) => field == col);

                    if (index >= 0)
                    {
                        searchKey[i] = (IComparable)proposedValues[index];
                    }
                    else
                    {
                        searchKey[i] = childRowTable.GetRowFieldValue(childRowHandle, childRowTable.GetColumn(column), DefaultValueType.ColumnBased, null) as IComparable;
                    }

                    parentColumnHandles[i] = parentKey.Columns[i];
                }
            }

            return (searchKey, parentColumnHandles, parentTable.MultiColumnIndexInfo.TryGetIndex(parentColumnHandles));
        }

        internal static (IComparable[] searchKey, int[] childColumnsHandles, IndexesOfMany childIndex)
            GetChildTableSearchInfo(DataKey parentKey,
                CoreDataTable rowTable,
                int parentRowHandle,
                DataKey childKey,
                IReadOnlyList<int> parentFields,
                CoreDataTable childTable,
                (IComparable proposedValue, int columnHandle)? proposedValue = null,
                IComparable[] proposedValues = null)
        {
            var searchKey = new IComparable[parentKey.Columns.Count];
            var childColumns = new int[parentKey.Columns.Count];

            if (proposedValues == null && proposedValue == null)
            {
                for (var i = 0; i < parentKey.Columns.Count; i++)
                {
                    var column = parentKey.Columns[i];

                    searchKey[i] = rowTable.GetRowFieldValue(parentRowHandle, rowTable.GetColumn(column), DefaultValueType.ColumnBased, null) as IComparable;

                    childColumns[i] = childKey.Columns[i];
                }
            }
            else
            {
                if (proposedValues != null)
                {
                    for (var i = 0; i < parentKey.Columns.Count; i++)
                    {
                        var column = parentKey.Columns[i];
                        var index = parentFields.FindIndex(column, (field, col) => field == col);

                        if (index >= 0)
                        {
                            searchKey[i] = proposedValues[index];
                        }
                        else
                        {
                            if (rowTable.GetIsRowColumnNull(parentRowHandle, rowTable.GetColumn(column)))
                            {
                                searchKey[i] = null;
                            }
                            else
                            {
                                searchKey[i] = (IComparable)rowTable.GetRowFieldValue(parentRowHandle, rowTable.GetColumn(column), DefaultValueType.ColumnBased, null);
                            }
                        }

                        childColumns[i] = childKey.Columns[i];
                    }
                }
                else
                {
                    for (var i = 0; i < parentKey.Columns.Count; i++)
                    {
                        var column = parentKey.Columns[i];

                        if (column == proposedValue.Value.columnHandle)
                        {
                            searchKey[i] = proposedValue.Value.proposedValue;
                        }
                        else
                        {
                            searchKey[i] = rowTable.GetRowFieldValue(parentRowHandle, rowTable.GetColumn(column), DefaultValueType.ColumnBased, null) as IComparable;
                        }

                        childColumns[i] = childKey.Columns[i];
                    }
                }
            }

            return (searchKey, childColumns, childTable.MultiColumnIndexInfo.TryGetIndex(childColumns));
        }

    }
}