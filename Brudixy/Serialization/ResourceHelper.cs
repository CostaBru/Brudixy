using System;
using System.IO;
using System.Reflection;

namespace Brudixy.Serialization;

/// <summary>
/// Helper class for accessing embedded resources in Brudixy
/// </summary>
public static class ResourceHelper
{
    private const string SchemaResourceName = "Brudixy.Resources.brudixy-table-schema.json";
    
    /// <summary>
    /// Gets the embedded brudixy-table-schema.json as a string
    /// </summary>
    /// <returns>The JSON schema content</returns>
    /// <exception cref="InvalidOperationException">Thrown when the schema resource cannot be found</exception>
    public static string GetEmbeddedSchemaJson()
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(SchemaResourceName);
        
        if (stream == null)
        {
            throw new InvalidOperationException(
                $"Could not find embedded resource '{SchemaResourceName}'. " +
                $"Available resources: {string.Join(", ", assembly.GetManifestResourceNames())}");
        }
        
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
