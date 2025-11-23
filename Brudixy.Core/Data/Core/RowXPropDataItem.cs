using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

using IXPropStore = Brudixy.IRandomAccessTransactionData<Konsarpoo.Collections.Map<string, object>, System.Collections.Generic.KeyValuePair<string, object>>;

namespace Brudixy
{
    public class RowXPropDataItem
    {
        internal CoreDataTable m_table;
        
        [NotNull] 
        public IXPropStore Storage = new RandomAccessTransactionData<Map<string, object>, KeyValuePair<string, object>>();
        
        public RowXPropDataItem(CoreDataTable table)
        {
            m_table = table;
        }

        protected void RejectStorageChange(IXPropStore store, IXPropStore.DataItemChange dataItemChange)
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

                var valueValue = dataItemChange.Value.Value;
                
                CoreDataRowContainer.CopyIfNeededBoxed(ref valueValue);
                
                storage[dataItemChange.Value.Key] = valueValue;
            }
        }
        
        public void Ensure(int capacity)
        {
            Storage.Ensure(capacity);
        }
        
        public void Clear()
        {
            Storage.Clear();
        }

        public void Dispose()
        {
            Storage.Dispose();
        }

        public RowXPropDataItem Copy(CoreDataTable table)
        {
            var dataItem = (RowXPropDataItem)this.MemberwiseClone();

            dataItem.Storage = Storage.Copy();

            dataItem.Storage.Ensure(Storage.Count);

            int i = 0;
            
            foreach (var xProps in Storage)
            {
                if (xProps?.Count > 0)
                {
                    var objects = new Map<string, object>(xProps);

                    foreach (var kv in xProps)
                    {
                        var val = kv.Value;
                                
                        CoreDataRowContainer.CopyIfNeededBoxed(ref val);
                                
                        objects[kv.Key] = val;
                    }
                            
                    dataItem.Storage[i] = objects;
                }
                else
                {
                    dataItem.Storage[i] = null;
                }

                i++;
            }
            
            return dataItem;
        }

        public object GetOriginalValue(int rowHandle, string propName)
        {
            var changes = Storage.GetChanges(rowHandle).ToArray();

            if (changes.Any())
            {
                var index = changes.FindIndex(propName, x => x.Value.Key);

                if (index < 0)
                {
                    return null;
                }

                var valueValue = changes[index].Value.Value;
                
                CoreDataRowContainer.CopyIfNeededBoxed(ref valueValue);
                
                return valueValue;
            }

            var originalValue = GetData(rowHandle, propName);
            
            CoreDataRowContainer.CopyIfNeededBoxed(ref originalValue);
            
            return originalValue;
        }

        public IEnumerable<string> GetChangedPropertyNames(int rowHandle)
        {
            var changes = Storage.GetChanges(rowHandle);
            
            foreach (var propName in changes.Select(c => c.Value.Key).Distinct())
            {
                yield return propName;
            }
        }

        public object GetData(int rowHandle, string propName)
        {
            if (rowHandle >= Storage.Count)
            {
                return null;
            }

            return Storage[rowHandle]?.GetOrDefault(propName);
        }
        
        public void SilentlySetValue(int rowHandle, string propName, object value)
        {
            Ensure(Math.Max(m_table.RowCount, rowHandle + 1));

            var xPropStorage = Storage[rowHandle];

            if (xPropStorage == null)
            {
                Storage[rowHandle] = xPropStorage = new();
            }

            CoreDataRowContainer.CopyIfNeededBoxed(ref value);
            
            xPropStorage[propName] = value;

            Storage.UpdateCellAge(rowHandle);
        }
        
        public bool SetValue(int rowHandle, string propName, object newVal, object prevValue, int? tranId)
        {
            Ensure(Math.Max(m_table.RowCount, rowHandle + 1));

            var xPropStorage = Storage[rowHandle];

            if (xPropStorage == null)
            {
                Storage[rowHandle] = xPropStorage = new();
            }

            var changed = xPropStorage.MissingKey(propName) || newVal != prevValue;

            if (changed == false)
            {
                return false;
            }
            
            if (ShouldLogChange(rowHandle))
            {
                var pVal = new KeyValuePair<string, object>(propName, prevValue);
                var nVal = new KeyValuePair<string, object>(propName, newVal);

                Storage.LogChange(rowHandle, prevValue is null, ref pVal, ref nVal, tranId);
            }
            
            CoreDataRowContainer.CopyIfNeededBoxed(ref newVal);

            xPropStorage[propName] = newVal;

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
            return Storage.RollbackRowTransaction(rowHandle, tranId, RejectStorageChange);
        }

        public void StopLoggingTransactionChanges(int rowHandle)
        {
            Storage.StopLoggingTransactionChanges(rowHandle);
        }

        public void RejectAllChanges()
        {
            Storage.RejectAllChanges(null, RejectStorageChange );
        }

        public void RejectChanges(int rowHandle)
        {
            Storage.RejectChanges(rowHandle, null, RejectStorageChange);
        }

        public void AcceptChanges(int rowHandle)
        {
            Storage.AcceptChanges(rowHandle);
        }
    }
}