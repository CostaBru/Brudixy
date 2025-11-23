using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Xml.Linq;
using Brudixy.Interfaces;
using Brudixy.Constraints;
using Brudixy.Converter;
using Brudixy.Exceptions;
using JetBrains.Annotations;
using Konsarpoo.Collections;
using Newtonsoft.Json.Linq;

namespace Brudixy
{
    [DebuggerDisplay("{DataSetName}, Tables {Tables.Count}, ObjId {m_objectID}, DataAge = {DataAge}, MetaAge = {MetaAge}, EnforceConstraints = {EnforceConstraints}, RO = {m_isReadOnly}")]
    public partial class CoreDataSet : ICoreDataSet, IDataEditTransaction, IReadonlySupported
    {
        private static int m_objectTypeCount;

        private readonly int m_objectID = Interlocked.Increment(ref m_objectTypeCount);

        internal Map<string, CoreDataTable> TablesMap = new (StringComparer.OrdinalIgnoreCase);
        internal Map<string, DataRelation> RelationsMap = new (StringComparer.OrdinalIgnoreCase);
        
        internal Map<string, ExtPropertyValue> ExtProperties;
        
        private uint m_metaAge = 0;

        protected Type DataTableType = typeof(CoreDataTable);
        
        private bool m_isReadOnly;
        
        public string DataSetName { get; set; }
        
        public string DataSetNamespace { get; set; }

        int ICoreReadOnlyDataSet.TablesCount => TablesMap.Count;

        public ulong DataAge => Tables.Select(t => t.DataAge).DefaultIfEmpty().Max();

        public uint MetaAge => Math.Max(m_metaAge, Tables.Select(t => t.MetaAge).DefaultIfEmpty().Max());

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreDataTable> ICoreDataSet.Tables => TablesMap.Values;

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<ICoreReadOnlyDataTable> ICoreReadOnlyDataSet.Tables => TablesMap.Values;

        public IReadOnlyCollection<CoreDataTable> Tables => TablesMap.Values;
        
        public bool EnforceConstraints { get; set; }

        [CanBeNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataTable ICoreDataSet.TryGetTable([NotNull] string tableName)
        {
            return TryGetTable(tableName);
        }

        [CanBeNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreReadOnlyDataTable ICoreReadOnlyDataSet.TryGetTable([NotNull] string tableName)
        {
            return TryGetTable(tableName);
        }

        [CanBeNull]
        public CoreDataTable TryGetTable([NotNull] string tableName)
        {
            if (TablesMap.TryGetValue(tableName, out var value))
            {
                return value;
            }

            return null;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataTable ICoreDataSet.GetTable([NotNull] string tableName)
        {
            return GetTable(tableName);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreReadOnlyDataTable ICoreReadOnlyDataSet.GetTable([NotNull] string tableName)
        {
            return GetTable(tableName);
        }

        [NotNull]
        public CoreDataTable GetTable([NotNull] string tableName)
        {
            if (TablesMap.TryGetValue(tableName, out var value))
            {
                return value;
            }

            throw new MissingMetadataException($"Table '{tableName}' is not exist in '{DataSetName}' dataset.");
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataTable ICoreDataSet.AddTable(string tableName)
        {
            return AddTable(tableName);
        }

        [NotNull]
        public CoreDataTable AddTable(string tableName)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new table with name '{tableName}' to '{DataSetName}' dataset because it is in readonly.");
            }
            
            if (TablesMap.ContainsKey(tableName))
            {
                throw new InvalidOperationException($"Table '{tableName}' is already exists in DataSet '{DataSetName}'.");
            }

            var table = CreateTableInstance(tableName);

            //table.SetUpDataSet(this);
            
            table.Name = tableName;

            TablesMap[tableName] = table;

            return table;
        }

        protected virtual CoreDataTable CreateTableInstance(string tableName)
        {
            return (CoreDataTable)Activator.CreateInstance(DataTableType);
        }

        public bool HasTable(string tableName)
        {
            return TablesMap.ContainsKey(tableName);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataTable ICoreDataSet.AddTable([NotNull] ICoreDataTable table)
        {
            return AddTable((CoreDataTable)table);
        }

        public void MergeMeta(ICoreDataSet dataSet)
        {
            MergeSchema((CoreDataSet)dataSet, true);
        }

        public void MergeData(ICoreDataSet dataSet, bool overrideExisting = true)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot merge data of '{DataSetName}' dataset because it is in readonly.");
            }
            
            foreach (var table in Tables)
            {
                var sourceTable = dataSet.TryGetTable(table.Name);

                if (sourceTable != null)
                {
                    MergeTable(overrideExisting, table, sourceTable);
                }
            }
        }

        protected virtual void MergeTable(bool overrideExisting, CoreDataTable table, ICoreDataTable sourceTable)
        {
            table.MergeDataOnly((CoreDataTable)sourceTable, overrideExisting);
        }

        public void FullMerge(ICoreDataSet dataSet)
        {
            if (dataSet == null)
            {
                throw new ArgumentNullException(nameof(dataSet));
            }
            
            MergeMeta(dataSet);
            MergeData(dataSet);
        }

        [NotNull]
        public CoreDataTable AddTable([NotNull]CoreDataTable table)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new table with name '{table.Name}' to '{DataSetName}' dataset because it is in readonly.");
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
                throw new InvalidOperationException($"Table {table.Name} is already exists in DataSet '{DataSetName}'.");
            }

            //table.DataSetReference = new WeakReference<CoreDataSet>(this);

            TablesMap[table.Name] = table;

            return table;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        void ICoreDataSet.MergeSchema(ICoreDataSet source, bool addTables = true)
        {
            MergeSchema((CoreDataSet)source, addTables);
        }

        //todo unit tests
        public void MergeSchema(CoreDataSet source, bool addTables = true)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot merge schema of {DataSetName} dataset because it is in readonly.");
            }
            
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


