using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Interfaces;

namespace Brudixy
{
    public partial class CoreDataTable : IDataColumnCollection<ICoreDataTableColumn>
    {
        public IDataColumnCollection<ICoreDataTableColumn> Columns => this;
        
        IEnumerator<ICoreDataTableColumn> IEnumerable<ICoreDataTableColumn>.GetEnumerator()
        {
            return this.GetColumns().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetColumns().GetEnumerator();
        }

        IDataColumnCollection<ICoreDataTableColumn> IDataColumnCollection<ICoreDataTableColumn>.Add(ICoreDataTableColumn item)
        {
            this.AddColumn(item);

            return this;
        }

        IDataColumnCollection<ICoreDataTableColumn> IDataColumnCollection<ICoreDataTableColumn>.AddRange(IEnumerable<ICoreDataTableColumn> items)
        {
            foreach (var item in items)
            {
                this.AddColumn(item);
            }
            
            return this;
        }

        void IDataColumnCollection<ICoreDataTableColumn>.Remove(ICoreDataTableColumn item)
        {
            this.RemoveColumn(item.ColumnName);
        }

        void IDataColumnCollection<ICoreDataTableColumn>.RemoveRange(IEnumerable<ICoreDataTableColumn> items)
        {
            foreach (var column in items.ToArray())
            {
                this.RemoveColumn(column.ColumnName);
            }
        }

        void IDataColumnCollection<ICoreDataTableColumn>.Clear()
        {
            var columns = this.GetColumns().ToArray();

            foreach (var column in columns)
            {
                this.RemoveColumn(column.ColumnHandle);
            }
        }
    }
}