using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    public partial class DataSet : CoreDataSet, IDataSet, IDataLockEventState
    {
        public DataSet()
        {
            DataTableType = typeof(DataTable);
        }

        IReadOnlyDataSet IReadOnlyDataSet.Copy()
        {
            return (DataSet)base.Copy();
        }
        
        public new IEnumerable<DataTable> Tables => base.Tables.OfType<DataTable>();
        
        [NotNull]
        public new DataTable GetTable([NotNull] string tableName)
        {
            return (DataTable)base.GetTable(tableName);
        }
        
        protected override void MergeTableMetadata(CoreDataTable currentTable, CoreDataTable sourceTable)
        {
            var lockEvents = ((DataTable)currentTable).LockEvents();

            try
            {
                currentTable.MergeMetaOnly(sourceTable);
            }
            finally
            {
                lockEvents.ResetAggregatedEvents();

                lockEvents.UnlockEvents();
            }
        }
        
        protected override void MergeTable(bool overrideExisting, CoreDataTable table, ICoreDataTable sourceTable)
        {
            var lockEvents = ((DataTable)table).LockEvents();

            try
            {
                base.MergeTable(overrideExisting, table, sourceTable);
            }
            finally
            {
                lockEvents.ResetAggregatedEvents();

                lockEvents.UnlockEvents();
            }
        }

        IEnumerable<IReadOnlyDataTable> IReadOnlyDataSet.Tables => base.Tables.OfType<DataTable>();

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IEnumerable<IDataTable> IDataSet.Tables => TablesMap.Values.OfType<DataTable>();
        
        [CanBeNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTable IDataSet.TryGetTable([NotNull] string tableName)
        {
            return (IDataTable)TryGetTable(tableName);
        }

        IReadOnlyDataTable IReadOnlyDataSet.GetTable(string tableName)
        {
            return (DataTable)base.GetTable(tableName);
        }

        IReadOnlyDataTable IReadOnlyDataSet.TryGetTable(string tableName)
        {
            return (DataTable)base.TryGetTable(tableName);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTable IDataSet.GetTable([NotNull] string tableName)
        {
            return (IDataTable)GetTable(tableName);
        }
        
        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTable IDataSet.AddTable(string tableName)
        {
            return AddTable(tableName);
        }
        
        [NotNull]
        public DataTable AddTable(string tableName)
        {
            return (DataTable)base.AddTable(tableName);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataTable IDataSet.AddTable([NotNull] IDataTable table)
        {
            return AddTable((DataTable)table);
        }

        void IDataSet.MergeMeta(IDataSet dataSet)
        {
            base.MergeMeta((DataSet)dataSet);
        }

        void IDataSet.MergeData(IDataSet dataSet, bool overrideExisting = true)
        {
            base.MergeData((DataSet)dataSet, overrideExisting);
        }

        public void FullMerge(IDataSet dataSet)
        {
            base.FullMerge((DataSet)dataSet);
        }

        IDataTable IDataSet.NewTable(string tableName = null)
        {
            return (IDataTable)base.NewTable(tableName);
        }

        [NotNull]
        public DataTable AddTable([NotNull]DataTable table)
        {
            return (DataTable)base.AddTable(table);
        }
        
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        void IDataSet.MergeSchema(IDataSet source, bool addTables = true)
        {
            MergeSchema((DataSet)source, addTables);
        }

        public void MergeSchema(DataSet source, bool addTables = true)
        {
            base.MergeSchema(source, addTables);
        }
        
        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataSet IDataSet.Copy() => (IDataSet)Copy();

        [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        IDataSet IDataSet.Clone() => (IDataSet)Clone();

        [NotNull]
        public new DataSet Copy(Thread thread = null) => (DataSet)base.Copy(thread);

        [NotNull]
        public new DataSet Clone(Thread thread = null) => (DataSet)base.Clone(thread);

        public void AttachEventHandlersTo(DataSet targetDataset, IDataOwner dataOwner = null)
        {
            foreach (var sourceTable in Tables)
            {
                var targetTable = targetDataset.TryGetTable(sourceTable.Name);

                if (targetTable is DataTable dt && sourceTable is DataTable st)
                {
                    st.AttachEventHandlersTo(dt, dataOwner);
                }
            }
        }
        
        public IDataLockEventState LockEvents()
        {
            foreach (var table in Tables)
            {
                table.LockEvents();
            }

            return this;
        }

        public void ResetAggregatedEvents()
        {
            foreach (var table in Tables)
            {
                ((IDataLockEventState)table).ResetAggregatedEvents();
            }
        }

        void IDataLockEventState.UnlockEvents()
        {
            foreach (var table in Tables)
            {
                ((IDataLockEventState)table).UnlockEvents();
            }
        } 
    }
}