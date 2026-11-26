using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

class ApplyAllRussianRestorations
{
    static void Main()
    {
        string filePath = @"C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\CheatDetector.cs";
        string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

        Console.WriteLine("Original line count: {0}", lines.Length);

        // Map line replacements by matching distinctive anchors in the line
        int replacedCount = 0;

        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (line.IndexOf('\uFFFD') < 0) continue;

            string newLine = FixLine(line, i + 1);
            if (newLine != line)
            {
                lines[i] = newLine;
                replacedCount++;
            }
        }

        Console.WriteLine("Replaced lines: {0}", replacedCount);

        // Check remaining FFFD
        int remainingFffd = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            if (lines[i].IndexOf('\uFFFD') >= 0)
            {
                remainingFffd++;
                Console.WriteLine("Remaining FFFD at L{0}: {1}", i + 1, lines[i].Trim());
            }
        }

        Console.WriteLine("Remaining FFFD count: {0}", remainingFffd);

        File.WriteAllLines(filePath, lines, new UTF8Encoding(true));
        Console.WriteLine("Saved successfully with UTF-8 BOM.");
    }

    static string FixLine(string line, int lineNum)
    {
        // 1. Categories
        if (line.Contains("checkResult.Category = \"") || line.Contains("checkResult2.Category = \"") || line.Contains("checkResult3.Category = \"") || line.Contains("Category = \""))
        {
            if (lineNum >= 1430 && lineNum <= 1445) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Визуалы\";");
            if (lineNum >= 1650 && lineNum <= 1655) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Удалённые программы\";");
            if (lineNum >= 2440 && lineNum <= 2460) return line.Replace("Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "Category = \"Процессы\";");
            if (lineNum >= 3055 && lineNum <= 3065) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Реестр\";");
            if (lineNum >= 3400 && lineNum <= 3410) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"История\";");
            if (lineNum >= 3680 && lineNum <= 3690) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"История\";");
            if (lineNum >= 3978 && lineNum <= 3985) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Следы запуска\";");
            if (lineNum >= 4070 && lineNum <= 4076) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Следы запуска\";");
            if (lineNum >= 4410 && lineNum <= 4420) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Инжекты / DLL\";");
            if (lineNum >= 4640 && lineNum <= 4650) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Реестр\";");
            if (lineNum >= 4695 && lineNum <= 4705) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Сеть\";");
            if (lineNum >= 4750 && lineNum <= 4760) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"История\";");
            if (lineNum >= 5135 && lineNum <= 5142) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Следы запуска\";");
            if (lineNum >= 5240 && lineNum <= 5248) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Служебные логи\";");
            if (lineNum >= 5330 && lineNum <= 5340) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Следы запуска\";");
            if (lineNum >= 6105 && lineNum <= 6125) return line.Replace("Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\";", "Category = \"Рабочий стол и окна\";");
            if (lineNum >= 7110 && lineNum <= 7145) return line.Replace("Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "Category = \"Следы запуска\";");
            if (lineNum >= 7485 && lineNum <= 7505) return line.Replace("Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd / \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\";", "Category = \"Автокликеры / Макросы\";");
            if (lineNum >= 8105 && lineNum <= 8140) return line.Replace("Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd / DLL\";", "Category = \"Инжекты / DLL\";");
            if (lineNum >= 8250 && lineNum <= 8260) return line.Replace("checkResult3.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult3.Category = \"Реестр\";");
            if (lineNum >= 8660 && lineNum <= 8690) return line.Replace("Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd / \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "Category = \"Реестр / Запуски и перехваты\";");
            if (lineNum >= 10205 && lineNum <= 10225) return line.Replace("Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\";", "Category = \"Рабочий стол и окна\";");
            if (lineNum >= 11920 && lineNum <= 11930) return line.Replace("Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\",", "Category = \"Процессы\",");
            if (lineNum >= 11938 && lineNum <= 11945) return line.Replace("Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\",", "Category = \"Файлы\",");
            if (lineNum >= 11968 && lineNum <= 11972) return line.Replace("Category = \"\ufffd\ufffd\ufffd\ufffd\",", "Category = \"Моды\",");
            if (lineNum >= 12095 && lineNum <= 12105) return line.Replace("checkResult.Category = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Category = \"Аномальные файлы\";");
        }

        // 2. Step Titles in RunFullScan
        if (line.Contains("RunWindowsBamDeletedUserSidExecutionForensicsCheck"))
        {
            return "\t\t\tTuple.Create<string, Func<List<CheckResult>>>(\"\\ud83d\\udc64 Аудит реестра BAM на запуски читов из-под удалённых или гостевых SID...\", ManualChecks.RunWindowsBamDeletedUserSidExecutionForensicsCheck),";
        }
        if (line.Contains("RunMinecraftPlayerCombatAutoClickerLevyFlightHeavyTailCheck"))
        {
            return "\t\t\tTuple.Create<string, Func<List<CheckResult>>>(\"\\ud83d\\udd0d Аналитика Levy Flight & Heavy-Tailed Click Spacing...\", ManualChecks.RunMinecraftPlayerCombatAutoClickerLevyFlightHeavyTailCheck),";
        }
        if (line.Contains("RunMinecraftPlayerCombatAutoClickerExtremeValueTheoryFrechetCheck"))
        {
            return "\t\t\tTuple.Create<string, Func<List<CheckResult>>>(\"\\ud83d\\udd0d Аналитика Extreme Value Theory (EVT/Frechet) CPS...\", ManualChecks.RunMinecraftPlayerCombatAutoClickerExtremeValueTheoryFrechetCheck),";
        }
        if (line.Contains("ScanMeowDoomsdayForensics"))
        {
            return "\t\t\tTuple.Create<string, Func<List<CheckResult>>>(\"\\ud83d\\udc31 MeowDoomsdayFucker: Поиск Doomsday Client (Лончер/Prefetch/DNS/Логи)...\", SsToolsManager.ScanMeowDoomsdayForensics),";
        }
        if (line.Contains("ScanCleanDetectingForensics"))
        {
            return "\t\t\tTuple.Create<string, Func<List<CheckResult>>>(\"\\ud83e\\uddf9 Clean Detecting: Поиск остаточных файлов, скриптов и реестровых ключей...\", SsToolsManager.ScanCleanDetectingForensics),";
        }
        if (line.Contains("ScanSsToolArtifacts"))
        {
            return "\t\t\tTuple.Create<string, Func<List<CheckResult>>>(\"\\ud83d\\udee0\\ufe0f SSTool & Discord: Поиск следов screenshare и логов...\", SsToolsManager.ScanSsToolArtifacts),";
        }
        if (line.Contains("ScanYCheckArtifacts"))
        {
            return "\t\t\tTuple.Create<string, Func<List<CheckResult>>>(\"\\ud83c\\udfaf y_check: Поиск детектов папок и настроек чекера...\", SsToolsManager.ScanYCheckArtifacts)";
        }

        // 3. Names
        if (line.Contains("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd, \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";"))
        {
            return line.Replace("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd, \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult.Name = \"Поиск удалённых программ, логов и очистки корзины\";");
        }
        if (line.Contains("BAM \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd Event Log (EventID 104/3079)"))
        {
            return line.Replace("BAM \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd Event Log (EventID 104/3079)", "BAM журнал и Event Log (EventID 104/3079)");
        }
        if (line.Contains("CrashDumps, JVM hs_err & WER"))
        {
            return line.Replace("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (CrashDumps, JVM hs_err & WER \ufffd\ufffd\ufffd\ufffd)\";", "checkResult.Name = \"Дампы аварийного завершения (CrashDumps, JVM hs_err & WER логи)\";");
        }
        if (line.Contains("USN Journal \ufffd\ufffd NTFS-"))
        {
            return line.Replace("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd USN Journal \ufffd\ufffd NTFS-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (\ufffd\ufffd\ufffd 3.6 \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd)\";", "checkResult.Name = \"Состояние USN Journal на NTFS-дисках (Пункт 3.6 регламента)\";");
        }
        if (line.Contains("(Task View / Desktop 2)"))
        {
            return line.Replace("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd (Task View / Desktop 2)\";", "checkResult.Name = \"Виртуальные рабочие столы и скрытые окна (Task View / Desktop 2)\";");
        }
        if (line.Contains("checkResult3.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";"))
        {
            return line.Replace("checkResult3.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult3.Name = \"Контроль безопасности\";");
        }
        if (line.Contains("Program Compatibility Assistant"))
        {
            return line.Replace("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd PCA (Program Compatibility Assistant)\";", "checkResult.Name = \"Системные логи PCA (Program Compatibility Assistant)\";")
                       .Replace("checkResult2.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd PCA (Program Compatibility Assistant)\";", "checkResult2.Name = \"Системные логи PCA (Program Compatibility Assistant)\";")
                       .Replace("checkResult3.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd PCA (Program Compatibility Assistant)\";", "checkResult3.Name = \"Системные логи PCA (Program Compatibility Assistant)\";");
        }
        if (line.Contains("Discord / Telegram"))
        {
            return line.Replace("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (Discord / Telegram)\";", "checkResult.Name = \"Клиенты передачи файлов и мессенджеры (Discord / Telegram)\";");
        }
        if (line.Contains("checkResult2.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\";"))
        {
            return line.Replace("checkResult2.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult2.Name = \"Загрузка чужих типов и путей\";");
        }
        if (line.Contains("checkResult3.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";"))
        {
            return line.Replace("checkResult3.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult3.Name = \"Инжекции процессов\";");
        }
        if (line.Contains("DirectX MostRecent & IFEO Hijacking"))
        {
            return line.Replace("(DirectInput/Direct3D/\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd)", "(DirectInput/Direct3D/Перехваты)");
        }
        if (line.Contains("IRC / Cloud Configs / IPC"))
        {
            return line.Replace("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd javaw.exe (IRC / Cloud Configs / IPC)\";", "checkResult.Name = \"Активные сетевые сокеты javaw.exe (IRC / Cloud Configs / IPC)\";");
        }
        if (line.Contains("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd (\ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd / \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd)\";"))
        {
            return line.Replace("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd (\ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd / \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd)\";", "checkResult.Name = \"Заголовки открытых окон (Чит-клиенты / Инжекторы)\";");
        }
        if (line.Contains("checkResult3.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";"))
        {
            return line.Replace("checkResult3.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "checkResult3.Name = \"Целостность системы\";");
        }
        if (line.Contains("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd / \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd)\";"))
        {
            return line.Replace("checkResult.Name = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd / \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd)\";", "checkResult.Name = \"История ников и активность на ПК (Сменяемость / Твинки)\";");
        }

        // 4. Details and Evidences
        if (line.Contains("checkResult.Detail = ((list2.Count > 0 || list3.Count > 0) ? (\"") && line.Contains("list2.Count"))
        {
            return "\t\t\tcheckResult.Detail = ((list2.Count > 0 || list3.Count > 0) ? (\"Найдено следов удаления читов: \" + list2.Count + \" | Очисток корзины: \" + list3.Count) : \"Следов намеренного удаления читов перед проверкой не найдено\");";
        }
        if (line.Contains("checkResult.Evidence = ((list2.Count > 0)") && line.Contains("list3.ToArray()"))
        {
            return "\t\t\tcheckResult.Evidence = ((list2.Count > 0) ? (string.Join(\"\\r\\n\", list2.ToArray()) + \"\\r\\n\\r\\nВсе проверенные ресурспаки:\\r\\n\" + string.Join(\"\\r\\n\", list3.ToArray())) : (\"Папка ресурспаков: \" + text));";
        }
        if (line.Contains("checkResult.Detail = (list2.Count > 0 ? (\"") && line.Contains("KillAura/Reach"))
        {
            return "\t\t\tcheckResult.Detail = (list2.Count > 0 ? (\"Обнаружены запрещённые AHK читы (KillAura/Reach): \" + list2.Count + \" шт.\") : (list3.Count > 0 ? \"Обнаружены AHK скрипты / автокликеры (разрешены правилами сервера: \" + list3.Count + \" шт.).\" : \"Макросов и AHK не найдено\"));";
        }
        if (line.Contains("checkResult.Detail = ((list2.Count > 0)") && line.Contains("Temp по размеру"))
        {
            return "\t\t\tcheckResult.Detail = ((list2.Count > 0) ? (\"Обнаружено записей в журнале событий: \" + list2.Count) : \"Журналы чисты\");";
        }
        if (line.Contains("checkResult2.Detail = \"") && line.Contains("Bedrock com.mojang"))
        {
            return "\t\t\tcheckResult2.Detail = \"В папке Bedrock com.mojang найдено подозрительных X-Ray/ESP/FullBright (\" + list3.Count + \" шт.) — требуется ручная проверка!\";";
        }
        if (line.Contains("checkResult.Detail = \"") && line.Contains("list2.Count + \" .).\";") && lineNum >= 5570 && lineNum <= 5576)
        {
            return "\t\t\tcheckResult.Detail = \"Обнаружено стирание или повреждение USN Journal на дисках (\" + list2.Count + \" шт.).\";";
        }
        if (line.Contains("checkResult.Detail = \"USN Journal") && line.Contains("checkedDrives"))
        {
            return "\t\t\tcheckResult.Detail = \"USN Journal на всех проверенных NTFS-дисках (\" + checkedDrives + \") на месте и читается — следы не затирались через deletejournal.\";";
        }
        if (line.Contains("checkResult.Detail = (list2.Count > 0 ? (\"") && line.Contains("5.01 / 5.08") && lineNum >= 5740 && lineNum <= 5748)
        {
            return "\t\t\tcheckResult.Detail = (list2.Count > 0 ? (\"В основном ядре обнаружены модификации кода, агенты или сигнатуры [5.01 / 5.08]: \" + list2.Count + \" шт.\") : (\"Проверено jar-файлов ядра: \" + jarFiles.Count + \". Модификаций ядра и агентов в jar файлах не найдено.\"));";
        }
        if (line.Contains("checkResult.Detail = (flagSuspicious ?") && lineNum >= 6112 && lineNum <= 6116)
        {
            return "\t\t\tcheckResult.Detail = (flagSuspicious ? \"Обнаружены признаки скрытия активных окон (Task View / Desktop 2)!\" : \"Второй рабочий стол и скрытые приложения на ПК отсутствуют\");";
        }
        if (line.Contains("SRUDB.dat") && line.Contains("5.01 / 5.08") && lineNum >= 6850 && lineNum <= 6860)
        {
            return "\t\t\t\t\t\t\t\t\t? \"В базе SRUM обнаружена подозрительная запись сетевого трафика читов или запуск клинеров [5.01 / 5.08]!\"";
        }
        if (line.Contains("DPS Start=4") && lineNum >= 6855 && lineNum <= 6860)
        {
            return "\t\t\t\t\t\t\t\t\t: \"Обнаружен сброс статистики или блокировка службы сбора SRUM (DPS Start=4 / очистка базы) [5.08]!\";";
        }
        if (line.Contains("PcaAppLaunchDic / PcaGeneralDbx"))
        {
            return "\t\t\t\tcheckResult2.Detail = \"В хранилище PCA (PcaAppLaunchDic / PcaGeneralDbx) следов запуска читов не обнаружено.\";";
        }
        if (line.Contains("checkResult.Detail = string.Format(\"") && line.Contains("PCA") && lineNum >= 7118 && lineNum <= 7122)
        {
            return "\t\t\t\tcheckResult.Detail = string.Format(\"Обнаружен запуск и следы читов в системных логах PCA ({0} записей)!\", list2.Count);";
        }
        if (line.Contains("MaxCacheTtl=0") && line.Contains("DoH=2") && lineNum >= 7265 && lineNum <= 7270)
        {
            return "\t\t\tcheckResult.Detail = \"Обнаружено принудительное отключение DNS / обход логирования (MaxCacheTtl=0 или DoH=2)!\";";
        }
        if (line.Contains("DoH/AutoDoH") && lineNum >= 7272 && lineNum <= 7276)
        {
            return "\t\t\tcheckResult.Detail = \"Обнаружены признаки изменения настроек DNS (DoH/AutoDoH/прокси) — веб-запросы могут маскироваться от проверок.\";";
        }
        if (line.Contains("DoH") && line.Contains("DNS") && lineNum >= 7277 && lineNum <= 7282)
        {
            return "\t\t\tcheckResult.Detail = \"Параметры DNS стандартные: принудительный DoH не включён, логирование DNS активно.\";";
        }
        if (line.Contains("Mixin") && line.Contains("5.01 / 5.08") && lineNum >= 7405 && lineNum <= 7410)
        {
            return "\t\t\tcheckResult.Detail = \"Обнаружены скрытые классы Mixin или чит-коды в модах [5.01 / 5.08]: \" + list2.Count + \" шт.\";";
        }
        if (line.Contains("checkResult.Detail = list2.Count > 0 ? \"") && lineNum >= 7490 && lineNum <= 7496)
        {
            return "\t\t\tcheckResult.Detail = list2.Count > 0 ? \"Обнаружено ПО мыши / макросы-кликеры (разрешены правилами сервера: \" + list2.Count + \" шт.).\" : \"Запрещённого управления ПО мыши не обнаружено.\";";
        }
        if (line.Contains("DirectInput/Direct3D") && line.Contains("IFEO") && lineNum >= 8665 && lineNum <= 8670)
        {
            return "\t\t\t\tcheckResult.Detail = isCritical ? (\"Обнаружены перехваты IFEO или подозрительные запуски DIRECTX [5.01 / 5.08] (\" + list2.Count + \" шт.)!\") : \"Записи DirectInput/Direct3D проверены, перехватов IFEO не обнаружено.\";";
        }
        if (line.Contains("DirectInput/Direct3D") && lineNum >= 8676 && lineNum <= 8680)
        {
            return "\t\t\t\tcheckResult2.Detail = \"Параметры Image File Execution Options и подозрительные запуски DirectInput/Direct3D не обнаружены.\";";
        }
        if (line.Contains("Amcache.hve") && line.Contains("5.01 / 5.08") && lineNum >= 8860 && lineNum <= 8866)
        {
            return "\t\t\tcheckResult.Detail = \"Обнаружены следы удалённых чит-клиентов / читов [5.01 / 5.08]: \" + list2.Count + \" шт.\";";
        }
        if (line.Contains("Uninstall") && lineNum >= 8872 && lineNum <= 8876)
        {
            return "\t\t\tcheckResult2.Detail = \"Следов установки известных чит-клиентов в реестре не обнаружено.\";";
        }
        if (line.Contains("IRC") && line.Contains("IPC") && lineNum >= 9015 && lineNum <= 9022)
        {
            return "\t\t\tcheckResult2.Detail = \"Подозрительных соединений IRC, локальных IPC или чит-соединений не обнаружено.\";";
        }
        if (line.Contains("checkResult2.Detail = \"\ud83e\uddf1 Minecraft Bedrock") && lineNum >= 9092 && lineNum <= 9097)
        {
            return "\t\t\t\tcheckResult2.Detail = \"Обнаружено подозрительных сигнатур в памяти javaw: \" + list2.Count + \" шт.!\";";
        }
        if (line.Contains("HKCU AppCompat") && line.Contains("5.01 / 5.08") && lineNum >= 9360 && lineNum <= 9366)
        {
            return "\t\t\tcheckResult.Detail = \"В ветках совместимости системы обнаружены читы, запуск под правами администратора [5.01 / 5.08]: \" + list2.Count + \" шт.\";";
        }
        if (line.Contains("checkResult2.Detail = \"") && lineNum >= 9372 && lineNum <= 9376)
        {
            return "\t\t\tcheckResult2.Detail = \"Подозрительных параметров совместимости и запусков читов не обнаружено.\";";
        }
        if (line.Contains("FeatureUsage") && line.Contains("5.01 / 5.08") && lineNum >= 9515 && lineNum <= 9520)
        {
            return "\t\t\tcheckResult.Detail = \"В статистике переключения окон обнаружены читы, скрываемые при проверке [5.01 / 5.08]: \" + list2.Count + \" шт.\";";
        }
        if (line.Contains("checkResult.Detail = isCritical ? (\"") && line.Contains("5.01") && lineNum >= 10200 && lineNum <= 10205)
        {
            return "\t\t\t\tcheckResult.Detail = isCritical ? (\"Обнаружены открытые окна читов/инжекторов [5.01] (\" + list2.Count + \" шт.)!\") : \"Подозрительных открытых окон не обнаружено.\";";
        }
        if (line.Contains("checkResult2.Detail = \"") && lineNum >= 10211 && lineNum <= 10215)
        {
            return "\t\t\t\tcheckResult2.Detail = \"Подозрительных окон и процессов не обнаружено.\";";
        }
        if (line.Contains("javaagent") && line.Contains("5.01") && lineNum >= 10305 && lineNum <= 10312)
        {
            return "\t\t\t\tcheckResult.Detail = \"В параметрах запуска javaw.exe обнаружены внешние инжекты / javaagent / читы [5.01]: \" + list2.Count + \" шт.\";";
        }
        if (line.Contains("checkResult2.Detail = \"") && lineNum >= 10316 && lineNum <= 10322)
        {
            return "\t\t\t\tcheckResult2.Detail = \"Аргументы запуска процесса javaw.exe стандартные, javaagent и читы отсутствуют.\";";
        }
        if (line.Contains("checkResult3.Detail = \"") && lineNum >= 10328 && lineNum <= 10332)
        {
            return "\t\t\t\tcheckResult3.Detail = \"Minecraft (javaw.exe) в данный момент не запущен.\";";
        }
        if (line.Contains("checkResult.Detail = \"") && line.Contains("5.08") && lineNum >= 10834 && lineNum <= 10838)
        {
            return "\t\t\tcheckResult.Detail = \"Нарушена целостность системных служб / обнаружены попытки сокрытия [5.08]: \" + list2.Count + \" шт.\";";
        }
        if (line.Contains("checkResult.Evidence = string.Join") && lineNum >= 10835 && lineNum <= 10839)
        {
            return "\t\t\tcheckResult.Evidence = string.Join(\"\\r\\n\", list2) + \"\\r\\n\\r\\nАптайм ПК: \" + (int)timeSpan.TotalHours + \"ч \" + timeSpan.Minutes + \"м \" + timeSpan.Seconds + \"с\";";
        }
        if (line.Contains("checkResult2.Detail = \"") && line.Contains("PcaSvc") && lineNum >= 10845 && lineNum <= 10849)
        {
            return "\t\t\tcheckResult2.Detail = \"Все системные службы (PcaSvc, DPS, EventLog, SysMain, BAM, W32Time) активны. Аптайм ПК: \" + (int)timeSpan.TotalHours + \"ч \" + timeSpan.Minutes + \"м \" + timeSpan.Seconds + \"с\";";
        }
        if (line.Contains("checkResult2.Evidence = \"") && lineNum >= 10846 && lineNum <= 10850)
        {
            return "\t\t\tcheckResult2.Evidence = \"Аптайм ПК: \" + (int)timeSpan.TotalHours + \"ч \" + timeSpan.Minutes + \"м \" + timeSpan.Seconds + \"с\\r\\nСостояние системных служб: Исправно\";";
        }
        if (line.Contains("checkResult.Detail = (list8.Count > 0 ? string.Format(\"") && line.Contains("5.01") && lineNum >= 11593 && lineNum <= 11598)
        {
            return "\t\t\tcheckResult.Detail = (list8.Count > 0 ? string.Format(\"⛔ Найдено следов подписок/токенов: {0} к коммерческим платным читам [5.01]!\", list8.Count) : \"Файлы лицензий и токенов платных чит-клиентов не найдены\");";
        }
        if (line.Contains("checkResult2.Detail = (list4.Count > 0 ? string.Format(\"") && line.Contains("5.01") && lineNum >= 11602 && lineNum <= 11606)
        {
            return "\t\t\tcheckResult2.Detail = (list4.Count > 0 ? string.Format(\"⛔ Найдено в реестре: {0} записей платных чит-клиентов [5.01]\", list4.Count) : \"В реестре записей платных чит-клиентов не найдено\");";
        }
        if (line.Contains("checkResult3.Detail = (list9.Count > 0 ? string.Format(\"") && line.Contains("5.08") && lineNum >= 11612 && lineNum <= 11616)
        {
            return "\t\t\tcheckResult3.Detail = (list9.Count > 0 ? string.Format(\"⛔ Найдено переходов сайтов платных читов: {0} в истории браузеров [5.08]\", list9.Count) : \"Посещений сайтов покупки/авторизации платных читов не найдено\");";
        }
        if (line.Contains("checkResult.Detail = (list8.Count > 0 ? string.Format(\"") && line.Contains("HWID") && lineNum >= 11870 && lineNum <= 11875)
        {
            return "\t\t\tcheckResult.Detail = (list8.Count > 0 ? string.Format(\"⛔ Найдено доказательств платных чит-клиентов [5.01]: {0} (токенов: {1}, лоадеров: {2})\", list8.Count, list2.Count, list3.Count) : \"Лоадеров и HWID-токенов платных клиентов не найдено\");";
        }
        if (line.Contains("checkResult2.Detail = (list9.Count > 0 ? string.Format(\"") && line.Contains("5.08") && lineNum >= 11882 && lineNum <= 11886)
        {
            return "\t\t\tcheckResult2.Detail = (list9.Count > 0 ? string.Format(\"⛔ Посещений сайтов покупок/авторизаций читов [5.08]: {0}\", list9.Count) : \"Посещений платных чит-ресурсов в веб-браузерах не найдено\");";
        }
        if (line.Contains("checkResult.Detail = (list2.Count > 0) ? (\"") && line.Contains("usercache.json") || (line.Contains("usercache") && lineNum >= 12346 && lineNum <= 12350))
        {
            return "\t\t\tcheckResult.Detail = (list2.Count > 0) ? (\"Обнаружено уникальных никнеймов/аккаунтов на ПК: \" + list2.Count + \" (для выявления твинков либо смены аккаунта)\") : \"История ников не найдена\";";
        }

        // 5. General String Literal Substitutions
        string s = line;

        // Common FFFD literals
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd Recent]", "⛔ [5.01 - Чит в Recent]");
        s = s.Replace("\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd [", "Удалённый чит-файл в корзине [");
        s = s.Replace("\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd [", "Удалённая папка в корзине [");
        s = s.Replace("< \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \"", "< совпадение \"");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd Minecraft]", "⛔ [5.01 - Чит в логах Minecraft]");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (", "⛔ [5.01 - Запуск чита в профиле (");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd -javaagent \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (", "⛔ [5.01 - Аргумент -javaagent в профиле (");
        s = s.Replace("\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Java-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd!", "внедрение внешнего Java-агента!");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd -Xbootclasspath \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (", "⛔ [5.01 - Аргумент -Xbootclasspath в профиле (");
        s = s.Replace("\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Bootstrap!", "подмена классов Bootstrap!");
        s = s.Replace("Path.Combine(folderPath, \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\")", "Path.Combine(folderPath, \"Загрузки\")");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd AHK \ufffd\ufffd\ufffd]", "⛔ [5.01 - Запрещённый AHK чит]");
        s = s.Replace("\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {2})", "совпадение: {2})");
        s = s.Replace("?? [\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] AHK \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd:", "ℹ️ [Разрешённый автокликер] AHK кликер:");
        s = s.Replace("\"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd <7 \ufffd\ufffd.\"", "\"недавно (<7 дн.)\"");
        s = s.Replace("\">14 \ufffd\ufffd.\"", "\">14 дн.\"");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd Prefetch] [{0}] {1} (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {2:dd.MM.yyyy HH:mm:ss}", "⛔ [5.01 - Чит в Prefetch] [{0}] {1} (запуск: {2:dd.MM.yyyy HH:mm:ss}");
        s = s.Replace(": \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd System \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (wevtutil)!", ": Журнал System был очищен (wevtutil)!");
        s = s.Replace("f.Contains(\"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\")", "f.Contains(\"очищен\") || f.Contains(\"очистка\")");
        s = s.Replace("?? [Bedrock Client Folder] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Bedrock: {0} ({1} \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd)", "⛔ [Bedrock Client Folder] Обнаружена директория чит-клиента Bedrock: {0} ({1} файлов модификаций)");
        s = s.Replace("?? [Bedrock Client Folder] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Bedrock: ", "⛔ [Bedrock Client Folder] Обнаружена директория чит-клиента Bedrock: ");
        s = s.Replace("?? [Bedrock NightVision] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd gfx_gamma={0} (FullBright/\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd) \ufffd options.txt", "⚠️ [Bedrock NightVision] Обнаружено изменённое значение gfx_gamma={0} (FullBright/ночное зрение без зелья) в options.txt");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd UserAssist \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (NoLog=1)]: \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd!", "⛔ [5.08 - Логирование UserAssist отключено (NoLog=1)]: скрытие истории запуска программ!");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd \ufffd UserAssist]: ", "⛔ [5.01 - Удалённый чит в UserAssist]: ");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd UserAssist]: ", "⛔ [5.01 - Чит в UserAssist]: ");
        s = s.Replace("\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd (ExtraCheatDomains) \ufffd DNS \ufffd\ufffd\ufffd\ufffd: ", "Подозрительный чит-домен (ExtraCheatDomains) в DNS кэше: ");
        s = s.Replace("\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (ExtraCheatClients) \ufffd DNS \ufffd\ufffd\ufffd\ufffd: ", "Подозрительный чит-клиент (ExtraCheatClients) в DNS кэше: ");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd PowerShell] [\ufffd\ufffd\ufffd. ", "⛔ [5.08 - Попытка затирания в PowerShell] [стр. ");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd PowerShell] [\ufffd\ufffd\ufffd. ", "⛔ [5.08 - Попытка затирания в PowerShell] [стр. ");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd PowerShell] [\ufffd\ufffd\ufffd. ", "⛔ [5.01 - Чит-команда в PowerShell] [стр. ");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd [{0}]: {1} ({2:dd.MM.yyyy HH:mm})", "⛔ [5.01 - Дамп чит-клиента] Аварийный дамп сбоя [{0}]: {1} ({2:dd.MM.yyyy HH:mm})");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd CrashDumps] \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd [{0}]: {1} ({2:dd.MM.yyyy HH:mm})", "⛔ [5.08 - Дамп клинера в CrashDumps] Дамп утилиты очистки [{0}]: {1} ({2:dd.MM.yyyy HH:mm})");
        s = s.Replace("?? \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd Minecraft javaw.exe: {0} ({1:dd.MM.yyyy HH:mm})", "ℹ️ Свежий дамп Minecraft javaw.exe: {0} ({1:dd.MM.yyyy HH:mm})");
        s = s.Replace("\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd Java/Minecraft: {0} ({1:dd.MM.yyyy HH:mm})", "Дамп сбоя Java/Minecraft: {0} ({1:dd.MM.yyyy HH:mm})");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd JVM (hs_err)] \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd Java \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd [{0}]: {1} (\ufffd\ufffd\ufffd\ufffd: {2:dd.MM.yyyy HH:mm})", "⛔ [5.01 - Чит в логе сбоя JVM (hs_err)] Лог сбоя Java упоминает сигнатуру чита [{0}]: {1} (дата: {2:dd.MM.yyyy HH:mm})");
        s = s.Replace("?? \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd Java hs_err: {0} ({1:dd.MM.yyyy HH:mm}, {2:0} \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd)", "⚠️ Свежий лог сбоя Java hs_err: {0} ({1:dd.MM.yyyy HH:mm}, {2:0} мин назад)");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd WER] \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd WER \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: '{0}' (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1}, \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {2}, \ufffd\ufffd\ufffd\ufffd: {3:dd.MM.yyyy HH:mm})", "⛔ [5.01 - Сбой чита в WER] В отчёте WER зафиксирован сбой известного чит-приложения: '{0}' (сигнатура: {1}, модуль: {2}, дата: {3:dd.MM.yyyy HH:mm})");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd javaw.exe] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Minecraft \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd: '{0}' (\ufffd\ufffd\ufffd\ufffd\ufffd: {1}, \ufffd\ufffd\ufffd\ufffd: {2:dd.MM.yyyy HH:mm})", "⛔ [5.01 - Инжекция в javaw.exe] Процесс Minecraft аварийно завершил внешний модуль: '{0}' (папка: {1}, дата: {2:dd.MM.yyyy HH:mm})");
        s = s.Replace("\ufffd\ufffd\ufffd\ufffd\ufffd WER \ufffd\ufffd\ufffd\ufffd Minecraft: {0} (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1}, \ufffd\ufffd\ufffd\ufffd: {2:dd.MM.yyyy HH:mm})", "Отчёт WER сбоя Minecraft: {0} (модуль сбоя: {1}, дата: {2:dd.MM.yyyy HH:mm})");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd WER] \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: '{0}' ({1}, \ufffd\ufffd\ufffd\ufffd: {2:dd.MM.yyyy HH:mm})", "⛔ [5.08 - Сбой клинера в WER] Сбой утилиты затирания следов: '{0}' ({1}, дата: {2:dd.MM.yyyy HH:mm})");
        s = s.Replace("\"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd USB-", "\"Запуск с USB-");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd [{0}]: {1} ({2:dd.MM.yyyy HH:mm}, {3:0} \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd)!", "⛔ [5.08 - Следы клинера] Запуск утилиты затирания следов [{0}]: {1} ({2:dd.MM.yyyy HH:mm}, {3:0} мин назад)!");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] [{0}]: {1} ({2:dd.MM.yyyy HH:mm}, {3:0} \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd)", "⛔ [5.08 - Запуск утилиты очистки диска] [{0}]: {1} ({2:dd.MM.yyyy HH:mm}, {3:0} мин назад)");
        s = s.Replace("text2.Contains(\"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\")", "text2.Contains(\"отказано в доступе\")");
        s = s.Replace("text2.Contains(\"\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\")", "text2.Contains(\"не найден\")");
        s = s.Replace("text2.Contains(\"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\")", "text2.Contains(\"отсутствует\")");
        s = s.Replace("\") — по мануалу это признак очистки USN Journal!\" + driveLetter + \" \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd.\"", "\") — по регламенту это признак очистки USN Journal на диске \" + driveLetter + \" (следы могли затираться).\"");
        s = s.Replace("?? \ufffd\ufffd\ufffd\ufffd\ufffd 3.6 \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: USN Journal \ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \" + driveLetter + \"\ud83d\udcbe Журнал USN (fsutil)\" + exitCode + \") \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd deletejournal / \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd!\"", "⛔ Пункт 3.6 регламента: USN Journal на диске \" + driveLetter + \" повреждён/очищен (код \" + exitCode + \") командой deletejournal / затирание следов!\"");
        s = s.Replace("\"\ud83d\uddc4\ufe0f Очистка USN Journal (EventID 3079 / FSUTIL.EXE)...\" + driveLetter + \"\ud83d\udcbe Журнал USN (fsutil)\" + exitCode + \", \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd).\"", "\"ℹ️ Проверка USN Journal на диске \" + driveLetter + \" вернула код (\" + exitCode + \", доступ или отсутствие записи).\"");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd '{0}' ({1} \ufffd\ufffd\ufffd\ufffd) \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd!", "⛔ [5.01 - Аномальный размер ядра] Размер '{0}' ({1} байт) совпадает с базой известных чит-клиентов!");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd Timestomp] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd '{0}' \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1:dd.MM.yyyy HH:mm}!", "⛔ [5.08 - Подделка даты Timestomp] Размер '{0}' имеет неестественную дату изменения: {1:dd.MM.yyyy HH:mm}!");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd '{0}' \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd {1:dd.MM.yyyy HH:mm} \ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd (Minecraft \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd {2:dd.MM.yyyy HH:mm})!", "⛔ [5.08 - Подмена запущенного клиента] Файл '{0}' изменён в {1:dd.MM.yyyy HH:mm} во время запущенной игры (Minecraft запущен в {2:dd.MM.yyyy HH:mm})!");
        s = s.Replace("?? [5.01 / 5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Java-\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd jar-\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd: {0}", "⛔ [5.01 / 5.08 - Внедрение Java-агента в ядро] В jar-файле обнаружен скрытый Java-агент: {0}");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd jar-\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd '{0}' \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd: {1}", "⛔ [5.01 - Модификация чит-класса в ядре] В jar-файле версии '{0}' обнаружен чит-класс: {1}");
        s = s.Replace("text2.Contains(\"\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\")", "text2.Contains(\"не синхронизировано\")");
        s = s.Replace("?? \ufffd\ufffd\ufffd 5 \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: \ufffd RAM javaw (PID={0}) \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd: {1} ({2}, \ufffd\ufffd\ufffd\ufffd\ufffd: {3})", "⛔ Пункт 5 регламента: В RAM javaw (PID={0}) обнаружен чит: {1} ({2}, адрес: {3})");
        s = s.Replace("?? \ufffd\ufffd\ufffd 5 \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd javaw: {0} ({1})", "⛔ Пункт 5 регламента: Подозрительный модуль в javaw: {0} ({1})");
        s = s.Replace("h.Contains(\"[\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\") || h.Contains(\"[\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\")", "h.Contains(\"[Удалён\") || h.Contains(\"[Запрещён\") || h.Contains(\"[Чит\")");
        s = s.Replace("?\ufe0f [\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd {0} \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (Task View / Win+Tab)! \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd/\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd Desktop 2.", "⚠️ [Виртуальные рабочие столы] В системе создано {0} рабочих столов (Task View / Win+Tab)! Возможно сокрытие окон читов/инжектора на Desktop 2.");
        s = s.Replace("?\ufe0f \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd 1 \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd (Task View \ufffd\ufffd\ufffd\ufffd).", "ℹ️ В системе активен 1 стандартный рабочий стол (Task View чист).");
        s = s.Replace("?? [\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd {0} (PID {1}): \\\"{2}\\\"", "⚠️ [Подозрительное окно в сессии] Процесс {0} (PID {1}): \\\"{2}\\\"");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd/\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd]: ", "⛔ [5.08 - Служба виртуализации/песочницы]: ");
        s = s.Replace("// \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd", "// Проверка драйверов устройств и служб виртуализации");
        s = s.Replace("// \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd SCSI \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd VM", "// Проверка SCSI дисков на метки VM");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd SCSI \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd]: ", "⛔ [5.08 - Идентификатор SCSI накопителя]: ");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd/\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd]: [", "⛔ [5.08 - Загрузка клинера/утилиты]: [");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd]: [", "⛔ [5.01 - Загрузка чит-клиента]: [");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd)]: [", "⛔ [5.01 - Загрузка мода (аномальный размер)]: [");
        s = s.Replace(" \ufffd\ufffd\ufffd\ufffd, \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: ", " байт, дата: ");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd {0:dd.MM.yyyy HH:mm:ss} ({1:0} \ufffd\ufffd\ufffd. \ufffd\ufffd\ufffd\ufffd\ufffd)! -> {2}", "⚠️ [5.08 - Свежее удаление файла пользователем] Файл попал в корзину {0:dd.MM.yyyy HH:mm:ss} ({1:0} мин. назад)! -> {2}");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd | {0}] [{1}] {2} (\ufffd\ufffd\ufffd\ufffd: {3:dd.MM.yyyy HH:mm:ss})", "⛔ [5.08 - Удаление клинера из корзины | {0}] [{1}] {2} (дата: {3:dd.MM.yyyy HH:mm:ss})");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd] {0} \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd {1:dd.MM.yyyy HH:mm:ss} ({2:0} \ufffd\ufffd\ufffd. \ufffd\ufffd\ufffd\ufffd\ufffd)", "⚠️ [5.08 - Свежее удаление подозрительного файла] {0} удалён {1:dd.MM.yyyy HH:mm:ss} ({2:0} мин. назад)");
        s = s.Replace("rCheatName = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-.jar (\" + fi.Length + \" \ufffd\ufffd\ufffd\ufffd)\";", "rCheatName = \"Аномальный размер чит-.jar (\" + fi.Length + \" байт)\";");
        s = s.Replace("[\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd SRUM] SRUDB.dat: {0:F1} \ufffd\ufffd, \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1:dd.MM.yyyy HH:mm:ss} ({2:F0} \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd)", "[База данных SRUM] SRUDB.dat: {0:F1} МБ, изменена: {1:dd.MM.yyyy HH:mm:ss} ({2:F0} мин назад)");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd SRUM] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd SRUDB.dat (< 100 \ufffd\ufffd) \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd/\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd.", "⛔ [5.08 - Сброс базы SRUM] Подозрительно маленький размер SRUDB.dat (< 100 КБ) — подозрение на затирание логов/активности сети.");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd SRUM] \ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Windows (SRUDB.dat) \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd/\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd: [{0}]", "⛔ [5.01 - Чит в SRUM] В базе учёта ресурсов Windows (SRUDB.dat) обнаружены следы запуска/трафика запрещённого софта: [{0}]");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd SRUM] \ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Windows (SRUDB.dat) \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: [{0}]", "⛔ [5.08 - Запуск клинера в SRUM] В базе учёта ресурсов Windows (SRUDB.dat) зафиксирована активность утилиты: [{0}]");
        s = s.Replace("[SRUDB.dat] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd: ", "[SRUDB.dat] Ошибка считывания записи базы: ");
        s = s.Replace("string timeStr = string.IsNullOrEmpty(timestamp) ? \"\" : (\" | \ufffd\ufffd\ufffd\ufffd\ufffd: \" + timestamp);", "string timeStr = string.IsNullOrEmpty(timestamp) ? \"\" : (\" | время: \" + timestamp);");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd PCA] [{0}] \ufffd\ufffd\ufffd: {1} | \ufffd\ufffd\ufffd\ufffd: {2}{3} | \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {4}", "⛔ [5.01 - Чит в PCA] [{0}] Чит: {1} | Путь: {2}{3} | Статус: {4}");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd PCA] [{0}] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1} | \ufffd\ufffd\ufffd\ufffd: {2}{3} | \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {4}", "⛔ [5.08 - Запуск клинера в PCA] [{0}] Утилита очистки: {1} | Путь: {2}{3} | Статус: {4}");
        s = s.Replace("warnings.Add(\"? Windows DoH (DNS-over-HTTPS) \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (DohFlags=1). \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd.\");", "warnings.Add(\"⚠️ Windows DoH (DNS-over-HTTPS) включён (DohFlags=1). Трафик может шифроваться.\");");
        s = s.Replace("evidence.Add(\"?? Windows DoH (DNS-over-HTTPS) \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (DohFlags=2)! \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DNS \ufffd\ufffd\ufffd Windows \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd)!\");", "evidence.Add(\"⛔ Windows DoH (DNS-over-HTTPS) принудительно включён (DohFlags=2)! Стандартный DNS кэш Windows отключён (попытка скрытия обращений читов)!\");");
        s = s.Replace("warnings.Add(\"? Windows EnableAutoDoh=1: \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DNS-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd.\");", "warnings.Add(\"⚠️ Windows EnableAutoDoh=1: автоматическое шифрование DNS-запросов активно.\");");
        s = s.Replace("evidence.Add(\"?? \ufffd\ufffd\ufffd\ufffd\ufffd 3.6 \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DNS \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (MaxCacheTtl=0)! \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DNS \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd [5.08]!\");", "evidence.Add(\"⛔ Пункт 3.6 регламента: Отключение кэша DNS (MaxCacheTtl=0)! Системный DNS кэш принудительно отключён для скрытия чит-доменов [5.08]!\");");
        s = s.Replace("warnings.Add(\"? MaxNegativeCacheTtl=0: \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DNS-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd.\");", "warnings.Add(\"⚠️ MaxNegativeCacheTtl=0: кэширование отрицательных DNS-ответов отключено.\");");
        s = s.Replace("warnings.Add(\"\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DoH-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (\" + servers.Length + \" \ufffd\ufffd.): \" + string.Join(\", \", servers));", "warnings.Add(\"В системе настроены публичные DoH-серверы (\" + servers.Length + \" шт.): \" + string.Join(\", \", servers));");
        s = s.Replace("warnings.Add(string.Format(\"? \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd/DoH/DPI: {0} (PID: {1}) \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd.\", p.ProcessName, p.Id));", "warnings.Add(string.Format(\"⚠️ Запущен процесс прокси/DoH/DPI: {0} (PID: {1}) — может влиять на логирование сетевых пакетов.\", p.ProcessName, p.Id));");
        s = s.Replace("warnings.Add(\"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Chrome: \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DnsOverHttpsMode='secure' (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DoH).\");", "warnings.Add(\"Браузер Chrome: включён режим DnsOverHttpsMode='secure' (принудительный DoH).\");");
        s = s.Replace("warnings.Add(\"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Edge: \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DnsOverHttpsMode='secure' (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DoH).\");", "warnings.Add(\"Браузер Edge: включён режим DnsOverHttpsMode='secure' (принудительный DoH).\");");
        s = s.Replace("warnings.Add(\"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DoH: \" + ex.Message);", "warnings.Add(\"Ошибка анализа настроек DoH: \" + ex.Message);");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd (Timestomp)] \ufffd\ufffd\ufffd '\" + Path.GetFileName(text) + \"' \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd: \" + fi.LastWriteTime.ToString(\"dd.MM.yyyy HH:mm\") + \" (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd timestomp/\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd [5.08])\"", "⛔ [5.08 - Подделка времени мода (Timestomp)] Мод '\" + Path.GetFileName(text) + \"' имеет неестественную дату: \" + fi.LastWriteTime.ToString(\"dd.MM.yyyy HH:mm\") + \" (признак timestomp/скрытия правки [5.08])\"");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd / \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd] \ufffd\ufffd\ufffd '\" + Path.GetFileName(text) + \"' \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \" + fi.LastWriteTime.ToString(\"dd.MM.yyyy HH:mm\") + \" (Minecraft \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \" + javawStartTime.ToString(\"dd.MM.yyyy HH:mm\") + \")!\"", "⛔ [5.08 - Горячая подмена / добавление мода во время запущенной игры] Мод '\" + Path.GetFileName(text) + \"' создан в \" + fi.LastWriteTime.ToString(\"dd.MM.yyyy HH:mm\") + \" (Minecraft запущен в \" + javawStartTime.ToString(\"dd.MM.yyyy HH:mm\") + \")!\"");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Mixin \ufffd \ufffd\ufffd\ufffd\ufffd \" + Path.GetFileName(text) + \"]", "⛔ [5.01 - Подозрительный класс Mixin в моде \" + Path.GetFileName(text) + \"]");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd \" + Path.GetFileName(text) + \"]", "⛔ [5.01 - Скрытый чит в моде \" + Path.GetFileName(text) + \"]");
        s = s.Replace("checkResult3.Detail = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd: \" + ex.Message;", "checkResult3.Detail = \"Ошибка проверки модов: \" + ex.Message;");
        s = s.Replace("?? [\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd Bloody/Oscar (.amc): ", "ℹ️ [Разрешённый кликер] Конфиг мыши Bloody/Oscar (.amc): ");
        s = s.Replace("?? [\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd\ufffd X-Mouse Button Control (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd).", "ℹ️ [Разрешённый кликер] ПО X-Mouse Button Control (разрешено в соответствии с правилами сервера).");
        s = s.Replace("checkResult2.Detail = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffd\ufffd: \" + ex.Message;", "checkResult2.Detail = \"Ошибка проверки ПО мыши: \" + ex.Message;");
        s = s.Replace("(\" + fi.Length + \" \ufffd\ufffd\ufffd\ufffd)\"", "(\" + fi.Length + \" байт)\"");
        s = s.Replace(" | \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd ({0} \ufffd\ufffd. \ufffd\ufffd\ufffd\ufffd\ufffd, {1:dd.MM.yyyy HH:mm})", " | свежая загрузка ({0} дн. назад, {1:dd.MM.yyyy HH:mm})");
        s = s.Replace(" | \ufffd\ufffd\ufffd\ufffd: {0:dd.MM.yyyy HH:mm}", " | дата: {0:dd.MM.yyyy HH:mm}");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Telegram/\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd\ufffd\ufffd\ufffd: {0} |", "⛔ [5.01 - Чит в кэше Telegram/Discord] Файл: {0} |");
        s = s.Replace("?? [5.01 - DirectInput \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DirectInput \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: '{0}'", "⛔ [5.01 - Чит в DirectInput] В истории DirectInput зафиксирован: '{0}'");
        s = s.Replace("?? [5.01 - Direct3D \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd Direct3D \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd/\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: '{0}' (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1})", "⛔ [5.01 - Direct3D чит-клиент] В Direct3D обнаружен запуск чита/инжектора: '{0}' (совпадение: {1})");
        s = s.Replace("?? [5.08 - IFEO \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd Debugger] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd '{0}' \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: Debugger = '{1}'!", "⛔ [5.08 - IFEO перехват через Debugger] Процесс '{0}' перехвачен отладчиком: Debugger = '{1}'!");
        s = s.Replace("?? [5.08 - IFEO SilentProcessExit \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd] \ufffd\ufffd\ufffd '{0}' \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: MonitorProcess = '{1}'!", "⛔ [5.08 - IFEO SilentProcessExit мониторинг] Для '{0}' настроен скрытый мониторинг при выходе: MonitorProcess = '{1}'!");
        s = s.Replace("checkResult3.Detail = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd DirectX/IFEO: \" + ex.Message;", "checkResult3.Detail = \"Ошибка проверки DirectX/IFEO: \" + ex.Message;");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Amcache] \ufffd\ufffd\ufffd\ufffd Amcache.hve \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd 0 \ufffd\ufffd\ufffd\ufffd (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd)!", "⛔ [5.08 - Очистка Amcache] Файл Amcache.hve имеет размер 0 байт (намеренная зачистка базы программ)!");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Amcache] Amcache.hve \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd ", "⛔ [5.08 - Свежая модификация Amcache] Amcache.hve был модифицирован всего ");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (Uninstall)] {0} (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1}, \ufffd\ufffd\ufffd\ufffd: {2})", "⛔ [5.01 - Деинсталлированный чит-клиент (Uninstall)] {0} (совпадение: {1}, путь: {2})");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd IPC/RPC \ufffd\ufffd\ufffd\ufffd\ufffd javaw.exe (Loopback)]: Minecraft (PID=\" + result + \") \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \" + foreignAddr + \" (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd Vape/Drip/Slinky) [\" + state + \"]\"", "⛔ [5.01 - Подозрительный IPC/RPC сокет javaw.exe (Loopback)]: Minecraft (PID=\" + result + \") подключён к \" + foreignAddr + \" (признак локального интерфейса чита Vape/Drip/Slinky) [\" + state + \"]\"");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd javaw.exe]: Minecraft (PID=\" + result + \") \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \" + foreignAddr + \" (PID: \" + remotePort + \" \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd IRC/Cheat RPC) [\" + state + \"]\"", "⛔ [5.01 - Подозрительный сокет javaw.exe]: Minecraft (PID=\" + result + \") подключён к \" + foreignAddr + \" (порт: \" + remotePort + \" совпадает с IRC/Cheat RPC) [\" + state + \"]\"");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd]: \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \" + procName + \" (PID=\" + result + \") \ufffd\ufffd\ufffd\ufffd\ufffd \" + localAddr + \" -> \" + foreignAddr + \" [\" + state + \"]\"", "⛔ [5.08 - Активное соединение клинера]: Процесс \" + procName + \" (PID=\" + result + \") через \" + localAddr + \" -> \" + foreignAddr + \" [\" + state + \"]\"");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd]: \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \" + procName + \" (PID=\" + result + \") \ufffd\ufffd\ufffd\ufffd\ufffd \" + localAddr + \" -> \" + foreignAddr + \" [\" + state + \"]\"", "⛔ [5.01 - Активное соединение чита]: Процесс \" + procName + \" (PID=\" + result + \") через \" + localAddr + \" -> \" + foreignAddr + \" [\" + state + \"]\"");
        s = s.Replace("?? \ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd javaw (PID={0}): {1} | {2} (\ufffd\ufffd\ufffd\ufffd\ufffd: {3}, \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {4}, \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {5})", "⛔ Чит в памяти javaw (PID={0}): {1} | {2} (адрес: {3}, защита: {4}, размер: {5})");
        s = s.Replace("?? \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd javaw (PID={0}): {1} (\ufffd\ufffd\ufffd\ufffd\ufffd: {2})", "⛔ Подозрительный модуль в javaw (PID={0}): {1} (адрес: {2})");
        s = s.Replace("(result.Severity == CheckSeverity.Critical) ? \"\ufffd\ufffd\ufffd\" : ((result.Severity == CheckSeverity.NeedsCheck) ? \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\" : \"\ufffd\ufffd\ufffd\ufffd\")) : \"\ufffd\ufffd\ufffd\ufffd\ufffd\");", "(result.Severity == CheckSeverity.Critical) ? \"БАН\" : ((result.Severity == CheckSeverity.NeedsCheck) ? \"ВАРНИНГ\" : \"ИНФО\")) : \"ЧИСТО\");");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd AppCompat] \ufffd HKCU AppCompat \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd: '{0}' (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1}, \ufffd\ufffd\ufffd\ufffd: '{2}')", "⛔ [5.01 - Чит в AppCompat] В HKCU AppCompat зафиксирован чит: '{0}' (совпадение: {1}, флаг: '{2}')");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd AppCompat] \ufffd HKLM AppCompat \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd: '{0}' (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1}, \ufffd\ufffd\ufffd\ufffd: '{2}')", "⛔ [5.01 - Чит в AppCompat] В HKLM AppCompat зафиксирован чит: '{0}' (совпадение: {1}, флаг: '{2}')");
        s = s.Replace("checkResult3.Detail = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd AppCompat: \" + ex.Message;", "checkResult3.Detail = \"Ошибка анализа AppCompat: \" + ex.Message;");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd FeatureUsage] \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd: '{0}' (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1}, \ufffd\ufffd\ufffd\ufffd: {2})", "⛔ [5.01 - Чит в FeatureUsage] В переключении приложений зафиксирован чит: '{0}' (совпадение: {1}, ключ: {2})");
        s = s.Replace("errorMessage = \"URL \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\";", "errorMessage = \"URL вебхука не указан\";");
        s = s.Replace("string player = string.IsNullOrEmpty(report.PlayerName) ? \"\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\" : report.PlayerName;", "string player = string.IsNullOrEmpty(report.PlayerName) ? \"Не указан\" : report.PlayerName;");
        s = s.Replace("hitsSb.Append(\"... \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd (\ufffd\ufffd\ufffd\ufffd\ufffd Discord)\\\\n\");", "hitsSb.Append(\"... и другие улики (лимит Discord)\\\\n\");");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd MuiCache] \ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd MuiCache \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd: '{0}' (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1})", "⛔ [5.01 - Чит в MuiCache] В кэше интерфейса MuiCache найден запуск чита: '{0}' (совпадение: {1})");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd RecentDocs MRU] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd: '{0}' (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1}, \ufffd\ufffd\ufffd\ufffd: {2})", "⛔ [5.01 - Чит в RecentDocs MRU] Обращение к исполняемому читу: '{0}' (совпадение: {1}, ключ: {2})");
        s = s.Replace("checkResult3.Detail = \"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd: \" + ex.Message;", "checkResult3.Detail = \"Ошибка сканирования окон: \" + ex.Message;");
        s = s.Replace("?? [5.01 - Java-\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd javaw]: Minecraft \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd -javaagent: ", "⛔ [5.01 - Java-агент в аргументах javaw]: Minecraft запущен с флагом -javaagent: ");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd Bootstrap \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd javaw]: Minecraft \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: ", "⛔ [5.01 - Подмена Bootstrap в аргументах javaw]: Minecraft запущен с подменой системных классов: ");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd javaw]: Minecraft \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd: '{0}' ({1})", "⛔ [5.01 - Чит-флаг в аргументах javaw]: Minecraft запущен с читом: '{0}' ({1})");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd \ufffd BAM] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {0} | \ufffd\ufffd\ufffd: {1} | \ufffd\ufffd\ufffd\ufffd: {2} | \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {3:dd.MM.yyyy HH:mm:ss} ({4:0} \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd)", "⛔ [5.01 - Чит в BAM] Пользователь: {0} | Чит: {1} | Путь: {2} | Запуск: {3:dd.MM.yyyy HH:mm:ss} ({4:0} мин назад)");
        s = s.Replace("?? [5.08 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd BAM] \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {0} | \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {1} | \ufffd\ufffd\ufffd\ufffd: {2} | \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {3:dd.MM.yyyy HH:mm:ss} ({4:0} \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd)", "⛔ [5.08 - Клинер в BAM] Пользователь: {0} | Клинер: {1} | Путь: {2} | Запуск: {3:dd.MM.yyyy HH:mm:ss} ({4:0} мин назад)");
        s = s.Replace("list2.Add(\"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \" + svc.DisplayName + \" (\" + svc.ServiceName + \") \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (Start=4)! \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd.\");", "list2.Add(\"⛔ Служба \" + svc.DisplayName + \" (\" + svc.ServiceName + \") отключена (Start=4)! Возможная попытка сокрытия следов.\");");
        s = s.Replace("list2.Add(\"? \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \" + svc.DisplayName + \" (\" + svc.ServiceName + \") \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd!\");", "list2.Add(\"⚠️ Служба \" + svc.DisplayName + \" (\" + svc.ServiceName + \") остановлена!\");");
        s = s.Replace("list3.Add(\"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \" + svc.DisplayName + \" \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd.\");", "list3.Add(\"ℹ️ Служба \" + svc.DisplayName + \" активна и работает.\");");
        s = s.Replace("list2.Add(\"?? \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (CrashDumpEnabled=0)! \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd.\");", "list2.Add(\"⛔ Дампы памяти отключены в реестре (CrashDumpEnabled=0)! Возможно сокрытие следов сбоев читов.\");");
        s = s.Replace("<div class='header-sub'>\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd, \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd \ufffd\ufffd\ufffd\ufffd (Open Source Framework)</div>", "<div class='header-sub'>Полный криминалистический отчёт системы проверки на запрещённые модификации, инжекты и читы (Community Edition)</div>");
        s = s.Replace("?? \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd)", "⛔ Рекомендуемая команда бана (Регламент)");
        s = s.Replace("this.innerText='\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd!';\">\\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd /ban</button>", "this.innerText='Скопировано!';\">Скопировать /ban</button>");
        s = s.Replace("this.innerText='!';\"> /ban</button>", "this.innerText='Скопировано!';\">Скопировать /ban</button>");
        s = s.Replace("<h3 style='margin:0;'>\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (", "<h3 style='margin:0;'>Результаты проверок (");
        s = s.Replace("\">\\\ufffd\ufffd\ufffd (\"", "\">Все (\"");
        s = s.Replace("\">\ufffd\ufffd\ufffd (\"", "\">Все (\"");
        s = s.Replace("\">\ufffd\ufffd\ufffd\ufffd\ufffd (\"", "\">Улики (\"");
        s = s.Replace("\">?? \ufffd\ufffd\ufffd (\"", "\">⛔ Бан (\"");
        s = s.Replace("placeholder='?? \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd, \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: 5.01, USN, Expensive)...'", "placeholder='🔍 Поиск по уликам, регламенту или совпадению (например: 5.01, USN, Expensive)...'");
        s = s.Replace("string text2 = \"\ufffd\ufffd\ufffd\ufffd\ufffd\";", "string text2 = \"ЧИСТО\";");
        s = s.Replace("text2 = \"\ufffd\ufffd\ufffd [\" + ruleTag + \"]\";", "text2 = \"БАН [\" + ruleTag + \"]\";");
        s = s.Replace("<div style='text-align:center; color:#505064; font-size:12px; margin-top:20px;'>\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd MC Cheat Checker \ufffd Community Edition | \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd</div>", "<div style='text-align:center; color:#505064; font-size:12px; margin-top:20px;'>Криминалистический комплекс MC Cheat Checker · Community Edition | Читов не скрыть</div>");
        s = s.Replace("\ufffd       MC CHEAT CHECKER \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd (Community Edition)         \ufffd", "│       MC CHEAT CHECKER · СВОДКА ПРОВЕРКИ (Community)       │");
        s = s.Replace("  ?? \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: ", "  ⛔ Бан-команда: ");
        s = s.Replace("\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffdСгенерировано MC Cheat Checker v4.0 (Community Edition)", "═════════════════════════════════════════════════════════════\r\nСгенерировано MC Cheat Checker v4.0 (Community Edition)");
        s = s.Replace("len + \" \ufffd\ufffd\ufffd\ufffd\";", "len + \" байт\";");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd/\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd {0}]: {1} ({2})\\n    \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {3}", "⛔ [5.01 - Лицензия/токен чита {0}]: {1} ({2})\\n    Совпадение: {3}");
        s = s.Replace("?? [5.01 - \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd]: \" + text + \" | создан: \" + SafeWrite(text) + \" | создан: \" + SafeLen(text) + \" \ufffd\ufffd\ufffd\ufffd\";", "⛔ [5.01 - Токен платного чита]: \" + text + \" | изменён: \" + SafeWrite(text) + \" | размер: \" + SafeLen(text) + \" байт\";");
        s = s.Replace("?? \ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd-\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: ", "⛔ В деинсталляторах найдена запись платного софта чит-клиента: ");
        s = s.Replace("Evidence = item.FilePath + \"\\r\\n\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: \" + item.GetMatchedStringSummary()", "Evidence = item.FilePath + \"\\r\\nСовпадение: \" + item.GetMatchedStringSummary()");
        s = s.Replace("list2.Add(string.Format(\"\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd {0}: \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd {1} \ufffd\ufffd (\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd: {2} \ufffd\ufffd, \ufffd\ufffd\ufffd\ufffd\ufffd\ufffd\ufffd {3} \ufffd\ufffd) > {4}\", fileName, num, value, Math.Abs(num - value), text2));", "list2.Add(string.Format(\"Версия {0}: размер {1} КБ (размер эталона: {2} КБ, разница {3} КБ) > {4}\", fileName, num, value, Math.Abs(num - value), text2));");
        s = s.Replace("list2.Add(string.Format(\"\ufffd\ufffd\ufffd: {0} | UUID: {1} ({2}\\\\usercache.json)\", name, uuid, Path.GetFileName(mcDir)));", "list2.Add(string.Format(\"Ник: {0} | UUID: {1} ({2}\\\\usercache.json)\", name, uuid, Path.GetFileName(mcDir)));");

        return s;
    }
}
