# Разработка и проверки

[Русский](DEVELOPMENT.md) · [English](en/DEVELOPMENT.md) · [Deutsch](de/DEVELOPMENT.md)

[Оглавление](README.md)

Используется то же оформление, что в Trexon.Logging: ядро `net8.0`, SDK 10 через `global.json`, xUnit v3 / Microsoft Testing Platform, тесты `net8.0;net10.0`, console sample, документация RU/EN/DE и NuGet workflows. Установите оба runtime. Для проверки документации нужен Python 3.

```text
Trexon.Configuration.slnx
Trexon.Configuration/                  основная библиотека без PackageReference
  Internal/                            binding, conversion, dotenv, ключи
tests/Trexon.Configuration.Tests/       непакуемые тесты net8/net10
examples/Trexon.Configuration.Sample/   непакуемый пример net8 + sample.env
docs/                                  русские руководства
docs/en/, docs/de/                     переводы
scripts/check-docs.py                   ссылки, языки, исполняемые C#-примеры
scripts/release-version.sh              проверка SemVer-тега
.github/workflows/                     CI и публикация
```

Обычная проверка из корня репозитория:

```sh
dotnet restore Trexon.Configuration.slnx
dotnet build Trexon.Configuration.slnx --configuration Release --no-restore -warnaserror
dotnet test --solution Trexon.Configuration.slnx --configuration Release --no-build --no-restore
dotnet run --project examples/Trexon.Configuration.Sample --configuration Release --no-build
python3 scripts/check-docs.py
git diff --check
```

## Что проверяют тесты

| Файл | Контракты |
| --- | --- |
| `BindingTests.cs` | Defaults, наследование, префиксы, aliases, вложенность, nullable, недопустимые схемы, ошибки пользовательского кода |
| `ConversionTests.cs` | Все поддерживаемые типы, культура ru-RU, overflow, нечисловые и неоднозначные значения |
| `ValidationTests.cs` | Required/Range, вложенные defaults, собственные validators, IValidatableObject, отсутствие значений в диагностике |
| `SourceAndManagerTests.cs` | Приоритеты, изоляция env, общий снимок менеджера, независимость, параллельное чтение, синтаксис dotenv и ошибки файлов |

Тесты, меняющие env процесса, выполняются в непараллельной коллекции и восстанавливают исходные значения через finally. Временные файлы удаляются. Для обычных тестов используется InMemory, чтобы не зависеть от машины разработчика.

## Документация

`python3 scripts/check-docs.py --links-only` проверяет три языка, ссылки, заголовки и маркеры. Полный запуск компилирует и выполняет C#-блоки `<!-- verify: unique-name -->` во временном проекте, ссылающемся на текущую библиотеку. Shell-блоки, Git-теги и публикация не исполняются. Названия маркеров уникальны во всех языках.

Для новой страницы в `docs/` добавьте одноимённые страницы в `docs/en/` и `docs/de/`. Публичный API отражается в API.md, поведение — в профильном руководстве, все три версии обновляются вместе. Проверка не оценивает качество перевода.

## Локальный NuGet

Рабочая версия — `1.0.0-dev`. После первого коммита можно проверить пакет с repository metadata:

```sh
dotnet build Trexon.Configuration/Trexon.Configuration.csproj --configuration Release -p:ContinuousIntegrationBuild=true -warnaserror
dotnet pack Trexon.Configuration/Trexon.Configuration.csproj --configuration Release --no-build --output artifacts -p:ContinuousIntegrationBuild=true -warnaserror
```

В `artifacts/` создаются nupkg и snupkg. В пакет входят DLL net8.0, XML-документация, MIT, README на трёх языках и Source Link metadata. Версия для build и pack должна совпадать; локально `-p:Version=1.0.0` допустим для проверки, но не создаёт релиз или тег.

CI выполняется на Linux, Windows, macOS: restore → docs на Linux → build с warnings-as-errors → тесты обоих runtime → sample → pack. Actions закреплены SHA. Настройки workflow проверяются локально через `actionlint`, если установлен. Локальная проверка не означает, что GitHub Actions уже запускался.

Документация, пакет и CI подготовлены к выпуску; фактический выпуск описан в [публикации](PUBLISHING.md).
