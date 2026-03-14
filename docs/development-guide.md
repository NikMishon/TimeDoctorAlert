# TimeDoctorAlert — Руководство разработчика

## Необходимые инструменты

### Общие

| Инструмент | Версия | Назначение |
|---|---|---|
| .NET SDK | 8.0+ | Сборка и запуск |
| IDE (опционально) | VS 2022 / Rider / VS Code | Разработка |

### Системные зависимости по платформам

#### Windows

Дополнительных зависимостей не требуется. Win32 API и NAudio работают из коробки.

#### macOS

| Зависимость | Установка | Назначение |
|---|---|---|
| `afplay` | Встроена в macOS | Воспроизведение MP3 |

Дополнительных установок не требуется — `afplay` входит в состав macOS.

#### Linux

| Зависимость | Установка | Назначение |
|---|---|---|
| `wmctrl` | `sudo apt install wmctrl` | Получение списка окон |
| `ffplay` или `mpg123` | `sudo apt install ffmpeg` или `sudo apt install mpg123` | Воспроизведение MP3 |

## Установка и настройка

### 1. Клонирование репозитория

```bash
git clone <repo-url>
cd TimeDoctorAlert
```

### 2. Восстановление пакетов

NuGet-пакеты восстанавливаются автоматически при сборке. Зависимости:

- **NAudio 2.2.1** — воспроизведение MP3 (только Windows)
- **Serilog 4.0** — структурированное логирование
- **Serilog.Sinks.Console** — вывод логов в консоль
- **Serilog.Sinks.Seq** — отправка логов на Seq-сервер

### 3. Конфигурация

URL Seq-сервера загружается из embedded-ресурса через `Resources.cs`:
```
Seq URL = http://seq.n2home.keenetic.link
```

Для изменения URL: отредактируйте соответствующий embedded resource и пересоберите проект.

## Сборка

### Через командную строку (все платформы)

```bash
# Debug
dotnet build TimeDoctorAlert/TimeDoctorAlert.csproj

# Release
dotnet build TimeDoctorAlert/TimeDoctorAlert.csproj -c Release
```

### Публикация для конкретной платформы

```bash
# Windows
dotnet publish -c Release -r win-x64

# macOS (Intel)
dotnet publish -c Release -r osx-x64

# macOS (Apple Silicon)
dotnet publish -c Release -r osx-arm64

# Linux
dotnet publish -c Release -r linux-x64
```

### Условная компиляция

Проект использует директивы условной компиляции для платформенного кода:

- `#if WINDOWS` — код для Windows (Win32 P/Invoke, NAudio, NotifyIcon)
- `#if MACOS` — код для macOS (CoreGraphics, afplay)
- `#if LINUX` — код для Linux (wmctrl, ffplay/mpg123)

Соответствующие символы определяются автоматически в `TimeDoctorAlert.csproj` на основе целевого RID.

## Запуск

### Через командную строку (все платформы)

```bash
dotnet run --project TimeDoctorAlert/TimeDoctorAlert.csproj
```

### Поведение при запуске

1. Приложение запускается как консольное приложение
2. Инициализируется Serilog (логи в консоль + Seq)
3. На Windows: в системном трее появляется иконка
4. Начинается фоновый мониторинг окон Time Doctor (каждые 500мс)
5. При обнаружении окна TD — воспроизведение MP3 в цикле
6. Завершение: Ctrl+C или закрытие консоли

## Структура кода

### Основные файлы для модификации

| Файл | Что менять |
|---|---|
| `Program.cs` | Инициализация приложения, настройка Serilog |
| `WindowMonitorService.cs` | Фильтр окон TD, интервал polling (500мс), таймаут оповещения (1 мин) |
| `Platform/WindowTracker.cs` | Логика отслеживания изменений окон |
| `Platform/PlatformFactory.cs` | Выбор платформенных реализаций |
| `Platform/Windows/*` | Windows-специфичная логика |
| `Platform/Mac/*` | macOS-специфичная логика |
| `Platform/Linux/*` | Linux-специфичная логика |
| `Resources.cs` | Доступ к embedded-ресурсам (MP3, иконка, Seq URL) |

### Архитектурные паттерны

- **Platform Abstraction** — интерфейсы IWindowEnumerator, IAudioPlayer, ITrayIcon
- **Factory** — PlatformFactory для создания платформенных реализаций
- **Conditional Compilation** — `#if WINDOWS/MACOS/LINUX`
- **Async/await** — все длительные операции асинхронны
- **CancellationToken** — корректная отмена операций
- **Embedded Resources** — звуковой файл и иконка встроены в сборку через `Assembly.GetManifestResourceStream`
- **Top-level statements** — точка входа в `Program.cs`

## Тестирование

Тесты в проекте отсутствуют.

Для добавления тестов рекомендуется:
1. Создать отдельный проект `TimeDoctorAlert.Tests`
2. Использовать xUnit или NUnit
3. Мокировать `IWindowEnumerator` и `IAudioPlayer` для тестирования логики мониторинга

## Типичные задачи

### Изменение фильтра окон

Фильтр находится в `WindowMonitorService.cs`:
```csharp
Func<WindowInfo, bool> filter = w =>
    (w.ProcessName == "Time Doctor" ||
     w.ProcessName == "timedoctor2") &&
    w.Rect.Right - w.Rect.Left > 550 &&
    w.Rect.Bottom - w.Rect.Top > 100;
```

### Замена звукового файла

1. Замените файл `TimeDoctorAlert/Resources/imperskij-marsh-8bit.mp3`
2. Убедитесь, что файл указан как `EmbeddedResource` в `TimeDoctorAlert.csproj`
3. При необходимости обновите имя в `Resources.cs`
4. Пересоберите проект

### Изменение интервала мониторинга

В `WindowMonitorService.cs`:
```csharp
await Task.Delay(500, cancellationToken); // Изменить 500 на нужное значение
```

### Добавление новой платформы

1. Создайте папку `Platform/НоваяПлатформа/`
2. Реализуйте `IWindowEnumerator`, `IAudioPlayer`, `ITrayIcon`
3. Добавьте ветку `#if` в `PlatformFactory.cs`
4. Добавьте символ условной компиляции в `TimeDoctorAlert.csproj`
