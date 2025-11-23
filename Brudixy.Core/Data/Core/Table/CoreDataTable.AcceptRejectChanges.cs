using System;
using System.Linq;
using System.Threading;
using Brudixy.Exceptions;
using Brudixy.Interfaces;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        private ulong m_baseAge = 0;
        
        public void AcceptChanges()
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot accept changes for '{Name}' table because  is readonly.");
            }
            
            if (GetIsInTransaction())
            {
                throw new InvalidOperationException(
                    $"Cannot accept changes for '{Name}' table because it is in transaction.");
            }
            
            OnBeforeAcceptChanges();

            AcceptChangesComposite();
            
            var childRelationsMap = ChildRelationsMap;
            
            if (childRelationsMap != null && GetEnforceConstraints())
            {
                foreach (var rel in childRelationsMap.Values)
                {
                    if (rel.ChildKeyConstraint is { AcceptRejectRule: AcceptRejectRule.Cascade })
                    {
                        if (ReferenceEquals(rel.ChildKeyConstraint.ChildTable, this) == false)
                        {
                            rel.ChildKeyConstraint.ChildTable.AcceptChanges();
                        }
                    }
                }
            }
            
            foreach (var dataItem in DataColumnInfo.Columns)
            {
                dataItem.DataStorageLink.AcceptAllChanges(dataItem);
            }

            StateInfo.AcceptChangesAll(this);

            Interlocked.Increment(ref m_dataAge);

            Interlocked.Exchange(ref m_baseAge, m_dataAge);
        }
        
        public void RejectChanges()
        {
            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot reject changes for '{Name}' table because  is readonly.");
            }
            
            if (GetIsInTransaction())
            {
                throw new InvalidOperationException(
                    $"Cannot reject changes for '{Name}' table because it is in transaction.");
            }

            OnBeforeRejectChanges();

            RejectChangesComposite();
            
            var childRelationsMap = ChildRelationsMap;
            
            if (childRelationsMap != null && GetEnforceConstraints())
            {
                foreach (var rel in childRelationsMap.Values)
                {
                    if (rel.ChildKeyConstraint is { AcceptRejectRule: AcceptRejectRule.Cascade })
                    {
                        if (ReferenceEquals(rel.ChildKeyConstraint.ChildTable, this) == false)
                        {
                            rel.ChildKeyConstraint.ChildTable.RejectChanges();
                        }
                    }
                }
            }
            
            foreach (var dataColumn in DataColumnInfo.Columns)
            {
                dataColumn.DataStorageLink.RejectAllChanges(null, dataColumn);
            }

            StateInfo.RejectChanges(this);
            
            ClearLoggedChanges();

            Interlocked.Increment(ref m_dataAge);

            Interlocked.Exchange(ref m_baseAge, m_dataAge);
        }

        public bool CanAcceptChanges => (GetIsInTransaction() || StateInfo.GetRowsInTransaction().Any()) == false;
        public bool CanRejectChanges => CanAcceptChanges;
    }
}