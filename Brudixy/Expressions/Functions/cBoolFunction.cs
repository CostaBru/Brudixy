using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class cBoolFunction : Function
{
    internal cBoolFunction() :
        base(
            name: "cBool", 
            result: typeof (bool),
            isValidateArguments: false, 
            IsVariantArgumentList: false,
            argumentCount: 1, 
            a1: null, 
            a2: null, 
            a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        var columnType = CoreDataTable.GetColumnType(argumentValues[0].GetType());

        if (columnType.typeModifier != TableStorageTypeModifier.Simple)
        {
            throw ExprException.DatatypeConvertion(argumentValues[0].GetType(), typeof(bool), "cBool");
        }
        
        switch (columnType.type)
        {
            case TableStorageType.Double:
            {
                return (double) (argumentValues[0]) != 0.0;
            }
            case TableStorageType.String:
            {
                return bool.Parse((string) argumentValues[0]);
            }
            case TableStorageType.Boolean:
            {
                return (bool) argumentValues[0];
            }
            case TableStorageType.Int32:
            {
                return (uint) (int) argumentValues[0] > 0U;
            }
            default:
            {
                throw ExprException.DatatypeConvertion(argumentValues[0].GetType(), typeof(bool), "cBool");
            }
        }
    }
}