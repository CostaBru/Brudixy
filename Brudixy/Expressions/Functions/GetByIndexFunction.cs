using Brudixy.Converter;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class GetByIndexFunction : Function
{
    internal GetByIndexFunction() 
        :
        base(
            name: "GetByIndex", 
            result: typeof (object), 
            isValidateArguments: true,
            IsVariantArgumentList: false, 
            argumentCount: 2, 
            a1: typeof (object), 
            a2: typeof(int), 
            a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row, IReadOnlyDictionary<string, object> testValues)
    {
        var array = (Array)argumentValues[0];
        var argumentValue = Tool.ConvertBoxed<int>(argumentValues[1]);

        return array.GetValue(argumentValue);
    }
}