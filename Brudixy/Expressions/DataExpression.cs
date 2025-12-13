using Brudixy.Interfaces;
using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    public sealed class DataExpression : IDisposable
    {
        private Data<string> m_dependency = new ();
        private string m_originalExpression;
        private bool m_parsed;
        private bool m_mounted;
        private ExpressionNode m_exprNode;
        private IExpressionDataSource m_dataSource;
        private readonly TableStorageType m_storageType;

        internal string Expression
        {
            get
            {
                if (m_originalExpression == null)
                {
                    return string.Empty;
                }
                return m_originalExpression;
            }
        }

        internal ExpressionNode ExpressionNode => m_exprNode;

        internal bool HasValue => m_exprNode != null;

        public DataExpression(IExpressionDataSource table, string expression)
          : this(table, expression, TableStorageType.String)
        {
        }

        public DataExpression(IExpressionDataSource table, string expression, TableStorageType type)
        {
            ExpressionParser expressionParser = new ExpressionParser(table);
            expressionParser.LoadExpression(expression);
            m_originalExpression = expression;
            m_exprNode = null;

            if (expression == null)
            {
                return;
            }

            m_storageType = type;

            if (m_storageType == TableStorageType.BigInteger)
            {
                throw new ArgumentException($"{table.Name} table '{expression}' expression cannot have expression because of unsupported {m_storageType} data type.");
            }
            
            m_exprNode = expressionParser.Parse();
            m_parsed = true;

            if (m_exprNode != null && table != null)
            {
                Mount(table);
            }
            else
            {
                m_mounted = false;
            }
        }

        internal void Update()
        {
            if (m_mounted == false && m_exprNode != null && m_dataSource != null)
            {
                Mount(m_dataSource);
            }
        }

        internal void Mount(IExpressionDataSource table)
        {
            this.m_dataSource = table;
            if (table == null || m_exprNode == null)
            {
                return;
            }
            var list = new Data<string>();
            m_exprNode.Mount(table, list);
            m_exprNode = m_exprNode.Optimize();
            this.m_dataSource = table;
            m_mounted = true;
            m_dependency = list;
        }

        public void Validate()
        {
            if(m_exprNode is BinaryNode bn)
            {
                ValidateBinaryExpression(m_dataSource, bn);
            }
        }

        private static void ValidateBinaryExpression(IExpressionDataSource table, BinaryNode bn)
        {
            var dictionary = new Dictionary<string, object>();

            foreach (var column in table.GetColumns())
            {
                var tableStorageType = table.GetColumnType(column);

                if (tableStorageType == TableStorageType.UserType)
                {
                    dictionary[column] = null;
                }
                else
                {
                    dictionary[column] = CoreDataTable.GetDefaultNotNull(tableStorageType ?? TableStorageType.String, TableStorageTypeModifier.Simple);
                }
            }

            bn.Eval(0, dictionary, true);
        }

        internal bool DependsOn(string column)
        {
            if (m_exprNode != null)
            {
                return m_exprNode.DependsOn(column);
            }
            return false;
        }

        internal object Evaluate(int? row = null, IReadOnlyDictionary<string, object> testValues = null)
        {
            if (!m_mounted)
            {
                Mount(m_dataSource);
            }

            object obj;
            if (m_exprNode != null)
            {
                obj = m_exprNode.Eval(row, testValues);
            }
            else
            {
                obj = null;
            }
            return obj;
        }
       
        internal object Evaluate(Data<DataRow> rows)
        {
            return Evaluate(rows, DataRowVersion.Current);
        }

        internal object Evaluate(Data<DataRow> rows, DataRowVersion version)
        {
            if (!m_mounted)
            {
                Mount(m_dataSource);
            }
            
            if (m_exprNode == null)
            {
                return null;
            }
            
            var list = new Data<int>();
            foreach (DataRow dataRow in rows)
            {
                if (dataRow.RowRecordState != RowState.Deleted && (version != DataRowVersion.Original))
                {
                    list.Add(dataRow.RowHandleCore);
                }
            }

            var eval = m_exprNode.Eval(list);
            
            list.Dispose();
            
            return eval;
        }

        public bool Invoke(int? row = null)
        {
            if (m_exprNode == null)
            {
                return true;
            }

            if (row == null)
            {
                throw new ArgumentException("row");
            }

            object obj = m_exprNode.Eval(row);
            try
            {
                return ToBoolean(obj);
            }
            catch (EvaluateException ex)
            {
                throw new InvalidExpressionException($"Filter expression {Expression} error.", ex);
            }
        }

        internal Data<string> GetDependency()
        {
            return m_dependency.ToData();
        }

        internal bool IsTableAggregate()
        {
            if (m_exprNode != null)
            {
                return m_exprNode.IsTableConstant();
            }
            return false;
        }

        internal static bool IsUnknown(object value)
        {
            return value == null;
        }

        internal bool HasLocalAggregate()
        {
            if (m_exprNode != null)
            {
                return m_exprNode.HasLocalAggregate();
            }
            return false;
        }

        internal bool HasRemoteAggregate()
        {
            if (m_exprNode != null)
            {
                return m_exprNode.HasRemoteAggregate();
            }
            return false;
        }

        internal static bool ToBoolean(object value)
        {
            if (IsUnknown(value))
            {
                return false;
            }
            
            if (value is bool b)
            {
                return b;
            }
            
            if (value is int i)
            {
                return i == 1;
            }

            if (!(value is string))
            {
                throw new EvaluateException($"Expression value parse error. Cannot convert '{value}' to bool.");
            }

            try
            {
                return bool.Parse((string)value);
            }
            catch (Exception ex)
            {
                if (!ADP.IsCatchableExceptionType(ex))
                {
                    throw;
                }

                ExceptionBuilder.TraceExceptionForCapture(ex);
                    
                throw new EvaluateException($"Expression value parse error. Cannot convert '{value}' to bool.", ex);
            }
        }

        public void Dispose()
        {
            m_exprNode?.Dispose();
            m_exprNode = null;
        }
    }
}
