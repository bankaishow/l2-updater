# Журнал Gemini

Новые записи сверху: дата, задача, что сделано, файлы, что проверено, что проверить на Windows, вопросы.

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
