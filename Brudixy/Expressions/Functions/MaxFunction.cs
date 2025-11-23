namespace Brudixy.Expressions.Functions;

public class MaxFunction : BuiltinTableAggregateFunction
{
    internal MaxFunction() :
        base(AggregateType.Count,
        name: "Max", 
        result: typeof (object), 
        isValidateArguments: false,
        isVariantArgumentList: false,
        argumentCount: 1, 
        a1: null, 
        a2: null, 
        a3: null)
    {
    }
}