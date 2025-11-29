namespace Brudixy.TypeGenerator.Core.Validation
{
    /// <summary>
    /// Represents a validation error with context and suggested fix.
    /// </summary>
    public class ValidationError
    {
        /// <summary>
        /// Gets or sets the file path where the error occurred.
        /// </summary>
        public string FilePath { get; set; }

        /// <summary>
        /// Gets or sets the property path in dot notation (e.g., "ColumnOptions.id.AllowNull").
        /// </summary>
        public string PropertyPath { get; set; }

        /// <summary>
        /// Gets or sets the error message.
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Gets or sets a suggested fix for the error (optional).
        /// </summary>
        public string SuggestedFix { get; set; }

        /// <summary>
        /// Gets or sets the error code for categorization.
        /// </summary>
        public string ErrorCode { get; set; }

        public ValidationError()
        {
        }

        public ValidationError(string filePath, string message)
        {
            FilePath = filePath;
            Message = message;
        }

        public ValidationError(string filePath, string propertyPath, string message)
        {
            FilePath = filePath;
            PropertyPath = propertyPath;
            Message = message;
        }

        public ValidationError(string filePath, string propertyPath, string message, string suggestedFix)
        {
            FilePath = filePath;
            PropertyPath = propertyPath;
            Message = message;
            SuggestedFix = suggestedFix;
        }

        public override string ToString()
        {
            var parts = new System.Collections.Generic.List<string>();

            if (!string.IsNullOrEmpty(FilePath))
            {
                parts.Add($"Error in '{FilePath}'");
            }

            if (!string.IsNullOrEmpty(PropertyPath))
            {
                parts.Add($"at '{PropertyPath}'");
            }

            var location = parts.Count > 0 ? string.Join(" ", parts) + ":" : "Error:";

            var result = $"{location}\n  {Message}";

            if (!string.IsNullOrEmpty(SuggestedFix))
            {
                result += $"\n  Suggested fix: {SuggestedFix}";
            }

            return result;
        }
    }
}
