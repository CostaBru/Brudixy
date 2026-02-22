using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using Brudixy.Exceptions;
using Brudixy.Interfaces;

using Konsarpoo.Collections;
using JetBrains.Annotations;

namespace Brudixy
{
    partial class CoreDataTable : IDisposable
    {
        public const string StringIndexCaseSensitiveXProp = "__IndxStrCaseSensitive";
        public const string StringIndexFullTextXProp = "__IndxStrFullText";
        
        internal static CompareOptions s_compareFlags = CompareOptions.IgnoreCase | CompareOptions.IgnoreKanaType | CompareOptions.IgnoreWidth;

        protected internal string Name = "Table";

        public Thread SourceThread = Thread.CurrentThread;
        
        public static Func<DateTime> UtcNow = () => DateTime.UtcNow;

        public static Func<Guid> NewGuid => () => Guid.CreateVersion7();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        string ICoreReadOnlyDataTable.TableName => Name;

        protected internal Type DataRowType = typeof(CoreDataRow);

        internal WeakReference<CoreDataTable> DataSetReference;

        private CompareInfo compareInfo;

        protected Data<CoreDataRow> m_rowReferences;

        public bool TrackChanges = true;

        protected ulong m_dataAge;
        protected ushort m_metaAge;
        
        public ulong DataAge => m_dataAge;
        public ushort MetaAge => m_metaAge;

        internal DateTimeKind StorageTimeKind = System.DateTimeKind.Utc;

        //do not copy
        private int m_initLockCount;

        //do not copy
        internal bool IsDisposed;
        public string Namespace { get; set; }
        public static int? DisplayDateTimeUtcOffsetTicksDefault { get; set; }
        
        public int? DisplayDateTimeUtcOffsetTicks { get; set; } = DisplayDateTimeUtcOffsetTicksDefault;
        
        public string Prefix { get; set; }
        
        private bool m_caseSensitive = false;

        [CanBeNull]
        private Map<int, Set<string>> m_columnParentRelations;
        
        [CanBeNull]
        private Map<int, Set<string>> m_columnChildRelations;
        
        [CanBeNull]
        internal Map<string, DataRelation> ChildRelationsMap;
        
        [CanBeNull]
        internal Map<string, DataRelation> ParentRelationsMap;
        
        private bool m_caseSensitiveUserSet = true;
        
        internal Map<string, ExtPropertyValue> ExtProperties;
     
        protected WeakReference<IDataOwner> m_dataOwnerReference;

        [NotNull]
        internal IndexInfo IndexInfo = new ();
        
        private DisposableCollection m_disposables = new ();
        
        internal Map<string, CoreDataTable> TablesMap = new (StringComparer.OrdinalIgnoreCase);
        
        internal Map<string, DataRelation> RelationsMap = new (StringComparer.OrdinalIgnoreCase);
        
        protected ImmutableDictionary<string, ImmutableList<IDataLogEntry>> m_dsChanges = ImmutableDictionary<string, ImmutableList<IDataLogEntry>>.Empty;
        
        protected ImmutableStack<object> m_dsChangesContext = ImmutableStack<object>.Empty;
        
        protected StopwatchSlim? m_dsStopwatch;
        
        protected DateTime? m_dsUtsStopWatchStart;

        [NotNull]
        internal IndexOfManyInfo MultiColumnIndexInfo = new ();

        [NotNull]
        internal CoreDataColumnInfo DataColumnInfo => m_dataColumnInfo ??= CreateDataColumnInfo();

        private StateInfo m_stateInfo;
        [NotNull]
        internal StateInfo StateInfo => m_stateInfo ??= CreateStateInfo();

        protected virtual StateInfo CreateStateInfo()
        {
            return new StateInfo(this);
        }

        internal bool TableIsReadOnly;
        
        [NotNull] private CoreDataColumnInfo m_dataColumnInfo;

        internal Set<int> RowInCascadeUpdate = null;

        public bool m_nestedInDataset;
        private bool m_enforceConstraints = true;
        
        private bool m_isBuildin;
        private bool m_areColumnsReadonly;

        public bool EnforceConstraints
        {
            get
            {
                return m_enforceConstraints;
            }
            set
            {
                if (IsInitializing == false && IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot change EnforceConstraints property for '{Name}' table because it is readonly.");
                }
                
                m_enforceConstraints = value;
            }
        }

        public bool CaseSensitive
        {
            get
            {
                return m_caseSensitive;
            }
            set
            {
                if (IsInitializing == false && IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot change CaseSensitive for '{Name}' table because it is readonly.");
                }
                
                m_caseSensitive = value;
            }
        }

