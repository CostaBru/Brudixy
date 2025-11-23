namespace Brudixy.Expressions.Functions;

public class CountFunction : BuiltinTableAggregateFunction
{
    internal CountFunction() : 
        base(AggregateType.Count,
            name: "Count",
            result: typeof (object), 
            isValidateArguments: false,
            isVariantArgumentList: false, 
            argumentCount: 1,
            a1: null, 
            a2: null, 
            a3: null)
    {
        IsAggregate = true;
    }
}