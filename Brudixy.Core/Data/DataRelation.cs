using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Brudixy.Interfaces;
using Brudixy.Constraints;
using Brudixy.Exceptions;
using Brudixy.Index;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class DataRelation : IDataRelation
    {
        private bool m_checkMultipleNested = true;
        
        internal DataKey ChildKeyRef;
        internal DataKey ParentKeyRef;

        internal string relationName = "";
        internal bool m_nested;
        
        internal RelationType m_type;

        public string ParentTableName => ParentTable?.Name ?? string.Empty;

        public string ChildTableName => ChildTable?.Name ?? string.Empty;


        IEnumerable<ICoreDataTableColumn> IDataRelation.ParentColumns => ParentColumns;

        IEnumerable<ICoreDataTableColumn> IDataRelation.ChildColumns => ChildColumns;

        public IEnumerable<CoreDataColumn> ChildColumns
        {
            get
            {
                CheckStateForProperty(nameof(ChildColumns));
                return ChildKeyRef.ColumnsReference;
            }
        }

        public int ChildColumnsCount => ChildKeyRef.Columns.Count;

        internal IEnumerable<CoreDataColumn> ChildColumnsReference
        {
            get
            {
                CheckStateForProperty(nameof(ChildColumnsReference));
                return ChildKeyRef.ColumnsReference;
            }
        }

        internal DataKey ChildKey
        {
            get
            {
                return ChildKeyRef;
            }
            set
            {
                ChildKeyRef = value;
            }
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreReadOnlyDataTable IDataRelation.ParentTable => ParentTable;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreReadOnlyDataTable IDataRelation.ChildTable => ChildTable;

        public CoreDataTable ChildTable => ChildKeyRef.Table;

        internal IEnumerable<string> ParentColumnNames => ParentKeyRef.GetColumnNames();

        internal IEnumerable<string> ChildColumnNames => ChildKeyRef.GetColumnNames();

        public IEnumerable<CoreDataColumn> ParentColumns => ParentKeyRef.ColumnsReference;

        public int ParentColumnsCount => ParentKeyRef.Columns.Count;

        internal IEnumerable<CoreDataColumn> ParentColumnsReference => ParentKeyRef.ColumnsReference;

        internal DataKey ParentKey => ParentKeyRef;

        string IDataRelation.Name => relationName;

        public CoreDataTable ParentTable => ParentKeyRef.Table;
        
        public CoreDataTable Parent { get; set; }

        [DefaultValue("")]
        public string RelationName
        {
            get
            {
                return relationName;
            }
            set
            {
                value ??= string.Empty;

                if (string.Compare(relationName, value, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    Parent.ChangeRelationName(this, relationName, value);

                    ParentTable?.ChangeChildRelationName(this, relationName, value);
                    ChildTable?.ChangeParentRelationName(this, relationName, value);

                    relationName = value;
                }
                else
                {
                    if (string.CompareOrdinal(relationName, value) == 0)
                    {
                        return;
                    }

                    relationName = value;
                }
            }
        }

        [DefaultValue(false)]
        public bool Nested
        {
            get
            {
                return m_nested;
            }
            set
            {
                if (m_nested == value)
                {
                    return;
                }

                if (value)
                {
                    ValidateMultipleNestedRelations();
                }

                var parentTable = ParentKeyRef.Table;
                var childTable = ChildKeyRef.Table;

                var childTableDataSet = childTable?.Parent;
                var parentTableDataSet = parentTable?.Parent;

                if (value)
                {
                    CheckNestedRelations();

                    if (parentTable != null && ReferenceEquals(parentTable, childTable))
                    {
                        foreach (var dataRowHandles in childTable.RowsHandles)
                        {
                            childTable.CheckForLoops(dataRowHandles);
                        }

                        childTable.m_nestedInDataset = false;
                    }
                }

                m_nested = value;

                if (childTableDataSet == null || !value || !string.IsNullOrEmpty(childTable?.Namespace) ||
                    childTableDataSet.ContainsRelation(RelationName))
                {
                    return;
                }

                string nameSpace = null;

                var parentRelations = childTable?.ParentRelationsMap;

                if (parentRelations != null)
                {
                    foreach (DataRelation dataRelation in parentRelations.Values)
                    {
                        if (dataRelation.Nested)
                        {
                            var relationParentTable = dataRelation?.ParentTable;

                            if (nameSpace == null)
                            {
                                nameSpace = relationParentTable?.Namespace;
                            }
                            else if (string.Compare(nameSpace, relationParentTable?.Namespace,
                                StringComparison.Ordinal) != 0)
                            {
                                m_nested = false;
                                throw new ArgumentException(
                                    $"Namespaces should be the same for {childTable.Name} child table and {relationParentTable?.Namespace} parent table.");
                            }
                        }
                    }
                }
            }
        }

        internal bool CheckMultipleNested
        {
            get { return m_checkMultipleNested; }
            set { m_checkMultipleNested = value; }
        }

        public ForeignKeyConstraint ChildKeyConstraint { get; set; }

        public ForeignKeyConstraint ParentKeyConstraint { get; set; }

        public RelationType Type
        {
            get => m_type;
            set => m_type = value;
        }

        public DataRelation(string relationName, CoreDataColumn parentColumn, CoreDataColumn childColumn)
        {
            var parentColumns = new int[] { parentColumn.ColumnHandle };
            var childColumns = new int[]{ childColumn.ColumnHandle };

            Create(relationName, parentColumns, parentColumn.DataTable, childColumns, childColumn.DataTable);
        }

        public DataRelation(string relationName, IReadOnlyList<CoreDataColumn> parentColumns,
            IReadOnlyList<CoreDataColumn> childColumns)
        {
            var pHandles = parentColumns.Select(p => p.ColumnHandle).ToArray();
            var cHandles = childColumns.Select(p => p.ColumnHandle).ToArray();

            Create(relationName, pHandles, parentColumns[0].DataTable, cHandles, childColumns[0].DataTable);
        }
        
        public DataRelation(string relationName, IReadOnlyList<int> parentColumns, CoreDataTable parentTable,
            IReadOnlyList<int> childColumns, CoreDataTable childTable)
        {
            Create(relationName, parentColumns, parentTable, childColumns, childTable);
        }

        internal static Data<int> GetChildRowHandles(CoreDataTable parentTable,
            CoreDataTable childTable,
            DataKey parentKey,
            DataKey childKey,
            int parentRowHandle,
            bool ignoreDeleted)
        {
            if (childKey.Columns.Count == 1)
            {
                var parentCol = parentTable.GetColumn(parentKey.Columns[0]);

                if (parentTable.GetIsRowColumnNull(parentRowHandle, parentCol))
                {
                    return new Data<int>(0);
                }

                var keyValue = (IComparable)parentTable.GetRowFieldValue(parentRowHandle, parentCol, DefaultValueType.ColumnBased, null);

                var columnHandle = childKey.Columns[0];

                var rowHandles = childTable.FindManyHandles(columnHandle, keyValue, ignoreDeleted: ignoreDeleted)
                    .ToData();
             
                return rowHandles;
            }

            var parentFields = parentKey.Columns;

            var (searchKey, childFields, multiIndex) =
                ForeignKeyConstraint.GetChildTableSearchInfo(parentKey, parentKey.Table, parentRowHandle, childKey, parentFields, childTable);

            if (multiIndex == null)
            {
                var strCol = string.Join(",", childFields);

                throw new ConstraintException(
                    $"Cannot get child rows of table '{childTable.Name}' using multi column key. Cannot find multi column index for '{strCol}' columns in {childTable.Name} child table defined.");
            }

            var searchResult = multiIndex.GetRowHandles(searchKey);

            var parents = new Data<int>(searchResult.Count);

            foreach (var rowHandle in searchResult)
            {
                if ((ignoreDeleted && childTable.StateInfo.IsNotRemoved(rowHandle)) || (ignoreDeleted == false &&
                    childTable.StateInfo.IsNotDeletedAndRemoved(rowHandle)))
                {
                    parents.Add(rowHandle);
                }
            }

            return parents;
        }

        internal static Data<int> GetParentRowHandles(CoreDataTable parentTable, DataKey parentKey, DataKey childKey,
            int childRowHandle)
        {
            var childTable = childKey.Table;

            if (childKey.Columns.Count == 1)
            {
                var parentId = childTable.GetRowFieldValue(childRowHandle, childTable.GetColumn(childKey.Columns[0]), DefaultValueType.ColumnBased, null);

                return parentTable.GetParentRowHandles(new ColumnHandle(parentKey.Columns[0]), (IComparable)parentId).ToData();
            }
            else
            {
                var comparables = new IComparable[parentKey.Columns.Count];

                for (int i = 0; i < comparables.Length; i++)
                {
                    comparables[i] = (IComparable) childTable.GetRowFieldValue(childRowHandle, childTable.GetColumn(childKey.Columns[i]), DefaultValueType.ColumnBased, null);
                }
                
                var columnHandles = parentKey.Columns.Select(c => new ColumnHandle(c)).ToArray();
                
                return parentTable.FindManyHandles(columnHandles, comparables, true).ToData();
            }
        }

        internal static int? GetParentRowHandle(CoreDataTable parentTable, DataKey parentKey, DataKey childKey, int childRowHandle)
        {
            if (parentKey.Columns.Count == 1)
            {
                var childTable = childKey.Table;

                if (childTable.GetIsRowColumnNull(childRowHandle, childTable.GetColumn(childKey.Columns[0])))
                {
                    return null;
                }

                var parentId = childTable.GetRowFieldValue(childRowHandle, childTable.GetColumn(childKey.Columns[0]), DefaultValueType.ColumnBased, null);
                
                var parents = parentTable.GetParentRowHandles(new ColumnHandle(parentKey.Columns[0]), (IComparable)parentId).ToData();

                if (parents.Count > 1)
                {
                    var parentKeyTable = parentKey.Table;
                    var childKeyTable = childTable;

                    throw new InvalidOperationException(
                        $"Multiple parents ({parents.Count}) found for {parentKeyTable.DataColumnInfo.Columns[parentKey.Columns[0]].ColumnName} parent key and {childKeyTable.DataColumnInfo.Columns[childKey.Columns[0]].ColumnName} child key {parentId} value.");
                }
                
                if (parents.Count == 1)
                {
                    var parent = parents[0];
                    
                    parents.Dispose();
                    
                    return parent;
                }

                return null;
            }
            else
            {
                var childFields = childKey.Columns;

                var (searchKey, parentColumns, multiIndex) =
                    ForeignKeyConstraint.GetParentTableSearchInfo(childKey, childKey.Table, childRowHandle, childFields, parentKey,
                        parentTable);

                if (multiIndex == null)
                {
                    var strCol = string.Join(",", parentColumns);

                    throw new ConstraintException(
                        $"Cannot get parent row of table '{parentTable.Name}' using multi column key. Cannot find multi column index for '{strCol}' columns in {parentTable.Name} parent table defined.");
                }

                var searchResult = multiIndex.GetRowHandles(searchKey);

                var parents = new Data<int>(searchResult.Count);

                foreach (var rowHandle in searchResult)
                {
                    if (parentTable.StateInfo.IsNotDeletedAndRemoved(rowHandle))
                    {
                        parents.Add(rowHandle);
                    }
                }

                searchResult.Dispose();

                if (parents.Count > 1)
                {
                    var strCol = string.Join(",", parentColumns);
                    var chlCol = string.Join(",", childFields);
                    var val = MultiColumnBisectIndex.ValueToString(searchKey, parentColumns, parentTable);

                    throw new InvalidOperationException(
                        $"Multiple parents ({parents.Count}) found for '{strCol}' parent key, '{chlCol}' child key '{val}' value.");
                }

                if (parents.Count == 1)
                {
                    var parent = parents[0];
                    
                    parents.Dispose();
                    
                    return parent;
                }

                return null;
            }
        }

        internal void SetDataSet(CoreDataTable dataSet)
        {
            Parent = dataSet;
        }

        internal void CheckNestedRelations()
        {
            var parentTable = ParentKeyRef.Table;
            var childTable = ChildKeyRef.Table;

            var list = new Set<string>();
            list.Add(childTable.Name);

            foreach (var dataRelation in childTable.ParentRelations ?? Enumerable.Empty<DataRelation>())
            {
                var relationParentTable = dataRelation.ParentTable;
                var relationChildTable = dataRelation.ChildTable;

                if (ReferenceEquals(relationParentTable, childTable) &&
                    ReferenceEquals(relationChildTable, childTable) == false)
                {
                    throw new InvalidOperationException(
                        $"Relation {relationName} validation failed: Loop found in Nested Relations for {childTable.Name}.");
                }

                list.Add(relationParentTable.Name);
            }

            list.Dispose();
        }

        internal void CheckState()
        {
            var parentTable = ParentKeyRef.Table;
            var childTable = ChildKeyRef.Table;

            var childTableDataSet = childTable.Parent;
            var parentTableDataSet = parentTable.Parent;

            if (!ReferenceEquals(childTableDataSet, parentTableDataSet))
            {
                throw new InvalidOperationException(
                    $"Parent table reference should point to the same object. Child\\parent relation {relationName} error.");
            }

            if (ChildKeyRef.ColumnsEqual(ParentKeyRef))
            {
                var parentColumns = string.Join(",", ParentKeyRef.ColumnsReference.Select(c => c.ColumnName));
                var childColumns = string.Join(",", ChildKeyRef.ColumnsReference.Select(c => c.ColumnName));

                throw new InvalidOperationException(
                    $"Data relation {relationName} key check error. FK columns are identical: parent {parentTable.Name} columns {parentColumns} and child {childTable.Name} columns {childColumns}.");

            }

            for (int i = 0; i < ParentKeyRef.Columns.Count; ++i)
            {
                var parentColumnType = parentTable.DataColumnInfo.Columns[ParentKeyRef.Columns[i]].Type;
                var childColumnType = childTable.DataColumnInfo.Columns[ChildKeyRef.Columns[i]].Type;
                
                var typeMisMatch = parentColumnType != childColumnType;

                if (typeMisMatch)
                {
                    var parentColumnName = parentTable.DataColumnInfo.Columns[ParentKeyRef.Columns[i]].ColumnName;
                    var childColumnName = childTable.DataColumnInfo.Columns[ChildKeyRef.Columns[i]].ColumnName;
                    
                    throw new InvalidOperationException(
                        $"Data relation {relationName} key types mismatch error. Parent {parentTable.Name} column {parentColumnName} and type {parentColumnType} is not equal to child {childTable.Name} column {childColumnName} and type {childColumnType}");
                }
            }
        }

        protected void CheckStateForProperty(string name)
        {
            try
            {
                CheckState();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot access '{name}' property. {ex.Message}");
            }
        }

        private void Create(string relationName, 
            IReadOnlyList<int> parentColumns, CoreDataTable parentTable,
            IReadOnlyList<int> childColumns, CoreDataTable childTable)
        {
            if (parentColumns.Count != childColumns.Count)
            {
                throw new InvalidOperationException(
                    $"{relationName} relation check error. Parent {parentColumns.Count} key column count are not equal to child {childColumns.Count} count.");
            }


            var childTableDataSet = childTable?.Parent;

            var parentTableDataSet = parentTable?.Parent;

            ParentKeyRef = new DataKey(parentTable, parentColumns);
            ChildKeyRef = new DataKey(childTable, childColumns);

            if (parentTable != childTable)
            {
                for (int index = 0; index < parentColumns.Count; ++index)
                {
                    if (parentTableDataSet == null)
                    {
                        throw new InvalidOperationException(
                            $"{this.relationName} relation check error. Parent table dataset reference is not set.");
                    }

                    if (parentTableDataSet == null || childTableDataSet == null)
                    {
                        throw new InvalidOperationException(
                            $"{this.relationName} relation check error. Child table dataset reference is not set.");
                    }
                }
            }

            CheckState();
            this.relationName = relationName ?? string.Empty;
        }

        internal DataRelation Clone(CoreDataTable destination)
        {
            CoreDataTable parentTableDestination = destination;
            CoreDataTable childTableDestination = destination;

            int length = ParentKeyRef.Columns.Count;

            DataRelation dataRelation = new DataRelation(relationName, ParentKey.Columns, parentTableDestination, ChildKey.Columns, childTableDestination);

            dataRelation.CheckMultipleNested = false;
            dataRelation.Nested = Nested;
            dataRelation.CheckMultipleNested = true;

            if (ChildKeyConstraint != null)
            {
                dataRelation.ChildKeyConstraint = ChildKeyConstraint.Clone(destination);
            }

            return dataRelation;
        }

        internal DataRelation CloneDs(CoreDataTable destination)
        {
            var parentTable = ParentKeyRef.Table;
            var childTable = ChildKeyRef.Table;

            var parentTableDestination = destination.TryGetTable(parentTable.Name);

            if (parentTableDestination == null)
            {
                throw new InvalidOperationException(
                    $"$Cannot clone the {RelationName} Relation to the dataset {destination.TableName}. {parentTable.Name} parent table doesn't exist.");
            }

            var childTableDestination = destination.TryGetTable(childTable.Name);

            if (childTableDestination == null)
            {
                throw new InvalidOperationException(
                    $"$Cannot clone the {RelationName} Relation to the dataset {destination.TableName}. {childTable.Name} child table doesn't exist.");
            }

            var dataRelation = new DataRelation(relationName, ParentKey.Columns, parentTableDestination, ChildKey.Columns, childTableDestination);

            if (ChildKeyConstraint != null)
            {
                dataRelation.ChildKeyConstraint = ChildKeyConstraint.CloneDs(destination);
            }

            dataRelation.CheckMultipleNested = false;
            dataRelation.Nested = Nested;
            dataRelation.CheckMultipleNested = true;

            return dataRelation;
        }

        public override string ToString()
        {
            return RelationName;
        }

        internal void ValidateMultipleNestedRelations()
        {
            var parentTable = ParentKeyRef.Table;
            var childTable = ChildKeyRef.Table;

            if (!Nested || !CheckMultipleNested || (childTable.ParentRelationsMap?.Count == 0))
            {
                return;
            }

            if (ChildKeyRef.Columns.Count != 1)
            {
                throw new InvalidOperationException(
                    $"Relation {relationName} validation failed: Multiple columns key detected for {childTable.Name}.");
            }
        }

        internal static void SetupSingleColumnRelationType(RelationType relationType,
            CoreDataTable parentTable,
            CoreDataColumn parentTableCol,
            CoreDataTable childTable,
            CoreDataColumn childTableCol)
        {
            switch (relationType)
            {
                case RelationType.OneToMany:
                    parentTable.AddIndex(parentTableCol, unique: true);
                    childTable.AddIndex(childTableCol);
                    break;
                case RelationType.OneToOne:
                    parentTable.AddIndex(parentTableCol, unique: true);
                    childTable.AddIndex(childTableCol, unique: true);
                    break;
                case RelationType.ManyToOne:
                    parentTable.AddIndex(parentTableCol);
                    childTable.AddIndex(childTableCol, unique: true);
                    break;
            }
        }

        internal static void SetupMultiColumnRelationType(RelationType relationType,
            CoreDataTable parentTable,
            IEnumerable<CoreDataColumn> parentTableCol,
            CoreDataTable childTable,
            IEnumerable<CoreDataColumn> childTableCol)
        {
            switch (relationType)
            {
                case RelationType.OneToMany:
                {
                    var pCols = parentTableCol.Select(a => a.ColumnName).ToData();

                    parentTable.AddMultiColumnIndex(
                        pCols,
                        unique: true);

                    pCols.Dispose();

                    var cCols = childTableCol.Select(a => a.ColumnName).ToData();

                    childTable.AddMultiColumnIndex(
                        cCols,
                        unique: false);

                    cCols.Dispose();
                }
                    break;
                case RelationType.OneToOne:
                {
                    var pCols = parentTableCol.Select(a => a.ColumnName).ToData();
                    parentTable.AddMultiColumnIndex(
                        pCols,
                        unique: true);

                    pCols.Dispose();

                    var cCols = childTableCol.Select(a => a.ColumnName).ToData();

                    childTable.AddMultiColumnIndex(
                        cCols,
                        unique: true);

                    cCols.Dispose();

                    break;
                }
                case RelationType.ManyToOne:
                {
                    var pCols = parentTableCol.Select(a => a.ColumnName).ToData();

                    parentTable.AddMultiColumnIndex(
                        pCols,
                        unique: false);

                    pCols.Dispose();

                    var cCols = childTableCol.Select(a => a.ColumnName).ToData();
                    childTable.AddMultiColumnIndex(
                        cCols,
                        unique: true);

                    cCols.Dispose();

                    break;
                }
            }
        }

        public void Dispose()
        {
            this.ChildKeyConstraint.Dispose();
            this.ParentKeyConstraint.Dispose();
        }

        public void RemapChildColumnHandles(Map<int,int> oldToNewMap)
        {
            ChildKeyRef.RemapColumnHandles(oldToNewMap);
        }

        public void RemapParentColumnHandles(Map<int,int> oldToNewMap)
        {
            ParentKeyRef.RemapColumnHandles(oldToNewMap);
        }

        public IReadOnlyList<T> GetChildRows<T>(int rowHandle, bool ignoreDeleted) where T: ICoreDataRowReadOnlyAccessor
        {
            var relation = this;

            var parentTable = relation.ParentKey.Table;
            var childTable = relation.ChildKey.Table;

            var childRowHandles = DataRelation.GetChildRowHandles(parentTable, childTable, relation.ParentKey, relation.ChildKey, rowHandle, ignoreDeleted);

            var dataRows = new Data<T>();

            foreach (var ch in childRowHandles)
            {
                dataRows.Add((T)(object)childTable.GetRow(ch));
            }

            childRowHandles.Dispose();

            return dataRows;
        }
        
        public ICoreDataRowReadOnlyAccessor GetParentRow(int rowHandle)
        {
            var relation = this;

            var table = relation.ParentTable;

            var parentRowHandle = DataRelation.GetParentRowHandle(relation.ParentTable, relation.ParentKey, relation.ChildKey, rowHandle);

            if (parentRowHandle.HasValue)
            {
                return table.GetRow(parentRowHandle.Value);
            }
            
            return null;
        }
        
        public IReadOnlyList<T> GetParentRows<T>(int rowHandle) where T : ICoreDataRowReadOnlyAccessor
        {
            var relation = this;
            var table = relation.ParentTable;
            
            var parentRowHandles = DataRelation.GetParentRowHandles(relation.ParentTable, relation.ParentKey, relation.ChildKey, rowHandle);

            var dataRows = new Data<T>();

            foreach (var ph in parentRowHandles)
            {
                dataRows.Add((T)(object)table.GetRow(ph));
            }
            
            parentRowHandles.Dispose();

            return dataRows;
        }
    }
}
