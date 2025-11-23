using System;
using System.Collections.Generic;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

using IRowXPropertyInfo = Brudixy.IRandomAccessTransactionData<Konsarpoo.Collections.Map<string, Konsarpoo.Collections.Map<string, object>>, System.Collections.Generic.KeyValuePair<string, (string key, object value)>>;

namespace Brudixy
{
    internal class RowXPropertyInfoDataItem 
    {
        internal CoreDataTable m_table;
        
        [NotNull] 
        public IRowXPropertyInfo Storage = new RandomAccessTransactionData<Map<string, Map<string, object>>, KeyValuePair<string, (string key, object value)>>();
        
        public RowXPropertyInfoDataItem(CoreDataTable table)
        {
            m_table = table;
        }

        protected void OnRejectStorageChange(IRowXPropertyInfo store, IRowXPropertyInfo.DataItemChange dataItemChange)
        {
            if (dataItemChange.IsNull == 1)
            {
                var storage = Storage[dataItemChange.RowHandle];

                if (storage == null)
                {
                    storage = new();
                }
                
                storage[dataItemChange.Value.Key] = null;
            }
            else
            {
                var storage = Storage[dataItemChange.RowHandle];

                if (storage == null)
                {
                    storage = new();
                }

                var orAdd = storage.GetOrAdd(dataItemChange.Value.Key,() => new Map<string, object>());
                
                orAdd[dataItemChange.Value.Key] = dataItemChange.Value.Value.value;
            }
        }
        
        public void Ensure(int capacity)
        {
            Storage.Ensure(capacity);
        }
        
        public void Clear(ICoreDataTableColumn column)
        {
            foreach (var xProps in Storage)
            {
                if (xProps != null)
                {
                    foreach (var kv in xProps)
                    {
                        kv.Value?.Clear();
                    }
                }
            }
            
            Storage.Clear();
        }

        public void Dispose(ICoreDataTableColumn column)
        {
            foreach (var xProps in Storage)
            {
                if (xProps != null)
                {
                    foreach (var kv in xProps)
                    {
                        kv.Value?.Dispose();
                    }

                    xProps.Dispose();
                }
            }

            Storage.Dispose();
        }

        public RowXPropertyInfoDataItem Copy(CoreDataTable table)
        {
            var dataItem = (RowXPropertyInfoDataItem)this.MemberwiseClone();

            dataItem.m_table = table;
            
            dataItem.Storage = Storage.Copy();

            dataItem.Storage.Ensure(Storage.Count);

            int i = 0;
            
            foreach (var xProps in Storage)
            {
                if (xProps?.Count > 0)
                {
                    var map = new Map<string, Map<string, object>>(xProps);
                    
                    foreach (var xProp in xProps)
                    {
                        if (xProp.Value != null)
                        {
                            var propValue = xProp.Value;

                            var objects = new Map<string, object>(propValue);

                            foreach (var kv in propValue)
                            {
                                var val = kv.Value;
                                
                                CoreDataRowContainer.CopyIfNeededBoxed(ref val);
                                
                                objects[kv.Key] = val;
                            }
                            
                            map[xProp.Key] = objects;
                        }
                    }
                    
                    dataItem.Storage[i] = map;
                }
                else
                {
                    dataItem.Storage[i] = null;
                }

                i++;
            }
            
            return dataItem;
        }

        public object GetData(int rowHandle, string propCode, string key)
        {
            if (rowHandle >= Storage.Count)
            {
                return null;
            }

            var data = Storage[rowHandle]?.GetOrDefault(propCode)?.GetOrDefault(key);

            if (data != null)
            {
                CoreDataRowContainer.CopyIfNeededBoxed(ref data);
            }
            
            return data;
        }

        public IReadOnlyDictionary<string, object> GetDataInfo(int rowHandle, string propCode)
        {
            if (rowHandle >= Storage.Count)
            {
                return null;
            }

            return Storage[rowHandle]?.GetOrDefault(propCode);
        }
        
        public bool SetValue(int rowHandle, string propCode, string key, object prevValue, object newVal, int? tranId)
        {
            Ensure(Math.Max(m_table.RowCount, rowHandle + 1));

            var xPropStorage = Storage[rowHandle];

            if (xPropStorage == null)
            {
                Storage[rowHandle] = xPropStorage = new();
            }

            var changed = xPropStorage.MissingKey(propCode) || xPropStorage[propCode].MissingKey(key) || !newVal.Equals(prevValue);

            if (changed == false)
            {
                return false;
            }
            
            if (ShouldLogChange(rowHandle))
            {
                var pVal = new KeyValuePair<string, (string key, object value)>(propCode, (key, prevValue));
                var nVal = new KeyValuePair<string, (string key, object value)>(propCode, (key, newVal));
                
                Storage.LogChange(rowHandle, prevValue is null, ref pVal, ref nVal, tranId);
            }

            xPropStorage.GetOrAdd(propCode, () => new Map<string, object>())[key] = newVal;

            Storage.UpdateCellAge(rowHandle);

            return true;
        }
        
        protected bool ShouldLogChange(int rowHandle)
        {
            return (m_table.TrackChanges ||
                    m_table.GetIsInTransaction() ||
                    (Storage.HasAnyChangeLogged(rowHandle)) == false);
        }
        
        public bool RollbackRowTransaction(int rowHandle, int tranId)
        {
            return Storage.RollbackRowTransaction(rowHandle, tranId, OnRejectStorageChange);
        }

        public void StopLoggingTransactionChanges(int rowHandle)
        {
            Storage.StopLoggingTransactionChanges(rowHandle);
        }

        public void RejectAllChanges()
        {
            Storage.RejectAllChanges(null, OnRejectStorageChange );
        }

        public void RejectChanges(int rowHandle)
        {
            Storage.RejectChanges(rowHandle, null, OnRejectStorageChange);
        }

        public void AcceptChanges(int rowHandle)
        {
            Storage.AcceptChanges(rowHandle);
        }
    }
}