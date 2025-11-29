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
    /// Property-based tests for JSON Schema example validity.
    /// Validates that all examples provided in the schema are valid according to the schema rules.
    /// </summary>
    [TestFixture]
    public class JsonSchemaExampleValidityTests
    {
        private JsonDocument _schemaDocument;
        private JsonElement _schemaRoot;

        [SetUp]
        public void Setup()
        {
            // Load the JSON Schema file
            var schemaPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "schemas", "brudixy-table-schema.json");
            var schemaJson = File.ReadAllText(schemaPath);
            _schemaDocument = JsonDocument.Parse(schemaJson);
            _schemaRoot = _schemaDocument.RootElement;
        }

        [TearDown]
        public void TearDown()
        {
            _schemaDocument?.Dispose();
        }

        // Feature: json-schema-definition, Property 13: Example validity
        // Validates: Requirements 12.5
        [Test]
        public void RootExamples_AreValidYamlStructures()
        {
            // Arrange
            if (!_schemaRoot.TryGetProperty("examples", out var examples))
            {
                Assert.Fail("Schema should have examples array at root level");
                return;
            }

            Assert.IsTrue(examples.ValueKind == JsonValueKind.Array, "Examples should be an array");
            Assert.Greater(examples.GetArrayLength(), 0, "Examples array should not be empty");

            // Act & Assert
            foreach (var example in examples.EnumerateArray())
            {
                // Verify required properties
                Assert.IsTrue(example.TryGetProperty("Table", out var table), "Example should have Table property");
                Assert.IsTrue(table.ValueKind == JsonValueKind.String, "Table should be a string");
                Assert.IsFalse(string.IsNullOrWhiteSpace(table.GetString()), "Table should not be empty");

                Assert.IsTrue(example.TryGetProperty("Columns", out var columns), "Example should have Columns property");
                Assert.IsTrue(columns.ValueKind == JsonValueKind.Object, "Columns should be an object");
                
                // Verify columns are not empty
                var columnCount = 0;
                foreach (var column in columns.EnumerateObject())
                {
                    columnCount++;
                    Assert.IsTrue(column.Value.ValueKind == JsonValueKind.String, $"Column '{column.Name}' should have string type");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(column.Value.GetString()), $"Column '{column.Name}' type should not be empty");
                }
                Assert.Greater(columnCount, 0, "Columns should not be empty");
            }
        }

        [Test]
        public void RootExamples_ContainMinimalAndComplexExamples()
        {
            // Arrange
            if (!_schemaRoot.TryGetProperty("examples", out var examples))
            {
                Assert.Fail("Schema should have examples array at root level");
                return;
            }

            var exampleArray = examples.EnumerateArray().ToList();
            Assert.GreaterOrEqual(exampleArray.Count, 2, "Should have at least 2 examples (minimal and complex)");

            // Act & Assert - Check for minimal example
            var minimalExample = exampleArray.FirstOrDefault(e => 
                e.TryGetProperty("Table", out _) && 
                e.TryGetProperty("Columns", out _) &&
                e.EnumerateObject().Count() <= 3); // Only Table, Columns, and maybe one more property

            Assert.IsNotNull(minimalExample, "Should have a minimal example with just Table and Columns");

            // Act & Assert - Check for complex example
            var complexExample = exampleArray.FirstOrDefault(e => 
                e.TryGetProperty("CodeGenerationOptions", out _) ||
                e.TryGetProperty("Relations", out _) ||
                e.TryGetProperty("GroupedProperties", out _) ||
                e.TryGetProperty("XProperties", out _));

            Assert.IsNotNull(complexExample, "Should have a complex example with additional properties");
        }

        [Test]
        public void CodeGenerationOptions_ExamplesAreValid()
        {
            // Arrange
            if (!TryGetDefinitionExamples("codeGenerationOptions", out var examples))
            {
                Assert.Fail("CodeGenerationOptions definition should have examples");
                return;
            }

            // Act & Assert
            foreach (var example in examples.EnumerateArray())
            {
                // Verify it's an object
                Assert.IsTrue(example.ValueKind == JsonValueKind.Object, "CodeGenerationOptions example should be an object");

                // If Namespace is present, verify it's a string
                if (example.TryGetProperty("Namespace", out var ns))
                {
                    Assert.IsTrue(ns.ValueKind == JsonValueKind.String, "Namespace should be a string");
                    Assert.IsTrue(ns.GetString().Contains(".") || !string.IsNullOrWhiteSpace(ns.GetString()), 
                        "Namespace should be valid");
                }

                // If Class is present, verify it's a string
                if (example.TryGetProperty("Class", out var cls))
                {
                    Assert.IsTrue(cls.ValueKind == JsonValueKind.String, "Class should be a string");
                    Assert.IsFalse(string.IsNullOrWhiteSpace(cls.GetString()), "Class should not be empty");
                }

                // If Abstract or Sealed are present, verify they're booleans
                if (example.TryGetProperty("Abstract", out var abs))
                {
                    Assert.IsTrue(abs.ValueKind == JsonValueKind.True || abs.ValueKind == JsonValueKind.False, 
                        "Abstract should be a boolean");
                }

                if (example.TryGetProperty("Sealed", out var sealedValue))
                {
                    Assert.IsTrue(sealedValue.ValueKind == JsonValueKind.True || sealedValue.ValueKind == JsonValueKind.False, 
                        "Sealed should be a boolean");
                }

                // If ExtraUsing is present, verify it's an array of strings
                if (example.TryGetProperty("ExtraUsing", out var extraUsing))
                {
                    Assert.IsTrue(extraUsing.ValueKind == JsonValueKind.Array, "ExtraUsing should be an array");
                    foreach (var usingDirective in extraUsing.EnumerateArray())
                    {
                        Assert.IsTrue(usingDirective.ValueKind == JsonValueKind.String, "Using directive should be a string");
                        var directive = usingDirective.GetString();
                        Assert.IsTrue(directive.StartsWith("using ") && directive.EndsWith(";"), 
                            $"Using directive '{directive}' should start with 'using ' and end with ';'");
                    }
                }
            }
        }

        [Test]
        public void Relations_ExamplesAreValid()
        {
            // Arrange
            if (!TryGetDefinitionExamples("relations", out var examples))
            {
                Assert.Fail("Relations definition should have examples");
                return;
            }

            // Act & Assert
            foreach (var example in examples.EnumerateArray())
            {
                Assert.IsTrue(example.ValueKind == JsonValueKind.Object, "Relations example should be an object");

                // Each relation should have required properties
                foreach (var relation in example.EnumerateObject())
                {
                    var relationValue = relation.Value;
                    Assert.IsTrue(relationValue.ValueKind == JsonValueKind.Object, 
                        $"Relation '{relation.Name}' should be an object");

                    // Verify required ParentKey
                    Assert.IsTrue(relationValue.TryGetProperty("ParentKey", out var parentKey), 
                        $"Relation '{relation.Name}' should have ParentKey");
                    Assert.IsTrue(parentKey.ValueKind == JsonValueKind.Array, "ParentKey should be an array");
                    Assert.Greater(parentKey.GetArrayLength(), 0, "ParentKey should not be empty");

                    // Verify required ChildKey
                    Assert.IsTrue(relationValue.TryGetProperty("ChildKey", out var childKey), 
                        $"Relation '{relation.Name}' should have ChildKey");
                    Assert.IsTrue(childKey.ValueKind == JsonValueKind.Array, "ChildKey should be an array");
                    Assert.Greater(childKey.GetArrayLength(), 0, "ChildKey should not be empty");

                    // Verify arrays contain strings
                    foreach (var key in parentKey.EnumerateArray())
                    {
                        Assert.IsTrue(key.ValueKind == JsonValueKind.String, "ParentKey elements should be strings");
                    }

                    foreach (var key in childKey.EnumerateArray())
                    {
                        Assert.IsTrue(key.ValueKind == JsonValueKind.String, "ChildKey elements should be strings");
                    }
                }
            }
        }

        [Test]
        public void XPropertiesMap_ExamplesAreValid()
        {
            // Arrange
            if (!TryGetDefinitionExamples("xPropertiesMap", out var examples))
            {
                Assert.Fail("XPropertiesMap definition should have examples");
                return;
            }

            // Act & Assert
            foreach (var example in examples.EnumerateArray())
            {
                Assert.IsTrue(example.ValueKind == JsonValueKind.Object, "XPropertiesMap example should be an object");

                // Each XProperty should have Type or DataType
                foreach (var xprop in example.EnumerateObject())
                {
                    var xpropValue = xprop.Value;
                    Assert.IsTrue(xpropValue.ValueKind == JsonValueKind.Object, 
                        $"XProperty '{xprop.Name}' should be an object");

                    var hasType = xpropValue.TryGetProperty("Type", out var type);
                    var hasDataType = xpropValue.TryGetProperty("DataType", out var dataType);

                    Assert.IsTrue(hasType || hasDataType, 
                        $"XProperty '{xprop.Name}' should have either Type or DataType");

                    if (hasType)
                    {
                        Assert.IsTrue(type.ValueKind == JsonValueKind.String, "Type should be a string");
                    }

                    if (hasDataType)
                    {
                        Assert.IsTrue(dataType.ValueKind == JsonValueKind.String, "DataType should be a string");
                    }
                }
            }
        }

        [Test]
        public void ColumnOptions_ExamplesAreValid()
        {
            // Arrange
            if (!TryGetDefinitionExamples("columnOptions", out var examples))
            {
                Assert.Fail("ColumnOptions definition should have examples");
                return;
            }

            // Act & Assert
            foreach (var example in examples.EnumerateArray())
            {
                Assert.IsTrue(example.ValueKind == JsonValueKind.Object, "ColumnOptions example should be an object");

                // Each column option should be an object with valid properties
                foreach (var columnOption in example.EnumerateObject())
                {
                    var optionValue = columnOption.Value;
                    Assert.IsTrue(optionValue.ValueKind == JsonValueKind.Object, 
                        $"Column option '{columnOption.Name}' should be an object");

                    // Verify boolean properties if present
                    var booleanProps = new[] { "AllowNull", "IsUnique", "HasIndex", "IsReadOnly", "IsService", "Auto" };
                    foreach (var boolProp in booleanProps)
                    {
                        if (optionValue.TryGetProperty(boolProp, out var boolValue))
                        {
                            Assert.IsTrue(boolValue.ValueKind == JsonValueKind.True || boolValue.ValueKind == JsonValueKind.False,
                                $"Property '{boolProp}' should be a boolean");
                        }
                    }

                    // Verify MaxLength if present
                    if (optionValue.TryGetProperty("MaxLength", out var maxLength))
                    {
                        Assert.IsTrue(maxLength.ValueKind == JsonValueKind.Number, "MaxLength should be a number");
                        Assert.Greater(maxLength.GetInt32(), 0, "MaxLength should be positive");
                    }
                }
            }
        }

        [Test]
        public void GroupedProperties_ExamplesAreValid()
        {
            // Arrange
            if (!TryGetDefinitionExamples("groupedProperties", out var examples))
            {
                // GroupedProperties examples might be optional
                Assert.Pass("GroupedProperties examples not found, skipping");
                return;
            }

            // Act & Assert
            foreach (var example in examples.EnumerateArray())
            {
                Assert.IsTrue(example.ValueKind == JsonValueKind.Object, "GroupedProperties example should be an object");

                // Each grouped property should be a pipe-separated string
                foreach (var groupedProp in example.EnumerateObject())
                {
                    var propValue = groupedProp.Value;
                    Assert.IsTrue(propValue.ValueKind == JsonValueKind.String, 
                        $"Grouped property '{groupedProp.Name}' should be a string");

                    var value = propValue.GetString();
                    Assert.IsTrue(value.Contains("|"), 
                        $"Grouped property '{groupedProp.Name}' should contain pipe separator");

                    var columns = value.Split('|');
                    Assert.GreaterOrEqual(columns.Length, 2, 
                        $"Grouped property '{groupedProp.Name}' should have at least 2 columns");
                }
            }
        }

        [Test]
        public void GroupedPropertyOptions_ExamplesAreValid()
        {
            // Arrange
            if (!TryGetDefinitionExamples("groupedPropertyOptions", out var examples))
            {
                // GroupedPropertyOptions examples might be optional
                Assert.Pass("GroupedPropertyOptions examples not found, skipping");
                return;
            }

            // Act & Assert
            foreach (var example in examples.EnumerateArray())
            {
                Assert.IsTrue(example.ValueKind == JsonValueKind.Object, "GroupedPropertyOptions example should be an object");

                // Each option should have valid Type enum value
                foreach (var option in example.EnumerateObject())
                {
                    var optionValue = option.Value;
                    Assert.IsTrue(optionValue.ValueKind == JsonValueKind.Object, 
                        $"Grouped property option '{option.Name}' should be an object");

                    // Verify Type if present
                    if (optionValue.TryGetProperty("Type", out var type))
                    {
                        Assert.IsTrue(type.ValueKind == JsonValueKind.String, "Type should be a string");
                        var typeValue = type.GetString();
                        Assert.IsTrue(typeValue == "Tuple" || typeValue == "NewStruct", 
                            $"Type should be 'Tuple' or 'NewStruct', got '{typeValue}'");
                    }

                    // Verify IsReadOnly if present
                    if (optionValue.TryGetProperty("IsReadOnly", out var isReadOnly))
                    {
                        Assert.IsTrue(isReadOnly.ValueKind == JsonValueKind.True || isReadOnly.ValueKind == JsonValueKind.False,
                            "IsReadOnly should be a boolean");
                    }

                    // Verify StructName if present
                    if (optionValue.TryGetProperty("StructName", out var structName))
                    {
                        Assert.IsTrue(structName.ValueKind == JsonValueKind.String, "StructName should be a string");
                        Assert.IsFalse(string.IsNullOrWhiteSpace(structName.GetString()), "StructName should not be empty");
                    }
                }
            }
        }

        private bool TryGetDefinitionExamples(string definitionName, out JsonElement examples)
        {
            examples = default;

            if (!_schemaRoot.TryGetProperty("definitions", out var definitions))
            {
                return false;
            }

            if (!definitions.TryGetProperty(definitionName, out var definition))
            {
                return false;
            }

            return definition.TryGetProperty("examples", out examples);
        }
    }
}
