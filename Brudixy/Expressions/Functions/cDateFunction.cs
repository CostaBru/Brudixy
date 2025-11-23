using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class cDateFunction : Function
{
    public IFormatProvider FormatProvider { get; }

    internal cDateFunction(IFormatProvider formatProvider) :
        base(
            name: "cDate",
            result: typeof (DateTime),
            isValidateArguments: false, 
            IsVariantArgumentList: false, 
            argumentCount: 1, 
            a1: null, 
            a2: null, 
            a3: null)
    {
        FormatProvider = formatProvider;
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource,
        Data<ExpressionNode> arguments,
        object[] argumentValues, int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        return Convert.ToDateTime(argumentValues[0], FormatProvider);
    }
}