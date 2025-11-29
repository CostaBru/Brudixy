namespace Brudixy.TypeGenerator.Core.Validation
{
    /// <summary>
    /// Represents a validation warning that does not prevent code generation.
    /// </summary>
    public class ValidationWarning
    {
        /// <summary>
        /// Gets or sets the file path where the warning occurred.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the property path in dot notation (e.g., "GroupedProperties.RecordCreator").
        /// </summary>
        public string PropertyPath { get; set; }

        /// <summary>
        /// Gets or sets the warning message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets the warning code for categorization.
        /// </summary>
        public string WarningCode { get; set; }

        public ValidationWarning()
        {
        }

        public ValidationWarning(string filePath, string message)
        {
            FilePath = filePath;
            Message = message;
        }

        public ValidationWarning(string filePath, string propertyPath, string message)
        {
            FilePath = filePath;
            PropertyPath = propertyPath;
            Message = message;
        }

        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(FilePath))
            {
                parts.Add($"Warning in '{FilePath}'");
            }

            if (!string.IsNullOrEmpty(PropertyPath))
            {
                parts.Add($"at '{PropertyPath}'");
            }

            var location = parts.Count > 0 ? string.Join(" ", parts) + ":" : "Warning:";

            return $"{location}\n  {Message}";
        }
    }
}
