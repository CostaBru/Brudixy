using System.Globalization;
using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    public abstract class ExpressionNode : IDisposable
    {
        private IExpressionDataSource m_table;

        internal IFormatProvider FormatProvider => CultureInfo.InvariantCulture;

        public IExpressionDataSource table => m_table;

        protected ExpressionNode(IExpressionDataSource table) => m_table = table;

        protected void BindTable(IExpressionDataSource table) => m_table = table;

        internal abstract void Mount(IExpressionDataSource table, Data<string> columns);

        internal abstract object Eval(int? rowHandle = null,
            IReadOnlyDictionary<string, object> testValues = null,
            bool test = false);

        internal abstract object Eval(Data<int> recordNos);

        internal abstract bool IsConstant();

        internal abstract bool IsTableConstant();

        internal abstract bool HasLocalAggregate();

        internal abstract bool HasRemoteAggregate();

        internal abstract ExpressionNode Optimize();

        internal virtual bool DependsOn(string column) => false;

        internal static bool IsInteger(TableStorageType type)
        {
            if (type != TableStorageType.Int16 && type != TableStorageType.Int32 &&
                (type != TableStorageType.Int64 && type != TableStorageType.UInt16) &&
                (type != TableStorageType.UInt32 && type != TableStorageType.UInt64 && type != TableStorageType.SByte))
            {
                return type == TableStorageType.Byte;
            }
            return true;
        }
     

        internal static bool IsSigned(TableStorageType type)
        {
            if (type is not TableStorageType.Int16 and not TableStorageType.Int32 and not TableStorageType.Int64 and not TableStorageType.SByte)
            {
                return IsFloat(type);
            }
            return true;
        }

        internal static bool IsUnsigned(TableStorageType type)
        {
            if (type is not TableStorageType.UInt16 and not TableStorageType.UInt32 and not TableStorageType.UInt64)
            {
                return type == TableStorageType.Byte;
            }
            return true;
        }

        internal static bool IsNumeric((TableStorageType type, TableStorageTypeModifier modifier) type)
        {
            if (type.modifier != TableStorageTypeModifier.Simple)
            {
                return false;
            }
            
            if (!IsFloat(type.type))
            {
                return IsInteger(type.type);
            }
            return true;
        }

        internal static bool IsFloat(TableStorageType type)
        {
            if (type is not TableStorageType.Single and not TableStorageType.Double)
            {
                return type == TableStorageType.Decimal;
            }
            return true;
        }

        public virtual void Dispose()
        {
        }
    }
}
