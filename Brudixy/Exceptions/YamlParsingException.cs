using System;
using System.Runtime.Serialization;

namespace Brudixy;

/// <summary>
/// Exception thrown when YAML content is malformed and cannot be parsed
/// </summary>
[Serializable]
public class YamlParsingException : Exception
{
    /// <summary>
    /// Gets the line number where the parsing error occurred
    /// </summary>
    public int? LineNumber { get; }
    
    /// <summary>
    /// Gets the column number where the parsing error occurred
    /// </summary>
    public int? ColumnNumber { get; }
    
    /// <summary>
    /// Gets the YAML content that failed to parse
    /// </summary>
    public string YamlContent { get; }
    
    /// <summary>
    /// Initializes a new instance of YamlParsingException
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="yamlContent">The YAML content that failed to parse</param>
    public YamlParsingException(string message, string yamlContent)
        : base(message)
    {
        YamlContent = yamlContent;
    }
    
    /// <summary>
    /// Initializes a new instance of YamlParsingException with line and column information
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="yamlContent">The YAML content that failed to parse</param>
    /// <param name="lineNumber">The line number where the error occurred</param>
    /// <param name="columnNumber">The column number where the error occurred</param>
    public YamlParsingException(string message, string yamlContent, int lineNumber, int columnNumber)
        : base($"{message} at line {lineNumber}, column {columnNumber}")
    {
        YamlContent = yamlContent;
        LineNumber = lineNumber;
        ColumnNumber = columnNumber;
    }
    
    /// <summary>
    /// Initializes a new instance of YamlParsingException with an inner exception
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="yamlContent">The YAML content that failed to parse</param>
    /// <param name="innerException">The inner exception</param>
    public YamlParsingException(string message, string yamlContent, Exception innerException)
        : base(message, innerException)
    {
        YamlContent = yamlContent;
    }
    
    protected YamlParsingException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        YamlContent = info.GetString(nameof(YamlContent));
        LineNumber = (int?)info.GetValue(nameof(LineNumber), typeof(int?));
        ColumnNumber = (int?)info.GetValue(nameof(ColumnNumber), typeof(int?));
    }
    
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(YamlContent), YamlContent);
        info.AddValue(nameof(LineNumber), LineNumber);
        info.AddValue(nameof(ColumnNumber), ColumnNumber);
    }
}
