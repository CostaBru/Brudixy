using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Brudixy.Interfaces.Generators;
using JetBrains.Annotations;

namespace Brudixy.TypeGenerator.Core
{
    public class DataCodeGenerator
    {
        public const string Indend1 = "   ";
        public const string Indend2 = "      ";
        public const string Indend3 = "          ";
        public const string Indend4 = "               ";
        public const string Indend5 = "                   ";
        
        public static IEnumerable<Tuple<string, string, string>> GenerateTableFiles([NotNull] string fullName,
            [NotNull] IFileSystemAccessor fileSystemAccessor,
            [NotNull] ISchemaReader schemaReader,
            string callingPath)
        {
            return GenerateDatasetFiles(string.Empty, fullName, fileSystemAccessor, schemaReader, callingPath);
        }

        private static void LoadBaseTables(string fullName,
            DataTableObj dataTable,
            ISchemaReader schemaReader,
            HashSet<string> skipColumns, 
            IFileSystemAccessor fileSystemAccessor)
        {
            var baseTableFileName = dataTable.CodeGenerationOptions.BaseTableFileName;

            var proceeded = new HashSet<string>();

            var hierarchy = new List<string>();
            
            hierarchy.Add(fullName);

            while (string.IsNullOrEmpty(baseTableFileName) == false)
            {
                try
                {
                    if (proceeded.Contains(baseTableFileName))
                    {
                        break;
                    }

                    proceeded.Add(baseTableFileName);

                    var baseFileName = baseTableFileName;

                    if (baseFileName.StartsWith("\\"))
                    {
                        baseFileName = Path.Combine(Path.GetDirectoryName(fullName), baseTableFileName.TrimStart('\\'));
                    }
                    else if(baseFileName.StartsWith("."))
                    {
                        baseFileName = Path.GetFullPath(baseTableFileName);
                    }
                    
                    hierarchy.Add(baseFileName);

                    var baseTable = schemaReader.GetTable(fileSystemAccessor.GetFileContents(baseFileName));

                    baseTable.EnsureDefaults();

                    foreach (var key in baseTable.Columns.Keys)
                    {
                        skipColumns.Add((string)key);
                    }

                    dataTable.Merge(baseTable, baseTableFileName);
                    
                    baseTableFileName = baseTable.CodeGenerationOptions.BaseTableFileName;
                }
                catch (Exception e)
                {
                    throw new BaseTableParseException(hierarchy, baseTableFileName, e);
                }
            }
        }

        public static IEnumerable<Tuple<string, string, string>> GenerateDatasetFiles(string dsFilePrefix,
            string fullName,
            [NotNull] IFileSystemAccessor fileSystemAccessor,
            [NotNull] ISchemaReader schemaReader,
            string callingPath)
        {
            if (fileSystemAccessor == null) throw new ArgumentNullException(nameof(fileSystemAccessor));
            if (schemaReader == null) throw new ArgumentNullException(nameof(schemaReader));
            
            Tuple<string, string, string> errorFile = null;

            var directoryName = Path.GetDirectoryName(fullName);

            var defaultNameSpace = GetDefaultNameSpace(directoryName, callingPath);

            var dataSet = new DataTableObj();

            var tableOptions = new Dictionary<string, DataTableObj>();
            var skipRelations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            var tableFiles = new Dictionary<string, string>();

            try
            {
                dataSet = schemaReader.GetDataSet(fileSystemAccessor.GetFileContents(fullName)); 
                
                dataSet.EnsureDefaults();

                if (string.IsNullOrEmpty(dataSet.CodeGenerationOptions.Namespace))
                {
                    dataSet.CodeGenerationOptions.Namespace = defaultNameSpace;
                }
                
                if (string.IsNullOrEmpty(dataSet.CodeGenerationOptions.InterfaceNamespace))
                {
                    dataSet.CodeGenerationOptions.InterfaceNamespace = defaultNameSpace;
                }
            }
            catch (Exception exception)
            {
                errorFile = new Tuple<string, string, string>($"{dataSet.Namespace}_{dataSet.Table}.Error", exception.ToString(), fullName);
            }

            if (errorFile != null)
            {
                yield return errorFile;

                yield break;
            }

            foreach (var tuple in GenerateDatasetCode(fullName, 
                         fileSystemAccessor, 
                         schemaReader,
                         dataSet, 
                         tableOptions,
                         skipRelations, 
                         tableFiles,
                         directoryName,
                         dsFilePrefix))
            {
                yield return tuple;
            }
        }

        private static string GetDefaultNameSpace(string fullName, string callingPath)
        {
            if(string.IsNullOrEmpty(callingPath))
            {
                return string.Empty;
            }
            
            var directoryInfo = Directory.GetParent(callingPath);

            if (directoryInfo == null)
            {
                return string.Empty;
            }
            
            var callingPathLength = directoryInfo.Parent?.FullName.Length ?? 0;

            var defaultNameSpace = fullName.Remove(0, callingPathLength).TrimStart('\\').Replace('\\', '.');
            return defaultNameSpace;
        }

        private static IEnumerable<Tuple<string, string, string>> GenerateDatasetCode(string fullName,
            IFileSystemAccessor fileSystemAccessor,
            ISchemaReader schemaReader, 
            DataTableObj dataSet,
            Dictionary<string, DataTableObj> tableOptions,
            HashSet<string> skipRelations, 
            Dictionary<string, string> tableFiles,
            string directoryName,
            string dsFilePrefix)
        {
            var queue = new Queue<DataTableObj>();
            queue.Enqueue(dataSet);

            while (queue.Any())
            {
                var dataTable = queue.Dequeue();
                
                var tableSkipColumns = new Dictionary<string, HashSet<string>>();

                foreach (var tuple in GenerateTableCode(fullName, 
                             fileSystemAccessor, 
                             schemaReader,
                             tableOptions, 
                             skipRelations, 
                             tableFiles, 
                             directoryName, 
                             dsFilePrefix, 
                             dataTable,
                             tableSkipColumns, 
                             dataTable.Namespace, 
                             dataTable.Table))
                {
                    yield return tuple;
                }

                foreach (var subTables in dataTable.TablesObjects)
                {
                    DataTableObj subTable = tableOptions[subTables.Key];

                    queue.Enqueue(subTable);
                }
            }
           
        }

        private static IEnumerable<Tuple<string, string, string>> GenerateTableCode(string fullName, 
            IFileSystemAccessor fileSystemAccessor,
            ISchemaReader schemaReader,
            Dictionary<string, DataTableObj> tableOptions,
            HashSet<string> skipRelations, 
            Dictionary<string, string> tableFiles,
            string directoryName, 
            string dsFilePrefix,
            DataTableObj dataTable, 
            Dictionary<string, HashSet<string>> tableSkipColumns, 
            string dataSetNamespace,
            string dataSetName)
        {
            Tuple<string, string, string> errorFile = null;

            var skipColumns = tableSkipColumns.GetOrAdd(dataTable.Table, () => new HashSet<string>());

            try
            {
                LoadBaseTables(fullName, dataTable, schemaReader, skipColumns, fileSystemAccessor);
            }
            catch (Exception e)
            {
                var fn = fullName;

                if (e is BaseTableParseException bs)
                {
                    fn = bs.FileName;
                }

                errorFile = new Tuple<string, string, string>($"{dataSetNamespace}_{dataTable.Table}.BaseTableParse.Error",
                    $"{fullName}:{fn}:{e}", fn);
            }

            if (errorFile != null)
            {
                yield return errorFile;
                yield break; // yield break;
            }

            var tableFilePrefix = string.IsNullOrEmpty(dsFilePrefix) ? string.Empty : dsFilePrefix + ".";

            foreach (var tableProp in dataTable.TablesObjects)
            {
                tableProp.Value.EnsureDefaults(tableProp.Key);

                var fileName = Path.Combine(directoryName, tableFilePrefix + tableProp.Value.FileName + ".dt.brudixy.yaml");

                try
                {
                    var nestedTable = schemaReader.GetTable(fileSystemAccessor.GetFileContents(fileName));

                    nestedTable.EnsureDefaults();
                    nestedTable.UpdateRelations(dataTable);

                    if (string.IsNullOrEmpty(nestedTable.Namespace))
                    {
                        nestedTable.CodeGenerationOptions.Namespace = dataTable.Namespace;
                    }

                    if (string.IsNullOrEmpty(nestedTable.InterfaceNamespace))
                    {
                        nestedTable.CodeGenerationOptions.InterfaceNamespace = dataTable.Namespace;
                    }

                    tableFiles[tableProp.Key] = fileName;

                    foreach (var relationObj in dataTable.Relations)
                    {
                        if (relationObj.Value.ChildTable == tableProp.Key || relationObj.Value.ParentTable == tableProp.Key)
                        {
                            skipRelations.Add(relationObj.Key);
                        }
                    }

                    tableOptions[tableProp.Key] = nestedTable;
                }
                catch (Exception e)
                {
                    errorFile = new Tuple<string, string, string>(
                        $"{dataTable.Namespace}{dsFilePrefix}_{dataSetName}_{dataTable.Table}.Error", $"{fileName}:{e}", fileName);

                    break;
                }
            }

            if (errorFile != null)
            {
                yield return errorFile;
                yield break; // yield break;
            }

            yield return GenerateTableClassFile(fullName,
                dataTable,
                $"{dataSetNamespace}_", tableOptions,
                skipColumns: skipColumns,
                skipRelations:
                skipRelations,
                dataSetNamespace);

            if (dataTable.HasBaseClass || HasAnyColumnToGenerate(dataTable, skipColumns))
            {
                string rowInterface = string.Empty;

                try
                {
                    rowInterface = GenerateRowInterface(fullName, dataTable, skipColumns);
                }
                catch (Exception e)
                {
                    errorFile = new Tuple<string, string, string>(
                        $"{dataSetNamespace}_{dataSetName}_{dataTable.Table}_TableRowInterfaceCodeGen.Error",
                        $"{fullName}:{tableFiles[dataTable.Table]}:{e}", tableFiles[dataTable.Table]);
                }

                if (errorFile != null)
                {
                    yield return errorFile;

                    errorFile = null;
                }
                else
                {
                    var subNamespace = string.Empty;

                    if (dataSetNamespace != dataTable.Namespace)
                    {
                        subNamespace = $"{dataTable.Namespace}_";
                    }

                    yield return new Tuple<string, string, string>(
                        $"{dataSetNamespace}_{subNamespace}{dataTable.Class}.RowInterfaces",
                        rowInterface,
                        dataTable.Table);
                }
            }
        }

        private static string GenerateRowInterface(string fullName, DataTableObj table,
            HashSet<string> skipColumns)
        {
            var stringBuilder = new StringBuilder();

            CreateHeader(fullName, table.InterfaceNamespace, stringBuilder, table.InterfaceNamespace, table.CodeGenerationOptions.ExtraUsing);
            
            stringBuilder.AppendLine(@$"{{");

            stringBuilder.AppendLine(@$"{Indend1}///<summary>Public interface for '{XmlConvert.EncodeName(table.Table)}' table row.</summary>");
            AddCodeGenAttr(stringBuilder, Indend1);
            stringBuilder.AppendLine(@$"{Indend1}public partial interface I{table.RowClass}Accessor: I{table.RowClass}ReadOnlyAccessor, global::{table.BaseInterfaceNamespace}.I{table.BaseRowClass}Accessor");
            stringBuilder.AppendLine(@$"{Indend1}{{");
            
            foreach (var column in table.ColumnObjects)
            {
                if (skipColumns.Contains(column.Key))
                {
                    continue;
                }

                if (column.Value.IsReadOnly ?? false)
                {
                    continue;
                }

                WriteFieldPropertyComment(stringBuilder, column.Value, column.Key, Indend2);

                var typeString = GetGenTypeString(column.Value);
                
                var propertyName = column.Value.CodeProperty ?? column.Key;
                
                stringBuilder.AppendLine($"{Indend2}new {typeString} @{propertyName} {{ get; set; }}");
            }
            
            foreach (var kv in table.GroupColumnObjects)
            {
                var groupOptions = kv.Value;
                var groupName = kv.Key;

                if (skipColumns.Contains(groupName) || groupOptions.Columns.Count <= 1)
                {
                    continue;
                }
                
                if (groupOptions.IsReadOnly)
                {
                    continue;
                }

                var (tupleDef, tupleCtr, tupleSet) = GetGroupColumnSetup(table, groupOptions);
                
                WriteGroupFieldPropertyComment(stringBuilder, groupOptions, groupOptions.Name, Indend2);

                var groupPropertyName = groupOptions.Name;

                stringBuilder.Append($"{Indend2}new {tupleDef} @{groupPropertyName} ");
                stringBuilder.Append($"{{");

                stringBuilder.Append($"get; set;");

                stringBuilder.Append($"}}");
                stringBuilder.AppendLine();
            }

            stringBuilder.AppendLine(@$"{Indend1}}}");

            stringBuilder.AppendLine();
            
            stringBuilder.AppendLine(@$"{Indend1}///<summary>Public interface for '{XmlConvert.EncodeName(table.Table)}' table row readonly access.</summary>");
            AddCodeGenAttr(stringBuilder, Indend1);
            stringBuilder.AppendLine(@$"{Indend1}public partial interface I{table.RowClass}ReadOnlyAccessor: global:: {table.BaseInterfaceNamespace}.I{table.BaseRowClass}ReadOnlyAccessor");
            
            stringBuilder.AppendLine(@$"{Indend1}{{");
            
            foreach (var column in table.ColumnObjects)
            {
                if (skipColumns.Contains(column.Key))
                {
                    continue;
                }


                var typeString = GetGenTypeString(column.Value);

                var propertyName = column.Value.CodeProperty ?? column.Key;
                
                WriteFieldPropertyComment(stringBuilder, column.Value, column.Key, Indend2, forceReadonly: true);

                stringBuilder.AppendLine($"{Indend2}new {typeString} @{propertyName} {{ get; }}");
                
                if (column.Value.TypeModifier == "Array")
                {
                    var genericTypeString = GetGenericArrayType(typeString);
                    
                    var linkTypeString = $"IReadOnlyList<{genericTypeString}>";
                
                    WriteFieldPropertyComment(stringBuilder, column.Value, column.Key, Indend2, forceReadonly: true);

                    stringBuilder.AppendLine($"{Indend2}new {linkTypeString} @{propertyName}Link {{ get; }}");
                }
            }
            
            foreach (var kv in table.GroupColumnObjects)
            {
                var groupOptions = kv.Value;
                var groupName = kv.Key;

                if (skipColumns.Contains(groupName) || groupOptions.Columns.Count <= 1)
                {
                    continue;
                }

                var (tupleDef, tupleCtr, tupleSet) = GetGroupColumnSetup(table, groupOptions);
                
                WriteGroupFieldPropertyComment(stringBuilder, groupOptions, groupOptions.Name, Indend2, true);

                var groupPropertyName = groupOptions.Name;

                stringBuilder.Append($"{Indend2}new {tupleDef} @{groupPropertyName} ");
                stringBuilder.Append($"{{");

                stringBuilder.Append($"get; ");

                stringBuilder.Append($"}}");
                stringBuilder.AppendLine();
            }

            stringBuilder.AppendLine(@$"{Indend1}}}");

            stringBuilder.AppendLine(@"}");

            return stringBuilder.ToString();
        }

