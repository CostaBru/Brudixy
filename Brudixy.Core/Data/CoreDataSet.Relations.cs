using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Constraints;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataSet
    {
        public bool ContainsRelation(string relationName)
        {
            var relationContains = RelationsMap?.ContainsKey(relationName);

            if (relationContains ?? false)
            {
                return true;
            }

            foreach (var dataTable in Tables)
            {
                var dataRelation = dataTable.ParentRelationsMap?.GetOrDefault(relationName);

                if (dataRelation != null)
                {
                    return true;
                }

                dataRelation = dataTable.ChildRelationsMap?.GetOrDefault(relationName);

                if (dataRelation != null)
                {
                    return true;
                }
            }

            return false;
        }

        internal IEnumerable<DataRelation> TableRelations
        {
            get
            {
                var relations = new Set<DataRelation>();

                foreach (var dataRelation in RelationsMap.Values)
                {
                    if (relations.IsMissing(dataRelation))
                    {
                        if (dataRelation.ParentTableName != dataRelation.ChildTableName)
                        {
                            yield return dataRelation;
                            relations.Add(dataRelation);
                        }
                    }
                }

                foreach (var dataTable in Tables)
                {
                    if (dataTable.ParentRelationsMap is not null)
                    {
                        foreach (var dataRelation in dataTable.ParentRelationsMap.Values)
                        {
                            if (relations.IsMissing(dataRelation))
                            {
                                if (dataRelation.ParentTableName != dataRelation.ChildTableName)
                                {
                                    yield return dataRelation;
                                    relations.Add(dataRelation);
                                }
                            }
                        }
                    }
                    
                    if (dataTable.ChildRelationsMap is not null)
                    {
                        foreach (var dataRelation in dataTable.ChildRelationsMap.Values)
                        {
                            if (relations.IsMissing(dataRelation))
                            {
                                if (dataRelation.ParentTableName != dataRelation.ChildTableName)
                                {
                                    yield return dataRelation;
                                    relations.Add(dataRelation);
                                }
                            }
                        }
                    }
                }
                
                relations.Dispose();
            }
        }

        private IEnumerable<IDataRelation> GetTableRelations()
        {
            foreach (var dataTable in Tables)
            {
                if (dataTable.ParentRelationsMap != null)
                {
                    foreach (var relation in dataTable.ParentRelationsMap.Values)
                    {
                        yield return relation;
                    }
                }

                if (dataTable.ChildRelationsMap != null)
                {
                    foreach (var relation in dataTable.ChildRelationsMap.Values)
                    {
                        yield return relation;
                    }
                }
            }
        }

        public IEnumerable<IDataRelation> Relations => GetTableRelations().Distinct();

        [CanBeNull]
        public DataRelation TryGetRelation(string relationName)
        {
            if (ContainsRelation(relationName) == false)
            {
                return null;
            }

            foreach (var dataTable in Tables)
            {
                if (dataTable.ParentRelationsMap != null && dataTable.ParentRelationsMap.TryGetValue(relationName, out var value))
                {
                    return value;
                }

                if (dataTable.ChildRelationsMap != null && dataTable.ChildRelationsMap.TryGetValue(relationName, out value))
                {
                    return value;
                }
            }

            return null;
        }

        public void RemoveRelation(string relationName)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove relation {relationName} from {DataSetName} dataset because it is in readonly.");
            }
            
            var dataRelation = RelationsMap.GetOrDefault(relationName);

            if (dataRelation != null)
            {
                RelationsMap?.Remove(relationName);

                foreach (var dataTable in Tables)
                {
                    dataTable.RemoveRelation(dataRelation);
                }

                dataRelation?.Dispose();
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataRelation ICoreReadOnlyDataSet.TryGetDataRelation(string relationName)
        {
            return TryGetRelation(relationName);
        }

        public IDataRelation AddRelation(
            [NotNull] string relationName,
            [NotNull] IReadOnlyList<(ICoreDataTableColumn parentColumn, ICoreDataTableColumn childColumn)> columns,
            RelationType relationType = RelationType.OneToMany,
            Rule constraintUpdate = Rule.None,
            Rule constraintDelete = Rule.None,
            AcceptRejectRule acceptRejectRule = AcceptRejectRule.None)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new relation with name {relationName} to {DataSetName} dataset because it is in readonly.");
            }
            
            if (columns.Count == 0)
            {
                return null;
            }
            
            if (columns.Count == 1)
            {
                return AddRelation(relationName, 
                    columns[0].parentColumn, 
                    columns[0].childColumn,
                    relationType,
                    constraintUpdate, 
                    constraintDelete, 
                    acceptRejectRule);
            }

            CoreDataTable parentTable = null;
            CoreDataTable childTable = null;

            var parentColumns = new CoreDataColumn[columns.Count];
            var childColumns = new CoreDataColumn[columns.Count];

            for (var i = 0; i < columns.Count; i++)
            {
                var column = columns[i];
                
                parentTable = GetTable(column.parentColumn.TableName);

                parentColumns[i] = parentTable.GetColumn(column.parentColumn.ColumnName);

                if (parentTable.ChildRelationsMap is null)
                {
                    parentTable.ChildRelationsMap = new(StringComparer.OrdinalIgnoreCase);
                }

                childTable = GetTable(column.childColumn.TableName);

                childColumns[i] = childTable.GetColumn(column.childColumn.ColumnName);

                if (childTable.ParentRelationsMap is null)
                {
                    childTable.ParentRelationsMap = new(StringComparer.OrdinalIgnoreCase);
                }
            }

            if (parentTable == null || childTable == null)
            {
                throw new MissingMetadataException(
                    $"Cannot create a multi column '{relationName}' relation in '{DataSetName}' dataset because parent or child table not found in this dataset.");
            }

            var dataRelation = new DataRelation(relationName, parentColumns, childColumns)
            {
                Type = relationType
            };

            //dataRelation.SetDataSet(this);
            
            RelationsMap[relationName] = dataRelation;

            parentTable.ChildRelationsMap[relationName] = dataRelation;
            childTable.ParentRelationsMap[relationName] = dataRelation;

            DataRelation.SetupMultiColumnRelationType(relationType, parentTable, parentColumns, childTable, childColumns);
            
            if (constraintUpdate != Rule.None || constraintDelete != Rule.None)
            {
                var foreignKeyConstraint = new ForeignKeyConstraint();

                foreignKeyConstraint.Create("FK_" + relationType + "_"  + relationName, parentTable, childTable, parentColumns, childColumns);

                foreignKeyConstraint.DeleteRule = constraintDelete;
                foreignKeyConstraint.UpdateRule = constraintUpdate;
                foreignKeyConstraint.AcceptRejectRule = acceptRejectRule;

                dataRelation.ParentKeyConstraint = foreignKeyConstraint;
                dataRelation.ChildKeyConstraint = foreignKeyConstraint;

                foreach (var parentColumn in parentColumns)
                {
                    parentTable.RegisterChildRelation(relationName, parentColumn.ColumnHandle);
                }
                
                foreach (var childColumn in childColumns)
                {
                    childTable.RegisterParentRelation(relationName, childColumn.ColumnHandle);
                }
            }

            return dataRelation;
        }

        public IDataRelation AddRelation(
            [NotNull] string relationName, 
            [NotNull] ICoreDataTableColumn parentColumn,
            [NotNull] ICoreDataTableColumn childColumn,
            RelationType relationType = RelationType.OneToMany,
            Rule constraintUpdate = Rule.None,
            Rule constraintDelete = Rule.None,
            AcceptRejectRule acceptRejectRule = AcceptRejectRule.None)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new relation with name {relationName} to {DataSetName} dataset because it is in readonly.");
            }
            
            if (parentColumn.Type != childColumn.Type)
            {
                throw new InvalidOperationException($"Cannot add relation {relationName} to {DataSetName} dataset. Column types mismatch. Parent {parentColumn.ColumnName} {parentColumn.Type} != Child {childColumn.ColumnName} {childColumn.Type}");
            }

            var parentTable = GetTable(parentColumn.TableName);
            var childTable = GetTable(childColumn.TableName);

            var parentTableCol = parentTable.GetColumn(parentColumn.ColumnName);
            var childTableCol = childTable.GetColumn(childColumn.ColumnName);
            
            var dataRelation = new DataRelation(relationName, parentTableCol, childTableCol)
            {
                Type = relationType
            };
            
            //dataRelation.SetDataSet(this);

            RelationsMap[relationName] = dataRelation;

            parentTable.ChildRelationsMap ??= new(StringComparer.OrdinalIgnoreCase);
            parentTable.ChildRelationsMap[relationName] = dataRelation;
           
            childTable.ParentRelationsMap ??= new(StringComparer.OrdinalIgnoreCase);
            childTable.ParentRelationsMap[relationName] = dataRelation;

            DataRelation.SetupSingleColumnRelationType(relationType, parentTable, parentTableCol, childTable, childTableCol);
            
            if (constraintUpdate != Rule.None || constraintDelete != Rule.None)
            {
                var foreignKeyConstraint = CoreDataTable.CreateForeignKeyConstraint(parentColumn.Type);
                
                var parentColumns = new CoreDataColumn[] {parentTableCol};
                var childColumns = new CoreDataColumn[]  {childTableCol};

                foreignKeyConstraint.Create("FK_" + relationType + "_"  + relationName, parentTable, childTable, parentColumns, childColumns);

                foreignKeyConstraint.DeleteRule = constraintDelete;
                foreignKeyConstraint.UpdateRule = constraintUpdate;
                foreignKeyConstraint.AcceptRejectRule = acceptRejectRule;

                dataRelation.ParentKeyConstraint = foreignKeyConstraint;
                dataRelation.ChildKeyConstraint = foreignKeyConstraint;

                parentTable.RegisterChildRelation(relationName, parentTableCol.ColumnHandle);
                childTable.RegisterParentRelation(relationName, childTableCol.ColumnHandle);
            }

            return dataRelation;
        }

        public void AddRelation(DataRelation relation)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new relation with name {relation.relationName} to {DataSetName} dataset because it is in readonly.");
            }

            AddRelationCore(relation);
        }

        internal void AddRelationCore(DataRelation relation)
        {
            if (ContainsRelation(relation.relationName))
            {
                throw new InvalidOperationException(
                    $"Cannot add new relation with name {relation.relationName} is already exist in {DataSetName} dataset.");
            }

            var parentTable = relation.ParentTable;
            var childTable = relation.ChildTable;

            if (parentTable == null)
            {
                throw new ArgumentNullException("parentTable");
            }

            if (childTable == null)
            {
                throw new ArgumentNullException("childTable");
            }

            childTable.AddParentRelation(relation);
            parentTable.AddChildRelation(relation);

            RelationsMap[relation.relationName] = relation;

            if (relation.ParentColumnsCount == 1)
            {
                DataRelation.SetupSingleColumnRelationType(relation.Type,
                    parentTable,
                    parentTable.GetColumn(relation.ParentColumns.First().ColumnName),
                    childTable,
                    childTable.GetColumn(relation.ChildColumns.First().ColumnName));
            }
            else
            {
                DataRelation.SetupMultiColumnRelationType(relation.Type,
                    parentTable,
                    relation.ParentColumns,
                    childTable,
                    relation.ChildColumns);
            }

            //relation.SetDataSet(this);
        }

        internal void ChangeRelationName(DataRelation relation, string oldRelationName, string newRelationName)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change relation name from '{newRelationName}' to '{oldRelationName}' in {DataSetName} dataset because it is in readonly.");
            }
            
            if (RelationsMap is not null)
            {
                if (RelationsMap.TryGetValue(newRelationName, out var registeredRel) && ReferenceEquals(registeredRel, relation) == false)
                {
                    throw new InvalidOperationException(
                        $"Cannot change relation name '{oldRelationName}' to '{newRelationName}' because is already exist in '{DataSetName}' in dataset."); 
                }
                
                RelationsMap[newRelationName] = RelationsMap[oldRelationName];
                RelationsMap.Remove(oldRelationName);

                relation.ParentTable?.ChangeChildRelationName(relation, oldRelationName, newRelationName);
                relation.ChildTable?.ChangeParentRelationName(relation, oldRelationName, newRelationName);
            }
        }
    }
}