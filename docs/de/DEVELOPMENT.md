# Entwicklung und Prüfung

[Русский](../DEVELOPMENT.md) · [English](../en/DEVELOPMENT.md) · [Deutsch](../de/DEVELOPMENT.md)

[Inhalt](README.md)

Der Aufbau folgt Trexon.Logging: net8.0-Kern, SDK 10 über `global.json`, xUnit v3 / Microsoft Testing Platform, Tests auf net8.0/net10.0, Konsolenbeispiel, RU/EN/DE-Dokumentation und NuGet-Workflows. Beide Laufzeitumgebungen installieren. Dokumentationsprüfungen benötigen Python 3.

```text
Trexon.Configuration.slnx
Trexon.Configuration/                  Kern ohne PackageReference
  Internal/                            Bindung, Konvertierung, dotenv, Schlüssel
tests/Trexon.Configuration.Tests/       nicht paketierte net8/net10-Tests
examples/Trexon.Configuration.Sample/   nicht paketiertes net8-Beispiel + sample.env
docs/                                  russische Anleitungen
docs/en/, docs/de/                     Übersetzungen
scripts/check-docs.py                   Links, Sprachen, ausführbare C#-Beispiele
scripts/release-version.sh              SemVer-Tag-Prüfung
.github/workflows/                     CI und Veröffentlichung
```

Im Repository-Stamm:

```sh
dotnet restore Trexon.Configuration.slnx
dotnet build Trexon.Configuration.slnx --configuration Release --no-restore -warnaserror
dotnet test --solution Trexon.Configuration.slnx --configuration Release --no-build --no-restore
dotnet run --project examples/Trexon.Configuration.Sample --configuration Release --no-build
python3 scripts/check-docs.py
git diff --check
```

## Testabdeckung

| Datei | Verträge |
| --- | --- |
| `BindingTests.cs` | Standardwerte, Vererbung, Präfixe, Aliase, Verschachtelung, nullable, ungültige Schemas, Fehler in Modellcode |
| `ConversionTests.cs` | Unterstützte Typen, Kultur ru-RU, Überläufe, ungültige und mehrdeutige Eingaben |
| `ValidationTests.cs` | Required/Range, verschachtelte Standardwerte, eigene Validatoren, IValidatableObject, Diagnosen ohne Werte |
| `SourceAndManagerTests.cs` | Prioritäten, Umgebungsisolation, gemeinsamer Snapshot, Unabhängigkeit, paralleles Lesen, dotenv-Grammatik und Dateifehler |

Tests mit Änderungen der Prozessumgebung laufen nicht parallel und stellen Werte in finally wieder her. Temporäre Dateien werden gelöscht. Andere Tests verwenden InMemory und hängen nicht von der Entwicklerumgebung ab.

## Dokumentation

`python3 scripts/check-docs.py --links-only` prüft drei Sprachen, Links, Anker und Marker. Der vollständige Aufruf kompiliert und startet C#-Blöcke mit `<!-- verify: unique-name -->` in temporären Projekten mit Verweis auf die aktuelle Bibliothek. Shell-Blöcke, Tags und Veröffentlichung werden nicht ausgeführt. Marker sind über alle Sprachen eindeutig.

Neue Seiten in `docs/` benötigen gleichnamige Seiten in `docs/en/` und `docs/de/`. Öffentliche Signaturen in API.md und Verhalten im passenden Leitfaden in allen Sprachen aktualisieren. Die Prüfungen bewerten keine Übersetzungsqualität.

## Lokales NuGet

Arbeitsversion: `1.0.0-dev`. Nach dem ersten Commit Paket und Repository-Metadaten prüfen:

```sh
dotnet build Trexon.Configuration/Trexon.Configuration.csproj --configuration Release -p:ContinuousIntegrationBuild=true -warnaserror
dotnet pack Trexon.Configuration/Trexon.Configuration.csproj --configuration Release --no-build --output artifacts -p:ContinuousIntegrationBuild=true -warnaserror
```

Unter artifacts entstehen nupkg und snupkg. Enthalten sind net8.0-DLL, XML-Dokumentation, MIT, drei README-Sprachen und Source-Link-Metadaten. Build- und Pack-Version müssen übereinstimmen. Lokales `-p:Version=1.0.0` dient der Prüfung und erstellt weder Release noch Tag.

CI auf Linux, Windows und macOS: restore → Dokumentation auf Linux → Build mit Warnungen als Fehler → Tests auf beiden Laufzeitumgebungen → Beispiel → pack. Actions sind per SHA fixiert. Falls vorhanden, Workflows lokal mit `actionlint` prüfen. Lokale Prüfungen bestätigen keinen bereits erfolgten GitHub-Actions-Lauf.

Der eigentliche Release wird separat in [Veröffentlichung](PUBLISHING.md) beschrieben.
