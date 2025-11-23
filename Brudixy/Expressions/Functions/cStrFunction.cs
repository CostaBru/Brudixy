using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class cStrFunction : Function
{
    public IFormatProvider FormatProvider { get; }

    internal cStrFunction(IFormatProvider formatProvider) : 
        base(
            name: "cStr", 
            result: typeof (string), 
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
        return CoreDataRowContainer.ValueToStringFormat(null, FormatProvider, argumentValues[0], null);
    }
}