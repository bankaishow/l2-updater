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

## Ещё не сделано

- Заявка в SignPath Foundation (README с политикой подписи готов, коммит `72a6763`); без подписи кода SmartScreen предупреждает.
- Аудит безопасности.
- Второй клиент (Interlude / Classic): отдельная папка `interlude/` в бакете, свой `files.json` (`--client`, `--root`) и запись в `clients` манифеста.
- Форум `https://forum.dimythras.com` ещё не работает; ссылка в манифесте оставлена.
- Проверить «только для чтения» (замена и удаление файлов с этим атрибутом, У-2б).
