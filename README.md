# Brudixy

High-performance in-memory data tables for .NET, with optional **compile-time code generation** from YAML schemas.

Brudixy is aimed at two kinds of consumers:

1) **Runtime users**: you want a fast in-memory table with indexing, querying, relations, expressions, and serialization.
2) **Schema-driven users**: you want strongly-typed tables/datasets generated from YAML at build time.

---

## Highlights

- Fast in-memory tables with **indexes** (single + multi-column)
- Fluent query API (`table.Rows.Where(...).Equals(...)`)
- **Computed columns / filters** via expressions
- **Relations** with cascade rules
- Change tracking, transactions, and optional **change logging**
- JSON/XML serialization
- Extensible metadata: XProperties + row/cell annotations

---

## Packages

Runtime:

- `Brudixy` – main consumer package (brings `Brudixy.Core` + `Brudixy.Interfaces`)

Build-time (source generators):

- `Brudixy.TypeGenerator` – YAML-driven generator that produces strongly-typed DataSets/DataTables

---

## Install

### Runtime only

Add the `Brudixy` package.

### Runtime + code generation

Add:

- `Brudixy` (runtime)
- `Brudixy.TypeGenerator` as an analyzer (`PrivateAssets="all"`)
- YAML schema files as `AdditionalFiles`

---

## Quickstart (runtime)

### 1) Create a table + add columns

```csharp
using Brudixy;

var table = new DataTable("Users");

table.AddColumn("Id", TableStorageType.Int32, unique: true);
table.AddColumn("Name", TableStorageType.String);

table.SetPrimaryKeyColumn("Id");
```

### 2) Add a row

```csharp
var r = table.NewRow();
r["Id"] = 1;
r["Name"] = "Alice";

table.AddRow(r);
```

### 3) Query

```csharp
var row = table.GetRow("Id", 1);
var name = row.Field<string>("Name");

var filtered = table.Rows
    .Where("Name").AsString().StartsWith("Al")
    .ToData();
```

---

## Quickstart (YAML schema → generated types)

1. Put schema files under `Schemas/`.
2. Include them as `AdditionalFiles`.
3. Add the generator as an analyzer.

**Example `.csproj`**

```xml
<ItemGroup>
  <PackageReference Include="Brudixy" Version="1.0.0" />

  <!-- build-time only -->
  <PackageReference Include="Brudixy.TypeGenerator" Version="1.0.0" PrivateAssets="all" />

  <AdditionalFiles Include="Schemas\**\*.brudixy.yaml" />
</ItemGroup>
```

**Single-table schema** (`*.st.brudixy.yaml`)

```yaml
---
Table: Users
PrimaryKey:
  - Id
Columns:
  Id: Int32
  Name: String
```

> Generator output becomes available during compilation. You don’t ship the generator at runtime.

---

## Core concepts

### DataTable, DataRow, and DataRowContainer

- `DataTable` is the main structure.
- `DataRow` is a row stored in a table.
- `DataRowContainer` is a detached, serializable/editable row container (useful for JSON/XML round-trips, UI, and patching).

> **Important:** Brudixy tables are composable.
>
> - A `DataTable` can act as a **dataset container** (a collection of child tables). This is why you’ll see APIs like `AddTable(...)`, `GetTable(...)`, and relations referencing table names.
> - A `DataTable` can also be stored as a **value**:
>   - inside a **cell** (column value)
>   - inside **XProperties** (table/row/column XProperties and XProperty annotations)
>
> This makes it possible to model nested documents/graphs directly in-memory.

### Indexes

Indexes are the key to performance.

- Single-column: `AddIndex("Email")` / `AddIndex("Email", unique: true)`
- Multi-column: `AddMultiColumnIndex(new[] { "A", "B" }, unique: false)`

### Arrays and ranges

Columns can store:

- a single value (`Simple`)
- arrays (`Array`)
- ranges (`Range`)

In YAML you can specify this via `TypeModifier: Array|Range`.

---

## Expressions (computed columns + filters)

### Computed columns

```csharp
table.AddColumn("FullName", TableStorageType.String, dataExpression: "FirstName + ' ' + LastName");
```

