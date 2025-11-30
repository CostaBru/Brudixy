using System;
using System.Collections.Generic;
using System.Linq;
using Brudixy.Serialization;

namespace Brudixy;

/// <summary>
/// Exception thrown when YAML schema validation fails
/// </summary>
public class SchemaValidationException : Exception
{
    /// <summary>
    /// Gets the validation errors that caused the exception
    /// </summary>
    public IReadOnlyList<ValidationError> Errors { get; }
    
    /// <summary>
    /// Gets the YAML content that failed validation
    /// </summary>
    public string YamlContent { get; }
    
    /// <summary>
    /// Initializes a new instance of SchemaValidationException
    /// </summary>
    /// <param name="errors">The validation errors</param>
    /// <param name="yamlContent">The YAML content that failed validation</param>
    public SchemaValidationException(IReadOnlyList<ValidationError> errors, string yamlContent)
        : base(BuildMessage(errors))
    {
        Errors = errors;
        YamlContent = yamlContent;
    }
    
    private static string BuildMessage(IReadOnlyList<ValidationError> errors)
    {
        if (errors == null || errors.Count == 0)
        {
            return "Schema validation failed";
        }
        
        var errorMessages = errors.Select(e => $"  - {e.Path}: {e.Message}");
        return $"Schema validation failed with {errors.Count} error(s):\n{string.Join("\n", errorMessages)}";
    }
}
