using System;
using System.Runtime.Serialization;

namespace Brudixy.Serialization;

/// <summary>
/// Exception thrown when a JSON schema file cannot be found
/// </summary>
[Serializable]
public class SchemaNotFoundException : Exception
{
    /// <summary>
    /// Gets the path to the schema file that was not found
    /// </summary>
    public string SchemaFilePath { get; }
    
    /// <summary>
    /// Initializes a new instance of SchemaNotFoundException
    /// </summary>
    /// <param name="schemaFilePath">The path to the schema file that was not found</param>
    public SchemaNotFoundException(string schemaFilePath)
        : base($"JSON schema file not found: {schemaFilePath}. " +
               $"Please ensure the schema file exists or use the default embedded schema.")
    {
        SchemaFilePath = schemaFilePath;
    }
    
    /// <summary>
    /// Initializes a new instance of SchemaNotFoundException with a custom message
    /// </summary>
    /// <param name="schemaFilePath">The path to the schema file that was not found</param>
    /// <param name="message">The error message</param>
    public SchemaNotFoundException(string schemaFilePath, string message)
        : base(message)
    {
        SchemaFilePath = schemaFilePath;
    }
    
    /// <summary>
    /// Initializes a new instance of SchemaNotFoundException with a custom message and inner exception
    /// </summary>
    /// <param name="schemaFilePath">The path to the schema file that was not found</param>
    /// <param name="message">The error message</param>
    /// <param name="innerException">The inner exception</param>
    public SchemaNotFoundException(string schemaFilePath, string message, Exception innerException)
        : base(message, innerException)
    {
        SchemaFilePath = schemaFilePath;
    }
    
    protected SchemaNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
        SchemaFilePath = info.GetString(nameof(SchemaFilePath));
    }
    
    public override void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        base.GetObjectData(info, context);
        info.AddValue(nameof(SchemaFilePath), SchemaFilePath);
    }
}
