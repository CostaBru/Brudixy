# Brudixy Type Pipeline Guide

## Overview
Brudixy turns compact YAML schemas into strongly-typed data tables and datasets ready for production code. This guide explains how schemas, generators, and runtime APIs fit together so you can design efficient models without digging through internals.

## Key Projects
- **Brudixy** / **Brudixy.Core**: runtime data structures (`DataTable`, `DataSet`, change tracking, serialization, persistence helpers).
- **Brudixy.TypeGenerator**: Roslyn source generator that parses `.brudixy.yaml` files and emits C# tables/datasets.
- **Brudixy.TypeGenerator.Core**: schema parsing, inheritance merging, row-interface generation, supporting types like `DataTableObj`.
- **Reference unit tests**: the solution ships extensive NUnit suites (e.g., `CodeGenTests`, `TestUserTypeClassTypedTable`) that double as usage samples referenced throughout this document.

## YAML Schema Conventions
Schema files live under `Brudixy.Tests/TypedDs`:
- `*.st.brudixy.yaml`: stand-alone tables. Example: `Test/BaseTable.st.brudixy.yaml` defines a common base table with audit columns.
- `*.dt.brudixy.yaml`: dataset table extensions referenced by dataset definitions.
- `*.ds.brudixy.yaml`: dataset definitions that list tables, relations, and code-generation options. Example: `Ds/Nmt.ds.brudixy.yaml` sets namespace `Flexols.Data`, lists tables, and defines relations plus `XProperties`.

Key schema fields:
- `CodeGenerationOptions`: namespaces, sealed/abstract, base table file path.
- `PrimaryKey`: column list.
- `Columns`: `name: Type | modifiers`. Modifiers include `Class`, `Auto`, `Index`, `Unique`, custom expressions, etc.
- `ColumnOptions`: per-column overrides (`AllowNull`, `DefaultValue`, `MaxLength`, `XProperties`).
- `Tables/TableOptions`: nested tables for datasets, optionally overriding `FileName` or property names.
- `Relations`: define parent/child tables and keys. The generator uses these to emit relation wiring inside datasets.
- `XProperties`: arbitrary metadata the runtime can read (e.g., `CheckErrors: true`).

## Generator Workflow
- Add `Brudixy.TypeGenerator` as an analyzer reference in your project and include your schema files via `AdditionalFiles`:
  ```xml
  <ItemGroup>
    <ProjectReference Include="..\Brudixy.TypeGenerator\Brudixy.TypeGenerator.csproj"
                      OutputItemType="Analyzer"
                      ReferenceOutputAssembly="false" />
    <AdditionalFiles Include="Schemas/**/*.brudixy.yaml" />
  </ItemGroup>
  ```
- Build the project so MSBuild feeds every `.brudixy.yaml` file to the generator.
1. During compilation, `TypeGenerator.Execute` (see `Brudixy.TypeGenerator/TypeGenerator.cs`) scans `AdditionalFiles`, classifies them by suffix, and inlines file contents into an in-memory `FileSystemAccess` for consistent parsing (supports relative base-table includes).
2. YAML parsing uses `YamlSchemaReader` (wraps YamlDotNet) to deserialize into `DataTableObj` graphs.
3. `DataCodeGenerator.GenerateDatasetFiles/GenerateTableFiles` (in `Brudixy.TypeGenerator.Core/CodeGenerator.cs`) walks each dataset/table:
   - Resolves `BaseTableFileName` chains via `LoadBaseTables`, merging ancestor columns and tracking columns that should be skipped in derived tables.
   - Loads nested tables (`*.dt`) referenced by dataset definitions, ensuring namespaces and relations propagate.
   - Emits table class + row interfaces (two files per table) and dataset container classes. Output tuples include `(hintName, sourceText, sourcePath)`; Roslyn writes them to `Brudixy.Tests/Brudixy.Gen/net8.0/Brudixy.TypeGenerator/Brudixy.TypeGenerator.TypeGenerator` because `EmitCompilerGeneratedFiles` is enabled in the test project.
4. Diagnostic helpers write `Files.log` and `Test.log` files into the generated folder for debugging.

