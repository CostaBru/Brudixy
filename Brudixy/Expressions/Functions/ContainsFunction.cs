using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class ContainsFunction : Function
{
    internal ContainsFunction() : base(
        name: "Contains", 
        result: typeof (bool),
        isValidateArguments: true, 
        IsVariantArgumentList: false,
        argumentCount: 2, 
        a1: typeof (object),
        a2: typeof (object), 
        a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row, IReadOnlyDictionary<string, object> testValues)
    {
        var argumentValue = argumentValues[0];
        var target = argumentValues[1];

        if (target == null)
        {
            return false;
        }

        if (argumentValue is Array arr)
        {
            return Array.IndexOf(arr, target) >= 0;
        }

        if (argumentValue is IRange range)
        {
            if (target is IRange argRange)
            {
                return range.ContainsRange(argRange);
            }

            return range.ContainsValue((IComparable)target);
        }

        var argStr = (string)argumentValue;

        if (target is string str)
        {
            return argStr.Contains(str);
        }

        var type = target.GetType();

        var tableStorageType = CoreDataTable.GetColumnType(type);

        var convertValue = Brudixy.TypeConvertor.ConvertValue<string>(target, 
            "Contains Func context", 
            "Contains Func context",
            tableStorageType.type, 
            tableStorageType.typeModifier, 
            "Contains Func context");
        
        return argStr.Contains(convertValue);
    }
}