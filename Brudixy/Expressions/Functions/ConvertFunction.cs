using System.Xml;
using Brudixy.Converter;
using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class ConvertFunction : Function
{
    private Type m_nameNodeType = typeof(NameNode);

    internal ConvertFunction() :
        base(
            name: "Convert", 
            result: typeof(object) ,
            isValidateArguments: false, 
            IsVariantArgumentList: true, 
            argumentCount: 2, 
            a1: typeof (object),
            a2: null, 
            a3: null)
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource,
        Data<ExpressionNode> expressionNodes, 
        object[] argumentValues, 
        int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        if (argumentCount != 2)
        {
            throw ExprException.FunctionArgumentCount(name, 2, argumentCount);
        }

        var argumentValue = argumentValues[0];
        
        if (argumentValue == null)
        {
            return null;
        }

        Type targetType = (Type)argumentValues[1];

        return ChangeType(argumentValue, targetType);
    }

    public static object ChangeType(object argumentValue, Type targetType)
    {
        if (targetType == typeof(string))
        {
            return CoreDataTable.ConvertObjectToString(argumentValue);
        }

        if (argumentValue is string str)
        {
            var columnType = CoreDataTable.GetColumnType(targetType);

            return CoreDataTable.ConvertStringToObject(columnType.type, columnType.typeModifier, str, targetType);
        }

        return Tool.ConvertBoxed(argumentValue, targetType);
    }

    public override void PrepareArguments(IExpressionDataSource expressionDataSource, 
        Data<ExpressionNode> arguments,
        int? row, IReadOnlyDictionary<string, object> testValues,
        object[] argumentValues)
    {
        if (arguments.Count != 2)
        {
            throw ExprException.FunctionArgumentCount(name, 2, argumentCount);
        }

        argumentValues[0] = arguments[0].Eval(row, testValues);
        argumentValues[1] = GetDataType(arguments[1]);
    }

    public override void BindArguments(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments, Data<string> columns)
    {
        if (arguments.Count != 2)
        {
            throw ExprException.FunctionArgumentCount(this.name, 2, arguments.Count);
        }

        arguments[0].Mount(expressionDataSource, columns);

        var expressionNode = arguments[1];
                
        if (expressionNode.GetType() == m_nameNodeType)
        {
            NameNode nameNode = (NameNode)expressionNode;
            arguments[1] = new ConstNode(expressionDataSource, ValueType.Str, nameNode.name);
        }

        expressionNode.Mount(expressionDataSource, columns);
    }

    private Type GetDataType(ExpressionNode node)
    {
        Type nodeType = node.GetType();
        string typeName = null;

        if (nodeType == m_nameNodeType)
        {
            typeName = ((NameNode)node).name;
        }
        if (nodeType == typeof(ConstNode))
        {
            typeName = ((ConstNode)node).m_val.ToString();
        }

        if (typeName == null)
        {
            throw ExprException.ArgumentType(name, 2, typeof(Type), "NULL");
        }

        if (Enum.TryParse<TableStorageType>(typeName, ignoreCase: true, out var type))
        {
            return TableStorageTypeMap.GetType(type);
        }

        Type dataType = Type.GetType(typeName);

        if (dataType == null)
        {
            throw ExprException.InvalidType(typeName);
        }

        return dataType;
    }
}