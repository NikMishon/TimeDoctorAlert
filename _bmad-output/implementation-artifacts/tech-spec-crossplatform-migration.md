---
title: 'Кроссплатформенная миграция TimeDoctorAlert'
slug: 'crossplatform-migration'
created: '2026-03-14'
status: 'implementation-complete'
stepsCompleted: [1, 2, 3, 4]
tech_stack: ['.NET 8+', 'C#', 'Serilog', 'Serilog.Sinks.Seq']
files_to_modify: ['TimeDoctorAlert/TimeDoctorAlert.csproj', 'TimeDoctorAlert/ApiWrapper.cs', 'TimeDoctorAlert/MainWindow.xaml.cs', 'TimeDoctorAlert/App.xaml.cs', 'TimeDoctorAlert/MainWindow.xaml', 'TimeDoctorAlert/AssemblyInfo.cs', 'TimeDoctorAlert/Properties/Resources.Designer.cs', 'TimeDoctorAlert/Properties/Resources.resx', 'docs/project-overview.md', 'docs/architecture.md', 'docs/component-inventory.md', 'docs/development-guide.md', 'docs/source-tree-analysis.md']
files_to_create: ['TimeDoctorAlert/Platform/WindowInfo.cs', 'TimeDoctorAlert/Platform/IWindowEnumerator.cs', 'TimeDoctorAlert/Platform/ITrayIcon.cs', 'TimeDoctorAlert/Platform/IAudioPlayer.cs', 'TimeDoctorAlert/Platform/WindowTracker.cs', 'TimeDoctorAlert/Platform/Windows/WindowsWindowEnumerator.cs', 'TimeDoctorAlert/Platform/Windows/WindowsTrayIcon.cs', 'TimeDoctorAlert/Platform/Windows/WindowsAudioPlayer.cs', 'TimeDoctorAlert/Platform/Mac/MacWindowEnumerator.cs', 'TimeDoctorAlert/Platform/Mac/MacTrayIcon.cs', 'TimeDoctorAlert/Platform/Mac/MacAudioPlayer.cs', 'TimeDoctorAlert/Platform/Linux/LinuxWindowEnumerator.cs', 'TimeDoctorAlert/Platform/Linux/LinuxTrayIcon.cs', 'TimeDoctorAlert/Platform/Linux/LinuxAudioPlayer.cs', 'TimeDoctorAlert/Platform/PlatformFactory.cs', 'TimeDoctorAlert/WindowMonitorService.cs', 'TimeDoctorAlert/Resources.cs', 'TimeDoctorAlert/Program.cs']
code_patterns: ['polling-цикл 500мс', 'CancellationToken для остановки', 'фильтрация окон по ProcessName и размеру', 'headless console app с platform-specific tray', 'интерфейсы для абстракции платформы']
test_patterns: ['тесты отсутствуют']
---

# Tech-Spec: Кроссплатформенная миграция TimeDoctorAlert

**Created:** 2026-03-14

## Overview

### Problem Statement

TimeDoctorAlert работает только на Windows из-за привязки к .NET Framework 4.8, WPF, Win32 API (user32.dll P/Invoke), WinForms NotifyIcon и NAudio WaveOutEvent. Необходимо сделать приложение кроссплатформенным (Windows, macOS, Linux).

### Solution

Мигрировать проект на .NET 8+ и абстрагировать платформо-зависимый код за интерфейсы с отдельными реализациями для каждой платформы. Заменить Windows-only компоненты (WPF, WinForms, NAudio) на кроссплатформенные аналоги где возможно, и на платформо-специфичные реализации где необходимо.

### Scope

**In Scope:**
- Миграция с .NET Framework 4.8 на .NET 8+
- Абстракция платформо-зависимого кода за интерфейсы (получение списка окон, системный трей, воспроизведение аудио)
- Реализации для Windows (Win32 API), macOS (CGWindowList), Linux (wmctrl)
- Замена WPF/WinForms на headless-архитектуру с платформенным треем
- Кроссплатформенное аудио-воспроизведение
- Сохранение существующей бизнес-логики (polling 500мс, фильтрация по процессу и размеру, таймаут 1 мин)
- Обновление проектной документации (docs/) для отражения новой архитектуры