## Default Behaviors and Naming Rules
- **Namespaces**: if `CodeGenerationOptions.Namespace` is omitted, the generator derives it from the schema file path relative to the consuming project (see `GetDefaultNameSpace` in `CodeGenerator.cs`). Interfaces default to the same namespace unless `InterfaceNamespace` overrides.
- **Class names**: default to the `Table` value. `CodeGenerationOptions.Class` and `RowClass` can rename the table or row types. Tables are sealed unless `Sealed: false` or `Abstract: true` is set.
- **Base classes**: fallback to `Brudixy.DataTable` / `Brudixy.DataRow`. Supplying `BaseClass`, `BaseRowClass`, or `BaseTableFileName` lets schemas inherit columns/logic. `LoadBaseTables` merges ancestors while guarding against circular chains.
- **Columns**: unspecified `AllowNull` defaults to `true` unless the column participates in the primary key. Adding `?` (nullable) or `!` (non-null) to the column type forces nullability. Arrays (`[]`) and ranges (`<>`) automatically set `AllowNull` unless overridden.
- **Indexes**: `ColumnOptions.<column>.HasIndex` or `| Index` modifier will create a runtime index. `Unique` and `Auto` translate into unique index definition and auto-increment metadata respectively.
- **Relations**: if a dataset omits `ParentTable` or `ChildTable`, both default to the root dataset table, so always specify both for cross-table relations.
- **XProperties**: declared at the column level or under `ColumnOptions.XProperties`; consumers can retrieve them through runtime metadata to drive UI rules or persistence features.

## Generated Artifacts
Location: `Brudixy.Tests/Brudixy.Gen/net8.0/Brudixy.TypeGenerator/Brudixy.TypeGenerator.TypeGenerator/`.
Typical outputs:
- `{Namespace}_{Table}.cs`: table definitions inheriting `Brudixy.DataTable`, e.g., `Flexols.Core.Common.Base.Data.BaseTables_BaseTable.cs`.
- `{Namespace}_{Table}.RowInterfaces.cs`: interface definitions for row/container types, e.g., `Flexols.Production.sNomenclature_t_nmt.RowInterfaces.cs`.
- Dataset aggregates such as `Flexols.Data_dsNmt.cs` expose typed properties `ItemsTable`, `GroupsTable`, etc., derived from `TableOptions` in the schema.
- `Brudixy.Tests.TypedDs.UserTypes_TestClassTable*.cs`: demonstrates user-defined column types (`UserClass`, tuple) per schemas in `TypedDs/UserTypes`.
- `Files.log.cs` and `Test.log.cs`: lightweight tracing of generator inputs and execution context.

## Consumer Workflow
1. **Author schema** under `TypedDs` (or your own project) following the suffix conventions. Reuse shared column sets via `BaseTableFileName` to avoid duplication.
2. **Tune defaults** by updating `CodeGenerationOptions` (namespaces, sealed, append method name, etc.) and `ColumnOptions` for validation, indexes, and metadata.
3. **Build** the project referencing `Brudixy.TypeGenerator`; generated files appear alongside your compilation artifacts (when `EmitCompilerGeneratedFiles` is enabled they land in `obj/$(Configuration)/generated`).
4. **Use typed APIs**: instantiate datasets/tables from their namespaces, call `Append`/`NewRow`, and rely on generated row interfaces for DI-friendly abstractions.
5. **Leverage metadata**: inspect `CoreDataColumnInfo` or `XProperties` to drive UI, validation, or persistence rules at runtime.
6. **Regenerate** whenever schemas change; incremental builds update generated sources automatically.

## Practical API Examples (drawn from unit tests)
- **Dataset logging and auditing** — see `Brudixy.Tests/Tests/CodeGenTests.cs` (`TestDatasetLogging`). Generated dataset `Flexols.Data.dsNmt` exposes typed tables `ItemsTable`, `GroupsTable`, `PropertiesTable`. Example usage:
  ```csharp
  var ds = new Flexols.Data.dsNmt();
  var g = ds.GroupsTable.Append(id: 1, name: "root", guid: Guid.NewGuid());
  using (ds.StartLoggingChanges("Session"))
  {
      var row = ds.ItemsTable.NewRow(_.MapObj(("id", 10)));
      row.fullname = "nmt code";
      ds.ItemsTable.AddRow(row);
  }
  var logs = ds.GetTableLoggedChanges("t_nmt");
  ```
  *Best usage*: wrap mutating operations inside `StartLoggingChanges` to capture granular change sets, then call `AcceptChanges` when the batch is committed.

