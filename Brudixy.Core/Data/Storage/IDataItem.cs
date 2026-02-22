using System;
using System.Collections;
using System.Collections.Generic;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy
{
    public interface IDataItem
    {
        void AcceptChanges(int rowHandle, ICoreDataTableColumn column);

        void RejectChanges(int rowHandle, int? changesCount, ICoreDataTableColumn column);

        void AddNew(int rowHandle, object value, ICoreDataTableColumn column);

        bool SetValue(int rowHandle, object value, int? tranId, ICoreDataTableColumn column);

        bool SetNull(int rowHandle, bool isNull, int? tranId, ICoreDataTableColumn column);

        IDataItem Copy(CoreDataTable table, ICoreDataTableColumn column);
        
        IDataItem Clone(CoreDataTable table, ICoreDataTableColumn column);

        IEnumerable GetStorage(ICoreDataTableColumn column);

        object GetDefaultValue(ICoreDataTableColumn column);

        void Dispose(ICoreDataTableColumn column);

        object GetData(int rowIndex, ICoreDataTableColumn column);
        
        object GetRawData(int rowIndex,  ICoreDataTableColumn column);

        void SilentlySetValue(int rowHandle, object value,  ICoreDataTableColumn column);

        bool IsNull(int rowHandle, ICoreDataTableColumn column);

        object GetOriginalValue(int rowHandle, ICoreDataTableColumn column);

        uint GetAge(int rowHandle, ICoreDataTableColumn column);

        void UpdateMax(object cellVal, ICoreDataTableColumn column);

        object GetAutomaticValue(ICoreDataTableColumn column);

        void RejectAllChanges(IReadOnlyDictionary<int, int> changesCount, ICoreDataTableColumn column);

        void AcceptAllChanges(ICoreDataTableColumn column);

        bool IsCellChanged(int rowHandle,  ICoreDataTableColumn column);

        void CreateEmptyRows(int rowCount,  ICoreDataTableColumn column);

        IEnumerable<int> Filter(object value, ICoreDataTableColumn column);

        object GetAggregateValue(IEnumerable<int> handles, AggregateType type, ICoreDataTableColumn column);

        void Clear(ICoreDataTableColumn column);

        void SetAllNull(ICoreDataTableColumn column);

        IComparable CalcMinMax(bool calcMax, ICoreDataTableColumn column, IEnumerable<int> rows = null);

        object TryGetData(int rowIndex, ICoreDataTableColumn column);
        
        void StartLoggingTransactionChanges(int rowHandle, int tranId, ICoreDataTableColumn column);
        
        void StopLoggingTransactionChanges(int rowHandle, ICoreDataTableColumn column);
        
        bool RollbackRowTransaction(int rowHandle, int tranId, ICoreDataTableColumn column);
        
        bool IsChangedInTransaction(int rowHandle, int tranId, ICoreDataTableColumn column);
        
        object GetTransactionOriginalValue(int rowHandle, int tranId, ICoreDataTableColumn column);
        
        object CheckValueIsCompatibleType(object value, ICoreDataTableColumn column);

        void GetValidValue(ref object value, int rowHandle, ICoreDataTableColumn column);
        
        void UpdateCellAge(int rowHandle, ICoreDataTableColumn column);
        
        bool IsNullValue(object value, ICoreDataTableColumn column);

        void Init(TableStorageType type, 
            TableStorageTypeModifier modifier, 
            CoreDataTable table
        );

        object GetLastautomaticValue(ICoreDataTableColumn column);
    }
}
