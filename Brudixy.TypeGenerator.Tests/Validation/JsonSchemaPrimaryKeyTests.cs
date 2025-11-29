using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using FsCheck;
using FsCheck.NUnit;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Property-based tests for JSON Schema PrimaryKey validation.
    /// Tests that PrimaryKey arrays are validated correctly.
    /// </summary>
    [TestFixture]
    public class JsonSchemaPrimaryKeyTests
    {
        [SetUp]
        public void Setup()
        {
        }

        // Feature: json-schema-definition, Property 9: Array property validation
        // Validates: Requirements 11.1, 11.3
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(PrimaryKeyGenerators) })]
        public Property ValidPrimaryKey_PassesValidation(ValidPrimaryKeyArray validPrimaryKey)
        {
            // Act
            var isValid = ValidatePrimaryKey(validPrimaryKey.Value, out var errors);

            // Assert
            return isValid.Label($"Valid PrimaryKey {JsonSerializer.Serialize(validPrimaryKey.Value)} should pass validation. Errors: {string.Join(", ", errors)}");
        }

        // Feature: json-schema-definition, Property 9: Array property validation
        // Validates: Requirements 11.1, 11.3
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(PrimaryKeyGenerators) })]
        public Property InvalidPrimaryKey_FailsValidation(InvalidPrimaryKeyArray invalidPrimaryKey)
        {
            // Act
            var isValid = ValidatePrimaryKeyJson(invalidPrimaryKey.Value, out var errors);

            // Assert
            return (!isValid).Label($"Invalid PrimaryKey {JsonSerializer.Serialize(invalidPrimaryKey.Value)} should fail validation");
        }

        private bool ValidatePrimaryKey(string[] primaryKey, out List<string> errors)
        {
            errors = new List<string>();

            // PrimaryKey must be an array of strings
            if (primaryKey == null)
            {
                errors.Add("PrimaryKey cannot be null");
                return false;
            }

            // All elements must be strings (already guaranteed by type, but check for empty strings)
            foreach (var column in primaryKey)
            {
                if (string.IsNullOrWhiteSpace(column))
                {
                    errors.Add("PrimaryKey cannot contain empty or whitespace-only strings");
                    return false;
                }
            }

            return true;
        }

        private bool ValidatePrimaryKeyJson(string json, out List<string> errors)
        {
            errors = new List<string>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (!root.TryGetProperty("PrimaryKey", out var primaryKeyProp))
                {
                    // PrimaryKey is optional, so missing is valid
                    return true;
                }

                // PrimaryKey must be an array
                if (primaryKeyProp.ValueKind != JsonValueKind.Array)
                {
                    errors.Add("PrimaryKey must be an array");
                    return false;
                }

                // All elements must be strings
                foreach (var element in primaryKeyProp.EnumerateArray())
                {
                    if (element.ValueKind != JsonValueKind.String)
                    {
                        errors.Add("PrimaryKey must contain only strings");
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

        [Test]
        public void EmptyPrimaryKey_PassesValidation()
        {
            // Arrange
            var primaryKey = new string[] { };

            // Act
            var isValid = ValidatePrimaryKey(primaryKey, out var errors);

            // Assert
            Assert.IsTrue(isValid, $"Empty PrimaryKey should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void SingleColumnPrimaryKey_PassesValidation()
        {
            // Arrange
            var primaryKey = new[] { "Id" };

            // Act
            var isValid = ValidatePrimaryKey(primaryKey, out var errors);

            // Assert
            Assert.IsTrue(isValid, $"Single column PrimaryKey should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void MultiColumnPrimaryKey_PassesValidation()
        {
            // Arrange
            var primaryKey = new[] { "UserId", "TenantId" };

            // Act
            var isValid = ValidatePrimaryKey(primaryKey, out var errors);

            // Assert
            Assert.IsTrue(isValid, $"Multi-column PrimaryKey should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void PrimaryKeyWithNonStringElements_FailsValidation()
        {
            // Arrange
            var json = @"{
                ""Table"": ""TestTable"",
                ""Columns"": { ""Id"": ""Int32"" },
                ""PrimaryKey"": [123, 456]
            }";

            // Act
            var isValid = ValidatePrimaryKeyJson(json, out var errors);

            // Assert
            Assert.IsFalse(isValid, "PrimaryKey with non-string elements should fail validation");
        }

        [Test]
        public void PrimaryKeyAsNonArray_FailsValidation()
        {
            // Arrange
            var json = @"{
                ""Table"": ""TestTable"",
                ""Columns"": { ""Id"": ""Int32"" },
                ""PrimaryKey"": ""Id""
            }";

            // Act
            var isValid = ValidatePrimaryKeyJson(json, out var errors);

            // Assert
            Assert.IsFalse(isValid, "PrimaryKey as non-array should fail validation");
        }

        [Test]
        public void MissingPrimaryKey_PassesValidation()
        {
            // Arrange
            var json = @"{
                ""Table"": ""TestTable"",
                ""Columns"": { ""Id"": ""Int32"" }
            }";

            // Act
            var isValid = ValidatePrimaryKeyJson(json, out var errors);

            // Assert
            Assert.IsTrue(isValid, $"Missing PrimaryKey should be valid (optional). Errors: {string.Join(", ", errors)}");
        }
    }

    /// <summary>
    /// Wrapper for valid PrimaryKey arrays
    /// </summary>
    public class ValidPrimaryKeyArray
    {
        public string[] Value { get; set; }

        public ValidPrimaryKeyArray(string[] value)
        {
            Value = value;
        }

        public override string ToString() => $"[{string.Join(", ", Value)}]";
    }

    /// <summary>
    /// Wrapper for invalid PrimaryKey values (as JSON string)
    /// </summary>
    public class InvalidPrimaryKeyArray
    {
        public string Value { get; set; }

        public InvalidPrimaryKeyArray(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// FsCheck generators for PrimaryKey arrays
    /// </summary>
    public static class PrimaryKeyGenerators
    {
        private static readonly string[] ColumnNames = new[]
        {
            "Id", "UserId", "TenantId", "CustomerId", "OrderId",
            "ProductId", "CategoryId", "Name", "Code", "Key"
        };

        public static Arbitrary<ValidPrimaryKeyArray> ValidPrimaryKeyArrayArbitrary()
        {
            // Generate empty arrays or arrays with 1-3 column names
            var genEmpty = Gen.Constant(new string[] { });
            var genSingle = Gen.Elements(ColumnNames).Select(c => new[] { c });
            var genMultiple = from count in Gen.Choose(2, 3)
                              from columns in Gen.ArrayOf(count, Gen.Elements(ColumnNames))
                              select columns;

            var genValidArray = Gen.OneOf(genEmpty, genSingle, genMultiple);

            return Arb.From(genValidArray.Select(arr => new ValidPrimaryKeyArray(arr)));
        }

        public static Arbitrary<InvalidPrimaryKeyArray> InvalidPrimaryKeyArrayArbitrary()
        {
            // Generate invalid JSON with PrimaryKey as non-array or array with non-strings
            var genString = Gen.Constant(@"{
                ""Table"": ""TestTable"",
                ""Columns"": { ""Id"": ""Int32"" },
                ""PrimaryKey"": ""Id""
            }");

            var genNumber = Gen.Constant(@"{
                ""Table"": ""TestTable"",
                ""Columns"": { ""Id"": ""Int32"" },
                ""PrimaryKey"": 123
            }");

            var genBool = Gen.Constant(@"{
                ""Table"": ""TestTable"",
                ""Columns"": { ""Id"": ""Int32"" },
                ""PrimaryKey"": true
            }");

            var genObject = Gen.Constant(@"{
                ""Table"": ""TestTable"",
                ""Columns"": { ""Id"": ""Int32"" },
                ""PrimaryKey"": { ""Column"": ""Id"" }
            }");

            var genArrayWithNumbers = Gen.Constant(@"{
                ""Table"": ""TestTable"",
                ""Columns"": { ""Id"": ""Int32"" },
                ""PrimaryKey"": [1, 2, 3]
            }");

            var genArrayWithMixed = Gen.Constant(@"{
                ""Table"": ""TestTable"",
                ""Columns"": { ""Id"": ""Int32"" },
                ""PrimaryKey"": [""Id"", 123, true]
            }");

            var genArrayWithNulls = Gen.Constant(@"{
                ""Table"": ""TestTable"",
                ""Columns"": { ""Id"": ""Int32"" },
                ""PrimaryKey"": [""Id"", null, ""Name""]
            }");

            var genInvalidValue = Gen.OneOf(
                genString, genNumber, genBool, genObject,
                genArrayWithNumbers, genArrayWithMixed, genArrayWithNulls
            );

            return Arb.From(genInvalidValue.Select(v => new InvalidPrimaryKeyArray(v)));
        }
    }
}