        private static Tuple<string, string, string> GenerateTableClassFile(string fullName,
            DataTableObj table,
            string datasetPrefix,
            Dictionary<string, DataTableObj> dsTablesOptions = null,
            HashSet<string> skipColumns = null,
            HashSet<string> skipRelations = null, 
            string dataSetNamespace = null)
        {
            string tableCode = string.Empty;

            Tuple<string, string, string> errorFile = null;

            try
            {
                tableCode = GenerateTableCode(fullName, table, dsTablesOptions, skipColumns, skipRelations).ToString();
            }
            catch (Exception e)
            {
                errorFile = new Tuple<string, string, string>(datasetPrefix + table.Class + "TableCodeGen.Error", $"{fullName}:{e}", fullName);
            }

            if (errorFile != null)
            {
                return errorFile;
            }

            if (table.Class == null)
            {
                throw new InvalidOperationException($"table class prop is null. {fullName}");
            }
            
            var subNamespace = string.Empty;

            if (dataSetNamespace != table.Namespace)
            {
                subNamespace = $"_{table.Namespace}_";
            }

            return new Tuple<string, string, string>($"{datasetPrefix}{subNamespace}{table.Class}", tableCode, fullName);
        }

        private static void GenerateCopyClone(StringBuilder stringBuilder, string className)
        {
            stringBuilder.AppendLine($"{Indend2}/// <inheritdoc />");
            stringBuilder.AppendLine($"{Indend2}public new {className} Copy()");
            stringBuilder.AppendLine($"{Indend2}{{");
            stringBuilder.AppendLine($"{Indend3}return ({className})base.Copy();");
            stringBuilder.AppendLine($"{Indend2}}}");

            stringBuilder.AppendLine($"{Indend2}/// <inheritdoc />");
            stringBuilder.AppendLine($"{Indend2}public new {className} Clone()");
            stringBuilder.AppendLine($"{Indend2}{{");
            stringBuilder.AppendLine($"{Indend3}return ({className}) base.Clone();");
            stringBuilder.AppendLine($"{Indend2}}}");
        }

        public static StringBuilder GenerateTableCode(string fileFullName, 
            DataTableObj table,
            Dictionary<string, DataTableObj> dsTablesOptions = null,
            HashSet<string> skipColumns = null,
            HashSet<string> skipRelations = null)
        {
            var stringBuilder = new StringBuilder();

            CreateHeader(fileFullName, table.Namespace, stringBuilder, table.InterfaceNamespace, table.CodeGenerationOptions.ExtraUsing);

            stringBuilder.AppendLine(@"{");

            GenerateTable(stringBuilder, table, dsTablesOptions, skipColumns ?? new HashSet<string>(), skipRelations ?? new HashSet<string>());

            stringBuilder.AppendLine(@"}");

            return stringBuilder;
        }

        private static void CreateHeader(string fileFullName,
            string nameSpace,
            StringBuilder stringBuilder,
            string interfaceNamespace,
            List<string> extraUsing = null)
        {
            stringBuilder
                .AppendLine("using System;")
                .AppendLine("using System.Collections.Generic;")
                .AppendLine("using System.Linq;")
                .AppendLine("using System.Text;")
                .AppendLine("using System.Xml.Linq;")
                .AppendLine("using Konsarpoo.Collections;")
                .AppendLine("using JetBrains.Annotations;")
                .AppendLine("using Brudixy;")
                .AppendLine("using Brudixy.Converter;")
                .AppendLine("using Brudixy.Exceptions;")
                .AppendLine("using Brudixy.Interfaces;");

            if (string.IsNullOrEmpty(interfaceNamespace) == false)
            {
                stringBuilder.AppendLine($"using {interfaceNamespace};");
            }

            if (extraUsing != null)
            {
                foreach (var extraItem in extraUsing)
                {
                    stringBuilder.AppendLine(extraItem);
                }
            }

            stringBuilder.AppendLine($"//datasource:{fileFullName}");
            stringBuilder.Append($"namespace {nameSpace}").AppendLine();
        }

        private static void GenerateTableProperties(DataTableObj dataSet, 
            StringBuilder stringBuilder,
            Dictionary<string, DataTableObj> tableOptions)
        {
            foreach (var table in dataSet.TablesObjects)
            {
                var @class = tableOptions[table.Key].Class;

                stringBuilder.AppendLine($"{Indend2}///<summary>Gets access to '{XmlConvert.EncodeName(table.Key)}' table.</summary>");
                stringBuilder.AppendLine($"{Indend2}public readonly @{@class} @{table.Value.CodeProperty}; ");
            }
        }

        private static void GenerateDatasetCtrCore(DataTableObj dataSet, StringBuilder stringBuilder, Dictionary<string, DataTableObj> tableOptions)
        {
            stringBuilder.AppendLine($@"{Indend3}this.TableName = ""{dataSet.Table}"";");
            stringBuilder.AppendLine($@"{Indend3}this.Namespace = ""{dataSet.Namespace}"";");
            
            int index = 0;

            var tableVarMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            foreach (var table in dataSet.TablesObjects)
            {
                var tableVar = $"this.@{table.Value.CodeProperty}";

                stringBuilder.AppendLine(
                    $"{Indend3}this.AddTable({tableVar} = new {tableOptions[table.Key].Class}() {{ IsBuildin = true }}) ;");

                tableVarMap[table.Key] = tableVar;

                index++;
            }

            if (tableVarMap.Any())
            {
                GenerateDatasetRelationsInit(dataSet, stringBuilder, tableVarMap);
            }
            
            var enf = dataSet.EnforceConstraints ? "true" : "false";
            stringBuilder.AppendLine($@"{Indend3}this.EnforceConstraints = {enf};");
        }

        private static void GenerateDatasetRelationsInit(DataTableObj dataSet, StringBuilder stringBuilder, Dictionary<string, string> tableVarMap)
        {
            var relations = dataSet.Relations;

            var ri = 0;

            foreach (var kv in relations)
            {
                var relation = kv.Value;
                
                var relationNameString = $"\"{kv.Key}\"";

                var parentTable = relation.ParentTable;
                var childTable = relation.ChildTable;

                var t1 = tableVarMap[parentTable];
                var t2 = tableVarMap[childTable];

                if (relation.ParentKey.Length == 1)
                {
                    stringBuilder.AppendLine($"{Indend3}this.AddRelation(new DataRelation({relationNameString}, {t1}.{GetColumnFieldName(relation.ParentKey[0])}, {t2}.{GetColumnFieldName(relation.ChildKey[0])}));");
                }
                else
                {
                    stringBuilder.AppendLine($"{Indend3}var _tmpCols1P{ri} = new CoreDataColumn[{relation.ParentKey.Length}];");
                    stringBuilder.AppendLine($"{Indend3}var _tmpCols2P{ri} = new CoreDataColumn[{relation.ParentKey.Length}];");

                    for (int i = 0; i < relation.ParentKey.Length; i++)
                    {
                        stringBuilder.AppendLine($"{Indend3}_tmpCols1P{ri}[{i}] = {t1}.{GetColumnFieldName(relation.ParentKey[i])};");
                        stringBuilder.AppendLine($"{Indend3}_tmpCols2P{ri}[{i}] = {t2}.{GetColumnFieldName(relation.ChildKey[i])};");
                    }

                    stringBuilder.AppendLine($"{Indend3}this.AddRelation(new Brudixy.DataRelation({relationNameString}, _tmpCols1P{ri}, _tmpCols2P{ri}));");
                }

                ri++;
            }
        }

        
        private static void GenerateTable(StringBuilder stringBuilder,
            DataTableObj table,
            Dictionary<string, DataTableObj> tableOptions,
            HashSet<string> skipColumns, 
            HashSet<string> skipRelations)
        {
            var baseDataRowClassFull = $"{table.BaseNamespace}.{table.BaseRowClass}";
            var baseClassFull =  $"{table.BaseNamespace}.{table.BaseClass}";

            var abstractDef = table.Abstract ? " abstract" : string.Empty;

            var sealedDef = table.Sealed ? " sealed " : string.Empty;

            stringBuilder.AppendLine($"{Indend1}///<summary>Public typed '{XmlConvert.EncodeName(table.Table)}' data table class.</summary>");
            AddCodeGenAttr(stringBuilder, Indend1);
            
            stringBuilder.AppendLine(
                $"{Indend1}[System.Diagnostics.DebuggerDisplay(\"{{Name}}, Rows: {{RowCount}}, Columns: {{ColumnCount}}, PK: {{PkDebug}}, Thread: {{SourceThread.Name}}, Indexed: {{IndexInfo.HasAny}}, MI {{MultiColumnIndexInfo.HasAny}}, RO = {{TableIsReadOnly}}\")]");
            
            stringBuilder.AppendLine(
                $"{Indend1}public{abstractDef}{sealedDef} partial class {table.Class} : global::{baseClassFull}");

            stringBuilder.AppendLine($"{Indend1}{{");

            GenerateTableProperties(table, stringBuilder, tableOptions);

            GenerateXProperties(stringBuilder, table.XProperties, null);

            var hasAnyColumnToGenerate = HasAnyColumnToGenerate(table, skipColumns);

            GenerateTableCtr(stringBuilder, table, table.Class, skipColumns, skipRelations, tableOptions, hasAnyColumnToGenerate);
          
            stringBuilder.AppendLine();
            
            if (hasAnyColumnToGenerate || table.HasBaseClass)
            {
                GenerateColumnClass(stringBuilder, table, sealedDef);

                GenerateColumnClassOverride(stringBuilder, table);
                
                GenerateColumnFields(stringBuilder, table, skipColumns);

                GenerateGetRowsByIndexes(stringBuilder, table);

                GenerateAppendRow(stringBuilder, table);
                
                GenerateCopyData(stringBuilder, table, table.RowClass, skipColumns);
                
                GenerateCopyAll(stringBuilder, table, table.RowClass, skipColumns);
                
                GenerateColumnsInitResetState(stringBuilder, table, skipColumns);
            }
            
            if (table.Abstract == false)
            {
                GenerateCopyClone(stringBuilder, table.Class);

                if (hasAnyColumnToGenerate || table.HasBaseClass)
                {
                    GenerateOverloads(stringBuilder, table.RowClass);

                    GenerateCreateInstanceOverloads(stringBuilder, table.RowClass);
                }
            }

            stringBuilder.AppendLine(@$"{Indend1}}}");

            stringBuilder.AppendLine();

            if (hasAnyColumnToGenerate || table.HasBaseClass)
            {
                GenerateRowClass(stringBuilder, table, table.RowClass, baseDataRowClassFull, table.Class, tableOptions, skipColumns);

                GenerateRowContainerClass(stringBuilder, table, table.RowClass, table.Class, baseDataRowClassFull, table.Class, skipColumns);
            }

            stringBuilder.AppendLine();
        }

        private static void AddCodeGenAttr(StringBuilder stringBuilder, string indent)
        {
            stringBuilder.AppendLine($"{indent}[System.CodeDom.Compiler.GeneratedCodeAttribute(\"Brudixy.TypeGenerator\", \"1.0\")]");
        }

        private static void GenerateAppendRow(StringBuilder stringBuilder, DataTableObj table)
        {
            if (table.ColumnObjects.Any() && table.CodeGenerationOptions.Abstract == false)
            {
                if (table.BaseLoadedTableName != "DataTable" && table.BaseLoadedTableName != "CoreDataTable" && string.IsNullOrEmpty(table.CodeGenerationOptions.BaseTableFileName))
                {
                    return;
                }
                
                var paramDef = new List<string>();
                var callDef = new List<string>();

                var notNullCols = new List<KeyValuePair<string, ColumnInfo>>();
                var nullCols = new List<KeyValuePair<string, ColumnInfo>>();

                foreach (var kv in table.ColumnObjects)
                {
                    var column = kv.Value;

                    if (column.IsReadOnly ?? false)
                    {
                        continue;
                    }

                    if (column.AllowNull ?? false)
                    {
                        nullCols.Add(kv);
                    }
                    else
                    {
                        notNullCols.Add(kv);
                    }
                }

                FillAppenderParams(table.Table, notNullCols, paramDef, callDef);
                
                FillAppenderParams(table.Table, nullCols, paramDef, callDef);

                if (paramDef.Count > 0)
                {
                    var methodName = table.CodeGenerationOptions.AppendRowMethodName ?? "Append";

                    var paramDefStr = string.Join(",", paramDef);
                    var callDefStr = string.Join(string.Empty, callDef);

                    stringBuilder.AppendLine(
                        $"{Indend2}///<summary>Appends a new row.</summary>");
                    stringBuilder.AppendLine(
                        $"{Indend2}public new {table.Class} {methodName}({paramDefStr}) {{ var r = this.NewRow(); {callDefStr} this.AddRow(r); return this; }}");
                }
            }
        }