                    var columns = new Data<(ICoreDataTableColumn parentColumn, ICoreDataTableColumn childColumn)>();

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

            foreach (var prop in source.GetXProperties())
            {
                SetXProperty(prop, source.GetXProperty<object>(prop));
            }
        }

        protected virtual void MergeTableMetadata(CoreDataTable currentTable, CoreDataTable sourceTable)
        {
            currentTable.MergeMetaOnly(sourceTable);
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataSet ICoreDataSet.Copy()
        {
            return Copy();
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreReadOnlyDataSet ICoreReadOnlyDataSet.Copy()
        {
            return Copy();
        }

        [NotNull]
        public CoreDataSet Copy(Thread thread = null)
        {
            var dataSet = CloneWithoutTables();

            foreach (var dataTable in TablesMap.Values)
            {
                dataSet.AddTableCore(dataTable.Copy(thread));
            }

            CopyRelationsTo(dataSet);

            return dataSet;
        }

        public bool HasChanges()
        {
            return TablesMap.Values.Any(t => t.HasChanges());
        }

        [NotNull]
        public CoreDataSet GetChanges()
        {
            var dataSet = CloneWithoutTables();

            foreach (var table in TablesMap.Values)
            {
                var changedTable = table.GetChanges();

                dataSet.AddTable(changedTable);
            }

            CopyRelationsTo(dataSet);

            return dataSet;
        }

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataSet ICoreDataSet.Clone()
        {
            return Clone();
        }

        [NotNull]
        public CoreDataSet Clone(Thread thread = null)
        {
            var dataSet = CloneWithoutTables();

            foreach (var dataTable in TablesMap.Values)
            {
                if (dataSet.HasTable(dataTable.Name) == false)
                {
                    dataSet.AddTable(dataTable.Clone(thread));
                }
            }

            CopyRelationsTo(dataSet);

            return dataSet;
        }

        [NotNull]
        private CoreDataSet CloneWithoutTables()
        {
            var dataSet = (CoreDataSet)MemberwiseClone();

            dataSet.TablesMap = new (StringComparer.OrdinalIgnoreCase);
            dataSet.RelationsMap = new (StringComparer.OrdinalIgnoreCase);

            return dataSet;
        }

        private void CopyRelationsTo(CoreDataSet dataSet)
        {
            foreach (var dataRelation in RelationsMap)
            {
                var relation = dataRelation.Value;

                if (dataSet.RelationsMap.ContainsKey(dataRelation.Key) == false)
                {
                    if (ReferenceEquals(relation.ParentTable, relation.ChildTable) == false)
                    {
                        var relationClone = relation.Clone(dataSet);

                        var pt = relationClone.ParentTable?.BeginLoadCore();
                        var ct = relationClone.ChildTable?.BeginLoadCore();

                        dataSet.AddRelationCore(relationClone);
                        
                        pt?.EndLoad();
                        ct?.EndLoad();
                    }
                }
            }
        }

        public void AcceptChanges()
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot accept {DataSetName} dataset changes because it is in readonly.");
            }

            foreach (var table in TablesMap.Values)
            {
                table.AcceptChanges();
            }
        }

