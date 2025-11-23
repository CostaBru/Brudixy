using System;

namespace Brudixy.Exceptions
{
    public class DataChangeCancelException : DataException
    {
        public string Reason { get; }
        public string TableName { get; }
        public string Column { get; }
        public int RowHandle { get; }

        public DataChangeCancelException(CoreDataRow row, string column, string reason) : base(GetMessage(row, column, reason))
        {
            Reason = reason;
            TableName = row?.GetTableName();
            RowHandle = row?.RowHandleCore ?? -1;
            Column = column;
        }

        public static string GetMessage(CoreDataRow row, string column, string reason)
        {
            return $"The changing '{column}' of the {row?.GetType().Name}('{row?.DebugKeyValue}') of the '{row?.GetTableName()}' table was canceled. Reason: {reason}";
        }
    }
}