        private static void FillAppenderParams(string tableName, List<KeyValuePair<string, ColumnInfo>> cols, List<string> paramDef, List<string> callDef)
        {
            foreach (var kv in cols)
            {
                var column = kv.Value;
                var columnName = kv.Key;

                if (column.IsReadOnly ?? false)
                {
                    continue;
                }
                
                if(column.Auto ?? false)
                {
                    continue;
                }

                var typeString = GetGenTypeString(column, out var isStruct);

                var propertyName = column.CodeProperty ?? columnName;

                var nullable = isStruct && (column.AllowNull ?? false);

                if (column.AllowNull ?? false)
                {
                    paramDef.Add($"{typeString} @{propertyName} = default");
                }
                else
                {
                    paramDef.Add($"{typeString} @{propertyName}");
                }

                if (nullable)
                {
                    callDef.Add($" if (@{propertyName} != null) r.@{propertyName} = @{propertyName}.Value;");
                }
                else
                {
                    if (isStruct)
                    {
                        callDef.Add($" r.@{propertyName} = @{propertyName};");
                    }
                    else
                    {
                        callDef.Add($" if (@{propertyName} != null) r.@{propertyName} = @{propertyName};");
                    }
                    
                }
            }
        }

        private static void GenerateGetRowsByIndexes(StringBuilder stringBuilder, DataTableObj table)
        {
            var singlePkCol = string.Empty;

            var columns = table.PrimaryKey;

            if (columns.Any())
            {
                if (columns.Count == 1)
                {
                    var pk = columns[0];

                    var col = table.ColumnObjects[pk];

                    var type = GetGenTypeString(col);

                    stringBuilder.AppendLine($"{Indend2}///<summary>Returns a row using a primary key index search.</summary>");
                    stringBuilder.AppendLine($"{Indend2}public new {table.RowClass} GetRowByPk({type} @{pk}) => ({table.RowClass})base.GetRowBySinglePk(@{pk});");

                    singlePkCol = pk;
                }
                else
                {
                    var passParamBuilder = new StringBuilder();
                    var paramBuilder = new StringBuilder();

                    var primaryKeyCount = columns.Count;

                    passParamBuilder.Append($"new IComparable[{primaryKeyCount}] {{");

                    for (var index = 0; index < primaryKeyCount; index++)
                    {
                        var pkCol = columns[index];
                        
                        var col = table.ColumnObjects[pkCol];

                        var type = GetGenTypeString(col).TrimEnd('?');

                        passParamBuilder.Append($"@{pkCol}");

                        paramBuilder.Append($"{type} @{pkCol}");

                        if (index + 1 < primaryKeyCount)
                        {
                            passParamBuilder.Append(",");
                            paramBuilder.Append(", ");
                        }
                    }

                    passParamBuilder.Append($"}}");

                    stringBuilder.AppendLine($"{Indend2}///<summary>Returns a row using a primary key index search.</summary>");
                    stringBuilder.AppendLine($"{Indend2}public new {table.RowClass} GetRowByPk({paramBuilder}) => ({table.RowClass})base.GetRowByMultiColPk({passParamBuilder});");
                }
            }

            var generatedRowGetters = new HashSet<string>();

            foreach (var kv in table.Indexes)
            {
                var colIndexes = kv.Value;
                
                var unique = colIndexes.Unique;

                if (colIndexes.Columns.Count == 1)
                {
                    var column = colIndexes.Columns[0];

                    if (generatedRowGetters.Contains(column))
                    {
                        continue;
                    }

                    generatedRowGetters.Add(column);

                    var tableColumn = table.ColumnObjects[column];

                    var type = GetGenTypeString(tableColumn, out var isStruct);

                    var generateColName = kv.Key ?? column + "Column";

                    var methodNameSuffix =
                        generateColName.Substring(0, 1).ToUpper()[0] + generateColName.Substring(1, generateColName.Length - 1);

                    if (unique)
                    {
                        stringBuilder.AppendLine($"{Indend2}///<summary>Returns rows using a '{XmlConvert.EncodeName(column)}' index search.</summary>");
                        stringBuilder.AppendLine($"{Indend2}public new {table.RowClass} GetRowBy{methodNameSuffix}({type.TrimEnd('?')} @{column}) => ({table.RowClass})base.GetRow(\"{column}\", @{column});");
                    }
                    else
                    {
                        GenerateRowsGetter(stringBuilder, table, isStruct, type, tableColumn, column, methodNameSuffix);
                    }
                }
                else
                {
                    if (string.IsNullOrEmpty(kv.Key))
                    {
                        continue;
                    }

                    var passParamBuilder = new StringBuilder();
                    var paramBuilder = new StringBuilder();
                    var columnBuilder = new StringBuilder();

                    var keyCount = colIndexes.Columns.Count;

                    passParamBuilder.Append($"new IComparable[{keyCount}] {{");
                    columnBuilder.Append($"new string[{keyCount}] {{");

                    for (var index = 0; index < keyCount; index++)
                    {
                        var column = colIndexes.Columns[index];
                        var col = table.ColumnObjects[column];

                        var type = GetGenTypeString(col);

                        if (unique)
                        {
                            type = type.TrimEnd('?');
                        }
                        
                        passParamBuilder.Append($"@{column}");

                        paramBuilder.Append($"{type} @{column}");

                        columnBuilder.Append($"\"{column}\"");

                        if (index + 1 < keyCount)
                        {
                            passParamBuilder.Append(",");
                            paramBuilder.Append(", ");
                            columnBuilder.Append(", ");
                        }
                    }

                    passParamBuilder.Append($"}}");
                    columnBuilder.Append($"}}");

                    var methodNameSuffix =
                        kv.Key.Substring(0, 1).ToUpper()[0] +
                        kv.Key.Substring(1, kv.Key.Length - 1);


                    if (unique)
                    {
                        stringBuilder.AppendLine($"{Indend2}///<summary>Returns a row using a '{XmlConvert.EncodeName(kv.Key)}' unique index search.</summary>");

                        stringBuilder.AppendLine(
                            $"{Indend2}public new {table.RowClass} GetRowBy{methodNameSuffix}({paramBuilder}) => base.GetRowsByArray<{table.RowClass}>({columnBuilder}, {passParamBuilder}).FirstOrDefault();");
                    }
                    else
                    {
                        stringBuilder.AppendLine($"{Indend2}///<summary>Returns rows using a '{XmlConvert.EncodeName(kv.Key)}' index search.</summary>");
                        stringBuilder.AppendLine(
                            $"{Indend2}public new IEnumerable<{table.RowClass}> GetRowsBy{methodNameSuffix}({paramBuilder}) => base.GetRowsByArray<{table.RowClass}>({columnBuilder}, {passParamBuilder});");

                    }
                }
            }
            
            foreach (var kv in table.ColumnObjects)
            {
                var column = kv.Value;
                var columnName = kv.Key;
                
                if (column.HasIndex == false || kv.Key == singlePkCol)
                {
                    continue;
                }
                
                if (generatedRowGetters.Contains(kv.Key))
                {
                    continue;
                }

                generatedRowGetters.Add(kv.Key);

                var type = GetGenTypeString(column, out var isStruct);

                var methodNameSuffix =
                    kv.Key.Substring(0, 1).ToUpper()[0] + kv.Key.Substring(1, kv.Key.Length - 1) +
                    "Column";

                if (column.IsUnique ?? false)
                {
                    stringBuilder.AppendLine($"{Indend2}///<summary>Returns a row using a '{kv.Key}' unique index search.</summary>");
                    
                    stringBuilder.AppendLine(
                        $"{Indend2}public new {table.RowClass} GetRowBy{methodNameSuffix}({type} key) => ({table.RowClass})base.GetRow(\"{columnName}\", key);");
                }
                else
                {
                    GenerateRowsGetter(stringBuilder, table, isStruct, type, column, columnName, methodNameSuffix);
                }
            }
        }

        private static void GenerateRowsGetter(StringBuilder stringBuilder, 
            DataTableObj table,
            bool isStruct,
            string type,
            ColumnInfo column,
            string columName,
            string generateColumnName)
        {
            stringBuilder.AppendLine($"{Indend2}///<summary>Returns rows using a '{XmlConvert.EncodeName(columName)}' index search.</summary>");
            if ((column.AllowNull ?? false) && isStruct)
            {
                stringBuilder.AppendLine(
                    $"{Indend2}public new IEnumerable<{table.RowClass}> GetRowsBy{generateColumnName}({type} key) => key.HasValue ? base.GetRows(\"{columName}\", key.Value).OfType<{table.RowClass}>() : base.GetRowsWhereNull(\"{columName}\").OfType<{table.RowClass}>();");
            }
            else
            {
                stringBuilder.AppendLine(
                    $"{Indend2}public new IEnumerable<{table.RowClass}> GetRowsBy{generateColumnName}({type} key) => base.GetRows(\"{columName}\", key).OfType<{table.RowClass}>();");
            }
        }

        private static bool HasAnyColumnToGenerate(DataTableObj table, HashSet<string> skipColumns)
        {
            return table.ColumnObjects.Count > 0 && table.ColumnObjects.Any(c => skipColumns.Contains(c.Key) == false);
        }

        private static void GenerateCopyData(StringBuilder stringBuilder, DataTableObj table, string tableRowClassName,
            HashSet<string> skipColumns)
        {
            stringBuilder.AppendLine();

            stringBuilder.AppendLine(
                $@"{Indend2}internal static bool CopyData(I{tableRowClassName}Accessor copyTo, ICoreDataRowReadOnlyAccessor copyFrom, IReadOnlyCollection<string> skipFields = null)");

            stringBuilder.AppendLine($@"{Indend2}{{");

            stringBuilder.AppendLine($@"{Indend3}if (copyFrom is I{tableRowClassName}ReadOnlyAccessor br)");
            stringBuilder.AppendLine($@"{Indend3}{{");

            var list = new List<(string propName, string copyCode)>();

            foreach (var kv in table.ColumnObjects)
            {
                var column = kv.Value;
                var columnName = kv.Key;
                
                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                if (column.IsReadOnly ?? false)
                {
                    continue;
                }
                
                var propertyName = column.CodeProperty ?? columnName;
                
                list.Add((columnName, $"copyTo.@{propertyName} = br.@{propertyName};"));
            }

            if (list.Any())
            {
                stringBuilder.Append($"{Indend4}if (skipFields == null)");
                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"{Indend4}{{");

                foreach (var code in list)
                {
                    stringBuilder.AppendLine($"{Indend5}    {code.copyCode}");
                }

                stringBuilder.AppendLine($"{Indend4}}}");
                stringBuilder.AppendLine($"{Indend4}else");
                stringBuilder.AppendLine($"{Indend4}{{");
                foreach (var code in list)
                {
                    stringBuilder.AppendLine($"{Indend5}    if (!skipFields.Contains(\"{code.propName}\")) {{ {code.copyCode} }}");
                }

                stringBuilder.AppendLine($"{Indend4}}}");
            }

            stringBuilder.AppendLine(@$"{Indend4}return true;");

            stringBuilder.AppendLine($@"{Indend3}}}");
            stringBuilder.AppendLine($@"{Indend3}return false;");     

            stringBuilder.AppendLine($@"{Indend2}}}");
        }
        
        private static void GenerateCopyAll(StringBuilder stringBuilder, DataTableObj table, string tableRowClassName,
            HashSet<string> skipColumns)
        {
            stringBuilder.AppendLine();

            stringBuilder.AppendLine(
                $@"{Indend2}internal static void CopyAll(I{tableRowClassName}Accessor copyTo, ICoreDataRowReadOnlyAccessor copyFrom)");

            stringBuilder.AppendLine($@"{Indend2}{{");

            stringBuilder.AppendLine($@"{Indend3}if (copyFrom is I{tableRowClassName}ReadOnlyAccessor br)");
            stringBuilder.AppendLine($@"{Indend3}{{");

            var list = new List<(string propName, string copyCode)>();

            foreach (var kv in table.ColumnObjects)
            {
                var column = kv.Value;
                var columnName = kv.Key;
                
                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                if (column.IsReadOnly ?? false)
                {
                    continue;
                }
                
                var propertyName = column.CodeProperty ?? columnName;
                
                list.Add((columnName, $"copyTo.@{propertyName} = br.@{propertyName};"));
            }

            if (list.Any())
            {
                stringBuilder.AppendLine();

                foreach (var code in list)
                {
                    stringBuilder.AppendLine($"{Indend4} {code.copyCode}");
                }
            }

            stringBuilder.AppendLine($@"{Indend3}}}");
            stringBuilder.AppendLine($@"{Indend2}}}");
        }

        public static void GenerateColumnClass(StringBuilder stringBuilder, DataTableObj table, string sealedDef)
        {
            var generationOptions = table.CodeGenerationOptions;
            
            var colClass = GetColumnClassName(generationOptions.Class);

            string baseClass;

            if (generationOptions.BaseNamespace == nameof(Brudixy) && generationOptions.BaseClass == "DataTable")
            {
                baseClass = $"{nameof(Brudixy)}.DataColumn";
            }
            else
            {
                baseClass = $"{generationOptions.BaseNamespace}.{generationOptions.BaseClass}.{GetColumnClassName(generationOptions.BaseClass)}";
            }

            stringBuilder.AppendLine($"{Indend2}///<summary>Public typed '{XmlConvert.EncodeName(table.Table)}' data table column class.</summary>");
            stringBuilder.AppendLine($"{Indend2}public{sealedDef} class {colClass}: {baseClass}");

            stringBuilder.AppendLine($"{Indend2}{{");

            stringBuilder.AppendLine($"{Indend3}public {generationOptions.Class}Column(CoreDataTable dataTable, CoreDataColumnObj columnObj) : base(dataTable, columnObj) {{ }}");
            
            foreach (var property in table.PersistantColumnXProperties)
            {
                var typeString = GetGenTypeString(property.Value);

                stringBuilder.AppendLine($"{Indend3}///<summary>Gets or sets the '{property.Key}' column XProperty value.</summary>");
                
                var propertyName = property.Value.CodePropertyName ?? property.Key.Replace(" ", "_");
                
                stringBuilder.AppendLine($"{Indend3}public {typeString} @{propertyName} {{ get {{ return this.GetXProperty<{typeString}>(\"{property.Key}\"); }} set {{  this.SetXProperty(\"{property.Key}\", value); }} }}");
            }
            
            stringBuilder.AppendLine($"{Indend2}}}");
        }

