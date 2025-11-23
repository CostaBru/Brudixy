using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml.Linq;
using Brudixy.Converter;
using Brudixy.Expressions;
using Brudixy.Interfaces;

namespace Brudixy
{
    [DebuggerDisplay("Count {Storage.Count}, Storage Type {CloneableStorage.GetType()}")]
    internal class ComplexTypeDataItem<T>: 
        ITypedDataItem<T>,
        IDataItem
        where T : class, ICloneable, IXmlSerializable, IJsonSerializable, new()
    {
        private static readonly bool s_readOnlySupported = Tool.IsReadonlySupported<T>();
        private static readonly bool s_immutable = Tool.IsImmutable<T>();

        internal IRandomAccessTransactionData<T, T> Storage = new RandomAccessTransactionData<T, T>();

        internal CoreDataTable m_table;
        
        protected object m_defaultBoxed;
        protected T m_defaultNullValue;
        
        public ComplexTypeDataItem()
        {
        }
      
        public void Init(TableStorageType type, TableStorageTypeModifier modifier, CoreDataTable table)
        {
            m_table = table;
        }

        public object GetCurrentMax(ICoreDataTableColumn column)
        {
            return null;
        }

        protected void OnRejectStorageChange(IRandomAccessTransactionData<T, T> data, IRandomAccessTransactionData<T, T>.DataItemChange dataItemChange)
        {
            if (dataItemChange.IsNull == 1)
            {
                data[dataItemChange.RowHandle] = null;
            }
            else
            {
                var value = dataItemChange.Value;

                if (value is null || s_immutable || (s_readOnlySupported && ((IReadonlySupported)value).IsReadOnly))
                {
                    data[dataItemChange.RowHandle] = value;
                }
                else
                {
                    data[dataItemChange.RowHandle] = value?.Clone() as T;
                }
            }
        }

        public bool IsCellChanged(int rowHandle, ICoreDataTableColumn column)
        {
            return Storage.IsChanged(rowHandle);
        }

        public void CreateEmptyRows(int rowCount, ICoreDataTableColumn column)
        {
            var storageCount = Storage.Count;

            if (storageCount < rowCount)
            {
                Storage.Ensure(rowCount);
            }
        }

        public IEnumerable<int> Filter(object value, ICoreDataTableColumn column)
        {
            var count = Storage.Count;

            var tv = ConvertBoxed(value, out var needClone, column);

            var equalityComparer = EqualityComparer<T>.Default;

            for (var index = 0;  index < count; index++)
            {
                var val = Storage[index];

                if (val is null)
                {
                    continue;
                }

                if (equalityComparer.Equals(tv, val))
                {
                    yield return index;
                }
            }
        }

        public object GetAggregateValue(IEnumerable<int> handles, AggregateType type, ICoreDataTableColumn column)
        {
            return null;
        }

        public virtual void Clear(ICoreDataTableColumn column)
        {
            foreach (var val in Storage)
            {
                if (val is IReadonlySupported rs && rs.IsReadOnly)
                {
                    continue;
                }
                
                (val as IDisposable)?.Dispose();
            }
        }

        public void SetAllNull(ICoreDataTableColumn column)
        {
            var count = Storage.Count;
            
            Storage.Clear();
            
            Storage.Ensure(count);
        }

        public IComparable CalcMinMax(bool calcMax, ICoreDataTableColumn column, IEnumerable<int> rows = null)
        {
            bool any = false;

            T minMax = default(T);

            if (rows != null)
            {
                foreach (var rowHandle in rows)
                {
                    CalcMinMax(rowHandle, calcMax, ref any, ref minMax);
                }
            }
            else
            {
                foreach (var rowHandle in m_table.StateInfo.RowHandles)
                {
                    CalcMinMax(rowHandle, calcMax, ref any, ref minMax);
                }
            }

            return minMax?.Clone() as IComparable;
        }
        
        private void CalcMinMax(int rowHandle, bool calcMinMaxValue, ref bool anyValue, ref T minMax1)
        {
            T value = Storage[rowHandle];

            if (value is IComparable<T> data)
            {
                if (anyValue == false)
                {
                    anyValue = true;

                    minMax1 = value;
                }
                else
                {
                    var compareTo = data.CompareTo(minMax1);

                    minMax1 = calcMinMaxValue ? compareTo > 0 ? value : minMax1 : compareTo > 0 ? minMax1 : value;
                }
            }
        }

        public object TryGetData(int rowIndex, ICoreDataTableColumn column)
        {
            if (IsNull(rowIndex, column))
            {
                return null;
            }

            var value = Storage[rowIndex];

            if (value is null || s_immutable || (s_readOnlySupported && ((IReadonlySupported)value).IsReadOnly))
            {
                return value;
            }
            
            return value?.Clone() as T;
        }

        public void StartLoggingTransactionChanges(int rowHandle, int tranId, ICoreDataTableColumn column)
        {
            Storage.StartLoggingTransactionChanges(rowHandle, tranId);
        }

        public void StopLoggingTransactionChanges(int rowHandle, ICoreDataTableColumn column)
        {
            Storage.StopLoggingTransactionChanges(rowHandle);
        }

        public bool RollbackRowTransaction(int rowHandle, int tranId, ICoreDataTableColumn column)
        {
            return Storage.RollbackRowTransaction(rowHandle, tranId, OnRejectStorageChange);
        }

        public bool IsChangedInTransaction(int rowHandle, int tranId, ICoreDataTableColumn column)
        {
            return Storage.IsChangedInTransaction(rowHandle, tranId);
        }

        public object GetTransactionOriginalValue(int rowHandle, int tranId, ICoreDataTableColumn column)
        {
            return Storage.GetTransactionOriginalValue(rowHandle, tranId);
        }

        public bool IsNull(int rowIndex, ICoreDataTableColumn column)
        {
            return Storage[rowIndex] is null;
        }

        public object CheckValueIsCompatibleType(object value, ICoreDataTableColumn column)
        {
            return ConvertBoxed(value, out var _, column);
        }
        
        void IDataItem.GetValidValue(ref object value, int rowHandle, ICoreDataTableColumn column)
        {
            if (value is not T)
            {
                if (value is XElement x)
                {
                    var item = new T();
                    
                    item.FromXml(x);

                    value = item;
                }
                else if(value is JElement j)
                {
                    var item = new T();
                    
                    item.FromJson(j);

                    value = item;
                }
                else if(value is string str)
                {
                    try
                    {
                        var item = new T();
                    
                        item.FromXml(XElement.Parse(str));

                        value = item;
                    }
                    catch 
                    {
                        var item = new T();
                    
                        item.FromJson(JElement.Parse(str));

                        value = item;
                    }
                }
            }
        }

        public void UpdateCellAge(int rowHandle, ICoreDataTableColumn column)
        {
            Storage.UpdateCellAge(rowHandle);
        }

        public bool IsNullValue(object value, ICoreDataTableColumn column)
        {
            return value is null;
        }

        private T ConvertBoxed(object value, out bool needClone, ICoreDataTableColumn column)
        {
            needClone = true;
            
            if(value is T tv)
            {
                return tv;
            }

            if (value is null)
            {
                return default;
            }

            var columnType = CoreDataTable.GetColumnType(value.GetType());
            
            try
            {
                var str = CoreDataTable.ConvertObjectToString(columnType.type, columnType.typeModifier, value?.ToString());

                var newT = new T();

                newT.FromXml(XElement.Parse(str));

                if (Trace.Listeners.Count > 0)
                {
                    Trace.WriteLine(
                        $"Performance critical column type mismatching XML conversion occured on set value. Please sync data types to avoid this. Table = '{m_table.Name}', ColumnName '{column.ColumnName}', ColumnType '{typeof(T).FullName}' ConversionType = '{value.GetType().FullName}'");
                }

                needClone = false;

                return newT;
            }
            catch (Exception)
            {
                var str = CoreDataTable.ConvertObjectToString(columnType.type, columnType.typeModifier, value?.ToString());

                var newT = new T();

                newT.FromJson(JElement.Parse(str));

                if (Trace.Listeners.Count > 0)
                {
                    Trace.WriteLine(
                        $"Performance critical column type mismatching JSON conversion occured on set value. Please sync data types to avoid this. Table = '{m_table.Name}', ColumnName '{column.ColumnName}', ColumnType '{typeof(T).FullName}' ConversionType = '{value.GetType().FullName}'");
                }

                needClone = false;

                return newT;
            }
        }

        public object GetDefaultValue(ICoreDataTableColumn column)
        {
            return m_defaultBoxed;
        }

        public virtual void Dispose(ICoreDataTableColumn column)
        {
            Clear(column);

            Storage.Dispose();
        }

        object IDataItem.GetData(int rowIndex, ICoreDataTableColumn column)
        {
            return GetData(rowIndex);
        }

        public object GetRawData(int rowIndex, ICoreDataTableColumn column)
        {
            if (rowIndex >= Storage.Count)
            {
                return default;
            }

            return Storage[rowIndex];
        }

        public void SilentlySetValue(int rowHandle, object value, ICoreDataTableColumn column)
        {
            if (Storage.Count == 0)
            {
                CreateEmptyRows(m_table.RowCount, column);
            }

            if (value is null)
            {
                Storage[rowHandle] = null;
            }
            else
            {
                //no clone or copy here
                Storage[rowHandle] = ConvertBoxed(value, out var _, column);
            }
        }

        public object GetOriginalValue(int rowHandle, ICoreDataTableColumn column)
        {
            return GetOriginalTypedValue(rowHandle, column);
        }

        public uint GetAge(int rowHandle, ICoreDataTableColumn column)
        {
            return Storage.GetAge(rowHandle);
        }

        public void UpdateMax(object cellVal, ICoreDataTableColumn column)
        {
        }

        public object NextAutoIncrementValue(ICoreDataTableColumn column)
        {
            return null;
        }

        public void RejectAllChanges(IReadOnlyDictionary<int, int> changesCount, ICoreDataTableColumn column)
        {
            Storage.RejectAllChanges(changesCount, OnRejectStorageChange);
        }

        public void AcceptAllChanges(ICoreDataTableColumn column)
        {
            Storage.AcceptAllChanges();
        }

        public virtual ComplexTypeDataItem<T> Copy(CoreDataTable table)
        {
            var dataItem = (ComplexTypeDataItem<T>)this.MemberwiseClone();

            dataItem.m_table = table;
            
            dataItem.Storage = this.Storage.Copy();
            
            dataItem.Storage.Ensure(Storage.Count);

            var count = Storage.Count;
            
            for (var index = 0; index < count; index++)
            {
                var value = Storage[index];

                if (value is null || (s_readOnlySupported && ((IReadonlySupported)value).IsReadOnly))
                {
                    dataItem.Storage[index] = value;
                }
                else
                {
                    dataItem.Storage[index] = value?.Clone() as T;
                }
            }

            return dataItem;
        }
        
        IDataItem IDataItem.Copy(CoreDataTable table, ICoreDataTableColumn column)
        {
            return this.Copy(table);
        }

        IDataItem IDataItem.Clone(CoreDataTable table, ICoreDataTableColumn column)
        {
            var dataItem = (ComplexTypeDataItem<T>)this.MemberwiseClone();

            dataItem.m_table = table;
            dataItem.Storage = Storage.Clone();
            
            return dataItem;
        }

        public IEnumerable GetStorage(ICoreDataTableColumn column) => Storage;

        public T GetData(int rowHandle)
        {
            if (rowHandle >= Storage.Count)
            {
                return default;
            }

            var value = Storage[rowHandle];

            if (value is null || s_immutable || (s_readOnlySupported && ((IReadonlySupported)value).IsReadOnly))
            {
                return value;
            }
            
            return value.Clone() as T;
        }

        public T GetDataTyped(int rowIndex, ICoreDataTableColumn column)
        {
            return GetData(rowIndex);
        }

        public T GetOriginalTypedValue(int rowHandle, ICoreDataTableColumn column)
        {
            if (Storage.TryGetFirstLoggedValue(rowHandle, out var originalTypedValue))
            {
                var val = originalTypedValue;

                if (val is null || s_immutable || (s_readOnlySupported && ((IReadonlySupported)val).IsReadOnly))
                {
                    return val;
                }
                    
                return val?.Clone() as T;
            }

            var value = Storage[rowHandle];

            if (value is null || s_immutable || (s_readOnlySupported && ((IReadonlySupported)value).IsReadOnly))
            {
                return value;
            }
            
            return value.Clone() as T;
        }

        public void AcceptChanges(int rowHandle, ICoreDataTableColumn column)
        {
            Storage.AcceptChanges(rowHandle);
        }

        public void RejectChanges(int rowHandle, int? changesCount, ICoreDataTableColumn column)
        {
            Storage.RejectChanges(rowHandle, changesCount, OnRejectStorageChange);
        }

        public void AddNew(int rowHandle, object value, ICoreDataTableColumn column)
        {
            var typedValue = ConvertBoxed(value, out var needClone, column);

            if (needClone && typedValue is not null && s_immutable == false && (s_readOnlySupported == false || ((IReadonlySupported)typedValue).IsReadOnly) == false)
            {
                typedValue = (T)typedValue.Clone();
            }
           
            if (rowHandle >= Storage.Count)
            {
                Storage.Add(typedValue);

                UpdateCellAge(rowHandle, column);
            }
            else
            {
                Storage[rowHandle] = typedValue;

                UpdateCellAge(rowHandle, column);
            }
        }

        public bool SetValue(int rowHandle, object value, int? tranId, ICoreDataTableColumn column)
        {
            if (value == null)
            {
                return SetNull(rowHandle, true, tranId, column);
            }

            var typedValue = ConvertBoxed(value, out var needClone, column);

            if (needClone && typedValue is not null &&  s_immutable == false &&(s_readOnlySupported == false || ((IReadonlySupported)typedValue).IsReadOnly) == false)
            {
                typedValue = (T)typedValue.Clone();
            }

            return SetValue(rowHandle, typedValue, tranId, column);
        }

        public bool SetNull(int rowHandle, bool isNull, int? tranId, ICoreDataTableColumn column)
        {
            var prevNull = IsNull(rowHandle, column);

            var changed = prevNull != isNull;

            if (changed == false)
            {
                return false;
            }

            if (ShouldLogChange(rowHandle))
            {
                var value = Storage[rowHandle];

                T prevValue;
                
                if (value is null ||  s_immutable || (s_readOnlySupported && ((IReadonlySupported)value).IsReadOnly))
                {
                    prevValue = value;
                }
                else
                {
                    prevValue = value.Clone() as T;
                }

                Storage.LogChange(rowHandle, prevNull, ref prevValue, ref m_defaultNullValue, tranId);
            }

            if (isNull)
            {
                Storage[rowHandle] = default;
            }

            UpdateCellAge(rowHandle, column);

            return true;
        }
        
        protected bool ShouldLogChange(int rowHandle)
        {
            return (m_table.TrackChanges ||
                    m_table.GetIsInTransaction() ||
                    (Storage.HasAnyChangeLogged(rowHandle)) == false);
        }

        public bool SetValue(int rowHandle, T newVal, int? tranId, ICoreDataTableColumn column)
        {
            Storage.Ensure(m_table.StateInfo.RowStorageCount);
            
            var prevValue = Storage[rowHandle];

            var isNullPrev = IsNull(rowHandle, column);

            var changed = isNullPrev || newVal.Equals(prevValue) == false;

            if (changed == false)
            {
                return false;
            }
            
            T valueToStore;
            if (newVal is null ||  s_immutable || (s_readOnlySupported && ((IReadonlySupported)newVal).IsReadOnly))
            {
                valueToStore = newVal;
            }
            else
            {
                valueToStore = newVal.Clone() as T;
            }
            
            if (ShouldLogChange(rowHandle))
            {
                T pVal;
                if (prevValue is null || s_immutable || (s_readOnlySupported && ((IReadonlySupported)prevValue).IsReadOnly))
                {
                    pVal = prevValue;
                }
                else
                {
                    pVal = prevValue.Clone() as T;
                }

                Storage.LogChange(rowHandle, isNullPrev, ref pVal, ref valueToStore, tranId);
            }
       
            Storage[rowHandle] = valueToStore;
            UpdateCellAge(rowHandle, column);

            return true;
        }

        public T NextAutoIncrementValueTyped(ICoreDataTableColumn column)
        {
            return default;
        }

        public void GetValidValue(ref T value, int rowHandle, ICoreDataTableColumn column)
        {
        }
    }
}