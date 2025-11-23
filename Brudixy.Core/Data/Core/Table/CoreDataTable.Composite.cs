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
    partial class CoreDataTable
    {
        public IReadOnlyCollection<CoreDataTable> Tables => TablesMap.Values;

        IEnumerable<ICoreReadOnlyDataTable> ICoreReadOnlyDataTable.Tables => Tables;

        IEnumerable<ICoreDataTable> ICoreDataTable.Tables => Tables;

        [CanBeNull]
        public CoreDataTable TryGetTable([NotNull] string tableName)
        {
            if (TablesMap.TryGetValue(tableName, out var value))
            {
                return value;
            }

            return null;
        }
        
        public bool CheckConstraints()
        {
            bool good = true;
            
            foreach (var dataTable in Tables)
            {
                good &= dataTable.CheckParentForeignKeyConstraints();
            }

            return good;
        }

        ICoreDataTable ICoreDataTable.GetTable(string tableName) => GetTable(tableName);

        ICoreDataTable ICoreDataTable.NewTable(string tableName) => NewTable(tableName);

        ICoreDataTable ICoreDataTable.TryGetTable(string tableName) => TryGetTable(tableName);

        ICoreReadOnlyDataTable ICoreReadOnlyDataTable.GetTable(string tableName) => GetTable(tableName);

        ICoreReadOnlyDataTable ICoreReadOnlyDataTable.TryGetTable(string tableName) => TryGetTable(tableName);

        [NotNull]
        public CoreDataTable GetTable([NotNull] string tableName)
        {
            if (TablesMap.TryGetValue(tableName, out var value))
            {
                return value;
            }

            throw new MissingMetadataException($"Table '{tableName}' is not exist in '{TableName}' dataset.");
        }
        
        [NotNull]
        public CoreDataTable AddTable(string tableName)
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new table with name '{tableName}' to '{TableName}' dataset because it is in readonly.");
            }
            
            if (TablesMap.ContainsKey(tableName))
            {
                throw new InvalidOperationException($"Table '{tableName}' is already exists in DataSet '{TableName}'.");
            }

            var table = CreateTableInstance(tableName);

            table.SetUpDataSet(this);
            
            table.Name = tableName;

            TablesMap[tableName] = table;

            return table;
        }

        [NotNull]
        public CoreDataTable NewTable(string tableName)
        {
            var coreDataTable = CreateTableInstance(tableName);
            
            coreDataTable.Name = tableName;
            
            return  coreDataTable;
        }

        protected Type DataTableType = typeof(CoreDataTable);
        
        protected virtual CoreDataTable CreateTableInstance(string tableName) => (CoreDataTable)Activator.CreateInstance(DataTableType);

        [NotNull]
        public CoreDataTable AddTable([NotNull]CoreDataTable table)
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new table with name '{table.Name}' to '{TableName}' dataset because it is in readonly.");
            }

            return AddTableCore(table);
        }
        
        private CoreDataTable AddTableCore(CoreDataTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }

            if (TablesMap.ContainsKey(table.Name))
            {
                throw new InvalidOperationException($"Table {table.Name} is already exists in DataSet '{TableName}'.");
            }

            table.DataSetReference = new WeakReference<CoreDataTable>(this);

            TablesMap[table.Name] = table;

            return table;
        }
        
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
        
        IDataRelation ICoreReadOnlyDataTable.TryGetDataRelation(string relationName) => TryGetRelation(relationName);

        int ICoreReadOnlyDataTable.TablesCount => TablesMap?.Count ?? 0;

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
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove relation {relationName} from {TableName} dataset because it is in readonly.");
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

        IDataRelation ICoreDataTable.AddRelation(
            [NotNull] string relationName,
            [NotNull] string parentTable,
            [NotNull] string childTable,
            [NotNull] IReadOnlyList<(string parentColumn, string childColumn)> key,
            RelationType relationType = RelationType.OneToMany,
            Rule constraintUpdate = Rule.None,
            Rule constraintDelete = Rule.None,
            AcceptRejectRule acceptRejectRule = AcceptRejectRule.None)
        {
            var columns = new List<(CoreDataColumn parentColumn, CoreDataColumn childColumn)>();

            var pt = GetTable(parentTable);
            var ct = GetTable(childTable);

            for (var i = 0; i < key.Count; i++)
            {
                var parent = key[i];
                var child = key[i];
                
                columns.Add((pt.GetColumn(parent.parentColumn), ct.GetColumn(child.childColumn)));
            }
            
            return AddRelation(relationName, columns, relationType, constraintUpdate, constraintDelete, acceptRejectRule);
        }
        

        public DataRelation AddRelation(
            [NotNull] string relationName,
            [NotNull] IReadOnlyList<(CoreDataColumn parentColumn, CoreDataColumn childColumn)> columns,
            RelationType relationType = RelationType.OneToMany,
            Rule constraintUpdate = Rule.None,
            Rule constraintDelete = Rule.None,
            AcceptRejectRule acceptRejectRule = AcceptRejectRule.None)
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new relation with name {relationName} to {TableName} dataset because it is in readonly.");
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
                    $"Cannot create a multi column '{relationName}' relation in '{TableName}' dataset because parent or child table not found in this dataset.");
            }

            var dataRelation = new DataRelation(relationName, parentColumns, childColumns)
            {
                Type = relationType
            };

            dataRelation.SetDataSet(this);
            
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

        IDataRelation ICoreDataTable.AddRelation(
            [NotNull] string relationName,
            [NotNull] (string parentTable, string parentColumn) parentKey,
            [NotNull] (string childTable, string childColumn) childKey,
            RelationType relationType = RelationType.OneToMany,
            Rule constraintUpdate = Rule.None,
            Rule constraintDelete = Rule.None,
            AcceptRejectRule acceptRejectRule = AcceptRejectRule.None)
        {
            var parentTable = GetTable(parentKey.parentTable);
            var childTable = GetTable(childKey.childTable);
            
            return AddRelation(relationName, parentTable.GetColumn(parentKey.parentColumn), childTable.GetColumn(childKey.childColumn), relationType, constraintUpdate, constraintDelete, acceptRejectRule);
        }

        public DataRelation AddRelation(
            [NotNull] string relationName, 
            [NotNull] CoreDataColumn parentColumn,
            [NotNull] CoreDataColumn childColumn,
            RelationType relationType = RelationType.OneToMany,
            Rule constraintUpdate = Rule.None,
            Rule constraintDelete = Rule.None,
            AcceptRejectRule acceptRejectRule = AcceptRejectRule.None)
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new relation with name {relationName} to {TableName} dataset because it is in readonly.");
            }
            
            if (parentColumn.Type != childColumn.Type)
            {
                throw new InvalidOperationException($"Cannot add relation {relationName} to {TableName} dataset. Column types mismatch. Parent {parentColumn.ColumnName} {parentColumn.Type} != Child {childColumn.ColumnName} {childColumn.Type}");
            }

            var parentTable = parentColumn.DataTable;
            var childTable = childColumn.DataTable;

            var parentTableCol = parentColumn;
            var childTableCol = childColumn;
            
            var dataRelation = new DataRelation(relationName, parentTableCol, childTableCol)
            {
                Type = relationType
            };
            
            dataRelation.SetDataSet(this);

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
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new relation with name {relation.relationName} to {TableName} dataset because it is in readonly.");
            }

            AddRelationCore(relation);
        }

        internal void AddRelationCore(DataRelation relation)
        {
            if (ContainsRelation(relation.relationName))
            {
                throw new InvalidOperationException(
                    $"Cannot add new relation with name {relation.relationName} is already exist in {TableName} dataset.");
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

            relation.SetDataSet(this);
        }

        internal void ChangeRelationName(DataRelation relation, string oldRelationName, string newRelationName)
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot change relation name from '{newRelationName}' to '{oldRelationName}' in {TableName} dataset because it is in readonly.");
            }
            
            if (RelationsMap is not null)
            {
                if (RelationsMap.TryGetValue(newRelationName, out var registeredRel) && ReferenceEquals(registeredRel, relation) == false)
                {
                    throw new InvalidOperationException(
                        $"Cannot change relation name '{oldRelationName}' to '{newRelationName}' because is already exist in '{TableName}' in dataset."); 
                }
                
                RelationsMap[newRelationName] = RelationsMap[oldRelationName];
                RelationsMap.Remove(oldRelationName);

                relation.ParentTable?.ChangeChildRelationName(relation, oldRelationName, newRelationName);
                relation.ChildTable?.ChangeParentRelationName(relation, oldRelationName, newRelationName);
            }
        }
        
        private void CloneComposite(CoreDataTable clone, bool withData)
        {
            foreach (var dataTable in TablesMap.Values)
            {
                var copyTo = clone.TryGetTable(dataTable.Name);

                if (copyTo == null)
                {
                    var table = withData ? dataTable.Copy(this.SourceThread) : dataTable.Clone(this.SourceThread);
                    
                    clone.AddTable(table);
                }
                else
                {
                    if (withData)
                    {
                        copyTo.MergeData(dataTable);
                    }
                }
            }

            CopyRelationsTo(clone);
        }

        public bool HasTable(string tableName) => TablesMap.ContainsKey(tableName);

        private void CopyRelationsTo(CoreDataTable dataSet)
        {
            foreach (var dataRelation in RelationsMap)
            {
                var relation = dataRelation.Value;

                if (dataSet.RelationsMap.ContainsKey(dataRelation.Key) == false)
                {
                    if (ReferenceEquals(relation.ParentTable, relation.ChildTable) == false)
                    {
                        var relationClone = relation.CloneDs(dataSet);

                        var pt = relationClone.ParentTable?.BeginLoadCore();
                        var ct = relationClone.ChildTable?.BeginLoadCore();

                        dataSet.AddRelationCore(relationClone);
                        
                        pt?.EndLoad();
                        ct?.EndLoad();
                    }
                }
            }
        }

        protected void AcceptChangesComposite()
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot accept {TableName} dataset changes because it is in readonly.");
            }

            foreach (var table in TablesMap.Values)
            {
                table.AcceptChanges();
            }
        }

        protected void RejectChangesComposite()
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot reject {TableName} dataset changes because it is in readonly.");
            }
            
            foreach (var table in TablesMap.Values)
            {
                table.RejectChanges();
            }
            
            ClearTableLoggedChanges();
        }

        private enum LoadTablesMode
        {
            SchemaOnly,
            DataOnly,
            Full
        }

        public void ClearData()
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException($"Cannot clear {TableName} dataset because it is in readonly.");
            }
            
            foreach (var table in TablesMap.Values)
            {
                table.ClearRows();
            }
            
            ClearRows();
        }

        protected void DisposeComposite()
        {
            if (TablesMap != null)
            {
                foreach (var table in TablesMap)
                {
                    table.Value.Dispose();
                }

                TablesMap.Dispose();
            }

            if (RelationsMap != null)
            {
                foreach (var kv in RelationsMap)
                {
                    kv.Value.Dispose();
                }

                RelationsMap.Dispose();
            }

            ExtProperties?.Dispose();
        }

        public void DropTable(string tableName)
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove table with name '{tableName}' from '{TableName}' dataset because it is in readonly.");
            }
            
            if (TablesMap.ContainsKey(tableName) == false)
            {
                throw new MissingMetadataException($"Cannot remove table because table '{tableName}' doesn't exists in DataSet '{TableName}'.");
            }

            var dataTable = GetTable(tableName);

            if (dataTable.IsBuildin == false)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove table with name '{tableName}' from '{TableName}' dataset because it is buildin.");
            }

            var dataRelations = GetTableRelations().ToData();

            var findIndex = dataRelations.FindIndex<IDataRelation, ICoreReadOnlyDataTable>(dataTable, (r, t) => ReferenceEquals(r.ChildTable , t) || ReferenceEquals(r.ParentTable , t));

            if (findIndex >= 0)
            {
                var dataRelation = dataRelations[findIndex];

                throw new InvalidOperationException(
                    $"Cannot remove table with name '{tableName}' from '{TableName}' dataset because of '{dataRelation.Name}' relation.");
            }

            TablesMap.Remove(tableName);
            
            dataTable.Dispose();
        }
        
        protected internal void MergeSchema(CoreDataTable source, bool addTables = true)
        {
            foreach (var sourceTable in source.Tables)
            {
                var currentTable = TryGetTable(sourceTable.Name);

                if (currentTable == null)
                {
                    if (addTables)
                    {
                        var clone = sourceTable.Clone();

                        AddTable(clone);
                    }
                }
                else
                {
                    MergeTableMetadata(currentTable, sourceTable);
                }
            }

            if (source.RelationsMap != null)
            {
                foreach (var sourceRelation in source.RelationsMap.Values)
                {
                    var childTable = TryGetTable(sourceRelation.ChildTableName);
                    var parentTable = TryGetTable(sourceRelation.ParentTableName);

                    if (childTable == null || parentTable == null)
                    {
                        if (addTables == false)
                        {
                            continue;
                        }

                        if (parentTable == null)
                        {
                            throw new MissingMetadataException(
                                $"Can't add relation '{sourceRelation.RelationName}' using parent table '{sourceRelation.ParentTableName}' because it doesn't exist. ");
                        }

                        throw new MissingMetadataException(
                            $"Can't add relation '{sourceRelation.RelationName}' using child table '{sourceRelation.ChildTableName}' because it doesn't exist. ");
                    }


                    var columns = new Data<(CoreDataColumn parentColumn, CoreDataColumn childColumn)>();

                    var childColumns = sourceRelation.ChildColumnNames.Select(childTable.GetColumn).ToData();
                    var parentColumns = sourceRelation.ParentColumnNames.Select(parentTable.GetColumn).ToData();

                    for (int i = 0; i < sourceRelation.ChildColumnsCount; i++)
                    {
                        columns.Add((parentColumns[i], childColumns[i]));
                    }

                    var constraintUpdate = Rule.None;
                    var constraintDelete = Rule.None;
                    var acceptRejectRule = AcceptRejectRule.None;

                    if (sourceRelation.ChildKeyConstraint != null)
                    {
                        constraintUpdate = sourceRelation.ChildKeyConstraint.UpdateRule;
                        constraintDelete = sourceRelation.ChildKeyConstraint.DeleteRule;
                        acceptRejectRule = sourceRelation.ChildKeyConstraint.AcceptRejectRule;
                    }

                    AddRelation(sourceRelation.relationName,
                        columns,
                        sourceRelation.Type,
                        constraintUpdate,
                        constraintDelete,
                        acceptRejectRule);

                    childColumns.Dispose();
                    parentColumns.Dispose();
                    columns.Dispose();
                }
            }

            foreach (var prop in source.XProperties)
            {
                SetXProperty(prop, source.GetXProperty<object>(prop));
            }
        }

        public void MergeDatasetMeta(CoreDataTable dataSet)
        {
            MergeSchema(dataSet, true);
        }
        
        protected void MergeDatasetData(CoreDataTable dataSet, bool overrideExisting = true)
        {
            foreach (var table in Tables)
            {
                var sourceTable = dataSet.TryGetTable(table.Name);

                if (sourceTable != null)
                {
                    MergeTable(overrideExisting, table, sourceTable);
                }
            }
        }

        protected void FullDatasetMerge(CoreDataTable dataSet)
        {
            MergeDatasetMeta(dataSet);
            MergeDatasetData(dataSet);
        }
    }
}