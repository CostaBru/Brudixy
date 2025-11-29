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
    /// Property-based tests for JSON Schema GroupedProperties validation.
    /// Tests the GroupedProperties and GroupedPropertyOptions definitions in the JSON Schema.
    /// </summary>
    [TestFixture]
    public class JsonSchemaGroupedPropertiesTests
    {
        private JsonDocument _schemaDocument;
        private JsonElement _groupedPropertiesDefinition;
        private JsonElement _groupedPropertyOptionsDefinition;
        private Regex _identifierPattern;
        private Regex _groupedPropertyPattern;

        [SetUp]
        public void SetUp()
        {
            // Load the JSON Schema
            var testDir = TestContext.CurrentContext.TestDirectory;
            var workspaceRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
            var schemaPath = Path.Combine(workspaceRoot, "schemas", "brudixy-table-schema.json");
            
            var schemaJson = File.ReadAllText(schemaPath);
            _schemaDocument = JsonDocument.Parse(schemaJson);
            
            // Extract the groupedProperties and groupedPropertyOptions definitions
            _groupedPropertiesDefinition = _schemaDocument.RootElement
                .GetProperty("definitions")
                .GetProperty("groupedProperties");
            
            _groupedPropertyOptionsDefinition = _schemaDocument.RootElement
                .GetProperty("definitions")
                .GetProperty("groupedPropertyOptions");
            
            // Extract patterns
            _identifierPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
            _groupedPropertyPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*(\|[A-Za-z_][A-Za-z0-9_]*)+$", RegexOptions.Compiled);
        }

        [TearDown]
        public void TearDown()
        {
            _schemaDocument?.Dispose();
        }

        // Feature: json-schema-definition, Property 7: GroupedProperty format validation
        // Validates: Requirements 6.1, 6.2, 6.3, 6.4
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(GroupedPropertiesGenerators) })]
        public Property ValidGroupedProperties_PassesValidation(ValidGroupedProperties validGroupedProperties)
        {
            // Act
            var isValid = ValidateGroupedProperties(validGroupedProperties.Value, out var errors);

            // Assert
            return isValid.Label($"Valid GroupedProperties should pass validation. Errors: {string.Join(", ", errors)}");
        }

        // Feature: json-schema-definition, Property 7: GroupedProperty format validation
        // Validates: Requirements 6.1, 6.2, 6.3, 6.4
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(GroupedPropertiesGenerators) })]
        public Property InvalidGroupedProperties_FailsValidation(InvalidGroupedProperties invalidGroupedProperties)
        {
            // Act
            var isValid = ValidateGroupedPropertiesJson(invalidGroupedProperties.Value, out var errors);

            // Assert
            return (!isValid).Label($"Invalid GroupedProperties should fail validation: {invalidGroupedProperties.Value}");
        }

        // Feature: json-schema-definition, Property 7: GroupedProperty format validation
        // Validates: Requirements 6.1, 6.2, 6.3, 6.4
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(GroupedPropertiesGenerators) })]
        public Property ValidGroupedPropertyOptions_PassesValidation(ValidGroupedPropertyOptions validOptions)
        {
            // Act
            var isValid = ValidateGroupedPropertyOptions(validOptions.Value, out var errors);

            // Assert
            return isValid.Label($"Valid GroupedPropertyOptions should pass validation. Errors: {string.Join(", ", errors)}");
        }

        private bool ValidateGroupedProperties(Dictionary<string, string> groupedProperties, out List<string> errors)
        {
            errors = new List<string>();

            foreach (var kvp in groupedProperties)
            {
                var groupName = kvp.Key;
                var columnList = kvp.Value;

                // Validate group name matches identifier pattern
                if (!_identifierPattern.IsMatch(groupName))
                {
                    errors.Add($"Group name '{groupName}' does not match C# identifier pattern");
                    return false;
                }

                // Validate column list matches pattern (minimum 2 columns)
                if (!_groupedPropertyPattern.IsMatch(columnList))
                {
                    errors.Add($"Column list '{columnList}' does not match pattern (must be pipe-separated with at least 2 columns)");
                    return false;
                }

                // Validate each column name is a valid identifier
                var columns = columnList.Split('|');
                foreach (var column in columns)
                {
                    if (!_identifierPattern.IsMatch(column))
                    {
                        errors.Add($"Column name '{column}' in group '{groupName}' is not a valid identifier");
                        return false;
                    }
                }
            }

            return true;
        }

        private bool ValidateGroupedPropertiesJson(string json, out List<string> errors)
        {
            errors = new List<string>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("GroupedProperties must be an object");
                    return false;
                }

                foreach (var prop in root.EnumerateObject())
                {
                    var groupName = prop.Name;
                    var columnList = prop.Value;

                    // Validate group name matches identifier pattern
                    if (!_identifierPattern.IsMatch(groupName))
                    {
                        errors.Add($"Group name '{groupName}' does not match C# identifier pattern");
                        return false;
                    }

                    // Validate column list is a string
                    if (columnList.ValueKind != JsonValueKind.String)
                    {
                        errors.Add($"Column list for group '{groupName}' must be a string");
                        return false;
                    }

                    var columnListStr = columnList.GetString();

                    // Validate column list matches pattern (minimum 2 columns)
                    if (!_groupedPropertyPattern.IsMatch(columnListStr))
                    {
                        errors.Add($"Column list '{columnListStr}' does not match pattern (must be pipe-separated with at least 2 columns)");
                        return false;
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

        private bool ValidateGroupedPropertyOptions(Dictionary<string, GroupedPropertyOption> options, out List<string> errors)
        {
            errors = new List<string>();

            foreach (var kvp in options)
            {
                var groupName = kvp.Key;
                var option = kvp.Value;

                // Validate group name matches identifier pattern
                if (!_identifierPattern.IsMatch(groupName))
                {
                    errors.Add($"Group name '{groupName}' does not match C# identifier pattern");
                    return false;
                }

                // Validate Type if present
                if (option.Type != null)
                {
                    if (option.Type != "Tuple" && option.Type != "NewStruct")
                    {
                        errors.Add($"Type for group '{groupName}' must be 'Tuple' or 'NewStruct', got '{option.Type}'");
                        return false;
                    }
                }

                // Validate StructName if present
                if (option.StructName != null)
                {
                    if (!_identifierPattern.IsMatch(option.StructName))
                    {
                        errors.Add($"StructName '{option.StructName}' for group '{groupName}' does not match C# identifier pattern");
                        return false;
                    }
                }
            }

            return true;
        }

        [Test]
        public void GroupedProperties_WithTwoColumns_PassesValidation()
        {
            var groupedProperties = new Dictionary<string, string>
            {
                ["Point"] = "X|Y"
            };

            var isValid = ValidateGroupedProperties(groupedProperties, out var errors);
            Assert.IsTrue(isValid, $"GroupedProperties with 2 columns should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void GroupedProperties_WithThreeColumns_PassesValidation()
        {
            var groupedProperties = new Dictionary<string, string>
            {
                ["Location"] = "X|Y|Z"
            };

            var isValid = ValidateGroupedProperties(groupedProperties, out var errors);
            Assert.IsTrue(isValid, $"GroupedProperties with 3 columns should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void GroupedProperties_WithMultipleGroups_PassesValidation()
        {
            var groupedProperties = new Dictionary<string, string>
            {
                ["Point"] = "X|Y",
                ["Location"] = "Latitude|Longitude",
                ["FullName"] = "FirstName|LastName"
            };

            var isValid = ValidateGroupedProperties(groupedProperties, out var errors);
            Assert.IsTrue(isValid, $"GroupedProperties with multiple groups should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void GroupedProperties_WithSingleColumn_FailsValidation()
        {
            var json = @"{""Point"": ""X""}";

            var isValid = ValidateGroupedPropertiesJson(json, out var errors);
            Assert.IsFalse(isValid, "GroupedProperties with single column should be invalid");
        }

        [Test]
        public void GroupedProperties_WithInvalidGroupName_FailsValidation()
        {
            var json = @"{""123Invalid"": ""X|Y""}";

            var isValid = ValidateGroupedPropertiesJson(json, out var errors);
            Assert.IsFalse(isValid, "GroupedProperties with invalid group name should be invalid");
        }

        [Test]
        public void GroupedProperties_WithInvalidColumnName_FailsValidation()
        {
            var groupedProperties = new Dictionary<string, string>
            {
                ["Point"] = "X|123Invalid"
            };

            var isValid = ValidateGroupedProperties(groupedProperties, out var errors);
            Assert.IsFalse(isValid, "GroupedProperties with invalid column name should be invalid");
        }

        [Test]
        public void GroupedProperties_EmptyMap_PassesValidation()
        {
            var groupedProperties = new Dictionary<string, string>();

            var isValid = ValidateGroupedProperties(groupedProperties, out var errors);
            Assert.IsTrue(isValid, $"Empty GroupedProperties should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void GroupedPropertyOptions_WithTupleType_PassesValidation()
        {
            var options = new Dictionary<string, GroupedPropertyOption>
            {
                ["Point"] = new GroupedPropertyOption { Type = "Tuple", IsReadOnly = true }
            };

            var isValid = ValidateGroupedPropertyOptions(options, out var errors);
            Assert.IsTrue(isValid, $"GroupedPropertyOptions with Tuple type should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void GroupedPropertyOptions_WithNewStructType_PassesValidation()
        {
            var options = new Dictionary<string, GroupedPropertyOption>
            {
                ["Point"] = new GroupedPropertyOption 
                { 
                    Type = "NewStruct", 
                    StructName = "Point2D",
                    IsReadOnly = false
                }
            };

            var isValid = ValidateGroupedPropertyOptions(options, out var errors);
            Assert.IsTrue(isValid, $"GroupedPropertyOptions with NewStruct type should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void GroupedPropertyOptions_WithInvalidType_FailsValidation()
        {
            var options = new Dictionary<string, GroupedPropertyOption>
            {
                ["Point"] = new GroupedPropertyOption { Type = "InvalidType" }
            };

            var isValid = ValidateGroupedPropertyOptions(options, out var errors);
            Assert.IsFalse(isValid, "GroupedPropertyOptions with invalid Type should be invalid");
        }

        [Test]
        public void GroupedPropertyOptions_WithInvalidStructName_FailsValidation()
        {
            var options = new Dictionary<string, GroupedPropertyOption>
            {
                ["Point"] = new GroupedPropertyOption 
                { 
                    Type = "NewStruct", 
                    StructName = "123Invalid"
                }
            };

            var isValid = ValidateGroupedPropertyOptions(options, out var errors);
            Assert.IsFalse(isValid, "GroupedPropertyOptions with invalid StructName should be invalid");
        }

        [Test]
        public void GroupedPropertyOptions_WithInvalidGroupName_FailsValidation()
        {
            var options = new Dictionary<string, GroupedPropertyOption>
            {
                ["123Invalid"] = new GroupedPropertyOption { Type = "Tuple" }
            };

            var isValid = ValidateGroupedPropertyOptions(options, out var errors);
            Assert.IsFalse(isValid, "GroupedPropertyOptions with invalid group name should be invalid");
        }

        [Test]
        public void GroupedPropertyOptions_EmptyMap_PassesValidation()
        {
            var options = new Dictionary<string, GroupedPropertyOption>();

            var isValid = ValidateGroupedPropertyOptions(options, out var errors);
            Assert.IsTrue(isValid, $"Empty GroupedPropertyOptions should be valid. Errors: {string.Join(", ", errors)}");
        }
    }

    /// <summary>
    /// Represents a grouped property option
    /// </summary>
    public class GroupedPropertyOption
    {
        public string Type { get; set; }
        public bool? IsReadOnly { get; set; }
        public string StructName { get; set; }
    }

    /// <summary>
    /// Wrapper for valid GroupedProperties
    /// </summary>
    public class ValidGroupedProperties
    {
        public Dictionary<string, string> Value { get; set; }

        public ValidGroupedProperties(Dictionary<string, string> value)
        {
            Value = value;
        }

        public override string ToString() => JsonSerializer.Serialize(Value);
    }

    /// <summary>
    /// Wrapper for invalid GroupedProperties
    /// </summary>
    public class InvalidGroupedProperties
    {
        public string Value { get; set; }

        public InvalidGroupedProperties(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Wrapper for valid GroupedPropertyOptions
    /// </summary>
    public class ValidGroupedPropertyOptions
    {
        public Dictionary<string, GroupedPropertyOption> Value { get; set; }

        public ValidGroupedPropertyOptions(Dictionary<string, GroupedPropertyOption> value)
        {
            Value = value;
        }

        public override string ToString() => JsonSerializer.Serialize(Value);
    }

    /// <summary>
    /// FsCheck generators for GroupedProperties and GroupedPropertyOptions
    /// </summary>
    public static class GroupedPropertiesGenerators
    {
        private static readonly char[] ValidFirstChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz_".ToCharArray();
        private static readonly char[] ValidSubsequentChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789_".ToCharArray();

        private static Gen<string> GenValidIdentifier()
        {
            var genFirstChar = Gen.Elements(ValidFirstChars);
            var genSubsequentChars = Gen.ArrayOf(Gen.Elements(ValidSubsequentChars));

            return from first in genFirstChar
                   from rest in genSubsequentChars
                   where rest.Length < 20
                   select $"{first}{new string(rest)}";
        }

        private static Gen<string> GenColumnList()
        {
            // Generate a list of at least 2 column names
            return from count in Gen.Choose(2, 5)
                   from columns in Gen.ListOf(count, GenValidIdentifier())
                   let distinctColumns = columns.Distinct().ToList()
                   where distinctColumns.Count >= 2
                   select string.Join("|", distinctColumns);
        }

        public static Arbitrary<ValidGroupedProperties> ValidGroupedPropertiesArbitrary()
        {
            var genGroupName = GenValidIdentifier();
            var genColumnList = GenColumnList();

            var genMap = from count in Gen.Choose(0, 5)
                         from groups in Gen.ListOf(count, Gen.Zip(genGroupName, genColumnList))
                         let distinctGroups = groups.GroupBy(g => g.Item1).Select(g => g.First()).ToList()
                         select distinctGroups.ToDictionary(g => g.Item1, g => g.Item2);

            return Arb.From(genMap.Select(m => new ValidGroupedProperties(m)));
        }

        public static Arbitrary<InvalidGroupedProperties> InvalidGroupedPropertiesArbitrary()
        {
            // Single column (invalid - needs at least 2)
            var genSingleColumn = Gen.Constant(@"{""Point"": ""X""}");
            
            // Invalid group name (starts with number)
            var genInvalidGroupName = Gen.Constant(@"{""123Invalid"": ""X|Y""}");
            
            // Invalid group name (contains space)
            var genInvalidGroupName2 = Gen.Constant(@"{""My Group"": ""X|Y""}");
            
            // Invalid group name (contains special chars)
            var genInvalidGroupName3 = Gen.Constant(@"{""My-Group"": ""X|Y""}");

            // Empty column list
            var genEmptyColumnList = Gen.Constant(@"{""Point"": """"}");

            // Column list without pipe
            var genNoPipe = Gen.Constant(@"{""Point"": ""X""}");

            var genInvalidGroupedProperties = Gen.OneOf(
                genSingleColumn,
                genInvalidGroupName,
                genInvalidGroupName2,
                genInvalidGroupName3,
                genEmptyColumnList,
                genNoPipe
            );

            return Arb.From(genInvalidGroupedProperties.Select(s => new InvalidGroupedProperties(s)));
        }

        public static Arbitrary<ValidGroupedPropertyOptions> ValidGroupedPropertyOptionsArbitrary()
        {
            var genGroupName = GenValidIdentifier();
            var genType = Gen.Elements("Tuple", "NewStruct");
            var genIsReadOnly = Gen.Elements(true, false);
            var genStructName = GenValidIdentifier();

            var genOption = from type in genType
                            from hasType in Gen.Elements(true, false)
                            from isReadOnly in genIsReadOnly
                            from hasIsReadOnly in Gen.Elements(true, false)
                            from structName in genStructName
                            from hasStructName in Gen.Elements(true, false)
                            select new GroupedPropertyOption
                            {
                                Type = hasType ? type : null,
                                IsReadOnly = hasIsReadOnly ? isReadOnly : null,
                                StructName = hasStructName ? structName : null
                            };

            var genMap = from count in Gen.Choose(0, 5)
                         from options in Gen.ListOf(count, Gen.Zip(genGroupName, genOption))
                         let distinctOptions = options.GroupBy(o => o.Item1).Select(o => o.First()).ToList()
                         select distinctOptions.ToDictionary(o => o.Item1, o => o.Item2);

            return Arb.From(genMap.Select(m => new ValidGroupedPropertyOptions(m)));
        }
    }
}
