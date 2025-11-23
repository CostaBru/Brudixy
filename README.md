# Brudixy

Brudixy is an advanced data structure library for .NET that provides high-performance, strongly-typed data management capabilities with code generation support.

## Overview

Brudixy provides a powerful alternative to traditional DataSet/DataTable approaches, offering:
- **Type-safe data structures** generated at compile-time from YAML schemas
- **High-performance collections** with advanced indexing capabilities
- **Change tracking and transactions**
- **Serialization support** (JSON, XML)
- **Source generators** for zero-runtime reflection
- **Flexible storage backends** with customizable column storage strategies

## Projects

### Core Libraries

#### Brudixy.Core
The core library containing the fundamental data structures and runtime components.

**Key Features:**
- `CoreDataSet` - Base dataset implementation with relation support
- `CoreDataTable` - High-performance table implementation
- Change tracking and transaction support
- JSON and XML serialization
- Constraint enforcement
- Event system for data modifications

**Target Framework:** .NET 8.0

**Dependencies:**
- Akade.IndexedSet
- Konsarpoo (high-performance collections)
- System.Text.Json

#### Brudixy
Extended dataset functionality building on `Brudixy.Core`.

**Key Features:**
- `DataSet` class extending `CoreDataSet`
- Additional helper methods and utilities
- Enhanced data manipulation APIs

**Target Framework:** .NET 8.0

**Dependencies:**
- Brudixy.Core
- Brudixy.Interfaces

#### Brudixy.Interfaces
Interface definitions and shared contracts for the Brudixy ecosystem.

**Key Features:**
- Core interfaces (ICoreDataSet, ICoreDataTable, etc.)
- Delegate definitions for events
- Tool abstractions
- Annotation attributes

**Target Framework:** .NET 8.0

### Code Generation

#### Brudixy.Interfaces.Generators
Source generator infrastructure for generating storage type implementations.

**Key Features:**
- Table storage type generators
- Type mapping and conversion generators
- Array creation generators
- Deep equality comparison generators
- Built-in storage type support

**Target Framework:** .NET Standard 2.0 (for Roslyn compatibility)

**Dependencies:**
- Microsoft.CodeAnalysis.CSharp 3.11.0
- JetBrains.Annotations

#### Brudixy.Generators
Source generators for creating data item implementations.

**Key Features:**
- Generates strongly-typed row classes
- Index generation
- Storage implementation generation

**Target Framework:** .NET Standard 2.0

**Dependencies:**
- Brudixy.Interfaces.Generators
- Microsoft.CodeAnalysis.CSharp 3.11.0

#### Brudixy.TypeGenerator
Source generator for creating strongly-typed DataSet and DataTable classes from YAML schema definitions.

**Key Features:**
- YAML schema parsing
- DataSet class generation
- DataTable class generation
- Index generation from schema
- Relation generation

**Target Framework:** .NET Standard 2.0

**Dependencies:**
- Brudixy.Generators
- Brudixy.Interfaces.Generators
- YamlDotNet 13.2.0

#### Brudixy.TypeGenerator.Core
Shared core components for the type generator system.

**Key Features:**
- Schema parsing logic
- Code generation helpers
- YAML schema reader
- Column and table metadata structures

**Target Framework:** .NET 8.0

**Dependencies:**
- Brudixy.Interfaces.Generators
- YamlDotNet 13.2.0

### Testing

#### Brudixy.Tests
Comprehensive test suite with unit tests and performance benchmarks.

**Key Features:**
- NUnit test cases
- BenchmarkDotNet performance tests
- YAML schema examples
- Integration tests

**Target Framework:** .NET 8.0

**Test Runner:** NUnit 3.13.2

#### Brudixy.TypeGenerator.Tests
Tests for the type generator functionality.

**Target Framework:** (To be determined)

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022 or JetBrains Rider (recommended)

### Building the Solution

```bash
# Restore dependencies
dotnet restore Brudixy.sln

# Build the solution
dotnet build Brudixy.sln

# Run tests
dotnet test Brudixy.sln
```

### Using Brudixy in Your Project

1. Reference the core libraries:
```xml
<ItemGroup>
  <PackageReference Include="Brudixy" Version="1.0.0" />
  <PackageReference Include="Brudixy.Core" Version="1.0.0" />
</ItemGroup>
```

2. Add the type generator for YAML-based schema generation:
```xml
<ItemGroup>
  <ProjectReference Include="Brudixy.TypeGenerator.csproj" 
                    OutputItemType="Analyzer" 
                    ReferenceOutputAssembly="true" />
  
  <!-- Add your schema files -->
  <AdditionalFiles Include="Schemas/*.brudixy.yaml" />
</ItemGroup>
```

3. Define your schema in YAML format (see examples in `Brudixy.Tests/TypedDs/`)

4. Build your project - strongly-typed classes will be generated automatically!

## Schema Example

```yaml
# MyTable.st.brudixy.yaml
Table: MyTable
Columns:
  - Name: Id
    Type: int
    PrimaryKey: true
  - Name: Name
    Type: string
  - Name: CreatedDate
    Type: DateTime
Indexes:
  - Columns: [Name]
```

This generates a strongly-typed `MyTable` class with:
- Type-safe row access
- LINQ-like querying with indexes
- Change tracking
- Serialization support

## Architecture

```
┌─────────────────────┐
│   Your Application  │
└──────────┬──────────┘
           │
┌──────────▼──────────┐       ┌────────────────────┐
│   Brudixy (API)     │◄──────┤ Generated Classes  │
└──────────┬──────────┘       └─────────┬──────────┘
           │                             │
┌──────────▼──────────┐       ┌─────────▼───────────┐
│   Brudixy.Core      │       │ Brudixy.TypeGenerator│
│   (Runtime)         │       │  (Source Generator)  │
└──────────┬──────────┘       └──────────────────────┘
           │
┌──────────▼──────────┐
│ Brudixy.Interfaces  │
└─────────────────────┘
```

## Performance

Brudixy is designed for high performance:
- Zero-allocation enumerators where possible
- Efficient indexing using Akade.IndexedSet
- Minimal boxing/unboxing with generic storage
- Compile-time code generation eliminates reflection

See `Brudixy.Tests/Benchmarks/` for detailed performance benchmarks.

## Advanced Features

### Custom Storage Types
Define custom column storage strategies for optimized memory usage and performance.

### Change Tracking
Track modifications at row and column level with transaction support.

### Relations
Define foreign key relationships between tables with referential integrity.

### Extensibility
Use extension properties to attach metadata without schema changes.

## Contributing

Contributions are welcome! Please ensure:
- All tests pass
- Code follows existing style conventions
- New features include tests
- Performance-critical code includes benchmarks

## License

(License information to be added)

## Dependencies

### Runtime Dependencies
- **Akade.IndexedSet** (1.4.0) - Indexed collections
- **Konsarpoo** (5.3.0) - High-performance collections
- **System.Text.Json** (9.0.0-preview.6) - JSON serialization
- **JetBrains.Annotations** (2024.2.0-eap1) - Code annotations

### Build-time Dependencies
- **Microsoft.CodeAnalysis.CSharp** (3.11.0) - Roslyn code generation
- **YamlDotNet** (13.2.0) - YAML parsing

## Version History

- **1.0.0** - Initial release
  - Core data structures
  - YAML-based type generation
  - Comprehensive test coverage

## Support

For issues, questions, or contributions, please use the GitHub issue tracker
