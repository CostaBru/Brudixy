using System;
using System.Collections.Generic;
using System.IO.Compression;
using Brudixy.Converter;
using Konsarpoo.Collections;

namespace Brudixy
{
    public interface IRandomAccessTransactionData<T, TChange> : IRandomAccessData<T>
    {
        void AcceptChanges(int index);

        DataItemChange RejectChanges(int rowHandle, int? changesCount,
            Action<IRandomAccessTransactionData<T, TChange>, DataItemChange> customReject);

        void AcceptAllChanges();
        bool IsChanged(int index);

        void RejectAllChanges(IReadOnlyDictionary<int, int> rejectCount,
            Action<IRandomAccessTransactionData<T, TChange>, DataItemChange> customReject);

        class DataItemChange
        {
            public TChange Value;

            public int RowHandle;

            public byte IsNull;

            public DataItemChange Copy()
            {
                TChange value;
                if (Tool.IsCloneableSupported<TChange>())
                {
                    value = (TChange)((ICloneable)Value)?.Clone();
                }
                else
                {
                    value = Value;
                }

                return new DataItemChange
                {
                    Value = value,
                    RowHandle = RowHandle,
                    IsNull = IsNull
                };
            }
        }

        void Dispose();
        new void Clear();
        uint GetAge(int rowHandle);
        void UpdateCellAge(int rowHandle);
        void LogChange(int rowHandle, bool isNullPrev, ref TChange prevValue, ref TChange newValue, int? tranId);
        bool TryGetFirstLoggedValue(int rowHandle, out TChange originalTypedValue);
        void StartLoggingTransactionChanges(int rowHandle, int tranId);
        void StopLoggingTransactionChanges(int rowHandle);
        bool IsChangedInTransaction(int rowHandle, int tranId);
        object GetTransactionOriginalValue(int rowHandle, int tranId);

        bool RollbackRowTransaction(int rowHandle, int tranId,
            Action<IRandomAccessTransactionData<T, TChange>, IRandomAccessTransactionData<T, TChange>.DataItemChange>
                customReject);

        new T this[int index] { get; set; }
        IRandomAccessTransactionData<T, TChange> Copy();
        IRandomAccessTransactionData<T, TChange> Clone();
        bool HasAnyChangeLogged(int index);
        IEnumerable<DataItemChange> GetChanges(int index);
    }

    public interface IFileData
    {
        void BeginWrite();
        void Flush();
        void EndWrite();
    }
}