using System.Diagnostics;

namespace Brudixy.Expressions
{
    internal static class ExceptionBuilder
    {
        private static void TraceException(string trace, Exception e)
        {
            if (e == null)
            {
                return;
            }
            
            if (Trace.Listeners.Count > 0)
            {
                Trace.WriteLine($"{trace}. {e.StackTrace}");
            }
        }

        internal static void TraceExceptionAsReturnValue(Exception e)
        {
            TraceException("THROW", e);
        }

        internal static void TraceExceptionForCapture(Exception e)
        {
            TraceException("CATCH", e);
        }

        internal static void TraceExceptionWithoutRethrow(Exception e)
        {
            TraceException("CATCH", e);
        }

        internal static ArgumentOutOfRangeException _ArgumentOutOfRange(string paramName, string msg)
        {
            ArgumentOutOfRangeException ofRangeException = new ArgumentOutOfRangeException(paramName, msg);
            TraceExceptionAsReturnValue(ofRangeException);
            return ofRangeException;
        }
    }
}