using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class RowXPropFunction: Function
{
    internal RowXPropFunction() : 
        base(
            name: "RowXProp",
            result: typeof(object) ,
            isValidateArguments: false, 
            IsVariantArgumentList: false, 
            argumentCount: 1,
            a1: null, 
            a2: null, 
            a3: null)
    {
        UseRow = true;
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource,
        Data<ExpressionNode> arguments,
        object[] argumentValues,
        int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        var xPropName = (string)argumentValues[0];

        if (testValues != null)
        {
            if(testValues.TryGetValue(xPropName, out var testValue))
            {
                return testValue;
            }
        }

        return expressionDataSource.GetRowXPropertyByHandle(row.Value, xPropName) ?? string.Empty;
    }
}