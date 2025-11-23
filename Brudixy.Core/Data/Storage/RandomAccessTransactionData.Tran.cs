using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class RandomAccessTransactionData<T, TChange>
    {
        protected TChange m_defaultNullValueChange;

        protected Map<int, uint> m_cellAges;
        protected Map<int, int> m_cellTran;

        public Map<int, uint> CellAges
        {
            get
            {
                if (m_cellAges != null)
                {
                    return m_cellAges;
                }
                else
                {
                    var cellAges = new Map<int, uint>();
                    cellAges.EnsureValues((m) => 0);
                    return m_cellAges = cellAges;
                }
            }
        }

        public bool HasAnyChangeLogged(int index)
        {
            return m_changes?.GetOrDefault(index)?.Any() ?? false;
        }

        public IEnumerable<IRandomAccessTransactionData<T, TChange>.DataItemChange> GetChanges(int index)
        {
            if (Changes.TryGetValue(index, out var changes) && changes.Count > 0)
            {
                return changes;
            }
            
            return Enumerable.Empty<IRandomAccessTransactionData<T, TChange>.DataItemChange>();
        }

        private Map<int, int> CellTran
        {
            get
            {
                if (m_cellTran != null)
                {
                    return m_cellTran;
                }
                else
                {
                    var cellTran = new Map<int, int>();
                    cellTran.EnsureValues((m) => 0);
                    return m_cellTran = cellTran;
                }
            }
        }

        protected Map<int, Data<(uint age, int tranId)>> EditChangesStack => m_editChangesStack ??= new ();
        protected Map<int, Data<IRandomAccessTransactionData<T, TChange>.DataItemChange>> Changes => m_changes ??= new ();

        private Map<int, Data<(uint age, int tranId)>> m_editChangesStack;
        
        private Map<int, Data<IRandomAccessTransactionData<T, TChange>.DataItemChange>> m_changes;
        
        public void AcceptChanges(int index)
        {
            if (m_changes != null)
            {
                if (m_changes.TryGetValue(index, out var changes))
                {
                    changes.Clear();

                    m_changes.Remove(index);
                }
            }
        }

        [CanBeNull]
        public IRandomAccessTransactionData<T, TChange>.DataItemChange RejectChanges(int rowHandle, int? changesCount, Action<IRandomAccessTransactionData<T, TChange>, IRandomAccessTransactionData<T, TChange>.DataItemChange> customReject)
        {
            var changes = m_changes;
            
            if (changes != null && changes.TryGetValue(rowHandle, out var rowChanges) && rowChanges.Count > 0)
            {
                var changeIndex = changesCount is null ? 0 : Math.Max(rowChanges.Count - changesCount.Value, 0);

                var dataItemChange = rowChanges[changeIndex];

                customReject(this, dataItemChange);

                ClearChanges(changeIndex, rowChanges);

                if (changeIndex == 0)
                {
                    changes.Remove(rowHandle);
                }

                return dataItemChange;
            }

            return null;
        }
        
        private static void ClearChanges(int changeIndex, Data<IRandomAccessTransactionData<T, TChange>.DataItemChange> rowChanges)
        {
            if (changeIndex == 0)
            {
                rowChanges.Clear();
            }
            else
            {
                for (int i = 0; i < rowChanges.Count - changeIndex; i++)
                {
                    rowChanges.RemoveLast();
                }
            }
        }
        
        private static void ClearChanges(int changeIndex, Data<(uint age, int tranId)> rowChanges)
        {
            if (changeIndex == 0)
            {
                rowChanges.Clear();
            }
            else
            {
                for (int i = 0; i < rowChanges.Count - changeIndex; i++)
                {
                    rowChanges.RemoveLast();
                }
            }
        }

        public void AcceptAllChanges()
        {
            if (m_changes == null)
            {
                return;
            }
            
            m_changes?.Dispose();

            m_changes = null;
        }

        public bool IsChanged(int index)
        {
            Data<IRandomAccessTransactionData<T, TChange>.DataItemChange> changes = null;
            if (m_changes?.TryGetValue(index, out changes) ?? false)
            {
                return changes?.Count > 0;
            }

            return false;
        }

        public void RejectAllChanges(IReadOnlyDictionary<int, int> rejectCount, Action<IRandomAccessTransactionData<T, TChange>, IRandomAccessTransactionData<T, TChange>.DataItemChange> customReject)
        {
            var changes = m_changes;
            
            if (changes != null)
            {
                if (rejectCount is null)
                {
                    m_changes = null;
                }

                var toRemove = new Data<int>();

                foreach (var kv in changes)
                {
                    if (kv.Value.Count == 0)
                    {
                        continue;
                    }
                    
                    var changeIndex = 0;

                    if (rejectCount != null && rejectCount.TryGetValue(kv.Key, out var count))
                    {
                        changeIndex = Math.Max(kv.Value.Count - count, 0);
                    }
                    
                    var dataItemChange = kv.Value[changeIndex];

                    customReject(this, dataItemChange);

                    ClearChanges(changeIndex, kv.Value);

                    if (changeIndex == 0)
                    {
                        toRemove.Add(kv.Key);
                    }
                }

                foreach (var rowHandle in toRemove)
                {
                    changes.Remove(rowHandle);
                }

                if (rejectCount is null)
                {
                    changes.Dispose();
                }
            }
        }

        public RandomAccessTransactionData<T, TChange> CopyCore()
        {
            var dataItem = (RandomAccessTransactionData<T, TChange>) MemberwiseClone();

            dataItem.m_editChangesStack = new (); 
            
            if (m_cellTran != null)
            {
                dataItem.m_cellTran = new (m_cellTran);
            }

            if (m_cellAges != null)
            {
                dataItem.m_cellAges = new (m_cellAges);
            }
            
            if (m_changes != null)
            {
                CopyChanges(dataItem);
            }

            return dataItem;
        }
        
        public RandomAccessTransactionData<T, TChange> CloneCore()
        {
            var dataItem = (RandomAccessTransactionData<T, TChange>) MemberwiseClone();

            dataItem.m_changes = null;
            dataItem.m_cellAges = null;
            dataItem.m_cellTran = null;
            dataItem.m_editChangesStack = null;
            
            return dataItem;
        }

        protected void CopyChanges(RandomAccessTransactionData<T, TChange> dataItem)
        {
            if (m_changes is not null)
            {
                dataItem.m_changes = new();

                foreach (var change in m_changes)
                {
                    var value = change.Value;

                    if (value != null)
                    {
                        var dataItemChanges = new Data<IRandomAccessTransactionData<T, TChange>.DataItemChange>(value.Count);

                        foreach (var itemChange in value)
                        {
                            dataItemChanges.Add(itemChange.Copy());
                        }
                        
                        dataItem.Changes[change.Key] = dataItemChanges;
                    }
                }
            }
        }

        public void Dispose()
        {
            if (Data is IDisposable d)
            {
                d.Dispose();
            }
            
            m_changes?.Dispose();
            m_changes = null;

            m_cellAges?.Dispose();
            m_editChangesStack?.Dispose();

            m_cellAges = null;
            m_editChangesStack = null;
        }

        public void Clear()
        {
            Changes?.Clear();
            Data.Clear();
            m_cellAges?.Clear();
            m_editChangesStack?.Clear();
        }

        public uint GetAge(int rowHandle)
        {
            return m_cellAges?[rowHandle] ?? 0;
        }

        public void UpdateCellAge(int rowHandle)
        {
            CellAges[rowHandle]++;
        }

        public void LogChange(int rowHandle, bool isNullPrev, ref TChange prevValue, ref TChange newValue, int? tranId)
        {
            if (tranId.HasValue)
            {
                if (CellTran[rowHandle] < tranId)
                {
                    CellTran[rowHandle] = tranId.Value;
                    
                    StartLoggingTransactionChanges(rowHandle, tranId.Value);
                }
                else
                {
                    //
                }
            }

            var dataItemChange = new IRandomAccessTransactionData<T, TChange>.DataItemChange
            {
                RowHandle = rowHandle,
                Value = prevValue,
                IsNull = (byte) (isNullPrev ? 1 : 0)
            };

            var changes = Changes.GetOrAdd(dataItemChange.RowHandle, ChangesValueFactory);
            
            changes.Add(dataItemChange);
        }

        private static Data<IRandomAccessTransactionData<T, TChange>.DataItemChange> ChangesValueFactory()
        {
            return new ();
        }

        public bool TryGetFirstLoggedValue(int rowHandle, out TChange originalTypedValue)
        {
            originalTypedValue = m_defaultNullValueChange;

            if (Changes.TryGetValue(rowHandle, out var changes) && changes.Count > 0)
            {
                originalTypedValue = changes[0].Value;

                return true;
            }

            return false;
        }
        
        public void StartLoggingTransactionChanges(int rowHandle, int tranId)
        {
            var age = GetAge(rowHandle);
            
            EditChangesStack.GetOrAdd(rowHandle, ValueFactory).Append((age, tranId));
        }

        private static Data<(uint age, int tranId)> ValueFactory()
        {
            return new Data<(uint age, int tranId)>();
        }

        public void StopLoggingTransactionChanges(int rowHandle)
        {
            if (EditChangesStack.TryGetValue(rowHandle, out var changesStack) && changesStack.Count > 0)
            {
                changesStack.Clear();

                EditChangesStack.Remove(rowHandle);
            }
        }

        public bool IsChangedInTransaction(int rowHandle, int tranId)
        {
            if (EditChangesStack.TryGetValue(rowHandle, out var changesStack) && changesStack.Count > 0)
            {
                var index = changesStack.BinarySearch(tranId, 0, changesStack.Count, (trId, change) => trId.CompareTo(change.tranId));

                if (index < 0)
                {
                    return false;
                }
                
                var currentAge = GetAge(rowHandle);

                var lastChange = changesStack[index];
                
                return currentAge - lastChange.age > 0;
            }

            return false;
        }

        public object GetTransactionOriginalValue(int rowHandle, int tranId)
        {
            if (EditChangesStack.TryGetValue(rowHandle, out var stackChanges) && stackChanges.Count > 0)
            {
                var index = stackChanges.BinarySearch(tranId, 0, stackChanges.Count, (trId, change) => trId.CompareTo(change.tranId));

                if (index < 0)
                {
                    return null;
                }
                
                var currentAge = GetAge(rowHandle);
                
                var lastChange = stackChanges[index];
                
                var changes = Changes;

                var changesCount = (int)(currentAge - lastChange.age);

                if (changes != null && changes.TryGetValue(rowHandle, out var rowChanges) && rowChanges.Count > 0)
                {
                    var changeIndex = Math.Max(rowChanges.Count - changesCount, 0);

                    var dataItemChange = rowChanges[changeIndex];
                    var isNull = dataItemChange.IsNull == 1;

                    if (isNull)
                    {
                        return null;
                    }

                    return dataItemChange.Value;
                }
            }

            return null;
        }

        public bool RollbackRowTransaction(int rowHandle, int tranId, Action<IRandomAccessTransactionData<T, TChange>, IRandomAccessTransactionData<T, TChange>.DataItemChange> customReject = null)
        {
            if (EditChangesStack.TryGetValue(rowHandle, out var stackChanges) && stackChanges.Count > 0)
            {
                var index = stackChanges.BinarySearch(tranId, 0, stackChanges.Count, (trId, change) => trId.CompareTo(change.tranId));

                if (index < 0)
                {
                    index = ~index;
                }

                if (index >= stackChanges.Count)
                {
                    return false;
                }
                
                var currentAge = GetAge(rowHandle);

                var lastChange = stackChanges[index];

                RejectChanges(rowHandle, (int)(currentAge - lastChange.age), customReject);

                m_cellAges[rowHandle] = lastChange.age;

                ClearChanges(index, stackChanges);

                return currentAge - lastChange.age > 0;
            }

            return false;
        }

        public virtual IRandomAccessTransactionData<T, TChange> Copy()
        {
            var transactionData = CopyCore();

            transactionData.m_data = new Data<T>(this.Data);
            
            return transactionData;
        }

        public virtual IRandomAccessTransactionData<T, TChange> Clone()
        {
            var clone = CloneCore();

            clone.m_data = new Data<T>();
            
            return clone;
        }
    }
}