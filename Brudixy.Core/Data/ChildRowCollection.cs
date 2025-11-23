using System;
using System.Collections;
using System.Collections.Generic;
using Brudixy.Converter;
using Brudixy.Exceptions;
using Brudixy.Interfaces;

namespace Brudixy
{
    public class ChildRelationRowCollection<T> : IChildRelationRowCollection<T> where T : ICoreDataRowAccessor
    {
        private readonly string m_relationName;
        private readonly WeakReference<CoreDataRow> m_reference;
        private readonly string m_tableName;
        private readonly int m_rowHandle;

        public ChildRelationRowCollection(CoreDataRow row, string relationName)
        {
            m_relationName = relationName;
            m_reference = new WeakReference<CoreDataRow>(row);
            m_tableName = row.GetTableName();
            m_rowHandle = row.RowHandleCore;
        }

        public void Add(T item)
        {
            if (m_reference.TryGetTarget(out var row))
            {
                row.AddChildRows(m_relationName, item.SingleToArray());
            }
            else
            {
                throw new DataDetachedException(
                    $"The ChildRelationRowCollection created for row {m_rowHandle} of {m_tableName} is detached.");
            }
        }

        public void AddRange(IEnumerable<T> items)
        {
            if (m_reference.TryGetTarget(out var row))
            {
                row.AddChildRows(m_relationName, items);
            }
            else
            {
                throw new DataDetachedException(
                    $"The ChildRelationRowCollection created for row {m_rowHandle} of {m_tableName} is detached.");
            }
        }

        public void Remove(T item)
        {
            if (m_reference.TryGetTarget(out var row))
            {
                row.RemoveChildRows(m_relationName, item.SingleToArray());
            }
            else
            {
                throw new DataDetachedException(
                    $"The ChildRelationRowCollection created for row {m_rowHandle} of {m_tableName} is detached.");
            }
        }

        public void RemoveRange(IEnumerable<T> items)
        {
            if (m_reference.TryGetTarget(out var row))
            {
                row.RemoveChildRows(m_relationName, items);
            }
            else
            {
                throw new DataDetachedException(
                    $"The ChildRelationRowCollection created for row {m_rowHandle} of {m_tableName} is detached.");
            }
        }

        public void Clear()
        {
            if (m_reference.TryGetTarget(out var row))
            {
                row.ClearChildRows(m_relationName);
            }
            else
            {
                throw new DataDetachedException(
                    $"The ChildRelationRowCollection created for row {m_rowHandle} of {m_tableName} is detached.");
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            if (m_reference.TryGetTarget(out var row))
            {
                return row.GetChildRows<T>(m_relationName).GetEnumerator();
            }
            else
            {
                throw new DataDetachedException(
                    $"The ChildRelationRowCollection created for row {m_rowHandle} of {m_tableName} is detached.");
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}