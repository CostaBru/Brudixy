using System.Linq;
using System.Text.RegularExpressions;

namespace Brudixy.TypeGenerator.Core.Validation.Rules
{
    /// <summary>
    /// Validates code generation options such as namespace, class names, and modifiers.
    /// </summary>
    public class CodeGenerationOptionsValidationRule : IValidationRule
    {
        public string RuleName => "CodeGenerationOptionsValidation";
        public int Priority => 40; // Run after basic validations

        // C# identifier pattern: starts with letter or underscore, followed by letters, digits, or underscores
        private static readonly Regex CSharpIdentifierPattern = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*$", RegexOptions.Compiled);
        
        // C# namespace pattern: identifiers separated by dots
        private static readonly Regex CSharpNamespacePattern = new Regex(@"^[a-zA-Z_][a-zA-Z0-9_]*(\.[a-zA-Z_][a-zA-Z0-9_]*)*$", RegexOptions.Compiled);

        public void Validate(ValidationContext context, ValidationResult result)
        {
            if (context.Table == null || context.Table.CodeGenerationOptions == null)
            {
                return;
            }

            var options = context.Table.CodeGenerationOptions;
            var filePath = context.FilePath;

            // Validate Namespace
            ValidateNamespace(options.Namespace, filePath, result);

            // Validate Class name
            ValidateClassName(options.Class, "Class", filePath, result);

            // Validate RowClass name
            ValidateClassName(options.RowClass, "RowClass", filePath, result);

            // Validate Abstract and Sealed are not both true
            ValidateAbstractSealed(options.Abstract, options.Sealed, filePath, result);

            // Validate ExtraUsing directives
            ValidateExtraUsing(options.ExtraUsing, filePath, result);

            // Validate BaseTableFileName if specified
            ValidateBaseTableFileName(options.BaseTableFileName, context, result);
        }

        private void ValidateNamespace(string namespaceName, string filePath, ValidationResult result)
        {
            // Namespace is optional - if not specified, it will be derived from the file path
            if (string.IsNullOrWhiteSpace(namespaceName))
            {
                return;
            }

            // Check for C# keywords in namespace segments first
            var segments = namespaceName.Split('.');
            var hasKeyword = false;
            foreach (var segment in segments)
            {
                if (IsCSharpKeyword(segment))
                {
                    result.AddWarning(new ValidationWarning(
                        filePath,
                        "CodeGenerationOptions.Namespace",
                        $"Namespace segment '{segment}' is a C# keyword. Consider using a different name."));
                    hasKeyword = true;
                }
            }

            // Only check pattern if no keywords (keywords are valid identifiers but warned about)
            if (!hasKeyword && !CSharpNamespacePattern.IsMatch(namespaceName))
            {
                result.AddError(new ValidationError(
                    filePath,
                    "CodeGenerationOptions.Namespace",
                    $"Namespace '{namespaceName}' does not follow valid C# namespace naming conventions.",
                    "Use only letters, digits, underscores, and dots. Each segment must start with a letter or underscore."));
            }
        }

        private void ValidateClassName(string className, string propertyName, string filePath, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(className))
            {
                result.AddError(new ValidationError(
                    filePath,
                    $"CodeGenerationOptions.{propertyName}",
                    $"{propertyName} is required but not specified.",
                    $"Specify a valid C# class name in CodeGenerationOptions.{propertyName}."));
                return;
            }

            if (!CSharpIdentifierPattern.IsMatch(className))
            {
                result.AddError(new ValidationError(
                    filePath,
                    $"CodeGenerationOptions.{propertyName}",
                    $"{propertyName} '{className}' does not follow valid C# identifier rules.",
                    "Use only letters, digits, and underscores. Must start with a letter or underscore."));
            }

            // Check if it's a C# keyword
            if (IsCSharpKeyword(className))
            {
                result.AddError(new ValidationError(
                    filePath,
                    $"CodeGenerationOptions.{propertyName}",
                    $"{propertyName} '{className}' is a C# keyword and cannot be used as a class name.",
                    $"Choose a different name for {propertyName}."));
            }
        }

        private void ValidateAbstractSealed(bool isAbstract, bool isSealed, string filePath, ValidationResult result)
        {
            if (isAbstract && isSealed)
            {
                result.AddError(new ValidationError(
                    filePath,
                    "CodeGenerationOptions",
                    "A class cannot be both Abstract and Sealed.",
                    "Set either Abstract or Sealed to false, or remove both properties."));
            }
        }

        private void ValidateExtraUsing(System.Collections.Generic.List<string> extraUsings, string filePath, ValidationResult result)
        {
            if (extraUsings == null || extraUsings.Count == 0)
            {
                return;
            }

            foreach (var usingDirective in extraUsings)
            {
                if (string.IsNullOrWhiteSpace(usingDirective))
                {
                    result.AddWarning(new ValidationWarning(
                        filePath,
                        "CodeGenerationOptions.ExtraUsing",
                        "ExtraUsing contains an empty or null entry."));
                    continue;
                }

                // Basic validation: should look like a namespace
                if (!CSharpNamespacePattern.IsMatch(usingDirective))
                {
                    result.AddWarning(new ValidationWarning(
                        filePath,
                        "CodeGenerationOptions.ExtraUsing",
                        $"ExtraUsing entry '{usingDirective}' does not appear to be a valid namespace format."));
                }
            }
        }

        private void ValidateBaseTableFileName(string baseTableFileName, ValidationContext context, ValidationResult result)
        {
            if (string.IsNullOrWhiteSpace(baseTableFileName))
            {
                return;
            }

            // Note: We don't validate file existence here because the path might be relative
            // and needs to be resolved by CodeGenerator. The CodeGenerator will handle
            // file loading errors with proper path resolution.
            
            // Just validate that it looks like a valid file path
            if (baseTableFileName.IndexOfAny(System.IO.Path.GetInvalidPathChars()) >= 0)
            {
                result.AddWarning(new ValidationWarning(
                    context.FilePath,
                    "CodeGenerationOptions.BaseTableFileName",
                    $"Base table file name '{baseTableFileName}' contains invalid path characters."));
            }
        }

        private bool IsCSharpKeyword(string identifier)
        {
            // C# keywords that cannot be used as identifiers
            var keywords = new[]
            {
                "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked",
                "class", "const", "continue", "decimal", "default", "delegate", "do", "double", "else",
                "enum", "event", "explicit", "extern", "false", "finally", "fixed", "float", "for",
                "foreach", "goto", "if", "implicit", "in", "int", "interface", "internal", "is", "lock",
                "long", "namespace", "new", "null", "object", "operator", "out", "override", "params",
                "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed",
                "short", "sizeof", "stackalloc", "static", "string", "struct", "switch", "this",
                "throw", "true", "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort",
                "using", "virtual", "void", "volatile", "while"
            };

            return keywords.Contains(identifier.ToLowerInvariant());
        }
    }
}
