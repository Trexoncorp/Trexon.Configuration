# Публичный API

[Русский](API.md) · [English](en/API.md) · [Deutsch](de/API.md)

[Оглавление](README.md)

Все типы расположены в `Trexon.Configuration`. Ниже приведены сигнатуры, а не самостоятельный компилируемый файл. Создание `SettingsSourceConfiguration`, `SettingsManager` и `SettingsException` выполняется библиотекой; их конструкторы не публичны.

```csharp
public static class Settings
{
    public static T Load<T>(Action<SettingsConfiguration>? configure = null) where T : class;
}
public sealed class SettingsManager
{
    public static SettingsManager Create(Action<SettingsConfiguration> configure);
    public T Get<T>() where T : class;
}
public sealed class SettingsConfiguration
{
    public SettingsConfiguration();
    public SettingsSourceConfiguration ReadFrom { get; }
    public SettingsConfiguration Override(string key, string value);
    public SettingsConfiguration Register<T>() where T : class;
}
public sealed class SettingsSourceConfiguration
{
    public SettingsConfiguration EnvironmentVariables();
    public SettingsConfiguration DotEnv(string path, bool optional = false);
    public SettingsConfiguration InMemory(IReadOnlyDictionary<string, string> values);
}
public sealed class EnvAttribute : Attribute
{
    public EnvAttribute(string name);
    public string Name { get; }
}
public sealed class EnvPrefixAttribute : Attribute
{
    public EnvPrefixAttribute(string prefix);
    public string Prefix { get; }
}
public sealed class SecretAttribute : Attribute;
public enum SettingsErrorCode { Source, Model, Conversion, Validation }
public sealed record SettingsError(
    string PropertyPath, string? Key, string Source,
    SettingsErrorCode Code, string Message, bool IsSecret = false);
public sealed class SettingsException : Exception
{
    public IReadOnlyList<SettingsError> Errors { get; }
}
```

| API | Контракт |
| --- | --- |
| `Settings.Load<T>` | Новый независимый объект при каждом вызове; без global cache. `Register` в этой цепочке не используется |
| `SettingsManager.Create` | Требует хотя бы одну регистрацию; источники читаются один раз для всех групп; загрузка eager |
| `Get<T>` | Только зарегистрированный тип, тот же экземпляр при каждом вызове; безопасны параллельные чтения менеджера |
| `ReadFrom` | Добавление источников от меньшего приоритета к большему; каждый метод возвращает основной builder |
| `Override` | Точный ключ, без преобразования имён; всегда побеждает источники; повторный вызов заменяет значение |
| `Register<T>` | Регистрация типа для менеджера; повторная регистрация того же типа — ошибка |
| `Env` | Только свойство; точный ключ, обходящий все префиксы; атрибут наследуется при переопределении |
| `EnvPrefix` | Только класс, без наследования; литеральный префикс, включая `_` |
| `Secret` | Свойство; пометка чувствительности для ошибок; не влияет на значение |
| `SettingsError` | Структурированная безопасная ошибка; значения никогда не сохраняются |
| `SettingsException.Errors` | Неизменяемый список всех собранных ошибок текущей загрузки |

Builder используется синхронно только во время callback. Изменения захваченного builder после `Load/Create` не изменяют загруженные объекты. Регистрировать типы нужно для менеджера; standalone `Load<T>` загружает только `T`. `Dispose`, глобального Configure, reload и событий изменений нет.

Требования к классам, точные форматы и источники описаны в [конфигурации](CONFIGURATION.md). Правила валидации и ошибки — в [валидации](VALIDATION.md).