        public DateTimeKind StorageDateTimeKind
        {
            get => StorageTimeKind;
            set
            {
                if (IsInitializing == false && IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot change storage DateTimeKind for '{Name}' table because it is readonly.");
                }
                
                if (value != StorageTimeKind && this.AllRowsHandles.Any())
                {
                    throw new InvalidOperationException(
                        $"Cannot change storage DateTimeKind for '{Name}' table because it has data. Please clear table before change this property.");
                }

                StorageTimeKind = value;
            }
        }
        
        public CoreDataTable Parent
        {
            get
            {
                if (DataSetReference == null)
                {
                    return null;
                }
                
                DataSetReference.TryGetTarget(out var ds);
                
                return ds;
            }
        }
        
        public string TableName
        {
            get
            {
                return Name;
            }
            set
            {
                if (IsInitializing == false && IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException($"Cannot change table name from '{Name}' to '{value}' because it is readonly.");
                }
                
                Name = value;
            }
        }

        public virtual bool IsBuildin
        {
            get
            {
                return m_isBuildin;
            }
            set
            {
                if (IsInitializing == false && IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException($"Cannot change table buildin flag because it is readonly.");
                }
                
                m_isBuildin = value;
            }
        }
        
        [NotNull]
        public IReadOnlyDictionary<string, CoreDataColumn> ColumnMapping => DataColumnInfo.ColumnMappings;
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        int ICoreReadOnlyDataTable.RowCount => RowCount;
        
        public bool HasNestedRelations => (ParentRelationsMap?.Any() ?? false) || (ChildRelationsMap?.Any() ?? false);
        
        private string PkDebug => string.Join(" | ", PrimaryKey.Select(c => c.ColumnName));
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        string ICoreDataTable.TableName
        {
            get => Name;
            set => TableName = value;
        }
        
        [NotNull]
        internal KeyValuePair<string, string>[] IndexInfoDebug
        {
            get
            {
                return IndexInfo.Indexes.Select(s =>
                        new KeyValuePair<string, string>(this.DataColumnInfo.Columns[s.ColumnHandle].ColumnName,
                            s.ReadyIndex.IsUnique + " " + s.ReadyIndex.StorageType + " " + s.ReadyIndex.Count))
                    .ToArray();
            }
        }
        
        [NotNull]
        internal KeyValuePair<string, string>[] MultiColumnIndexInfoDebug
        {
            get
            {
                return MultiColumnIndexInfo.Indexes.Select(s =>
                        new KeyValuePair<string, string>(string.Join(";", s.Columns.Select(c => this.DataColumnInfo.Columns[c].ColumnName)),
                            s.IsUnique + " " + s.ReadyIndex.Count))
                    .ToArray();
            }
        }

        public IEnumerable<string> XProperties => ExtProperties == null ? Enumerable.Empty<string>() : ExtProperties.Keys;

        public bool IsInitializing
        {
            get
            {
                if (m_initLockCount > 0)
                {
                    return true;
                }

                if (m_initLockCount <= 0)
                {
                    m_initLockCount = 0;
                }

                return false;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return Root.TableIsReadOnly;
            }
            set
            {
                var ds = Parent;
                
                if (ds is { IsReadOnly: true })
                {
                    throw new ReadOnlyAccessViolationException($"Cannot change readonly mode for '{Name}' table because '{Parent.TableName}' dataset is readonly.");
                }
                
                TableIsReadOnly = value;

                OnMetadataChanged();
            }
        }

        protected CoreDataTable Root
        {
            get
            {
                var ds = this;

                while (ds?.Parent != null)
                {
                    ds = ds.Parent;
                }
                
                return ds ?? this;
            }
        }

        public bool AreColumnsReadonly => m_areColumnsReadonly;
        
        public int Capacity
        {
            get;
            set;
        }

        public int RowCount => StateInfo.RowCount;

        public IDataOwner Owner
        {
            get
            {
                if (m_dataOwnerReference == null)
                {
                    return null;
                }

                m_dataOwnerReference.TryGetTarget(out var owner);

                return owner;
            }
            set
            {
                if (IsInitializing == false && IsReadOnly)
                {
                    throw new ReadOnlyAccessViolationException(
                        $"Cannot set data owner reference for '{Name}' table because it is readonly.");
                }
                
                if (value != null)
                {
                    m_dataOwnerReference = new WeakReference<IDataOwner>(value);
                }
                else
                {
                    m_dataOwnerReference = null;
                }
            }
        }

        public int ColumnCount => DataColumnInfo.ColumnsCount;

        public IEnumerable<DataRelation> ParentRelations => ParentRelationsMap?.Values ?? Enumerable.Empty<DataRelation>();

        public IEnumerable<DataRelation> ChildRelations => ChildRelationsMap?.Values ?? Enumerable.Empty<DataRelation>();

