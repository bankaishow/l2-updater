# Выпуск клиента и апдейтера: что настроено и как повторять

Состояние на 2026-10-26. Все шаги выполнены владельцем на Windows-машине разработки клиента; сайт только отдаёт готовые подписанные файлы.

## Что где лежит

| Что | Где | Адрес |
|---|---|---|
| Файлы клиента (11,3 ГБ, 2123 файла) | Cloudflare R2, бакет `dimythras-files`, папка `mythras/` | `https://dl.dimythras.com/mythras/` |
| `L2Updater.exe` для самообновления | тот же бакет, `updater/<версия>/` | `https://dl.dimythras.com/updater/0.1.0/L2Updater.exe` |
| `manifest.json` и `.sig` | боевая виртуалка, `storage/app/updater/` (контейнер `l2site-app-1`) | `https://dimythras.com/updater/manifest.json` |
| `files.json` и `.sig` клиента | там же, `clients/mythras/` | `https://dimythras.com/updater/clients/mythras/files.json` |
| Новости и статус серверов | сайт, без подписи | `https://dimythras.com/api/updater/news`, `/api/updater/status` |
| Закрытый ключ подписи | только у владельца: `C:\Users\dimag\keys\release.pem` (копии на флешке и в менеджере паролей) | нигде не публикуется |
| Открытый ключ | `AppConfig.TrustedKeys` (`key_id 615208d395e151de`) | в коде апдейтера |
| Эталонная папка клиента | `E:\PORTABLE_GAMES\dimythras-release` (рабочий клиент на `L:` не трогаем) | — |

Названия серверов в окне апдейтер берёт из `/api/updater/status`, в манифесте они не нужны (`"servers": []`).

## Один раз (сделано)

1. **R2.** Бакет `dimythras-files`, Custom Domain `dl.dimythras.com` (статус Active), API-токен Object Read & Write только на этот бакет. Подробно: `l2-site/docs/updater/r2.md`.
2. **rclone** (`winget install Rclone.Rclone`, `rclone config`): имя `r2`, тип s3, провайдер Cloudflare, endpoint `https://<account id>.r2.cloudflarestorage.com`, регион пустой, advanced config `n`. Секрет лежит в `%APPDATA%\rclone\rclone.conf`. Проверка: `rclone lsd r2:`, тестовый файл открывается по `https://dl.dimythras.com/...` кодом 200.
3. **Python и ключ.** `winget install Python.Python.3.12`, `pip install cryptography`, `git clone https://github.com/bankaishow/l2-updater`, затем:
   ```powershell
   mkdir $HOME\keys
   python l2-updater\tools\updater_build.py keygen --out $HOME\keys\release
   ```
   Открытый ключ (`release.spki.b64`) вписан в `src/L2Updater.App/AppConfig.cs`, тестовый ключ убран. Потеря `release.pem` = невозможно выпускать обновления, копии обязательны.

   **Пароль на ключ (с 2026-10-29).** Новый `keygen` запрашивает пароль (не короче 12 символов, вводится дважды) и сохраняет ключ зашифрованным (PKCS8, AES-256). Любая команда, которой нужен ключ (`files`, `manifest`), спросит пароль. Для проверки подписи пароль не нужен: `verify --key $HOME\keys\release.spki.b64 файл.json`. Защитить уже существующий ключ без смены (тот же `key_id`, `AppConfig` и апдейтер менять не надо):
   ```powershell
   python l2-updater\tools\updater_build.py protect --key $HOME\keys\release.pem
   ```
   Команда создаст `release.enc.pem`. Проверьте его (`manifest` с новым файлом и `verify`), сохраните копии файла и пароля в двух местах, потом удалите старый открытый `release.pem` и все его копии (флешка, облако). Пароль не хранить рядом с файлом ключа.
4. **Эталонная папка клиента.** Чистая копия без архивов и мусора (в корне рабочей папки лежали `*.7z`, `*.rar`, `*.zip`, `LineageII.new`, `Screenshot`; `updater_build.py` исключений не умеет и берёт всё из `--root`):
   ```powershell
   robocopy "L:\PORTABLE_GAMES\Lineage II-HF-Z\Lineage II" "E:\PORTABLE_GAMES\dimythras-release" /E /XF *.7z *.rar *.zip *.new /XD Screenshot
   ```

## Первый выпуск клиента Mythras (2026-10-26)

