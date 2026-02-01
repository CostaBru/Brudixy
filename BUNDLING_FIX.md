# Bundling Fix: Exposing Brudixy.Interfaces as Compile References

## Problem

When consumers install the Brudixy NuGet package, they cannot use types from the `Brudixy.Interfaces` namespace even though:
- The Brudixy public API exposes many `Brudixy.Interfaces` types (e.g., `IDataTable`, `IDataTableRow`, `IDataRowReadOnlyAccessor`)
- The `Brudixy.Interfaces.dll` is bundled into the package

**Root Cause**: The bundled assemblies were not properly marked as compile-time references for consuming projects.

## Solution Applied

### 1. Changed Bundling Target from `<None>` to `<TfmSpecificPackageFile>`

**Before** (in `Brudixy_IncludeBundledProjectOutputs` target):
```xml
<None Include="@(_BundledAssemblies)" Pack="true" PackagePath="lib\\$(TargetFramework)\\" />
```

**After**:
```xml
<TfmSpecificPackageFile Include="@(_BundledAssemblies)">
  <PackagePath>lib/$(TargetFramework)</PackagePath>
</TfmSpecificPackageFile>
```

**Why**: `TfmSpecificPackageFile` is the correct MSBuild item type for adding files to a NuGet package's `lib/` folder as compile-time references. Using `<None>` places files in the package but doesn't generate the proper `.nuspec` metadata to make them reference assemblies.

### 2. Changed Target Hook from `BeforeTargets="Pack"` to `BeforeTargets="_GetPackageFiles"`

**Why**: `_GetPackageFiles` is the correct target for collecting package content. Running before `Pack` was too late for the `TfmSpecificPackageFile` items to be properly processed.

### 3. Added Transitive Package Dependencies

Added explicit `<PackageReference>` entries for dependencies of the bundled projects:

```xml
<ItemGroup>
  <!-- Direct dependencies of Brudixy -->
  <PackageReference Include="System.Text.Json" Version="9.0.0-preview.6.24327.7" />
  <PackageReference Include="YamlDotNet" Version="16.2.0" />
  <PackageReference Include="NJsonSchema" Version="11.1.0" />
  
  <!-- Transitive dependencies from bundled Brudixy.Core and Brudixy.Interfaces -->
  <PackageReference Include="Akade.IndexedSet" Version="1.4.0" />
  <PackageReference Include="JetBrains.Annotations" Version="2024.2.0-eap1" />
  <PackageReference Include="Konsarpoo" Version="5.3.0" />
  <PackageReference Include="System.Reflection.Emit.Lightweight" Version="4.6.0-preview8.19405.3" />
</ItemGroup>
```

**Why**: When you bundle assemblies with `PrivateAssets="all"`, their package dependencies don't flow through automatically. Since `Brudixy.Interfaces` depends on `JetBrains.Annotations` and `System.Text.Json`, and `Brudixy.Core` depends on additional packages, we must explicitly declare these in the Brudixy package.

## Technical Details

### How NuGet Package References Work

When a project references a NuGet package:

1. **Assemblies in `lib/$(TargetFramework)/`**: Automatically added as compile-time references
2. **Dependencies in `.nuspec`**: NuGet resolves and adds as transitive package references
3. **Content files**: Copied to project but not referenced

### Why `PrivateAssets="all"` Breaks Bundling

```xml
<ProjectReference Include="..\Brudixy.Interfaces\Brudixy.Interfaces.csproj" PrivateAssets="all" />
```

This tells MSBuild:
- ✅ DO compile against `Brudixy.Interfaces.dll`
- ❌ DON'T add it as a package dependency in the `.nuspec`
- ❌ DON'T flow its package dependencies

So we must:
1. Manually include the DLL in `lib/` (done via `TfmSpecificPackageFile`)
2. Manually declare its dependencies (done via `PackageReference`)

## Verification

To verify the fix works:

### 1. Pack the Brudixy package

```powershell
dotnet pack .\Brudixy\Brudixy.csproj -c Release -o .\artifacts\test-pack
```

### 2. Inspect the package contents

```powershell
# Extract the .nupkg (it's a zip file)
Expand-Archive .\artifacts\test-pack\Brudixy.*.nupkg -DestinationPath .\artifacts\test-pack\extracted -Force

# Check lib folder contains all three DLLs
Get-ChildItem .\artifacts\test-pack\extracted\lib\net8.0\
# Should show: Brudixy.dll, Brudixy.Core.dll, Brudixy.Interfaces.dll

# Check .nuspec has all dependencies
Get-Content .\artifacts\test-pack\extracted\Brudixy.nuspec
```

### 3. Test with a consumer project

```powershell
# Create test project
dotnet new console -n TestConsumer
cd TestConsumer

# Add local package source
dotnet nuget add source D:\Dev\Brudixy\artifacts\test-pack -n BrudixLocal

# Add the package
dotnet add package Brudixy --version 1.0.0

# Create test code
@"
using Brudixy;
using Brudixy.Interfaces;

var table = new DataTable();
IDataTable iTable = table;
Console.WriteLine("Success! Both Brudixy and Brudixy.Interfaces types are available.");
"@ | Out-File Program.cs

# Build - should succeed without errors
dotnet build
```

## Expected Results

After this fix:
- ✅ Consumer projects can use `Brudixy.Interfaces` types
- ✅ IntelliSense works for interface types
- ✅ Compilation succeeds without "type not found" errors
- ✅ All dependencies are automatically restored

## Files Changed

- `Brudixy\Brudixy.csproj`: 
  - Modified `Brudixy_IncludeBundledProjectOutputs` target
  - Added transitive package dependencies
