using System.Linq;

namespace Brudixy.TypeGenerator.Core.Validation.Rules
{
    /// <summary>
    /// Validates column type definitions for correctness and consistency.
    /// </summary>
    public class ColumnTypeValidationRule : IValidationRule
    {
        public string RuleName => "ColumnTypeValidation";
        public int Priority => 10; // Run early

        public void Validate(ValidationContext context, ValidationResult result)
        {
            if (context.Table == null)
            {
                return;
            }

            // Validate each column type
            foreach (var column in context.Table.Columns)
            {
                var columnName = column.Key;
                var typeString = column.Value;

                // Parse the type string
                var typeInfo = ColumnTypeInfo.Parse(typeString);

                // Validate the parsed type
                if (!typeInfo.IsValid(out var error))
                {
                    result.AddError(new ValidationError(
                        context.FilePath,
                        $"Columns.{columnName}",
                        $"Invalid type definition for column '{columnName}': {error}",
                        "Check the type modifiers and ensure only one structural modifier is used."));
                    continue;
                }

                // Check for conflicting options in ColumnOptions
                if (context.Table.ColumnOptions.ContainsKey(columnName))
                {
                    var options = context.Table.ColumnOptions[columnName];

                    // Check for conflicting AllowNull with type modifiers
                    if (options.ContainsKey("AllowNull"))
                    {
                        var allowNull = DataTableObj.ConvertToBool(options["AllowNull"]);

                        if (allowNull == false && typeInfo.IsNullable)
                        {
                            result.AddError(new ValidationError(
                                context.FilePath,
                                $"ColumnOptions.{columnName}.AllowNull",
                                $"Column '{columnName}' has conflicting null settings: type is nullable (?) but AllowNull is false.",
                                "Either remove the ? modifier from the type or set AllowNull to true."));
                        }

                        if (allowNull == true && typeInfo.IsNonNull)
                        {
                            result.AddError(new ValidationError(
                                context.FilePath,
                                $"ColumnOptions.{columnName}.AllowNull",
                                $"Column '{columnName}' has conflicting null settings: type is non-null (!) but AllowNull is true.",
                                "Either remove the ! modifier from the type or set AllowNull to false."));
                        }
                    }

                    // Validate EnumType is specified when needed
                    if (options.ContainsKey("EnumType"))
                    {
                        var enumType = options["EnumType"] as string;
                        if (string.IsNullOrWhiteSpace(enumType))
                        {
                            result.AddWarning(new ValidationWarning(
                                context.FilePath,
                                $"ColumnOptions.{columnName}.EnumType",
                                $"Column '{columnName}' has an empty EnumType property."));
                        }
                    }
                }
            }

            // Validate ColumnInfo objects after EnsureDefaults
            foreach (var columnInfo in context.Table.ColumnObjects)
            {
                var columnName = columnInfo.Key;
                var info = columnInfo.Value;

                // Validate that enum columns have EnumType specified
                if (!string.IsNullOrEmpty(info.EnumType))
                {
                    // EnumType is specified, this is good
                    continue;
                }

                // Check if the type suggests it should be an enum but EnumType is missing
                // This is a heuristic check - we can't be 100% sure without more context
                if (info.Type == "Int32" || info.Type == "String")
                {
                    // These could be enums, but we can't be sure, so we don't warn
                    continue;
                }
            }
        }
    }
}
