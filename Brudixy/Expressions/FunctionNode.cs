using Brudixy.Exceptions;
using Konsarpoo.Collections;

namespace Brudixy.Expressions
{
    internal sealed class FunctionNode : ExpressionNode
    {
        internal readonly string Name;

        private readonly Function m_function;

        internal int ArgumentCount;
        
        internal Data<ExpressionNode> Arguments;

        internal bool IsAggregate => m_function.IsAggregate;

        public Function Function => m_function;

        internal FunctionNode(IExpressionDataSource table, string name)
          : base(table)
        {
            Name = name;

            m_function = table.GetFunctionRegistry().GetFunctionFactory(name)?.Invoke(FormatProvider);
            
            if (m_function == null)
            {
                throw ExprException.UndefinedFunction(this.Name);
            }
        }

        internal void AddArgument(ExpressionNode argument)
        {
            if (!m_function.IsVariantArgumentList && ArgumentCount >= m_function.argumentCount)
            {
                throw ExprException.FunctionArgumentCount(Name, m_function.argumentCount, ArgumentCount);
            }

            if (Arguments == null)
            {
                Arguments = new Data<ExpressionNode>();
            }
            
            Arguments.Add(argument);

            ArgumentCount++;
        }

        internal override void Mount(IExpressionDataSource table, Data<string> columns)
        {
            BindTable(table);

            Check();

            m_function.BindArguments(table, Arguments, columns);
        }

        internal override object Eval(int? row = null,
            IReadOnlyDictionary<string, object> testValues = null)
        {
            return m_function.Eval(table, Arguments,  row, testValues: testValues);
        }

        internal override object Eval(Data<int> recordNos)
        {
            throw ExprException.ComputeNotAggregate(ToString());
        }

        internal override bool IsConstant()
        {
            bool flag = true;
            for (int index = 0; index < ArgumentCount; ++index)
            {
                flag = flag && Arguments[index].IsConstant();
            }

            if (flag)
            {
                return m_function.UseRow == false; 
            }

            return false;
        }

        internal override bool IsTableConstant()
        {
            for (int index = 0; index < ArgumentCount; ++index)
            {
                if (!Arguments[index].IsTableConstant())
                {
                    return false;
                }
            }
            return true;
        }

        internal override bool HasLocalAggregate()
        {
            for (int index = 0; index < ArgumentCount; ++index)
            {
                if (Arguments[index].HasLocalAggregate())
                {
                    return true;
                }
            }
            return false;
        }

        internal override bool HasRemoteAggregate()
        {
            for (int index = 0; index < ArgumentCount; ++index)
            {
                if (Arguments[index].HasRemoteAggregate())
                {
                    return true;
                }
            }
            return false;
        }

        internal override bool DependsOn(string column)
        {
            for (int index = 0; index < ArgumentCount; ++index)
            {
                if (Arguments[index].DependsOn(column))
                {
                    return true;
                }
            }
            return false;
        }

        internal override ExpressionNode Optimize()
        {
            for (int index = 0; index < ArgumentCount; ++index)
            {
                Arguments[index] = Arguments[index].Optimize();
            }
            
            if (m_function.name == "In")
            {
                if (!IsConstant())
                {
                    throw ExprException.NonConstantArgument();
                }
            }
            else if (IsConstant())
            {
                if (table != null)
                {
                    return new ConstNode(table, ValueType.Object, Eval(), false);
                }
            }
            
            return this;
        }

        internal void Check()
        {
            if (m_function == null)
            {
                throw ExprException.UndefinedFunction(Name);
            }

            if (m_function.IsVariantArgumentList)
            {
                if (ArgumentCount >= m_function.argumentCount)
                {
                    return;
                }

                if (m_function.name == "In")
                {
                    throw ExprException.InWithoutList();
                }
                throw ExprException.FunctionArgumentCount(Name, m_function.argumentCount, ArgumentCount);
            }

            if (ArgumentCount != m_function.argumentCount)
            {
                throw ExprException.FunctionArgumentCount(Name, m_function.argumentCount, ArgumentCount);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            
            Arguments?.Dispose();

            Arguments = null;
        }
    }
}
