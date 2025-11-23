using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    partial class CoreDataTable
    {
        public IDataRelation AddNestedRelation(
            [NotNull] string relationName, 
            [NotNull] string parentColName,
            [NotNull] string childColName,
            Rule constraintUpdate = Rule.None,
            Rule constraintDelete = Rule.None,
            AcceptRejectRule acceptRejectRule = AcceptRejectRule.None)
        {
            var childRelationsMap = ChildRelationsMap;
            
            if (childRelationsMap == null)
            {
                ChildRelationsMap = childRelationsMap = new (StringComparer.OrdinalIgnoreCase);
            }

            var parentRelationsMap = ParentRelationsMap;
            
            if (parentRelationsMap == null)
            {
                ParentRelationsMap = parentRelationsMap = new (StringComparer.OrdinalIgnoreCase);
            }

            var parentColumn = GetColumn(parentColName);
            var childColumn = GetColumn(childColName);

            if (parentColumn.Type != childColumn.Type)
            {
                throw new InvalidOperationException($"Column types mismatch. Parent {parentColumn.ColumnName} {parentColumn.Type} != Child {childColumn.ColumnName} {childColumn.Type}");
            }
            
            var dataRelation = new DataRelation(relationName, parentColumn, childColumn);

            dataRelation.Type = RelationType.OneToMany;
            
            childRelationsMap[relationName] = dataRelation;
            parentRelationsMap[relationName] = dataRelation;
            
            AddIndex(parentColumn, unique: true);
            AddIndex(childColumn, unique: false);
            
            RegisterParentRelation(relationName, parentColumn.ColumnHandle);
            RegisterChildRelation(relationName, childColumn.ColumnHandle);

            if (constraintUpdate != Rule.None || constraintDelete != Rule.None)
            {
                var foreignKeyConstraint = CoreDataTable.CreateForeignKeyConstraint(parentColumn.Type);
                
                var parentColumns = new Data<CoreDataColumn> {parentColumn};
                var childColumns = new Data<CoreDataColumn> {childColumn};

                foreignKeyConstraint.Create("FK_NESTED_" +  relationName, this, this, parentColumns, childColumns);
                
                foreignKeyConstraint.DeleteRule = constraintDelete;
                foreignKeyConstraint.UpdateRule = constraintUpdate;
                foreignKeyConstraint.AcceptRejectRule = acceptRejectRule;

                dataRelation.ParentKeyConstraint = foreignKeyConstraint;
                dataRelation.ChildKeyConstraint = foreignKeyConstraint;
                
                parentColumns.Dispose();
                childColumns.Dispose();
            }

            return dataRelation;
        }

        public void AddParentRelation([NotNull] DataRelation relation)
        {
            if (relation == null)
            {
                throw new ArgumentNullException(nameof(relation));
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new '{relation.relationName}' parent relation to the '{TableName}'table because table is readonly.");
            }
            
            if (ParentRelationsMap == null)
            {
                ParentRelationsMap = new (StringComparer.OrdinalIgnoreCase);
            }

            ParentRelationsMap[relation.relationName] = relation;

            if (ReferenceEquals(relation.ChildTable, this))
            {
                foreach (var column in relation.ChildColumns)
                {
                    RegisterParentRelation(relation.relationName, column.ColumnHandle);
                }
            }
            else
            {
                foreach (var parentColumn in relation.ParentColumns)
                {
                    RegisterParentRelation(relation.relationName, parentColumn.ColumnHandle);
                }
            }
        }

        public void AddChildRelation([NotNull] DataRelation relation)
        {  
            if (relation == null)
            {
                throw new ArgumentNullException(nameof(relation));
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new '{relation.relationName}' child relation to the '{TableName}'table because table is readonly.");
            }
            
            if (ChildRelationsMap == null)
            {
                ChildRelationsMap = new (StringComparer.OrdinalIgnoreCase);
            }

            ChildRelationsMap[relation.relationName] = relation;
            
            if (ReferenceEquals(relation.ParentTable, this))
            {
                foreach (var column in relation.ParentColumns)
                {
                    RegisterChildRelation(relation.relationName, column.ColumnHandle);
                }
            }
            else
            {
                foreach (var column in relation.ChildColumns)
                {
                    RegisterParentRelation(relation.relationName, column.ColumnHandle);
                }
            }
        }

        public CoreDataRow GetParentRow(string idColumn, string parentIdColumn, IComparable parentIdValue)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(parentIdColumn, out var dataColumn))
            {
                return (CoreDataRow)GetRow(dataColumn.ColumnHandle, (IComparable)parentIdValue);
            }

            return null;
        }

        public IEnumerable<T> GetParentRows<T>(IComparable parentIdValue) where T : CoreDataRow
        { 
            var parentRelations = ParentNestedRelations.FirstOrDefault();

            if (parentRelations != null && parentRelations.ParentKey.Columns?.Count > 0)
            {
                return GetParentRows<T>(new ColumnHandle(parentRelations.ParentKey.Columns[0]), parentIdValue);
            }

            return Enumerable.Empty<T>();
        }

        public IEnumerable<int> GetParentRowHandles(string idColumn, IComparable parentIdValue)
        {
            return FindManyHandles(GetColumn(idColumn).ColumnHandle, (IComparable)parentIdValue);
        }
        
        public IEnumerable<int> GetParentRowHandles(ColumnHandle idColumn, IComparable parentIdValue)
        {
            return FindManyHandles(idColumn.Handle, (IComparable)parentIdValue);
        }

        public IEnumerable<T> GetParentRows<T>(CoreDataColumn idColumn, IComparable parentIdValue) where T : CoreDataRow
        {
            return GetRows<T, IComparable>(idColumn.ColumnName, (IComparable)parentIdValue);
        }
        
        public IEnumerable<T> GetParentRows<T>(ColumnHandle idColumn, IComparable parentIdValue) where T : CoreDataRow
        {
            return GetRows<T, IComparable>(GetColumn(idColumn.Handle), (IComparable)parentIdValue);
        }

        public IEnumerable<T> GetParentRows<T>(string idColumn, IComparable parentIdValue) where T : CoreDataRow
        {
            return GetRows<T, IComparable>(idColumn, (IComparable)parentIdValue);
        }

        public IEnumerable<int> GetChildrenHandles(string idColumn, IComparable IdValue)
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(idColumn, out var columnIndex))
            {
                var columnHandle = GetColumn(idColumn).ColumnHandle;
                
                return GetChildrenHandles(new ColumnHandle(columnHandle), IdValue);
            }

            return Enumerable.Empty<int>();
        }

        public IEnumerable<int> GetChildrenHandles(ColumnHandle columnHandle, IComparable IdValue)
        {
            return FindManyHandles(columnHandle.Handle, (IComparable)IdValue);
        }

        public IEnumerable<T> GetChildren<T>(string idColumn, IComparable IdValue) where T : CoreDataRow
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(idColumn, out var columnIndex))
            {
                return GetRows<T, IComparable>(idColumn, (IComparable)IdValue);
            }

            return Enumerable.Empty<T>();
        }

        public IEnumerable<T> GetChildren<T>(ColumnHandle idColumn, IComparable IdValue) where T : CoreDataRow
        {
            return GetRows<T, IComparable>(GetColumn(idColumn.Handle), (IComparable)IdValue);
        }

        public IEnumerable<T> GetChildren<T>(ICoreDataTableColumn idColumn, IComparable IdValue) where T : CoreDataRow
        {
            if (DataColumnInfo.ColumnMappings.TryGetValue(idColumn.ColumnName, out var columnIndex))
            {
                return GetRows<T, IComparable>(idColumn.ColumnName, IdValue);
            }

            return Enumerable.Empty<T>();
        }

        public IEnumerable<int> GetAllChildrenHandles(string idColumn, string parentIdColumn, IComparable IdValue)
        {
            bool loop = false;

            var idCol = GetColumn(idColumn).ColumnHandle;
            var parentIdCol = GetColumn(parentIdColumn).ColumnHandle;

            return GetAllChildrenHandles(new ColumnHandle(idCol), new ColumnHandle(parentIdCol), IdValue, out loop);
        }
        
        public IEnumerable<int> GetAllChildrenHandles([NotNull] ColumnHandle[] idColumn, [NotNull] ColumnHandle[] parentIdColumn,
            [NotNull] IComparable[] idValue)
        {
            if (idColumn == null)
            {
                throw new ArgumentNullException(nameof(idColumn));
            }

            if (parentIdColumn == null)
            {
                throw new ArgumentNullException(nameof(parentIdColumn));
            }

            if (idValue == null)
            {
                throw new ArgumentNullException(nameof(idValue));
            }

            if (idColumn.Length != parentIdColumn.Length)
            {
                throw new ArgumentException("Id and ParentId column arrays should have same size.");
            }
            
            if (idColumn.Length != idValue.Length)
            {
                throw new ArgumentException("Id and value arrays should have same size.");
            }

            bool loop = false;

            return GetAllChildrenHandles(idColumn.Select(c => c.Handle).ToArray(), parentIdColumn.Select(c => c.Handle).ToArray(), idValue, out loop);
        }

        private IEnumerable<int> GetAllChildrenHandles(ColumnHandle idColumn, ColumnHandle parentIdColumn, object IdValue, out bool loop)
        {
            loop = false;

            var row = GetRowHandle(GetColumn(idColumn.Handle), (IComparable)IdValue);

            if (row < 0)
            {
                return Enumerable.Empty<int>();
            }

            var capacity = StateInfo.RowStorageCount;

            var queue = new Queue<int>(capacity);
            var result = new Data<int>(capacity);
            var proceeded = new Set<object>();

            queue.Enqueue(row);

            while (queue.Any())
            {
                var dequeuedRow = queue.Dequeue();

                var idValue = GetRowFieldValue(dequeuedRow, GetColumn(idColumn.Handle), DefaultValueType.ColumnBased, null);

                if (proceeded.ContainsKey(idValue) == false)
                {
                    result.Add(dequeuedRow);

                    proceeded.Add(idValue);

                    var parentIdValue = (IComparable)GetRowFieldValue(dequeuedRow, GetColumn(parentIdColumn.Handle), DefaultValueType.ColumnBased, null);

                    if (parentIdValue != null)
                    {
                        foreach (var childRow in GetChildrenHandles(idColumn, parentIdValue))
                        {
                            queue.Enqueue(childRow);
                        }
                    }
                }
                else
                {
                    loop = true;
                }
            }
            
            proceeded.Dispose();

            return result;
        }
        
        private IEnumerable<int> GetAllChildrenHandles(IReadOnlyList<int> idColumn, IReadOnlyList<int> parentIdColumn, IComparable[] IdValue, out bool loop)
        {
            loop = false;
            
            var indexes = MultiColumnIndexInfo.GetColumnFirstIndex(idColumn);

            if (indexes == 0)
            {
                return Array.Empty<int>();
            }

            var row = MultiColumnIndexInfo.GetRowHandle(this, indexes, IdValue);

            if (row < 0)
            {
                return Array.Empty<int>();
            }

            var capacity = StateInfo.RowStorageCount;

            var queue = new Queue<int>(capacity);
            var result = new Data<int>(capacity);
            var proceeded = new Set<object>();

            queue.Enqueue(row);

            while (queue.Any())
            {
                var dequeuedRow = queue.Dequeue();
                
                var idValue = new IComparable[idColumn.Count];

                for (int i = 0; i < idColumn.Count; i++)
                {
                    idValue[i] = (IComparable)this.GetRowFieldValue(dequeuedRow, GetColumn(idColumn[i]), DefaultValueType.ColumnBased, null);
                }
                
                if (proceeded.ContainsKey(idValue) == false)
                {
                    result.Add(dequeuedRow);

                    proceeded.Add(idValue);

                    var parentIdValue = new IComparable[idColumn.Count];

                    int nullCount = 0;
                    
                    for (int i = 0; i < idColumn.Count; i++)
                    {
                      if(GetIsRowColumnNull(dequeuedRow, GetColumn(parentIdColumn[i])))
                      {
                          nullCount++;
                          continue;
                      }

                      idValue[i] = (IComparable)this.GetRowFieldValue(dequeuedRow, GetColumn(parentIdColumn[i]), DefaultValueType.ColumnBased, null);
                    }
                    
                    if (nullCount != parentIdColumn.Count)
                    {
                        foreach (var childRow in MultiColumnIndexInfo.GetRowHandles(indexes, parentIdValue))
                        {
                            if (this.StateInfo.IsNotDeletedAndRemoved(childRow))
                            {
                                queue.Enqueue(childRow);
                            }
                        }
                    }
                }
                else
                {
                    loop = true;
                }
            }
            
            proceeded.Dispose();

            return result;
        }

        public void CheckForLoops(int rowHandle)
        {
            var relations = ParentNestedRelations;

            foreach (var relation in relations)
            {
                if (relation.ParentKey.Count == 1)
                {
                    var idColumn = relation.ParentKey.Columns[0];
                    var parentIdColumn = relation.ChildKey.Columns[0];

                    var key = this.GetRowFieldValue(rowHandle, GetColumn(parentIdColumn), DefaultValueType.ColumnBased, null);

                    bool loop = false;

                    GetAllChildrenHandles(new ColumnHandle(idColumn), new ColumnHandle(parentIdColumn), key, out loop);

                    if (loop)
                    {
                        throw new InvalidOperationException(
                            $"Loops found in nested relation {relation.RelationName}. Column {parentIdColumn} ");
                    }
                }
                else
                {
                        
                    var idColumn = relation.ParentKey.Columns;
                    var parentIdColumn = relation.ChildKey.Columns;

                    var key = new IComparable[relation.ParentKey.Columns.Count];

                    for (int i = 0; i < relation.ParentKey.Columns.Count; i++)
                    {
                         key[i] = (IComparable)this.GetRowFieldValue(rowHandle, GetColumn(parentIdColumn[i]), DefaultValueType.ColumnBased, null);
                    }


                    bool loop = false;

                    GetAllChildrenHandles(idColumn, parentIdColumn, key, out loop);

                    if (loop)
                    {
                        throw new InvalidOperationException(
                            $"Loops found in nested relation {relation.RelationName}. Column {parentIdColumn} ");
                    }
                }
            }
        }
    }
}
