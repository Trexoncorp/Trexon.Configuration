# Валидация, ошибки и секреты

[Русский](VALIDATION.md) · [English](en/VALIDATION.md) · [Deutsch](de/VALIDATION.md)

[Оглавление](README.md)

Загрузка проверяет значения после применения всех источников. Поддерживаются синхронные `ValidationAttribute` из DataAnnotations: например `[Required]`, `[Range]`, `[StringLength]`, `[RegularExpression]`, а также пользовательские атрибуты. Проверяются и defaults, не только значения из env. Вложенные группы проверяются рекурсивно.

`[Required]` проверяет null и пустые/пробельные строки, а не сам факт наличия env. Непустой default удовлетворяет правилу. Для обязательного числа без default используйте nullable-свойство, например `[Required] public int? Port { get; init; }`. C# `required` и nullable-аннотации не заменяют runtime-валидацию.

<!-- verify: ru-validation -->
```csharp
using System.ComponentModel.DataAnnotations;
using Trexon.Configuration;

try
{
    Settings.Load<AppSettings>(config => config
        .ReadFrom.InMemory(new Dictionary<string, string> { ["APP_PORT"] = "70000" }));
    throw new Exception("Expected validation failure");
}
catch (SettingsException exception)
{
    if (exception.Errors.Count != 2) throw;
    Console.WriteLine(exception.Message);
}

public sealed class AppSettings
{
    [Env("APP_PORT"), Range(1, 65535)] public int Port { get; init; } = 8000;
    [Required, Secret] public string Token { get; init; } = "";
}
```

## Связанные правила

Для проверки нескольких свойств реализуйте `IValidatableObject`. Его `Validate(ValidationContext)` вызывается после успешного заполнения и проверки свойств. Атрибуты класса также поддерживаются. Не помещайте загрузку ресурсов и сетевые запросы в конструкторы, getters/setters и валидаторы; это обычный пользовательский код, который библиотека вызывает синхронно.

## Ошибки

`SettingsException.Errors` — read-only-список `SettingsError`: путь свойства, ключ, источник, стадия, безопасное сообщение и флаг чувствительности. Стадии: `Source`, `Model`, `Conversion`, `Validation`. Источники значений: `default`, `environment`, `memory`, `override`, `dotenv:<absolute path>`; для ошибок схемы и правил объекта — `model`.

Ошибки свойств собираются вместе. В менеджере собираются ошибки всех зарегистрированных групп. Ошибка источника останавливает чтение сразу; сломанный конструктор или схема не дают проверить свойства этой группы. Объектные правила не запускаются, если его свойства уже некорректны. Частично заполненный объект или менеджер не возвращается.

Неверный аргумент fluent API вызывает `ArgumentException`/`ArgumentNullException`. Пустой менеджер и `Get<T>` для незарегистрированного типа вызывают `InvalidOperationException`.

## Секреты

Библиотека не включает значения настроек в свои сообщения, `SettingsError` и inner exceptions. Пользовательские сообщения валидаторов и исключений также не копируются: выводится название правила или этап сбоя. Имена ключей, пути свойств и путь dotenv-файла остаются видимыми, поэтому не помещайте секреты в эти идентификаторы.

`[Secret]` выставляет `IsSecret` в метаданных ошибки свойства. Для вложенной группы флаг передаётся её свойствам. Это не шифрование и не маскирование самого объекта: приложение получает настоящий ключ. `JsonSerializer.Serialize(settings)`, запись объекта в лог и пользовательский `GetVapidKeys()` могут раскрыть секрет. Логируйте только нужные несекретные поля. Побочные эффекты собственного кода модели библиотека не контролирует.

В Git игнорируются `.env` и `.env.*`, кроме `.env.example`. В `sample.env` допустимы только демонстрационные значения.
