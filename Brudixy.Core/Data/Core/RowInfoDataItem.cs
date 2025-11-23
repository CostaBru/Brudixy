using System;
using Brudixy.Interfaces;
using JetBrains.Annotations;

namespace Brudixy
{
    internal class RowInfoDataItem
    {
        private object m_defaultNullValue;
        private CoreDataTable m_dataTable;
        
        [NotNull] 
        public IRandomAccessTransactionData<object, object> Storage;
        
        public RowInfoDataItem(CoreDataTable table)
        {
            m_dataTable = table;
            Storage = new RandomAccessTransactionData<object, object>();
        }

        public void Clear(ICoreDataTableColumn column)
        {
            Storage.Clear();
        }

        public void Dispose(ICoreDataTableColumn column)
        {
            Storage.Dispose();
        }

        public RowInfoDataItem Copy(CoreDataTable table)
        {
            var dataItem = (RowInfoDataItem)base.MemberwiseClone();

            dataItem.Storage = Storage.Copy();

            return dataItem;
        }

        public object GetData(int rowHandle)
        {
            Storage.Ensure(m_dataTable.StateInfo.RowStorageCount);
            
            return Storage[rowHandle];
        }
        
        public bool SetValue(int rowHandle, object newVal, int? tranId)
        {
            Storage.Ensure(m_dataTable.StateInfo.RowStorageCount);
        
            var prevValue = Storage[rowHandle];

            var changed = newVal != prevValue;

            if (changed == false)
            {
                return false;
            }
            
            CoreDataRowContainer.CopyIfNeededBoxed(ref newVal);
            
            if (ShouldLogChange(rowHandle))
            {
                Storage.LogChange(rowHandle, prevValue == null, ref prevValue, ref newVal, tranId);
            }

            Storage[rowHandle] = newVal;

            Storage.UpdateCellAge(rowHandle);

            return true;
        }
        
        protected bool ShouldLogChange(int rowHandle)
        {
            return (m_dataTable.TrackChanges ||
                    m_dataTable.GetIsInTransaction() ||
                    (Storage.HasAnyChangeLogged(rowHandle)) == false);
        }

        public void Merge(RowInfoDataItem list)
        {
            this.Storage = list.Storage.Copy();
        }

        public void RollbackRowTransaction(int rowHandle, int tranId)
        {
            Storage.RollbackRowTransaction(rowHandle, tranId, OnRejectStorageChange);
        }

        public void StopLoggingTransactionChanges(int rowHandle)
        {
            Storage.StopLoggingTransactionChanges(rowHandle);
        }

        public void RejectAllChanges()
        {
            Storage.RejectAllChanges(null, OnRejectStorageChange);
        }
        
        protected void OnRejectStorageChange(IRandomAccessTransactionData<object, object> data, IRandomAccessTransactionData<object, object>.DataItemChange dataItemChange)
        {
            if (dataItemChange.IsNull == 1)
            {
                data[dataItemChange.RowHandle] = null;
            }
            else
            {
                var value = dataItemChange.Value;

                if (value is null || (value is IReadonlySupported && ((IReadonlySupported)value).IsReadOnly))
                {
                    data[dataItemChange.RowHandle] = value;
                }
                else
                {
                    data[dataItemChange.RowHandle] = (value as ICloneable)?.Clone() ?? value;
                }
            }
        }

    }
}