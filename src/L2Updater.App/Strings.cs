namespace L2Updater.App;

internal static class Strings
{
    private static readonly Dictionary<string, string> Ru = new(StringComparer.Ordinal)
    {
        ["app_title"] = "L2 ZALUPA",
        ["client_label"] = "Клиент:",
        ["folder_label"] = "Папка:",
        ["btn_change_folder"] = "Изменить…",
        ["news_title"] = "Новости",
        ["servers_title"] = "Серверы",
        ["no_news"] = "Нет новостей",
        ["no_servers"] = "Нет данных",
        ["file_label"] = "Файл:",
        ["total_label"] = "Всего:",
        ["status_ready"] = "Готов к обновлению",
        ["btn_site"] = "Сайт",
        ["btn_forum"] = "Форум",
        ["btn_cabinet"] = "Кабинет",
        ["btn_check_all"] = "Проверить все файлы",
        ["btn_play"] = "ИГРАТЬ",
        ["lang_button"] = "EN",
        ["offline"] = "выключен",
        ["players_suffix"] = "чел.",
        ["empty_folder"] = "—",
        ["zero_percent"] = "0%",
        ["zero_bytes"] = "0 B / 0 B",
    };

    private static readonly Dictionary<string, string> En = new(StringComparer.Ordinal)
    {
        ["app_title"] = "L2 ZALUPA",
        ["client_label"] = "Client:",
        ["folder_label"] = "Folder:",
        ["btn_change_folder"] = "Change…",
        ["news_title"] = "News",
        ["servers_title"] = "Servers",
        ["no_news"] = "No news",
        ["no_servers"] = "No data",
        ["file_label"] = "File:",
        ["total_label"] = "Total:",
        ["status_ready"] = "Ready to update",
        ["btn_site"] = "Website",
        ["btn_forum"] = "Forum",
        ["btn_cabinet"] = "Dashboard",
        ["btn_check_all"] = "Check all files",
        ["btn_play"] = "PLAY",
        ["lang_button"] = "RU",
        ["offline"] = "offline",
        ["players_suffix"] = "players",
        ["empty_folder"] = "—",
        ["zero_percent"] = "0%",
        ["zero_bytes"] = "0 B / 0 B",
    };

    public static string Get(string language, string key)
    {
        var dict = string.Equals(language, "en", StringComparison.OrdinalIgnoreCase) ? En : Ru;
        return dict.TryGetValue(key, out var val) ? val : key;
    }
}
