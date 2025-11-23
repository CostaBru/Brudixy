using System;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    [Serializable]
    public class RowStateInfo
    {
        public static RowStateInfo New()
        {
            return new RowStateInfo()
            {
                RowState = RowState.New,
                Age = 1,
                AnnAge = 1,
                EventLock = 0
            };
        }
        
        protected bool Equals(RowStateInfo other)
        {
            return RowState == other.RowState && Age == other.Age && AnnAge == other.AnnAge && EventLock == other.EventLock;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RowStateInfo)obj);
        }

        public RowStateInfo Copy()
        {
            return (RowStateInfo)this.MemberwiseClone();
        }

        public override int GetHashCode()
        {
            return HashCode.Combine((int)RowState, Age, AnnAge, EventLock);
        }

        public RowState RowState { get; set; }
        public uint Age { get; set; }
        public uint AnnAge { get; set; }
        public int EventLock { get; set; }
        
        public static bool operator ==(RowStateInfo p1, RowStateInfo p2)
        {
            if (ReferenceEquals(p1, p2)) return true;
            if (p1 is null || p2 is null) return false;
            return p1.Equals(p2);
        }

        // Overloading the != operator
        public static bool operator !=(RowStateInfo p1, RowStateInfo p2)
        {
            return !(p1 == p2);
        }
    }
    
    public class RowStateInfoDataItem 
    {
        internal CoreDataTable m_table;
        
        [NotNull] 
        public IRandomAccessTransactionData<RowStateInfo, RowStateInfo> Storage = new RandomAccessTransactionData<RowStateInfo, RowStateInfo>();
        
        public RowStateInfoDataItem(CoreDataTable table)
        {
            m_table = table;
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

        public RowStateInfoDataItem Copy(CoreDataTable table)
        {
            var dataItem = (RowStateInfoDataItem)this.MemberwiseClone();

            dataItem.Storage = dataItem.Storage.Copy();

            return dataItem;
        }
        
        public bool SetValue(int rowHandle, RowStateInfo newVal, int? tranId)
        {
            Ensure(Math.Max(m_table.RowCount, rowHandle + 1));
            
            var prevValue = Storage[rowHandle];

            var isNullPrev = Storage[rowHandle] == null;

            var changed = isNullPrev || newVal != prevValue;

            if (changed == false)
            {
                return false;
            }

            var nVal = newVal;
            
            if (ShouldLogChange(rowHandle))
            {
                var pValue = prevValue;
                Storage.LogChange(rowHandle, isNullPrev, ref pValue, ref nVal, tranId);
            }

            Storage[rowHandle] = nVal;

            Storage.UpdateCellAge(rowHandle);

            return true;
        }
        
        protected bool ShouldLogChange(int rowHandle)
        {
            return (m_table.TrackChanges ||
                    m_table.GetIsInTransaction() ||
                    (Storage.HasAnyChangeLogged(rowHandle)) == false);
        }

        public void Merge(RowStateInfoDataItem list)
        {
            this.Storage = list.Storage.Copy();
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
            Storage.RejectAllChanges(null, RejectStorageChange);
        }
        
        protected virtual void RejectStorageChange(IRandomAccessTransactionData<RowStateInfo, RowStateInfo> data, IRandomAccessTransactionData<RowStateInfo, RowStateInfo>.DataItemChange dataItemChange)
        {
            if (dataItemChange.IsNull == 1)
            {
                data[dataItemChange.RowHandle] = null;
            }
            else
            {
                var value = dataItemChange.Value;

                data[dataItemChange.RowHandle] = value;
            }
        }

        public bool SetState(int rowHandle, RowState rowState, int? tranId)
        {
            var stateInfo = Storage[rowHandle];

            if (stateInfo == null)
            {
                return false;
            }
            
            var rowStateInfo = stateInfo.Copy();

            rowStateInfo.RowState = rowState;

            return SetValue(rowHandle, rowStateInfo, tranId);
        }

        public void IncRowAge(int rowHandle)
        {
            var stateInfo = Storage[rowHandle];

            if (stateInfo == null)
            {
                return;
            }
            
            var rowStateInfo = stateInfo.Copy();

            rowStateInfo.Age++;

            SetValue(rowHandle, rowStateInfo, null);
        }

        public void LockRowEvents(int rowHandle)
        {
            var stateInfo = Storage[rowHandle];

            if (stateInfo == null)
            {
                return;
            }
            
            var rowStateInfo = stateInfo.Copy();

            rowStateInfo.EventLock++;

            SetValue(rowHandle, rowStateInfo, null);
        }

        public void UnlockRowEvents(int rowHandle)
        {
            var stateInfo = Storage[rowHandle];

            if (stateInfo == null)
            {
                return;
            }
            
            var rowStateInfo = stateInfo.Copy();

            rowStateInfo.EventLock--;

            if (rowStateInfo.EventLock < 0)
            {
                rowStateInfo.EventLock = 0;
            }

            SetValue(rowHandle, rowStateInfo, null);
        }

        public bool RowEventLocked(int rowHandle)
        {
           return Storage[rowHandle]?.EventLock > 0;
        }

        public void IncRowAnnotationAge(int rowHandle)
        {
            var stateInfo = Storage[rowHandle];
           
            if (stateInfo == null)
            {
                return;
            }
            
            var rowStateInfo = stateInfo.Copy();

            rowStateInfo.AnnAge++;

            SetValue(rowHandle, rowStateInfo, null);
        }

        public void IncAllRowAges(IRandomAccessData<int> rowHandles)
        {
            foreach (var rowHandle in rowHandles)
            {
                var stateInfo = Storage[rowHandle];
           
                if (stateInfo == null)
                {
                    return;
                }
            
                var rowStateInfo = stateInfo.Copy();

                rowStateInfo.Age++;

                Storage[rowHandle] = rowStateInfo;
            }
        }
    }
}