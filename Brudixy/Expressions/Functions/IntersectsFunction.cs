using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class IntersectsFunction : Function
{
    internal IntersectsFunction() 
        : 
        base(
            name: "Intersects",
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
        var argumentValue = (IRange)argumentValues[0];
        var arg = (IRange)argumentValues[1];

        return argumentValue.Intersects(arg);
    }
}