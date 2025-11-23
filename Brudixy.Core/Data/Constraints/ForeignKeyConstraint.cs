using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using Brudixy.Expressions;
using Konsarpoo.Collections;

namespace Brudixy.Constraints
{
    public partial class ForeignKeyConstraint
    {
        internal DataKey m_childKey;
        internal DataKey m_parentKey;
        internal string constraintName;
        
        protected Rule m_deleteRule = Rule_Default;
        protected Rule m_updateRule = Rule_Default;
        protected AcceptRejectRule m_acceptRejectRule = AcceptRejectRule_Default;
        
        private const Rule Rule_Default = Rule.Cascade;
        private const AcceptRejectRule AcceptRejectRule_Default = AcceptRejectRule.None;
        
        private string m_schemaName = string.Empty;

        public void Dispose()
        {
        }
        
        public ForeignKeyConstraint()
        {
        }

        public ForeignKeyConstraint(CoreDataTable parentTable, CoreDataTable childTable, CoreDataColumn parentColumn,
            CoreDataColumn childColumn)
            : this(null, parentTable, childTable, parentColumn, childColumn)
        {
        }

        public ForeignKeyConstraint(string constraintName,
            CoreDataTable parentTable,
            CoreDataTable childTable,
            CoreDataColumn parentColumn,
            CoreDataColumn childColumn)
        {
            var parentColumns = new Data<CoreDataColumn> { parentColumn };
            var childColumns = new Data<CoreDataColumn> { childColumn };

            Create(constraintName, parentTable, childTable, parentColumns, childColumns);
        }

        public ForeignKeyConstraint(CoreDataTable parentTable, CoreDataTable childTable,
            IReadOnlyList<CoreDataColumn> parentColumns, IReadOnlyList<CoreDataColumn> childColumns)
            : this(null, parentTable, childTable, parentColumns, childColumns)
        {
        }


        public ForeignKeyConstraint(string constraintName, CoreDataTable parentTable, CoreDataTable childTable,
            IReadOnlyList<CoreDataColumn> parentColumns, IReadOnlyList<CoreDataColumn> childColumns)
        {
            Create(constraintName, parentTable, childTable, parentColumns, childColumns);
        }

        internal DataKey ChildKey
        {
            get
            {
                CheckStateForProperty(nameof(ChildKey));
                return m_childKey;
            }
        }

        [ReadOnly(true)]
        public IEnumerable<CoreDataColumn> ChildColumns
        {
            get
            {
                CheckStateForProperty(nameof(ChildColumns));
                return m_childKey.ColumnsReference;
            }
        }

        [ReadOnly(true)]
        public CoreDataTable ChildTable
        {
            get
            {
                CheckStateForProperty(nameof(ChildTable));

                return m_childKey.Table;
            }
        }
        
        internal IEnumerable<CoreDataColumn> RelatedColumnsReference
        {
            get
            {
                CheckStateForProperty(nameof(RelatedColumnsReference));
                return m_parentKey.ColumnsReference;
            }
        }

        internal DataKey ParentKey
        {
            get
            {
                CheckStateForProperty(nameof(ParentKey));
                return m_parentKey;
            }
        }
     
        [ReadOnly(true)]
        public CoreDataTable RelatedTable
        {
            get
            {
                CheckStateForProperty(nameof(RelatedTable));
                return m_parentKey.Table;
            }
        }
     
        [DefaultValue(Rule_Default)]
        public Rule UpdateRule
        {
            get
            {
                CheckStateForProperty(nameof(UpdateRule));
                return m_updateRule;
            }
            set
            {
                switch (value)
                {
                    // @perfnote: Enum.IsDefined
                    case Rule.None:
                    case Rule.Cascade:
                    case Rule.SetNull:
                    case Rule.SetDefault:
                        m_updateRule = value;
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid UpdateRune :{value}");
                }
            }
        }

        [DefaultValue("")]
        public string ConstraintName
        {
            get { return constraintName; }
            set
            {
                value ??= string.Empty;

                if (string.IsNullOrEmpty(value) && ChildTable != null)
                {
                    throw new ArgumentException("Constraint should have a proper name.");
                }

                if (string.Compare(constraintName, value, StringComparison.OrdinalIgnoreCase) != 0)
                {
                    constraintName = value;
                }
                else
                {
                    if (string.CompareOrdinal(constraintName, value) == 0)
                    {
                        return;
                    }

                    constraintName = value;
                }
            }
        }
        
