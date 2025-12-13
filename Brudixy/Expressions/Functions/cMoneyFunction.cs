using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class cMoneyFunction : Function
{
    public IFormatProvider FormatProvider { get; }

    internal cMoneyFunction(IFormatProvider formatProvider) : base(
        name: "cMoney",
        result: typeof (double), 
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
        object[] argumentValues,
        int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        return Convert.ToDecimal(argumentValues[0], FormatProvider);
    }
}