### Filtering

```csharp
var rows = table.Select("Id = 5 AND Len(Name) > 2");
```

### Check a filter against a single row

```csharp
bool ok = table.Rows.First().CheckFilter("Id = 5 AND Name <> ''");
```

### Register custom functions

You can extend the expression engine:

```csharp
using Brudixy.Expressions;

FunctionRegistry.Registry.RegisterFunction("IsEven", _ => new IsEvenFunction());
```

---

## Relations (including cascade rules)

Brudixy supports relations between tables (dataset style) and self-relations.

```csharp
var ds = new DataTable("MyDs");
var parent = ds.AddTable("Parent");
var child = ds.AddTable("Child");

parent.AddColumn("Id", TableStorageType.Int32, unique: true);
child.AddColumn("ParentId", TableStorageType.Int32);

ds.AddRelation(
    relationName: "FK_Child_Parent",
    parentKey: ("Parent", "Id"),
    childKey:  ("Child", "ParentId"),
    relationType: RelationType.OneToMany,
    constraintUpdate: Rule.Cascade,
    constraintDelete: Rule.Cascade,
    acceptRejectRule: AcceptRejectRule.Cascade);
```

Notes:

- `constraintUpdate` controls parent key update behavior.
- `constraintDelete` controls delete cascades.
- `acceptRejectRule` controls `AcceptChanges()` / `RejectChanges()` propagation.

---

## Change tracking, transactions, and logging

### Transactions

```csharp
var tran = table.StartTransaction();

// ... make changes

tran.Rollback(); // or tran.Commit();
```

### Change logging (audit stream)

```csharp
using var _ = table.StartLoggingChanges("Import #42");
// ... change rows / xprops
var log = table.GetLoggedChanges();
```

Transaction connection:

- Each log entry can carry a `TranId`.
- On rollback, Brudixy removes log entries for rolled-back transactions.

---

## Defaults, nullability, and safe conversion

- `row["Col"]` returns the raw stored value (can be `null`).
- `row.Field<T>("Col")` returns a typed value and can apply safe conversions.
- For nullable strings/arrays, `Field<string>` can return `""` and `Field<int[]>` can return an empty array even when stored value is null.

Arrays are treated as immutable values:

- assignment copies the array
- `FieldArray<T>()` provides a cached reference for fast repeated access

---

## Debugging (visualizers)

Brudixy is debugger-friendly:

- `DataTable` has a helpful `DebuggerDisplay` with counts/index info.
- `DataRow` / `DataRowContainer` have a debug view (`DataRowDebugView`) that shows:
  - column values
  - ages / changed fields
  - annotations and XProperties
  - parent/child summaries (when relations exist)

---

## Comparing rows and containers

Recommended pattern when validating serialization or container logic:

```csharp
var row = table.GetRowByPk(new (1, 1));
var container = row.ToContainer();

var cmp = DataRowContainer.CompareDataRows(row, container);
if (cmp.cmp != 0)
{
    throw new Exception(cmp.ToString());
}
```

---

## YAML schemas (runtime loading)

If you want runtime-loaded schemas (plugins/config-driven), use:

- `LoadSchemaFromYaml(string yaml)`
- `LoadSchemaFromYamlFile(string path)`
- `ToYaml()`

Brudixy supports a compact schema form:

```yaml
Table: SimpleTable
Columns:
  Id: Int32
  Name: String
PrimaryKey:
  - Id
```

For advanced column options (default values, max length, expressions, etc.), use the verbose form.

---

## Dapper support (vendored)

Brudixy includes a built-in copy of Dapper (`SqlMapper`) in the `Brudixy` assembly.

```csharp
using System.Data;
using Brudixy;

using var conn = /* IDbConnection */;
var rows = conn.Query<MyPoco>("select Id, Name from Users where Id = @id", new { id = 1 });
```

If you also reference the external `Dapper` package, you may get ambiguous extension method resolution.

---

## For contributors / maintainers

- Build and pack instructions live in `PROJECT_SETUP.md` and `NUGET_PUBLISHING.md`.
- This README is intended for package consumers.
