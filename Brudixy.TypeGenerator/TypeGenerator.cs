
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Brudixy.TypeGenerator.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace Brudixy.TypeGenerator
{
    public static class SourceGeneratorExtensions
    {
        /// <summary>Gets the file path the source generator was called from.</summary>
        /// <param name="context">The context of the Generator's Execute method.</param>
        /// <returns>The file path the generator was called from.</returns>
        public static string GetCallingPath(this GeneratorExecutionContext context)
        {
            return context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.projectdir", out var result)
                ? result
                : null;
        }
    }

    [Generator]
    public class TypeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            /*#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif */
        }
        
        private enum yamlType
        {
            none,
            singleTabl,
            datasetTab,
            datasetDef,
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var callingPath = context.GetCallingPath();

            // NOTE: Don't emit debug/log files via AddSource. They become compilation units when
            // EmitCompilerGeneratedFiles is enabled and can break builds on some platforms.
           // context.AddSource(Brudixy.TypeGenerator.Core.HintNameHelper.Sanitize("Test.log", "Test"), $"//{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss")} at " + callingPath);

            string lastFileItem = string.Empty;

            try
            {
                YamlDotNetLoader.EnsureInstalled();
                
                var brudixyFiles = new Dictionary<string, string>();

                var singleTables = new List<string>();
                var dataSets = new List<(string name, string yaml)>();

                var yamlSources = ReadSchemaFiles(context);

                // Only proceed with YAML processing if there are schema files.
                if (yamlSources.Count == 0)
                {
                    return;
                }

                // NOTE: Don't emit debug/log files via AddSource. See comment above.
                //context.AddSource(Brudixy.TypeGenerator.Core.HintNameHelper.Sanitize("Files.log", "Files"), string.Join(Environment.NewLine, yamlSources.Select(s => "//" + s.path)));

                foreach (var source in yamlSources)
                {
                    if (source.type is yamlType.singleTabl or yamlType.datasetTab)
                    {
                        if (string.IsNullOrEmpty(source.src))
                        {
                            continue;
                        }

                        brudixyFiles[source.src] = source.src;

                        if (source.type == yamlType.singleTabl)
                        {
                            singleTables.Add(source.path);
                        }
                    }
                    else if(source.type == yamlType.datasetDef)
                    {
                        if (string.IsNullOrEmpty(source.src))
                        {
                            continue;
                        }
                    
                        dataSets.Add((source.path, source.src));
                        
                        brudixyFiles[source.path] = source.src;
                    }
                }

                // Ensure YamlDotNet is available before constructing the schema reader.
                try
                {
                    YamlDotNetLoader.EnsureInstalled();
                }
                catch (Exception ex)
                {
                    // Emit a diagnostic instead of crashing the compilation.
                    var descriptor = new DiagnosticDescriptor(
                        id: "BRXTY001",
                        title: "Failed to load embedded YamlDotNet for Brudixy.TypeGenerator",
                        messageFormat: "Brudixy.TypeGenerator could not load the embedded YamlDotNet assembly. YAML-based generation is disabled. Details: {0}",
                        category: "Brudixy.TypeGenerator",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true);

                    context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, ex.Message));
                    return;
                }

                ISchemaReader schemaReader;
                try
                {
                    schemaReader = new YamlSchemaReader();
                }
                catch (Exception ex)
                {
                    var descriptor = new DiagnosticDescriptor(
                        id: "BRXTY002",
                        title: "Failed to initialize YamlSchemaReader",
                        messageFormat: "Brudixy.TypeGenerator failed to initialize the YAML schema reader. YAML-based generation is disabled. Details: {0}",
                        category: "Brudixy.TypeGenerator",
                        defaultSeverity: DiagnosticSeverity.Warning,
                        isEnabledByDefault: true);

                    context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, ex.Message));
                    return;
                }

                var fileSystemAccess = new FileSystemAccess(brudixyFiles);
                
                // Create validation engine with all rules
                var validationEngine = Core.Validation.SchemaValidationEngine.CreateDefault();
                
                // Check if validation is disabled via MSBuild property
                var validationDisabled = false;
                if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.BrudixyDisableValidation", out var disableValue))
                {
                    validationDisabled = string.Equals(disableValue, "true", StringComparison.OrdinalIgnoreCase);
                }
                
                // Check if strict validation mode is enabled
                var strictValidation = false;
                if (context.AnalyzerConfigOptions.GlobalOptions.TryGetValue("build_property.BrudixyStrictValidation", out var strictValue))
                {
                    strictValidation = string.Equals(strictValue, "true", StringComparison.OrdinalIgnoreCase);
                }
            
                foreach (var table in singleTables)
                {
                    // Validate schema before code generation (unless validation is disabled)
                    if (!validationDisabled)
                    {
                        var yamlContent = fileSystemAccess.GetFileContents(table);
                        var tableObj = schemaReader.GetTable(yamlContent);
                        
                        // Load base tables for validation
                        var loadedBaseTables = LoadBaseTablesForValidation(tableObj, table, fileSystemAccess, schemaReader, callingPath);
                        
                        var validationContext = new Core.Validation.ValidationContext(tableObj, table, fileSystemAccess, loadedBaseTables);
                        var validationResult = validationEngine.Validate(validationContext);
                        
                        // Report validation errors as diagnostics
                        foreach (var error in validationResult.Errors)
                        {
                            var descriptor = new DiagnosticDescriptor(
                                id: "BRXVAL001",
                                title: "Schema Validation Error",
                                messageFormat: "{0}: {1}",
                                category: "Brudixy.Validation",
                                defaultSeverity: DiagnosticSeverity.Error,
                                isEnabledByDefault: true);
                            
                            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, error.FilePath, error.Message));
                        }
                        
                        // Report validation warnings as diagnostics
                        foreach (var warning in validationResult.Warnings)
                        {
                            var severity = strictValidation ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;
                            var descriptor = new DiagnosticDescriptor(
                                id: "BRXVAL002",
                                title: "Schema Validation Warning",
                                messageFormat: "{0}: {1}",
                                category: "Brudixy.Validation",
                                defaultSeverity: severity,
                                isEnabledByDefault: true);
                            
                            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, warning.FilePath, warning.Message));
                        }
                        
                        // Block code generation if validation failed
                        if (!validationResult.IsValid)
                        {
                            // Generate an error file to show validation failed
                            var errorMessage = $"// Schema validation failed for {table}\n" +
                                             $"// {validationResult.Errors.Count} error(s) found\n" +
                                             string.Join("\n", validationResult.Errors.Select(e => $"// - {e.PropertyPath}: {e.Message}"));
                            context.AddSource(Brudixy.TypeGenerator.Core.HintNameHelper.Sanitize($"{Path.GetFileNameWithoutExtension(table)}.ValidationErrors.cs", "ValidationErrors"), SourceText.From(errorMessage, Encoding.UTF8));
                            continue; // Skip code generation for this table
                        }
                    }
                    
                    var files = DataCodeGenerator.GenerateTableFiles(table, fileSystemAccess, schemaReader, callingPath);

                    foreach (var file in files)
                    {
                        lastFileItem = file.Item3;
                        context.AddSource(Brudixy.TypeGenerator.Core.HintNameHelper.Sanitize(file.Item1, Path.GetFileNameWithoutExtension(lastFileItem)), SourceText.From(file.Item2, Encoding.UTF8));
                    }
                }

                foreach (var dataSet in dataSets)
                {
                    // Validate dataset schema before code generation (unless validation is disabled)
                    if (!validationDisabled)
                    {
                        var tableObj = schemaReader.GetTable(dataSet.yaml);
                        var validationContext = new Core.Validation.ValidationContext(tableObj, dataSet.name, fileSystemAccess);
                        var validationResult = validationEngine.Validate(validationContext);
                        
                        // Report validation errors as diagnostics
                        foreach (var error in validationResult.Errors)
                        {
                            var descriptor = new DiagnosticDescriptor(
                                id: "BRXVAL001",
                                title: "Schema Validation Error",
                                messageFormat: "{0}: {1}",
                                category: "Brudixy.Validation",
                                defaultSeverity: DiagnosticSeverity.Error,
                                isEnabledByDefault: true);
                            
                            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, error.FilePath, error.Message));
                        }
                        
                        // Report validation warnings as diagnostics
                        foreach (var warning in validationResult.Warnings)
                        {
                            var severity = strictValidation ? DiagnosticSeverity.Error : DiagnosticSeverity.Warning;
                            var descriptor = new DiagnosticDescriptor(
                                id: "BRXVAL002",
                                title: "Schema Validation Warning",
                                messageFormat: "{0}: {1}",
                                category: "Brudixy.Validation",
                                defaultSeverity: severity,
                                isEnabledByDefault: true);
                            
                            context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, warning.FilePath, warning.Message));
                        }
                        
                        // Block code generation if validation failed
                        if (!validationResult.IsValid)
                        {
                            // Generate an error file to show validation failed
                            var errorMessage = $"// Schema validation failed for {dataSet.name}\n" +
                                             $"// {validationResult.Errors.Count} error(s) found\n" +
                                             string.Join("\n", validationResult.Errors.Select(e => $"// - {e.PropertyPath}: {e.Message}"));
                            context.AddSource(Brudixy.TypeGenerator.Core.HintNameHelper.Sanitize($"{Path.GetFileNameWithoutExtension(dataSet.name)}.ValidationErrors.cs", "ValidationErrors"), SourceText.From(errorMessage, Encoding.UTF8));
                            continue; // Skip code generation for this dataset
                        }
                    }
                    
                    var fileName = Path.GetFileName(dataSet.name);

                    var indexOf = fileName.IndexOf("ds.brudixy.yaml");

                    var dsFileNamePart = fileName.Substring(0, indexOf - 1);

                    var files = DataCodeGenerator.GenerateDatasetFiles(dsFileNamePart, dataSet.name, fileSystemAccess, schemaReader, callingPath);

                    foreach (var file in files)
                    {
                        lastFileItem = file.Item3;
                        context.AddSource(hintName: Brudixy.TypeGenerator.Core.HintNameHelper.Sanitize(file.Item1, Path.GetFileNameWithoutExtension(lastFileItem)), source: file.Item2);
                    }
                }
            }
            catch (Exception e)
            {
                // Generate a readable error file with source information
                var errorContent = new StringBuilder();
                errorContent.AppendLine("// ========================================");
                errorContent.AppendLine("// Brudixy Type Generator Error");
                errorContent.AppendLine("// ========================================");
                errorContent.AppendLine($"// Source File: {lastFileItem}");
                errorContent.AppendLine($"// Error: {e.Message}");
                errorContent.AppendLine("//");
                errorContent.AppendLine("// Stack Trace:");
                foreach (var line in e.StackTrace?.Split('\n') ?? Array.Empty<string>())
                {
                    errorContent.AppendLine($"// {line.Trim()}");
                }
                errorContent.AppendLine("// ========================================");
                
                context.AddSource(Brudixy.TypeGenerator.Core.HintNameHelper.Sanitize("Error.cs", "Error"), SourceText.From(errorContent.ToString(), Encoding.UTF8));
                
                // Also report as diagnostic
                var descriptor = new DiagnosticDescriptor(
                    id: "BRXTY003",
                    title: "Type Generator Error",
                    messageFormat: "Error generating code for {0}: {1}",
                    category: "Brudixy.TypeGenerator",
                    defaultSeverity: DiagnosticSeverity.Error,
                    isEnabledByDefault: true);
                
                context.ReportDiagnostic(Diagnostic.Create(descriptor, Location.None, lastFileItem, e.Message));
            }
        }

        private static Dictionary<string, DataTableObj> LoadBaseTablesForValidation(
            DataTableObj dataTable,
            string fullName,
            IFileSystemAccessor fileSystemAccessor,
            ISchemaReader schemaReader,
            string callingPath)
        {
            var loadedBaseTables = new Dictionary<string, DataTableObj>();
            var baseTableFileName = dataTable.CodeGenerationOptions.BaseTableFileName;

            if (string.IsNullOrEmpty(baseTableFileName))
            {
                return loadedBaseTables;
            }

            var proceeded = new HashSet<string>();

            while (!string.IsNullOrEmpty(baseTableFileName))
            {
                try
                {
                    if (proceeded.Contains(baseTableFileName))
                    {
                        break; // Circular reference detected
                    }

                    proceeded.Add(baseTableFileName);

                    var baseFileName = baseTableFileName;

                    if (baseFileName.StartsWith("\\"))
                    {
                        baseFileName = Path.Combine(Path.GetDirectoryName(fullName), baseTableFileName.TrimStart('\\'));
                    }
                    else if (baseFileName.StartsWith("."))
                    {
                        baseFileName = Path.GetFullPath(baseTableFileName);
                    }

                    var baseYamlContent = fileSystemAccessor.GetFileContents(baseFileName);
                    var baseTable = schemaReader.GetTable(baseYamlContent);
                    baseTable.EnsureDefaults();

                    loadedBaseTables[baseFileName] = baseTable;

                    baseTableFileName = baseTable.CodeGenerationOptions.BaseTableFileName;
                }
                catch (Exception)
                {
                    // If we can't load a base table, just stop trying
                    // The validation will report the error
                    break;
                }
            }

            return loadedBaseTables;
        }

        private static List<(yamlType type, string src, string path)> ReadSchemaFiles(GeneratorExecutionContext context)
        {
            var yamlSources = new List<(yamlType type, string src, string path)>();

            foreach (var file in context.AdditionalFiles)
            {
                var filePath = file.Path;

                var directoryInfo = new DirectoryInfo(filePath);

                if (directoryInfo.Exists)
                {
                    var dirQ = new Queue<DirectoryInfo>();

                    var files = new List<FileInfo>();

                    dirQ.Enqueue(directoryInfo);

                    while (dirQ.Count > 0)
                    {
                        var d = dirQ.Dequeue();

                        files.AddRange(d.GetFiles());

                        foreach (var dir in d.GetDirectories())
                        {
                            dirQ.Enqueue(dir);
                        }
                    }

                    foreach (var f in files)
                    {
                        var ft = GetSchemaType(f.Name);

                        if (ft != yamlType.none)
                        {
                            yamlSources.Add((ft, File.ReadAllText(f.FullName), f.FullName));
                        }
                    }
                }
                else
                {
                    var type = GetSchemaType(filePath);

                    if (type != yamlType.none)
                    {
                        yamlSources.Add((type, file.GetText()?.ToString(), filePath));
                    }
                }
            }

            return yamlSources;
        }

        private static yamlType GetSchemaType(string filePath)
        {
            var singleTable = filePath.EndsWith("st.brudixy.yaml");
            var datasetTable = singleTable == false && filePath.EndsWith("dt.brudixy.yaml");
            var datasetDef = singleTable == false && filePath.EndsWith("ds.brudixy.yaml");

            yamlType type = yamlType.none;

            if (singleTable)
            {
                type = yamlType.singleTabl;
            }
            else if (datasetTable)
            {
                type = yamlType.datasetTab;
            }
            else if (datasetDef)
            {
                type = yamlType.datasetDef;
            }

            return type;
        }

        private class FileSystemAccess : IFileSystemAccessor
        {
            private readonly Dictionary<string, string> m_data;

            public FileSystemAccess(Dictionary<string, string> data)
            {
                m_data = data;
            }
            
            public string GetFileContents(string path)
            {
                if (m_data.TryGetValue(path, out var src))
                {
                    return src;
                }

                return File.ReadAllText(path);
            }
        }
    }
}