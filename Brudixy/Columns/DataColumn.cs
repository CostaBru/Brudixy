using System.Diagnostics;
using Brudixy.Expressions;
using Brudixy.Interfaces;

namespace Brudixy
{
    [DebuggerDisplay("{ColumnName} {Type} of {TableName}")]
    public class DataColumn : CoreDataColumn, IDataTableColumn
    {
        internal DataTable DataTable => (DataTable)base.DataTable;

        internal DataColumnObj ColObj => (DataColumnObj)this.ColumnObj;
        
        public DataColumnType FixType
        {
            get
            {
                return ColObj.FixType;
            }
        }

        public string Caption
        {
            get
            {
                return ColObj.Caption;
            }
            set
            {
                var table = DataTable;

                if (table != null)
                {
                    if (Caption != value)
                    {
                        table.SetColumnCaption(ColumnHandle, value);
                    }

                    return;
                }

                throw GetDetachedException();
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return ColObj.IsReadOnly;
            }
            set
            {
                var table = DataTable;

                if (table != null)
                {
                    if (IsReadOnly != value)
                    {
                        table.SetColumnReadOnly(ColumnHandle, value);
                    }

                    return;
                }

                throw GetDetachedException();
            }
        }

        public string Expression
        {
            get
            {
                return ColObj.Expression;
            }
            set
            {
                var table = DataTable;

                if (table != null)
                {
                    if (Expression != value)
                    {
                        table.ChangeColumnExpression(this, value);
                    }

                    return;
                }

                throw GetDetachedException();
            }
        }

        public DataColumn(CoreDataTable dataTable, CoreDataColumnObj dataColumnObj) : base(dataTable, dataColumnObj)
        {
        }

        string ICoreTableReadOnlyColumn.TableName => base.TableName;
        public override CoreDataColumn Clone(CoreDataTable owner, bool withData)
        {
            var column = (DataColumn)base.Clone(owner, withData);

            if (string.IsNullOrEmpty(this.Expression) == false)
            {
                column.ExpressionLink = new((IExpressionDataSource)owner, this.Expression);
            }
            
            return column;
        }

        internal DataExpression ExpressionLink { get; set; }
    }
}
