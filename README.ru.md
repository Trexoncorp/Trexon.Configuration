# Trexon.Configuration

[English](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/README.md) · [Русский](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/README.ru.md) · [Deutsch](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/README.de.md)

Типизированная конфигурация для C# / .NET 8+: классы с defaults, автоматическое чтение env, aliases, префиксы, `.env`, валидация и независимые менеджеры. Ядро без внешних NuGet-зависимостей. Лицензия MIT.

**Версия `1.0.0`** — первый стабильный релиз. Публичный API сохраняет совместимость в рамках 1.x.

## Начать с одного класса

<!-- verify: ru-readme-basic -->
```csharp
using Trexon.Configuration;

var settings = Settings.Load<AppSettings>();
Console.WriteLine($"{settings.ProjectName}: {settings.Port}");

public sealed class AppSettings
{
    public string ProjectName { get; init; } = "Widget iSpace";
    [Env("APP_PORT")] public int Port { get; init; } = 8000;
}
```

Обычный `new AppSettings()` использует defaults. `Settings.Load<AppSettings>()` создаёт новый объект, читает env и проверяет значения. Автоматическая перезагрузка отсутствует.

## Группы настроек

<!-- verify: ru-readme-manager -->
```csharp
using Trexon.Configuration;

var manager = SettingsManager.Create(config => config
    .ReadFrom.InMemory(new Dictionary<string, string> { ["VAPID_PUBLIC_KEY"] = "demo" })
    .Override("APP_PORT", "9000")
    .Register<AppSettings>()
    .Register<VapidSettings>());

if (manager.Get<AppSettings>().Port != 9000) throw new Exception("Port mismatch");
if (manager.Get<VapidSettings>().PublicKey != "demo") throw new Exception("Key mismatch");

public sealed class AppSettings
{
    [Env("APP_PORT")] public int Port { get; init; } = 8000;
}

[EnvPrefix("VAPID_")]
public sealed class VapidSettings
{
    public string PublicKey { get; init; } = "";
    [Secret] public string PrivateKey { get; init; } = "";
}
```

## Установка

Установить пакет из NuGet:

```sh
dotnet add package Trexon.Configuration --version 1.0.0
```

## Документация

- [Оглавление](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/README.md)
- [Быстрый старт](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/GETTING_STARTED.md)
- [Источники и модели](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/CONFIGURATION.md)
- [Валидация и секреты](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/VALIDATION.md)
- [Справочник API](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/API.md)
- [Архитектура](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/ARCHITECTURE.md)
- [Разработка и тесты](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/DEVELOPMENT.md)
- [Подготовка и публикация](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/PUBLISHING.md)
- [Границы версии 1.0](https://github.com/Trexoncorp/Trexon.Configuration/blob/main/docs/ROADMAP.md)
