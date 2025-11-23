using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using Brudixy.Converter;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using Brudixy.Storage;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    [DebuggerDisplay("Count {Storage.Count}, Storage.Type {Storage.GetType()}")]
    internal class StringDataItem : DataItemTranBase<string>, IDataItem, ITypedDataItem<string>
    {
        internal Func<CoreDataTable, ColumnHandle, string, int, bool, string> ValueCheck;
        
        [NotNull] public StringStorage Storage;

        public StringDataItem(CoreDataTable table, 
            TableStorageType storageType, 
            int initColumnHandle,
            object defaultNullValue, 
            Func<CoreDataTable, ColumnHandle, string, int, bool, string> valueCheck = null) 
            : base(table)
        {
            ValueCheck = valueCheck;
            Storage = new StringStorage(table.StateInfo.RowCapacity);

            m_defaultNullValue = null;
            m_defaultBoxed = m_defaultNullValue;
        }

        protected override void RejectStorageChange(bool isNull, DataItemChange dataItemChange)
        {
            if (isNull)
            {
                Storage[dataItemChange.RowHandle] = null;
            }
            else
            {
                Storage[dataItemChange.RowHandle] = dataItemChange.Value;
            }
        }

        public object GetAggregateValue(IEnumerable<int> handles, AggregateType type, ICoreDataTableColumn column)
        {
            throw new NotImplementedException();
        }

        public override void Clear(ICoreDataTableColumn column)
        {
            base.Clear(column);

            Storage.Clear();
        }

        public void SetAllNull(ICoreDataTableColumn column)
        {
            var storageCount = Storage.Count;
            Storage.Clear();
            Storage.Ensure(storageCount);
        }

        public override void Dispose(ICoreDataTableColumn column)
        {
            base.Dispose(column);
            Storage.Dispose();
        }

        public IDataItem Copy(CoreDataTable table, ICoreDataTableColumn column)
        {
            var dataItem = (StringDataItem)base.Copy(table);

            dataItem.Storage = new StringStorage(Storage);

            return dataItem;
        }

        public IDataItem Clone(CoreDataTable table, ICoreDataTableColumn column)
        {
            var dataItem = (StringDataItem)base.Clone(table);

            dataItem.Storage = new StringStorage(0);

            return dataItem;
        }

        public void AddNew(int rowHandle, [NotNull] string value, ICoreDataTableColumn column)
        {
            var valueCheck = ValueCheck != null ? ValueCheck(this.m_table, new ColumnHandle(column.ColumnHandle), value, rowHandle, false) : value;
            Storage.PlaceAt(rowHandle, valueCheck);
            UpdateCellAge(rowHandle, column);
        }

        public void AddNewNull(int rowHandle, ICoreDataTableColumn column)
        {
            Storage.PlaceAt(rowHandle, null);
            UpdateCellAge(rowHandle, column);
        }

        public bool SetValue(int rowHandle, string value, int? tranId, ICoreDataTableColumn column)
        {
            var prevValue = Storage.GetOrDefault(rowHandle);

            var isNullPrev = IsNull(rowHandle, column);
            
            var valueCheck = ValueCheck != null ? ValueCheck(this.m_table, new ColumnHandle(column.ColumnHandle), value, rowHandle, false) : value;

            var changed = isNullPrev || prevValue != valueCheck;

            if (changed == false)
            {
                return false;
            }

            if (ShouldLogChange(rowHandle))
            {
                LogChange(rowHandle, isNullPrev, ref prevValue, ref valueCheck, tranId);
            }

            Storage[rowHandle] = valueCheck;
            
            UpdateCellAge(rowHandle, column);

            return true;
        }

        public string NextAutoIncrementValueTyped(ICoreDataTableColumn column)
        {
            return string.Empty;
        }

        public void GetValidValue(ref string value, int rowHandle, ICoreDataTableColumn column)
        {
            if (value is not null)
            {
                if (ValueCheck != null)
                {
                    value = ValueCheck(m_table, new ColumnHandle(column.ColumnHandle), value, rowHandle, true);
                }
            }
        }

        public bool IsNull(int rowHandle, ICoreDataTableColumn column)
        {
            if (rowHandle >= Storage.Count)
            {
                return true;
            }
            
            return string.IsNullOrEmpty(Storage[rowHandle]);
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
            throw new NotImplementedException();
        }

        public IEnumerable<int> Filter<V>(V value, ICoreDataTableColumn column) where V : IComparable
        {
            return Storage.Filter(value);
        }

        public object GetAggregateValue(Data<int> handles, AggregateType type, ICoreDataTableColumn column)
        {
            return m_defaultBoxed;
        }

        public void AcceptChanges(int rowHandle, ICoreDataTableColumn column)
        {
            AcceptChanges(rowHandle);
        }

        public void RejectChanges(int rowHandle, int? changesCount, ICoreDataTableColumn column)
        {
            RejectChanges(rowHandle, changesCount);
        }

        void IDataItem.AddNew(int rowHandle, object value, ICoreDataTableColumn column)
        {
            if (value == null)
            {
                AddNewNull(rowHandle, column);
            }
            else
            {
                if(value is string tv)
                {
                    AddNew(rowHandle, tv, column);
                }
                else
                {
                    AddNew(rowHandle, ConvertValue(value, null, column), column);
                }
            }
        }

        bool IDataItem.SetValue(int rowHandle, object value, int? tranId, ICoreDataTableColumn column)
        {
            if (value == null)
            {
                return SetNull(rowHandle, true, tranId, column);
            }

            if(value is string b)
            {
                return SetValue(rowHandle, b, tranId, column);
            }
            else
            {
                return SetValue(rowHandle, ConvertValue(value, null, column), tranId, column);
            }
        }

        IEnumerable IDataItem.GetStorage(ICoreDataTableColumn column) => Storage;

        public object GetData(int rowHandle, ICoreDataTableColumn column)
        {
            if (IsNull(rowHandle, column))
            {
                return m_defaultBoxed;
            }

            return Storage[rowHandle];
        }

        public object GetRawData(int rowHandle, ICoreDataTableColumn column)
        {
            return GetData(rowHandle, column);
        }

        public object TryGetData(int rowHandle, ICoreDataTableColumn column)
        {
            if (IsNull(rowHandle, column))
            {
                return null;
            }

            return Storage[rowHandle];
        }

        public void StartLoggingTransactionChanges(int rowHandle, int tranId, ICoreDataTableColumn column)
        {
            StartLoggingTransactionChanges(rowHandle, tranId);
        }

        public void StopLoggingTransactionChanges(int rowHandle, ICoreDataTableColumn column)
        {
            StopLoggingTransactionChanges(rowHandle);
        }

        public bool RollbackRowTransaction(int rowHandle, int tranId, ICoreDataTableColumn column)
        {
            return RollbackRowTransaction(rowHandle, tranId);
        }

        public bool IsChangedInTransaction(int rowHandle, int tranId, ICoreDataTableColumn column)
        {
            return IsChangedInTransaction(rowHandle, tranId);
        }

        public object GetTransactionOriginalValue(int rowHandle, int tranId, ICoreDataTableColumn column)
        {
            return GetTransactionOriginalValue(rowHandle, tranId);
        }

        public object CheckValueIsCompatibleType(object value, ICoreDataTableColumn column)
        {
            if(value is string b)
            {
                return b;
            }
            {
                return ConvertValue(value, null, column);
            }
        }

        public void GetValidValue(ref object value, int rowHandle, ICoreDataTableColumn column)
        {
            if(value is not string && value is not null)
            {
                value = ConvertValue(value, null, column);
            }
        }

        public void UpdateCellAge(int rowHandle, ICoreDataTableColumn column)
        {
            UpdateCellAge(rowHandle);
        }

        public bool IsNullValue(object value, ICoreDataTableColumn column)
        {
            return value is null || value is "";
        }

        public void Init(TableStorageType type, TableStorageTypeModifer modifer, CoreDataTable table)
        {
            throw new NotImplementedException();
        }

        private string ConvertValue(object value, InvalidCastException invalidCast, ICoreDataTableColumn column)
        {
            try
            {
                var converted = Tool.ConvertBoxed<string>(value);

                if (Trace.Listeners.Count > 0)
                {
                    Trace.WriteLine(
                        $"Performance critical column type mismatching conversion occured on SilentlySet value. Please sync data types to avoid this. Table = '{m_table.Name}', ColumnName '{column.ColumnName}', ColumnType '{column.Type}' ConversionType = '{typeof(string)}'");
                }

                return converted;
            }
            catch (InvalidCastException ex)
            {
                var convertStringToObject =
                    CoreDataTable.ConvertStringToObject(TableStorageType.String, value.ToString());

                if (Trace.Listeners.Count > 0)
                {
                    Trace.WriteLine(
                        $"Performance critical column type mismatching conversion occured on SilentlySet value using string parsing. Please sync data types to avoid this. Table = '{m_table.Name}', ColumnName '{column.ColumnName}', ColumnType '{column.Type}' ConversionType = '{typeof(string)}'");
                }

                return (string)convertStringToObject;
            }
            catch (FormatException frm)
            {
                if (invalidCast == null)
                {
                    throw;
                }

                throw invalidCast;
            }
        }

        public string GetDataTyped(int rowHandle, ICoreDataTableColumn column)
        {
            return Storage[rowHandle] ?? string.Empty;
        }

        public void SilentlySetValue(int rowHandle, object value, ICoreDataTableColumn column)
        {
            if (Storage.Count == 0)
            {
                CreateEmptyRows(m_table.RowCount, column);
            }

            if (value is null || value is "")
            {
                Storage.PlaceAt(rowHandle, null);
            }
            else
            {
                if(value is string str)
                {
                    var valueCheck = ValueCheck != null ? ValueCheck(this.m_table, new ColumnHandle(column.ColumnHandle), str, rowHandle, false) : str;
                    
                    Storage.PlaceAt(rowHandle, valueCheck);
                }
                else
                {
                    var convertValue = ConvertValue(value, null, column);
                    
                    var valueCheck = ValueCheck != null ? ValueCheck(this.m_table, new ColumnHandle(column.ColumnHandle), convertValue, rowHandle, false) : convertValue;

                    Storage.PlaceAt(rowHandle, valueCheck);
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
                var prevValue = Storage[rowHandle] ?? string.Empty;

                string newNull = null; 
                
                LogChange(rowHandle, prevNull, ref prevValue, ref newNull, tranId);
            }

            if (isNull)
            {
                Storage.PlaceAt(rowHandle, null); 
            }
            else
            {
                Storage.PlaceAt(rowHandle, string.Empty); 
            }

            UpdateCellAge(rowHandle, column);

            return true;
        }

        public object GetOriginalValue(int rowHandle, ICoreDataTableColumn column)
        {
            return GetOriginalTypedValue(rowHandle, column);
        }

        public uint GetAge(int rowHandle, ICoreDataTableColumn column)
        {
            return GetAge(rowHandle);
        }

        public string GetOriginalTypedValue(int rowHandle, ICoreDataTableColumn column)
        {
            if (Changes != null)
            {
                if (TryGetFirstLoggedValue(rowHandle, out var originalTypedValue))
                {
                    return originalTypedValue;
                }
            }

            return Storage[rowHandle] ?? string.Empty;
        }

        public void UpdateMax(object currentValue, ICoreDataTableColumn column)
        {
        }

        public object NextAutoIncrementValue(ICoreDataTableColumn column)
        {
            return m_defaultBoxed;
        }

        public IComparable CalcMinMax(bool calcMax, ICoreDataTableColumn column, IEnumerable<int> rows = null)
        {
            return m_defaultNullValue;
        }
    }
}
