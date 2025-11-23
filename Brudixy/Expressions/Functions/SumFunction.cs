namespace Brudixy.Expressions.Functions;

public class SumFunction : BuiltinTableAggregateFunction
{
    internal SumFunction() : 
        base(AggregateType.Sum,
        name: "Sum", 
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