- **User-defined reference types** — `Brudixy.Tests/Tests/TestUserTypeClassTypedTable.cs` exercises schema `TypedDs/UserTypes/TestUserTypeClass.st.brudixy.yaml`. Generated table `Brudixy.Tests.TypedDs.UserTypes.TestClassTable` stores a `UserClass` column declared as `UserClass | Class`. Example:
  ```csharp
  var table = new TestClassTable();
  table.Append(id: new UserClass { Name = "Custom" });
  var typedRow = table.GetRowByPk(new UserClass { Id = 1 });
  ```
  *Best usage*: use `| Class` modifier for reference types so the generator marks the column nullable and avoids struct boxing.

- **Tuple-based structural columns** — `TestUserTypeTupleTypedTable.cs` relies on `TestUserTypeTuple.st.brudixy.yaml`, which defines `id: (int val1, int val2)`. Generated rows expose strongly typed tuples:
  ```csharp
  var tupleTable = new TestTupleTable();
  tupleTable.Append(id: (val1: 1, val2: 5));
  ```
  *Best usage*: tuple syntax `(<fields>)` is ideal for compound keys or coordinates without authoring a dedicated class.

- **Indexes for fast lookups** — `IndexesTests.cs` shows how `ColumnOptions` with `HasIndex: true` surfaces runtime helpers:
  ```csharp
  var table = new Flexols.Core.Common.Base.Data.BaseTables.BaseDocTable();
  var row = table.NewRow(_.MapObj(("id", 1)));
  table.AddRow(row);
  var indexedRow = table.GetRow("id", 1); // O(log n) via generated index
  ```
  *Best usage*: pair `HasIndex`/`Unique` with lookups via `GetRow`/`GetRows` for deterministic performance; avoid manual scans.

- **Transactions + edit isolation** — `BeginEditTests.cs` illustrates how to combine `BeginEditRow`, transactions, and commit/rollback semantics:
  ```csharp
  var table = CreateTableWithData();
  var original = table.GetRowByHandle(0);
  var edit = table.BeginEditRow(original);
  var tx = table.StartTransaction();
  edit.Set("TestSet1", 42);
  tx.Commit(); // or tx.Rollback();
  table.EndEditRow(edit); // finalize edit, reattaches row
  ```
  *Best usage*: wrap long-running edits inside transactions to coordinate multi-row updates; use `BeginEditRow` for copy-on-write safety, then `EndEditRow` to merge changes.

- **Dataset event subscriptions** — `CodeGenTests.TestDeadRefWeakEvent` and `TestWeakEventExc` demonstrate subscribing via weak events:
  ```csharp
  var disposable = new DisposableCollection();
  var changeEvent = new DataEvent<object>(disposable);
  changeEvent.Subscribe((payload, ctx) => Console.WriteLine(payload));
  changeEvent.Raise("Changed");
  disposable.Dispose(); // handlers auto-detach
  ```
  *Best usage*: prefer `DataEvent<T>` for dataset/table notifications to avoid leaks; wrap subscriptions in `DisposableCollection` so disposing the scope unsubscribes listeners.

- **Serialization, deep copy, deep equals** — `SeriealizationTests.cs` covers XML/JSON persistence and structural equality:
  ```csharp
  var table = CreateNmtTable();
  var copy = table.Copy(); // deep copy including rows/metadata
  var xml = table.ToXml(SerializationMode.Full);
  var fromXml = new DataTable();
  fromXml.LoadFromXml(xml);
  var equals = table.EqualsExt(fromXml);
  ```
  *Best usage*: call `Copy` for in-memory cloning, `Clone` for schema-only copies, and `EqualsExt` to validate serialized payloads before applying migrations.

- **Row/table cloning** — `BeginEditTests` clones edit containers to preserve snapshots:
  ```csharp
  var editRow = table.BeginEditRow(row);
  var snapshot = (DataRowContainer)editRow.Clone();
  // mutate editRow
  table.EndEditRow(editRow);
  snapshot.CopyTo(rowAccessor);
  ```
  *Best usage*: clone containers via `RowContainer.Clone()` when you need a point-in-time view for audit or rollback logic.

