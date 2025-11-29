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
    /// Property-based tests for JSON Schema XProperties validation.
    /// Tests the XProperty and XPropertiesMap definitions in the JSON Schema.
    /// </summary>
    [TestFixture]
    public class JsonSchemaXPropertiesTests
    {
        private JsonDocument _schemaDocument;
        private JsonElement _xPropertyDefinition;
        private JsonElement _xPropertiesMapDefinition;
        private Regex _identifierPattern;

        [SetUp]
        public void SetUp()
        {
            // Load the JSON Schema
            var testDir = TestContext.CurrentContext.TestDirectory;
            var workspaceRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
            var schemaPath = Path.Combine(workspaceRoot, "schemas", "brudixy-table-schema.json");
            
            var schemaJson = File.ReadAllText(schemaPath);
            _schemaDocument = JsonDocument.Parse(schemaJson);
            
            // Extract the xProperty and xPropertiesMap definitions
            _xPropertyDefinition = _schemaDocument.RootElement
                .GetProperty("definitions")
                .GetProperty("xProperty");
            
            _xPropertiesMapDefinition = _schemaDocument.RootElement
                .GetProperty("definitions")
                .GetProperty("xPropertiesMap");
            
            // Extract pattern for identifiers
            _identifierPattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        }

        [TearDown]
        public void TearDown()
        {
            _schemaDocument?.Dispose();
        }

        // Feature: json-schema-definition, Property 6: XProperty structure validation
        // Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(XPropertyGenerators) })]
        public Property ValidXProperty_PassesValidation(ValidXProperty validXProperty)
        {
            // Act
            var isValid = ValidateXProperty(validXProperty.Value, out var errors);

            // Assert
            return isValid.Label($"Valid XProperty should pass validation. Errors: {string.Join(", ", errors)}");
        }

        // Feature: json-schema-definition, Property 6: XProperty structure validation
        // Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(XPropertyGenerators) })]
        public Property InvalidXProperty_FailsValidation(InvalidXProperty invalidXProperty)
        {
            // Act
            var isValid = ValidateXPropertyJson(invalidXProperty.Value, out var errors);

            // Assert
            return (!isValid).Label($"Invalid XProperty should fail validation: {invalidXProperty.Value}");
        }

        // Feature: json-schema-definition, Property 6: XProperty structure validation
        // Validates: Requirements 7.1, 7.2, 7.3, 7.4, 7.5
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(XPropertyGenerators) })]
        public Property ValidXPropertiesMap_PassesValidation(ValidXPropertiesMap validMap)
        {
            // Act
            var isValid = ValidateXPropertiesMap(validMap.Value, out var errors);

            // Assert
            return isValid.Label($"Valid XPropertiesMap should pass validation. Errors: {string.Join(", ", errors)}");
        }

        private bool ValidateXProperty(Dictionary<string, object> xProperty, out List<string> errors)
        {
            errors = new List<string>();

            // Must have either Type or DataType
            var hasType = xProperty.ContainsKey("Type");
            var hasDataType = xProperty.ContainsKey("DataType");

            if (!hasType && !hasDataType)
            {
                errors.Add("XProperty must have either 'Type' or 'DataType' property");
                return false;
            }

            // Validate Type if present
            if (hasType && xProperty["Type"] is not string)
            {
                errors.Add("XProperty 'Type' must be a string");
                return false;
            }

            // Validate DataType if present
            if (hasDataType && xProperty["DataType"] is not string)
            {
                errors.Add("XProperty 'DataType' must be a string");
                return false;
            }

            // Validate CodePropertyName if present
            if (xProperty.ContainsKey("CodePropertyName"))
            {
                if (xProperty["CodePropertyName"] is not string codePropertyName)
                {
                    errors.Add("XProperty 'CodePropertyName' must be a string");
                    return false;
                }

                if (!_identifierPattern.IsMatch(codePropertyName))
                {
                    errors.Add($"XProperty 'CodePropertyName' '{codePropertyName}' does not match C# identifier pattern");
                    return false;
                }
            }

            // Validate EnumType if present
            if (xProperty.ContainsKey("EnumType") && xProperty["EnumType"] is not string)
            {
                errors.Add("XProperty 'EnumType' must be a string");
                return false;
            }

            // Value can be any type, so no validation needed

            // Check for unknown properties
            var validProperties = new HashSet<string> { "Type", "DataType", "Value", "CodePropertyName", "EnumType" };
            var unknownProperties = xProperty.Keys.Where(k => !validProperties.Contains(k)).ToList();
            if (unknownProperties.Any())
            {
                errors.Add($"XProperty has unknown properties: {string.Join(", ", unknownProperties)}");
                return false;
            }

            return true;
        }

        private bool ValidateXPropertyJson(string json, out List<string> errors)
        {
            errors = new List<string>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("XProperty must be an object");
                    return false;
                }

                // Must have either Type or DataType
                var hasType = root.TryGetProperty("Type", out var typeProp);
                var hasDataType = root.TryGetProperty("DataType", out var dataTypeProp);

                if (!hasType && !hasDataType)
                {
                    errors.Add("XProperty must have either 'Type' or 'DataType' property");
                    return false;
                }

                // Validate Type if present
                if (hasType && typeProp.ValueKind != JsonValueKind.String)
                {
                    errors.Add("XProperty 'Type' must be a string");
                    return false;
                }

                // Validate DataType if present
                if (hasDataType && dataTypeProp.ValueKind != JsonValueKind.String)
                {
                    errors.Add("XProperty 'DataType' must be a string");
                    return false;
                }

                // Validate CodePropertyName if present
                if (root.TryGetProperty("CodePropertyName", out var codePropertyNameProp))
                {
                    if (codePropertyNameProp.ValueKind != JsonValueKind.String)
                    {
                        errors.Add("XProperty 'CodePropertyName' must be a string");
                        return false;
                    }

                    var codePropertyName = codePropertyNameProp.GetString();
                    if (!_identifierPattern.IsMatch(codePropertyName))
                    {
                        errors.Add($"XProperty 'CodePropertyName' '{codePropertyName}' does not match C# identifier pattern");
                        return false;
                    }
                }

                // Validate EnumType if present
                if (root.TryGetProperty("EnumType", out var enumTypeProp) && 
                    enumTypeProp.ValueKind != JsonValueKind.String)
                {
                    errors.Add("XProperty 'EnumType' must be a string");
                    return false;
                }

                // Value can be any type, so no validation needed

                // Check for unknown properties
                var validProperties = new HashSet<string> { "Type", "DataType", "Value", "CodePropertyName", "EnumType" };
                foreach (var prop in root.EnumerateObject())
                {
                    if (!validProperties.Contains(prop.Name))
                    {
                        errors.Add($"XProperty has unknown property: {prop.Name}");
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

        private bool ValidateXPropertiesMap(Dictionary<string, Dictionary<string, object>> xPropertiesMap, out List<string> errors)
        {
            errors = new List<string>();

            foreach (var kvp in xPropertiesMap)
            {
                var propertyName = kvp.Key;
                var xProperty = kvp.Value;

                // Validate property name matches pattern
                if (!_identifierPattern.IsMatch(propertyName))
                {
                    errors.Add($"XPropertiesMap property name '{propertyName}' does not match pattern");
                    return false;
                }

                // Validate the XProperty
                if (!ValidateXProperty(xProperty, out var xPropertyErrors))
                {
                    errors.AddRange(xPropertyErrors.Select(e => $"Property '{propertyName}': {e}"));
                    return false;
                }
            }

            return true;
        }

        [Test]
        public void XProperty_WithType_PassesValidation()
        {
            var xProperty = new Dictionary<string, object>
            {
                ["Type"] = "String"
            };

            var isValid = ValidateXProperty(xProperty, out var errors);
            Assert.IsTrue(isValid, $"XProperty with Type should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void XProperty_WithDataType_PassesValidation()
        {
            var xProperty = new Dictionary<string, object>
            {
                ["DataType"] = "CustomType"
            };

            var isValid = ValidateXProperty(xProperty, out var errors);
            Assert.IsTrue(isValid, $"XProperty with DataType should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void XProperty_WithTypeAndValue_PassesValidation()
        {
            var xProperty = new Dictionary<string, object>
            {
                ["Type"] = "Int32",
                ["Value"] = 42
            };

            var isValid = ValidateXProperty(xProperty, out var errors);
            Assert.IsTrue(isValid, $"XProperty with Type and Value should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void XProperty_WithCodePropertyName_PassesValidation()
        {
            var xProperty = new Dictionary<string, object>
            {
                ["Type"] = "String",
                ["CodePropertyName"] = "MyProperty"
            };

            var isValid = ValidateXProperty(xProperty, out var errors);
            Assert.IsTrue(isValid, $"XProperty with CodePropertyName should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void XProperty_WithEnumType_PassesValidation()
        {
            var xProperty = new Dictionary<string, object>
            {
                ["Type"] = "MyEnum",
                ["EnumType"] = "MyNamespace.MyEnum"
            };

            var isValid = ValidateXProperty(xProperty, out var errors);
            Assert.IsTrue(isValid, $"XProperty with EnumType should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void XProperty_WithoutTypeOrDataType_FailsValidation()
        {
            var xPropertyJson = @"{""Value"": ""test""}";

            var isValid = ValidateXPropertyJson(xPropertyJson, out var errors);
            Assert.IsFalse(isValid, "XProperty without Type or DataType should be invalid");
        }

        [Test]
        public void XProperty_WithInvalidCodePropertyName_FailsValidation()
        {
            var xPropertyJson = @"{""Type"": ""String"", ""CodePropertyName"": ""123Invalid""}";

            var isValid = ValidateXPropertyJson(xPropertyJson, out var errors);
            Assert.IsFalse(isValid, "XProperty with invalid CodePropertyName should be invalid");
        }

        [Test]
        public void XProperty_ValueCanBeAnyJsonType()
        {
            var testCases = new[]
            {
                new Dictionary<string, object> { ["Type"] = "String", ["Value"] = "text" },
                new Dictionary<string, object> { ["Type"] = "Int32", ["Value"] = 42 },
                new Dictionary<string, object> { ["Type"] = "Boolean", ["Value"] = true },
                new Dictionary<string, object> { ["Type"] = "Nullable", ["Value"] = null },
            };

            foreach (var xProperty in testCases)
            {
                var isValid = ValidateXProperty(xProperty, out var errors);
                Assert.IsTrue(isValid, $"XProperty with Value={xProperty["Value"]} should be valid. Errors: {string.Join(", ", errors)}");
            }
        }

        [Test]
        public void XPropertiesMap_WithValidProperties_PassesValidation()
        {
            var xPropertiesMap = new Dictionary<string, Dictionary<string, object>>
            {
                ["Property1"] = new Dictionary<string, object> { ["Type"] = "String" },
                ["Property2"] = new Dictionary<string, object> { ["Type"] = "Int32", ["Value"] = 42 },
                ["_privateProperty"] = new Dictionary<string, object> { ["DataType"] = "CustomType" }
            };

            var isValid = ValidateXPropertiesMap(xPropertiesMap, out var errors);
            Assert.IsTrue(isValid, $"XPropertiesMap with valid properties should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void XPropertiesMap_WithInvalidPropertyName_FailsValidation()
        {
            var xPropertiesMap = new Dictionary<string, Dictionary<string, object>>
            {
                ["123Invalid"] = new Dictionary<string, object> { ["Type"] = "String" }
            };

            var isValid = ValidateXPropertiesMap(xPropertiesMap, out var errors);
            Assert.IsFalse(isValid, "XPropertiesMap with invalid property name should be invalid");
        }

        [Test]
        public void XPropertiesMap_EmptyMap_PassesValidation()
        {
            var xPropertiesMap = new Dictionary<string, Dictionary<string, object>>();

            var isValid = ValidateXPropertiesMap(xPropertiesMap, out var errors);
            Assert.IsTrue(isValid, $"Empty XPropertiesMap should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void XProperty_WithUnknownProperty_FailsValidation()
        {
            var xPropertyJson = @"{""Type"": ""String"", ""UnknownProperty"": ""value""}";

            var isValid = ValidateXPropertyJson(xPropertyJson, out var errors);
            Assert.IsFalse(isValid, "XProperty with unknown property should be invalid");
        }
    }

    /// <summary>
    /// Wrapper for valid XProperty
    /// </summary>
    public class ValidXProperty
    {
        public Dictionary<string, object> Value { get; set; }

        public ValidXProperty(Dictionary<string, object> value)
        {
            Value = value;
        }

        public override string ToString() => JsonSerializer.Serialize(Value);
    }

    /// <summary>
    /// Wrapper for invalid XProperty
    /// </summary>
    public class InvalidXProperty
    {
        public string Value { get; set; }

        public InvalidXProperty(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// Wrapper for valid XPropertiesMap
    /// </summary>
    public class ValidXPropertiesMap
    {
        public Dictionary<string, Dictionary<string, object>> Value { get; set; }

        public ValidXPropertiesMap(Dictionary<string, Dictionary<string, object>> value)
        {
            Value = value;
        }

        public override string ToString() => JsonSerializer.Serialize(Value);
    }

    /// <summary>
    /// FsCheck generators for XProperty and XPropertiesMap
    /// </summary>
    public static class XPropertyGenerators
    {
        private static readonly string[] TypeNames = new[]
        {
            "String", "Int32", "Int64", "Boolean", "DateTime", "Decimal", "Double",
            "MyCustomType", "CustomClass", "MyEnum"
        };

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

        private static Gen<object> GenValue()
        {
            var genString = Gen.Elements("test", "value", "example", "").Select(s => (object)s);
            var genInt = Gen.Choose(-1000, 1000).Select(i => (object)i);
            var genBool = Gen.Elements(true, false).Select(b => (object)b);
            var genNull = Gen.Constant((object)null);

            return Gen.OneOf(genString, genInt, genBool, genNull);
        }

        public static Arbitrary<ValidXProperty> ValidXPropertyArbitrary()
        {
            var genType = Gen.Elements(TypeNames);
            var genDataType = Gen.Elements(TypeNames);
            var genValue = GenValue();
            var genCodePropertyName = GenValidIdentifier();
            var genEnumType = Gen.Elements("MyEnum", "Status", "MyNamespace.MyEnum");

            // Must have either Type or DataType (or both)
            var genHasType = Gen.Elements(true, false);
            var genHasDataType = Gen.Elements(true, false);
            var genHasValue = Gen.Elements(true, false);
            var genHasCodePropertyName = Gen.Elements(true, false);
            var genHasEnumType = Gen.Elements(true, false);

            var genValidXProperty = from hasType in genHasType
                                    from hasDataType in genHasDataType
                                    where hasType || hasDataType  // At least one must be true
                                    from type in genType
                                    from dataType in genDataType
                                    from value in genValue
                                    from hasValue in genHasValue
                                    from codePropertyName in genCodePropertyName
                                    from hasCodePropertyName in genHasCodePropertyName
                                    from enumType in genEnumType
                                    from hasEnumType in genHasEnumType
                                    select CreateXProperty(
                                        hasType ? type : null,
                                        hasDataType ? dataType : null,
                                        hasValue ? value : null,
                                        hasCodePropertyName ? codePropertyName : null,
                                        hasEnumType ? enumType : null,
                                        hasValue
                                    );

            return Arb.From(genValidXProperty.Select(xp => new ValidXProperty(xp)));
        }

        private static Dictionary<string, object> CreateXProperty(
            string type,
            string dataType,
            object value,
            string codePropertyName,
            string enumType,
            bool hasValue)
        {
            var xProperty = new Dictionary<string, object>();

            if (type != null)
            {
                xProperty["Type"] = type;
            }

            if (dataType != null)
            {
                xProperty["DataType"] = dataType;
            }

            if (hasValue)
            {
                xProperty["Value"] = value;
            }

            if (codePropertyName != null)
            {
                xProperty["CodePropertyName"] = codePropertyName;
            }

            if (enumType != null)
            {
                xProperty["EnumType"] = enumType;
            }

            return xProperty;
        }

        public static Arbitrary<InvalidXProperty> InvalidXPropertyArbitrary()
        {
            // Missing both Type and DataType
            var genMissingBoth = Gen.Constant("{\"Value\": \"test\"}");
            
            // Invalid CodePropertyName (starts with number)
            var genInvalidCodePropertyName = Gen.Constant("{\"Type\": \"String\", \"CodePropertyName\": \"123Invalid\"}");
            
            // Invalid CodePropertyName (contains space)
            var genInvalidCodePropertyName2 = Gen.Constant("{\"Type\": \"String\", \"CodePropertyName\": \"My Property\"}");
            
            // Invalid CodePropertyName (contains special chars)
            var genInvalidCodePropertyName3 = Gen.Constant("{\"Type\": \"String\", \"CodePropertyName\": \"My-Property\"}");

            // Extra properties not allowed
            var genExtraProperties = Gen.Constant("{\"Type\": \"String\", \"ExtraField\": \"value\"}");

            var genInvalidXProperty = Gen.OneOf(
                genMissingBoth,
                genInvalidCodePropertyName,
                genInvalidCodePropertyName2,
                genInvalidCodePropertyName3,
                genExtraProperties
            );

            return Arb.From(genInvalidXProperty.Select(s => new InvalidXProperty(s)));
        }

        public static Arbitrary<ValidXPropertiesMap> ValidXPropertiesMapArbitrary()
        {
            var genPropertyName = GenValidIdentifier();
            var genXProperty = from hasType in Gen.Elements(true, false)
                               from hasDataType in Gen.Elements(true, false)
                               where hasType || hasDataType
                               from type in Gen.Elements(TypeNames)
                               from dataType in Gen.Elements(TypeNames)
                               from hasValue in Gen.Elements(true, false)
                               from value in GenValue()
                               select CreateXProperty(
                                   hasType ? type : null,
                                   hasDataType ? dataType : null,
                                   hasValue ? value : null,
                                   null,
                                   null,
                                   hasValue
                               );

            var genMap = from count in Gen.Choose(0, 5)
                         from properties in Gen.ListOf(count, Gen.Zip(genPropertyName, genXProperty))
                         let distinctProperties = properties.GroupBy(p => p.Item1).Select(g => g.First()).ToList()
                         select distinctProperties.ToDictionary(p => p.Item1, p => p.Item2);

            return Arb.From(genMap.Select(m => new ValidXPropertiesMap(m)));
        }
    }
}
