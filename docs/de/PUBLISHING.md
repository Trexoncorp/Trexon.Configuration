# Vorbereitung und Veröffentlichung

[Русский](../PUBLISHING.md) · [English](../en/PUBLISHING.md) · [Deutsch](../de/PUBLISHING.md)

[Inhalt](README.md)

## Aktueller Stand

Vorbereitung der ersten Version `1.0.0`. Quellversion: `1.0.0-dev`. `v1.0.0` **erst nach gesonderter Anweisung des Eigentümers** erstellen und pushen. Workflow-Konfiguration und lokales Packen sind keine Veröffentlichung.

Wie bei Trexon.Logging gelten Tags `vMAJOR.MINOR.PATCH[-PRERELEASE]`, etwa `v1.0.0`. Die öffentliche API bleibt innerhalb von 1.x kompatibel; inkompatible Änderungen benötigen eine Major-Version.

## Vor einem genehmigten Release

1. Alle Dateien auf main committen, nach origin pushen, Arbeitsverzeichnis sauber halten.
2. CI für den aktuellen main muss auf Linux, Windows und macOS erfolgreich sein. Lokale Befehle: [Entwicklung](DEVELOPMENT.md).
3. Ausführbare Dokumentation und Paketinhalt prüfen; Beispiele dürfen keine echten Geheimnisse enthalten.
4. GitHub-Environment `nuget` und Repository-Variable `NUGET_USER` mit dem nuget.org-Profilnamen einrichten.
5. Auf nuget.org eine Trusted-Publishing-Policy einrichten: owner `Trexoncorp`, repository `Trexon.Configuration`, workflow file `publish.yml`, environment `nuget`. Beim ersten Paket Verfügbarkeit der PackageId und Besitzerrechte prüfen. Die Konfiguration von Logging gilt nicht automatisch für dieses Repository.

Trusted Publishing tauscht OIDC gegen einen kurzlebigen API-Schlüssel. Der Workflow benötigt kein dauerhaftes NuGet-Secret. YAML allein bestätigt keine Kontokonfiguration. Siehe [offizielle NuGet-Anleitung](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).

## Release nach Freigabe

Diese Befehle dokumentieren den später freigegebenen Release; Dokumentationsprüfungen führen sie niemals aus:

```sh
bash scripts/release-version.sh v1.0.0
git tag -a v1.0.0 -m "Trexon.Configuration 1.0.0"
git push origin v1.0.0
```

`release-version.sh` prüft SemVer: Präfix `v` erforderlich, keine Build-Metadaten und keine führenden Nullen in numerischen Bezeichnern. Der getaggte Commit muss von `origin/main` erreichbar sein. Veröffentlichte Tags nicht verschieben.

Bei Push von `v*` prüft `publish.yml` Tag, Dokumentation, Build und Tests erneut, packt die Tag-Version und bewahrt nupkg/snupkg 30 Tage auf. Ein separater Job im Environment `nuget` holt den OIDC-Schlüssel und veröffentlicht Paket und Symbole. Fehlende NUGET_USER oder eine falsche Policy führen zum Fehler; ein Tag allein bestätigt keinen erfolgreichen NuGet-Release.

Danach beide Jobs, Version `1.0.0` auf nuget.org und Installation in einer sauberen Anwendung prüfen. Nach Erfolg den Release-Status in allen README-Sprachen aktualisieren. Ein GitHub Release mit Notes ist eine separate Operation; dieser Workflow erstellt keines.