        private static string GetColumnClassName(string @class)
        {
            return $"{@class}Column";
        }

        public static void GenerateColumnClassOverride(StringBuilder stringBuilder, DataTableObj table)
        {
            var colClass = GetColumnClassName(table.CodeGenerationOptions.Class);
            
            stringBuilder.AppendLine($"{Indend2}protected override CoreDataColumn CreateColumnInstance(CoreDataColumnObj columnObj) => new {colClass}(this, columnObj);");
        }

        private static void GenerateColumnFields(StringBuilder stringBuilder, DataTableObj table, HashSet<string> skipColumns)
        {
            foreach (var kv in table.ColumnObjects)
            {
                var column = kv.Key;

                if (skipColumns.Contains(column))
                {
                    continue;
                }

                stringBuilder.AppendLine($"{Indend2}///<summary>Gets access to '{XmlConvert.EncodeName(column)}' column.</summary>");
                stringBuilder.AppendLine($"{Indend2}public {table.CodeGenerationOptions.Class}Column @{column}Column {{ get; private set; }}");
            }
        }
        
        private static void GenerateXProperties(StringBuilder stringBuilder, Dictionary<string, XProperty> xProps, HashSet<string> skipXProps)
        {
            foreach (var property in xProps)
            {
                if (skipXProps?.Contains(property.Key) ?? false)
                {
                    continue;
                }
                
                var typeString = GetGenTypeString(property.Value);

                stringBuilder.AppendLine($"{Indend2}///<summary>Gets or sets the '{XmlConvert.EncodeName(property.Key)}' XProperty value.</summary>");
                
                var propertyName = property.Value.CodePropertyName ?? property.Key.Replace(" ", "_");
                
                stringBuilder.AppendLine($"{Indend2}public {typeString} @{propertyName} {{ get {{ return this.GetXProperty<{typeString}>(\"{property.Key}\"); }} set {{  this.SetXProperty(\"{property.Key}\", value); }} }}");
            }
        }

        private static void GenerateTableCtr(StringBuilder stringBuilder,
            DataTableObj table,
            string tableClassName,
            HashSet<string> skipColumns,
            HashSet<string> skipRelations,
            Dictionary<string, DataTableObj> tableOptions, 
            bool hasAnyColumnToGenerate)
        {
            stringBuilder.AppendLine();
            
            stringBuilder.AppendLine($"{Indend2}///<summary>Default '{XmlConvert.EncodeName(table.Table)}' class constructor.</summary>");
            stringBuilder.Append($"{Indend2}public {tableClassName}()").AppendLine();

            stringBuilder.AppendLine(@$"{Indend2}{{");

            GenerateDatasetCtrCore(table, stringBuilder, tableOptions);

            if (hasAnyColumnToGenerate)
            {
                GenerateColumnsInit(stringBuilder, table, skipColumns);

                GeneratePrimaryKeyInit(stringBuilder, table, skipColumns);
            
                GenerateIndexedInit(stringBuilder, table, skipColumns);
                
                GenerateRelationsInit(stringBuilder, table, skipRelations);
                
                GenerateColumnOverrides(stringBuilder, table, skipColumns);
            }

            GenerateXPropsDefaultValues(Indend3, stringBuilder, table.XProperties);

            stringBuilder.AppendLine(@$"{Indend2}}}");
        }

        private static void GenerateXPropsDefaultValues(string indent, StringBuilder stringBuilder, Dictionary<string, XProperty> xProperties)
        {
            var keyValuePairs = xProperties
                .Where(p => string.IsNullOrEmpty(p.Value.Value) == false);
            
            foreach (var xProp in keyValuePairs)
            {
                var typeString = GetGenTypeString(xProp.Value);

                stringBuilder.AppendLine(
                    $"{indent}this.SetXProperty(\"{xProp.Key}\", Tool.ConvertBoxed<{typeString}>(\"{xProp.Value.Value}\"));");
            }
        }
        
        private static void GenerateColumnOverrides(StringBuilder stringBuilder, DataTableObj table, HashSet<string> skipColumns)
        {
            int index = 0;
            foreach (var kv in table.ColumnOverrides)
            {
                var overCol = kv.Value;
                var columnName = kv.Key;

                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                var column = $"\"{columnName}\"";

                var colVar = $"col{columnName}{index}";

                stringBuilder.AppendLine($"{Indend3}var {colVar} = GetColumn({column});");
                
                if (overCol.Expression == "None")
                {
                    stringBuilder.AppendLine($"{Indend3}{colVar}.Expression = string.Empty; ");
                }
                
                if (overCol.Auto == "None")
                {
                    stringBuilder.AppendLine($"{Indend3}{colVar}.IsAutomaticValue = false; ");
                }
                
                if (overCol.DefaultValue == "None")
                {
                    stringBuilder.AppendLine($"{Indend3}{colVar}.DefaultValue = null; ");
                }
                
                if (overCol.DisplayName == "None")
                {
                    stringBuilder.AppendLine($"{Indend3}{colVar}.DisplayName = null; ");
                }
                
                if (overCol.IsReadOnly == "None" || string.IsNullOrEmpty(overCol.IsReadOnly) == false)
                {
                    stringBuilder.AppendLine($"{Indend3}{colVar}.IsReadOnly = {overCol.IsReadOnly}; ");
                }
                
                if (overCol.IsServiceColumn == "None" || string.IsNullOrEmpty(overCol.IsServiceColumn) == false)
                {
                    stringBuilder.AppendLine($"{Indend3}{colVar}.IsServiceColumn = {overCol.IsServiceColumn}; ");
                }
                
                if (overCol.MaxLength == "None" || string.IsNullOrEmpty(overCol.MaxLength) == false)
                {
                    stringBuilder.AppendLine($"{Indend3}{colVar}.MaxLength = {overCol.MaxLength}; ");
                }

                foreach (var xProp in overCol.XProperties)
                {
                    var typeString = GetGenTypeString(xProp.Value);
                    
                    stringBuilder.AppendLine(
                        $"{Indend4}{colVar}.SetXProperty(\"{xProp.Key}\", Tool.ConvertBoxed<{typeString}>(\"{xProp.Value.Value}\"));");
                }

                index++;
            }
        }

        private static void GenerateColumnsInitResetState(StringBuilder stringBuilder,
            DataTableObj table,
            HashSet<string> skipColumns)
        {
            stringBuilder.Append($"{Indend2}protected override void CloneDataColumnInfo(CoreDataColumnInfo dataColumnInfo, bool withData)").AppendLine();
            stringBuilder.AppendLine(@$"{Indend2}{{");
            stringBuilder.AppendLine(@$"{Indend3}base.CloneDataColumnInfo(dataColumnInfo, withData);");
            
            var colClass = GetColumnClassName(table.CodeGenerationOptions.Class);

            foreach (var kv in table.ColumnObjects)
            {
                var columnName = kv.Key;

                if (skipColumns.Contains(columnName))
                {
                    continue;
                }
                
                var column = $"\"{columnName}\"";
                
                var columnField = GetColumnFieldName(columnName);
                
                stringBuilder.AppendLine(
                    $"{Indend3}this.{columnField} = ({colClass})this.GetColumn({column});");

            }
            stringBuilder.AppendLine(@$"{Indend2}}}");
        }

        private static void GenerateColumnsInit(StringBuilder stringBuilder,
            DataTableObj table,
            HashSet<string> skipColumns)
        {
            int i = 0;
            
            var colClass = GetColumnClassName(table.CodeGenerationOptions.Class);
            
            foreach (var kv in table.ColumnObjects)
            {
                var tableColumn = kv.Value;
                
                var columnName =  kv.Key;

                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                var (column, columnType, columnTypeModifier, autoString, readOnlyString, expressionString, displayNameString,  uniqueString, defaultValueString, maxLenString, serviceColumn, allowNull, columnExtPropertiesOptional, columnField) = GetColumnGenerationParams(columnName, tableColumn);

                var extPropOptional = "";

                if (string.IsNullOrEmpty(columnExtPropertiesOptional) == false)
                {
                    extPropOptional = ", xProps: " + columnExtPropertiesOptional;
                }
                
                if(tableColumn.TypeModifier == "Complex" && tableColumn.DataType != null)
                {
                    stringBuilder.Append(
                        $"{Indend3}this.{columnField} = ({colClass})this.AddColumn<{tableColumn.DataType}>(columnName: {column}, displayName: {displayNameString}, readOnly: {readOnlyString}, defaultValue: ({tableColumn.DataType}){defaultValueString}, builtin:true, serviceColumn: {serviceColumn}{extPropOptional});");
                }
                else 
                {
                    var dataType = "null";
                    if (kv.Value.DataType != null)
                    {
                        dataType = $"typeof({kv.Value.DataType})";
                    }

                    stringBuilder.Append(
                        $"{Indend3}this.{columnField} = ({colClass})this.AddColumn(columnName: {column}, valueType: {columnType}, valueTypeModifier: {columnTypeModifier}, dataType: {dataType}, displayName: {displayNameString}, auto: {autoString}, readOnly: {readOnlyString}, unique: {uniqueString}, dataExpression: {expressionString}, columnMaxLength: {maxLenString}, defaultValue: {defaultValueString}, builtin:true, serviceColumn: {serviceColumn}, allowNull: {allowNull}{extPropOptional});");
                }
               
                stringBuilder.AppendLine();

                var indexed = tableColumn.HasIndex;

                if (indexed ?? false)
                {
                    stringBuilder.Append($"{Indend3}this.AddIndex({column});");

                    stringBuilder.AppendLine();
                }

                i++;
            }
        }

        private static (string column, string columnType, string columnTypeModifier, string autoString, string readOnlyString, string expressionString, string displayNameString, string uniqueString, string defaultValueString, string maxLenString, string serviceColumn, string allowNull, string columnExtPropertiesOptional, string columnField)
            GetColumnGenerationParams(string columnName, ColumnInfo tableColumn, bool dict = true)
        {
            var column = $"\"{columnName}\"";

            var tableStorageType = tableColumn.Type;
            var tableStorageTypeModifier = tableColumn.TypeModifier;
            
            var columnType = $"Brudixy.TableStorageType.{tableStorageType}";
            
            var columnTypeModifier = $"Brudixy.TableStorageTypeModifier.{tableStorageTypeModifier}";

            var auto = tableColumn.Auto;
            var autoString = "null";

            if (auto.HasValue)
            {
                autoString = auto.Value ? "true" : "false";
            }

            var readOnly = tableColumn.IsReadOnly;
            var readOnlyString = "null";

            if (readOnly ?? false)
            {
                readOnlyString = readOnly ?? false ? "true" : "false";
            }

            var expressionString = "null";
            var displayNameString = "null";
              
            var expression = tableColumn.Expression;

            if (expression != null)
            {
                expressionString = $"\"{expression}\"";
            }

            var displayName = tableColumn.DisplayName;

            if (string.IsNullOrEmpty(displayName) == false)
            {
                displayNameString = $"\"{displayName}\"";
            }

            var unique = tableColumn.IsUnique ?? false;

            var uniqueString = "null";

            if (unique && (tableColumn.HasIndex ?? false))
            {
                uniqueString = unique ? "true" : "false";
            }

            var defaultValue = tableColumn.DefaultValue;
            var defaultValueString = "null";
                
            if (defaultValue != null)
            {
                if (tableStorageType == "String")
                {
                    defaultValueString = $"\"{defaultValue}\"";
                }
                else if(tableStorageType == "Boolean")
                {
                    defaultValueString = defaultValue.ToString().ToLower(); 
                }
            }

            var maxLen = tableColumn.MaxLength;
            var maxLenString = "null";
            if (maxLen.HasValue)
            {
                maxLenString = maxLen.Value.ToString();
            }

            var serviceColumn = tableColumn.IsService ?? false ? "true" : "false";
                
            var allowNull = tableColumn.AllowNull ?? true ? "true" : "false";

            var columnExtPropertiesOptional = string.Empty;

            if (tableColumn.XProperties.Any())
            {
                var mapClass = dict ? "Dictionary" : "Map";
                
                columnExtPropertiesOptional = $"new {mapClass}<string, object>() {{" +
                                              string.Join(",", tableColumn.XProperties.Select(x =>
                                                  $" {{ \"{x.Key}\", Tool.ConvertBoxed<{x.Value.Type}>(\"{x.Value.Value}\")}} ")) + "}";

            }
                
            var columnField = GetColumnFieldName(columnName);
            return (column, columnType, columnTypeModifier, autoString, readOnlyString, expressionString, displayNameString, uniqueString, defaultValueString, maxLenString, serviceColumn, allowNull, columnExtPropertiesOptional, columnField);
        }

        private static string GetColumnFieldName(string columnName)
        {
            var columnField = $"@{columnName}Column";
            return columnField;
        }

        private static void GenerateContainerCreationChain(StringBuilder stringBuilder, string tableRowClass, bool anyColumns)
        {
            var thisCollection = anyColumns
                ? $"{GetContainerClassName(tableRowClass)}.{FieldContainerStorageName}.Columns.TryCombine(skipFields);"
                : "skipFields;";
            
            stringBuilder.Append($@"{Indend2}protected override CoreDataRowContainer CreateDataRowContainerCore(int rowHandle, IReadOnlyCollection<string> skipFields = null) {{ var collection = {thisCollection}; return base.CreateDataRowContainerCore(rowHandle, collection);  }}");
        }