1. Залить клиент: `rclone copy E:\PORTABLE_GAMES\dimythras-release r2:dimythras-files/mythras --progress` (около 2 часов, при обрыве повторить ту же команду). Проверка: `rclone check E:\PORTABLE_GAMES\dimythras-release r2:dimythras-files/mythras --one-way` → `0 differences found`.
2. Собрать подписанный список, **одной строкой** (при переносе строки PowerShell выполнит только первую часть):
   ```powershell
   python l2-updater\tools\updater_build.py files --key $HOME\keys\release.pem --client mythras --version 2026.10.26-1 --root E:\PORTABLE_GAMES\dimythras-release --out $HOME\out --user system/Option.ini --user system/user.ini --user system/WindowsInfo.ini --user system/chatfilter.ini --user system/Running.ini --user system/s_info.ini
   ```
   Файлы с `--user` скачиваются только если их нет у игрока; существующие не перезаписываются. `L2.ini` (адрес сервера) не помечен и обновляется. Макросы и панели хранятся на сервере (`character_macroses`), в `system` их нет. Ожидаемый итог: `2123 files, 12176991648 bytes`.
3. Положить `L2Updater.exe` из релиза CI (`https://github.com/bankaishow/l2-updater/releases/tag/test-build`) в `r2:dimythras-files/updater/<версия>/`. SHA-256 и размер файла вписать в `tools/manifest.config.json` (`updater`). Версия в манифесте должна равняться версии сборки exe (`0.1.<номер запуска CI>`, сейчас 0.1.19; имя папки в R2 может быть любым), иначе окно будет без конца обновляться.
4. Собрать манифест: адреса и клиент заданы в `tools/manifest.config.json`.
   ```powershell
   python l2-updater\tools\updater_build.py manifest --key $HOME\keys\release.pem --config l2-updater\tools\manifest.config.json --out $HOME\out
   python l2-updater\tools\updater_build.py verify --key $HOME\keys\release.pem $HOME\out\manifest.json
   ```
   Нужно `signature OK`.
5. Перенести `out` на боевую виртуалку и положить в хранилище сайта:
   ```powershell
   scp -r $HOME\out john@<адрес боевой виртуалки>:~/out
   ```
   ```bash
   docker exec l2site-app-1 mkdir -p /var/www/html/storage/app/updater
   docker cp ~/out/. l2site-app-1:/var/www/html/storage/app/updater/
   docker exec -u root l2site-app-1 chown -R www-data:www-data /var/www/html/storage/app/updater
   ```
6. Проверка: четыре адреса (`manifest.json`, `manifest.json.sig`, `files.json`, `files.json.sig` под `https://dimythras.com/updater/…`) отдают 200.

## Обновление клиента (потом)

1. Изменить файлы в эталонной папке.
2. `rclone copy` в R2 (не `sync`: он удаляет файлы, которые ещё нужны не обновившимся игрокам).
3. Пересобрать `files.json` (команда из шага 2 выше, новая `--version`, при необходимости `--delete` устаревших файлов).
4. Выложить `files.json` и `.sig` на сайт (шаг 5). Порядок строгий: **сначала файлы в R2, потом списки на сайт**.
5. Откат: выложить прежний `files.json`; его файлы должны остаться в R2.

Манифест пересобирается только при смене списка клиентов, ссылок или версии апдейтера. Серийный номер берётся из времени, апдейтер не принимает манифест старее уже виденного.

## Новая версия апдейтера

1. Пуш в `main` → CI собирает `L2Updater.exe` (`0.1.<номер запуска>`, один файл, win-x64).
2. Скачать exe, положить в новую папку R2 (`updater/<версия>/`), в `tools/manifest.config.json` обновить `version`, `url`, `sha256`, `size`.
3. Пересобрать и выложить манифест. Игроки обновятся сами при следующем запуске (`SelfUpdater`).

## Проверка на Windows (сделана 2026-10-26)

Скачан exe, выбрана пустая папка, докачано 11,3 ГБ, «Проверить все файлы», «Играть», самообновление 0.1.19 → 0.1.22: всё работает. Замечено при проверке и исправлено в 0.1.22: проверка хешей зависала без прогресса, `l2.exe` просит права администратора (апдейтер теперь запрашивает UAC сам). SmartScreen предупреждает (подписи кода нет), права администратора на сам апдейтер не нужны.

Для следующей проверки: скачать exe в пустую папку, «Подробнее → Выполнить в любом случае», выбрать пустую папку игры, дождаться докачки, повторный запуск должен сказать «Все файлы в порядке».

## План ближайших шагов (что нужно сделать)

