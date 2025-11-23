using Brudixy.Interfaces;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class RangeFunction : Function
{
    internal RangeFunction() : base(
        name: "Range", 
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
        object start = argumentValues[0];
        object end = argumentValues[1];

        if (start == null)
        {
            throw new ArgumentException("Range start param cannot be null");
        }
                    
        if (end == null)
        {
            throw new ArgumentException("Range end param cannot be null");
        }

        var st = start.GetType();
        var et = end.GetType();

        if (st != et)
        {
            throw new ArgumentException($"Range start end end types doesn't match. {st.AssemblyQualifiedName} != {et.AssemblyQualifiedName}.");
        }

        Type constructedType = typeof(Range<>).MakeGenericType(st);
                    
        return System.Activator.CreateInstance(constructedType, new object[] { start, end });
    }
}