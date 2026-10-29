# Release preparation and publishing

[Русский](../PUBLISHING.md) · [English](../en/PUBLISHING.md) · [Deutsch](../de/PUBLISHING.md)

[Contents](README.md)

## Current status

The first stable release is `1.0.0`. The source working version is `1.0.0-dev`; release workflows take the version from the tag. Create and push `v1.0.0` **only after a separate owner instruction**. Workflow setup and local packing do not constitute publication.

This repository follows Trexon.Logging: `vMAJOR.MINOR.PATCH[-PRERELEASE]` tags, such as `v1.0.0`. Public API compatibility is maintained within 1.x; breaking changes require a major version.

## Before an authorized release

1. Commit all files on main, push to origin and keep a clean working tree.
2. CI on the current main must pass on Linux, Windows and macOS. Local commands are in [development](DEVELOPMENT.md).
3. Verify runnable docs and package contents; samples must contain no real secrets.
4. Create GitHub environment `nuget` and repository variable `NUGET_USER` containing the nuget.org profile name.
5. Configure a nuget.org Trusted Publishing policy: owner `Trexoncorp`, repository `Trexon.Configuration`, workflow file `publish.yml`, environment `nuget`. For the first package, check PackageId availability and owner rights. Logging account configuration does not automatically cover this repository.

Trusted Publishing exchanges OIDC for a short-lived API key. No permanent NuGet secret is needed by this workflow. YAML alone does not confirm account configuration. See the [official NuGet guide](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).

## Release after authorization

These commands document the future authorized release; documentation checks never execute them:

```sh
bash scripts/release-version.sh v1.0.0
git tag -a v1.0.0 -m "Trexon.Configuration 1.0.0"
git push origin v1.0.0
```

`release-version.sh` validates SemVer: a `v` prefix is required; build metadata and leading zeros in numeric identifiers are rejected. The tagged commit must be reachable from `origin/main`. Never move published tags.

On `v*` pushes, `publish.yml` rechecks the tag, docs, build and tests, packs the tag version and retains nupkg/snupkg for 30 days. A separate job in environment `nuget` obtains an OIDC key and publishes package and symbols. Missing NUGET_USER or an incorrect policy fails publication; a tag alone does not prove a successful NuGet release.

After running, check both jobs, version `1.0.0` on nuget.org and installation in a clean consumer application. Update release status in all README languages after success. A GitHub Release with notes is a separate operation; this workflow does not create one.
