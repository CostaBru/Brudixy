namespace Brudixy.Expressions.Functions;

public class VarFunction : BuiltinTableAggregateFunction
{
    internal VarFunction() : 
        base(AggregateType.Var,
        name: "Var",
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