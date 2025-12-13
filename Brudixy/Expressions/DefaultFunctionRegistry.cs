using Brudixy.Expressions.Functions;
using JetBrains.Annotations;
using Konsarpoo.Collections;

namespace Brudixy.Expressions;

public interface IFunctionRegistry
{
    Func<IFormatProvider, Function> GetFunctionFactory(string name);
}

public static class FunctionRegistry
{
    public static readonly DefaultFunctionRegistry Registry = new DefaultFunctionRegistry();
} 

public class DefaultFunctionRegistry : IFunctionRegistry
{
    private readonly Dictionary<string, Func<IFormatProvider, Function>> m_funcFactory = new(StringComparer.OrdinalIgnoreCase)
    {
        { "Abs", (p) => new AbsFunction() },
        { "IIf",  (p) => new IIfFunction() },
        { "In",  (p) => new InFunction() },
        { "IsNull",  (p) => new IsNullFunction() },
        { "Len",  (p) => new LenFunction() },
        { "Contains",  (p) => new ContainsFunction() },
        { "IndexOf",  (p) => new IndexOfFunction() },
        { "GetByIndex",  (p) => new GetByIndexFunction() },
        { "XValue",  (p) => new XValueFunction() },
        { "XElement",  (p) => new XElementFunction() },
        { "XElements",  (p) => new XElementsFunction() },
        { "XAttribute",  (p) => new XAttributeFunction() },
        { "XAttributeNames",  (p) => new XAttributeNamesFunction() },
        { "XSelectAttributes",  (p) => new XSelectAttributesFunction() },
        { "XSelectElements",  (p) => new XSelectElementsFunction() },
        { "XQueryElements",  (p) => new XQueryElementsFunction() },
        { "Range",  (p) => new RangeFunction() },
        { "Intersects",  (p) => new IntersectsFunction() },
        { "Substring",  (p) => new SubstringFunction() },
        { "Trim",  (p) => new TrimFunction() },
        { "Convert",  (p) => new ConvertFunction() },
        { "DateTimeOffset",  (p) =>new DateTimeOffsetFunction() },
        { "cInt",  (p) => new cIntFunction() },
        { "cBool",  (p) => new cBoolFunction() },
        { "cDate",  (p) => new cDateFunction(p) },
        { "cDbl",  (p) => new cDblFunction(p) },
        { "cMoney",  (p) => new cMoneyFunction(p) },
        { "cStr",  (p) => new cStrFunction(p) },
        { "Max",  (p) => new MaxFunction() },
        { "Min",  (p) => new MinFunction() },
        { "Sum",  (p) => new SumFunction() },
        { "Count",  (p) => new CountFunction() },
        { "Var",  (p) => new VarFunction() },
        { "StDev",  (p) => new StDevFunction() },
        { "Avg",  (p) => new AvgFunction() },
        { "RowXProp",  (p) => new RowXPropFunction() },
        { "CharIndex",  (p) => new CharIndexFunction() },
    };

    public IEnumerable<string> GetFunctions() => m_funcFactory.Keys;

    public Func<IFormatProvider, Function> GetFunctionFactory([NotNull] string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }
        
        return m_funcFactory.GetOrDefault(name);
    }
    
    public void RegisterFunction([NotNull] string name, [NotNull] Func<IFormatProvider, Function> functionFactory)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }
        m_funcFactory[name] = functionFactory ?? throw new ArgumentNullException(nameof(functionFactory));
    }
    
    public void DeregisterFunction([NotNull] string name)
    {
        if (name == null)
        {
            throw new ArgumentNullException(nameof(name));
        }
        m_funcFactory.Remove(name); 
    }
}