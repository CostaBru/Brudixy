using System.Diagnostics;
using Brudixy.Expressions;
using Brudixy.Interfaces;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy
{
    [DebuggerDisplay("{ColumnName}, {Type}")]
    public class DataColumnContainer : CoreDataColumnContainer, IDataTableColumn
    {
        public DataColumnContainer()
        {
            ColumnObj = new DataColumnObj();
        }
        
        public DataColumnContainer(DataColumnObj obj) : base(obj)
        {
        }
        
        public DataColumnContainer([NotNull] ICoreTableReadOnlyColumn dataColumn) : base(dataColumn)
        {
            var bld = new DataColumnContainerBuilder();
            
            if (dataColumn is DataColumn dc)
            {
                bld.AllowNull = dc.AllowNull;
                bld.ColumnHandle = dc.ColumnHandle;
            }

            bld.TableName = dataColumn.TableName;
            bld.ColumnName = dataColumn.ColumnName;
            bld.IsAutomaticValue = dataColumn.IsAutomaticValue;
            bld.DefaultValue = dataColumn.DefaultValue;
            bld.MaxLength = dataColumn.MaxLength;
            bld.Type = dataColumn.Type;
            bld.TypeModifier = dataColumn.TypeModifier;

            if (dataColumn is IDataTableReadOnlyColumn rdc)
            {
                bld.Caption = rdc.Caption;
                bld.IsReadOnly = rdc.IsReadOnly;
                bld.Expression = rdc.Expression;
            }

            bld.IsUnique = dataColumn.IsUnique;
            bld.DataType = dataColumn.DataType;

            bld.InitExtProperties(dataColumn);
            
            ColumnObj = bld.ToImmutable();
        }
        
        public new DataColumnContainer Clone()
        {
            return (DataColumnContainer)base.CloneCore();
        }

        private DataColumnObj ColObj => (DataColumnObj)this.ColumnObj;

        public string Expression
        {
            get { return ColObj.Expression; }
            set { ColumnObj = ColObj.WithExpression(value); }
        }

        public bool IsReadOnly
        {
            get { return ColObj.IsReadOnly; }
            set { ColumnObj = ColObj.WithIsReadOnly(value); }
        }

        public string Caption
        {
            get { return ColObj.Caption; }
            set { ColumnObj = ColObj.WithCaption(value); }
        }
    }
}