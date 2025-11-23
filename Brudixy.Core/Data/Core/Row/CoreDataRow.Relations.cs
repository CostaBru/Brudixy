using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Index;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataRow
    {
        [NotNull]
        public IEnumerable<T> GetChildRows<T>(string relationName)  where T: ICoreDataRowReadOnlyAccessor
        {
            var rowState = RowRecordState;
            
            if (table != null && rowState != RowState.Detached)
            {
                return GetChildRows<T>(table.GetChildRelation(relationName), table, rowState != RowState.Deleted);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public IEnumerable<T> GetChildRows<T>(IDataRelation relation) where T: ICoreDataRowReadOnlyAccessor
        {
            return GetChildRowsCore<T>(relation);
        }

        internal IReadOnlyList<T> GetChildRowsCore<T>(IDataRelation relation) where T: ICoreDataRowReadOnlyAccessor
        {
            var rowState = RowRecordState;

            if (table != null && rowState != RowState.Detached)
            {
                return GetChildRows<T>(relation, table, rowState != RowState.Deleted);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [CanBeNull]
        public int GetParentRowHandle(string relationName)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return GetParentRowHandle(table.GetParentRelation(relationName), table);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [CanBeNull]
        public int GetParentRowHandle(DataRelation relation)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return GetParentRowHandle(relation, table);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }
        
        [CanBeNull]
        public int GetParentRowHandle(IDataRelation relation)
        {
            if (table != null && relation is DataRelation dr && ReferenceEquals(this.table, dr.ChildTable))
            {
                if (RowRecordState != RowState.Detached)
                {
                    return GetParentRowHandle(dr, table);
                }
            }

            return GetParentRowHandle(relation.Name);
        }

        [CanBeNull]
        public CoreDataRow GetParentRow(string relationName)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return GetParentRow(table.GetParentRelation(relationName), table);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [CanBeNull]
        public ICoreDataRowReadOnlyAccessor GetParentRow(IDataRelation relation)
        {
            if (table != null && relation is DataRelation dr && ReferenceEquals(this.table, dr.ChildTable))
            {
                if (RowRecordState != RowState.Detached)
                {
                    return GetParentRow(dr, table);
                }
            }

            return GetParentRow(relation.Name);
        }

        [CanBeNull]
        public ICoreDataRowReadOnlyAccessor GetParentRow(DataRelation relation)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return GetParentRow(relation, table);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public IEnumerable<CoreDataRow> GetParentRows(string relationName)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return GetParentRows(table.GetParentRelation(relationName), table);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        [NotNull]
        public IEnumerable<CoreDataRow> GetParentRows(DataRelation relation)
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return GetParentRows(relation, table);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }
        
        [NotNull]
        public IEnumerable<T> GetParentRows<T>(IDataRelation relation) where T : ICoreDataRowReadOnlyAccessor
        {
            return relation.GetParentRows<T>(this.RowHandleCore);
        }

        private CoreDataRow GetParentRow(DataRelation relation, CoreDataTable table)
        {
            if (relation == null)
            {
                return null;
            }

            var relationDataset = relation.Parent;

            if (relationDataset != null && relationDataset != table.Parent)
            {
                throw new MissingMetadataException($"Dataset reference mismatch for {relation.RelationName}.");
            }

            var parentRowHandle = DataRelation.GetParentRowHandle(relation.ParentTable, relation.ParentKey, relation.ChildKey, this.RowHandleCore);

            if (parentRowHandle.HasValue)
            {
                return relation.ParentTable.GetRow(parentRowHandle.Value);
            }
            
            return null;
        }

        private int GetParentRowHandle(DataRelation relation, CoreDataTable table)
        {
            if (relation == null)
            {
                return -1;
            }

            var dataSet = table.Parent;
            var relationDataset = relation.Parent;

            if (relationDataset != null && relationDataset != dataSet)
            {
                throw new MissingMetadataException($"Dataset reference mismatch for {relation.RelationName}.");
            }

            return DataRelation.GetParentRowHandle(relation.ParentTable, relation.ParentKey, relation.ChildKey, this.RowHandleCore) ?? -1;
        }

        public IEnumerable<T> GetChildRows<T>(string parentKey, string childKey) where T : CoreDataRow
        {
            if (table is not null && RowRecordState != RowState.Detached)
            {
                return table.GetChildren<T>(childKey, (IComparable)this[parentKey]);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public IEnumerable<T> GetChildRows<T>(ICoreDataTableColumn idColumn, ICoreDataTableColumn parentIdColumn) where T : CoreDataRow
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var childId = (IComparable)this[idColumn];
                
                return GetChildRows<T>(parentIdColumn, childId);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        private IEnumerable<T> GetChildRows<T>(ICoreDataTableColumn parentIdColumn, IComparable childId) where T : CoreDataRow
        {
            return table.GetChildren<T>(parentIdColumn, childId);
        }

        public IEnumerable<T> GetParentRows<T>(string parentKey, string childKey) where T : CoreDataRow
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                return table.GetParentRows<T>(parentKey, (IComparable)this[childKey]);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        public IEnumerable<T> GetParentRows<T>(CoreDataColumn parentKey, CoreDataColumn childKey) where T : CoreDataRow
        {
            if (table != null && RowRecordState != RowState.Detached)
            {
                var parentid = (IComparable)this[childKey];
                
                return GetParentRows<T>(parentKey, parentid);
            }

            throw new DataDetachedException($"{DebugKeyValue}");
        }

        private IEnumerable<T> GetParentRows<T>(CoreDataColumn parentKey, IComparable parentid) where T : CoreDataRow
        {
            return table.GetRows<T, IComparable>(parentKey.ColumnName, parentid);
        }

        public IEnumerable<T> GetAllParentRows<T>(string keyField, string parentField, bool addCurrent = false) where T : CoreDataRow
        {
            var keyColumn = TryGetColumnCore(keyField) as CoreDataColumn;
            var parentColumn = TryGetColumnCore(parentField) as CoreDataColumn;

            if (keyColumn == null || parentColumn == null)
            {
                return Array.Empty<T>();
            }

            return GetAllParentRows<T>(addCurrent, keyColumn, parentColumn);
        }

        public Data<T> GetAllParentRows<T>(CoreDataColumn keyField, CoreDataColumn parentField, bool addCurrent = false) where T : CoreDataRow
        {
            return GetAllParentRows<T>(addCurrent, keyField, parentField);
        }

        private Data<T> GetAllParentRows<T>(bool addCurrent, CoreDataColumn keyColumn, CoreDataColumn parentColumn) where T : CoreDataRow
        {
            var proceded = new Set<object>();

            var storage = new Data<T>();

            Data<T> allParentsPath = new Data<T>();
            
            var queue = new Qu<T>(storage);

            if (addCurrent)
            {
                queue.Enqueue((T)(object)this);
            }
            else
            {
                queue.EnqueueRange(GetParentRows<T>(keyColumn, parentColumn));
            }

            while (queue.Any)
            {
                var currentElement = queue.Dequeue();

                var key = currentElement[keyColumn];

                if (proceded.Contains(key))
                {
                    continue;
                }
                
                allParentsPath.Add(currentElement);
                
                var parentRows =  GetParentRows<T>(keyColumn, (IComparable)currentElement[parentColumn]);

                foreach (var parentRow in parentRows)
                {
                    queue.Enqueue(parentRow);
                }

                proceded.Add(key);
            }

            proceded.Dispose();
            queue.Clear();
            
            storage.Dispose();

            return allParentsPath;
        }

        public IEnumerable<T> GetAllChildRows<T>(string keyField, string parentField, bool addCurrent = false) where T : CoreDataRow
        {
            var keyColumn = TryGetColumnCore(keyField);
            var parentColumn = TryGetColumnCore(parentField);

            if (keyColumn == null || parentColumn == null)
            {
                return Array.Empty<T>();
            }

            return GetAllChildRows<T>(keyColumn, parentColumn, addCurrent);
        }

        protected Data<T> GetAllChildRows<T>(ICoreDataTableColumn keyColumn, ICoreDataTableColumn parentColumn, bool addCurrent = false) where T : CoreDataRow
        {
            var proceded = new Set<IComparable>();

            var storage = new Data<T>();

            Data<T> allChilds = new Data<T>();
            
            var queue = new Qu<T>(storage);

            if (addCurrent)
            {
                queue.Enqueue((T)(object)this);
            }
            else
            {
                queue.EnqueueRange(GetChildRows<T>(keyColumn, parentColumn));
            }

            while (queue.Any)
            {
                var currentElement = queue.Dequeue();

                var key = (IComparable)currentElement[keyColumn];

                if (proceded.Contains(key))
                {
                    continue;
                }
                
                allChilds.Add(currentElement);

                var childRows = GetChildRows<T>(parentColumn, (IComparable)currentElement[keyColumn]);

                foreach (var childRow in childRows)
                {
                    queue.Enqueue(childRow);
                }
                proceded.Add(key);
            }

            proceded.Dispose();
            queue.Clear();
            
            storage.Dispose();

            return allChilds;
        }
        
        public Data<T> GetAllChildRows<T>(string relationName, bool addCurrent = false) where T: ICoreDataRowReadOnlyAccessor
        {
            var dataTable = table;

            var childRelation = dataTable.GetChildRelation(relationName);

            if (childRelation == null)
            {
                throw new InvalidOperationException(
                    $"Child relation {relationName} wasn't found in {table.TableName} table.");
            }

            var proceded = new Set<int>();
            
            var storage = new Data<T>();

            Data<T> allChilds = new Data<T>();
            
            var queue = new Qu<T>(storage);

            if (addCurrent)
            {
                queue.Enqueue((T)(object)this);
            }
            else
            {
                queue.EnqueueRange(GetChildRows<T>(relationName));
            }

            while (queue.Any)
            {
                var currentElement = queue.Dequeue();

                if (proceded.Contains(currentElement.RowHandle))
                {
                    continue;
                }
                
                allChilds.Add(currentElement);

                var childRows = childRelation.GetChildRows<T>(currentElement.RowHandle, true);

                foreach (var childRow in childRows)
                {
                    queue.Enqueue(childRow);
                }
                proceded.Add(currentElement.RowHandle);
            }

            proceded.Dispose();
            queue.Clear();
            
            storage.Dispose();

            return allChilds;
        }

        protected Data<CoreDataRow> GetParentRows(DataRelation relation, CoreDataTable table)
        {
            var parentRowHandles = GetParentRowHandles(relation, table);

            var dataRows = new Data<CoreDataRow>();

            foreach (var ph in parentRowHandles)
            {
                dataRows.Add(relation.ParentTable.GetRow(ph));
            }
            
            parentRowHandles.Dispose();

            return dataRows;
        }

        protected Data<int> GetParentRowHandles(DataRelation relation, CoreDataTable table)
        {
            if (relation == null)
            {
                return new Data<int>();
            }

            var dataSet = table.Parent;
            var relationDataset = relation.Parent;

            if (relationDataset != null && relationDataset != dataSet)
            {
                throw new MissingMetadataException($"Dataset reference mismatch for {relation.RelationName}.");
            }

            return DataRelation.GetParentRowHandles(relation.ParentTable, relation.ParentKey, relation.ChildKey, this.RowHandleCore);
        }

        protected IReadOnlyList<T> GetChildRows<T>(IDataRelation relation, CoreDataTable table, bool ignoreDeleted) where T: ICoreDataRowReadOnlyAccessor
        {
            return relation.GetChildRows<T>(this.RowHandleCore, ignoreDeleted);
        }
       
        public void AddChildRows<T>(string relationName, IEnumerable<T> rows) where T : ICoreDataRowAccessor
        {
            if (this.table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup parent row reference for '{DebugKeyValue}' row of '{table.Name}' table because table is readonly.");
            }

            DataRelation dataRelation = table.ChildRelationsMap?.GetOrDefault(relationName);

            if (dataRelation == null)
            {
                throw new ArgumentNullException($"Cannot add child row reference for '{DebugKeyValue}' row of '{table.Name}' table because the given relation '{relationName}' is missing.");
            }
            
            AddChildRows(dataRelation, rows);
        }

        public void AddChildRows<T>(DataRelation relation, IEnumerable<T> rows) where T : ICoreDataRowAccessor
        {
            var parentColumns = relation.ParentColumns.ToArray();
            var childColumns = relation.ChildColumns.ToArray();

            if (parentColumns.Length == 1)
            {
                foreach (var coreDataTableRow in rows)
                {
                    coreDataTableRow[childColumns[0]] = this[parentColumns[0]];
                }
            }
            else
            {
                var transaction = this.StartTransaction();
                    
                try
                {
                    foreach (var coreDataTableRow in rows)
                    {
                        for (var index = 0; index < parentColumns.Length; index++)
                        {
                            coreDataTableRow[childColumns[index]] = this[parentColumns[index]];
                        }
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        
        public void RemoveChildRows<T>(string relationName, IEnumerable<T> rows) where T : ICoreDataRowAccessor
        {
            if (this.table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup parent row reference for '{DebugKeyValue}' row of '{table.Name}' table because table is readonly.");
            }

            DataRelation dataRelation = table.ChildRelationsMap?.GetOrDefault(relationName);

            if (dataRelation == null)
            {
                throw new ArgumentNullException($"Cannot add child row reference for '{DebugKeyValue}' row of '{table.Name}' table because the given relation '{relationName}' is missing.");
            }
            
            RemoveChildRows(dataRelation, rows);
        }
        
        public void RemoveChildRows<T>(DataRelation relation, IEnumerable<T> rows) where T : ICoreDataRowAccessor
        {
            var parentColumns = relation.ParentColumns.ToArray();
            var childColumns = relation.ChildColumns.ToArray();

            if (parentColumns.Length == 1)
            {
                if (IsNull(parentColumns[0]))
                {
                    return;
                }
                
                var pk = (IComparable)this[parentColumns[0]];

                foreach (var coreDataTableRow in rows)
                {
                    var ck = (IComparable)coreDataTableRow[childColumns[0]];

                    if (ck is not null && ck.CompareTo(pk) == 0)
                    {
                        coreDataTableRow.SetNull(childColumns[0]);
                    }
                }
            }
            else
            {
                var pk = new IComparable[parentColumns.Length];

                bool allNull = true;
                    
                for (var index = 0; index < parentColumns.Length; index++)
                {
                    var comparable = (IComparable)this[parentColumns[index]];
                        
                    pk[index] = comparable;

                    if (comparable is not null)
                    {
                        allNull = false;
                    }
                }

                if (allNull)
                {
                    return;
                }
                
                var transaction = this.StartTransaction();
                    
                try
                {
                    var ck = new IComparable[parentColumns.Length];
                    
                    foreach (var childRow in rows)
                    {
                        for (var index = 0; index < parentColumns.Length; index++)
                        {
                            ck[index] = (IComparable)childRow[childColumns[index]];
                        }

                        if (MultiColumnBisectIndex.Compare(pk, ck) == 1)
                        {
                            for (var index = 0; index < parentColumns.Length; index++)
                            {
                                childRow.SetNull(childColumns[index]);
                            }
                        }
                        
                        Array.Clear(ck, 0, ck.Length);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            }
        }
        
        public void ClearChildRows(string relationName)
        {
            if (this.table == null || RowRecordState == RowState.Detached)
            {
                throw new DataDetachedException($"{DebugKeyValue}");
            }
            
            if (table.IsInitializing == false && table.IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup parent row reference for '{DebugKeyValue}' row of '{table.Name}' table because table is readonly.");
            }

            DataRelation dataRelation = table.ChildRelationsMap?.GetOrDefault(relationName);

            if (dataRelation == null)
            {
                throw new ArgumentNullException($"Cannot add child row reference for '{DebugKeyValue}' row of '{table.Name}' table because the given relation '{relationName}' is missing.");
            }
            
            RemoveChildRows(dataRelation, GetChildRows<ICoreDataRowAccessor>(relationName).ToArray());
        }
    }
}