- **Extended properties metadata** — `SeriealizationTests.TestColXProps` shows attaching metadata at every level:
  ```csharp
  table.SetXProperty("Width", "555"); // table-level
  var nameCol = table.GetColumn("Name");
  nameCol.SetXProperty("Display", "Full name");
  var row = table.GetRowByPk(1);
  row.SetXProperty("Flag", "1");
  var cellValue = row["Name"];
  row.SetColumnInfo("Name", "Read-only when archived");
  ```
  *Best usage*: use X-properties for dynamic UI hints and diagnostics—`SetColumnError/Warning/Info` populate per-cell annotations, while `SetXPropertyAnnotation` can stash structured metadata (key/value per property).

- **Row-cell metadata and validation** — `BeginEditTests.MutateRow` sets errors/warnings:
  ```csharp
  row.SetColumnError("TestSet1", "Invalid range");
  row.SetRowWarning("Pending approval");
  row.SetColumnInfo("TestSet1", "Displayed as code");
  row.SetXPropertyAnnotation("X", "Key", 1);
  ```
  *Best usage*: prefer per-column annotations for granular validation feedback; pair them with `GetColumnErrors()` when surfacing validation summaries.

## API Guidelines and Best Usage
- Prefer **dataset-level operations** (`StartLoggingChanges`, `StartTrackingChangeTimes`, relations navigation) when multiple tables interact, as illustrated in `CodeGenTests.TestDatasetLogging`.
- Use **row containers** (`RowContainer` derivatives) for detached editing; they honor column-level validation and support `CopyFrom` semantics (`TestClassTableRowContainer` examples in generated files).
- Exploit **relation helpers** by naming relations clearly in YAML; generated datasets expose `Relations` metadata plus navigation methods (e.g., `GetChildRows`, `GetParentRows`).
- Extend functionality via **partial classes** next to generated files; avoid editing generated code so schema-driven updates remain painless.
- For **custom metadata**, rely on `XProperties` and query them through `CoreDataColumnInfo.PersistedXProperties` to keep UI/business layers schema-driven.

## Troubleshooting
- Missing output: ensure YAML file suffix is correct (`st/dt/ds`). Generator ignores others.
- Relation errors: check `Relations` section for consistent column names; generator logs issues as `.Error` files in `Brudixy.Gen/...` (see `CodeGenerator.GenerateTableCode`).
- Base table merge loops: `LoadBaseTables` throws `BaseTableParseException` with hierarchy list to help locate circular references.
- YamlDotNet loader issues: generator emits warnings `BRXTY001/BRXTY002` when embedded YAML parser fails; fix by rebuilding to restore resource or referencing `YamlDotNet` properly.

## Coding-Agent Checklist
1. Confirm the schema + generator inputs live under `Brudixy.Tests/TypedDs` (or caller-specific folder) and the project includes them as `AdditionalFiles`.
2. When adjusting defaults, reason through `CodeGenerationOptions` and column modifiers rather than editing generated files.
3. If runtime behavior needs customization, add partial classes near the consumer project and keep generated artifacts untouched.
4. Before diagnosing runtime issues, inspect `Files.log.cs`/`Test.log.cs` in `Brudixy.Gen` to verify the generator saw the expected YAML files.
5. Capture repro steps and relevant YAML snippets when filing bugs—`DataTableObj` mirrors schema input, so matching terminology accelerates fixes.

## Artifact Locations Cheat Sheet
- Schemas: `Brudixy.Tests/TypedDs/**/*.brudixy.yaml`
- Generated code: `Brudixy.Tests/Brudixy.Gen/net8.0/Brudixy.TypeGenerator/Brudixy.TypeGenerator.TypeGenerator/`
- Logs from generator: `Files.log.cs`, `Test.log.cs` in the same folder.
- Runtime APIs tested: `Brudixy` (`DataTable`, `DataSet`, persistence, events) + `Brudixy.Core` internals.

## Next Steps for Agents
- Need to modify generator? Start in `Brudixy.TypeGenerator.Core` (metadata/merging) and `Brudixy.TypeGenerator/TypeGenerator.cs` (Roslyn glue).
- Need to add/adjust schemas? Edit YAML under `TypedDs`, rebuild tests, inspect `Brudixy.Gen` output.
- Need to expand documentation? This file can be extended with sections on YAML syntax, schema validation, or runtime extension points.
