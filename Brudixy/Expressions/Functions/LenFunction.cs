using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class LenFunction : Function
{
    internal LenFunction() : base(
        name: "Len", 
        result: typeof (int), 
        isValidateArguments: true, 
        IsVariantArgumentList: false,
        argumentCount: 1, 
        a1: typeof (object), 
        a2: null, 
        a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row, IReadOnlyDictionary<string, object> testValues)
    {
        var argumentValue = argumentValues[0];

        if(argumentValue is Array arr)
        {
            return arr.Length;
        }
        if(argumentValue is IRange ramge)
        {
            return (int)(ramge.GetLenghtD() ?? 0);
        }
                    
        return ((string)argumentValue).Length;
    }
}