        private static void GeneratePrimaryKeyInit(StringBuilder stringBuilder,
            DataTableObj table, HashSet<string> skipColumns)
        {
            if (table.PrimaryKey.Count == 0)
            {
                return;
            }
            
            if (table.PrimaryKey.Count == 1)
            {
                if(skipColumns.Contains(table.PrimaryKey[0]) == false)
                {
                    var pkName = GetColumnFieldName(table.PrimaryKey[0]);
                    
                    stringBuilder.AppendLine(
                    $"{Indend3}this.SetPrimaryKeyColumnCore(this.{pkName});");
                }
            }
            else
            {
                stringBuilder.AppendLine(
                    $"{Indend3}var _tmpPkCols = new CoreDataColumn[{table.PrimaryKey.Count}];");

                for (var index = 0; index < table.PrimaryKey.Count; index++)
                {
                    if (skipColumns.Contains(table.PrimaryKey[index]))
                    {
                        return;
                    }
                    
                    var pkName = GetColumnFieldName(table.PrimaryKey[index]);
                    
                    stringBuilder.AppendLine(
                        $"{Indend3}_tmpPkCols[{index}] = this.{pkName};");
                }

                stringBuilder.AppendLine(
                    $"{Indend3}this.SetPrimaryKeyColumnsCore(_tmpPkCols);");
            }
        }
        
