# Журнал Gemini

Новые записи сверху: дата, задача, что сделано, файлы, что проверено, что проверить на Windows, вопросы.

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
