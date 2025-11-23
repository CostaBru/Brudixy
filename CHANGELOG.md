# Changelog

All notable changes to the Brudixy project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- Initial project structure
- Core data structure implementations
- YAML-based type generation
- Comprehensive test coverage
- NuGet package configuration
- GitHub Actions CI/CD pipelines

## [1.0.0] - TBD

### Added
- **Brudixy.Core** - Core library with CoreDataSet and CoreDataTable
- **Brudixy** - Extended DataSet functionality
- **Brudixy.Interfaces** - Interface definitions and contracts
- **Brudixy.Interfaces.Generators** - Source generator infrastructure
- **Brudixy.Generators** - Data item source generators
- **Brudixy.TypeGenerator** - YAML-based DataSet/DataTable generator
- **Brudixy.TypeGenerator.Core** - Type generator core components
- High-performance collections with advanced indexing
- Change tracking and transaction support
- JSON and XML serialization
- Constraint enforcement
- Relation support with referential integrity
- Extension property system
- Comprehensive unit tests
- Performance benchmarks using BenchmarkDotNet

### Dependencies
- .NET 8.0 target framework
- Akade.IndexedSet 1.4.0
- Konsarpoo 5.3.0
- System.Text.Json 9.0.0-preview.6
- JetBrains.Annotations
- Microsoft.CodeAnalysis.CSharp 3.11.0 (for generators)
- YamlDotNet 13.2.0 (for schema parsing)

### Documentation
- Comprehensive README with architecture overview
- Contributing guidelines
- MIT License
- Code examples and schema samples

[Unreleased]: https://github.com/brudixy/brudixy/compare/v1.0.0...HEAD
[1.0.0]: https://github.com/brudixy/brudixy/releases/tag/v1.0.0
