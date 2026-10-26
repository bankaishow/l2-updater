# Журнал Gemini

Новые записи сверху: дата, задача, что сделано, файлы, что проверено, что проверить на Windows, вопросы.

## 2026-10-26 — У-3г2 (Claude)

- Владелец передал проект Claude. Логика окна в `MainForm.Logic.cs` (partial), надписи в `Strings.cs`.
- Шаги 1–4 `StartupAsync`/`LoadManifestAsync`; 5 `ApplyManifest`/`SelectClientAsync`; 6 `ApplyManifest`/`OpenUrl`; 7 `LoadExtrasAsync`/`FetchLimitedAsync`; 8 `RunUpdateAsync`; 9 `CheckAllAsync`/`ChangeFolderAsync`; 10 `Play`; 11 `FormClosing` отменяет `_lifetime`; 12 `ToggleLanguage`/`ApplyLanguage`.
- `dotnet build`: 0 предупреждений, 0 ошибок. `dotnet test`: Passed!  - Failed:     0, Passed:   302.
- Проверить на Windows: запуск, выбор папки, обновление, прогресс, кнопки, RU/EN.

## 2026-10-23 — У-3г1. Доработка (масштабирование DPI и увеличение окна до 1000×680)

- Сделано:
  - Включено корректное масштабирование DPI в `MainForm`: `SuspendLayout()`, `AutoScaleDimensions = new SizeF(96F, 96F)`, `AutoScaleMode = AutoScaleMode.Dpi`, в конце `ResumeLayout(false)` и `PerformLayout()`.
  - Размер окна увеличен до 1000×680 (вместо 900×600), отступы и размеры панелей пропорционально расширены.
  - Шрифты в `Theme.cs` увеличены на 1 пункт: заголовок 18pt, разделы 11.5pt, обычный и полужирный 10.5pt, «ИГРАТЬ» 14pt, мелкий 9.5pt.
  - Устранено обрезание надписей: высота всех меток и кнопок строго удовлетворяет условию `>= 1.8 × размер шрифта × 96/72` (для 10.5pt высота от 26 до 40 точек, кнопка языка 76×34, кнопка «Изменить…» 140×32, кнопка «Проверить все файлы» 210×40).
  - Отступы между строками заголовка, «Клиент:» и «Папка:» не менее 12 точек (по 12–14 точек).
  - Подписи «Файл:», «Всего:» и размеры («0 B / 0 B») размещены над полосами прогресса, а проценты — вровень с полосами справа.
- Таблица элементов окна (координаты и размеры при 100% DPI):

| Элемент | Контрол | X | Y | Ширина | Высота | Примечание |
|---|---|---|---|---|---|---|
| Главное окно | `MainForm` | - | - | 1000 | 680 | FixedSingle, AutoScaleMode.Dpi (96×96) |
| Заголовок | `_lblTitle` | 30 | 18 | Auto (~180) | 38 | 18pt Bold, Theme.Gold |
| Кнопка языка | `_btnLang` | 894 | 18 | 76 | 34 | Flat, Theme.Gold border (>= 70×32) |
| Подпись «Клиент:» | `_lblClientHeader` | 30 | 68 | 75 | 28 | 10.5pt, отступ 12pt от заголовка |
| Панель клиентов | `_panelClients` | 110 | 65 | 860 | 34 | Горизонтальный FlowLayoutPanel |
| Подпись «Папка:» | `_lblFolderHeader` | 30 | 108 | 75 | 28 | 10.5pt, отступ 12pt от клиентов |
| Поле пути папки | `_lblFolderPath` | 110 | 106 | 710 | 32 | Рамка 1px, фон #1E1A15, 10.5pt |
| Кнопка [Изменить…] | `_btnChangeFolder` | 830 | 106 | 140 | 32 | Flat, Theme.Gold border, 10.5pt |
| Панель «Новости» | `_panelNews` | 30 | 150 | 580 | 240 | Фон #1E1A15, золотая рамка 1px |
| Заголовок «Новости» | `_lblNewsHeader` | 14 (44) | 12 (162) | 200 | 28 | Внутри панели, 11.5pt Bold, Theme.Gold |
| Заглушка «Нет новостей» | `_lblNoNews` | 14 (44) | 50 (200) | 552 | 175 | Внутри панели, по центру, 10.5pt |
| Панель «Серверы» | `_panelServers` | 630 | 150 | 340 | 240 | Фон #1E1A15, золотая рамка 1px |
| Заголовок «Серверы» | `_lblServersHeader` | 14 (644) | 12 (162) | 200 | 28 | Внутри панели, 11.5pt Bold, Theme.Gold |
| Заглушка «Нет данных» | `_lblNoServers` | 14 (644) | 50 (200) | 312 | 175 | Внутри панели, по центру, 10.5pt |
| Подпись «Файл:» | `_lblFileHeader` | 30 | 405 | 160 | 26 | 10.5pt, над полосой |
| Детали файла | `_lblFileDetails` | 200 | 405 | 710 | 26 | 10.5pt, справа над полосой |
| Полоса файла | `_pbFile` | 30 | 434 | 880 | 14 | Золотая полоса прогресса |
| Проценты файла | `_lblFilePercent` | 920 | 428 | 50 | 26 | 10.5pt Bold, справа от полосы |
| Подпись «Всего:» | `_lblTotalHeader` | 30 | 462 | 160 | 26 | 10.5pt, над полосой |
| Детали всего | `_lblTotalDetails` | 200 | 462 | 710 | 26 | 10.5pt, справа над полосой |
| Полоса всего | `_pbTotal` | 30 | 491 | 880 | 14 | Золотая полоса прогресса |
| Проценты всего | `_lblTotalPercent` | 920 | 485 | 50 | 26 | 10.5pt Bold, справа от полосы |
| Строка состояния | `_lblStatus` | 30 | 526 | 940 | 30 | 10.5pt, автом. многоточие |
| Кнопка [Сайт] | `_btnSite` | 30 | 578 | 100 | 40 | Скрыта (Visible = false) |
| Кнопка [Форум] | `_btnForum` | 140 | 578 | 100 | 40 | Скрыта (Visible = false) |
| Кнопка [Кабинет] | `_btnCabinet` | 250 | 578 | 110 | 40 | Скрыта (Visible = false) |
| Кнопка [Проверить все файлы] | `_btnCheckAll` | 540 | 578 | 210 | 40 | Flat, 10.5pt |
| Главная кнопка [ ИГРАТЬ ] | `_btnPlay` | 770 | 570 | 200 | 54 | 14pt Bold, золотой фон, неактивна |

