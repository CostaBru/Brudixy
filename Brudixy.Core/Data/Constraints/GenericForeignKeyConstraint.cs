using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.Constraints
{
    internal interface IComparableForeignKeyConstraint<T> where T: IComparable
    {
        IDataEditTransaction CascadeUpdate(CoreDataTable rowTable, int rowHandle, T[] proposedValues, T[]  currentValues, bool inTransaction);
        
        IDataEditTransaction CascadeUpdate(CoreDataTable rowTable, int rowHandle, int columnHandle, ref T proposedValue, ref T currentValue, bool inTransaction);
        
        bool CheckCurrentConstraint(CoreDataTable rowTable, int rowHandle, DataRowAction action, IReadOnlyList<int> fields, T[]  proposedValues);

        bool CheckCurrentConstraint(CoreDataTable rowTable, int rowHandle, DataRowAction action, ColumnHandle columnHandle, ref T proposedValues);
    }

    public class TypedComparableForeignKeyConstraint<T> : ForeignKeyConstraint, IComparableForeignKeyConstraint<T> where T: IComparable
    {
        public TypedComparableForeignKeyConstraint()
        {
        }
        
        public TypedComparableForeignKeyConstraint(CoreDataTable parentTable, CoreDataTable childTable, CoreDataColumn parentColumn, CoreDataColumn childColumn) : base(parentTable, childTable, parentColumn, childColumn)
        {
        }

        public TypedComparableForeignKeyConstraint(string constraintName, CoreDataTable parentTable, CoreDataTable childTable, CoreDataColumn parentColumn, CoreDataColumn childColumn) : base(constraintName, parentTable, childTable, parentColumn, childColumn)
        {
        }

        internal TypedComparableForeignKeyConstraint(CoreDataTable parentTable, CoreDataTable childTable, Data<CoreDataColumn> parentColumns, Data<CoreDataColumn> childColumns) : base(parentTable, childTable, parentColumns, childColumns)
        {
        }

        internal TypedComparableForeignKeyConstraint(string constraintName, CoreDataTable parentTable, CoreDataTable childTable, Data<CoreDataColumn> parentColumns, Data<CoreDataColumn> childColumns) : base(constraintName, parentTable, childTable, parentColumns, childColumns)
        {
        }
        
        public IDataEditTransaction CascadeUpdate(CoreDataTable rowTable, int rowHandle, int columnHandle, ref T? proposedValue, ref T? currentValue, bool inTransaction)
        {
            return CascadeUpdateCore(rowTable,  rowHandle, columnHandle, proposedValue, currentValue, null, null, inTransaction);
        }
        
        public IDataEditTransaction CascadeUpdate(CoreDataTable rowTable, int rowHandle, T[]  proposedValues, T[]  currentValues, bool inTransaction)
        {
            return CascadeUpdateCore(rowTable,  rowHandle,  -1, default, default, proposedValues, currentValues, inTransaction);
        }

        internal IDataEditTransaction  CascadeUpdateCore(CoreDataTable rowTable, int rowHandle, int columnHandle, T proposedValue, T currentValue, T[] proposedValues, T[] currentValues, bool inTransaction)
        {
            if (currentValue == null && currentValues == null)
            {
                return null;
            }
            
            if (UpdateRule == Rule.Cascade && m_parentKey.Count == 1 && currentValue != null)
            {
                if (rowTable.GetIsRowColumnNull(rowHandle, rowTable.GetColumn(m_parentKey.Columns[0])))
                {
                    return null;
                }

                var childTable = m_childKey.Table;
                
                if (childTable == null)
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
                    var childColumnHandle = m_childKey.Columns[0];
                    
                    var childRowHandles = childTable
                        .FindManyHandles(childColumnHandle, currentValue)
                        .ToData();

                    if (childRowHandles.Count > 0)
                    {
                        IDataEditTransaction startTransaction = null;
                        
                        if (inTransaction)
                        {
                            startTransaction = childTable.StartTransaction();
                        }

                        foreach (var childRowHandle in childRowHandles)
                        {
                            if (childTable.RowInCascadeUpdate?.Contains(childRowHandle) ?? false)
                            {
                                continue;
                            }

                            var childCol = childTable.GetColumn(childColumnHandle);
                            
                            var prevValue = childTable.GetRowFieldValue<T>(childRowHandle, childCol, DefaultValueType.ColumnBased, default);

                            var typedDataItem = (ITypedDataItem<T>)childCol.DataStorageLink;

                            childTable.SetRowColumnValue<T>(childRowHandle, typedDataItem, childCol, ref proposedValue, ref prevValue, null);
                        }

                        return startTransaction;
                    }
                }
                finally
                {
                    rowTable.RowInCascadeUpdate.Remove(rowHandle);
                }
            }
            else
            {
                if (currentValues != null)
                {
                    var objValues = new IComparable[proposedValues.Length];
                    var curValues = new IComparable[currentValues.Length];

                    for (var i = 0; i < proposedValues.Length; i++)
                    {
                        objValues[i] = proposedValues[i];
                    }

                    for (var i = 0; i < currentValues.Length; i++)
                    {
                        curValues[i] = currentValues[i];
                    }

                    var transaction = base.CascadeUpdate(rowTable, rowHandle, columnHandle, null, null, objValues, curValues, inTransaction);
                   return transaction;
                }
                else
                {
                    var transaction =  base.CascadeUpdate(rowTable, rowHandle, columnHandle, proposedValue, currentValue, null, null, inTransaction);
                    return transaction;
                }
            }

            return null;
        }
        
        internal override IDataEditTransaction CascadeDelete(CoreDataTable rowTable, int rowHandle, bool inTransaction)
        {
            if (UpdateRule == Rule.Cascade && m_parentKey.Count == 1)
            {
                if (rowTable.GetIsRowColumnNull(rowHandle, rowTable.GetColumn(m_parentKey.Columns[0])))
                {
                    return null;
                }

                var childTable = m_childKey.Table;
                
                if (childTable == null)
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
                    var keyValue = rowTable.GetRowFieldValue<T>(rowHandle, rowTable.GetColumn(m_parentKey.Columns[0]), DefaultValueType.ColumnBased, default);

                    var childRowHandles = childTable.FindManyHandles(m_childKey.Columns[0], keyValue).ToData();

                    IDataEditTransaction transaction = null;
                    
                    if (childRowHandles.Length > 0)
                    {
                        if (inTransaction)
                        {
                            transaction = childTable.StartTransaction();
                        }

                        foreach (var childRowHandle in childRowHandles)
                        {
                            if (childTable.RowInCascadeUpdate?.Contains(childRowHandle) ?? false)
                            {
                                continue;
                            }

                            childTable.DeleteRow(childRowHandle);
                        }

                        return transaction;
                    }
                }
                finally
                {
                    rowTable.RowInCascadeUpdate.Remove(rowHandle);
                }
            }
            else
            {
                var transaction = base.CascadeDelete(rowTable, rowHandle, inTransaction);
                return transaction;
            }

            return null;
        }


        public bool CheckCurrentConstraint(CoreDataTable rowTable, int rowHandle, DataRowAction action, IReadOnlyList<int> columnHandles, T[] proposedValues)
        {
            var parentTable = m_parentKey.Table;

            if (proposedValues != null && (parentTable?.GetEnforceConstraints() ?? false) && m_parentKey.Columns.Count == 1)
            {
                var parentRow = DataRelation.GetParentRowHandle(parentTable, m_parentKey, ChildKey, rowHandle);

                if (parentRow != null && (parentTable.RowInCascadeUpdate?.Contains(parentRow.Value) ?? false))
                {
                    var parentKeyValues = parentTable.GetRowFieldValue<T>(parentRow.Value, parentTable.GetColumn(m_parentKey.Columns[0]), DefaultValueType.ColumnBased, default);

                    if (parentKeyValues.CompareTo(proposedValues[0]) == 0)
                    {
                        return true;
                    }
                }

                // now check to see if someone exists... it will have to be in a parent row's current, not a proposed. 
                var childValues = rowTable.GetRowFieldValue<T>(rowHandle, rowTable.GetColumn(m_childKey.Columns[0]), DefaultValueType.ColumnBased, default);

                var parentKeyColumn = m_parentKey.Columns[0];

                if (!parentTable.IndexInfo.IndexMappings.TryGetValue(parentKeyColumn, out var parentIndexIndex))
                {
                    var parentColumnName = parentTable.DataColumnInfo.Columns[parentKeyColumn].ColumnName;

                    throw new InvalidOperationException(
                        $"FK constraint check {constraintName} violation occurred. No parent '{parentTable.Name}' with column '{parentColumnName}' is not indexed.");
                }

                var rowHandles = parentTable.IndexInfo.GetRowHandles(parentIndexIndex, childValues);
                    
                if (rowHandles.Any(r => parentTable.StateInfo.IsNotDeletedAndRemoved(r)) == false)
                {
                    var parentColumnName = parentTable.DataColumnInfo.Columns[parentKeyColumn].ColumnName;
                    
                    throw new InvalidOperationException(
                        $"FK constraint check {constraintName} violation occurred. No parent '{parentTable.Name}' with key '{parentColumnName}' and '{childValues}' value exists.");
                }
            }

            return CheckConstraint(rowTable, rowHandle, action, columnHandles, proposedValues.OfType<object>().ToArray());
        }

        public bool CheckCurrentConstraint(CoreDataTable rowTable, int rowHandle,  DataRowAction action, ColumnHandle columnHandle, ref T proposedValues)
        {
            return CheckCurrentConstraint(rowTable, rowHandle, action,  new int[] {columnHandle.Handle}, new T[] {proposedValues});
        }
    }
}