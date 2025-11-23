using System.Linq;
using System.Text;
using Konsarpoo.Collections;

namespace Brudixy.Exceptions
{
    public class ParentConstraintViolationException : System.Exception
    {
        private Map<int,string> m_rowErrors = new Map<int, string>();
        public Map<int, string> ParentConstraintViolationRowHandles { get; }

        public Map<int, string> RowsError => m_rowErrors;

        public ParentConstraintViolationException(CoreDataTable table, Map<int,string> parentConstraintViolationRowHandles)
        {
            ParentConstraintViolationRowHandles = parentConstraintViolationRowHandles;

            var tableParentRelationsMap = table.ParentRelationsMap;
            
            if (tableParentRelationsMap is not null)
            {
                foreach (var kv in parentConstraintViolationRowHandles)
                {
                    var rowHandle = kv.Key;
                    var constraintError = kv.Value;

                    var relation = tableParentRelationsMap.Values
                        .First(c => c.ParentKeyConstraint.constraintName == constraintError);

                    var stringBuilder = new StringBuilder();

                    stringBuilder.Append($"Child row handle = '{rowHandle}', ");

                    foreach (var column in relation.ParentKeyConstraint.ChildKey.ColumnsReference)
                    {
                        stringBuilder.Append(
                            $"Child key value [{column.ColumnName}] = '{table.GetRowFieldValue(rowHandle, column, DefaultValueType.ColumnBased, null)}'; ");
                    }

                    stringBuilder.AppendLine();

                    var rowMessage = $"Constraint '{constraintError}' valid check failed. Parent '{relation.ParentTableName}' table has missing keys. Error info: {stringBuilder}";

                    m_rowErrors[rowHandle] = rowMessage;
                }
            }
        }
    }
}