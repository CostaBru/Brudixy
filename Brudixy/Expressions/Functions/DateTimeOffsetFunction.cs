using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions.Functions;

public class DateTimeOffsetFunction : Function
{
    internal DateTimeOffsetFunction() 
        : 
        base(
            name: "DateTimeOffset", 
            result: typeof (DateTimeOffset),
            isValidateArguments: false, 
            IsVariantArgumentList: true, 
            argumentCount: 3, 
            a1: typeof (DateTime), 
            a2: typeof (int),
            a3: typeof (int))
    {
    }

    protected override object EvalFunction(IExpressionDataSource expressionDataSource, Data<ExpressionNode> arguments,
        object[] argumentValues, int? row,
        IReadOnlyDictionary<string, object> testValues)
    {
        if (argumentValues[0] == null || argumentValues[1] == null ||
            argumentValues[2] == null)
        {
            return null;
        }

        switch (((DateTime)argumentValues[0]).Kind)
        {
            case DateTimeKind.Utc:
                if ((int)argumentValues[1] != 0 && (int)argumentValues[2] != 0)
                {
                    throw ExprException.MismatchKindandTimeSpan(((DateTime)argumentValues[0]).Kind,
                        (int)argumentValues[1], (int)argumentValues[2]);
                }

                break;
            case DateTimeKind.Local:
                DateTimeOffset now = DateTimeOffset.Now;
                TimeSpan offset = now.Offset;
                if (offset.Hours != (int)argumentValues[1])
                {
                    now = DateTimeOffset.Now;
                    offset = now.Offset;
                    if (offset.Minutes != (int)argumentValues[2])
                    {
                        throw ExprException.MismatchKindandTimeSpan(((DateTime)argumentValues[0]).Kind,
                            (int)argumentValues[1], (int)argumentValues[2]);
                    }
                }

                break;
        }

        if ((int)argumentValues[1] < -14 || (int)argumentValues[1] > 14)
        {
            throw ExprException.InvalidHoursArgument((int)argumentValues[1]);
        }

        if ((int)argumentValues[2] < -59 || (int)argumentValues[2] > 59)
        {
            throw ExprException.InvalidMinutesArgument((int)argumentValues[2]);
        }

        if ((int)argumentValues[1] == 14 && (int)argumentValues[2] > 0)
        {
            throw ExprException.InvalidTimeZoneRange((int)argumentValues[1], (int)argumentValues[2]);
        }

        if ((int)argumentValues[1] == -14 && (int)argumentValues[2] < 0)
        {
            throw ExprException.InvalidTimeZoneRange((int)argumentValues[1], (int)argumentValues[2]);
        }

        return new DateTimeOffset((DateTime)argumentValues[0],
            new TimeSpan((int)argumentValues[1], (int)argumentValues[2], 0));

    }
}