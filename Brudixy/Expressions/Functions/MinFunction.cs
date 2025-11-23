namespace Brudixy.Expressions.Functions;

public class MinFunction : BuiltinTableAggregateFunction
{
    internal MinFunction() : 
        base(AggregateType.Min,
        name: "Min", 
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