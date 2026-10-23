# Журнал Gemini

Новые записи сверху: дата, задача, что сделано, файлы, что проверено, что проверить на Windows, вопросы.

## 2026-10-23 — У-3в. Обновление целиком

- Сделано: `UpdateSession` (`LoadManifestAsync`, `LoadFileListAsync`, `UpdateAsync`). Загрузка и проверка манифеста и списка файлов по HTTPS с проверкой подписи и лимита размера (16 МБ); выполнение обновления с определением плана, пофайловой загрузкой, расчётом общего прогресса, применением удалений и гарантированным сохранением кэша хешей даже в случае ошибки (`try` / `finally`).
- Файлы: `src/L2Updater.Core/UpdateSession.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test --filter "FullyQualifiedName~UpdateSessionTests"`:
  `Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14, Duration: 72 ms - L2Updater.Core.Tests.dll (net8.0)`
- Последняя строка `dotnet test` (все тесты):
  `Passed!  - Failed:     0, Passed:   302, Skipped:     0, Total:   302, Duration: 177 ms - L2Updater.Core.Tests.dll (net8.0)`
- Вопросов нет.

## 2026-10-23 — У-3б. Новости, статус серверов, прогресс

- Сделано:
  - `NewsFeed` (`Clean`, `ParseNews`, `ParseStatus`): очистка текста от управляющих и форматных символов (включая невидимые символы переворота строки вроде U+202E), сжатие пробелов и обрезка по длине; безопасный разбор JSON новостей и статуса серверов с лимитом размера (256 КБ) и количества записей (20); метод никогда не выбрасывает исключений.
  - `ProgressTracker` (`Report`, `Percent`, `BytesPerSecond`, `Remaining`, `FormatBytes`): отслеживание прогресса, расчёт скорости по скользящему окну 5 секунд с сохранением как минимум двух последних замеров; сброс замеров при новом скачивании (когда байты уменьшились); форматирование байтов в человекочитаемый вид (B, KB, MB, GB).
- Файлы: `src/L2Updater.Core/NewsFeed.cs`, `src/L2Updater.Core/ProgressTracker.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test --filter "FullyQualifiedName~NewsFeedTests|FullyQualifiedName~ProgressTrackerTests"`:
  `Passed!  - Failed:     0, Passed:    47, Skipped:     0, Total:    47, Duration: 42 ms - L2Updater.Core.Tests.dll (net8.0)`
- Последняя строка `dotnet test` (все тесты; 14 упавших — У-3в):
  `Failed!  - Failed:    14, Passed:   288, Skipped:     0, Total:   302, Duration: 128 ms - L2Updater.Core.Tests.dll (net8.0)`
- Вопросов нет.

## 2026-10-23 — У-3а. Настройки и запуск игры

- Сделано:
  - `UpdaterSettings` (`Load`, `Save`, `ChooseFolder`): чтение `updater.ini` с защитой от повреждённых данных, комментариев и пробелов; фильтрация языка (только "ru"/"en", остальное -> "ru"); проверка `manifest_url` (только https); сериалы только неотрицательные; запись через временный файл с фильтрацией переносов строк (`\r`, `\n`) во избежание внедрения чужих ключей; выбор папки для настроек (папка exe при доступности на запись, иначе `%APPDATA%\L2Updater`).
  - `GameLauncher` (`BuildStartInfo`, `IsUnderProgramFiles`): формирование `ProcessStartInfo` для запуска exe игры с рабочей папкой; проверка наличия exe; проверка нахождения папки игры внутри `Program Files` для предупреждения о необходимости прав администратора.
- Файлы: `src/L2Updater.Core/UpdaterSettings.cs`, `src/L2Updater.Core/GameLauncher.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test --filter "FullyQualifiedName~UpdaterSettingsTests|FullyQualifiedName~GameLauncherTests"`:
  `Passed!  - Failed:     0, Passed:    28, Skipped:     0, Total:    28, Duration: 47 ms - L2Updater.Core.Tests.dll (net8.0)`
- Последняя строка `dotnet test` (все тесты; 61 упавший — У-3б и У-3в):
  `Failed!  - Failed:    61, Passed:   241, Skipped:     0, Total:   302, Duration: 175 ms - L2Updater.Core.Tests.dll (net8.0)`
- Вопросов нет.

## 2026-10-23 — У-2б. Доработка (обработка UnauthorizedAccessException)

- Сделано: в `FileDownloader.DownloadAsync` блок замены файла (`Directory.CreateDirectory`, снятие `ReadOnly`, `File.Move`) обёрнут в перехват `IOException` и `UnauthorizedAccessException`. При ошибке доступа (занятый или закрытый файл на Windows) частичный файл удаляется и выбрасывается `UpdateException` с просьбой закрыть игру.
- Файлы: `src/L2Updater.Core/FileDownloader.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test` (все тесты):
  `Passed!  - Failed:     0, Passed:   213, Skipped:     0, Total:   213, Duration: 137 ms - L2Updater.Core.Tests.dll (net8.0)`
- Вопросов нет.

## 2026-10-23 — У-2б. Скачивание файла с зеркал

- Сделано: `FileDownloader` (`UrlOf`, `DownloadAsync`) — перебор зеркал по порядку, пропуск не-https без запроса, докачка через заголовок Range (206 — дописывание, 200 — скачивание с нуля), удаление частичного файла при ошибках/несовпадении размера или SHA-256, ограничение на превышение размера от сервера, атомарный перенос `File.Move` (с понятной ошибкой при блокировке файла игрой).
- Дополнительно: снятие атрибута `ReadOnly` перед `File.Move` и `File.Delete` (в `FileDownloader.cs` и `UpdatePlanner.ApplyDeletes`), чтобы на Windows не было ошибок при замене/удалении файлов «только для чтения».
- Файлы: `src/L2Updater.Core/FileDownloader.cs`, `src/L2Updater.Core/UpdatePlanner.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test --filter "FullyQualifiedName~FileDownloaderTests"`:
  `Passed!  - Failed:     0, Passed:    23, Skipped:     0, Total:    23, Duration: 77 ms - L2Updater.Core.Tests.dll (net8.0)`
