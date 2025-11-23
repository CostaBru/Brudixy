using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class IIfFunction : Function
{
    internal IIfFunction() : 
        base(
            name: "IIf", 
            result: typeof (object), 
            isValidateArguments: false, 
            IsVariantArgumentList: false, 
            argumentCount: 3, 
            a1: typeof (object), 
            a2: typeof (object) ,
            a3: typeof (object))
    {
    }

    public override void PrepareArguments(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        int? row, IReadOnlyDictionary<string, object> testValues, object[] argumentValues)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues,
        int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        if (DataExpression.ToBoolean(arguments[0].Eval(row, testValues)))
        {
            return arguments[1].Eval(row, testValues);
        }

        return arguments[2].Eval(row, testValues);
    }
}