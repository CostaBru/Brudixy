using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class CharIndexFunction : Function
{
    public CharIndexFunction() :   base(
        name: "CharIndex", 
        result: typeof (int), 
        isValidateArguments: true, 
        IsVariantArgumentList: false, 
        argumentCount: 2, 
        a1: typeof (string),
        a2: typeof(int), 
        a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        if (argumentValues[0] == null || argumentValues[1] == null)
        {
            return null;
        }

        return ((string) argumentValues[1]).IndexOf((string) argumentValues[0], StringComparison.Ordinal);
    }
}