# MCCheatChecker — Community Edition

Открытая утилита криминалистического анализа и проверки клиентов Minecraft (ScreenShare) для администраторов и модераторов игровых серверов.

## Особенности
* **Чистый код:** 0 ошибок компиляции под .NET Framework 4.8.
* **Автономность:** мгновенный запуск без сторонних серверов и сбора данных.
* **Приватность (Zero Disk Footprint):** анализ выполняется в оперативной памяти, не оставляет логов и следов.
* **Поддержка High-DPI (Windows 10/11):** корректное отображение на любых мониторах.
* **Поддержка популярных лаунчеров:** .minecraft, Modrinth, Prism Launcher, MultiMC, ATLauncher, Lunar, Feather, Badlion, TLauncher.
* **Актуальная база проверок:** распознавание чит-клиентов и запрещенных модов (5.01–5.08).
* **Анализ запущенных процессов:** сканирование памяти `javaw.exe` и `java.exe`.

## Структура проекта
* `MCCheatChecker/Program.cs` — точка входа в приложение.
* `MCCheatChecker/MainForm.cs` — главное окно интерфейса и результаты проверки.
* `MCCheatChecker/CheatDetector.cs` — ядро автоматических тестов и форензики.
* `MCCheatChecker/ManualChecks.cs` — криминалистика реестра, USN Journal, Prefetch, BAM.
* `MCCheatChecker/CheatDatabases.cs` — базы сигнатур читов и белые списки.
* `MCCheatChecker/RuleBook.cs` — официальные пункты правил серверов (5.01–5.08).
* `MCCheatChecker/ProcessMemoryScanner.cs` — инспекция запущенных процессов игры.

## Сборка
Запустите `build.bat` из корня проекта. Сборка выполняется через стандартный `csc.exe` (входит в состав Windows).
Готовый бинарник будет скомпилирован в `MCCheatChecker\bin\MCCheatChecker.exe`.

## Лицензия
Распространяется по лицензии [MIT](LICENSE). Подробнее об использовании читайте в [DISCLAIMER.md](DISCLAIMER.md).