        [DefaultValue(AcceptRejectRule_Default)]
        public AcceptRejectRule AcceptRejectRule
        {
            get
            {
                CheckStateForProperty(nameof(AcceptRejectRule));
                return m_acceptRejectRule;
            }
            set
            {
                switch (value)
                {
                    case AcceptRejectRule.None:
                    case AcceptRejectRule.Cascade:
                        m_acceptRejectRule = value;
                        break;
                    default:
                        throw new ArgumentException("InvalidAcceptRejectRule");
                }
            }
        }

        internal ForeignKeyConstraint CloneDs(CoreDataTable destination)
        {
            CoreDataTable parentTable = destination.TryGetTable(RelatedTable.Name);

            if (parentTable == null)
            {
                throw new MissingMetadataException(
                    $"$Cannot clone {constraintName} ForeignKeyConstraint to the dataset {destination.TableName}. {RelatedTable.Name} parent table doesn't exist.");
            }

            CoreDataTable childTable  = destination.TryGetTable(ChildTable.Name);

            if (childTable == null)
            {
                throw new MissingMetadataException(
                    $"$Cannot clone {constraintName} ForeignKeyConstraint to the dataset {destination.TableName}. {ChildTable.Name} child table doesn't exist.");
            }

            int keys = m_childKey.Columns.Count;

            GetThisAndRefColumnsClone(keys, parentTable, childTable, out var parentColumns, out var childColumns);

            var clone =
                new ForeignKeyConstraint(ConstraintName, parentTable, childTable, parentColumns, childColumns)
                {
                    UpdateRule = UpdateRule,
                    DeleteRule = DeleteRule,
                    AcceptRejectRule = AcceptRejectRule
                };
            
            parentColumns.Dispose();
            childColumns.Dispose();

            return clone;
        }

        private void GetThisAndRefColumnsClone(int keys,
            CoreDataTable parentTable,
            CoreDataTable childTable,
            out Data<CoreDataColumn> parentColumns,
            out Data<CoreDataColumn> childColumns)
        {
            parentColumns = new Data<CoreDataColumn>(keys);
            childColumns = new Data<CoreDataColumn>(keys);

            parentColumns.Ensure(keys);
            childColumns.Ensure(keys);

            for (int i = 0; i < keys; i++)
            {
                var src = m_childKey.Table.GetColumn(m_childKey.Columns[i]);
                var tc = childTable.TryGetColumn(src.ColumnName);

                if (tc == null)
                    return;

                childColumns[i] = tc;

                src = m_parentKey.Table.GetColumn(m_parentKey.Columns[i]);
                var rc = parentTable.TryGetColumn(src.ColumnName);

                if (rc == null)
                    return;

                parentColumns[i] = rc;
            }
        }

        internal ForeignKeyConstraint Clone(CoreDataTable destination)
        {
            if (ReferenceEquals(ChildTable, RelatedTable) == false)
            {
                throw new InvalidOperationException(
                    $"Cannot clone {constraintName} ForeignKeyConstraint to the {destination.Name} table, because constraint build for different tables and should be cloned to the dataset.");
            }

            GetThisAndRefColumnsClone(m_childKey.Count, destination, destination, out var parentColumns, out var childColumns);

            var clone = (ForeignKeyConstraint)Activator.CreateInstance(GetType());

            clone.Create(ConstraintName, destination, destination, parentColumns, childColumns);

            clone.AcceptRejectRule = AcceptRejectRule;
            clone.DeleteRule = DeleteRule;
            clone.UpdateRule = UpdateRule;

            return clone;
        }