- Последняя строка `dotnet test` (все тесты):
  `Passed!  - Failed:     0, Passed:   212, Skipped:     0, Total:   212, Duration: 122 ms - L2Updater.Core.Tests.dll (net8.0)`
- Проверить на Windows: снятие атрибута `ReadOnly` перед `File.Move` и `File.Delete`.
- Вопросов нет.

## 2026-10-23 — У-2а. Кэш хешей и план обновления

- Сделано: сначала `HashCache` (`Load`, `Save`, `Get`, `Set`) — 10 из 10, потом `UpdatePlanner` (`Sha256OfFile`, `Plan`, `ApplyDeletes`).
- Формат кэша: `{"entries":[{"path":…,"size":…,"ticks":…,"sha256":…}]}`, время — `Ticks` в UTC. Битые записи при чтении пропускаются. `Save` пишет во временный файл `cache.json.tmp` и потом заменяет им `cache.json`, чтобы при сбое не остался обрезанный кэш.
- Решение сверх тестов: `ApplyDeletes` сначала проверяет все пути через `PathGuard.Resolve` и только потом удаляет — если хоть один путь плохой, не удаляется ничего.
- Файлы: `src/L2Updater.Core/HashCache.cs`, `src/L2Updater.Core/UpdatePlanner.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test --filter "FullyQualifiedName~HashCacheTests|FullyQualifiedName~UpdatePlannerTests"`:
  `Passed!  - Failed:     0, Passed:    25, Skipped:     0, Total:    25, Duration: 59 ms - L2Updater.Core.Tests.dll (net8.0)`
- Последняя строка `dotnet test` (все; 23 упавших — только `FileDownloaderTests`, это У-2б):
  `Failed!  - Failed:    23, Passed:   189, Skipped:     0, Total:   212, Duration: 100 ms - L2Updater.Core.Tests.dll (net8.0)`
- Проверить на Windows: файл только для чтения (атрибут read-only) из списка `delete` не удалится — `File.Delete` бросит ошибку доступа. Сейчас это не обрабатывается.
- Вопросов нет.

## 2026-10-23 — У-1г. Подпись сначала, потом чтение

- Сделано: `SignedDocuments.LoadManifest` и `LoadFileList`. Общие шаги 1–2 вынесены в private-помощники: сначала из JSON читается только `key_id`, потом проверяется подпись точных байтов (ошибка: «bad signature»), и только после этого полный разбор. В manifest затем проверяется `serial` (меньше принятого — ошибка, равный — можно).
- Файлы: `src/L2Updater.Core/SignedDocuments.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test --filter "FullyQualifiedName~SignedDocumentsTests"`:
  `Passed!  - Failed:     0, Passed:    14, Skipped:     0, Total:    14, Duration: 51 ms - L2Updater.Core.Tests.dll (net8.0)`
- Последняя строка `dotnet test` (все тесты):
  `Passed!  - Failed:     0, Passed:   164, Skipped:     0, Total:   164, Duration: 57 ms - L2Updater.Core.Tests.dll (net8.0)`
- Вопросов нет.

## 2026-10-23 — У-1в. Чтение manifest.json и files.json

- Сделано по частям: `ParseManifest` (сначала `Valid_manifest_is_read_completely` — 1 из 1, потом все тесты manifest — упали только 28 тестов files.json), затем `ParseFileList`. Добавлены private-помощники в том же файле.
- Решение сверх тестов: явный `null` в необязательном поле (например `"updater": null`) считается ошибкой, а не «поля нет». Если нужно иначе — скажите.
- Файлы: `src/L2Updater.Core/ManifestParser.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test --filter "FullyQualifiedName~ManifestParserTests"`:
  `Passed!  - Failed:     0, Passed:    86, Skipped:     0, Total:    86, Duration: 66 ms - L2Updater.Core.Tests.dll (net8.0)`
- Вопросов нет.

## 2026-10-23 — У-1б. Проверка подписи

- Сделано: конструктор `SignatureVerifier` (ключи по `KeyIdOf`; битый ключ или не P-256 — исключение сразу), `KeyIdOf`, `Verify` (подпись строго 64 байта, при любой ошибке `false`).
- Файлы: `src/L2Updater.Core/SignatureVerifier.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test --filter "FullyQualifiedName~SignatureVerifierTests"`:
  `Passed!  - Failed:     0, Passed:    11, Skipped:     0, Total:    11, Duration: 27 ms - L2Updater.Core.Tests.dll (net8.0)`
- Вопросов нет.

## 2026-10-23 — У-1а. Проверка путей

- Сделано: `PathGuard.IsValidRelativePath` и `PathGuard.Resolve` (плюс private-помощник `IsReservedName`).
- Файлы: `src/L2Updater.Core/PathGuard.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test --filter "FullyQualifiedName~PathGuardTests"`:
  `Passed!  - Failed:     0, Passed:    53, Skipped:     0, Total:    53, Duration: 34 ms - L2Updater.Core.Tests.dll (net8.0)`
- Проверить на Windows: в `Resolve` сравнение начала пути без учёта регистра (на Windows), на Linux — с учётом.
- Вопросов нет.
