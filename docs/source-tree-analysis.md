# TimeDoctorAlert — Анализ дерева исходников

## Структура каталогов

```
TimeDoctorAlert/                              # Корень репозитория
├── TimeDoctorAlert.sln                       # Solution-файл
├── .gitignore                                # Правила игнорирования для Git
├── docs/                                     # Документация проекта
└── TimeDoctorAlert/                          # Основной проект
    ├── TimeDoctorAlert.csproj                # Файл проекта (.NET 8+, Console App)
    ├── Program.cs                            # ★ Точка входа (top-level statements)
    ├── WindowMonitorService.cs               # ★ Сервис мониторинга окон
    ├── Resources.cs                          # ★ Доступ к embedded-ресурсам
    ├── Platform/                             # Платформенная абстракция
    │   ├── IWindowEnumerator.cs              # Интерфейс перечисления окон
    │   ├── IAudioPlayer.cs                   # Интерфейс воспроизведения звука
    │   ├── ITrayIcon.cs                      # Интерфейс иконки трея
    │   ├── PlatformFactory.cs                # ★ Фабрика платформенных реализаций
    │   ├── WindowInfo.cs                     # Модель данных окна
    │   ├── WindowTracker.cs                  # ★ Отслеживание изменений окон
    │   ├── Windows/                          # Реализации для Windows
    │   │   ├── WindowsWindowEnumerator.cs    # Win32 EnumWindows P/Invoke
    │   │   ├── WindowsAudioPlayer.cs         # NAudio MP3-воспроизведение
    │   │   └── WindowsTrayIcon.cs            # WinForms NotifyIcon
    │   ├── Mac/                              # Реализации для macOS
    │   │   ├── MacWindowEnumerator.cs        # CoreGraphics CGWindowListCopyWindowInfo
    │   │   ├── MacAudioPlayer.cs             # afplay (системная утилита)
    │   │   └── MacTrayIcon.cs                # No-op заглушка
    │   └── Linux/                            # Реализации для Linux
    │       ├── LinuxWindowEnumerator.cs      # wmctrl (парсинг stdout)
    │       ├── LinuxAudioPlayer.cs           # ffplay / mpg123
    │       └── LinuxTrayIcon.cs              # No-op заглушка
    └── Resources/                            # Встроенные ресурсы (EmbeddedResource)
        ├── imperskij-marsh-8bit.mp3          # Звуковой сигнал оповещения
        └── logo.ico                          # Иконка приложения (для трея)
```

## Критические папки и файлы

### Точка входа

- **Program.cs** — точка входа приложения (top-level statements). Инициализация Serilog, создание `PlatformFactory`, запуск `WindowMonitorService`.

### Ключевые исходные файлы

| Файл | Назначение |
|---|---|
| `Program.cs` | Точка входа: инициализация логирования, создание сервисов, запуск |
| `WindowMonitorService.cs` | Бизнес-логика: polling-цикл, обнаружение окон TD, запуск звука |
| `Resources.cs` | Загрузка embedded-ресурсов через `Assembly.GetManifestResourceStream` |
| `Platform/PlatformFactory.cs` | Фабрика: выбор реализаций по платформе (`#if WINDOWS/MACOS/LINUX`) |
| `Platform/WindowTracker.cs` | Отслеживание списка окон: фильтрация, сравнение, логирование |
| `Platform/WindowInfo.cs` | Модель данных: Handle, Title, ProcessName, Rect, ClassName, IsForeground |

### Интерфейсы платформенной абстракции

| Файл | Назначение |
|---|---|
| `Platform/IWindowEnumerator.cs` | Контракт получения списка окон |
| `Platform/IAudioPlayer.cs` | Контракт воспроизведения звука |
| `Platform/ITrayIcon.cs` | Контракт иконки системного трея |

### Платформенные реализации

| Платформа | Окна | Аудио | Трей |
|---|---|---|---|
| Windows | `WindowsWindowEnumerator.cs` — Win32 P/Invoke | `WindowsAudioPlayer.cs` — NAudio | `WindowsTrayIcon.cs` — NotifyIcon |
| macOS | `MacWindowEnumerator.cs` — CoreGraphics | `MacAudioPlayer.cs` — afplay | `MacTrayIcon.cs` — No-op |
| Linux | `LinuxWindowEnumerator.cs` — wmctrl | `LinuxAudioPlayer.cs` — ffplay/mpg123 | `LinuxTrayIcon.cs` — No-op |

### Ресурсы

| Файл | Назначение |
|---|---|
| `Resources/logo.ico` | Иконка в системном трее (Windows) |
| `Resources/imperskij-marsh-8bit.mp3` | Звуковой сигнал при обнаружении окна Time Doctor |

### Конфигурация

| Параметр | Значение | Хранение |
|---|---|---|
| Seq URL | `http://seq.n2home.keenetic.link` | Embedded resource (через `Resources.cs`) |
| Фильтр окон | "Time Doctor" / "timedoctor2", >550x100px | `WindowMonitorService.cs` |
| Интервал polling | 500мс | `WindowMonitorService.cs` |
| Таймаут оповещения | 1 минута | `WindowMonitorService.cs` |