        public IEnumerable<DataRelation> ChildNestedRelations
        {
            get
            {
                if (ChildRelationsMap is not null)
                {
                    foreach (var relation in ChildRelationsMap)
                    {
                        if (relation.Value.ChildTableName == Name)
                        {
                            yield return relation.Value;
                        }
                    }
                }
            }
        }
        
        public IEnumerable<DataRelation> ParentNestedRelations
        {
            get
            {
                if (ParentRelationsMap is not null)
                {
                    foreach (var relation in ParentRelationsMap)
                    {
                        if (relation.Value.ParentTableName == Name)
                        {
                            yield return relation.Value;
                        }
                    }
                }
            }
        }
        
        public bool HasAnyIndex => IndexInfo.HasAny || MultiColumnIndexInfo.HasAny;

        public virtual CoreDataTable CreatePartitionTable(Thread thread = null)
        {
            m_areColumnsReadonly = true;

            var partTable = CloneCore(thread, withData: false, cloneColumns: false);

            return partTable;
        }

        private CoreDataTable CloneTable(Thread thread, bool withData, bool cloneColumns)
        {
            var clone = (CoreDataTable)this.MemberwiseClone();

            clone.ResetState();

            clone.SourceThread = thread ?? Thread.CurrentThread;
            clone.Name = Name;
            clone.m_dataAge = m_dataAge;
            clone.m_baseAge = m_baseAge;
            clone.m_metaAge = m_metaAge;
            clone.StorageTimeKind = StorageTimeKind;
            clone.CaseSensitive = CaseSensitive;
            clone.m_caseSensitiveUserSet = m_caseSensitiveUserSet;
            clone.Namespace = Namespace;
            clone.Prefix = Prefix;
            clone.DisplayDateTimeUtcOffsetTicks = DisplayDateTimeUtcOffsetTicks;
            
            if (cloneColumns)
            {
                clone.CloneDataColumnInfo(DataColumnInfo, withData);
            }

            if (ExtProperties != null)
            {
                clone.ExtProperties = new(ExtProperties);
            }

            CopyRelations(clone);

            clone.IndexInfo.CreateFrom(IndexInfo, withData);

            clone.MultiColumnIndexInfo.CreateFrom(MultiColumnIndexInfo, withData);

            if (withData)
            {
                CopyData(clone);
            }

            if (m_dataOwnerReference != null)
            {
                clone.Owner = Owner;
            }

            clone.m_enforceConstraints = m_enforceConstraints;
            
            return clone;
        }

        protected virtual void ResetState()
        {
            CoreDataTable clone = this;

            clone.m_dataColumnInfo = CreateDataColumnInfo();
            clone.m_caseSensitiveUserSet = true;
            clone.TablesMap = new(StringComparer.OrdinalIgnoreCase);
            clone.RelationsMap = new(StringComparer.OrdinalIgnoreCase);
            clone.MultiColumnIndexInfo = new();
            clone.m_stateInfo = null;
            clone.IndexInfo = new IndexInfo();

            clone.DataSetReference = null;
            clone.m_rowReferences = null;
            clone.m_changes = ImmutableList<IDataLogEntry>.Empty;
            clone.m_changesContext = ImmutableStack<object>.Empty;
            clone.m_disposables = new DisposableCollection();
            clone.m_stopwatch = null;
            clone.m_dsChanges = ImmutableDictionary<string, ImmutableList<IDataLogEntry>>.Empty;
            clone.m_dsChangesContext = ImmutableStack<object>.Empty;
            clone.m_dataOwnerReference = null;
            clone.m_areColumnsReadonly = false;
            clone.m_initLockCount = 0;
            clone.m_utsStopWatchStart = null;
            clone.m_dsStopwatch = null;
            clone.m_maxColumnLenConstraint = null;
            clone.IsDisposed = false;
            clone.ExtProperties = null;
            clone.ParentRelationsMap = null;
            clone.ChildRelationsMap = null;
            clone.m_columnChildRelations = null;
            clone.m_columnParentRelations = null;
            clone.RowInCascadeUpdate = null;
            clone.Name = "Table";
            clone.SourceThread = null;
            clone.compareInfo= null;
            clone.TrackChanges = true;
            clone.StorageTimeKind = System.DateTimeKind.Utc;
            clone.Namespace = null;
            clone.DisplayDateTimeUtcOffsetTicks = null;
            clone.Prefix = null;
            clone.m_caseSensitive = false;
            clone.m_dsUtsStopWatchStart = null;
            clone.TableIsReadOnly = false;
            clone.m_nestedInDataset = false;
            clone.m_enforceConstraints = true;
            clone.m_isBuildin = false;
        }

        private void CopyData(CoreDataTable clone)
        {
            clone.StateInfo.Merge(StateInfo);
        }

