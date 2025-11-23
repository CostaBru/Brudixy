using System.Xml.Linq;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class XValueFunction : Function
{
    internal XValueFunction() : 
        base(
            name: "XValue", 
            result: typeof (object), 
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
        return (argumentValues[0] as XElement)?.Value ?? string.Empty;
    }
}