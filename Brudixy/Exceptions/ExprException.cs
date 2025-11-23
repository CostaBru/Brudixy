using Brudixy.Expressions;

namespace Brudixy.Exceptions
{
    internal sealed class ExprException
    {
        private ExprException()
        {
        }

        private static InvalidExpressionException _Expr(string error)
        {
            var expressionException = new InvalidExpressionException(error);
            ExceptionBuilder.TraceExceptionAsReturnValue(expressionException);
            return expressionException;
        }

        private static SyntaxErrorException _Syntax(string error)
        {
            var syntaxErrorException = new SyntaxErrorException(error);
            ExceptionBuilder.TraceExceptionAsReturnValue(syntaxErrorException);
            return syntaxErrorException;
        }

        private static EvaluateException _Eval(string error)
        {
            var evaluateException = new EvaluateException(error);
            ExceptionBuilder.TraceExceptionAsReturnValue(evaluateException);
            return evaluateException;
        }

        public static Exception InFuncError(string moreinfo)
        {
            return _Expr($"Func {moreinfo} parse error.");
        }

        public static Exception TypeMismatch(string expr)
        {
            return _Eval($"Type mismatch error occured. Expression: {expr}");
        }

        public static Exception FunctionArgumentOutOfRange(string arg, string func, string range)
        {
            return ExceptionBuilder._ArgumentOutOfRange(arg, $"Function {func} argument {arg} is out of range ({range}). ");
        }

        public static Exception ExpressionTooComplex(int maxNodes)
        {
            return _Eval($"Expression is too complex and contains more than {maxNodes} nodes.");
        }

        public static Exception UnboundName(string error)
        {
            return _Eval(error);
        }

        public static Exception InvalidString(string str)
        {
            return _Syntax($"Expression parse error. Invalid string: '{str}'");
        }

        public static Exception UndefinedFunction(string name)
        {
            return _Eval($"'{name}' given function is not supported.");
        }
   
        public static Exception FunctionArgumentCount(string name, int argCount, int givenArgCount)
        {
            return _Eval($"Expression parsing error. Function {name} expects {argCount} argument count, but was used with {givenArgCount}.");
        }
       
        public static Exception UnknownToken(string token, int position)
        {
            return _Syntax($"Expression parsing error. Cannot parse token '{token}' at {position} position.");
        }

        public static Exception UnknownToken(Tokens tokExpected, Tokens tokCurr, int position)
        {
            return _Syntax($"Expression parsing error. Unexpected token {tokCurr} as {position} position, expected one is {tokExpected}");
        }

        public static Exception DatatypeConvertion(Type type1, Type type2, string funcName)
        {
            return _Eval($"Expression evaluation error cannot convert (func {funcName}) from {type1.ToString()} to {type2.ToString()}");
        }

        public static Exception InvalidName(string name)
        {
            return _Syntax($"Name parse error '{name}'.");
        }

        public static Exception InvalidDate(string date)
        {
            return _Syntax($"Date parse error '{date}'. Date should we between # characters.");
        }

        public static Exception NonConstantArgument()
        {
            return _Eval("Cannot evaluate IN function with non constant node. It supposed to be evaluated beforehand.");
        }

        public static Exception InvalidPattern(string pat, string reason)
        {
            return _Eval($"Like expression parse error. Pattern: '{pat}'. Reason: {reason}.");
        }

        public static Exception InWithoutList()
        {
            return _Syntax("In arguments parsing error. No arguments are given.");
        }

        public static Exception ArgumentType(string function, int arg, Type funcType, string argType)
        {
            return _Eval($"Function {function} evaluation error. Argument #{arg} has invalid type '{argType}', '{funcType}' is expected one.");
        }

        public static Exception ArgumentAbsTypeMismatch(object value)
        {
            return _Eval($"Expression evaluation error. Cannot call ABS for non number value '{value}' with type {value.GetType()}.");
        }
       
        public static Exception UnsupportedOperator(int op)
        {
            return _Eval($"Expression evaluation error. Non supported operator opt code {op}.");
        }

        public static Exception InvalidNameBracketing(string name)
        {
            return _Syntax($"Expression parse error. Invalid bracket string {name}");
        }

        public static Exception UnresolvedRelation(string name, string expr, string error)
        {
            return _Eval($"Evaluation error. Cannot resolve relation expression {expr} for {name}. Error: {error}");
        }

        internal static EvaluateException BindFailure(string relationName, string tableTableName)
        {
            return _Eval($"Evaluation error. Data table '{tableTableName}' doesn't contain '{relationName}' relation.");
        }

        public static Exception AggregateArgument()
        {
            return _Syntax("Cannot parse aggregate expression.");
        }

        public static Exception ExpressionUnbound(string expr)
        {
            return _Eval($"Evaluation error. {expr} node is unbound to any data table.");
        }

        public static Exception ComputeNotAggregate(string expr)
        {
            return _Eval($"Evaluation error. Cannot evaluate aggregate {expr}.");
        }

        public static Exception FilterConvertion(string expr, object filterValue)
        {
            return _Eval($"Evaluate error. Select '{expr}' filter should return boolean value, but has '{filterValue}' with '{filterValue?.GetType()}' type.");
        }

        public static Exception InvalidType(string typeName)
        {
            return _Eval($"Evaluate error. Cannot load unknown type {typeName}.");
        }

        public static Exception InvalidHoursArgument(int argumentValue)
        {
            return _Eval($"DatetimeOffset func has wrong ({argumentValue}) hour offset argument. ");
        }

        public static Exception InvalidMinutesArgument(int argumentValue)
        {
            return _Eval($"DatetimeOffset func has wrong ({argumentValue}) minute offset argument. ");
        }

        public static Exception InvalidTimeZoneRange(int hour, int minute)
        {
            return _Eval($"DatetimeOffset func has wrong ({hour}:{minute}) offset arguments.");
        }

        public static Exception MismatchKindandTimeSpan(DateTimeKind kind, int hour, int minute)
        {
            return _Eval($"DatetimeOffset func has {hour}:{minute} offset and time kind {kind} arguments.");
        }
    }
}