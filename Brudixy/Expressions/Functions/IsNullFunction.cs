using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class IsNullFunction : Function
{
    internal IsNullFunction() : base(
        name: "IsNull",
        result: typeof (object), 
        isValidateArguments: false, 
        IsVariantArgumentList: false, 
        argumentCount: 2, 
        a1: typeof (object), 
        a2: typeof (object),
        a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        if (argumentValues[0] == null)
        {
            return argumentValues[1];
        }

        return argumentValues[0];
    }
}