$path = 'C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\ForensicHubForm.cs'
$txt = [System.IO.File]::ReadAllText($path, [System.Text.Encoding]::UTF8)

# Update subtitle and category button count
$txt = $txt.Replace('по 40 криминалистическим анализаторам', 'по 42 криминалистическим анализаторам')
$txt = $txt.Replace('Все (40)', 'Все (42)')

# Tool insertion anchor
$anchor = 'LaunchAction = delegate { try { new ExpressTriageForm(targetPlayerName, targetModeratorName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Экспресс-триаж", MessageBoxButtons.OK, MessageBoxIcon.Error); } }' + "`r`n`t`t`t});"

$newTools = @"

			allTools.Add(new ForensicToolItem
			{
				Name = "📎 Форензика альтернативных потоков NTFS (ADS) и меток Zone.Identifier",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Shift+Z",
				TargetRule = "5.01 / 5.08 (Потоки ADS)",
				Description = "Аудит скрытых потоков NTFS ADS и меток Zone.Identifier: обнаружение маскировки читов в скрытых потоках и выявление загрузок чит-клиентов с внешних доменов.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new NtfsAlternateDataStreamsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Потоки ADS", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📑 Генератор итогового судебного досье ScreenShare (HTML/PDF)",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Shift+O",
				TargetRule = "Официальное досье проверки",
				Description = "Формирование всеобъемлющего экспертного криминалистического досье проверки: живой HTML-предпросмотр, матрица улик, гарантия белого списка кликеров и экспорт в PDF.",
				AccentColor = Ui.AccentPurple,
				LaunchAction = delegate { try { new ForensicDossierReportGeneratorForm(targetPlayerName, targetModeratorName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Судебное досье", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});
"@

if ($txt.Contains($anchor)) {
    $txt = $txt.Replace($anchor, $anchor + $newTools)
    [System.IO.File]::WriteAllText($path, $txt, [System.Text.Encoding]::UTF8)
    Write-Output "SUCCESS"
} else {
    Write-Output "ANCHOR NOT FOUND"
}
