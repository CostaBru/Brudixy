using System;
using System.Collections.Generic;

namespace Brudixy.TypeGenerator.Core
{
    public class BaseTableParseException : Exception
    {
        public List<string> Hierarchy { get; }
        
        public string FileName { get; }
        
        public Exception Exception { get; }

        public BaseTableParseException(List<string> hierarchy, string fileName, Exception exception) : base(exception.Message)
        {
            Hierarchy = hierarchy;
            FileName = fileName;
            Exception = exception;
        }

        public override string ToString()
        {
            var h = string.Join(Environment.NewLine, Hierarchy);
            
            return $"Parse base file parsing error. Error file name: {FileName}. Error: {Exception}. Hierarchy: {h}";
        }
    }
}