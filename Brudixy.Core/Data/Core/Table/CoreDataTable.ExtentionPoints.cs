using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        protected virtual bool OnBeforeSetXProperty<T>(string propertyName, [CanBeNull] ref T value) => true;

        protected virtual void OnSetXProperty<T>(string propertyName, [CanBeNull] T value) => OnMetadataChanged();

        protected virtual void RemapColumnHandles(Map<int, int> oldToNewMap)
        {
            DataColumnInfo.RemapColumnHandles(oldToNewMap);
            
            IndexInfo.RemapColumnHandles(oldToNewMap);
            MultiColumnIndexInfo.RemapColumnHandles(oldToNewMap);

            RemapRelationColumnHandles(oldToNewMap);
        }

        protected virtual void OnBeforeAcceptChanges()
        {
        }

        protected virtual void OnBeforeRejectChanges()
        {
        }

        protected virtual void OnBeforeRowColumnSet(int rowHandle, 
            CoreDataColumn column, 
            ref object value,
            object prevValue,
            ref bool canContinue,
            ref string cancelExceptionMessage, 
            ref Map<int, object> cascadePrevValues, 
            bool isNotInit)
        {
        }

        protected virtual void OnBeforeRowColumnSet<T>(int rowHandle, 
            CoreDataColumn column, 
            ref T value,
            T prevValue,
            ref bool canContinue,
            ref string cancelExceptionMessage, 
            ref Map<int, object> cascadePrevValues, 
            bool isNotInit)
        {
        }

        protected virtual void OnRowChanged(CoreDataColumn column, int rowHandle, object value, object prevValue,
            Map<int, object> cascadePrevValues)
        {
            LogChange(column.ColumnName, value, prevValue, rowHandle);
        }

        protected virtual void OnRowChanged<T>(CoreDataColumn colum, int rowHandle, T? value, T? prevValue, Map<int, object> cascadePrevValues) where T : struct, IComparable, IComparable<T>
        {
            LogChange(colum.ColumnName, value, prevValue, rowHandle);
        }

        protected virtual IDataLogEntry CreateLogEntry(object contextChange, string columnOrXProperty, object value, object prevValue, int rowHandle, DateTime? changeUtcTimestamp)
        {
            var tranId = GetTranId();

            return new DataLogEntry(rowHandle, contextChange, columnOrXProperty, value, prevValue, m_dataAge, changeUtcTimestamp, tranId);
        }
        
        protected virtual DateTime? GetChangeUtcTimestamp()
        {
            return GetChangeTimeStamp(m_stopwatch, m_utsStopWatchStart);
        }

        protected virtual void OnColumnRemoved(int columnHandle)
        {
        }

        protected virtual void OnClearColumns()
        {
        }

        protected virtual void MergeTableMetadata(CoreDataTable currentTable, CoreDataTable sourceTable)
        {
            currentTable.MergeMetaOnly(sourceTable);
        }

        protected virtual void MergeTable(bool overrideExisting, CoreDataTable table, CoreDataTable sourceTable)
        {
            table.MergeDataOnly(sourceTable, overrideExisting);
        }

        protected virtual void OnNewRowAdded(int rowHandle)
        {
        }

        protected virtual void OnNewRowAdding(out bool isCancel)
        {
            isCancel = false;
        }

        protected virtual void OnBeforeNewRowValueSet(int columnHandle, Data<object> data, ref bool canContinue)
        {
        }

        protected virtual void OnEndLoad()
        {
        }

        protected virtual void OnEventsUnlocked()
        {
        }

        protected virtual void OnRollbackTransaction(int tranId)
        {
            CoreDataTable ds = null;
            if (this.DataSetReference?.TryGetTarget(out ds) ?? false)
            {
                ds.ClearTableTransactionChanges(TableName, tranId);
            }
        }

        protected virtual void OnTransactionFullCommit()
        {
        }

        protected virtual void OnRollbackRowTransaction(int rowHandle, int transaction, IReadOnlyCollection<int> columnChangedInfo)
        {
        }

        protected virtual void ResetTransactionAggregatedEvents(int rowHandle,
            int transaction,
            IReadOnlyCollection<int> columnChangedInfo,
            IReadOnlyCollection<string> xPropChangedInfo)
        {
        }

        protected virtual void StopLoggingTransactionChanges(int rowHandle)
        {
            StateInfo.CommitRowTransaction(rowHandle, true);
        }

        protected virtual CoreDataColumnInfo CreateDataColumnInfo() => new ();

        protected virtual CoreDataTable CloneCore(Thread thread, bool withData = true, bool cloneColumns = true)
        {
            if (IsDisposed)
            {
                return null;
            }

            var clone = CloneTable(thread, withData, cloneColumns);

            CloneComposite(clone, withData);
            
            clone.TableIsReadOnly = TableIsReadOnly;
            
            return clone;
        }

        protected virtual void CloneDataColumnInfo(CoreDataColumnInfo dataColumnInfo, bool withData)
        {
            DataColumnInfo.CopyFrom(this, dataColumnInfo, withData);
        }

        protected virtual void OnDisposed()
        {
        }
    }
}