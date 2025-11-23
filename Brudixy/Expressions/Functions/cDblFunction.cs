using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class cDblFunction : Function
{
    public IFormatProvider FormatProvider { get; }

    internal cDblFunction(IFormatProvider formatProvider) : base(
        name: "cDbl",
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
        return Convert.ToDouble(argumentValues[0], FormatProvider);
    }
}