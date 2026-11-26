using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class ForensicHubForm : Form
	{
		#region Models

		public enum HubCategory
		{
			All,
			MemoryAndProcesses,
			FileSystemAndRegistry,
			RecycleAndStorage,
			NetworkAndDownloads,
			RulesAndEvidence
		}

		public class ForensicToolItem
		{
			public string Name;
			public string CategoryName;
			public HubCategory Category;
			public string Hotkey;
			public string TargetRule;
			public string Description;
			public Action LaunchAction;
			public Color AccentColor;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private string targetModeratorName;
		private HubCategory currentCategory = HubCategory.All;
		private List<ForensicToolItem> allTools = new List<ForensicToolItem>();

		private DarkListView listView;
		private DarkTextBox searchBox;
		private Label lblHeaderTitle;
		private Label lblHeaderSubtitle;
		private Label lblPlayerBadge;

		private Label lblDetailName;
		private Label lblDetailCategory;
		private Label lblDetailRule;
		private Label lblDetailHotkey;
		private Label lblDetailDescription;

		private DarkButton btnLaunchSelected;
		private DarkButton btnQuickChecklist;
		private DarkButton btnQuickEvidence;
		private DarkButton btnQuickRulebook;
		private DarkButton btnQuickClipboard;

		private DarkButton btnCatAll;
		private DarkButton btnCatMemory;
		private DarkButton btnCatFileSystem;
		private DarkButton btnCatRecycle;
		private DarkButton btnCatNetwork;
		private DarkButton btnCatRules;

		#endregion

		#region UI Initialization

		public ForensicHubForm(string playerName = "Player", string moderatorName = "")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			targetModeratorName = string.IsNullOrEmpty(moderatorName) ? Environment.UserName : moderatorName;

			InitializeToolsCatalog();
			InitializeComponent();
			ApplyFilter();
		}

		private void InitializeToolsCatalog()
		{
			// 1. Память, Инжекторы и Процессы
			allTools.Add(new ForensicToolItem
			{
				Name = "⚡ Сканер оперативной памяти javaw.exe",
				CategoryName = "Память и Инжекторы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "F4",
				TargetRule = "5.01 (Чит-клиенты)",
				Description = "Низкоуровневый аудит адресного пространства javaw.exe: MZ заголовки скрытых DLL, сигнатуры инжекторов (Vape, Drip, Celestial, Expensive) и исполняемый байткод.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new JavawRamViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "RAM Scanner", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔗 Дескрипторы и скрытые инжекции в javaw",
				CategoryName = "Память и Инжекторы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+H",
				TargetRule = "5.01 (Инжекторы)",
				Description = "Аудит открытых дескрипторов (Handles), именованных мутексов, скрытых потоков и внедренных DLL сторонних процессов в память Minecraft.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new ProcessHandlesViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Дескрипторы", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "⚙️ Ядерные драйверы инжекторов и службы Windows",
				CategoryName = "Память и Инжекторы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Shift+D",
				TargetRule = "5.01 / 5.08 (Драйверы)",
				Description = "Поиск драйверов инжекции ядра (Cheat Engine dbk64, Process Hacker kprocesshacker, Xenos), уязвимых DSE-драйверов и аудит отключения служб DPS/SysMain/EventLog.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new KernelDriversServicesViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Драйверы", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "💥 Аварийные дампы CrashDumps и отчеты WER",
				CategoryName = "Память и Инжекторы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "F9",
				TargetRule = "5.01 / 5.02 (Следы сбоев)",
				Description = "Диагностика дампов сбоев читов (%LOCALAPPDATA%\\CrashDumps), отчетов об ошибках Windows Error Reporting (Report.wer) и падений javaw при инжекции.",
				AccentColor = Ui.AccentOrange,
				LaunchAction = delegate { try { new CrashDumpsAndWerInspectorForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "CrashDumps & WER", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📦 Инспектор структуры JAR, модификаций и байткода",
				CategoryName = "Память и Инжекторы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+O",
				TargetRule = "5.01 (Чит-моды)",
				Description = "Сканирование структуры .jar файлов, манифестов, пула строковых констант и скрытых пакетов читов (Celestial, Wild, Expensive) внутри папки mods.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new JarStructureViewerForm(null, targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "JAR", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			// 2. Файловая система, Артефакты ОС и Запуски
			allTools.Add(new ForensicToolItem
			{
				Name = "🗑️ Журнал удалений NTFS USN Journal ($UsnJrnl)",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+T",
				TargetRule = "5.08 (Удаление улик)",
				Description = "Низкоуровневый криминалистический аудит журнала изменений файловой системы NTFS: точный факт создания, переименования и удаления файлов читов с таймкодами.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new UsnJournalViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "USN Journal", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🗄️ Кэш ядра AppCompatCache (ShimCache)",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Y",
				TargetRule = "5.01 / 5.02 (Запуски .exe)",
				Description = "Аудит кэша ядра Session Manager\\AppCompatCache: до 1024 записей выполненных программ, включая запуск с извлеченных USB-флешек и удаленные бинарники читов.",
				AccentColor = Ui.AccentOrange,
				LaunchAction = delegate { try { new ShimcacheViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "ShimCache", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "⚡ Журнал Windows Prefetch (MAM/SCCA & 8 запусков)",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+E",
				TargetRule = "5.01 / 5.02 (Prefetch)",
				Description = "Глубокий анализ папки Prefetch (.pf), счетчиков запусков, 8-launch таймлайна, времени первого и последнего выполнения, а также загруженных DLL.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new PrefetchViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Prefetch", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🖥️ Инспектор запусков UserAssist, MuiCache и PCA",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "F10",
				TargetRule = "5.01 / 5.02 (Реестр запусков)",
				Description = "Декодирование ROT13 записей реестра UserAssist (счетчики запусков и фокусное время окна), кэша MuiCache и журнала помощника совместимости программ PCA.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new UserAssistMuiCacheViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "UserAssist & MuiCache", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📂 Аудит RecentDocs (.jar/.exe) и журнала BAM ядра",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "F12",
				TargetRule = "5.01 / 5.08 (RecentDocs & BAM)",
				Description = "Декодирование MRU недавних документов RecentDocs и чтение ядра Background Activity Moderator (BAM/DAM) с 64-битными штампами UTC и проверкой удаления с диска.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new AmcacheRecentDocsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "RecentDocs & BAM", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔗 Ярлыки Recent (.lnk) и списки Jump Lists",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+J",
				TargetRule = "5.01 / 5.08 (Ярлыки LNK)",
				Description = "Аудит ярлыков Recent (.lnk) и баз AutomaticDestinations: извлечение оригинальных путей, серийных номеров томов, времени создания и удаленных читов.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new JumpListLnkViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Recent / JumpLists", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📁 История папок Проводника ShellBags",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+B",
				TargetRule = "5.02 (Хранение / Папки)",
				Description = "Реконструкция истории открытия директорий и архивов через реестр BagMRU: обнаружение папок читов, даже если они были удалены до проверки.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new ShellbagsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "ShellBags", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔍 Быстрый охотник за файлами читов и DLL",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Q",
				TargetRule = "5.02 (Хранение читов)",
				Description = "Мгновенный высокоскоростной поиск по дискам, кэшам и реестру известных бинарников чит-клиентов, DLL инжекторов и конфигураций.",
				AccentColor = Ui.AccentOrange,
				LaunchAction = delegate { try { new FastCheatHunterForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Поиск читов", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🎭 Инспектор замаскированных читов и подмен модов",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+G",
				TargetRule = "5.01 / 5.08 (Маскировки)",
				Description = "Проверка CRC32, SHA-256 и сигнатур файлов в папке .minecraft/mods на подмену чит-клиентами под видом легитимных модов (OptiFine, Sodium, Iris).",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new DisguisedFilesViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Маскировки", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			// 3. Корзина, Внешние носители и Анти-форензика
			allTools.Add(new ForensicToolItem
			{
				Name = "🗑️ Корзина $Recycle.Bin ($I / $R индексы и восстановление)",
				CategoryName = "Корзина и Носители",
				Category = HubCategory.RecycleAndStorage,
				Hotkey = "Ctrl+Shift+R",
				TargetRule = "5.08 / 5.02 (Удаление в корзину)",
				Description = "Низкоуровневый парсер $I индексов корзины на всех дисках: точный момент удаления, оригинальное имя и 1-клик быстрое восстановление удаленного чита на Рабочий стол!",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new RecycleBinForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Корзина $Recycle.Bin", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "💾 Инспектор USB-накопителей и флешек (USBSTOR)",
				CategoryName = "Корзина и Носители",
				Category = HubCategory.RecycleAndStorage,
				Hotkey = "Ctrl+Shift+U",
				TargetRule = "5.08 / 5.01 (Флешки)",
				Description = "Аудит реестра подключенных USB-накопителей, серийных номеров, букв дисков и сопоставление с запусками чита с извлеченной перед проверкой флешки.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new UsbDeviceForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "USBSTOR", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📋 Буфер обмена, консоль PowerShell и RunMRU",
				CategoryName = "Корзина и Носители",
				Category = HubCategory.RecycleAndStorage,
				Hotkey = "Ctrl+Shift+C",
				TargetRule = "5.08 / 5.01 (Скрипты очистки)",
				Description = "Поиск команд сокрытия (wevtutil cl, fsutil usn deletejournal, Set-MpPreference), истории PSReadLine, команд Win+R и чит-ссылок/ключей в буфере.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new ClipboardConsoleHistoryViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Буфер и консоль", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🛡️ Инспектор анти-форензики и сокрытия улик (5.08)",
				CategoryName = "Корзина и Носители",
				Category = HubCategory.RecycleAndStorage,
				Hotkey = "Ctrl+I",
				TargetRule = "5.08 (Сокрытие улик)",
				Description = "Комплексный аудит признаков зачистки: сброс журнала USN, остановка служб DPS/EventLog/SysMain, очистка корзины и следы запуска bat-клинеров.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new AntiForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Анти-форензика", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🕒 Хронология активности игрока (Timeline)",
				CategoryName = "Корзина и Носители",
				Category = HubCategory.RecycleAndStorage,
				Hotkey = "Ctrl+U",
				TargetRule = "5.01 - 5.08 (Таймлайн)",
				Description = "Сводная интерактивная временная шкала событий перед вызовом на проверку: загрузки файлов, запуск приложений, подключения флешек и удаление файлов.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new UserActivityTimelineForm().Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Timeline", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			// 4. Сеть, Загрузки и Сокеты
			allTools.Add(new ForensicToolItem
			{
				Name = "🌐 Сетевые обходы, Hosts, DNS-over-HTTPS и VPN",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "F11",
				TargetRule = "5.01 / 5.08 (Сетевые обходы)",
				Description = "Аудит файла hosts на перенаправление серверов лицензий читов и Mojang, проверка DoH в Chrome/Edge/Firefox/Opera/Yandex/Brave, системных прокси и VPN-адаптеров.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new NetworkBypassViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Сетевой аудит", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🌐 Аудит загрузок браузеров, Discord и Telegram",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+Shift+B",
				TargetRule = "5.01 / 5.02 (Скачивание читов)",
				Description = "Чтение SQLite баз истории загрузок Chrome, Edge, Yandex, Opera, Opera GX, Brave через теневые копии, а также аудит вложений Telegram Desktop и Discord.",
				AccentColor = Ui.AccentOrange,
				LaunchAction = delegate { try { new BrowserMessengerDownloadsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Загрузки", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔍 Поисковые запросы и веб-история браузеров",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+Shift+Y",
				TargetRule = "5.01 / 5.08 (Поиски читов)",
				Description = "Аудит поисковых запросов Google, Yandex, YouTube, DuckDuckGo на читы, кряки, скрипты зачистки, а также посещения сайтов читов и зачистка истории.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new BrowserSearchAndHistoryViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Веб-история", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔌 Сетевые сокеты и зондирование Web-GUI читов",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+N",
				TargetRule = "5.01 (Web-панели чита)",
				Description = "Анализ открытых TCP/UDP сокетов системы и javaw.exe, отправка HTTP-зондов на localhost для детекта скрытых веб-интерфейсов чита (Vape, Drip, Slinky).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new ActiveConnectionsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Сокеты", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🌐 Системный DNS-кэш Windows",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "F6",
				TargetRule = "5.01 (Чит-домены)",
				Description = "Просмотр кэша DNS-клиента Windows (ipconfig /displaydns): выявление обращений к сайтам покупок и API читов (vape.gg, celestial.lol, nursultan.fun).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new DnsCacheViewerForm().Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "DNS-кэш", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🌐 Журнал DNS-запросов (Microsoft-Windows-DNS-Client)",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+Shift+K",
				TargetRule = "5.01 / 5.08 (DNS-запросы)",
				Description = "Аудит системного журнала резолвинга DNS (Event ID 3008): сопоставление запросов с PID процессов, поиск обращений к серверам читов даже после ipconfig /flushdns.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new DnsClientLogViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Журнал DNS", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📥 Форензика фоновых загрузок BITS и qmgr.db",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+Shift+I",
				TargetRule = "5.01 / 5.08 (BITS загрузки)",
				Description = "Аудит фоновых передач BITS (Event ID 59/60), очередей bitsadmin и парсинг баз qmgr.db: выявление скачивания читов через PowerShell Start-BitsTransfer в обход браузеров.",
				AccentColor = Ui.AccentOrange,
				LaunchAction = delegate { try { new BitsTransferForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Фоновые загрузки BITS", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "💽 Виртуальные диски (VHD/ISO), VeraCrypt и монтирования",
				CategoryName = "Корзина и Носители",
				Category = HubCategory.RecycleAndStorage,
				Hotkey = "Ctrl+Shift+V",
				TargetRule = "5.08 / 5.01 (Виртуальные диски)",
				Description = "Аудит событий VHDMP (Event ID 1/2), томов MountedDevices, драйверов VeraCrypt/ImDisk и детекция экстренного размонтирования образов перед ScreenShare.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new VirtualDisksAndSandboxViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Виртуальные диски", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			// 5. Регламент, Улики и Комплексный аудит
			allTools.Add(new ForensicToolItem
			{
				Name = "📸 Фиксация доказательств со штампом SHA-256",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Shift+S",
				TargetRule = "Протокол фиксации",
				Description = "Создание криптографически заверенного снимка экрана с полупрозрачным штампом (Игрок, Модератор, Пункт регламента, Время UTC/МСК, SHA-256) для апелляций.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new EvidenceCaptureForm(targetPlayerName, targetModeratorName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Фиксация", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📋 Пошаговый чек-лист модератора ScreenShare (86 шагов)",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "F2 / Ctrl+K",
				TargetRule = "Регламент ScreenShare",
				Description = "Интерактивный регламентированный протокол из 86 шагов с чекбоксами, таймером вызова и кнопками быстрого перехода ко всем криминалистическим утилитам.",
				AccentColor = Ui.AccentGreen,
				LaunchAction = delegate { try { new ModeratorChecklistForm(targetPlayerName, targetModeratorName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Чек-лист", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "⚖️ Справочник регламента и правил (5.01 - 5.08)",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "F3 / Ctrl+R",
				TargetRule = "Правила проекта",
				Description = "Официальная база правил сервера (5.01 читы 30d, 5.02 хранение 15d, 5.05 признание 15d, 5.08 сокрытие 30d, 2.06 багоюз 3-7d, автокликеры РАЗРЕШЕНЫ).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new RuleBookViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Правила", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🛡️ Системные журналы Windows Event Log",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+W",
				TargetRule = "5.08 (Очистка логов)",
				Description = "Аудит журналов Security, System, Application: выявление очистки журналов событий (Event ID 1102 / 104), сбоев приложений и остановки системных служб.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new WindowsEventLogViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Event Log", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📜 Монитор логов latest.log и чит-команд",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+L / F5",
				TargetRule = "5.01 (Чит-команды)",
				Description = "Мониторинг активного файла latest.log и архивов .log.gz: фиксация чит-команд (.bind, .vape, .config, #goto), заходов на сервер и киков античита.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new MinecraftLogViewerForm().Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Логи", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🖱️ Аудит ПО мыши, макросов и замер CPS",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+M",
				TargetRule = "Разрешено / Аудит",
				Description = "Аудит софта мышей (Bloody, Logitech, Razer, Glorious дебаунс) и живой замер CPS кликов. Важно: согласно регламенту сервера автокликеры РАЗРЕШЕНЫ.",
				AccentColor = Ui.AccentGreen,
				LaunchAction = delegate { try { new PeripheralMacroForensicsForm().Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Мыши", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "⚙️ Аудит автозагрузки, реестра Run и планировщика задач",
				CategoryName = "Корзина и Носители",
				Category = HubCategory.RecycleAndStorage,
				Hotkey = "Ctrl+Shift+T",
				TargetRule = "5.08 / 5.01 (Автозапуск)",
				Description = "Сканирование задач Windows (System32\\Tasks), ключей Run/RunOnce и папок Startup: обнаружение скриптов зачистки следов, скрытых bat-файлов и вредоносных PowerShell команд.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new ScheduledTasksStartupViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Автозапуск и задачи", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔔 Форензика базы уведомлений Action Center и Defender",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Shift+N",
				TargetRule = "5.01 / 5.02 (Алерты читов)",
				Description = "Низкоуровневый парсер wpndatabase.db через теневой поток: обнаружение скрытых уведомлений Windows Defender о детекте троянов/читов и сообщений Discord/Telegram.",
				AccentColor = Ui.AccentOrange,
				LaunchAction = delegate { try { new WindowsNotificationForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "База уведомлений", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔍 Инспектор поисков Проводника, TypedPaths и RecentApps",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Shift+E",
				TargetRule = "5.01 / 5.08 (Поиски читов)",
				Description = "Декодирование постоянной истории поиска файлов WordWheelQuery, введенных путей в адресную строку TypedPaths и списка RecentApps с таймкодами и счетчиками запусков.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new ExplorerSearchesTypedPathsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Поиски Проводника", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🛡️ Аудит исключений и политик Windows Defender",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Shift+F",
				TargetRule = "5.08 / 5.01 (Исключения антивируса)",
				Description = "Сканирование белых списков Windows Defender (Exclusions Paths/Processes) и политик Tampering: выявление папок чита в исключениях и фиксация удаленных папок читов.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new DefenderExclusionsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Исключения Defender", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "💎 Инспектор ресурспаков, шейдеров и X-Ray",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Shift+P",
				TargetRule = "5.06 / 5.03 (X-Ray)",
				Description = "Аудит .zip ресурспаков на прозрачные модели камня (stone.json), X-Ray шейдерпаки и эксплойт заглушения шагов в sounds.json (Silent-Step).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new ResourcePacksXrayViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Ресурспаки и X-Ray", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🎮 Форензика кэша шейдеров DirectX, NVidia и AMD",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Shift+G",
				TargetRule = "5.01 / 5.08 (Кэш шейдеров)",
				Description = "Аудит D3DSCache, NVidia DXCache и AMD DxCache на ImGui оверлеи читов, сигнатуры Celestial/Expensive/Vape и фиксация тотальной зачистки кэша перед проверкой (5.08).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new DirectXShaderCacheViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Кэш шейдеров DirectX", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "⚡ Экспресс-триаж ScreenShare (30 секунд)",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Shift+X",
				TargetRule = "Экспресс-триаж 5.01-5.08",
				Description = "Многопоточный экспресс-скрининг системы за 3-5 секунд по 10 критическим маркерам нарушений с моментальным копированием бана.",
				AccentColor = Ui.AccentOrange,
				LaunchAction = delegate { try { new ExpressTriageForm(targetPlayerName, targetModeratorName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Экспресс-триаж", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

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

			allTools.Add(new ForensicToolItem
			{
				Name = "🚀 Форензика базы запусков PCA и AppCompatFlags",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Shift+A",
				TargetRule = "5.01 / 5.08 (Запуски PCA)",
				Description = "Аудит журнала Program Compatibility Assistant (PcaAppLaunchDic.txt, PcaGeneralDb0) и ветки AppCompatFlags\\Store: детекция запущенных и удаленных исполняемых файлов.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new PcaAppLaunchForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "База PCA", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🧹 Детектор клинеров, батников очистки и улик сокрытия (5.08)",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Shift+W",
				TargetRule = "5.08 (Сокрытие улик)",
				Description = "Поиск 50+ клинеров (JournalTrace, Echo, Slapper, CCleaner), батников зачистки (wevtutil, fsutil, del prefetch) и аномалий очистки журналов перед ScreenShare.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new CleanersAndWipingTracesViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Детектор клинеров", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔏 Инспектор цифровых подписей Authenticode и сертификатов JAR",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Shift+L",
				TargetRule = "5.01 (Подписи и сертификаты)",
				Description = "Проверка цепочек доверия Authenticode (.exe/.dll) и манифестов JAR (.SF/.RSA): обнаружение поддельных сертификатов читов и неподписанных модулей в .minecraft.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new AuthenticodeSignaturesViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Подписи Authenticode", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "💾 Форензика теневых копий тома (VSS) и точек восстановления",
				CategoryName = "Корзина и Носители",
				Category = HubCategory.RecycleAndStorage,
				Hotkey = "Ctrl+Shift+Q",
				TargetRule = "5.08 (Удаление VSS)",
				Description = "Аудит доступных снимков тома VSS, состояния службы System Restore и выявление принудительного удаления теневых копий тома (vssadmin delete shadows, Event ID 22).",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new VolumeShadowCopiesViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Теневые копии VSS", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🖼️ Форензика кэша эскизов Thumbcache и IconCache",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Shift+1",
				TargetRule = "5.08 / 5.01 (Кэш эскизов)",
				Description = "Парсинг баз thumbcache_*.db Explorer: извлечение визуальных миниатюр удаленных файлов (JPEG/PNG) и выявление зачистки кэша эскизов перед ScreenShare (5.08).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new ThumbcacheAndIconCacheViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Кэш эскизов", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🌐 Аудит сетевых туннелей, Wintun и сертификатов MitM",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+Shift+J",
				TargetRule = "5.01 / 5.02 (Снифферы и туннели)",
				Description = "Поиск корневых сертификатов перехвата трафика (Fiddler, Charles, mitmproxy) в Root CA, проверка виртуальных адаптеров Wintun/WireGuard и параметров WinINet прокси.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new NetworkTunnelsAndCertificatesViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Сетевые туннели", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "⏳ Форензика Windows Timeline и журнала активностей CDP",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Shift+2",
				TargetRule = "5.01 / 5.08 (Активности Timeline)",
				Description = "Парсинг базы ConnectedDevicesPlatform (ActivitiesCache.db): обнаружение запусков чита, времени активного использования и удаленных исполняемых файлов.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new WindowsTimelineAndActivitiesViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Windows Timeline", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🚀 Инспектор лаунчеров Minecraft, профилей версий и аккаунтов",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Shift+3",
				TargetRule = "5.01 / 5.02 / 5.08 (Лаунчеры и профили)",
				Description = "Аудит 10+ лаунчеров (.minecraft, TLauncher, Prism, MultiMC, Modrinth, Lunar, Badlion, Feather): детекция чит-версий (mainClass), аргументов -javaagent, твинков (5.02) и очистки логов (5.08).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new MinecraftLaunchersAndProfilesViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Лаунчеры Minecraft", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔍 Инспектор аппаратных идентификаторов (HWID), детекции спуферов и MAC",
				CategoryName = "Корзина и Носители",
				Category = HubCategory.RecycleAndStorage,
				Hotkey = "Ctrl+Shift+4",
				TargetRule = "5.02 / 5.08 (HWID спуферы)",
				Description = "Аудит MachineGuid, SMBIOS UUID, серийников дисков/BIOS, детекция подмены MAC (LAA/TMAC), утилит VolumeID и спуферов обхода бана.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new HwidAndHardwareSpoofingViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "HWID & Спуферы", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🌐 Инспектор расширений браузеров, Tampermonkey и Web-панелей",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+Shift+5",
				TargetRule = "5.01 / 5.02 / 5.08 (Web-панели и скрипты)",
				Description = "Аудит Chrome, Edge, Opera, Yandex, Brave, Firefox: детекция юзерскриптов Tampermonkey, Web-GUI читов (Vape/Drip/Slinky), сессий LocalStorage и закладок.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new BrowserExtensionsAndWebCheatPanelsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Расширения и Web-панели", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📁 Форензика .minecraft\\config, Baritone и остаточных конфигов",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Shift+6",
				TargetRule = "5.01 / 5.08 / 2.06 (Конфиги и Baritone)",
				Description = "Аудит .minecraft\\config и экземпляров: детекция осиротевших конфигов удаленных читов (5.08), ботов Baritone, чит-модулей и эксплойтов options.txt (Gamma).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new MinecraftConfigsAndBaritoneForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Конфигурации Minecraft", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📸 Инспектор скриншотов Minecraft (F2) и записей GameDVR",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Shift+7",
				TargetRule = "5.01 / 5.08 (Скриншоты и видео)",
				Description = "Аудит папок скриншотов .minecraft, Lunar, Feather, Prism и видеозаписей GameDVR/ShadowPlay: живая галерея предпросмотра, детекция поддельных PNG (JAR/EXE) и зачисток улик.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new MinecraftScreenshotsAndGameDvrViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Скриншоты Minecraft", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📜 Инспектор блоков кода PowerShell ScriptBlock (Event ID 4104)",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Shift+8",
				TargetRule = "5.01 / 5.08 (PowerShell скрипты)",
				Description = "Аудит журнала Microsoft-Windows-PowerShell/Operational: детекция скриптов зачистки (wevtutil, fsutil), загрузчиков читов (WebClient, BitsTransfer) и авто-декодирование Base64.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new PowerShellScriptBlock4104ViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "PowerShell ScriptBlock", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📁 Инспектор диалогов ComDlg32 (OpenSavePidlMRU и LastVisitedPidlMRU)",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Shift+9",
				TargetRule = "5.01 / 5.08 (Диалоги ComDlg32)",
				Description = "Аудит системных диалогов OpenSavePidlMRU и LastVisitedPidlMRU: дешифрование бинарных структур PIDL, детекция инжекторов, проверка наличия файлов на диске и фиксация удаления читов (5.08).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new OpenSaveAndLastVisitedPidlViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Диалоги ComDlg32", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "⏱️ Инспектор событий сна, скачков времени и перезагрузок (Event 1/41/42/107/1074/6008)",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Shift+0",
				TargetRule = "5.04 / 5.08 (Сон, сброс питания и время)",
				Description = "Аудит событий питания и времени Windows: детекция панического сна (Event 42), изменения системных часов (Time Jump Event 1) и сбросов питания/Hard Reset (Event 41/6008).",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new SleepWakeTimeJumpAndRebootViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "События сна и времени", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "☕ Инспектор JVM инъекций, прокси DLL (dwrite/version/opengl/dxgi) и переменных окружения",
				CategoryName = "Память и Инжекторы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Shift+M",
				TargetRule = "5.01 / 5.08 (JVM, Proxy DLL, _JAVA_OPTIONS)",
				Description = "Аудит сред выполнения Java (JRE/JDK): выявление подмены библиотек (dwrite.dll, version.dll, opengl32.dll Hijack в bin\\), перехватчиков _JAVA_OPTIONS, удаленных JavaAgent и активных proxy DLL в памяти javaw.exe.",
				AccentColor = Ui.AccentOrange,
				LaunchAction = delegate { try { new JvmInjectionAndProxyDllViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "JVM Инъекции и Прокси DLL", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🚰 Инспектор именованных каналов Named Pipes, IPC и межпроцессного взаимодействия",
				CategoryName = "Память и Инжекторы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Alt+P",
				TargetRule = "5.01 (Named Pipes & IPC)",
				Description = "Аудит системных каналов \\\\.\\pipe\\: выявление каналов связи инжекторов чита с игрой (Vape, Drip, Slinky, Celestial), анонимных GUID-каналов и детекция соединений с javaw.exe.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new NamedPipesAndIpcEndpointsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Named Pipes IPC", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🛡️ Инспектор следов запуска SACP, BAM и FeatureUsage (Артефакты выполнения)",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Alt+B",
				TargetRule = "5.01 / 5.08 (SACP, BAM, AppSwitched)",
				Description = "Аудит баз Compatibility Assistant (SACP Store), Background Activity Moderator (BAM), FeatureUsage и AppCompat: фиксация удаленных файлов перед проверкой (5.08) и запусков читов (5.01).",
				AccentColor = Ui.AccentOrange,
				LaunchAction = delegate { try { new BamAndAppCompatStoreViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "SACP и BAM", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🪟 Инспектор сокрытия экрана DWM, OBS/Discord Bypass и графических оверлеев",
				CategoryName = "Память и Инжекторы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Alt+G",
				TargetRule = "5.08 / 5.01 (WDA Bypass, Stealth Overlays, Hook DLLs)",
				Description = "Аудит оконных дескрипторов HWND: обнаружение окон сокрытия экрана от Discord/OBS стрима (SetWindowDisplayAffinity 0x11), прозрачных кликабельных ESP оверлеев и фальшивых Discord/Steam хуков в javaw.exe.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new ScreenCaptureEvasionAndOverlayViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "DWM & Оверлеи", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📜 Инспектор истории команд Minecraft, чат-логов и чит-префиксов",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Alt+C",
				TargetRule = "5.01 / 5.08 (command_history.txt, Baritone, Чит-команды)",
				Description = "Аудит файлов command_history.txt и чат-логов: выявление чит-команд (.vape, .bind, .killaura, .reach), ботов Baritone (#goto, #mine) и фиксация очистки/удаления истории перед проверкой (5.08).",
				AccentColor = Ui.AccentOrange,
				LaunchAction = delegate { try { new MinecraftCommandHistoryAndChatLogsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "История команд", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📦 Инспектор стеганографии в модах, ZIP EOCD и скрытых нагрузок",
				CategoryName = "Файловая система",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Alt+Y",
				TargetRule = "5.01 / 5.08 (ZIP EOCD Steganography, Скрытые хвосты)",
				Description = "Аудит структуры ZIP-архивов .jar/.zip: поиск скрытой полезной нагрузки за пределами каталога EOCD (PK\x05\x06), внедренных PE/DLL файлов, скрытых вторичных архивов и чит-сигнатур.",
				AccentColor = Ui.AccentOrange,
				LaunchAction = delegate { try { new ZipSteganographyAndModPayloadViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "ZIP Стеганография", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔌 Инспектор хронологии подключения USB и PnP устройств (SetupAPI.dev.log)",
				CategoryName = "Корзина и Накопители",
				Category = HubCategory.RecycleAndStorage,
				Hotkey = "Ctrl+Alt+I",
				TargetRule = "5.08 / 5.01 (SetupAPI, USB Хронология, KMBox/Arduino)",
				Description = "Аудит системных логов setupapi.dev.log: фиксация точного времени подключения флешек перед вызовом на проверку (5.08) и выявление аппаратных чит-контроллеров KMBox/Arduino/Teensy (5.01).",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new SetupApiUsbTimelineViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "SetupAPI USB", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});
			allTools.Add(new ForensicToolItem
			{
				Name = "🔥 Инспектор аварийных логов JVM HotSpot и дампов сбоев (hs_err_pid.log)",
				CategoryName = "Память и Процессы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Alt+Q",
				TargetRule = "5.01 / 5.08 (JVM HotSpot, Инжекты нативных DLL, Краш-дампы)",
				Description = "Аудит аварийных логов hs_err_pid*.log и replay_pid*.log: выявление внедрения читов (Vape, Celestial, Expensive), инжектированных нативных DLL библиотек и искусственных крашей перед проверкой.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new JvmCrashLogsAndHotspotDumpsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "JVM HotSpot Логи", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔮 Глубокий анализатор шейдерпаков на скрытый X-Ray (GLSL Discard)",
				CategoryName = "Файлы и Реестр",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Alt+X",
				TargetRule = "5.01 (X-Ray Шейдерпаки, GLSL Discard, Прозрачность руды)",
				Description = "Синтаксический разбор шейдеров .vsh/.fsh/.glsl: выявление скрытия блоков камня/земли инструкцией GLSL discard, маскировки под BSL/Complementary и модификации альфа-канала руды.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new DeepShaderpackXrayViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "X-Ray Шейдеры", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🧬 Детектор полого внедрения процессов и неразмеченной памяти (Process Hollowing)",
				CategoryName = "Память и Процессы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Alt+H",
				TargetRule = "5.01 / 5.08 (Process Hollowing, RunPE, Неразмеченная RWX)",
				Description = "Низкоуровневый аудит виртуальной памяти (VirtualQueryEx): выявление исполняемых страниц без DLL на диске, заголовков PE (MZ) и скрытых инжекций читов (Vape, Slinky, Drip).",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new ProcessHollowingAndInjectedMemoryViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Process Hollowing", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🖥️ Инспектор подмены ПК, удаленных сессий и AnyDesk профилей",
				CategoryName = "Сети и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+Alt+A",
				TargetRule = "5.08 / 5.01 (Подмена ПК, AnyDesk ID, Параллельные сессии, RDP, VM)",
				Description = "Криминалистическая проверка на подставной компьютер/чистый ноутбук, параллельные удаленные сессии чистильщика (AnyDesk/RDP), подмену AnyDesk ID и виртуальные машины (5.08).",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new FakeComputerAndRemoteAnyDeskViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Подмена ПК и AnyDesk", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🥷 Инспектор обхода проверок, сокрытия читов и анти-форензики",
				CategoryName = "Файлы и Реестр",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Alt+E",
				TargetRule = "5.08 / 5.01 (MountPoints2, Ejected USB/RAM, Буфер history.db, Очистка логов)",
				Description = "Комплексный аудит попыток сокрытия софта: извлечение USB/RamDisk (MountPoints2), анализ БД буфера обмена history.db на токены/ключи (Vape/Drip/Slinky) и детекция самоликвидаторов (fsutil, wevtutil, vssadmin).",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new AntiEvasionAndStealthBypassViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Обход проверок и скрытие софта", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🛡️ Инспектор гипервизоров, виртуальных машин, песочниц и Stream-Proof сокрытия",
				CategoryName = "Память и Процессы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Alt+V",
				TargetRule = "5.08 / 5.01 (VM, Sandbox, Stream-Proof WDA, Виртуальные мониторы, DBVM)",
				Description = "Глубокая экспертиза изоляции и сокрытия: виртуальные машины (VMware/VBox/Hyper-V/KVM), песочницы Sandboxie/Windows Sandbox, сокрытие окон WDA_EXCLUDEFROMCAPTURE, виртуальные мониторы IddSample и драйвер DBVM (5.08).",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new HypervisorVmAndSandboxEvasionViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Гипервизоры и VM", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "💬 Инспектор Discord, Rich Presence и RPC криминалистики",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+Alt+D",
				TargetRule = "5.01 / 5.08 (Discord LevelDB, Rich Presence, discord-ipc, Vencord / BetterDiscord)",
				Description = "Глубокая экспертиза Discord: анализ LevelDB хранилища на Rich Presence читов (Vape, Celestial, Expensive, Nursultan), аудит именованных каналов discord-ipc-* и проверка плагинов обхода screenshare в Vencord/BetterDiscord/Vesktop.",
				AccentColor = Ui.AccentBlue,
				LaunchAction = delegate { try { new DiscordRichPresenceAndRpcForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Discord & RPC", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "💉 Инспектор патчинга памяти, обхода AMSI и ETW",
				CategoryName = "Память и Процессы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Alt+P",
				TargetRule = "5.01 / 5.08 (AMSI Patches, ETW Event Write, DLL .text Unhooking, MinHook / Detours)",
				Description = "Аудит оперативной памяти javaw.exe и лаунчеров: детекция байт-патчей AmsiScanBuffer (C3, 31 C0 C3, B8 57..), подавления событий безопасности ETW (EtwEventWrite/NtTraceEvent), модификаций секций .text нативных библиотек и нативных перехватов MinHook/Detours.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new AmsiEtwAndPatchingDetectorViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Патчинг памяти, AMSI и ETW", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🖼️ Инспектор скриншотов, EXIF метаданных и вотермарок читов",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Alt+S",
				TargetRule = "5.01 / 5.08 (PNG Chunks, EXIF, Photoshop/Paint.NET/GIMP, Чит-вотермарки, Подмена дат)",
				Description = "Глубокая экспертиза скриншотов Minecraft: анализ чанков PNG/EXIF на графические редакторы (Photoshop/Paint.NET/GIMP), поиск вотермарок читов (Vape/Expensive/Celestial) и манипуляций датами создания (5.01/5.08).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new MinecraftScreenshotsExifAndOcrForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Скриншоты и EXIF", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🖱️ Инспектор мыши, дебаунса и аппаратных макросов",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Alt+M",
				TargetRule = "2.06 / 5.01 / 5.08 (Debounce 0-2ms, Bloody Oscar, Interception, G HUB/Synapse)",
				Description = "Аппаратный аудит HID-мышей: детекция заниженного debounce time Glorious/Roccat (багоюз 2.06), аппаратных скриптов Bloody/Oscar и Ultra-Core 3/4 (5.01), драйверов перехвата кликов Interception (5.08) и циклических макросов вендоров.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new HardwareMouseAndPollingRateAuditViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Мышь, дебаунс и макросы", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🛡️ Инспектор драйверов ядра, BYOVD эксплойтов и TestSigning",
				CategoryName = "Память и Процессы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Alt+K",
				TargetRule = "5.01 / 5.08 (BYOVD gdrv/mhyprot2/RTCore64, kdmapper, TestSigning, Unbacked Drivers)",
				Description = "Глубокая экспертиза ядра: аудит загруженных модулей ядра EnumDeviceDrivers, детекция уязвимых BYOVD драйверов (gdrv, mhyprot2, RTCore64, dbk64), следов kdmapper/kdu (Event ID 7045), параметров TestSigning и бесфайловых руткитов.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new KernelDriversAndHypervisorHooksViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Драйверы ядра и TestSigning", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "💎 Инспектор X-Ray ресурспаков, моделей CEM и невидимых шрифтов",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Alt+X",
				TargetRule = "5.01 / 5.08 (X-Ray текстуры, OptiFine CEM Hitbox, Нулевые шрифты, Маскировка паков)",
				Description = "Глубокая экспертиза ресурспаков .minecraft\\resourcepacks: прозрачные текстуры камня/породы (X-Ray 5.01), увеличение хитбоксов через OptiFine CEM (5.01) и сокрытие текста через нулевые шрифты (5.08).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new FontAndTexturePackBypassViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "X-Ray Ресурспаки и Шрифты", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "⏱️ Инспектор аномалий системного времени и перевода часов",
				CategoryName = "Файловая система и Реестр",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Alt+T",
				TargetRule = "5.08 (Time Anomaly, Event ID 1 Clock Rewind, Reboot Before SS, Time-Travel Files)",
				Description = "Глубокая экспертиза системного времени: аудит Kernel-General Event ID 1 (откат часов назад > 5 мин -> 5.08), Event ID 12/13/41 (перезагрузка/выключение перед проверкой), служб w32time и файлов из будущего (Time-Travel).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new TimeAnomalyAndSystemClockTamperingViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Аномалии системного времени", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "📋 Инспектор истории буфера обмена Win+V и базы history.db",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Alt+B",
				TargetRule = "5.01 / 5.08 (history.db SQLite, Чит-токены Vape/Drip/Slinky, Клинеры, Зачистка базы)",
				Description = "Глубокая экспертиза базы буфера обмена %LocalAppData%\\Microsoft\\Windows\\Clipboard\\history.db: поиск токенов/ключей читов (5.01), команд клинеров (fsutil, wevtutil, vssadmin) и детекция зачистки/обнуления базы (5.08).",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new ClipboardWinVAndHistoryDatabaseViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Буфер обмена и history.db", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔄 Инспектор фальсификации обновлений Windows, UsoClient и срыва проверок",
				CategoryName = "Файловая система и Реестр",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Alt+U",
				TargetRule = "5.08 (Fake Update Screen, fakeupdate.net, UsoClient Reboot, wuauserv Tampering)",
				Description = "Глубокая экспертиза экранов обновления: детекция полноэкранных браузерных симуляций fakeupdate.net (5.08), фиктивных перезагрузок usoclient RestartDevice перед проверкой, отключения wuauserv и расхождений с журналом CBS.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new UsoAndWindowsUpdateTamperingViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Обновления Windows и UsoClient", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🎙️ Инспектор виртуальных аудиокабелей и стриминга",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+Alt+R",
				TargetRule = "5.08 (VB-Cable, VoiceMeeter, VAC, Parsec Audio, Посторонняя помощь)",
				Description = "Судебная экспертиза аудиоустройств и драйверов: аудит VB-Audio Cable, VoiceMeeter, VAC, Parsec Audio, стримеров Sunshine/Moonlight и детекция скрытых каналов подсказок третьих лиц (5.08).",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new RemoteStreamingAndVirtualAudioDriverViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Виртуальные аудиокабели и стриминг", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🎮 Инспектор Direct3D 11, OpenGL VTable Hooks и ESP оверлеев",
				CategoryName = "Память и Процессы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Alt+F",
				TargetRule = "5.01 / 5.08 (wglSwapBuffers, D3D11 Present, ESP Overlay, Overlay DLL Hijacking)",
				Description = "Судебная экспертиза рендеринга и перехватов: аудит пролога wglSwapBuffers/Present на JMP-трамплины (ESP/Chams 5.01), подмены оверлейных DLL и внешних прозрачных клик-тру окон.",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new Direct3dD3d11HookAndEspOverlayViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Direct3D/OpenGL Hooks & ESP", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🌐 Инспектор сетевых туннелей, VPN и прокси",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+Alt+N",
				TargetRule = "5.08 (Ngrok, Cloudflared, Frp, Wintun, WireGuard, PAC, hosts)",
				Description = "Судебная экспертиза сетевого трафика и уклонений: аудит туннелей Ngrok/Cloudflared/Frp, VPN адаптеров (Wintun/WireGuard/TAP), локальных прокси (V2Ray/Tor) и подмены hosts (5.08).",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new LocalTunnelsVpnAndProxyBypassViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Сетевые туннели и прокси", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "☕ Инспектор Java Agent и аргументов JVM",
				CategoryName = "Память и Процессы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Alt+J",
				TargetRule = "5.01 (Java Agent, -javaagent, BootClassPath, JAVA_TOOL_OPTIONS)",
				Description = "Судебная экспертиза аргументов JVM: аудит параметров -javaagent:, -agentpath:, -Xbootclasspath:, поиск чит-агентов в Temp/AppData и проверка переменных JAVA_OPTIONS (5.01).",
				AccentColor = Ui.AccentRed,
				LaunchAction = delegate { try { new JavaAgentAndJvmFlagsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Java Agent и JVM Flags", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🧬 Инспектор Mixin & mod.json архивов модов",
				CategoryName = "Файловая система и Реестр",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Alt+Z",
				TargetRule = "5.01 / 5.08 (Mixin Target-классы, CPVP WhiteList «Атлантис», Маскировка)",
				Description = "Глубокий парсинг .jar модов, fabric.mod.json, mcmod.info и Mixin JSON: аудит target-классов (ClientPlayerEntity, EntityRenderer), проверка белого списка CPVP «Атлантис» (Marlow/Haram/Walksy) и маскировки модов (5.01/5.08).",
				AccentColor = Ui.AccentPurple,
				LaunchAction = delegate { try { new MixinAndModJsonForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Mixin & mod.json", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "⏳ Единый судебно-криминалистический таймлайн",
				CategoryName = "Регламент и Улики",
				Category = HubCategory.RulesAndEvidence,
				Hotkey = "Ctrl+Alt+L",
				TargetRule = "5.01 / 5.08 (Prefetch, BAM, EventLog 1074/7045/1102, LNK, USN)",
				Description = "Сводный хронологический анализ всех артефактов ОС: Prefetch, BAM/SACP реестра, Event Logs (1074/7045/1102), Recent LNK и Shellbags в единой временной шкале проверок ScreenShare.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new UnifiedForensicTimelineViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Единый таймлайн", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🎮 Инспектор настроек и биндов Minecraft (options.txt)",
				CategoryName = "Файловая система и Реестр",
				Category = HubCategory.FileSystemAndRegistry,
				Hotkey = "Ctrl+Alt+O",
				TargetRule = "5.01 / 5.03 (Чит-бинды, Макросы кликера, Серверный WhiteList, CPVP)",
				Description = "Глубокий парсинг options.txt, keybinds.txt и конфигураций модов: аудит чит-привязок (killaura, fly, speed, xray -> 5.01), макросов автокликера (5.03) с учетом белого списка команд и CPVP «Атлантис».",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new MinecraftOptionsAndKeybindsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Настройки и бинды Minecraft", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🌍 Инспектор взлома сида мира и ChunkBase локаторов",
				CategoryName = "Сеть и Загрузки",
				Category = HubCategory.NetworkAndDownloads,
				Hotkey = "Ctrl+Alt+W",
				TargetRule = "5.06 (SeedCrackerX, SeedMapper, Cubiomes, Amidst, ChunkBase, MineAtlas)",
				Description = "Судебная экспертиза сида мира: поиск модов SeedCrackerX/SeedMapper, утилит Cubiomes/Amidst и глубокий аудит истории 6 веб-браузеров на посещение онлайн-локаторов ChunkBase/MineAtlas по регламенту 5.06.",
				AccentColor = Ui.AccentGreen,
				LaunchAction = delegate { try { new WorldSeedAndChunkbaseForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Взлом сида мира и ChunkBase", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🔬 Инспектор памяти System Informer / Process Hacker 2",
				CategoryName = "Память и Инжекторы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Alt+8 / Ctrl+Alt+F8",
				TargetRule = "5.01 / 5.08 (javaw.exe, DLL, Explorer, DcomLaunch, PcaSvc, DPS, csrss.exe)",
				Description = "Нативная автоматизация всех проверок мануала System Informer / Process Hacker 2: сканирование строк в ОЗУ javaw.exe (мин. длина 4), модули DLL (пустой Description + веса 1.42-1.89 МБ), InjectDllHistory, explorer.exe (pcaclient, file:///), DcomLaunch/PcaSvc (.jar), DPS Replace и csrss.exe.",
				AccentColor = Ui.AccentTeal,
				LaunchAction = delegate { try { new SystemInformerAndProcessHackerForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "System Informer / Process Hacker 2", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});

			allTools.Add(new ForensicToolItem
			{
				Name = "🧬 Инспектор Luyten REGEX MODS, HxD Hex DLL, BAM & SystemDLC",
				CategoryName = "Память и Инжекторы",
				Category = HubCategory.MemoryAndProcesses,
				Hotkey = "Ctrl+Alt+9 / Ctrl+Alt+F9",
				TargetRule = "5.01 / 5.08 (Luyten REGEX, HxD DLL, BAM Unsigned, Event Logs, SystemDLC)",
				Description = "Высокоуровневая криминалистика: декомпиляция и поиск REGEX MODS в JAR (Forge/Fabric/LabyMod), Hex-аудит DLL/переименованных модулей, фильтр BAM Not Signed Only, аудит Event Logs & Registry (очистка логов / AppCompatCache) и глубокое обнаружение SystemDLC.",
				AccentColor = Ui.AccentPurple,
				LaunchAction = delegate { try { new LuytenHxdAndHighLevelForensicsViewerForm(targetPlayerName).Show(this); } catch (Exception ex) { MessageBox.Show(ex.Message, "Luyten / HxD / BAM / SystemDLC", MessageBoxButtons.OK, MessageBoxIcon.Error); } }
			});
		}

		private void InitializeComponent()
		{
			this.Text = "🎛️ Единый Центр Судебной Криминалистики (Forensic Hub) — MCCheatChecker";
			this.Size = new Size(1220, 760);
			this.MinimumSize = new Size(980, 600);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.KeyPreview = true;
			this.Icon = SystemIcons.Shield;

			Ui.ApplyDarkScrollbars(this);

			this.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					this.Close();
					e.Handled = true;
				}
				else if (e.KeyCode == Keys.Enter)
				{
					LaunchSelectedTool();
					e.Handled = true;
				}
				else if (e.KeyCode == Keys.F2)
				{
					try { new ModeratorChecklistForm(targetPlayerName, targetModeratorName).Show(this); } catch { }
					e.Handled = true;
				}
				else if (e.KeyCode == Keys.F3)
				{
					try { new RuleBookViewerForm(targetPlayerName).Show(this); } catch { }
					e.Handled = true;
				}
				else if (e.Control && e.Shift && e.KeyCode == Keys.S)
				{
					try { new EvidenceCaptureForm(targetPlayerName, targetModeratorName).Show(this); } catch { }
					e.Handled = true;
				}
			};

			// Панель заголовка
			Panel headerPanel = new Panel();
			headerPanel.Dock = DockStyle.Top;
			headerPanel.Height = 74;
			headerPanel.BackColor = Ui.Surface;
			headerPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(headerPanel);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🎛️ ЕДИНЫЙ ЦЕНТР СУДЕБНОЙ КРИМИНАЛИСТИКИ (FORENSIC COMMAND CENTER)";
			lblHeaderTitle.Font = Ui.FontTitle(13.5f, true);
			lblHeaderTitle.ForeColor = Ui.AccentTeal;
			lblHeaderTitle.Location = new Point(16, 12);
			lblHeaderTitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderTitle);

			lblHeaderSubtitle = new Label();
			lblHeaderSubtitle.Text = "Быстрый запуск, моментальный поиск по 89 криминалистическим анализаторам артефактов и протокол проверки.";
			lblHeaderSubtitle.Font = Ui.FontBody(9f, false);
			lblHeaderSubtitle.ForeColor = Ui.TextSecondary;
			lblHeaderSubtitle.Location = new Point(18, 42);
			lblHeaderSubtitle.AutoSize = true;
			headerPanel.Controls.Add(lblHeaderSubtitle);

			lblPlayerBadge = new Label();
			lblPlayerBadge.Text = string.Format("Игрок: {0} | Модератор: {1}", targetPlayerName, targetModeratorName);
			lblPlayerBadge.Font = Ui.FontTitle(9f, true);
			lblPlayerBadge.ForeColor = Ui.AccentGreen;
			lblPlayerBadge.BackColor = Color.FromArgb(15, 45, 20);
			lblPlayerBadge.TextAlign = ContentAlignment.MiddleCenter;
			lblPlayerBadge.Size = new Size(330, 28);
			lblPlayerBadge.Location = new Point(headerPanel.Width - 350, 22);
			lblPlayerBadge.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			headerPanel.Controls.Add(lblPlayerBadge);

			// Панель быстрых действий (Top Quick Bar)
			Panel quickBar = new Panel();
			quickBar.Dock = DockStyle.Top;
			quickBar.Height = 44;
			quickBar.BackColor = Ui.SurfaceAlt;
			quickBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(quickBar);

			int qx = 12;
			btnQuickChecklist = CreateQuickButton("⚡ Экспресс-триаж (Ctrl+Shift+X)", ref qx, quickBar, delegate
			{
				try { new ExpressTriageForm(targetPlayerName, targetModeratorName).Show(this); } catch { }
			});
			CreateQuickButton("📋 Чек-лист проверки (F2)", ref qx, quickBar, delegate
			{
				try { new ModeratorChecklistForm(targetPlayerName, targetModeratorName).Show(this); } catch { }
			});
			btnQuickEvidence = CreateQuickButton("📸 Фиксация SHA-256 (Ctrl+Shift+S)", ref qx, quickBar, delegate
			{
				try { new EvidenceCaptureForm(targetPlayerName, targetModeratorName).Show(this); } catch { }
			});
			btnQuickRulebook = CreateQuickButton("⚖️ Правила 5.01-5.08 (F3)", ref qx, quickBar, delegate
			{
				try { new RuleBookViewerForm(targetPlayerName).Show(this); } catch { }
			});
			btnQuickClipboard = CreateQuickButton("📋 Буфер и PowerShell (Ctrl+Shift+C)", ref qx, quickBar, delegate
			{
				try { new ClipboardConsoleHistoryViewerForm(targetPlayerName).Show(this); } catch { }
			});

			// Панель категорий и поиска
			Panel filterBar = new Panel();
			filterBar.Dock = DockStyle.Top;
			filterBar.Height = 44;
			filterBar.BackColor = Ui.Backdrop;
			filterBar.Padding = new Padding(12, 6, 12, 6);
			this.Controls.Add(filterBar);

			int fx = 12;
			btnCatAll = CreateCategoryButton(string.Format("Все ({0})", allTools.Count), HubCategory.All, ref fx, filterBar);
			btnCatMemory = CreateCategoryButton("🧠 Память и Инжекторы", HubCategory.MemoryAndProcesses, ref fx, filterBar);
			btnCatFileSystem = CreateCategoryButton("🗄️ Файловая система", HubCategory.FileSystemAndRegistry, ref fx, filterBar);
			btnCatRecycle = CreateCategoryButton("🗑️ Корзина и Следы", HubCategory.RecycleAndStorage, ref fx, filterBar);
			btnCatNetwork = CreateCategoryButton("🌐 Сеть и Загрузки", HubCategory.NetworkAndDownloads, ref fx, filterBar);
			btnCatRules = CreateCategoryButton("🛡️ Регламент и Улики", HubCategory.RulesAndEvidence, ref fx, filterBar);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(filterBar.Width - 260, 8);
			searchBox.Size = new Size(245, 26);
			searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			searchBox.Font = Ui.FontBody(9.5f, false);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterBar.Controls.Add(searchBox);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(9f, false);
			lblSearch.Location = new Point(filterBar.Width - 315, 12);
			lblSearch.AutoSize = true;
			lblSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			filterBar.Controls.Add(lblSearch);

			// Нижняя панель действий и информации о выбранном инструменте
			Panel bottomPanel = new Panel();
			bottomPanel.Dock = DockStyle.Bottom;
			bottomPanel.Height = 110;
			bottomPanel.BackColor = Ui.Surface;
			bottomPanel.Padding = new Padding(18, 10, 18, 10);
			this.Controls.Add(bottomPanel);

			lblDetailName = new Label();
			lblDetailName.Text = "Выберите криминалистический инструмент для запуска.";
			lblDetailName.Font = Ui.FontTitle(11.5f, true);
			lblDetailName.ForeColor = Ui.TextPrimary;
			lblDetailName.Location = new Point(16, 10);
			lblDetailName.AutoSize = true;
			bottomPanel.Controls.Add(lblDetailName);

			lblDetailCategory = new Label();
			lblDetailCategory.Text = "";
			lblDetailCategory.Font = Ui.FontBody(9f, true);
			lblDetailCategory.ForeColor = Ui.AccentTeal;
			lblDetailCategory.Location = new Point(16, 36);
			lblDetailCategory.AutoSize = true;
			bottomPanel.Controls.Add(lblDetailCategory);

			lblDetailRule = new Label();
			lblDetailRule.Text = "";
			lblDetailRule.Font = Ui.FontTitle(9f, true);
			lblDetailRule.ForeColor = Ui.AccentRed;
			lblDetailRule.Location = new Point(220, 36);
			lblDetailRule.AutoSize = true;
			bottomPanel.Controls.Add(lblDetailRule);

			lblDetailHotkey = new Label();
			lblDetailHotkey.Text = "";
			lblDetailHotkey.Font = Ui.FontMono(9f, true);
			lblDetailHotkey.ForeColor = Ui.AccentOrange;
			lblDetailHotkey.Location = new Point(480, 36);
			lblDetailHotkey.AutoSize = true;
			bottomPanel.Controls.Add(lblDetailHotkey);

			lblDetailDescription = new Label();
			lblDetailDescription.Text = "";
			lblDetailDescription.Font = Ui.FontBody(9f, false);
			lblDetailDescription.ForeColor = Ui.TextSecondary;
			lblDetailDescription.Location = new Point(16, 60);
			lblDetailDescription.Size = new Size(bottomPanel.Width - 320, 42);
			lblDetailDescription.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
			bottomPanel.Controls.Add(lblDetailDescription);

			btnLaunchSelected = new DarkButton();
			btnLaunchSelected.Text = "▶  Запустить анализ (Enter)";
			btnLaunchSelected.Font = Ui.FontTitle(10.5f, true);
			btnLaunchSelected.AccentStyle = true;
			btnLaunchSelected.Location = new Point(bottomPanel.Width - 280, 30);
			btnLaunchSelected.Size = new Size(260, 48);
			btnLaunchSelected.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnLaunchSelected.Click += delegate { LaunchSelectedTool(); };
			bottomPanel.Controls.Add(btnLaunchSelected);

			// Таблица инструментов (ListView)
			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.GridLines = true;
			listView.MultiSelect = false;
			listView.Font = Ui.FontBody(9.5f, false);

			listView.Columns.Add("Инструмент криминалистики", 340);
			listView.Columns.Add("Сектор / Категория", 180);
			listView.Columns.Add("Горячая клавиша", 130);
			listView.Columns.Add("Цель / Регламент", 180);
			listView.Columns.Add("Криминалистическое назначение и метод обнаружения", 500);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			listView.DoubleClick += delegate { LaunchSelectedTool(); };
			this.Controls.Add(listView);
		}

		private DarkButton CreateQuickButton(string text, ref int curX, Panel parent, EventHandler onClick)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontTitle(8.5f, false);
			btn.Location = new Point(curX, 7);
			btn.Size = new Size(220, 28);
			btn.Click += onClick;
			parent.Controls.Add(btn);
			curX += 228;
			return btn;
		}

		private DarkButton CreateCategoryButton(string text, HubCategory cat, ref int curX, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Font = Ui.FontBody(8.5f, cat == HubCategory.All);
			btn.Location = new Point(curX, 7);
			btn.Size = new Size(150, 28);
			btn.Click += delegate
			{
				currentCategory = cat;
				HighlightCategoryButtons();
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			curX += 156;
			return btn;
		}

		private void HighlightCategoryButtons()
		{
			btnCatAll.AccentStyle = (currentCategory == HubCategory.All);
			btnCatMemory.AccentStyle = (currentCategory == HubCategory.MemoryAndProcesses);
			btnCatFileSystem.AccentStyle = (currentCategory == HubCategory.FileSystemAndRegistry);
			btnCatRecycle.AccentStyle = (currentCategory == HubCategory.RecycleAndStorage);
			btnCatNetwork.AccentStyle = (currentCategory == HubCategory.NetworkAndDownloads);
			btnCatRules.AccentStyle = (currentCategory == HubCategory.RulesAndEvidence);
		}

		#endregion

		#region Filtering & Display

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox.Text.Trim().ToLowerInvariant();

			foreach (ForensicToolItem item in allTools)
			{
				if (currentCategory != HubCategory.All && item.Category != currentCategory) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool matches = (item.Name != null && item.Name.ToLowerInvariant().Contains(search)) ||
								   (item.CategoryName != null && item.CategoryName.ToLowerInvariant().Contains(search)) ||
								   (item.Hotkey != null && item.Hotkey.ToLowerInvariant().Contains(search)) ||
								   (item.TargetRule != null && item.TargetRule.ToLowerInvariant().Contains(search)) ||
								   (item.Description != null && item.Description.ToLowerInvariant().Contains(search));
					if (!matches) continue;
				}

				ListViewItem lvi = new ListViewItem(item.Name);
				lvi.SubItems.Add(item.CategoryName);
				lvi.SubItems.Add(item.Hotkey);
				lvi.SubItems.Add(item.TargetRule);
				lvi.SubItems.Add(item.Description);
				lvi.ForeColor = item.AccentColor;
				lvi.Tag = item;

				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				ClearDetails();
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0)
			{
				ClearDetails();
				return;
			}

			ForensicToolItem item = listView.SelectedItems[0].Tag as ForensicToolItem;
			if (item == null) return;

			lblDetailName.Text = item.Name;
			lblDetailName.ForeColor = item.AccentColor;
			lblDetailCategory.Text = string.Format("Сектор: {0}", item.CategoryName);
			lblDetailRule.Text = string.Format("Регламент: {0}", item.TargetRule);
			lblDetailHotkey.Text = string.Format("Горячая клавиша: [{0}]", item.Hotkey);
			lblDetailDescription.Text = item.Description;
		}

		private void ClearDetails()
		{
			lblDetailName.Text = "Инструмент не выбран";
			lblDetailName.ForeColor = Ui.TextPrimary;
			lblDetailCategory.Text = "";
			lblDetailRule.Text = "";
			lblDetailHotkey.Text = "";
			lblDetailDescription.Text = "Выберите инструмент из списка для просмотра подробностей и запуска.";
		}

		private void LaunchSelectedTool()
		{
			if (listView.SelectedItems.Count == 0) return;
			ForensicToolItem item = listView.SelectedItems[0].Tag as ForensicToolItem;
			if (item == null || item.LaunchAction == null) return;

			try
			{
				item.LaunchAction();
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Ошибка запуска", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		#endregion
	}
}
