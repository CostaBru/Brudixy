# NuGet Publishing Guide

This guide explains how to build and publish Brudixy packages to NuGet.org.

## Prerequisites

1. **NuGet API Key**: Obtain an API key from [NuGet.org](https://www.nuget.org/account/apikeys)
2. **.NET 8.0 SDK**: Ensure you have .NET 8.0 or later installed
3. **GitHub Repository**: Set up a GitHub repository with the code

## Local Build and Pack

### Build All Projects

```powershell
# Clean previous builds
dotnet clean Brudixy.sln

# Restore dependencies
dotnet restore Brudixy.sln

# Build in Release mode
dotnet build Brudixy.sln --configuration Release
```

### Run Tests

```powershell
# Run all tests
dotnet test Brudixy.sln --configuration Release
```

### Create NuGet Packages

```powershell
# Create output directory
New-Item -ItemType Directory -Force -Path .\packages

# Pack all projects
dotnet pack Brudixy.Interfaces\Brudixy.Interfaces.csproj --configuration Release --output .\packages
dotnet pack Brudixy.Core\Brudixy.Core.csproj --configuration Release --output .\packages
dotnet pack Brudixy\Brudixy.csproj --configuration Release --output .\packages
dotnet pack Brudixy.Interfaces.Generators\Brudixy.Interfaces.Generators.csproj --configuration Release --output .\packages
dotnet pack Brudixy.Generators\Brudixy.Generators.csproj --configuration Release --output .\packages
dotnet pack Brudixy.TypeGenerator\Brudixy.TypeGenerator.csproj --configuration Release --output .\packages
```

### Publish to NuGet.org

```powershell
# Set your API key (one time only)
$env:NUGET_API_KEY = "your-api-key-here"

# Push all packages
dotnet nuget push ".\packages\*.nupkg" --api-key $env:NUGET_API_KEY --source https://api.nuget.org/v3/index.json --skip-duplicate
```

## GitHub Actions (Automated Publishing)

### Setup GitHub Secrets

1. Go to your GitHub repository
2. Navigate to **Settings > Secrets and variables > Actions**
3. Add a new repository secret:
   - Name: `NUGET_API_KEY`
   - Value: Your NuGet.org API key

### Publish via GitHub Actions

#### Option 1: Tag-based Release

Create and push a version tag:

```bash
git tag v1.0.0
git push origin v1.0.0
```

This automatically triggers the `publish.yml` workflow.

#### Option 2: Manual Workflow Dispatch

1. Go to **Actions** tab in GitHub
2. Select **Publish NuGet Packages** workflow
3. Click **Run workflow**
4. Enter the version number (e.g., `1.0.0`)
5. Click **Run workflow**

## Version Management

### Update Version Numbers

Before publishing, update version numbers in `.csproj` files:

```xml
<Version>1.0.1</Version>
```

Or use the automated GitHub Actions workflow which updates versions automatically.

### Versioning Strategy

Follow [Semantic Versioning](https://semver.org/):

- **MAJOR** version (1.0.0): Incompatible API changes
- **MINOR** version (1.1.0): New functionality, backward compatible
- **PATCH** version (1.0.1): Bug fixes, backward compatible

## Package Dependencies

The packages have the following dependency order (publish in this order):

1. **Brudixy.Interfaces** (no dependencies on other Brudixy packages)
2. **Brudixy.Interfaces.Generators** (no dependencies on other Brudixy packages)
3. **Brudixy.Core** (depends on Brudixy.Interfaces)
4. **Brudixy.Generators** (depends on Brudixy.Interfaces.Generators)
5. **Brudixy** (depends on Brudixy.Core, Brudixy.Interfaces)
6. **Brudixy.TypeGenerator** (depends on multiple packages)

The automated workflow handles this order automatically.

## Verifying Published Packages

After publishing, verify on NuGet.org:

1. Go to [NuGet.org](https://www.nuget.org/)
2. Search for "Brudixy"
3. Check that all packages are listed
4. Verify package metadata and dependencies

## Testing Published Packages

Create a test project to verify:

```powershell
# Create test console app
dotnet new console -n BrudixTest
cd BrudixTest

# Add the packages
dotnet add package Brudixy --version 1.0.0
dotnet add package Brudixy.TypeGenerator --version 1.0.0

# Build and test
dotnet build
```

## Troubleshooting

### Package Already Exists

If you get an error that the package version already exists:
- Increment the version number
- Use `--skip-duplicate` flag (already in scripts)

### Missing Dependencies

Ensure all dependency packages are published first following the order above.

### Authentication Errors

- Verify your API key is valid
- Check that the API key has push permissions
- Ensure the secret is correctly set in GitHub

### Build Errors

- Clean solution: `dotnet clean`
- Delete `bin` and `obj` folders
- Restore packages: `dotnet restore`
- Try building individual projects

## Continuous Deployment

The GitHub Actions workflows provide:

1. **Build and Test** (`build.yml`): Runs on every push/PR
   - Multi-platform testing (Linux, Windows, macOS)
   - Code coverage reporting
   - Test result artifacts

2. **Publish** (`publish.yml`): Runs on version tags
   - Automated version updates
   - Package creation
   - NuGet.org publishing
   - GitHub release creation with artifacts

## Best Practices

1. **Always test before publishing**: Run full test suite
2. **Update CHANGELOG.md**: Document changes
3. **Update README.md**: Keep documentation current
4. **Use tags for releases**: Enables automatic deployment
5. **Monitor CI/CD**: Check GitHub Actions for failures
6. **Verify packages**: Test installation in clean environment

## Support

For issues with publishing or packages:
- Check GitHub Actions logs
- Review NuGet.org package status
- Create issue in GitHub repository
