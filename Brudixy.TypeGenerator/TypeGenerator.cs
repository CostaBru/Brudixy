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
            
            context.AddSource("Test.log", $"//{DateTime.Now.ToString("MM/dd/yyyy hh:mm:ss")} at " + callingPath);

            string lastFileItem = string.Empty;

            try
            {
                var brudixyFiles = new Dictionary<string, string>();

                var singleTables = new List<string>();
                var dataSets = new List<(string name, string yaml)>();

                var yamlSources = ReadSchemaFiles(context);

                context.AddSource("Files.log", string.Join(Environment.NewLine, yamlSources.Select(s => "//" + s.path)));

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

                var fileSystemAccess = new FileSystemAccess(brudixyFiles);
                var schemaReader = new YamlSchemaReader();
            
                foreach (var table in singleTables)
                {
                    var files = DataCodeGenerator.GenerateTableFiles(table, fileSystemAccess, schemaReader, callingPath);

                    foreach (var file in files)
                    {
                        lastFileItem = file.Item3;
                        
                        context.AddSource(file.Item1, SourceText.From(file.Item2, Encoding.UTF8));
                    }
                }

                foreach (var dataSet in dataSets)
                {
                    var fileName = Path.GetFileName(dataSet.name);

                    var indexOf = fileName.IndexOf("ds.brudixy.yaml");

                    var dsFileNamePart = fileName.Substring(0, indexOf - 1);

                    var files = DataCodeGenerator.GenerateDatasetFiles(dsFileNamePart, dataSet.name, fileSystemAccess, schemaReader, callingPath);

                    foreach (var file in files)
                    {
                        lastFileItem = file.Item3;
                        
                        context.AddSource(hintName: file.Item1, source: file.Item2);
                    }
                }
            }
            catch (Exception e)
            {
                context.AddSource("Error.cs", SourceText.From(lastFileItem + ": " + e, Encoding.UTF8));
            }
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