# Brudixy.TypeGenerator

Source generator for Brudixy that turns YAML schemas into strongly-typed DataTable/DataSet code at build time.

## Install

```xml
<ItemGroup>
  <PackageReference Include="Brudixy" Version="1.0.0" />
  <PackageReference Include="Brudixy.TypeGenerator" Version="1.0.0" PrivateAssets="all" />
  <AdditionalFiles Include="Schemas\**\*.brudixy.yaml" />
  <AdditionalFiles Include="Schemas\**\*.st.brudixy.yaml" />
</ItemGroup>
```

## YAML schema files

There are two common shapes:

- **Single table**: `*.st.brudixy.yaml` with a `Table` root.
- **DataSet-style container**: `*.brudixy.yaml` with `Tables` and optional relations/indexes.

### Single table (minimal)

```yaml
---
Table: Users
PrimaryKey:
  - Id
Columns:
  Id: Int32
  Name: String
```

### DataSet-style (multiple tables)

```yaml
---
Tables:
  - Users
  - Orders
TableOptions:
  Users:
    FileName: Users.st.brudixy.yaml
  Orders:
    FileName: Orders.st.brudixy.yaml
Relations:
  FK_Orders_Users:
    ParentTable: Users
    ParentKey: Id
    ChildTable: Orders
    ChildKey: UserId
    RelationType: OneToMany
```

## Schema reference (key fields)

Top-level keys (table or dataset):

- `Table`: name of a single table (single-table schemas).
- `Tables`: list of table names (dataset schema).
- `TableOptions`: per-table options for dataset schemas. Common fields: `FileName`, `CodeProperty`.
- `Columns`: map of column name to type (single-table schemas).
- `PrimaryKey`: list of column names.
- `Relations`: map of relation name to definition.
- `Indexes`: map of index name to definition.
- `XProperties`: table-level extended properties.
- `EnforceConstraints`: enable/disable relation constraints.

Column options (in `ColumnOptions`):

- `Type`: storage type (see supported types below).
- `TypeModifier`: `Simple`, `Array`, or `Range`.
- `DataType`: full CLR type name for `UserType` or `EnumType`.
- `EnumType`: CLR enum type for enum-backed columns.
- `AllowNull`, `IsUnique`, `IsReadOnly`, `IsService`, `Auto`, `HasIndex`.
- `DefaultValue`, `MaxLength`, `Expression`, `DisplayName`, `CodeProperty`.
- `XProperties`: column-level extended properties.

## Supported types

Built-in storage types:

- `Object`
- `Boolean` (`Bool`, `Flag`)
- `Char`
- `SByte`, `Byte`
- `Int16`, `UInt16`
- `Int32` (`Integer`), `UInt32`
- `Int64`, `UInt64`
- `Single`, `Double`, `Decimal` (`Money`)
- `DateTime`, `DateTimeOffset`, `TimeSpan`
- `String`
- `Guid`
- `BigInteger`
- `Uri`, `Type`
- `Xml` (XElement), `Json` (JsonObject)
- `UserType`

Type aliases are case-insensitive. `TypeModifier` can be `Array` or `Range` where supported.

## Custom types

To store your own CLR type:

1) Use `UserType` and set `DataType` to the full CLR type name.
2) Register the type at runtime:

```csharp
using Brudixy;

CoreDataTable.RegisterUserType<MyType>();
// Optional: string conversion for YAML or serialization
CoreDataTable.RegisterUserTypeStringMethods<MyType>(
    value => value.ToString(),
    text => MyType.Parse(text));
```

## Notes

- Generators run at build time; the runtime library is `Brudixy`.
- For schema validation or advanced options, check the README in the root package.
