using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Brudixy.TypeGenerator.Tests.Validation
{
    /// <summary>
    /// Unit tests for JSON Schema structure validation.
    /// Validates that the schema itself is well-formed and valid.
    /// </summary>
    [TestFixture]
    public class JsonSchemaStructureTests
    {
        private JsonDocument _schemaDocument;
        private JsonElement _schemaRoot;
        private const string SchemaPath = "schemas/brudixy-table-schema.json";

        [OneTimeSetUp]
        public void OneTimeSetUp()
        {
            // Find the schema file by walking up the directory tree
            var currentDir = TestContext.CurrentContext.TestDirectory;
            var schemaFile = FindSchemaFile(currentDir);
            
            Assert.IsNotNull(schemaFile, $"Could not find schema file: {SchemaPath}");
            Assert.IsTrue(File.Exists(schemaFile), $"Schema file does not exist: {schemaFile}");

            var schemaJson = File.ReadAllText(schemaFile);
            _schemaDocument = JsonDocument.Parse(schemaJson);
            _schemaRoot = _schemaDocument.RootElement;
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            _schemaDocument?.Dispose();
        }

        private string FindSchemaFile(string startDir)
        {
            var dir = new DirectoryInfo(startDir);
            while (dir != null)
            {
                var schemaFile = Path.Combine(dir.FullName, SchemaPath);
                if (File.Exists(schemaFile))
                {
                    return schemaFile;
                }
                dir = dir.Parent;
            }
            return null;
        }

        // Feature: json-schema-definition, Task 11.1: Validate schema is valid JSON Schema Draft 7
        // Validates: Requirements 8.1
        [Test]
        public void Schema_HasRequiredMetadata_SchemaProperty()
        {
            // Assert
            Assert.IsTrue(_schemaRoot.TryGetProperty("$schema", out var schemaProperty),
                "Schema must have $schema property");
            Assert.AreEqual("http://json-schema.org/draft-07/schema#", schemaProperty.GetString(),
                "$schema must point to JSON Schema Draft 7");
        }

        // Feature: json-schema-definition, Task 11.1: Validate schema is valid JSON Schema Draft 7
        // Validates: Requirements 8.1, 8.2
        [Test]
        public void Schema_HasRequiredMetadata_IdProperty()
        {
            // Assert
            Assert.IsTrue(_schemaRoot.TryGetProperty("$id", out var idProperty),
                "Schema must have $id property");
            
            var id = idProperty.GetString();
            Assert.IsNotNull(id, "$id must not be null");
            Assert.IsTrue(Uri.IsWellFormedUriString(id, UriKind.Absolute),
                "$id must be a valid absolute URI");
        }

        // Feature: json-schema-definition, Task 11.1: Validate schema is valid JSON Schema Draft 7
        // Validates: Requirements 8.1, 8.2
        [Test]
        public void Schema_HasRequiredMetadata_TitleProperty()
        {
            // Assert
            Assert.IsTrue(_schemaRoot.TryGetProperty("title", out var titleProperty),
                "Schema must have title property");
            
            var title = titleProperty.GetString();
            Assert.IsNotNull(title, "title must not be null");
            Assert.IsNotEmpty(title, "title must not be empty");
        }

        // Feature: json-schema-definition, Task 11.1: Validate schema is valid JSON Schema Draft 7
        // Validates: Requirements 8.1
        [Test]
        public void Schema_HasRequiredMetadata_DescriptionProperty()
        {
            // Assert
            Assert.IsTrue(_schemaRoot.TryGetProperty("description", out var descProperty),
                "Schema must have description property");
            
            var description = descProperty.GetString();
            Assert.IsNotNull(description, "description must not be null");
            Assert.IsNotEmpty(description, "description must not be empty");
        }

        // Feature: json-schema-definition, Task 11.1: Validate schema is valid JSON Schema Draft 7
        // Validates: Requirements 8.1
        [Test]
        public void Schema_AllDefinitions_ArePresent()
        {
            // Arrange
            var expectedDefinitions = new[]
            {
                "columnType",
                "xProperty",
                "xPropertiesMap",
                "relation",
                "relations",
                "codeGenerationOptions",
                "columnOptions",
                "groupedProperties",
                "groupedPropertyOptions"
            };

            // Assert
            Assert.IsTrue(_schemaRoot.TryGetProperty("definitions", out var definitions),
                "Schema must have definitions section");

            foreach (var expectedDef in expectedDefinitions)
            {
                Assert.IsTrue(definitions.TryGetProperty(expectedDef, out _),
                    $"Schema must have definition: {expectedDef}");
            }
        }

        // Feature: json-schema-definition, Task 11.1: Validate schema is valid JSON Schema Draft 7
        // Validates: Requirements 8.1
        [Test]
        public void Schema_AllPatterns_AreValidRegex()
        {
            // Act & Assert
            ValidateRegexPatterns(_schemaRoot);
        }

        private void ValidateRegexPatterns(JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("pattern", out var patternProp))
                {
                    var pattern = patternProp.GetString();
                    Assert.DoesNotThrow(() => new Regex(pattern),
                        $"Pattern must be valid regex: {pattern}");
                }

                foreach (var property in element.EnumerateObject())
                {
                    ValidateRegexPatterns(property.Value);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    ValidateRegexPatterns(item);
                }
            }
        }

        // Feature: json-schema-definition, Task 11.1: Validate schema is valid JSON Schema Draft 7
        // Validates: Requirements 8.1
        [Test]
        public void Schema_AllReferences_Resolve()
        {
            // Arrange
            var definitions = _schemaRoot.GetProperty("definitions");
            var definitionNames = definitions.EnumerateObject()
                .Select(p => p.Name)
                .ToHashSet();

            // Act & Assert
            ValidateReferences(_schemaRoot, definitionNames);
        }

        private void ValidateReferences(JsonElement element, HashSet<string> definitionNames)
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("$ref", out var refProp))
                {
                    var refValue = refProp.GetString();
                    
                    // Check if it's a definition reference
                    if (refValue.StartsWith("#/definitions/"))
                    {
                        var defName = refValue.Substring("#/definitions/".Length);
                        Assert.IsTrue(definitionNames.Contains(defName),
                            $"Reference to undefined definition: {refValue}");
                    }
                }

                foreach (var property in element.EnumerateObject())
                {
                    ValidateReferences(property.Value, definitionNames);
                }
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    ValidateReferences(item, definitionNames);
                }
            }
        }

        // Feature: json-schema-definition, Task 11.1: Validate schema is valid JSON Schema Draft 7
        // Validates: Requirements 8.1
        [Test]
        public void Schema_IsValidJson()
        {
            // The fact that we successfully parsed the schema in OneTimeSetUp
            // proves it's valid JSON, but let's be explicit
            Assert.IsNotNull(_schemaDocument, "Schema must be valid JSON");
            Assert.AreEqual(JsonValueKind.Object, _schemaRoot.ValueKind,
                "Schema root must be a JSON object");
        }

        // Feature: json-schema-definition, Task 11.1: Validate schema is valid JSON Schema Draft 7
        // Validates: Requirements 8.1
        [Test]
        public void Schema_HasValidType()
        {
            // Assert
            Assert.IsTrue(_schemaRoot.TryGetProperty("type", out var typeProperty),
                "Schema must have type property");
            Assert.AreEqual("object", typeProperty.GetString(),
                "Schema type must be 'object'");
        }

        // Feature: json-schema-definition, Task 11.1: Validate schema is valid JSON Schema Draft 7
        // Validates: Requirements 8.1
        [Test]
        public void Schema_HasRequiredFields()
        {
            // Assert
            Assert.IsTrue(_schemaRoot.TryGetProperty("required", out var requiredProperty),
                "Schema must have required property");
            
            var requiredFields = requiredProperty.EnumerateArray()
                .Select(e => e.GetString())
                .ToList();
            
            Assert.Contains("Table", requiredFields, "Table must be in required fields");
            Assert.Contains("Columns", requiredFields, "Columns must be in required fields");
        }

        // Feature: json-schema-definition, Task 11.1: Validate schema is valid JSON Schema Draft 7
        // Validates: Requirements 8.1
        [Test]
        public void Schema_HasPropertiesSection()
        {
            // Assert
            Assert.IsTrue(_schemaRoot.TryGetProperty("properties", out var properties),
                "Schema must have properties section");
            
            Assert.IsTrue(properties.TryGetProperty("Table", out _),
                "Schema must define Table property");
            Assert.IsTrue(properties.TryGetProperty("Columns", out _),
                "Schema must define Columns property");
        }
    }
}
