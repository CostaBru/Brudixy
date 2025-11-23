using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Brudixy.Converter;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowAccessor ICoreDataTable.AddRow(ICoreDataRowReadOnlyAccessor rowAccessor) => AddRow(rowAccessor);

        public CoreDataRow AddRow(IReadOnlyDictionary<string, object> values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            return AddRow(NewRow(values));
        }

        public CoreDataRow AddRow(ICoreDataRowReadOnlyAccessor rowAccessor)
        {
            if (rowAccessor == null)
            {
                throw new ArgumentNullException(nameof(rowAccessor));
            }

            if (rowAccessor.GetTableName() != Name)
            {
                throw new ArgumentOutOfRangeException($"Cannot add row from table '{rowAccessor.GetTableName()}' to the '{Name}' table. Plese use the import API.");
            }
            
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot add new row to the '{Name}' table because it is readonly.");
            }

            var newRow = AddRow();

            if (newRow == null)
            {
                return null;
            }

            try
            {
                ImportNewRowCore(rowAccessor, newRow);
                
                StateInfo.SetAdded(newRow.RowHandle, GetTranId());

                if (IsInitializing == false)
                {
                    CheckRowConstraints(newRow.RowHandle);
                }

                OnNewRowAdded(newRow.RowHandle);
            }
            catch
            {
                RemoveRow(newRow.RowHandle);
                throw;
            }

            return newRow;
        }
        
           [NotNull]
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverageAttribute]
        ICoreDataRowContainer ICoreDataTable.NewRow(IReadOnlyDictionary<string, object> values = null) => NewRow(values);

        [NotNull]
        public CoreDataRowContainer NewRow(IReadOnlyDictionary<string, object> values = null)
        {
            var columnsCount = DataColumnInfo.ColumnsCount;

            var data = SetupNewRowData(values, columnsCount);

            var keyColumn = new Data<string>(PrimaryKeyColumns());

            var columnMap = CreateDataColumnContainers();

            var dataRowContainer = CreateContainerInstance();
            
            var columnContainers = DataColumnInfo.Columns.Select(c => CreateColumnContainerInstance(c.ColumnObj)).ToArray();

            var metadataProps = new CoreContainerMetadataProps(Name, columnContainers, columnMap, keyColumn, MetaAge);

            var propsBuilder = CreateContainerDataPropsBuilderInstance();
            propsBuilder.Data = data;
            propsBuilder.DataRowState = RowState.New;
            propsBuilder.RowHandle = -1;
            propsBuilder.DisplayDateTimeUtcOffsetTicks = DisplayDateTimeUtcOffsetTicks;

            dataRowContainer.Init(metadataProps, propsBuilder.ToProps());

            if (m_dataOwnerReference != null)
            {
                dataRowContainer.SetOwner(Owner);
            }

            dataRowContainer.RowRecordState = RowState.New;

            return dataRowContainer;
        }

        private Data<object> SetupNewRowData(IReadOnlyDictionary<string, object> values, int columnsCount)
        {
            var data = new Data<object>(columnsCount);

            data.Ensure(columnsCount);

            for (int columnHandle = 0; columnHandle < columnsCount; columnHandle++)
            {
                var dataColumn = DataColumnInfo.Columns[columnHandle];

                if (values != null)
                {
                    if (values.TryGetValue(dataColumn.ColumnName, out var predefinedValue))
                    {
                        if (predefinedValue != null)
                        {
                            data[columnHandle] = predefinedValue;

                            continue;
                        }
                    }
                }

                bool canContinue = true;
                
                OnBeforeNewRowValueSet(columnHandle, data, ref canContinue);

                if (canContinue == false)
                {
                    continue;
                }

                var isAutomaticValue = dataColumn.IsAutomaticValue;

                if (isAutomaticValue)
                {
                    var nextAutoIncrementValue = dataColumn.DataStorageLink.NextAutoIncrementValue(dataColumn);
                    
                    data[columnHandle] = nextAutoIncrementValue;
                }
                else
                {
                    var defaultValue = dataColumn.DefaultValue;
                    
                    if (defaultValue != null)
                    {
                        CoreDataRowContainer.CopyIfNeededBoxed(ref defaultValue);
                        
                        data[columnHandle] = defaultValue;
                    }
                }
            }

            return data;
        }

        [CanBeNull]
        protected CoreDataRow AddRow()
        {
            OnNewRowAdding(out var isCancel);

            if (isCancel)
            {
                return null;
            }

            var rowHandle = StateInfo.GetNewRowHandle(this);

            var count = DataColumnInfo.ColumnsCount;

            for (int columnIndex = 0; columnIndex < count; columnIndex++)
            {
                var dataColumn = DataColumnInfo.Columns[columnIndex];
                
                var dataItem = dataColumn.DataStorageLink;

                dataItem.AddNew(rowHandle, null, dataColumn);
            }
            
            return GetRowInstance(rowHandle);
        }
        
        private void SetupRowState(RowState rowState, int rowHandle)
        {
            var tranId = GetTranId();

            switch (rowState)
            {
                case RowState.New:
                case RowState.Added:
                    StateInfo.SetAdded(rowHandle, tranId);
                    break;
                case RowState.Modified:
                    StateInfo.SetModified(rowHandle, tranId);
                    break;
                case RowState.Deleted:
                    StateInfo.SetDeleted(rowHandle, tranId);
                    break;
                case RowState.Unchanged:
                    StateInfo.AcceptChangesRow(this, rowHandle, tranId);
                    break;
            }
        }
        
        internal void AttachRow(CoreDataRow row, object debugKeyValue)
        {
            if (m_rowReferences != null && row.RowHandleCore < m_rowReferences.Count)
            {
                var instance = m_rowReferences[row.RowHandleCore];

                if (instance != null && ReferenceEquals(instance, row) == false)
                {
                    throw new DataDetachedException($"Cannot attach current data row reference '{debugKeyValue}' to the data table because only one instance of row is allowed.");
                }
            }
        }
    }
}