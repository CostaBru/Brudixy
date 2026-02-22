using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO.Compression;
using System.Linq;
using System.Text.Json.Nodes;
using System.Xml.Linq;
using Brudixy.Converter;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using Brudixy.Storage;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    [DebuggerDisplay("Count {Storage.Count}, Storage.Type {Storage.GetType()}")]
    internal class DataItem<T> :  IDataItem, ITypedDataItem<T>
    {
        internal CoreDataTable m_table;
        
        protected object m_defaultBoxed;
        protected T m_defaultNullValue;
        
        private Func<CoreDataTable, ColumnHandle, T, int, bool, T> ValueCheck;
        private static readonly EqualityComparer<T> s_equalityComparer = EqualityComparer<T>.Default;
        
        private static readonly Type m_dataType = typeof(T);

        private EqualityComparer<T> m_equalityComparer = s_equalityComparer;
        
        internal bool IsArray => ItemTypeModifier == TableStorageTypeModifier.Array;

        private Func<IRandomAccessTransactionData<T, T>, IRandomAccessTransactionData<T, T>> CopyFunc { get; set; } = DataItemFeatureSetup<T>.CopyFunc;
        private Func<IRandomAccessTransactionData<T, T>, IRandomAccessTransactionData<T, T>> CloneFunc { get; set; } = DataItemFeatureSetup<T>.CloneFunc;
        private Func<TableStorageType, TableStorageTypeModifier, T, T, bool> EqualsFunc { get; set; } = DataItemFeatureSetup<T>.EqualsFunc;
        private Func<IRandomAccessData<T>, T,  Func<TableStorageType, TableStorageTypeModifier, T, T, bool>, IEnumerable<int>> FilterFunc { get; set; } = DataItemFeatureSetup<T>.FilterFunc;
        private Func<IRandomAccessData<T>, IEnumerable<int>, AggregateType, object> AggregateFunc { get; set; } = DataItemFeatureSetup<T>.AggregateFunc;
        private Func<ICoreDataTableColumn, Type, T, T> GetAutomaticValueFunc { get; set; }
        private Func<Type, T, T, T> UpdateAutoIncrementFunc { get; set; }

        public T DefaultNullValue => m_defaultNullValue;

        private T m_lastAutomaticValue = default;
        
        public DataItem()
        {
        }

        public void Init(TableStorageType type,
            TableStorageTypeModifier modifier,
            CoreDataTable table
        )
        {
            m_table = table;
            
            Storage = new RandomAccessTransactionData<T, T>();

            StorageItemType = type;
            ItemTypeModifier = modifier;

            if (IsArray)
            {
                ValueCheck = DefaultDataItemValueValidator.ArrayValidator;
                EqualsFunc = (t, tm, x, y) => Tool.ArrayDeepEquals((Array)(object)x, (Array)(object)y);

                switch (type)
                {
                    case TableStorageType.Xml:
                        CopyFunc = DeepCopyImplXml;
                        break;
                    case TableStorageType.Json:
                        CopyFunc = DeepCopyImplJObj;
                        break;
                    default:
                        CopyFunc = DeepCopyImplArr;
                        break;
                }
            }
            else
            {
                EqualsFunc = (t, tm, x, y) => m_equalityComparer.Equals(x, y);

                GetAutomaticValueFunc = GetAutomaticValueImpl;
                UpdateAutoIncrementFunc = UpdateAutoIncImpl;
                AggregateFunc = AggregateFuncImpl;

                if (type == TableStorageType.Object)
                {
                    CopyFunc = DeepCopyImplObj;
                }
                
                if (Tool.IsString<T>())
                {
                    ValueCheck = DefaultDataItemValueValidator.StringValidator;
                }
                else if (Tool.IsDateTime<T>() || Tool.IsNullableDateTime<T>())
                {
                    ValueCheck = DefaultDataItemValueValidator.DateTimeValidator;
                }
            }
        }

        [NotNull] public IRandomAccessTransactionData<T, T> Storage;
        
        protected TableStorageType StorageItemType;
        protected TableStorageTypeModifier ItemTypeModifier;

        private static T UpdateAutoIncImpl(Type t, T v, T m)
        {
            var maxFunc = DataItemFeatureSetup<T>.MaxFuncRepository;
            if (maxFunc != null)
            {
                return maxFunc(v, m);
            }

            return v;
        }

        private static T GetAutomaticValueImpl(ICoreDataTableColumn column, Type t, T v)
        {
            var incrementFunc = DataItemFeatureSetup<T>.IncrementFuncRepository;
            
            if (incrementFunc != null)
            {
                return incrementFunc(column, v);
            }

            var func = DataItemFeatureSetup<T>.AutomaticValueFuncRepository;

            if (func != null)
            {
                return func(column);
            }

            return v;
        }

        private static IRandomAccessTransactionData<T, T> DeepCopyImplArr(IRandomAccessTransactionData<T, T> d)
        {
            var result = d.Clone();

            result.Ensure(d.Count);

            int index = 0;
            foreach (var v in d)
            {
                if (v is Array arr)
                {
                    var newVal = Array.CreateInstance(typeof(T).GetElementType(), arr.Length);

                    Array.Copy(arr, 0, newVal, 0, newVal.Length);

                    result[index] = (T)(object)newVal;
                }

                index++;
            }

            return result;
        }
        
        private static IRandomAccessTransactionData<T, T> DeepCopyImplObj(IRandomAccessTransactionData<T, T> d)
        {
            var result = d.Clone();

            result.Ensure(d.Count);

            int index = 0;
            foreach (var v in d)
            {
                if (d[index] is ICloneable cloneable)
                {
                    result[index] = (T)cloneable.Clone();
                }
                else
                {
                    result[index] = v;
                }

                index++;
            }

            return result;
        }
        
        private static IRandomAccessTransactionData<T, T> DeepCopyImplJObj(IRandomAccessTransactionData<T, T> d)
        {
            var result = d.Clone();

            result.Ensure(d.Count);

            int index = 0;
            foreach (var v in d)
            {
                if (v is JsonObject jo)
                {
                    result[index] = (T)(object)jo.DeepClone();
                }

                index++;
            }

            return result;
        }
        
        private static IRandomAccessTransactionData<T, T> DeepCopyImplXml(IRandomAccessTransactionData<T, T> d)
        {
            var result = d.Clone();

            result.Ensure(d.Count);

            int index = 0;
            foreach (var v in d)
            {
                if (v is XElement ex)
                {
                    result[index] = (T)(object)new XElement(ex);
                }

                index++;
            }

            return result;
        }

        private static object AggregateFuncImpl(IRandomAccessData<T> d, IEnumerable<int> r, AggregateType t)
        {
            if (d.Count == 0)
            {
                return null;
            }
            
            if (r.Any() == false)
            {
                return null;
            }

            var sumFunc = DataItemFeatureSetup<T>.SumFuncRepository;
            
            switch (t)
            {
                case AggregateType.Count:
                {
                    return r.Count();
                }
                case AggregateType.First:
                {
                    return d[r.OrderBy(c => c).First()];
                }
                case AggregateType.Sum:
                {
                    if (sumFunc != null)
                    {  
                        var hasData = false;
                        T sum = default;
                                
                        foreach (int record in r)
                        {
                            checked
                            {
                                sum = sumFunc(sum, d[record]);
                            }

                            hasData = true;
                        }

                        if (hasData)
                        {
                            return sum;
                        }
                    }

                    return null;
                }
                case AggregateType.Mean:
                {
                    var fivFunc = DataItemFeatureSetup<T>.DivByIntFuncRepository;
                    if (sumFunc != null && fivFunc != null)
                    {
                        var hasData = false;

                        var temp = r.ToData();

                        T sum = default;
                        int len = temp.Count;

                        foreach (int record in temp)
                        {
                            var value = d[record];
                            
                            if (value is null)
                            {
                                continue;
                            }
                            
                            checked
                            {
                                sum = sumFunc(sum, value);
                            }

                            hasData = true;
                        }

                        if (hasData)
                        {
                            return fivFunc(sum, len);
                        }
                    }

                    return null;
                }
                case AggregateType.Max:
                {
                    return r.Select(c => d[c]).Max();
                }
                case AggregateType.Min:
                {
                    return r.Select(c => d[c]).Min();
                }
                case AggregateType.Var:
                case AggregateType.StDev:
                {
                    int count = 0;
                    double var = 0.0f;
                    double prec = 0.0f;
                    double dsum = 0.0f;
                    double sqrsum = 0.0f;

                    var doubleType = typeof(System.Double);

                    foreach (int record in r)
                    {
                        var value = d[record];

                        if (value is null)
                        {
                            continue;
                        }
                        
                        var val = (double)Convert.ChangeType(value, doubleType);

                        dsum += val;
                        sqrsum += val * val;
                        count++;
                    }

                    if (count > 1)
                    {
                        var = ((double)count * sqrsum - (dsum * dsum));
                        prec = var / (dsum * dsum);

                        if ((prec < 1e-15) || (var < 0))
                        {
                            var = 0;
                        }
                        else
                        {
                            var = var / (count * (count - 1));
                        }

                        if (t == AggregateType.StDev)
                        {
                            return Math.Sqrt(var);
                        }

                        return var;
                    }

                    return null;
                }
                case AggregateType.None:
                {
                    return null;
                }
            }

            return null;
        }

        protected virtual void RejectStorageChange(IRandomAccessTransactionData<T, T> data, IRandomAccessTransactionData<T, T>.DataItemChange dataItemChange)
        {
            if (dataItemChange.IsNull == 1)
            {
                var defaultNullValue = m_defaultNullValue;

                data[dataItemChange.RowHandle] = defaultNullValue;
            }
            else
            {
                var value = dataItemChange.Value;

                data[dataItemChange.RowHandle] = value;
            }
        }

        public virtual void Clear(ICoreDataTableColumn column)
        {
            Storage.Clear();
        }

        public void SetAllNull(ICoreDataTableColumn column)
        {
            var storageCount = Storage.Count;

            for (int i = 0; i < storageCount; i++)
            {
                Storage[i] = m_defaultNullValue;
            }
        }

        public object GetDefaultValue(ICoreDataTableColumn column)
        {
            return m_defaultBoxed;
        }

        public virtual void Dispose(ICoreDataTableColumn column)
        {
            if (Storage is IDisposable d)
            {
                d.Dispose();
            }
        }

        public IDataItem Copy(CoreDataTable table, ICoreDataTableColumn column)
        {
            var dataItem = (DataItem<T>) MemberwiseClone();

            dataItem.m_table = table;

            dataItem.Storage = (IRandomAccessTransactionData<T, T>)CopyFunc(this.Storage);

            return dataItem;
        }

        public IDataItem Clone(CoreDataTable table, ICoreDataTableColumn column)
        {
            var dataItem = (DataItem<T>) MemberwiseClone();
            
            dataItem.Storage = CloneFunc(this.Storage);
            
            dataItem.m_table = table;
            
            return dataItem;
        }
        
        protected bool ShouldLogChange(int rowHandle)
        {
            return (m_table.TrackChanges ||
                    m_table.GetIsInTransaction() ||
                    (Storage.HasAnyChangeLogged(rowHandle)) == false);
        }

        public void AddNew(int rowHandle, [NotNull] T value, ICoreDataTableColumn column)
        {
            var val = ValueCheck != null
                ? ValueCheck(this.m_table, new ColumnHandle(column.ColumnHandle), value, rowHandle, false)
                : value;

            if (rowHandle >= Storage.Count)
            {
                Storage.Add(val);
            }
            else
            {
                Storage[rowHandle] = val;
            }

            UpdateCellAge(rowHandle, column);
        }

        public void AddNewNull(int rowHandle, ICoreDataTableColumn column)
        {
            if (rowHandle >= Storage.Count)
            {
                Storage.Add(m_defaultNullValue);
            }
            else
            {
                Storage[rowHandle] = m_defaultNullValue;
            }

            Storage.UpdateCellAge(rowHandle);
        }
        
        public bool SetValue(int rowHandle, T value, int? tranId, ICoreDataTableColumn column)
        {
            Storage.Ensure(m_table.StateInfo.RowStorageCount);
            
            var prevValue = Storage[rowHandle];

            var isNullPrev = IsNull(rowHandle, column);

            //pre check value already happened.
            var newVal = value;

            var changed = isNullPrev || !EqualValues(newVal, prevValue);

            if (changed == false)
            {
                return false;
            }
            
            if (ShouldLogChange(rowHandle))
            {
                Storage.LogChange(rowHandle, isNullPrev, ref prevValue, ref newVal, tranId);
            }

            Storage[rowHandle] = newVal;

            Storage.UpdateCellAge(rowHandle);

            return true;
        }

        private bool EqualValues(T value, T prevValue)
        {
            if (EqualsFunc != null)
            {
                return EqualsFunc(StorageItemType, ItemTypeModifier, value, prevValue);
            }

            return Equals(prevValue, value);
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
                Storage.Ensure(rowCount, m_defaultNullValue);
            }
        }

        public IEnumerable<int> Filter(object value, ICoreDataTableColumn column) 
        {
            if (value is null)
            {
                yield break;
            }
            
            var tv = value is T tv1 ? tv1 : ConvertBoxed(value, column);

            if (FilterFunc != null)
            {
                var rows = FilterFunc(Storage, tv, EqualsFunc);
                
                foreach (var i in rows)
                {
                    yield return i;
                }
                
                yield break;
            }
           
            var storageCount = Storage.Count;

            for (var index = 0; index < storageCount; index++)
            {
                var val = Storage[index];

                if (val is null)
                {
                    continue;
                }

                if (EqualsFunc(StorageItemType, ItemTypeModifier, tv, val))
                {
                    yield return index;
                }
            }
        }

        public object GetAggregateValue(IEnumerable<int> handles, AggregateType type, ICoreDataTableColumn column)
        {
            return AggregateFunc?.Invoke(Storage, handles, type);
        }

        public void AcceptChanges(int rowHandle, ICoreDataTableColumn column)
        {
            Storage.AcceptChanges(rowHandle);
        }

        public void RejectChanges(int rowHandle, int? changesCount, ICoreDataTableColumn column)
        {
            Storage.RejectChanges(rowHandle, changesCount, RejectStorageChange);
        }

        void IDataItem.AddNew(int rowHandle, object value, ICoreDataTableColumn column)
        {
            if (value == null)
            {
                AddNewNull(rowHandle, column);
            }
            else
            {
                if (value is T tv)
                {
                    AddNew(rowHandle, tv, column);   
                }
                else
                {
                    AddNew(rowHandle, ConvertBoxed(value, column), column);   
                }
            }
        }

        bool IDataItem.SetValue(int rowHandle, object value, int? tranId, ICoreDataTableColumn column)
        {
            if (IsNullValue(value, column))
            {
                return SetNull(rowHandle, true, tranId, column);
            }
            
            if(value is T tv)
            {
                return SetValue(rowHandle, tv, tranId, column);
            }

            return SetValue(rowHandle, ConvertBoxed(value, column), tranId, column);
        }

        IEnumerable IDataItem.GetStorage(ICoreDataTableColumn column) => Storage;

        public object GetData(int rowIndex, ICoreDataTableColumn column)
        {
            if (IsNull(rowIndex, column))
            {
                return m_defaultBoxed;
            }
            
            var value = GetLatestChangedValue(rowIndex);

            if (IsArray)
            {
                if (value is Array array)
                {
                    return array.Clone();
                }

                return m_defaultBoxed;
            }
            
            return value;
        }

        protected virtual T GetLatestChangedValue(int rowIndex)
        {
            return Storage[rowIndex];
        }

        public object GetRawData(int rowIndex, ICoreDataTableColumn column)
        {
            if (IsNull(rowIndex, column))
            {
                return m_defaultBoxed;
            }

            return GetLatestChangedValue(rowIndex);
        }

        public object TryGetData(int rowIndex, ICoreDataTableColumn column)
        {
            if (IsNull(rowIndex, column))
            {
                return m_defaultBoxed;
            }

            return GetLatestChangedValue(rowIndex);
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
            return Storage.RollbackRowTransaction(rowHandle, tranId, RejectStorageChange);
        }

        public bool IsChangedInTransaction(int rowHandle, int tranId, ICoreDataTableColumn column)
        {
            return Storage.IsChangedInTransaction(rowHandle, tranId);
        }

        public object GetTransactionOriginalValue(int rowHandle, int tranId, ICoreDataTableColumn column)
        {
            return Storage.GetTransactionOriginalValue(rowHandle, tranId);
        }
     
        public T GetDataTyped(int rowIndex, ICoreDataTableColumn column)
        {
            if (IsNull(rowIndex, column))
            {
                return m_defaultNullValue;
            }

            var value = GetLatestChangedValue(rowIndex);

            if (IsArray)
            {
                if (value is Array array)
                {
                    return (T)array.Clone();
                }

                return m_defaultNullValue;
            }

            return value;
        }

        public object CheckValueIsCompatibleType(object value, ICoreDataTableColumn column)
        {
            if (value is T tv)
            {
                return tv;
            }

            return ConvertBoxed(value, column);
        }

        private T ConvertBoxed(object value, ICoreDataTableColumn column)
        {
            try
            {
                if (IsArray)
                {
                    if (value is Array ac)
                    {
                        return (T)(object)ac;
                    }
                    
                    if (value is IList rc)
                    {
                        var instance = Array.CreateInstance(m_dataType, rc.Count);

                        rc.CopyTo(instance, 0);

                        return (T)(object)instance;
                    }
                }
                
                var converted = Brudixy.TypeConvertor.ConvertValue<T>(value, column.ColumnName, column.TableName, column.Type, column.TypeModifier, "DataItem set");

                return converted;
            }
            catch (InvalidCastException)
            {
                throw new InvalidCastException($"Cannot cast '{value.GetType()}' to the '{typeof(T)}'.");
            }
            catch (FormatException)
            {
                throw new InvalidCastException($"Cannot cast '{value.GetType()}' to the '{typeof(T)}'.");
            }
        }

        public void SilentlySetValue(int rowHandle, object value, ICoreDataTableColumn column)
        {
            Storage.Ensure(m_table.StateInfo.RowStorageCount);

            var isNull = value == m_defaultBoxed;
            
            if (isNull)
            {
                Storage[rowHandle] = m_defaultNullValue;
            }
            else
            {
                if(value is T tv)
                {
                    var newVal = ValueCheck != null ? ValueCheck(m_table, new ColumnHandle(column.ColumnHandle), tv, rowHandle, false) : tv;
                    
                    Storage[rowHandle] = newVal;
                }
                else 
                {
                    var converted = ConvertBoxed(value, column);
                    
                    Storage[rowHandle] = ValueCheck != null ? ValueCheck(m_table, new ColumnHandle(column.ColumnHandle), converted , rowHandle, false) : converted;
                }
            }
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
                var latestChange = GetLatestChangedValue(rowHandle);
                
                Storage.LogChange(rowHandle, prevNull, ref latestChange, ref m_defaultNullValue, tranId);
            }

            if (isNull)
            {
                var defaultNullValue = m_defaultNullValue;

                Storage[rowHandle] = defaultNullValue;
            }

            Storage.UpdateCellAge(rowHandle);

            return true;
        }

        public bool IsNull(int rowHandle, ICoreDataTableColumn column)
        {
            if (rowHandle >= Storage.Count)
            {
                return true;
            }
            
            return TypeConvertor.CanBeTreatedAsNull<T>(this.Storage[rowHandle]);
        }

        public object GetOriginalValue(int rowHandle, ICoreDataTableColumn column)
        {
            return GetOriginalTypedValue(rowHandle, column);
        }

        public uint GetAge(int rowHandle, ICoreDataTableColumn column)
        {
            return Storage.GetAge(rowHandle); 
        }

        public T GetOriginalTypedValue(int rowHandle, ICoreDataTableColumn column)
        {
            if (Storage.TryGetFirstLoggedValue(rowHandle, out var originalTypedValue))
            {
                return originalTypedValue;
            }

            return Storage[rowHandle];
        }
        
             
        protected void RejectStorageChange(IRandomAccessTransactionData<T, T>.DataItemChange dataItemChange)
        {
            if (dataItemChange.IsNull == 1)
            {
                var defaultNullValue = m_defaultNullValue;

                Storage[dataItemChange.RowHandle] = (T)defaultNullValue;
            }
            else
            {
                var value = dataItemChange.Value;

                Storage[dataItemChange.RowHandle] = (T)value;
            }
        }

        public void UpdateMax(object value, ICoreDataTableColumn column)
        {
            if (UpdateAutoIncrementFunc != null && value is not null)
            {
                if(value is T tv)
                {
                    m_lastAutomaticValue = UpdateAutoIncrementFunc(m_dataType, m_lastAutomaticValue, tv);
                }
                else 
                {
                    m_lastAutomaticValue = UpdateAutoIncrementFunc(m_dataType, m_lastAutomaticValue, ConvertBoxed(value, column));
                }
            }
        }

        public void UpdateMax(T currentValue)
        {
            m_lastAutomaticValue = currentValue;
        }

        public object GetLastautomaticValue(ICoreDataTableColumn column) => m_lastAutomaticValue;

        public object GetAutomaticValue(ICoreDataTableColumn column)
        {
            if (GetAutomaticValueFunc != null)
            {
                return m_lastAutomaticValue = GetAutomaticValueFunc(column, m_dataType,  m_lastAutomaticValue);
            }

            return column.DefaultValue ?? m_defaultBoxed;
        }

        public void RejectAllChanges(IReadOnlyDictionary<int, int> changesCount, ICoreDataTableColumn column)
        {
            Storage.RejectAllChanges(changesCount, RejectStorageChange);
        }

        public void AcceptAllChanges(ICoreDataTableColumn column)
        {
            Storage.AcceptAllChanges();
        }

        public T GetAutomaticValueTyped(ICoreDataTableColumn column)
        {
            if (GetAutomaticValueFunc != null)
            {
                return m_lastAutomaticValue = GetAutomaticValueFunc(column, m_dataType, m_lastAutomaticValue);
            }

            if (column.DefaultValue != null)
            {
                return Tool.ConvertBoxed<T>(column.DefaultValue);
            }

            return m_defaultNullValue;
        }

        public void GetValidValue(ref object value, int rowHandle, ICoreDataTableColumn column)
        {
            if (value is not null)
            {
                if (value is T tv)
                {
                    if (ValueCheck != null)
                    {
                        value = ValueCheck(m_table, new ColumnHandle(column.ColumnHandle), tv, rowHandle, true);
                    }
                }
                else
                {
                    var typedValue = ConvertBoxed(value, column);

                    if (ValueCheck != null)
                    {
                        value = ValueCheck(m_table, new ColumnHandle(column.ColumnHandle), typedValue, rowHandle, true);
                    }
                    else
                    {
                        value = typedValue;
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
            return TypeConvertor.CanBeTreatedAsNullBoxed(StorageItemType, ItemTypeModifier, value);
        }

        public void GetValidValue(ref T value, int rowHandle, ICoreDataTableColumn column)
        {
            if (ValueCheck != null)
            {
                value = ValueCheck(m_table, new ColumnHandle(column.ColumnHandle), value, rowHandle, true);
            }
        }

        public IComparable CalcMinMax(bool calcMax, ICoreDataTableColumn column, IEnumerable<int> rows = null)
        {
            var aggregateType = calcMax ? AggregateType.Max : AggregateType.Min;
            
            return (IComparable)GetAggregateValue(rows ?? Enumerable.Range(0, Storage.Count), aggregateType, column);
        }
    }
}
