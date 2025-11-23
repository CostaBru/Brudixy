using System.Security;

namespace Brudixy.Expressions
{
    internal static class ADP
    {
        private static readonly Type StackOverflowType = typeof(StackOverflowException);

        private static readonly Type OutOfMemoryType = typeof(OutOfMemoryException);

        private static readonly Type ThreadAbortType = typeof(ThreadAbortException);

        private static readonly Type NullReferenceType = typeof(NullReferenceException);

        private static readonly Type AccessViolationType = typeof(AccessViolationException);

        private static readonly Type SecurityType = typeof(SecurityException);

        internal static bool IsCatchableExceptionType(Exception e)
        {
            Type type = e.GetType();
            if (type != StackOverflowType && type != OutOfMemoryType && (type != ThreadAbortType && type != NullReferenceType) && type != AccessViolationType)
                return !SecurityType.IsAssignableFrom(type);
            return false;
        }
      
    }
}