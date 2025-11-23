namespace Brudixy.Expressions.Functions;

public class StDevFunction : BuiltinTableAggregateFunction
{
    internal StDevFunction() :
        base(AggregateType.StDev,
        name: "StDev", 
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