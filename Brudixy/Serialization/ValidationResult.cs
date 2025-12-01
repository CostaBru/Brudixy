using System.Collections.Generic;

namespace Brudixy.Serialization;

/// <summary>
/// Represents the result of a schema validation operation
/// </summary>
public class ValidationResult
{
    /// <summary>
    /// Gets or sets whether the validation succeeded
    /// </summary>
    public bool IsValid { get; set; }
    
    /// <summary>
    /// Gets or sets the list of validation errors (empty if IsValid is true)
    /// </summary>
    public List<ValidationError> Errors { get; set; } = new List<ValidationError>();
}