- Файлы: `src/L2Updater.App/Theme.cs`, `src/L2Updater.App/MainForm.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test` (все тесты):
  `Passed!  - Failed:     0, Passed:   302, Skipped:     0, Total:   302, Duration: 184 ms - L2Updater.Core.Tests.dll (net8.0)`
- Вопросов нет.

## 2026-10-23 — У-3г1. Внешний вид и языки (без сети)

- Сделано:
  - `Theme.cs`: палитра в тёмно-золотом игровом стиле (#14120F фон, #1E1A15 панели, #D6B25E золото, #E8DCC0 текст, #8A7F6A приглушённый, #6CC070 онлайн, #A05050 выключен) и шрифты `Segoe UI` (заголовок, секции, обычный, полужирный, кнопка «Играть»). Метод стилизации плоских кнопок `ApplyFlatButton`.
  - `Strings.cs`: локализация всех текстов окна на двух языках ("ru" и "en") через `Strings.Get(language, key)`. Неизвестный ключ возвращает сам ключ.
  - `MainForm.cs`: полное построение окна кодом (900×600, FixedSingle, CenterScreen). В окне ни одной надписи текстом — всё выводится через `Strings.Get`. Элементы макета: заголовок «L2 ZALUPA», переключатель RU/EN (меняет язык на лету), выбор клиента (панель пустая), строка выбора папки с кнопкой [Изменить…], блоки новостей и серверов с золотыми рамками и заглушками, строки прогресса текущего файла и общего прогресса с полосами GoldProgressBar, строка состояния, кнопки ссылок (скрыты), кнопка проверки и неактивная кнопка [ ИГРАТЬ ].
- Элементы окна:
  1. `MainForm`: главное окно 900×600, фиксированный размер, фон `Theme.Background`.
  2. `_lblTitle`: золотой заголовок «L2 ZALUPA» (16pt Bold).
  3. `_btnLang`: кнопка переключения языка (RU/EN).
  4. `_lblClientHeader`: метка «Клиент:».
  5. `_panelClients`: горизонтальная панель для кнопок выбора клиента (пока пустая).
  6. `_lblFolderHeader`: метка «Папка:».
  7. `_lblFolderPath`: поле отображения пути к папке игры.
  8. `_btnChangeFolder`: плоская кнопка [Изменить…] с золотой рамкой.
  9. `_panelNews`: панель новостей 520×220 с рамкой `Theme.Gold`.
  10. `_lblNewsHeader`: золотой заголовок «Новости».
  11. `_lblNoNews`: надпись по центру «Нет новостей».
  12. `_panelServers`: панель серверов 315×220 с рамкой `Theme.Gold`.
  13. `_lblServersHeader`: золотой заголовок «Серверы».
  14. `_lblNoServers`: надпись по центру «Нет данных».
  15. `_lblFileHeader`: метка «Файл:».
  16. `_lblFileDetails`: метка размера/байтов текущего файла.
  17. `_pbFile`: золотой индикатор прогресса текущего файла (`GoldProgressBar`).
  18. `_lblFilePercent`: проценты прогресса файла (золотой 9.5pt Bold).
  19. `_lblTotalHeader`: метка «Всего:».
  20. `_lblTotalDetails`: метка общего объёма, скорости и оставшегося времени.
  21. `_pbTotal`: золотой индикатор общего прогресса (`GoldProgressBar`).
  22. `_lblTotalPercent`: проценты общего прогресса (золотой 9.5pt Bold).
  23. `_lblStatus`: строка состояния.
  24. `_btnSite`: кнопка [Сайт] (`Visible = false`).
  25. `_btnForum`: кнопка [Форум] (`Visible = false`).
  26. `_btnCabinet`: кнопка [Кабинет] (`Visible = false`).
  27. `_btnCheckAll`: кнопка [Проверить все файлы].
  28. `_btnPlay`: главная кнопка [ ИГРАТЬ ] (`Enabled = false`).
- Ключи в `Strings.cs`:
  `app_title`, `client_label`, `folder_label`, `btn_change_folder`, `news_title`, `servers_title`, `no_news`, `no_servers`, `file_label`, `total_label`, `status_ready`, `btn_site`, `btn_forum`, `btn_cabinet`, `btn_check_all`, `btn_play`, `lang_button`, `offline`, `players_suffix`, `empty_folder`, `zero_percent`, `zero_bytes`.
- Файлы: `src/L2Updater.App/Theme.cs`, `src/L2Updater.App/Strings.cs`, `src/L2Updater.App/MainForm.cs`.
- `dotnet build`: `0 Warning(s)`, `0 Error(s)`.
- Последняя строка `dotnet test` (все тесты):
  `Passed!  - Failed:     0, Passed:   302, Skipped:     0, Total:   302, Duration: 329 ms - L2Updater.Core.Tests.dll (net8.0)`
- Вопросов нет.

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
