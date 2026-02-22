using Brudixy.Interfaces;

namespace Brudixy
{
    internal interface ITypedDataItem<T>
    {
        
        T GetDataTyped(int rowIndex, ICoreDataTableColumn column);

        T GetOriginalTypedValue(int rowHandle, ICoreDataTableColumn column);

        bool IsNull(int rowHandle, ICoreDataTableColumn column);

        bool SetNull(int rowHandle, bool isNull, int? tranId, ICoreDataTableColumn column);

        bool SetValue(int rowHandle, T value, int? tranId, ICoreDataTableColumn column);

        T GetAutomaticValueTyped(ICoreDataTableColumn column);

        void GetValidValue(ref T value, int rowHandle, ICoreDataTableColumn column);
    }
}