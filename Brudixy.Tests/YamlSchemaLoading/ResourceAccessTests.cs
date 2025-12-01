using System;
using System.IO;
using System.Reflection;
using NUnit.Framework;

namespace Brudixy.Tests.YamlSchemaLoading;

/// <summary>
/// Tests to verify that embedded resources are correctly configured
/// </summary>
[TestFixture]
public class ResourceAccessTests
{
    [Test]
    public void EmbeddedSchemaResource_CanBeAccessed()
    {
        // Arrange
        var assembly = typeof(DataTable).Assembly;
        const string resourceName = "Brudixy.Resources.brudixy-table-schema.json";
        
        // Act
        using var stream = assembly.GetManifestResourceStream(resourceName);
        
        // Assert
        Assert.IsNotNull(stream);
        Assert.IsTrue(stream.Length > 0, "Schema resource should not be empty");
        
        // Verify it's valid JSON by reading it
        using var reader = new StreamReader(stream);
        var content = reader.ReadToEnd();
        
        Assert.That(content, Does.Contain("$schema"));
        Assert.That(content, Does.Contain("brudixy-table-schema"));
        Assert.That(content, Does.Contain("Table"));
    }
    
    [Test]
    public void ResourceHelper_GetEmbeddedSchemaJson_ReturnsValidSchema()
    {
        // Act
        var schemaJson = Brudixy.Serialization.ResourceHelper.GetEmbeddedSchemaJson();
        
        // Assert
        Assert.IsNotNull(schemaJson);
        Assert.IsNotEmpty(schemaJson);
        Assert.That(schemaJson, Does.Contain("$schema"));
        Assert.That(schemaJson, Does.Contain("brudixy-table-schema"));
    }
}
