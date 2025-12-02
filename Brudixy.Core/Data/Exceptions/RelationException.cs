using System;

namespace Brudixy;

/// <summary>
/// Exception thrown when relation creation or validation fails
/// </summary>
public class RelationException : Exception
{
    public string RelationName { get; }
    public string[] Errors { get; }
    public List<string> AvailableTables { get; }

    /// <summary>
    /// Initializes a new instance of RelationException
    /// </summary>
    /// <param name="message">The error message</param>
    public RelationException(string message) : base(message)
    {
    }
    
    /// <summary>
    /// Initializes a new instance of RelationException
    /// </summary>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public RelationException(string message, Exception innerException) : base(message, innerException)
    {
    }

    public RelationException(string message, string relationName, string[] errors, List<string> availableTables) : base(message)
    {
        RelationName = relationName;
        Errors = errors;
        AvailableTables = availableTables;
    }
}