        private static void GenerateIndexedInit(StringBuilder stringBuilder, 
            DataTableObj table,
            HashSet<string> skipColumns)
        {
            if (table.Indexes.Count == 0)
            {
                return;
            }

            foreach (var kv in table.Indexes)
            {
                var index = kv.Value;
                
                var unique = index.Unique ? "true" : "false";

                if (index.Columns.Count == 1)
                {
                    var indexColumn = index.Columns[0];

                    if (skipColumns.Contains(indexColumn))
                    {
                        return;
                    }
                    
                    stringBuilder.AppendLine(
                        $"{Indend3}this.AddIndex(\"{indexColumn}\", unique: {unique});");
                }
                else
                {
                    if (index.Columns.Any(c => skipColumns.Contains(c)))
                    {
                        return;
                    }
                    
                    var columns = $"new string[] {{ {string.Join(",", index.Columns.Select(c => $"\"{c}\""))}}}";
                    
                    stringBuilder.AppendLine(
                        $"{Indend3}this.AddMultiColumnIndex({columns}, unique: {unique});");
                }
            }
        }

        private static void GenerateRelationsInit(StringBuilder stringBuilder,
            DataTableObj table,
            HashSet<string> skipRelations)
        {
            int ri = 0;
            
            AddRelationInit(stringBuilder, table, ref ri, "AddParentRelation", skipRelations, table.Relations.Where(r => r.Value.ParentTable == table.Table).Union(table.ParentRelations));
            AddRelationInit(stringBuilder, table, ref ri, "AddChildRelation", skipRelations, table.Relations.Where(r => r.Value.ChildTable == table.Table).Union(table.ChildRelations));
        }

        private static int AddRelationInit(StringBuilder stringBuilder, 
            DataTableObj table,
            ref int ri,
            string addRelationMethodName,
            HashSet<string> skipRelations,
            IEnumerable<KeyValuePair<string, DataRelationObj>> relations)
        {
            foreach (var kv in relations.Where(rel =>
                         rel.Value.ChildTable == table.Table && rel.Value.ParentTable == table.Table))
            {
                var relation = kv.Value;
                var relationName = kv.Key;
                
                if (skipRelations.Contains(relationName))
                {
                    continue;
                }
                
                var relationNameString = $"\"{relationName}\"";

                if (relation.ParentKey.Length == 1)
                {
                    var pCol = GetColumnFieldName(relation.ParentKey[0]);
                    var cCol = GetColumnFieldName(relation.ChildKey[0]);
                    
                    stringBuilder.AppendLine(
                        $"{Indend3}this.{addRelationMethodName}(new Brudixy.DataRelation({relationNameString}, this.{pCol}, this.{cCol}));");
                }
                else
                {
                    stringBuilder.AppendLine(
                        $"{Indend3}var _tmpColsP{ri} = new CoreDataColumn[{relation.ParentKey.Length}];");
                    stringBuilder.AppendLine(
                        $"{Indend3}var _tmpColsC{ri} = new CoreDataColumn[{relation.ParentKey.Length}];");

                    for (int i = 0; i < relation.ParentKey.Length; i++)
                    {
                        var pCol = GetColumnFieldName(relation.ParentKey[i]);
                        var cCol = GetColumnFieldName(relation.ChildKey[i]);
                        
                        stringBuilder.AppendLine(
                            $"{Indend3}_tmpColsP{ri}[{i}] = this.{pCol};");
                        stringBuilder.AppendLine(
                            $"{Indend3}_tmpColsC{ri}[{i}] = this.{cCol};");
                    }

                    stringBuilder.AppendLine(
                        $"{Indend3}this.{addRelationMethodName}(new Brudixy.DataRelation({relationNameString}, _tmpColsP{ri}, _tmpColsC{ri}));");
                }

                ri++;
            }

            return ri;
        }

        private static string GetColumnVar(Dictionary<string, string> columnVarsMap, string column)
        {
            return columnVarsMap.GetOrDefault(column, GetColumnFieldName(column));
        }

        private const string FieldContainerStorageName = "FieldContainer";

        private static void GenerateRowFieldStorageClass(StringBuilder stringBuilder,
            DataTableObj table,
            string rowInterfaceName,
            HashSet<string> skipColumns)
        {

            stringBuilder.Append     ($"{Indend3}internal sealed class {FieldContainerStorageName}").AppendLine(); 
            stringBuilder.AppendLine(@$"{Indend3}{{");


            stringBuilder.AppendLine($"{Indend4}private static readonly Brudixy.Interfaces.Tools.StringValueStore<(Action<FieldContainer, object> set, Func<FieldContainer, object> get)> m_getSetAct;");
            stringBuilder.AppendLine($"{Indend4}public static IReadOnlyCollection<string> Columns => m_getSetAct; ");
           
            stringBuilder.AppendLine();

            var fieldsBuilder = new StringBuilder();

            stringBuilder.AppendLine($@"{Indend4}static  {FieldContainerStorageName}()");
            stringBuilder.AppendLine($@"{Indend4}{{");

            var getSetActions = new List<(string column, string getSet)>();

            foreach (var kv in table.ColumnObjects)
            {
                var column = kv.Value;
                var columnName = kv.Key;
                
                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                var typeString = GetGenTypeString(column);

                var tableNameStr = $"\"{table.Table}\"";
                var columnNameStr = $"\"{kv.Key}\"";
                var maxLenStr = column.MaxLength.HasValue ? column.MaxLength.ToString() : "null";

                var convertMethodName = GetConvertMethodName(column);
                
                getSetActions.Add((columnName, $"(f, val) => f.@{columnName} = CoreDataRowContainer.{convertMethodName}<{typeString}>({tableNameStr},{columnNameStr},val, {maxLenStr}), (f) => f.@{columnName})"));
                
                fieldsBuilder.AppendLine($"{Indend4}internal {typeString} @{columnName}; ");
            }

            stringBuilder.AppendLine($"{Indend5}var items = new (string column, (Action<FieldContainer, object> set, Func<FieldContainer, object> get))[]");
            stringBuilder.AppendLine($"{Indend5}{{");
            
            foreach (var setAction in getSetActions)
            {
                stringBuilder.AppendLine($"{Indend5}    (\"{setAction.column}\", ({setAction.getSet}),"); 
            }
            
            stringBuilder.AppendLine($"{Indend5}}};");

            stringBuilder.AppendLine($"{Indend5}m_getSetAct = new (items);");
            
            stringBuilder.AppendLine($@"{Indend4}}}");

            stringBuilder.AppendLine(fieldsBuilder.ToString());

            stringBuilder.AppendLine($@"{Indend4}public void Set(string col, object objValue)");

            stringBuilder.AppendLine($@"{Indend4}{{");
            
            stringBuilder.AppendLine($@"{Indend5}if (m_getSetAct.TryGetValue(col, out var getSetAct))");
            stringBuilder.AppendLine($@"{Indend5}{{");
            stringBuilder.AppendLine($@"{Indend5}   getSetAct.set(this, objValue);");
            stringBuilder.AppendLine($@"{Indend5}}}");

            stringBuilder.AppendLine($@"{Indend4}}}");
            
            stringBuilder.AppendLine($@"{Indend4}public object Get(string col)");
            stringBuilder.AppendLine($@"{Indend4}{{");
            stringBuilder.AppendLine($@"{Indend5}if (m_getSetAct.TryGetValue(col, out var getSetAct))");
            stringBuilder.AppendLine($@"{Indend5}{{");
            stringBuilder.AppendLine($@"{Indend5}   return getSetAct.get(this);");
            stringBuilder.AppendLine($@"{Indend5}}}");
            stringBuilder.AppendLine($@"{Indend5}return null; ");
            stringBuilder.AppendLine($@"{Indend4}}}");

            stringBuilder.AppendLine(@$"{Indend4}public {FieldContainerStorageName} Clone() {{  return ({FieldContainerStorageName})this.MemberwiseClone(); }}");

            stringBuilder.AppendLine();

            stringBuilder.AppendLine(@$"{Indend4}public void Init(ICoreDataRowReadOnlyAccessor row)");
            stringBuilder.AppendLine($@"{Indend4}{{");

            var typeValSafeSet = new StringBuilder();
            var regularRowSet = new StringBuilder();

            foreach (var kv in table.ColumnObjects)
            {
                var column = kv.Value;
                var columnName = kv.Key;
                
                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                if (column.IsReadOnly ?? false)
                {
                    continue;
                }

                var typeString = GetGenTypeString(column);
                
                var tableNameStr = $"\"{table.Table}\"";
                var columnNameStr = $"\"{kv.Key}\"";
                var maxLenStr = column.MaxLength.HasValue ? column.MaxLength.ToString() : "null";

                var convertMethodName = GetConvertMethodName(column);
               
                regularRowSet.AppendLine($"{Indend5}    @{columnName} = CoreDataRowContainer.{convertMethodName}<{typeString}>({tableNameStr},{columnNameStr}, row[\"{columnName}\"], {maxLenStr});");
                
                var propertyName = column.CodeProperty ?? columnName;
                
                typeValSafeSet.AppendLine($"{Indend5}   @{columnName} = rr.@{propertyName};");
            }

            stringBuilder.AppendLine($"{Indend5}if(row is {rowInterfaceName} rr)");
            stringBuilder.AppendLine($"{Indend5}{{");
            stringBuilder.AppendLine(typeValSafeSet.ToString());
            stringBuilder.AppendLine($"{Indend5}}} else {{ ");
            stringBuilder.AppendLine(regularRowSet.ToString());
            stringBuilder.AppendLine($"{Indend5}}}");

            stringBuilder.AppendLine($@"{Indend4}}}");

            stringBuilder.AppendLine(@$"{Indend4}public void Init({FieldContainerStorageName} c)");
            stringBuilder.AppendLine($@"{Indend4}{{");

            foreach (var kv in table.ColumnObjects)
            {
                var column = kv.Value;
                var columnName = kv.Key;
                
                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                if (column.IsReadOnly ?? false)
                {
                    continue;
                }

                stringBuilder.AppendLine($"{Indend5}this.@{columnName} = c.@{columnName};");
            }

            stringBuilder.AppendLine($@"{Indend4}}}");

          //f (table is DataTable)
          {
              stringBuilder.AppendLine(@$"{Indend4}public void Init(CoreContainerMetadataProps metaProps, ContainerDataProps containerProps)"); 
              stringBuilder.AppendLine($@"{Indend4}{{");

              stringBuilder.AppendLine($@"{Indend5}foreach (var kv in metaProps.ColumnMap)");
              stringBuilder.AppendLine($@"{Indend5}{{");
              stringBuilder.AppendLine($@"{Indend5}     var dc = (DataColumnContainer)kv.Value;");
              stringBuilder.AppendLine($@"{Indend5}     var val = containerProps.Data[dc.ColumnHandle];");
              stringBuilder.AppendLine($@"{Indend5}     if (val != null && dc.IsReadOnly == false) {{ Set(kv.Key, val); }}");
              stringBuilder.AppendLine($@"{Indend5}}}");                                  
              stringBuilder.AppendLine($@"{Indend4}}}");
            }
            /*else
            {
                stringBuilder.Append(@"
             public void Init(ContainerProps containerProps)
            {
                foreach (var kv in containerProps.ColumnMap)
                {
                    var val = containerProps.Data[kv.Value];

                    if (val != null))
                    {
                        Set(kv.Key, val);
                    }
                }
            }");
            }*/

            stringBuilder.AppendLine();

            stringBuilder.AppendLine(@$"{Indend3}}}");
        }
        
        private static void GenerateContainerColumns(StringBuilder stringBuilder,
            DataTableObj table,
            HashSet<string> skipColumns)
        {
            var columnInfos = new List<KeyValuePair<string, ColumnInfo>>();

            foreach (var kv in table.ColumnObjects)
            {
                var columnName = kv.Key;

                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                columnInfos.Add(kv);
            }

            if (columnInfos.Any() == false)
            {
                return;
            }
            
            stringBuilder.AppendLine();

            stringBuilder.AppendLine(
                @$"{Indend3}protected override IEnumerable<CoreDataColumnContainerBuilder> GetDataColumnContainers()");

            stringBuilder.AppendLine(
                @$"{Indend3}{{");
            
            stringBuilder.AppendLine();
            stringBuilder.AppendLine($"{Indend4}foreach(var dc in base.GetDataColumnContainers()) yield return dc;");

            int i = 0;
            foreach (var kv in columnInfos)
            {
                var (column, columnType, columnTypeModifier, autoString, readOnlyString, expressionString, displayNameString, uniqueString, defaultValueString, maxLenString, serviceColumn, allowNull, columnExtPropertiesOptional, columnField) = 
                    GetColumnGenerationParams(kv.Key, kv.Value, dict: false);

                string xPropInit = "null";

                if (string.IsNullOrEmpty(columnExtPropertiesOptional) == false)
                {
                    xPropInit = columnExtPropertiesOptional;
                }

                if (readOnlyString == "null")
                {
                    readOnlyString = "false";
                }
                
                if (uniqueString == "null")
                {
                    uniqueString = "false";
                }

                var dataType = "null";
                if (kv.Value.DataType != null)
                {
                    dataType = $"typeof({kv.Value.DataType})";
                }

                stringBuilder.AppendLine();
                stringBuilder.AppendLine($"{Indend4}var columnContainer{i} = this.CreateDataColumnContainerBuilder({column}, {columnType}, {columnTypeModifier}, {dataType}, {xPropInit});");

                if (expressionString != "null")
                {
                    stringBuilder.AppendLine($"{Indend4}columnContainer{i}.Expression = {expressionString};");
                }

                if (readOnlyString != "false")
                {
                    stringBuilder.AppendLine($"{Indend4}columnContainer{i}.IsReadOnly = {readOnlyString};");
                }

                if (uniqueString != "false")
                {
                    stringBuilder.AppendLine($"{Indend4}columnContainer{i}.IsUnique = {uniqueString};");
                }

                if (displayNameString != "null")
                {
                    stringBuilder.AppendLine($"{Indend4}columnContainer{i}.Caption = {displayNameString};");
                }

                if (allowNull != "true")
                {
                    stringBuilder.AppendLine($"{Indend4}columnContainer{i}.AllowNull = {allowNull};");
                }

                if (defaultValueString != "null")
                {
                    stringBuilder.AppendLine($"{Indend4}columnContainer{i}.DefaultValue = {defaultValueString};");
                }

                if (maxLenString != "null")
                {
                    stringBuilder.AppendLine($"{Indend4}columnContainer{i}.MaxLength = {maxLenString};");
                }

                if (autoString != "false")
                {
                    stringBuilder.AppendLine($"{Indend4}columnContainer{i}.IsAutomaticValue = {autoString};");
                }
                stringBuilder.AppendLine($"{Indend4}yield return columnContainer{i};");
                i++;
            }
            
            stringBuilder.AppendLine($@"{Indend3}}}");
        }

        private static void GeneratePkColumns(StringBuilder stringBuilder,
            DataTableObj table,
            HashSet<string> skipColumns)
        {
            var columnInfos = new List<string>();

            foreach (var kv in table.PrimaryKey)
            {
                var columnName = kv;

                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                columnInfos.Add(kv);
            }

            if (columnInfos.Any() == false)
            {
                return;
            }
            
            stringBuilder.AppendLine();
            
            stringBuilder.AppendLine(@$"{Indend3}protected override List<string> GetPrimaryKey()");
            stringBuilder.AppendLine(@$"{Indend3}{{");
            stringBuilder.AppendLine(@$"{Indend4}var list = base.GetPrimaryKey();");
            
            foreach (var columnInfo in columnInfos)
            {
                stringBuilder.AppendLine($@"{Indend4}list.Add(" + "\"" + columnInfo + "\");");
            }
            
            stringBuilder.AppendLine(@$"{Indend4}return list;");
            stringBuilder.AppendLine($@"{Indend3}}}");
        }

        private static string GetConvertMethodName(ColumnInfo column)
        {
            var convertMethodName = "ConvertBoxed";

            if (string.IsNullOrEmpty(column.EnumType) == false)
            {
                return convertMethodName;
            }

            if (column.Type == "String")
            {
                convertMethodName = "ConvertBoxedString";
            }
            else if (column.Type == "DateTime")
            {
                convertMethodName = "ConvertBoxedDateTime";
            }
            
            if (column.TypeModifier == "Array")
            {
                convertMethodName = "ConvertBoxedArray";
            }

            return convertMethodName;
        }

        private static void GenerateContainerField(StringBuilder stringBuilder, string containerClassName)
        {
            stringBuilder
                .Append($"{Indend3}private {FieldContainerStorageName} s => m_currentEditRow == null ? storage : (({containerClassName})m_currentEditRow).s;")
                .AppendLine();
            
            stringBuilder
                .Append($"{Indend3}private {FieldContainerStorageName} storage = new {FieldContainerStorageName}();")
                .AppendLine();

            stringBuilder
                .Append($"{Indend3}private {FieldContainerStorageName} o;")
                .AppendLine();

            stringBuilder
                .Append(@$"{Indend3}private {FieldContainerStorageName} GetOrig() {{   if (o == null) {{  return o = new {FieldContainerStorageName}(); }} return o; }}")
                .AppendLine();
        }

        private static void GenerateContainerGetData(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine(@$"{Indend3}protected override object GetData(CoreDataColumnContainer column)");
            stringBuilder.AppendLine(@$"{Indend3}{{");

            stringBuilder.AppendLine(@$"{Indend4}var dc = (DataColumnContainer)column;");
            stringBuilder.AppendLine(@$"{Indend4}if (FieldContainer.Columns.Contains(dc.ColumnName) && string.IsNullOrEmpty(dc.Expression)) {{ return s.Get(dc.ColumnName); }}");
            stringBuilder.AppendLine(@$"{Indend4}return base.GetData(dc);");
            
            stringBuilder.Append(@$"{Indend3}}}");
        }

        private static void GenerateContainerSetData(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(@$"{Indend3}protected override void SetData(CoreDataColumnContainer column, object objValue)");
            stringBuilder.AppendLine(@$"{Indend3}{{");
            stringBuilder.AppendLine(@$"{Indend4}var dc = (DataColumnContainer)column;");
            stringBuilder.AppendLine(@$"{Indend4}if (dc.IsReadOnly){{ return; }}");
            stringBuilder.AppendLine(@$"{Indend4}if (FieldContainer.Columns.Contains(dc.ColumnName)) {{ s.Set(dc.ColumnName, objValue); }}");
            stringBuilder.AppendLine(@$"{Indend4}else {{ base.SetData(dc, objValue); }}");
         
            stringBuilder.AppendLine(@$"{Indend3}}}");
        }

        private static void GenerateContainerRejectData(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine();
            
            stringBuilder.AppendLine(
                @$"{Indend3}protected override void RejectChangesCore() {{ base.RejectChangesCore(); if(o != null) {{ storage.Init(o); o = null; }} }}");
        }

        private static void GenerateContainerGetOriginalValue(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine(@$"{Indend3}protected override bool TryGetOriginalValue(CoreDataColumnContainer column, out object value)");

            stringBuilder.AppendLine(@$"{Indend3}{{");
            
            stringBuilder.AppendLine(@$"{Indend4}if (o != null)");
            stringBuilder.AppendLine(@$"{Indend4}{{");
            stringBuilder.AppendLine(@$"{Indend5}var dc = (DataColumnContainer)column;");
            stringBuilder.AppendLine(@$"{Indend5}if (FieldContainer.Columns.Contains(dc.ColumnName))");
            stringBuilder.AppendLine(@$"{Indend5}{{  value = o.Get(dc.ColumnName); return true; }}");
            stringBuilder.AppendLine(@$"{Indend4}}}");
            stringBuilder.AppendLine(@$"{Indend4}return base.TryGetOriginalValue(column, out value);");
            
            stringBuilder.Append(@$"{Indend3}}}");
        }

        private static void GenerateRowContainerClass(StringBuilder stringBuilder,
            DataTableObj table,
            string tableRowClassName,
            string tableClassName,
            string baseDataRowClassFull,
            string tableClass,
            HashSet<string> skipColumns)
        {
            var baseContainerDefinition = string.Empty;
            var baseContainerInterfaceDefinition = string.Empty;

            if (!string.IsNullOrEmpty(baseDataRowClassFull))
            {
                baseContainerDefinition = $": global::{baseDataRowClassFull}Container";
            }
            else
            {
                baseContainerDefinition = ": DataRowContainer";
            }

            var rowInterfaceName = $"I{tableRowClassName}Accessor";
            
            baseContainerInterfaceDefinition = $", {rowInterfaceName}";

            stringBuilder.AppendLine();
           
            stringBuilder.AppendLine($"{Indend2}///<summary>Public typed '{XmlConvert.EncodeName(table.Table)}' table row data container.</summary>");
            stringBuilder.AppendLine($"{Indend2}[System.Diagnostics.DebuggerTypeProxy(typeof(DataRowDebugView))]");
            stringBuilder.AppendLine($"{Indend2}[System.Diagnostics.DebuggerDisplay(\"Row container of {{TableName}}, State {{RowRecordState}}, Age {{GetRowAge()}}, # {{DebugKeyValue}} \")]");

            var containerClassName = GetContainerClassName(tableRowClassName);
            
            stringBuilder.Append($"{Indend2}public partial class {containerClassName}{baseContainerDefinition}{baseContainerInterfaceDefinition}").AppendLine();
            stringBuilder.Append(@$"{Indend2}{{");
            stringBuilder.AppendLine();
            
            if (HasAnyColumnToGenerate(table, skipColumns))
            {
                GenerateRowFieldStorageClass(stringBuilder, table, rowInterfaceName, skipColumns);
                
                GenerateContainerField(stringBuilder, containerClassName);

                GenerateContainerInit(stringBuilder);

                GenerateContainerProperties(stringBuilder, table, skipColumns);

                GenerateContainerGetData(stringBuilder);

                GenerateContainerSetData(stringBuilder);

                GenerateContainerRejectData(stringBuilder);

                GenerateContainerGetOriginalValue(stringBuilder);

                GenerateCopyFrom(stringBuilder, tableClassName, tableRowClassName);
                
                GenerateCopyAll(stringBuilder, tableClassName, tableRowClassName);

                GenerateContainerClone(stringBuilder, tableRowClassName, containerClassName);

                GenerateContainerDefaultTableName(stringBuilder, table.Table);
                
                GenerateContainerColumns(stringBuilder, table, skipColumns);
                
                GeneratePkColumns(stringBuilder, table, skipColumns);
            }
            
            if (table.RowSubTypes.Any())
            {
                foreach (var rowSubType in table.RowSubTypesObjects)
                {
                    GenerateRowContainerSubClass(stringBuilder, table.Table, containerClassName, rowSubType.Value);
                }
            }

            stringBuilder.AppendLine();

            stringBuilder.Append(@$"{Indend2}}}");
            stringBuilder.AppendLine();
        }

        private static string GetContainerClassName(string tableRowClassName)
        {
            return $"{tableRowClassName}Container";
        }

        private static void GenerateContainerClone(StringBuilder stringBuilder, string tableRowClassName, string containerClassName)
        {
            var containerCloneBaseClass =  "CoreDataRowContainer";
            var containerClass = tableRowClassName + "Container";

            stringBuilder.AppendLine();

            stringBuilder.Append(
                @$"{Indend3}protected override {containerCloneBaseClass} CloneCore() {{  var clone = ({containerClass})base.CloneCore();  clone.storage = s.Clone(); clone.o = o?.Clone();  return clone; }}");
            
            stringBuilder.AppendLine();
            
            stringBuilder.Append(
                @$"{Indend3}public new {containerClassName} Clone() {{ return ({containerClassName})base.Clone(); }}");
        }
        
        private static void GenerateContainerDefaultTableName(StringBuilder stringBuilder, string tableName)
        {
            stringBuilder.AppendLine();

            stringBuilder.Append(
                @$"{Indend3}protected override string GetDefaultTableName() {{  return ""{tableName}""; }}");
        }

        private static void GenerateCopyFrom(StringBuilder stringBuilder, string tableClassName, string tableRowClassName)
        {
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"{Indend3}/// <inheritdoc />");
            stringBuilder.Append(
                $@"{Indend3}public override void CopyFrom(ICoreDataRowReadOnlyAccessor rowAccessor, IReadOnlyCollection<string> skipFields = null) {{ var copied = {tableClassName}.CopyData(this, rowAccessor, skipFields); base.CopyFrom(rowAccessor, copied ? {GetContainerClassName(tableRowClassName)}.{FieldContainerStorageName}.Columns.TryCombine(skipFields) : skipFields);  }}");
        }
        
        private static void GenerateCopyAll(StringBuilder stringBuilder, string tableClassName, string tableRowClassName)
        {
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"{Indend3}/// <inheritdoc />");
            stringBuilder.Append(
                $@"{Indend3}public override void CopyAll(ICoreDataRowReadOnlyAccessor rowAccessor) {{ base.CopyAll(rowAccessor); {tableClassName}.CopyAll(this, rowAccessor); }}");
        }

        private static void GenerateContainerInit(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"{Indend3}/// <inheritdoc />");
            stringBuilder.AppendLine(
                @$"{Indend3}public override void Init(ICoreDataRowReadOnlyAccessor row, IReadOnlyCollection<string> skipFields = null) {{ storage = new {FieldContainerStorageName}(); storage.Init(row);  base.Init(row, FieldContainer.Columns.TryCombine(skipFields));  }}");

            stringBuilder.AppendLine();
            
            stringBuilder.AppendLine($"{Indend3}/// <inheritdoc />");
            stringBuilder.Append(
                @$"{Indend3}public override void Init(CoreContainerMetadataProps metadataProps, CoreContainerDataProps containerProps, ICoreDataRowReadOnlyAccessor row = null)");

            stringBuilder.Append($"{Indend3}{{");

            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"{Indend4}base.Init(metadataProps, containerProps, row);");
            stringBuilder.AppendLine($"{Indend4}if (row != null) {{ storage = new {FieldContainerStorageName}(); storage.Init(row); }}");
            stringBuilder.AppendLine($"{Indend4}else");
            stringBuilder.AppendLine($"{Indend4}{{");
            stringBuilder.AppendLine($"{Indend4}    var cp = (ContainerDataProps)containerProps;");
            stringBuilder.AppendLine($"{Indend4}    storage = new {FieldContainerStorageName}();");
            stringBuilder.AppendLine($"{Indend4}    storage.Init(metadataProps, cp);");
            stringBuilder.AppendLine($"{Indend4}}}");
            stringBuilder.AppendLine();
            stringBuilder.Append($"{Indend3}}}");

            stringBuilder.AppendLine();
        }

        private static void GenerateContainerProperties(StringBuilder stringBuilder, DataTableObj table,
            HashSet<string> skipColumns)
        {
            stringBuilder.AppendLine();

            foreach (var kv in table.ColumnObjects)
            {
                var column = kv.Value;
                var columnName = kv.Key;
                
                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                WriteFieldPropertyComment(stringBuilder, column, columnName, Indend3);

                var typeString = GetGenTypeString(column);

                var getter = $"get => s.@{columnName};";

                if (column.TypeModifier == "Array")
                {
                    getter =  $"get => ({typeString})s.@{columnName}?.Clone();";
                }

                var propertyName = column.CodeProperty ?? columnName;

                if (string.IsNullOrEmpty(column.Expression))
                {
                    stringBuilder
                        .Append($"{Indend3}public {typeString} @{propertyName} ")
                        .Append($"{{")
                        .Append(getter);

                    if (column.IsReadOnly == false)
                    {
                        stringBuilder
                            .Append(
                                $" set => SetValue(ref value, ref s.@{columnName}, ref GetOrig().@{columnName}, \"{columnName}\");");
                    }

                    stringBuilder.Append($"}}");

                    if (column.TypeModifier == "Array")
                    {
                        stringBuilder.AppendLine();
                        
                        var genericTypeString = GetGenericArrayType(typeString);
                        
                        stringBuilder
                            .Append($"{Indend3}public IReadOnlyList<{genericTypeString}> @{propertyName}Link ")
                            .Append($"{{")
                            .Append($"  get => s.@{columnName}; ")
                            .Append($"}}");
                    }
                }
                else
                {
                    stringBuilder
                        .Append($"{Indend3}public {typeString} @{propertyName} ")
                        .Append($"{{")
                        .Append($"get => ({typeString})GetExpressionValue(\"{columnName}\");")
                        .Append($"}}");
                    
                    if (column.TypeModifier == "Array")
                    {
                        var genericTypeString = GetGenericArrayType(typeString);
                        
                        stringBuilder
                            .Append($"{Indend3}public IReadOnlyList<{genericTypeString}> @{propertyName}Link ")
                            .Append($"{{")
                            .Append($"get => (IReadOnlyList<{genericTypeString}>)GetExpressionValue(\"{columnName}\");")
                            .Append($"}}");
                    }
                }

                stringBuilder.AppendLine();
            }
            
            foreach (var kv in table.GroupColumnObjects)
            {
                var groupOptions = kv.Value;
                var groupName = kv.Key;

                if (skipColumns.Contains(groupName) || groupOptions.Columns.Count <= 1)
                {
                    continue;
                }

                var (tupleDef, tupleCtr, tupleSet) = GetGroupColumnSetup(table, groupOptions);

                WriteGroupFieldPropertyComment(stringBuilder, groupOptions, groupOptions.Name, Indend3);

                var groupPropertyName = groupOptions.Name;

                stringBuilder.Append($"{Indend3}public {tupleDef} @{groupPropertyName} ");
                stringBuilder.Append($"{{");

                stringBuilder.Append($"get {{ return {tupleCtr}; }}");

                if (groupOptions.IsReadOnly == false)
                {
                    stringBuilder.Append($" set {{ this.BeginEdit(); try {{ {tupleSet} this.EndEdit(); }} catch {{ this.CancelEdit(); throw; }} }} ");
                }

                stringBuilder.Append($"}}");
                stringBuilder.AppendLine();
            }
        }

        private static (string tupleDef, string tupleCtr, string tupleSet) GetGroupColumnSetup(DataTableObj table,
            GroupColumInfo groupOptions)
        {
            string tupleDef = string.Empty;
            string tupleCtr = string.Empty;
            string tupleSet = string.Empty;

            if (groupOptions.Type == GroupType.Tuple)
            {
                var colTypes = new List<string>();
                var colCtr = new List<string>();
                var colSet = new List<string>();

                foreach (var c in groupOptions.Columns)
                {
                    var tableColumnObject = table.ColumnObjects[c];
                    
                    var propertyName = tableColumnObject.CodeProperty ?? c;

                    var genTypeString = GetGenTypeString(tableColumnObject);

                    if (tableColumnObject.TypeModifier == "Array")
                    {
                        genTypeString = $"IReadOnlyList<{GetGenericArrayType(genTypeString)}>";
                    }
                    
                    colTypes.Add(genTypeString + " " + propertyName);

                    colCtr.Add($"this.{propertyName}");

                    if (tableColumnObject.IsReadOnly == false)
                    {
                        colSet.Add($"this.{propertyName} = value.{propertyName};");
                    }
                }

                tupleDef = "(" + string.Join(",", colTypes) + ")";
                tupleCtr = "(" + string.Join(",", colCtr) + ")";
                tupleSet = string.Join(" ", colSet);
            }

            return (tupleDef, tupleCtr, tupleSet);
        }

        private static void GenerateRowClass(StringBuilder stringBuilder,
            DataTableObj table,
            string tableRowClassName,
            string baseDataRowClassFull,
            string tableClassName,
            Dictionary<string, DataTableObj> tableOptions,
            HashSet<string> skipColumns)
        {
            var baseInterfaceDefinition = $", I{tableRowClassName}Accessor";

            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"{Indend2}///<summary>Public typed '{XmlConvert.EncodeName(table.Table)}' table row class.</summary>");
            
            AddCodeGenAttr(stringBuilder, Indend2);
            
            stringBuilder.AppendLine($"{Indend2}[System.Diagnostics.DebuggerTypeProxy(typeof(DataRowDebugView))]");
            stringBuilder.AppendLine($"{Indend2}[System.Diagnostics.DebuggerDisplay(\"Row of {{GetTableName()}}, State {{RowRecordState}}, Age {{GetRowAge()}}, # {{DebugKeyValue}} \")]");
            stringBuilder.Append($"{Indend2}public partial class {tableRowClassName} : global::{baseDataRowClassFull}{baseInterfaceDefinition}").AppendLine();

            stringBuilder.AppendLine(@$"{Indend2}{{");

            stringBuilder.Append($"{Indend3}public {tableRowClassName}()")
                .Append($"{Indend3}{{ m_tableName = \"{table.Table}\"; }}")
                .AppendLine();

            stringBuilder.Append($"{Indend3}public {tableRowClassName}(int rowHandle, Brudixy.DataTable dataTable) : base(rowHandle, dataTable) ")
                .Append($"{Indend3}{{ m_tableName = \"{table.Table}\"; }}")
                .AppendLine();

            GenerateRowProperties(stringBuilder, table, tableClassName, skipColumns);

            GenerateRowRelations(stringBuilder, table, tableRowClassName, tableOptions);

            GenerateContainerGetters(stringBuilder, tableRowClassName);

            if (HasAnyColumnToGenerate(table, skipColumns))
            {
                GenerateCopyFrom(stringBuilder, tableClassName, tableRowClassName);
                GenerateCopyAll(stringBuilder, tableClassName, tableRowClassName);
            }

            if (table.RowSubTypes.Any())
            {
                var containerClassName = GetContainerClassName(tableRowClassName);
                
                foreach (var rowSubType in table.RowSubTypesObjects)
                {
                    GenerateRowSubClass(stringBuilder, table.Table, table.RowClass, rowSubType.Value, containerClassName);
                }
            }
            
            stringBuilder.AppendLine();

            stringBuilder.AppendLine(@$"{Indend2}}}");
        }
        
         private static void GenerateRowSubClass(StringBuilder stringBuilder,
             string tableName,
             string tableRowClassName,
             [NotNull] RowSubTypeInfo rowSubClassInfo, 
             string containerClassName)
        {
            stringBuilder.AppendLine();
            
            stringBuilder.AppendLine($"{Indend3}public {rowSubClassInfo.Name} As{rowSubClassInfo.Name}()");
            stringBuilder.AppendLine(@$"{Indend3}{{");
            if (string.IsNullOrEmpty(rowSubClassInfo.Expression))
            {
                stringBuilder.AppendLine($"{Indend4}return new {rowSubClassInfo.Name}(this.RowHandleCore, this.table);");
            }
            else
            {
                stringBuilder.AppendLine($"{Indend4}return this.CheckFilter(\"{rowSubClassInfo.Expression}\") ? new {rowSubClassInfo.Name}(this.RowHandleCore, this.table) : null;");
            }
            stringBuilder.AppendLine(@$"{Indend3}}}");
            stringBuilder.AppendLine();

            stringBuilder.AppendLine($"{Indend3}///<summary>Public typed '{XmlConvert.EncodeName(tableName)}' table row's '{rowSubClassInfo.Name}' sub class.</summary>");
            
            AddCodeGenAttr(stringBuilder, Indend3);
            stringBuilder.AppendLine($"{Indend3}[System.Diagnostics.DebuggerTypeProxy(typeof(DataRowDebugView))]");
            stringBuilder.Append($"{Indend3}public sealed partial class {rowSubClassInfo.Name} : {tableRowClassName}").AppendLine();
            stringBuilder.AppendLine(@$"{Indend3}{{");

            stringBuilder.AppendLine($"{Indend4}public {rowSubClassInfo.Name}() : base() {{}}");
            stringBuilder.AppendLine($"{Indend4}public {rowSubClassInfo.Name}(int rowHandle, Brudixy.DataTable dataTable) : base(rowHandle, dataTable) {{}}");
          
            GenerateRowSubTypeXProperties(stringBuilder, rowSubClassInfo, Indend4);
            
            stringBuilder.AppendLine($"{Indend4}/// <inheritdoc />");
            stringBuilder.AppendLine($"{Indend4}public new {containerClassName}.{rowSubClassInfo.Name}Container ToContainer()");
            stringBuilder.AppendLine($"{Indend4}{{");
            stringBuilder.AppendLine($"{Indend5}var cnt = new {containerClassName}.{rowSubClassInfo.Name}Container();");
            stringBuilder.AppendLine($"{Indend5}cnt.Init(this);");
            stringBuilder.AppendLine($"{Indend5}cnt.CopyFrom(this);");
            stringBuilder.AppendLine($"{Indend5}return cnt;");
            stringBuilder.AppendLine($"{Indend4}}}");
            
            stringBuilder.AppendLine();
            stringBuilder.AppendLine(@$"{Indend3}}}");
        }
         
         private static void GenerateRowContainerSubClass(StringBuilder stringBuilder,
             string tableName,
             string containerRowClassName,
             [NotNull] RowSubTypeInfo rowSubClassInfo)
         {
             var subContainerClassName = rowSubClassInfo.Name + "Container";
             
             stringBuilder.AppendLine();
            
             stringBuilder.AppendLine($"{Indend3}public {subContainerClassName} As{rowSubClassInfo.Name}()");
             stringBuilder.AppendLine(@$"{Indend3}{{");
             if (string.IsNullOrEmpty(rowSubClassInfo.Expression))
             {
                 stringBuilder.AppendLine($"{Indend4}var cnt = new {subContainerClassName}();");
                 stringBuilder.AppendLine($"{Indend4}cnt.Init(this);");
                 stringBuilder.AppendLine($"{Indend4}cnt.CopyFrom(this);");
                 stringBuilder.AppendLine($"{Indend4}return cnt;");
             }
             else
             {
                 stringBuilder.AppendLine($"{Indend4}if(this.CheckFilter(\"{rowSubClassInfo.Expression}\") == false) return null;");
                 stringBuilder.AppendLine($"{Indend4}var cnt = new {subContainerClassName}();");
                 stringBuilder.AppendLine($"{Indend4}cnt.Init(this);");
                 stringBuilder.AppendLine($"{Indend4}cnt.CopyFrom(this);");
                 stringBuilder.AppendLine($"{Indend4}return cnt;");
             }
             stringBuilder.AppendLine(@$"{Indend3}}}");
             stringBuilder.AppendLine();

             stringBuilder.AppendLine($"{Indend3}///<summary>Public typed '{XmlConvert.EncodeName(tableName)}' table row container's '{rowSubClassInfo.Name}' sub class.</summary>");
            
             AddCodeGenAttr(stringBuilder, Indend3);
             stringBuilder.AppendLine($"{Indend3}[System.Diagnostics.DebuggerTypeProxy(typeof(DataRowDebugView))]");
             stringBuilder.Append($"{Indend3}public sealed partial class {subContainerClassName} : {containerRowClassName}").AppendLine();
             stringBuilder.AppendLine(@$"{Indend3}{{");

             GenerateRowSubTypeXProperties(stringBuilder, rowSubClassInfo, Indend4);
             stringBuilder.AppendLine();
             stringBuilder.AppendLine(@$"{Indend3}}}");
         }

         private static void GenerateRowSubTypeXProperties(StringBuilder stringBuilder, RowSubTypeInfo rowSubClassInfo, string indent)
         {
             stringBuilder.AppendLine();

             foreach (var property in rowSubClassInfo.XProperties)
             {
                 var typeString = GetGenTypeString(property.Value);

                 stringBuilder.AppendLine($"{indent}///<summary>Gets or sets the '{XmlConvert.EncodeName(property.Key)}' XProperty value.</summary>");
                
                 var propertyName = property.Value.CodePropertyName ?? property.Key.Replace(" ", "_");
                
                 stringBuilder.AppendLine($"{indent}public {typeString} @{propertyName} {{ get {{ return this.GetXProperty<{typeString}>(\"{property.Key}\"); }} set {{  this.SetXProperty(\"{property.Key}\", value); }} }}");
             }
         }

        private static void GenerateContainerGetters(StringBuilder stringBuilder, string tableRowClassName)
        {
            stringBuilder.AppendLine($"{Indend3}/// <inheritdoc />");
            stringBuilder.AppendLine($"{Indend3}public new {tableRowClassName}Container ToContainer() {{ return ({tableRowClassName}Container) base.ToContainer(); }}");
        }

        private static void GenerateOverloads(StringBuilder stringBuilder, string tableRowClassName)
        {
            stringBuilder.AppendLine($"{Indend2}/// <inheritdoc />");
            stringBuilder.AppendLine(
                @$"{Indend2}public new IDataTableRowEnumerable<{tableRowClassName}> Rows {{ get {{ return RowExtensions.RowsOfType<{tableRowClassName}>(this); }} }}");

            stringBuilder.AppendLine($"{Indend2}/// <inheritdoc />");
            stringBuilder.AppendLine(
                @$"{Indend2}public new {tableRowClassName} AddRow({tableRowClassName}Container container) {{ return ({tableRowClassName}) base.AddRow(container);}}");
            
            stringBuilder.AppendLine($"{Indend2}/// <inheritdoc />");
            stringBuilder.AppendLine(
                @$"{Indend2}public new IEnumerable<{tableRowClassName}> GetRows<T>(string column, T value) where T : IComparable {{ return base.GetRows(column, value).OfType<{tableRowClassName}>();  }}");
            
            stringBuilder.AppendLine($"{Indend2}/// <inheritdoc />");
            stringBuilder.AppendLine(
                @$"{Indend2}public new {tableRowClassName} GetRow<T>(string column, T value) where T : IComparable {{ return  ({tableRowClassName}) base.GetRow(column, value);  }}");

            stringBuilder.AppendLine($"{Indend2}/// <inheritdoc />");
            stringBuilder.AppendLine(
                @$"{Indend2}public new {tableRowClassName}Container NewRow(IReadOnlyDictionary<string, object> values = null) {{ return ({tableRowClassName}Container)base.NewRow(values); }}");
        }

        private static void GenerateCreateInstanceOverloads(StringBuilder stringBuilder, string tableRowClassName)
        {
            stringBuilder.AppendLine(
                @$"{Indend2}protected override CoreDataRow CreateRowInstance() {{ return new {tableRowClassName}();  }}");

            stringBuilder.AppendLine(
                $"{Indend2}protected override CoreDataRowContainer CreateContainerInstance() {{ return new {tableRowClassName}Container(); }}");       
        }

        private static void GenerateRowProperties(StringBuilder stringBuilder, DataTableObj table, string tableClassName, HashSet<string> skipColumns)
        {
            stringBuilder.AppendLine();

            foreach (var kv in table.ColumnObjects)
            {
                var columnName = kv.Key;
                
                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                stringBuilder.AppendLine($"{Indend3}protected Brudixy.DataColumn @{columnName}Column => (({tableClassName})this.table).@{columnName}Column;");
            }

            stringBuilder.AppendLine();

            foreach (var kv in table.ColumnObjects)
            {
                var column = kv.Value;
                var columnName = kv.Key;
                
                if (skipColumns.Contains(columnName))
                {
                    continue;
                }

                var typeString = GetGenTypeString(column);

                var colName = $"@{columnName}Column";

                var fieldAccessor = "Field";

                if ((column.IsUnique ?? false || column.AllowNull == false) && column.Type != "String" && column.TypeModifier == "Simple" && column.DataTypeIsStruct)
                {
                    fieldAccessor = "FieldNotNull";
                }
                
                var propertyName = column.CodeProperty ?? columnName;

                GenerateRowFieldAccessor(stringBuilder, column, columnName, typeString, fieldAccessor, typeString, colName, propertyName);
                
                if (column.TypeModifier == "Array")
                {
                    var genericParamTypeString = GetGenericArrayType(typeString);
                    
                    var typeLinkString = $"IReadOnlyList<{genericParamTypeString}>";
                    
                    GenerateRowFieldAccessor(stringBuilder, 
                        column,
                        columnName, 
                        typeLinkString, 
                        "FieldArray",
                        genericParamTypeString,
                        colName, 
                        propertyName + "Link",
                        disableSetter: true);
                }
            }

            foreach (var kv in table.GroupColumnObjects)
            {
                var groupOptions = kv.Value;
                var groupName = kv.Key;

                if (skipColumns.Contains(groupName) || groupOptions.Columns.Count <= 1)
                {
                    continue;
                }

                var (tupleDef, tupleCtr, tupleSet) = GetGroupColumnSetup(table, groupOptions);
                
                WriteGroupFieldPropertyComment(stringBuilder, groupOptions, groupOptions.Name, Indend3);

                var groupPropertyName = groupOptions.Name;

                stringBuilder.Append($"{Indend3}public {tupleDef} @{groupPropertyName} ");
                stringBuilder.Append($"{{");

                stringBuilder.Append($"get {{ return {tupleCtr}; }}");

                if (groupOptions.IsReadOnly == false)
                {
                    stringBuilder.Append($" set {{ var tran = this.StartTransaction(); try {{ {tupleSet} tran.Commit(); }} catch {{ tran.Rollback(); throw; }} }} ");
                }

                stringBuilder.Append($"}}");
                stringBuilder.AppendLine();
            }
        }

        private static string GetGenericArrayType(string typeString)
        {
            return typeString.TrimEnd(']').TrimEnd('[');
        }

        private static void GenerateRowFieldAccessor(StringBuilder stringBuilder, 
            ColumnInfo column, 
            string columnName,
            string typeString,
            string fieldAccessor, 
            string genericParamTypeString,
            string colName, 
            string propertyName,
            bool disableSetter = false)
        {
            WriteFieldPropertyComment(stringBuilder, column, columnName, Indend3);

            stringBuilder.Append($"{Indend3}public {typeString} @{propertyName} ");
            stringBuilder.Append($"{{");

            stringBuilder.Append($"get {{ return this.{fieldAccessor}<{genericParamTypeString}>({colName}); }}");

            if (column.IsReadOnly == false)
            {
                if (disableSetter == false)
                {
                    stringBuilder.Append($" set {{ this.Set({colName}, value); }} ");
                }
            }

            stringBuilder.Append($"}}");
            stringBuilder.AppendLine();
        }

        private static void WriteFieldPropertyComment(StringBuilder stringBuilder, ColumnInfo column, string columnName, string indent, bool forceReadonly = false)
        {
            var encodeName = XmlConvert.EncodeName(column.DisplayName ?? columnName);

            if (forceReadonly || (column.IsReadOnly ?? false))
            {
                stringBuilder.AppendLine($"{indent}///<summary>Gets '{encodeName}' field value.</summary>");
            }
            else
            {
                string extraComment = string.Empty;

                if (column.MaxLength.HasValue)
                {
                    extraComment += $"Field max length is limited to {column.MaxLength}.";
                }

                stringBuilder.AppendLine($"{indent}///<summary>Gets or sets '{encodeName}' field value. {extraComment}</summary>");
            }
        }
        
        private static void WriteGroupFieldPropertyComment(StringBuilder stringBuilder, GroupColumInfo column, string columnName, string indent, bool forceReadonly = false)
        {
            var encodeName = XmlConvert.EncodeName(column.Name ?? columnName);

            if (forceReadonly || column.IsReadOnly)
            {
                stringBuilder.AppendLine($"{indent}///<summary>Gets '{encodeName}' property value.</summary>");
            }
            else
            {
                string extraComment = string.Empty;

                stringBuilder.AppendLine($"{indent}///<summary>Gets or sets '{encodeName}' property value. {extraComment}</summary>");
            }
        }

        private static void GenerateRowRelations(StringBuilder stringBuilder, 
            DataTableObj table,
            string rowClassName,
            Dictionary<string, DataTableObj> tableOptions)
        {
            stringBuilder.AppendLine();

            foreach (var relationGroup in (table.Relations.Union(table.ChildRelations)).GroupBy(rel => rel.Value.ParentTable))
            {
                var dataRelations = relationGroup.OrderBy(rel => rel.Key).ToArray();

                var counterSuffix = dataRelations.Length > 1 ? "1" : string.Empty;

                int counter = 1;

                foreach (var kv in dataRelations)
                {
                    var relation = kv.Value;
                    var relationName = kv.Key;
                    
                    var relationNameString = $"\"{relationName}\"";

                    var (parentTableType,  parentRowType) = GetRelationTableClassNames(table, rowClassName, tableOptions, relationName, relation,  true);

                    stringBuilder.AppendLine($"{Indend3}///<summary> Gets or sets parent relation rows of '{XmlConvert.EncodeName(relation.ParentTable)}' table using '{XmlConvert.EncodeName(relationName)}' relation.</summary>");

                    var relationPropertyName = relation.ParentProperty ?? $"{parentTableType}Parent{counterSuffix}";
                    
                    stringBuilder.Append($"{Indend3}public {parentRowType} {relationPropertyName}");
                    stringBuilder.Append($"{{");

                    stringBuilder.Append($"get {{ return ({parentRowType})this.GetParentRow({relationNameString}); }} ");
                    stringBuilder.Append($"set {{ this.SetParentRow({relationNameString}, value); }}   ");

                    stringBuilder.Append($"}}");

                    stringBuilder.AppendLine();

                    counter++;

                    counterSuffix = counter.ToString();
                }
            }

            foreach (var relationGroup in (table.Relations.Union(table.ParentRelations)).GroupBy(rel => rel.Value.ChildTable))
            {
                var dataRelations = relationGroup.OrderBy(rel => rel.Key).ToArray();

                var counterSuffix = dataRelations.Length > 1 ? "1" : string.Empty;

                int counter = 1;

                foreach (var kv in dataRelations)
                {
                    var relation = kv.Value;
                    var relationName = kv.Key;
                    
                    var relationNameString = $"\"{relationName}\"";

                    var (childTableType, childRowType) = GetRelationTableClassNames(table, rowClassName, tableOptions, relationName, relation, false);

                    stringBuilder.AppendLine($"{Indend3}///<summary>Gets child relation row collection of '{XmlConvert.EncodeName(relation.ChildTable)}' table using '{XmlConvert.EncodeName(relationName)}' relation.</summary>");

                    var relationPropertyName = relation.ChildProperty ?? $"{childTableType}Rows{counterSuffix}";
                    
                    stringBuilder.Append($"{Indend3}public IChildRelationRowCollection<{childRowType}> {relationPropertyName} ");
                    stringBuilder.Append($"{{ ");

                    stringBuilder.Append($"get {{ return new ChildRelationRowCollection<{childRowType}>(this, {relationNameString});  }} ");

                    stringBuilder.Append($"}}");

                    stringBuilder.AppendLine();

                    counter++;

                    counterSuffix = counter.ToString();
                }
            }
        }

        private static (string tableClassName, string rowClassName) GetRelationTableClassNames(DataTableObj table,
            string rowClassName,
            Dictionary<string, DataTableObj> tableOptions,
            string relationName,
            DataRelationObj relation, 
            bool parent)
        {
            string tableType = string.Empty;
            string rowType = string.Empty;

            var relTable = parent ? relation.ParentTable : relation.ChildTable;

            if (table.Table == relTable)
            {
                rowType = rowClassName;
                tableType = table.Class;
            }
            else
            {
                if (tableOptions == null)
                {
                    throw new InvalidOperationException($"Table generate options wasn't for relation generation. Table {table.Table}, RowClass {rowClassName}, RelationName {relationName}");
                }

                if (tableOptions.ContainsKey(relTable) == false)
                {
                    throw new InvalidOperationException($"Table generate options didn't have settings for '{relTable}' table for relation generation. Table {table.Table}, RowClass {rowClassName}, RelationName {relationName}");
                }

                var relationGenOptions = tableOptions[relTable];

                rowType = relationGenOptions.RowClass;
                tableType = relationGenOptions.Class;
            }

            return (tableType, rowType);
        }

        private static string GetGenTypeString(ColumnInfo column)
        {
            return GetGenTypeString(column, out _);
        }
        
        public static string GetStorageFieldType(ColumnInfo strType)
        {
            if (strType.Type == "Object")
            {
                return "System.object";
            }
            
            if (strType.EnumType != null)
            {
                if (strType.AllowNull ?? true)
                {
                    return $"{strType.EnumType}?";
                }
                
                return strType.EnumType;
            }

            var knowType = strType.Type;
            
            if (strType.Type != "UserType")
            {
                if (BuiltinSupportStorageTypes.KnownTypesToGenClassName.TryGetValue(strType.Type, out var genClassName))
                {
                    knowType = genClassName;
                }
            }

            var strTypeDataType = (strType.DataType ?? knowType);
            
            if (strType.TypeModifier  == "Array")
            {
                return strTypeDataType + "[]";
            }

            if (strType.TypeModifier  == "Range")
            {
                return $"Range<{strTypeDataType}>";
            }

            if (strType.DataTypeIsStruct && (strType.AllowNull ?? false))
            {
                return strTypeDataType + "?";
            }

            return strTypeDataType;
        }

        private static string GetGenTypeString(ColumnInfo column, out bool isStruct)
        {
            var type = GetStorageFieldType(column);
            
            isStruct = column.DataTypeIsStruct;
            
            return type;
        }
        
        private static string GetGenTypeString(XProperty xProperty)
        {
            var storageType = xProperty.Type;

            if (storageType == "Complex")
            {
                return xProperty.DataType ?? "object";
            }

            var type = GetXStorageFieldType(storageType, true);

            return type.type;
        }
        
        public static (string type, bool isStruct) GetXStorageFieldType(string strType, bool allowNull)
        {
            if (strType == "Complex")
            {
                return ("object", false);
            }
	        
            var type = TableStorageTypeGenerator.StorageTypes
                .FirstOrDefault(s => s.EnumName == strType);

            if (type != null)
            {
                if (type.Struct && allowNull)
                {
                    return (type.GenClassName + "?", true);
                }
		        
                return (type.GenClassName, type.Struct);
            }

            return (strType, false);
        }
    }
}