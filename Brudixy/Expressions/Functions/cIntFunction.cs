using Brudixy.Converter;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class cIntFunction : Function
{
    internal cIntFunction() : 
        base(
            name: "cInt", 
            result: typeof (int),
            isValidateArguments: false, 
            IsVariantArgumentList: false, 
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
        var evalFunction = argumentValues[0].GetInt32(int.MinValue);
                    
        return evalFunction;
    }
}