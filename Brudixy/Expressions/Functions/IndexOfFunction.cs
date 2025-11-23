using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class IndexOfFunction : Function
{
    internal IndexOfFunction() : 
        base(
            name: "IndexOf", 
            result: typeof (int), 
            isValidateArguments: true, 
            IsVariantArgumentList: false, 
            argumentCount: 2, 
            a1: typeof (object),
            a2: null, 
            a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row, IReadOnlyDictionary<string, object> testValues)
    {
        var array = (Array)argumentValues[0];
        var argumentValue = (object)argumentValues[1];

        return Array.IndexOf(array, argumentValue);
    }
}