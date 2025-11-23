namespace Brudixy.Expressions.Functions;

public class AvgFunction : BuiltinTableAggregateFunction
{
    internal AvgFunction() 
        : 
        base(AggregateType.Mean,
            name: "Avg", 
            result: typeof(object),
            isValidateArguments: false,
            isVariantArgumentList: false,
            argumentCount: 1, 
            a1: null, 
            a2: null,
            a3: null)
    {
    }
}