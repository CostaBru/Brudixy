using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        protected virtual CoreContainerMetadataProps GetContainerMetaProps()
        {
            var columnMap = CreateDataColumnContainers();

            var keys = new Data<string>(PrimaryKeyColumns());

            var columnContainers = DataColumnInfo.Columns.Select(c => CreateColumnContainerInstance(c.ColumnObj)).ToArray();

            var containerMetaProps = new CoreContainerMetadataProps(
                tableName: Name,
                columns: columnContainers,
                columnMap: columnMap,
                keyColumn: keys,
                MetaAge);
            
            return containerMetaProps;
        }

        private FrozenDictionary<string, CoreDataColumnContainer> CreateDataColumnContainers()
        {
            var columnMap = DataColumnInfo
                .ColumnMappings
                .ToFrozenDictionary(c => c.Key,
                    c => CreateColumnContainerInstance(c.Value.ColumnObj),
                    StringComparer.OrdinalIgnoreCase);
            return columnMap;
        }

        [NotNull]
        public IEnumerable<CoreDataRowContainer> CreateDataRowContainers([NotNull] IEnumerable<int> rows)
        {
            foreach (var dataRow in rows)
            {
                yield return CreateDataRowContainer(dataRow);
            }
        }

        [NotNull]
        public CoreDataRowContainer CreateDataRowContainer([NotNull] CoreDataRow dataRow) => CreateDataRowContainerCore(dataRow.RowHandle, null);

        [CanBeNull]
        public CoreDataRowContainer CreateDataRowContainer(int rowHandle, IReadOnlyCollection<string> skipColumns = null) => CreateDataRowContainerCore(rowHandle, skipColumns);

        protected virtual CoreDataRowContainer CreateDataRowContainerCore(int rowHandle, IReadOnlyCollection<string> skipColumns)
        {
            if (rowHandle < 0 || rowHandle >= StateInfo.RowStorageCount)
            {
                return null;
            }

            var rowAccessor = GetRowInstance(rowHandle);

            var dataRowContainer = CreateContainerInstance();

            var containerMetaProps = GetContainerMetaProps();

            var propsBuilder = CreateContainerProps(rowHandle, skipColumns);

            dataRowContainer.Init(containerMetaProps, propsBuilder.ToProps(), rowAccessor);

            if (m_dataOwnerReference != null)
            {
                dataRowContainer.SetOwner(Owner);
            }

            if (IsReadOnly)
            {
                dataRowContainer.IsReadOnly = true;
            }

            return dataRowContainer;
        }

        protected virtual CoreDataRowContainer CreateContainerInstance() => new();
        protected virtual CoreContainerDataPropsBuilder CreateContainerDataPropsBuilderInstance() => new CoreContainerDataPropsBuilder();

        protected virtual CoreContainerDataPropsBuilder CreateContainerProps(int rowHandle, IReadOnlyCollection<string> skipColumns)
        {
            var columnsCount = DataColumnInfo.ColumnsCount;

            var data = new Data<object>();
            
            data.Ensure(columnsCount);

            var copyOriginalData = StateInfo.GetRowState(rowHandle) == RowState.Modified;

            Data<object> originalData = null;
            
            if (copyOriginalData)
            {
                originalData = new Data<object>();
                originalData.Ensure(columnsCount);
            }

            var dataStorageCount = DataColumnInfo.Columns.Count;
            
            for (int columnHandle = 0; columnHandle < columnsCount && columnHandle < dataStorageCount; columnHandle++)
            {
                var dataColumn = DataColumnInfo.Columns[columnHandle];
                
                if (skipColumns == null || skipColumns.Contains(dataColumn.ColumnName) == false)
                {
                    data[columnHandle] = GetRowFieldValue(rowHandle, dataColumn, DefaultValueType.Passed, null);

                    if (copyOriginalData)
                    {
                        originalData[columnHandle] = GetOriginalData(rowHandle, dataColumn);
                    }
                }
            }

            Map<string, ExtPropertyValue> extProperties = null;

            if (rowHandle < StateInfo.RowXProps.Storage.Count)
            {
                var dict = StateInfo.RowXProps.Storage[rowHandle];

                Set<string> changedPropertyNames = null;

                if (copyOriginalData)
                {
                    changedPropertyNames = StateInfo.RowXProps.GetChangedPropertyNames(rowHandle).ToSet();
                }

                if (dict != null && dict.Count > 0)
                {
                    extProperties = new Map<string, ExtPropertyValue>();

                    foreach (var kv in dict)
                    {
                        var original = kv.Value;

                        if (changedPropertyNames != null && changedPropertyNames.Contains(kv.Key))
                        {
                            original = StateInfo.RowXProps.GetOriginalValue(rowHandle, kv.Key);
                        }
                        
                        extProperties[kv.Key] = new ExtPropertyValue {Current = original == kv.Value ? null : kv.Value, Original = original};
                    }
                }
                
                changedPropertyNames?.Dispose();
            }

            var builder = CreateContainerDataPropsBuilderInstance();

            builder.Data = data;
            builder.DataRowState = StateInfo.GetRowState(rowHandle);
            builder.ExtProperties = extProperties;
            builder.Age = StateInfo.GetRowAge(rowHandle);
            builder.OriginalData = originalData;
            builder.DisplayDateTimeUtcOffsetTicks = DisplayDateTimeUtcOffsetTicks;
            builder.RowHandle = rowHandle;
            builder.AnnotationAge = StateInfo.GetRowAnnotationAge(rowHandle);

            return builder;
        }
    }
}