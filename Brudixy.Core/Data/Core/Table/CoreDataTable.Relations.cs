using System;
using Brudixy.Exceptions;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    public partial class CoreDataTable
    {
        [CanBeNull]
        public DataRelation GetChildRelation(string relationName)
        {
            return ChildRelationsMap?[relationName];
        }

        public DataRelation GetParentRelation(string relationName)
        {
            return ParentRelationsMap?[relationName];
        }
        
        public bool CheckParentForeignKeyConstraints()
        {
           return CheckParentForeignKeyConstraintsCore();
        }

        protected virtual bool CheckParentForeignKeyConstraintsCore()
        {
            if (ParentRelationsMap is not null)
            {
                foreach (var value in ParentRelationsMap.Values)
                {
                    var parentConstraintViolationRowHandles = new Map<int, string>();

                    value.ParentKeyConstraint?.CheckConstraint(parentConstraintViolationRowHandles);

                    if (parentConstraintViolationRowHandles.Count > 0)
                    {
                        throw new ParentConstraintViolationException(this, parentConstraintViolationRowHandles);
                    }
                }
            }

            return true;
        }


        internal void ChangeChildRelationName(DataRelation relation, string oldRelationName, string newRelationName)
        {
            if (ChildRelationsMap is not null)
            {
                if (ChildRelationsMap.TryGetValue(newRelationName, out var registeredRel) && ReferenceEquals(registeredRel, relation) == false)
                {
                    throw new InvalidOperationException(
                        $"Cannot change child relation name '{oldRelationName}' to '{newRelationName}' because is already exist in '{Name}' in table."); 
                }
                
                ChildRelationsMap[newRelationName] = ChildRelationsMap[oldRelationName];

                ChildRelationsMap.Remove(oldRelationName);
                
                if (m_columnChildRelations is not null)
                {
                    foreach (var column in relation.ChildColumns)
                    {
                        var columnChildRelation = m_columnChildRelations[column.ColumnHandle];
                        
                        columnChildRelation.Remove(oldRelationName);
                        columnChildRelation.Add(newRelationName);
                    }
                }
            }
        }
        
        internal void ChangeParentRelationName(DataRelation relation, string oldRelationName, string newRelationName)
        {
            if (ParentRelationsMap is not null)
            {
                if (ParentRelationsMap.TryGetValue(newRelationName, out var registeredRel) && ReferenceEquals(registeredRel, relation) == false)
                {
                    throw new InvalidOperationException(
                        $"Cannot change parent relation name '{oldRelationName}' to '{newRelationName}' because is already exist in '{Name}' in table."); 
                }
                
                ParentRelationsMap[newRelationName] = ParentRelationsMap[oldRelationName];

                ParentRelationsMap.Remove(oldRelationName);
                
                if (m_columnParentRelations is not null)
                {
                    foreach (var column in relation.ParentColumns)
                    {
                        var columnParentRelation = m_columnParentRelations[column.ColumnHandle];
                        
                        columnParentRelation.Remove(oldRelationName);
                        columnParentRelation.Add(newRelationName);
                    }
                }
            }
        }

        public void RemoveRelation([NotNull] DataRelation relation)
        {
            if (relation == null)
            {
                throw new ArgumentNullException(nameof(relation));
            }

            if (IsInitializing == false && IsReadOnly)
            {
                throw new ReadOnlyAccessViolationException(
                    $"Cannot remove '{relation.relationName}' relation from '{Name}' table because it is readonly.");
            }

            foreach (var column in relation.ChildColumns)
            {
                RegisterChildRelation(relation.relationName, column.ColumnHandle, register: false);
            }
            
            foreach (var column in relation.ParentColumns)
            {
                RegisterParentRelation(relation.relationName, column.ColumnHandle, register: false);
            }

            ParentRelationsMap?.Remove(relation.relationName);
            ChildRelationsMap?.Remove(relation.relationName);
        }

        internal void RegisterParentRelation(string relationName, int columnHandle, bool register = true)
        {
            if (m_columnParentRelations == null)
            {
                m_columnParentRelations = new ();
            }

            if (register)
            {
                m_columnParentRelations.GetOrAdd(columnHandle, CreateNewHashset).Add(relationName);
            }
            else
            {
                m_columnParentRelations.TryGetValue(columnHandle, out var relNames);

                if (relNames != null)
                {
                    relNames.Dispose();
                }
                
                m_columnParentRelations.Remove(columnHandle);
            }
        }
        
        internal void RegisterChildRelation(string relationName, int columnHandle, bool register = true)
        {
            if (m_columnChildRelations == null)
            {
                m_columnChildRelations = new ();
            }

            if (register)
            {
                m_columnChildRelations.GetOrAdd(columnHandle, CreateNewHashset).Add(relationName);
            }
            else
            {
                m_columnChildRelations.TryGetValue(columnHandle, out var relNames);

                if (relNames != null)
                {
                    relNames.Dispose();
                }
                
                m_columnChildRelations.Remove(columnHandle);
            }
        }

        private Set<string> CreateNewHashset()
        {
            return new Set<string>(StringComparer.OrdinalIgnoreCase);
        }
        
        private Set<int> CreateIntNewHashset()
        {
            return new Set<int>();
        }
    }
}