        private void CopyRelations(CoreDataTable clone)
        {
            var copiedParentRelationNames = new Map<string, Set<int>>();
            var copiedChildRelationNames = new Map<string, Set<int>>();

            var clonedInstances = new Map<string, DataRelation>();

            if (ParentRelationsMap?.Count > 0)
            {
                clone.ParentRelationsMap = new(StringComparer.OrdinalIgnoreCase);

                foreach (var relation in ParentRelationsMap.Values)
                {
                    var table = relation.ParentTable;

                    if (ReferenceEquals(table, this))
                    {
                        clone.ParentRelationsMap[relation.relationName] =
                            clonedInstances.GetOrAdd(relation.relationName, () => relation.Clone(clone));

                        var set = copiedParentRelationNames.GetOrAdd(relation.relationName, CreateIntNewHashset);

                        set.UnionWith(relation.ParentKey.Columns);
                    }
                }
            }

            if (ChildRelationsMap?.Count > 0)
            {
                clone.ChildRelationsMap = new(StringComparer.OrdinalIgnoreCase);

                foreach (var relation in ChildRelationsMap.Values)
                {
                    var table = relation.ChildTable;
                    if (ReferenceEquals(table, this))
                    {
                        clone.ChildRelationsMap[relation.relationName] =
                            clonedInstances.GetOrAdd(relation.relationName, () => relation.Clone(clone));

                        var set = copiedChildRelationNames.GetOrAdd(relation.relationName, CreateIntNewHashset);

                        set.UnionWith(relation.ChildKey.Columns);
                    }
                }
            }

            if (m_columnParentRelations?.Count > 0)
            {
                clone.m_columnParentRelations = new();

                SetupColumnToRelation(copiedParentRelationNames, clone.m_columnParentRelations);
            }

            if (m_columnChildRelations?.Count > 0)
            {
                clone.m_columnChildRelations = new();

                SetupColumnToRelation(copiedChildRelationNames, clone.m_columnChildRelations);
            }

            foreach (var kv in copiedChildRelationNames)
            {
                kv.Value.Dispose();
            }

            copiedChildRelationNames.Dispose();

            foreach (var kv in copiedParentRelationNames)
            {
                kv.Value.Dispose();
            }

            copiedParentRelationNames.Dispose();

            clonedInstances.Dispose();
        }

        private static void SetupColumnToRelation(Map<string, Set<int>> copiedRelationNames,  Map<int, Set<string>> columnToRelationMap)
        {
            foreach (var kv in copiedRelationNames)
            {
                foreach (var column in kv.Value)
                {
                    var set = columnToRelationMap.GetOrAdd(column, () => new Set<string>());

                    set.Add(kv.Key);
                }
            }
        }

        public void Dispose()
        {
            if (IsDisposed)
            {
                return;
            }

            IsDisposed = true;

            SourceThread = null;

            m_dataAge = ulong.MinValue;
            m_metaAge = ushort.MinValue;

            DataColumnInfo?.Dispose();
            IndexInfo?.Dispose();
            MultiColumnIndexInfo?.Dispose();
            StateInfo?.Dispose();
            ExtProperties?.Dispose();

            if (m_rowReferences != null)
            {
                foreach (var reference in m_rowReferences)
                {
                    if (reference != null)
                    {
                        reference.table = null;
                    }
                }

                m_rowReferences?.Dispose();
            }

            m_columnParentRelations?.Dispose();
            m_columnChildRelations?.Dispose();
       
            m_maxColumnLenConstraint?.Dispose();
        
            RowInCascadeUpdate?.Dispose();

            if (ParentRelationsMap is not null)
            {
                foreach (var relation in ParentRelationsMap)
                {
                    if (relation.Key == Name)
                    {
                        relation.Value.Dispose();
                    }
                }
                
                ParentRelationsMap?.Dispose();
            }

            if (ChildRelationsMap is not null)
            {
                foreach (var relation in ChildRelationsMap)
                {
                    if (relation.Key == Name)
                    {
                        relation.Value.Dispose();
                    }
                }
                
                ChildRelationsMap?.Dispose();
            }

            if (m_columnChildRelations is not null)
            {
                foreach (var kv in m_columnChildRelations)
                {
                    kv.Value.Dispose();
                }
                
                m_columnChildRelations?.Dispose();
            }
            
            if (m_columnParentRelations is not null)
            {
                foreach (var kv in m_columnParentRelations)
                {
                    kv.Value.Dispose();
                }
                
                m_columnParentRelations?.Dispose();
            }

            if (TablesMap != null)
            {
                foreach (var value in TablesMap.Values)
                {
                    value.Dispose();
                }

                TablesMap.Dispose();
            }

            if (RelationsMap != null)
            {
                foreach (var value in RelationsMap.Values)
                {
                    value.Dispose();
                }

                RelationsMap.Dispose();
            }

            m_disposables?.Dispose();

            OnDisposed();
        }
    }
}
