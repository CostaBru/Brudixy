# Contributing to Brudixy

Thank you for your interest in contributing to Brudixy! This document provides guidelines and instructions for contributing.

## Code of Conduct

Please be respectful and professional in all interactions. We aim to foster an inclusive and welcoming community.

## How to Contribute

### Reporting Issues

- Check existing issues before creating a new one
- Provide a clear title and description
- Include steps to reproduce the issue
- Specify your environment (OS, .NET version, etc.)
- Include relevant code snippets or error messages

### Submitting Changes

1. **Fork the repository** and create your branch from `main`
2. **Make your changes** following the coding standards below
3. **Add tests** for any new functionality
4. **Run all tests** to ensure nothing broke
5. **Update documentation** if needed
6. **Submit a pull request** with a clear description

## Development Setup

### Prerequisites

- .NET 8.0 SDK or later
- Visual Studio 2022, JetBrains Rider, or VS Code

### Getting Started

```bash
# Clone your fork
git clone https://github.com/YOUR-USERNAME/brudixy.git
cd brudixy

# Restore dependencies
dotnet restore Brudixy.sln

# Build the solution
dotnet build Brudixy.sln

# Run tests
dotnet test Brudixy.sln
```

## Coding Standards

### General Guidelines

- Follow existing code style and conventions
- Use meaningful variable and method names
- Keep methods focused and concise
- Add XML documentation comments for public APIs
- Avoid unnecessary dependencies

### C# Conventions

- Use `PascalCase` for class names, method names, and properties
- Use `camelCase` for local variables and parameters
- Use `_camelCase` for private fields
- Prefer `var` when the type is obvious
- Use modern C# features appropriately

### Performance Considerations

- Be mindful of allocations in hot paths
- Use `Span<T>` and `Memory<T>` where appropriate
- Benchmark performance-critical code changes
- Document any performance implications

## Testing

- Write unit tests for all new functionality
- Maintain or improve code coverage
- Use descriptive test names that explain the scenario
- Include edge cases and error conditions
- Performance benchmarks should use BenchmarkDotNet

### Test Structure

```csharp
[Test]
public void MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var sut = CreateSystemUnderTest();
    
    // Act
    var result = sut.DoSomething();
    
    // Assert
    Assert.That(result, Is.EqualTo(expected));
}
```

## Documentation

- Update README.md for significant changes
- Add XML documentation for public APIs
- Include code examples for new features
- Keep CHANGELOG.md updated

## Pull Request Process

1. Update the README.md with details of changes if applicable
2. Ensure all tests pass and code builds without warnings
3. Request review from maintainers
4. Address any feedback or requested changes
5. Once approved, your PR will be merged

## Performance Benchmarks

When adding benchmarks:

- Use BenchmarkDotNet
- Include baseline comparisons
- Document benchmark results in PR
- Place benchmarks in `Brudixy.Tests/Benchmarks/`

## Source Generator Development

When working on source generators:

- Target `netstandard2.0` for analyzer/generator projects
- Test with multiple Visual Studio/Roslyn versions
- Provide clear error messages
- Document the generated code structure
- Include example schemas

## Release Process

Maintainers will handle releases:

1. Update version numbers in .csproj files
2. Update CHANGELOG.md
3. Create a version tag (e.g., `v1.0.0`)
4. GitHub Actions will build and publish NuGet packages

## Questions?

Feel free to open an issue for questions or clarifications about contributing.

## License

By contributing to Brudixy, you agree that your contributions will be licensed under the MIT License.
