# Подготовка и публикация

[Русский](PUBLISHING.md) · [English](en/PUBLISHING.md) · [Deutsch](de/PUBLISHING.md)

[Оглавление](README.md)

## Текущее состояние

Готовится первый релиз `1.0.0`. Версия исходников — `1.0.0-dev`. Тег `v1.0.0` создаётся и отправляется **только после отдельного разрешения владельца**. Настройка workflow и локальный pack не являются выпуском.

В этом репозитории применяется соглашение Trexon.Logging: релизные теги `vMAJOR.MINOR.PATCH[-PRERELEASE]`, например `v1.0.0`. API в рамках 1.x сохраняет совместимость; несовместимое изменение требует major.

## Перед разрешённым выпуском

1. Все файлы закоммичены в main и отправлены в origin; рабочая копия чистая.
2. CI на актуальном main завершён успешно на Linux, Windows, macOS. Команды локальной проверки приведены в [разработке](DEVELOPMENT.md).
3. Проверены примеры документации и содержимое NuGet-пакета, примеры не содержат настоящих секретов.
4. На GitHub создан environment `nuget`, установлена repository variable `NUGET_USER` с именем профиля nuget.org.
5. На nuget.org настроена Trusted Publishing policy: owner `Trexoncorp`, repository `Trexon.Configuration`, workflow file `publish.yml`, environment `nuget`. Для первого пакета проверьте доступность PackageId и права владельца. Конфигурация Logging не распространяется на новый репозиторий автоматически.

Trusted Publishing использует OIDC и краткоживущий API key. Постоянный NuGet secret в workflow не нужен. Настройка учётной записи не подтверждается самим наличием YAML. См. [официальную инструкцию NuGet](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing).

## Выпуск после разрешения

Следующие команды — инструкция для будущего разрешённого выпуска; проверка документации их не запускает:

```sh
bash scripts/release-version.sh v1.0.0
git tag -a v1.0.0 -m "Trexon.Configuration 1.0.0"
git push origin v1.0.0
```

`release-version.sh` проверяет SemVer: только префикс `v`, без build metadata и ведущих нулей в числовых идентификаторах. Тег должен указывать на commit, достижимый из `origin/main`. Не перемещайте опубликованные теги.

`publish.yml` по push `v*` повторно проверяет тег, docs, build и тесты, собирает пакет с версией из тега и сохраняет nupkg/snupkg на 30 дней. Отдельный job в environment `nuget` получает OIDC key и отправляет пакет и symbols. При неверной настройке NUGET_USER или policy публикация завершится ошибкой; тег сам по себе не подтверждает успешный NuGet release.

После запуска проверьте оба jobs, наличие версии `1.0.0` на nuget.org и установку в чистом тестовом приложении. После успеха обновите статус релиза в README на всех языках. GitHub Release с release notes — отдельная операция; текущий workflow его не создаёт.