**Out of Scope:**
- Новая функциональность (новые фильтры, настраиваемые таймауты и т.д.)
- Изменение бизнес-логики мониторинга
- Написание тестов

## Context for Development

### Codebase Patterns

- Приложение headless — WPF `MainWindow.xaml` пустой, окно скрывается сразу. Используется только как хост для WinForms NotifyIcon
- Polling-цикл каждые 500мс через `Task.Run` + `async/await`
- `CancellationToken` для остановки мониторинга и воспроизведения звука
- Фильтрация окон по `ProcessName` ("Time Doctor", "timedoctor2") и размеру (>550x100 px)
- Звук из embedded ресурса (MP3 byte[]) через NAudio `Mp3FileReader` + `WaveOutEvent`
- Логирование: Serilog → Seq (`seq.n2home.keenetic.link`)
- Неиспользуемый код: `PlaySirenAsync`, `BeepAsync`, `NoteSequence`, `GetIdleTime` — удалить при миграции

### Files to Reference

| File | Purpose | Действие |
| ---- | ------- | -------- |
| TimeDoctorAlert/MainWindow.xaml.cs | Бизнес-логика (мониторинг, аудио, трей) ~270 строк | Разделить на WindowMonitorService + платформенные реализации |
| TimeDoctorAlert/ApiWrapper.cs | Win32 P/Invoke перечисление окон ~175 строк | Переместить в Platform/Windows/WindowsWindowEnumerator |
| TimeDoctorAlert/TimeDoctorAlert.csproj | .NET Framework 4.8 + WPF | Мигрировать на .NET 8+ console app |
| TimeDoctorAlert/MainWindow.xaml | Пустое WPF окно | Удалить |
| TimeDoctorAlert/App.xaml / App.xaml.cs | WPF Application entry point | Заменить на Program.cs |
| TimeDoctorAlert/AssemblyInfo.cs | WPF ThemeInfo | Удалить |
| TimeDoctorAlert/Properties/Resources.resx | Иконка (Icon), MP3 (byte[]), SeqUrl (string) | Мигрировать на embedded resources .NET 8+ |
| docs/*.md | Проектная документация | Обновить для отражения новой архитектуры |

### Technical Decisions

- **Убрать WPF полностью** — приложение становится console app
- **Интерфейс `IWindowEnumerator`** содержит только `List<WindowInfo> GetVisibleWindows()` — получение списка окон платформо-специфично. Общая логика отслеживания (детекция новых/закрытых окон, логирование) вынесена в класс `WindowTracker`, который используется в `WindowMonitorService`.
- **`WindowInfo` содержит `string Id`** — платформо-агностичный уникальный идентификатор окна (каждая платформа маппит свой нативный handle в строку). Нужен для детекции новых/закрытых окон.
- **`IAudioPlayer : IDisposable`** — реализации на macOS/Linux используют temp-файлы и внешние процессы, `Dispose()` чистит ресурсы.
- **Фильтр окон** адаптирован к новой модели: `w.Width > 550 && w.Height > 100`. ProcessName различается по платформам: на Windows — имя процесса ("Time Doctor", "timedoctor2"), на macOS — `kCGWindowOwnerName` ("Time Doctor"), на Linux — имя бинарника из `/proc/{pid}/comm`. Фильтр: `(w.ProcessName.Contains("time doctor", OrdinalIgnoreCase) || w.ProcessName.Contains("timedoctor", OrdinalIgnoreCase))` — покрывает оба варианта (с пробелом и без).
- **`PlayAsync` зацикливает MP3** до отмены через CancellationToken. **Это изменение поведения**: оригинал проигрывает MP3 один раз и замолкает. Зацикливание добавлено намеренно — MP3 короче минуты, без зацикливания большую часть времени будет тишина. `Stop()` убран из интерфейса — остановка только через CancellationToken (как в оригинале).
- **Threading на Windows**: main thread запускает `Application.Run(ApplicationContext)` для message pump (нужен NotifyIcon), `WindowMonitorService.RunAsync()` работает в background thread.
- **PlatformFactory** — выбор реализаций по `RuntimeInformation.IsOSPlatform`
- **Windows**: Win32 P/Invoke (существующий код ApiWrapper), NotifyIcon, NAudio
- **macOS**: `CGWindowListCopyWindowInfo` через P/Invoke CoreGraphics + CoreFoundation (маршаллинг CFArray/CFDictionary/CFString). Для аудио — `afplay`.
- **Linux**: `wmctrl -l -G -p` (парсинг stdout, geometry включена через флаг `-G`). Для аудио — `ffplay -nodisp -autoexit` (primary), `mpg123` (fallback).
- **Трей macOS/Linux**: no-op заглушка, логирует что трей не поддерживается. Приложение продолжает работать в консоли.
- **Seq URL**: `http://seq.n2home.keenetic.link` — хардкод константой в `Program.cs` (аналогично текущему подходу через Resources.resx).
- **MP3-ресурс**: embedded resource через .csproj `<EmbeddedResource>`
- **Serilog + Seq**: остаются как есть (кроссплатформенные)
- Неиспользуемый код (`PlaySirenAsync`, `BeepAsync`, `NoteSequence`, `GetIdleTime`) удаляется

## Implementation Plan

### Порядок задач

Задачи упорядочены так, чтобы проект оставался в компилируемом состоянии как можно дольше. Сначала создаются новые файлы (интерфейсы, реализации, сервисы), затем переключается .csproj, затем удаляются старые артефакты.

### Tasks

- [ ] Task 1: Создать модель WindowInfo
  - File: `TimeDoctorAlert/Platform/WindowInfo.cs`
  - Action: Создать кроссплатформенный класс с полями: `string Id` (уникальный идентификатор окна, маппится из нативного handle каждой платформой), `string Title`, `string ProcessName`, `int Width`, `int Height`, `bool IsForeground`.

- [ ] Task 2: Создать интерфейс IWindowEnumerator
  - File: `TimeDoctorAlert/Platform/IWindowEnumerator.cs`
  - Action: Создать интерфейс с единственным методом: `List<WindowInfo> GetVisibleWindows()`. Только получение списка окон — платформо-специфичная часть.

- [ ] Task 3: Создать WindowTracker — общая логика отслеживания окон
  - File: `TimeDoctorAlert/Platform/WindowTracker.cs`
  - Action: Класс принимает `IWindowEnumerator` через конструктор. Метод `int UpdateWindowList(Func<WindowInfo, bool> filter)` — перенести логику из `ApiWrapper.UpdateWindowList`: фильтрация, детекция новых/закрытых/изменённых окон по `Id`, логирование через Serilog. Хранит `_windows` список. Возвращает количество отфильтрованных окон (аналогично оригиналу). `WindowTracker` — единственный владелец состояния `_windows` и `Count`. `WindowMonitorService` использует только возвращаемое значение `UpdateWindowList` для сравнения с предыдущим вызовом (хранит `_previousCount`). Не дублировать список окон в сервисе.
  - Notes: Сравнение окон по `w.Id` вместо `w.Handle`. Убрать `Console.WriteLine` из оригинала — заменить на Serilog (Console sink уже добавлен).

- [ ] Task 4: Создать интерфейс IAudioPlayer
  - File: `TimeDoctorAlert/Platform/IAudioPlayer.cs`
  - Action: Создать интерфейс наследующий `IDisposable`: `Task PlayAsync(CancellationToken ct)` (зацикливает воспроизведение до отмены через CT, метод возвращает Task который завершается при отмене). Аудио-файл загружается из embedded ресурса внутри реализации. `Dispose()` чистит temp-файлы и убивает процессы. Отдельный `Stop()` не нужен — остановка только через CancellationToken.

- [ ] Task 5: Создать интерфейс ITrayIcon
  - File: `TimeDoctorAlert/Platform/ITrayIcon.cs`
  - Action: Создать интерфейс наследующий `IDisposable`: `void Show()`, `void Hide()`, `event Action? OnExitClicked`. Убрать `OnDoubleClick` — в console app нет окна для показа.

- [ ] Task 6: Извлечь бизнес-логику в WindowMonitorService
  - File: `TimeDoctorAlert/WindowMonitorService.cs`
  - Action: Класс принимает `WindowTracker`, `IAudioPlayer`, фильтр `Func<WindowInfo, bool>` через конструктор. Метод `Task RunAsync(CancellationToken ct)` — основной polling-цикл (перенести логику `MonitorWindow` из `MainWindow.xaml.cs`). Метод `CheckActivityAndPlaySound` — перенести из `MainWindow.xaml.cs`. Фильтр адаптировать: `w.Width > 550 && w.Height > 100 && (w.ProcessName.Contains("time doctor", StringComparison.OrdinalIgnoreCase) || w.ProcessName.Contains("timedoctor", StringComparison.OrdinalIgnoreCase))`. Убрать неиспользуемый код (`PlaySirenAsync`, `BeepAsync`, `NoteSequence`). Добавить `await Task.Delay(200, ct)` во внутренний цикл `CheckActivityAndPlaySound` (в оригинале — busy-wait без задержки). 200мс — компромисс между отзывчивостью и нагрузкой (оригинал: 0мс busy-wait, внешний цикл: 500мс).
  - Notes: Polling 500мс, таймаут 1 мин — сохраняются.

- [ ] Task 7: Windows-реализация IWindowEnumerator
  - File: `TimeDoctorAlert/Platform/Windows/WindowsWindowEnumerator.cs`
  - Action: Перенести из `ApiWrapper.cs` все P/Invoke (`EnumWindows`, `GetWindowText`, `GetWindowRect`, `GetWindowThreadProcessId`, `GetClassName`, `IsWindowVisible`, `GetForegroundWindow`) и метод `GetAllWindows()`. Адаптировать возврат к `WindowInfo`: `Id = hWnd.ToString()`, `Width = rect.Right - rect.Left`, `Height = rect.Bottom - rect.Top`. Реализовать `IWindowEnumerator.GetVisibleWindows()`.
  - Notes: `RECT`, делегат `EnumWindowsProc` — внутренние типы класса. `LASTINPUTINFO` и `GetIdleTime` не переносить.

- [ ] Task 8: Windows-реализация IAudioPlayer
  - File: `TimeDoctorAlert/Platform/Windows/WindowsAudioPlayer.cs`
  - Action: Реализовать `IAudioPlayer`. В `PlayAsync` — загрузить MP3 из embedded resource через `Resources.GetMp3Stream()`, создать `Mp3FileReader` + `WaveOutEvent`, зацикливать воспроизведение (при окончании трека — seek на начало и играть снова) пока не отменён CancellationToken. При отмене CT — остановить `WaveOutEvent`. `Dispose()` — освободить NAudio ресурсы.

- [ ] Task 9: Windows-реализация ITrayIcon
  - File: `TimeDoctorAlert/Platform/Windows/WindowsTrayIcon.cs`
  - Action: Реализовать `ITrayIcon`. Использовать WinForms `NotifyIcon`. Иконку загружать из embedded resource через `new Icon(Resources.GetIconStream())`. Добавить контекстное меню с пунктом "Exit" → вызывает `OnExitClicked`. `Show()` — `_trayIcon.Visible = true`. `Hide()` — `_trayIcon.Visible = false`. `Dispose()` — `_trayIcon.Dispose()`.
  - Notes: Требует `System.Windows.Forms` и `System.Drawing.Common` (только Windows). В `Program.cs` на Windows main thread запускает `Application.Run(new ApplicationContext())`, а `WindowMonitorService.RunAsync()` работает в `Task.Run`. При `OnExitClicked` — `Application.ExitThread()` + cancel token.

- [ ] Task 10: macOS-реализация IWindowEnumerator
  - File: `TimeDoctorAlert/Platform/Mac/MacWindowEnumerator.cs`
  - Action: Реализовать через P/Invoke к CoreGraphics/CoreFoundation. Вызвать `CGWindowListCopyWindowInfo(kCGWindowListOptionOnScreenOnly | kCGWindowListExcludeDesktopElements, kCGNullWindowID)`. Итерировать по CFArray, для каждого CFDictionary извлечь: `kCGWindowOwnerName` → `ProcessName`, `kCGWindowName` → `Title`, `kCGWindowBounds` → dict с x/y/Width/Height, `kCGWindowNumber` → `Id` (преобразовать в string), `kCGWindowIsOnscreen` → видимость. Необходимые P/Invoke сигнатуры:
    ```
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    static extern IntPtr CGWindowListCopyWindowInfo(int option, uint relativeToWindow);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    static extern int CFArrayGetCount(IntPtr theArray);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    static extern IntPtr CFArrayGetValueAtIndex(IntPtr theArray, int idx);

    [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
    static extern IntPtr CFDictionaryGetValue(IntPtr theDict, IntPtr key);

    // CGRectMakeWithDictionaryRepresentation — в CoreGraphics, НЕ CoreFoundation:
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    static extern bool CGRectMakeWithDictionaryRepresentation(IntPtr dict, out CGRect rect);

    // + CFStringCreateWithCString, CFStringGetCString, CFRelease, CFNumberGetValue
    // IsForeground: определить через NSWorkspace.frontmostApplication (P/Invoke к AppKit)
    // или через kCGWindowLayer == 0 + kCGWindowOwnerPID == frontmost PID. Если слишком сложно — IsForeground = false на macOS (не критично для фильтрации).
    ```
  - Notes: `kCGWindowOwnerName` возвращает имя приложения (напр. "Time Doctor"), не имя бинарника. Фильтр `Contains("time doctor") || Contains("timedoctor")` покрывает оба варианта. Обязательно вызывать `CFRelease` на возвращённый CFArray. На macOS может потребоваться разрешение Screen Recording в System Preferences.

- [ ] Task 11: macOS-реализация IAudioPlayer
  - File: `TimeDoctorAlert/Platform/Mac/MacAudioPlayer.cs`
  - Action: Реализовать `IAudioPlayer`. При первом `PlayAsync` — извлечь MP3 из embedded resource во временный файл (`Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3")`). Зацикливание: в цикле запускать `Process.Start("afplay", tempFilePath)`, дожидаться завершения, повторять пока не отменён CT. `Stop()` — убить процесс `afplay` через `Process.Kill()`. `Dispose()` — убить процесс + удалить temp-файл.
  - Notes: `afplay` встроен в macOS, поддерживает MP3.

- [ ] Task 12: macOS-реализация ITrayIcon (заглушка)
  - File: `TimeDoctorAlert/Platform/Mac/MacTrayIcon.cs`
  - Action: No-op реализация. `Show()`/`Hide()` логируют `Log.Information("Tray icon not supported on macOS")`. `OnExitClicked` не вызывается. `Dispose()` — пусто.

- [ ] Task 13: Linux-реализация IWindowEnumerator
  - File: `TimeDoctorAlert/Platform/Linux/LinuxWindowEnumerator.cs`
  - Action: Реализовать через вызов `Process.Start("wmctrl", "-l -G -p")` и парсинг stdout. Формат строки: `0x{wid}  {desktop}  {pid}  {x}  {y}  {width}  {height}  {hostname}  {title...}`. Парсинг: split по whitespace первые 8 полей, всё остальное — title (может содержать пробелы!). `Id = wid`, `Width/Height` из stdout, `ProcessName` через `File.ReadAllText($"/proc/{pid}/comm").Trim()` (обрезается до 15 символов ОС — учитывать в фильтре), `Title` = remainder. `IsForeground` — сравнить с `xdotool getactivewindow` (один вызов за итерацию). При запуске проверить наличие `wmctrl` через `which wmctrl` — если не найден, бросить `InvalidOperationException("wmctrl is required but not found. Install with: sudo apt install wmctrl")`.
  - Notes: Требует `wmctrl` (`sudo apt install wmctrl`). `xdotool` нужен только для foreground (одна команда). Ограничение: только X11, не Wayland.

- [ ] Task 14: Linux-реализация IAudioPlayer
  - File: `TimeDoctorAlert/Platform/Linux/LinuxAudioPlayer.cs`
  - Action: Реализовать `IAudioPlayer`. MP3 из embedded resource во temp-файл (`Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}.mp3")`). Зацикливание: в цикле запускать аудио-процесс, дожидаться, повторять. Порядок попыток команд: `ffplay -nodisp -autoexit {file}`, затем `mpg123 {file}`. При старте проверить доступность через `which ffplay` / `which mpg123` — если ни один не найден, бросить `InvalidOperationException("ffplay or mpg123 is required. Install with: sudo apt install ffmpeg or sudo apt install mpg123")`. `Dispose()` — kill процесс + удалить temp-файл.
  - Notes: `ffplay` (часть FFmpeg) — наиболее распространён. `mpg123` — легковесная альтернатива. Документировать оба как зависимости.

- [ ] Task 15: Linux-реализация ITrayIcon (заглушка)
  - File: `TimeDoctorAlert/Platform/Linux/LinuxTrayIcon.cs`
  - Action: No-op реализация аналогично macOS.

- [ ] Task 16: Создать PlatformFactory
  - File: `TimeDoctorAlert/Platform/PlatformFactory.cs`
  - Action: Статический класс с методами `CreateWindowEnumerator()`, `CreateAudioPlayer()`, `CreateTrayIcon()`. Внутри — `RuntimeInformation.IsOSPlatform(OSPlatform.Windows/OSX/Linux)` для выбора реализации. При неизвестной платформе — `PlatformNotSupportedException`.

- [ ] Task 17: Создать Resources.cs — хелпер embedded ресурсов
  - File: `TimeDoctorAlert/Resources.cs`
  - Action: Статический класс с методами `Stream GetMp3Stream()`, `Stream GetIconStream()` — обёртки над `Assembly.GetManifestResourceStream()`. Бросать `InvalidOperationException` если ресурс не найден. Имя manifest resource в SDK-style проекте формируется как `{DefaultNamespace}.{RelativePath.Replace('/','.').Replace('\\','.')}`. Например, если MP3 лежит в `Resources/imperskij-marsh-8bit.mp3`, manifest name = `TimeDoctorAlert.Resources.imperskij_marsh_8bit.mp3`. Можно задать явно через `<EmbeddedResource Include="..." LogicalName="TimeDoctorAlert.alert.mp3" />` в .csproj для предсказуемости.

- [ ] Task 18: Создать Program.cs — точку входа
  - File: `TimeDoctorAlert/Program.cs`
  - Action: Точка входа приложения.
    ```
    static void Main()
    {
        // 1. Инициализация Serilog
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Seq("http://seq.n2home.keenetic.link")
            .WriteTo.Console()
            .CreateLogger();

        // 2. Создание сервисов через PlatformFactory
        var enumerator = PlatformFactory.CreateWindowEnumerator();
        var tracker = new WindowTracker(enumerator);
        using var audioPlayer = PlatformFactory.CreateAudioPlayer();
        using var trayIcon = PlatformFactory.CreateTrayIcon();

        var cts = new CancellationTokenSource();
        var filter = (WindowInfo w) =>
            w.ProcessName.Contains("timedoctor", StringComparison.OrdinalIgnoreCase) &&
            w.Width > 550 && w.Height > 100;

        var service = new WindowMonitorService(tracker, audioPlayer, filter);

        // 3. Graceful shutdown (Console.CancelKeyPress — primary на всех платформах,
        //    OnExitClicked — дополнительный на Windows через tray)
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };
        trayIcon.OnExitClicked += () => cts.Cancel();

        // 4. Запуск
        var monitorTask = Task.Run(() => service.RunAsync(cts.Token));
        trayIcon.Show();

        // 5. На Windows — Application.Run() для message pump, иначе — ждать задачу
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // trayIcon.RunMessageLoop() блокирует до вызова cts
            // При отмене — Application.ExitThread()
        }
        else
        {
            monitorTask.GetAwaiter().GetResult();
        }
    }
    ```
  - Notes: `Serilog.Sinks.Console` добавить для удобной отладки на всех платформах.

- [ ] Task 19: Мигрировать .csproj на .NET 8+ console app
  - File: `TimeDoctorAlert/TimeDoctorAlert.csproj`
  - Action: Изменить `TargetFramework` на `net8.0`, убрать `UseWPF`, изменить `OutputType` на `Exe`. Добавить embedded resources для MP3 и иконки. Условные зависимости:
    ```xml
    <!-- Все платформы -->
    <PackageReference Include="Serilog" Version="3.1.1" />
    <PackageReference Include="Serilog.Sinks.Seq" Version="6.0.0" />
    <PackageReference Include="Serilog.Sinks.Console" Version="..." />

    <!-- Только Windows -->
    <PropertyGroup Condition="$([MSBuild]::IsOSPlatform('Windows'))">
      <UseWindowsForms>true</UseWindowsForms>
    </PropertyGroup>
    <ItemGroup Condition="$([MSBuild]::IsOSPlatform('Windows'))">
      <PackageReference Include="NAudio" Version="2.2.1" />
      <PackageReference Include="System.Drawing.Common" Version="8.0.0" />
    </ItemGroup>
    ```
  - Notes: Убрать старые ссылки на `System.Net.Http`, `System.Windows.Forms` (из безусловных). Убрать ссылку на logo.png из Downloads.

- [ ] Task 20: Удалить WPF-артефакты и старые ресурсы
  - Files: `TimeDoctorAlert/MainWindow.xaml`, `TimeDoctorAlert/MainWindow.xaml.cs`, `TimeDoctorAlert/App.xaml`, `TimeDoctorAlert/App.xaml.cs`, `TimeDoctorAlert/AssemblyInfo.cs`, `TimeDoctorAlert/ApiWrapper.cs`, `TimeDoctorAlert/Properties/Resources.Designer.cs`, `TimeDoctorAlert/Properties/Resources.resx`
  - Action: Удалить файлы. Вся логика уже перенесена в новые файлы.

- [ ] Task 21: Обновить проектную документацию
  - Files: `docs/project-overview.md`, `docs/architecture.md`, `docs/component-inventory.md`, `docs/development-guide.md`, `docs/source-tree-analysis.md`
  - Action: Обновить каждый документ для отражения новой архитектуры:
    - **project-overview.md**: изменить "десктопная утилита для Windows" → "кроссплатформенная утилита (Windows, macOS, Linux)". Обновить тех. стек: .NET 8+, убрать WPF. Добавить платформенные зависимости.
    - **architecture.md**: заменить архитектурную диаграмму — вместо MainWindow+ApiWrapper описать Program.cs → PlatformFactory → IWindowEnumerator/IAudioPlayer/ITrayIcon → WindowTracker → WindowMonitorService. Обновить потоки данных, управление состоянием, конфигурацию (Seq URL). Убрать секцию "Неиспользуемый код".
    - **component-inventory.md**: убрать MainWindow и ApiWrapper. Добавить: WindowMonitorService, WindowTracker, PlatformFactory, IWindowEnumerator (+ 3 реализации), IAudioPlayer (+ 3 реализации), ITrayIcon (+ 3 реализации), Resources.
    - **development-guide.md**: обновить инструкции сборки для каждой платформы (`dotnet build`, `dotnet publish -r osx-arm64`, `dotnet publish -r linux-x64`). Добавить системные зависимости для macOS (Screen Recording permission) и Linux (wmctrl, ffplay/mpg123). Добавить секцию по добавлению новой платформы.
    - **source-tree-analysis.md**: обновить дерево файлов — новая структура `Platform/` с подпапками `Windows/`, `Mac/`, `Linux/`.
  - Notes: Документация должна быть актуальной после каждого PR. Язык документов — русский (совпадает с текущим).

### Acceptance Criteria

- [ ] AC 1: Given проект собран на Windows, when запущен рядом с Time Doctor, then обнаруживает окна TD и воспроизводит звук в цикле (существующее поведение сохранено)
- [ ] AC 2: Given проект собран на macOS, when запущен рядом с Time Doctor, then обнаруживает окна TD и воспроизводит звук через afplay
- [ ] AC 3: Given проект собран на Linux с установленным wmctrl и ffplay/mpg123, when запущен рядом с Time Doctor, then обнаруживает окна TD и воспроизводит звук
- [ ] AC 4: Given окно Time Doctor закрыто, when монитор обнаруживает закрытие, then звук останавливается (все платформы)
- [ ] AC 5: Given окно Time Doctor открыто более 1 минуты, when проходит таймаут, then звук останавливается (все платформы)
- [ ] AC 6: Given приложение запущено, when пользователь нажимает Ctrl+C или Exit в трее, then приложение завершается корректно, temp-файлы удалены
- [ ] AC 7: Given неподдерживаемая платформа, when попытка запуска, then выбрасывается PlatformNotSupportedException с понятным сообщением
- [ ] AC 8: Given платформа без поддержки трея (macOS/Linux), when приложение запущено, then работает без трея, логирует предупреждение
- [ ] AC 9: Given реализация завершена, when проверяется docs/, then документация соответствует актуальной архитектуре

## Additional Context

### Dependencies

**NuGet-пакеты (все платформы):**
- `Serilog` 3.1.1 — структурированное логирование
- `Serilog.Sinks.Seq` 6.0.0 — отправка логов в Seq
- `Serilog.Sinks.Console` — логирование в консоль (новая зависимость)

**NuGet-пакеты (только Windows):**
- `NAudio` 2.2.1 — воспроизведение MP3
- `System.Drawing.Common` — для `System.Drawing.Icon` (нужен на .NET 8+)

**Системные зависимости (macOS):**
- `afplay` — встроен в macOS, установка не требуется
- Разрешение Screen Recording в System Preferences (для CGWindowListCopyWindowInfo)

**Системные зависимости (Linux):**
- `wmctrl` — перечисление окон с geometry (`sudo apt install wmctrl`)
- `ffplay` (FFmpeg, primary) или `mpg123` (fallback) — воспроизведение MP3
- Ограничение: только X11, не Wayland

### Testing Strategy

Тесты вне скоупа данной спеки. Проверка вручную:

1. **Windows**: собрать и запустить, убедиться что существующее поведение сохранено (обнаружение окон TD, воспроизведение звука в цикле, остановка при закрытии, таймаут, трей с Exit)
2. **macOS**: собрать `dotnet publish -r osx-arm64`, дать Screen Recording permission, запустить, проверить обнаружение окон и воспроизведение звука
3. **Linux**: собрать `dotnet publish -r linux-x64`, установить wmctrl + ffplay, запустить, проверить аналогично
4. **Graceful shutdown**: на каждой платформе нажать Ctrl+C, убедиться что приложение завершается, temp-файлы удалены
5. **Документация**: проверить что docs/ соответствует новой архитектуре

### Notes

- **macOS Screen Recording**: без этого разрешения `CGWindowListCopyWindowInfo` вернёт список окон без заголовков и имён процессов. Нужно документировать для пользователя.
- **macOS трей**: полноценная реализация NSStatusItem через Objective-C runtime возможна, но сложна. Оставлена как future work.
- **Linux Wayland**: `wmctrl` работает только с X11. На Wayland потребуется альтернативный подход (D-Bus, wlr-foreign-toplevel-management). Документировать как known limitation.
- **Порядок сборки**: задачи 1-18 создают новые файлы не ломая существующую компиляцию. Task 19 переключает .csproj. Task 20 удаляет старое. Task 21 обновляет документацию.
