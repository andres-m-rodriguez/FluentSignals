# NuGet Package Publishing Guide

This guide explains how to set up and use the automated NuGet package publishing for FluentSignals.

## Overview

The project is configured to automatically publish NuGet packages when you push to the `master` or `main` branch. The CI/CD pipeline handles:

- Building and testing the code
- Creating NuGet packages
- Publishing to NuGet.org
- Creating GitHub releases

## Prerequisites

1. **NuGet Account**: Create an account at [nuget.org](https://www.nuget.org/)
2. **API Key**: Generate an API key from your NuGet account settings
3. **GitHub Repository**: Push your code to GitHub

## Setup Instructions

### 1. Add NuGet API Key to GitHub Secrets

1. Go to your GitHub repository
2. Navigate to **Settings** → **Secrets and variables** → **Actions**
3. Click **New repository secret**
4. Name: `NUGET_API_KEY`
5. Value: Your NuGet API key
6. Click **Add secret**

### 2. Update Package Information

Edit the `Directory.Build.props` file to update:

```xml
<Authors>Andres Rodriguez</Authors>
<Company>Andres Rodriguez</Company>
<PackageProjectUrl>https://github.com/yourusername/FluentSignals</PackageProjectUrl>
<RepositoryUrl>https://github.com/yourusername/FluentSignals</RepositoryUrl>
```

Replace `yourusername` with your actual GitHub username.

### 3. Add Package Icons

Replace the placeholder `icon.png` files in each project folder with actual PNG icons:
- Recommended size: 128x128 or 256x256 pixels
- Format: PNG with transparency
- Location: 
  - `/FluentSignals/icon.png`
  - `/FluentSignals.Blazor/icon.png`

## Publishing Process

### Automatic Publishing

1. Update the version in `Directory.Build.props`:
   ```xml
   <Version>1.0.1</Version>
   ```

2. Commit and push to master:
   ```bash
   git add .
   git commit -m "Release v1.0.1"
   git push origin master
   ```

3. The GitHub Actions workflow will:
   - Build and test the code
   - Create NuGet packages
   - Check if the version already exists
   - Publish to NuGet.org if it's a new version
   - Create a GitHub release

### Manual Publishing

To manually create and publish packages:

```bash
# Clean previous builds
dotnet clean

# Create Release packages
dotnet pack --configuration Release

# Push to NuGet
dotnet nuget push ./FluentSignals/bin/Release/FluentSignals.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json

dotnet nuget push ./FluentSignals.Blazor/bin/Release/FluentSignals.Blazor.1.0.0.nupkg \
  --api-key YOUR_API_KEY \
  --source https://api.nuget.org/v3/index.json
```

## Version Management

### Semantic Versioning

Follow [Semantic Versioning](https://semver.org/):
- **Major** (1.0.0 → 2.0.0): Breaking changes
- **Minor** (1.0.0 → 1.1.0): New features, backward compatible
- **Patch** (1.0.0 → 1.0.1): Bug fixes, backward compatible

### Pre-release Versions

For pre-release versions, use suffixes:
```xml
<Version>1.0.0-beta1</Version>
<Version>1.0.0-rc1</Version>
```

## Troubleshooting

### Build Fails

1. Check the GitHub Actions logs
2. Ensure all tests pass locally: `dotnet test`
3. Verify the code builds: `dotnet build`

### Package Not Publishing

1. Check if the version already exists on NuGet
2. Verify the API key is correctly set in GitHub secrets
3. Ensure you're pushing to the correct branch (master/main)

### Icon Not Showing

1. Ensure icon.png is a valid PNG file
2. Check that the icon is included in the package:
   ```xml
   <None Include="icon.png" Pack="true" PackagePath="\" />
   ```

## Local Testing

Before publishing, test the packages locally:

1. Create local packages:
   ```bash
   dotnet pack --configuration Release
   ```

2. Create a local NuGet source:
   ```bash
   dotnet nuget add source ./packages --name local
   ```

3. Test in a new project:
   ```bash
   dotnet new console -n TestApp
   cd TestApp
   dotnet add package FluentSignals --source ../packages
   ```

## Package Contents

Each package includes:
- Compiled assemblies
- XML documentation
- README.md
- License information
- Package icon
- Source Link for debugging

## Best Practices

1. **Always test locally** before pushing to master
2. **Update README** when adding new features
3. **Document breaking changes** in release notes
4. **Keep version synchronized** between packages
5. **Tag releases** in Git for reference

## Support

For issues or questions:
- Create an issue on GitHub
- Check the build logs in GitHub Actions
- Review the NuGet publishing documentation