using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    public abstract class Function 
    {
        internal readonly Type[] parameters = new Type[3];
        internal readonly string name;
        internal readonly Type result;
        internal readonly bool IsValidateArguments;
        internal readonly bool IsVariantArgumentList;
        internal readonly int argumentCount;

        public Function(string name, Type result, bool isValidateArguments, bool IsVariantArgumentList, int argumentCount, Type a1, Type a2, Type a3)
        {
            this.name = name;
            this.result = result;
            this.IsValidateArguments = isValidateArguments;
            this.IsVariantArgumentList = IsVariantArgumentList;
            this.argumentCount = argumentCount;
            if (a1 != null)
            {
                parameters[0] = a1;
            }

            if (a2 != null)
            {
                parameters[1] = a2;
            }

            if (!(a3 != null))
            {
                return;
            }
            parameters[2] = a3;
        }
        
        public virtual object Eval(IExpressionDataSource expressionDataSource,
            Data<ExpressionNode> arguments,
            int? row = null, 
            IReadOnlyDictionary<string, object> testValues = null)
        {
            var argumentValues = new object[arguments.Count];

            PrepareArguments(expressionDataSource, arguments, row, testValues, argumentValues);

            var evalFunction = EvalFunction(expressionDataSource, arguments, argumentValues, row, testValues: testValues);
            
            return evalFunction;
        }

        protected abstract object EvalFunction(IExpressionDataSource expressionDataSource,
            Data<ExpressionNode> arguments,
            object[] argumentValues,
            int? row,
            IReadOnlyDictionary<string, object> testValues);

        public bool IsAggregate { get; protected set; }

        public bool UseRow  { get; protected set; }

        public virtual void PrepareArguments(IExpressionDataSource expressionDataSource,
            Data<ExpressionNode> arguments,
            int? row, IReadOnlyDictionary<string, object> testValues,
            object[] argumentValues)
        {
            for (int index = 0; index < argumentCount; ++index)
            {
                argumentValues[index] = arguments[index].Eval(row, testValues);

                if (IsValidateArguments)
                {
                    var isObj = typeof(object) == parameters[index];

                    if (argumentValues[index] == null && isObj)
                    {
                        continue;
                    }

                    if (argumentValues[index].GetType() != parameters[index])
                    {
                        if (parameters[index] == typeof(Object))
                        {
                            continue;
                        }
                        
                        argumentValues[index] = Convert.ChangeType(argumentValues[index], parameters[index]);
                    }
                }
            }
        }

        public virtual void BindArguments(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments, Data<string> columns)
        {
            foreach (var argument in arguments)
            {
                argument.Mount(expressionDataSource, columns);
            }
        }
    }
}
