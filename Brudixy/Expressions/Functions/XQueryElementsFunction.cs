using System.Xml.Linq;
using System.Xml.XPath;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class XQueryElementsFunction : Function
{
    internal XQueryElementsFunction() :
        base(
            name: "XQueryElements",
            result: typeof (object), 
            isValidateArguments: true, 
            IsVariantArgumentList: false, 
            argumentCount: 2, 
            a1: typeof (object), 
            a2: typeof (object), 
            a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row, IReadOnlyDictionary<string, object> testValues)
    {
        var value = (XElement)argumentValues[0];
        var elVal = (string)argumentValues[1];

        var elements = value.XPathSelectElements(elVal)
            .ToArray();
                    
        return elements;
    }
}