        public void RejectChanges()
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot reject {DataSetName} dataset changes because it is in readonly.");
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
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException($"Cannot clear {DataSetName} dataset because it is in readonly.");
            }
            
            foreach (var table in TablesMap.Values)
            {
                table.ClearRows();
            }
        }

        public void Dispose()
        {
            foreach (var table in TablesMap)
            {
                table.Value.Dispose();
            }

            TablesMap.Dispose();

            foreach (var kv in RelationsMap)
            {
                kv.Value.Dispose();
            }
            
            RelationsMap.Dispose();
            ExtProperties?.Dispose();
        }

        public void DropTable(string tableName)
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove table with name '{tableName}' from '{DataSetName}' dataset because it is in readonly.");
            }
            
            if (TablesMap.ContainsKey(tableName) == false)
            {
                throw new MissingMetadataException($"Cannot remove table because table '{tableName}' doesn't exists in DataSet '{DataSetName}'.");
            }

            var dataTable = GetTable(tableName);

            if (dataTable.IsBuildin == false)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove table with name '{tableName}' from '{DataSetName}' dataset because it is buildin.");
            }

            var dataRelations = GetTableRelations().ToData();

            var findIndex = dataRelations.FindIndex<IDataRelation, ICoreReadOnlyDataTable>(dataTable, (r, t) => ReferenceEquals(r.ChildTable , t) || ReferenceEquals(r.ParentTable , t));

            if (findIndex >= 0)
            {
                var dataRelation = dataRelations[findIndex];

                throw new InvalidOperationException(
                    $"Cannot remove table with name '{tableName}' from '{DataSetName}' dataset because of '{dataRelation.Name}' relation.");
            }

            TablesMap.Remove(tableName);
            
            dataTable.Dispose();
        }

        public ICoreDataTable NewTable(string tableName = null)
        {
            return new CoreDataTable(tableName ?? string.Empty);
        }

        public IDataEditTransaction StartTransaction()
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot start new transaction in {DataSetName} dataset because it is in readonly.");
            }
            
            foreach (var dataTable in Tables)
            {
                dataTable.StartTransaction();
            }

            return this;
        }

        void IDataEditTransaction.Commit()
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot commit that transaction in {DataSetName} dataset because it is in readonly.");
            }
            
            foreach (var dataTable in Tables)
            {
                ((IDataEditTransaction)dataTable).Commit();
            }
        }
        
        bool IDataEditTransaction.Rollback()
        {
            if (m_isReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot rollback that transaction in {DataSetName} dataset because it is in readonly.");
            }
            
            var any = false;

            foreach (var dataTable in Tables)
            {
                if (dataTable.GetIsInTransaction())
                {
                    var dataEditTransaction = (IDataEditTransaction)dataTable;

                    any = dataEditTransaction.Rollback() || any;
                }
            }

            return any;
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

        public bool IsReadOnly
        {
            get
            {
                return m_isReadOnly;
            }
            set
            {
                if (value == m_isReadOnly)
                {
                    return;
                }
                
                if (value == false)
                {
                    m_isReadOnly = false;
                    
                    foreach (var dataTable in Tables)
                    {
                        dataTable.IsReadOnly = false;
                    }
                }
                
                if (value)
                {
                    foreach (var dataTable in Tables)
                    {
                        dataTable.IsReadOnly = true;
                    }
                    
                    m_isReadOnly = true;
                }
            }
        }

        public bool HasXProperty(string propertyName)
        {
            return ExtProperties?.ContainsKey(propertyName) ?? false;
        }
        
        [CanBeNull]
        public T GetXProperty<T>(string propertyName, bool original = false)
        {
            if (ExtProperties == null)
            {
                return TypeConvertor.ReturnDefault<T>(CoreDataTable.GetColumnType(typeof(T)));
            }

            ExtProperties.TryGetValue(propertyName, out var value);

            if (original)
            {
                return XPropertyValueConverter.TryConvert<T>("Dataset original", propertyName, value.Original);
            }
            
            if (value.Changed is not null)
            {
                return XPropertyValueConverter.TryConvert<T>("Dataset", propertyName, value.Changed);
            }
        
            return XPropertyValueConverter.TryConvert<T>("Dataset original", propertyName, value.Original);
        }
        
        public void SetXProperty<T>(string propertyName, T value)
        {
            SetXPropertyCore<T>(propertyName, value, false);
        }
        
        public IEnumerable<string> GetXProperties()
        {
            if (ExtProperties == null)
            {
                return Enumerable.Empty<string>();
            }

            return ExtProperties.Keys;
        }

        protected void SetXPropertyCore<T>(string propertyName, T value, bool IsInitializing)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot setup '{propertyName}' extended property for '{DataSetName}' data set because it is readonly.");
            }

            if (ExtProperties == null)
            {
                ExtProperties = new Map<string, ExtPropertyValue>(StringComparer.OrdinalIgnoreCase);
            }

            if (ExtProperties.ContainsKey(propertyName))
            {
                ExtProperties.ValueByRef(propertyName, out var _).Changed = value;
            }
            else
            {
                ExtProperties[propertyName] = new ExtPropertyValue { Original = value };
            }

            if (IsInitializing == false)
            {
                m_metaAge = (ushort)(MetaAge + 1u);
            }
        }
    }
}
