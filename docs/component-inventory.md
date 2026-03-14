# TimeDoctorAlert — Инвентарь компонентов

## Обзор

Проект построен на кроссплатформенной архитектуре с платформенной абстракцией через интерфейсы. Бизнес-логика отделена от платформенных реализаций.

## Точка входа

### Program.cs

**Файл:** `TimeDoctorAlert/Program.cs`
**Тип:** Top-level statements (точка входа .NET 8+)

**Ответственность:**
- Инициализация Serilog (Console + Seq)
- Создание `PlatformFactory`
- Создание и запуск `WindowMonitorService`
- Обработка завершения приложения (Ctrl+C)

---

## Сервисы

### WindowMonitorService

**Файл:** `TimeDoctorAlert/WindowMonitorService.cs`
**Тип:** Класс сервиса мониторинга

**Ответственность:**
- Основной polling-цикл (каждые 500мс)
- Запуск звукового оповещения при обнаружении нового окна TD
- Управление жизненным циклом воспроизведения (таймаут 1 мин)

**Методы:**
| Метод | Доступ | Возврат | Назначение |
|---|---|---|---|
| `RunAsync(CancellationToken)` | public async | Task | Основной polling-цикл |
| `CheckActivityAndPlaySound(CancellationToken)` | private async | Task | Запуск звука и ожидание закрытия окна |

---

### WindowTracker

**Файл:** `TimeDoctorAlert/Platform/WindowTracker.cs`
**Тип:** Класс отслеживания окон

**Ответственность:**
- Получение списка окон через `IWindowEnumerator`
- Применение фильтра
- Логирование открытия/закрытия/изменения окон
- Подсчёт отфильтрованных окон

**Методы:**
| Метод | Доступ | Возврат | Назначение |
|---|---|---|---|
| `Update(Func<WindowInfo, bool>)` | public | int | Обновление списка с фильтром, логирование изменений |

---

## Платформенная абстракция

### PlatformFactory

**Файл:** `TimeDoctorAlert/Platform/PlatformFactory.cs`
**Тип:** Фабрика платформенных реализаций

**Ответственность:**
- Создание платформенных реализаций через условную компиляцию (`#if WINDOWS/MACOS/LINUX`)

**Методы:**
| Метод | Доступ | Возврат | Назначение |
|---|---|---|---|
| `CreateWindowEnumerator()` | public static | `IWindowEnumerator` | Создание перечислителя окон |
| `CreateAudioPlayer()` | public static | `IAudioPlayer` | Создание аудиоплеера |
| `CreateTrayIcon()` | public static | `ITrayIcon` | Создание иконки трея |

---

### Интерфейсы

#### IWindowEnumerator

**Файл:** `TimeDoctorAlert/Platform/IWindowEnumerator.cs`

| Метод | Возврат | Назначение |
|---|---|---|
| `GetAllWindows()` | `List<WindowInfo>` | Получение списка всех видимых окон |

#### IAudioPlayer

**Файл:** `TimeDoctorAlert/Platform/IAudioPlayer.cs`

| Метод | Возврат | Назначение |
|---|---|---|
| `PlayLoopAsync(Stream, CancellationToken)` | `Task` | Воспроизведение MP3 в цикле до отмены |

#### ITrayIcon

**Файл:** `TimeDoctorAlert/Platform/ITrayIcon.cs`

| Метод | Возврат | Назначение |
|---|---|---|
| `Show()` | `void` | Показать иконку в трее |
| `Hide()` | `void` | Скрыть иконку |

---

### WindowInfo

**Файл:** `TimeDoctorAlert/Platform/WindowInfo.cs`
**Тип:** Модель данных окна

**Свойства:** Handle, Title, ProcessName, Rect (позиция/размер), ClassName, IsForeground

---

## Платформенные реализации

### Windows (`Platform/Windows/`)

| Класс | Файл | Реализует | Технология |
|---|---|---|---|
| `WindowsWindowEnumerator` | `WindowsWindowEnumerator.cs` | `IWindowEnumerator` | Win32 P/Invoke: EnumWindows, GetWindowText, GetWindowRect, GetWindowThreadProcessId, IsWindowVisible, GetClassName, GetForegroundWindow |
| `WindowsAudioPlayer` | `WindowsAudioPlayer.cs` | `IAudioPlayer` | NAudio: Mp3FileReader + WaveOutEvent, воспроизведение в цикле |
| `WindowsTrayIcon` | `WindowsTrayIcon.cs` | `ITrayIcon` | WinForms NotifyIcon |

### macOS (`Platform/Mac/`)

| Класс | Файл | Реализует | Технология |
|---|---|---|---|
| `MacWindowEnumerator` | `MacWindowEnumerator.cs` | `IWindowEnumerator` | CoreGraphics P/Invoke: CGWindowListCopyWindowInfo |
| `MacAudioPlayer` | `MacAudioPlayer.cs` | `IAudioPlayer` | Системная утилита `afplay` (Process.Start) |
| `MacTrayIcon` | `MacTrayIcon.cs` | `ITrayIcon` | No-op заглушка |

### Linux (`Platform/Linux/`)

| Класс | Файл | Реализует | Технология |
|---|---|---|---|
| `LinuxWindowEnumerator` | `LinuxWindowEnumerator.cs` | `IWindowEnumerator` | Утилита `wmctrl` (Process.Start, парсинг stdout) |
| `LinuxAudioPlayer` | `LinuxAudioPlayer.cs` | `IAudioPlayer` | Утилиты `ffplay` или `mpg123` (Process.Start) |
| `LinuxTrayIcon` | `LinuxTrayIcon.cs` | `ITrayIcon` | No-op заглушка |

---

## Ресурсы

### Resources.cs

**Файл:** `TimeDoctorAlert/Resources.cs`
**Тип:** Статический класс доступа к embedded-ресурсам

**Ответственность:**
- Загрузка embedded-ресурсов через `Assembly.GetManifestResourceStream`

### Файлы ресурсов

| Ресурс | Файл | Назначение |
|---|---|---|
| MP3-файл | `Resources/imperskij-marsh-8bit.mp3` | Звуковой сигнал оповещения (Имперский марш 8-bit) |
| Иконка | `Resources/logo.ico` | Иконка приложения (для трея на Windows) |

### Конфигурация

| Параметр | Значение | Хранение |
|---|---|---|
| Seq URL | `http://seq.n2home.keenetic.link` | Embedded resource |
