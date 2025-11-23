using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class AbsFunction : Function
{
    internal AbsFunction() 
        : base(name: "Abs", 
            result: typeof (object),
            isValidateArguments: true, 
            IsVariantArgumentList: false, 
            argumentCount: 1, 
            a1: typeof (object), 
            a2: null, 
            a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        var storageType3 = CoreDataTable.GetColumnType(argumentValues[0].GetType());
        if (IsInteger(storageType3.type) && storageType3.typeModifier == TableStorageTypeModifier.Simple)
        {
            switch (storageType3.type)
            {
                case TableStorageType.Decimal: return Math.Abs((decimal)argumentValues[0]);
                case TableStorageType.SByte: return Math.Abs((sbyte)argumentValues[0]);
                case TableStorageType.Double: return Math.Abs((double)argumentValues[0]);
                case TableStorageType.Single: return Math.Abs((float)argumentValues[0]);
                case TableStorageType.Int32: return Math.Abs((int)argumentValues[0]);
                case TableStorageType.Int16: return Math.Abs((short)argumentValues[0]);
                case TableStorageType.Int64: return Math.Abs((long)argumentValues[0]);
                case TableStorageType.UInt16: return argumentValues[0];
                case TableStorageType.UInt32:  return argumentValues[0];
                case TableStorageType.UInt64:  return argumentValues[0];
                case TableStorageType.Byte:  return argumentValues[0];
            }

            return Math.Abs((int)Convert.ChangeType(argumentValues[0], typeof(int)));
        }

        if (IsNumeric(storageType3.type) && storageType3.typeModifier == TableStorageTypeModifier.Simple)
        {
            return Math.Abs((double) argumentValues[0]);
        }
                    
        throw ExprException.ArgumentAbsTypeMismatch(argumentValues[0]);
    }
    
    internal static bool IsNumeric(TableStorageType type)
    {
        if (!IsFloat(type))
            return IsInteger(type);
        return true;
    }

    internal static bool IsFloat(TableStorageType type)
    {
        if (type != TableStorageType.Single && type != TableStorageType.Double)
        {
            return type == TableStorageType.Decimal;
        }
        return true;
    }
    
    internal static bool IsInteger(TableStorageType type)
    {
        if (type != TableStorageType.Int16 && type != TableStorageType.Int32 &&
            (type != TableStorageType.Int64 && type != TableStorageType.UInt16) && (type != TableStorageType.UInt32 &&
                type != TableStorageType.UInt64 && type != TableStorageType.SByte))
        {
            return type == TableStorageType.Byte;
        }
        return true;
    }
}