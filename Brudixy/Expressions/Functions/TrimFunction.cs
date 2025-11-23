using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class TrimFunction : Function
{
    internal TrimFunction() : base(
        name: "Trim", 
        result: typeof (string), 
        isValidateArguments: true,
        IsVariantArgumentList: false, 
        argumentCount: 1, 
        a1: typeof (string),
        a2: null,
        a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource,
        Data<ExpressionNode> arguments,
        object[] argumentValues,
        int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        if (argumentValues[0] == null)
        {
            return null;
        }

        return ((string) argumentValues[0]).Trim();
    }
}