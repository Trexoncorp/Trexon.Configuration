# Development and verification

[Русский](../DEVELOPMENT.md) · [English](../en/DEVELOPMENT.md) · [Deutsch](../de/DEVELOPMENT.md)

[Contents](README.md)

The layout follows Trexon.Logging: net8.0 core, SDK 10 selected by `global.json`, xUnit v3 / Microsoft Testing Platform, net8.0/net10.0 tests, console sample, RU/EN/DE documentation and NuGet workflows. Install both runtimes. Documentation checks require Python 3.

```text
Trexon.Configuration.slnx
Trexon.Configuration/                  core without PackageReference
  Internal/                            binding, conversion, dotenv, keys
tests/Trexon.Configuration.Tests/       non-packable net8/net10 tests
examples/Trexon.Configuration.Sample/   non-packable net8 sample + sample.env
docs/                                  Russian guides
docs/en/, docs/de/                     translations
scripts/check-docs.py                   links, languages, runnable C# examples
scripts/release-version.sh              SemVer tag validation
.github/workflows/                     CI and publishing
```

From the repository root:

```sh
dotnet restore Trexon.Configuration.slnx
dotnet build Trexon.Configuration.slnx --configuration Release --no-restore -warnaserror
dotnet test --solution Trexon.Configuration.slnx --configuration Release --no-build --no-restore
dotnet run --project examples/Trexon.Configuration.Sample --configuration Release --no-build
python3 scripts/check-docs.py
git diff --check
```

## Test coverage

| File | Contracts |
| --- | --- |
| `BindingTests.cs` | Defaults, inheritance, prefixes, aliases, nesting, nullable, invalid schemas, user code failures |
| `ConversionTests.cs` | Supported types, ru-RU culture, overflow, invalid and ambiguous input |
| `ValidationTests.cs` | Required/Range, nested defaults, custom validators, IValidatableObject, value-free diagnostics |
| `SourceAndManagerTests.cs` | Precedence, env isolation, shared manager snapshot, independence, concurrent reads, dotenv grammar and file failures |

Tests changing process env use a nonparallel collection and restore original values in finally. Temporary files are deleted. Ordinary tests use InMemory to avoid machine-specific env.

## Documentation

`python3 scripts/check-docs.py --links-only` checks three languages, links, anchors and markers. The full command compiles and runs C# blocks marked `<!-- verify: unique-name -->` in temporary projects referencing the current library. It does not execute shell blocks, tags or publication. Marker names are unique across all languages.

A new page in `docs/` needs matching pages in `docs/en/` and `docs/de/`. Update API.md for public signatures and the appropriate guide for behavior; keep all languages aligned. Checks do not assess translation quality.

## Local NuGet

The working version is `1.0.0-dev`. After the first commit, verify repository metadata and packing:

```sh
dotnet build Trexon.Configuration/Trexon.Configuration.csproj --configuration Release -p:ContinuousIntegrationBuild=true -warnaserror
dotnet pack Trexon.Configuration/Trexon.Configuration.csproj --configuration Release --no-build --output artifacts -p:ContinuousIntegrationBuild=true -warnaserror
```

The artifacts directory contains nupkg/snupkg. Packages include the net8.0 DLL, XML docs, MIT, three README languages and Source Link metadata. Build and pack versions must match. Local `-p:Version=1.0.0` is allowed for verification and does not create a release or tag.

CI runs on Linux, Windows and macOS: restore → docs on Linux → warnings-as-errors build → tests on both runtimes → sample → pack. Actions use pinned SHAs. Use `actionlint` locally when available. Local verification does not imply GitHub Actions has already run.

See [publishing](PUBLISHING.md) for the separate release operation.
