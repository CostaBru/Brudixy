using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class SubstringFunction : Function
{
    internal SubstringFunction() : base(
        name: "Substring", 
        result: typeof (string),
        isValidateArguments: true, 
        IsVariantArgumentList: false,
        argumentCount: 3,
        a1: typeof (string), 
        a2: typeof (int),
        a3: typeof (int))
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row, IReadOnlyDictionary<string, object> testValues)
    {
        int startIndex = Math.Max((int) argumentValues[1] - 1, 0);
        int length = (int) argumentValues[2];

        if (length < 0)
        {
            throw ExprException.FunctionArgumentOutOfRange("length", "Substring", $"{length} is less than 0");
        }
                    
        if (length == 0)
        {
            return string.Empty;
        }

        int length2 = ((string) argumentValues[0]).Length;
        if (startIndex > length2)
        {
            return null;
        }

        if (startIndex + length > length2)
        {
            throw ExprException.FunctionArgumentOutOfRange("length", "Substring", $"startIndex + length ({startIndex + length}) is greater than string length ({length2})");
        }

        return ((string) argumentValues[0]).Substring(startIndex, length);
    }
}