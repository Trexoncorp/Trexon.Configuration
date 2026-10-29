# Umfang von Version 1.0

[Русский](../ROADMAP.md) · [English](../en/ROADMAP.md) · [Deutsch](../de/ROADMAP.md)

[Inhalt](README.md)

Die erste Version umfasst typisierte Klassen, Standardwerte, Umgebung, dotenv, InMemory, Overrides, Aliase, Präfixe, Vererbung, verschachtelte Gruppen, explizite skalare Konvertierung, DataAnnotations, IValidatableObject, sichere Diagnosen und unabhängige Manager.

Für eine einfache API enthält 1.0 kein automatisches Reload, keine File Watcher, keine Änderung bereits ausgegebener Objekte, DI/IOptions-Adapter, JSON/YAML/XML-Dateien, CLI-Parsing, entfernten Secret Stores, Sammlungen oder JSON in Umgebungswerten, beliebigen Converter, eigenen Provider, dotenv-Interpolation, Source Generator oder Native-AOT-Garantien.

Für neue Werte `Settings.Load<T>` erneut aufrufen oder einen neuen Manager erstellen. Bereits ausgegebene Objekte bleiben unverändert. Änderungen in einer übergeordneten Shell aktualisieren nicht die Umgebung eines bereits laufenden Prozesses.

Weitere Funktionen werden nach tatsächlichem Bedarf ergänzt; es gibt keine Terminzusagen. Der Release `v1.0.0` wartet auf eine gesonderte Anweisung des Eigentümers.
