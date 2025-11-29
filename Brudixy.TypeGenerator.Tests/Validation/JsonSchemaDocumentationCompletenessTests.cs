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
    /// Property-based tests for JSON Schema documentation completeness.
    /// Validates that all properties in the schema have descriptions.
    /// </summary>
    [TestFixture]
    public class JsonSchemaDocumentationCompletenessTests
    {
        private JsonDocument _schemaDocument;
        private JsonElement _schemaRoot;

        [SetUp]
        public void Setup()
        {
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

        // Feature: json-schema-definition, Property 12: Documentation completeness
        // Validates: Requirements 1.2, 9.1, 9.4
        [Test]
        public void AllTopLevelProperties_HaveDescriptions()
        {
            // Arrange
            var properties = _schemaRoot.GetProperty("properties");
            var missingDescriptions = new List<string>();

            // Act
            foreach (var property in properties.EnumerateObject())
            {
                var propertyName = property.Name;
                var propertyValue = property.Value;

                if (!HasDescription(propertyValue))
                {
                    missingDescriptions.Add($"properties.{propertyName}");
                }
            }

            // Assert
            Assert.IsEmpty(missingDescriptions, 
                $"The following top-level properties are missing descriptions: {string.Join(", ", missingDescriptions)}");
        }

        // Feature: json-schema-definition, Property 12: Documentation completeness
        // Validates: Requirements 1.2, 9.1, 9.4
        [Test]
        public void AllDefinitions_HaveDescriptions()
        {
            // Arrange
            if (!_schemaRoot.TryGetProperty("definitions", out var definitions))
            {
                Assert.Pass("No definitions section found");
                return;
            }

            var missingDescriptions = new List<string>();

            // Act
            foreach (var definition in definitions.EnumerateObject())
            {
                var definitionName = definition.Name;
                var definitionValue = definition.Value;

                if (!HasDescription(definitionValue))
                {
                    missingDescriptions.Add($"definitions.{definitionName}");
                }
            }

            // Assert
            Assert.IsEmpty(missingDescriptions, 
                $"The following definitions are missing descriptions: {string.Join(", ", missingDescriptions)}");
        }

        // Feature: json-schema-definition, Property 12: Documentation completeness
        // Validates: Requirements 1.2, 9.1, 9.4
        [Test]
        public void AllNestedProperties_HaveDescriptions()
        {
            // Arrange
            if (!_schemaRoot.TryGetProperty("definitions", out var definitions))
            {
                Assert.Pass("No definitions section found");
                return;
            }

            var missingDescriptions = new List<string>();

            // Act - Check properties within each definition
            foreach (var definition in definitions.EnumerateObject())
            {
                var definitionName = definition.Name;
                var definitionValue = definition.Value;

                if (definitionValue.TryGetProperty("properties", out var properties))
                {
                    foreach (var property in properties.EnumerateObject())
                    {
                        var propertyName = property.Name;
                        var propertyValue = property.Value;

                        if (!HasDescription(propertyValue))
                        {
                            missingDescriptions.Add($"definitions.{definitionName}.properties.{propertyName}");
                        }
                    }
                }
            }

            // Assert
            Assert.IsEmpty(missingDescriptions, 
                $"The following nested properties are missing descriptions: {string.Join(", ", missingDescriptions)}");
        }

        // Feature: json-schema-definition, Property 12: Documentation completeness
        // Validates: Requirements 1.2, 9.1, 9.4
        [Test]
        public void SchemaMetadata_HasRequiredFields()
        {
            // Assert
            Assert.IsTrue(_schemaRoot.TryGetProperty("$schema", out _), "Schema must have $schema property");
            Assert.IsTrue(_schemaRoot.TryGetProperty("$id", out _), "Schema must have $id property");
            Assert.IsTrue(_schemaRoot.TryGetProperty("title", out _), "Schema must have title property");
            Assert.IsTrue(_schemaRoot.TryGetProperty("description", out _), "Schema must have description property");
        }

        // Feature: json-schema-definition, Property 12: Documentation completeness
        // Validates: Requirements 9.2, 9.4
        [Test]
        public void ComplexProperties_HaveExamples()
        {
            // Arrange
            var complexPropertyNames = new[]
            {
                "CodeGenerationOptions",
                "ColumnOptions",
                "Relations",
                "GroupedProperties",
                "GroupedPropertyOptions",
                "XProperties",
                "Indexes",
                "RowSubTypes",
                "RowSubTypeOptions"
            };

            var missingExamples = new List<string>();
            var properties = _schemaRoot.GetProperty("properties");

            // Act
            foreach (var propertyName in complexPropertyNames)
            {
                if (properties.TryGetProperty(propertyName, out var property))
                {
                    if (!HasExamples(property))
                    {
                        // Check if it's a reference
                        if (property.TryGetProperty("$ref", out var refValue))
                        {
                            var refPath = refValue.GetString();
                            if (refPath.StartsWith("#/definitions/"))
                            {
                                var definitionName = refPath.Substring("#/definitions/".Length);
                                var definitions = _schemaRoot.GetProperty("definitions");
                                if (definitions.TryGetProperty(definitionName, out var definition))
                                {
                                    if (!HasExamples(definition))
                                    {
                                        missingExamples.Add(propertyName);
                                    }
                                }
                            }
                        }
                        else
                        {
                            missingExamples.Add(propertyName);
                        }
                    }
                }
            }

            // Assert
            Assert.IsEmpty(missingExamples, 
                $"The following complex properties are missing examples: {string.Join(", ", missingExamples)}");
        }

        // Feature: json-schema-definition, Property 12: Documentation completeness
        // Validates: Requirements 9.2
        [Test]
        public void PatternProperties_HaveDescriptions()
        {
            // Arrange
            if (!_schemaRoot.TryGetProperty("definitions", out var definitions))
            {
                Assert.Pass("No definitions section found");
                return;
            }

            var missingDescriptions = new List<string>();

            // Act - Check patternProperties within each definition
            foreach (var definition in definitions.EnumerateObject())
            {
                var definitionName = definition.Name;
                var definitionValue = definition.Value;

                if (definitionValue.TryGetProperty("patternProperties", out var patternProperties))
                {
                    foreach (var pattern in patternProperties.EnumerateObject())
                    {
                        var patternKey = pattern.Name;
                        var patternValue = pattern.Value;

                        if (!HasDescription(patternValue))
                        {
                            missingDescriptions.Add($"definitions.{definitionName}.patternProperties.{patternKey}");
                        }
                    }
                }
            }

            // Assert
            Assert.IsEmpty(missingDescriptions, 
                $"The following pattern properties are missing descriptions: {string.Join(", ", missingDescriptions)}");
        }

        private bool HasDescription(JsonElement element)
        {
            if (element.TryGetProperty("description", out var description))
            {
                var descText = description.GetString();
                return !string.IsNullOrWhiteSpace(descText);
            }

            // If it's a reference, we don't require a description at this level
            if (element.TryGetProperty("$ref", out _))
            {
                return true;
            }

            return false;
        }

        private bool HasExamples(JsonElement element)
        {
            if (element.TryGetProperty("examples", out var examples))
            {
                return examples.GetArrayLength() > 0;
            }

            return false;
        }
    }
}
