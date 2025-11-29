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
    /// Property-based tests for JSON Schema Relations validation.
    /// Tests that the Relations schema correctly validates relation structures.
    /// </summary>
    [TestFixture]
    public class JsonSchemaRelationsTests
    {
        private JsonDocument _schemaDocument;
        private JsonElement _relationsDefinition;
        private Regex _relationNamePattern;

        [SetUp]
        public void SetUp()
        {
            // Load the JSON Schema
            var testDir = TestContext.CurrentContext.TestDirectory;
            var workspaceRoot = Path.GetFullPath(Path.Combine(testDir, "..", "..", "..", ".."));
            var schemaPath = Path.Combine(workspaceRoot, "schemas", "brudixy-table-schema.json");
            
            var schemaJson = File.ReadAllText(schemaPath);
            _schemaDocument = JsonDocument.Parse(schemaJson);
            
            // Extract the relations definition
            _relationsDefinition = _schemaDocument.RootElement
                .GetProperty("definitions")
                .GetProperty("relations");
            
            // Extract pattern
            _relationNamePattern = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$", RegexOptions.Compiled);
        }

        [TearDown]
        public void TearDown()
        {
            _schemaDocument?.Dispose();
        }

        // Feature: json-schema-definition, Property 5: Relation structure validation
        // Validates: Requirements 5.1, 5.2, 5.3, 5.5
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(RelationsGenerators) })]
        public Property ValidRelations_PassValidation(ValidRelations validRelations)
        {
            // Act
            var isValid = ValidateRelations(validRelations.Value, out var errors);

            // Assert
            return isValid.Label($"Valid Relations should pass validation. Errors: {string.Join(", ", errors)}");
        }

        // Feature: json-schema-definition, Property 5: Relation structure validation
        // Validates: Requirements 5.1, 5.2, 5.3, 5.5
        [FsCheck.NUnit.Property(MaxTest = 100, Arbitrary = new[] { typeof(RelationsGenerators) })]
        public Property InvalidRelations_FailValidation(InvalidRelations invalidRelations)
        {
            // Act
            var isValid = ValidateRelationsJson(invalidRelations.Value, out var errors);

            // Assert
            return (!isValid).Label($"Invalid Relations should fail validation: {invalidRelations.Value}");
        }

        private bool ValidateRelations(Dictionary<string, Dictionary<string, object>> relations, out List<string> errors)
        {
            errors = new List<string>();

            foreach (var kvp in relations)
            {
                var relationName = kvp.Key;
                var relation = kvp.Value;

                // Validate relation name matches pattern
                if (!_relationNamePattern.IsMatch(relationName))
                {
                    errors.Add($"Relation name '{relationName}' does not match pattern");
                    return false;
                }

                // Check required properties
                if (!relation.ContainsKey("ParentKey"))
                {
                    errors.Add($"Relation '{relationName}' missing required property 'ParentKey'");
                    return false;
                }

                if (!relation.ContainsKey("ChildKey"))
                {
                    errors.Add($"Relation '{relationName}' missing required property 'ChildKey'");
                    return false;
                }

                // Validate ParentKey
                if (relation["ParentKey"] is not List<object> parentKey)
                {
                    errors.Add($"Relation '{relationName}' ParentKey must be an array");
                    return false;
                }

                if (parentKey.Count == 0)
                {
                    errors.Add($"Relation '{relationName}' ParentKey must have at least one element");
                    return false;
                }

                if (parentKey.Any(item => item is not string))
                {
                    errors.Add($"Relation '{relationName}' ParentKey must contain only strings");
                    return false;
                }

                // Validate ChildKey
                if (relation["ChildKey"] is not List<object> childKey)
                {
                    errors.Add($"Relation '{relationName}' ChildKey must be an array");
                    return false;
                }

                if (childKey.Count == 0)
                {
                    errors.Add($"Relation '{relationName}' ChildKey must have at least one element");
                    return false;
                }

                if (childKey.Any(item => item is not string))
                {
                    errors.Add($"Relation '{relationName}' ChildKey must contain only strings");
                    return false;
                }

                // Validate optional properties
                if (relation.ContainsKey("ParentTable") && relation["ParentTable"] is not string)
                {
                    errors.Add($"Relation '{relationName}' ParentTable must be a string");
                    return false;
                }

                if (relation.ContainsKey("ChildTable") && relation["ChildTable"] is not string)
                {
                    errors.Add($"Relation '{relationName}' ChildTable must be a string");
                    return false;
                }

                // Check for unknown properties
                var validProperties = new HashSet<string> { "ParentKey", "ChildKey", "ParentTable", "ChildTable" };
                var unknownProperties = relation.Keys.Where(k => !validProperties.Contains(k)).ToList();
                if (unknownProperties.Any())
                {
                    errors.Add($"Relation '{relationName}' has unknown properties: {string.Join(", ", unknownProperties)}");
                    return false;
                }
            }

            return true;
        }

        private bool ValidateRelationsJson(string json, out List<string> errors)
        {
            errors = new List<string>();

            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.ValueKind != JsonValueKind.Object)
                {
                    errors.Add("Relations must be an object");
                    return false;
                }

                foreach (var property in root.EnumerateObject())
                {
                    var relationName = property.Name;

                    // Validate relation name
                    if (!_relationNamePattern.IsMatch(relationName))
                    {
                        errors.Add($"Relation name '{relationName}' does not match pattern");
                        return false;
                    }

                    if (property.Value.ValueKind != JsonValueKind.Object)
                    {
                        errors.Add($"Relation '{relationName}' must be an object");
                        return false;
                    }

                    var relation = property.Value;

                    // Check required properties
                    if (!relation.TryGetProperty("ParentKey", out var parentKeyProp))
                    {
                        errors.Add($"Relation '{relationName}' missing required property 'ParentKey'");
                        return false;
                    }

                    if (!relation.TryGetProperty("ChildKey", out var childKeyProp))
                    {
                        errors.Add($"Relation '{relationName}' missing required property 'ChildKey'");
                        return false;
                    }

                    // Validate ParentKey
                    if (parentKeyProp.ValueKind != JsonValueKind.Array)
                    {
                        errors.Add($"Relation '{relationName}' ParentKey must be an array");
                        return false;
                    }

                    var parentKeyArray = parentKeyProp.EnumerateArray().ToList();
                    if (parentKeyArray.Count == 0)
                    {
                        errors.Add($"Relation '{relationName}' ParentKey must have at least one element");
                        return false;
                    }

                    if (parentKeyArray.Any(item => item.ValueKind != JsonValueKind.String))
                    {
                        errors.Add($"Relation '{relationName}' ParentKey must contain only strings");
                        return false;
                    }

                    // Validate ChildKey
                    if (childKeyProp.ValueKind != JsonValueKind.Array)
                    {
                        errors.Add($"Relation '{relationName}' ChildKey must be an array");
                        return false;
                    }

                    var childKeyArray = childKeyProp.EnumerateArray().ToList();
                    if (childKeyArray.Count == 0)
                    {
                        errors.Add($"Relation '{relationName}' ChildKey must have at least one element");
                        return false;
                    }

                    if (childKeyArray.Any(item => item.ValueKind != JsonValueKind.String))
                    {
                        errors.Add($"Relation '{relationName}' ChildKey must contain only strings");
                        return false;
                    }

                    // Validate optional properties
                    if (relation.TryGetProperty("ParentTable", out var parentTableProp) && 
                        parentTableProp.ValueKind != JsonValueKind.String)
                    {
                        errors.Add($"Relation '{relationName}' ParentTable must be a string");
                        return false;
                    }

                    if (relation.TryGetProperty("ChildTable", out var childTableProp) && 
                        childTableProp.ValueKind != JsonValueKind.String)
                    {
                        errors.Add($"Relation '{relationName}' ChildTable must be a string");
                        return false;
                    }

                    // Check for unknown properties
                    var validProperties = new HashSet<string> { "ParentKey", "ChildKey", "ParentTable", "ChildTable" };
                    foreach (var relProp in relation.EnumerateObject())
                    {
                        if (!validProperties.Contains(relProp.Name))
                        {
                            errors.Add($"Relation '{relationName}' has unknown property: {relProp.Name}");
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
        public void Relations_WithRequiredProperties_IsValid()
        {
            var relations = new Dictionary<string, Dictionary<string, object>>
            {
                ["FK_User_Group"] = new Dictionary<string, object>
                {
                    ["ParentKey"] = new List<object> { "id" },
                    ["ChildKey"] = new List<object> { "groupid" }
                }
            };

            var isValid = ValidateRelations(relations, out var errors);

            Assert.IsTrue(isValid, $"Relations with required properties should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void Relations_WithAllProperties_IsValid()
        {
            var relations = new Dictionary<string, Dictionary<string, object>>
            {
                ["FK_User_Group"] = new Dictionary<string, object>
                {
                    ["ParentTable"] = "Groups",
                    ["ChildTable"] = "Users",
                    ["ParentKey"] = new List<object> { "id" },
                    ["ChildKey"] = new List<object> { "groupid" }
                }
            };

            var isValid = ValidateRelations(relations, out var errors);

            Assert.IsTrue(isValid, $"Relations with all properties should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void Relations_WithMultipleColumns_IsValid()
        {
            var relations = new Dictionary<string, Dictionary<string, object>>
            {
                ["FK_Composite"] = new Dictionary<string, object>
                {
                    ["ParentKey"] = new List<object> { "id", "type" },
                    ["ChildKey"] = new List<object> { "parentid", "parenttype" }
                }
            };

            var isValid = ValidateRelations(relations, out var errors);

            Assert.IsTrue(isValid, $"Relations with multiple columns should be valid. Errors: {string.Join(", ", errors)}");
        }

        [Test]
        public void Relations_MissingParentKey_IsInvalid()
        {
            var relationsJson = @"{
                ""FK_User_Group"": {
                    ""ChildKey"": [""groupid""]
                }
            }";

            var isValid = ValidateRelationsJson(relationsJson, out var errors);

            Assert.IsFalse(isValid, "Relations missing ParentKey should be invalid");
        }

        [Test]
        public void Relations_MissingChildKey_IsInvalid()
        {
            var relationsJson = @"{
                ""FK_User_Group"": {
                    ""ParentKey"": [""id""]
                }
            }";

            var isValid = ValidateRelationsJson(relationsJson, out var errors);

            Assert.IsFalse(isValid, "Relations missing ChildKey should be invalid");
        }

        [Test]
        public void Relations_EmptyParentKey_IsInvalid()
        {
            var relationsJson = @"{
                ""FK_User_Group"": {
                    ""ParentKey"": [],
                    ""ChildKey"": [""groupid""]
                }
            }";

            var isValid = ValidateRelationsJson(relationsJson, out var errors);

            Assert.IsFalse(isValid, "Relations with empty ParentKey should be invalid");
        }

        [Test]
        public void Relations_EmptyChildKey_IsInvalid()
        {
            var relationsJson = @"{
                ""FK_User_Group"": {
                    ""ParentKey"": [""id""],
                    ""ChildKey"": []
                }
            }";

            var isValid = ValidateRelationsJson(relationsJson, out var errors);

            Assert.IsFalse(isValid, "Relations with empty ChildKey should be invalid");
        }

        [Test]
        public void Relations_InvalidRelationName_IsInvalid()
        {
            var relationsJson = @"{
                ""123Invalid"": {
                    ""ParentKey"": [""id""],
                    ""ChildKey"": [""groupid""]
                }
            }";

            var isValid = ValidateRelationsJson(relationsJson, out var errors);

            Assert.IsFalse(isValid, "Relations with invalid relation name should be invalid");
        }

        [Test]
        public void Relations_NonStringInParentKey_IsInvalid()
        {
            var relationsJson = @"{
                ""FK_User_Group"": {
                    ""ParentKey"": [123],
                    ""ChildKey"": [""groupid""]
                }
            }";

            var isValid = ValidateRelationsJson(relationsJson, out var errors);

            Assert.IsFalse(isValid, "Relations with non-string in ParentKey should be invalid");
        }

        [Test]
        public void Relations_NonStringInChildKey_IsInvalid()
        {
            var relationsJson = @"{
                ""FK_User_Group"": {
                    ""ParentKey"": [""id""],
                    ""ChildKey"": [123]
                }
            }";

            var isValid = ValidateRelationsJson(relationsJson, out var errors);

            Assert.IsFalse(isValid, "Relations with non-string in ChildKey should be invalid");
        }

        [Test]
        public void Relations_InvalidParentTableType_IsInvalid()
        {
            var relationsJson = @"{
                ""FK_User_Group"": {
                    ""ParentTable"": 123,
                    ""ParentKey"": [""id""],
                    ""ChildKey"": [""groupid""]
                }
            }";

            var isValid = ValidateRelationsJson(relationsJson, out var errors);

            Assert.IsFalse(isValid, "Relations with non-string ParentTable should be invalid");
        }

        [Test]
        public void Relations_UnknownProperty_IsInvalid()
        {
            var relationsJson = @"{
                ""FK_User_Group"": {
                    ""ParentKey"": [""id""],
                    ""ChildKey"": [""groupid""],
                    ""UnknownProperty"": ""value""
                }
            }";

            var isValid = ValidateRelationsJson(relationsJson, out var errors);

            Assert.IsFalse(isValid, "Relations with unknown property should be invalid");
        }
    }

    /// <summary>
    /// Wrapper for valid Relations
    /// </summary>
    public class ValidRelations
    {
        public Dictionary<string, Dictionary<string, object>> Value { get; set; }

        public ValidRelations(Dictionary<string, Dictionary<string, object>> value)
        {
            Value = value;
        }

        public override string ToString() => JsonSerializer.Serialize(Value);
    }

    /// <summary>
    /// Wrapper for invalid Relations
    /// </summary>
    public class InvalidRelations
    {
        public string Value { get; set; }

        public InvalidRelations(string value)
        {
            Value = value;
        }

        public override string ToString() => Value;
    }

    /// <summary>
    /// FsCheck generators for Relations
    /// </summary>
    public static class RelationsGenerators
    {
        private static readonly string[] ValidRelationNames = new[]
        {
            "FK_User_Group", "FK_Order_Customer", "FK_Item_Category", "FK_Post_Author"
        };

        private static readonly string[] ValidColumnNames = new[]
        {
            "id", "groupid", "userid", "parentid", "type", "categoryid", "authorid"
        };

        private static readonly string[] ValidTableNames = new[]
        {
            "Users", "Groups", "Orders", "Customers", "Items", "Categories", "Posts", "Authors"
        };

        public static Arbitrary<ValidRelations> ValidRelationsArbitrary()
        {
            var genRelationName = Gen.Elements(ValidRelationNames);
            
            var genRelation = from numParentKeys in Gen.Choose(1, 3)
                              from parentKeys in Gen.ArrayOf(numParentKeys, Gen.Elements(ValidColumnNames))
                              from childKeys in Gen.ArrayOf(numParentKeys, Gen.Elements(ValidColumnNames))
                              from includeParentTable in Arb.Generate<bool>()
                              from includeChildTable in Arb.Generate<bool>()
                              from parentTable in Gen.Elements(ValidTableNames)
                              from childTable in Gen.Elements(ValidTableNames)
                              select CreateRelation(
                                  parentKeys.ToList(),
                                  childKeys.ToList(),
                                  includeParentTable ? parentTable : null,
                                  includeChildTable ? childTable : null
                              );

            var genRelations = from numRelations in Gen.Choose(1, 3)
                               from relations in Gen.ArrayOf(numRelations, genRelationName).Select(arr => arr.Distinct().ToArray())
                               from relationDefs in Gen.ArrayOf(relations.Length, genRelation)
                               select relations.Zip(relationDefs, (name, def) => new { name, def })
                                              .ToDictionary(x => x.name, x => x.def);

            return Arb.From(genRelations.Select(rels => new ValidRelations(rels)));
        }

        private static Dictionary<string, object> CreateRelation(
            List<string> parentKeys,
            List<string> childKeys,
            string parentTable,
            string childTable)
        {
            var relation = new Dictionary<string, object>
            {
                ["ParentKey"] = parentKeys.Cast<object>().ToList(),
                ["ChildKey"] = childKeys.Cast<object>().ToList()
            };

            if (parentTable != null)
            {
                relation["ParentTable"] = parentTable;
            }

            if (childTable != null)
            {
                relation["ChildTable"] = childTable;
            }

            return relation;
        }

        public static Arbitrary<InvalidRelations> InvalidRelationsArbitrary()
        {
            // Missing ParentKey
            var genMissingParentKey = Gen.Constant(@"{
                ""FK_User_Group"": {
                    ""ChildKey"": [""groupid""]
                }
            }");

            // Missing ChildKey
            var genMissingChildKey = Gen.Constant(@"{
                ""FK_User_Group"": {
                    ""ParentKey"": [""id""]
                }
            }");

            // Empty ParentKey
            var genEmptyParentKey = Gen.Constant(@"{
                ""FK_User_Group"": {
                    ""ParentKey"": [],
                    ""ChildKey"": [""groupid""]
                }
            }");

            // Empty ChildKey
            var genEmptyChildKey = Gen.Constant(@"{
                ""FK_User_Group"": {
                    ""ParentKey"": [""id""],
                    ""ChildKey"": []
                }
            }");

            // Invalid relation name
            var genInvalidRelationName = Gen.Constant(@"{
                ""123Invalid"": {
                    ""ParentKey"": [""id""],
                    ""ChildKey"": [""groupid""]
                }
            }");

            // Non-string in ParentKey
            var genNonStringParentKey = Gen.Constant(@"{
                ""FK_User_Group"": {
                    ""ParentKey"": [123],
                    ""ChildKey"": [""groupid""]
                }
            }");

            // Non-string in ChildKey
            var genNonStringChildKey = Gen.Constant(@"{
                ""FK_User_Group"": {
                    ""ParentKey"": [""id""],
                    ""ChildKey"": [123]
                }
            }");

            // Unknown property
            var genUnknownProperty = Gen.Constant(@"{
                ""FK_User_Group"": {
                    ""ParentKey"": [""id""],
                    ""ChildKey"": [""groupid""],
                    ""UnknownProperty"": ""value""
                }
            }");

            var genInvalidRelations = Gen.OneOf(
                genMissingParentKey,
                genMissingChildKey,
                genEmptyParentKey,
                genEmptyChildKey,
                genInvalidRelationName,
                genNonStringParentKey,
                genNonStringChildKey,
                genUnknownProperty
            );

            return Arb.From(genInvalidRelations.Select(s => new InvalidRelations(s)));
        }
    }
}
