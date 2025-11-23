using System;
using Brudixy.Converter;
using Konsarpoo.Collections;

namespace Brudixy
{
    internal class ValueInfo
    {
        public const string Error = "Error";
        public const string Warning = "Warning";
        public const string Info = "Info";
        public const string Fault = "Fault";
        
        public Map<string, RowInfoDataItem> RowAnnotations = new();

        public bool SetRowAnnotation(CoreDataTable dataTable, int rowHandle, object value, int? tranId, string key)
        {
            if (RowAnnotations.TryGetValue(key, out var item) == false)
            {
                RowAnnotations[key] = item = new RowInfoDataItem(dataTable);
            }

            return item.SetValue(rowHandle, value, tranId);
        }

        public T GetRowAnnotation<T>(int rowHandle, string key)
        {
            if (RowAnnotations.TryGetValue(key, out var dataItem) == false)
            {
                return TypeConvertor.ReturnDefault<T>();
            }

            var value = dataItem.GetData(rowHandle);
            
            return XPropertyValueConverter.TryConvert<T>("RowAnnotation", key, value);
        }
        
        public bool HasRowAnnotation(int rowHandle, string key)
        {
            if (RowAnnotations.TryGetValue(key, out var dataItem) == false)
            {
                return false;
            }

            var value = dataItem.GetData(rowHandle);
            
            return value != null;
        }

        public ValueInfo Clone(CoreDataTable table)
        {
            ValueInfo clone = new();

            clone.RowAnnotations = new Map<string, RowInfoDataItem>();

            foreach (var into in RowAnnotations)
            {
                clone.RowAnnotations[into.Key] = (RowInfoDataItem)into.Value.Copy(table);
            }

            return clone;
        }

        public void Dispose()
        {
            Clear();
        }

        public void Clear()
        {
            RowAnnotations.Clear();

            foreach (var info in RowAnnotations)
            {
                info.Value.Clear(null);
            }
        }

        public void Merge(ValueInfo sourceValueInfo)
        {
            foreach (var info in sourceValueInfo.RowAnnotations)
            {
                var thisVal = info.Value;
                
                if(sourceValueInfo.RowAnnotations.TryGetValue(info.Key, out var sourceValue))
                {
                    MergeItem(sourceValue, ref thisVal);
                }
            }
        }
        
        private static void MergeItem(RowInfoDataItem list, ref RowInfoDataItem infos)
        {
            infos.Merge(list);
        }

        public bool HasData()
        {
            return RowAnnotations.Count > 0;
        }

        public void RollbackRowTransaction(int rowHandle, int tranId)
        {
            foreach (var kv in RowAnnotations)
            {
                kv.Value.RollbackRowTransaction(rowHandle, tranId);
            }
        }

        public void StopLoggingTransactionChanges(int rowHandle)
        {
            foreach (var kv in RowAnnotations)
            {
                kv.Value.StopLoggingTransactionChanges(rowHandle);
            }
        }

        public void RejectChanges()
        {
            foreach (var kv in RowAnnotations)
            {
                kv.Value.RejectAllChanges();
            }
        }

        public bool ClearMetaInfo(string type)
        {
            if(RowAnnotations.TryGetValue(type, out var item))
            {
                item?.Clear(null);

                return true;
            }

            return false;
        }

        public void OnRowRemoved(int rowHandle)
        {
            foreach (var kv in RowAnnotations)
            {
                kv.Value.SetValue(rowHandle, null, null);
            }
        }
    }
}
