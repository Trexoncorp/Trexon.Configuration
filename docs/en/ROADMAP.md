# Version 1.0 scope

[Русский](../ROADMAP.md) · [English](../en/ROADMAP.md) · [Deutsch](../de/ROADMAP.md)

[Contents](README.md)

The first version includes typed classes, defaults, env, dotenv, InMemory, overrides, aliases, prefixes, inheritance, nested groups, explicit scalar conversion, DataAnnotations, IValidatableObject, safe diagnostics and independent managers.

To keep the API simple, 1.0 excludes automatic reload, file watchers, mutation of previously returned objects, DI/IOptions adapters, JSON/YAML/XML files, CLI parsing, remote secret stores, collections or JSON inside env, arbitrary converters, custom providers, dotenv interpolation, source generators and Native AOT guarantees.

To obtain new values, call `Settings.Load<T>` again or create another manager. Previously issued objects do not change. Changes in a parent shell do not update the environment of an already running process.

Further features require an actual use case; no delivery dates are promised. The first stable release is `v1.0.0`.
