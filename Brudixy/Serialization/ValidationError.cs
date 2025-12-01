namespace Brudixy.Serialization;

/// <summary>
/// Represents a single validation error
/// </summary>
public class ValidationError
{
    /// <summary>
    /// Gets or sets the path to the property that failed validation
    /// </summary>
    public string Path { get; set; }
    
    /// <summary>
    /// Gets or sets the error message describing the validation failure
    /// </summary>
    public string Message { get; set; }
    
    /// <summary>
    /// Gets or sets the kind of validation error
    /// </summary>
    public string Kind { get; set; }
}
