using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy
{
    public class StackTransactionInfo
    {
        private readonly Data<(int tranId, Set<int> items, Set<string> xprops, Data<IDataEditTransaction> dependantTransaction)> m_transactionStack = new ();
        private readonly Data<int> m_transactionIdStack = new ();

        public bool HasAny() => m_transactionIdStack.Count > 0;
        public bool Last() => m_transactionIdStack.Count == 1;

        public int CurrentTranId
        {
            get
            {
                if (m_transactionIdStack.Count == 0)
                {
                    return 0;
                }

                return m_transactionIdStack[m_transactionIdStack.Count - 1];
            }
        }

        public void AddDependantTransactions(IDataEditTransaction transactions)
        {
            var last = m_transactionStack.Last();

            if (last.dependantTransaction == null)
            {
                last.dependantTransaction = new Data<IDataEditTransaction>();
            }

            last.dependantTransaction.Add(transactions);

            m_transactionStack[^1] = last;
        }

        public void PushTransaction(int tranId)
        {
            m_transactionStack.Add((tranId, null, null, null));
                
            m_transactionIdStack.Add(tranId);
        }
        
        public IEnumerable<IDataEditTransaction> GetTransactions()
        {
            var currentTranId = CurrentTranId;

            var left = m_transactionStack
                .BinarySearchLeft(currentTranId, 0, m_transactionStack.Count, (tran, id) => tran.tranId.CompareTo(id));

            for (int i = left; i >= 0 && i < m_transactionStack.Count; i++)
            {
                var dependantTransaction = m_transactionStack[i].dependantTransaction;
                
                if (dependantTransaction != null)
                {
                    foreach (var item in dependantTransaction)
                    {
                        yield return item;
                    }
                }
            }
        }

        public IEnumerable<int> GetChangedItems()
        {
            var currentTranId = CurrentTranId;

            var left = m_transactionStack
                .BinarySearchLeft(currentTranId, 0, m_transactionStack.Count, (tran, id) => tran.tranId.CompareTo(id));

            for (int i = left; i >= 0 && i < m_transactionStack.Count; i++)
            {
                var items = m_transactionStack[i].items;
                
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        yield return item;
                    }
                }
            }
        }
        
        public IEnumerable<string> GetChangedXProps()
        {
            var currentTranId = CurrentTranId;

            var left = m_transactionStack
                .BinarySearchLeft(currentTranId, 0, m_transactionStack.Count, (tran, id) => tran.tranId.CompareTo(id));

            for (int i = left; i >= 0 && i < m_transactionStack.Count; i++)
            {
                var items = m_transactionStack[i].xprops;
                
                if (items != null)
                {
                    foreach (var item in items)
                    {
                        yield return item;
                    }
                }
            }
        }
        

        public void RollbackTransaction()
        {
            if (m_transactionIdStack.Count == 0)
            {
                m_transactionStack.Clear();
            }
            else
            {
                var index = m_transactionStack.BinarySearchLeft(CurrentTranId, 0, m_transactionStack.Count, (item, tranId) => item.tranId.CompareTo(tranId));

                if (index >= 0)
                {
                    for (int i = m_transactionStack.Count - 1; i >= index; i--)
                    {
                        m_transactionStack.RemoveLast();
                    }
                }
                    
                m_transactionIdStack.RemoveLast();
            }
        }
            
        public void PopTransaction()
        {
            m_transactionIdStack.RemoveLast();

            if (m_transactionIdStack.Count == 0)
            {
                m_transactionStack.Clear();
            }
        }

        public void Clear()
        {
            m_transactionIdStack.Clear();

            foreach (var item in m_transactionStack)
            {
                item.dependantTransaction?.Clear();
                item.items?.Clear();
            }
            
            m_transactionStack.Clear();
        }

        public bool HasStarted => m_transactionStack.Length > 0;

        public void AddChangedItem(int itemHandle)
        {
            ref var last = ref m_transactionStack.ValueByRef(m_transactionStack.Length - 1);

            if (last.items == null)
            {
                last.items = new Set<int>();
            }
            
            last.items.Add(itemHandle);
        }
        
        public void AddChangedXProp(string xProp)
        {
            ref var last = ref m_transactionStack.ValueByRef(m_transactionStack.Length - 1);

            if (last.xprops == null)
            {
                last.xprops = new Set<string>();
            }
            
            last.xprops.Add(xProp);
        }
    }
}