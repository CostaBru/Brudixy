using System.Collections.Immutable;
using System.Diagnostics;
using Brudixy.Interfaces;

namespace Brudixy
{
    [DebuggerDisplay("{ColumnName}, {Type}")]
    public class DataColumnObj : CoreDataColumnObj
    {
        public DataColumnObj()
        {
        }

        public DataColumnObj(
            string caption,
            bool isReadOnly,
            string expression,
            string tableName,
            object defaultValue,
            string columnName,
            bool allowNull,
            bool isAutomaticValue,
            uint? maxLength,
            TableStorageType type,
            TableStorageTypeModifier typeModifier,
            Type dataType,
            bool isUnique,
            int columnHandle,
            bool isBuiltin,
            bool isServiceColumn,
            bool hasIndex,
            ImmutableDictionary<string, object> xPropStore) : 
            base(tableName, 
                defaultValue,
                columnName,
                allowNull,
                isAutomaticValue,
                maxLength,
                type, 
                typeModifier,
                dataType,
                isUnique, 
                columnHandle,
                isBuiltin,
                isServiceColumn,
                hasIndex, 
                xPropStore)
        {
            IsReadOnly = isReadOnly;
            Caption = caption;
            Expression = expression;
        }

        public string Caption { get; private set; }

        public DataColumnObj WithCaption(string value)
        {
            if (Caption == value)
            {
                return this;
            }
            
            var clone = (DataColumnObj)this.CloneCore();
            clone.Caption = value;
            return clone;
        }

        public bool IsReadOnly { get; private set; }

        public CoreDataColumnObj WithIsReadOnly(bool value)
        {
            if (IsReadOnly == value)
            {
                return this;
            }
            
            var clone = (DataColumnObj)this.CloneCore();
            clone.IsReadOnly = value;
            return clone;
        }

        public string Expression { get; private set; }

        public DataColumnType FixType => string.IsNullOrEmpty(Expression) ? DataColumnType.Common : DataColumnType.Expression;

        public CoreDataColumnObj WithExpression(string value)
        {
            if (Expression == value)
            {
                return this;
            }
            
            var clone = (DataColumnObj)this.CloneCore();
            clone.Expression = value;
            return clone;
        }

        public DataColumnObj WithCaptionExpressionReadOnly(string caption, string dataExpression, bool readOnlyValue)
        {
            var clone = (DataColumnObj)this.CloneCore();
            clone.Caption = caption;
            clone.Expression = dataExpression;
            clone.IsReadOnly = readOnlyValue;
            return clone;
        }
        
        public DataColumnObj WithExpressionReadOnly( string dataExpression, bool readOnlyValue)
        {
            var clone = (DataColumnObj)this.CloneCore();
            clone.Expression = dataExpression;
            clone.IsReadOnly = readOnlyValue;
            return clone;
        }
    }
}
