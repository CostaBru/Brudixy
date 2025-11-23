namespace Brudixy.Expressions
{
    internal sealed class OperatorInfo
    {
        internal Nodes type;
        internal int op;
        internal int priority;

        internal OperatorInfo(Nodes type, int op, int pri)
        {
            this.type = type;
            this.op = op;
            priority = pri;
        }
    }
}
