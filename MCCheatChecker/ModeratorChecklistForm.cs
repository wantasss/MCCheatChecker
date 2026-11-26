using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class ModeratorChecklistForm : Form
	{
		private class CheckStep
		{
			public int Number;
			public string Title;
			public string Description;
			public string ActionButtonText;
			public Action ActionCallback;
			public bool IsChecked;
			public CheckBox CkBox;
			public Label StatusLabel;
		}

		private string targetPlayerName;
		private string targetModeratorName;
		private Action onOpenModsAction;
		private Action onOpenCpsAction;
		private Action onRunRamScanAction;
		private Action onRunBuiltinSearchAction;

		private List<CheckStep> steps = new List<CheckStep>();
		private Label lblProgressText;
		private Panel pnlProgressBarFill;
		private Panel pnlProgressBarBg;

		public ModeratorChecklistForm(string player, string moderator,
			Action openMods = null, Action openCps = null, Action runRamScan = null, Action runBuiltinSearch = null)
		{
			targetPlayerName = string.IsNullOrEmpty(player) ? "Player" : player;
			targetModeratorName = string.IsNullOrEmpty(moderator) ? Environment.UserName : moderator;
			onOpenModsAction = openMods;
			onOpenCpsAction = openCps;
			onRunRamScanAction = runRamScan;
			onRunBuiltinSearchAction = runBuiltinSearch;

			InitializeSteps();
			InitializeComponent();
			UpdateProgress();
		}

		private void InitializeSteps()
		{
			steps.Add(new CheckStep
			{
				Number = 1,
				Title = "Вызов на проверку и 5-минутный таймер",
				Description = "Отправить игроку официальное уведомление в чат сервера с таймером 5 минут и требованием контактов AnyDesk/Discord.",
				ActionButtonText = "📢 Вызов в чат",
				ActionCallback = delegate
				{
					string text = string.Format("[ПРОВЕРКА] Игрок {0}, вы вызваны на проверку Screenshare! Предоставьте AnyDesk/Discord модератору {1}. У вас есть 5 минут! Выход/очистка = Бан 5.08 (30 дней).",
						targetPlayerName, targetModeratorName);
					Clipboard.SetText(text);
					MessageBox.Show("Текст вызова на проверку скопирован в буфер обмена!\nВставьте его в чат сервера.\n\n" + text, "Вызов на проверку", MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			});

			steps.Add(new CheckStep
			{
				Number = 2,
				Title = "Сверка AnyDesk / RuDesktop и целостности Windows",
				Description = "Проверить службы (DPS, SysMain, EventLog), скрутку времени и очистку логов (Регламент 5.08).",
				ActionButtonText = "🛡️ Целостность 5.08",
				ActionCallback = delegate
				{
					try
					{
						AntiForensicsViewerForm afForm = new AntiForensicsViewerForm(targetPlayerName);
						afForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Анти-криминалистика", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 3,
				Title = "Аудит папок .minecraft и инспекция структуры JAR",
				Description = "Проверить модификации, скрытые классы и пул строковых констант байткода модов.",
				ActionButtonText = "📦 Инспектор JAR",
				ActionCallback = delegate
				{
					try
					{
						JarStructureViewerForm jarForm = new JarStructureViewerForm(null, targetPlayerName);
						jarForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Инспектор JAR", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 4,
				Title = "Поиск поддельных модов (Fake Mods) и маскировок",
				Description = "Глубокая проверка несоответствия заголовков (Magic Bytes), подмен легитимных модов (Sodium/Iris) и скрытых исполняемых файлов.",
				ActionButtonText = "🎭 Маскировки (Ctrl+G)",
				ActionCallback = delegate
				{
					try
					{
						DisguisedFilesViewerForm dForm = new DisguisedFilesViewerForm(targetPlayerName);
						dForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Маскировки", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 5,
				Title = "Проверка ShellBags и истории открытия папок",
				Description = "Аудит истории открытия папок в ShellBags (включая удалённые директории и каталоги на флешках).",
				ActionButtonText = "📁 ShellBags (Ctrl+B)",
				ActionCallback = delegate
				{
					try
					{
						ShellbagsViewerForm sForm = new ShellbagsViewerForm(targetPlayerName);
						sForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "ShellBags", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 6,
				Title = "Аудит индексов корзины $Recycle.Bin ($I / $R) и экстренных удалений",
				Description = "Декодирование оригинальных имен удаленных файлов и времени отправки в корзину. Выявление выброса чита в корзину при вызове (5.08).",
				ActionButtonText = "🗑️ Корзина $I / $R",
				ActionCallback = delegate
				{
					try
					{
						RecycleBinForensicsViewerForm rbForm = new RecycleBinForensicsViewerForm(targetPlayerName);
						rbForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Корзина $Recycle.Bin", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 7,
				Title = "Инспектор ярлыков Recent (.lnk) и Jump Lists (AutomaticDestinations)",
				Description = "Аудит истории запуска файлов, удаленных читов, запусков со съемных USB-флешек и списков Jump Lists.",
				ActionButtonText = "🔗 Ярлыки / JumpLists",
				ActionCallback = delegate
				{
					try
					{
						JumpListLnkViewerForm jForm = new JumpListLnkViewerForm(targetPlayerName);
						jForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Recent / JumpLists", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 8,
				Title = "Аудит истории открытия RecentDocs (.jar/.exe) и журнала BAM ядра",
				Description = "Извлечение истории RecentDocs реестра и неуничтожимого лога ядра Background Activity Moderator (BAM/DAM) на удаленные читы (5.08).",
				ActionButtonText = "📂 RecentDocs & BAM (F12)",
				ActionCallback = delegate
				{
					try
					{
						AmcacheRecentDocsViewerForm amForm = new AmcacheRecentDocsViewerForm(targetPlayerName);
						amForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "RecentDocs & BAM", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 9,
				Title = "Аудит USB-накопителей, USBSTOR и запусков с извлеченных флешек",
				Description = "Проверить историю подключений съемных USB-дисков и серийных номеров. Выявить запуски чита с флешки, извлеченной перед проверкой (5.08/5.01).",
				ActionButtonText = "💾 Флешки USBSTOR",
				ActionCallback = delegate
				{
					try
					{
						UsbDeviceForensicsViewerForm usbForm = new UsbDeviceForensicsViewerForm(targetPlayerName);
						usbForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "USB-накопители", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 34,
				Title = "Аудит виртуальных дисков (VHDX/ISO), VeraCrypt и размонтирования (5.08)",
				Description = "Проверка журнала VHDMP (Event ID 1/2), томов MountedDevices и драйверов контейнеров на извлечение образов перед проверкой.",
				ActionButtonText = "💽 Образы VHD/ISO (5.08)",
				ActionCallback = delegate
				{
					try
					{
						VirtualDisksAndSandboxViewerForm vdForm = new VirtualDisksAndSandboxViewerForm(targetPlayerName);
						vdForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Виртуальные диски", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 10,
				Title = "Аудит удалений и изменений NTFS USN Journal",
				Description = "Трассировка системного журнала NTFS на удаление читов перед проверкой, переименования файлов и затирание улик.",
				ActionButtonText = "🗑️ Журнал USN (Ctrl+T)",
				ActionCallback = delegate
				{
					try
					{
						UsnJournalViewerForm uForm = new UsnJournalViewerForm(targetPlayerName);
						uForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "USN Journal", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 11,
				Title = "Аудит системных журналов Windows Event Log (1102/104/7045/1000/1116)",
				Description = "Проверка журналов System, Security, Application и Defender на очистку логов, скрутку времени, краши читов и установку драйверов.",
				ActionButtonText = "🛡️ Event Log (Ctrl+W)",
				ActionCallback = delegate
				{
					try
					{
						WindowsEventLogViewerForm evForm = new WindowsEventLogViewerForm(targetPlayerName);
						evForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Event Log", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 12,
				Title = "Аудит ядерных драйверов инжекторов и служб Windows",
				Description = "Поиск драйверов инжекции памяти (Cheat Engine, Process Hacker, Xenos) и аудит отключения служб DPS/SysMain/EventLog (5.08).",
				ActionButtonText = "⚙️ Драйверы и службы",
				ActionCallback = delegate
				{
					try
					{
						KernelDriversServicesViewerForm drvForm = new KernelDriversServicesViewerForm(targetPlayerName);
						drvForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Драйверы и службы", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 13,
				Title = "Проверка запусков Prefetch (.pf) и истории до 8 запусков",
				Description = "Глубокий анализ папки Prefetch (MAM/SCCA), счётчиков запусков, 8-launch таймлайна и загруженных в javaw сторонних DLL.",
				ActionButtonText = "⚡ Prefetch (Ctrl+E)",
				ActionCallback = delegate
				{
					try
					{
						PrefetchViewerForm pForm = new PrefetchViewerForm(targetPlayerName);
						pForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Prefetch", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 14,
				Title = "Аудит системного кэша ядра AppCompatCache (ShimCache)",
				Description = "Проверка реестра Session Manager\\AppCompatCache: до 1024 запущенных файлов, включая запуск с извлеченных USB-флешек и удалённые .exe.",
				ActionButtonText = "🗄️ ShimCache (Ctrl+Y)",
				ActionCallback = delegate
				{
					try
					{
						ShimcacheViewerForm scForm = new ShimcacheViewerForm(targetPlayerName);
						scForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "ShimCache", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 15,
				Title = "Аудит истории запусков Shell (UserAssist, MuiCache, PCA Store)",
				Description = "Анализ счетчиков запусков, времени активности окна чита, MuiCache и удаленных файлов в реестре Explorer.",
				ActionButtonText = "🖥️ UserAssist / Shell (F10)",
				ActionCallback = delegate
				{
					try
					{
						UserAssistMuiCacheViewerForm uForm = new UserAssistMuiCacheViewerForm(targetPlayerName);
						uForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "UserAssist / Shell", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 16,
				Title = "Аудит загрузок браузеров, Discord и Telegram",
				Description = "Извлечение истории загрузок из Chrome/Edge/Opera/Yandex, файлов из Telegram Desktop и вложений Discord на факт скачивания читов (5.08/5.01).",
				ActionButtonText = "🌐 Загрузки софта",
				ActionCallback = delegate
				{
					try
					{
						BrowserMessengerDownloadsViewerForm dlForm = new BrowserMessengerDownloadsViewerForm(targetPlayerName);
						dlForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 35,
				Title = "Аудит поисковых запросов Google/Yandex и сайтов читов",
				Description = "Аудит поисков читов, батников очистки (5.08), переходов на vape.gg, celestial.lol и зачистки истории браузера.",
				ActionButtonText = "🔍 Поиски Google/Яндекс",
				ActionCallback = delegate
				{
					try
					{
						BrowserSearchAndHistoryViewerForm bForm = new BrowserSearchAndHistoryViewerForm(targetPlayerName);
						bForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Веб-история", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 36,
				Title = "Форензика фоновых загрузок BITS и передачи файлов",
				Description = "Аудит службы BITS (Event ID 59/60), заданий bitsadmin и qmgr.db на скачивание софта через PowerShell.",
				ActionButtonText = "📥 Фоновые BITS",
				ActionCallback = delegate
				{
					try
					{
						BitsTransferForensicsViewerForm bitsForm = new BitsTransferForensicsViewerForm(targetPlayerName);
						bitsForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Фоновые загрузки BITS", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 17,
				Title = "Хронология активности и история PowerShell",
				Description = "Сводный хронологический таймлайн событий и проверка файла ConsoleHost_history на скрипты очистки.",
				ActionButtonText = "📜 Таймлайн & PS",
				ActionCallback = delegate
				{
					try
					{
						UserActivityTimelineForm uForm = new UserActivityTimelineForm();
						uForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Таймлайн", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 18,
				Title = "Быстрый охотник за файлами читов (Fast Cheat Hunter)",
				Description = "Сканирование по 350+ именам читов, 140 размерам JAR, поиск .bat/.cmd скриптов очистки и запуск Everything.",
				ActionButtonText = "🔍 Поиск (Ctrl+Q)",
				ActionCallback = delegate
				{
					try
					{
						new FastCheatHunterForm(targetPlayerName).Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Поиск читов", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 19,
				Title = "Инспектор дескрипторов и инжекций в память javaw.exe",
				Description = "Аудит открытых дескрипторов ядра к Minecraft (VM_WRITE/VM_READ/AllAccess), unbacked потоков шеллкода и инжектированных DLL.",
				ActionButtonText = "🔗 Дескрипторы (Ctrl+H)",
				ActionCallback = delegate
				{
					try
					{
						ProcessHandlesViewerForm hForm = new ProcessHandlesViewerForm(targetPlayerName);
						hForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Дескрипторы", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 20,
				Title = "Инспектор оперативной памяти RAM javaw.exe",
				Description = "Глубокое сканирование виртуальной памяти javaw на инжекции PE (MZ) и сигнатуры чит-клиентов.",
				ActionButtonText = "⚡ Инспектор RAM",
				ActionCallback = delegate
				{
					try
					{
						JavawRamViewerForm ramForm = new JavawRamViewerForm(targetPlayerName);
						ramForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Инспектор RAM", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 21,
				Title = "Аудит аварийных дампов CrashDumps и отчетов об ошибках WER",
				Description = "Анализ дампов сбоев чит-клиентов (.dmp), крашей javaw.exe и отчетов Report.wer на удаленный софт.",
				ActionButtonText = "💥 Сбои и WER (F9)",
				ActionCallback = delegate
				{
					try
					{
						CrashDumpsAndWerInspectorForm cForm = new CrashDumpsAndWerInspectorForm(targetPlayerName);
						cForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "CrashDumps & WER", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 22,
				Title = "Монитор активного лога latest.log",
				Description = "Проверить активный или архивный лог игры на команды привязки (.bind, .vape, #goto) и кики античита.",
				ActionButtonText = "📜 Открыть лог",
				ActionCallback = delegate
				{
					try
					{
						MinecraftLogViewerForm logForm = new MinecraftLogViewerForm();
						logForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Монитор логов", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 23,
				Title = "Аудит ПО мыши, макросов и замер CPS",
				Description = "Аудит драйверов мыши (Logitech Lua, Bloody Oscar, Razer, дебаунс Glorious) и замер CPS кликов.",
				ActionButtonText = "🖱️ Аудит мыши",
				ActionCallback = delegate
				{
					try
					{
						PeripheralMacroForensicsForm pForm = new PeripheralMacroForensicsForm();
						pForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Аудит мыши", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 24,
				Title = "Сетевые обходы, Hosts, DNS-over-HTTPS (DoH) и туннели",
				Description = "Аудит файла hosts на перенаправление лицензий читов и серверов авторизации, проверка DoH в браузерах и VPN-адаптеров.",
				ActionButtonText = "🌐 Hosts & DoH (F11)",
				ActionCallback = delegate
				{
					try
					{
						NetworkBypassViewerForm netForm = new NetworkBypassViewerForm(targetPlayerName);
						netForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Сетевой аудит", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 25,
				Title = "Сетевые сокеты и зондирование Web-GUI читов",
				Description = "Проверить активные TCP/UDP сокеты, localhost IPC порты и отправить зонды для детекта скрытых Web-панелей чита (Vape/Drip/Slinky).",
				ActionButtonText = "🔌 Сокеты (Ctrl+N)",
				ActionCallback = delegate
				{
					try
					{
						ActiveConnectionsViewerForm connForm = new ActiveConnectionsViewerForm(targetPlayerName);
						connForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Сетевые сокеты", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 37,
				Title = "Аудит журнала DNS-клиента (Event 3008) и резолвинга чит-доменов",
				Description = "Аудит истории резолвинга доменов читов с привязкой к PID процессов. Не стирается командой ipconfig /flushdns.",
				ActionButtonText = "🌐 Журнал DNS (Event 3008)",
				ActionCallback = delegate
				{
					try
					{
						DnsClientLogViewerForm dForm = new DnsClientLogViewerForm(targetPlayerName);
						dForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Журнал DNS", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 26,
				Title = "Аудит буфера обмена, консоли PowerShell и диалога 'Выполнить' (RunMRU)",
				Description = "Поиск команд сокрытия (wevtutil cl, fsutil usn deletejournal, Set-MpPreference), bat-клинеров, ссылок pastebin и ключей чита.",
				ActionButtonText = "📋 Буфер и PowerShell",
				ActionCallback = delegate
				{
					try
					{
						ClipboardConsoleHistoryViewerForm cForm = new ClipboardConsoleHistoryViewerForm(targetPlayerName);
						cForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Буфер и консоль", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

									steps.Add(new CheckStep
			{
				Number = 27,
				Title = "Аудит задач планировщика (Tasks) и реестра автозагрузки",
				Description = "Проверка запланированных задач на скрипты очистки следов и скрытых параметров реестра Run/Startup.",
				ActionButtonText = "⏱️ Задачи & Run",
				ActionCallback = delegate
				{
					try
					{
						ScheduledTasksStartupViewerForm stForm = new ScheduledTasksStartupViewerForm(targetPlayerName);
						stForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Планировщик и автозагрузка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 28,
				Title = "Форензика базы уведомлений Action Center и Defender (wpndatabase.db)",
				Description = "Аудит скрытых алертов антивируса о троянах/читах и переписок в wpndatabase.db через теневой поток.",
				ActionButtonText = "🔔 База уведомлений",
				ActionCallback = delegate
				{
					try
					{
						WindowsNotificationForensicsViewerForm notifForm = new WindowsNotificationForensicsViewerForm(targetPlayerName);
						notifForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "База уведомлений", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 29,
				Title = "Аудит поисков Проводника (WordWheelQuery) и путей TypedPaths",
				Description = "Декодирование постоянной истории поиска файлов и введенных путей в адресную строку с проверкой удаления.",
				ActionButtonText = "🔍 Поиски Explorer",
				ActionCallback = delegate
				{
					try
					{
						ExplorerSearchesTypedPathsViewerForm expForm = new ExplorerSearchesTypedPathsViewerForm(targetPlayerName);
						expForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Поиски Проводника", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 30,
				Title = "Аудит исключений Windows Defender и политик безопасности",
				Description = "Проверка белых списков путей/процессов в Defender и детекция удаленных папок читов (5.08).",
				ActionButtonText = "🛡️ Исключения Defender",
				ActionCallback = delegate
				{
					try
					{
						DefenderExclusionsViewerForm defForm = new DefenderExclusionsViewerForm(targetPlayerName);
						defForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Исключения Defender", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 31,
				Title = "Аудит ресурспаков на скрытый X-Ray (stone.json) и sounds.json",
				Description = "Проверка .zip архивов на прозрачные модели руд, X-Ray шейдеры и заглушение шагов Silent-Step.",
				ActionButtonText = "💎 Ресурспаки & X-Ray",
				ActionCallback = delegate
				{
					try
					{
						ResourcePacksXrayViewerForm rpForm = new ResourcePacksXrayViewerForm(targetPlayerName);
						rpForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Ресурспаки и X-Ray", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 32,
				Title = "Форензика кэша шейдеров GPU / DirectX (D3DSCache & DXCache)",
				Description = "Аудит кэшей GPU на ImGui оверлеи читов, сигнатуры Celestial/Expensive и зачистку кэша перед проверкой.",
				ActionButtonText = "🎮 Кэш шейдеров GPU",
				ActionCallback = delegate
				{
					try
					{
						DirectXShaderCacheViewerForm dxForm = new DirectXShaderCacheViewerForm(targetPlayerName);
						dxForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Кэш шейдеров DirectX", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 33,
				Title = "Фиксация доказательств (SHA-256) и итоговый вердикт бана",
				Description = "Создать заверенный снимок экрана со штампом времени и SHA-256 хэшем для форума/апелляции, сверить регламент и скопировать команду бана.",
				ActionButtonText = "📸 Фиксация (SHA-256)",
				ActionCallback = delegate
				{
					try
					{
						EvidenceCaptureForm evForm = new EvidenceCaptureForm(targetPlayerName, targetModeratorName);
						evForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Фиксация доказательств", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 38,
				Title = "Аудит альтернативных потоков NTFS (ADS) и меток Zone.Identifier",
				Description = "Проверка скрытых потоков данных (ADS) и меток безопасности Zone.Identifier на факт загрузки читов из сети.",
				ActionButtonText = "📎 Потоки ADS",
				ActionCallback = delegate
				{
					try
					{
						NtfsAlternateDataStreamsViewerForm adsForm = new NtfsAlternateDataStreamsViewerForm(targetPlayerName);
						adsForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Потоки ADS", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 39,
				Title = "Формирование и экспорт официального судебного досье ScreenShare (HTML/PDF)",
				Description = "Сформировать итоговое экспертное криминалистическое досье с таблицей доказательств, гарантией белого списка и печатью в PDF.",
				ActionButtonText = "📑 Итоговое Досье",
				ActionCallback = delegate
				{
					try
					{
						ForensicDossierReportGeneratorForm dosForm = new ForensicDossierReportGeneratorForm(targetPlayerName, targetModeratorName);
						dosForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Судебное досье", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 40,
				Title = "Аудит базы запусков PCA (PcaAppLaunchDic.txt) и удаленных бинарников",
				Description = "Проверка журнала Program Compatibility Assistant и реестра AppCompatFlags на запуски чита и сокрытия.",
				ActionButtonText = "🚀 База PCA",
				ActionCallback = delegate
				{
					try
					{
						PcaAppLaunchForensicsViewerForm pcaForm = new PcaAppLaunchForensicsViewerForm(targetPlayerName);
						pcaForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "База PCA", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 41,
				Title = "Глубокий поиск клинеров, батников очистки и сброса журналов (5.08)",
				Description = "Поиск скриптов зачистки (.bat/.ps1), утилит сокрытия и сброса журналов EventLog/Prefetch перед проверкой.",
				ActionButtonText = "🧹 Клинеры (5.08)",
				ActionCallback = delegate
				{
					try
					{
						CleanersAndWipingTracesViewerForm clForm = new CleanersAndWipingTracesViewerForm(targetPlayerName);
						clForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Клинеры 5.08", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 42,
				Title = "Проверка цифровых подписей Authenticode, сертификатов и целостности JAR",
				Description = "Аудит цифровых сертификатов DLL/EXE и структуры META-INF модов на предмет поддельных чит-подписей.",
				ActionButtonText = "🔏 Подписи & JAR",
				ActionCallback = delegate
				{
					try
					{
						AuthenticodeSignaturesViewerForm signForm = new AuthenticodeSignaturesViewerForm(targetPlayerName);
						signForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Подписи Authenticode", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 43,
				Title = "Аудит теневых копий тома (VSS), точек восстановления и улик зачистки 5.08",
				Description = "Проверка наличия снимков тома VSS и выявление очистки теневых копий (Event ID 22, vssadmin delete).",
				ActionButtonText = "💾 Снимки VSS (5.08)",
				ActionCallback = delegate
				{
					try
					{
						VolumeShadowCopiesViewerForm vssForm = new VolumeShadowCopiesViewerForm(targetPlayerName);
						vssForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Теневые копии VSS", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 44,
				Title = "Форензика кэша эскизов Thumbcache и извлечение удаленных изображений",
				Description = "Аудит баз thumbcache_*.db Explorer, карвинг JPEG/PNG миниатюр удаленных файлов и выявление зачистки кэша (5.08).",
				ActionButtonText = "🖼️ Кэш эскизов",
				ActionCallback = delegate
				{
					try
					{
						ThumbcacheAndIconCacheViewerForm thumbForm = new ThumbcacheAndIconCacheViewerForm(targetPlayerName);
						thumbForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Кэш эскизов", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 45,
				Title = "Аудит сетевых туннелей (Wintun/WireGuard) и корневых сертификатов перехвата (5.01)",
				Description = "Поиск сертификатов снифферов (Fiddler/Charles) в Root CA, проверка активных TUN/TAP интерфейсов и WinINet прокси.",
				ActionButtonText = "🌐 Туннели & CA",
				ActionCallback = delegate
				{
					try
					{
						NetworkTunnelsAndCertificatesViewerForm netForm = new NetworkTunnelsAndCertificatesViewerForm(targetPlayerName);
						netForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Сетевые туннели", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 46,
				Title = "Форензика базы Windows Timeline и журнала активностей CDP (5.01 / 5.08)",
				Description = "Аудит базы ActivitiesCache.db: фиксация запусков чит-клиентов, времени активного фокуса и удаленных исполняемых файлов.",
				ActionButtonText = "⏳ Timeline CDP",
				ActionCallback = delegate
				{
					try
					{
						WindowsTimelineAndActivitiesViewerForm tlForm = new WindowsTimelineAndActivitiesViewerForm(targetPlayerName);
						tlForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Windows Timeline", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 47,
				Title = "Инспектор лаунчеров Minecraft, профилей версий и аккаунтов (5.01 / 5.02 / 5.08)",
				Description = "Аудит .minecraft, TLauncher, Prism, Lunar, Feather: выявление чит-версий (mainClass), аргументов -javaagent, твинков и зачищенных логов.",
				ActionButtonText = "🚀 Лаунчеры & Профили",
				ActionCallback = delegate
				{
					try
					{
						MinecraftLaunchersAndProfilesViewerForm lForm = new MinecraftLaunchersAndProfilesViewerForm(targetPlayerName);
						lForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Лаунчеры Minecraft", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 48,
				Title = "Инспектор аппаратных идентификаторов (HWID), детекции спуферов и подмены MAC (5.02 / 5.08)",
				Description = "Аудит MachineGuid, SMBIOS UUID, серийников дисков/BIOS, детекция Locally Administered MAC (TMAC), утилит VolumeID и спуферов обхода бана.",
				ActionButtonText = "🔍 HWID & Спуферы",
				ActionCallback = delegate
				{
					try
					{
						HwidAndHardwareSpoofingViewerForm hwidForm = new HwidAndHardwareSpoofingViewerForm(targetPlayerName);
						hwidForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "HWID & Спуферы", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 49,
				Title = "Инспектор расширений браузеров, скриптов Tampermonkey и Web-панелей читов (5.01 / 5.02)",
				Description = "Аудит Chrome, Edge, Opera, Yandex, Brave, Firefox: поиск юзерскриптов Tampermonkey, Web-GUI сессий (Vape/Drip) в LocalStorage и закладок читов.",
				ActionButtonText = "🌐 Web-панели & Скрипты",
				ActionCallback = delegate
				{
					try
					{
						BrowserExtensionsAndWebCheatPanelsViewerForm bForm = new BrowserExtensionsAndWebCheatPanelsViewerForm(targetPlayerName);
						bForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Web-панели читов", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 50,
				Title = "Форензика .minecraft\\config, ботов Baritone и остаточных конфигов читов (5.01 / 5.08 / 2.06)",
				Description = "Аудит .minecraft\\config и multi-instance папок: поиск осиротевших конфигов удаленных читов, ботов Baritone и эксплойтов options.txt (Gamma 1000).",
				ActionButtonText = "📁 Конфиги & Baritone",
				ActionCallback = delegate
				{
					try
					{
						MinecraftConfigsAndBaritoneForensicsViewerForm cfgForm = new MinecraftConfigsAndBaritoneForensicsViewerForm(targetPlayerName);
						cfgForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Конфигурации Minecraft", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 51,
				Title = "Инспектор скриншотов Minecraft (F2) и видеозаписей GameDVR (5.01 / 5.08)",
				Description = "Аудит папок скриншотов .minecraft, Lunar, Feather и видео GameDVR: визуальный просмотр галереи на следы ESP/читов и поиск замаскированных JAR/EXE под PNG.",
				ActionButtonText = "📸 Скриншоты & GameDVR",
				ActionCallback = delegate
				{
					try
					{
						MinecraftScreenshotsAndGameDvrViewerForm scForm = new MinecraftScreenshotsAndGameDvrViewerForm(targetPlayerName);
						scForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Скриншоты Minecraft", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 52,
				Title = "Инспектор блоков кода PowerShell ScriptBlock (Event ID 4104 / 5.01 / 5.08)",
				Description = "Аудит журнала PowerShell/Operational: обнаружение зачисток следов перед ScreenShare, загрузчиков читов и авто-декодирование Base64 команд.",
				ActionButtonText = "📜 ScriptBlock 4104",
				ActionCallback = delegate
				{
					try
					{
						PowerShellScriptBlock4104ViewerForm psForm = new PowerShellScriptBlock4104ViewerForm(targetPlayerName);
						psForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "PowerShell 4104", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 53,
				Title = "Инспектор диалогов ComDlg32 (OpenSavePidlMRU и LastVisitedPidlMRU / 5.01 / 5.08)",
				Description = "Аудит истории системных диалогов открытия/сохранения файлов: дешифрование бинарных PIDL, проверка наличия файлов на диске и фиксация удаления читов перед проверкой.",
				ActionButtonText = "📁 Диалоги ComDlg32",
				ActionCallback = delegate
				{
					try
					{
						OpenSaveAndLastVisitedPidlViewerForm dlgForm = new OpenSaveAndLastVisitedPidlViewerForm(targetPlayerName);
						dlgForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Диалоги ComDlg32", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 54,
				Title = "Инспектор сна, скачков времени и перезагрузок (Event 1/41/42/107/1074/6008 / 5.04 / 5.08)",
				Description = "Аудит событий питания и таймлайна: выявление панического перевода ПК в сон перед ScreenShare, изменения системного времени (Time Jump) и аварийных сбросов питания (Hard Reset).",
				ActionButtonText = "⏱️ Сон, Время & Ребуты",
				ActionCallback = delegate
				{
					try
					{
						SleepWakeTimeJumpAndRebootViewerForm pwrForm = new SleepWakeTimeJumpAndRebootViewerForm(targetPlayerName);
						pwrForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "События сна и времени", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 55,
				Title = "РРЅСЃРїРµРєС‚РѕСЂ JVM РёРЅСЉРµРєС†РёР№, РїСЂРѕРєСЃРё DLL Рё РїРµСЂРµРјРµРЅРЅС‹С… РѕРєСЂСѓР¶РµРЅРёСЏ (5.01 / 5.08)",
				Description = "РђСѓРґРёС‚ СЃСЂРµРґ Java (JRE/JDK): РїРѕРёСЃРє Р±РёР±Р»РёРѕС‚РµРє-РїРµСЂРµС…РІР°С‚С‡РёРєРѕРІ dwrite.dll, version.dll, opengl32.dll РІ bin\\, СЃРєСЂС‹С‚С‹С… JavaAgent РІ _JAVA_OPTIONS, СѓРґР°Р»РµРЅРЅС‹С… Р°РіРµРЅС‚РѕРІ РІ РїСЂРѕС„РёР»СЏС… Р»Р°СѓРЅС‡РµСЂР° Рё Р¶РёРІС‹С… proxy DLL РІ javaw.exe.",
				ActionButtonText = "в• JVM & РџСЂРѕРєСЃРё DLL",
				ActionCallback = delegate
				{
					try
					{
						JvmInjectionAndProxyDllViewerForm jvmForm = new JvmInjectionAndProxyDllViewerForm(targetPlayerName);
						jvmForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "JVM РРЅСЉРµРєС†РёРё Рё РџСЂРѕРєСЃРё DLL", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 56,
				Title = "Инспектор именованных каналов Named Pipes и IPC (5.01)",
				Description = "Аудит активных системных каналов \\\\.\\pipe\\: поиск скрытых каналов связи инжекторов с игрой (Vape, Drip, Slinky, Celestial), анонимных GUID и детекция соединений с javaw.exe.",
				ActionButtonText = "🚰 Named Pipes & IPC",
				ActionCallback = delegate
				{
					try
					{
						NamedPipesAndIpcEndpointsViewerForm pipeForm = new NamedPipesAndIpcEndpointsViewerForm(targetPlayerName);
						pipeForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Named Pipes IPC", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 57,
				Title = "Инспектор следов запуска SACP, BAM и FeatureUsage (5.01 / 5.08)",
				Description = "Аудит баз Compatibility Assistant (SACP Store), Background Activity Moderator (BAM), FeatureUsage и AppCompat: выявление удаленных файлов перед ScreenShare (5.08) и подтверждение запусков читов (5.01).",
				ActionButtonText = "🛡️ SACP, BAM & Запуски",
				ActionCallback = delegate
				{
					try
					{
						BamAndAppCompatStoreViewerForm bamForm = new BamAndAppCompatStoreViewerForm(targetPlayerName);
						bamForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "SACP и BAM", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 58,
				Title = "Инспектор сокрытия экрана DWM, OBS/Discord Bypass и оверлеев (5.08 / 5.01)",
				Description = "Аудит окон HWND и вызовов SetWindowDisplayAffinity: поиск сокрытия читов от захвата экрана Discord/AnyDesk (WDA_EXCLUDEFROMCAPTURE), внешних ESP оверлеев поверх игры и фальшивых DiscordHook DLL в javaw.exe.",
				ActionButtonText = "🪟 DWM & Оверлеи",
				ActionCallback = delegate
				{
					try
					{
						ScreenCaptureEvasionAndOverlayViewerForm overlayForm = new ScreenCaptureEvasionAndOverlayViewerForm(targetPlayerName);
						overlayForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "DWM & Оверлеи", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 59,
				Title = "Инспектор истории команд Minecraft и чат-логов (5.01 / 5.08)",
				Description = "Аудит command_history.txt во всех профилях и лаунчерах: выявление чит-префиксов (., #, -, !, @), ботов Baritone и фактов обнуления файла истории перед вызовом на ScreenShare.",
				ActionButtonText = "📜 История команд",
				ActionCallback = delegate
				{
					try
					{
						MinecraftCommandHistoryAndChatLogsViewerForm cmdForm = new MinecraftCommandHistoryAndChatLogsViewerForm(targetPlayerName);
						cmdForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "История команд", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 60,
				Title = "Инспектор стеганографии в модах и ZIP EOCD (5.01 / 5.08)",
				Description = "Аудит структуры всех .jar модов: поиск скрытых данных за пределами EOCD (PK\x05\x06), внедренных PE (.exe/.dll) файлов и скрытых вторичных архивов с читами.",
				ActionButtonText = "📦 ZIP Стеганография",
				ActionCallback = delegate
				{
					try
					{
						ZipSteganographyAndModPayloadViewerForm zipForm = new ZipSteganographyAndModPayloadViewerForm(targetPlayerName);
						zipForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "ZIP Стеганография", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 61,
				Title = "Инспектор хронологии подключений USB SetupAPI (5.08 / 5.01)",
				Description = "Аудит setupapi.dev.log: точный поминутный таймлайн подключения флешек перед вызовом на ScreenShare и детекция аппаратных чит-контроллеров KMBox/Arduino.",
				ActionButtonText = "🔌 USB SetupAPI",
				ActionCallback = delegate
				{
					try
					{
						SetupApiUsbTimelineViewerForm usbForm = new SetupApiUsbTimelineViewerForm(targetPlayerName);
						usbForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "SetupAPI USB", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 62,
				Title = "Инспектор аварийных логов JVM HotSpot и дампов сбоев (5.01 / 5.08)",
				Description = "Анализ файлов hs_err_pid*.log, replay_pid*.log: выявление следов внедрения читов (Vape, Celestial, Expensive, Nursultan), инжектированных нативных DLL библиотек и искусственных крашей для сокрытия улик.",
				ActionButtonText = "🔥 Логи JVM HotSpot",
				ActionCallback = delegate
				{
					try
					{
						JvmCrashLogsAndHotspotDumpsViewerForm jvmForm = new JvmCrashLogsAndHotspotDumpsViewerForm(targetPlayerName);
						jvmForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "JVM HotSpot Логи", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 63,
				Title = "Глубокий анализатор шейдерпаков на скрытый X-Ray (5.01)",
				Description = "Синтаксический разбор шейдеров .vsh/.fsh/.glsl: выявление сокрытия руды и блоков через GLSL discard, маскировки под BSL/Complementary/Continuum и прозрачности непрозрачных блоков.",
				ActionButtonText = "🔮 X-Ray Шейдеры",
				ActionCallback = delegate
				{
					try
					{
						DeepShaderpackXrayViewerForm xrayForm = new DeepShaderpackXrayViewerForm(targetPlayerName);
						xrayForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "X-Ray Шейдеры", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 64,
				Title = "Детектор полого внедрения процессов и неразмеченной памяти (5.01 / 5.08)",
				Description = "Аудит виртуальной памяти системных процессов: поиск исполняемых страниц без DLL на диске, заголовков MZ в куче и сигнатур читов (Process Hollowing).",
				ActionButtonText = "🧬 Process Hollowing",
				ActionCallback = delegate
				{
					try
					{
						ProcessHollowingAndInjectedMemoryViewerForm hollowingForm = new ProcessHollowingAndInjectedMemoryViewerForm(targetPlayerName);
						hollowingForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Process Hollowing", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 65,
				Title = "Инспектор подмены ПК, удаленных сессий и AnyDesk профилей (5.08)",
				Description = "Проверка на предоставление модератору чужого/чистого ПК, рассинхронизацию AnyDesk ID, параллельные удаленные сессии чистильщика (ad.trace/RDP) и виртуальные машины.",
				ActionButtonText = "🖥️ Подмена ПК / AnyDesk",
				ActionCallback = delegate
				{
					try
					{
						FakeComputerAndRemoteAnyDeskViewerForm fakePcForm = new FakeComputerAndRemoteAnyDeskViewerForm(targetPlayerName);
						fakePcForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Подмена ПК и AnyDesk", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 66,
				Title = "Инспектор обхода проверок, сокрытия читов и анти-форензики (5.08 / 5.01)",
				Description = "Аудит монтирования и извлечения съёмных USB/RAM дисков (MountPoints2), базы буфера обмена (%LocalAppData%\\Microsoft\\Windows\\Clipboard\\history.db) и следов самоликвидации (fsutil, wevtutil, vssadmin).",
				ActionButtonText = "🥷 Обход и сокрытие",
				ActionCallback = delegate
				{
					try
					{
						AntiEvasionAndStealthBypassViewerForm evasionForm = new AntiEvasionAndStealthBypassViewerForm(targetPlayerName);
						evasionForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Обход проверок и скрытие софта", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 67,
				Title = "Инспектор гипервизоров, виртуальных машин, песочниц и Stream-Proof сокрытия (5.08 / 5.01)",
				Description = "Глубокая экспертиза изоляции среды: виртуальные машины (VMware, VirtualBox, Hyper-V, KVM), песочницы Sandboxie/Windows Sandbox, скрытие окон от записи WDA_EXCLUDEFROMCAPTURE, виртуальные мониторы IddSample и DBVM.",
				ActionButtonText = "🛡️ Гипервизоры и VM",
				ActionCallback = delegate
				{
					try
					{
						HypervisorVmAndSandboxEvasionViewerForm vmForm = new HypervisorVmAndSandboxEvasionViewerForm(targetPlayerName);
						vmForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Гипервизоры и VM", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 68,
				Title = "Инспектор Discord, Rich Presence и RPC криминалистики (5.01 / 5.08)",
				Description = "Глубокая экспертиза Discord: проверка кэша LevelDB на следы Rich Presence читов (Vape, Celestial, Expensive, Nursultan), аудит каналов discord-ipc-* и плагинов сокрытия экрана в Vencord/BetterDiscord/Vesktop.",
				ActionButtonText = "💬 Discord & RPC",
				ActionCallback = delegate
				{
					try
					{
						DiscordRichPresenceAndRpcForensicsViewerForm discordForm = new DiscordRichPresenceAndRpcForensicsViewerForm(targetPlayerName);
						discordForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Discord & RPC", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 69,
				Title = "Инспектор патчинга памяти, обхода AMSI и ETW (5.01 / 5.08)",
				Description = "Аудит оперативной памяти javaw.exe и лаунчеров: детекция байт-патчей AmsiScanBuffer (C3, 31 C0 C3, B8 57..), подавления событий безопасности ETW (EtwEventWrite/NtTraceEvent), модификаций секций .text и нативных перехватов MinHook/Detours.",
				ActionButtonText = "💉 Патчинг памяти",
				ActionCallback = delegate
				{
					try
					{
						AmsiEtwAndPatchingDetectorViewerForm amsiForm = new AmsiEtwAndPatchingDetectorViewerForm(targetPlayerName);
						amsiForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Патчинг памяти, AMSI и ETW", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 70,
				Title = "Инспектор скриншотов, EXIF метаданных и вотермарок читов (5.01 / 5.08)",
				Description = "Глубокая экспертиза скриншотов: проверка чанков PNG/EXIF на графические редакторы (Photoshop, Paint.NET, GIMP), поиск вотермарок читов (Vape, Celestial, Expensive) и манипуляций датами.",
				ActionButtonText = "🖼️ Скриншоты и EXIF",
				ActionCallback = delegate
				{
					try
					{
						MinecraftScreenshotsExifAndOcrForensicsViewerForm scForm = new MinecraftScreenshotsExifAndOcrForensicsViewerForm(targetPlayerName);
						scForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Скриншоты и EXIF", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 71,
				Title = "Инспектор мыши, дебаунса и аппаратных макросов (2.06 / 5.01 / 5.08)",
				Description = "Аппаратный аудит HID-мышей: проверка дебаунса Glorious/Roccat (багоюз 2.06 при < 4 мс), скриптов Bloody Oscar и режимов Ultra-Core 3/4 (5.01), драйверов перехвата кликов Interception (5.08) и макросов вендоров.",
				ActionButtonText = "🖱️ Мышь и макросы",
				ActionCallback = delegate
				{
					try
					{
						HardwareMouseAndPollingRateAuditViewerForm mouseForm = new HardwareMouseAndPollingRateAuditViewerForm(targetPlayerName);
						mouseForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Мышь и макросы", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 73,
				Title = "Инспектор драйверов ядра, BYOVD эксплойтов и TestSigning (5.01 / 5.08)",
				Description = "Глубокая экспертиза ядра: аудит загруженных модулей ядра EnumDeviceDrivers, детекция уязвимых BYOVD драйверов (gdrv, mhyprot2, RTCore64, dbk64), следов kdmapper/kdu (Event ID 7045), параметров TestSigning и бесфайловых руткитов.",
				ActionButtonText = "🛡️ Драйверы ядра",
				ActionCallback = delegate
				{
					try
					{
						KernelDriversAndHypervisorHooksViewerForm kernelForm = new KernelDriversAndHypervisorHooksViewerForm(targetPlayerName);
						kernelForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Драйверы ядра и TestSigning", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 72,
				Title = "Инспектор X-Ray ресурспаков, моделей CEM и невидимых шрифтов (5.01 / 5.08)",
				Description = "Глубокая экспертиза ресурспаков: анализ прозрачных текстур камня/породы (X-Ray), проверка увеличения хитбоксов в моделях OptiFine CEM и сокрытия чит-модулей нулевыми шрифтами.",
				ActionButtonText = "💎 X-Ray и Ресурспаки",
				ActionCallback = delegate
				{
					try
					{
						FontAndTexturePackBypassViewerForm rpForm = new FontAndTexturePackBypassViewerForm(targetPlayerName);
						rpForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "X-Ray Ресурспаки", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 74,
				Title = "Инспектор аномалий системного времени и перевода часов (5.08)",
				Description = "Глубокая экспертиза системного времени: аудит Kernel-General Event ID 1 (откат часов назад > 5 мин -> 5.08), Event ID 12/13/41 (перезагрузка/выключение перед проверкой), служб w32time и файлов из будущего (Time-Travel).",
				ActionButtonText = "⏱️ Аномалии времени",
				ActionCallback = delegate
				{
					try
					{
						TimeAnomalyAndSystemClockTamperingViewerForm timeForm = new TimeAnomalyAndSystemClockTamperingViewerForm(targetPlayerName);
						timeForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Аномалии системного времени", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 75,
				Title = "Инспектор истории буфера обмена Win+V и базы history.db (5.01 / 5.08)",
				Description = "Глубокая экспертиза базы буфера обмена %LocalAppData%\\Microsoft\\Windows\\Clipboard\\history.db: поиск токенов/ключей читов (5.01), команд клинеров (fsutil, wevtutil, vssadmin) и детекция зачистки/обнуления базы (5.08).",
				ActionButtonText = "📋 Буфер и history.db",
				ActionCallback = delegate
				{
					try
					{
						ClipboardWinVAndHistoryDatabaseViewerForm clipForm = new ClipboardWinVAndHistoryDatabaseViewerForm(targetPlayerName);
						clipForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Буфер обмена и history.db", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 76,
				Title = "Инспектор фальсификации обновлений Windows, UsoClient и срыва проверок (5.08)",
				Description = "Глубокая экспертиза экранов обновления: детекция браузерных симуляций fakeupdate.net (5.08), фиктивных перезагрузок usoclient RestartDevice, отключения wuauserv и расхождений с CBS.log.",
				ActionButtonText = "🔄 Обновления Windows",
				ActionCallback = delegate
				{
					try
					{
						UsoAndWindowsUpdateTamperingViewerForm usoForm = new UsoAndWindowsUpdateTamperingViewerForm(targetPlayerName);
						usoForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Обновления Windows и UsoClient", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 77,
				Title = "Инспектор виртуальных аудиокабелей и стриминга (5.08)",
				Description = "Судебная экспертиза аудиоустройств и драйверов: аудит VB-Audio Cable, VoiceMeeter, VAC, Parsec Audio, стримеров Sunshine/Moonlight и детекция скрытых каналов подсказок третьих лиц (5.08).",
				ActionButtonText = "🎙️ Аудиокабели 5.08",
				ActionCallback = delegate
				{
					try
					{
						RemoteStreamingAndVirtualAudioDriverViewerForm audioForm = new RemoteStreamingAndVirtualAudioDriverViewerForm(targetPlayerName);
						audioForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Виртуальные аудиокабели и стриминг", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 78,
				Title = "Инспектор Direct3D 11, OpenGL VTable Hooks и ESP оверлеев (5.01 / 5.08)",
				Description = "Судебная экспертиза рендеринга и перехватов: аудит пролога wglSwapBuffers/Present на JMP-трамплины (ESP/Chams 5.01), подмены оверлейных DLL и внешних прозрачных клик-тру окон.",
				ActionButtonText = "🎮 D3D/OpenGL Hooks",
				ActionCallback = delegate
				{
					try
					{
						Direct3dD3d11HookAndEspOverlayViewerForm d3dForm = new Direct3dD3d11HookAndEspOverlayViewerForm(targetPlayerName);
						d3dForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Direct3D/OpenGL Hooks & ESP", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 79,
				Title = "Инспектор сетевых туннелей, VPN и прокси (5.08)",
				Description = "Судебная экспертиза сетевого трафика и уклонений: аудит туннелей Ngrok/Cloudflared/Frp, VPN адаптеров (Wintun/WireGuard/TAP), локальных прокси (V2Ray/Tor) и подмены hosts (5.08).",
				ActionButtonText = "🌐 Сеть и прокси 5.08",
				ActionCallback = delegate
				{
					try
					{
						LocalTunnelsVpnAndProxyBypassViewerForm tunnelForm = new LocalTunnelsVpnAndProxyBypassViewerForm(targetPlayerName);
						tunnelForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Сетевые туннели и прокси", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 80,
				Title = "Инспектор Java Agent и аргументов JVM (5.01)",
				Description = "Судебная экспертиза аргументов JVM: аудит параметров -javaagent:, -agentpath:, -Xbootclasspath:, поиск чит-агентов в Temp/AppData и проверка переменных JAVA_OPTIONS (5.01).",
				ActionButtonText = "☕ Java Agent 5.01",
				ActionCallback = delegate
				{
					try
					{
						JavaAgentAndJvmFlagsViewerForm agentForm = new JavaAgentAndJvmFlagsViewerForm(targetPlayerName);
						agentForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Java Agent и JVM Flags", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 81,
				Title = "Инспектор Mixin & mod.json архивов модов (5.01 / 5.08)",
				Description = "Глубокий парсинг .jar модов, fabric.mod.json, mcmod.info и Mixin JSON: аудит target-классов (ClientPlayerEntity, EntityRenderer), проверка белого списка CPVP «Атлантис» (Marlow/Haram/Walksy) и маскировки модов (5.01/5.08).",
				ActionButtonText = "🧬 Mixin & mod.json",
				ActionCallback = delegate
				{
					try
					{
						MixinAndModJsonForensicsViewerForm mixinForm = new MixinAndModJsonForensicsViewerForm(targetPlayerName);
						mixinForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Mixin & mod.json", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 82,
				Title = "Единый судебно-криминалистический таймлайн (5.01 / 5.08)",
				Description = "Сводный хронологический анализ всех артефактов ОС: Prefetch, BAM/SACP реестра, Event Logs (1074/7045/1102), Recent LNK и Shellbags в единой временной шкале проверок ScreenShare.",
				ActionButtonText = "⏳ Единый таймлайн",
				ActionCallback = delegate
				{
					try
					{
						UnifiedForensicTimelineViewerForm timelineForm = new UnifiedForensicTimelineViewerForm(targetPlayerName);
						timelineForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Единый таймлайн", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 83,
				Title = "Инспектор настроек и биндов Minecraft (options.txt / 5.01 / 5.03)",
				Description = "Глубокий парсинг options.txt, keybinds.txt и конфигураций модов: аудит чит-привязок (killaura, fly, speed, xray -> 5.01), макросов автокликера (5.03) с учетом белого списка команд и CPVP «Атлантис».",
				ActionButtonText = "🎮 Бинды Minecraft (Ctrl+Alt+O)",
				ActionCallback = delegate
				{
					try
					{
						MinecraftOptionsAndKeybindsViewerForm kbForm = new MinecraftOptionsAndKeybindsViewerForm(targetPlayerName);
						kbForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Настройки и бинды Minecraft", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 84,
				Title = "Инспектор взлома сида мира и ChunkBase локаторов (5.06)",
				Description = "Судебная экспертиза сида мира: поиск модов SeedCrackerX/SeedMapper, утилит Cubiomes/Amidst и глубокий аудит истории 6 веб-браузеров на посещение онлайн-локаторов ChunkBase/MineAtlas по регламенту 5.06.",
				ActionButtonText = "🌍 Сид & ChunkBase (Ctrl+Alt+W)",
				ActionCallback = delegate
				{
					try
					{
						WorldSeedAndChunkbaseForensicsViewerForm seedForm = new WorldSeedAndChunkbaseForensicsViewerForm(targetPlayerName);
						seedForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Взлом сида мира и ChunkBase", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 85,
				Title = "Инспектор памяти System Informer / Process Hacker 2 (5.01 / 5.08)",
				Description = "Нативная автоматизация мануала System Informer: поиск сигнатур читов в ОЗУ javaw.exe (мин. длина 4), модули DLL (пустой Description + веса 1.42-1.89 МБ), InjectDllHistory, explorer.exe (pcaclient, file:///), DcomLaunch/PcaSvc (.jar), DPS Replace и csrss.exe.",
				ActionButtonText = "🔬 System Informer (Ctrl+Alt+8)",
				ActionCallback = delegate
				{
					try
					{
						SystemInformerAndProcessHackerForensicsViewerForm siForm = new SystemInformerAndProcessHackerForensicsViewerForm(targetPlayerName);
						siForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "System Informer / Process Hacker 2", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});

			steps.Add(new CheckStep
			{
				Number = 86,
				Title = "Инспектор Luyten REGEX MODS, HxD DLL HEX, BAM Unsigned и SystemDLC (5.01 / 5.08)",
				Description = "Высокоуровневая криминалистика: Luyten REGEX MODS (Forge/Fabric/LabyMod), Hex-аудит DLL (пустой Description и переименования), BAM Not Signed Only, Event Logs & Registry (очистка логов / AppCompatCache) и глубокое обнаружение SystemDLC.",
				ActionButtonText = "🧬 Luyten / HxD / SystemDLC (Ctrl+Alt+9)",
				ActionCallback = delegate
				{
					try
					{
						LuytenHxdAndHighLevelForensicsViewerForm hlForm = new LuytenHxdAndHighLevelForensicsViewerForm(targetPlayerName);
						hlForm.Show(this);
					}
					catch (Exception ex)
					{
						MessageBox.Show(ex.Message, "Luyten / HxD / BAM / SystemDLC", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			});
		}

		private void InitializeComponent()
		{
			Text = "📋 Чек-лист модератора — Протокол проверки ScreenShare";
			Size = new Size(1000, 720);
			MinimumSize = new Size(820, 520);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(8.5f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 85;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(14, 10, 14, 6);
			topPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, topPanel.Height - 1, topPanel.Width, topPanel.Height - 1);
				}
			};

			Label lblTitle = new Label();
			lblTitle.Text = "📋  ПРОТОКОЛ И ПОШАГОВЫЙ ЧЕК-ЛИСТ SCREENSHARE ПРОВЕРКИ";
			lblTitle.Font = Ui.FontTitle(10.5f, true);
			lblTitle.ForeColor = Ui.AccentTeal;
			lblTitle.Location = new Point(14, 8);
			lblTitle.AutoSize = true;
			topPanel.Controls.Add(lblTitle);

			Label lblSub = new Label();
			lblSub.Text = string.Format("Игрок: {0}  |  Проверяющий модератор: {1}  |  Регламент: Раздел 5 (5.01 - 5.08)",
				targetPlayerName, targetModeratorName);
			lblSub.Font = Ui.FontBody(8.5f, true);
			lblSub.ForeColor = Ui.AccentBlue;
			lblSub.Location = new Point(14, 32);
			lblSub.AutoSize = true;
			topPanel.Controls.Add(lblSub);

			lblProgressText = new Label();
			lblProgressText.Text = "Прогресс: 0 / 11 (0%)";
			lblProgressText.Font = Ui.FontBody(8.5f, true);
			lblProgressText.ForeColor = Ui.AccentOrange;
			lblProgressText.Location = new Point(14, 55);
			lblProgressText.AutoSize = true;
			topPanel.Controls.Add(lblProgressText);

			pnlProgressBarBg = new Panel();
			pnlProgressBarBg.Location = new Point(180, 58);
			pnlProgressBarBg.Size = new Size(780, 12);
			pnlProgressBarBg.BackColor = Ui.SurfaceAlt;
			pnlProgressBarBg.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

			pnlProgressBarFill = new Panel();
			pnlProgressBarFill.Location = new Point(0, 0);
			pnlProgressBarFill.Size = new Size(0, 12);
			pnlProgressBarFill.BackColor = Ui.AccentGreen;
			pnlProgressBarBg.Controls.Add(pnlProgressBarFill);

			topPanel.Controls.Add(pnlProgressBarBg);
			Controls.Add(topPanel);

			Panel scrollPanel = new Panel();
			scrollPanel.Dock = DockStyle.Fill;
			scrollPanel.AutoScroll = true;
			scrollPanel.BackColor = Ui.Backdrop;
			scrollPanel.Padding = new Padding(14, 10, 14, 10);

			int curY = 10;
			foreach (var step in steps)
			{
				Panel itemPnl = new Panel();
				itemPnl.Location = new Point(14, curY);
				itemPnl.Size = new Size(940, 52);
				itemPnl.BackColor = Ui.Surface;
				itemPnl.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
				itemPnl.Paint += delegate(object s, PaintEventArgs e)
				{
					using (Pen pen = new Pen(Ui.Border))
					{
						e.Graphics.DrawRectangle(pen, 0, 0, itemPnl.Width - 1, itemPnl.Height - 1);
					}
				};

				CheckBox chk = new CheckBox();
				chk.Location = new Point(12, 16);
				chk.Size = new Size(20, 20);
				chk.Cursor = Cursors.Hand;
				step.CkBox = chk;

				Label lblNum = new Label();
				lblNum.Text = string.Format("{0}. {1}", step.Number, step.Title);
				lblNum.Font = Ui.FontBody(9f, true);
				lblNum.ForeColor = Ui.TextPrimary;
				lblNum.Location = new Point(36, 6);
				lblNum.AutoSize = true;

				Label lblDesc = new Label();
				lblDesc.Text = step.Description;
				lblDesc.Font = Ui.FontBody(8f);
				lblDesc.ForeColor = Ui.TextMuted;
				lblDesc.Location = new Point(36, 28);
				lblDesc.Size = new Size(650, 18);
				lblDesc.AutoEllipsis = true;

				Label lblSt = new Label();
				lblSt.Text = "⚪ Не проверено";
				lblSt.Font = Ui.FontBody(8.5f, true);
				lblSt.ForeColor = Ui.TextSecondary;
				lblSt.Location = new Point(700, 16);
				lblSt.AutoSize = true;
				step.StatusLabel = lblSt;

				chk.CheckedChanged += delegate
				{
					step.IsChecked = chk.Checked;
					if (step.IsChecked)
					{
						lblSt.Text = "🟢 Выполнено";
						lblSt.ForeColor = Ui.AccentGreen;
						lblNum.ForeColor = Ui.AccentTeal;
					}
					else
					{
						lblSt.Text = "⚪ Не проверено";
						lblSt.ForeColor = Ui.TextSecondary;
						lblNum.ForeColor = Ui.TextPrimary;
					}
					UpdateProgress();
				};

				DarkButton actBtn = new DarkButton();
				actBtn.Text = step.ActionButtonText;
				actBtn.Size = new Size(130, 28);
				actBtn.Location = new Point(800, 11);
				actBtn.BackColor = Ui.SurfaceAlt;
				actBtn.ForeColor = Ui.TextPrimary;
				actBtn.Font = Ui.FontBody(8f, true);
				actBtn.Cursor = Cursors.Hand;
				actBtn.Anchor = AnchorStyles.Top | AnchorStyles.Right;
				actBtn.Click += delegate
				{
					if (step.ActionCallback != null) step.ActionCallback();
					chk.Checked = true;
				};

				itemPnl.Controls.Add(chk);
				itemPnl.Controls.Add(lblNum);
				itemPnl.Controls.Add(lblDesc);
				itemPnl.Controls.Add(lblSt);
				itemPnl.Controls.Add(actBtn);

				scrollPanel.Controls.Add(itemPnl);
				curY += 58;
			}

			Controls.Add(scrollPanel);
			scrollPanel.BringToFront();

			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 44;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(12, 6, 12, 6);
			bottomPanel.Paint += delegate(object s, PaintEventArgs e)
			{
				using (Pen pen = new Pen(Ui.Border))
				{
					e.Graphics.DrawLine(pen, 0, 0, bottomPanel.Width, 0);
				}
			};

			DarkButton btnCopyProtocol = new DarkButton();
			btnCopyProtocol.Text = "📋 Скопировать протокол проверки";
			btnCopyProtocol.Size = new Size(260, 28);
			btnCopyProtocol.Location = new Point(14, 8);
			btnCopyProtocol.BackColor = Color.FromArgb(0, 121, 107);
			btnCopyProtocol.ForeColor = Color.White;
			btnCopyProtocol.Font = Ui.FontBody(8.5f, true);
			btnCopyProtocol.Cursor = Cursors.Hand;
			btnCopyProtocol.Click += delegate
			{
				CopyProtocolToClipboard();
			};
			bottomPanel.Controls.Add(btnCopyProtocol);

			DarkButton btnCheckAll = new DarkButton();
			btnCheckAll.Text = "✔ Отметить все";
			btnCheckAll.Size = new Size(140, 28);
			btnCheckAll.Location = new Point(284, 8);
			btnCheckAll.BackColor = Ui.SurfaceAlt;
			btnCheckAll.ForeColor = Ui.TextPrimary;
			btnCheckAll.Cursor = Cursors.Hand;
			btnCheckAll.Click += delegate
			{
				foreach (var st in steps) if (st.CkBox != null) st.CkBox.Checked = true;
			};
			bottomPanel.Controls.Add(btnCheckAll);

			DarkButton btnReset = new DarkButton();
			btnReset.Text = "🔄 Сброс";
			btnReset.Size = new Size(110, 28);
			btnReset.Location = new Point(434, 8);
			btnReset.BackColor = Ui.SurfaceAlt;
			btnReset.ForeColor = Ui.TextPrimary;
			btnReset.Cursor = Cursors.Hand;
			btnReset.Click += delegate
			{
				foreach (var st in steps) if (st.CkBox != null) st.CkBox.Checked = false;
			};
			bottomPanel.Controls.Add(btnReset);

			DarkButton btnClose = new DarkButton();
			btnClose.Text = "Закрыть";
			btnClose.Size = new Size(110, 28);
			btnClose.Location = new Point(860, 8);
			btnClose.BackColor = Ui.SurfaceAlt;
			btnClose.ForeColor = Ui.TextPrimary;
			btnClose.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnClose.DialogResult = DialogResult.OK;
			bottomPanel.Controls.Add(btnClose);

			Controls.Add(bottomPanel);
			bottomPanel.BringToFront();
		}

		private void UpdateProgress()
		{
			int checkedCount = steps.Count(s => s.IsChecked);
			int total = steps.Count;
			int pct = (total > 0) ? (checkedCount * 100 / total) : 0;

			lblProgressText.Text = string.Format("Прогресс: {0} / {1} ({2}%)", checkedCount, total, pct);
			lblProgressText.ForeColor = (pct == 100) ? Ui.AccentGreen : ((pct >= 50) ? Ui.AccentTeal : Ui.AccentOrange);

			if (pnlProgressBarBg != null && pnlProgressBarFill != null)
			{
				int fillW = (pnlProgressBarBg.Width * pct) / 100;
				pnlProgressBarFill.Width = fillW;
				pnlProgressBarFill.BackColor = (pct == 100) ? Ui.AccentGreen : Ui.AccentTeal;
			}
		}

		private void CopyProtocolToClipboard()
		{
			int checkedCount = steps.Count(s => s.IsChecked);
			int total = steps.Count;
			int pct = (total > 0) ? (checkedCount * 100 / total) : 0;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== ПРОТОКОЛ SCREENSHARE ПРОВЕРКИ ИГРОКА ===");
			sb.AppendLine(string.Format("👤 Игрок: {0}", targetPlayerName));
			sb.AppendLine(string.Format("🛡️ Модератор: {0}", targetModeratorName));
			sb.AppendLine(string.Format("⏰ Дата и время: {0:dd.MM.yyyy HH:mm:ss}", DateTime.Now));
			sb.AppendLine(string.Format("📊 Выполнение чек-листа: {0} из {1} ({2}%)", checkedCount, total, pct));
			sb.AppendLine(new string('-', 60));

			foreach (var step in steps)
			{
				string mark = step.IsChecked ? "[✔ ВЫПОЛНЕНО]  " : "[❌ НЕ ПРОВЕРЕНО]";
				sb.AppendLine(string.Format("{0} {1,2}. {2}", mark, step.Number, step.Title));
			}

			sb.AppendLine(new string('-', 60));
			sb.AppendLine(string.Format("⚖️ Итоговый статус: {0}", (pct == 100) ? "Проверка завершена полностью" : "Частичная проверка"));

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Протокол проверки (" + checkedCount + "/" + total + ") скопирован в буфер обмена!", "Чек-лист модератора", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		public static List<string> CheckSystemTimeTampering()
		{
			List<string> issues = new List<string>();
			try
			{
				using (EventLog el = new EventLog("System"))
				{
					DateTime cutoff = DateTime.Now.AddHours(-12);
					int count = el.Entries.Count;
					int scanned = 0;
					for (int i = count - 1; i >= 0 && scanned < 1000; i--, scanned++)
					{
						try
						{
							EventLogEntry entry = el.Entries[i];
							if (entry.TimeGenerated < cutoff) break;
							if (entry.InstanceId == 1 && entry.Source.IndexOf("Kernel-General", StringComparison.OrdinalIgnoreCase) >= 0)
							{
								issues.Add(string.Format("• [{0:dd.MM.yyyy HH:mm:ss}] Изменение системного времени (EventID 1): {1}",
									entry.TimeGenerated, entry.Message.Replace("\r", " ").Replace("\n", " ")));
							}
						}
						catch { }
					}
				}
			}
			catch { }

			try
			{
				DateTime nowUtc = DateTime.UtcNow.AddMinutes(3);
				string[] scanDirs = new string[]
				{
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
					"C:\\Windows\\Prefetch"
				};
				foreach (string d in scanDirs)
				{
					if (!Directory.Exists(d)) continue;
					try
					{
						foreach (string f in Directory.GetFiles(d, "*.*", SearchOption.TopDirectoryOnly))
						{
							FileInfo fi = new FileInfo(f);
							if (fi.LastWriteTimeUtc > nowUtc)
							{
								issues.Add(string.Format("• [ФАЙЛ ИЗ БУДУЩЕГО] {0} (дата: {1:dd.MM.yyyy HH:mm:ss}, сейчас: {2:dd.MM.yyyy HH:mm:ss})",
									Path.GetFileName(f), fi.LastWriteTime, DateTime.Now));
							}
						}
					}
					catch { }
				}
			}
			catch { }

			return issues;
		}
	}
}
