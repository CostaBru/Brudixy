using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Brudixy.Exceptions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    [DebuggerDisplay("{ColumnName} {Type} of {TableName}")]
    public class CoreDataColumn : ICoreDataTableColumn
    {
        internal int ColumnHandle => ColumnObj.ColumnHandle;

        internal CoreDataTable DataTable;

        public readonly string TableName;

        public int Ordinal => ColumnHandle;

        public CoreDataColumn(CoreDataTable dataTable, CoreDataColumnObj dataColumnContainer)
        {
            ColumnObj = dataColumnContainer;

            DataTable = dataTable;

            TableName = DataTable.Name;
        }

        public string ColumnName
        {
            get
            {
                return ColumnObj.ColumnName;
            }
            set
            {
                var table = DataTable;

                if (table != null)
                {
                    table.ChangeColumnName(ColumnName, value);
                }
            }
        }

        public bool IsBuiltin
        {
            get
            {
                return ColumnObj.IsBuiltin;
            }
        }
        
        public bool IsServiceColumn
        {
            get
            {
                return ColumnObj.IsServiceColumn;
            }
            set
            {
                var table = DataTable;

                if (table != null)
                {
                    if (table.AreColumnsReadonly)
                    {
                        throw new ReadOnlyAccessViolationException(
                            $"Cannot change '{ColumnName}' column of '{table.Name}' table because columns are readonly."); 
                    }
                    
                    if (table.IsInitializing == false && table.IsReadOnly)
                    {
                        throw new ReadOnlyAccessViolationException(
                            $"Cannot setup '{ColumnName}' column of the '{table.Name}' table column service flag because it is readonly.");
                    }
                    
                    table.DataColumnInfo.SetServiceColumn(ColumnHandle, value);

                    return;
                }

                throw GetDetachedException();
            }
        }

        public bool AllowNull => ColumnObj.AllowNull;
        
        internal CoreDataColumnObj ColumnObj;
        private IDataItem m_dataStorageLink;

        internal bool HasDataLink => m_dataStorageLink != null;

        public bool IsAutomaticValue
        {
            get
            {
                return ColumnObj.IsAutomaticValue;
            }
            set
            {
                var table = DataTable;

                if (table != null)
                {
                    if (IsAutomaticValue != value)
                    {
                        table.SetupColumnAutomaticValue(ColumnHandle, value);
                    }

                    return;
                }

                throw GetDetachedException();
            }
        }

        string ICoreTableReadOnlyColumn.TableName => this.TableName;

        [CanBeNull]
        public object DefaultValue
        {
            get
            {
                return ColumnObj.DefaultValue;
            }
            set
            {
                var table = DataTable;

                if (table != null)
                {
                    if (DefaultValue == null || DefaultValue.Equals(value) == false)
                    {
                        table.SetupColumnDefaultValue(ColumnHandle, value);
                    }

                    return;
                }

                throw GetDetachedException();
            }
        }

        public uint? MaxLength
        {
            get
            {
                return ColumnObj.MaxLength;
            }
            set
            {
                var table = DataTable;

                if (table != null)
                {
                    if (MaxLength != value)
                    {
                        table.SetupColumnMaxLen(ColumnHandle, value);
                    }

                    return;
                }

                throw GetDetachedException();
            }
        }

        public bool HasIndex
        {
            get
            {
                return ColumnObj.HasIndex;
            }
        }

        public TableStorageTypeModifier TypeModifier
        {
            get
            {
                return ColumnObj.TypeModifier;
            }
            set
            {
                var table = DataTable;

                if (table != null)
                {
                    if (TypeModifier != value)
                    {
                        table.ChangeColumnTypeModifier(this, value);
                    }

                    return;
                }

                throw GetDetachedException();
            }
        }

        public TableStorageType Type
        {
            get
            {
                return ColumnObj.Type;
            }
            set
            {
                var table = DataTable;

                if (table != null)
                {
                    if (Type != value)
                    {
                        table.ChangeColumnType(this, value);
                    }

                    return;
                }

                throw GetDetachedException();
            }
        }
        
        

        public bool IsDetached => DataTable == null;

        public bool IsUnique
        {
            get
            {
                return ColumnObj.IsUnique;
            }
            set
            {
                var table = DataTable;

                if (table != null)
                {
                    if (IsUnique != value)
                    {
                        table.ChangeColumnUnique(ColumnHandle, value);
                    }

                    return;
                }

                throw GetDetachedException();
            }
        }

        [CanBeNull]
        public T GetXProperty<T>(string xPropertyName)
        {
            return ColumnObj.GetXProperty<T>(xPropertyName);
        }
        
        public IReadOnlyCollection<KeyValuePair<string, object>> GetXProperties()
        {
            return ColumnObj.GetXProperties();
        }

        public bool HasXProperty(string xPropertyName)
        {
            return ColumnObj.HasXProperty(xPropertyName);
        }

        public void SetXProperty<T>(string propertyName, T value)
        {
            var table = DataTable;

            if (table != null)
            {
                table.SetColumnXProperty(ColumnHandle, propertyName, value);
                
                return;
            }

            throw GetDetachedException();
        }

        public IEnumerable<string> XProperties
        {
            get
            {
                return ColumnObj.XProperties;
            }
        }

        
        public Type DataType
        {
            get
            {
                return ColumnObj.DataType;
            }
        }

        int ICoreTableReadOnlyColumn.ColumnHandle => ColumnHandle;
        
        protected DataDetachedException GetDetachedException()
        {
            return new DataDetachedException($"Column '{ColumnName}' is detached from the '{TableName}' data table.");
        }

        public virtual CoreDataColumn Clone(CoreDataTable owner, bool withData)
        {
            var clone = (CoreDataColumn)this.MemberwiseClone();

            clone.ColumnObj = ColumnObj.Clone();

            clone.DataTable = owner;

            var dataStorageLink = m_dataStorageLink;
            if (dataStorageLink != null)
            {
                clone.DataStorageLink = withData ? dataStorageLink.Copy(owner, clone) : dataStorageLink.Clone(owner, clone);
            }

            return clone;
        }

        internal IDataItem DataStorageLink
        {
            get
            {
                if (m_dataStorageLink == null)
                {
                    var item = DataTable.CreateDataItem(this);

                    m_dataStorageLink = item;
                }

                return m_dataStorageLink;
            }
            set => m_dataStorageLink = value;
        }

        public void SetRemoved()
        {
            m_dataStorageLink?.Dispose(this);
            DataTable = null;
        }
    }
}