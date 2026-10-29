# Быстрый старт

[Русский](GETTING_STARTED.md) · [English](en/GETTING_STARTED.md) · [Deutsch](de/GETTING_STARTED.md)

[Оглавление](README.md)

Потребителю нужен .NET 8 или новее. Разработчику репозитория нужен SDK 10 и runtime .NET 8 и .NET 10. Ядро не зависит от ASP.NET, DI и Trexon.Logging.

Установка из NuGet:

```sh
dotnet add package Trexon.Configuration --version 1.0.0
```

Версия `1.0.0` — первый стабильный выпуск.

Класс должен иметь публичный конструктор без параметров. Настройки описываются публичными свойствами с `get; init;` или `get; set;`. Предпочтителен `init`, чтобы приложение не меняло загруженные настройки.

<!-- verify: ru-start -->
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

В оболочке задайте `APP_PORT=9000` перед запуском процесса — `Port` станет `9000`. Если переменной нет, сохранится `8000`. Загрузчик не читает свойства при каждом обращении: env применяется один раз при `Load`.

Несколько групп можно загрузить одним менеджером. Значения `InMemory` скопированы при добавлении источника, поэтому пример изолирован от env процесса.

<!-- verify: ru-groups -->
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

Полный пример по сценарию Widget/VAPID находится в `examples/Trexon.Configuration.Sample/`. Из корня репозитория:

```sh
dotnet run --project examples/Trexon.Configuration.Sample
```

Рядом с бинарником копируется `sample.env` только с демонстрационными значениями. Env процесса имеет больший приоритет. Приватный ключ не печатается. Чтобы увидеть ошибку валидации, запустите пример с `APP_PORT=70000`. Пример вернёт код 1.

Для использования с Trexon.Logging сначала загрузите настройки, затем передайте нужный уровень в `Logger.Create`. Библиотеки независимы; преобразование своего строкового уровня выполняет приложение.
