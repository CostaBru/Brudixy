using System;
using System.Linq;
using Brudixy.Interfaces.Generators;

namespace Brudixy.TypeGenerator.Core.Validation
{
    /// <summary>
    /// Represents parsed column type information including base type and modifiers.
    /// </summary>
    public class ColumnTypeInfo
    {
        /// <summary>
        /// Gets or sets the base type name (e.g., "String", "Int32", "MyCustomType").
        /// </summary>
        public string BaseType { get; set; }

        /// <summary>
        /// Gets or sets whether the type is nullable (? modifier).
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets or sets whether the type is explicitly non-null (! modifier).
        /// </summary>
        public bool IsNonNull { get; set; }

        /// <summary>
        /// Gets or sets whether the type is an array ([] modifier).
        /// </summary>
        public bool IsArray { get; set; }

        /// <summary>
        /// Gets or sets whether the type is a range (<> modifier).
        /// </summary>
        public bool IsRange { get; set; }

        /// <summary>
        /// Gets or sets whether the type is complex (Complex modifier).
        /// </summary>
        public bool IsComplex { get; set; }

        /// <summary>
        /// Gets or sets whether the type is a user-defined type.
        /// </summary>
        public bool IsUserType { get; set; }

        /// <summary>
        /// Gets or sets the maximum length for strings or arrays.
        /// </summary>
        public uint? MaxLength { get; set; }

        /// <summary>
        /// Gets or sets whether the column has an index.
        /// </summary>
        public bool HasIndex { get; set; }

        /// <summary>
        /// Gets or sets whether the column is auto-generated.
        /// </summary>
        public bool IsAuto { get; set; }

        /// <summary>
        /// Gets or sets whether the column is unique.
        /// </summary>
        public bool IsUnique { get; set; }

        /// <summary>
        /// Gets or sets whether the column is a service column.
        /// </summary>
        public bool IsService { get; set; }

        /// <summary>
        /// Gets or sets the expression for computed columns.
        /// </summary>
        public string Expression { get; set; }

        /// <summary>
        /// Parses a column type string into a ColumnTypeInfo object.
        /// </summary>
        /// <param name="typeString">The type string to parse (e.g., "String | 256 | Index").</param>
        /// <returns>A ColumnTypeInfo object with parsed information.</returns>
        public static ColumnTypeInfo Parse(string typeString)
        {
            if (string.IsNullOrWhiteSpace(typeString))
            {
                return new ColumnTypeInfo { BaseType = "String", IsNullable = true };
            }

            var info = new ColumnTypeInfo();
            var parts = typeString.Split('|').Select(s => s.Trim()).ToList();

            if (parts.Count == 0)
            {
                return info;
            }

            // Parse the type part (first element)
            var typePart = parts[0];

            // Check for both nullable and non-null modifiers before trimming
            var hasNullable = typePart.Contains("?");
            var hasNonNull = typePart.Contains("!");

            if (hasNullable)
            {
                info.IsNullable = true;
                typePart = typePart.Replace("?", "");
            }

            if (hasNonNull)
            {
                info.IsNonNull = true;
                typePart = typePart.Replace("!", "");
            }

            // Check for array modifier ([])
            if (typePart.Contains("[]"))
            {
                info.IsArray = true;
                typePart = typePart.Replace("[]", "");
            }

            // Check for range modifier (<>)
            if (typePart.Contains("<>"))
            {
                info.IsRange = true;
                typePart = typePart.Replace("<>", "");
            }

            // Check if it's a built-in type or user type
            if (BuiltinSupportStorageTypes.AliasMapTypes.ContainsKey(typePart) ||
                BuiltinSupportStorageTypes.KnownTypesToGenClassName.ContainsKey(typePart))
            {
                info.BaseType = typePart;
                info.IsUserType = false;
            }
            else
            {
                // Check if it's a struct (wrapped in parentheses)
                if (typePart.Length > 2 && typePart[0] == '(' && typePart[typePart.Length - 1] == ')')
                {
                    info.BaseType = typePart;
                    info.IsUserType = true;
                }
                else
                {
                    info.BaseType = typePart;
                    info.IsUserType = true;
                }
            }

            // Parse additional options
            foreach (var part in parts.Skip(1))
            {
                if (uint.TryParse(part, out var maxLen))
                {
                    info.MaxLength = maxLen;
                    continue;
                }

                switch (part)
                {
                    case "Complex":
                        info.IsComplex = true;
                        break;
                    case "Index":
                        info.HasIndex = true;
                        break;
                    case "Auto":
                        info.IsAuto = true;
                        break;
                    case "Nullable":
                        info.IsNullable = true;
                        break;
                    case "Not null":
                        info.IsNullable = false;
                        info.IsNonNull = true;
                        break;
                    case "Service":
                        info.IsService = true;
                        break;
                    case "Unique":
                        info.IsUnique = true;
                        break;
                    default:
                        // Check if it's an expression (quoted string)
                        if (part.Length >= 2 && part[0] == '\"' && part[part.Length - 1] == '\"')
                        {
                            info.Expression = part.Trim('\"');
                        }
                        break;
                }
            }

            return info;
        }

        /// <summary>
        /// Validates the column type information and returns any errors.
        /// </summary>
        /// <param name="error">The error message if validation fails.</param>
        /// <returns>True if valid, false otherwise.</returns>
        public bool IsValid(out string error)
        {
            error = null;

            // Check for conflicting nullability modifiers
            if (IsNullable && IsNonNull)
            {
                error = "Column type cannot have both nullable (?) and non-null (!) modifiers.";
                return false;
            }

            // Check for multiple structural modifiers
            var structuralModifierCount = 0;
            if (IsArray) structuralModifierCount++;
            if (IsRange) structuralModifierCount++;
            if (IsComplex) structuralModifierCount++;

            if (structuralModifierCount > 1)
            {
                error = "Column type can have at most one structural modifier (array [], range <>, or Complex).";
                return false;
            }

            // Check for empty base type
            if (string.IsNullOrWhiteSpace(BaseType))
            {
                error = "Column type must specify a base type.";
                return false;
            }

            return true;
        }

        /// <summary>
        /// Gets the count of structural modifiers (array, range, complex).
        /// </summary>
        public int StructuralModifierCount
        {
            get
            {
                var count = 0;
                if (IsArray) count++;
                if (IsRange) count++;
                if (IsComplex) count++;
                return count;
            }
        }
    }
}
