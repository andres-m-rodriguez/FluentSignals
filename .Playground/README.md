# Playground Folder

This folder is for local testing and experimentation. Everything in this folder is:
- ✅ Ignored by Git (.gitignore)
- ✅ Not included in the solution file
- ✅ Safe for local experiments

## Usage

Create any console apps or test projects here for local development:

```bash
# Example: Create a console app
cd .Playground
dotnet new console -n MyTestApp
```

## Important Notes

1. **This folder is local only** - Nothing here will be committed to Git
2. **Don't reference these projects** from main solution projects
3. **Perfect for**:
   - Quick testing
   - API experiments
   - Performance benchmarks
   - Learning and prototyping

## Keeping Projects Out of Solution

To ensure your playground projects aren't accidentally added to the solution:

1. Always create projects inside `.Playground/` folder
2. Don't use Visual Studio's "Add Project" on these
3. If using `dotnet sln add`, be careful not to include playground projects

Example structure:
```
.Playground/
├── README.md (this file)
├── ConsoleTest1/
│   └── ConsoleTest1.csproj
├── HttpPlayground/
│   └── HttpPlayground.csproj
└── PerformanceTests/
    └── PerformanceTests.csproj
```