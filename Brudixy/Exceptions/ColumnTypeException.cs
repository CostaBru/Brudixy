using System;
using System.Runtime.Serialization;

namespace Brudixy;

/// <summary>
/// Exception thrown when a column type in YAML cannot be mapped to a CoreDataTable type
/// </summary>
[Serializable]
public class ColumnTypeException : Exception
{
    /// <summary>
    /// Gets the name of the column with the invalid type
    /// </summary>
    public string ColumnName { get; }
    
    /// <summary>
    /// Gets the invalid type string from the YAML
    /// </summary>
    public string InvalidType { get; }
    
    /// <summary>
    /// Initializes a new instance of ColumnTypeException
    /// </summary>
    /// <param name="columnName">The name of the column with the invalid type</param>
    /// <param name="invalidType">The invalid type string</param>
    public ColumnTypeException(string columnName, string invalidType)
        : base(BuildMessage(columnName, invalidType))
    {
        ColumnName = columnName;
        InvalidType = invalidType;
    }
    
    /// <summary>
    /// Initializes a new instance of ColumnTypeException with a custom message
    /// </summary>
    /// <param name="columnName">The name of the column with the invalid type</param>
    /// <param name="invalidType">The invalid type string</param>
    /// <param name="message">The error message</param>
    public ColumnTypeException(string columnName, string invalidType, string message)
        : base(message)
    {
        ColumnName = columnName;
        InvalidType = invalidType;
    }
    
    /// <summary>
    /// Initializes a new instance of ColumnTypeException with an inner exception
    /// </summary>
    /// <param name="columnName">The name of the column with the invalid type</param>
    /// <param name="invalidType">The invalid type string</param>
    /// <param name="innerException">The inner exception</param>
    public ColumnTypeException(string columnName, string invalidType, Exception innerException)
        : base(BuildMessage(columnName, invalidType), innerException)
    {
        ColumnName = columnName;
        InvalidType = invalidType;
    }
    
    protected ColumnTypeException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        ColumnName = info.GetString(nameof(ColumnName));
        InvalidType = info.GetString(nameof(InvalidType));
    }
    
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(ColumnName), ColumnName);
        info.AddValue(nameof(InvalidType), InvalidType);
    }
    
    private static string BuildMessage(string columnName, string invalidType)
    {
        return $"Column '{columnName}' has invalid type '{invalidType}'. " +
               $"Valid types include: Int32, String, DateTime, Boolean, Guid, Decimal, " +
               $"array types (e.g., Int32[]), range types (e.g., Int32<>), and nullable types (e.g., String?).";
    }
}
