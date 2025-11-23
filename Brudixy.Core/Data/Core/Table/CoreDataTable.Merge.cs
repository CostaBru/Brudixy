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
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        void ICoreDataTable.Merge([NotNull] ICoreDataTable source) => FullMerge((CoreDataTable)source);

        public void FullMerge([NotNull] ICoreDataTable table) => FullMerge((CoreDataTable)table);

        public void FullMerge([NotNull] CoreDataTable table)
        {
            if (table == null)
            {
                throw new ArgumentNullException(nameof(table));
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot merge '{Name}' table because it is readonly.");
            }

            FullDatasetMerge(table);

            MergeMetaOnly(table);

            MergeDataOnly(table);
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public void MergeMeta([NotNull] ICoreDataTable source) => MergeMetaOnly((CoreDataTable)source);

        public void MergeMetaOnly([NotNull] CoreDataTable sourceTable)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot merge '{Name}' table because it is readonly.");
            }

            if (GetIsInTransaction())
            {
                throw new InvalidOperationException(
                    $"Cannot merge '{Name}' table meta data because it is data transaction.");
            }
            
            if (sourceTable == null)
            {
                throw new ArgumentNullException(nameof(sourceTable));
            }
            
            MergeDatasetMeta(sourceTable);

            var targetTable = this;

            var targetColumnInfo = targetTable.DataColumnInfo;
            var sourceColumnInfo = sourceTable.DataColumnInfo;
            
            targetTable.Capacity = Math.Max(targetTable.Capacity, sourceTable.Capacity);

            CheckColumnTypesBeforeMerge(sourceTable, sourceColumnInfo, targetColumnInfo, targetTable);
            MergeColumns(sourceTable);
            
            MergeSingleColIndex(sourceTable, targetTable);
            MergeMultiColIndex(sourceTable, targetTable);

            MergeIndexes(sourceTable);

            MergeTableXProperties(sourceTable, targetTable);
        }

        private static void MergeSingleColIndex(CoreDataTable sourceTable, CoreDataTable targetTable)
        {
            var indexInfo = sourceTable.IndexInfo;

            foreach (var index in indexInfo.Indexes)
            {
                var dataColumn = sourceTable.GetColumn(index.ColumnHandle);
                
                var isUnique = dataColumn.IsUnique;

                var targetColumn = targetTable.GetColumn(dataColumn.ColumnName);

                targetTable.AddIndex(targetColumn.ColumnHandle, isUnique);
            }
        }
        
        private static void MergeMultiColIndex(CoreDataTable sourceTable, CoreDataTable targetTable)
        {
            var indexInfo = sourceTable.MultiColumnIndexInfo;

            foreach (var index in indexInfo.Indexes)
            {
                var dataColumns = index.Columns.Select(s => sourceTable.GetColumn(s).ColumnName).ToArray();
                
                var isUnique = index.IsUnique;

                targetTable.AddMultiColumnIndex(dataColumns, isUnique);
            }
        }

        protected void MergeIndexes(CoreDataTable sourceTable)
        {
            var targetTable = this;

            targetTable.IndexInfo.Merge(targetTable, sourceTable);
            targetTable.MultiColumnIndexInfo.Merge(targetTable, sourceTable);
        }
        
        private void ChangeHandles(Map<int,int> oldToNewMap, Map<int, Set<string>> relNames)
        {
            if (relNames == null)
            {
                return;
            }
            
            Map<int, Set<string>> temp = new(relNames);

            foreach (var kv in temp)
            {
                var oldColumnHandle = kv.Key;
                
                if (oldToNewMap.TryGetValue(oldColumnHandle, out var newColumnHandle))
                {
                    if (newColumnHandle == oldColumnHandle)
                    {
                        continue;
                    }
                    
                    var values = relNames[oldColumnHandle];

                    relNames[newColumnHandle] = values;

                    relNames.Remove(oldColumnHandle);
                }
            }
            
            temp.Dispose();
        }

        private void RemapRelationColumnHandles(Map<int, int> oldToNewMap)
        {
            var targetTable = this;

            ChangeHandles(oldToNewMap, targetTable.m_columnParentRelations);
            ChangeHandles(oldToNewMap, targetTable.m_columnChildRelations);
           
            foreach (var childRelation in targetTable.ChildRelations.Union(targetTable.ParentRelations))
            {
                if (ReferenceEquals(childRelation.ChildTable, targetTable))
                {
                    childRelation.RemapChildColumnHandles(oldToNewMap);
                }
                else if (ReferenceEquals(childRelation.ParentTable, targetTable))
                {
                    childRelation.RemapParentColumnHandles(oldToNewMap);
                }
            }
        }

        private static void MergeTableXProperties(CoreDataTable source, CoreDataTable targetTable)
        {
            if (source.ExtProperties != null)
            {
                if (targetTable.ExtProperties == null)
                {
                    targetTable.ExtProperties = new (source.ExtProperties);
                }
                else
                {
                    foreach (var property in source.ExtProperties)
                    {
                        targetTable.ExtProperties[property.Key] = property.Value;
                    }
                }
            }
        }

        private void MergeColumns(CoreDataTable sourceTable)
        {
            var sourceColumnInfo = sourceTable.DataColumnInfo;
            var columnsCount = sourceColumnInfo.ColumnsCount;

            var oldNewMap = new Map<int, int>();

            for (int sourceColumnHandle = 0; sourceColumnHandle < columnsCount; sourceColumnHandle++)
            {
                var sourceColumn = sourceColumnInfo.Columns[sourceColumnHandle];

                var targetColumn = MergeColumn(sourceTable, sourceColumn, sourceColumnInfo);

                oldNewMap[targetColumn.ColumnHandle] = sourceColumnHandle;
            }

            MergePk(sourceColumnInfo);

            RemapColumnHandles(oldNewMap);
        }

        private void MergePk(CoreDataColumnInfo sourceColumnInfo)
        {
            if (sourceColumnInfo.PrimaryKeyColumns.Length > 0)
            {
                var pk = new CoreDataColumn[sourceColumnInfo.PrimaryKeyColumns.Length];

                for (var index = 0; index < sourceColumnInfo.PrimaryKeyColumns.Length; index++)
                {
                    var cols = sourceColumnInfo.PrimaryKeyColumns[index];
                    pk[index] = this.DataColumnInfo.ColumnMappings[cols.ColumnName];
                }

                this.DataColumnInfo.PrimaryKeyColumns = pk;
            }
        }

        protected virtual CoreDataColumn MergeColumn(CoreDataTable sourceTable,
            CoreDataColumn sourceColumn,
            CoreDataColumnInfo sourceColumnInfo)
        {
            CoreDataTable targetTable = this;
            var targetColumnInfo = targetTable.DataColumnInfo;
            
            CoreDataColumn targetColumnVal = null;

            if (targetColumnInfo.ColumnMappings.TryGetValue(sourceColumn.ColumnName, out var targetColumn))
            {
                var pldXProps = targetColumn.ColumnObj.XPropertiesStore;

                var mergedXProps = pldXProps.AddRange(sourceColumn.ColumnObj.XPropertiesStore);

                targetColumn.ColumnObj = mergedXProps.Count > 0
                    ? sourceColumn.ColumnObj.WithXPropertiesHandle(mergedXProps, targetColumn.ColumnHandle)
                    : sourceColumn.ColumnObj.WithColumnHandle(targetColumn.ColumnHandle);
                
                targetColumnVal = targetColumn;
            }
            else
            {
                targetColumnVal = AddNewColumnToTarget(sourceColumn, targetTable, sourceColumn.ColumnName);
            }

            return targetColumnVal;
        }

        private static void CheckColumnTypesBeforeMerge(CoreDataTable source, CoreDataColumnInfo sourceColumnInfo, CoreDataColumnInfo targetColumnInfo, CoreDataTable targetTable)
        {
            if (targetTable.StateInfo.RowStorageCount == 0)
            {
                return;
            }

            for (int sourceColumnHandle = 0; sourceColumnHandle < sourceColumnInfo.ColumnsCount; sourceColumnHandle++)
            {
                var sourceColumn = sourceColumnInfo.Columns[sourceColumnHandle];

                if (targetColumnInfo.ColumnMappings.TryGetValue(sourceColumn.ColumnName, out var targetColumn))
                {
                    var targetColumnDataType = targetColumn.Type;
                    var sourceColumnDataType = sourceColumn.Type;

                    if (targetColumnDataType != sourceColumnDataType ||
                        (targetColumnDataType == sourceColumnDataType && targetColumnDataType == TableStorageType.DateTime &&
                         targetTable.StorageTimeKind != source.StorageTimeKind))
                    {
                        throw new InvalidOperationException(
                            $"Columns type mismatch. Target '{targetTable.Name}'.'{targetColumn.ColumnName}' and source '{source.Name}.{sourceColumn}'");
                    }
                }
            }
        }

        private static CoreDataColumn AddNewColumnToTarget(
            CoreDataColumn sourceColumn,
            CoreDataTable targetTable,
            string targetColumnName)
        {
            var columnType = sourceColumn.Type;
            var calcMax = sourceColumn.IsAutomaticValue;
            var unique = sourceColumn.IsUnique;
            var defaultValue = sourceColumn.DefaultValue;
            var maxLen = sourceColumn.MaxLength;
            var dataType = sourceColumn.DataType;
            var xProperties = sourceColumn.GetXProperties();

            var newColumn = targetTable.AddColumn(columnName: targetColumnName, 
                valueType: columnType,
                type: dataType,
                auto: calcMax, unique: unique, columnMaxLength: maxLen, defaultValue: defaultValue, builtin: sourceColumn.IsBuiltin, serviceColumn: sourceColumn.IsServiceColumn, allowNull: sourceColumn.AllowNull, xProps: xProperties);

            return newColumn;
        }

        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        public void MergeData([NotNull] ICoreDataTable source)
        {
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            MergeDataOnly((CoreDataTable)source);
        }

        public void MergeDataOnly([NotNull] CoreDataTable source, bool overrideExisting = true)
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot merge '{Name}' table data because it is readonly.");
            }
            
            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            MergeDatasetData(source);

            var targetTable = this;

            if (source.ColumnCount > 0 && source.RowCount > 0 && targetTable.ColumnCount > 0)
            {
                var dataRows = targetTable.LoadRows(source.AllRows, overrideExisting: overrideExisting);

                dataRows.Dispose();
            }
        }
    }
}
