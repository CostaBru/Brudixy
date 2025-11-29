using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Property-based tests for JSON Schema ColumnOptions validation.
    /// Tests that the ColumnOptions schema correctly validates column option structures.
    /// </summary>
    [TestFixture]
    public class JsonSchemaColumnOptionsTests
    {
        private JsonDocument _schemaDocument;
        private JsonElement _columnOptionsDefinition;
        private Regex _columnNamePattern;
        private Regex _csharpIdentifierPattern;

        [SetUp]
        public void SetUp()
        {
            // Load the JSON Schema - navigate from test directory to workspace root
            // Test directory is typically: D:\Dev\Brudixy\Brudixy.TypeGenerator.Tests\bin\Debug\net8.0
            // We need to go up 4 levels to reach workspace root (Brudixy.TypeGenerator.Tests folder)
            var testDir = TestContext.CurrentContext.TestDirectory;
            var workspaceRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
            var schemaPath = Path.Combine(workspaceRoot, "schemas", "brudixy-table-schema.json");
            
            var schemaJson = File.ReadAllText(schemaPath);
            _schemaDocument = JsonDocument.Parse(schemaJson);
            
            // Extract the columnOptions definition
            _columnOptionsDefinition = _schemaDocument.RootElement
                .GetProperty("definitions")
                .GetProperty("columnOptions");
            
            // Extract patterns
            _columnNamePattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
            _csharpIdentifierPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        }

        [TearDown]
        public void TearDown()
        {
            _schemaDocument?.Dispose();
        }

        // Feature: json-schema-definition, Property 4: ColumnOptions structure validation
        // Validates: Requirements 4.3, 4.4, 4.5
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(ColumnOptionsGenerators) })]
        public Property ValidColumnOptions_PassValidation(ValidColumnOptions validOptions)
        {
            // Act
            var isValid = ValidateColumnOptions(validOptions.Value, out var errors);

            // Assert
            return isValid.Label($"Valid ColumnOptions should pass validation. Errors: {string.Join(", ", errors)}");
        }

        // Feature: json-schema-definition, Property 4: ColumnOptions structure validation
        // Validates: Requirements 4.3, 4.4, 4.5
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(ColumnOptionsGenerators) })]
        public Property InvalidColumnOptions_FailValidation(InvalidColumnOptions invalidOptions)
        {
            // Act
            var isValid = ValidateColumnOptionsJson(invalidOptions.Value, out var errors);

            // Assert
            return (!isValid).Label($"Invalid ColumnOptions should fail validation: {invalidOptions.Value}");
        }

        private bool ValidateColumnOptions(Dictionary<string, Dictionary<string, object>> columnOptions, out List<string> errors)
        {
            errors = new List<string>();

            foreach (var kvp in columnOptions)
            {
                var columnName = kvp.Key;
                var options = kvp.Value;

                // Validate column name matches pattern
                if (!_columnNamePattern.IsMatch(columnName))
                {
                    errors.Add($"Column name '{columnName}' does not match pattern");
                    return false;
                }

                // Validate each property
                foreach (var optKvp in options)
                {
                    var propName = optKvp.Key;
                    var propValue = optKvp.Value;

                    switch (propName)
                    {
                        case "AllowNull":
                        case "IsUnique":
                        case "HasIndex":
                        case "IsReadOnly":
                        case "IsService":
                        case "Auto":
                            if (propValue is not bool)
                            {
                                errors.Add($"Property '{propName}' must be boolean");
                                return false;
                            }
                            break;

                        case "MaxLength":
                            if (propValue is not int maxLen || maxLen < 1)
                            {
                                errors.Add($"Property 'MaxLength' must be integer >= 1");
                                return false;
                            }
                            break;

                        case "CodeProperty":
                            if (propValue is not string codeProperty || !_csharpIdentifierPattern.IsMatch(codeProperty))
                            {
                                errors.Add($"Property 'CodeProperty' must be valid C# identifier");
                                return false;
                            }
                            break;

                        case "Type":
                        case "DataType":
                        case "Expression":
                        case "DefaultValue":
                        case "DisplayName":
                        case "EnumType":
                            if (propValue is not string)
                            {
                                errors.Add($"Property '{propName}' must be string");
                                return false;
                            }
                            break;

                        case "XProperties":
                            // XProperties validation would go here
                            break;

                        default:
                            errors.Add($"Unknown property '{propName}'");
                            return false;
                    }
                }
            }

            return true;
        }

        private bool ValidateColumnOptionsJson(string json, out List<string> errors)
        {
            errors = new List<string>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("ColumnOptions must be an object");
                    return false;
                }

                foreach (var property in root.EnumerateObject())
                {
                    var columnName = property.Name;

                    // Validate column name
                    if (!_columnNamePattern.IsMatch(columnName))
                    {
                        errors.Add($"Column name '{columnName}' does not match pattern");
                        return false;
                    }

                    if (property.Value.ValueKind != JsonValueKind.Object)
                    {
                        errors.Add($"Column options for '{columnName}' must be an object");
                        return false;
                    }

                    // Validate properties
                    foreach (var optProperty in property.Value.EnumerateObject())
                    {
                        var propName = optProperty.Name;
                        var propValue = optProperty.Value;

                        switch (propName)
                        {
                            case "AllowNull":
                            case "IsUnique":
                            case "HasIndex":
                            case "IsReadOnly":
                            case "IsService":
                            case "Auto":
                                if (propValue.ValueKind != JsonValueKind.True && propValue.ValueKind != JsonValueKind.False)
                                {
                                    errors.Add($"Property '{propName}' must be boolean");
                                    return false;
                                }
                                break;

                            case "MaxLength":
                                if (propValue.ValueKind != JsonValueKind.Number || !propValue.TryGetInt32(out var maxLen) || maxLen < 1)
                                {
                                    errors.Add($"Property 'MaxLength' must be integer >= 1");
                                    return false;
                                }
                                break;

                            case "CodeProperty":
                                if (propValue.ValueKind != JsonValueKind.String)
                                {
                                    errors.Add($"Property 'CodeProperty' must be string");
                                    return false;
                                }
                                var codeProperty = propValue.GetString();
                                if (!_csharpIdentifierPattern.IsMatch(codeProperty))
                                {
                                    errors.Add($"Property 'CodeProperty' must be valid C# identifier");
                                    return false;
                                }
                                break;

                            case "Type":
                            case "DataType":
                            case "Expression":
                            case "DefaultValue":
                            case "DisplayName":
                            case "EnumType":
                                if (propValue.ValueKind != JsonValueKind.String)
                                {
                                    errors.Add($"Property '{propName}' must be string");
                                    return false;
                                }
                                break;

                            case "XProperties":
                                if (propValue.ValueKind != JsonValueKind.Object)
                                {
                                    errors.Add($"Property 'XProperties' must be object");
                                    return false;
                                }
                                break;

                            default:
                                errors.Add($"Unknown property '{propName}'");
                                return false;
                        }
                    }
                }

                return true;
            }
            catch (JsonException ex)
            {
                errors.Add($"Invalid JSON: {ex.Message}");
                return false;
            }
        }

        [Test]
        public void ColumnOptions_WithAllProperties_IsValid()
        {
            var columnOptions = new Dictionary<string, Dictionary<string, object>>
            {
                ["id"] = new Dictionary<string, object>
                {
                    ["Type"] = "Int32",
                    ["AllowNull"] = false,
                    ["IsUnique"] = true,
                    ["HasIndex"] = true,
                    ["IsReadOnly"] = false,
                    ["IsService"] = false,
                    ["Auto"] = true,
                    ["MaxLength"] = 100,
                    ["DataType"] = "System.Int32",
                    ["Expression"] = "id + 1",
                    ["DefaultValue"] = "0",
                    ["DisplayName"] = "ID",
                    ["CodeProperty"] = "Id",
                    ["EnumType"] = "MyEnum"
                }
            };

            var isValid = ValidateColumnOptions(columnOptions, out var errors);

            Assert.IsTrue(isValid, $"ColumnOptions with all properties should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void ColumnOptions_WithBooleanProperties_IsValid()
        {
            var columnOptions = new Dictionary<string, Dictionary<string, object>>
            {
                ["name"] = new Dictionary<string, object>
                {
                    ["AllowNull"] = true,
                    ["IsUnique"] = false,
                    ["HasIndex"] = true
                }
            };

            var isValid = ValidateColumnOptions(columnOptions, out var errors);

            Assert.IsTrue(isValid, $"ColumnOptions with boolean properties should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void ColumnOptions_WithInvalidCodeProperty_IsInvalid()
        {
            var columnOptionsJson = @"{
                ""id"": {
                    ""CodeProperty"": ""123Invalid""
                }
            }";

            var isValid = ValidateColumnOptionsJson(columnOptionsJson, out var errors);

            Assert.IsFalse(isValid, "ColumnOptions with invalid CodeProperty should be invalid");
        }

        [Test]
        public void ColumnOptions_WithInvalidMaxLength_IsInvalid()
        {
            var columnOptionsJson = @"{
                ""name"": {
                    ""MaxLength"": 0
                }
            }";

            var isValid = ValidateColumnOptionsJson(columnOptionsJson, out var errors);

            Assert.IsFalse(isValid, "ColumnOptions with MaxLength 0 should be invalid");
        }

        [Test]
        public void ColumnOptions_WithInvalidPropertyType_IsInvalid()
        {
            var columnOptionsJson = @"{
                ""id"": {
                    ""AllowNull"": ""not a boolean""
                }
            }";

            var isValid = ValidateColumnOptionsJson(columnOptionsJson, out var errors);

            Assert.IsFalse(isValid, "ColumnOptions with wrong property type should be invalid");
        }

        [Test]
        public void ColumnOptions_WithInvalidColumnName_IsInvalid()
        {
            var columnOptionsJson = @"{
                ""123invalid"": {
                    ""AllowNull"": true
                }
            }";

            var isValid = ValidateColumnOptionsJson(columnOptionsJson, out var errors);

            Assert.IsFalse(isValid, "ColumnOptions with invalid column name should be invalid");
        }
    }

    /// <summary>
    /// Wrapper for valid ColumnOptions
    /// </summary>
    public class ValidColumnOptions
    {
        public Dictionary<string, Dictionary<string, object>> Value { get; set; }

        public ValidColumnOptions(Dictionary<string, Dictionary<string, object>> value)
        {
            Value = value;
        }

        public override string ToString() => JsonSerializer.Serialize(Value);
    }

    /// <summary>
    /// Wrapper for invalid ColumnOptions
    /// </summary>
    public class InvalidColumnOptions
    {
        public string Value { get; set; }

        public InvalidColumnOptions(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// FsCheck generators for ColumnOptions
    /// </summary>
    public static class ColumnOptionsGenerators
    {
        private static readonly string[] ValidColumnNames = new[]
        {
            "id", "name", "createdt", "creatorid", "lmdt", "isdeleted", "guid", "type"
        };

        private static readonly string[] ValidTypes = new[]
        {
            "Int32", "String", "DateTime", "Boolean", "Guid"
        };

        public static Arbitrary<ValidColumnOptions> ValidColumnOptionsArbitrary()
        {
            var genColumnName = Gen.Elements(ValidColumnNames);
            
            var genColumnOption = from allowNull in Arb.Generate<bool>()
                                  from isUnique in Arb.Generate<bool>()
                                  from hasIndex in Arb.Generate<bool>()
                                  from isReadOnly in Arb.Generate<bool>()
                                  from type in Gen.Elements(ValidTypes)
                                  select new Dictionary<string, object>
                                  {
                                      ["AllowNull"] = allowNull,
                                      ["IsUnique"] = isUnique,
                                      ["HasIndex"] = hasIndex,
                                      ["IsReadOnly"] = isReadOnly,
                                      ["Type"] = type
                                  };

            var genColumnOptions = from numColumns in Gen.Choose(1, 3)
                                   from columns in Gen.ArrayOf(numColumns, genColumnName).Select(arr => arr.Distinct().ToArray())
                                   from options in Gen.ArrayOf(columns.Length, genColumnOption)
                                   select columns.Zip(options, (name, opt) => new { name, opt })
                                                 .ToDictionary(x => x.name, x => x.opt);

            return Arb.From(genColumnOptions.Select(opts => new ValidColumnOptions(opts)));
        }

        public static Arbitrary<InvalidColumnOptions> InvalidColumnOptionsArbitrary()
        {
            // Invalid column name (starts with number)
            var genInvalidColumnName = Gen.Constant(@"{
                ""123invalid"": {
                    ""AllowNull"": true
                }
            }");

            // Invalid property type (string instead of boolean)
            var genInvalidPropertyType = Gen.Constant(@"{
                ""id"": {
                    ""AllowNull"": ""not a boolean""
                }
            }");

            // Invalid MaxLength (zero or negative)
            var genInvalidMaxLength = Gen.Constant(@"{
                ""name"": {
                    ""MaxLength"": 0
                }
            }");

            // Invalid CodeProperty (starts with number)
            var genInvalidCodeProperty = Gen.Constant(@"{
                ""id"": {
                    ""CodeProperty"": ""123Invalid""
                }
            }");

            // Invalid additional property
            var genInvalidAdditionalProperty = Gen.Constant(@"{
                ""id"": {
                    ""InvalidProperty"": ""value""
                }
            }");

            var genInvalidOptions = Gen.OneOf(
                genInvalidColumnName,
                genInvalidPropertyType,
                genInvalidMaxLength,
                genInvalidCodeProperty,
                genInvalidAdditionalProperty
            );

            return Arb.From(genInvalidOptions.Select(s => new InvalidColumnOptions(s)));
        }
    }
}
