using System.Xml.Linq;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class XAttributeNamesFunction : Function
{
    internal XAttributeNamesFunction() 
        : base(
            name: "XAttributeNames", 
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
        var value = (XElement)argumentValues[0];

        return value.Attributes().Select(a => a.Name.ToString()).ToArray();
    }
}