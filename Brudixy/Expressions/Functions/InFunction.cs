using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class InFunction : Function
{
    internal InFunction() :
        base(
            name: "In", 
            result: typeof (bool), 
            isValidateArguments: false, 
            IsVariantArgumentList: true, 
            argumentCount: 1, 
            a1: null, 
            a2: null, 
            a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        return null;
    }
}