        public void Create(string constraintName,
            CoreDataTable parentTable,
            CoreDataTable childTable,
            IReadOnlyList<CoreDataColumn> parentColumns,
            IReadOnlyList<CoreDataColumn> childColumns)
        {
            if (parentColumns.Count == 0 || childColumns.Count == 0)
            {
                throw new InvalidOperationException(
                    $"{this.constraintName} constraint check error. Key length is zero");
            }

            if (parentColumns.Count != childColumns.Count)
            {
                throw new InvalidOperationException(
                    $"{this.constraintName} constraint check error. Parent {parentColumns.Count} key column count are not equal to child {childColumns.Count} count.");
            }

            for (int i = 0; i < parentColumns.Count; i++)
            {
                if (parentTable.IsReadOnlyColumn(parentTable.GetColumn(parentColumns[i].ColumnHandle)))
                {
                    throw new ReadOnlyAccessViolationException(
                        $"{this.constraintName} constraint check error. Parent {parentColumns[i]} column is readonly.");
                }

                if (childTable.IsReadOnlyColumn(childTable.GetColumn(childColumns[i].ColumnHandle)))
                {
                    throw new ReadOnlyAccessViolationException(
                        $"{this.constraintName} constraint check error. Child {parentColumns[i]} column is readonly.");
                }
            }

            m_parentKey = new DataKey(parentTable, parentColumns.Select(c => c.ColumnHandle).ToArray());
            m_childKey = new DataKey(childTable, childColumns.Select(c => c.ColumnHandle).ToArray());

            ConstraintName = constraintName;

            NonVirtualCheckState();
        }

        [DefaultValue(Rule_Default)]
        public Rule DeleteRule
        {
            get
            {
                CheckStateForProperty(nameof(DeleteRule));
                return m_deleteRule;
            }
            set
            {
                switch (value)
                {
                    case Rule.None:
                    case Rule.Cascade:
                    case Rule.SetNull:
                    case Rule.SetDefault:
                        m_deleteRule = value;
                        break;
                    default:
                        throw new InvalidOperationException($"Invalid DeleteRule {value}.");
                }
            }
        }

        public override bool Equals(object key)
        {
            if (key is not ForeignKeyConstraint keyConstraint)
            {
                return false;
            }

            // The ParentKey and ChildKey completely identify the ForeignKeyConstraint
            return constraintName == keyConstraint.constraintName && ParentKey.ColumnsEqual(keyConstraint.ParentKey) && ChildKey.ColumnsEqual(keyConstraint.ChildKey);
        }
        
        public override string ToString()
        {
            return ConstraintName;
        }

        protected void CheckStateForProperty(string name)
        {
            try
            {
                CheckState();
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Cannot access '{name}' property. {ex.Message}");
            }
        }
        
        private void NonVirtualCheckState()
        {
            var parentTable = m_parentKey.Table;
            var childTable = m_childKey.Table;

            if (parentTable == null || childTable == null)
            {
                throw new InvalidOperationException("Table references were GC collected.");
            }

            for (int i = 0; i < m_parentKey.Columns.Count && i < m_childKey.Columns.Count; i++)
            {
                var parentColType = m_parentKey.Table.DataColumnInfo.Columns[m_parentKey.Columns[i]].Type;
                var childColType = m_childKey.Table.DataColumnInfo.Columns[m_childKey.Columns[i]].Type;
                
                if (parentColType != childColType)
                {
                    var parentColName =  m_parentKey.Table.DataColumnInfo.Columns[m_parentKey.Columns[i]].ColumnName;
                    var childColName = m_childKey.Table.DataColumnInfo.Columns[m_childKey.Columns[i]].ColumnName;
                    
                    throw new InvalidOperationException(
                        $"FK {constraintName} constraint column type mismatch. Parent {parentTable.Name} column {parentColName} and type {parentColType} is not equal to child {childTable.Name} column {childColName} and type {childColType}");
                }
            }

            if (m_childKey.ColumnsEqual(m_parentKey))
            {
                var parentColumns = string.Join(",", m_parentKey.ColumnsReference.Select(c => c.ColumnName));
                var childColumns = string.Join(",", m_childKey.ColumnsReference.Select(c => c.ColumnName));

                throw new InvalidOperationException(
                    $"FK {constraintName} constraint column type mismatch. FK columns are identical: parent {parentTable.Name} columns {parentColumns} and child {childTable.Name} columns {childColumns}.");
            }
        }

        // If we're not in a DataSet relations collection, we need to verify on every property get that we're
        // still a good relation object. 
        internal void CheckState()
        {
            NonVirtualCheckState();
        }
    }
}