Сделано 2026-10-29: аудит безопасности (`docs/security-audit.md`), запрет списков файлов в `.l2updater/`, защита от зависания проверки, запуск игры с запросом UAC, поддержка защитной программы `guard`, пароль на ключ подписи. Для игроков в боевом манифесте пока апдейтер 0.1.22: всё, что выпущено после него, до них не дошло.

**Шаг 1. Владелец, сразу и независимо от остального.**
1. `git pull` в `l2-updater` на Windows, затем `updater_build.py protect --key $HOME\keys\release.pem` (команды в разделе «Один раз», пункт 3).
2. Проверить зашифрованный ключ, сохранить копии файла и пароля в двух местах, удалить открытый `release.pem` и все его копии.
3. Завести второй запасной ключ (`keygen`, тоже с паролем), запечатать и хранить отдельно. В апдейтер его открытую часть добавить при ближайшей сборке (`AppConfig.TrustedKeys` умеет несколько ключей), чтобы смена ключа не отрезала игроков.
4. Включить двухфакторную защиту на аккаунте GitHub и защиту ветки `main`.

**Шаг 2. Следующая версия апдейтера (одна, пакетом).** Не выпускать апдейтер на каждую мелочь: игроки обновляются только при запуске, а новые поля формата работают лишь у тех, кто уже обновился. В пакет войдёт:
- уже готовое: запрет `.l2updater/` в списках, `guard`, проверка в фоне, UAC;
- `files_sha256` в записи клиента и `expires_at` в манифесте (находка 1 аудита: защита `files.json` от отката и «заморозки»; меняет `ManifestParser`, `UpdateSession.LoadFileListAsync`, `updater_build.py`, `docs/format.md`, тесты);
- по согласованию с Claude анти-чита и Claude сервера: аргумент `--launcher-version` для Agent и код выхода «обновите программу» (предложение в `/home/john/l2-exchange/updater.md`);
- запасной ключ в `TrustedKeys`;
- отдельный workflow выпуска по тегу, действия GitHub закреплены по хешу коммита, правильный текст релиза (находка 3 аудита).

**Шаг 3. Выпуск этой версии** (как «Новая версия апдейтера» ниже): CI по тегу, exe в `r2:dimythras-files/updater/<версия>/`, обновить `updater` в `tools/manifest.config.json`, пересобрать манифест (в нём пока без новых полей), выложить на сайт. Постоянную ссылку для игроков `https://dl.dimythras.com/updater/L2Updater.exe` обновить той же копией.

**Шаг 4. Подождать, пока игроки обновятся** (несколько дней, смотреть число обновившихся по логам сайта или по статусу). Старые версии игнорируют новые поля манифеста.

**Шаг 5. Включить защиту** (только когда Agent готов Claude анти-чита, README и SignPath согласованы владельцем):
1. `L2Sec.Agent.exe` с вшитой конфигурацией положить в эталонную папку клиента.
2. `rclone copy` в R2, пересобрать `files.json`.
3. В `tools/manifest.config.json` добавить клиенту `"guard": "system/L2Sec.Agent.exe"`, у клиента `files_sha256` считается `updater_build.py` сам, в манифест добавить `expires_at`.
4. Пересобрать и выложить манифест. Сервер включает приём (`/v1/challenge`, `/v1/hwid`), потом `RequireClientGuard`, потом `MinLauncherVersion` (в этом порядке).
5. Проверка на чистой Windows: Agent запускается без окна, код 0 пускает, виртуалка (код 10) блокирует с сообщением.

**Что обновлять в каком случае** (кратко):
| Что изменилось | Что делать |
|---|---|
| код апдейтера | новая версия exe: R2, `updater` в манифесте, выложить манифест |
| Agent или файлы клиента | `rclone copy` в R2 и пересобрать `files.json` |
| ссылки, список клиентов | пересобрать и выложить манифест |
| сервер, процесс у владельца | для игроков ничего |

Исправление в коде апдейтера работает только у тех, кто уже получил новую версию. Новые поля формата вводить только после того, как игроки перешли на версию, которая их понимает.

## Ещё не сделано

- Заявка в SignPath Foundation (README с политикой подписи готов, коммит `72a6763`); без подписи кода SmartScreen предупреждает.
- Аудит безопасности.
- Второй клиент (Interlude / Classic): отдельная папка `interlude/` в бакете, свой `files.json` (`--client`, `--root`) и запись в `clients` манифеста.
- Форум `https://forum.dimythras.com` ещё не работает; ссылка в манифесте оставлена.
- Проверить «только для чтения» (замена и удаление файлов с этим атрибутом, У-2б).
