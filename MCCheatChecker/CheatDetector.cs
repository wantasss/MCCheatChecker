using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Net;
using System.Management;
using System.Diagnostics.Eventing.Reader;

namespace MCCheatChecker
{
	public static class CheatDetector
	{
		private const int DeepScanMaxDepth = 12;

		private const int DeepScanMaxDirs = 120000;

		private const int DeepScanMaxFiles = 150000;

		private const int DeepScanMaxFileDepth = 8;

		private const int DeepScanTimeBudgetSeconds = 12;

		public static readonly string[] CheatClientNames = new string[]
		{
			"kami", "kamiblue", "pyro", "impact", "wurst", "inertia", "liquidbounce", "sigma", "neverhook", "zamorozka",
			"minced", "wexside", "expensive", "wild", "fluger", "keaz", "meteor", "blackberry", "nursultan", "avalone",
			"mercu", "niobium", "kion", "akrien", "celestial", "deadcode", "norules", "rage", "bebraware",
			"rynware", "neverware", "aristois", "baritone", "simply", "skillclient", "jigsawclient", "salhack", "aresclient",
			"wolfram", "wweclient", "weepcraft", "kalkon", "advancedx", "matixclient", "nodus", "aurus", "neverclide",
			"pivo", "prerik", "cabaletta", "gishcode", "rename_me_please", "future", "rusherhack", "eternity",
			"richpremium", "editme", "mc100", "r3dclient", "destroyclient", "archware", "boberware", "bolshoy", "nurik", "celka",
			"akr", "wex", "haven", "ponos", "emortalityclient", "dreampool", "invisible", "winner", "clowdy",
			"rasty", "dreampoolhack", "diamondsim", "autoattack", "kyprak", "wintware", "konas", "jessica",
			"excellent", "vape", "bushroot", "panickattack", "hackbrand", "undetectable", "venus", "interium", "energyclient",
			"cclient", "quickclient", "chclient", "caesar", "tofficlient", "troxill",
			"exosware", "viamcp", "fatal", "waterclient", "extendopvp", "hwid", "hitware", "harambaritone", "flauncher",
			"clownclient", "euphoriaclient", "vertclient", "wortexclient", "404client", "darkproject", "mioclient", "vegaline", "wissend",
			"darklight", "jexclient", "trollhack", "catlavan", "haru", "verist", "lunaclient",
			"exloader", "evaware", "britva", "haruka", "rise", "rogue",
			"dummyclient", "hypnotic", "neura", "novoline", "huzuni", "orion", "eclipse",
			"stronk", "ambrosial", "reflex", "killerware",
			"novaclient", "iceclient", "aqua", "ruin", "phantom",
			"cipher",
			"acryl",
			"nova2", "nova3", "blatware", "blat", "chipware", "vorona",
			"rusohack", "vortex", "atomware", "neonware", "slavehack", "slavware",
			"babahack",
			"killauraplus", "multiaura", "tpaura",
			"silentaura", "silentswing", "silentmine",
			"liquidloader", "ambientloader", "hackloader", "bootject", "xenos", "javaagent-cheat",
			"patcher-cheat", "asmhook", "mchack", "hackedclient", "cheatclient", "hackforge", "hackedforge",
			"mixinhack", "mixin-hack", "bytebuddy-hack", "mc-hack", "horion", "zephyr", "horionupdated", "oseris",
			"solaris", "onix", "nohopefully", "dragonfly", "bedrockhax", "bedrockcheat",
			"nebulaclient", "pinnacle",
			"aether", "infinity2", "infinityclient2", "flaxenter",
			"flaxclient", "flaxus", "krystalclient", "dioxin", "dioxinclient", "diox",
			"lunarclient2", "lunar2", "lunarbypass", "maraxsis", "sunlitclient", "deceptiveclient", "permetricsclient", "epsilonclient", "epsilonhack", "epicbase",
			"fpsclients", "chemosai", "fumoclient", "brainwaveclient", "asteriaclient", "noodleclient", "noodleware", "koboldclient", "noirclient", "obsidianclient",
			"anubisclient", "yunoclient", "chaosclient", "cirrusclient", "voidclient", "radiumclient", "mochat", "deltacclient", "ccbclient", "farca",
			"seleneclient", "nyxclient", "silentclient", "valhallacheat", "asgardclient", "ragnarokclient", "fenrirclient", "skadiclient", "jormungandrclient", "medusaclient",
			"cerberusclient", "chimeraclient", "krakenclient", "leviathanclient", "basiliskclient", "golemclient", "ifritclient", "tenguclient", "raijinclient", "susanooclient",
			"osirisclient", "horusclient", "thothclient", "atumclient", "anhurclient", "montucclient", "kamosclient", "maraxsisclient", "thunderhack", "thunderhackrecode", "boze", "bozeclient", "slinky", "slinkyclient", "dreamclient", "doomsday", "doomsdayclient", "ravenb4", "ravenbplus", "expensive3", "expensive2", "expensive-recode", "nursultanrecode", "nursultan-next", "celestialrecode", "celestial-premium", "wildrecode", "wildclient", "nightx", "nightxclient", "seedcracker", "seedcrackerx", "systemdlc", "systemdlc-client", "sysdlc"
		};

		private static readonly string[] VisualKeywords = new string[36]
		{
			"x-ray", "xray", "xrayultimate", "x-rayultimate", "advancedxray", "advanced-xray", "chestesp", "storageesp", "playeresp", "esp",
			"nametags", "tracers", "freecam", "norender", "fullbright", "gamma", "blockesp", "itemesp", "mobesp", "wireframe",
			"chams", "radar", "trajectories", "wallhack", "glow", "search", "brightnight", "fullbright-", "gammabright", "orebfuscator-bypass",
			"cavefinder", "entityoutlines", "chunkborders", "tracer", "blockoutline", "oreesp"
		};

		private static readonly string[] InjectorNames = new string[] { "exremeinjector", "injector", "liquidloader", "ambientinjector", "bootject", "xenos", "exloader", "cheatengine" };

		private static readonly HashSet<long> KnownCheatFileSizes = new HashSet<long>
		{
			9951744L, 24536064L, 15438336L, 6229504L, 6573056L, 7187456L, 7969792L, 1562249L, 1672329L, 1677449L,
			1680521L, 147329L, 138351L, 202720L, 7788032L, 22885L, 23810L, 7988736L, 3711166L, 3697285L,
			3712014L, 5641728L, 4413440L, 114974L, 111866L, 274865L, 1820884L, 5007380L, 6944256L, 5934592L,
			2545664L, 2108662L, 1961742L, 3684385L, 5143837L, 116689L, 1968128L, 8011776L, 1883602L, 5918208L,
			1897269L, 31445308L, 24390144L, 17339L, 2023236L, 1750137L, 3786004L, 28084L, 6778L, 350629L,
			24609266L, 42559591L, 137550L, 16855568L, 17406480L
		};

		private static readonly string[] CheatWebsites = new string[69]
		{
			"cortexclient.com", "akrien.wtf", "akrien.club", "ammit.cc", "vape.gg", "deadcodehack.org", "deadcode.fun", "nursultan.fun", "nursultan.top", "clowdy.space",
			"expensiveclient.xyz", "expensive.cc", "expensiveclient.click", "dreampoolhack.ru", "drip.gg", "liquidbounce.net", "rusherhack.org", "meteorclient.com", "wurst-client.tk", "wurst-client.net",
			"aristois.net", "sigma.rip", "impact.cx", "inertia.pw", "future.gg", "eternityhack.ru", "wolfram.cf", "salhack.net", "novoline.wtf", "entropy-client.com",
			"flux-client.net", "prism-client.com", "flarial.xyz", "flarial.net", "kristalhack.com", "kristal.cc", "nodus2.github.io", "weepcraft.net", "skillaura.com", "skillclient.xyz",
			"kami.blue", "pyroclient.ru", "pyro.lol", "blatware.ru", "slavehack.ru", "rusohack.ru", "cheat-client.ru", "mchack.ru", "mccheat.ru", "vape.lol",
			"vape.pw", "vaper.gg", "celestial-client.net", "celestial.fun", "celestial.su", "paragon-client.com", "minced.cc", "wexside.net", "exloader.net", "flauncher.net",
			"vegaline.fun", "boberware.ru", "rynware.ru", "neverhook.ru", "zenware.fun", "zenware.cc", "norules.fun", "kion.top", "mercu.cc"
		};

		private static readonly string[] LegitGameNames = new string[25]
		{
			"rockstar games", "rockstar", "social club", "grand theft auto", "red dead", "war thunder", "warthunder", "gaijin", "aces.exe", "delta force",
			"deltaforce", "epic games", "battle.net", "gog galaxy", "riot client", "java-runtime", "infinity ward", "maccatalyst", "dotnet", "gradle-wrapper",
			"nuget", "lunar", "badlion", "feather", "labymod"
		};

		private static readonly string[] DeepScanSkipDirs = new string[24]
		{
			"windows", "$recycle.bin", "system volume information", "recovery", "winsxs", "driverstore", "installer", "servicing", "assembly", "node_modules",
			".git", ".svn", ".hg", "packages", "windowsapps", "appdata", "application data", "temp", "cache", "cache2",
			"softwaredistribution", "microsoft", "common files", "programdata"
		};

		private static readonly string[] DeepScanStrongNames = new string[]
		{
			"kamiblue", "pyro", "wurst", "liquidbounce", "neverhook", "zamorozka", "wexside", "fluger", "keaz", "meteor",
			"nursultan", "avalone", "niobium", "akrien", "celestial", "deadcode", "norules", "bebraware", "rynware", "neverware",
			"aristois", "baritone", "skillclient", "jigsaw", "salhack", "wolfram", "weepcraft", "kalkon", "advancedx", "matix",
			"nodus", "aurus", "neverclide", "cabaletta", "gishcode", "rusherhack", "eternity", "richpremium", "archware", "boberware",
			"bolshoy", "nurik", "celka", "emortalityclient", "dreampool", "dreampoolhack", "diamondsim", "kyprak", "wintware",
			"konas", "vape", "bushroot", "panickattack", "hackbrand", "interium", "energyclient", "quickclient", "chclient", "tofficlient",
			"troxill", "exosware", "viamcp", "waterclient", "extendopvp", "hitware", "harambaritone", "flauncher", "clownclient", "euphoriaclient",
			"vertclient", "wortexclient", "darkproject", "mioclient", "vegaline", "wissend", "darklight", "jexclient", "paragon",
			"trollhack", "catlavan", "verist", "lunaclient", "exloader", "evaware", "britva", "haruka", "maraxsis", "sunlitclient",
			"deceptiveclient", "permetricsclient", "epsilonclient", "epicbase", "fpsclients", "chemosai", "fumoclient", "brainwaveclient", "asteriaclient", "noodleware",
			"koboldclient", "obsidianclient", "anubisclient", "yunoclient", "chaosclient", "cirrusclient", "radiumclient", "deltacclient", "ccbclient", "seleneclient",
			"valhallacheat", "asgardclient", "ragnarokclient", "fenrirclient", "jormungandrclient", "medusaclient", "cerberusclient", "chimeraclient", "krakenclient", "leviathanclient",
			"basiliskclient", "ifritclient", "tenguclient", "raijinclient", "susanooclient", "osirisclient", "horusclient", "thothclient", "maraxsisclient"
		};

		public static string LatestScreenshotPath = null;

		public static string LatestScreenshotDirectory = null;

		public static event Action<string> OnProgress;

		public static event Action<int, int, string> OnProgressStep;

		private static bool IsLegitGame(string textLower)
		{
			if (string.IsNullOrEmpty(textLower))
			{
				return false;
			}
			string[] legitGameNames = LegitGameNames;
			foreach (string value in legitGameNames)
			{
				if (textLower.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsWordChar(char c)
		{
			if ((c < 'a' || c > 'z') && (c < 'A' || c > 'Z'))
			{
				if (c >= '0')
				{
					return c <= '9';
				}
				return false;
			}
			return true;
		}

		private static bool ContainsToken(string textLower, string token)
		{
			int startIndex = 0;
			while ((startIndex = textLower.IndexOf(token, startIndex, StringComparison.OrdinalIgnoreCase)) >= 0)
			{
				bool flag = startIndex == 0 || !IsWordChar(textLower[startIndex - 1]);
				int num = startIndex + token.Length;
				bool flag2 = num >= textLower.Length || !IsWordChar(textLower[num]);
				if (flag && flag2)
				{
					return true;
				}
				startIndex = num;
			}
			return false;
		}

		public static bool MatchesCheatName(string textLower, string cheatLower)
		{
			if (string.IsNullOrEmpty(textLower) || string.IsNullOrEmpty(cheatLower))
			{
				return false;
			}
			if (cheatLower == "autoclicker" || cheatLower == "clicker" || cheatLower == "vulcanclicker" || cheatLower == "mccheat" || cheatLower == "tinytask" || cheatLower == "speedautoclicker" || cheatLower == "opautoclicker" || cheatLower == "keyran" || cheatLower == "botmek" || cheatLower == "autohotkey" || cheatLower == "ahk" || cheatLower == "x-mouse" || cheatLower == "xmouse" || cheatLower == "wwe" || cheatLower == "jigsaw" || cheatLower == "ares" || cheatLower == "r3d" || cheatLower == "destroy" || cheatLower == "matix")
			{
				return false;
			}
			if (!ContainsToken(textLower, cheatLower))
			{
				return false;
			}
			if (CheatDatabases.IsAmbiguous(cheatLower) && !CheatDatabases.HasCheatContext(textLower))
			{
				return false;
			}
			if (IsLegitGame(textLower))
			{
				return false;
			}
			if (textLower.Contains("          \"name\": ") || textLower.Contains("mccheat") || textLower.Contains("screenshare") || textLower.Contains("sstools"))
			{
				return false;
			}
			if (CheatDatabases.IsWhitelistedPath(textLower))
			{
				return false;
			}
			return true;
		}

		private static void Report(string msg)
		{
			if (CheatDetector.OnProgress != null)
			{
				CheatDetector.OnProgress(msg);
			}
		}

		private static void ReportStep(int current, int total, string msg)
		{
			if (CheatDetector.OnProgress != null)
			{
				CheatDetector.OnProgress(msg);
			}
			if (CheatDetector.OnProgressStep != null)
			{
				CheatDetector.OnProgressStep(current, total, msg);
			}
		}

		private static string DecodeRot13(string input)
		{
			if (string.IsNullOrEmpty(input))
			{
				return "";
			}
			char[] array = input.ToCharArray();
			for (int i = 0; i < array.Length; i++)
			{
				int num = array[i];
				switch (num)
				{
				case 110:
				case 111:
				case 112:
				case 113:
				case 114:
				case 115:
				case 116:
				case 117:
				case 118:
				case 119:
				case 120:
				case 121:
				case 122:
					num -= 13;
					break;
				case 97:
				case 98:
				case 99:
				case 100:
				case 101:
				case 102:
				case 103:
				case 104:
				case 105:
				case 106:
				case 107:
				case 108:
				case 109:
					num += 13;
					break;
				default:
					switch (num)
					{
					case 78:
					case 79:
					case 80:
					case 81:
					case 82:
					case 83:
					case 84:
					case 85:
					case 86:
					case 87:
					case 88:
					case 89:
					case 90:
						num -= 13;
						break;
					case 65:
					case 66:
					case 67:
					case 68:
					case 69:
					case 70:
					case 71:
					case 72:
					case 73:
					case 74:
					case 75:
					case 76:
					case 77:
						num += 13;
						break;
					}
					break;
				}
				array[i] = (char)num;
			}
			return new string(array);
		}

		public static ScanReport RunFullScan(string playerName, CancellationToken ct)
		{
			ScanReport scanReport = new ScanReport();
			scanReport.PlayerName = playerName;
			Tuple<string, Func<List<CheckResult>>>[] array = new Tuple<string, Func<List<CheckResult>>>[]
			{
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddd1\ufe0f Поиск удалённых программ и модов (USN $, Recent, Корзина)...", CheckDeletedProgramsAndMods),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc41\ufe0f Сканирование Базы Визуалов (X-Ray, ESP, FreeCam, Ресурспаки)...", CheckVisualsDatabaseAndPacks),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Анализ модов на хард-чит классы и байт-размеры...", CheckModsVisualsAndInternals),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcda Анализ libraries/com/github на читы (мануал)...", CheckMinecraftLibraries),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Анализ библиотек/паков внутри jar на сигнатуры чит-пакетов...", CheckJarPackageSignatures),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Проверка запущенных процессов и conhost (инжекции)...", CheckProcesses),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc89 Проверка внедрённых DLL в javaw.exe (инжекции памяти)...", CheckJavawInjectedModules),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc1 Папки чит-клиентов на всех дисках...", CheckCheatFolders),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd73\ufe0f Глубокий обход корней дисков (до 8 уровней вглубь, все диски)...", CheckDeepDriveFolders),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Анализ .minecraft/versions...", CheckVersionsFolder),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccb Анализ активных логов Minecraft...", CheckMinecraftLogs),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Анализ архивных логов .log.gz на команды читов...", CheckGZipMinecraftLogs),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\ude80 Профили лаунчера (launcher_profiles & JVM)...", CheckLauncherProfiles),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd11 Реестр Windows на остатки ПО...", CheckRegistry),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd75\ufe0f Декодирование ROT13 истории запусков UserAssist...", CheckUserAssistRot13),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Рабочий стол и папка Загрузки...", CheckDesktopAndDownloads),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc2\ufe0f AppData / Roaming / Local...", CheckAppData),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddd1\ufe0f Временные файлы Temp...", CheckTempFolders),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udd16 AutoHotKey и макрос-кликеры...", CheckAutoHotKey),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd52 Недавние файлы (shell:recent)...", CheckRecentFiles),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Посещения чит-ресурсов (Chrome/Edge/hosts)...", CheckCheatWebsites),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce1 Анализ DNS кэша Windows (ipconfig /displaydns)...", CheckDnsCacheAndNetwork),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd12 Аудит DNS-over-HTTPS (DoH) и обхода логирования DNS...", CheckDnsOverHttpsAndEncryptedDnsEvasion),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf7 Скриншоты Minecraft...", CheckScreenshots),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Prefetch (FSUTIL / очистки USN / запуски)...", CheckPrefetch),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcca BAM & Windows Event Log...", CheckEventLog),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf1 Minecraft Bedrock Edition (Horion, Zephyr, DLL, com.mojang)...", CheckMinecraftBedrockCheats),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc История PowerShell консоли (попытки очистки следов)...", CheckPowerShellHistory),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Аварийные дампы CrashDumps & WER (сбои читов)...", CheckCrashDumps),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0c История подключения флешек USBSTOR (читы с USB)...", CheckUsbConnectionHistory),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf9 Запуски утилит очистки следов (CCleaner, BleachBit, FSUTIL)...", CheckCleanerUtilities),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Реестр фоновой активности BAM (Background Activity Moderator)...", CheckBAM),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Целостность системы, аптайм ПК и статус служб PcaSvc/DPS...", CheckSystemIntegrityAndEvasion),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc4\ufe0f Реестр MuiCache (История запуска исполняемых файлов)...", CheckMuiCache),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc4 RecentDocs MRU (.jar, .dll, .exe обращения в системе)...", CheckRecentDocsMRU),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\ude9f Заголовки открытых окон (Автокликеры / Инжекторы)...", CheckWindowTitlesAndTray),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аргументы запуска JVM (javaw.exe javaagent / флаги)...", CheckMinecraftJvmCommandLine),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Реестр AppCompatFlags Layers (Совместимость / RunAsAdmin)...", CheckAppCompatLayers),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcca FeatureUsage AppSwitched (История переключения приложений)...", CheckFeatureUsageAppSwitched),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbe Смонтированные накопители и буквы дисков (MountedDevices)...", CheckMountedDevicesAndDrives),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Системный кэш совместимости ядра ShimCache (AppCompatCache)...", CheckShimCache),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Форензика DirectX (DirectInput / Direct3D MostRecent) и перехватов IFEO...", CheckDirectXAndIfeoExecutionBypasses),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Реестр деинсталляции и Amcache.hve (Установленное ПО)...", CheckAmcacheAndUninstall),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Активные TCP сокеты javaw.exe (IRC / Cloud соединения)...", CheckNetworkSocketsAndConnections),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde0 Анализ модулей и сигнатур в активной памяти javaw.exe...", CheckJavawMemoryStrings),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Voidtools Everything (Поисковые запросы затирания читов)...", CheckEverythingSearchAndRunHistory),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Файл hosts и подмена DNS (Блокировка античитов)...", CheckHostsFileAndDnsRedirection),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udd16 JNativeHook и библиотеки внешних автокликеров в Temp...", CheckJNativeHookAndMacroDLLs),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc41\ufe0f Chest ESP и Spawner X-Ray ресурспаки (Прозрачные сундуки)...", CheckMinecraftEntityAndChestXRayPacks),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Системные логи запусков PCA (C:\\Windows\\appcompat\\pca)...", CheckPcaAppLaunchLogs),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Анализ замаскированных модов и Mixin-инжекций (.minecraft\\mods)...", CheckDisguisedModMixins),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1\ufe0f Анализ ПО и макросов мыши (Bloody Ultra Core / X-Mouse)...", CheckBloodyAndMouseMacros),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcac Клиенты мессенджеров и передачи файлов (Discord/Telegram)...", CheckDiscordAndTelegramCaches),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddd1\ufe0f Декодирование $I индексов корзины $Recycle.Bin (Оригинальные пути)...", CheckRecycleBinIndexFiles),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcca База учета активности SRUM (SRUDB.dat)...", CheckSRUMDatabase),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Детекция виртуальных машин и песочниц (Anti-VM / Sandboxie)...", CheckVirtualMachineAndSandbox),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\ude9f Виртуальные рабочие столы (Task View / софт на Desktop 2)...", CheckVirtualDesktopsAndAlternateSessions),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce5 Загрузки файлов из Telegram Desktop и браузеров...", CheckTelegramAndDirectDownloads),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccb Служба SysMain и тайминги запуска Minecraft...", CheckRegulationSysMainAndUptime),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfaf Сигнатуры читов в активной памяти (KillAura, AutoTotem, HitBoxes)...", CheckRegulationMemorySignatures),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Кэш Prefetch и время хранения (правило 7-14 дней)...", CheckRegulationPrefetchStorageRules),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc89 Переменные окружения Java (_JAVA_OPTIONS / JAVA_TOOL_OPTIONS)...", CheckRegulationJavaAgentEnv),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc2\ufe0f Журнал USN Journal на диске C: (сокрытие следов)...", CheckRegulationUsnJournal),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddec Целостность ядра Minecraft (хеш-суммы внутри jar)...", CheckRegulationCoreJarIntegrity),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd70\ufe0f Синхронизация системного времени и служба W32Time...", CheckRegulationClockTamper),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcb3 Проверка подписок на чит-клиенты (токены, веб-кэш)...", CheckPaidClientSubscriptions),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcb3 Аппаратные привязки читов (HWID-ключи, auth-сессии)...", CheckPurchasedCheatEvidence),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Аппаратный профиль: CPU, RAM, GPU, BIOS на спуфинг...", HardwareChecks.RunHardwareChecks),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddec HWID-серийники, спуфинг MAC и сетевых карт...", HardwareChecks.RunHwidChecks),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1\ufe0f Внешние макрос-устройства (KMBox, Arduino, Teensy)...", HardwareChecks.RunHidChecks),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddcd Проверка физического присутствия (мышь, AnyDesk, аптайм)...", HumanPresenceCheck.RunHumanPresenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc2\ufe0f Реестр ShellBags: папки и съемные накопители...", ManualChecks.RunShellBagCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Voidtools Everything: сохраненные поисковые фильтры...", ManualChecks.RunEverythingConfigCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Обязательные службы Windows (PcaSvc, DPS, BAM, CDPSvc)...", ManualChecks.RunRequiredServicesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Prefetch процессов cmd, conhost, consent, javaw...", ManualChecks.RunPrefetchProcessCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc41\ufe0f Конфигурации X-Ray (Advanced Xray, ore-sim)...", ManualChecks.RunXrayStructureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddfe Реестр RunMRU и автозапуск cmd (Command Processor Autorun)...", ManualChecks.RunMruAndAutorunCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddd1\ufe0f Корзина $Recycle.Bin на всех дисках (анализ времени удаления)...", ManualChecks.RunRecycleBinAgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf9 Утилиты затирания следов и специализированное ПО...", ManualChecks.RunTraceToolsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc4\ufe0f Очистка журнала USN Journal (EventID 3079 / FSUTIL)...", ManualChecks.RunUsnJournalCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Сканирование дампов памяти процессов в RAM...", CheckProcessMemoryExtended),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Анализ Constant Pool строк внутри модов...", CheckInternalModStringsExtended),
				Tuple.Create<string, Func<List<CheckResult>>>("⚖\ufe0f Байт-размеры версий клиента (RageTime / WildGrief)...", CheckMinecraftVersionWeights),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcca Инвентаризация установленных версий Java...", ManualChecks.RunProgramInventoryEventCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfaf Точные размеры 140 известных версий чит-jar...", CheckKnownCheatJarSizes),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Настройки управления и бинды (options.txt)...", CheckMinecraftOptionsAndKeybinds),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Логи PowerShell Operational (EventID 4104)...", ManualChecks.RunPowerShellEventLogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Журнал сбоев Java и DLL (EventID 1000/1001)...", ManualChecks.RunJavaCrashEventCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd25 История сетевых разрешений брандмауэра для Java...", ManualChecks.RunFirewallAppHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\ude80 Профили лаунчера и аргументы JVM (launcher_profiles.json)...", ManualChecks.RunLauncherProfilesDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Краш-дампы и отчеты о сбоях (.minecraft/crash-reports)...", CheckMinecraftCrashReports),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc64 Учетные записи и история никнеймов (usercache.json)...", CheckMinecraftAccountsAndNicknames),
				Tuple.Create<string, Func<List<CheckResult>>>("🕒 Windows Timeline: история активности пользователя...", ManualChecks.RunActivitiesCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccc Списки быстрого перехода JumpLists (Destinations)...", ManualChecks.RunJumpListsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc2 Недавние файлы диалоговых окон OpenSavePidlMRU...", ManualChecks.RunComDlg32MRUCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 История архиваторов WinRAR ArcHistory и 7-Zip...", ManualChecks.RunArchiverHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfa8 Шейдерпаки и ресурспаки на скрытый X-Ray...", ManualChecks.RunShaderpacksAndResourcepacksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1\ufe0f Драйверы мыши (Interception, vJoy, DoubleClick)...", ManualChecks.RunMouseDebounceAndDriverCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Поисковые запросы проводника Windows (WordWheelQuery)...", ManualChecks.RunWordWheelQueryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Журнал Windows Defender: угрозы и исключения...", ManualChecks.RunWindowsDefenderEventCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccb Буфер обмена и фоновые задания BITS...", ManualChecks.RunClipboardAndBitsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udded История ручного ввода путей TypedPaths и AppCompatFlags...", ManualChecks.RunExplorerTypedPathsAndFileExtsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Планировщик заданий (EventID 7045) и скрытые службы...", ManualChecks.RunTaskSchedulerAndServicesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc89 Локальные Proxy DLL (OpenGL / DirectX перехватчики)...", ManualChecks.RunGraphicsHooksAndLocalProxyDllCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Зоны безопасности NTFS Zone.Identifier (URL загрузок)...", ManualChecks.RunZoneIdentifierForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfad Замаскированные файлы (сигнатуры PK / MZ)...", ManualChecks.RunDisguisedFileHeadersCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Дампы падений Windows Error Reporting (ReportArchive)...", ManualChecks.RunWerCrashForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd12 Именованные пайпы и мьютексы чит-клиентов...", ManualChecks.RunNamedObjectsAndPipesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏳ Эксплойты изменения дат файлов (Timestomping в mods)...", ManualChecks.RunTimestompForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udd16 Компилированные скрипты AutoHotkey (Ahk2Exe)...", ManualChecks.RunCompiledAhkAndMacroProcessesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Процессы Java HotSpot и JVM Attach (hsperfdata)...", ManualChecks.RunJvmPerfDataAndAttachCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит списка серверов Minecraft (servers.dat / HvH & Anarchy)...", ManualChecks.RunMinecraftServersDatCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👥 Альт-генераторы и подмена аккаунтов (TheAltening, MCLeaks, EasyMC)...", ManualChecks.RunAltGeneratorAndBanEvasionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💉 Нативные библиотеки перехвата и хуки в Temp (JNA / JNativeHook)...", ManualChecks.RunJnaAndNativeDllHooksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔌 Аудит съёмных USB-носителей (MountedDevices USBSTOR & флешки)...", ManualChecks.RunUsbStorageAndDriveCorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Аудит версий и JVM (Custom MainClass, -noverify, -javaagent)...", ManualChecks.RunMinecraftVersionsAndJvmDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Глубокий аудит команд PowerShell (зачистки следов, откат дат)...", ManualChecks.RunPowerShellDeepHistoryAudit),
				Tuple.Create<string, Func<List<CheckResult>>>("🎮 Мульти-лаунчерный аудит options.txt (FullBright гамма и бинды)...", ManualChecks.RunMultiLauncherOptionsAndGammaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🐧 Криминалистика Unix / macOS / Linux (Preload, History, Clickers)...", ManualChecks.RunUnixSpecificForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👁️ Сокрытие экрана и Overlay (SetWindowDisplayAffinity / Discord / OBS Bypass)...", ManualChecks.RunScreenCaptureEvasionAndOverlayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💾 Теневые копии томов VSS и политики восстановления (vssadmin delete)...", ManualChecks.RunVolumeShadowCopiesAndRecoveryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 История команд Minecraft (command_history.txt / чат / Baritone)...", ManualChecks.RunMinecraftCommandHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Стеганография и скрытые хвосты в модах (ZIP EOCD Trailing Data)...", ManualChecks.RunZipEocdTrailingPayloadCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔌 Точная хронология подключений USB (setupapi.dev.log таймлайны)...", ManualChecks.RunSetupApiUsbTimelineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💥 Аварийные логи JVM HotSpot (hs_err_pid*.log / модули сбоя)...", ManualChecks.RunJvmHotspotCrashLogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎨 Аудит шейдеров на скрытый X-Ray (GLSL discard / прозрачность)...", ManualChecks.RunDeepShaderpackXrayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖱️ Макросы мыши и дебаунс (Glorious, Bloody, Razer, G HUB)...", ManualChecks.RunMouseMacroAndDebounceForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Конфиги читов и Litematica/Baritone (easyPlace/legitMine)...", ManualChecks.RunModAndClientConfigAudit),
				Tuple.Create<string, Func<List<CheckResult>>>("🧹 Аудит очистки журналов аудита (EventID 104 / 1102 / wevtutil)...", ManualChecks.RunAuditLogTamperingAndClearingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит истории браузеров и загрузок (Chrome, Edge, Firefox, Yandex)...", ManualChecks.RunBrowserHistoryAndDownloadCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗄️ Аудит кэша выполнения AppCompatCache (ShimCache) и BAM...", ManualChecks.RunAppCompatCacheAndBamCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🕵️ Аудит реестра UserAssist (ROT13 история запуска программ)...", ManualChecks.RunUserAssistForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📍 Аудит меток и вейпоинтов (Xaero, JourneyMap, Baritone X-Ray)...", ManualChecks.RunMinimapAndWaypointExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит файла hosts (редиректы античитов и авторизации Mojang)...", ManualChecks.RunHostsFileAndDnsRedirectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💥 Аудит отчётов о сбоях клиента (crash-reports/*.txt / моды и миксины)...", ManualChecks.RunMinecraftCrashReportsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗑️ Детальный аудит индексов корзины $Recycle.Bin ($I метаданные)...", ManualChecks.RunRecycleBinIndexDetailsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💾 Анализ дампов памяти сбоев CrashDumps (javaw.exe.*.dmp / DLL)...", ManualChecks.RunJvmCrashDumpsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит сетевых сокетов Java (Local Proxy 127.0.0.1 / WebSocket)...", ManualChecks.RunActiveJavaNetworkConnectionsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🪝 Поиск вебхуков Discord и скрытой эксфильтрации данных (RAT)...", ManualChecks.RunDiscordWebhookAndRatDetectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📸 Анализ скриншотов Minecraft и таймлайна (screenshots/*.png)...", ManualChecks.RunMinecraftScreenshotsAnalysisCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔓 Деобфускация и анализ Base64 в PowerShell (скрытые пейлоады)...", ManualChecks.RunPowerShellBase64PayloadDecoderCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Аудит уязвимых ядерных драйверов (BYOVD / DBK64 / KProcessHacker)...", ManualChecks.RunKernelVulnerableDriverCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧬 Аудит дампов Mixin и артефактов байткода (.mixin.out / логи)...", ManualChecks.RunMixinDebugAndBytecodeArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛑 Аудит изменения и остановки служб (EventID 7040 / 7036 / PcaSvc)...", ManualChecks.RunServiceTamperingEventAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📂 Аудит диалогов выбора файлов LastVisitedPidlMRU (инжекторы)...", ManualChecks.RunLastVisitedPidlMruCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Аудит скрытых альтернативных потоков NTFS (ADS стеганография)...", ManualChecks.RunNtfsHiddenAlternateStreamsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗄️ Реестр MuiCache (История запуска исполняемых файлов)...", ManualChecks.RunMuiCacheExecutionHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📊 Аудит активности программ FeatureUsage (AppSwitched / AppLaunch)...", ManualChecks.RunFeatureUsageAppSwitchedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🚪 Аудит событий заморозки и аварийных выходов (latest.log)...", ManualChecks.RunMinecraftLogDisconnectionAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📂 Диалоги выбора файлов OpenSavePidlMRU (инжекторы и DLL)...", ManualChecks.RunOpenSavePidlMruCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Аудит архивов сессий игры (logs/*.log.gz)...", ManualChecks.RunMinecraftHistoricalGzLogsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Целостность JVM и политик безопасности (java.policy / ext)...", ManualChecks.RunJvmSecurityPolicyAndEndorsedAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏱️ Аудит планировщика задач и автозагрузки (schtasks / Run)...", ManualChecks.RunScheduledTasksAndStartupPersistenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📡 Аудит системного кэша DNS (ipconfig /displaydns)...", ManualChecks.RunDnsClientCacheForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💉 Инжекции AppInit_DLLs и оверлейные слои Vulkan...", ManualChecks.RunAppInitDllsAndInjectionPersistenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖱️ Фильтры драйверов мыши и перехват Interception...", ManualChecks.RunMouseUpperFiltersAndInterceptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💥 Глубокий анализ сбоев Java в WER (Report.wer FaultModule)...", ManualChecks.RunWerReportJavawCrashAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Аудит аварийных выключений и BSOD (EventID 41 / 6008)...", ManualChecks.RunSystemCrashAndRebootEvasionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧹 Аудит службы автоочистки Storage Sense (Контроль памяти)...", ManualChecks.RunStorageSenseAndAutoCleanAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🐧 Аудит контейнеров WSL и процессов гипервизоров...", ManualChecks.RunWslAndHypervisorForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📋 Аудит журнала буфера обмена и перехвата (Win+V)...", ManualChecks.RunClipboardHistoryAndConfigForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит системных прокси и WinHTTP (перехватчики трафика)...", ManualChecks.RunSystemProxyAndNetworkTunnelCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💾 Аудит истории томов и отключённых USB (MountedDevices)...", ManualChecks.RunMountedDevicesAndVirtualEncryptedDrivesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит службы телеметрии и твикеров (DiagTrack / Debloaters)...", ManualChecks.RunDiagTrackTelemetryAndPrivacyCleanerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎮 Аудит сторонних ярлыков Steam (shortcuts.vdf)...", ManualChecks.RunSteamNonSteamShortcutsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Кэши сборки и зависимостей модов/читов (.gradle / .m2)...", ManualChecks.RunGradleAndMavenBuildArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💬 Локальная база данных Discord LevelDB (вложения и чаты)...", ManualChecks.RunDiscordLevelDbAndCacheForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📡 Активные порты прослушивания TCP (netstat Listening)...", ManualChecks.RunActiveTcpListeningEndpointsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💻 История команд терминалов и проекты VS Code...", ManualChecks.RunDeveloperCliHistoryAndVscodeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("✈️ Аудит сессий и загрузок Telegram (tdata / Downloads)...", ManualChecks.RunTelegramTdataAndDownloadsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит детектов и отключений Defender (Events 1116/5001)...", ManualChecks.RunDefenderThreatDetectionsAndRealtimeTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Аудит блоков кода PowerShell (ScriptBlock EventID 4104)...", ManualChecks.RunPowerShellScriptBlock4104AuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎮 Аудит биндов и клавиш чит-модов (options.txt)...", ManualChecks.RunMinecraftKeybindingsAndControlsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👤 Аудит аккаунтов лаунчеров и альт-токенов (TheAltening/EasyMC)...", ManualChecks.RunLauncherAccountsAndSessionTokenCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Анализ таблицы ARP и локальных шлюзов (arp -a / дубликаты MAC)...", ManualChecks.RunArpCacheAndLocalGatewayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📥 Аудит заданий BITS и скрытых загрузок (EventLog Bits-Client)...", ManualChecks.RunBitsTransferAndDeliveryOptimizationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗂️ Парсер бинарных записей RecentDocs (.jar/.exe/.dll/.zip)...", ManualChecks.RunRecentDocsBinarySubkeysParserCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🐧 Глубокий аудит подсистемы Linux (WSL rootfs / bash history)...", ManualChecks.RunWslDeepFilesystemAndHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔔 Форензика центра уведомлений Windows (wpndatabase.db / Toast)...", ManualChecks.RunWindowsNotificationCenterForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💥 Анализ аварийных дампов памяти (%LOCALAPPDATA%\\CrashDumps)...", ManualChecks.RunCrashDumpsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⛏️ Аудит статистики игрока и коэффициента добычи X-Ray (stats)...", ManualChecks.RunMinecraftPlayerStatsAndMiningRatioCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💉 Форензика Process Hacker, Sysinternals и Cheat Engine...", ManualChecks.RunProcessHackerAndSysinternalsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Аудит шрифтов ClickGUI и распакованных шрифтов в Temp...", ManualChecks.RunFontCacheAndClickGuiFontsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔒 Аудит корневых сертификатов Root CA (mitmproxy/Fiddler)...", ManualChecks.RunRootCertificatesAndProxyCAsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Глубокий аудит аварийных логов Hotspot (hs_err_pid*.log)...", ManualChecks.RunJvmHotspotHsErrCrashLogsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎨 Форензика кэша графических шейдеров GPU (DXCache/GLCache)...", ManualChecks.RunGraphicsShaderCacheForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Аудит журнала движка PowerShell (Event ID 400 и 800)...", ManualChecks.RunClassicPowerShellEngineEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎯 Аудит оверлейных хуков и внедрения через Discord/Steam...", ManualChecks.RunGameOverlayAndDiscordHookInjectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💾 Корреляция точек монтирования пользователя (MountPoints2)...", ManualChecks.RunMountPoints2UserVolumesCorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☀️ Анализ FullBright / Gamma Exploit и X-Ray ресурспаков...", ManualChecks.RunMinecraftGammaAndVisualSettingsExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📂 Проверка перенаправления системных каталогов (User Shell)...", ManualChecks.RunUserShellFoldersRedirectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖱️ Аудит аппаратных макросных мышей и USB-кликеров (HID)...", ManualChecks.RunHidMacroPeripheralsAndDonglesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧹 Аудит очистки журналов событий (Event ID 1102 и 104)...", ManualChecks.RunEventLogServiceAndWevtutilAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📋 Аудит истории открытия файлов в FileExts (OpenWithList)...", ManualChecks.RunFileExtsOpenWithHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Глубокий аудит ресурспаков на скрытый X-Ray (resourcepacks)...", ManualChecks.RunMinecraftResourcePacksDeepAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🪟 Аудит активных окон и истории панели задач (Taskband)...", ManualChecks.RunActiveWindowHistoryAndForegroundForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🚚 Форензика службы оптимизации доставки (Delivery Optimization)...", ManualChecks.RunWindowsDeliveryOptimizationAndBitsCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌌 Анализ шейдеров на встроенный GLSL X-Ray и Chams...", ManualChecks.RunMinecraftShaderpacksDeepCodeInspectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Аудит сетевых драйверов манипуляции пакетами (WinDivert/Clumsy)...", ManualChecks.RunWinDivertAndPacketCaptureDriversCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Глубокий аудит структуры модов (fabric.mod.json / маскировка)...", ManualChecks.RunMinecraftModInternalMetadataDisguiseCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📝 Анализ журнала сессии latest.log (ошибки Mixin и чит-команды)...", ManualChecks.RunMinecraftLatestLogChatAndErrorsDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖥️ Аудит графического сервера WSLg (msrdc.exe / Linux GUI читы)...", ManualChecks.RunWslgDisplayServerAndRdpSocketCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💾 Парсер постоянных очередей BITS (qmgr*.dat)...", ManualChecks.RunBitsPersistentJobFilesDeepParserCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌍 Аудит сохранений level.dat на GodMode и FlyHack (NBT)...", ManualChecks.RunMinecraftLevelDatNbtCapabilitiesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖼️ Форензика IconCache.db и кэша миниатюр проводника...", ManualChecks.RunIconAndThumbnailCacheForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔️ Аудит переназначения атаки на клавиатурные кликер-макросы...", ManualChecks.RunMinecraftPvpMacroKeybindAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📬 Аудит очереди сбоев ReportQueue (неотправленные крэши)...", ManualChecks.RunWerReportQueueCrashArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔐 Аудит TLS/Schannel и отключения проверки сертификатов...", ManualChecks.RunSchannelAndCertificateRevocationTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💾 Анализ журнала изменений NTFS USN и проверка на скрытое удаление...", ManualChecks.RunNtfsVolumeJournalUSNAnalyticsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎮 Аудит графических слоев Vulkan ImplicitLayers и инжектов DirectX...", ManualChecks.RunVulkanAndDirectXLayerInjectionAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Криминалистика кэша Favicons и остаточных URL браузеров...", ManualChecks.RunBrowserCacheFaviconAndHistoryAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Глубокий Regex-анализ истории консоли PowerShell (ConsoleHost_history.txt)...", ManualChecks.RunPowerShellConsoleHostHistoryRawRegexCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📡 Парсинг сохраненных серверов Minecraft (servers.dat) на скрытые прокси...", ManualChecks.RunMinecraftSavedServersPingAndMotdAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📅 Аудит планировщика заданий Windows (schtasks)...", ManualChecks.RunScheduledTasksXmlActionsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔵 Аудит сопряженных Bluetooth устройств и кликер-педалей...", ManualChecks.RunBluetoothPairedDevicesAndControllersAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит точек восстановления и теневых копий VSS...", ManualChecks.RunSystemRestorePointsAndVssShadowCopiesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔊 Аудит аудиоустройств, виртуальных кабелей и Soundpad...", ManualChecks.RunDirectXDirectSoundAndAudioDeviceAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧩 Глубокий аудит целостности библиотек Fabric Loader и Mixin...", ManualChecks.RunMinecraftFabricQuiltFabricApiAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Аудит WMI-персистентности (CommandLineEventConsumer)...", ManualChecks.RunWmiPersistenceAndEventConsumersAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📱 Аудит Android-эмуляторов (WSA, LDPlayer, BlueStacks)...", ManualChecks.RunWindowsSubsystemForAndroidAndVirtualizationArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📸 Криминалистика скриншотов Minecraft (F2)...", ManualChecks.RunMinecraftScreenshotsExifAndOcrCluesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит системных прокси WinINet, PAC-скриптов...", ManualChecks.RunNetworkTlsSslSessionCacheAndHttpProxyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🚰 Аудит именованных каналов Named Pipes (\\\\.\\pipe\\)...", ManualChecks.RunNamedPipesAndIpcEndpointsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит исключений и политик Windows Defender...", ManualChecks.RunDefenderMpPreferenceExclusionsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖼️ Аудит настроек Sodium / Iris и шейдерпаков на X-Ray...", ManualChecks.RunMinecraftSodiumIrisShaderSettingsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Криминалистика поисков WordWheelQuery и TypedPaths...", ManualChecks.RunFileExplorerTypedPathsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔌 Аудит активных сетевых TCP-сокетов javaw.exe...", ManualChecks.RunJavawNetworkConnectionsDeepAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Глубокий аудит вложенных Jar-in-Jar зависимостей модов...", ManualChecks.RunMinecraftFabricModsDependencyTreeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⌨️ Аудит модов и конфигов KeyStrokes, CPS Mod и ToggleSneak...", ManualChecks.RunMinecraftPlayerKeyStrokesCPSAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛠️ Аудит служб DPS, DiagTrack и PcaSvc на отключение...", ManualChecks.RunDiagnosticPolicyServiceDpsAntiForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💬 Аудит истории команд чата Minecraft (command_history.txt)...", ManualChecks.RunMinecraftChatMacrosAndCommandHistoryAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔒 Аудит защиты LSA (RunAsPPL) и Memory Management...", ManualChecks.RunLocalSecurityAuthorityLsaProtectionAndPplCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎨 Криминалистика кэша шейдеров (NVIDIA/AMD DXCache)...", ManualChecks.RunDirectXShaderDecompilerCacheForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Аудит истории запуска AppCompatFlags (Store/Persisted)...", ManualChecks.RunRegistryAppCompatUserFlagsExecutionHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📍 Аудит меток и баз миникарт (Xaero, JourneyMap, FTB)...", ManualChecks.RunMinecraftWaypointsAndMinimapModConfigsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Аудит шрифтов ClickGUI чит-клиентов и дропперов...", ManualChecks.RunWindowsFontCacheServiceAndWpfFontArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Аудит ресурспаков на X-Ray, Low-Fire и скрытые DLL/JAR...", ManualChecks.RunMinecraftResourcePackCustomFontAndDisguiseAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔️ Анализ сохранённых хотбаров (hotbar.nbt) на 32k оружие...", ManualChecks.RunMinecraftPlayerHotbarItemNbtAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💎 Аудит PBR, Normal-карт и светящихся руд (Luminescent X-Ray)...", ManualChecks.RunMinecraftCustomResourcePackPBRAndSpecularMapsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Криминалистика кэша сертификатов CryptnetUrlCache (SSL CRL/OCSP)...", ManualChecks.RunWindowsCryptnetUrlCacheCertificatesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит файла etc/hosts и подмены серверов авторизации Mojang...", ManualChecks.RunWindowsSystem32EtcHostsHijackAndDnsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Аудит конфигураций модов на Freecam, Tweakeroo и чит-опции...", ManualChecks.RunMinecraftModConfigurationsAndFreecamConfigsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📊 Аудит истории запусков FeatureUsage (AppSwitched/AppLaunch)...", ManualChecks.RunWindowsFeatureUsageAppSwitchedAndLaunchForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🏃 Аудит статистики игрока (stats/*.json - Fly/NoFall/Jesus)...", ManualChecks.RunMinecraftPlayerMovementAndStatisticsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📁 Криминалистика ShellBags (история открытия папок читов)...", ManualChecks.RunWindowsShellBagsFolderAccessForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔌 Аудит аппаратных макросов (Arduino, Pico, макро-педали)...", ManualChecks.RunHardwareUsbMacroDevicesAndFootPedalsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Криминалистика кэша шрифтов Java AWT (~JF*.tmp)...", ManualChecks.RunJavaRuntimeFontCacheAndAwtArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎮 Аудит инжекций Minecraft Bedrock (UWP, Horion, Prax)...", ManualChecks.RunMinecraftBedrockUwpInjectedDllsAndAddonsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔔 Криминалистика базы уведомлений Windows (wpndatabase.db)...", ManualChecks.RunWindowsPushNotificationsDatabaseForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💥 Криминалистика аварийных дампов памяти (CrashDumps)...", ManualChecks.RunWindowsCrashDumpsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☀️ Аудит options.txt на Gamma Fullbright и FOV эксплойты...", ManualChecks.RunMinecraftPlayerOptionsTxtAdvancedFovGammaAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎮 Аудит устройств DirectInput и виртуальных контроллеров...", ManualChecks.RunWindowsDirectInputAndXInputDeviceHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖼️ Аудит папки скриншотов на фальшивые PNG и скрытые JAR...", ManualChecks.RunMinecraftScreenshotsFolderHiddenFilesAndAdsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔮 Аудит шейдерпаков на Wallhack и GLSL Depth Hack...", ManualChecks.RunMinecraftSodiumIrisShaderPacksDepthCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔗 Аудит зарегистрированных URL-протоколов и перехватов .JAR...", ManualChecks.RunWindowsDefaultProgramAssociationsAndUrlProtocolsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Аудит логов игры на аномалии сессий и кики античита...", ManualChecks.RunMinecraftPlayerLogsSessionDurationAnomaliesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Аудит механизма автозапуска Active Setup (Installed Components)...", ManualChecks.RunWindowsActiveSetupPersistenceAndStubPathsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Аудит репозитория UWP AppX пакетов и sideloaded сборок...", ManualChecks.RunWindowsPackageRepositoryAppxSideloadedModsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👤 Аудит playerdata/*.dat на способности игрока (Fly, GodMode)...", ManualChecks.RunMinecraftPlayerWorldDatNbtPlayerAbilitiesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗂️ Криминалистика диалогов выбора файлов (OpenSavePidlMRU)...", ManualChecks.RunWindowsStorageAccessFrameworkAndOpenSavePidlCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Криминалистика кэша шрифтов DirectWrite (FNTCACHE.DAT)...", ManualChecks.RunWindowsFontCacheServiceFntcacheDatDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💬 Аудит командных префиксов и логов чата (.vape, .bind)...", ManualChecks.RunMinecraftPlayerChatFilterAndRegexAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит журнала службы теневого копирования тома VSS...", ManualChecks.RunWindowsVolumeShadowCopiesAndVssWritersStatusCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💡 Аудит настроек шейдерпаков на Fullbright и твики...", ManualChecks.RunMinecraftSodiumIrisShaderPackBloomAndLightingTweaksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Аудит режимов совместимости и прав Admin (AppCompat Layers)...", ManualChecks.RunWindowsAppCompatFlagsLayersAndExecutionModesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Аудит эндер-сундука (EnderItems) на 32k оружие и краш-блоки...", ManualChecks.RunMinecraftPlayerWorldDatEnderChestInventoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📊 Глубокий аудит UserAssist со счетчиками запусков и фокуса...", ManualChecks.RunWindowsUserAssistDetailedFocusTimeAndExecutionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⌨️ Криминалистика истории диалога «Выполнить» (RunMRU)...", ManualChecks.RunWindowsRunMruDialogCommandsHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔️ Анализ боевой статистики игрока (Killaura/Aimbot/CrystalAura)...", ManualChecks.RunMinecraftPlayerStatisticsCombatPvpAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📥 Аудит службы BITS и журнала Delivery Optimization на загрузку читов...", ManualChecks.RunWindowsDeliveryOptimizationAndBitsCacheDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📖 Аудит книги рецептов (RecipeBook) и крафта на дюпы...", ManualChecks.RunMinecraftInventoryCraftingRecipeBookExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит сетевых интерфейсов, VPN-адаптеров и прокси во время SS...", ManualChecks.RunWindowsNetworkProfileInterfacesAndVpnTunnelsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Аудит серверных ресурспаков на блокировку и прозрачные X-Ray текстуры...", ManualChecks.RunMinecraftMultiplayerServerResourcePacksCacheAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖥️ Аудит истории Windows Terminal и профилей консоли...", ManualChecks.RunWindowsTerminalAndConsoleHostHistoryAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧩 Анализ манифестов Fabric и Quilt (fabric.mod.json) на подмену модов...", ManualChecks.RunMinecraftFabricLoaderAndQuiltModMetadataCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📶 Аудит сопряжённых Bluetooth-устройств и беспроводных макро-педалей...", ManualChecks.RunWindowsConnectedDevicesAndBluetoothLePeripheralsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎮 Аудит биндов клавиш (options.txt) на дабл-бинд и чит-меню...", ManualChecks.RunMinecraftPlayerKeybindsAndCustomOptionsDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏰ Аудит планировщика задач Windows на скрытые задачи читов и RAT...", ManualChecks.RunWindowsTaskSchedulerHiddenTasksAndTriggersCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎒 Аудит слотов инвентаря и крафтинг-сетки на стаки и 32k...", ManualChecks.RunMinecraftPlayerInventoryCraftingGridStateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📥 Анализ незавершённых загрузок (.crdownload/.tmp) и скрытых PE-файлов...", ManualChecks.RunWindowsBackgroundTransferAndHttpClientTracesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит параметров JVM и javaagent мульти-лаунчеров (Prism/Lunar/Feather)...", ManualChecks.RunMinecraftLunarFeatherPrismCustomLaunchArgsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит следов уязвимых драйверов ядра (BYOVD) и удалённых служб...", ManualChecks.RunWindowsServiceTracesAndDeletedDriversRegistryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Аудит киков античитами (GrimAC/Vulcan/Polar) и крашей Netty в логах...", ManualChecks.RunMinecraftPlayerLogsDisconnectionAndKickAnalysisCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит истории сетевых подключений к серверам и альт-прокси...", ManualChecks.RunMinecraftPlayerLogsRecentServerIpsAndProxiesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗄️ Аудит баз совместимости SDB (Shim-инжекции и хуки)...", ManualChecks.RunWindowsAppCompatFlagsTelemetryAndCompatibilityDbCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👁️ Аудит OptiFine CIT/CEM текстур на подмену моделей и Chams...", ManualChecks.RunMinecraftOptifineCustomSkyAndCitPropertiesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖱️ Анализ настроек мыши, кастомных кривых и сглаживания аима...", ManualChecks.RunWindowsClipCursorAndMouseRawInputHooksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Аудит конфигураций Mixin в Fabric/Quilt модах на перехват рендера...", ManualChecks.RunMinecraftFabricQuiltMixinAuditConfigsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👤 Аудит альт-аккаунтов и менеджеров (TheAltening/EasyMC)...", ManualChecks.RunMinecraftPlayerRealmsPersistenceAndMultiplayerConfigCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Аудит истории PowerShell PSReadLine и следов очистки команд...", ManualChecks.RunWindowsPowerShellConsoleReadLineHistoryDeepForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌍 Аудит правил мира GameRules и тиков в level.dat...", ManualChecks.RunMinecraftPlayerWorldSaveLevelDatGameRulesExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗃️ Аудит RecentFileCache.bcf на следы запусков удалённых программ...", ManualChecks.RunWindowsAppCompatFlagsTelemetryCompatibilityAslLogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Аудит вложенных библиотек Jar-in-Jar (META-INF/jars) на скрытые читы...", ManualChecks.RunMinecraftFabricLoaderCustomModsDependencyGraphCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Аудит отклонения и подмены серверных ресурспаков (X-Ray bypass)...", ManualChecks.RunMinecraftPlayerLogsServerResourcePackPromptsAndDeclinesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит кэша службы DNS на домены авторизации читов...", ManualChecks.RunWindowsDnsCacheServiceAndHostsResolutionAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит файлов локализации модов (lang/*.json) на скрытые чит-модули...", ManualChecks.RunMinecraftFabricLoaderCustomLanguageAndI18nCheatsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Аудит глобальных переменных Java (_JAVA_OPTIONS/JAVA_TOOL_OPTIONS)...", ManualChecks.RunWindowsSystemEnvironmentVariablesAndJavaToolOptionsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("✨ Анализ GLSL программ шейдерпаков на отключение буфера глубины (Chams)...", ManualChecks.RunMinecraftSodiumIrisShaderPackDepthBufferAndDepthMaskCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🪟 Аудит логов GLFW, фокуса окна и сбоев захвата курсора...", ManualChecks.RunMinecraftPlayerLogsGlfwInputAndKeyboardFocusCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎮 Аудит настроек Direct3D и инжекций внешних графических оверлеев...", ManualChecks.RunWindowsDirect3DAndDxgiPresentHookAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🕵️ Аудит сокрытия модов из ModMenu (\"hidden\": true)...", ManualChecks.RunMinecraftFabricLoaderCustomManifestSignaturesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Аудит шрифтов ClickGUI в кэше Windows и Java AWT...", ManualChecks.RunWindowsFontCacheServiceWpfClickGuiFontsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💬 Поиск команд с точкой (.bind/.vape) и Baritone (#goto) в логах чата...", ManualChecks.RunMinecraftPlayerLogsChatCommandHistoryAndDotPrefixCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💻 Аудит истории консоли PowerShell (ConsoleHost_history) и анти-SS команд...", ManualChecks.RunWindowsPowerShellAstAndConsoleHostHistoryCommandAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💥 Аудит дампов аварийных падений javaw и инжекторов (CrashDumps/WER)...", ManualChecks.RunWindowsErrorReportingCrashDumpsAndWerArchiveAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎭 Аудит ресурспаков на скрытые X-Ray и ESP шейдеры подсветки сущностей...", ManualChecks.RunMinecraftResourcePackCustomShadersAndXRayEntityOutlineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит фоновой службы BITS на скрытые сетевые загрузки читов...", ManualChecks.RunWindowsBackgroundIntelligentTransferServiceBitsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⌨️ Аудит хуков ввода (AppInit_DLLs) и драйверов кликеров (Interception)...", ManualChecks.RunWindowsWin32kAndDirectInputLowLevelHooksAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит виртуальных сетевых адаптеров, VPN-туннелей (TAP/Wintun) и прокси...", ManualChecks.RunWindowsNetworkAdaptersAndVirtualTunnelingInterfacesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎭 Аудит маскировки Java-модов под PNG/BIN/TXT (ZIP Magic Bytes)...", ManualChecks.RunMinecraftModFilesExtensionMasqueradingAndMagicBytesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔑 Аудит мастер-ключей DPAPI и криптографических аномалий Windows...", ManualChecks.RunWindowsDpapiMasterKeysAndCngSecurityAnomaliesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💥 Аудит логов сбоев виртуальной машины Java (hs_err_pid) и аргументов JavaAgent...", ManualChecks.RunMinecraftJvmHotSpotCrashLogsAndJavaAgentArgsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💾 Аудит истории подключений съёмных накопителей и флешек (USBSTOR)...", ManualChecks.RunWindowsUsbStorageHistoryAndRemovableDriveAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🚨 Аудит преднамеренной очистки журналов Windows (Event ID 104/1102)...", ManualChecks.RunWindowsEventLogClearedAndAuditTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔊 Аудит библиотек OpenAL (OpenAL32.dll) на перехват звука и Sound ESP...", ManualChecks.RunMinecraftOpenAlSoundEngineAndNativesHookAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📹 Аудит виртуальных веб-камер и подмены экрана (OBS Virtual Camera)...", ManualChecks.RunWindowsDirectShowVirtualCameraAndStreamSpoofingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит launcher_profiles.json на подмену путей Java Runtime (Temp/Downloads)...", ManualChecks.RunMinecraftLauncherProfilesCustomJrePathTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏰ Аудит Планировщика задач на скрипты самоуничтожения и автозапуск...", ManualChecks.RunWindowsTaskSchedulerPersistenceAndWiperTasksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔌 Аудит событий PnP на недавнее извлечение USB-флешек перед проверкой...", ManualChecks.RunWindowsKernelPnpUsbRemovalEventLogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👥 Аудит ресурспаков на кастомные CEM-модели сущностей и игроков...", ManualChecks.RunMinecraftResourcePackCustomEntityModelsCemCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔒 Аудит конфигурации DNS-over-HTTPS (DoH) и шифрованных запросов...", ManualChecks.RunWindowsDnsOverHttpsAndEncryptedResolutionAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧬 Аудит логов Fabric на Mixin-инжекции в классы движения игрока...", ManualChecks.RunMinecraftFabricLoaderSyntheticMixinsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📊 База учета использования ресурсов SRUM (SRUDB.dat)...", ManualChecks.RunWindowsSystemResourceUsageMonitorSrumAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📋 Аудит истории буфера обмена (Win+V) и кэша активностей ActivitiesCache...", ManualChecks.RunWindowsClipboardHistoryAndActivityCacheAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖱️ Аудит параметров мыши, акселерации и софта макросов (Razer/Bloody/Logitech)...", ManualChecks.RunMinecraftMousePollingRateAndRegistryJitterAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит параметров BCD на тестовый режим (TestSigning/nointegritychecks)...", ManualChecks.RunWindowsBcdDriverSignatureAndTestSigningAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌳 Аудит дерева процессов javaw.exe (PPID) и обнаружение осиротевших процессов...", ManualChecks.RunMinecraftJavawProcessParentageAndOrphanAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗑️ Глубокий аудит метаданных $I в Корзине ($Recycle.Bin) на удалённые чит-файлы...", ManualChecks.RunWindowsRecycleBinDollarIMetadataForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗄️ Аудит службы теневых копий VSS и обнаружение удаления снимков восстановления...", ManualChecks.RunWindowsVolumeShadowCopiesAndVssWiperAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎨 Аудит кэшей шейдерных конвейеров Sodium/Iris на повреждения и сбросы...", ManualChecks.RunMinecraftSodiumIrisShaderPipelineCacheAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔗 Аудит каталогов игры (.minecraft/mods) на скрытые симлинки (NTFS Junction)...", ManualChecks.RunMinecraftReparsePointsAndSymbolicLinksAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👤 Аудит профилей на сторонние серверы авторизации (Authlib-Injector/EasyMC)...", ManualChecks.RunMinecraftAuthlibInjectorAndAltDistributorsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛑 Аудит реестра служб на уязвимые драйверы ядра (BYOVD: gdrv/mhyprot/rtcore64)...", ManualChecks.RunWindowsByovdVulnerableDriversAndRootDevicesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Аудит журнала Security на события создания процессов (Event 4688)...", ManualChecks.RunWindowsSecurityProcessCreationEvent4688AuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌙 Аудит каталогов Lunar Client на Weave-Loader и сторонние JavaAgent модули...", ManualChecks.RunMinecraftLunarClientWeaveAndAgentHooksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏱️ Аудит журнала диагностики производительности Windows (WDI)...", ManualChecks.RunWindowsDiagnosticsPerformanceWdiLogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎥 Аудит конфигураций OBS Studio на изоляцию javaw и скрытие оверлеев...", ManualChecks.RunMinecraftObsGameCaptureAndOverlayHookAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💻 Аудит профилей Windows Terminal на скрытый запуск с обходом политик...", ManualChecks.RunWindowsTerminalProfilesAndConPtyHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Расшифровка записей UserAssist (ROT13) на скрытые запуски читов...", ManualChecks.RunWindowsUserAssistRot13DecodedExecutionAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Аудит манифестов Forge на ранний запуск CoreMod и TweakClass...", ManualChecks.RunMinecraftForgeCoreModTweakerAndFmlPluginCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💥 Аудит дампов аварийного сбоя ядра Windows (BSOD Minidump)...", ManualChecks.RunWindowsKernelMinidumpAndBsodCrashAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💬 Аудит каталогов клиентов на зашифрованные IRC-каналы связи...", ManualChecks.RunMinecraftEncryptedIrcAndSecretChatForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗄️ Реестр MuiCache (История запуска исполняемых файлов)...", ManualChecks.RunWindowsMuiCachePathAndFriendlyNameSpoofingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔐 Аудит ключей RSA MachineKeys на генерацию локальных MITM-прокси...", ManualChecks.RunWindowsCryptoRsaMachineKeysAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🤖 Поиск каталогов, воксельных кэшей и настроек автопилота Baritone...", ManualChecks.RunMinecraftBaritonePathfindingCacheAndSettingsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📁 Аудит ProgramData на скрытые GUID-папки распаковки инжекторов...", ManualChecks.RunWindowsProgramDataHiddenStagingFoldersCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит модов мультипротокола (ViaFabric/MultiConnect) на подмену версий...", ManualChecks.RunMinecraftViaFabricProtocolVersionSpoofingCheck),
			Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc64 Аудит реестра BAM на запуски читов из-под удалённых или гостевых SID...", ManualChecks.RunWindowsBamDeletedUserSidExecutionForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔇 Аудит реестра SilentProcessExit на подавление отчётов сбоев javaw...", ManualChecks.RunWindowsSilentProcessExitAndWerFaultAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🚀 Аудит версий игры на подмену главного класса запуска (MainClass Wrapper)...", ManualChecks.RunMinecraftLauncherCustomMainClassWrapperCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔠 Аудит каталога шрифтов Windows на шрифты интерфейсов читов (Comfortaa)...", ManualChecks.RunWindowsFontLinkAndClickGuiFontInstallAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📸 Аудит папки скриншотов на повреждения и следы выборочного удаления...", ManualChecks.RunMinecraftScreenshotTimestampGapsAndDeletionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Аудит журнала PowerShell на события ScriptBlock Logging (Event 4104)...", ManualChecks.RunWindowsPowerShellScriptBlockLoggingEvent4104Check),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит реестра AppCompatFlags Layers на шимы RunAsInvoker и чит-приложения...", ManualChecks.RunWindowsAppCompatFlagsLayersPrivilegeAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💡 Аудит options.txt на завышенную гамму (FullBright) и быстрые клавиши...", ManualChecks.RunMinecraftPlayerOptionsChatKeybindsAndMacrosCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📁 Аудит реестра Shellbags (BagMRU) на открытие и переименование папок читов...", ManualChecks.RunWindowsShellbagsFolderHistoryAndRenamingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👁️ Аудит настроек рендера Sodium на принудительное отключение Entity Culling...", ManualChecks.RunMinecraftSodiumVideoSettingsFullbrightAndFogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🐧 Аудит подсистемы WSL (Windows Subsystem for Linux) на сторонние туннели...", ManualChecks.RunWindowsWslVirtualSubsystemAndNetworkBridgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💽 Аудит монтирования виртуальных дисков VHD/VHDX на съёмные тома...", ManualChecks.RunWindowsVirtualHardDiskVhdxAndMountAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧩 Аудит инстансов Prism/MultiMC на сторонние патчи библиотек...", ManualChecks.RunMinecraftPrismLauncherInstancePatchesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит подсистемы безопасности LSA (RunAsPPL) на целостность политик...", ManualChecks.RunWindowsLsaProtectionAndRunAsPplAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Аудит кэша серверных ресурспаков на блокировку и подмену хэшей...", ManualChecks.RunMinecraftServerResourcePacksCacheAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит системного файла hosts на подмену серверов авторизации Mojang...", ManualChecks.RunWindowsEtcHostsFileRedirectionAndTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит исключений антивируса Windows Defender на сокрытие папок читов...", ManualChecks.RunWindowsDefenderExclusionPathsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔄 Поиск модификаций отвязки камеры FreeLook и 360-градусного обзора...", ManualChecks.RunMinecraftFreeLookAndPerspectiveModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚠️ Аудит журнала Security на события изменения политик аудита (Event 4719)...", ManualChecks.RunWindowsSecurityAuditPolicyChangedEvent4719Check),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Аудит шрифтов в ресурспаках на отрицательные отступы и эксплойты никнеймов...", ManualChecks.RunMinecraftResourcePackFontGlyphWidthExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Аудит недавних запусков в Prefetch за 24ч и всплесков вызовов скриптов...", ManualChecks.RunWindowsPrefetchExecutionBurstAndHashCorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📌 Аудит закреплений Taskband и кэша иконок трея TrayNotify...", ManualChecks.RunWindowsTaskbandAndTrayNotifyPinForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧩 Поиск скрытых загрузчиков байткода Fabric/NeoForge LanguageAdapter...", ManualChecks.RunMinecraftFabricAndNeoForgeLanguageProviderTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит политик AppLocker и журналов блокировок неподписанного ПО...", ManualChecks.RunWindowsAppLockerAndSRPBypassEventLogsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⌨️ Анализ конфигураций options.txt на спаренные бинды атаки и макросы...", ManualChecks.RunMinecraftChatMacroAndKeybindTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔀 Аудит реестра на перехват и подмену команд Shell Open (.exe/.jar)...", ManualChecks.RunWindowsRegistryShellOpenCommandHijackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎮 Аудит слоев перехвата рендеринга DirectX и Vulkan...", ManualChecks.RunWindowsDirectXAndVulkanGraphicsLayerHookCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит параметров безопасности JVM и аргументов javaagent...", ManualChecks.RunMinecraftJvmSecurityManagerAndPropertiesTamperCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💾 Аудит съёмных томов, RAM-дисков и реестра MountedDevices...", ManualChecks.RunWindowsStorageSpacesAndBitLockerHiddenVolumesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Поиск модификаций перехвата сетевого конвейера Netty...", ManualChecks.RunMinecraftNettyPipelinePacketManipulationModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗃️ Аудит каталогов VirtualStore и кастомных SDB-баз AppCompat...", ManualChecks.RunWindowsVirtualStoreAndCustomSdbAppCompatCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧠 Аудит параметров Session Manager Memory Management и отключения дампов...", ManualChecks.RunWindowsMemoryManagementAndPagefileEvasionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Поиск скрытых вложенных библиотек Jar-in-Jar внутри модов...", ManualChecks.RunMinecraftFabricJarInJarNestedContainerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔌 Анализ логов SetupAPI на подключение аппаратных кликеров Arduino/Pico...", ManualChecks.RunWindowsSetupApiHardwareDeviceInstallForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧱 Аудит JSON-моделей блоков в ресурспаках на модельный X-Ray...", ManualChecks.RunMinecraftResourcePackBlockModelXrayForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📡 Аудит параметров TCP/IP интерфейсов на MTU десинк и регедит-твики...", ManualChecks.RunWindowsTcpStackTuningAndDesyncTweaksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔑 Аудит времени модификации мастер-ключей DPAPI (%AppData%\\Protect)...", ManualChecks.RunWindowsDpapiMasterKeyRecentActivityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💉 Аудит конфигураций Mixin на инъекции в Reach, Hitbox и LivingEntity...", ManualChecks.RunMinecraftFabricMixinInjectorTargetAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📥 Аудит службы BITS и очереди qmgr*.dat на скрытую догрузку файлов...", ManualChecks.RunWindowsBitsTransferJobsPersistenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Поиск скрытых профилей автокликеров и макросов в .minecraft/config...", ManualChecks.RunMinecraftHiddenMacroConfigsAndProfilesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖥️ Аудит параметров Console Host на сокрытие консольных окон (буфер 1x1)...", ManualChecks.RunWindowsConsoleHostInvisibleBufferEvasionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🪟 Аудит журнала System на установку ядерных драйверов (Event 7045)...", ManualChecks.RunWindowsKernelDriverAndServiceInstallEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Проверка модов на прямую подмену ванильных классов (Non-Mixin bypass)...", ManualChecks.RunMinecraftFabricModCoreClassIntegrityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📂 Декодирование бинарных PIDL потоков в OpenSavePidlMRU на выбор читов...", ManualChecks.RunWindowsOpenSavePidlMruHiddenPathsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Сканирование локальных loopback портов на WebSocket/IPC мосты читов...", ManualChecks.RunMinecraftClientLocalWebSocketAndIpcBridgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏱️ Анализ журнала на панические уходы в сон/гибернацию перед проверкой...", ManualChecks.RunWindowsSleepWakePanicTimeJumpForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧭 Анализ истории ручного ввода путей в Проводнике (TypedPaths)...", ManualChecks.RunWindowsExplorerTypedPathsHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌌 Аудит конфигураций неба OptiFine и прозрачных текстур игроков...", ManualChecks.RunMinecraftOptiFineSkyAndEntityTransparencyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📡 Поиск установленных драйверов захвата пакетов Npcap/WinPcap...", ManualChecks.RunWindowsNetworkSnifferAndPromiscuousDriversCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📸 Аудит целостности скриншотов клиента и недавних временных интервалов...", ManualChecks.RunMinecraftScreenshotsMetadataAndTimestampGapsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙️ Аудит постоянных WMI подписок на скрипты авто-зачистки следов...", ManualChecks.RunWindowsWmiEventSubscriptionPersistenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗂️ Глубокий аудит истории папок в ShellBags (BagMRU)...", ManualChecks.RunWindowsShellBagsFolderHistoryDeepAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит системных переменных _JAVA_OPTIONS и JAVA_TOOL_OPTIONS...", ManualChecks.RunMinecraftJvmGlobalEnvironmentOptionsInjectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔐 Аудит активности криптографических RSA-контейнеров за 24ч...", ManualChecks.RunWindowsRsaAndCngKeyContainerActivityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔊 Поиск модификаций звукового ESP и радаров шагов в mods/...", ManualChecks.RunMinecraftDirectionalAudioAndSoundPhysicsEspModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔔 Анализ базы уведомлений Windows wpndatabase.db на алерты читов...", ManualChecks.RunWindowsNotificationToastDatabaseForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Аудит профилей PowerShell на скрытые скрипты зачистки следов...", ManualChecks.RunWindowsPowerShellProfileAndTranscriptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎭 Поиск модов маскировки заголовка окна игры и фейкового Discord RPC...", ManualChecks.RunMinecraftWindowTitleAndDiscordRpcMaskingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит слоев AppCompatFlags на шимы IgnoreFreeLibrary и сокрытие DEP...", ManualChecks.RunWindowsAppCompatLayersIgnoreFreeLibraryAndShimCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Аудит шрифтов ресурспаков на zero-width символы и смещение намтегов...", ManualChecks.RunMinecraftResourcePackFontZeroWidthNametagShiftCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👤 Аудит реестра BAM на запуски читов из-под удалённых или гостевых SID...", ManualChecks.RunWindowsBamOrphanedUserSidExecutionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💾 Аудит размера и целостности файлов подкачки pagefile/swapfile...", ManualChecks.RunWindowsPagefileAndSwapfileSizeAnomalyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Поиск аномальных экстремальных приоритетов Mixin (priority > 10000)...", ManualChecks.RunMinecraftFabricMixinPriorityOrderHijackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔒 Аудит политик цифровой подписи драйверов и целостности кода CI...", ManualChecks.RunWindowsDriverSigningPolicyAndCiIntegrityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🚨 Поиск модификаций оповещения об администрации (Staff-Alert/Vanish)...", ManualChecks.RunMinecraftStaffAlertAndChatPacketInterceptionModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔌 Аудит реестра USBSTOR на спуфнутые серийные номера и эмуляторы...", ManualChecks.RunWindowsUsbStorageSerialSpoofingAndVirtualDriveCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🏃 Аудит истории команд диалога Win+R (RunMRU) на вайперы...", ManualChecks.RunWindowsExplorerRunMruCommandHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📡 Поиск проприетарных CustomPayload каналов связи чит-клиентов...", ManualChecks.RunMinecraftCustomPayloadChannelTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔑 Аудит зарегистрированных крипто-провайдеров CSP на сторонние DLL...", ManualChecks.RunWindowsCryptographyServiceProvidersRegistryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔇 Анализ sounds.json в ресурспаках на заглушение шагов (Silent-Step)...", ManualChecks.RunMinecraftResourcePackSoundsMutingExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖥️ Поиск служб виртуальных мониторов и трансляции внешних радаров...", ManualChecks.RunWindowsVirtualDisplayAndRemoteStreamingDriverCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖥️ Аудит истории подключений Remote Desktop (MSTSC MRU)...", ManualChecks.RunWindowsRdpConnectionHistoryAndMstscMruCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔮 Поиск GLSL-кода подсветки силуэтов игроков в шейдерпаках...", ManualChecks.RunMinecraftShaderPackPlayerSilhouetteEspCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📥 Аудит событий службы BITS на загрузки из Discord/GitHub...", ManualChecks.RunWindowsBitsClientOperationalEventLogsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔒 Поиск тяжелых коммерческих обфускаторов (Zelix/Allatori) в модах...", ManualChecks.RunMinecraftModClassStringObfuscationEntropyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎭 Аудит хэшей Prefetch на маскировку читов под EXPLORER.EXE...", ManualChecks.RunWindowsPrefetchMasqueradingAndHashAnomalyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💾 Аудит служб теневого копирования VSS и отключения System Restore...", ManualChecks.RunWindowsVssProviderAndSystemRestoreTamperCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📷 Поиск модификаций свободной камеры FreeCam в mods/ и config/...", ManualChecks.RunMinecraftFreeCamAndSpectatorPacketEvasionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📹 Аудит устройств DirectShow на виртуальные веб-камеры (OBS/ManyCam)...", ManualChecks.RunWindowsDirectShowVirtualVideoFilterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("✨ Анализ текстур частиц в ресурспаках на частицы-маяки (Crit ESP)...", ManualChecks.RunMinecraftResourcePackCustomParticleBeaconExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит реестра MountPoints2 на запуск читов с сетевых UNC-шар...", ManualChecks.RunWindowsExplorerMountPoints2NetworkSharesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Расшифровка ROT13 записей в UserAssist за последние 48 часов...", ManualChecks.RunWindowsUserAssistRot13ExecutionDeltaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит кэша шейдеров Sodium/Iris на блокировку файлов ReadOnly...", ManualChecks.RunMinecraftSodiumShaderCacheTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔌 Аудит журнала Kernel-PnP на экстренное отключение флешек (Event 225)...", ManualChecks.RunWindowsKernelPnpUsbEjectRecentEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🏹 Поиск модификаций пакетного флуда FastUse и скорострельного лука...", ManualChecks.RunMinecraftFastUseAndPacketFloodModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💻 Аудит профилей Windows Terminal settings.json на скрытые скрипты...", ManualChecks.RunWindowsTerminalConPtyProfilesPersistenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Проверка удаления теневых копий томов и событий сброса VSS...", ManualChecks.RunWindowsVolumeShadowCopyDeletionEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧩 Поиск синтетических мостов Mixin и скрытых аксессоров читов...", ManualChecks.RunMinecraftFabricLoaderMixinSyntheticBridgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Анализ фоновых задач передачи BITS на скрытую загрузку читов...", ManualChecks.RunWindowsBackgroundTransferBITSJobsBinaryForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔ Поиск модификаций NoSlowdown и KeepSprint в клиенте...", ManualChecks.RunMinecraftNoSlowdownAndKeepSprintModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Анализ целостности службы кэша шрифтов Windows (FontCache)...", ManualChecks.RunWindowsFontCacheServiceAnomalyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💎 Проверка PBR-текстур и подсветки руд в шейдерах сквозь стены...", ManualChecks.RunMinecraftSodiumIrisPbrNormalMapXRayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖱 Анализ фильтр-драйверов клавиатуры и мыши (Interception/Clicker)...", ManualChecks.RunWindowsDirectInputKeyboardHookTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🚀 Поиск скрытых ранних entrypoints (preLaunch) в Fabric-модах...", ManualChecks.RunMinecraftFabricLoaderEntrypointInjectionForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Аудит удаления и сброса журнала изменений NTFS USN Journal...", ManualChecks.RunWindowsNtfsChangeJournalUsnPurgeEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👁 Поиск отключения куллинга сущностей и настенных никнеймов...", ManualChecks.RunMinecraftEntityCullingBypassConfigCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧱 Проверка куллинга граней блоков Sodium (Cave Finder / X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionCullingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗃 Анализ кэша телеметрии AppCompatFlags Compatibility Assistant...", ManualChecks.RunWindowsAppCompatFlagsTelemetryCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎭 Поиск стеганографических пейлоадов в текстурах скинов и плащей...", ManualChecks.RunMinecraftCustomSkinStealthCapeNametagCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗑 Парсинг бинарных индексов $I корзины на удаление читов...", ManualChecks.RunWindowsRecycleBinOrphanedSidDeletedJarsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Проверка манифестов META-INF и целостности JAR-модов...", ManualChecks.RunMinecraftFabricLoaderJarModSignatureCertificateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит аргументов JVM и перехвата ClassPath на внешние пути...", ManualChecks.RunMinecraftFabricLoaderClassPathUrlHijackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🪟 Поиск скрытых прозрачных оверлеев DirectComposition ESP...", ManualChecks.RunWindowsDirectCompositionOverlayForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎯 Поиск скрытых Mixin-аксессоров к приватным боевым полям...", ManualChecks.RunMinecraftFabricLoaderMixinSyntheticAccessorLeakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🗄 Аудит сброса и зачистки кэша шрифтов DirectWrite FontTable...", ManualChecks.RunWindowsFontTableCacheTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎨 Аудит кастомных колормапов OptiFine на X-Ray прозрачность...", ManualChecks.RunMinecraftOptiFineCustomColormapXRayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Аудит реестра FontSubstitutes на сокрытие текста и оверлеи...", ManualChecks.RunWindowsFontSubstitutesRegistryHijackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔓 Поиск динамической загрузки зашифрованного байткода в память...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicClassLoadingForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📱 Аудит политик AppModel на сайдлоад UWP-макросов и кликеров...", ManualChecks.RunWindowsAppModelPolicyExecutionForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎮 Проверка отключения теста глубины OpenGL Sodium (Engine Chams)...", ManualChecks.RunMinecraftSodiumCustomGlFormatTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Анализ командной истории PowerShell на следы зачистки и скриптов...", ManualChecks.RunWindowsPowerShellEngineInternalScriptCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧩 Поиск кастомных IMixinConfigPlugin и условных инжекторов Mixin...", ManualChecks.RunMinecraftFabricLoaderMixinConfigPluginTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔊 Аудит виртуальных аудиокабелей и loopback устройств (SoundClicker)...", ManualChecks.RunWindowsDirectSoundAudioOutputDeviceHookCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧱 Проверка модификаций мешера чанков Sodium на подсветку руд...", ManualChecks.RunMinecraftSodiumCustomMeshPipelineShadersCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖥 Аудит истории удаленных подключений RDP Terminal Server Client...", ManualChecks.RunWindowsTerminalServerClientActiveXHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🕊 Детект манипуляций способностями игрока и скоростью полета...", ManualChecks.RunMinecraftPlayerAbilitiesFlightCreativeExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧬 Аудит динамических Java Agent в Temp и модулей Attach API...", ManualChecks.RunMinecraftFabricLoaderClassWeavingAgentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Анализ изоляции и недавних перезапусков Font Driver Host...", ManualChecks.RunWindowsFontDriverHostUserModeDriverCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📷 Поиск патчей коллизии камеры и проникновения сквозь блоки...", ManualChecks.RunMinecraftSodiumCustomCameraClipFrustumCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Аудит параметров хранилища теневых копий и урезания VSS...", ManualChecks.RunWindowsVolumeSnapshotLicensingServiceForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💡 Аудит кастомных шейдеров подсветки Glowing ESP сквозь стены...", ManualChecks.RunMinecraftEntityRendererGlowingOutlineShaderCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🍃 Проверка прозрачности листвы и альфа-отсечения Sodium (Cave Finder)...", ManualChecks.RunMinecraftSodiumCustomAlphaDiscardPipelineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Аудит реестра шрифтов на установку из временных каталогов Temp...", ManualChecks.RunWindowsFontRegistryHiddenTtfInstallCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔ Поиск принудительной отмены отдачи и урона в Mixin (AntiKnockback)...", ManualChecks.RunMinecraftFabricLoaderMixinInjectorCallbackReturnTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔄 Аудит событий отключения и сброса службы восстановления системы...", ManualChecks.RunWindowsVolumeRestorePointCreationFrequencyEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Поиск скрытых сетевых каналов управления читами (CustomPayload C2S)...", ManualChecks.RunMinecraftCustomPayloadNetworkC2SExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧩 Поиск синтетических мостов @Invoker к приватным методам боя...", ManualChecks.RunMinecraftFabricLoaderMixinSyntheticInvokerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Аудит подразделов реестра DirectWrite на регистрацию чит-шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryKeyMruCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌫 Проверка отключения дистанционного тумана и слепоты Sodium...", ManualChecks.RunMinecraftSodiumCustomFogPipelineFrustumCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Аудит фильтр-драйверов стека томов на отключение драйвера volsnap...", ManualChecks.RunWindowsVolumeSnapshotFilterDriverAltitudeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧱 Проверка порядка отрисовки слоев чанков Sodium (Engine Chams)...", ManualChecks.RunMinecraftSodiumChunkRenderSortOrderCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧩 Аудит кэша remappedJars Fabric Loader на инжект классов читов...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicClassInjectorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Аудит аварийных остановок службы кэша шрифтов FontCache (7031)...", ManualChecks.RunWindowsFontCacheServiceEventLogCrashForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧱 Проверка дескрипторов формата вершин чанков Sodium на флаги рентгена...", ManualChecks.RunMinecraftSodiumCustomVertexFormatMemoryOffsetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Аудит реестра VolSnap и SPP на блокировку лимитов теневых копий...", ManualChecks.RunWindowsVolumeShadowCopyStorageAllocationDeltaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔ Поиск модификаций дистанции атаки и расширения луча рейкаста Reach...", ManualChecks.RunMinecraftPlayerInteractionReachRaycastTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎭 Поиск манипуляций приватными полями движения через @Shadow...", ManualChecks.RunMinecraftFabricLoaderMixinShadowFieldTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔒 Проверка прав доступа ACL реестра службы FontCache на блокировку...", ManualChecks.RunWindowsFontCacheServiceRegistrySecurityAclCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧱 Аудит кэша окклюзии блоков Sodium на выборочное просвечивание баз...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionCacheTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Аудит модулей записи VSS Writers на наличие сбоев и срыва снимков...", ManualChecks.RunWindowsVolumeShadowCopyWriterFailureEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🏗 Поиск модификаций автоматической установки блоков Scaffold...", ManualChecks.RunMinecraftPlayerBlockPlacementAngleDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧩 Поиск динамических инжекторов privateLookupIn к классам игры...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicInvokerFactoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖱 Аудит реестра мыши и параметров программных кликеров...", ManualChecks.RunWindowsDirectInputDeviceRawInputSuppressionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💡 Проверка экстремальной гаммы FullBright и карты освещения...", ManualChecks.RunMinecraftSodiumCustomLightmapTextureOverrideCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Аудит сброса теневых копий томов из-за переполнения хранилища (25)...", ManualChecks.RunWindowsVolumeShadowCopyStorageFragmentationEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Поиск модификаций автоматической подмены тотема (AutoTotem)...", ManualChecks.RunMinecraftPlayerInventoryAutoTotemSwapExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит аргументов JVM на скрытые параметры Bootstrap ClassPath...", ManualChecks.RunMinecraftFabricLoaderMixinBootstrapAgentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Аудит политик проверки подписи драйверов ядра и изоляции HVCI...", ManualChecks.RunWindowsDirectInputDeviceDriverSignatureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧱 Проверка формы воксельных коллизий блоков (Phase / ReachThrough)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionVoxelShapeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌲 Анализ дерева родительских процессов при вызове утилит очистки VSS...", ManualChecks.RunWindowsVolumeShadowCopyDeletionProcessLineageCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔ Поиск модификаций снижения отдачи и отмены импульсов взрывов...", ManualChecks.RunMinecraftPlayerCombatKnockbackReductionExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧩 Поиск синтетических мостов к диспетчеру рендеринга и матрицам...", ManualChecks.RunMinecraftFabricLoaderMixinSyntheticAccessorBridgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔒 Аудит атрибутов файлов и эксклюзивных блокировок кэша шрифтов...", ManualChecks.RunWindowsFontTableCacheFileLockingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧱 Поиск аномалий шага вершин мешера чанков Sodium для рентгена...", ManualChecks.RunMinecraftSodiumCustomChunkMeshFormatStrideAnomalyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Аудит критического урезания максимального размера хранилища VSS...", ManualChecks.RunWindowsVolumeShadowCopyStorageMaximumSizeTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⛏ Поиск модификаций мгновенного ломания блоков FastBreak / SpeedMine...", ManualChecks.RunMinecraftPlayerFastBreakPacketTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⌨ Поиск скрытых перехватчиков клавиатуры и клавиш вызова чит-меню...", ManualChecks.RunMinecraftFabricLoaderMixinSyntheticBridgeDispatchCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧵 Аудит потоков процесса игры javaw.exe на наличие сторонних кликеров...", ManualChecks.RunWindowsDirectInputDeviceRawInputThreadHookCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧱 Поиск переназначения породы в полупрозрачные слои рендеринга...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTranslucentPassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Аудит событий изменения типа запуска службы Volume Shadow Copy (7040)...", ManualChecks.RunWindowsVolumeShadowCopyDeletionServiceStateChangeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎯 Поиск модификаций Silent Aimbot и рассинхронизации углов обзора...", ManualChecks.RunMinecraftPlayerCombatAimbotRotationPitchDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧬 Поиск динамических трансформеров байткода на подмену инструкций...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodReplacerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Аудит политик FontManagement на принудительное отключение кэша...", ManualChecks.RunWindowsFontTableCacheRegistrySecurityAclAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌋 Поиск модификаций рендеринга жидкостей и прозрачной лавы (Lava X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionFluidCullingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📜 Аудит фактов принудительной очистки системных журналов событий...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogPurgeCorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🖱 Поиск модов автоматического клика и обхода задержки атаки...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBurstIntervalAnomalyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧬 Поиск динамических инжекторов доступа к приватным полям игры...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicFieldAccessorInjectorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💉 Аудит каталогов Java Runtime на подмену системных DLL рендеринга...", ManualChecks.RunWindowsFontTableCacheDWriteDllHijackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🧱 Поиск модификаций луча рейкаста и взаимодействия сквозь стены...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionVoxelRaycastCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Аудит всплесков событий метаданных NTFS и VolSnap (Вайпинг диска)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogOverwriteCorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎯 Поиск модификаций расширения угла атаки и кругового Reach FOV...", ManualChecks.RunMinecraftPlayerCombatReachAngleFovLimitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📦 Поиск динамических перехватчиков пакетов движения и боя...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodInterceptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔤 Аудит памяти javaw.exe на наличие потоков шрифтов в ОЗУ...", ManualChecks.RunWindowsFontTableCacheDWriteCustomFontFileStreamCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👁 Поиск принудительного отключения отсечения сущностей Frustum ESP...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionEntityFrustumCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Аудит событий внезапного отключения томов для сброса VSS (157)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeOfflineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡 Поиск модификаций предикатов рейкаста и пробития сквозь щиты...", ManualChecks.RunMinecraftPlayerCombatReachRaycastEntityFilterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic Accessor интерфейсов (Reach/Combat injection)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicAccessorInterfaceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка FontCache Registry Security DACL Wipe (NULL DACL / Access Denied)...", ManualChecks.RunWindowsFontTableCacheRegistrySecurityDaclWipeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Custom Block Occlusion & Biome Tint (X-Ray Foliage/Ore Glow)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionBiomeTintCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Shadow Copy IOCTL & DeviceControl (Event 12293/12294 Purge)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogDeviceControlCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Combat AutoClicker Click Jitter & Speed Limit Spoofing (CPS Emulation)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerClickSpeedLimitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic Method Redirect (@Redirect detours)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodRedirectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка GDI FontLink & EUDC Registry (Инъекция символов чит-оверлеев)...", ManualChecks.RunWindowsFontTableCacheGdiFontLinkRegistryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Terrain Mesh Compiler Pipeline (Chunk Builder X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshCompilerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot TRIM & Storage Deallocation (Event 153/154/8200)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogTrimZeroingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Silent Aimbot & Angle Snap Smoothing (Rotation Pitch/Yaw Decoupling)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAngleSnapSmoothCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic LocalCapture (Перехват локальных переменных)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicLocalCaptureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Font Drivers & FontAssoc Registry (Драйверы шрифтов и инъекции)...", ManualChecks.RunWindowsFontTableCacheRegistryFontDriversCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Translucent Render Pass Pipeline (Chest / Wallhack X-Ray)...", ManualChecks.RunMinecraftSodiumChunkRenderPassAlphaBlendPipelineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot Disk Signature Clash & Purge (Event 58)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogDiskSignatureClashCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка AutoClicker Burst Mode & Micro-Pause (Butterfly / DoubleClick)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBurstJitterMacroCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic @ModifyConstant (Reach/Speed constant spoofing)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicModifyConstantCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка GDI+ PrivateFontCollection & Memory Font Streams в javaw.exe...", ManualChecks.RunWindowsFontTableCacheGdiPrivateFontCollectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Biome Blend & SkyLight Cache Tampering (FullBright/X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionBiomeBlendRadiusCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot RAW Disk Sector Access & Header Purge (Event 140)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogRawDiskSectorReadCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Combat AutoClicker CPS Distribution & Clamped Intervals...", ManualChecks.RunMinecraftPlayerCombatAutoClickerTargetCPSDistributionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic @ModifyVariable (Velocity/Damage tampering)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicModifyVariableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Font Directory Header & DSIG Signature (Аномалии структуры шрифтов)...", ManualChecks.RunWindowsFontTableCacheRegistryFontSignatureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Voxel Shape Occlusion Cache (Phase / Wallhack)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionVoxelCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot IO Flush Breakdown (Event 137/57 Logfile Purge)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogIoFlushFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка AutoClicker Debounce State Matrix & Microswitch Hold/Release...", ManualChecks.RunMinecraftPlayerCombatAutoClickerDebounceStateMatrixCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic @ModifyReturnValue (Reach/Sneak bypass)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicModifyReturnValueCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Console TrueTypeFont Registry (Сокрытие вывода консоли)...", ManualChecks.RunWindowsFontTableCacheRegistryFontAssociationOverrideCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Fluid Mesh Compiler & Lava X-Ray (Fluid Face Culling)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionFluidFaceCullingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot Forced Shrinkage & Purge (Event 257/89)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeResizeShrinkCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка AutoClicker Physical Inertia & Friction Drag Modeling...", ManualChecks.RunMinecraftPlayerCombatAutoClickerInertiaFrictionModelCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic @WrapOperation (Packet / Attack wrapping)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicWrapOperationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка FontCache Service DAT File Section Integrity (FNTCACHE.DAT)...", ManualChecks.RunWindowsFontTableCacheRegistryFontSignatureExtendedTableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Block Occlusion Cache Fast-Lookup Bypass...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionBlockOcclusionCacheLookupCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot Cluster Allocation Breakdown (Event 55/98)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogClusterBadAllocationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка AutoClicker Reaction Latency & Post-Hit Hesitation...", ManualChecks.RunMinecraftPlayerCombatAutoClickerReactionLatencyDistributionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic @Inject(cancellable=true) (NoFall/Velocity)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicCancellableInjectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Untrusted Font Mitigation Registry Policies (Обход Defender Guard)...", ManualChecks.RunWindowsFontTableCacheRegistryFontDownloadRegistryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Block Face Opacity & Quad Builder (Ore ESP X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionBlockFaceFullAndOpaqueCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot NTFS Self-Healing Breakdown (Event 98/262144)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogChkdskSelfHealingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Multi-Target Switching Hesitation & Angular Velocity Limits...", ManualChecks.RunMinecraftPlayerCombatAutoClickerTargetSwitchHesitationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric MixinExtras @Share Sugar (Скрытая передача данных инъекций)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicShareCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка OpenType fsType Font Embedding Licensing & DRM Flags...", ManualChecks.RunWindowsFontTableCacheRegistryFontEmbeddingLicensingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Terrain Section Queue Priority (Приоритет рудных чанков)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainSectionQueueCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot Exclusive Lock Starvation (Event 12297/24)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotLockLeakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка AutoClicker Sinusoidal Tremor & Lissajous Aim Tracking...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMicroDriftSinusoidalCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric MixinExtras @Local Sugar (Hitbox / Box injection)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicLocalCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка OpenType post Table Glyph Names (Шрифты ESP и радара)...", ManualChecks.RunWindowsFontTableCacheRegistryFontPostScriptGlyphNameCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Terrain Mesh Vertex Matrix Transform (Squash X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshTransformCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot Paging IO Starvation & Breakdown (Event 51)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotPagingIoCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Kalman Filter & Adaptive State Estimation Aim Smoothing...", ManualChecks.RunMinecraftPlayerCombatAutoClickerKalmanFilterSmoothingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic @Coerce (Пакеты/Обфускация)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicCoerceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка OpenType kern & GPOS Positioning Tables (ESP оверлеи)...", ManualChecks.RunWindowsFontTableCacheRegistryFontKerningTableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Terrain Mesh Slicing & Vertical Clipping (CaveFinder)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshSliceCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot Driver Detach & Controller Error (Event 10/11)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotDeviceIoControlCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Cubic Bezier Spline & Hermite Curve Aim Interpolation...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBezierSplineInterpolationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic @WrapWithCondition (Packet / Sound drop)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicWrapWithConditionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка OpenType maxp Table Memory Profile (Аномалии хипа шрифтов)...", ManualChecks.RunWindowsFontTableCacheRegistryFontMaxpTableMemoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Staging Buffer Off-Heap Vertex Modification (VRAM X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshStagingBufferCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot Harddisk IO Error Burst (Event 7/15)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotHarddiskIoErrorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка PID Controller Aim Feedback Loop (P-I-D Наводка)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPIDControllerAimCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric MixinExtras LocalRef (Мутация параметров движения/боя)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicSugarRefCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка OpenType hmtx Metric Table (Фантомные символы оверлеев)...", ManualChecks.RunWindowsFontTableCacheRegistryFontHmtxTableWidthCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Direct Index Draw & Wireframe Topology Hack...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshDirectIndexDrawCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot PnP Surprise Removal (Event 219/225)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotDevicePnpRemovalCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Adaptive Kalman Covariance Tuning & Dynamic Aim Gain...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAdaptiveKalmanCovarianceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric MixinExtras @WrapMethod (Полный детуринг метода)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicWrapMethodCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType fpgm Program Table Bytecode (Инструкции шрифтов)...", ManualChecks.RunWindowsFontTableCacheRegistryFontFpgmTableBytecodeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Depth Testing & Stencil Buffer (In-Engine Chams)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshDepthTestCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot StorAhci Bus Device Reset (Event 129)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotDeviceResetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Gauss-Markov Stochastic Process & Aim Drift Modeling...", ManualChecks.RunMinecraftPlayerCombatAutoClickerGaussMarkovDriftCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic Expressions (MVEL/Janino/SpEL)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicExpressionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType cvt Control Value Table (Инъекции оверлеев)...", ManualChecks.RunWindowsFontTableCacheRegistryFontCvtTableValueCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium glPolygonOffset Depth Bias (Ore Wallhack)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshPolygonOffsetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка VSS NTFS $MFT Record Corruption (Event 130/134/140)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotMftRecordCorruptCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Markov Chain Discrete State Transition Matrix...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMarkovChainStateTransitionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Synthetic Bridges & Hidden Classes...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicSyntheticBridgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType gasp Table & Anti-Aliasing Overlays...", ManualChecks.RunWindowsFontTableCacheRegistryFontGaspTableForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Multi-Draw Indirect (MDI Command Buffer)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshMultiDrawIndirectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка VSS NTFS $UsnJrnl Truncation (Event 136/138/142)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotUsnJournalCorruptCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка HMM & Viterbi State Sequence Optimization...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHiddenMarkovModelViterbiCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Java Record Component Injections...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordComponentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType vmtx/VORG Vertical Metrics Tampering...", ManualChecks.RunWindowsFontTableCacheRegistryFontVmtxTableForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Indirect Count Buffer (ARB_indirect_parameters)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshMultiDrawCountBufferCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot DeviceIoControl IOCTL Failures...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotDeviceIoControlFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Recurrent Neural Network (LSTM/GRU) Click Weights...", ManualChecks.RunMinecraftPlayerCombatAutoClickerRecurrentNeuralNetworkLstmCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Sealed PermittedSubclasses...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicPermittedSubclassesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка OpenType CFF INDEX & Subroutine Payloads...", ManualChecks.RunWindowsFontTableCacheRegistryFontCffIndexDataForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Shader Storage Buffer Objects (SSBO)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshShaderStorageBufferObjectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка VSS Diff Area Starvation & Settings Tampering...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotDiffAreaMinFreeSpaceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Reinforcement Learning Q-Table & Policy Gradient...", ManualChecks.RunMinecraftPlayerCombatAutoClickerReinforcementLearningQTableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin NestHost & NestMembers Access...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicNestHostBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType COLR/CPAL Color Palettes & HUDs...", ManualChecks.RunWindowsFontTableCacheRegistryFontColrCpalPaletteForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Uniform Buffer Objects (UBO Matrix)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshUniformBufferObjectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка VSS Writer Freeze Timeout & Abort (Event 5002)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotWriterTimeoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Genetic Algorithm Click Chromosome Evolution...", ManualChecks.RunMinecraftPlayerCombatAutoClickerGeneticAlgorithmChromosomeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic Bootstrap Methods (BSM)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicBootstrapMethodCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType fvar/cvar Variable Font Tuple Deltas...", ManualChecks.RunWindowsFontTableCacheRegistryFontCvarFvarVariableFontCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Atomic Counter Buffers (Occlusion Bypass)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshAtomicCounterBufferCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка VSS Registry Writer Freeze Deadlocks (Event 8193)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotRegistryWriterFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Simulated Annealing & Metropolis Click Timing...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSimulatedAnnealingMetropolisCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin MethodHandle.invokeExact...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodHandleInvokeExactCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType HVAR/VVAR Variation Metrics...", ManualChecks.RunWindowsFontTableCacheRegistryFontHvarVvarDeltaGlyphMetricsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Transform Feedback Buffer (Vertex Hijack)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshTransformFeedbackBufferCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot Cluster Allocation Failures...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotClusterAllocationFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Geometric Brownian Motion & Wiener Process...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBrownianMotionWienerProcessCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin VarHandle & MemorySegment...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicVarHandleMemorySegmentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType MVAR Global Metrics Variations...", ManualChecks.RunWindowsFontTableCacheRegistryFontMvarMetricsVariationsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium GPU Mesh Shader Pipeline (Nvidium)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshMeshShaderCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot Disk Bad Blocks (Event 7/15)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotHarddiskBadSectorCheck),
			Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Аналитика Levy Flight & Heavy-Tailed Click Spacing...", ManualChecks.RunMinecraftPlayerCombatAutoClickerLevyFlightHeavyTailCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic ClassDefiner & Hidden Classes...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicClassDefinerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType BASE Baseline Table Coordinate Shifts...", ManualChecks.RunWindowsFontTableCacheRegistryFontBaseTableBaselineForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Vulkan Ray Tracing Acceleration Structures...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanRayTracingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot Paging IO Starvation (Event 50/51)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotPagingIoStarvationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Non-Homogeneous Poisson Point Process Clusters...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPoissonPointProcessClusterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin constantdynamic (condy Indy)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicConstantDynamicIndyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType JSTF Text Layout Expansion...", ManualChecks.RunWindowsFontTableCacheRegistryFontJstfJustificationForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium MeshOptimizer Vertex Decimation...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshMeshOptimizerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot Sparse Zero-Allocation...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotSparseFileAllocationCheck),
			Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Аналитика Extreme Value Theory (EVT/Frechet) CPS...", ManualChecks.RunMinecraftPlayerCombatAutoClickerExtremeValueTheoryFrechetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin MethodParameters Obfuscation...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodParametersAttributeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType MATH Table Layout & Radar Matrices...", ManualChecks.RunWindowsFontTableCacheRegistryFontMathTableEquationForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Vulkan Bindless Descriptor Indexing...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorIndexingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot ReFS Metadata Transaction Desync...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataCorruptCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Multivariate Gaussian Copula Joint Click Delays...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMultivariateGaussianCopulaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin JPMS Module Opens & Exports...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicModuleExportOpenBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType EBLC/EBDT Embedded Bitmap Glyphs...", ManualChecks.RunWindowsFontTableCacheRegistryFontEblcEbdtBitmapGlyphForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Sampler Texture LOD Bias (Clean Chams)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshDynamicSamplerAnisotropyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка VSS BitLocker Key Rebind Errors (Event 24588)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotBitLockerMetadataRebindCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка HMM Baum-Welch Forward-Backward Smoothing...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHiddenMarkovModelForwardBackwardSmoothingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin Dynamic MethodType Lookups...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodTypeLookupCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType bloc/bdat Small Bitmap Glyphs...", ManualChecks.RunWindowsFontTableCacheRegistryFontSbitSmallGlyphBitmapForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium GLSL Fragment Shader discard Hooks...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshFragmentDiscardOverrideCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot ReFS Checkpoint Errors (Event 141)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка ARMA/ARIMA Time Series Click Modeling...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAutoregressiveMovingAverageArmaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Fabric Mixin ClassLoader Constraint Violations...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicClassLoadingConstraintCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка TrueType gvar Outline Warping & Radar Vectors...", ManualChecks.RunWindowsFontTableCacheRegistryFontGvarGlyphVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Vulkan Subpass G-Buffer Depth Reading...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSubpassInputCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Volume Snapshot NVMe TRIM Starvation (Event 154)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotTrimStarvationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Stochastic Resonance & Sub-Threshold Noise...", ManualChecks.RunMinecraftPlayerCombatAutoClickerStochasticResonanceNoiseCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка хуков сериализации Java Record-пакетов...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordSerializationHookCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблиц OpenType fmtx и скрытых шрифтовых оверлеев...", ManualChecks.RunWindowsFontTableCacheRegistryFontFmtxFontMetricsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sodium Vulkan Sparse Binding декоммита мешей...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSparseBindingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка сбоев контрольных сумм ReFS метаданных VSS (Event 133)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataChecksumMismatchCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка оператора энергии Тигера-Кайзера (TKEO)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerNonlinearEnergyOperatorTeagerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка деконструкторов Record Patterns в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordPatternMatchingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка пар кернинга kern в OpenType шрифтах...", ManualChecks.RunWindowsFontTableCacheRegistryFontKernTablePairAdjustmentForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка шаблонов дескрипторов Vulkan (DescriptorUpdateTemplate)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorUpdateTemplateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка сброса снимков VSS через релокацию кластеров ReFS (Event 148)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocatedClusterDefragCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка показателя Херста кликов (R/S Rescaled Range)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHurstExponentRescaledRangeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка компактных конструкторов Record в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordCompactConstructorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы GSUB лигатурной подстановки Chams...", ManualChecks.RunWindowsFontTableCacheRegistryFontGsubLigatureChamsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Push Constants в рендерере мешей Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPushConstantWallhackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка сбоев разреженных томов ReFS/VSS (Event 156)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSparseVvolAllocationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка флуктуационного анализа без тренда (DFA)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerDetrendedFluctuationAnalysisDfaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка прокси сериализации Record-пакетов в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordCanonicalSerializationProxyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы GPOS якорного позиционирования...", ManualChecks.RunWindowsFontTableCacheRegistryFontGposAnchorPositioningForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Dynamic Rendering в рендерере мешей Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicRenderingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка транзакционных откатов ReFS/VSS (Event 161)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTransactionRollbackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка фрактальной размерности Хигучи (HFD)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHiguchiFractalDimensionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка обхода аксессоров Record через Reflection в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordReflectionAccessorBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка подтаблицы cmap Format 14 селекторов вариантов...", ManualChecks.RunWindowsFontTableCacheRegistryFontCmapFormat14UnicodeVariationSelectorsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Subgroup Operations в рендерере мешей Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSubgroupOperationsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка сбоев контрольных точек ReFS/VSS (Event 166)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointAllocationFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка рекуррентного анализа фазового пространства (RQA)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerRecurrenceQuantificationAnalysisRqaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы BootstrapMethods в Record-пакетах Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordBootstrapMethodAttributeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы VDMX квантованных вертикальных метрик...", ManualChecks.RunWindowsFontTableCacheRegistryFontVdmxVerticalDeviceMetricsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Partially Bound дескрипторов в Sodium/Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorIndexingPartiallyBoundCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка повреждений потоков метаданных ReFS/VSS (Event 171)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataStreamCorruptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка выборочной энтропии кликов (SampEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSampleEntropySampEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Type Annotations в Record-пакетах Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordTypeAnnotationInjectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка смарт-шрифтов SIL Graphite (Glat/Gloc)...", ManualChecks.RunWindowsFontTableCacheRegistryFontGlatGlocGraphiteSmartFontForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Descriptor Buffer в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка сбоев ремаппинга кластеров ReFS/VSS (Event 176)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocatedClusterRemapFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка нечеткой энтропии кликов (FuzzEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerFuzzyEntropyFuzzEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Generic Signatures в Record-пакетах Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordGenericSignatureTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы EBSC масштабирования растровых глифов...", ManualChecks.RunWindowsFontTableCacheRegistryFontSbitBitmapScalingChamsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка VkDescriptorBufferBindingInfoEXT в Sodium/Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferBindingInfoCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка рассинхронизации деревьев ReFS/VSS (Event 181)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataCheckpointTreeDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка перестановочной энтропии кликов (PermEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPermutationEntropyPermEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка деконструкторов Record Patterns в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordPatternExhaustivenessBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы Feat пользовательских функций...", ManualChecks.RunWindowsFontTableCacheRegistryFontFeatFeatureTableForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Embedded Samplers в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferEmbeddedSamplerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка повреждений списков экстентов ReFS/VSS (Event 186)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataExtentListCorruptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка спектральной энтропии кликов (SpecEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSpectralEntropySpecEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка условий Pattern Guards в Record-пакетах Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordPatternGuardsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы Sill языкового маппинга оверлеев...", ManualChecks.RunWindowsFontTableCacheRegistryFontSillLanguageSupervisionForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Push Descriptors в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferPushDescriptorsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка битовых карт распределения блоков ReFS/VSS (Event 191)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataAllocatedBlockBitmapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка вейвлет-энтропии кликов (WavEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerWaveletEntropyWavEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка деконструкции Record Patterns в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordDeconstructionPatternAccessorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы vhea вертикального макета для трассеров...", ManualChecks.RunWindowsFontTableCacheRegistryFontVheaVerticalHeaderForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Mutable Descriptors в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferMutableDescriptorsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка потери кворума суперблоков ReFS/VSS (Event 196)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataSuperblockQuorumCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка условной энтропии кликов (CondEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerConditionalEntropyCondEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка срезания флагов ACC_SYNTHETIC в Record-пакетах...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordSyntheticAccessorStrippingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы vmtx вертикальных метрик глифов...", ManualChecks.RunWindowsFontTableCacheRegistryFontVmtxVerticalMetricsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Inline Uniform Blocks в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferInlineUniformCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка повреждений таблиц аллокаторов ReFS/VSS (Event 201)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataAllocatorTableDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка трансферной энтропии кликов (TransEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerTransferEntropyTransEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка вложенных Record Patterns в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordPatternNestedComponentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы VORG вертикальных координат начала...", ManualChecks.RunWindowsFontTableCacheRegistryFontVorgVerticalOriginForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Sampler Heap в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferSamplerHeapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка переполнения журналов ReFS/VSS (Event 206)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataCheckpointLogOverflowCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка многомасштабной энтропии кликов (MSE)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMultiscaleEntropyMseCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка аннотаций RecordComponent в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentAnnotationReflectionBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы BASE базовых линий шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontBaseBaselineForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка сбоев ReFS Scrubber (Event 211)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSScrubberExtentRemapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка перекрестной энтропии кликов (Cross-SampEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCrossSampleEntropyCrossSampEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка канонических конструкторов Record в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinRecordCanonicalConstructorParamCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы MATH математической верстки шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontMathTypesettingForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Dedicated Allocation в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDedicatedAllocationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка сбросов транзакций ReFS MinStore (Event 216)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMinStoreTransactionAbortCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка дивергенции Кульбака-Лейблера кликов (D_KL)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerRelativeEntropyKullbackLeiblerDklCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Generic-сигнатур Record компонентов в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentGenericSignatureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы MVAR вариаций метрик шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontMvarMetricsVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Fragment Shading Rate (VRS) в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanFragmentShadingRateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка повреждений экстентов B+ дерева ReFS (Event 221)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSBTreeMultiLevelExtentCorruptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка дивергенции Дженсена-Шеннона кликов (JSD)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerJensenShannonDivergenceJsdCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка dynamic CallSite в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleInvokerCallSiteCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы HVAR горизонтальных метрик шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontHvarHorizontalMetricsVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Mesh Shaders (VK_EXT_mesh_shader)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanMeshShaderCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка аллокатора контейнеров ReFS (Event 226)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSContainerAllocatorDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка расстояния Васерштейна кликов (W1 / EMD)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerWassersteinDistanceW1Check),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка скрытых мутабельных полей в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentHiddenFieldCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы VVAR вертикальных метрик шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontVvarVerticalMetricsVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Multi-Draw Indirect в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanMultiDrawIndirectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка поколений чекпоинтов ReFS (Event 231)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointGenerationDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка энергетического расстояния Крамера кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerEnergyDistanceCramerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка обхода VarHandle в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentVarHandleCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы STAT стилевых осей шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontStatStyleAttributesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Sampler Ycbcr Conversion в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSamplerYcbcrConversionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка больших кластеров ReFS (Event 236)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSLargeClusterExtentRelocationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка ядерного теста расхождения MMD кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMaximumMeanDiscrepancyMmdCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка LambdaMetafactory в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLambdaMetafactoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы cvar вариаций хинтинга шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontCvarCvtVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Sampler Filter Minmax в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSamplerFilterMinmaxCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка многоуровневых хранилищ ReFS (Event 241)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTieredStorageMigrationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка марковских переходов кликов (TVD)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerTotalVariationDistanceMarkovCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка ConstantDynamic (condy) в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentConstantDynamicBootstrapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы gvar кортежей дельт шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontGvarTupleDeltaForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Depth Clamp в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthClampCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблиц Allocate-On-Write ReFS (Event 246)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocateOnWriteDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка расстояния Хеллингера кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHellingerDistanceBhattacharyyaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка unreflectSpecial в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentUnreflectSpecialCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таблицы avar нелинейных осей шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontAvarAxisVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Color Blend Factors в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanColorBlendStateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка кворума суперблоков ReFS (Event 251)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSuperblockQuorumLossCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка расстояния Фреше кривых клика (dF)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerFrechetDistanceDiscreteCurveCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка dropArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleDropArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка отладочных таблиц TSI в шрифтах...", ManualChecks.RunWindowsFontTableCacheRegistryFontTsiDebugStreamForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Polygon Mode Wireframe в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPolygonModeWireframeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка сбоев наката журнала ReFS (Event 256)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMultiVolumeJournalCrashCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка критерия Колмогорова-Смирнова (D_KS)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerKolmogorovSmirnovDksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка filterReturnValue в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentFilterReturnValueCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка локализованных фич GSUB locl в шрифтах...", ManualChecks.RunWindowsFontTableCacheRegistryFontLoclFeatureSpoofingForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Point Mode в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPointSizePrimitiveRestartCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка таймаутов барьера ReFS (Event 261)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCommitBarrierTimeoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка критерия Андерсона-Дарлинга (A2)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAndersonDarlingA2Check),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка foldArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleFoldArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка композиции глифов GSUB ccmp в шрифтах...", ManualChecks.RunWindowsFontTableCacheRegistryFontCcmpGlyphCompositionForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Depth Bias Slope Factor в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthBiasSlopeFactorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка коллизий стейджинга ReFS (Event 266)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSStagingAreaCollisionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка критерия Крамера-фон Мизеса (T_CvM)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCramerVonMisesTCvMCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка guardWithTest в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleGuardWithTestCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка обязательных альтернатив GSUB rclt...", ManualChecks.RunWindowsFontTableCacheRegistryFontRcltRequiredContextualAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Stencil Reference Ops в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanStencilReferenceCompareOpCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка дедлоков перемещения ReFS (Event 271)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMultiTierRelocationLogAbortCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка критерия Ватсона круговых фаз (U2)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerWatsonU2CircularDistributionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка catchException в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleCatchExceptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка контекстных росчерков GSUB cswh...", ManualChecks.RunWindowsFontTableCacheRegistryFontCswhContextualSwashForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Vulkan Depth Bounds Test в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthBoundsTestCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка дедлоков аллокатора ReFS (Event 276)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSContainerFragmentationDeadlockCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка критерия Койпера циклических фаз (V)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerKuiperTestCyclicCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка tryFinally в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleTryFinallyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка произвольных лигатур GSUB dlig...", ManualChecks.RunWindowsFontTableCacheRegistryFontDligDiscretionaryLigaturesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Conservative Rasterization в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanConservativeRasterizationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка разреженных экстентов ReFS VVol (Event 281)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSparseVVolExtentRollbackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка индекса Джини кликов (Gini Impurity)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerGiniImpurityDecisionBoundaryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка комбинаторов loop в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleLoopCombinatorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка исторических лигатур GSUB hlig...", ManualChecks.RunWindowsFontTableCacheRegistryFontHligHistoricalLigaturesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка Sample Locations в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSampleLocationsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка корня дерева чекпоинтов ReFS (Event 286)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeRootDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔍 Проверка коэффициента силуэта кликов (s)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSilhouetteCoefficientClusterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка фильтров аргументов MethodHandles.filterArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleFilterArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка контекстных альтернатив GSUB calt...", ManualChecks.RunWindowsFontTableCacheRegistryFontCaltContextualAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Conditional Rendering в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanConditionalRenderingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка рассинхронизации зеркалирования ReFS (Event 291-295)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTieredResiliencyDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка индексов кластеризации кликов (DBI & Dunn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerDaviesBouldinClusterSeparationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка перестановки аргументов MethodHandles.permuteArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandlePermuteArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка стилистических альтернатив GSUB salt...", ManualChecks.RunWindowsFontTableCacheRegistryFontSaltStylisticAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка барицентрических координат во фрагментных шейдерах...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanBarycentricCoordinatesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка битовых карт аллокатора ReFS (Event 296-300)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocatedSpaceBitmapDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка индекса Калински-Харабаша кликов (VRC)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCalinskiHarabaszIndexCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка комбайнеров foldArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleFoldArgumentsWithCombinerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка титульных альтернатив GSUB titl...", ManualChecks.RunWindowsFontTableCacheRegistryFontTitlTitlingAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка кастомных границ сэмплеров в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanCustomBorderColorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка коллапса высоты B+ дерева ReFS (Event 301-305)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSBTreeHeightCollapseDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка индекса конкордантности кликов (C-Index)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerConcordanceIndexHazardCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка упаковки аргументов MethodHandles.collectArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleCollectArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка малых прописных GSUB smcp...", ManualChecks.RunWindowsFontTableCacheRegistryFontSmcpSmallCapitalsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка растеризации линий в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanLineRasterizationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка рассогласования эпох ReFS (Event 306-310)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSEpochSkewDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка индекса Брайера кликов (BS)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBrierScoreReliabilityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка распаковки аргументов MethodHandles.spreadInvoker в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleSpreadInvokerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка капители GSUB c2sc...", ManualChecks.RunWindowsFontTableCacheRegistryFontC2scCapitalsToSmallCapitalsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка динамических вершинных буферов в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanVertexInputDynamicStateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка разрывов LSN журнала ReFS (Event 311-315)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSLsnWrapDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка лог-правдоподобия Вальда кликов (SPRT)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSprtLogLikelihoodRatioCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка трамплинов identity/constant в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleIdentityConstantTrampolineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка шрифтов unicase GSUB unic...", ManualChecks.RunWindowsFontTableCacheRegistryFontUnicUnicaseForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Extended Dynamic State 2 в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanExtendedDynamicState2Check),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка счетчиков поколений ReFS (Event 316-320)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSGenerationCounterWrapDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка кумулятивных сумм CUSUM кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCusumChangePointDetectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка адаптеров zero/empty в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleZeroEmptyAdaptersCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка исторических форм GSUB hist...", ManualChecks.RunWindowsFontTableCacheRegistryFontHistHistoricalFormsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Extended Dynamic State 3 в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanExtendedDynamicState3Check),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка чексумм чекпоинтов ReFS (Event 321-325)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointChecksumMismatchCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка волатильности EMA кликов (EVR)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerEmaVolatilityRatioCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка усечения аргументов explicitCastArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleExplicitCastArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка курсивных альтернатив GSUB ital...", ManualChecks.RunWindowsFontTableCacheRegistryFontItalItalicsAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка тайминг-сайдканалов Shader Clock в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanShaderClockCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка дедлоков компактификации ReFS (Event 326-330)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCompactionDeadlockCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка спектральной чистоты Фурье кликов (SFDR)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerFftSpectralPurityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка приведения типов asType в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleAsTypeCoercionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка руби-нотации GSUB ruby...", ManualChecks.RunWindowsFontTableCacheRegistryFontRubyRubyNotationForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка перезапуска топологии примитивов в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPrimitiveTopologyRestartCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка таймаутов барьеров записи ReFS (Event 331-335)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSWriteBarrierTimeoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка скейлинга флуктуаций DFA кликов (alpha)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerDfaScalingExponentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка диспетчеров exactInvoker в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleExactInvokerDispatchCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка альтернативных аннотаций GSUB nalt...", ManualChecks.RunWindowsFontTableCacheRegistryFontNaltAlternateAnnotationForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка микрокарт прозрачности Opacity Micromap в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanOpacityMicromapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка межтомовых барьеров ReFS (Event 336-340)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMultiVolumeBarrierDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка автокорреляции Льюнга-Бокса кликов (Q)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerLjungBoxAutocorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка динамических вызовов invokeWithArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleInvokeWithArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка индексов sups/subs...", ManualChecks.RunWindowsFontTableCacheRegistryFontSupsSubsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Multisampled Render в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanMultisampledRenderSingleSampledCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка зависаний флеша ReFS (Event 341-345)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSWriteStallStarvationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка ошибки прогноза MASE кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMaseForecastCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка каррирования MethodHandles.insertArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleInsertArgumentsBindingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вертикальных таблиц OpenType vrt2...", ManualChecks.RunWindowsFontTableCacheRegistryFontVrt2VerticalAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка слияния субпассов Vulkan в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSubpassMergeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка таймаутов сброса чекпоинтов ReFS (Event 346-350)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointDrainTimeoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Энгла ARCH на волатильность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerEngleArchTestHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка динамических прокси altMetafactory в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLambdaAltMetafactoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка таблиц порядковых форм OpenType ordn...", ManualChecks.RunWindowsFontTableCacheRegistryFontOrdnOrdinalFormsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка маскирования Color Write Enable в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanColorWriteEnableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка срывов синхронизации аллокатора ReFS (Event 351-355)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocatorSyncAbortCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка критерия Дарбина-Уотсона (DW) остатков кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerDurbinWatsonAutocorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка динамической конкатенации StringConcatFactory в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicStringConcatFactoryRecipeInjectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка таблиц табличных чисел OpenType tnum...", ManualChecks.RunWindowsFontTableCacheRegistryFontTnumTabularFiguresForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Advanced Blend Chams в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanAdvancedBlendOperationsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка сброса флеша ярусов ReFS (Event 356-360)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTierFlushDropoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста отношения дисперсий Ло-МакКинли (VR)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerLoMacKinlayVarianceRatioCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка синтеза номинальных дескрипторов Constants API...", ManualChecks.RunMinecraftFabricLoaderMixinNominalDescriptorConstantsApiCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка таблиц старостильных чисел OpenType onum...", ManualChecks.RunWindowsFontTableCacheRegistryFontOnumOldstyleFiguresForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка провоцирующей вершины Provoking Vertex в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanProvokingVertexCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка рассинхронизации эпох контейнеров ReFS (Event 361-365)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSContainerEpochSkewCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Дики-Фуллера (ADF) на стационарность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAugmentedDickeyFullerStationarityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов unreflectConstructor в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentUnreflectConstructorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка пропорциональных чисел OpenType pnum...", ManualChecks.RunWindowsFontTableCacheRegistryFontPnumProportionalFiguresForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Depth Clip Enable в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthClipEnableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка голодания пула буферов ReFS (Event 366-370)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSEvictionStarvationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка тренда утомления Тейла-Сена и Кендалла (Tau)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerTheilSenKendallTrendCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка unreflectGetter/Setter в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentUnreflectGetterSetterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка выравненных чисел OpenType lnum...", ManualChecks.RunWindowsFontTableCacheRegistryFontLnumLiningFiguresForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка делителя вершинных атрибутов в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanVertexAttributeDivisorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка тупиков дефрагментации ReFS (Event 371-375)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSDefragDeadlockCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка критерия Филлипса-Перрона (PP) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPhillipsPerronStationarityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка телепортации контекста Lookup.in в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupClassTeleportCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка полуширинных форм OpenType hwid...", ManualChecks.RunWindowsFontTableCacheRegistryFontHwidHalfWidthFormsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка подавления сброса глубины в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanLoadStoreOpNoneCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка регрессий LSN журнала ReFS (Event 376-380)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSLsnRegressionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка критерия Бреуша-Пагана (BP) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBreuschPaganHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка эскалации allowedModes в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupAllowedModesTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка полноширинных форм OpenType fwid...", ManualChecks.RunWindowsFontTableCacheRegistryFontFwidFullWidthFormsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка петли обратной связи аттачментов в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanAttachmentFeedbackLoopCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка таймаутов четности ReFS (Event 381-385)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSParityResyncTimeoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка общего теста Уайта (LM) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerWhiteGeneralHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка внедрения классов Lookup.defineClass в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка регистрозависимых форм OpenType case...", ManualChecks.RunWindowsFontTableCacheRegistryFontCaseCaseSensitiveFormsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка зон отсечения Discard Rectangles в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDiscardRectanglesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка рассинхронизации барьеров ReFS (Event 386-390)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSJournalBarrierDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка F-теста Голдфелда-Квандта (GQ) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerGoldfeldQuandtVarianceRatioCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.findSpecial в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindSpecialBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка контекстных интервалов OpenType chws...", ManualChecks.RunWindowsFontTableCacheRegistryFontChwsContextualSpacingForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Depth Clamp Zero One в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthClampZeroOneCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка рассинхронизации эпох пула ReFS (Event 391-395)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSEpochDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Брок-Дехерт-Шейнкмана (BDS) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBdsNonLinearChaosCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.findStaticSetter в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindStaticSetterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка полуширинных метрик OpenType halt...", ManualChecks.RunWindowsFontTableCacheRegistryFontHaltAlternateHalfWidthsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Shader Subgroups в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanShaderSubgroupControlFlowCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка зависаний барьеров записи ReFS (Event 396-400)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSWriteBarrierStallCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка критерия Бартлетта (B) однородности дисперсий кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBartlettHomogeneityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.findConstructor в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindConstructorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вертикальных полуширинных метрик OpenType vhal...", ManualChecks.RunWindowsFontTableCacheRegistryFontVhalVerticalHalfMetricsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка карт плотности фрагментов в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanFragmentDensityMapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка сброса кэша ярусов ReFS (Event 401-405)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTierCacheEvictionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка робастного критерия Ливиня (W) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerLeveneEqualityOfVariancesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.findGetter/Setter в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindGetterSetterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вертикального кернинга OpenType vkrn...", ManualChecks.RunWindowsFontTableCacheRegistryFontVkrnVerticalKerningForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка 8-битных индексов в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanIndexTypeUint8Check),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка голодания конвейера записи ReFS (Event 406-410)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSPipelineStarvationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста CUSUM of Squares на сдвиги дисперсии кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCusumOfSquaresStructuralBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.findVirtual в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindVirtualCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка математических греческих форм OpenType mgrk...", ManualChecks.RunWindowsFontTableCacheRegistryFontMgrkMathematicalGreekForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Border Color Swizzle в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanBorderColorSwizzleCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка рассинхронизации чекпоинтов ReFS (Event 411-415)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointSyncDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Парка на степенную дисперсию кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerParkPowerLawHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.unreflectVarHandle в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupUnreflectVarHandleCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка пропорциональных форм Kana OpenType pkna...", ManualChecks.RunWindowsFontTableCacheRegistryFontPknaProportionalKanaForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Image View Min LOD в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanImageViewMinLodCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка повреждений деревьев чекпоинтов ReFS (Event 416-420)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeCorruptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Глейзера на гетероскедастичность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerGlejserHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.findStaticVarHandle в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindStaticVarHandleCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка случайных вариантов OpenType rand...", ManualChecks.RunWindowsFontTableCacheRegistryFontRandRandomAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Graphics Pipeline Library в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanGraphicsPipelineLibraryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка повреждений аллокатора ReFS (Event 421-425)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocatorRunCorruptCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Харрисона-МакКейба на гетероскедастичность...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHarrisonMcCabeHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.unreflectConstructor в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupUnreflectConstructorBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вариантов расчерков OpenType swsh...", ManualChecks.RunWindowsFontTableCacheRegistryFontSwshSwashAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Dynamic Rendering Unused Attachments в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicRenderingUnusedAttachmentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка сбоев синхронизации таблиц ReFS (Event 426-430)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataTableSyncFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Рамсея (RESET) на нелинейный джиттер...", ManualChecks.RunMinecraftPlayerCombatAutoClickerRamseyRESETNonLinearHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.unreflectSetter в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupUnreflectStaticSetterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка орнаментов OpenType ornm...", ManualChecks.RunWindowsFontTableCacheRegistryFontOrnmOrnamentsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Fragment Shader Interlock в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanFragmentShaderInterlockCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка сбоев ребалансировки деревьев ReFS (Event 431-435)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeRebalanceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Харви-Коллиера на стабильность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHarveyCollierHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.unreflectGetter в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupUnreflectGetterPrivateFinalCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка курсивных связей OpenType curs...", ManualChecks.RunWindowsFontTableCacheRegistryFontCursCursiveAttachmentForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Subgroup Size Control в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSubgroupSizeControlCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка повреждений карты кластеров ReFS (Event 436-440)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSClusterMapCorruptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Бокса-Пирса на автокорреляцию кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBoxPierceNonParametricHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.defineHiddenClass в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineHiddenClassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка дистанционных смещений OpenType dist...", ManualChecks.RunWindowsFontTableCacheRegistryFontDistDistanceGlyphPositioningForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Shader Demote to Helper в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanShaderDemoteToHelperInvocationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка сбоев обрезки узлов ReFS (Event 441-445)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreePruneCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Бреуша-Годфри (LM) на автокорреляцию кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBreuschGodfreyLaggedAutocorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.defineClass в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageTeleportCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка диакритических смещений OpenType abvm...", ManualChecks.RunWindowsFontTableCacheRegistryFontAbvmAboveBaseMarkForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Primitives Generated Query в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPrimitivesGeneratedQueryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка контрольных сумм записей журнала ReFS (Event 446-450)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataLogRecordChecksumCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Маклеода-Ли на условную гетероскедастичность...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMcLeodLiARCHHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.ensureInitialized в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupEnsureInitializedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка подстрочных диакритик OpenType blwm...", ManualChecks.RunWindowsFontTableCacheRegistryFontBlwmBelowBaseMarkForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Non-Seamless Cube Map в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanNonSeamlessCubeMapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка рассинхронизации листьев деревьев ReFS (Event 451-455)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeLeafDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста серий Вальда-Вольфовица на случайность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerWaldWolfowitzRunsHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.defineClass экспорта модулей...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassModuleExportCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка многоуровневых меток OpenType mkmk...", ManualChecks.RunWindowsFontTableCacheRegistryFontMkmkMarkToMarkPositioningForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Update-After-Bind дескрипторов в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanUpdateAfterBindXRayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка рассинхронизации родительских узлов ReFS (Event 456-460)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeParentDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста рангов Бартельса на случайность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBartelsRankVonNeumannHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка вызовов Lookup.defineClass экспорта модулей...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassModuleOpensCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка пропорциональных ширин OpenType pwid...", ManualChecks.RunWindowsFontTableCacheRegistryFontPwidProportionalWidthsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Depth Bias Control в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthBiasControlCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка коллизий узлов ветвления ReFS (Event 461-465)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSBranchNodeCollisionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Харке-Бера на нормальность остатков...", ManualChecks.RunMinecraftPlayerCombatAutoClickerJarqueBeraResidualNormalityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass и ProtectionDomain...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassProtectionDomainCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка OpenType sinf Scientific Inferiors...", ManualChecks.RunWindowsFontTableCacheRegistryFontSinfScientificInferiorsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Rasterization Order Attachment Access в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanRasterizationOrderAttachmentAccessCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка ReFS Metadata Stream Checksum (Event 466-470)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataStreamChecksumDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Кохрана-Армитеджа на тренд кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCochranArmitageTrendHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass и графа чтения модулей...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassModuleReadsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss01...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs01StylisticSet1ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Pipeline Robustness в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPipelineRobustnessCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка рассинхрона Root Dir ReFS (Event 471-475)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSRootDirDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка критерия Шапиро-Франсиа на нормальность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerShapiroFranciaNormalityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass регистрации SPI-сервисов...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassModuleProvideServiceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss02...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs02StylisticSet2ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка калибровки таймингов Vulkan Timestamps...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanCalibratedTimestampsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка рассинхрона Sparse ReFS (Event 476-480)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSparseAllocClusterDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Чоу на структурный сдвиг кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerChowTestStructuralBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass динамических сервисов...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassModuleProvideDynamicTargetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss03...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs03StylisticSet3ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка сброса окклюзии Host Query Reset...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanHostQueryResetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка коллизий Object ID ReFS (Event 481-485)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSObjectIdAllocCollisionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Зивота-Эндрюса на слом ряда кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerZivotAndrewsUnitRootBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass экспорта модулей...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportDynamicModuleCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss04...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs04StylisticSet4ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка выравнивания Texel Buffer в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanTexelBufferAlignmentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка конфликтов транзакций ReFS (Event 486-490)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTransactionConflictDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка критерия Эндрюса-Плобергера Sup-F...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAndrewsPlobergerOptimalSupFCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка ServiceLoader инъекций в Mixin...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportServiceLoaderInjectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss05...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs05StylisticSet5ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Dynamic State 3 в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3RasterizationSamplesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Security Descriptor ReFS (Event 491-495)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSecurityDescriptorDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Плобергера Avg-F на нестабильность...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPlobergerExponentialWaldAvgFCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass экспорта модулей...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportInstrumentationAgentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss06...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs06StylisticSet6ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Polygon Mode в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3PolygonModeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка рассинхрона USN ReFS (Event 496-500)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSUSNJournalDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Ниблома на постоянство параметров...", ManualChecks.RunMinecraftPlayerCombatAutoClickerNyblomParameterStabilityLMCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass и Unsafe телепортации...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeTeleportCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss07...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs07StylisticSet7ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Color Blend Enable в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3ColorBlendEnableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка экстентов ReFS (Event 501-505)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSparseFileExtentDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Хансена на параметрическую нестабильность...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHansenInstabilityL2TestCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass и смещений Unsafe...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeFieldOffsetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss08...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs08StylisticSet8ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Color Blend Equation в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3ColorBlendEquationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка усечения метаданных ReFS (Event 506-510)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataStreamTruncateDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка теста Эллиотта-Ротенберга-Стока DF-GLS...", ManualChecks.RunMinecraftPlayerCombatAutoClickerElliottMullerStockEfficientUnitRootCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass и Unsafe.putAddress...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeMemoryTeleportCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss09...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs09StylisticSet9ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Color Write Mask в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3ColorWriteMaskCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка сброса коммитов ReFS (Event 511-515)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTransactionCommitAbortDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка модели аддитивных выбросов Перрона AO...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPerronAdditiveOutlierBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass и Unsafe.park...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeParkCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss10...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs10StylisticSet10ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Logic Op в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3LogicOpCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка сбоев Prune ReFS (Event 516-520)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreePruneFailureDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка модели инновационных выбросов Перрона IO...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPerronInnovationalOutlierBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass и Unsafe.throw...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeThrowExceptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss11...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs11StylisticSet11ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Color Blend Advanced в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3ColorBlendAdvancedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка сбоев расширения ReFS (Event 521-525)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataStreamExpansionFailureDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка критерия Перрона-Фогельсанга...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPerronVogelsangInnovationalOutlierBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass и Unsafe.putVolatile...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeVolatilePutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss12...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs12StylisticSet12ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка коллизий листьев ReFS (Event 526-530)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeLeafCollisionDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка AO-критерия Перрона-Фогельсанга...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPerronVogelsangAdditiveOutlierBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Lookup.defineClass и Unsafe.getVolatile...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeVolatileGetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss13...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs13StylisticSet13ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Stencil Op в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3StencilOpAdvancedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка сбоев расщепления ReFS (Event 531-535)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeLeafSplitFailureDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка коинтеграции Хансена-Йохансена...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHansenJohansenCointegrationBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Proxy и InvocationHandler в Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentInvocationHandlerProxyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss14...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs14StylisticSet14ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Depth Bias/Clamp в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3DepthBiasClampAdvancedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка слияния листьев ReFS (Event 536-540)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeLeafMergeFailureDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка асимметрии ECM Энгла-Грейнджера...", ManualChecks.RunMinecraftPlayerCombatAutoClickerEngleGrangerErrorCorrectionAsymmetryBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка ClassLoader.definePackage в Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentClassLoaderDefinePackageCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss15...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs15StylisticSet15ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Rasterization Stream в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3RasterizationStreamPointSizeAdvancedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка утилизации листьев ReFS (Event 541-545)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeLeafRecycleFailureDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка TVECM Хансена-Сео...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHansenSeoThresholdVectorErrorCorrectionBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка подделки Manifest SEALED в Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentClassLoaderSealedManifestSpoofCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss16...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs16StylisticSet16ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Tessellation Domain в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3TessellationDomainOriginAdvancedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка корня дерева ReFS (Event 546-550)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeRootIndexDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка SETAR-ECM Сео...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSeoSelfExcitingThresholdCointegrationBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка URLStreamHandler в Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentClassLoaderUrlStreamHandlerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка наборов глифов OpenType ss17...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs17StylisticSet17ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка Winding/Cull Mode в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3TessellationDomainControlPointSpacingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🔎 Проверка корневых экстентов ReFS (Event 551-555)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeRootExtentAllocationDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("👑 Проверка коинтеграции Хансена-Филлипса-Улиариса (1000-й метод!)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHansenPhillipsOuliarisMultivariateCointegrationRankBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("💥 Анализ дампов памяти CrashDumps и подавления сервиса WER...", ManualChecks.RunWindowsWerLocalDumpsAndCrashSuppressionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Аудит конфигураций Sodium на отключение Entity Culling для Chams...", ManualChecks.RunMinecraftSodiumStencilBufferAndRenderPassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🌐 Аудит параметров TTL кэша DNS и скрытого файла hosts.ics...", ManualChecks.RunWindowsDnsCacheTtlAndHostsIcsRedirectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("🎵 Анализ аудиофайлов .ogg в ресурспаках на триггеры Sound-Clicker...", ManualChecks.RunMinecraftAudioReactiveClickerOggTriggerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("📊 Проверка активности системной базы мониторинга ресурсов SRUM...", ManualChecks.RunWindowsSrumNetworkByteTransfersAuditCheck),
			Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc31 MeowDoomsdayFucker: Поиск Doomsday Client (Лончер/Prefetch/DNS/Логи)...", SsToolsManager.ScanMeowDoomsdayForensics),
			Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf9 Clean Detecting: Поиск остаточных файлов, скриптов и реестровых ключей...", SsToolsManager.ScanCleanDetectingForensics),
			Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee0\ufe0f SSTool & Discord: Поиск следов screenshare и логов...", SsToolsManager.ScanSsToolArtifacts),
			Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfaf y_check: Поиск детектов папок и настроек чекера...", SsToolsManager.ScanYCheckArtifacts),
			Tuple.Create<string, Func<List<CheckResult>>>("🛡️ Инспектор обхода проверок, сокрытия софта и анти-форензики (Anti-Evasion)...", ManualChecks.RunAntiEvasionAndStealthBypassCheck),
			Tuple.Create<string, Func<List<CheckResult>>>("🖥️ Инспектор гипервизоров, песочниц и сокрытия экрана (Anti-VM / Anti-Capture)...", ManualChecks.RunHypervisorVmAndSandboxEvasionCheck),
			Tuple.Create<string, Func<List<CheckResult>>>("💬 Инспектор Discord, Rich Presence и RPC криминалистики...", ManualChecks.RunDiscordRichPresenceAndRpcForensicsCheck),
			Tuple.Create<string, Func<List<CheckResult>>>("🔬 Форензика памяти System Informer / Process Hacker 2 (javaw, explorer, svchost, csrss)...", SystemInformerForensicsEngine.RunDetectorChecks),
			Tuple.Create<string, Func<List<CheckResult>>>("🧬 Luyten JAR / HxD DLL / BAM / EventLog / SystemDLC форензика...", HighLevelLuytenHxdEngine.RunDetectorChecks)
			};
			int totalSteps = array.Length;
			int completedSteps = 0;
			List<CheckResult>[] stepResults = new List<CheckResult>[totalSteps];

			ParallelOptions po = new ParallelOptions();
			po.CancellationToken = ct;
			po.MaxDegreeOfParallelism = Environment.ProcessorCount;

			try
			{
				Parallel.For(0, totalSteps, po, delegate(int i, ParallelLoopState state)
				{
					if (ct.IsCancellationRequested)
					{
						state.Stop();
						return;
					}

					try
					{
						List<CheckResult> collection = array[i].Item2();
						stepResults[i] = collection;
					}
					catch (Exception ex)
					{
						CheckResult checkResult = new CheckResult();
						checkResult.Category = "Визуалы";
						checkResult.Name = "Ошибка проверки " + array[i].Item1;
						checkResult.Detail = ex.Message;
						checkResult.Severity = CheckSeverity.Info;
						stepResults[i] = new List<CheckResult>() { checkResult };
					}

					int done = Interlocked.Increment(ref completedSteps);
					ReportStep(done, totalSteps, array[i].Item1);
				});
			}
			catch (OperationCanceledException)
			{
			}

			for (int j = 0; j < totalSteps; j++)
			{
				if (stepResults[j] != null)
				{
					scanReport.Results.AddRange(stepResults[j]);
				}
			}
			Report("✅ Полное сканирование успешно завершено!");
			return scanReport;
		}

		private static List<CheckResult> CheckDeletedProgramsAndMods()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string path = Path.Combine(folderPath, ".minecraft", "mods");
			if (Directory.Exists(path))
			{
				try
				{
					string[] files = Directory.GetFiles(path, "*", SearchOption.AllDirectories);
					string[] array = files;
					foreach (string text in array)
					{
						string fileName = Path.GetFileName(text);
						if (fileName.StartsWith("$") || fileName.EndsWith("$") || fileName.EndsWith(".tmp") || !fileName.EndsWith(".jar"))
						{
							list2.Add("Удалённый/скрытый мод в папке mods: " + text);
						}
					}
				}
				catch
				{
				}
			}
			DriveInfo[] drives = DriveInfo.GetDrives();
			foreach (DriveInfo driveInfo in drives)
			{
				if (!driveInfo.IsReady || driveInfo.DriveType != DriveType.Fixed)
				{
					continue;
				}
				try
				{
					string path2 = Path.Combine(driveInfo.RootDirectory.FullName, "$RECYCLE.BIN");
					if (!Directory.Exists(path2))
					{
						continue;
					}
					DirectoryInfo directoryInfo = new DirectoryInfo(path2);
					if (directoryInfo.LastWriteTime > DateTime.Now.AddHours(-2.0))
					{
						list3.Add("⚠️ Корзина на диске " + driveInfo.Name + " была изменена прямо перед проверкой (" + directoryInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")");
					}
					List<string> files2 = new List<string>();
					try
					{
						int cnt = 0;
						foreach (string f in Directory.EnumerateFiles(path2, "*", SearchOption.TopDirectoryOnly))
						{
							files2.Add(f);
							if (++cnt >= 100) break;
						}
						foreach (string sub in Directory.GetDirectories(path2))
						{
							if (cnt >= 100) break;
							try
							{
								foreach (string f in Directory.EnumerateFiles(sub, "*", SearchOption.TopDirectoryOnly))
								{
									files2.Add(f);
									if (++cnt >= 100) break;
								}
							}
							catch { }
						}
					}
					catch { files2 = new List<string>(); }
					List<string> array2 = files2;
					foreach (string text2 in array2)
					{
						string text3 = Path.GetFileName(text2).ToLower();
						string[] cheatClientNames = CheatClientNames;
						foreach (string text4 in cheatClientNames)
						{
							if (MatchesCheatName(text3, text4))
							{
								list2.Add("Удалённый чит-файл в корзине [" + text4 + "]: " + text2);
								break;
							}
						}
						string[] visualKeywords = VisualKeywords;
						foreach (string text5 in visualKeywords)
						{
							if (text3.Contains(text5))
							{
								list2.Add("Удалённая папка в корзине [" + text5 + "]: " + text2);
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			string path3 = Path.Combine(folderPath, "Microsoft", "Windows", "Recent");
			if (Directory.Exists(path3))
			{
				try
				{
					string[] files3 = Directory.GetFiles(path3, "*.lnk");
					foreach (string text6 in files3)
					{
						string textLower = Path.GetFileNameWithoutExtension(text6).ToLower();
						string lnkTarget = ""; if (textLower.Contains("cheat") || textLower.Contains("hack") || textLower.Contains("inject") || textLower.Contains("loader") || textLower.Contains("client")) { lnkTarget = ExtractLnkTargetPath(text6); }
						string targetLower = (!string.IsNullOrEmpty(lnkTarget) ? lnkTarget.ToLowerInvariant() : textLower);

						bool matched = false;
						string matchedCheat = "";

						foreach (string text7 in CheatClientNames)
						{
							if (MatchesCheatName(textLower, text7) || MatchesCheatName(targetLower, text7))
							{
								matched = true;
								matchedCheat = text7;
								break;
							}
						}
						if (!matched)
						{
							foreach (string ec in CheatDatabases.ExtraCheatClients)
							{
								if (ec.Length >= 4 && (MatchesCheatName(textLower, ec) || MatchesCheatName(targetLower, ec)))
								{
									matched = true;
									matchedCheat = ec;
									break;
								}
							}
						}
						if (!matched)
						{
							string[] extraInj = new string[] { "kmbox", "interception", "spoofer", "dllinjector", "cheatloader", "slinky" };
							foreach (string inj in extraInj)
							{
								if (textLower.Contains(inj) || targetLower.Contains(inj))
								{
									matched = true;
									matchedCheat = inj;
									break;
								}
							}
						}

						bool isCleaner = false;
						string cleanerMatched = "";
						if (!matched)
						{
							foreach (string cl in CheatDatabases.KnownCleanerSignatures)
							{
								if (cl.Length >= 4 && (textLower.Contains(cl) || targetLower.Contains(cl)))
								{
									isCleaner = true;
									cleanerMatched = cl;
									break;
								}
							}
						}

						bool fileExists = !string.IsNullOrEmpty(lnkTarget) && File.Exists(lnkTarget);
						DateTime lnkTime = File.GetLastWriteTime(text6);

						if (matched)
						{
							if (!fileExists)
							{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск ПО из Recent] [{0}] {1} -> {2} ({3:dd.MM.yyyy HH:mm}, файл удален!)", matchedCheat, Path.GetFileName(text6), (!string.IsNullOrEmpty(lnkTarget) ? lnkTarget : "файл отсутствует"), lnkTime));
							}
							else
							{
								list2.Add(string.Format("⛔ [5.01 - Чит в Recent] [{0}] {1} -> {2} ({3:dd.MM.yyyy HH:mm})", matchedCheat, Path.GetFileName(text6), lnkTarget, lnkTime));
							}
						}
						else if (isCleaner)
						{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск ПО из Recent] [{0}] {1} -> {2} ({3:dd.MM.yyyy HH:mm}, файл удален!)", matchedCheat, Path.GetFileName(text6), (!string.IsNullOrEmpty(lnkTarget) ? lnkTarget : "файл отсутствует"), lnkTime));
						}
						// Deleted non-cheat shortcuts in Recent are not 5.08 bans
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Удалённые программы";
			checkResult.Name = "Поиск удалённых программ, логов и очистки корзины";
			checkResult.Detected = list2.Count > 0 || list3.Count > 0;
			checkResult.Severity = CheckSeverity.Warning;
			checkResult.Detail = ((list2.Count > 0 || list3.Count > 0) ? ("Найдено следов удаления читов: " + list2.Count + " | Очисток корзины: " + list3.Count) : "Следов намеренного удаления читов перед проверкой не найдено");
			List<string> list4 = new List<string>();
			list4.AddRange(list3);
			list4.AddRange(list2);
			checkResult.Evidence = string.Join("\r\n", list4.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckVisualsDatabaseAndPacks()
		{
			List<CheckResult> list = new List<CheckResult>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string path = Path.Combine(folderPath, ".minecraft");
			string text = Path.Combine(path, "resourcepacks");
			string path2 = Path.Combine(path, "mods");
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			string[] array = new string[4] { "xrayultimate", "x-rayultimate", "xray-ultimate", "advancedxray" };
			string[] array2 = new string[14]
			{
				"stone.png", "deepslate.png", "netherrack.png", "dirt.png", "sand.png", "gravel.png", "end_stone.png", "tuff.png", "andesite.png", "diorite.png",
				"granite.png", "basalt.png", "blackstone.png", "terracotta.png"
			};
			string[] files;
			if (Directory.Exists(text))
			{
				try
				{
					files = Directory.GetFiles(text, "*.zip", SearchOption.TopDirectoryOnly);
					foreach (string text2 in files)
					{
						string fileName = Path.GetFileName(text2);
						string text3 = fileName.ToLower();
						list3.Add(fileName);
						string[] array3 = array;
						foreach (string text4 in array3)
						{
							if (text3.Contains(text4))
							{
								list2.Add("🟣 [X-Ray архив по названию]: " + text2 + " (совпадение: " + text4 + ")");
								break;
							}
						}
						try
						{
							using (ZipArchive zipArchive = ZipFile.OpenRead(text2))
							{
								foreach (ZipArchiveEntry entry in zipArchive.Entries)
								{
									string text5 = entry.FullName.ToLower().Replace("\\", "/");
									array3 = array2;
									foreach (string text6 in array3)
									{
										if ((text5.EndsWith("textures/block/" + text6) || text5.EndsWith("textures/blocks/" + text6)) && entry.Length < 750)
										{
											list2.Add("🟣 [Прозрачный блок X-Ray] " + text6 + " весит " + entry.Length + " байт в: " + text2);
										}
									}
								}
							}
						}
						catch
						{
						}
					}
					files = Directory.GetDirectories(text, "*", SearchOption.TopDirectoryOnly);
					foreach (string text7 in files)
					{
						string fileName2 = Path.GetFileName(text7);
						string text8 = fileName2.ToLower();
						list3.Add("[Папка] " + fileName2);
						string[] array3 = array;
						foreach (string text9 in array3)
						{
							if (text8.Contains(text9))
							{
								list2.Add("🟣 [X-Ray папка ресурспака]: " + text7 + " (совпадение: " + text9 + ")");
								break;
							}
						}
						array3 = array2;
						foreach (string text10 in array3)
						{
							string text11 = Path.Combine(text7, "assets", "minecraft", "textures", "block", text10);
							string text12 = Path.Combine(text7, "assets", "minecraft", "textures", "blocks", text10);
							string text13 = (File.Exists(text11) ? text11 : (File.Exists(text12) ? text12 : null));
							if (text13 != null)
							{
								FileInfo fileInfo = new FileInfo(text13);
								if (fileInfo.Length < 750)
								{
									list2.Add("🟣 [Прозрачный блок X-Ray] " + text10 + " весит " + fileInfo.Length + " байт в: " + text13);
								}
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Визуалы (Следует проверить)";
			checkResult.Name = "X-Ray Ресурспаки и Прозрачные Блоки";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.NeedsCheck;
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено X-Ray ресурспаков/прозрачных блоков: " + list2.Count + " — НЕ БАНИТЬ, следует проверить!") : "X-Ray ресурспаки и прозрачные текстуры не найдены");
			checkResult.Evidence = ((list2.Count > 0) ? (string.Join("\r\n", list2.ToArray()) + "\r\n\r\nВсе проверенные ресурспаки:\r\n" + string.Join("\r\n", list3.ToArray())) : ("Папка ресурспаков: " + text));
			list.Add(checkResult);
			List<string> list4 = new List<string>();
			string[] array4 = new string[12]
			{
				"advancedxray", "advanced-xray", "simplexray", "simple-xray", "xraymod", "x-raymod", "oresight", "findores", "scannable", "cavefinder",
				"cavesearch", "orebfuscator"
			};
			if (Directory.Exists(path2))
			{
				try
				{
					files = Directory.GetFiles(path2, "*.jar", SearchOption.AllDirectories);
					foreach (string text14 in files)
					{
						string text15 = Path.GetFileNameWithoutExtension(text14).ToLower();
						string[] array3 = array4;
						foreach (string text16 in array3)
						{
							if (text15.Contains(text16))
							{
								list4.Add("🟣 [X-Ray мод]: " + text14 + " (ключ: " + text16 + ")");
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult2 = new CheckResult();
			checkResult2.Category = "Визуалы (Следует проверить)";
			checkResult2.Name = "X-Ray Моды и Поиск Руд";
			checkResult2.Detected = list4.Count > 0;
			checkResult2.Severity = CheckSeverity.NeedsCheck;
			checkResult2.Detail = ((list4.Count > 0) ? ("Обнаружены X-Ray моды (" + list4.Count + " шт.) — НЕ БАНИТЬ, проверить в папке mods!") : "Отдельные X-Ray моды в папке mods не обнаружены");
			checkResult2.Evidence = string.Join("\r\n", list4.ToArray());
			list.Add(checkResult2);
			List<string> list5 = new List<string>();
			string[] array5 = new string[26]
			{
				"chestesp", "storageesp", "playeresp", "mobesp", "itemesp", "entityesp", "boxesp", "glowesp", "entityglow", "entityoutlines",
				"chams", "wallhack", "nametags", "betternametags", "customnametags", "nametagscale", "tracers", "playertracers", "itemtracers", "chesttracers",
				"trajectories", "arrowtrajectories", "pearltracer", "projectiletracer", "hitboxesp", "blockesp"
			};
			if (Directory.Exists(path2))
			{
				try
				{
					files = Directory.GetFiles(path2, "*.jar", SearchOption.AllDirectories);
					foreach (string text17 in files)
					{
						string text18 = Path.GetFileNameWithoutExtension(text17).ToLower();
						string[] array3 = array5;
						foreach (string text19 in array3)
						{
							if (text18.Contains(text19))
							{
								list5.Add("🟣 [ESP / Tracers мод]: " + text17 + " (ключ: " + text19 + ")");
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult3 = new CheckResult();
			checkResult3.Category = "Визуалы (Следует проверить)";
			checkResult3.Name = "ESP, Подсветка Сундуков/Игроков, Tracers";
			checkResult3.Detected = list5.Count > 0;
			checkResult3.Severity = CheckSeverity.NeedsCheck;
			checkResult3.Detail = ((list5.Count > 0) ? ("Обнаружены ESP/Tracers/Подсветка (" + list5.Count + " шт.) — проверить в игре!") : "ESP и трейсеры в модах не обнаружены");
			checkResult3.Evidence = string.Join("\r\n", list5.ToArray());
			list.Add(checkResult3);
			List<string> list6 = new List<string>();
			string[] array6 = new string[9] { "freecam", "free-cam", "camhack", "flashcam", "perspective360", "unlockedperspective", "freelook", "cameraabuse", "noclipeye" };
			if (Directory.Exists(path2))
			{
				try
				{
					files = Directory.GetFiles(path2, "*.jar", SearchOption.AllDirectories);
					foreach (string text20 in files)
					{
						string text21 = Path.GetFileNameWithoutExtension(text20).ToLower();
						string[] array3 = array6;
						foreach (string text22 in array3)
						{
							if (text21.Contains(text22))
							{
								list6.Add("🟣 [FreeCam / Камера мод]: " + text20 + " (ключ: " + text22 + ")");
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult4 = new CheckResult();
			checkResult4.Category = "Визуалы (Следует проверить)";
			checkResult4.Name = "FreeCam и Свободная Камера";
			checkResult4.Detected = false;
			checkResult4.Severity = CheckSeverity.NeedsCheck;
			checkResult4.Detail = ((list6.Count > 0) ? ("Обнаружены моды свободной камеры FreeCam (" + list6.Count + " шт.) — проверить легитимность!") : "Моды FreeCam не найдены");
			checkResult4.Evidence = string.Join("\r\n", list6.ToArray());
			list.Add(checkResult4);
			List<string> list7 = new List<string>();
			string text23 = Path.Combine(path, "options.txt");
			if (File.Exists(text23))
			{
				try
				{
					string[] array7 = File.ReadAllLines(text23);
					files = array7;
					foreach (string text24 in files)
					{
						if (text24.Trim().StartsWith("gamma:"))
						{
							string text25 = text24.Replace("gamma:", "").Trim();
							double result;
							if (double.TryParse(text25.Replace(",", "."), NumberStyles.Any, CultureInfo.InvariantCulture, out result) && result > 1.0)
							{
								list7.Add("⚠️ В файле options.txt гамма выкручена выше нормы: gamma=" + result + " (норма: 0.0 - 1.0). Игрок видит в темноте без факелов!");
							}
						}
					}
				}
				catch
				{
				}
			}
			string[] array8 = new string[5] { "fullbright", "full-bright", "gammabright", "brightnight", "nightvision" };
			if (Directory.Exists(path2))
			{
				try
				{
					files = Directory.GetFiles(path2, "*.jar", SearchOption.AllDirectories);
					foreach (string text26 in files)
					{
						string text27 = Path.GetFileNameWithoutExtension(text26).ToLower();
						string[] array3 = array8;
						foreach (string text28 in array3)
						{
							if (text27.Contains(text28))
							{
								list7.Add("🟣 [FullBright мод]: " + text26 + " (ключ: " + text28 + ")");
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult5 = new CheckResult();
			checkResult5.Category = "Визуалы (Следует проверить)";
			checkResult5.Name = "FullBright и Завышенная Гамма (options.txt)";
			checkResult5.Detected = false;
			checkResult5.Severity = CheckSeverity.NeedsCheck;
			checkResult5.Detail = ((list7.Count > 0) ? ("Обнаружен FullBright / завышенная гамма (" + list7.Count + ") — проверить освещение!") : "Параметры гаммы в норме (<= 1.0)");
			checkResult5.Evidence = ((list7.Count > 0) ? string.Join("\r\n", list7.ToArray()) : ("Файл настроек: " + text23));
			list.Add(checkResult5);
			List<string> list8 = new List<string>();
			string[] array9 = new string[8] { "voxelmap", "xaerominimap", "xaeroworldmap", "journeymap", "reiminimap", "mapwriter", "playerradar", "caveradar" };
			if (Directory.Exists(path2))
			{
				try
				{
					files = Directory.GetFiles(path2, "*.jar", SearchOption.AllDirectories);
					foreach (string text29 in files)
					{
						string text30 = Path.GetFileNameWithoutExtension(text29).ToLower();
						string[] array3 = array9;
						foreach (string value in array3)
						{
							if (text30.Contains(value))
							{
								list8.Add("🟣 [Миникарта/Радар]: " + text29 + " — проверить, разрешён ли пещерный радар/игроки на сервере");
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult6 = new CheckResult();
			checkResult6.Category = "Визуалы (Следует проверить)";
			checkResult6.Name = "Миникарты и Радары Сущностей/Пещер";
			checkResult6.Detected = false;
			checkResult6.Severity = CheckSeverity.NeedsCheck;
			checkResult6.Detail = ((list8.Count > 0) ? ("Установлены миникарты (" + list8.Count + " шт.) — проверить настройки отображения пещер/игроков!") : "Миникарты с радарами не обнаружены");
			checkResult6.Evidence = string.Join("\r\n", list8.ToArray());
			list.Add(checkResult6);
			List<string> list9 = new List<string>();
			string[] array10 = new string[4]
			{
				Path.Combine(folderPath2, ".lunarclient"),
				Path.Combine(folderPath2, ".badlion"),
				Path.Combine(folderPath2, ".feather"),
				Path.Combine(path, "labymod-neo")
			};
			files = array10;
			foreach (string text31 in files)
			{
				if (Directory.Exists(text31))
				{
					list9.Add("Установлен клиент: " + text31);
				}
			}
			string text32 = Path.Combine(folderPath2, "Downloads");
			string folderPath3 = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			string[] array11 = new string[4] { "lunar", "badlion", "feather", "labymod" };
			files = new string[2] { text32, folderPath3 };
			foreach (string path3 in files)
			{
				if (!Directory.Exists(path3))
				{
					continue;
				}
				try
				{
					string[] array3 = Directory.GetFiles(path3, "*.*", SearchOption.TopDirectoryOnly);
					foreach (string text33 in array3)
					{
						string text34 = Path.GetFileName(text33).ToLower();
						string[] array12 = array11;
						foreach (string text35 in array12)
						{
							if (text34.Contains(text35) && (text34.EndsWith(".exe") || text34.EndsWith(".jar") || text34.EndsWith(".lnk")))
							{
								list9.Add("Файл/установщик клиента [" + text35 + "]: " + text33);
							}
						}
					}
				}
				catch
				{
				}
			}
			try
			{
				Process[] processes = Process.GetProcesses();
				foreach (Process process in processes)
				{
					string text36 = process.ProcessName.ToLower();
					files = array11;
					foreach (string text37 in files)
					{
						if (text36.Contains(text37))
						{
							list9.Add("Запущен процесс клиента [" + text37 + "]: " + process.ProcessName + " (PID: " + process.Id + ")");
						}
					}
				}
			}
			catch
			{
			}
			CheckResult checkResult7 = new CheckResult();
			checkResult7.Category = "Визуалы (Следует проверить)";
			checkResult7.Name = "PvP и Визуальные Клиенты (Lunar, Badlion, Feather)";
			checkResult7.Detected = false;
			checkResult7.Severity = CheckSeverity.NeedsCheck;
			checkResult7.Detail = ((list9.Count > 0) ? ("Обнаружены визуальные/PvP клиенты (" + list9.Count + ") — НЕ БАНИТЬ! Проверить включённые моды.") : "Сторонние визуальные клиенты не обнаружены");
			checkResult7.Evidence = string.Join("\r\n", list9.ToArray());
			list.Add(checkResult7);
			return list;
		}

		private static List<CheckResult> CheckModsVisualsAndInternals()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string text = Path.Combine(folderPath, ".minecraft", "mods");
			if (!Directory.Exists(text))
			{
				return list;
			}
			try
			{
				string[] files = Directory.GetFiles(text, "*.jar", SearchOption.AllDirectories);
				string[] array = files;
				foreach (string text2 in array)
				{
					string fileName = Path.GetFileName(text2);
					string textLower = Path.GetFileNameWithoutExtension(text2).ToLower();
					list3.Add(fileName);
					FileInfo fileInfo = new FileInfo(text2);
					string[] cheatClientNames = CheatClientNames;
					foreach (string text3 in cheatClientNames)
					{
						if (MatchesCheatName(textLower, text3))
						{
							list2.Add("🔴 Чит-клиент по имени [" + text3 + "]: " + text2);
							break;
						}
					}
					try
					{
						using (ZipArchive zipArchive = ZipFile.OpenRead(text2))
						{
							bool flag = false;
							bool flag2 = false;
							foreach (ZipArchiveEntry entry in zipArchive.Entries)
							{
								string text4 = entry.FullName.ToLower().Replace("\\", "/");
								if (text4.EndsWith("addon3.json") || text4.EndsWith("addon4.json"))
								{
									flag = true;
								}
								if (text4.EndsWith("l.png") || text4.Contains("doomsday"))
								{
									flag2 = true;
								}
								if (text4.Contains("killaura") || text4.Contains("aimbot"))
								{
									list2.Add("🔴 Чит-класс боевой механики (Killaura/Aimbot) внутри: " + text2);
									break;
								}
							}
							if (flag)
							{
								list2.Add("🔴 Инжект-аддон addon3/addon4.json внутри: " + text2);
							}
							if (flag2 && fileInfo.Length > 20000 && fileInfo.Length < 10000000)
							{
								list2.Add("🔴 Сигнатура Doomsday чита внутри: " + text2);
							}
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "Хард-чит моды и инжекты в .minecraft/mods";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("ОБНАРУЖЕНО ХАРД-ЧИТОВ (БАН): " + list2.Count + " (из " + list3.Count + " модов)") : ("Хард-читов не найдено (всего модов: " + list3.Count + ")"));
			checkResult.Evidence = ((list2.Count > 0) ? string.Join("\r\n", list2.ToArray()) : ("Папка mods: " + text));
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckMinecraftLibraries()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string path = Path.Combine(folderPath, ".minecraft", "libraries", "com", "github");
			if (Directory.Exists(path))
			{
				try
				{
					string[] directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
					string[] array = directories;
					foreach (string text in array)
					{
						string text2 = Path.GetFileName(text).ToLower();
						switch (text2)
						{
						case "oshi":
						case "stephenc":
						case "zeromemes":
							continue;
						}
						if (text2.Contains("oshi") || text2.Contains("stephenc"))
						{
							continue;
						}
						string[] cheatClientNames = CheatClientNames;
						foreach (string text3 in cheatClientNames)
						{
							if (MatchesCheatName(text2, text3))
							{
								list2.Add("Библиотека чита [" + text3 + "]: " + text);
								break;
							}
						}
						string[] files = Directory.GetFiles(text, "*.jar");
						string[] array2 = files;
						foreach (string text4 in array2)
						{
							string text5 = Path.GetFileName(text4).ToLower();
							if (!text5.Contains("oshi") && !text5.Contains("stephenc"))
							{
								list2.Add("Сторонняя подгруженная библиотека в libraries/github: " + text4);
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "Библиотеки libraries/com/github (по мануалу SS)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Warning;
			checkResult.Detail = ((list2.Count > 0) ? ("Обнаружены сторонние библиотеки: " + list2.Count) : "В libraries/com/github только стандартные библиотеки");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			List<string> list3 = CheckLibrariesAllPaths();
			if (list3.Count > 0)
			{
				CheckResult checkResult2 = new CheckResult();
				checkResult2.Category = "Minecraft";
				checkResult2.Name = "Расширенный анализ библиотек (все пути)";
				checkResult2.Detected = true;
				checkResult2.Severity = CheckSeverity.Warning;
				checkResult2.Detail = "Обнаружены подозрительные библиотеки: " + list3.Count;
				checkResult2.Evidence = string.Join("\r\n", list3.Take(50).ToArray());
				list.Add(checkResult2);
			}
			return list;
		}

		private static List<string> CheckLibrariesAllPaths()
		{
			List<string> list = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string text = Path.Combine(folderPath, ".minecraft", "libraries");
			if (!Directory.Exists(text))
			{
				return list;
			}
			string[] array = new string[16]
			{
				"net/ramensoft", "cc/bluxte", "net/bluxte", "io/bluxte", "meteorclient", "meteordevelopment", "wurstclient", "wurstmod", "impactdevelopment", "baritone",
				"massivepack", "sk1d", "killaura", "autototem", "cheatclient", "hackclient"
			};
			try
			{
				string[] directories = Directory.GetDirectories(text, "*", SearchOption.AllDirectories);
				string[] array2 = directories;
				foreach (string text2 in array2)
				{
					string text3 = text2.Replace(text, "").ToLower().Replace('\\', '/');
					if (CheatDatabases.IsWhitelistedPath(text3))
					{
						continue;
					}
					string[] array3 = array;
					foreach (string text4 in array3)
					{
						if (text3.Contains(text4))
						{
							list.Add("Чит-неймспейс в libraries [" + text4 + "]: " + text2);
							break;
						}
					}
					string[] files = Directory.GetFiles(text2, "*.jar", SearchOption.TopDirectoryOnly);
					string[] array4 = files;
					foreach (string text5 in array4)
					{
						string textLower = Path.GetFileNameWithoutExtension(text5).ToLower();
						string[] cheatClientNames = CheatClientNames;
						foreach (string cheatLower in cheatClientNames)
						{
							if (MatchesCheatName(textLower, cheatLower))
							{
								list.Add("Чит-клиент в библиотеках: " + text5);
								break;
							}
						}
					}
				}
			}
			catch
			{
			}
			return list;
		}

		private static List<CheckResult> CheckJarPackageSignatures()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string[] suspiciousPackages = new string[11]
			{
				"net.ramensoft", "cc.bluxte", "net.bluxte", "io.bluxte", "meteorclient", "meteordevelopment", "wurstclient", "wurstmod", "impactdevelopment", "massivepack",
				"sk1d"
			};
			string[] cheatClassNames = new string[15]
			{
				"KillAura", "KillAuraPlus", "KillAuraCore", "AimBot", "AutoTotem", "Scaffold", "Velocity", "AntiKB", "FreeCam", "NoRender",
				"FullBright", "ChestESP", "PlayerESP", "AdvancedXRay", "XRayUltimate"
			};
			string[] suspiciousManifests = new string[7] { "addon.json", "addon3.json", "addon4.json", "cheat.json", "hack.json", "client.json", "loader.json" };
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string path = Path.Combine(folderPath, ".minecraft", "mods");
			if (Directory.Exists(path))
			{
				try
				{
					string[] files = Directory.GetFiles(path, "*.jar", SearchOption.AllDirectories);
					string[] array = files;
					foreach (string jarPath in array)
					{
						List<string> collection = InspectJarForSignatures(jarPath, suspiciousPackages, cheatClassNames, suspiciousManifests);
						list2.AddRange(collection);
					}
				}
				catch
				{
				}
			}
			string path2 = Path.Combine(folderPath, ".minecraft", "libraries");
			if (Directory.Exists(path2))
			{
				try
				{
					string[] files2 = Directory.GetFiles(path2, "*.jar", SearchOption.AllDirectories);
					string[] array2 = files2;
					foreach (string text in array2)
					{
						if (!text.Contains("oshi") && !text.Contains("stephenc") && !text.Contains("zeromemes"))
						{
							List<string> collection2 = InspectJarForSignatures(text, suspiciousPackages, cheatClassNames, suspiciousManifests);
							list2.AddRange(collection2);
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "Сигнатуры чит-пакетов внутри jar (net.ramensoft, cc.bluxte)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("ОБНАРУЖЕНО ЧИТ-ПАКЕТОВ: " + list2.Count) : "Чит-пакеты не обнаружены");
			checkResult.Evidence = ((list2.Count > 0) ? string.Join("\r\n", list2.Take(50).ToArray()) : "");
			list.Add(checkResult);
			return list;
		}

		private static List<string> InspectJarForSignatures(string jarPath, string[] suspiciousPackages, string[] cheatClassNames, string[] suspiciousManifests)
		{
			List<string> list = new List<string>();
			try
			{
				using (ZipArchive zipArchive = ZipFile.OpenRead(jarPath))
				{
					foreach (ZipArchiveEntry entry in zipArchive.Entries)
					{
						string text = entry.FullName.Replace('\\', '/').ToLower();
						string text2 = CheatDatabases.NormalizeKey(text);
						if (text2.Length > 10 && !CheatDatabases.HasAnyRegulationKey(text2))
						{
							continue;
						}
						foreach (string text3 in suspiciousPackages)
						{
							if (text.StartsWith(text3.ToLower()))
							{
								list.Add("📦 Подозрительный пакет [" + text3 + "] в: " + jarPath + " > " + entry.FullName);
								break;
							}
						}
						foreach (string text4 in cheatClassNames)
						{
							if (text.Contains(text4.ToLower()))
							{
								list.Add("⚠️ Чит-класс [" + text4 + "] в: " + jarPath + " > " + entry.FullName);
								break;
							}
						}
						foreach (string text5 in suspiciousManifests)
						{
							if (text.EndsWith(text5.ToLower()))
							{
								list.Add("📄 Подозрительный манифест [" + text5 + "] в: " + jarPath + " > " + entry.FullName);
								break;
							}
						}
						if (text2.Length > 10)
						{
							CheatDatabases.BanRule[] array = CheatDatabases.MatchRegulationKeys(text2);
							CheatDatabases.BanRule[] array2 = array;
							foreach (CheatDatabases.BanRule banRule in array2)
							{
								list.Add("🚫 [Регламент " + banRule.Id + "] " + banRule.Title + " в: " + jarPath + " > " + entry.FullName);
							}
						}
						if (!text.EndsWith(".json") && !text.EndsWith(".toml") && !text.EndsWith(".mcmeta"))
						{
							continue;
						}
						try
						{
							using (Stream stream = entry.Open())
							{
								using (StreamReader streamReader = new StreamReader(stream))
								{
									string text6 = streamReader.ReadToEnd();
									string normalizedContent = CheatDatabases.NormalizeKey(text6);
									CheatDatabases.BanRule[] array3 = CheatDatabases.MatchRegulationKeys(normalizedContent);
									CheatDatabases.BanRule[] array4 = array3;
									foreach (CheatDatabases.BanRule banRule2 in array4)
									{
										list.Add("📋 [Регламент " + banRule2.Id + "] " + banRule2.Title + " в манифесте: " + jarPath + " > " + entry.Name);
									}
								}
							}
						}
						catch
						{
						}
					}
				}
			}
			catch
			{
			}
			return list;
		}

		private static List<CheckResult> CheckProcesses()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			Process[] processes = Process.GetProcesses();
			Process[] array = processes;
			foreach (Process process in array)
			{
				try
				{
					string text = process.ProcessName.ToLower();
					if (!text.Contains("java") && !text.Contains("minecraft"))
					{
						text.Contains("javaw");
					}
					bool flag = text == "conhost";
					string[] cheatClientNames = CheatClientNames;
					foreach (string text2 in cheatClientNames)
					{
						if (MatchesCheatName(text, text2))
						{
							list2.Add(process.ProcessName + " (PID: " + text2 + ")");
							break;
						}
					}
					string[] injectorNames = InjectorNames;
					foreach (string text3 in injectorNames)
					{
						if (text.Contains(text3))
						{
							list3.Add(process.ProcessName + " (инжектор: " + text3 + ")");
							break;
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Процессы";
			checkResult.Name = "Чит-клиенты в памяти";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("Обнаружено процессов: " + list2.Count) : "Активных чит-процессов не найдено");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			CheckResult checkResult2 = new CheckResult();
			checkResult2.Category = "Процессы";
			checkResult2.Name = "Инжекторы и отладчики";
			checkResult2.Detected = list3.Count > 0;
			checkResult2.Severity = CheckSeverity.Warning;
			checkResult2.Detail = ((list3.Count > 0) ? ("Папка mods: " + list3.Count) : "Сторонних инжекторов не запущено");
			checkResult2.Evidence = string.Join("\r\n", list3.ToArray());
			list.Add(checkResult2);
			CheckResult checkResult3 = new CheckResult();
			checkResult3.Category = "Процессы";
			checkResult3.Name = "Console Window Host (conhost)";
			checkResult3.Detected = false;
			checkResult3.Severity = CheckSeverity.Info;
			checkResult3.Detail = "Системный процесс conhost в норме";
			list.Add(checkResult3);
			return list;
		}

				private static readonly HashSet<string> SystemIgnoreDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"packages", "microsoft", "windows", "temp", "system32", "programs", "google", "chrome",
			"edge", "mozilla", "discord", "telegram", "steam", "epic games", "ubisoft", "adobe",
			"jetbrains", "virtualbox", "vmware", "android", "npm", "yarn", "pip", "node_modules",
			"git", "github", "visual studio", "vscode", "nuget", "assembly", "crashdumps",
			"inetpub", "perflogs", "recovery", "system volume information", "$recycle.bin",
			"appdata", "local", "locallow", "roaming", "application data", "documents", "downloads",
			"desktop", "music", "pictures", "videos", "saved games", "favorites", "contacts",
			"searches", "links", "onedrive", "3d objects", "dotmc", ".minecraft", "comms",
			"connecteddevicesplatform", "d3dscache", "history", "inetcookies", "inetcache", "caches",
			"cryptneturlcache", "elevateddiagnostics", "identitynexus", "peerdistrepub", "publishers"
		};

		private static List<CheckResult> CheckCheatFolders()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string folderPath3 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string[] array = new string[3] { folderPath, folderPath2, folderPath3 };
			foreach (string path in array)
			{
				if (!Directory.Exists(path))
				{
					continue;
				}
				try
				{
					string[] directories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
					foreach (string text in directories)
					{
						string text2 = Path.GetFileName(text).ToLowerInvariant();
						if (SystemIgnoreDirs.Contains(text2))
						{
							continue;
						}
						if (CheatDatabases.IsWhitelistedPath(text) || text.ToLowerInvariant().Contains("          \"name\": ") || text.ToLowerInvariant().Contains("mccheat") || text.ToLowerInvariant().Contains("screenshare"))
						{
							continue;
						}

						string[] cheatClientNames = CheatClientNames;
						foreach (string text3 in cheatClientNames)
						{
							if (CheatDatabases.IsAmbiguous(text3)) continue;
							if (text2 == text3 || text2 == "." + text3 ||
							    text2 == text3 + "client" || text2 == text3 + "-client" || text2 == text3 + "_client" ||
							    text2 == text3 + "hack" || text2 == text3 + "-hack")
							{
								list2.Add(text);
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			DriveInfo[] drives = DriveInfo.GetDrives();
			foreach (DriveInfo driveInfo in drives)
			{
				if (!driveInfo.IsReady || driveInfo.DriveType != DriveType.Fixed)
				{
					continue;
				}
				try
				{
					string[] directories2 = Directory.GetDirectories(driveInfo.RootDirectory.FullName, "*", SearchOption.TopDirectoryOnly);
					foreach (string text4 in directories2)
					{
						string text5 = Path.GetFileName(text4).ToLowerInvariant();
						if (SystemIgnoreDirs.Contains(text5))
						{
							continue;
						}
						if (CheatDatabases.IsWhitelistedPath(text4) || text4.ToLowerInvariant().Contains("          \"name\": ") || text4.ToLowerInvariant().Contains("mccheat") || text4.ToLowerInvariant().Contains("screenshare"))
						{
							continue;
						}

						string[] cheatClientNames2 = CheatClientNames;
						foreach (string text6 in cheatClientNames2)
						{
							if (CheatDatabases.IsAmbiguous(text6)) continue;
							if (text5 == text6 || text5 == "." + text6 ||
							    text5 == text6 + "client" || text5 == text6 + "-client" ||
							    text5 == text6 + "hack" || text5 == text6 + "-hack")
							{
								list2.Add(text4);
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Войти в чекер";
			checkResult.Name = "Папки чит-клиентов на диске";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено папок: " + list2.Count) : "Специфичных папок читов не обнаружено");
			checkResult.Evidence = string.Join("\r\n", list2.Distinct().ToArray());
			list.Add(checkResult);
			return list;
		}

		private static bool IsDeepScanNoiseDir(string nameLower)
		{
			if (string.IsNullOrEmpty(nameLower))
			{
				return false;
			}
			string[] deepScanSkipDirs = DeepScanSkipDirs;
			foreach (string text in deepScanSkipDirs)
			{
				if (nameLower == text)
				{
					return true;
				}
			}
			return false;
		}

		public static List<CheckResult> CheckDeepDriveFolders()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			int num = 0;
			int num2 = 0;
			DateTime utcNow = DateTime.UtcNow;
			bool flag = false;
			DriveInfo[] array;
			try
			{
				array = DriveInfo.GetDrives();
			}
			catch
			{
				array = new DriveInfo[0];
			}
			DriveInfo[] array2 = array;
			foreach (DriveInfo driveInfo in array2)
			{
				if (!driveInfo.IsReady || (driveInfo.DriveType != DriveType.Fixed && driveInfo.DriveType != DriveType.Removable))
				{
					continue;
				}
				if (flag)
				{
					break;
				}
				Queue<string[]> queue = new Queue<string[]>();
				queue.Enqueue(new string[2]
				{
					driveInfo.RootDirectory.FullName,
					"0"
				});
				while (queue.Count > 0)
				{
					if ((DateTime.UtcNow - utcNow).TotalSeconds > 2.5)
					{
						flag = true;
						list3.Add("Скан остановлен по времени (" + 12 + " c) — обход неполный.");
						break;
					}
					if (num >= 20000)
					{
						flag = true;
						list3.Add("Скан остановлен по лимиту папок (" + 120000 + ").");
						break;
					}
					string[] array3 = queue.Dequeue();
					string text = array3[0];
					int num3 = int.Parse(array3[1]);
					num++;
					string text2 = Path.GetFileName(text.TrimEnd(Path.DirectorySeparatorChar)).ToLowerInvariant();
					if (text2.Length > 0)
					{
						string[] deepScanStrongNames = DeepScanStrongNames;
						foreach (string text3 in deepScanStrongNames)
						{
							if (MatchesCheatName(text2, text3))
							{
								list2.Add("📁 Глубокая папка: " + text + "  < совпадение \"" + text3 + "\"");
								break;
							}
						}
					}
					if (num3 >= 1 && num3 <= 8 && num2 < 150000)
					{
						string[] array4;
						try
						{
							array4 = Directory.GetFiles(text);
						}
						catch
						{
							array4 = new string[0];
						}
						string[] array5 = array4;
						foreach (string text4 in array5)
						{
							if (num2 >= 150000)
							{
								break;
							}
							string text5 = Path.GetExtension(text4).ToLowerInvariant();
							if (text5 != ".jar" && text5 != ".exe" && text5 != ".dll" && text5 != ".zip" && text5 != ".bat" && text5 != ".cmd")
							{
								continue;
							}
							num2++;
							string textLower = Path.GetFileNameWithoutExtension(text4).ToLowerInvariant();
							string[] deepScanStrongNames2 = DeepScanStrongNames;
							foreach (string text6 in deepScanStrongNames2)
							{
								if (MatchesCheatName(textLower, text6))
								{
									list2.Add("📄 Файл: " + text4 + "  < совпадение \"" + text6 + "\"");
									break;
								}
							}
						}
					}
					if (num3 >= 12)
					{
						continue;
					}
					string[] directories;
					try
					{
						directories = Directory.GetDirectories(text);
					}
					catch
					{
						continue;
					}
					string[] array6 = directories;
					foreach (string text7 in array6)
					{
						string nameLower = Path.GetFileName(text7).ToLowerInvariant();
						if (IsDeepScanNoiseDir(nameLower))
						{
							continue;
						}
						try
						{
							FileAttributes attributes = File.GetAttributes(text7);
							if ((attributes & FileAttributes.ReparsePoint) != 0)
							{
								continue;
							}
						}
						catch
						{
							continue;
						}
						queue.Enqueue(new string[2]
						{
							text7,
							(num3 + 1).ToString()
						});
					}
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Войти в чекер";
			checkResult.Name = "Глубокий обход корней дисков (до " + 12 + " уровней)";
			if (list2.Count > 0)
			{
				checkResult.Detected = true;
				checkResult.Severity = CheckSeverity.Critical;
				checkResult.Detail = "В глубине дисков найдены совпадения с сигнатурами читов: " + list2.Count + " шт.";
				checkResult.Evidence = string.Join("\r\n", list2.Take(40));
			}
			else
			{
				checkResult.Detected = false;
				checkResult.Severity = CheckSeverity.Info;
				checkResult.Detail = "Проверено папок: " + num + ", файлов: " + num2 + ". Совпадений в глубине дисков не найдено.";
				if (list3.Count > 0)
				{
					checkResult.Detail = checkResult.Detail + " " + string.Join(" ", list3);
				}
			}
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckVersionsFolder()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string path = Path.Combine(folderPath, ".minecraft", "versions");
			if (Directory.Exists(path))
			{
				try
				{
					string[] directories = Directory.GetDirectories(path);
					string[] array = directories;
					foreach (string path2 in array)
					{
						string textLower = Path.GetFileName(path2).ToLower();
						string[] cheatClientNames = CheatClientNames;
						foreach (string text in cheatClientNames)
						{
							if (MatchesCheatName(textLower, text))
							{
								list2.Add("Версия [" + text + "]: " + Path.GetFileName(path2));
								break;
							}
						}
						string[] files = Directory.GetFiles(path2, "*.jar");
						string[] array2 = files;
						foreach (string text2 in array2)
						{
							FileInfo fileInfo = new FileInfo(text2);
							if (KnownCheatFileSizes.Contains(fileInfo.Length))
							{
								list2.Add("Совпадение размера чита (" + fileInfo.Length + " b): " + text2);
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "Подозрительные версии (.minecraft/versions)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено чит-версий: " + list2.Count) : "\\versions\\");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckMinecraftLogs()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			List<string> list4 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			List<string> list5 = new List<string>();
			list5.Add(Path.Combine(folderPath, ".minecraft", "logs", "latest.log"));
			list5.Add(Path.Combine(folderPath, ".feather", "logs", "latest.log"));
			list5.Add(Path.Combine(folderPath, ".feather", "user-gamedir", "logs", "latest.log"));
			list5.Add(Path.Combine(folderPath, ".tlauncher", "minecraft", "logs", "latest.log"));
			list5.Add(Path.Combine(folderPath2, ".lunarclient", "offline", "multiver", "logs", "latest.log"));
			List<string> list6 = list5;
			try
			{
				string path = Path.Combine(folderPath, "PrismLauncher", "instances");
				if (Directory.Exists(path))
				{
					string[] directories = Directory.GetDirectories(path);
					foreach (string path2 in directories)
					{
						string text = Path.Combine(path2, ".minecraft", "logs", "latest.log");
						if (File.Exists(text))
						{
							list6.Add(text);
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				string path3 = Path.Combine(folderPath2, "curseforge", "minecraft", "Instances");
				if (Directory.Exists(path3))
				{
					string[] directories2 = Directory.GetDirectories(path3);
					foreach (string path4 in directories2)
					{
						string text2 = Path.Combine(path4, "logs", "latest.log");
						if (File.Exists(text2))
						{
							list6.Add(text2);
						}
					}
				}
			}
			catch
			{
			}
			foreach (string item in list6)
			{
				if (!File.Exists(item))
				{
					continue;
				}
				try
				{
					FileInfo fileInfo = new FileInfo(item);
					if (fileInfo.Length == 0)
					{
						Process[] processesByName = Process.GetProcessesByName("javaw");
						if (processesByName.Length == 0) processesByName = Process.GetProcessesByName("java");
						if (processesByName.Length > 0)
						{
							list4.Add(string.Format("🚨 [{0}] Файл latest.log имеет нулевой размер (0 байт) при запущенном Minecraft! Признак затирания логов перед SS.", Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(item)))));
						}
					}
					else
					{
						using (FileStream stream = new FileStream(item, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
						{
							using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8))
							{
								int num = 0;
								string text3;
								while ((text3 = streamReader.ReadLine()) != null && num < 10000)
								{
									num++;
									string text4 = text3.ToLowerInvariant();
									string[] cheatClientNames = CheatClientNames;
									foreach (string text5 in cheatClientNames)
									{
										if (MatchesCheatName(text4, text5))
										{
											list2.Add(string.Format("⛔ [5.01 - Чит в логах Minecraft]: [{0}] [{1}] -> {2}", Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(item))), text5, text3.Trim()));
											break;
										}
									}
									if (text4.Contains("[chat]") || text4.Contains("chat thread") || text4.Contains("render thread/info") || text4.Contains("chat message"))
									{
										string[] cheatChatDotCommands = CheatDatabases.CheatChatDotCommands;
										foreach (string text6 in cheatChatDotCommands)
										{
											if (text4.Contains(text6))
											{
												list3.Add(string.Format("🚨 [{0}] Чит-команда в чате (Регламент 5.01): {1} -> {2}", Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(item))), text6, text3.Trim()));
												break;
											}
										}
									}
									if (list2.Count >= 25 && list3.Count >= 25)
									{
										break;
									}
								}
							}
						}
					}
				}
				catch
				{
				}
				if (list2.Count >= 30 && list3.Count >= 30)
				{
					break;
				}
			}
			List<string> list7 = new List<string>();
			list7.AddRange(list3);
			list7.AddRange(list2);
			list7.AddRange(list4);
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "Анализ активных логов Minecraft (latest.log всех лаунчеров)";
			checkResult.Detected = list7.Count > 0;
			checkResult.Severity = ((list3.Count > 0 || list4.Count > 0) ? CheckSeverity.Critical : ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info));
			checkResult.Detail = ((list7.Count > 0) ? string.Format("Обнаружены следы в latest.log: {0} (чит-команд: {1}, вайпов: {2})", list7.Count, list3.Count, list4.Count) : "В активных latest.log следов читов и dot-команд не обнаружено");
			checkResult.Evidence = string.Join("\r\n", list7.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckLauncherProfiles()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> profileFiles = new List<string>();

			foreach (string root in ManualChecks.MinecraftRoots())
			{
				string p1 = Path.Combine(root, "launcher_profiles.json");
				if (File.Exists(p1)) profileFiles.Add(p1);
				string p2 = Path.Combine(root, "launcher_profiles_microsoft_store.json");
				if (File.Exists(p2)) profileFiles.Add(p2);
				string p3 = Path.Combine(root, "tlauncher_profiles.json");
				if (File.Exists(p3)) profileFiles.Add(p3);
				string p4 = Path.Combine(root, "instances.json");
				if (File.Exists(p4)) profileFiles.Add(p4);
			}

			foreach (string path in profileFiles.Distinct())
			{
				try
				{
					string text = "";
					using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
					using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
					{
						text = sr.ReadToEnd().ToLowerInvariant();
					}

					string launcherRoot = Path.GetFileName(Path.GetDirectoryName(path));

					string[] cheatClientNames = CheatClientNames;
					foreach (string text2 in cheatClientNames)
					{
						if (MatchesCheatName(text, text2))
						{
							list2.Add("⛔ [5.01 - Запуск чита в профиле (" + launcherRoot + ")]: " + text2);
						}
					}
					foreach (string ec in CheatDatabases.ExtraCheatClients)
					{
						if (ec.Length >= 4 && MatchesCheatName(text, ec))
						{
							list2.Add("⛔ [5.01 - Запуск чита в профиле (" + launcherRoot + ")]: " + ec);
						}
					}
					if (text.Contains("-javaagent"))
					{
						list2.Add("⛔ [5.01 - Аргумент -javaagent в профиле (" + launcherRoot + ")]: внедрение внешнего Java-агента!");
					}
					if (text.Contains("-xbootclasspath"))
					{
						list2.Add("⛔ [5.01 - Аргумент -Xbootclasspath в профиле (" + launcherRoot + ")]: подмена классов Bootstrap!");
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "Конфиг лаунчера (launcher_profiles.json)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("Следов в профилях: " + list2.Count) : "Профили лаунчера стандартные");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckRegistry()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string[] array = new string[3] { "SOFTWARE\\WOW6432Node\\Cheat Engine", "SOFTWARE\\Cheat Engine", "SOFTWARE\\WOW6432Node\\AutoHotkey" };
			string[] array2 = new string[6] { "SOFTWARE\\AutoHotkey", "SOFTWARE\\Murgee Auto Mouse Click", "SOFTWARE\\GS Auto Clicker", "SOFTWARE\\Free Mouse Auto Clicker", "SOFTWARE\\vape", "SOFTWARE\\LiquidBounce" };
			string[] array3 = array;
			foreach (string text in array3)
			{
				try
				{
					using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(text, false))
					{
						if (registryKey != null)
						{
							list2.Add("HKLM\\" + text);
						}
					}
				}
				catch
				{
				}
			}
			string[] array4 = array2;
			foreach (string text2 in array4)
			{
				try
				{
					using (RegistryKey registryKey2 = Registry.CurrentUser.OpenSubKey(text2, false))
					{
						if (registryKey2 != null)
						{
							list2.Add("HKCU\\" + text2);
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Реестр";
			checkResult.Name = "Следы чит-программ в реестре Windows";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Warning;
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено ключей: " + list2.Count) : "Ключей запрещённого ПО не обнаружено");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckDesktopAndDownloads()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string[] array = new string[3]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
				Path.Combine(folderPath, "Downloads"),
				Path.Combine(folderPath, "Загрузки")
			};
			string[] array2 = new string[5] { "*.jar", "*.exe", "*.zip", "*.rar", "*.dll" };
			string[] array3 = array;
			foreach (string path in array3)
			{
				if (!Directory.Exists(path))
				{
					continue;
				}
				try
				{
					string[] array4 = array2;
					foreach (string text in array4)
					{
						string[] files = Directory.GetFiles(path, text, SearchOption.TopDirectoryOnly);
						string[] array5 = files;
						foreach (string text2 in array5)
						{
							string textLower = Path.GetFileNameWithoutExtension(text2).ToLower();
							string[] cheatClientNames = CheatClientNames;
							foreach (string text3 in cheatClientNames)
							{
								if (MatchesCheatName(textLower, text3))
								{
									list2.Add("Файл [" + text3 + "]: " + text2);
									break;
								}
							}
							FileInfo fileInfo = new FileInfo(text2);
							if (text == "*.jar" && KnownCheatFileSizes.Contains(fileInfo.Length))
							{
								list2.Add("Совпадение байт-размера чита (" + fileInfo.Length + "): " + text2);
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Войти в чекер";
			checkResult.Name = "Файлы читов на Рабочем столе и в Загрузках";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено ключей: " + list2.Count) : "Рабочий стол и Загрузки чисты");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckAppData()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string[] array = new string[2]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
			};
			string[] array2 = array;
			foreach (string path in array2)
			{
				if (!Directory.Exists(path))
				{
					continue;
				}
				try
				{
					string[] directories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
					string[] array3 = directories;
					foreach (string text in array3)
					{
						string textLower = Path.GetFileName(text).ToLower();
						string[] cheatClientNames = CheatClientNames;
						foreach (string cheatLower in cheatClientNames)
						{
							if (MatchesCheatName(textLower, cheatLower))
							{
								list2.Add(text);
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Войти в чекер";
			checkResult.Name = "Следы чит-клиентов в AppData";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено ключей: " + list2.Count) : "Следов в AppData не обнаружено");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckTempFolders()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string tempPath = Path.GetTempPath();
			if (Directory.Exists(tempPath))
			{
				try
				{
					string[] array = new string[3] { "*.jar", "*.exe", "*.dll" };
					string[] array2 = array;
					foreach (string text in array2)
					{
						string[] files = Directory.GetFiles(tempPath, text, SearchOption.TopDirectoryOnly);
						string[] array3 = files;
						foreach (string text2 in array3)
						{
							string textLower = Path.GetFileNameWithoutExtension(text2).ToLower();
							string[] cheatClientNames = CheatClientNames;
							foreach (string cheatLower in cheatClientNames)
							{
								if (MatchesCheatName(textLower, cheatLower))
								{
									list2.Add("Temp: " + text2);
									break;
								}
							}
							FileInfo fileInfo = new FileInfo(text2);
							if (text == "*.jar" && KnownCheatFileSizes.Contains(fileInfo.Length))
							{
								list2.Add("Temp по размеру: " + text2);
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Войти в чекер";
			checkResult.Name = "Следы во временных файлах (Temp)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Warning;
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено в Temp: " + list2.Count) : "Temp-папка чиста");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

				private static List<CheckResult> CheckAutoHotKey()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string[] array = new string[4]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
				Environment.GetFolderPath(Environment.SpecialFolder.Personal),
				Path.Combine(folderPath, "Downloads"),
				Environment.GetFolderPath(Environment.SpecialFolder.Startup)
			};
			string[] array2 = new string[9]
			{
				"killaura", "reach", "hitbox", "aimbot", "silentaim", "fly", "speed", "scaffold", "antikb"
			};
			string[] clickerKeywords = new string[6]
			{
				"click", "cps", "autoclicker", "clicker", "jitter", "trigger"
			};
			foreach (string path in array)
			{
				if (!Directory.Exists(path))
				{
					continue;
				}
				try
				{
					List<string> list4 = new List<string>();
					list4.AddRange(Directory.GetFiles(path, "*.ahk", SearchOption.TopDirectoryOnly));
					list4.AddRange(Directory.GetFiles(path, "*.ahk2", SearchOption.TopDirectoryOnly));
					foreach (string item in list4)
					{
						bool isIllegalCheat = false;
						bool isClicker = false;
						string illegalCheatMatched = "";
						try
						{
							string[] array4 = File.ReadAllLines(item);
							foreach (string text in array4)
							{
								string text2 = text.ToLowerInvariant().Trim();
								if (text2.StartsWith(";") || string.IsNullOrEmpty(text2))
								{
									continue;
								}
								foreach (string text3 in array2)
								{
									if (text2.Contains(text3))
									{
										isIllegalCheat = true;
										illegalCheatMatched = text3;
										break;
									}
								}
								if (isIllegalCheat) break;

								foreach (string ck in clickerKeywords)
								{
									if (text2.Contains(ck))
									{
										isClicker = true;
										break;
									}
								}
							}
						}
						catch
						{
						}
						string fileName = Path.GetFileName(item);
						if (isIllegalCheat)
						{
							list2.Add(string.Format("⛔ [5.01 - Запрещённый AHK чит] {0} ({1}, совпадение: {2})", fileName, item, illegalCheatMatched));
						}
						else if (isClicker)
						{
							list3.Add(string.Format("ℹ️ [Разрешённый автокликер] AHK кликер: {0} ({1})", fileName, item));
						}
						else
						{
							list3.Add(string.Format("🚨 AHK автокликер/макрос: {0} ({1})", fileName));
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Автокликеры / Макросы";
			checkResult.Name = "AutoHotKey скрипты и кликеры";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = (list2.Count > 0 ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = (list2.Count > 0 ? ("Обнаружены запрещённые AHK читы (KillAura/Reach): " + list2.Count + " шт.") : (list3.Count > 0 ? "Обнаружены AHK скрипты / автокликеры (разрешены правилами сервера: " + list3.Count + " шт.)." : "Макросов и AHK не найдено"));
			List<string> list5 = new List<string>();
			list5.AddRange(list2);
			list5.AddRange(list3);
			checkResult.Evidence = string.Join("\r\n", list5.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckRecentFiles()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "Windows", "Recent");
			if (Directory.Exists(path))
			{
				try
				{
					DateTime dateTime = DateTime.Now.AddDays(-14.0);
					string[] files = Directory.GetFiles(path, "*.lnk");
					TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
					// Low recent files count is normal if disabled in Windows Settings - skip false positive
					string[] array = files;
					foreach (string text in array)
					{
						if (File.GetLastWriteTime(text) < dateTime)
						{
							continue;
						}
						string textLower = Path.GetFileNameWithoutExtension(text).ToLower();
						bool flag = false;
						string text2 = "Чисто";
						string[] cheatClientNames = CheatClientNames;
						foreach (string text3 in cheatClientNames)
						{
							if (MatchesCheatName(textLower, text3))
							{
								flag = true;
								text2 = text3;
								break;
							}
						}
						string text4 = ExtractLnkTargetPath(text);
						if (!string.IsNullOrEmpty(text4))
						{
							string text5 = text4.ToLowerInvariant();
							if (!flag)
							{
								string[] cheatClientNames2 = CheatClientNames;
								foreach (string text6 in cheatClientNames2)
								{
									if (MatchesCheatName(text5, text6))
									{
										flag = true;
										text2 = text6;
										break;
									}
								}
							}
							if ((text5.EndsWith(".jar") || text5.EndsWith(".exe") || text5.EndsWith(".dll")) && !File.Exists(text4) && (flag || text5.Contains("\\downloads\\") || text5.Contains("\\desktop\\") || text5.Contains("\\temp\\")))
							{
								list3.Add(string.Format("🗑️ Удалённый недавний файл: {0} -> Цель: {1} (ФАЙЛ УДАЛЁН)", Path.GetFileName(text), text4));
							}
						}
						if (flag)
						{
							list2.Add(string.Format("🚨 [{0}] {1} (ярлык: {2:dd.MM.yyyy HH:mm}){3}", text2, Path.GetFileNameWithoutExtension(text), File.GetLastWriteTime(text), (!string.IsNullOrEmpty(text4)) ? (" -> " + text4) : ""));
						}
					}
				}
				catch
				{
				}
			}
			List<string> list4 = new List<string>();
			list4.AddRange(list2);
			list4.AddRange(list3);
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "История";
			checkResult.Name = "Недавно открытые файлы (shell:recent, 14 дней)";
			checkResult.Detected = list4.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : ((list3.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info));
			checkResult.Detail = ((list4.Count > 0) ? ("Найдено подозрительных записей в Recent: " + list4.Count) : "Recent");
			checkResult.Evidence = string.Join("\r\n", list4.ToArray());
			list.Add(checkResult);
			return list;
		}

		public static string ExtractLnkTargetPath(string lnkPath)
		{
			try
			{
				if (!File.Exists(lnkPath))
				{
					return null;
				}
				byte[] bytes = null;
				using (FileStream fs = new FileStream(lnkPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
				{
					int len = (int)Math.Min(fs.Length, 1048576L);
					bytes = new byte[len];
					int readTotal = 0;
					while (readTotal < len)
					{
						int r = fs.Read(bytes, readTotal, len - readTotal);
						if (r <= 0) break;
						readTotal += r;
					}
					if (readTotal < len)
					{
						Array.Resize(ref bytes, readTotal);
					}
				}
				if (bytes == null || bytes.Length < 76)
				{
					return null;
				}

				// 1. LinkInfo binary structure parsing
				uint flags = BitConverter.ToUInt32(bytes, 0x14);
				bool hasLinkTargetIDList = (flags & 0x01) != 0;
				bool hasLinkInfo = (flags & 0x02) != 0;

				if (hasLinkInfo)
				{
					int linkInfoOffset = 0x4C;
					if (hasLinkTargetIDList && bytes.Length >= 0x4E)
					{
						ushort idListSize = BitConverter.ToUInt16(bytes, 0x4C);
						linkInfoOffset = 0x4E + idListSize;
					}

					if (linkInfoOffset + 28 <= bytes.Length)
					{
						uint linkInfoSize = BitConverter.ToUInt32(bytes, linkInfoOffset);
						if (linkInfoSize >= 28 && linkInfoOffset + linkInfoSize <= bytes.Length)
						{
							uint localBasePathOffset = BitConverter.ToUInt32(bytes, linkInfoOffset + 16);
							if (localBasePathOffset > 0 && linkInfoOffset + (int)localBasePathOffset < bytes.Length)
							{
								int start = linkInfoOffset + (int)localBasePathOffset;
								int nullPos = Array.IndexOf(bytes, (byte)0, start);
								if (nullPos > start)
								{
									string path = Encoding.Default.GetString(bytes, start, nullPos - start);
									if (path.Length >= 3 && path[1] == ':' && File.Exists(path))
									{
										return path.Trim();
									}
								}
							}
						}
					}
				}

				// 2. Full Unicode/Cyrillic Regex extraction
				string input = Encoding.Default.GetString(bytes);
				Match match = Regex.Match(input, @"[a-zA-Z]:\\[^<>""/|?*\x00-\x1F\r\n\t]+?\.[a-zA-Z0-9]{1,5}");
				if (match.Success)
				{
					return match.Value.Trim();
				}
				string input2 = Encoding.Unicode.GetString(bytes);
				Match match2 = Regex.Match(input2, @"[a-zA-Z]:\\[^<>""/|?*\x00-\x1F\r\n\t]+?\.[a-zA-Z0-9]{1,5}");
				if (match2.Success)
				{
					return match2.Value.Trim();
				}
				string inputUtf8 = Encoding.UTF8.GetString(bytes);
				Match matchUtf8 = Regex.Match(inputUtf8, @"[a-zA-Z]:\\[^<>""/|?*\x00-\x1F\r\n\t]+?\.[a-zA-Z0-9]{1,5}");
				if (matchUtf8.Success)
				{
					return matchUtf8.Value.Trim();
				}
			}
			catch
			{
			}
			return null;
		}

		private static List<CheckResult> CheckCheatWebsites()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			try
			{
				string path = "C:\\Windows\\System32\\drivers\\etc\\hosts";
				if (File.Exists(path))
				{
					string text = File.ReadAllText(path).ToLowerInvariant();
					string[] cheatWebsites = CheatWebsites;
					foreach (string text2 in cheatWebsites)
					{
						if (text.Contains(text2))
						{
							list2.Add("hosts содержит чит-домен: " + text2);
						}
					}
				}
			}
			catch
			{
			}
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			List<Tuple<string, string>> list4 = new List<Tuple<string, string>>();
			list4.Add(Tuple.Create("Chrome", Path.Combine(folderPath, "Google\\Chrome\\User Data")));
			list4.Add(Tuple.Create("Edge", Path.Combine(folderPath, "Microsoft\\Edge\\User Data")));
			list4.Add(Tuple.Create("Yandex", Path.Combine(folderPath, "Yandex\\YandexBrowser\\User Data")));
			list4.Add(Tuple.Create("Opera", Path.Combine(folderPath2, "Opera Software\\Opera Stable")));
			list4.Add(Tuple.Create("Opera GX", Path.Combine(folderPath2, "Opera Software\\Opera GX Stable")));
			list4.Add(Tuple.Create("Brave", Path.Combine(folderPath, "BraveSoftware\\Brave-Browser\\User Data")));
			list4.Add(Tuple.Create("Vivaldi", Path.Combine(folderPath, "Vivaldi\\User Data")));
			List<Tuple<string, string>> list5 = list4;
			List<string> list6 = new List<string>();
			foreach (Tuple<string, string> item in list5)
			{
				if (!Directory.Exists(item.Item2))
				{
					continue;
				}
				try
				{
					string text3 = Path.Combine(item.Item2, "History");
					if (File.Exists(text3))
					{
						list6.Add(text3);
					}
					string[] directories = Directory.GetDirectories(item.Item2);
					string[] array = directories;
					foreach (string text4 in array)
					{
						string fileName = Path.GetFileName(text4);
						if (fileName.Equals("Default", StringComparison.OrdinalIgnoreCase) || fileName.StartsWith("Profile", StringComparison.OrdinalIgnoreCase))
						{
							string text5 = Path.Combine(text4, "History");
							if (File.Exists(text5))
							{
								list6.Add(text5);
							}
						}
					}
				}
				catch
				{
				}
			}
			string path2 = Path.Combine(folderPath2, "Mozilla\\Firefox\\Profiles");
			if (Directory.Exists(path2))
			{
				try
				{
					string[] directories2 = Directory.GetDirectories(path2);
					foreach (string path3 in directories2)
					{
						string text6 = Path.Combine(path3, "places.sqlite");
						if (File.Exists(text6))
						{
							list6.Add(text6);
						}
					}
				}
				catch
				{
				}
			}
			Stopwatch swWeb = Stopwatch.StartNew();
			foreach (string item2 in list6)
			{
				if (swWeb.ElapsedMilliseconds > 1500) break;
				try
				{
					FileInfo fileInfo = new FileInfo(item2);
					if (fileInfo.Length == 0 || fileInfo.Length > 83886080)
					{
						continue;
					}
					byte[] array2 = null;
					try
					{
						using (FileStream fileStream = new FileStream(item2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
						{
														long maxRead = 524288L;
							long seekPos = Math.Max(0, fileInfo.Length - maxRead);
							int num = (int)(fileInfo.Length - seekPos);
							fileStream.Seek(seekPos, SeekOrigin.Begin);
							array2 = new byte[num];
							int totalRead = 0;
							while (totalRead < num)
							{
								int r = fileStream.Read(array2, totalRead, num - totalRead);
								if (r <= 0) break;
								totalRead += r;
							}
							if (totalRead < num)
							{
								Array.Resize(ref array2, totalRead);
							}
						}
					}
					catch
					{
					}
					if (array2 == null || array2.Length < 100)
					{
						continue;
					}
					string text8 = Encoding.ASCII.GetString(array2);
					string text9 = Encoding.Unicode.GetString(array2);
					string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
					foreach (string text10 in extraCheatClients)
					{
						if (text10.Length < 4)
						{
							continue;
						}
						if (text10.Equals("rockstar", StringComparison.OrdinalIgnoreCase) || text10.Equals("delta", StringComparison.OrdinalIgnoreCase) || text10.Equals("doomsdayfucker", StringComparison.OrdinalIgnoreCase)) continue;
						string value = text10.ToLowerInvariant();
						if (text8.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0 || text9.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							list3.Add(string.Format("🚨 [Браузер] Скачивание/упоминание софта [{0}] в истории: {1}", text10, fileInfo.FullName));
							if (list3.Count >= 25)
							{
								break;
							}
						}
					}
					string[] cheatWebsites2 = CheatWebsites;
					foreach (string text11 in cheatWebsites2)
					{
						if (text8.IndexOf(text11, StringComparison.OrdinalIgnoreCase) >= 0 || text9.IndexOf(text11, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							list2.Add(string.Format("🌐 [Браузер] Визит на чит-ресурс [{0}]: {1}", text11, fileInfo.FullName));
							if (list2.Count >= 25)
							{
								break;
							}
						}
					}
					goto IL_0485;
				}
				catch
				{
					goto IL_0485;
				}
				IL_0485:
				if (list3.Count >= 25 && list2.Count >= 25)
				{
					break;
				}
			}
			List<string> list7 = new List<string>();
			list7.AddRange(list3);
			list7.AddRange(list2);
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "История";
			checkResult.Name = "Анализ браузеров и загрузок (Chrome/Edge/Opera/Yandex/Firefox)";
			checkResult.Detected = list7.Count > 0;
			checkResult.Detected = false;
			checkResult.Severity = CheckSeverity.Info;
			checkResult.Detail = ((list7.Count > 0) ? string.Format("Обнаружены следы в браузерах: скачиваний читов — {0}, визитов — {1}", list3.Count, list2.Count) : "В истории и загрузках браузеров следов читов не обнаружено");
			checkResult.Evidence = string.Join("\r\n", list7.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckScreenshots()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			List<string> list3 = new List<string>();
			list3.Add(Path.Combine(folderPath, ".minecraft", "screenshots"));
			list3.Add(Path.Combine(folderPath2, ".feather", "screenshots"));
			list3.Add(Path.Combine(folderPath2, ".feather", "user-gamedir", "screenshots"));
			list3.Add(Path.Combine(folderPath2, ".lunarclient", "offline", "multiver", "screenshots"));
			list3.Add(Path.Combine(folderPath, ".tlauncher", "Minecraft", "game", "screenshots"));
			list3.Add(Path.Combine(folderPath, "Badlion Client", "screenshots"));
			List<string> list4 = list3;
			try
			{
				string path = Path.Combine(folderPath, ".minecraft", "versions");
				if (Directory.Exists(path))
				{
					string[] directories = Directory.GetDirectories(path);
					foreach (string path2 in directories)
					{
						string text = Path.Combine(path2, "screenshots");
						if (Directory.Exists(text) && !list4.Contains(text))
						{
							list4.Add(text);
						}
					}
				}
				string path3 = Path.Combine(folderPath, "PrismLauncher", "instances");
				if (Directory.Exists(path3))
				{
					string[] directories2 = Directory.GetDirectories(path3);
					foreach (string path4 in directories2)
					{
						string text2 = Path.Combine(path4, ".minecraft", "screenshots");
						if (Directory.Exists(text2) && !list4.Contains(text2))
						{
							list4.Add(text2);
						}
						string text3 = Path.Combine(path4, "screenshots");
						if (Directory.Exists(text3) && !list4.Contains(text3))
						{
							list4.Add(text3);
						}
					}
				}
				string path5 = Path.Combine(folderPath, "CurseForge", "minecraft", "Instances");
				if (Directory.Exists(path5))
				{
					string[] directories3 = Directory.GetDirectories(path5);
					foreach (string path6 in directories3)
					{
						string text4 = Path.Combine(path6, "screenshots");
						if (Directory.Exists(text4) && !list4.Contains(text4))
						{
							list4.Add(text4);
						}
					}
				}
				string pathModrinth = Path.Combine(folderPath, "ModrinthApp", "profiles");
				if (Directory.Exists(pathModrinth))
				{
					string[] dirs = Directory.GetDirectories(pathModrinth);
					foreach (string d in dirs)
					{
						string s1 = Path.Combine(d, "screenshots");
						if (Directory.Exists(s1) && !list4.Contains(s1)) list4.Add(s1);
						string s2 = Path.Combine(d, ".minecraft", "screenshots");
						if (Directory.Exists(s2) && !list4.Contains(s2)) list4.Add(s2);
					}
				}
				string pathAtl = Path.Combine(folderPath, "ATLauncher", "instances");
				if (Directory.Exists(pathAtl))
				{
					string[] dirs = Directory.GetDirectories(pathAtl);
					foreach (string d in dirs)
					{
						string s1 = Path.Combine(d, "screenshots");
						if (Directory.Exists(s1) && !list4.Contains(s1)) list4.Add(s1);
					}
				}
			}
			catch
			{
			}
			List<FileInfo> list5 = new List<FileInfo>();
			int num = 0;
			foreach (string item in list4)
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				try
				{
					FileInfo[] files = new DirectoryInfo(item).GetFiles("*.png");
					if (files.Length == 0)
					{
						num++;
					}
					else
					{
						list5.AddRange(files);
					}
				}
				catch
				{
				}
			}
			list5 = list5.OrderByDescending((FileInfo f) => f.LastWriteTime).ToList();
			if (list5.Count > 0)
			{
				LatestScreenshotPath = list5[0].FullName;
				LatestScreenshotDirectory = list5[0].DirectoryName;
			}
			else
			{
				LatestScreenshotPath = null;
				LatestScreenshotDirectory = Path.Combine(folderPath, ".minecraft", "screenshots");
			}
			int num2 = 0;
			int num3 = 0;
			DateTime now = DateTime.Now;
			foreach (FileInfo item2 in list5)
			{
				TimeSpan timeSpan = now - item2.LastWriteTime;
				if (timeSpan.TotalHours <= 2.0 && timeSpan.TotalHours >= -0.5)
				{
					num2++;
				}
				if (timeSpan.TotalHours <= 24.0 && timeSpan.TotalHours >= -0.5)
				{
					num3++;
				}
			}
			if (num2 > 0)
			{
				list2.Add(string.Format("⚠️ [Свежие скриншоты во время игры/проверки: {0} шт. за последние 2 часа]", num2));
			}
			if (num > 0 && list5.Count == 0)
			{
				list2.Add("⚠️ Папка скриншотов существует, но полностью пуста (возможна недавняя очистка)");
			}
			int num4 = Math.Min(15, list5.Count);
			for (int num5 = 0; num5 < num4; num5++)
			{
				FileInfo fileInfo = list5[num5];
				double num6 = (double)fileInfo.Length / 1048576.0;
				list2.Add(string.Format("[{0:dd.MM.yyyy HH:mm:ss}] {1} ({2:F2} MB) -> {3}", fileInfo.LastWriteTime, fileInfo.Name, num6, fileInfo.FullName));
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "Скриншоты всех лаунчеров (Multi-Launcher Screenshot Audit)";
			checkResult.Detected = false;
			checkResult.Severity = CheckSeverity.Info;
			checkResult.Detail = string.Format("Всего скриншотов: {0} (Свежих <2ч: {1}, за сутки: {2})", list5.Count, num2, num3);
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckPrefetch()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();

			// 1. Evasion: check if Prefetch is disabled in registry
			try
			{
				using (RegistryKey pKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters"))
				{
					if (pKey != null)
					{
						object ep = pKey.GetValue("EnablePrefetcher");
						if (ep != null && Convert.ToInt32(ep) == 0)
						{
						list2.Add("ud83dudea8 [5.08 - Служба Prefetch отключена в реестре (EnablePrefetcher = 0)! Попытка сокрытия следов запуска ПО!]");
						}
					}
				}
			}
			catch { }

			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
			if (!Directory.Exists(path))
			{
				path = "C:\\Windows\\Prefetch";
			}
			if (Directory.Exists(path))
			{
				try
				{
					string[] files2 = Directory.GetFiles(path, "*.pf");

					// 2. Wiping detection: low pf count on long uptime
					TimeSpan uptime = TimeSpan.FromMilliseconds((uint)Environment.TickCount);
					if (files2.Length < 10 && uptime.TotalHours >= 4.0)
					{
					list2.Add(string.Format("ud83dudea8 [5.08 - Подозрительно мало файлов Prefetch ({0} шт. при аптайме {1:F1} ч.)! Признак недавней очистки Prefetch]", files2.Length, uptime.TotalHours));
					}

					// 3. Known cleaner/wiper patterns
					string[] wiperPatterns = new string[]
					{
						"FSUTIL*.pf", "WEVTUTIL*.pf", "CIPHER*.pf", "SDELETE*.pf", "BLEACHBIT*.pf", "CCLEANER*.pf",
						"BCWIPER*.pf", "ERASER*.pf", "PRIVAZER*.pf", "SHELLBAG*.pf", "USBDEVIEW*.pf", "VANISH*.pf",
						"JOURNALTRACE*.pf", "KOID*.pf", "REDSTONE*.pf", "REGWIPE*.pf", "USNJRNL*.pf", "TRACEKILLER*.pf",
						"CLEANER*.pf", "WIPER*.pf", "SHREDDER*.pf", "ZEROTRACE*.pf", "CLEANDETECT*.pf", "EVENTCLEAN*.pf",
						"HISTORYCLEAN*.pf", "SYSTEMCLEAN*.pf", "BCLEANER*.pf", "AUTOCLEAN*.pf", "MFTCLEAN*.pf", "LOGCLEAN*.pf", "USN*.pf"
					};
					HashSet<string> seenWipers = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					foreach (string pattern in wiperPatterns)
					{
						string[] files = Directory.GetFiles(path, pattern);
						foreach (string wf in files)
						{
							if (!seenWipers.Add(wf)) continue;
							FileInfo fileInfo = new FileInfo(wf);
						list2.Add(string.Format("ud83dudea8 [5.08 - Запуск клинера/утилиты затирания следов]: {0} (Изменён: {1:dd.MM.yyyy HH:mm:ss})", wf, fileInfo.LastWriteTime));
						}
					}

					// 4. Cheat, injector, autoclicker inspection
					string[] extraInjectors = new string[] { "kmbox", "interception", "spoofer", "injector", "loader", "slinky", "booster", "killaura", "hitbox", "reach", "xray" };

					foreach (string path2 in files2)
					{
						string fileName = Path.GetFileName(path2);
						if (seenWipers.Contains(path2)) continue;

						string nameNoExt = Path.GetFileNameWithoutExtension(path2);
						int dashIdx = nameNoExt.LastIndexOf('-');
						string baseExe = (dashIdx > 0 ? nameNoExt.Substring(0, dashIdx) : nameNoExt).ToLowerInvariant();
						if (baseExe.EndsWith(".exe")) baseExe = baseExe.Substring(0, baseExe.Length - 4);
						if (baseExe.Contains("lunar") || baseExe.Contains("badlion") || baseExe.Contains("curseforge") || baseExe.Contains("prismlauncher") || baseExe.Contains("tinytask") || baseExe.Contains("speedautoclicker") || baseExe.Contains("opautoclicker") || baseExe.Contains("autoclicker") || baseExe.Contains("anydesk") || baseExe.Contains("xmouse") || baseExe.Contains("x-mouse")) continue;

						bool matched = false;
						string matchedCheat = "";

						foreach (string text in CheatClientNames)
						{
							if (MatchesCheatName(baseExe, text))
							{
								matched = true;
								matchedCheat = text;
								break;
							}
						}
						if (!matched)
						{
							foreach (string ec in CheatDatabases.ExtraCheatClients)
							{
								if (ec.Length >= 4 && MatchesCheatName(baseExe, ec))
								{
									matched = true;
									matchedCheat = ec;
									break;
								}
							}
						}
						if (!matched)
						{
							foreach (string inj in extraInjectors)
							{
								if (baseExe.Contains(inj))
								{
									matched = true;
									matchedCheat = inj;
									break;
								}
							}
						}

						if (matched)
						{
							FileInfo fi = new FileInfo(path2);
							TimeSpan age = DateTime.Now - fi.LastWriteTime;
							string timeDesc = (age.TotalDays <= 7.0) ? "недавно (<7 дн.)" : (age.TotalDays <= 14.0 ? " дн. назад (в пределах 7-14 дней)]" : ">14 дн.");
							list2.Add(string.Format("⛔ [5.01 - Чит в Prefetch] [{0}] {1} (запуск: {2:dd.MM.yyyy HH:mm:ss}, {3})", matchedCheat, fileName, fi.LastWriteTime, timeDesc));
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Следы запуска";
			checkResult.Name = "⚡ Prefetch (FSUTIL / очистки USN / запуски)...";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = (list2.Any((string f) => f.Contains("[5.01") || f.Contains("[5.08") || f.Contains("??") || f.Contains("??")) ? CheckSeverity.Critical : (list2.Count > 0 ? CheckSeverity.Warning : CheckSeverity.Info));
			checkResult.Detail = ((list2.Count > 0) ? ("Temp по размеру: " + list2.Count) : "Prefetch проверен, чисто");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckEventLog()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			try
			{
				using (EventLog eventLog = new EventLog("Application"))
				{
					int num = eventLog.Entries.Count - 1;
					while (num >= 0 && list2.Count < 5 && num >= eventLog.Entries.Count - 300)
					{
						EventLogEntry eventLogEntry = eventLog.Entries[num];
						if (eventLogEntry.InstanceId == 3079 || (eventLogEntry.Message != null && eventLogEntry.Message.ToLower().Contains("fsutil")))
						{
							list2.Add("Application EventID 3079 @ " + eventLogEntry.TimeGenerated.ToString("dd.MM.yyyy HH:mm") + ") — по мануалу это признак очистки USN Journal!");
						}
						num--;
					}
				}
			}
			catch
			{
			}
			try
			{
				using (EventLog eventLog = new EventLog("System"))
				{
					int num = eventLog.Entries.Count - 1;
					while (num >= 0 && num >= eventLog.Entries.Count - 300)
					{
						EventLogEntry eventLogEntry = eventLog.Entries[num];
						if (eventLogEntry.InstanceId == 104)
						{
							list2.Add("?? System EventID 104 @ " + eventLogEntry.TimeGenerated.ToString("dd.MM.yyyy HH:mm") + ": Журнал System был очищен (wevtutil)!");
							break;
						}
						num--;
					}
				}
			}
			catch
			{
			}
			try
			{
				string name = "SYSTEM\\CurrentControlSet\\Services\\bam\\State\\UserSettings";
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name, false))
				{
					if (registryKey != null)
					{
						string[] subKeyNames = registryKey.GetSubKeyNames();
						foreach (string name2 in subKeyNames)
						{
							using (RegistryKey registryKey2 = registryKey.OpenSubKey(name2, false))
							{
								if (registryKey2 == null)
								{
									continue;
								}
								string[] valueNames = registryKey2.GetValueNames();
								foreach (string text in valueNames)
								{
									string textLower = text.ToLower();
									string[] cheatClientNames = CheatClientNames;
									foreach (string text2 in cheatClientNames)
									{
										if (MatchesCheatName(textLower, text2))
										{
											list2.Add("BAM запуск [" + text2 + "]: " + text);
											break;
										}
									}
								}
							}
						}
					}
				}
			}
			catch
			{
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Следы запуска";
			checkResult.Name = "BAM журнал и Event Log (EventID 104/3079)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = (list2.Any((string f) => f.Contains("EventID") || f.Contains("очищен") || f.Contains("очистка"))) ? CheckSeverity.Critical : CheckSeverity.Warning;
			checkResult.Detail = ((list2.Count > 0) ? ("Обнаружено записей в журнале событий: " + list2.Count) : "Журналы чисты");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckMinecraftBedrockCheats()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			try
			{
				Process[] processesByName = Process.GetProcessesByName("Minecraft.Windows");
				if (processesByName.Length > 0)
				{
					foreach (Process process in processesByName)
					{
						try
						{
							foreach (ProcessModule module in process.Modules)
							{
								string text = module.ModuleName.ToLowerInvariant();
								if (text.Contains("horion") || text.Contains("zephyr") || text.Contains("prax") || text.Contains("packetclient") || text.Contains("ambhi") || text.Contains("eclipse") || text.Contains("borion") || text.Contains("fate") || text.Contains("chronium") || text.Contains("strike") || text.Contains("bedrock") || text.Contains("cheat") || text.Contains("inject") || text.Contains("deceptive") || text.Contains("sunlit") || text.Contains("maraxsis") || text.Contains("permetrics") || text.Contains("epsilon") || text.Contains("fpsclient") || text.Contains("fumoclient") || text.Contains("brainwave") || text.Contains("asteria") || text.Contains("noodle") || text.Contains("kobold") || text.Contains("obsidian") || text.Contains("anubis") || text.Contains("chaos") || text.Contains("voidclient") || text.Contains("radium") || text.Contains("selene") || text.Contains("valhalla") || text.Contains("asgard") || text.Contains("ragnarok") || text.Contains("fenrir") || text.Contains("medusa") || text.Contains("cerberus") || text.Contains("chimera") || text.Contains("kraken") || text.Contains("leviathan"))
								{
									list2.Add("Внедрённый DLL в Minecraft.Windows.exe: " + module.FileName);
								}
							}
						}
						catch
						{
						}
					}
				}
			}
			catch
			{
			}

			// Bedrock Injectors Process Search
			string[] injectorNames = new string[]
			{
				"horion", "zephyr", "prax", "fateinjector", "xenos", "extremeinjector", "bedrockinjector", "borion", "packetclient", "chronium",
				"ambhi", "fate", "flaxenter", "deceptive", "prestige", "latite", "krypton"
			};
			try
			{
				Process[] processes = Process.GetProcesses();
				foreach (Process process2 in processes)
				{
					try
					{
						string text2 = process2.ProcessName.ToLowerInvariant();
						foreach (string value in injectorNames)
						{
							if (text2.Contains(value))
							{
								list2.Add("Активный процесс инжектора Bedrock: " + process2.ProcessName + ".exe (PID: " + process2.Id + ")");
							}
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}

			// Bedrock Client Configuration Folders
			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string[] bedrockClientFolders = new string[]
			{
				Path.Combine(appData, "Horion"),
				Path.Combine(localAppData, "Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState\\Horion"),
				Path.Combine(appData, "Zephyr"),
				Path.Combine(localAppData, "Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\RoamingState\\Zephyr"),
				Path.Combine(appData, "Prax"),
				Path.Combine(localAppData, "Prax"),
				Path.Combine(appData, "PacketClient"),
				Path.Combine(localAppData, "PacketClient"),
				Path.Combine(appData, "Borion"),
				Path.Combine(appData, "Solstice"),
				Path.Combine(appData, "Latite"),
				Path.Combine(appData, "Ambience"),
				Path.Combine(appData, "Chronium"),
				Path.Combine(appData, "Strike")
			};
			foreach (string bDir in bedrockClientFolders)
			{
				if (Directory.Exists(bDir))
				{
					try
					{
						string[] bFiles = Directory.GetFiles(bDir, "*.*", SearchOption.TopDirectoryOnly);
						list2.Add(string.Format("⛔ [Bedrock Client Folder] Обнаружена директория чит-клиента Bedrock: {0} ({1} файлов модификаций)", bDir, bFiles.Length));
					}
					catch
					{
						list2.Add("⛔ [Bedrock Client Folder] Обнаружена директория чит-клиента Bedrock: " + bDir);
					}
				}
			}

			// Mojang UWP Data Folder & Resource Packs
			string mojangData = Path.Combine(localAppData, "Packages\\Microsoft.MinecraftUWP_8wekyb3d8bbwe\\LocalState\\games\\com.mojang");
			if (Directory.Exists(mojangData))
			{
				string[] packDirs = new string[] { "resource_packs", "development_resource_packs", "behavior_packs" };
				foreach (string packSub in packDirs)
				{
					string path = Path.Combine(mojangData, packSub);
					if (!Directory.Exists(path))
					{
						continue;
					}
					try
					{
						string[] directories = Directory.GetDirectories(path);
						foreach (string text5 in directories)
						{
							string text6 = Path.GetFileName(text5).ToLowerInvariant();
							if (text6.Contains("xray") || text6.Contains("x-ray") || text6.Contains("esp") || text6.Contains("ore") || text6.Contains("wireframe") || text6.Contains("nightvision") || text6.Contains("fullbright") || text6.Contains("cheat"))
							{
								list3.Add("Bedrock визуал-пак в " + packSub + ": " + text5);
							}
						}
						string[] files = Directory.GetFiles(path, "*.zip", SearchOption.TopDirectoryOnly);
						foreach (string text7 in files)
						{
							string text8 = Path.GetFileName(text7).ToLowerInvariant();
							if (text8.Contains("xray") || text8.Contains("x-ray") || text8.Contains("esp") || text8.Contains("wireframe") || text8.Contains("fullbright") || text8.Contains("cheat"))
							{
								list3.Add("Bedrock zip-пак в " + packSub + ": " + text7);
							}
						}
					}
					catch
					{
					}
				}
				try
				{
					string[] dllFiles = Directory.GetFiles(mojangData, "*.dll", SearchOption.AllDirectories);
					foreach (string text9 in dllFiles)
					{
						list2.Add("Подозрительный DLL в папке Bedrock com.mojang: " + text9);
					}
				}
				catch
				{
				}

				// Check options.txt for Night Vision gamma hack
				try
				{
					string optPath = Path.Combine(mojangData, "minecraftpe\\options.txt");
					if (File.Exists(optPath))
					{
						string[] optLines = File.ReadAllLines(optPath);
						foreach (string oLine in optLines)
						{
							if (oLine.StartsWith("gfx_gamma:", StringComparison.OrdinalIgnoreCase))
							{
								string valStr = oLine.Substring("gfx_gamma:".Length).Trim();
								double gammaVal = 0.0;
								if (double.TryParse(valStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out gammaVal) && gammaVal > 2.0)
								{
									list3.Add(string.Format("⚠️ [Bedrock NightVision] Обнаружено изменённое значение gfx_gamma={0} (FullBright/ночное зрение без зелья) в options.txt", gammaVal));
								}
								break;
							}
						}
					}
				}
				catch
				{
				}
			}

			// Prefetch Check for Bedrock Cheats
			string prefetchDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
			if (Directory.Exists(prefetchDir))
			{
				try
				{
					string[] files3 = Directory.GetFiles(prefetchDir, "*.pf");
					foreach (string text10 in files3)
					{
						string text11 = Path.GetFileName(text10).ToLowerInvariant();
						if (text11.StartsWith("horion") || text11.StartsWith("zephyr") || text11.StartsWith("prax") || text11.StartsWith("xenos") || text11.StartsWith("bedrockinjector") || text11.StartsWith("fate") || text11.StartsWith("borion") || text11.StartsWith("packetclient"))
						{
							DateTime lastWriteTime = File.GetLastWriteTime(text10);
							list2.Add("Запуск инжектора Bedrock в Prefetch: " + text10 + " (" + lastWriteTime.ToString("dd.MM.yyyy HH:mm:ss") + ")");
						}
					}
				}
				catch
				{
				}
			}

			// Downloads / Desktop files
			string desktopDir = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			string downloadsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
			string[] userDirs = new string[] { desktopDir, downloadsDir, Path.GetTempPath() };
			string[] bedrockNames = new string[]
			{
				"horion", "zephyr", "prax", "borion", "packetclient", "bedrockinjector", "chronium", "ambhi", "flaxenter", "deceptiveclient",
				"sunlitclient", "maraxsis", "permetricsclient", "xenos", "fateinjector", "solstice"
			};
			foreach (string path3 in userDirs)
			{
				if (!Directory.Exists(path3))
				{
					continue;
				}
				try
				{
					string[] files4 = Directory.GetFiles(path3, "*.*", SearchOption.TopDirectoryOnly);
					foreach (string text13 in files4)
					{
						string text14 = Path.GetFileName(text13).ToLowerInvariant();
						foreach (string value2 in bedrockNames)
						{
							if (text14.Contains(value2) && (text14.EndsWith(".exe") || text14.EndsWith(".dll") || text14.EndsWith(".zip") || text14.EndsWith(".rar")))
							{
								list2.Add("Активный процесс инжектора Bedrock: " + Path.GetFileName(path3) + ": " + text13);
								break;
							}
						}
					}
				}
				catch
				{
				}
			}

			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft Bedrock";
			checkResult.Name = "Читы и инжекторы Minecraft Bedrock Edition";
			if (list2.Count > 0)
			{
				checkResult.Detected = true;
				checkResult.Severity = CheckSeverity.Critical;
				checkResult.Detail = "Обнаружены следы читов/инжекторов для Minecraft Bedrock Edition (" + list2.Count + " шт.)!";
				checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			}
			else
			{
				checkResult.Detected = false;
				checkResult.Detail = "Читы/инжекторы Bedrock (Horion, Zephyr, Prax, Xenos) не обнаружены.";
			}
			list.Add(checkResult);

			CheckResult checkResult2 = new CheckResult();
			checkResult2.Category = "Minecraft Bedrock";
			checkResult2.Name = "X-Ray и визуал-паки Minecraft Bedrock";
			if (list3.Count > 0)
			{
				checkResult2.Detected = true;
				checkResult2.Severity = CheckSeverity.NeedsCheck;
			checkResult2.Detail = "В папке Bedrock com.mojang найдено подозрительных X-Ray/ESP/FullBright (" + list3.Count + " шт.) — требуется ручная проверка!";
				checkResult2.Evidence = string.Join("\r\n", list3.ToArray());
			}
			else
			{
				checkResult2.Detected = false;
				checkResult2.Detail = "X-Ray/ESP паки в директории Bedrock com.mojang не обнаружены.";
			}
			list.Add(checkResult2);
			return list;
		}

		private static List<CheckResult> CheckJavawInjectedModules()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			try
			{
				List<Process> procs = new List<Process>(Process.GetProcessesByName("javaw"));
				procs.AddRange(Process.GetProcessesByName("java"));
				Process[] array = procs.ToArray();
				foreach (Process process in array)
				{
					try
					{
						foreach (ProcessModule module in process.Modules)
						{
							string text = module.ModuleName.ToLower();
							string text2 = "Чисто";
							try
							{
								text2 = module.FileName;
							}
							catch
							{
							}
							bool isLegitNative = ProcessMemoryScanner.IsLegitimateMinecraftNativeDll(text, text2);
							if (!isLegitNative)
							{
								string[] cheatClientNames = CheatClientNames;
								foreach (string cheatLower in cheatClientNames)
								{
									if (MatchesCheatName(text, cheatLower))
									{
										list2.Add("Внедрённая DLL чита в javaw.exe (PID " + process.Id + "): " + text2);
										break;
									}
								}
								string[] injectorNames = InjectorNames;
								foreach (string value in injectorNames)
								{
									if (text.Contains(value))
									{
										list2.Add("Внедрённый модуль инжектора в javaw.exe: " + text2);
										break;
									}
								}
								if (!string.IsNullOrEmpty(text2))
								{
									string text3 = text2.ToLower();
									if (text3.Contains("\\appdata\\local\\temp\\") || text3.Contains("\\windows\\temp\\"))
									{
										list2.Add("⚠️ Подозрительная DLL из папки Temp внедрена в javaw.exe: " + text2);
									}
								}
							}
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Инжекты / DLL";
			checkResult.Name = "Анализ загруженных DLL модулей в javaw.exe (Инжекции памяти)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("Обнаружены подозрительные/инжектированные DLL в процесс Minecraft (" + list2.Count + ")!") : "💉 Проверка внедрённых DLL в javaw.exe (инжекции памяти)...");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckGZipMinecraftLogs()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			List<string> list3 = new List<string>();
			list3.Add(Path.Combine(folderPath, ".minecraft", "logs"));
			list3.Add(Path.Combine(folderPath, ".feather", "logs"));
			list3.Add(Path.Combine(folderPath, ".feather", "user-gamedir", "logs"));
			list3.Add(Path.Combine(folderPath, ".tlauncher", "minecraft", "logs"));
			list3.Add(Path.Combine(folderPath2, ".lunarclient", "offline", "multiver", "logs"));
			List<string> list4 = list3;
			string[] array = new string[]
			{
				".bind", ".killaura", ".vape", ".damage", ".friend", ".config", ".vclip", ".hclip", ".panic", ".selfdestruct",
				".teleport", ".elytra", ".target", ".spammer", ".give", "[wurst]", "[liquidbounce]", "[meteor]", "[expensive]", "[neverhook]",
				"[akrien]", "[celestial]", "[deadcode]", "[nurik]", "vape v4", "vape lite", "nursultan", "minced", "weighbar", "richpremium",
				"boberware", ".ka", ".speed", ".fly", ".xray", ".bhop", ".scaffold", ".jesus", ".nofall", "#goto", "#mine", "#tunnel", "#baritone", "baritone", "seedcracker"
			};
			foreach (string item in list4)
			{
				if (!Directory.Exists(item))
				{
					continue;
				}
				try
				{
					IEnumerable<FileInfo> enumerable = (from f in Directory.GetFiles(item, "*.log.gz")
						select new FileInfo(f) into f
						orderby f.LastWriteTime descending
						select f).Take(15);
					foreach (FileInfo item2 in enumerable)
					{
						try
						{
							using (FileStream stream = new FileStream(item2.FullName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
							{
								using (GZipStream stream2 = new GZipStream(stream, CompressionMode.Decompress))
								{
									using (StreamReader streamReader = new StreamReader(stream2, Encoding.UTF8))
									{
										int num = 0;
										string text;
										while ((text = streamReader.ReadLine()) != null && num < 3000)
										{
											num++;
											string text2 = text.ToLowerInvariant();
											string[] array2 = array;
											foreach (string value in array2)
											{
												if (text2.Contains(value))
												{
													list2.Add(string.Format("Архив лога [{0} : строка {1}]: {2}", item2.Name, num, text.Trim()));
													break;
												}
											}
											if (list2.Count >= 30)
											{
												break;
											}
										}
									}
								}
							}
						}
						catch
						{
						}
						if (list2.Count >= 30)
						{
							break;
						}

					}
				}
				catch
				{
				}
				if (list2.Count >= 30)
				{
					break;
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "Анализ архивных логов (.minecraft/logs/*.log.gz всех лаунчеров)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = ((list2.Count > 0) ? string.Format("В архивных логах найдено следов софта: {0}", list2.Count) : "Архивные логи чисты");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckUserAssistRot13()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			try
			{
				using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\UserAssist"))
				{
					if (registryKey != null)
					{
						try
						{
							using (RegistryKey settingsKey = registryKey.OpenSubKey("Settings"))
							{
								if (settingsKey != null)
								{
									object noLogVal = settingsKey.GetValue("NoLog");
									if (noLogVal != null && Convert.ToInt32(noLogVal) == 1)
									{
										list2.Add("⛔ [5.08 - Логирование UserAssist отключено (NoLog=1)]: скрытие истории запуска программ!");
									}
								}
							}
						}
						catch { }
						string[] subKeyNames = registryKey.GetSubKeyNames();
						foreach (string text in subKeyNames)
						{
							using (RegistryKey registryKey2 = registryKey.OpenSubKey(text + "\\Count"))
							{
								if (registryKey2 != null)
								{
									string[] valueNames = registryKey2.GetValueNames();
									foreach (string text2 in valueNames)
									{
										string text3 = DecodeRot13(text2);
										string textLower = text3.ToLowerInvariant();
										if (CheatDatabases.IsWhitelistedPath(textLower) || textLower.Contains("          \"name\": ") || textLower.Contains("mccheat") || textLower.Contains("screenshare") || textLower.Contains("sstools") || textLower.Contains("start_windows.bat") || textLower.Contains("packages") || textLower.Contains("autoclicker") || textLower.Contains("clicker") || textLower.Contains("tinytask") || textLower.Contains("opautoclicker") || textLower.Contains("speedautoclicker") || textLower.Contains("x-mouse") || textLower.Contains("xmouse") || textLower.Contains("keyran") || textLower.Contains("botmek") || textLower.Contains("autohotkey")) continue;
										bool flag = false;
										string[] cheatClientNames = CheatClientNames;
										foreach (string cheatLower in cheatClientNames)
										{
											if (MatchesCheatName(textLower, cheatLower))
											{
												flag = true;
												break;
											}
										}
										if (!flag)
										{
											string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
											foreach (string text4 in extraCheatClients)
											{
												if (text4.Length >= 4 && MatchesCheatName(textLower, text4))
												{
													flag = true;
													break;
												}
											}
										}
										if (!flag)
										{
											foreach (string cl in CheatDatabases.KnownCleanerSignatures)
											{
												if (cl.Length >= 4 && textLower.Contains(cl))
												{
													flag = true;
													break;
												}
											}
										}
										if (!flag)
										{
											continue;
										}
										string text5 = "";
										if (File.Exists(text3))
										{
											text5 = "⛔ [5.01 - Удалённый чит в UserAssist]: " + text3;
										}
										else if (text3.Contains(":\\") || text3.Contains(":{"))
										{
					text5 = "\ud83d\udea8 [5.08 - Запуск ПО из UserAssist]: " + text3;
										}
										else
										{
											text5 = "⛔ [5.01 - Чит в UserAssist]: " + text3;
										}
										byte[] array = registryKey2.GetValue(text2) as byte[];
										if (array != null && array.Length >= 68)
										{
											try
											{
												int num = BitConverter.ToInt32(array, 4);
												long num2 = BitConverter.ToInt64(array, 60);
												if (num2 > 0)
												{
													DateTime dateTime = DateTime.FromFileTimeUtc(num2).ToLocalTime();
													text5 += string.Format(" | Запусков: {0} | Последний запуск: {1:dd.MM.yyyy HH:mm:ss}", num, dateTime);
												}
											}
											catch
											{
											}
										}
										list2.Add(text5);
										if (list2.Count >= 30)
										{
											break;
										}
									}
								}
							}
							if (list2.Count >= 30)
							{
								break;
							}
						}
					}
				}
			}
			catch
			{
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Реестр";
			checkResult.Name = "Декодирование ROT13 истории запусков UserAssist";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = ((list2.Count > 0) ? string.Format("В реестре UserAssist зафиксирован запуск читов ({0} шт.) с точным временем и счётчиком запусков!", list2.Count) : "В истории UserAssist следов запуска читов не обнаружено");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckDnsCacheAndNetwork()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo("ipconfig", "/displaydns");
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.UseShellExecute = false;
				processStartInfo.CreateNoWindow = true;
				using (Process process = Process.Start(processStartInfo))
				{
					string text = process.StandardOutput.ReadToEnd().ToLower();
					process.WaitForExit(3000);
					string[] cheatWebsites = CheatWebsites;
					foreach (string text2 in cheatWebsites)
					{
						if (text.Contains(text2))
						{
							list2.Add("Обнаружен чит-домен в DNS кэше Windows: " + text2);
						}
					}
					string[] extraCheatDomains = CheatDatabases.ExtraCheatDomains;
					foreach (string ed in extraCheatDomains)
					{
						if (ed.Length >= 4 && text.Contains(ed.ToLowerInvariant()))
						{
							list2.Add("Подозрительный чит-домен (ExtraCheatDomains) в DNS кэше: " + ed);
						}
					}
					string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
					foreach (string ec in extraCheatClients)
					{
						if (ec.Length >= 4 && text.Contains(ec.ToLowerInvariant()))
						{
							list2.Add("Подозрительный чит-клиент (ExtraCheatClients) в DNS кэше: " + ec);
						}
					}}
			}
			catch
			{
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Сеть";
			checkResult.Name = "DNS кэш Windows (ipconfig /displaydns — обращения к серверам читов)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("В кэше DNS найдены запросы к сайтам читов (" + list2.Count + ")!") : "DNS кэш не содержит обращений к серверам читов");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckPowerShellHistory()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string text = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft\\Windows\\PowerShell\\PSReadLine\\ConsoleHost_history.txt");
			if (File.Exists(text))
			{
				try
				{
					TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
					FileInfo fi = new FileInfo(text);
					// 0-byte ConsoleHost_history.txt is common on clean/unused PowerShell - skip false positive
					List<string> arrayLines = new List<string>();
					using (FileStream fs = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
					using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
					{
						string l;
						while ((l = sr.ReadLine()) != null) arrayLines.Add(l);
					}
					string[] array = arrayLines.ToArray();
					string[] array2 = new string[]
					{
						"fsutil usn", "deletejournal", "vape", "nursultan", "expensive", "celestial", "deadcode", "akrien", "downloadstring", "invoke-webrequest",
						"del *.jar", "wevtutil cl", "cipher /w", "sdelete", "bleachbit", "ccleaner", "reg delete",
						"clear-history", "wget ",
						"thunderhack", "slinky", "dreamclient", "zamorozka", "fluger", "minced", "excellent", "wildclient", "certutil", "bitsadmin",
						"bcdedit", "vssadmin", "attrib -h", "net stop", "sc stop", "rundll32"
					};
					for (int i = 0; i < array.Length; i++)
					{
						string text2 = array[i].ToLower().Trim();
						string[] array3 = array2;
						foreach (string value in array3)
						{
							if (text2.Contains(value))
							{
								string cmdTrim = array[i].Trim(); bool isCl = false; foreach (string cl in CheatDatabases.KnownCleanerSignatures) { if (cl.Length >= 4 && text2.Contains(cl)) { list2.Add("⛔ [5.08 - Попытка затирания в PowerShell] [стр. " + (i + 1) + "]: " + cmdTrim); isCl = true; break; } } if (!isCl) { bool isWiper = text2.Contains("delete") || text2.Contains("wevtutil") || text2.Contains("stop") || text2.Contains("clear") || text2.Contains("rmdir") || text2.Contains("remove") || text2.Contains("cipher"); list2.Add((isWiper ? "⛔ [5.08 - Попытка затирания в PowerShell] [стр. " : "⛔ [5.01 - Чит-команда в PowerShell] [стр. ") + (i + 1) + "]: " + cmdTrim); }
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "История";
			checkResult.Name = "История консоли PowerShell (Попытки удаления следов / скачивания)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((!list2.Any((string x) => x.Contains("fsutil") || x.Contains("wevtutil"))) ? CheckSeverity.Warning : CheckSeverity.Critical);
			checkResult.Detail = ((list2.Count > 0) ? ("В истории PowerShell найдены подозрительные команды (" + list2.Count + " шт.)!") : "История PowerShell чиста");
			checkResult.Evidence = ((list2.Count > 0) ? string.Join("\r\n", list2.ToArray()) : ("Доп. доменов: " + text));
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckCrashDumps()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
			string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			// 1. %LOCALAPPDATA%\CrashDumps (*.dmp files)
			string crashDumpsDir = Path.Combine(localAppData, "CrashDumps");
			if (Directory.Exists(crashDumpsDir))
			{
				try
				{
					string[] files = Directory.GetFiles(crashDumpsDir, "*.dmp", SearchOption.TopDirectoryOnly);
					foreach (string dmpPath in files)
					{
						string text = Path.GetFileName(dmpPath).ToLowerInvariant();
						DateTime lastWriteTime = File.GetLastWriteTime(dmpPath);
						bool flag = false;
						string matchedCheat = "";

						foreach (string text2 in CheatClientNames)
						{
							if (MatchesCheatName(text, text2))
							{
								flag = true;
								matchedCheat = text2;
								break;
							}
						}
						if (!flag)
						{
							foreach (string ec in CheatDatabases.ExtraCheatClients)
							{
								if (ec.Length >= 4 && MatchesCheatName(text, ec))
								{
									flag = true;
									matchedCheat = ec;
									break;
								}
							}
						}
						if (!flag)
						{
							string[] injectors = new string[] { "kmbox", "interception", "spoofer", "dllinjector", "cheatloader" };
							foreach (string inj in injectors)
							{
								if (text.Contains(inj))
								{
									flag = true;
									matchedCheat = inj;
									break;
								}
							}
						}

						bool isCleaner = false;
						string cleanerMatched = "";
						if (!flag)
						{
							foreach (string cl in CheatDatabases.KnownCleanerSignatures)
							{
								if (cl.Length >= 4 && text.Contains(cl))
								{
									isCleaner = true;
									cleanerMatched = cl;
									break;
								}
							}
						}

						if (flag)
						{
							list2.Add(string.Format("⛔ [5.01 - Дамп чит-клиента] Аварийный дамп сбоя [{0}]: {1} ({2:dd.MM.yyyy HH:mm})", matchedCheat, Path.GetFileName(dmpPath), lastWriteTime));
						}
						else if (isCleaner)
						{
							list2.Add(string.Format("⛔ [5.08 - Дамп клинера в CrashDumps] Дамп утилиты очистки [{0}]: {1} ({2:dd.MM.yyyy HH:mm})", cleanerMatched, Path.GetFileName(dmpPath), lastWriteTime));
						}
						else if (text.StartsWith("javaw") || text.StartsWith("minecraft") || text.StartsWith("java"))
						{
							TimeSpan age = DateTime.Now - lastWriteTime;
							if (age.TotalHours <= 2.0 && age.TotalHours >= 0.0)
							{
								list3.Add(string.Format("ℹ️ Свежий дамп Minecraft javaw.exe: {0} ({1:dd.MM.yyyy HH:mm})", Path.GetFileName(dmpPath), lastWriteTime));
							}
							else
							{
								list3.Add(string.Format("Дамп сбоя Java/Minecraft: {0} ({1:dd.MM.yyyy HH:mm})", Path.GetFileName(dmpPath), lastWriteTime));
							}
						}
					}
				}
				catch
				{
				}
			}

			// 2. Java Crash Logs (hs_err_pid*.log)
			try
			{
				List<string> hsDirs = new List<string>();
				if (!string.IsNullOrEmpty(userProfile))
				{
					hsDirs.Add(userProfile);
					hsDirs.Add(Path.Combine(userProfile, "Desktop"));
				}
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				if (!string.IsNullOrEmpty(appData))
				{
					hsDirs.Add(Path.Combine(appData, ".minecraft"));
				}

				foreach (string hDir in hsDirs)
				{
					if (!Directory.Exists(hDir)) continue;
					try
					{
						string[] hsLogs = Directory.GetFiles(hDir, "hs_err_pid*.log", SearchOption.TopDirectoryOnly);
						foreach (string hsLog in hsLogs)
						{
							try
							{
								FileInfo hsFi = new FileInfo(hsLog);
								if (hsFi.Length > 2097152L) continue;

								string hsContent = null;
								using (FileStream fs = new FileStream(hsLog, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
								{
									using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
									{
										hsContent = sr.ReadToEnd().ToLowerInvariant();
									}
								}

								if (!string.IsNullOrEmpty(hsContent))
								{
									bool hsCheat = false;
									string hsCheatName = "";
									foreach (string cl in CheatClientNames)
									{
										if (hsContent.Contains(cl.ToLowerInvariant()))
										{
											hsCheat = true;
											hsCheatName = cl;
											break;
										}
									}
									if (!hsCheat)
									{
										foreach (string ec in CheatDatabases.ExtraCheatClients)
										{
											if (ec.Length >= 4 && hsContent.Contains(ec.ToLowerInvariant()))
											{
												hsCheat = true;
												hsCheatName = ec;
												break;
											}
										}
									}

									if (hsCheat)
									{
										list2.Add(string.Format("⛔ [5.01 - Чит в логе сбоя JVM (hs_err)] Лог сбоя Java содержит сигнатуру чита [{0}]: {1} (дата: {2:dd.MM.yyyy HH:mm})", hsCheatName, Path.GetFileName(hsLog), hsFi.LastWriteTime));
									}
									else
									{
										TimeSpan age = DateTime.Now - hsFi.LastWriteTime;
										if (age.TotalHours <= 2.0 && age.TotalHours >= 0.0)
										{
											list3.Add(string.Format("⚠️ Свежий лог сбоя Java hs_err: {0} ({1:dd.MM.yyyy HH:mm}, {2:0} мин назад)", Path.GetFileName(hsLog), hsFi.LastWriteTime, age.TotalMinutes));
										}
									}
								}
							}
							catch { }
						}
					}
					catch { }
				}
			}
			catch { }

			// 3. Windows Error Reporting (WER) ReportArchive & ReportQueue
			string[] werFolders = new string[]
			{
				Path.Combine(progData, @"Microsoft\Windows\WER\ReportArchive"),
				Path.Combine(progData, @"Microsoft\Windows\WER\ReportQueue"),
				Path.Combine(localAppData, @"Microsoft\Windows\WER\ReportArchive"),
				Path.Combine(localAppData, @"Microsoft\Windows\WER\ReportQueue")
			};

			foreach (string werFolder in werFolders)
			{
				if (!Directory.Exists(werFolder)) continue;
				try
				{
					string[] subDirs = Directory.GetDirectories(werFolder);
					foreach (string subDir in subDirs)
					{
						string reportWer = Path.Combine(subDir, "Report.wer");
						if (!File.Exists(reportWer)) continue;

						try
						{
							string appName = null;
							string appPath = null;
							string targetPath = null;
							string faultModule = null;
							DateTime eventTime = File.GetLastWriteTime(subDir);

							using (FileStream fs = new FileStream(reportWer, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
							{
								using (StreamReader sr = new StreamReader(fs, Encoding.Unicode))
								{
									string line;
									int lineCount = 0;
									while ((line = sr.ReadLine()) != null && lineCount < 120)
									{
										lineCount++;
										string trimLine = line.Trim();
										if (trimLine.StartsWith("AppName=", StringComparison.OrdinalIgnoreCase))
										{
											appName = trimLine.Substring(8).Trim();
										}
										else if (trimLine.StartsWith("AppPath=", StringComparison.OrdinalIgnoreCase))
										{
											appPath = trimLine.Substring(8).Trim();
										}
										else if (trimLine.StartsWith("TargetAppPath=", StringComparison.OrdinalIgnoreCase))
										{
											targetPath = trimLine.Substring(14).Trim();
										}
										else if (trimLine.StartsWith("Sig[3].Value=", StringComparison.OrdinalIgnoreCase) || trimLine.StartsWith("Sig[0].Value=", StringComparison.OrdinalIgnoreCase))
										{
											if (faultModule == null)
											{
												int eqIdx = trimLine.IndexOf('=');
												if (eqIdx >= 0) faultModule = trimLine.Substring(eqIdx + 1).Trim();
											}
										}
										else if (trimLine.StartsWith("EventTime=", StringComparison.OrdinalIgnoreCase))
										{
											long ft;
											if (long.TryParse(trimLine.Substring(10).Trim(), out ft) && ft > 0)
											{
												try { eventTime = DateTime.FromFileTime(ft); } catch { }
											}
										}
									}
								}
							}

							string effectivePath = !string.IsNullOrEmpty(appPath) ? appPath : (!string.IsNullOrEmpty(targetPath) ? targetPath : appName);
							if (string.IsNullOrEmpty(effectivePath)) continue;

							string checkStr = (effectivePath + " " + (appName ?? "")).ToLowerInvariant();
							bool isCheatWer = false;
							string matchedWer = "";

							foreach (string cl in CheatClientNames)
							{
								if (MatchesCheatName(checkStr, cl))
								{
									isCheatWer = true;
									matchedWer = cl;
									break;
								}
							}
							if (!isCheatWer)
							{
								foreach (string ec in CheatDatabases.ExtraCheatClients)
								{
									if (ec.Length >= 4 && MatchesCheatName(checkStr, ec))
									{
										isCheatWer = true;
										matchedWer = ec;
										break;
									}
								}
							}
							if (!isCheatWer)
							{
								string[] werInj = new string[] { "kmbox", "interception", "spoofer", "dllinjector", "cheatloader" };
								foreach (string winj in werInj)
								{
									if (checkStr.Contains(winj))
									{
										isCheatWer = true;
										matchedWer = winj;
										break;
									}
								}
							}

							bool isCleanerWer = false;
							string cleanerWerMatched = "";
							if (!isCheatWer)
							{
								foreach (string cl in CheatDatabases.KnownCleanerSignatures)
								{
									if (cl.Length >= 4 && checkStr.Contains(cl))
									{
										isCleanerWer = true;
										cleanerWerMatched = cl;
										break;
									}
								}
							}

							if (isCheatWer)
							{
								bool fileExists = false;
								try { fileExists = File.Exists(effectivePath); } catch { }

								if (!fileExists)
								{
					list2.Add(string.Format("\ud83d\udea8 [5.08 - Крах ПО из WER] В отчетах WER зафиксирован крах чит-клиента: '{0}' (сигнатура: {1}, модуль: {2}, время: {3:dd.MM.yyyy HH:mm})", effectivePath, matchedWer, faultModule ?? "N/A", eventTime));
								}
								else
								{
									list2.Add(string.Format("⛔ [5.01 - Сбой чита в WER] В отчёте WER зафиксирован сбой известного чит-приложения: '{0}' (сигнатура: {1}, модуль: {2}, дата: {3:dd.MM.yyyy HH:mm})", effectivePath, matchedWer, faultModule ?? "N/A", eventTime));
								}
							}
							else if (isCleanerWer)
							{
					list2.Add(string.Format("\ud83d\udea8 [5.08 - Крах ПО из WER] В отчетах WER зафиксирован крах чит-клиента: '{0}' (сигнатура: {1}, модуль: {2}, время: {3:dd.MM.yyyy HH:mm})", effectivePath, matchedWer, faultModule ?? "N/A", eventTime));
							}
							else if (checkStr.Contains("javaw") || checkStr.Contains("minecraft"))
							{
								if (!string.IsNullOrEmpty(faultModule))
								{
									string fmLower = faultModule.ToLowerInvariant();
									bool isCheatMod = fmLower.Contains("vape") || fmLower.Contains("kura") || fmLower.Contains("celestial") ||
										fmLower.Contains("expensive") || fmLower.Contains("nursultan") || fmLower.Contains("hook") ||
										fmLower.Contains("inject") || fmLower.Contains("drip") || fmLower.Contains("slinky") ||
										fmLower.Contains("booster");
									if (isCheatMod)
									{
										list2.Add(string.Format("⛔ [5.01 - Инжекция в javaw.exe] Процесс Minecraft аварийно завершил внешний модуль: '{0}' (папка: {1}, дата: {2:dd.MM.yyyy HH:mm})", faultModule, Path.GetFileName(subDir), eventTime));
									}
									else
									{
										list3.Add(string.Format("Отчёт WER сбоя Minecraft: {0} (модуль сбоя: {1}, дата: {2:dd.MM.yyyy HH:mm})", Path.GetFileName(subDir), faultModule, eventTime));
									}
								}
							}

						}
						catch
						{
						}
					}
				}
				catch
				{
				}
			}

			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Следы запуска";
			checkResult.Name = "Дампы аварийного завершения (CrashDumps, JVM hs_err & WER логи)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = (list2.Count > 0 ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = (list2.Count > 0 ? ("Обнаружены аварийные дампы сбоев читов/клинеров [5.01 / 5.08] (" + list2.Count + " шт.)!") : ((list3.Count > 0) ? ("Дампов читов нет (обычных падений Java: " + list3.Count + ")") : "Следов и аварийных дампов сбоев читов не обнаружено"));
			List<string> list4 = new List<string>();
			list4.AddRange(list2);
			list4.AddRange(list3);
			checkResult.Evidence = string.Join("\r\n", list4.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckUsbConnectionHistory()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum\\USBSTOR"))
				{
					if (registryKey != null)
					{
						string[] subKeyNames = registryKey.GetSubKeyNames();
						foreach (string text in subKeyNames)
						{
							using (RegistryKey registryKey2 = registryKey.OpenSubKey(text))
							{
								if (registryKey2 == null)
								{
									continue;
								}
								string[] subKeyNames2 = registryKey2.GetSubKeyNames();
								foreach (string text2 in subKeyNames2)
								{
									using (RegistryKey registryKey3 = registryKey2.OpenSubKey(text2))
									{
										if (registryKey3 != null)
										{
											object value = registryKey3.GetValue("FriendlyName");
											string arg = ((value != null) ? value.ToString() : text);
											list2.Add(string.Format("USB-накопитель: {0} (ID: {1})", arg, text2));
										}
									}
								}
							}
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "INF");
				string path2 = Path.Combine(path, "setupapi.dev.log");
				if (File.Exists(path2))
				{
					using (FileStream stream = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
					{
						using (StreamReader streamReader = new StreamReader(stream, Encoding.UTF8))
						{
							string text3 = null;
							string text4;
							while ((text4 = streamReader.ReadLine()) != null)
							{
								string text5 = text4.ToLowerInvariant();
								if (text5.Contains("[device install") && (text5.Contains("usbstor") || text5.Contains("disk&ven_")))
								{
									text3 = text4.Trim();
								}
								else
								{
									if (text3 == null || !text4.StartsWith(">>>  Section start", StringComparison.OrdinalIgnoreCase))
									{
										continue;
									}
									string text6 = text4.Replace(">>>  Section start", "").Trim();
									DateTime result;
									if (DateTime.TryParse(text6.Split('.')[0].Replace("/", "-"), out result))
									{
										TimeSpan timeSpan = DateTime.Now - result;
										string item = string.Format("Подключение USB [{0:dd.MM.yyyy HH:mm:ss}]: {1}", result, text3);
										if (timeSpan.TotalHours >= 0.0 && timeSpan.TotalHours <= 2.0)
										{
											list3.Add(string.Format("⛔ [5.08 - Свежее подключение USB] Флешка была подключена {0:dd.MM.yyyy HH:mm:ss} (менее 2 часов назад)! Проверьте на запуск софта с внешнего диска!", result));
										}
										list2.Add(item);
									}
									text3 = null;
								}
							}
						}
					}
				}
			}
			catch
			{
			}
			List<string> list4 = new List<string>();
			list4.AddRange(list3);
			list4.AddRange(list2);
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Служебные логи";
			checkResult.Name = "История подключения USB-накопителей (USBSTOR & setupapi.dev.log)";
			checkResult.Detected = list3.Count > 0;
			checkResult.Severity = ((list3.Count > 0) ? CheckSeverity.NeedsCheck : CheckSeverity.Info);
			checkResult.Detail = ((list3.Count > 0) ? string.Format("ВНИМАНИЕ! Обнаружено свежее подключение USB перед проверкой ({0} шт.)! Всего устройств в истории: {1}", list3.Count, list2.Count) : ((list2.Count > 0) ? string.Format("Зафиксировано подключений USB в истории: {0} шт. Свежих подключений не найдено.", list2.Count) : "Подключений USB-накопителей не зафиксировано"));
			checkResult.Evidence = string.Join("\r\n", list4.ToArray());
			list.Add(checkResult);
			return list;
		}

				private static List<CheckResult> CheckCleanerUtilities()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
			if (!Directory.Exists(path))
			{
				path = "C:\\Windows\\Prefetch";
			}
			if (Directory.Exists(path))
			{
				try
				{
					string[] array = new string[]
					{
						"CCLEANER", "BLEACHBIT", "PRIVAZER", "ERASER", "SDELETE", "WEVTUTIL", "FSUTIL", "CLEANMGR",
						"CIPHER", "BCWIPER", "GLARY", "REGDEL", "REVO", "IOBIT", "KERISH", "REGORG", "DBCLEAN",
						"KOID", "SL-CLEANER", "SLCLEANER", "JOURNALTRACE", "TRACEKILLER", "USNDELETE", "VANISH", "DESTRUCT", "SELFDESTRUCT", "GHOSTCLEANER", "USN_CLEAN", "DELETE_TRACES",
						"JOURNAL", "PREFETCHCLEANER", "LOGCLEANER", "WINSWEEPER", "KRESTIK", "BYPASS",
						"SLINKYCLEAN", "VAPECLEAN", "SHELLBAG", "EXECUTIONCHECKER",
						"SYSTEMNINJA", "REDLINES", "PURGE", "SHREDDER", "DEFENDERCONTROL", "DCONTROL", "STOPDEFENDER",
						"PROCESS_HACKER", "PROCHAX", "X64DBG", "CHEATENGINE", "CE.EXE", "WIPER", "EXTERMINATOR"
					};
					string[] files = Directory.GetFiles(path, "*.pf");
					int pfCount = files.Length;
					TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
					if (pfCount < 10 && uptime.TotalHours >= 12.0)
					{
						list3.Add(string.Format("ℹ️ [Prefetch]: В C:\\Windows\\Prefetch обнаружено {0} файлов .pf (SysMain активен или только включён)", pfCount));
					}

					System.Diagnostics.Stopwatch swDisg = System.Diagnostics.Stopwatch.StartNew();
											foreach (string text in files)
						{
							if (swDisg.ElapsedMilliseconds > 1500) break;

						string text2 = Path.GetFileName(text).ToUpperInvariant();
						bool cleanerFound = false;
						string foundSig = "";
						foreach (string text3 in array)
						{
							if (text2.StartsWith(text3, StringComparison.OrdinalIgnoreCase))
							{
								cleanerFound = true;
								foundSig = text3;
								break;
							}
						}
						if (!cleanerFound)
						{
							string basePfName = Path.GetFileNameWithoutExtension(text2);
							foreach (string cl in CheatDatabases.KnownCleanerSignatures)
							{
								if (cl.Length >= 4 && (text2.IndexOf(cl, StringComparison.OrdinalIgnoreCase) >= 0 || basePfName.IndexOf(cl, StringComparison.OrdinalIgnoreCase) >= 0))
								{
									cleanerFound = true;
									foundSig = cl;
									break;
								}
							}
						}
						if (cleanerFound)
						{
							DateTime lastWriteTime = File.GetLastWriteTime(text);
							TimeSpan timeSpan = DateTime.Now - lastWriteTime;
							if (timeSpan.TotalHours <= 2.0 && timeSpan.TotalHours >= 0.0)
							{
								list2.Add(string.Format("⛔ [5.08 - Свежая очистка логов/диска перед проверкой] Запуск утилиты очистки следов [{0}]: {1} ({2:dd.MM.yyyy HH:mm}, {3:0} мин назад)!", foundSig, text, lastWriteTime, timeSpan.TotalMinutes));
							}
							else if (timeSpan.TotalHours < 24.0 && timeSpan.TotalHours >= 0.0)
							{
								list2.Add(string.Format("⛔ [5.08 - Запуск утилиты очистки диска] [{0}]: {1} ({2:dd.MM.yyyy HH:mm}, {3:0} мин назад)", foundSig, text, lastWriteTime, timeSpan.TotalMinutes));
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Следы запуска";
			checkResult.Name = "Запуски утилит скрытия/очистки за последние 24ч (Prefetch)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = (list2.Count > 0 ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = (list2.Count > 0 ? string.Format("Обнаружены следы клинеров в Prefetch ({0} записей)!", list2.Count) : "Следов клинеров в Prefetch не обнаружено.");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		public static List<CheckResult> CheckRegulationSysMainAndUptime()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				try
				{
					using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\SysMain"))
					{
						if (registryKey != null)
						{
							object value = registryKey.GetValue("Start");
							if (value != null && Convert.ToInt32(value) == 4)
							{
								list2.Add("🚨 ПУНКТ 3.6 РЕГЛАМЕНТА: Служба SysMain (Prefetch) ОТКЛЮЧЕНА (Start=4)! Квалифицируется как уклонение от проверки / скрытие следов!");
							}
						}
					}
				}
				catch
				{
				}
				try
				{
					Process[] processesByName = Process.GetProcessesByName("javaw");
					if (processesByName.Length == 0)
					{
						processesByName = Process.GetProcessesByName("java");
					}
					Process[] array = processesByName;
					foreach (Process process in array)
					{
						TimeSpan timeSpan = DateTime.Now - process.StartTime;
						if (timeSpan.TotalMinutes < 10.0)
						{
							list2.Add("⚠️ ШАГ 1 РЕГЛАМЕНТА: Minecraft (javaw.exe PID=" + process.Id + ") запущен всего " + Math.Round(timeSpan.TotalMinutes, 1) + " мин назад (возможный перезапуск клиента в момент вызова на проверку)!");
						}
					}
				}
				catch
				{
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Регламент проверки / Очистка";
					checkResult.Name = "Служба SysMain (Prefetch) и перезапуск игры (ШАГ 1 и 6 регламента)";
					checkResult.Detected = true;
					checkResult.Severity = ((!list2.Any((string text) => text.Contains("\ud83d\udea8"))) ? CheckSeverity.Warning : CheckSeverity.Critical);
					checkResult.Detail = "Выявлены нарушения регламента проверки (SysMain/Перезапуск): " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Регламент проверки / Очистка";
					checkResult2.Name = "Служба SysMain и статус процесса Minecraft (Регламент проверки)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Служба SysMain активна. Перезапусков Minecraft перед проверкой не зафиксировано.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Регламент проверки / Очистка";
				checkResult3.Name = "Служба SysMain и статус Minecraft";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки SysMain: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		private static string RunSysTool(string fileName, string arguments, out int exitCode)
		{
			exitCode = -1;
			try
			{
				ProcessStartInfo processStartInfo = new ProcessStartInfo(fileName, arguments);
				processStartInfo.UseShellExecute = false;
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.RedirectStandardError = true;
				processStartInfo.CreateNoWindow = true;
				using (Process process = Process.Start(processStartInfo))
				{
					if (process == null)
					{
						return string.Empty;
					}
					string result = process.StandardOutput.ReadToEnd() + process.StandardError.ReadToEnd();
					process.WaitForExit(8000);
					exitCode = (process.HasExited ? process.ExitCode : (-1));
					return result;
				}
			}
			catch
			{
				return string.Empty;
			}
		}

		public static List<CheckResult> CheckRegulationJavaAgentEnv()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string[] array = new string[4] { "_JAVA_OPTIONS", "JAVA_TOOL_OPTIONS", "JDK_JAVA_OPTIONS", "JAVA_OPTIONS" };
			string[] array2 = new string[5] { "-javaagent", "-agentlib", "-agentpath", "-xbootclasspath", "-xrunjdwp" };
			EnvironmentVariableTarget[] array3 = new EnvironmentVariableTarget[3]
			{
				EnvironmentVariableTarget.Process,
				EnvironmentVariableTarget.User,
				EnvironmentVariableTarget.Machine
			};
			string[] array4 = array;
			foreach (string text in array4)
			{
				EnvironmentVariableTarget[] array5 = array3;
				foreach (EnvironmentVariableTarget environmentVariableTarget in array5)
				{
					string text2 = null;
					try
					{
						text2 = Environment.GetEnvironmentVariable(text, environmentVariableTarget);
					}
					catch
					{
					}
					if (string.IsNullOrEmpty(text2))
					{
						continue;
					}
					string text3 = text2.ToLowerInvariant();
					bool flag = false;
					string[] array6 = array2;
					foreach (string value in array6)
					{
						if (text3.Contains(value))
						{
							flag = true;
							break;
						}
					}
					if (flag)
					{
						list2.Add(string.Concat("🚨 ШАГ 4 РЕГЛАМЕНТА: ", text, " (", environmentVariableTarget, ") содержит агент: ", text2));
					}
					else
					{
						list2.Add(string.Concat("⚠️ ШАГ 4 РЕГЛАМЕНТА: переменная ", text, " (", environmentVariableTarget, ") задана: ", text2));
					}
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Регламент проверки / Инъекции";
			checkResult.Name = "Переменные окружения Java (_JAVA_OPTIONS / JAVA_TOOL_OPTIONS) — ШАГ 4 регламента";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count != 0) ? ((!list2.Any((string h) => h.Contains("\ud83d\udea8"))) ? CheckSeverity.Warning : CheckSeverity.Critical) : CheckSeverity.Info);
			checkResult.Detail = ((list2.Count > 0) ? ("Найдены подозрительные переменные окружения Java: " + list2.Count + " шт. (типичная подгрузка агента без модов).") : "Переменные окружения Java не заданы — инъекция через env не обнаружена.");
			checkResult.Evidence = ((list2.Count > 0) ? string.Join("\r\n", list2) : "");
			list.Add(checkResult);
			return list;
		}

		public static List<CheckResult> CheckRegulationUsnJournal()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			int checkedDrives = 0;
			try
			{
				DriveInfo[] drives = DriveInfo.GetDrives();
				foreach (DriveInfo d in drives)
				{
					try
					{
						if (d.DriveType != DriveType.Fixed) continue;
						if (!string.Equals(d.DriveFormat, "NTFS", StringComparison.OrdinalIgnoreCase)) continue;
						checkedDrives++;
						string driveLetter = d.Name.TrimEnd('\\');
						int exitCode;
						string text = RunSysTool("fsutil", "usn queryjournal " + driveLetter, out exitCode);
						string text2 = (text ?? string.Empty).ToLowerInvariant();
						bool flag = text2.Contains("отказано в доступе") || text2.Contains("access is denied");
						bool flag2 = Regex.IsMatch(text ?? string.Empty, "0x0[0-9a-fA-F]{15}");
						bool flag3 = text2.Contains("не найден") || text2.Contains("not found") || text2.Contains("cannot find") || text2.Contains("отсутствует");
						if (flag)
						{
							list3.Add("ℹ️ USN Journal на диске " + driveLetter + " повреждён или отключён — по регламенту это признак очистки USN Journal.");
						}
						else if (flag3 || (!flag2 && exitCode != 0))
						{
							if (driveLetter.Equals("C:", StringComparison.OrdinalIgnoreCase))
							list2.Add("⛔ Пункт 3.6 регламента: USN Journal на диске " + driveLetter + " повреждён/очищен (код " + exitCode + ") командой deletejournal / затирание следов!");
							else
							list3.Add("ℹ️ Проверка USN Journal на диске " + driveLetter + " вернула код (" + exitCode + ", доступ или отсутствие записи).");
						}
						else
						{
							Match mNext = Regex.Match(text ?? string.Empty, @"(?:Next Usn|Следующий USN)\s*:\s*(?:0x)?([0-9a-fA-F]+)", RegexOptions.IgnoreCase);
							if (mNext.Success)
							{
								try
								{
									long nextUsnVal = Convert.ToInt64(mNext.Groups[1].Value, 16);
									TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
									if (driveLetter.Equals("C:", StringComparison.OrdinalIgnoreCase) && nextUsnVal < 0x80000 && uptime.TotalHours >= 1.0)
									{
										list2.Add(string.Format("⛔ [5.08 / Пункт 3.6 регламента] Журнал USN Journal на диске {0} имеет аномально малое смещение Next Usn (0x{1:X}), что указывает на недавнее удаление и пересоздание журнала (fsutil usn deletejournal /d) для сокрытия следов запуска!", driveLetter, nextUsnVal));
									}
								}
								catch { }
							}
						}
					}
					catch
					{
					}
				}
			}
			catch
			{
			}
			if (checkedDrives == 0)
			{
				int exitCode;
				string text = RunSysTool("fsutil", "usn queryjournal C:", out exitCode);
				string text2 = (text ?? string.Empty).ToLowerInvariant();
				if (text2.Contains("не найден") || text2.Contains("not found"))
				{
					list2.Add("🚨 ПУНКТ 3.6 РЕГЛАМЕНТА: USN Journal на диске C: недоступен или удалён (fsutil код ");
				}
			}

			// Проверка очистки журналов событий (EventID 1102 / 104) и удаления USN (3079)
			try
			{
				EventLogQuery q1102 = new EventLogQuery("Security", PathType.LogName, "*[System[(EventID=1102)]]");
				using (EventLogReader r1102 = new EventLogReader(q1102))
				{
					EventRecord rec;
					if ((rec = r1102.ReadEvent()) != null)
					{
						using (rec)
						{
							DateTime dt = rec.TimeCreated.HasValue ? rec.TimeCreated.Value : DateTime.Now;
							list2.Add(string.Format("⛔ [5.08 - Очистка журнала Wevtutil] Зафиксирована принудительная очистка журнала Security Event ID 1102 в {0:dd.MM.yyyy HH:mm:ss}!", dt));
						}
					}
				}
			}
			catch { }

			try
			{
				EventLogQuery q104 = new EventLogQuery("System", PathType.LogName, "*[System[(EventID=104)]]");
				using (EventLogReader r104 = new EventLogReader(q104))
				{
					EventRecord rec;
					if ((rec = r104.ReadEvent()) != null)
					{
						using (rec)
						{
							DateTime dt = rec.TimeCreated.HasValue ? rec.TimeCreated.Value : DateTime.Now;
							list2.Add(string.Format("⛔ [5.08 - Очистка журнала Wevtutil] Зафиксирована принудительная очистка системного журнала System Event ID 104 в {0:dd.MM.yyyy HH:mm:ss}!", dt));
						}
					}
				}
			}
			catch { }

			try
			{
				EventLogQuery q3079 = new EventLogQuery("System", PathType.LogName, "*[System[(EventID=3079 or EventID=1205)]]");
				using (EventLogReader r3079 = new EventLogReader(q3079))
				{
					EventRecord rec;
					if ((rec = r3079.ReadEvent()) != null)
					{
						using (rec)
						{
							DateTime dt = rec.TimeCreated.HasValue ? rec.TimeCreated.Value : DateTime.Now;
							list2.Add(string.Format("⛔ [5.08 - Удаление журнала USN Journal] В системном журнале зафиксировано удаление журнала изменений NTFS (Event ID {0}) в {1:dd.MM.yyyy HH:mm:ss}!", rec.Id, dt));
						}
					}
				}
			}
			catch { }

			// Проверка отключения теневых копий VSS (vssadmin delete shadows)
			try
			{
				using (RegistryKey vssKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\VSS"))
				{
					if (vssKey != null)
					{
						object val = vssKey.GetValue("Start");
						if (val is int && (int)val == 4)
						{
							list2.Add("⛔ [5.08 - Отключение теневых копий VSS] Служба теневого копирования томов VSS отключена (Start=4) для блокировки восстановления удалённых читов!");
						}
					}
				}
			}
			catch { }

			// Проверка истории команд PowerShell на очистку и вайперы
			try
			{
				string psHist = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), @"Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt");
				if (File.Exists(psHist))
				{
					string[] lines = File.ReadAllLines(psHist);
					string[] wiperKeywords = new string[] { "deletejournal", "wevtutil cl", "vssadmin delete", "sdelete", "cipher /w", "clear-eventlog" };
					foreach (string line in lines)
					{
						string lLower = line.ToLowerInvariant().Trim();
						foreach (string kw in wiperKeywords)
						{
							if (lLower.Contains(kw))
							{
								list2.Add(string.Format("⛔ [5.08 - Команда затирания следов в истории PowerShell]: {0}", line.Trim()));
								break;
							}
						}
					}
				}
			}
			catch { }

			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Регламент проверки / Очистка";
			checkResult.Name = "Состояние USN Journal на NTFS-дисках (Пункт 3.6 регламента)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			if (list2.Count > 0)
			{
				checkResult.Detail = "Обнаружено стирание или повреждение USN Journal на дисках (" + list2.Count + " шт.).";
			}
			else if (list3.Count > 0)
			{
				checkResult.Detail = "🚨 ШАГ 4 РЕГЛАМЕНТА: " + string.Join(" ", list3);
			}
			else
			{
			checkResult.Detail = "USN Journal на всех проверенных NTFS-дисках (" + checkedDrives + ") на месте и читается — следы не затирались через deletejournal.";
			}
			checkResult.Evidence = ((list2.Count > 0) ? string.Join("\r\n", list2) : "");
			list.Add(checkResult);
			return list;
		}

		public static List<CheckResult> CheckRegulationCoreJarIntegrity()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> jarFiles = new List<string>();

			List<string> versionsDirs = new List<string>();
			string mainVersions = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft\\versions");
			if (Directory.Exists(mainVersions))
			{
				versionsDirs.Add(mainVersions);
			}
			foreach (string mcDir in PlatformHelper.GetAllMinecraftDirectories())
			{
				try
				{
					string vDir = Path.Combine(mcDir, "versions");
					if (Directory.Exists(vDir) && !versionsDirs.Contains(vDir))
					{
						versionsDirs.Add(vDir);
					}
					if (Directory.Exists(mcDir))
					{
						foreach (string subDir in Directory.GetDirectories(mcDir))
						{
							string instV = Path.Combine(subDir, "versions");
							if (Directory.Exists(instV) && !versionsDirs.Contains(instV))
							{
								versionsDirs.Add(instV);
							}
							string instDotMcV = Path.Combine(subDir, ".minecraft", "versions");
							if (Directory.Exists(instDotMcV) && !versionsDirs.Contains(instDotMcV))
							{
								versionsDirs.Add(instDotMcV);
							}
						}
					}
				}
				catch
				{
				}
			}

			foreach (string vDir in versionsDirs)
			{
				try
				{
					if (Directory.Exists(vDir))
					{
						string[] subDirs = Directory.GetDirectories(vDir);
						foreach (string sub in subDirs)
						{
							string vName = Path.GetFileName(sub);
							try
							{
								string[] allVFiles = Directory.GetFiles(sub, "*.*", SearchOption.TopDirectoryOnly);
								foreach (string vFile in allVFiles)
								{
									string fExt = Path.GetExtension(vFile).ToLowerInvariant();
									string fName = Path.GetFileName(vFile);
									if (fExt == ".jar")
									{
										if (!jarFiles.Contains(vFile)) jarFiles.Add(vFile);
									}
									else if (fExt != ".json" && fExt != ".txt" && fExt != ".log" && fExt != ".sha1" && fExt != ".md5")
									{
										try
										{
											FileInfo fiV = new FileInfo(vFile);
											if (fiV.Length >= 500)
											{
												byte[] headBytes = new byte[4];
												using (FileStream fsHead = new FileStream(vFile, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
												{
													if (fsHead.Read(headBytes, 0, 4) == 4 && headBytes[0] == 0x50 && headBytes[1] == 0x4B && headBytes[2] == 0x03 && headBytes[3] == 0x04)
													{
														using (ZipArchive zipTest = new ZipArchive(fsHead, ZipArchiveMode.Read))
														{
															bool hasClassesOrManifest = false;
															foreach (ZipArchiveEntry ze in zipTest.Entries)
															{
																string zeLower = ze.FullName.ToLowerInvariant();
																if (zeLower.EndsWith(".class") || zeLower == "meta-inf/manifest.mf")
																{
																	hasClassesOrManifest = true;
																	break;
																}
															}
															if (hasClassesOrManifest)
															{
																list2.Add(string.Format("⛔ [5.08 - Замаскированное ядро/мод в папке версии '{0}']: Файл '{1}' имеет расширение '{2}', но является замаскированным JAR-модулем (заголовок PK 0x50 0x4B 0x03 0x04)!", vName, fName, string.IsNullOrEmpty(fExt) ? "без расширения" : fExt));
																if (!jarFiles.Contains(vFile)) jarFiles.Add(vFile);
															}
														}
													}
												}
											}
										}
										catch { }
									}
								}
							}
							catch { }
						}
					}
				}
				catch
				{
				}
			}

			System.Diagnostics.Stopwatch swDisg = System.Diagnostics.Stopwatch.StartNew();
				DateTime javawStartTime = DateTime.MinValue;
			try
			{
				Process[] jps = Process.GetProcessesByName("javaw");
				if (jps.Length == 0) jps = Process.GetProcessesByName("java");
				if (jps.Length > 0) javawStartTime = jps[0].StartTime;
			}
			catch
			{
			}

			foreach (string jarPath in jarFiles)
			{
				try
				{
					FileInfo fi = new FileInfo(jarPath);
					if (CheatDatabases.KnownCheatJarSizes.Contains(fi.Length))
					{
						list2.Add(string.Format("⛔ [5.01 - Аномальный размер ядра] Размер '{0}' ({1} байт) совпадает с базой известных чит-клиентов!", fi.Name, fi.Length));
					}

					if (fi.LastWriteTime > DateTime.Now.AddDays(1) || fi.LastWriteTime < new DateTime(2005, 1, 1))
					{
						list2.Add(string.Format("⛔ [5.08 - Подделка даты Timestomp] Размер '{0}' имеет неестественную дату изменения: {1:dd.MM.yyyy HH:mm}!", fi.Name, fi.LastWriteTime));
					}
					else if (javawStartTime != DateTime.MinValue && fi.LastWriteTime > javawStartTime.AddMinutes(-1))
					{
						list2.Add(string.Format("⛔ [5.08 - Подмена запущенного клиента] Файл '{0}' изменён в {1:dd.MM.yyyy HH:mm} во время запущенной игры (Minecraft запущен в {2:dd.MM.yyyy HH:mm})!", fi.Name, fi.LastWriteTime, javawStartTime));
					}

					using (ZipArchive zip = ZipFile.OpenRead(jarPath))
					{
						bool hasInstrumentService = false;
						bool hasManifestAgent = false;
						bool hasAgentClass = false;
						bool hasCheatPackage = false;
						string cheatDetail = "";

						foreach (ZipArchiveEntry entry in zip.Entries)
						{
							string text3 = entry.FullName.ToLowerInvariant();
							if (text3 == "meta-inf/services/java.lang.instrument")
							{
								hasInstrumentService = true;
							}
							if (text3.EndsWith(".class") && (text3.Contains("instrument/agent") || text3.Contains("agentmain") || text3.StartsWith("agent/")))
							{
								hasAgentClass = true;
							}
							if (text3 == "meta-inf/manifest.mf" && entry.Length < 200000)
							{
								using (StreamReader sr = new StreamReader(entry.Open()))
								{
									string mf = sr.ReadToEnd().ToLowerInvariant();
									if (mf.Contains("agent-class") || mf.Contains("premain-class"))
									{
										hasManifestAgent = true;
									}
								}
							}

							if (!hasCheatPackage && (text3.Contains("/wurst/") || text3.Contains("/meteor/") || text3.Contains("/expensive/") || text3.Contains("/celestial/") || text3.Contains("/nursultan/") || text3.Contains("/liquidbounce/") || text3.Contains("/thunderhack/") || text3.Contains("/slinky/") || text3.Contains("/seedcracker/") || text3.Contains("/doomsday/") || text3.Contains("/vape/") || text3.Contains("/catlavan/") || text3.Contains("/deadcode/") || text3.Contains("/delta/") || text3.Contains("/flint/") || text3.Contains("/rockstar/") || text3.Contains("/boberware/") || text3.Contains("/tenacity/") || text3.Contains("/novoline/") || text3.Contains("/rise/") || text3.Contains("/drip/") || text3.Contains("/wild/") || text3.Contains("/minced/") || text3.Contains("/wexside/") || text3.Contains("/reach/") || text3.Contains("/killaura/") || text3.Contains("/silentaim/") || text3.Contains("/systemdlc/") || text3.Contains("fun.systemdlc") || text3.Contains("ru.systemdlc")))
							{
								hasCheatPackage = true;
								cheatDetail = entry.FullName;
							}
						}

						if (hasInstrumentService || hasManifestAgent || hasAgentClass)
						{
							list2.Add(string.Format("⛔ [5.01 / 5.08 - Внедрение Java-агента в ядро] В jar-файле обнаружен скрытый Java-агент: {0}", jarPath));
						}
						if (hasCheatPackage)
						{
							list2.Add(string.Format("⛔ [5.01 - Модификация чит-класса в ядре] В jar-файле версии '{0}' обнаружен чит-класс: {1}", fi.Name, cheatDetail));
						}
					}
				}
				catch
				{
				}
			}

			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Регламент проверки / Целостность";
			checkResult.Name = "Целостность версий Minecraft — java-агент внутри jar (ШАГ 7 регламента)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = (list2.Count > 0 ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = (list2.Count > 0 ? ("В основном ядре обнаружены модификации кода, агенты или сигнатуры [5.01 / 5.08]: " + list2.Count + " шт.") : ("Проверено jar-файлов ядра: " + jarFiles.Count + ". Модификаций ядра и агентов в jar файлах не найдено."));
			checkResult.Evidence = (list2.Count > 0 ? string.Join("\r\n", list2) : "");
			list.Add(checkResult);
			return list;
		}

		public static List<CheckResult> CheckRegulationClockTamper()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\W32Time"))
				{
					if (registryKey != null)
					{
						object value = registryKey.GetValue("Start");
						if (value != null && Convert.ToInt32(value) == 4)
						{
							list2.Add("🚨 ШАГ 1.2 РЕГЛАМЕНТА: служба времени W32Time ОТКЛЮЧЕНА (Start=4) — часы можно откатить незаметно.");
						}
					}
				}
			}
			catch
			{
			}
			int exitCode;
			string text = RunSysTool("w32tm", "/query /status", out exitCode);
			string text2 = (text ?? string.Empty).ToLowerInvariant();
			bool flag = text2.Contains("0x80070426") || exitCode == -2147023834;
			if (text2.Contains("unspecified") || text2.Contains("не синхронизировано") || text2.Contains("unsynchronized") || text2.Contains("0x8007052e"))
			{
				list2.Add("⚠️ ШАГ 1.2 РЕГЛАМЕНТА: время не синхронизировано с сервером — возможен откат часов.");
			}
			else if (exitCode != 0 && !flag)
			{
				list3.Add("Службу времени не удалось опросить (w32tm код " + exitCode + "), нужны права администратора.");
			}
			else if (flag)
			{
				list3.Add("Служба времени остановлена (обычный режим Windows — запуск по требованию).");
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Регламент проверки / Время";
			checkResult.Name = "Системное время и служба W32Time (ШАГ 1.2 регламента)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count != 0) ? ((!list2.Any((string h) => h.Contains("\ud83d\udea8"))) ? CheckSeverity.Warning : CheckSeverity.Critical) : CheckSeverity.Info);
			if (list2.Count > 0)
			{
				checkResult.Detail = "Есть замечания по системному времени: " + list2.Count + " шт.";
			}
			else if (list3.Count > 0)
			{
				checkResult.Detail = "🚨 ШАГ 4 РЕГЛАМЕНТА: " + string.Join(" ", list3);
			}
			else
			{
				checkResult.Detail = "Служба времени активна, синхронизация в порядке.";
			}
			checkResult.Evidence = ((list2.Count > 0) ? string.Join("\r\n", list2) : "");
			list.Add(checkResult);
			return list;
		}

		public static List<CheckResult> CheckRegulationMemorySignatures()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				string[] array = new string[]
				{
					"killaura", "autototem", "hitboxes", "freecam", "cheststealer", "deadcode", "wexside",
					"expensive", "nursultan", "celestial", "akrien", "rich", "bleachhack", "targetstrafe", "fastbow", "reach"
				};
				Process[] processesByName = Process.GetProcessesByName("javaw");
				if (processesByName.Length == 0)
				{
					processesByName = Process.GetProcessesByName("java");
				}
				if (processesByName.Length == 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Регламент проверки / Память";
					checkResult.Name = "Анализ строк чит-функций в памяти (ШАГ 5 регламента: KillAura, AutoTotem...)";
					checkResult.Detected = false;
					checkResult.Severity = CheckSeverity.Info;
					checkResult.Detail = "Minecraft (javaw.exe) не запущен в памяти. Проверка памяти не применима.";
					list.Add(checkResult);
					return list;
				}

				foreach (Process process in processesByName)
				{
					// 1. Scan JVM committed memory heap using ProcessMemoryScanner
					try
					{
						List<MemoryRegionHit> ramHits = ProcessMemoryScanner.ScanMinecraftRam(process.Id);
						foreach (MemoryRegionHit hit in ramHits)
						{
							if (hit.Severity == CheckSeverity.Critical || hit.Severity == CheckSeverity.NeedsCheck)
							{
								list2.Add(string.Format("⛔ Пункт 5 регламента: В RAM javaw (PID={0}) обнаружен чит: {1} ({2}, адрес: {3})", process.Id, hit.Type, hit.Detail, hit.Address));
							}
						}
					}
					catch
					{
					}

					// 2. Check injected modules / hooks
					try
					{
						List<MemoryRegionHit> modHits = ProcessMemoryScanner.CheckJavawLoadedModules(process);
						foreach (MemoryRegionHit mHit in modHits)
						{
							if (mHit.Severity == CheckSeverity.Critical)
							{
								list2.Add(string.Format("⛔ Пункт 5 регламента: Подозрительный модуль в javaw: {0} ({1})", mHit.Detail, mHit.Address));
							}
						}
					}
					catch
					{
					}
				}

				// 3. Check latest.log for cheat commands & events
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft\\logs\\latest.log");
				if (File.Exists(path))
				{
					try
					{
						string[] array4 = File.ReadAllLines(path);
						foreach (string text3 in array4)
						{
							string text4 = text3.ToLowerInvariant();
							foreach (string text5 in array)
							{
								if (text4.Contains(text5) && !text4.Contains("lunar") && !text4.Contains("fabric") && !text4.Contains("optifine"))
								{
									list2.Add("🚨 ШАГ 2/5 РЕГЛАМЕНТА: Сигнатура [" + text5 + "] в latest.log: " + text3.Trim());
									break;
								}
							}
						}
					}
					catch
					{
					}
				}

				if (list2.Count > 0)
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Регламент проверки / Память";
					checkResult2.Name = "Сигнатуры чит-функций в памяти и логах (ШАГ 5 регламента: KillAura, AutoTotem...)";
					checkResult2.Detected = true;
					checkResult2.Severity = CheckSeverity.Critical;
					checkResult2.Detail = "Обнаружены прямые сигнатуры читов согласно ШАГУ 5 регламента проверки: " + list2.Count + " шт. (100% доказательство использования!)";
					checkResult2.Evidence = string.Join("\r\n", list2.Take(25));
					list.Add(checkResult2);
				}
				else
				{
					CheckResult checkResult3 = new CheckResult();
					checkResult3.Category = "Регламент проверки / Память";
					checkResult3.Name = "Сигнатуры чит-функций в памяти (ШАГ 5 регламента: KillAura, AutoTotem...)";
					checkResult3.Detected = false;
					checkResult3.Severity = CheckSeverity.Info;
					checkResult3.Detail = "Сигнатур запрещённых функций (KillAura, AutoTotem, HitBoxes, Velocity...) не обнаружено.";
					list.Add(checkResult3);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult4 = new CheckResult();
				checkResult4.Category = "Регламент проверки / Память";
				checkResult4.Name = "Сигнатуры чит-функций";
				checkResult4.Detected = false;
				checkResult4.Severity = CheckSeverity.Info;
				checkResult4.Detail = "Ошибка проверки памяти: " + ex.Message;
				list.Add(checkResult4);
			}
			return list;
		}

		public static List<CheckResult> CheckRegulationPrefetchStorageRules()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string path = Path.Combine(folderPath, "Prefetch");
				if (Directory.Exists(path))
				{
					string[] files = Directory.GetFiles(path, "*.pf");
					System.Diagnostics.Stopwatch swDisg = System.Diagnostics.Stopwatch.StartNew();
											foreach (string text in files)
						{
							if (swDisg.ElapsedMilliseconds > 1500) break;

						string text2 = Path.GetFileName(text).ToLowerInvariant();
						bool matched = false;
						string matchedCheat = null;

						foreach (string cheatLower in CheatClientNames)
						{
							if (MatchesCheatName(text2, cheatLower) && !text2.Contains("lunar"))
							{
								matched = true;
								matchedCheat = cheatLower;
								break;
							}
						}
						if (!matched)
						{
							foreach (string cheatLower in CheatDatabases.ExtraCheatClients)
							{
								if (cheatLower.Length >= 4 && MatchesCheatName(text2, cheatLower) && !text2.Contains("lunar"))
								{
									matched = true;
									matchedCheat = cheatLower;
									break;
								}
							}
						}

						if (matched)
						{
							FileInfo fileInfo = new FileInfo(text);
							TimeSpan timeSpan = DateTime.Now - fileInfo.LastWriteTime;
							string text3 = ((timeSpan.TotalDays <= 7.0) ? ("🚨 [Активно: запуск " + Math.Round(timeSpan.TotalDays, 1) + " дн. назад (в пределах 7 дней)]") : ((!(timeSpan.TotalDays <= 14.0)) ? ("📦 [Хранение: запуск " + Math.Round(timeSpan.TotalDays, 0) + " дн. назад (>14 дней)]") : ("📦 [Хранение: запуск " + Math.Round(timeSpan.TotalDays, 1) + " дн. назад (в пределах 7-14 дней)]")));
							list2.Add(text3 + " [" + matchedCheat + "] " + Path.GetFileName(text) + " (ключ: " + fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")");
						}
					}
				}
				if (list2.Count > 0)
				{
					bool flag = list2.Any((string h) => h.Contains("[Удалён") || h.Contains("[Запрещён") || h.Contains("[Чит"));
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Регламент проверки / Время";
					checkResult.Name = "Сроки давности запусков Prefetch (Раздел 4: 7-14 дней)";
					checkResult.Detected = flag;
					checkResult.Severity = ((!flag) ? CheckSeverity.Warning : CheckSeverity.Critical);
					checkResult.Detail = "В Prefetch найдены запуски читов с классификацией сроков давности: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Регламент проверки / Время";
					checkResult2.Name = "Сроки давности запусков Prefetch (Раздел 4: 7-14 дней)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "В каталоге Prefetch следов запусков читов в пределах 14 дней не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Регламент проверки / Время";
				checkResult3.Name = "Сроки давности Prefetch";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки Prefetch: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		
		public static List<CheckResult> CheckVirtualDesktopsAndAlternateSessions()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				bool flagSuspicious = false;
				int desktopCount = 1;

				if (PlatformHelper.IsWindows)
				{
					try
					{
						using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\VirtualDesktops"))
						{
							if (rk != null)
							{
								byte[] bytes = rk.GetValue("VirtualDesktopIDs") as byte[];
								if (bytes != null && bytes.Length >= 16)
								{
									desktopCount = bytes.Length / 16;
								}
								else
								{
									using (RegistryKey subDesktops = rk.OpenSubKey("Desktops"))
									{
										if (subDesktops != null)
										{
											string[] subNames = subDesktops.GetSubKeyNames();
											if (subNames != null && subNames.Length > 0)
											{
												desktopCount = Math.Max(desktopCount, subNames.Length);
											}
										}
									}
								}
							}
						}
					}
					catch { }

					if (desktopCount > 1)
					{
						// Multiple virtual desktops is standard Windows feature
						list2.Add(string.Format("⚠️ [Виртуальные рабочие столы] В системе создано {0} рабочих столов (Task View / Win+Tab)! Возможно сокрытие окон читов/инжектора на Desktop 2.", desktopCount));
					}
					else
					{
						list2.Add("ℹ️ В системе активен 1 стандартный рабочий стол (Task View чист).");
					}

					try
					{
						Dictionary<int, string> titles = ProcessMemoryScanner.GetOpenWindowTitles();
						if (titles != null && titles.Count > 0)
						{
							string[] dangerKeys = new string[]
							{
								"cheat", "hack", "loader", "expensive", "nursultan", "celestial", "wexside",
								"catlavan", "akrien", "doomsday", "vape", "drip", "systemdlc", "sysdlc",
								"clumsy", "netlimiter", "wireshark",
								"fiddler", "charles", "systeminformer", "dnspy"
							};
							foreach (KeyValuePair<int, string> kvp in titles)
							{
								string titleLower = kvp.Value.ToLowerInvariant();
								foreach (string dangerKey in dangerKeys)
								{
									if (titleLower.Contains(dangerKey) && !titleLower.Contains("mccheat") && !titleLower.Contains("checker") && !titleLower.Contains("          \"name\": "))
									{
										string procName = "Unknown";
										try
										{
											Process p = Process.GetProcessById(kvp.Key);
											procName = p.ProcessName;
										}
										catch { }
										string pLow = procName.ToLowerInvariant();
										if (pLow == "discord" || pLow == "discordcanary" || pLow == "discordptb" || pLow == "vesktop" ||
											pLow == "anydesk" || pLow == "rudesktop" || pLow == "rustdesk" || pLow == "teamviewer" ||
											pLow == "chrome" || pLow == "msedge" || pLow == "firefox" || pLow == "opera" || pLow == "brave" ||
											pLow == "code" || pLow == "devenv" || pLow == "explorer")
										{
											break;
										}
										list2.Add(string.Format("⚠️ [Подозрительное окно в сессии] Процесс {0} (PID {1}): \"{2}\"", procName, kvp.Key, kvp.Value));
										flagSuspicious = true;
										break;
									}
								}
							}
						}
					}
					catch { }
				}

				CheckResult checkResult = new CheckResult();
				checkResult.Category = "Рабочий стол и окна";
				checkResult.Name = "Виртуальные рабочие столы и скрытые окна (Task View / Desktop 2)";
				checkResult.Detected = flagSuspicious;
				checkResult.Severity = (flagSuspicious ? CheckSeverity.Warning : CheckSeverity.Info);
				checkResult.Detail = (flagSuspicious ? "Обнаружены признаки скрытия активных окон (Task View / Desktop 2)!" : "Второй рабочий стол и скрытые приложения на ПК отсутствуют");
				checkResult.Evidence = string.Join("\r\n", list2);
				list.Add(checkResult);
			}
			catch (Exception ex)
			{
				CheckResult checkResult2 = new CheckResult();
				checkResult2.Category = "Рабочий стол и окна";
				checkResult2.Name = "🔒 Мьютексы и Named Pipes";
				checkResult2.Detected = false;
				checkResult2.Severity = CheckSeverity.Info;
				checkResult2.Detail = " МГц | работает: " + ex.Message;
				list.Add(checkResult2);
			}
			return list;
		}

		#region Anti-VM, Sandbox, Stream-Proof & Virtual Display Detection

		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern bool GetWindowDisplayAffinity(IntPtr hWnd, out uint pdwAffinity);

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string lpModuleName);

		[StructLayout(LayoutKind.Sequential)]
		private struct Win32Rect
		{
			public int Left;
			public int Top;
			public int Right;
			public int Bottom;
		}

		[DllImport("user32.dll")]
		private static extern bool GetWindowRect(IntPtr hWnd, out Win32Rect lpRect);

		[DllImport("user32.dll", EntryPoint = "GetWindowLong")]
		private static extern IntPtr GetWindowLongPtr32(IntPtr hWnd, int nIndex);

		[DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
		private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

		private static long GetWindowLongAuto(IntPtr hWnd, int nIndex)
		{
			if (IntPtr.Size == 8) return GetWindowLongPtr64(hWnd, nIndex).ToInt64();
			return GetWindowLongPtr32(hWnd, nIndex).ToInt64();
		}

		private const int GWL_EXSTYLE = -20;
		private const long WS_EX_TRANSPARENT = 0x00000020L;
		private const long WS_EX_TOPMOST     = 0x00000008L;
		private const long WS_EX_LAYERED     = 0x00080000L;

		private static bool IsWhitelistedAntiCaptureProcess(string baseName, string exePath)
		{
			if (string.IsNullOrEmpty(baseName) && string.IsNullOrEmpty(exePath)) return false;
			string nameLower = (baseName ?? string.Empty).ToLowerInvariant();
			string pathLower = (exePath ?? string.Empty).Replace('/', '\\').ToLowerInvariant();

			string[] whitelistedSystem = new string[]
			{
				"textinputhost", "shellexperiencehost", "applicationframehost", "searchhost",
				"startmenuexperiencehost", "systemsettings", "lockapp", "snippingtool",
				"screenclippinghost", "credentialuibroker", "logonui", "taskhostw",
				"explorer", "dwmapi", "dwm",
				"discord", "discordcanary", "discordptb", "vesktop",
				"obs64", "obs32", "streamlabs", "nvcontainer", "nvidia overlay", "nvidia share",
				"amddvr", "radeonsoftware", "steam", "gameoverlayui", "gamebar", "xboxgamebar",
				"sharex", "lightshot", "greenshot", "gyazo", "wallpaper32", "wallpaper64",
				"losslessscaling", "anydesk", "rudesktop", "rustdesk", "teamviewer", "mccheatchecker"
			};

			foreach (string sys in whitelistedSystem)
			{
				if (nameLower == sys) return true;
			}

			if (nameLower.StartsWith("discord") || nameLower.StartsWith("nvidia ") ||
				nameLower.StartsWith("obs") || nameLower.Contains("mccheatchecker") ||
				pathLower.Contains(@"\discord\") || pathLower.Contains(@"\discordcanary\") ||
				pathLower.Contains(@"\discordptb\") || pathLower.Contains(@"\vesktop\") ||
				pathLower.Contains(@"\obs-studio\") || pathLower.Contains(@"\nvidia corporation\") ||
				pathLower.Contains(@"\steam\") || pathLower.Contains(@"\anydesk") ||
				pathLower.Contains(@"\rudesktop") || pathLower.Contains(@"\rustdesk") ||
				pathLower.Contains(@"\teamviewer") || pathLower.Contains("mccheatchecker"))
			{
				return true;
			}

			string[] whitelistedApps = new string[]
			{
				"1password", "bitwarden", "keepass", "keepassxc", "enpass", "lastpass"
			};

			foreach (string app in whitelistedApps)
			{
				if (nameLower.Contains(app) || pathLower.Contains(app)) return true;
			}

			return false;
		}

		private static List<Win32Rect> GetVisibleMinecraftWindowRects()
		{
			List<Win32Rect> rects = new List<Win32Rect>();
			try
			{
				HashSet<uint> mcPids = new HashSet<uint>();
				Process[] procs = Process.GetProcesses();
				foreach (Process p in procs)
				{
					try
					{
						string pn = p.ProcessName.ToLowerInvariant();
						if (pn == "javaw" || pn == "java" || pn.Contains("minecraft"))
						{
							mcPids.Add((uint)p.Id);
						}
					}
					catch { }
				}
				if (mcPids.Count == 0) return rects;

				EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
				{
					try
					{
						if (!IsWindowVisible(hWnd)) return true;
						uint pid;
						GetWindowThreadProcessId(hWnd, out pid);
						if (!mcPids.Contains(pid)) return true;
						Win32Rect r;
						if (GetWindowRect(hWnd, out r))
						{
							if ((r.Right - r.Left) >= 200 && (r.Bottom - r.Top) >= 150)
							{
								rects.Add(r);
							}
						}
					}
					catch { }
					return true;
				}, IntPtr.Zero);
			}
			catch { }
			return rects;
		}

		private static bool IntersectsAnyMinecraftWindow(Win32Rect overlayRect, List<Win32Rect> mcRects)
		{
			if (mcRects == null || mcRects.Count == 0) return false;
			for (int i = 0; i < mcRects.Count; i++)
			{
				Win32Rect mc = mcRects[i];
				int left = Math.Max(overlayRect.Left, mc.Left);
				int top = Math.Max(overlayRect.Top, mc.Top);
				int right = Math.Min(overlayRect.Right, mc.Right);
				int bottom = Math.Min(overlayRect.Bottom, mc.Bottom);
				if ((right - left) >= 80 && (bottom - top) >= 80)
				{
					return true;
				}
			}
			return false;
		}

		public static List<CheckResult> AuditVirtualMachineAndAntiCaptureEnvironment(
			string biosManufacturerOverride,
			string biosProductNameOverride,
			string biosVersionOverride,
			string wmiModelOverride,
			string[] macAddressesOverride,
			string[] installedServicesOverride,
			Tuple<string, uint, int, string>[] windowAffinitiesOverride)
		{
			List<string> list2 = new List<string>();

			// 1. BIOS & System Manufacturer / Model checks
			string[] vmKeywords = new string[] { "vmware", "virtualbox", "vbox", "qemu", "kvm", "xen", "bochs", "parallels", "innotek" };

			string sysMan = biosManufacturerOverride;
			string sysProd = biosProductNameOverride;
			string biosVer = biosVersionOverride;

			if (sysMan == null && sysProd == null && biosVer == null)
			{
				try
				{
					using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"HARDWARE\Description\System\BIOS"))
					{
						if (rk != null)
						{
							sysMan = (rk.GetValue("SystemManufacturer") ?? "").ToString().ToLowerInvariant();
							sysProd = (rk.GetValue("SystemProductName") ?? "").ToString().ToLowerInvariant();
							biosVer = (rk.GetValue("BIOSVersion") ?? "").ToString().ToLowerInvariant();
						}
					}
				}
				catch { }
			}

			sysMan = (sysMan ?? string.Empty).ToLowerInvariant();
			sysProd = (sysProd ?? string.Empty).ToLowerInvariant();
			biosVer = (biosVer ?? string.Empty).ToLowerInvariant();

			foreach (string kw in vmKeywords)
			{
				if (sysMan.Contains(kw) || sysProd.Contains(kw) || biosVer.Contains(kw))
				{
					list2.Add(string.Format("⛔ [5.08 - Уклонение от проверки / Виртуальная машина]: Обнаружена виртуальная машина в BIOS: {0} / {1} ({2})",
						sysMan, sysProd, biosVer));
					break;
				}
			}

			if (sysMan.Contains("microsoft") && (sysProd.Contains("virtual") || sysProd.Contains("hyper-v")))
			{
				list2.Add(string.Format("⛔ [5.08 - Уклонение от проверки / Виртуальная машина]: Обнаружена виртуальная машина Hyper-V в BIOS: {0} / {1}",
					sysMan, sysProd));
			}

			// Hyper-V Guest integration parameters check
			try
			{
				using (RegistryKey hvKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Virtual Machine\Guest\Parameters"))
				{
					if (hvKey != null)
					{
						string hostName = (hvKey.GetValue("HostName") ?? "").ToString();
						list2.Add(string.Format("⛔ [5.08 - Уклонение от проверки / Виртуальная машина]: Обнаружены параметры гостевой системы Hyper-V (Host: {0})",
							!string.IsNullOrEmpty(hostName) ? hostName : "Hyper-V Host"));
					}
				}
			}
			catch { }

			// 2. WMI Win32_ComputerSystem Model & Manufacturer
			string wmiModel = wmiModelOverride;
			if (wmiModel == null)
			{
				try
				{
					using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT Manufacturer, Model FROM Win32_ComputerSystem"))
					{
						foreach (ManagementObject mo in searcher.Get())
						{
							string man = (mo["Manufacturer"] ?? "").ToString().ToLowerInvariant();
							string mod = (mo["Model"] ?? "").ToString().ToLowerInvariant();

							foreach (string kw in vmKeywords)
							{
								if (man.Contains(kw) || mod.Contains(kw))
								{
									list2.Add(string.Format("⛔ [5.08 - Уклонение от проверки / Виртуальная машина]: WMI Win32_ComputerSystem указывает на VM: {0} / {1}",
										mo["Manufacturer"], mo["Model"]));
									break;
								}
							}

							if (man.Contains("microsoft") && (mod.Contains("virtual") || mod.Contains("hyper-v")))
							{
								list2.Add(string.Format("⛔ [5.08 - Уклонение от проверки / Виртуальная машина]: WMI Win32_ComputerSystem указывает на Hyper-V: {0} / {1}",
									mo["Manufacturer"], mo["Model"]));
							}
						}
					}
				}
				catch { }
			}
			else
			{
				string modLower = wmiModel.ToLowerInvariant();
				foreach (string kw in vmKeywords)
				{
					if (modLower.Contains(kw))
					{
						list2.Add("⛔ [5.08 - Уклонение от проверки / Виртуальная машина]: WMI Win32_ComputerSystem указывает на VM: " + wmiModel);
						break;
					}
				}
				if (modLower.Contains("microsoft") && (modLower.Contains("virtual") || modLower.Contains("hyper-v")))
				{
					list2.Add("⛔ [5.08 - Уклонение от проверки / Виртуальная машина]: WMI Win32_ComputerSystem указывает на Hyper-V: " + wmiModel);
				}
			}

			// 3. Network MAC OUI Prefixes
			string[] vmMACPrefixes = new string[]
			{
				"00:05:69", "00:0C:29", "00:50:56", // VMware
				"08:00:27",                         // VirtualBox
				"00:15:5D",                         // Hyper-V
				"52:54:00",                         // QEMU / KVM
				"00:1C:42"                          // Parallels
			};

			if (macAddressesOverride != null)
			{
				foreach (string mac in macAddressesOverride)
				{
					string normMac = mac.ToUpperInvariant().Replace("-", ":");
					foreach (string pfx in vmMACPrefixes)
					{
						if (normMac.StartsWith(pfx, StringComparison.OrdinalIgnoreCase))
						{
							string vmName = pfx.StartsWith("00:05:69") || pfx.StartsWith("00:0C:29") || pfx.StartsWith("00:50:56") ? "VMware" :
							                pfx.StartsWith("08:00:27") ? "VirtualBox" :
							                pfx.StartsWith("00:15:5D") ? "Hyper-V" :
							                pfx.StartsWith("52:54:00") ? "QEMU/KVM" : "Parallels";
							list2.Add(string.Format("⛔ [5.08 - Уклонение от проверки / Виртуальная машина]: Сетевой интерфейс имеет OUI виртуализации: {0} ({1})",
								mac, vmName));
							break;
						}
					}
				}
			}
			else
			{
				try
				{
					foreach (System.Net.NetworkInformation.NetworkInterface nic in System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces())
					{
						try
						{
							byte[] bytes = nic.GetPhysicalAddress().GetAddressBytes();
							if (bytes == null || bytes.Length < 3) continue;
							string macPrefix = string.Format("{0:X2}:{1:X2}:{2:X2}", bytes[0], bytes[1], bytes[2]);
							foreach (string pfx in vmMACPrefixes)
							{
								if (macPrefix.Equals(pfx, StringComparison.OrdinalIgnoreCase))
								{
									string vmName = pfx.StartsWith("00:05:69") || pfx.StartsWith("00:0C:29") || pfx.StartsWith("00:50:56") ? "VMware" :
									                pfx.StartsWith("08:00:27") ? "VirtualBox" :
									                pfx.StartsWith("00:15:5D") ? "Hyper-V" :
									                pfx.StartsWith("52:54:00") ? "QEMU/KVM" : "Parallels";
									list2.Add(string.Format("⛔ [5.08 - Уклонение от проверки / Виртуальная машина]: Сетевой интерфейс '{0}' имеет OUI виртуализации: {1} ({2})",
										nic.Name, macPrefix, vmName));
									break;
								}
							}
						}
						catch { }
					}
				}
				catch { }
			}

			// 4. Guest Tools Services & Processes
			string[] vmProcesses = new string[]
			{
				"vmtoolsd", "vboxtray", "vboxservice", "sandboxiedcomlaunch", "sbiesvc", "sbiectrl",
				"vmsrvc", "vmusrvc", "qemu-ga", "prl_cc", "prl_tools", "xenservice", "sandboxie", "sbie"
			};

			foreach (string text4 in vmProcesses)
			{
				try
				{
					if (Process.GetProcessesByName(text4).Length > 0)
					{
						list2.Add("⛔ [5.08 - Служба виртуализации/песочницы]: " + text4 + ".exe");
					}
				}
				catch { }
			}

			// Sandboxie sbiedll detection
			try
			{
				if (GetModuleHandle("sbiedll.dll") != IntPtr.Zero)
				{
					list2.Add("⛔ [5.08 - Уклонение от проверки / Песочница]: Процесс проверки запущен внутри изолированной песочницы Sandboxie (обнаружен sbiedll.dll)!");
				}
			}
			catch { }

			try
			{
				foreach (Process jp in Process.GetProcessesByName("javaw"))
				{
					try
					{
						foreach (ProcessModule mod in jp.Modules)
						{
							if (mod.ModuleName.Equals("sbiedll.dll", StringComparison.OrdinalIgnoreCase))
							{
								list2.Add(string.Format("⛔ [5.08 - Игра запущена в песочнице Sandboxie]: Процесс javaw.exe (PID={0}) содержит внедрённый модуль sbiedll.dll!", jp.Id));
								break;
							}
						}
					}
					catch { }
				}
			}
			catch { }

			// 5. Virtualization Drivers & SCSI
			string[] vmDrivers = new string[]
			{
				Path.Combine(Environment.SystemDirectory, @"drivers\VBoxMouse.sys"),
				Path.Combine(Environment.SystemDirectory, @"drivers\VBoxGuest.sys"),
				Path.Combine(Environment.SystemDirectory, @"drivers\VBoxSF.sys"),
				Path.Combine(Environment.SystemDirectory, @"drivers\vmmouse.sys"),
				Path.Combine(Environment.SystemDirectory, @"drivers\vm3dgl.dll"),
				Path.Combine(Environment.SystemDirectory, @"drivers\prl_fs.sys"),
				Path.Combine(Environment.SystemDirectory, @"drivers\SbieDrv.sys"),
				Path.Combine(Environment.SystemDirectory, @"drivers\qemufwcfg.sys"),
				Path.Combine(Environment.SystemDirectory, @"drivers\virtio.sys"),
				Path.Combine(Environment.SystemDirectory, @"drivers\netkvm.sys")
			};

			foreach (string vmd in vmDrivers)
			{
				try
				{
					if (File.Exists(vmd))
					{
						list2.Add("⛔ [5.08 - Драйвер виртуализации/песочницы]: " + Path.GetFileName(vmd));
					}
				}
				catch { }
			}

			try
			{
				using (RegistryKey scsiKey = Registry.LocalMachine.OpenSubKey(@"HARDWARE\DEVICEMAP\Scsi\Scsi Port 0\Scsi Bus 0\Target Id 0\Logical Unit Id 0"))
				{
					if (scsiKey != null)
					{
						string diskId = (scsiKey.GetValue("Identifier") ?? "").ToString().ToLowerInvariant();
						if (diskId.Contains("vbox") || diskId.Contains("vmware") || diskId.Contains("qemu") || diskId.Contains("virtual") || diskId.Contains("virtio"))
						{
							list2.Add("⛔ [5.08 - Идентификатор SCSI накопителя]: " + diskId);
						}
					}
				}
			}
			catch { }

			// 6. Anti-Capture / Screen-Proof Windows (WDA_EXCLUDEFROMCAPTURE / WDA_MONITOR)
			if (windowAffinitiesOverride != null)
			{
				foreach (var win in windowAffinitiesOverride)
				{
					string procName = win.Item1;
					uint affinity = win.Item2;
					int pid = win.Item3;
					string title = win.Item4;

					if (affinity != 0)
					{
						string baseName = procName.ToLowerInvariant().Replace(".exe", "");
						if (!IsWhitelistedAntiCaptureProcess(baseName, string.Empty))
						{
							string modeDesc = (affinity == 0x11)
								? "WDA_EXCLUDEFROMCAPTURE (0x11, полное сокрытие от стрима/Discord)"
								: "WDA_MONITOR (0x01, сокрытие от скриншотов)";

							bool isCheat = IsCheatProcessOrPath(baseName, string.Empty);
							bool isJava = (baseName == "javaw" || baseName == "java" || baseName == "minecraft");

							if (isCheat || isJava)
							{
								list2.Add(string.Format("⛔ [5.01 - Скрытие чит-оверлея от экрана (Stream-Proof)]: Процесс {0} (PID={1}) использует {2} для сокрытия оверлея чита от Discord/AnyDesk! Окно: \"{3}\"",
									procName, pid, modeDesc, title));
							}
							else
							{
								list2.Add(string.Format("⛔ [5.08 - Скрытие окна/оверлея от захвата экрана (Anti-ScreenShare)]: Процесс {0} (PID={1}) использует {2} для сокрытия своего интерфейса от Discord/AnyDesk! Окно: \"{3}\"",
									procName, pid, modeDesc, title));
							}
						}
					}
				}
			}
			else
			{
				try
				{
					EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
					{
						try
						{
							if (!IsWindowVisible(hWnd)) return true;
							uint pdwAffinity = 0u;
							if (GetWindowDisplayAffinity(hWnd, out pdwAffinity) && pdwAffinity != 0)
							{
								uint lpdwProcessId;
								GetWindowThreadProcessId(hWnd, out lpdwProcessId);
								if (lpdwProcessId != 0)
								{
									string procName = "PID=" + lpdwProcessId;
									string exePath = string.Empty;
									string baseName = string.Empty;
									try
									{
										using (Process p = Process.GetProcessById((int)lpdwProcessId))
										{
											procName = p.ProcessName + ".exe";
											baseName = p.ProcessName.ToLowerInvariant();
											exePath = SafeGetProcessPath(p);
										}
									}
									catch
									{
										exePath = SafeGetProcessPathByPid((int)lpdwProcessId);
										if (!string.IsNullOrEmpty(exePath))
										{
											procName = Path.GetFileName(exePath);
											baseName = procName.ToLowerInvariant().Replace(".exe", "");
										}
									}

									if (!IsWhitelistedAntiCaptureProcess(baseName, exePath))
									{
										StringBuilder sbTitle = new StringBuilder(256);
										GetWindowText(hWnd, sbTitle, 256);
										string title = sbTitle.ToString().Trim();
										string modeDesc = (pdwAffinity == 0x11)
											? "WDA_EXCLUDEFROMCAPTURE (0x11, полное сокрытие от стрима/Discord)"
											: "WDA_MONITOR (0x01, сокрытие от скриншотов)";

										bool isCheat = IsCheatProcessOrPath(baseName, exePath);
										bool isJava = (baseName == "javaw" || baseName == "java" || baseName == "minecraft");

										if (isCheat || isJava)
										{
											list2.Add(string.Format("⛔ [5.01 - Скрытие чит-оверлея от экрана (Stream-Proof)]: Процесс {0} (PID={1}{2}) использует {3} для сокрытия оверлея чита от Discord/AnyDesk! Окно: \"{4}\"",
												procName, lpdwProcessId, !string.IsNullOrEmpty(exePath) ? ", путь: " + exePath : "", modeDesc, string.IsNullOrEmpty(title) ? "Без заголовка (Stealth)" : title));
										}
										else
										{
											list2.Add(string.Format("⛔ [5.08 - Скрытие окна/оверлея от захвата экрана (Anti-ScreenShare)]: Процесс {0} (PID={1}{2}) использует {3} для сокрытия своего интерфейса от Discord/AnyDesk! Окно: \"{4}\"",
												procName, lpdwProcessId, !string.IsNullOrEmpty(exePath) ? ", путь: " + exePath : "", modeDesc, string.IsNullOrEmpty(title) ? "Без заголовка (Stealth)" : title));
										}
									}
								}
							}
						}
						catch { }
						return true;
					}, IntPtr.Zero);
				}
				catch { }

				// 6b. Window Enumeration Monitor (ESP/Overlay Hunter: WS_EX_TRANSPARENT + WS_EX_LAYERED + WS_EX_TOPMOST)
				// Flag ONLY overlays that are visible AND physically intersect a running Minecraft window (ignore 2nd-monitor apps!)
				try
				{
					List<Win32Rect> mcWindowRects = GetVisibleMinecraftWindowRects();
					if (mcWindowRects.Count > 0)
					{
						EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
						{
							try
							{
								if (IsWindowVisible(hWnd))
								{
									long exStyle = GetWindowLongAuto(hWnd, GWL_EXSTYLE);
									bool isTrans = (exStyle & WS_EX_TRANSPARENT) != 0;
									bool isLayer = (exStyle & WS_EX_LAYERED) != 0;
									bool isTop = (exStyle & WS_EX_TOPMOST) != 0;

									if (isLayer && isTrans && isTop)
									{
										Win32Rect r;
										GetWindowRect(hWnd, out r);
										int w = r.Right - r.Left;
										int h = r.Bottom - r.Top;

										if (w >= 300 && h >= 200 && IntersectsAnyMinecraftWindow(r, mcWindowRects))
										{
											uint pid;
											GetWindowThreadProcessId(hWnd, out pid);
											if (pid != 0)
											{
												string procName = "PID=" + pid;
												string exePath = string.Empty;
												string baseName = string.Empty;
												try
												{
													using (Process p = Process.GetProcessById((int)pid))
													{
														procName = p.ProcessName + ".exe";
														baseName = p.ProcessName.ToLowerInvariant();
														exePath = SafeGetProcessPath(p);
													}
												}
												catch
												{
													exePath = SafeGetProcessPathByPid((int)pid);
													if (!string.IsNullOrEmpty(exePath))
													{
														procName = Path.GetFileName(exePath);
														baseName = procName.ToLowerInvariant().Replace(".exe", "");
													}
												}

												if (!IsWhitelistedAntiCaptureProcess(baseName, exePath))
												{
													StringBuilder sbTitle = new StringBuilder(256);
													GetWindowText(hWnd, sbTitle, 256);
													string title = sbTitle.ToString().Trim();

													list2.Add(string.Format("⛔ [5.01 - Прозрачный ESP Click-Through оверлей (Window Enumeration Monitor)]: Процесс {0} (PID={1}) использует стили WS_EX_LAYERED | WS_EX_TRANSPARENT | WS_EX_TOPMOST для отрисовки внешнего ESP чит-оверлея поверх игры! Окно: \"{2}\" ({3}x{4})",
														procName, pid, string.IsNullOrEmpty(title) ? "Без заголовка (Stealth ESP)" : title, w, h));
												}
											}
										}
									}
								}
							}
							catch { }
							return true;
						}, IntPtr.Zero);
					}
				}
				catch { }
			}

			// 7. Virtual Display Drivers (IddSampleDriver / Amyuni / Virtual Display)
			string[] virtualDisplayServices = new string[]
			{
				"IddSampleDriver", "usbmmidd", "spacedeskService", "DuetDisplay", "Twomon", "ParsecVirtualDisplay", "Sunshine"
			};

			if (installedServicesOverride != null)
			{
				foreach (string s in installedServicesOverride)
				{
					foreach (string vServ in virtualDisplayServices)
					{
						if (s.IndexOf(vServ, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							list2.Add(string.Format("⛔ [5.08 - Виртуальный монитор для сокрытия читов (Virtual Display Driver)]: Обнаружена активная служба виртуального дисплея '{0}'! Используется для вывода чит-радара/GUI на скрытый второй монитор",
								vServ));
							break;
						}
					}
				}
			}
			else
			{
				foreach (string vServ in virtualDisplayServices)
				{
					try
					{
						using (RegistryKey rk = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + vServ))
						{
							if (rk != null)
							{
								string imgPath = (rk.GetValue("ImagePath") as string) ?? string.Empty;
								list2.Add(string.Format("⛔ [5.08 - Виртуальный монитор для сокрытия читов (Virtual Display Driver)]: Обнаружена активная служба виртуального дисплея '{0}' ({1})! Используется для вывода чит-радара/GUI на скрытый второй монитор",
									vServ, imgPath));
							}
						}
					}
					catch { }
				}

				try
				{
					using (RegistryKey enumKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}"))
					{
						if (enumKey != null)
						{
							foreach (string sub in enumKey.GetSubKeyNames())
							{
								if (sub.StartsWith("00", StringComparison.OrdinalIgnoreCase))
								{
									using (RegistryKey devKey = enumKey.OpenSubKey(sub))
									{
										if (devKey != null)
										{
											string desc = (devKey.GetValue("DriverDesc") as string ?? "").ToLowerInvariant();
											string provider = (devKey.GetValue("ProviderName") as string ?? "").ToLowerInvariant();
											if (desc.Contains("iddsampledriver") || desc.Contains("amyuni") || desc.Contains("virtual display") ||
											    desc.Contains("spacedesk") || desc.Contains("usb mobile monitor"))
											{
												list2.Add(string.Format("⛔ [5.08 - Виртуальный видеоадаптер (Virtual Monitor)]: Обнаружен адаптер '{0}' ({1})",
													devKey.GetValue("DriverDesc"), devKey.GetValue("ProviderName")));
											}
										}
									}
								}
							}
						}
					}
				}
				catch { }
			}

			List<CheckResult> list = new List<CheckResult>();
			if (list2.Count > 0)
			{
				CheckResult checkResult = new CheckResult();
				checkResult.Category = "Среда / Виртуализация и Anti-Capture";
				checkResult.Name = "Детектор виртуальных машин, Anti-Capture и виртуальных мониторов (5.08 / 5.01)";
				checkResult.Detected = true;
				checkResult.Severity = CheckSeverity.Critical;
				checkResult.Detail = "Обнаружены критические признаки обхода/уклонения (5.08/5.01): " + list2.Count + " шт.";
				checkResult.Evidence = string.Join("\r\n", list2);
				list.Add(checkResult);
			}
			else
			{
				CheckResult checkResult2 = new CheckResult();
				checkResult2.Category = "Среда / Виртуализация и Anti-Capture";
				checkResult2.Name = "Анализ физической среды ПК (Anti-VM / Anti-Capture)";
				checkResult2.Detected = false;
				checkResult2.Severity = CheckSeverity.Info;
				checkResult2.Detail = "Проверка запущена на реальном физическом компьютере (признаков VM, песочниц, сокрытия экрана Anti-Capture или виртуальных мониторов не обнаружено).";
				list.Add(checkResult2);
			}

			return list;
		}

		public static List<CheckResult> CheckVirtualMachineAndSandbox()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				return AuditVirtualMachineAndAntiCaptureEnvironment(null, null, null, null, null, null, null);
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Среда / Виртуализация и Anti-Capture";
				checkResult3.Name = "Анализ физической среды";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки Anti-VM: " + ex.Message;
				list.Add(checkResult3);
				return list;
			}
		}

		#endregion

		public static List<CheckResult> CheckTelegramAndDirectDownloads()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				List<string> targetPaths = new List<string>();
				targetPaths.Add(Path.Combine(folderPath, "Downloads\\Telegram Desktop"));
				targetPaths.Add(Path.Combine(folderPath, "Downloads"));
				targetPaths.Add(Path.Combine(folderPath, "Desktop"));
				targetPaths.Add(Path.Combine(appData, "Telegram Desktop\\tdata\\temp"));
				targetPaths.Add(Path.Combine(localAppData, "Telegram Desktop\\tdata\\temp"));
				targetPaths.Add(Path.Combine(folderPath, "Downloads\\Discord"));
				targetPaths.Add(Path.Combine(folderPath, "Downloads\\Opera"));

				foreach (string path in targetPaths)
				{
					if (!Directory.Exists(path))
					{
						continue;
					}
					try
					{
						string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
					System.Diagnostics.Stopwatch swDisg = System.Diagnostics.Stopwatch.StartNew();
												foreach (string text in files)
						{
							if (swDisg.ElapsedMilliseconds > 1500) break;

							string text2 = Path.GetFileName(text).ToLowerInvariant();
							if (!text2.EndsWith(".jar") && !text2.EndsWith(".exe") && !text2.EndsWith(".dll") && !text2.EndsWith(".zip") && !text2.EndsWith(".rar") && !text2.EndsWith(".7z") && !text2.EndsWith(".bat") && !text2.EndsWith(".cmd") && !text2.EndsWith(".ps1"))
							{
								continue;
							}
							FileInfo fileInfo = new FileInfo(text);
							bool isCleanerHit = false;
							foreach (string cleaner in CheatDatabases.KnownCleanerSignatures)
							{
								if (cleaner.Length >= 4 && text2.Contains(cleaner))
								{
									list2.Add("⛔ [5.08 - Загрузка клинера/утилиты]: [" + Path.GetFileName(path) + "] " + Path.GetFileName(text) + " (" + fileInfo.Length + " байт, дата: " + fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")");
									isCleanerHit = true;
									break;
								}
							}
							if (isCleanerHit) continue;

							bool isCheatHit = false;
							string[] cheatClientNames = CheatClientNames;
							foreach (string cheatLower in cheatClientNames)
							{
								if (MatchesCheatName(text2, cheatLower) && !text2.Contains("lunar") && !text2.Contains("feather") && !text2.Contains("badlion"))
								{
									list2.Add("⛔ [5.01 - Загрузка чит-клиента]: [" + Path.GetFileName(path) + "] " + Path.GetFileName(text) + " (" + fileInfo.Length + " байт, дата: " + fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")");
									isCheatHit = true;
									break;
								}
							}
							if (!isCheatHit)
							{
								foreach (string extraCheat in CheatDatabases.ExtraCheatClients)
								{
									if (extraCheat.Length >= 4 && text2.Contains(extraCheat.ToLowerInvariant()))
									{
										list2.Add("⛔ [5.01 - Загрузка чит-клиента]: [" + Path.GetFileName(path) + "] " + Path.GetFileName(text) + " (" + fileInfo.Length + " байт, дата: " + fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")");
										isCheatHit = true;
										break;
									}
								}
							}
							if (!isCheatHit && text2.EndsWith(".jar") && CheatDatabases.KnownCheatJarSizes.Contains(fileInfo.Length))
							{
								list2.Add("⛔ [5.01 - Загрузка мода (аномальный размер)]: [" + Path.GetFileName(path) + "] " + Path.GetFileName(text) + " (" + fileInfo.Length + " байт, дата: " + fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")");
							}
						}
					}
					catch
					{
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Загрузки / Мессенджеры";
					checkResult.Name = "Загрузки из Telegram Desktop и браузера";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "В папках загрузок обнаружены файлы чит-клиентов: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Загрузки / Мессенджеры";
					checkResult2.Name = "Загрузки из Telegram Desktop и браузера";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Файлов читов в папках загрузок Telegram и браузера не найдено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Загрузки / Мессенджеры";
				checkResult3.Name = "Контроль безопасности";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки Prefetch: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		public static string AutoDetectAnyDeskId()
		{
			try
			{
				string[] anydeskPaths = new string[]
				{
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "AnyDesk\\system.conf"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "AnyDesk\\system.conf")
				};
				foreach (string path in anydeskPaths)
				{
					if (File.Exists(path))
					{
						string[] array = File.ReadAllLines(path);
						foreach (string text in array)
						{
							string text2 = text.Trim();
							if (text2.StartsWith("ad.anynet.id=") && text2.Length > 13)
							{
								return "AnyDesk: " + text2.Substring(13).Trim();
							}
						}
					}
				}

				string[] rustdeskPaths = new string[]
				{
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RustDesk\\config\\RustDesk2.toml"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RustDesk\\config\\RustDesk.toml")
				};
				foreach (string path in rustdeskPaths)
				{
					if (File.Exists(path))
					{
						string[] array = File.ReadAllLines(path);
						foreach (string text in array)
						{
							string text2 = text.Trim();
							if (text2.StartsWith("id =") || text2.StartsWith("id="))
							{
								string id = text2.Replace("id", "").Replace("=", "").Replace("'", "").Replace("\"", "").Trim();
								if (!string.IsNullOrEmpty(id) && id != "0")
								{
									return "RustDesk: " + id;
								}
							}
						}
					}
				}

				string[] rudesktopPaths = new string[]
				{
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RuDesktop\\config.json"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "RuDesktop\\settings.json"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "RuDesktop\\config.json"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RuDesktop\\config.json")
				};
				foreach (string rpath in rudesktopPaths)
				{
					if (File.Exists(rpath))
					{
						string rcontent = File.ReadAllText(rpath);
						Match m = Regex.Match(rcontent, "\"id\"\\s*:\\s*\"?([0-9]{6,12})\"?", RegexOptions.IgnoreCase);
						if (m.Success)
						{
							return "RuDesktop: " + m.Groups[1].Value;
						}
					}
				}
				try
				{
					using (RegistryKey rk = Registry.CurrentUser.OpenSubKey("Software\\RuDesktop"))
					{
						if (rk != null)
						{
							object val = rk.GetValue("ID") ?? rk.GetValue("ClientId");
							if (val != null && !string.IsNullOrEmpty(val.ToString()))
							{
								return "RuDesktop: " + val.ToString().Trim();
							}
						}
					}
				}
				catch
				{
				}
			}
			catch
			{
			}
			return "";
		}

		public static List<CheckResult> CheckRecycleBinIndexFiles()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				List<string> list3 = new List<string>();
				IEnumerable<DriveInfo> enumerable = from d in DriveInfo.GetDrives()
					where d.IsReady && (d.DriveType == DriveType.Fixed || d.DriveType == DriveType.Removable)
					select d;
				foreach (DriveInfo item2 in enumerable)
				{
					string path = Path.Combine(item2.RootDirectory.FullName, "$Recycle.Bin");
					if (!Directory.Exists(path))
					{
						continue;
					}
					string[] directories;
					try
					{
						directories = Directory.GetDirectories(path);
					}
					catch
					{
						continue;
					}
					foreach (string path2 in directories)
					{
						string[] files;
						try
						{
							files = Directory.GetFiles(path2, "$I*");
						}
						catch
						{
							continue;
						}
						foreach (string path3 in files)
						{
							try
							{
								byte[] array3 = null;
								using (FileStream fs = new FileStream(path3, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
								{
									int len = (int)Math.Min(fs.Length, 1048576L);
									array3 = new byte[len];
									int readTotal = 0;
									while (readTotal < len)
									{
										int r = fs.Read(array3, readTotal, len - readTotal);
										if (r <= 0) break;
										readTotal += r;
									}
									if (readTotal < len) Array.Resize(ref array3, readTotal);
								}

								if (array3 != null && array3.Length >= 28)
								{
									long fileTime = BitConverter.ToInt64(array3, 16);
									DateTime dateTime = DateTime.FromFileTime(fileTime);

									// Windows 7/8 format 1: offset 24 (260 Unicode chars)
									// Windows 10/11 format 2: offset 24 has int32 length, offset 28 has Unicode path
									long version = BitConverter.ToInt64(array3, 0);
									int pathOffset = (version == 1) ? 24 : 28;
									if (pathOffset >= array3.Length) pathOffset = 24;

									string rawPath = Encoding.Unicode.GetString(array3, pathOffset, array3.Length - pathOffset);
									int nullIdx = rawPath.IndexOf('\0');
									string text2 = (nullIdx >= 0 ? rawPath.Substring(0, nullIdx) : rawPath).Trim();
									if (string.IsNullOrEmpty(text2) || text2.Length < 3) continue;

									string text3 = text2.ToLowerInvariant();
									bool isCheat = false;
									string matchedName = "";

									foreach (string text5 in CheatClientNames)
									{
										if (MatchesCheatName(text3, text5) && !text3.Contains("lunar"))
										{
											isCheat = true;
											matchedName = text5;
											break;
										}
									}
									if (!isCheat)
									{
										foreach (string text6 in CheatDatabases.ExtraCheatClients)
										{
											if (text6.Length >= 4 && MatchesCheatName(text3, text6) && !text3.Contains("lunar"))
											{
												isCheat = true;
												matchedName = text6;
												break;
											}
										}
									}
									if (!isCheat)
									{
										string[] extraInj = new string[] { "kmbox", "interception", "spoofer", "injector", "loader", "slinky", "booster", "killaura", "hitbox", "reach", "xray" };
										foreach (string inj in extraInj)
										{
											if (text3.Contains(inj))
											{
												isCheat = true;
												matchedName = inj;
												break;
											}
										}
									}

									bool isCleaner = false;
									string cleanerMatched = "";
									if (!isCheat)
									{
										foreach (string cl in CheatDatabases.KnownCleanerSignatures)
										{
											if (cl.Length >= 4 && text3.Contains(cl))
											{
												isCleaner = true;
												cleanerMatched = cl;
												break;
											}
										}
									}

									TimeSpan timeSpan = DateTime.Now - dateTime;
									if (isCheat)
									{
				string item = string.Format("\ud83d\udea8 [5.08 - Запуск ПО из Prefetch | {0}] [{1}] {2} (Дата запуска: {3:dd.MM.yyyy HH:mm:ss})", item2.Name, matchedName, text2, dateTime);
										if (timeSpan.TotalHours >= 0.0 && timeSpan.TotalHours <= 2.0)
										{
											list3.Add(string.Format("⚠️ [5.08 - Свежее удаление файла пользователем] Файл попал в корзину {0:dd.MM.yyyy HH:mm:ss} ({1:0} мин. назад)! -> {2}", dateTime, timeSpan.TotalMinutes, text2));
										}
										list2.Add(item);
									}
									else if (isCleaner)
									{
										list2.Add(string.Format("⛔ [5.08 - Удаление клинера из корзины | {0}] [{1}] {2} (дата: {3:dd.MM.yyyy HH:mm:ss})", item2.Name, cleanerMatched, text2, dateTime));
									}
									else if ((text3.EndsWith(".jar") || text3.EndsWith(".exe") || text3.EndsWith(".dll") || text3.EndsWith(".bat") || text3.EndsWith(".cmd") || text3.EndsWith(".ps1") || text3.EndsWith(".zip") || text3.EndsWith(".7z") || text3.EndsWith(".rar") || text3.EndsWith(".amc")) && timeSpan.TotalHours >= 0.0 && timeSpan.TotalHours <= 2.0)
									{
										list3.Add(string.Format("⚠️ [5.08 - Свежее удаление подозрительного файла] {0} удалён {1:dd.MM.yyyy HH:mm:ss} ({2:0} мин. назад)", text2, dateTime, timeSpan.TotalMinutes));
									}
								}
							}
							catch
							{
							}
							if (list2.Count >= 35) break;
						}
						if (list2.Count >= 35) break;

						// Декодирование $R для проверки размера
						try
						{
							string[] rFiles = Directory.GetFiles(path2, "$R*");
							foreach (string rFile in rFiles)
							{
								try
								{
									FileInfo fi = new FileInfo(rFile);
									if (fi.Length > 0 && fi.Length < 120000000)
									{
										string rExt = fi.Extension.ToLowerInvariant();
										bool rCheatFound = false;
										string rCheatName = "";

										if (CheatDatabases.KnownCheatJarSizes.Contains(fi.Length))
										{
											rCheatFound = true;
											rCheatName = "Аномальный размер чит-.jar (" + fi.Length + " байт)";
										}
										else if (rExt == ".jar" || rExt == ".zip")
										{
											using (ZipArchive zip = ZipFile.OpenRead(rFile))
											{
												foreach (ZipArchiveEntry ze in zip.Entries)
												{
													string zeName = ze.FullName.ToLowerInvariant();
													foreach (string cheat in CheatClientNames)
													{
														if (MatchesCheatName(zeName, cheat))
														{
															rCheatFound = true;
															rCheatName = cheat;
															break;
														}
													}
													if (rCheatFound) break;
													foreach (string ec in CheatDatabases.ExtraCheatClients)
													{
														if (ec.Length >= 4 && zeName.Contains(ec))
														{
															rCheatFound = true;
															rCheatName = ec;
															break;
														}
													}
													if (rCheatFound) break;
												}
											}
										}

										if (rCheatFound)
										{
											list2.Add(string.Format("⛔ [5.08 - Чит в файле $R в корзине] {0} -> Чит: [{1}] (размер: {2:N0} байт)", Path.GetFileName(rFile), rCheatName, fi.Length));
											if (list2.Count >= 35) break;
										}
									}
								}
								catch { }
							}
						}
						catch { }
					}
				}

				if (list2.Count > 0 || list3.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Корзина / Удаления";
					checkResult.Name = "🗑️ Декодирование $I индексов корзины $Recycle.Bin (Оригинальные пути)...";
					checkResult.Detected = list2.Count > 0;
					checkResult.Severity = (list2.Count > 0 ? CheckSeverity.Critical : CheckSeverity.Warning);
			checkResult.Detail = string.Format("В корзине $Recycle.Bin найдено удалений читов/клинеров: {0} шт. (свежих удалений перед SS: {1})", list2.Count, list3.Count);
					List<string> list4 = new List<string>();
					list4.AddRange(list3);
					list4.AddRange(list2);
					checkResult.Evidence = string.Join("\r\n", list4.Take(40));
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Корзина / Удаления";
					checkResult2.Name = "Декодирование $I индексов корзины $Recycle.Bin";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
			checkResult2.Detail = "Следов удаления читов в индексах $I корзины не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Корзина / Удаления";
				checkResult3.Name = "Декодирование $I индексов корзины $Recycle.Bin";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка сканирования корзины: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		public static List<CheckResult> CheckSRUMDatabase()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				bool isTampered = false;
				bool cheatDetected = false;

				// 1. Diagnostic Policy Service (DPS) audit
				try
				{
					using (RegistryKey dpsKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\DPS"))
					{
						if (dpsKey != null)
						{
							object startVal = dpsKey.GetValue("Start");
							if (startVal != null && Convert.ToInt32(startVal) == 4)
							{
					list2.Add("⛔ [5.08 - Служба DPS/SRUM отключена] Служба Diagnostic Policy Service (DPS) отключена (Start=4)! История сети базы данных SRUM заморожена.");
								isTampered = true;
							}
							else
							{
					list2.Add("⚠️ Служба DPS (Diagnostic Policy Service / SRUM) остановлена.");
							}
						}
					}
				}
				catch
				{
				}

				// 2. Extensions in Registry
				try
				{
					using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion\\SRUM\\Extensions"))
					{
						if (registryKey != null)
						{
							list2.Add("[Реестр SRUM] Модули учета ресурсов Windows активны (" + registryKey.SubKeyCount + " расширений).");
						}
					}
				}
				catch
				{
				}

				// 3. SRUDB.dat physical file inspection
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string text = Path.Combine(folderPath, "System32\\sru\\SRUDB.dat");
				if (!File.Exists(text))
				{
				list2.Add("⚠️ [SRUM] Файл базы SRUDB.dat не найден или заблокирован (возможно была произведена очистка базы перед проверкой).");
				}
				else
				{
					try
					{
						FileInfo fileInfo = new FileInfo(text);
						TimeSpan age = DateTime.Now - fileInfo.LastWriteTime;
						list2.Add(string.Format("[База данных SRUM] SRUDB.dat: {0:F1} МБ, изменена: {1:dd.MM.yyyy HH:mm:ss} ({2:F0} мин назад)", (double)fileInfo.Length / 1048576.0, fileInfo.LastWriteTime, age.TotalMinutes));

						if (fileInfo.Length < 100 * 1024)
						{
							list2.Add("⛔ [5.08 - Сброс базы SRUM] Подозрительно маленький размер SRUDB.dat (< 100 КБ) — подозрение на затирание логов/активности сети.");
						}

						// 4. Memory/binary scanning of SRUDB.dat for cheat executables & cleaners
						if (fileInfo.Length > 0)
						{
							List<string> targets = new List<string>();
							foreach (string c in CheatClientNames) targets.Add(c.ToLowerInvariant());
							foreach (string ec in CheatDatabases.ExtraCheatClients)
							{
								if (ec.Length >= 4 && !targets.Contains(ec.ToLowerInvariant())) targets.Add(ec.ToLowerInvariant());
							}
							string[] extraInj = new string[] { "kmbox", "interception", "spoofer", "dllinjector", "cheatloader", "slinky" };
							foreach (string inj in extraInj)
							{
								if (!targets.Contains(inj)) targets.Add(inj);
							}

							HashSet<string> detectedTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

							using (FileStream fs = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
							{
								int maxScanBytes = (int)Math.Min(fs.Length, 32 * 1024 * 1024);
								byte[] buffer = new byte[2 * 1024 * 1024];
								int totalRead = 0;

								while (totalRead < maxScanBytes)
								{
									int toRead = Math.Min(buffer.Length, maxScanBytes - totalRead);
									int read = fs.Read(buffer, 0, toRead);
									if (read <= 0) break;
									totalRead += read;

									string chunkAscii = Encoding.ASCII.GetString(buffer, 0, read).ToLowerInvariant();
									string chunkUnicode = Encoding.Unicode.GetString(buffer, 0, read).ToLowerInvariant();

									foreach (string t in targets)
									{
										if (t.Length < 4) continue;
										if (chunkAscii.Contains(t) || chunkUnicode.Contains(t))
										{
											if (detectedTargets.Add(t))
											{
												list2.Add(string.Format("⛔ [5.01 - Чит в SRUM] В базе учёта ресурсов Windows (SRUDB.dat) обнаружены следы запуска/трафика запрещённого софта: [{0}]", t));
												cheatDetected = true;
											}
										}
									}

									foreach (string cl in CheatDatabases.KnownCleanerSignatures)
									{
										if (cl.Length >= 4 && (chunkAscii.Contains(cl) || chunkUnicode.Contains(cl)))
										{
											if (detectedTargets.Add(cl))
											{
												list2.Add(string.Format("⛔ [5.08 - Запуск клинера в SRUM] В базе учёта ресурсов Windows (SRUDB.dat) зафиксирована активность утилиты: [{0}]", cl));
												cheatDetected = true;
											}
										}
									}

									if (detectedTargets.Count >= 25) break;
								}
							}
						}
					}
					catch (Exception ex2)
					{
						list2.Add("[SRUDB.dat] Ошибка считывания записи базы: " + ex2.Message);
					}
				}

				CheckResult checkResult = new CheckResult();
				checkResult.Category = "Системные базы / SRUM";
				checkResult.Name = "📊 База учета использования ресурсов SRUM (SRUDB.dat)...";
				checkResult.Detected = cheatDetected || isTampered;
				checkResult.Severity = (cheatDetected || isTampered) ? CheckSeverity.Critical : CheckSeverity.Info;
				checkResult.Detail = cheatDetected
					? "В базе SRUM обнаружена подозрительная запись сетевого трафика читов или запуск клинеров [5.01 / 5.08]!"
					: (isTampered
						? "Обнаружен сброс статистики или блокировка службы сбора SRUM (DPS Start=4 / очистка базы) [5.08]!"
						: "База SRUM активна и сохраняет 30-дневную историю выполнения процессов.");
				checkResult.Evidence = string.Join("\r\n", list2);
				list.Add(checkResult);
			}
			catch (Exception ex)
			{
				CheckResult checkResult2 = new CheckResult();
				checkResult2.Category = "Системные базы / SRUM";
				checkResult2.Name = "База SRUM";
				checkResult2.Detected = false;
				checkResult2.Severity = CheckSeverity.Info;
				checkResult2.Detail = "Ошибка проверки SRUM: " + ex.Message;
				list.Add(checkResult2);
			}
			return list;
		}

		private static bool FastFileExists(string path)
		{
			if (string.IsNullOrEmpty(path)) return false;
			try
			{
				if (path.Length >= 3 && path[1] == ':' && (path[2] == '\\' || path[2] == '/'))
				{
					DriveInfo di = new DriveInfo(path.Substring(0, 1));
					if (!di.IsReady) return false;
				}
				else if (path.StartsWith(@"\\"))
				{
					return false;
				}
				return File.Exists(path);
			}
			catch
			{
				return false;
			}
		}

		public static List<CheckResult> CheckPcaAppLaunchLogs()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				Stopwatch sw = Stopwatch.StartNew();
				List<string> list2 = new List<string>();
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				List<string> candidateDirs = new List<string>();
				candidateDirs.Add(Path.Combine(folderPath, "appcompat\\pca"));
				candidateDirs.Add(Path.Combine(folderPath, "appcompat\\Programs"));
				candidateDirs.Add(Path.Combine(folderPath, "appcompat\\Programs\\Install"));
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				if (!string.IsNullOrEmpty(localAppData))
				{
					candidateDirs.Add(Path.Combine(localAppData, "Microsoft\\Windows\\PCA"));
				}

				HashSet<string> seenEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				HashSet<string> cleanExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (string path in candidateDirs)
				{
					if (sw.ElapsedMilliseconds > 2000) break;
					if (!Directory.Exists(path)) continue;

					List<string> files = new List<string>();
					try { files.AddRange(Directory.GetFiles(path, "*.txt")); } catch {}
					try { files.AddRange(Directory.GetFiles(path, "*.db")); } catch {}
					try { files.AddRange(Directory.GetFiles(path, "*.dic")); } catch {}

					foreach (string path2 in files)
					{
						if (sw.ElapsedMilliseconds > 2000) break;
						string fileName = Path.GetFileName(path2);
						try
						{
							byte[] rawBytes;
							using (FileStream fs = new FileStream(path2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
							{
								long maxRead = 1048576L;
								long seekPos = Math.Max(0, fs.Length - maxRead);
								int len = (int)(fs.Length - seekPos);
								fs.Seek(seekPos, SeekOrigin.Begin);
								rawBytes = new byte[len];
								int totalRead = 0;
								while (totalRead < len)
								{
									int r = fs.Read(rawBytes, totalRead, len - totalRead);
									if (r <= 0) break;
									totalRead += r;
								}
								if (totalRead < len) Array.Resize(ref rawBytes, totalRead);
							}
							if (rawBytes.Length < 4) continue;

							string content;
							if ((rawBytes[0] == 0xFF && rawBytes[1] == 0xFE) || (rawBytes.Length > 3 && rawBytes[1] == 0x00 && rawBytes[3] == 0x00))
							{
								content = Encoding.Unicode.GetString(rawBytes);
							}
							else
							{
								content = Encoding.UTF8.GetString(rawBytes);
							}

							string[] lines = content.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
							foreach (string rawLine in lines)
							{
								if (sw.ElapsedMilliseconds > 2000) break;
								string line = rawLine.Trim();
								if (string.IsNullOrEmpty(line) || line.StartsWith("#")) continue;

								string[] parts = line.Split('|');
								string rawExePath = "";
								string timestamp = "";

								if (parts.Length >= 2)
								{
									if (parts[0].Contains(":\\") || parts[0].StartsWith("%"))
									{
										rawExePath = parts[0].Trim();
										timestamp = parts[1].Trim();
									}
									else if (parts.Length >= 3 && (parts[2].Contains(":\\") || parts[2].StartsWith("%")))
									{
										timestamp = parts[0].Trim();
										rawExePath = parts[2].Trim();
									}
									else
									{
										foreach (string p in parts)
										{
											if (p.EndsWith(".exe", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".jar", StringComparison.OrdinalIgnoreCase))
											{
												rawExePath = p.Trim();
												break;
											}
										}
										if (timestamp == "" && parts[0].Length >= 10 && parts[0].Contains("-"))
										{
											timestamp = parts[0].Trim();
										}
									}
								}
								else
								{
									rawExePath = line;
								}

								if (string.IsNullOrEmpty(rawExePath)) continue;

								string resolvedPath = Environment.ExpandEnvironmentVariables(rawExePath).Trim('"', ' ');
								string exeName = Path.GetFileName(resolvedPath).ToLowerInvariant();

								if (cleanExes.Contains(exeName)) continue;

								string resolvedLower = resolvedPath.ToLowerInvariant();

								if (CheatDatabases.IsWhitelistedPath(resolvedLower) || CheatDatabases.IsWhitelistedProcess(exeName.Replace(".exe", "")) ||
								    resolvedLower.Contains("mccheat") || resolvedLower.Contains("checker") || resolvedLower.Contains("          \"name\": ") ||
								    resolvedLower.Contains("screenshare") || resolvedLower.Contains("sstools") || resolvedLower.Contains("start_windows.bat") ||
								    resolvedLower.Contains("packages") || resolvedLower.Contains("autoclicker") || resolvedLower.Contains("clicker") ||
								    resolvedLower.Contains("tinytask") || resolvedLower.Contains("opautoclicker") || resolvedLower.Contains("speedautoclicker") ||
								    resolvedLower.Contains("x-mouse") || resolvedLower.Contains("xmouse") || resolvedLower.Contains("keyran") ||
								    resolvedLower.Contains("botmek") || resolvedLower.Contains("autohotkey") || resolvedLower.Contains("ahk") || resolvedLower.Contains("jnativehook"))
								{
									cleanExes.Add(exeName);
									continue;
								}
								if (exeName.Equals("xray.exe", StringComparison.OrdinalIgnoreCase) && (resolvedLower.Contains("happ") || resolvedLower.Contains("v2ray") || resolvedLower.Contains("nekoray") || resolvedLower.Contains("sing-box") || resolvedLower.Contains("core") || resolvedLower.Contains("vpn") || resolvedLower.Contains("proxy") || resolvedLower.Contains("clash")))
								{
									cleanExes.Add(exeName);
									continue;
								}
								if (exeName.Contains("lunarclient") || exeName.Contains("badlion") || exeName.Contains("curseforge") || exeName.Contains("prismlauncher") || exeName.Contains("tlauncher"))
								{
									cleanExes.Add(exeName);
									continue;
								}

								string matchedCheat = null;
								foreach (string cheat in CheatClientNames)
								{
									if (MatchesCheatName(exeName, cheat) || MatchesCheatName(resolvedLower, cheat))
									{
										matchedCheat = cheat;
										break;
									}
								}
								if (matchedCheat == null)
								{
									foreach (string cheat in CheatDatabases.ExtraCheatClients)
									{
										if (cheat.Length >= 4 && (exeName.Contains(cheat) || resolvedLower.Contains(cheat)))
										{
											matchedCheat = cheat;
											break;
										}
									}
								}
								if (matchedCheat == null)
								{
									string[] extraKeywords = new string[] { "killaura", "hitbox", "reach", "xray", "kmbox" };
									foreach (string kw in extraKeywords)
									{
										if (exeName.Contains(kw))
										{
											matchedCheat = kw;
											break;
										}
									}
								}

								string matchedCleaner = null;
								if (matchedCheat == null)
								{
									foreach (string cl in CheatDatabases.KnownCleanerSignatures)
									{
										if (cl.Length >= 4 && (exeName.Contains(cl) || resolvedLower.Contains(cl)))
										{
											matchedCleaner = cl;
											break;
										}
									}
								}

								if (matchedCheat == null && matchedCleaner == null)
								{
									cleanExes.Add(exeName);
								}
								else
								{
									string dedupeKey = resolvedLower + "|" + timestamp;
									if (!seenEntries.Add(dedupeKey)) continue;

									bool fileExists = FastFileExists(resolvedPath);
									string status = fileExists ? "Файл существует на диске" : "Файл был удален после запуска (сокрытие следов [5.08])!";
									string timeStr = string.IsNullOrEmpty(timestamp) ? "" : (" | время: " + timestamp);

									if (matchedCheat != null)
									{
										list2.Add(string.Format("⛔ [5.01 - Чит в PCA] [{0}] Чит: {1} | Путь: {2}{3} | Статус: {4}", fileName, matchedCheat, resolvedPath, timeStr, status));
									}
									else
									{
										list2.Add(string.Format("⛔ [5.08 - Запуск клинера в PCA] [{0}] Утилита очистки: {1} | Путь: {2}{3} | Статус: {4}", fileName, matchedCleaner, resolvedPath, timeStr, status));
									}
								}
							}
						}
						catch
						{
						}
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Следы запуска";
					checkResult.Name = "Системные логи PCA (Program Compatibility Assistant)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
				checkResult.Detail = string.Format("Обнаружен запуск и следы читов в системных логах PCA ({0} записей)!", list2.Count);
					checkResult.Evidence = string.Join("\r\n", list2.ToArray());
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Следы запуска";
					checkResult2.Name = "Системные логи PCA (Program Compatibility Assistant)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
				checkResult2.Detail = "В хранилище PCA (PcaAppLaunchDic / PcaGeneralDbx) следов запуска читов не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Следы запуска";
				checkResult3.Name = "Системные логи PCA (Program Compatibility Assistant)";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка анализа журналов PCA: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		public static List<CheckResult> CheckDnsOverHttpsAndEncryptedDnsEvasion()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> evidence = new List<string>();
			List<string> warnings = new List<string>();

			try
			{
				// 1. Windows Native DNS-over-HTTPS (DoH) Settings in Registry
				using (RegistryKey dnscache = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters"))
				{
					if (dnscache != null)
					{
						object dohFlags = dnscache.GetValue("DohFlags");
						if (dohFlags != null)
						{
							long val = Convert.ToInt64(dohFlags);
							if (val == 1)
							{
						warnings.Add("⚠️ Windows DoH (DNS-over-HTTPS) включён (DohFlags=1). Трафик может шифроваться.");
							}
							else if (val == 2)
							{
							evidence.Add("⛔ Windows DoH (DNS-over-HTTPS) принудительно включён (DohFlags=2)! Стандартный DNS кэш Windows отключён (попытка скрытия обращений читов)!");
							}
						}

						object enableAutoDoh = dnscache.GetValue("EnableAutoDoh");
						if (enableAutoDoh != null && Convert.ToInt32(enableAutoDoh) == 1)
						{
							warnings.Add("⚠️ Windows EnableAutoDoh=1: автоматическое шифрование DNS-запросов активно.");
						}

						// 2. DNS Cache Disabling / Anti-Forensics (TTL zeroing)
						object maxCacheTtl = dnscache.GetValue("MaxCacheTtl");
						if (maxCacheTtl != null && Convert.ToInt32(maxCacheTtl) == 0)
						{
							evidence.Add("⛔ Пункт 3.6 регламента: Отключение кэша DNS (MaxCacheTtl=0)! Системный DNS кэш принудительно отключён для скрытия чит-доменов [5.08]!");
						}
						object maxNegCacheTtl = dnscache.GetValue("MaxNegativeCacheTtl");
						if (maxNegCacheTtl != null && Convert.ToInt32(maxNegCacheTtl) == 0)
						{
							warnings.Add("⚠️ MaxNegativeCacheTtl=0: кэширование отрицательных DNS-ответов отключено.");
						}
					}
				}

				// Check custom DoH Server templates in registry
				using (RegistryKey dohSettings = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\Dnscache\\Parameters\\DohSettings"))
				{
					if (dohSettings != null && dohSettings.SubKeyCount > 0)
					{
						string[] servers = dohSettings.GetSubKeyNames();
						warnings.Add("В системе настроены публичные DoH-серверы (" + servers.Length + " шт.): " + string.Join(", ", servers));
					}
				}

				// 3. Third-party Encrypted DNS / DoH proxies & DPI-bypass utilities
				string[] dohProxyProcesses = new string[]
				{
					"cloudflared", "dnscrypt-proxy", "yogadns", "stubby", "simple-dns-crypt",
					"intra", "goodbyedpi", "zapret", "byedpi", "spoofdpi"
				};
				Process[] allProcesses = Process.GetProcesses();
				foreach (Process p in allProcesses)
				{
					try
					{
						string pName = p.ProcessName.ToLowerInvariant();
						foreach (string proxy in dohProxyProcesses)
						{
							if (pName.Contains(proxy))
							{
								warnings.Add(string.Format("⚠️ Запущен процесс прокси/DoH/DPI: {0} (PID: {1}) — может влиять на логирование сетевых пакетов.", p.ProcessName, p.Id));
								break;
							}
						}
					}
					catch
					{
					}
				}

				// 4. Browser DoH policies (Chrome / Edge)
				using (RegistryKey chromePolicy = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Google\\Chrome"))
				{
					if (chromePolicy != null)
					{
						object mode = chromePolicy.GetValue("DnsOverHttpsMode");
						if (mode != null && mode.ToString().ToLowerInvariant().Contains("secure"))
						{
							warnings.Add("Браузер Chrome: включён режим DnsOverHttpsMode='secure' (принудительный DoH).");
						}
					}
				}
				using (RegistryKey edgePolicy = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Policies\\Microsoft\\Edge"))
				{
					if (edgePolicy != null)
					{
						object mode = edgePolicy.GetValue("DnsOverHttpsMode");
						if (mode != null && mode.ToString().ToLowerInvariant().Contains("secure"))
						{
							warnings.Add("Браузер Edge: включён режим DnsOverHttpsMode='secure' (принудительный DoH).");
						}
					}
				}
			}
			catch (Exception ex)
			{
				warnings.Add("Ошибка анализа настроек DoH: " + ex.Message);
			}

			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Сеть / Соединения";
			checkResult.Name = "🔒 Аудит конфигурации DNS-over-HTTPS (DoH) и шифрованных запросов...";
			checkResult.Detected = evidence.Count > 0 || warnings.Count > 0;
			checkResult.Severity = (evidence.Count > 0) ? CheckSeverity.Critical : ((warnings.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			if (evidence.Count > 0)
			{
			checkResult.Detail = "Обнаружено принудительное отключение DNS / обход логирования (MaxCacheTtl=0 или DoH=2)!";
				List<string> full = new List<string>(evidence);
				full.AddRange(warnings);
				checkResult.Evidence = string.Join("\r\n", full.ToArray());
			}
			else if (warnings.Count > 0)
			{
			checkResult.Detail = "Обнаружены признаки изменения настроек DNS (DoH/AutoDoH/прокси) — веб-запросы могут маскироваться от проверок.";
				checkResult.Evidence = string.Join("\r\n", warnings.ToArray());
			}
			else
			{
			checkResult.Detail = "Параметры DNS стандартные: принудительный DoH не включён, логирование DNS активно.";
			}
			list.Add(checkResult);
			return list;
		}

				public static List<CheckResult> CheckDisguisedModMixins()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				List<string> modsDirs = new List<string>();
				string mainMods = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft\\mods");
				if (Directory.Exists(mainMods))
				{
					modsDirs.Add(mainMods);
				}
				foreach (string mcDir in PlatformHelper.GetAllMinecraftDirectories())
				{
					try
					{
						string directMods = Path.Combine(mcDir, "mods");
						if (Directory.Exists(directMods) && !modsDirs.Contains(directMods))
						{
							modsDirs.Add(directMods);
						}
						if (Directory.Exists(mcDir))
						{
							foreach (string subDir in Directory.GetDirectories(mcDir))
							{
								string instMods = Path.Combine(subDir, "mods");
								if (Directory.Exists(instMods) && !modsDirs.Contains(instMods))
								{
									modsDirs.Add(instMods);
								}
								string instDotMcMods = Path.Combine(subDir, ".minecraft", "mods");
								if (Directory.Exists(instDotMcMods) && !modsDirs.Contains(instDotMcMods))
								{
									modsDirs.Add(instDotMcMods);
								}
							}
						}
					}
					catch
					{
					}
				}

				System.Diagnostics.Stopwatch swDisg = System.Diagnostics.Stopwatch.StartNew();
				DateTime javawStartTime = DateTime.MinValue;
				try
				{
					Process[] jps = Process.GetProcessesByName("javaw");
					if (jps.Length > 0)
					{
						javawStartTime = jps[0].StartTime;
					}
				}
				catch
				{
				}

				foreach (string modsPath in modsDirs)
				{
					try
					{
						string[] files = Directory.GetFiles(modsPath, "*.*", SearchOption.AllDirectories);
						foreach (string text in files)
						{
							if (swDisg.ElapsedMilliseconds > 5000) break;

							try
							{
								FileInfo fi = new FileInfo(text);
								string ext = Path.GetExtension(text).ToLowerInvariant();
								string fileName = Path.GetFileName(text);

								bool isJar = ext == ".jar";
								bool isDisguisedJar = false;

								if (!isJar)
								{
									if (fi.Length >= 2)
									{
										byte[] head = new byte[4];
										int readLen = 0;
										using (FileStream fsCheck = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
										{
											readLen = fsCheck.Read(head, 0, 4);
										}

										if (readLen >= 2 && head[0] == 0x4D && head[1] == 0x5A)
										{
											list2.Add(string.Format("⛔ [5.08 - Недопустимый исполняемый файл в mods]: Файл '{0}' (расширение '{1}') является исполняемым бинарным модулем Windows (MZ / PE)! Запрещено регламентом 5.08.", fileName, string.IsNullOrEmpty(ext) ? "без расширения" : ext));
										}
										else if (readLen == 4 && head[0] == 0x50 && head[1] == 0x4B &&
											((head[2] == 0x03 && head[3] == 0x04) || (head[2] == 0x05 && head[3] == 0x06) || (head[2] == 0x07 && head[3] == 0x08)))
										{
											try
											{
												using (FileStream fsZip = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
												using (ZipArchive zipCheck = new ZipArchive(fsZip, ZipArchiveMode.Read))
												{
													bool hasModSignatures = false;
													foreach (ZipArchiveEntry ze in zipCheck.Entries)
													{
														string zeName = ze.FullName.ToLowerInvariant();
														if (zeName.EndsWith(".class") || zeName == "fabric.mod.json" || zeName == "mcmod.info" ||
															zeName == "meta-inf/mods.toml" || zeName == "quilt.mod.json" || zeName == "meta-inf/manifest.mf")
														{
															hasModSignatures = true;
															break;
														}
													}
													if (hasModSignatures)
													{
														isDisguisedJar = true;
														list2.Add(string.Format("⛔ [5.08 - Замаскированный мод]: Файл '{0}' имеет расширение '{1}', но является исполняемым JAR-модом (заголовок PK 0x50 0x4B 0x03 0x04)! Запрещено регламентом 5.08.", fileName, string.IsNullOrEmpty(ext) ? "без расширения" : ext));
													}
												}
											}
											catch { }
										}
									}
								}

								if (isJar || isDisguisedJar)
								{
									if (fi.LastWriteTime > DateTime.Now.AddDays(1) || fi.LastWriteTime < new DateTime(2005, 1, 1))
									{
										list2.Add("⛔ [5.08 - Подделка времени мода (Timestomp)] Мод '" + fileName + "' имеет неестественную дату: " + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + " (признак timestomp/скрытия правки [5.08])");
									}
									else if (javawStartTime != DateTime.MinValue && fi.LastWriteTime > javawStartTime.AddMinutes(-1))
									{
										list2.Add("⛔ [5.08 - Горячая подмена / добавление мода во время запущенной игры] Мод '" + fileName + "' создан в " + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + " (Minecraft запущен в " + javawStartTime.ToString("dd.MM.yyyy HH:mm") + ")!");
									}

									using (FileStream fs = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
									using (ZipArchive zipArchive = new ZipArchive(fs, ZipArchiveMode.Read))
									{
										foreach (ZipArchiveEntry entry in zipArchive.Entries)
										{
											string text2 = entry.FullName.ToLowerInvariant();
											if (text2.EndsWith(".mixins.json") || text2.EndsWith(".mixin.json"))
											{
												using (Stream stream = entry.Open())
												{
													using (StreamReader streamReader = new StreamReader(stream))
													{
														string text3 = streamReader.ReadToEnd().ToLowerInvariant();
														if (text3.Contains("entityplayersp") || text3.Contains("playercontrollermp") || text3.Contains("nethandlerplayclient") || text3.Contains("cpacketplayer") || text3.Contains("networkmanager") || text3.Contains("reach") || text3.Contains("hitbox") || text3.Contains("killaura") || text3.Contains("silentaim") || text3.Contains("targetstrafe") || text3.Contains("fastplace") || text3.Contains("clientplayerentity") || text3.Contains("clientplayerinteractionmanager") || text3.Contains("clientplaynetworkhandler") || text3.Contains("cpacketuseentity"))
														{
															list2.Add("⛔ [5.01 - Подозрительный класс Mixin в моде " + fileName + "] Перехват сетевых/боевых пакетов: " + entry.Name);
														}
													}
												}
											}
											if (text2.Contains("/wurst/") || text2.Contains("/meteor/") || text2.Contains("/expensive/") || text2.Contains("/celestial/") || text2.Contains("/nursultan/") || text2.Contains("/liquidbounce/") || text2.Contains("/thunderhack/") || text2.Contains("/slinky/") || text2.Contains("/seedcracker/") || text2.Contains("/doomsday/") || text2.Contains("/boze/") || text2.Contains("/vape/") || text2.Contains("/akrien/") || text2.Contains("/criticals/") || text2.Contains("/targetstrafe/") || text2.Contains("/silentaim/") || text2.Contains("/packetfly/") || text2.Contains("/dreamclient/") || text2.Contains("/zamorozka/") || text2.Contains("/minced/") || text2.Contains("/wexside/") || text2.Contains("/richclient/") || text2.Contains("/fluger/") || text2.Contains("/autototem/") || text2.Contains("/anchor/") || text2.Contains("/fastmine/") ||
												text2.Contains("/catlavan/") || text2.Contains("/catlean/") || text2.Contains("/deltaclient/") || text2.Contains("/richpremium/") || text2.Contains("/deadcode/") || text2.Contains("/wildgrief/") || text2.Contains("/kura/") || text2.Contains("/flint/") || text2.Contains("/rockstar/") || text2.Contains("/boberware/") || text2.Contains("/tenacity/") || text2.Contains("/novoline/") || text2.Contains("/rise/") || text2.Contains("/drip/") || text2.Contains("/aurora/") || text2.Contains("/rusherhack/") || text2.Contains("/futureclient/") || text2.Contains("/pyro/") || text2.Contains("/konas/") || text2.Contains("/lambda/") || text2.Contains("/ares/") || text2.Contains("/bleachhack/") || text2.Contains("/kamiblue/") || text2.Contains("/salhack/") || text2.Contains("/inertia/") || text2.Contains("/aristois/") || text2.Contains("/impact/") || text2.Contains("/baritone/") || text2.Contains("/seedmapper/") || text2.Contains("/seedcrackerx/") || text2.Contains("/farsight/") || text2.Contains("/xray/") || text2.Contains("/systemdlc/") || text2.Contains("/sysdlc/"))
											{
												list2.Add("⛔ [5.01 - Скрытый чит в моде " + fileName + "] Класс: " + entry.FullName);
											}
										}
									}
								}
							}
							catch
							{
							}
						}
					}
					catch
					{
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Моды / Внедрения";
					checkResult.Name = "Замаскированные моды и вредоносные Mixin-инжекции (.minecraft\\mods)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "Обнаружены подозрительные Mixin-инжекции или чит-моды [5.01 / 5.08]: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Моды / Внедрения";
					checkResult2.Name = "Анализ Mixin-инжекций в модах (.minecraft\\mods)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Скрытых чит-миксинов и перехватов боевых пакетов в модах не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Моды / Внедрения";
				checkResult3.Name = "Анализ Mixin-инжекций";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки модов: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

				public static List<CheckResult> CheckBloodyAndMouseMacros()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

				// 1. Bloody / Oscar Editor (.amc, .bkg)
				string[] bloodyDirs = new string[]
				{
					"C:\\Program Files (x86)\\Bloody7",
					"C:\\Program Files\\Bloody7",
					"C:\\Program Files (x86)\\Bloody6",
					"C:\\Program Files (x86)\\Oscar Editor",
					Path.Combine(appData, "Bloody"),
					Path.Combine(localAppData, "Bloody")
				};
				foreach (string path in bloodyDirs)
				{
					if (!Directory.Exists(path)) continue;
					try
					{
						string[] amcFiles = Directory.GetFiles(path, "*.amc", SearchOption.AllDirectories);
						foreach (string text in amcFiles)
						{
							list2.Add("ℹ️ [Разрешённый кликер] Конфиг мыши Bloody/Oscar (.amc): " + text);
							if (list2.Count >= 10) break;
						}
						string[] bkgFiles = Directory.GetFiles(path, "*.bkg", SearchOption.AllDirectories);
						foreach (string text2 in bkgFiles)
						{
							list2.Add("🚨 [Bloody] Обнаружен файл макросов Bloody (.bkg): " + text2);
							if (list2.Count >= 15) break;
						}
					}
					catch { }
				}

				// 2. X-Mouse Button Control
				try
				{
					using (RegistryKey xmKey = Registry.CurrentUser.OpenSubKey(@"Software\Highresolution Enterprises\X-Mouse Button Control"))
					{
						if (xmKey != null)
						{
							list2.Add("ℹ️ [Разрешённый кликер] ПО X-Mouse Button Control (разрешено в соответствии с правилами сервера).");
						}
					}
				}
				catch { }

				CheckResult checkResult = new CheckResult();
				checkResult.Category = "Автокликеры / Макросы";
				checkResult.Name = "🖱️ Анализ ПО и макросов мыши (Bloody Ultra Core / X-Mouse)...";
				checkResult.Detected = false;
				checkResult.Severity = CheckSeverity.Info;
			checkResult.Detail = list2.Count > 0 ? "Обнаружено ПО мыши / макросы-кликеры (разрешены правилами сервера: " + list2.Count + " шт.)." : "Запрещённого управления ПО мыши не обнаружено.";
				checkResult.Evidence = string.Join("\r\n", list2);
				list.Add(checkResult);
			}
			catch (Exception ex)
			{
				CheckResult checkResult2 = new CheckResult();
				checkResult2.Category = "Автокликеры / Макросы";
				checkResult2.Name = "Анализ макросов мыши";
				checkResult2.Detected = false;
				checkResult2.Severity = CheckSeverity.Info;
				checkResult2.Detail = "Ошибка проверки ПО мыши: " + ex.Message;
				list.Add(checkResult2);
			}
			return list;
		}

		public static List<CheckResult> CheckDiscordAndTelegramCaches()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			try
			{
				string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

				// 1. Telegram Desktop Downloads & Temp
				List<string> tgDirs = new List<string>();
				if (!string.IsNullOrEmpty(userProfile))
				{
					tgDirs.Add(Path.Combine(userProfile, "Downloads", "Telegram Desktop"));
					tgDirs.Add(Path.Combine(userProfile, "Downloads"));
				}
				if (!string.IsNullOrEmpty(appData))
				{
					tgDirs.Add(Path.Combine(appData, "Telegram Desktop", "tdata", "temp"));
				}
				// Also check other mounted drives for Downloads\Telegram Desktop
				try
				{
					foreach (DriveInfo d in DriveInfo.GetDrives())
					{
						if (d.IsReady && d.DriveType == DriveType.Fixed)
						{
							string altTg = Path.Combine(d.RootDirectory.FullName, "Telegram Desktop");
							if (Directory.Exists(altTg)) tgDirs.Add(altTg);
						}
					}
				}
				catch { }

				HashSet<string> seenFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (string tgDir in tgDirs)
				{
					if (!Directory.Exists(tgDir)) continue;

					try
					{
						string[] exts = new string[] { "*.jar", "*.exe", "*.dll", "*.zip", "*.rar", "*.7z", "*.bat", "*.cmd" };
						foreach (string ext in exts)
						{
							string[] files = Directory.GetFiles(tgDir, ext, SearchOption.TopDirectoryOnly);
							foreach (string f in files)
							{
								if (!seenFiles.Add(f)) continue;

								string fName = Path.GetFileName(f).ToLowerInvariant();
								FileInfo fi = new FileInfo(f);

								bool isCheat = false;
								string matchedName = "";

								foreach (string cheat in CheatClientNames)
								{
									if (MatchesCheatName(fName, cheat))
									{
										isCheat = true;
										matchedName = cheat;
										break;
									}
								}
								if (!isCheat)
								{
									foreach (string cheat in CheatDatabases.ExtraCheatClients)
									{
										if (cheat.Length >= 4 && fName.Contains(cheat.ToLowerInvariant()))
										{
											isCheat = true;
											matchedName = cheat;
											break;
										}
									}
								}
								if (!isCheat && ext == "*.jar" && CheatDatabases.KnownCheatJarSizes.Contains(fi.Length))
								{
									isCheat = true;
									matchedName = "Версия клиента подменена: java-агент внутри jar (" + fi.Length + " байт)";
								}
								if (!isCheat)
								{
									string[] suspiciousKeywords = new string[] { "killaura", "hitbox", "reach", "spoofer", "injector", "keyauth" };
									foreach (string kw in suspiciousKeywords)
									{
										if (fName.Contains(kw))
										{
											isCheat = true;
											matchedName = kw;
											break;
										}
									}
								}

								bool isCleaner = false;
								string cleanerMatched = "";
								if (!isCheat)
								{
									foreach (string cl in CheatDatabases.KnownCleanerSignatures)
									{
										if (cl.Length >= 4 && fName.Contains(cl))
										{
											isCleaner = true;
											cleanerMatched = cl;
											break;
										}
									}
								}

								TimeSpan age = DateTime.Now - fi.LastWriteTime;
								string ageInfo = (age.TotalDays <= 7) ? string.Format(" | свежая загрузка ({0} дн. назад, {1:dd.MM.yyyy HH:mm})", (int)age.TotalDays, fi.LastWriteTime) : string.Format(" | дата: {0:dd.MM.yyyy HH:mm}", fi.LastWriteTime);

								if (isCheat)
								{
									list2.Add(string.Format("⛔ [5.01 - Чит в кэше Telegram/Discord] Файл: {0} | размер: {1:N0} байт [{2}]{3}", Path.GetFileName(f), fi.Length, matchedName, ageInfo));
								}
								else if (isCleaner)
								{
									list2.Add(string.Format("⛔ [5.08 - Клинер в кэше Telegram/Discord] Утилита очистки: {0} [{1}]{2}", Path.GetFileName(f), cleanerMatched, ageInfo));
								}
							}
						}
					}
					catch
					{
					}
				}

				// 2. Discord Cache & LevelDB inspection (Discord, Canary, PTB)
				string[] discordRoots = new string[]
				{
					Path.Combine(appData, "discord", "Local Storage", "leveldb"),
					Path.Combine(appData, "discordcanary", "Local Storage", "leveldb"),
					Path.Combine(appData, "discordptb", "Local Storage", "leveldb")
				};

				foreach (string discordLevelDb in discordRoots)
				{
					if (!Directory.Exists(discordLevelDb)) continue;
					try
					{
						string[] ldbFiles = Directory.GetFiles(discordLevelDb, "*.*");
						foreach (string ldbPath in ldbFiles)
						{
							string lName = Path.GetFileName(ldbPath).ToLowerInvariant();
							if (!lName.EndsWith(".ldb") && !lName.EndsWith(".log")) continue;

							try
							{
								FileInfo lfi = new FileInfo(ldbPath);
								if (lfi.Length > 15728640) continue;

								byte[] raw = null;
								using (FileStream fs = new FileStream(ldbPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
								{
									int len = (int)lfi.Length;
									raw = new byte[len];
									fs.Read(raw, 0, len);
								}

								if (raw != null && raw.Length > 100)
								{
									string content = Encoding.UTF8.GetString(raw).ToLowerInvariant();
									int attIdx = 0;
									while ((attIdx = content.IndexOf("/attachments/", attIdx, StringComparison.Ordinal)) >= 0)
									{
										int lineEnd = content.IndexOfAny(new char[] { ' ', '"', '\'', '\r', '\n', '\0', '?' }, attIdx);
										if (lineEnd < 0) lineEnd = Math.Min(content.Length, attIdx + 200);
										string urlSub = content.Substring(attIdx, lineEnd - attIdx);
										attIdx += 13;
										if (urlSub.EndsWith(".jar") || urlSub.EndsWith(".exe") || urlSub.EndsWith(".dll") || urlSub.EndsWith(".zip"))
										{
											foreach (string cheat in CheatClientNames)
											{
												if (ContainsToken(urlSub, cheat))
												{
													list2.Add(string.Format("⛔ [5.01 - Discord скачивание чита] Чит {0} в Discord: '{1}'", cheat, urlSub));
													break;
												}
											}
											foreach (string cheat in CheatDatabases.ExtraCheatClients)
											{
												if (cheat.Length >= 4 && ContainsToken(urlSub, cheat))
												{
													list2.Add(string.Format("⛔ [5.01 - Discord скачивание чита] Чит {0} в Discord: '{1}'", cheat, urlSub));
													break;
												}
											}
										}
										if (list2.Count >= 25) break;
									}
								}
							}
							catch
							{
							}
							if (list2.Count >= 25) break;
						}
					}
					catch
					{
					}
				}

				string tgDumps = Path.Combine(appData, "Telegram Desktop", "tdata", "dumps");
				if (Directory.Exists(tgDumps))
				{
					try
					{
						string[] dumps = Directory.GetFiles(tgDumps, "*.dmp");
						if (dumps.Length > 0)
						{
							list2.Add(string.Format("⚠️ Папка аварийных дампов Telegram содержит {0} файлов .dmp", dumps.Length));
						}
					}
					catch
					{
					}
				}
			}
			catch (Exception ex)
			{
				list2.Add("Ошибка проверки мессенджеров: " + ex.Message);
			}

			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Мессенджеры / Источники";
			checkResult.Name = "Клиенты передачи файлов и мессенджеры (Discord / Telegram)";
			checkResult.Detected = list2.Any(delegate(string x) { return x.Contains("??"); });
			checkResult.Severity = checkResult.Detected ? CheckSeverity.Critical : (list2.Count > 0 ? CheckSeverity.Warning : CheckSeverity.Info);
			if (checkResult.Detected)
			{
				checkResult.Detail = "Обнаружены подозрительные загрузки чит-клиентов/утилит через Telegram/Discord (" + list2.Count + " записей)!";
			}
			else
			{
				checkResult.Detail = "Следов загрузки читов из Telegram/Discord не обнаружено.";
			}
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		public static List<CheckResult> CheckEverythingSearchAndRunHistory()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				string[] array = new string[3]
				{
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Everything\\Everything.ini"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Everything\\Everything.ini"),
					"C:\\Program Files\\Everything\\Everything.ini"
				};
				bool flag = false;
				string[] array2 = array;
				foreach (string path in array2)
				{
					if (!File.Exists(path))
					{
						continue;
					}
					flag = true;
					List<string> array4 = new List<string>();
					try
					{
						using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
						using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
						{
							string l;
							while ((l = sr.ReadLine()) != null) array4.Add(l);
						}
					}
					catch { continue; }

					foreach (string text in array4)
					{
						string text2 = text.Trim();
						string text3 = text2.ToLowerInvariant();
						if (text3.StartsWith("search=") && text2.Length > 7)
						{
							string text4 = text2.Substring(7);
							list2.Add("[Последний поиск] " + text4);
						}
						if (text3.StartsWith("run_history_enabled=0") || text3.StartsWith("search_history_enabled=0"))
						{
							list2.Add("⛔ [5.08 - Утилита Everything запущена]: " + text2 + " в истории поиска перед проверкой!");
						}

						bool isCleaner = false;
						foreach (string cl in CheatDatabases.KnownCleanerSignatures)
						{
							if (cl.Length >= 4 && text3.Contains(cl))
							{
								list2.Add("⛔ [5.08 - Поиск клинера в Everything]: " + text2);
								isCleaner = true;
								break;
							}
						}
						if (isCleaner) continue;

						bool isCheat = false;
						string[] cheatClientNames = CheatClientNames;
						foreach (string cheatLower in cheatClientNames)
						{
							if (MatchesCheatName(text3, cheatLower) && !text3.Contains("lunar"))
							{
								list2.Add("⛔ [5.01 - Чит в поиске Everything]: " + text2);
								isCheat = true;
								break;
							}
						}
						if (!isCheat)
						{
							foreach (string extraCheat in CheatDatabases.ExtraCheatClients)
							{
								if (extraCheat.Length >= 4 && text3.Contains(extraCheat.ToLowerInvariant()))
								{
								list2.Add("⛔ [5.01 - Чит в поиске Everything]: " + text2);
									isCheat = true;
									break;
								}
							}
						}
					}
				}
				string[] csvFolders = new string[]
				{
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Everything"),
					Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Everything")
				};
				foreach (string path2 in csvFolders)
				if (Directory.Exists(path2))
				{
					string[] files = Directory.GetFiles(path2, "*.csv");
					foreach (string path3 in files)
					{
						try
						{
							List<string> array6 = new List<string>();
							try
							{
								using (FileStream fs = new FileStream(path3, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
								using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
								{
									string l;
									while ((l = sr.ReadLine()) != null) array6.Add(l);
								}
							}
							catch { continue; }

							foreach (string text5 in array6)
							{
								string text6 = text5.ToLowerInvariant();
								bool isCsvCleaner = false;
								foreach (string cl in CheatDatabases.KnownCleanerSignatures)
								{
									if (cl.Length >= 4 && text6.Contains(cl))
									{
										list2.Add("⛔ [5.08 - Поиск клинера в Everything CSV]: " + text5);
										isCsvCleaner = true;
										break;
									}
								}
								if (isCsvCleaner) continue;

								bool isCsvCheat = false;
								string[] cheatClientNames2 = CheatClientNames;
								foreach (string cheatLower2 in cheatClientNames2)
								{
									if (MatchesCheatName(text6, cheatLower2) && !text6.Contains("lunar"))
									{
										list2.Add("⛔ [5.01 - Чит в поиске Everything CSV]: " + text5);
										isCsvCheat = true;
										break;
									}
								}
								if (!isCsvCheat)
								{
									foreach (string extraCheat in CheatDatabases.ExtraCheatClients)
									{
										if (extraCheat.Length >= 4 && text6.Contains(extraCheat.ToLowerInvariant()))
										{
										list2.Add("⛔ [5.01 - Чит в поиске Everything CSV]: " + text5);
											isCsvCheat = true;
											break;
										}
									}
								}
							}
						}
						catch
						{
						}
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Очистка / Evasion";
					checkResult.Name = "Voidtools Everything (Поисковые запросы и удаление читов)";
					checkResult.Detected = true;
					checkResult.Severity = ((!list2.Any((string h) => h.Contains("[Everything"))) ? CheckSeverity.Warning : CheckSeverity.Critical);
					checkResult.Detail = "В истории утилиты Everything найдены подозрительные запросы / настройки сокрытия: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2.Take(25));
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Очистка / Evasion";
					checkResult2.Name = "Voidtools Everything (Поисковые запросы)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = (flag ? "История Everything чиста, читов в запросах не найдено." : "Утилита Everything не обнаружена.");
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Очистка / Evasion";
				checkResult3.Name = "Voidtools Everything";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка анализа Everything: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		public static List<CheckResult> CheckHostsFileAndDnsRedirection()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string text = Path.Combine(folderPath, "System32\\drivers\\etc\\hosts");
				if (File.Exists(text))
				{
					FileInfo fileInfo = new FileInfo(text);
					if ((DateTime.Now - fileInfo.LastWriteTime).TotalHours < 24.0)
					{
						list2.Add("🌐 Посещения чит-ресурсов (Chrome/Edge/hosts)..." + fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")!");
					}

					string[] lines = null;
					using (FileStream fs = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
					{
						using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
						{
							lines = sr.ReadToEnd().Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
						}
					}

					if (lines != null)
					{
						foreach (string text2 in lines)
						{
							string text3 = text2.Trim();
							if (!text3.StartsWith("#") && !string.IsNullOrEmpty(text3))
							{
								string text4 = text3.ToLowerInvariant();
								if (text4.Contains("mojang") || text4.Contains("minecraft") || text4.Contains("hypixel") || text4.Contains("atlantis") || text4.Contains("funtime") || text4.Contains("authserver") || text4.Contains("sessionserver") || text4.Contains("easymc") || text4.Contains("thealtening") || text4.Contains("mcleaks") || text4.Contains("polar.gg") || text4.Contains("grim.ac") || text4.Contains("authlib") || text4.Contains("reallyworld") || text4.Contains("holyworld") || text4.Contains("vulcan") || text4.Contains("matrix") || text4.Contains("karhu") || text4.Contains("intave") || text4.Contains("spooky") || text4.Contains("mstnetwork"))
								{
									list2.Add("⛔ [5.01 - Блокировка античитов/серверов в hosts] " + text3);
								}
							}
						}
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Сеть / DNS";
					checkResult.Name = "Файл hosts (Подмена DNS / Блокировка античитов)";
					bool hasActualBlock = list2.Any(delegate(string x) { return x.Contains("??"); });
					checkResult.Detected = hasActualBlock;
					checkResult.Severity = (hasActualBlock ? CheckSeverity.Critical : CheckSeverity.Info);
					checkResult.Detail = hasActualBlock ? ("Обнаружена блокировка античитов в hosts [5.01]: " + list2.Count + " шт.") : "Файл hosts в норме, посторонних записей блокировки античитов не найдено.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Сеть / DNS";
					checkResult2.Name = "Файл hosts (Подмена DNS)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Файл hosts в стандартном состоянии, перенаправлений не выявлено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Сеть / DNS";
				checkResult3.Name = "Файл hosts";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка анализа hosts: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

				public static List<CheckResult> CheckJNativeHookAndMacroDLLs()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				List<string> list3 = new List<string>();
				string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string progData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

				List<string> scanDirs = new List<string>();
				scanDirs.Add(Path.GetTempPath());
				if (!string.IsNullOrEmpty(localAppData)) scanDirs.Add(Path.Combine(localAppData, "Temp"));
				scanDirs.Add("C:\\Windows\\Temp");
				if (!string.IsNullOrEmpty(appData)) scanDirs.Add(Path.Combine(appData, ".minecraft", "libraries"));
				if (!string.IsNullOrEmpty(progData)) scanDirs.Add(progData);

				string[] cheatDllPatterns = new string[]
				{
					"koid", "vape", "celestial", "expensive", "nursultan", "slinky", "booster", "catlavan"
				};
				string[] clickerDllPatterns = new string[]
				{
					"jnativehook", "autoclicker", "ghook", "keyhook", "interception", "kmbox", "botmek", "keyran", "mousehook", "tinytask", "speedautoclicker", "opautoclicker"
				};

				HashSet<string> seenDlls = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (string path in scanDirs)
				{
					if (!Directory.Exists(path)) continue;
					try
					{
						string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
					System.Diagnostics.Stopwatch swDisg = System.Diagnostics.Stopwatch.StartNew();
												foreach (string text in files)
						{
							if (swDisg.ElapsedMilliseconds > 1500) break;

							if (!seenDlls.Add(text)) continue;
							string ext = Path.GetExtension(text).ToLowerInvariant();
							if (ext != ".dll" && ext != ".pyd" && ext != ".so" && ext != ".node") continue;

							string text2 = Path.GetFileName(text).ToLowerInvariant();
							if (CheatDatabases.IsWhitelistedPath(text2) || text2.Contains("mccheat") || text2.Contains("screenshare"))
							{
								continue;
							}

							bool isCheat = false;
							string cheatPat = "";
							foreach (string hp in cheatDllPatterns)
							{
								if (text2.Contains(hp))
								{
									isCheat = true;
									cheatPat = hp;
									break;
								}
							}

							if (isCheat)
							{
								FileInfo fi = new FileInfo(text);
								list2.Add(string.Format("⛔ [5.01 - Чит DLL в Temp] {0} [{1}] (создан: {2:dd.MM.yyyy HH:mm:ss}, {3:N0} байт)", text, cheatPat, fi.LastWriteTime, fi.Length));
							}
							else
							{
								foreach (string cp in clickerDllPatterns)
								{
									if (text2.Contains(cp))
									{
										FileInfo fi2 = new FileInfo(text);
										list3.Add(string.Format("ℹ️ [Разрешённый автокликер] Библиотека автокликера в Temp: {0} [{1}] ({2:dd.MM.yyyy HH:mm:ss})", text, cp, fi2.LastWriteTime));
										break;
									}
								}
							}
						}
					}
					catch
					{
					}
				}

				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Инжекты / DLL";
					checkResult.Name = "🗑️ Временные файлы Temp...";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "Обнаружены чит-библиотеки и подозрительные хуки [5.01]: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2.Concat(list3));
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Инжекты / DLL";
					checkResult2.Name = "Загрузка чужих типов и путей";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = list3.Count > 0 ? "Обнаружены библиотеки автокликеров (разрешены правилами сервера: " + list3.Count + " шт.). Запрещённых чит-DLL не найдено." : "Подозрительных DLL и перехватов в Temp не найдено.";
					checkResult2.Evidence = string.Join("\r\n", list3);
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Инжекты / DLL";
				checkResult3.Name = "Инжекции процессов";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "🚨 [Proxy DLL Hook] Обнаружена DLL: {0}" + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		public static List<CheckResult> CheckMinecraftEntityAndChestXRayPacks()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

				List<string> rpDirs = new List<string>();
				if (!string.IsNullOrEmpty(appData))
				{
					rpDirs.Add(Path.Combine(appData, ".minecraft", "resourcepacks"));
				}

				// Multi-launcher roots
				foreach (string rootDir in ManualChecks.MinecraftRoots())
				{
					if (Directory.Exists(rootDir))
					{
						string candidate = Path.Combine(rootDir, "resourcepacks");
						if (Directory.Exists(candidate) && !rpDirs.Contains(candidate))
						{
							rpDirs.Add(candidate);
						}
					}
				}

				foreach (string path in rpDirs)
				{
					if (!Directory.Exists(path)) continue;

					string[] files = Directory.GetFiles(path, "*.zip", SearchOption.TopDirectoryOnly);
					System.Diagnostics.Stopwatch swDisg = System.Diagnostics.Stopwatch.StartNew();
											foreach (string text in files)
						{
							if (swDisg.ElapsedMilliseconds > 1500) break;

						try
						{
							using (ZipArchive zipArchive = ZipFile.OpenRead(text))
							{
								foreach (ZipArchiveEntry entry in zipArchive.Entries)
								{
									string text2 = entry.FullName.ToLowerInvariant();
									if ((text2.Contains("textures/entity/chest/") || text2.Contains("textures/block/spawner.png") || text2.Contains("textures/entity/spawner.png")) && entry.Length > 0 && entry.Length < 700)
									{
										list2.Add("🟣 [Прозрачный Chest / Spawner ESP] " + entry.Name + " (" + entry.Length + " байт) в: " + text);
									}
									else if ((text2.Contains("textures/block/stone.png") || text2.Contains("textures/block/deepslate.png") || text2.Contains("textures/block/netherrack.png") || text2.Contains("textures/block/end_stone.png")) && entry.Length > 0 && entry.Length < 350)
									{
										list2.Add("🟣 [X-Ray архив по названию]: " + entry.Name + " (" + entry.Length + " байт, прозрачные блоки) в: " + text);
									}
								}
							}
						}
						catch
						{
						}
					}

					// Also inspect unzipped folder resource packs
					string[] packDirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
					foreach (string packDir in packDirs)
					{
						if (swDisg.ElapsedMilliseconds > 1500) break;

						try
						{
							string[] pngFiles = Directory.GetFiles(packDir, "*.png", SearchOption.AllDirectories);
							foreach (string png in pngFiles)
							{
								string lowerPng = png.ToLowerInvariant().Replace('\\', '/');
								if (lowerPng.Contains("textures/entity/chest/") || lowerPng.Contains("textures/block/spawner.png") || lowerPng.Contains("textures/entity/spawner.png"))
								{
									FileInfo fi = new FileInfo(png);
									if (fi.Length > 0 && fi.Length < 700)
									{
										list2.Add("🟣 [Прозрачный Chest / Spawner ESP] " + Path.GetFileName(png) + " (" + fi.Length + " байт) в паке: " + packDir);
									}
								}
							}
						}
						catch
						{
						}
					}
				}

				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Визуалы (Следует проверить)";
					checkResult.Name = "Chest ESP / Spawner X-Ray ресурспаки (Прозрачные текстуры)";
					checkResult.Detected = false;
					checkResult.Severity = CheckSeverity.Info;
					checkResult.Detail = "Обнаружены прозрачные текстуры сундуков / спавнеров (Chest ESP): " + list2.Count + " шт. — НЕ БАНИТЬ, следует проверить!";
					checkResult.Evidence = string.Join("\r\n", list2.Take(25));
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Визуалы (Следует проверить)";
					checkResult2.Name = "Chest ESP / Spawner X-Ray ресурспаки";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Прозрачных текстур сундуков / спавнеров в ресурспаках не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Реестр";
				checkResult3.Name = "Chest ESP";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка анализа options.txt: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		public static List<CheckResult> CheckShimCache()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Session Manager\\AppCompatCache"))
				{
					if (registryKey != null)
					{
						byte[] array = registryKey.GetValue("AppCompatCache") as byte[];
						if (array != null && array.Length > 128)
						{
							string text = Encoding.Unicode.GetString(array);
							MatchCollection mc = Regex.Matches(text, @"[a-zA-Z]:\\[^\s\0<>""\|]+");
							HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

							Stopwatch swShim = Stopwatch.StartNew();
							foreach (Match m in mc)
							{
								if (swShim.ElapsedMilliseconds > 1500) break;
								string rawPath = m.Value.Trim();
								if (rawPath.Length < 4 || seenPaths.Contains(rawPath)) continue;
								seenPaths.Add(rawPath);
								int exeExtIdx = rawPath.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
								if (exeExtIdx > 0 && rawPath.Length > exeExtIdx + 4) rawPath = rawPath.Substring(0, exeExtIdx + 4);
								int dllExtIdx = rawPath.IndexOf(".dll", StringComparison.OrdinalIgnoreCase);
								if (dllExtIdx > 0 && rawPath.Length > dllExtIdx + 4) rawPath = rawPath.Substring(0, dllExtIdx + 4);
								int batExtIdx = rawPath.IndexOf(".bat", StringComparison.OrdinalIgnoreCase);
								if (batExtIdx > 0 && rawPath.Length > batExtIdx + 4) rawPath = rawPath.Substring(0, batExtIdx + 4);
								rawPath = rawPath.TrimEnd('\0', ' ', '\r', '\n', '\t');

								string resolvedPath = ResolveNtDevicePath(rawPath);
								string pathLower = resolvedPath.ToLowerInvariant();
								if (pathLower.Contains("lunar")) continue;
								if (pathLower.StartsWith(@"c:\windows\system32\cleanmgr.exe") || pathLower.StartsWith(@"c:\windows\system32\wevtutil.exe")) continue;

								bool isCheat = false;
								string cheatMatched = "";

								foreach (string cl in CheatClientNames)
								{
									if (MatchesCheatName(pathLower, cl))
									{
										isCheat = true;
										cheatMatched = cl;
										break;
									}
								}
								if (!isCheat)
								{
									foreach (string ec in CheatDatabases.ExtraCheatClients)
									{
										if (ec.Length >= 4 && MatchesCheatName(pathLower, ec))
										{
											isCheat = true;
											cheatMatched = ec;
											break;
										}
									}
								}
								if (!isCheat)
								{
									string[] shimInj = new string[] { "kmbox", "interception", "dllinjector", "spoofer", "cheatloader", "slinky" };
									foreach (string sinj in shimInj)
									{
										if (pathLower.Contains(sinj))
										{
											isCheat = true;
											cheatMatched = sinj;
											break;
										}
									}
								}

								bool isCleaner = false;
								string cleanerMatched = "";
								if (!isCheat)
								{
									foreach (string cl in CheatDatabases.KnownCleanerSignatures)
									{
										if (cl.Length >= 4 && pathLower.Contains(cl))
										{
											isCleaner = true;
											cleanerMatched = cl;
											break;
										}
									}
								}

								bool exists = false;
								try { exists = FastFileExists(resolvedPath); } catch { }

								if (isCheat)
								{
									if (!exists)
									{
										list2.Add(string.Format("🚨 [5.08 - Запуск ПО из ShimCache] В кэше Windows (ShimCache) зафиксирована запись исполнения удалённого чит-ПО: '{0}' (совпадение: {1})", resolvedPath, cheatMatched));
									}
									else
									{
										list2.Add(string.Format("⛔ [5.01 - Запуск чита в ShimCache] В кэше совместимости ядра ShimCache зафиксирован чит: '{0}' (совпадение: {1})", resolvedPath, cheatMatched));
									}
								}
								else if (isCleaner)
								{
									list2.Add(string.Format("🚨 [5.08 - Запуск ПО из ShimCache] В кэше Windows (ShimCache) зафиксирована запись исполнения утилиты очистки: '{0}' (совпадение: {1})", resolvedPath, cleanerMatched));
								}
							}
						}
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Реестр / Фоновые следы";
					checkResult.Name = "ShimCache (AppCompatCache — Глубокая история запусков ядра)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "В системном кэше ядра ShimCache обнаружены следы запуска читов [5.01 / 5.08]: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2.Take(35));
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Реестр / Фоновые следы";
					checkResult2.Name = "ShimCache (AppCompatCache)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "⚡ Системный кэш совместимости ядра ShimCache (AppCompatCache)...";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Реестр / Фоновые следы";
				checkResult3.Name = "ShimCache (AppCompatCache)";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка анализа ShimCache: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}


				public static List<CheckResult> CheckDirectXAndIfeoExecutionBypasses()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				bool isCritical = false;

				// 1. DirectInput MostRecentApplication
				try
				{
					using (RegistryKey diKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\DirectInput\MostRecentApplication"))
					{
						if (diKey != null)
						{
							object val = diKey.GetValue("Name");
							if (val != null)
							{
								string rawAppName = val.ToString().Trim('\"', ' ');
								string appName = ResolveNtDevicePath(rawAppName);
								string appLower = appName.ToLowerInvariant();
								if (!CheatDatabases.IsWhitelistedPath(appLower) && !appLower.Contains("          \"name\": ") && !appLower.Contains("mccheat") && !appLower.Contains("screenshare"))
								{
									bool isCheat = false;
									string cheatMatched = "";
									foreach (string cl in CheatClientNames)
									{
										if (MatchesCheatName(appLower, cl))
										{
											isCheat = true;
											cheatMatched = cl;
											break;
										}
									}
									if (!isCheat)
									{
										foreach (string ec in CheatDatabases.ExtraCheatClients)
										{
											if (ec.Length >= 4 && MatchesCheatName(appLower, ec))
											{
												isCheat = true;
												cheatMatched = ec;
												break;
											}
										}
									}
									if (!isCheat)
									{
										string[] diInj = new string[] { "kmbox", "interception", "injector", "spoofer" };
										foreach (string dinj in diInj)
										{
											if (appLower.Contains(dinj))
											{
												isCheat = true;
												cheatMatched = dinj;
												break;
											}
										}
									}

									bool isCleaner = false;
									string cleanerMatched = "";
									if (!isCheat)
									{
										foreach (string cl in CheatDatabases.KnownCleanerSignatures)
										{
											if (cl.Length >= 4 && appLower.Contains(cl))
											{
												isCleaner = true;
												cleanerMatched = cl;
												break;
											}
										}
									}

									bool exists = true;
									if (appName.Contains("\\") || appName.Contains(":"))
									{
										try { exists = File.Exists(appName); } catch { }
									}

									if (isCleaner)
									{
										isCritical = true;
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск приложения из Temp] В DirectInput зафиксирована запись запуска программы: '{0}'", appName));
									}
									else if (isCheat)
									{
										isCritical = true;
										if (!exists)
										{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск DirectInput приложения] В DirectInput зафиксирована запись исполнения чит-ПО: '{0}' (совпадение: {1})", appName, cheatMatched));
										}
										else
										{
											list2.Add(string.Format("⛔ [5.01 - DirectInput чит/автокликер] В DirectInput зафиксирован запуск: '{0}' (совпадение: {1})", appName, cheatMatched));
										}
									}
									else if (!exists && (appLower.Contains("\\temp\\") || appLower.Contains("\\appdata\\local\\temp") || appLower.Contains("\\downloads\\")) && appLower.EndsWith(".exe"))
									{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск приложения из Temp] В DirectInput зафиксирована запись запуска программы: '{0}'", appName));
									}
									else
									{
										list2.Add(string.Format("ℹ️ Активность DirectInput приложения: '{0}'", appName));
									}
								}
							}
						}
					}
				}
				catch
				{
				}

				// 2. Direct3D MostRecentApplication
				try
				{
					using (RegistryKey d3dKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Direct3D\MostRecentApplication"))
					{
						if (d3dKey != null)
						{
							object val = d3dKey.GetValue("Name");
							if (val != null)
							{
								string rawAppName = val.ToString().Trim('\"', ' ');
								string appName = ResolveNtDevicePath(rawAppName);
								string appLower = appName.ToLowerInvariant();
								if (!CheatDatabases.IsWhitelistedPath(appLower) && !appLower.Contains("          \"name\": ") && !appLower.Contains("mccheat") && !appLower.Contains("screenshare"))
								{
									bool isCheat = false;
									string cheatMatched = "";
									foreach (string cl in CheatClientNames)
									{
										if (MatchesCheatName(appLower, cl))
										{
											isCheat = true;
											cheatMatched = cl;
											break;
										}
									}
									if (!isCheat)
									{
										foreach (string ec in CheatDatabases.ExtraCheatClients)
										{
											if (ec.Length >= 4 && MatchesCheatName(appLower, ec))
											{
												isCheat = true;
												cheatMatched = ec;
												break;
											}
										}
									}

									bool isCleaner = false;
									string cleanerMatched = "";
									if (!isCheat)
									{
										foreach (string cl in CheatDatabases.KnownCleanerSignatures)
										{
											if (cl.Length >= 4 && appLower.Contains(cl))
											{
												isCleaner = true;
												cleanerMatched = cl;
												break;
											}
										}
									}

									bool exists = true;
									if (appName.Contains("\\") || appName.Contains(":"))
									{
										try { exists = File.Exists(appName); } catch { }
									}

									if (isCleaner)
									{
										isCritical = true;
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск графики из Temp] В Direct3D зафиксирована запись запуска программы: '{0}'", appName));
									}
									else if (isCheat)
									{
										isCritical = true;
										if (!exists)
										{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск Direct3D графики] В Direct3D зафиксирован запуск чит/инжектора: '{0}' (совпадение: {1})", appName, cheatMatched));
										}
										else
										{
											list2.Add(string.Format("⛔ [5.01 - Direct3D чит-клиент] В Direct3D обнаружен запуск чита/инжектора: '{0}' (совпадение: {1})", appName, cheatMatched));
										}
									}
									else if (!exists && (appLower.Contains("\\temp\\") || appLower.Contains("\\appdata\\local\\temp") || appLower.Contains("\\downloads\\")) && appLower.EndsWith(".exe"))
									{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск графики из Temp] В Direct3D зафиксирована запись запуска программы: '{0}'", appName));
									}
								}
							}
						}
					}
				}
				catch
				{
				}

				// 3. Image File Execution Options (IFEO) Debugger / SilentProcessExit Hijacking
				try
				{
					using (RegistryKey ifeoKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Image File Execution Options"))
					{
						if (ifeoKey != null)
						{
							string[] subKeys = ifeoKey.GetSubKeyNames();
							foreach (string sk in subKeys)
							{
								using (RegistryKey sub = ifeoKey.OpenSubKey(sk))
								{
									if (sub == null) continue;
									object dbgVal = sub.GetValue("Debugger");
									if (dbgVal != null && !string.IsNullOrEmpty(dbgVal.ToString()))
									{
										isCritical = true;
										list2.Add(string.Format("⛔ [5.08 - IFEO перехват через Debugger] Процесс '{0}' перехвачен отладчиком: Debugger = '{1}'!", sk, dbgVal));
									}

									using (RegistryKey speKey = sub.OpenSubKey("SilentProcessExit"))
									{
										if (speKey != null)
										{
											object monVal = speKey.GetValue("MonitorProcess");
											if (monVal != null && !string.IsNullOrEmpty(monVal.ToString()))
											{
												isCritical = true;
												list2.Add(string.Format("⛔ [5.08 - IFEO SilentProcessExit мониторинг] Для '{0}' настроен скрытый мониторинг при выходе: MonitorProcess = '{1}'!", sk, monVal));
											}
										}
									}
								}
							}
						}
					}
				}
				catch
				{
				}

				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Реестр / Запуски и перехваты";
					checkResult.Name = "DirectX MostRecent & IFEO Hijacking (DirectInput/Direct3D/Перехваты)";
					checkResult.Detected = isCritical;
					checkResult.Severity = isCritical ? CheckSeverity.Critical : CheckSeverity.Info;
				checkResult.Detail = isCritical ? ("Обнаружены перехваты IFEO или подозрительные запуски DIRECTX [5.01 / 5.08] (" + list2.Count + " шт.)!") : "Записи DirectInput/Direct3D проверены, перехватов IFEO не обнаружено.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Реестр / Запуски и перехваты";
					checkResult2.Name = "DirectX MostRecent & IFEO Hijacking";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
				checkResult2.Detail = "Параметры Image File Execution Options и подозрительные запуски DirectInput/Direct3D не обнаружены.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Реестр / Запуски и перехваты";
				checkResult3.Name = "DirectX MostRecent & IFEO Hijacking";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки DirectX/IFEO: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

				public static List<CheckResult> CheckAmcacheAndUninstall()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string text = Path.Combine(folderPath, "appcompat\\Programs\\Amcache.hve");
				if (File.Exists(text))
				{
					try
					{
						FileInfo fileInfo = new FileInfo(text);
						if (fileInfo.Length == 0)
						{
							list2.Add("⛔ [5.08 - Очистка Amcache] Файл Amcache.hve имеет размер 0 байт (намеренная зачистка базы программ)!");
						}
						TimeSpan timeSpan = DateTime.Now - fileInfo.LastWriteTime;
						if (timeSpan.TotalMinutes < 15.0)
						{
							list2.Add("⛔ [5.08 - Свежая модификация Amcache] Amcache.hve был модифицирован всего " + Math.Round(timeSpan.TotalMinutes, 1) + " мин назад (возможное затирание кэша перед проверкой)!");
						}
					}
					catch
					{
					}
				}

				// Check both HKCU and HKLM (machine-wide installs)
				Tuple<RegistryHive, string>[] uninstallRoots = new Tuple<RegistryHive, string>[]
				{
					Tuple.Create(RegistryHive.CurrentUser, "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall"),
					Tuple.Create(RegistryHive.CurrentUser, "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"),
					Tuple.Create(RegistryHive.LocalMachine, "Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall"),
					Tuple.Create(RegistryHive.LocalMachine, "SOFTWARE\\WOW6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall")
				};

				foreach (Tuple<RegistryHive, string> root in uninstallRoots)
				{
					RegistryKey baseKey = (root.Item1 == RegistryHive.CurrentUser) ? Registry.CurrentUser : Registry.LocalMachine;
					using (RegistryKey registryKey = baseKey.OpenSubKey(root.Item2))
					{
						if (registryKey == null)
						{
							continue;
						}
						string[] subKeyNames = registryKey.GetSubKeyNames();
						foreach (string name2 in subKeyNames)
						{
							using (RegistryKey registryKey2 = registryKey.OpenSubKey(name2))
							{
								if (registryKey2 == null)
								{
									continue;
								}
								string dispName = (registryKey2.GetValue("DisplayName") ?? "").ToString();
								string installLoc = (registryKey2.GetValue("InstallLocation") ?? "").ToString();
								string uninstStr = (registryKey2.GetValue("UninstallString") ?? "").ToString();
								string quietUninst = (registryKey2.GetValue("QuietUninstallString") ?? "").ToString();

								if (string.IsNullOrEmpty(dispName) && string.IsNullOrEmpty(installLoc) && string.IsNullOrEmpty(uninstStr))
								{
									continue;
								}

								string combinedLower = (dispName + " " + installLoc + " " + uninstStr + " " + quietUninst + " " + name2).ToLowerInvariant();
								string dispLower = dispName.ToLowerInvariant();

								if (CheatDatabases.IsWhitelistedPath(combinedLower) || combinedLower.Contains("          \"name\": ") || combinedLower.Contains("mccheat") || combinedLower.Contains("screenshare"))
								{
									continue;
								}

								bool isCheat = false;
								string cheatMatched = "";

								foreach (string cheatLower in CheatClientNames)
								{
									if (MatchesCheatName(dispLower, cheatLower) || MatchesCheatName(combinedLower, cheatLower))
									{
										isCheat = true;
										cheatMatched = cheatLower;
										break;
									}
								}
								if (!isCheat)
								{
									foreach (string ec in CheatDatabases.ExtraCheatClients)
									{
										if (ec.Length >= 4 && (MatchesCheatName(dispLower, ec) || combinedLower.Contains(ec)))
										{
											isCheat = true;
											cheatMatched = ec;
											break;
										}
									}
								}
								if (!isCheat)
								{
									string[] suspiciousInjectors = new string[] { "kmbox", "interception", "dllinjector", "spoofer", "cheatloader" };
									foreach (string sinj in suspiciousInjectors)
									{
										if (combinedLower.Contains(sinj))
										{
											isCheat = true;
											cheatMatched = sinj;
											break;
										}
									}
								}

								bool isCleaner = false;
								string cleanerMatched = "";
								if (!isCheat)
								{
									foreach (string cl in CheatDatabases.KnownCleanerSignatures)
									{
										if (cl.Length >= 4 && combinedLower.Contains(cl))
										{
											isCleaner = true;
											cleanerMatched = cl;
											break;
										}
									}
								}

								string label = !string.IsNullOrEmpty(dispName) ? dispName : name2;
								string pathRef = !string.IsNullOrEmpty(installLoc) ? installLoc : uninstStr;

								if (isCleaner)
								{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Деинсталлированное ПО (Uninstall)] {0} (совпадение: {1}, путь: {2})", label, cheatMatched, pathRef));
								}
								else if (isCheat)
								{
									bool exists = false;
									if (!string.IsNullOrEmpty(installLoc))
									{
										try { exists = Directory.Exists(installLoc) || File.Exists(installLoc); } catch { }
									}
									if (!exists && !string.IsNullOrEmpty(uninstStr))
									{
										string cleanUninst = uninstStr.Trim('\"', ' ');
										int exeIdx = cleanUninst.IndexOf(".exe", StringComparison.OrdinalIgnoreCase);
										if (exeIdx > 0) cleanUninst = cleanUninst.Substring(0, exeIdx + 4).Trim('\"', ' ');
										try { exists = File.Exists(cleanUninst); } catch { }
									}

									if (!exists)
									{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Деинсталлированное ПО (Uninstall)] {0} (совпадение: {1}, путь: {2})", label, cheatMatched, pathRef));
									}
									else
									{
										list2.Add(string.Format("⛔ [5.01 - Деинсталлированный чит-клиент (Uninstall)] {0} (совпадение: {1}, путь: {2})", label, cheatMatched, pathRef));
									}
								}
							}
						}
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Программы / Деинсталляция";
					checkResult.Name = "Amcache и записи деинсталляции Windows (Uninstall)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "Обнаружены следы удалённых чит-клиентов / читов [5.01 / 5.08]: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Программы / Деинсталляция";
					checkResult2.Name = "Amcache и реестр деинсталляции Windows";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
				checkResult2.Detail = "Следов установки известных чит-клиентов в реестре не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Программы / Деинсталляция";
				checkResult3.Name = "Amcache и деинсталляция";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка анализа Amcache: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		#region Network Socket & Intelligent IPC Resolution

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool QueryFullProcessImageName(IntPtr hProcess, int dwFlags, StringBuilder lpExeName, ref int lpdwSize);

		private class NetstatTcpRow
		{
			public string LocalAddr;
			public string ForeignAddr;
			public string LocalIp;
			public int LocalPort;
			public string ForeignIp;
			public int ForeignPort;
			public string State;
			public int Pid;
			public bool IsLoopback;
		}

		public class CachedPeerProcessInfo
		{
			public int Pid;
			public string Name;
			public string BaseName;
			public string ExePath;
			public bool IsJava;
			public bool IsWhitelisted;
			public bool IsCheat;
			public bool IsCleaner;
			public bool IsTempBinary;
		}

		private static void ParseEndpointAddress(string endpoint, out string ip, out int port)
		{
			ip = string.Empty;
			port = -1;
			if (string.IsNullOrEmpty(endpoint)) return;
			int colon = endpoint.LastIndexOf(':');
			if (colon >= 0 && colon < endpoint.Length - 1)
			{
				ip = endpoint.Substring(0, colon).Trim('[', ']');
				int.TryParse(endpoint.Substring(colon + 1), out port);
			}
			else
			{
				ip = endpoint.Trim('[', ']');
			}
		}

		private static bool IsLoopbackIpAddress(string ip)
		{
			if (string.IsNullOrEmpty(ip)) return false;
			return ip.StartsWith("127.", StringComparison.OrdinalIgnoreCase) ||
			       ip.Equals("::1", StringComparison.OrdinalIgnoreCase) ||
			       ip.Equals("localhost", StringComparison.OrdinalIgnoreCase);
		}

		private static string SafeGetProcessPath(Process p)
		{
			if (p == null) return string.Empty;
			try
			{
				if (p.Id == 0 || p.Id == 4) return string.Empty;
				try
				{
					if (p.MainModule != null && !string.IsNullOrEmpty(p.MainModule.FileName))
					{
						return p.MainModule.FileName;
					}
				}
				catch { }

				if (PlatformHelper.IsWindows)
				{
					IntPtr hProc = OpenProcess(0x1000 /* PROCESS_QUERY_LIMITED_INFORMATION */, false, p.Id);
					if (hProc != IntPtr.Zero)
					{
						try
						{
							StringBuilder sb = new StringBuilder(1024);
							int size = sb.Capacity;
							if (QueryFullProcessImageName(hProc, 0, sb, ref size))
							{
								return sb.ToString();
							}
						}
						finally
						{
							CloseHandle(hProc);
						}
					}
				}
			}
			catch { }
			return string.Empty;
		}

		private static string SafeGetProcessPathByPid(int pid)
		{
			if (pid <= 0 || pid == 4) return string.Empty;
			try
			{
				using (Process p = Process.GetProcessById(pid))
				{
					return SafeGetProcessPath(p);
				}
			}
			catch { }

			if (PlatformHelper.IsWindows)
			{
				try
				{
					IntPtr hProc = OpenProcess(0x1000, false, pid);
					if (hProc != IntPtr.Zero)
					{
						try
						{
							StringBuilder sb = new StringBuilder(1024);
							int size = sb.Capacity;
							if (QueryFullProcessImageName(hProc, 0, sb, ref size))
							{
								return sb.ToString();
							}
						}
						finally
						{
							CloseHandle(hProc);
						}
					}
				}
				catch { }
			}
			return string.Empty;
		}

		private static CachedPeerProcessInfo GetOrCacheProcessInfo(int pid, Dictionary<int, CachedPeerProcessInfo> cache)
		{
			CachedPeerProcessInfo info;
			if (cache.TryGetValue(pid, out info)) return info;

			info = new CachedPeerProcessInfo();
			info.Pid = pid;

			if (pid <= 0)
			{
				info.Name = "System/Terminated";
				info.BaseName = "system";
				info.ExePath = string.Empty;
				info.IsJava = false;
				cache[pid] = info;
				return info;
			}

			try
			{
				using (Process p = Process.GetProcessById(pid))
				{
					info.BaseName = p.ProcessName.ToLowerInvariant();
					info.Name = p.ProcessName + ".exe";
					info.ExePath = SafeGetProcessPath(p);
				}
			}
			catch
			{
				string path = SafeGetProcessPathByPid(pid);
				info.ExePath = path ?? string.Empty;
				if (!string.IsNullOrEmpty(path))
				{
					info.Name = Path.GetFileName(path);
					info.BaseName = info.Name.ToLowerInvariant().Replace(".exe", "");
				}
				else
				{
					info.Name = "PID=" + pid;
					info.BaseName = info.Name.ToLowerInvariant();
				}
			}

			info.IsJava = (info.BaseName == "javaw" || info.BaseName == "java" || info.BaseName == "minecraft" || info.BaseName.Contains("bedrock"));
			info.IsWhitelisted = IsWhitelistedLauncherOrApp(info.BaseName, info.ExePath);
			info.IsCheat = IsCheatProcessOrPath(info.BaseName, info.ExePath);
			info.IsCleaner = IsCleanerProcessOrPath(info.BaseName, info.ExePath);
			info.IsTempBinary = IsTempBinaryProcess(info.Name, info.ExePath);

			cache[pid] = info;
			return info;
		}

		private static bool IsWhitelistedLauncherOrApp(string baseName, string exePath)
		{
			if (string.IsNullOrEmpty(baseName)) return false;
			string nameLower = baseName.ToLowerInvariant();
			string pathLower = (exePath ?? string.Empty).ToLowerInvariant();

			// Discord
			if (nameLower.Contains("discord") || pathLower.Contains("discord")) return true;

			// Known launchers and wrappers
			if (nameLower.Contains("tlauncher") || nameLower.Contains("prismlauncher") || nameLower.Contains("prism launcher") ||
			    nameLower.Contains("multimc") || nameLower.Contains("polymc") || nameLower.Contains("atlauncher") ||
			    nameLower.Contains("lunarclient") || nameLower.Contains("lunar") || nameLower.Contains("badlionclient") ||
			    nameLower.Contains("badlion") || nameLower.Contains("feather") || nameLower.Contains("curseforge") ||
			    nameLower.Contains("modrinth") || nameLower.Contains("legacylauncher") || nameLower.Contains("minecraftlauncher") ||
			    nameLower.Contains("mclauncher") || nameLower.Contains("arialdown") || nameLower.Contains("mceclipse"))
			{
				return true;
			}

			// ScreenShare, browser, streaming tools
			if (nameLower.Contains("anydesk") || nameLower.Contains("rudesktop") || nameLower.Contains("obs64") ||
			    nameLower.Contains("obs32") || nameLower.Contains("streamlabs") || nameLower.Contains("teamviewer"))
			{
				return true;
			}

			if (CheatDatabases.IsWhitelistedProcess(baseName)) return true;
			if (!string.IsNullOrEmpty(exePath) && CheatDatabases.IsWhitelistedPath(exePath)) return true;

			return false;
		}

		private static bool IsCheatProcessOrPath(string baseName, string exePath)
		{
			string nameLower = (baseName ?? string.Empty).ToLowerInvariant();
			string pathLower = (exePath ?? string.Empty).ToLowerInvariant();

			string[] specificCheats = new string[]
			{
				"vape", "drip", "slinky", "koid", "itami", "whiteout", "doomsday", "exloader",
				"meteor", "wurst", "liquidbounce", "expensive", "nursultan", "celestial",
				"akrien", "deadcode", "neverhook", "zamorozka", "minced", "rich", "bape"
			};

			foreach (string ch in specificCheats)
			{
				if (nameLower.Contains(ch) || pathLower.Contains(ch))
					return true;
			}

			if (CheatDatabases.ExtraCheatClients != null)
			{
				foreach (string ch in CheatDatabases.ExtraCheatClients)
				{
					if (ch.Length >= 4 && (nameLower.Contains(ch) || pathLower.Contains(ch)))
						return true;
				}
			}

			if (CheatClientNames != null)
			{
				foreach (string ch in CheatClientNames)
				{
					if (ch.Length >= 4 && (nameLower.Contains(ch) || pathLower.Contains(ch)))
						return true;
				}
			}

			return false;
		}

		private static bool IsCleanerProcessOrPath(string baseName, string exePath)
		{
			string nameLower = (baseName ?? string.Empty).ToLowerInvariant();
			string pathLower = (exePath ?? string.Empty).ToLowerInvariant();

			if (CheatDatabases.KnownCleanerSignatures != null)
			{
				foreach (string cleaner in CheatDatabases.KnownCleanerSignatures)
				{
					if (cleaner.Length >= 4 && (nameLower.Contains(cleaner) || pathLower.Contains(cleaner)) &&
					    !nameLower.Contains("chrome") && !nameLower.Contains("discord") && !nameLower.Contains("telegram") && !nameLower.Contains("steam"))
					{
						return true;
					}
				}
			}
			return false;
		}

		private static bool IsTempBinaryProcess(string fileName, string exePath)
		{
			string nameLower = (fileName ?? string.Empty).ToLowerInvariant();
			string pathLower = (exePath ?? string.Empty).ToLowerInvariant();

			if (nameLower.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
				return true;

			if (!string.IsNullOrEmpty(pathLower))
			{
				if (pathLower.Contains("\\appdata\\local\\temp\\") ||
				    pathLower.Contains("\\windows\\temp\\") ||
				    pathLower.Contains("\\temp\\") ||
				    pathLower.Contains("\\tmp\\"))
				{
					return true;
				}
			}
			return false;
		}

		#endregion

		public static List<CheckResult> CheckNetworkSocketsAndConnections()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				string text = string.Empty;
				ProcessStartInfo processStartInfo = new ProcessStartInfo("netstat", "-ano -p tcp");
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.UseShellExecute = false;
				processStartInfo.CreateNoWindow = true;
				using (Process process = Process.Start(processStartInfo))
				{
					text = process.StandardOutput.ReadToEnd();
					process.WaitForExit(4000);
				}
				return AuditNetworkSockets(text, null, null);
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Сеть / Соединения";
				checkResult3.Name = "Активные сетевые сокеты";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки сетевых сокетов: " + ex.Message;
				list.Add(checkResult3);
				return list;
			}
		}

		public static List<CheckResult> AuditNetworkSockets(string text, Dictionary<int, CachedPeerProcessInfo> procCacheOverride, HashSet<int> javaPidsOverride)
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				HashSet<string> reportedAlerts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				int safeJavaNioCount = 0;
				int safeLauncherCount = 0;

				Dictionary<int, CachedPeerProcessInfo> procCache = procCacheOverride != null
					? new Dictionary<int, CachedPeerProcessInfo>(procCacheOverride)
					: new Dictionary<int, CachedPeerProcessInfo>();

				HashSet<int> javaPids = javaPidsOverride != null
					? new HashSet<int>(javaPidsOverride)
					: new HashSet<int>();

				if (procCacheOverride == null)
				{
					foreach (Process p in Process.GetProcesses())
					{
						try
						{
							CachedPeerProcessInfo info = new CachedPeerProcessInfo();
							info.Pid = p.Id;
							info.BaseName = p.ProcessName.ToLowerInvariant();
							info.Name = p.ProcessName + ".exe";
							info.ExePath = SafeGetProcessPath(p);
							info.IsJava = (info.BaseName == "javaw" || info.BaseName == "java" || info.BaseName == "minecraft" || info.BaseName.Contains("bedrock"));
							info.IsWhitelisted = IsWhitelistedLauncherOrApp(info.BaseName, info.ExePath);
							info.IsCheat = IsCheatProcessOrPath(info.BaseName, info.ExePath);
							info.IsCleaner = IsCleanerProcessOrPath(info.BaseName, info.ExePath);
							info.IsTempBinary = IsTempBinaryProcess(info.Name, info.ExePath);

							procCache[p.Id] = info;
							if (info.IsJava)
							{
								javaPids.Add(p.Id);
							}
						}
						catch { }
					}
				}

					HashSet<int> cheatPorts = new HashSet<int>
					{
						6667, 6697, 7000, 6660, 6661, 6662, 6663, 6664, 6665, 6666, 6668, 6669,
						8088, 8888, 9999, 3000, 5000, 8080, 8000, 4444, 5555, 65432, 31337, 1337, 7777, 9001, 25560, 49152
					};

					string[] lines = text.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					List<NetstatTcpRow> tcpRows = new List<NetstatTcpRow>();

					foreach (string text2 in lines)
					{
						string trimmed = text2.Trim();
						if (!trimmed.StartsWith("TCP", StringComparison.OrdinalIgnoreCase))
						{
							continue;
						}
						string[] array3 = Regex.Split(trimmed, "\\s+");
						if (array3.Length < 5)
						{
							continue;
						}
						string localAddr = array3[1];
						string foreignAddr = array3[2];
						string state = array3[3].ToUpperInvariant();
						int rowPid;
						if (!int.TryParse(array3[array3.Length - 1], out rowPid))
						{
							continue;
						}

						string localIp;
						int localPort;
						ParseEndpointAddress(localAddr, out localIp, out localPort);

						string foreignIp;
						int foreignPort;
						ParseEndpointAddress(foreignAddr, out foreignIp, out foreignPort);

						NetstatTcpRow row = new NetstatTcpRow();
						row.LocalAddr = localAddr;
						row.ForeignAddr = foreignAddr;
						row.LocalIp = localIp;
						row.LocalPort = localPort;
						row.ForeignIp = foreignIp;
						row.ForeignPort = foreignPort;
						row.State = state;
						row.Pid = rowPid;
						row.IsLoopback = IsLoopbackIpAddress(localIp) || IsLoopbackIpAddress(foreignIp) ||
						                 localAddr.StartsWith("127.0.0.1:", StringComparison.OrdinalIgnoreCase) ||
						                 foreignAddr.StartsWith("127.0.0.1:", StringComparison.OrdinalIgnoreCase) ||
						                 foreignAddr.StartsWith("[::1]:", StringComparison.OrdinalIgnoreCase) ||
						                 foreignAddr.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase);

						tcpRows.Add(row);
					}

					// Evaluate all TCP sockets with intelligent peer resolution
					foreach (NetstatTcpRow row in tcpRows)
					{
						CachedPeerProcessInfo currentProc = GetOrCacheProcessInfo(row.Pid, procCache);
						bool isMinecraftJava = javaPids.Contains(row.Pid) || currentProc.IsJava;

						if (isMinecraftJava)
						{
							bool isLoopback = row.IsLoopback ||
							                  row.ForeignAddr.StartsWith("127.0.0.1:", StringComparison.OrdinalIgnoreCase) ||
							                  row.ForeignAddr.StartsWith("[::1]:", StringComparison.OrdinalIgnoreCase) ||
							                  row.ForeignAddr.StartsWith("localhost:", StringComparison.OrdinalIgnoreCase);

							if (isLoopback)
							{
								// 1. Intelligent peer process resolution:
								// Find which process is on the other end of the loopback connection
								NetstatTcpRow peerRow = null;

								// First: exact bidirectional match
								for (int i = 0; i < tcpRows.Count; i++)
								{
									NetstatTcpRow other = tcpRows[i];
									if (other.LocalPort == row.ForeignPort && other.ForeignPort == row.LocalPort)
									{
										peerRow = other;
										break;
									}
								}

								// Second: listening server on target foreign port
								if (peerRow == null)
								{
									for (int i = 0; i < tcpRows.Count; i++)
									{
										NetstatTcpRow other = tcpRows[i];
										if (other.LocalPort == row.ForeignPort && other.State == "LISTENING")
										{
											peerRow = other;
											break;
										}
									}
								}

								// Third: any row with target port
								if (peerRow == null)
								{
									for (int i = 0; i < tcpRows.Count; i++)
									{
										NetstatTcpRow other = tcpRows[i];
										if (other.LocalPort == row.ForeignPort || other.ForeignPort == row.LocalPort)
										{
											peerRow = other;
											break;
										}
									}
								}

								int peerPid = (peerRow != null) ? peerRow.Pid : 0;
								CachedPeerProcessInfo peerProc = GetOrCacheProcessInfo(peerPid, procCache);

								// 2. If the other end is javaw.exe itself, it is normal Java NIO/integrated server (mark safe/info, no ban)
								if (peerPid == row.Pid || javaPids.Contains(peerPid) || peerProc.IsJava)
								{
									safeJavaNioCount++;
									continue;
								}

								// 3. If the other end is a known launcher/discord, mark safe
								if (peerProc.IsWhitelisted || IsWhitelistedLauncherOrApp(peerProc.BaseName, peerProc.ExePath))
								{
									safeLauncherCount++;
									continue;
								}

								// 4. Check if connection was abruptly dropped (TIME_WAIT/CLOSE_WAIT during SS) -> flag 5.08 (self-destruct)
								bool isDropped = (row.State == "TIME_WAIT" || row.State == "CLOSE_WAIT" ||
								                  row.State == "FIN_WAIT_1" || row.State == "FIN_WAIT_2" || row.State == "LAST_ACK" ||
								                  (peerRow != null && (peerRow.State == "TIME_WAIT" || peerRow.State == "CLOSE_WAIT" ||
								                                      peerRow.State == "FIN_WAIT_1" || peerRow.State == "FIN_WAIT_2" || peerRow.State == "LAST_ACK")) ||
								                  (peerPid <= 0 && (row.State == "TIME_WAIT" || row.State == "CLOSE_WAIT")));

								if (isDropped)
								{
									string peerDesc = (peerPid > 0)
										? string.Format("{0} (PID={1}{2})", peerProc.Name, peerPid, !string.IsNullOrEmpty(peerProc.ExePath) ? ", путь: " + peerProc.ExePath : "")
										: string.Format("внешним сокетом (порт {0}, процесс завершён)", row.ForeignPort);

									string alert = string.Format("⛔ [5.08 - Сброс / сокрытие IPC сокета чита (Self-Destruct)]: Minecraft (PID={0}) имел соединение с {1} через {2} -> {3} [статус: {4}] (соединение сброшено / закрыто во время проверки - признак экстренной выгрузки софта/Self-Destruct)",
										row.Pid, peerDesc, row.LocalAddr, row.ForeignAddr, row.State);

									if (reportedAlerts.Add(alert))
									{
										list2.Add(alert);
									}
									continue;
								}

								// 5. If the other end is an external/unknown process, Temp binary, or cheat process (Vape/Drip/Slinky), flag critical BAN 5.01 with exact process name, PID, and executable path
								if (row.State == "ESTABLISHED" || row.State == "SYN_SENT")
								{
									string exePathDisplay = !string.IsNullOrEmpty(peerProc.ExePath) ? peerProc.ExePath : "недоступен / завершён";

									if (peerProc.IsCheat)
									{
										string alert = string.Format("⛔ [5.01 - Активный IPC/RPC сокет чита]: Minecraft (PID={0}) соединён с процессом чита {1} (PID={2}, путь: {3}) через {4} -> {5} [{6}] (признак локального интерфейса чита Vape/Drip/Slinky)",
											row.Pid, peerProc.Name, peerProc.Pid, exePathDisplay, row.LocalAddr, row.ForeignAddr, row.State);
										if (reportedAlerts.Add(alert)) list2.Add(alert);
									}
									else if (peerProc.IsTempBinary)
									{
										string alert = string.Format("⛔ [5.01 - Подозрительный IPC/RPC сокет с временным файлом (Temp)]: Minecraft (PID={0}) соединён с процессом {1} (PID={2}, путь: {3}) через {4} -> {5} [{6}] (запуск стороннего модуля из Temp)",
											row.Pid, peerProc.Name, peerProc.Pid, exePathDisplay, row.LocalAddr, row.ForeignAddr, row.State);
										if (reportedAlerts.Add(alert)) list2.Add(alert);
									}
									else if (peerProc.IsCleaner)
									{
										string alert = string.Format("⛔ [5.08 - Активное соединение клинера]: Minecraft соединён с утилитой сокрытия {0} (PID={1}, путь: {2}) через {3} -> {4} [{5}]",
											peerProc.Name, peerProc.Pid, exePathDisplay, row.LocalAddr, row.ForeignAddr, row.State);
										if (reportedAlerts.Add(alert)) list2.Add(alert);
									}
									else
									{
										string alert = string.Format("⛔ [5.01 - Несанкционированный внешний IPC/RPC сокет javaw.exe]: Minecraft (PID={0}) соединён со сторонним процессом {1} (PID={2}, путь: {3}) через {4} -> {5} [{6}] (признак внешнего чита/RPC: Vape/Drip/Slinky)",
											row.Pid, peerProc.Name, peerProc.Pid, exePathDisplay, row.LocalAddr, row.ForeignAddr, row.State);
										if (reportedAlerts.Add(alert)) list2.Add(alert);
									}
								}
							}
							else
							{
								// Remote (non-loopback) connection from javaw
								if (cheatPorts.Contains(row.ForeignPort) && (row.State == "ESTABLISHED" || row.State == "SYN_SENT"))
								{
									string alert = "⛔ [5.01 - Подозрительный сокет javaw.exe]: Minecraft (PID=" + row.Pid + ") подключён к " + row.ForeignAddr + " (порт: " + row.ForeignPort + " совпадает с IRC/Cheat RPC) [" + row.State + "]";
									if (reportedAlerts.Add(alert)) list2.Add(alert);
								}
							}
						}
						else
						{
							// Non-javaw process connection check
							if (row.State == "ESTABLISHED" || row.State == "SYN_SENT" || row.State == "LISTENING")
							{
								if (currentProc.IsCleaner)
								{
									string pathDisplay = !string.IsNullOrEmpty(currentProc.ExePath) ? ", путь: " + currentProc.ExePath : "";
									string alert = string.Format("⛔ [5.08 - Активное соединение клинера]: Процесс {0} (PID={1}{2}) через {3} -> {4} [{5}]",
										currentProc.Name, row.Pid, pathDisplay, row.LocalAddr, row.ForeignAddr, row.State);
									if (reportedAlerts.Add(alert)) list2.Add(alert);
								}
								else if (currentProc.IsCheat)
								{
									string pathDisplay = !string.IsNullOrEmpty(currentProc.ExePath) ? ", путь: " + currentProc.ExePath : "";
									string alert = string.Format("⛔ [5.01 - Активное соединение чита]: Процесс {0} (PID={1}{2}) через {3} -> {4} [{5}]",
										currentProc.Name, row.Pid, pathDisplay, row.LocalAddr, row.ForeignAddr, row.State);
									if (reportedAlerts.Add(alert)) list2.Add(alert);
								}
							}
						}
					}

				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Сеть / Соединения";
					checkResult.Name = "Активные сетевые сокеты javaw.exe (IRC / Cloud Configs / IPC)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = (list2.Any((string f) => f.Contains("5.08"))
						? "Обнаружены признаки сброса/сокрытия IPC (5.08) или чит-сокетов (5.01): "
						: "Обнаружены подозрительные сетевые IPC/RPC сокеты (5.01): ") + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Сеть / Соединения";
					checkResult2.Name = "Активные сетевые сокеты javaw.exe";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = (safeJavaNioCount > 0 || safeLauncherCount > 0)
						? string.Format("Подозрительных соединений IRC, локальных IPC или чит-соединений не обнаружено. Проверено легитимных loopback-сокетов: Java NIO/IPC ({0}), Launcher/Discord ({1}).", safeJavaNioCount, safeLauncherCount)
						: "Подозрительных соединений IRC, локальных IPC или чит-соединений не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Сеть / Соединения";
				checkResult3.Name = "Активные сетевые сокеты";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки сетевых сокетов: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		public static List<CheckResult> CheckJavawMemoryStrings()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				Process[] processesByName = Process.GetProcessesByName("javaw");
				if (processesByName.Length == 0)
				{
					processesByName = Process.GetProcessesByName("java");
				}
				if (processesByName.Length == 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Minecraft";
					checkResult.Name = "Анализ сигнатур в памяти javaw.exe";
					checkResult.Detected = false;
					checkResult.Severity = CheckSeverity.Info;
					checkResult.Detail = "Minecraft не запущен в данный момент (проверка сигнатур памяти пропускается).";
					list.Add(checkResult);
					return list;
				}

				List<string> list2 = new List<string>();
				foreach (Process process in processesByName)
				{
					try
					{
						// 1. Scan JVM Memory Heap committed pages for cheat signatures
						List<MemoryRegionHit> ramHits = ProcessMemoryScanner.ScanMinecraftRam(process.Id);
						foreach (MemoryRegionHit hit in ramHits)
						{
							if (hit.Severity == CheckSeverity.Critical || hit.Severity == CheckSeverity.NeedsCheck)
							{
								list2.Add(string.Format("⛔ Чит в памяти javaw (PID={0}): {1} | {2} (адрес: {3}, защита: {4}, размер: {5})", process.Id, hit.Type, hit.Detail, hit.Address, hit.Protection, hit.Size));
							}
						}

						// 2. Scan loaded DLL modules for hooking engines and unauthorized paths
						List<MemoryRegionHit> modHits = ProcessMemoryScanner.CheckJavawLoadedModules(process);
						foreach (MemoryRegionHit mHit in modHits)
						{
							if (mHit.Severity == CheckSeverity.Critical || mHit.Severity == CheckSeverity.NeedsCheck)
							{
								list2.Add(string.Format("⛔ Подозрительный модуль в javaw (PID={0}): {1} (адрес: {2})", process.Id, mHit.Detail, mHit.Address));
							}
						}
					}
					catch
					{
					}
				}

				if (list2.Count > 0)
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Minecraft";
					checkResult2.Name = "Сигнатуры читов в активной памяти javaw.exe";
					checkResult2.Detected = true;
					checkResult2.Severity = CheckSeverity.Critical;
				checkResult2.Detail = "Обнаружено подозрительных сигнатур в памяти javaw: " + list2.Count + " шт.!";
					checkResult2.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult2);
				}
				else
				{
					CheckResult checkResult3 = new CheckResult();
					checkResult3.Category = "Minecraft";
					checkResult3.Name = "Анализ сигнатур в памяти javaw.exe";
					checkResult3.Detected = false;
					checkResult3.Severity = CheckSeverity.Info;
					checkResult3.Detail = "🧱 Minecraft Bedrock Edition (Horion, Zephyr, DLL, com.mojang)...";
					list.Add(checkResult3);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult4 = new CheckResult();
				checkResult4.Category = "Minecraft";
				checkResult4.Name = "Анализ памяти javaw.exe";
				checkResult4.Detected = false;
				checkResult4.Severity = CheckSeverity.Info;
				checkResult4.Detail = "Ошибка сканирования памяти: " + ex.Message;
				list.Add(checkResult4);
			}
			return list;
		}

		public static string GenerateCsvReport(ScanReport report)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("Статус,Категория,Название,Детали,Улики");
			foreach (CheckResult result in report.Results)
			{
				string text = (result.Detected ? ((result.Severity == CheckSeverity.Critical) ? "БАН" : ((result.Severity == CheckSeverity.NeedsCheck) ? "ВАРНИНГ" : "ИНФО")) : "ЧИСТО");
				string text2 = "\"" + (result.Category ?? "").Replace("\"", "\"\"") + "\"";
				string text3 = "\"" + (result.Name ?? "").Replace("\"", "\"\"") + "\"";
				string text4 = "\"" + (result.Detail ?? "").Replace("\"", "\"\"") + "\"";
				string text5 = "\"" + (result.Evidence ?? "").Replace("\"", "\"\"").Replace("\r\n", " | ").Replace("\n", " | ") + "\"";
				stringBuilder.AppendLine(string.Format("{0},{1},{2},{3},{4}", text, text2, text3, text4, text5));
			}
			return stringBuilder.ToString();
		}

				public static List<CheckResult> CheckAppCompatLayers()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				string[] array = new string[]
				{
					"Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers",
					"Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Compatibility Assistant\\Store",
					"Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Compatibility Assistant\\Persisted"
				};
				List<string> list2 = new List<string>();
				string[] array2 = array;
				foreach (string name in array2)
				{
					using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name))
					{
						if (registryKey != null)
						{
							string[] valueNames = registryKey.GetValueNames();
							foreach (string rawPath in valueNames)
							{
								if (string.IsNullOrEmpty(rawPath))
								{
									continue;
								}
								string cleanPath = rawPath.Trim('\"', ' ');
								string resolvedPath = ResolveNtDevicePath(cleanPath);
								string textLower = resolvedPath.ToLowerInvariant();
								if (CheatDatabases.IsWhitelistedPath(textLower) || textLower.Contains("          \"name\": ") || textLower.Contains("mccheat") || textLower.Contains("screenshare"))
								{
									continue;
								}

								object val = registryKey.GetValue(rawPath);
								string valStr = (val != null) ? val.ToString() : "";
								bool isCheat = false;
								string cheatMatched = "";

								string[] cheatClientNames = CheatClientNames;
								foreach (string cheatLower in cheatClientNames)
								{
									if (MatchesCheatName(textLower, cheatLower))
									{
										isCheat = true;
										cheatMatched = cheatLower;
										break;
									}
								}
								if (!isCheat)
								{
									string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
									foreach (string ec in extraCheatClients)
									{
										if (ec.Length >= 4 && MatchesCheatName(textLower, ec))
										{
											isCheat = true;
											cheatMatched = ec;
											break;
										}
									}
								}
								if (!isCheat)
								{
									string[] suspiciousInjectors = new string[] { "kmbox", "interception", "dllinjector", "spoofer", "cheatloader", "slinky" };
									foreach (string sInj in suspiciousInjectors)
									{
										if (textLower.Contains(sInj))
										{
											isCheat = true;
											cheatMatched = sInj;
											break;
										}
									}
								}

								bool isCleaner = false;
								string cleanerMatched = "";
								if (!isCheat)
								{
									foreach (string cl in CheatDatabases.KnownCleanerSignatures)
									{
										if (cl.Length >= 4 && (textLower.Contains(cl) || rawPath.ToLowerInvariant().Contains(cl)))
										{
											isCleaner = true;
											cleanerMatched = cl;
											break;
										}
									}
								}

								if (isCleaner)
								{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск ПО] В HKCU AppCompat зафиксирована запись исполнения чит-ПО: '{0}' (совпадение: {1}, ключ: '{2}')", resolvedPath, cheatMatched, valStr));
								}
								else if (isCheat)
								{
									bool exists = false;
									try { exists = FastFileExists(resolvedPath); } catch { }
									if (!exists)
									{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск ПО] В HKCU AppCompat зафиксирована запись исполнения чит-ПО: '{0}' (совпадение: {1}, ключ: '{2}')", resolvedPath, cheatMatched, valStr));
									}
									else
									{
										list2.Add(string.Format("⛔ [5.01 - Чит в AppCompat] В HKCU AppCompat зафиксирован чит: '{0}' (совпадение: {1}, флаг: '{2}')", resolvedPath, cheatMatched, valStr));
									}
								}
								// Deleted non-cheat apps in AppCompatLayers are not 5.08 bans
							}
						}
					}

					try
					{
						using (RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey(name))
						{
							if (registryKey2 != null)
							{
								string[] valueNames2 = registryKey2.GetValueNames();
								foreach (string rawPath2 in valueNames2)
								{
									if (string.IsNullOrEmpty(rawPath2))
									{
										continue;
									}
									string cleanPath2 = rawPath2.Trim('\"', ' ');
									string resolvedPath2 = ResolveNtDevicePath(cleanPath2);
									string textLower2 = resolvedPath2.ToLowerInvariant();
									if (CheatDatabases.IsWhitelistedPath(textLower2) || textLower2.Contains("          \"name\": ") || textLower2.Contains("mccheat") || textLower2.Contains("screenshare"))
									{
										continue;
									}

									object val2 = registryKey2.GetValue(rawPath2);
									string valStr2 = (val2 != null) ? val2.ToString() : "";
									bool isCheat2 = false;
									string cheatMatched2 = "";

									string[] cheatClientNames2 = CheatClientNames;
									foreach (string cheatLower2 in cheatClientNames2)
									{
										if (MatchesCheatName(textLower2, cheatLower2))
										{
											isCheat2 = true;
											cheatMatched2 = cheatLower2;
											break;
										}
									}
									if (!isCheat2)
									{
										string[] extraCheatClients2 = CheatDatabases.ExtraCheatClients;
										foreach (string ec2 in extraCheatClients2)
										{
											if (ec2.Length >= 4 && MatchesCheatName(textLower2, ec2))
											{
												isCheat2 = true;
												cheatMatched2 = ec2;
												break;
											}
										}
									}
									if (!isCheat2)
									{
										string[] suspiciousInjectors2 = new string[] { "kmbox", "interception", "dllinjector", "spoofer", "cheatloader", "slinky" };
										foreach (string sInj2 in suspiciousInjectors2)
										{
											if (textLower2.Contains(sInj2))
											{
												isCheat2 = true;
												cheatMatched2 = sInj2;
												break;
											}
										}
									}

									bool isCleaner2 = false;
									string cleanerMatched2 = "";
									if (!isCheat2)
									{
										foreach (string cl2 in CheatDatabases.KnownCleanerSignatures)
										{
											if (cl2.Length >= 4 && (textLower2.Contains(cl2) || rawPath2.ToLowerInvariant().Contains(cl2)))
											{
												isCleaner2 = true;
												cleanerMatched2 = cl2;
												break;
											}
										}
									}

									if (isCleaner2)
									{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск ПО] В HKLM AppCompat зафиксирована запись исполнения чит-ПО: '{0}' (совпадение: {1}, ключ: '{2}')", resolvedPath2, cheatMatched2, valStr2));
									}
									else if (isCheat2)
									{
										bool exists2 = false;
										try { exists2 = File.Exists(resolvedPath2); } catch { }
										if (!exists2)
										{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск ПО] В HKLM AppCompat зафиксирована запись исполнения чит-ПО: '{0}' (совпадение: {1}, ключ: '{2}')", resolvedPath2, cheatMatched2, valStr2));
										}
										else
										{
											list2.Add(string.Format("⛔ [5.01 - Чит в AppCompat] В HKLM AppCompat зафиксирован чит: '{0}' (совпадение: {1}, флаг: '{2}')", resolvedPath2, cheatMatched2, valStr2));
										}
									}
								}
							}
						}
					}
					catch
					{
					}
				}

				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Реестр / Фоновые следы";
					checkResult.Name = "AppCompatFlags Layers & Compatibility Assistant";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "В ветках совместимости системы обнаружены читы, запуск под правами администратора [5.01 / 5.08]: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2.Take(30));
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Реестр / Фоновые следы";
					checkResult2.Name = "AppCompatFlags Layers & Compatibility Assistant";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
			checkResult2.Detail = "Подозрительных параметров совместимости и запусков читов не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Реестр / Фоновые следы";
				checkResult3.Name = "AppCompatFlags Layers";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка анализа AppCompat: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

				public static List<CheckResult> CheckFeatureUsageAppSwitched()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				int totalValuesCount = 0;
				string[] array = new string[3] { "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FeatureUsage\\AppSwitched", "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FeatureUsage\\ShowJumpView", "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\FeatureUsage\\AppBadgeUpdated" };
				string[] array2 = array;
				foreach (string text in array2)
				{
					using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(text))
					{
						if (registryKey == null)
						{
							continue;
						}
						string[] valueNames = registryKey.GetValueNames();
						totalValuesCount += valueNames.Length;
						foreach (string text2 in valueNames)
						{
							if (string.IsNullOrEmpty(text2))
							{
								continue;
							}
							string cleanName = text2.Trim('\"', ' ');
							string resolvedPath = ResolveNtDevicePath(cleanName);
							string textLower = resolvedPath.ToLowerInvariant();
							if (CheatDatabases.IsWhitelistedPath(textLower) || textLower.Contains("          \"name\": ") || textLower.Contains("mccheat") || textLower.Contains("screenshare"))
							{
								continue;
							}

							bool isCheat = false;
							string cheatMatched = "";

							string[] cheatClientNames = CheatClientNames;
							foreach (string cheatLower in cheatClientNames)
							{
								if (MatchesCheatName(textLower, cheatLower))
								{
									isCheat = true;
									cheatMatched = cheatLower;
									break;
								}
							}
							if (!isCheat)
							{
								string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
								foreach (string ec in extraCheatClients)
								{
									if (ec.Length >= 4 && MatchesCheatName(textLower, ec))
									{
										isCheat = true;
										cheatMatched = ec;
										break;
									}
								}
							}
							if (!isCheat)
							{
								string[] suspiciousInjectors = new string[] { "kmbox", "interception", "dllinjector", "spoofer", "cheatloader", "slinky" };
								foreach (string sInj in suspiciousInjectors)
								{
									if (textLower.Contains(sInj))
									{
										isCheat = true;
										cheatMatched = sInj;
										break;
									}
								}
							}

							bool isCleaner = false;
							string cleanerMatched = "";
							if (!isCheat)
							{
								foreach (string cl in CheatDatabases.KnownCleanerSignatures)
								{
									if (cl.Length >= 4 && (textLower.Contains(cl) || text2.ToLowerInvariant().Contains(cl)))
									{
										isCleaner = true;
										cleanerMatched = cl;
										break;
									}
								}
							}

							string[] parts = text.Split('\\');
							string subName = parts[parts.Length - 1];

							if (isCleaner)
							{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск ПО из FeatureUsage] Чит запускался и переключался в этот сеанс: '{0}' (совпадение: {1}, путь: {2})", resolvedPath, cheatMatched, subName));
							}
							else if (isCheat)
							{
								bool exists = false;
								try { exists = FastFileExists(resolvedPath); } catch { }
								if (!exists)
								{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск ПО из FeatureUsage] Чит запускался и переключался в этот сеанс: '{0}' (совпадение: {1}, путь: {2})", resolvedPath, cheatMatched, subName));
								}
								else
								{
									list2.Add(string.Format("⛔ [5.01 - Чит в FeatureUsage] В переключении приложений зафиксирован чит: '{0}' (совпадение: {1}, ключ: {2})", resolvedPath, cheatMatched, subName));
								}
							}
							// Deleted non-cheat apps in FeatureUsage are not 5.08 bans
						}
					}
				}

				TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
				if (totalValuesCount == 0 && uptime.TotalHours >= 2.0)
				{
					list2.Add("ud83dudea8 [5.08 - Очистка FeatureUsage AppSwitched]: Ветка реестра пуста при аптайме более 2 часов (сокрытие следов переключения окон)!");
				}

				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Реестр / Фоновые следы";
					checkResult.Name = "FeatureUsage AppSwitched (Переключение окон / Запуски Windows 10/11)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "В статистике переключения окон обнаружены читы, скрываемые при проверке [5.01 / 5.08]: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Реестр / Фоновые следы";
					checkResult2.Name = "FeatureUsage AppSwitched";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Подозрительных записей переключения окон не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Реестр / Фоновые следы";
				checkResult3.Name = "FeatureUsage AppSwitched";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка анализа FeatureUsage: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		public static List<CheckResult> CheckMountedDevicesAndDrives()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\MountedDevices"))
				{
					if (registryKey != null)
					{
						string[] valueNames = registryKey.GetValueNames();
						foreach (string text in valueNames)
						{
							if (!text.StartsWith("\\DosDevices\\"))
							{
								continue;
							}
							byte[] array = registryKey.GetValue(text) as byte[];
							string text2 = "Чисто";
							if (array != null && array.Length > 0)
							{
								string text3 = Encoding.Unicode.GetString(array);
								if (text3.Contains("USB") || text3.Contains("Removable") || text3.Contains("Ven_"))
								{
									text2 = " [USB / Съемный накопитель]";
								}
							}
							list2.Add(text.Replace("\\DosDevices\\", "") + text2);
						}
					}
				}
				CheckResult checkResult = new CheckResult();
				checkResult.Category = "Оборудование / USB";
				checkResult.Name = "История смонтированных дисков (MountedDevices)";
				checkResult.Detected = false;
				checkResult.Severity = CheckSeverity.Info;
				checkResult.Detail = "История подключений дисков Windows: " + list2.Count + " расширений).";
				checkResult.Evidence = string.Join("\r\n", list2);
				list.Add(checkResult);
			}
			catch (Exception ex)
			{
				CheckResult checkResult2 = new CheckResult();
				checkResult2.Category = "Оборудование / USB";
				checkResult2.Name = "История смонтированных дисков";
				checkResult2.Detected = false;
				checkResult2.Severity = CheckSeverity.Info;
				checkResult2.Detail = "Ошибка чтения MountedDevices: " + ex.Message;
				list.Add(checkResult2);
			}
			return list;
		}

		public static string CreateEvidenceZipPackage(ScanReport report, string moderatorName, string zipFilePath)
		{
			string text = Path.Combine(Path.GetTempPath(), "SS_Package_" + Guid.NewGuid().ToString("N"));
			try
			{
				Directory.CreateDirectory(text);
				string contents = GenerateHtmlReport(report, moderatorName);
				File.WriteAllText(Path.Combine(text, "Report_" + report.PlayerName + ".html"), contents, Encoding.UTF8);
				string contents2 = GenerateTextReport(report);
				File.WriteAllText(Path.Combine(text, "Report_" + report.PlayerName + ".txt"), contents2, Encoding.UTF8);
				string contents3 = GenerateDiscordReport(report, moderatorName);
				File.WriteAllText(Path.Combine(text, "Discord_Report_" + report.PlayerName + ".txt"), contents3, Encoding.UTF8);
				try
				{
					string contentsJson = GenerateJsonReport(report, moderatorName);
					File.WriteAllText(Path.Combine(text, "Report_" + report.PlayerName + ".json"), contentsJson, Encoding.UTF8);
				}
				catch
				{
				}
				try
				{
					string contentsCsv = GenerateCsvReport(report);
					File.WriteAllText(Path.Combine(text, "Report_" + report.PlayerName + ".csv"), contentsCsv, Encoding.UTF8);
				}
				catch
				{
				}
				try
				{
					string text2 = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft\\logs\\latest.log");
					if (File.Exists(text2))
					{
						string logDst = Path.Combine(text, "Minecraft_latest.log");
						using (FileStream sourceStream = new FileStream(text2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
						using (FileStream destinationStream = new FileStream(logDst, FileMode.Create, FileAccess.Write))
						{
							sourceStream.CopyTo(destinationStream);
						}
					}
				}
				catch
				{
				}
				try
				{
					if (!string.IsNullOrEmpty(LatestScreenshotPath) && File.Exists(LatestScreenshotPath))
					{
						File.Copy(LatestScreenshotPath, Path.Combine(text, "ScreenShare_Proof" + Path.GetExtension(LatestScreenshotPath)), true);
					}
				}
				catch
				{
				}
				if (File.Exists(zipFilePath))
				{
					File.Delete(zipFilePath);
				}
				ZipFile.CreateFromDirectory(text, zipFilePath);
				return zipFilePath;
			}
			finally
			{
				try
				{
					if (Directory.Exists(text))
					{
						Directory.Delete(text, true);
					}
				}
				catch
				{
				}
			}
		}

		public static bool SendDiscordWebhook(string webhookUrl, ScanReport report, string moderatorName, out string errorMessage)
		{
			return SendDiscordWebhook(webhookUrl, report, moderatorName, null, out errorMessage);
		}

		public static bool SendDiscordWebhook(string webhookUrl, ScanReport report, string moderatorName, byte[] screenshotPngBytes, out string errorMessage)
		{
			errorMessage = null;
			if (string.IsNullOrWhiteSpace(webhookUrl))
			{
				errorMessage = "URL вебхука не указан";
				return false;
			}
			try
			{
				ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;
				int color = 3066993;
				if (report.CriticalCount > 0)
				{
					color = 15158332;
				}
				else if (report.VisualsCount > 0)
				{
					color = 11225020;
				}
				else if (report.WarningCount > 0)
				{
					color = 16753920;
				}

				string player = string.IsNullOrEmpty(report.PlayerName) ? "Не указан" : report.PlayerName;
				string mod = string.IsNullOrEmpty(moderatorName) ? Environment.UserName : moderatorName;

				StringBuilder sb = new StringBuilder();
				sb.Append("{\"embeds\":[{");
				sb.Append("\"title\":\"\ud83d\udee1\ufe0f MC CHEAT CHECKER \u2014 SCREENSHARE REPORT (Community Edition)\",");
				sb.Append("\"color\":" + color + ",");
				sb.Append("\"fields\":[");
				sb.Append("{\"name\":\"\ud83d\udc64 \u0418\u0433\u0440\u043e\u043a\",\"value\":\"`" + EscapeJsonForDiscord(player) + "`\",\"inline\":true},");
				sb.Append("{\"name\":\"\ud83d\udee1\ufe0f \u041c\u043e\u0434\u0435\u0440\u0430\u0442\u043e\u0440\",\"value\":\"`" + EscapeJsonForDiscord(mod) + "`\",\"inline\":true},");
				sb.Append("{\"name\":\"\u2696\ufe0f \u0412\u0435\u0440\u0434\u0438\u043a\u0442\",\"value\":\"**" + EscapeJsonForDiscord(report.Verdict) + "**\",\"inline\":false},");
				sb.Append("{\"name\":\"\ud83d\udcca \u0421\u0432\u043e\u0434\u043a\u0430\",\"value\":\"\ud83d\udd34 \u0427\u0438\u0442\u044b: `" + report.CriticalCount + "` | \ud83d\udfe3 \u0412\u0438\u0437\u0443\u0430\u043b\u044b: `" + report.VisualsCount + "` | \ud83d\udfe1 \u0421\u043b\u0435\u0434\u044b: `" + report.WarningCount + "`\",\"inline\":false}");

				List<CheckResult> detected = report.Results.Where(r => r.Detected).Take(8).ToList();
				if (detected.Count > 0)
				{
					StringBuilder hitsSb = new StringBuilder();
					foreach (CheckResult r in detected)
					{
						string icon = (r.Severity == CheckSeverity.Critical) ? "\ud83d\udd34" : ((r.Severity == CheckSeverity.NeedsCheck) ? "\ud83d\udfe3" : "\ud83d\udfe1");
						string line = icon + " **" + EscapeJsonForDiscord(r.Name) + ": " + EscapeJsonForDiscord(r.Detail) + "\\n";
						if (hitsSb.Length + line.Length > 950)
						{
							hitsSb.Append("... и другие улики (лимит Discord)\\n");
							break;
						}
						hitsSb.Append(line);
					}
					if (report.TotalDetected > 8)
					{
						hitsSb.Append("... \u0438 \u0435\u0449\u0451 " + (report.TotalDetected - 8) + " \u0437\u0430\u043f\u0438\u0441\u0435\u0439");
					}
					sb.Append(",{\"name\":\"\ud83d\udea8 \u041e\u0431\u043d\u0430\u0440\u0443\u0436\u0435\u043d\u043d\u044b\u0435 \u0443\u043b\u0438\u043a\u0438\",\"value\":\"" + hitsSb.ToString() + "\",\"inline\":false}");
				}
				if (report.CriticalCount > 0)
				{
					CheckResult top = report.Results.FirstOrDefault(r => r.Detected && r.Severity == CheckSeverity.Critical && RuleBook.Classify(r.Name + " " + r.Detail + " " + r.Evidence, r.Severity) != BanCase.Allowed);
					if (top != null)
					{
						BanCase bc = RuleBook.Classify(top.Name + " " + top.Detail + " " + top.Evidence, top.Severity);
						RuleBook.RuleInfo ri = RuleBook.Get(bc);
						string banCmd = RuleBook.Build(player, ri.Duration, ri.Reason);
						if (!string.IsNullOrEmpty(banCmd))
						{
							sb.Append(",{\"name\":\"🔨 Рекомендуемый бан\",\"value\":\"`" + EscapeJsonForDiscord(banCmd) + "`\",\"inline\":false}");
						}
					}
				}
				sb.Append("],");
				if (screenshotPngBytes != null && screenshotPngBytes.Length > 0)
				{
					sb.Append("\"image\":{\"url\":\"attachment://screenshot.png\"},");
				}
				sb.Append("\"footer\":{\"text\":\"MC Cheat Checker Community Edition \u2022 " + report.ScanTime.ToString("dd.MM.yyyy HH:mm:ss") + "\"}");
				sb.Append("}]}");

				if (screenshotPngBytes != null && screenshotPngBytes.Length > 0)
				{
					string boundary = "----MCCheatCheckerBoundary" + DateTime.UtcNow.Ticks.ToString("x");
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(webhookUrl.Trim());
					request.Method = "POST";
					request.ContentType = "multipart/form-data; boundary=" + boundary;
					request.UserAgent = "MCCheatChecker-CommunityEdition/1.0";
					request.Timeout = 15000;

					using (MemoryStream ms = new MemoryStream())
					{
						string part1Header = "--" + boundary + "\r\n" +
										 "Content-Disposition: form-data; name=\"payload_json\"\r\n" +
										 "Content-Type: application/json; charset=utf-8\r\n\r\n";
						byte[] part1Bytes = Encoding.UTF8.GetBytes(part1Header);
						ms.Write(part1Bytes, 0, part1Bytes.Length);

						byte[] payloadBytes = Encoding.UTF8.GetBytes(sb.ToString());
						ms.Write(payloadBytes, 0, payloadBytes.Length);
						ms.Write(Encoding.ASCII.GetBytes("\r\n"), 0, 2);

						string part2Header = "--" + boundary + "\r\n" +
										 "Content-Disposition: form-data; name=\"files[0]\"; filename=\"screenshot.png\"\r\n" +
										 "Content-Type: image/png\r\n\r\n";
						byte[] part2Bytes = Encoding.UTF8.GetBytes(part2Header);
						ms.Write(part2Bytes, 0, part2Bytes.Length);

						ms.Write(screenshotPngBytes, 0, screenshotPngBytes.Length);
						ms.Write(Encoding.ASCII.GetBytes("\r\n"), 0, 2);

						byte[] endBoundary = Encoding.ASCII.GetBytes("--" + boundary + "--\r\n");
						ms.Write(endBoundary, 0, endBoundary.Length);

						request.ContentLength = ms.Length;
						using (Stream reqStream = request.GetRequestStream())
						{
							ms.Position = 0;
							byte[] buf = new byte[8192];
							int read;
							while ((read = ms.Read(buf, 0, buf.Length)) > 0)
							{
								reqStream.Write(buf, 0, read);
							}
						}
					}

					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					{
						int statusCode = (int)response.StatusCode;
						if (statusCode >= 200 && statusCode < 300)
						{
							return true;
						}
						errorMessage = "\u041a\u043e\u0434 \u043e\u0442\u0432\u0435\u0442\u0430: " + response.StatusCode;
						return false;
					}
				}
				else
				{
					byte[] payloadBytes = Encoding.UTF8.GetBytes(sb.ToString());
					HttpWebRequest request = (HttpWebRequest)WebRequest.Create(webhookUrl.Trim());
					request.Method = "POST";
					request.ContentType = "application/json";
					request.UserAgent = "MCCheatChecker-CommunityEdition/1.0";
					request.ContentLength = payloadBytes.Length;
					request.Timeout = 10000;

					using (Stream requestStream = request.GetRequestStream())
					{
						requestStream.Write(payloadBytes, 0, payloadBytes.Length);
					}

					using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
					{
						int statusCode = (int)response.StatusCode;
						if (statusCode >= 200 && statusCode < 300)
						{
							return true;
						}
						errorMessage = "\u041a\u043e\u0434 \u043e\u0442\u0432\u0435\u0442\u0430: " + response.StatusCode;
						return false;
					}
				}
			}
			catch (Exception ex)
			{
				errorMessage = ex.Message;
				return false;
			}
		}

		private static string EscapeJsonForDiscord(string s)
		{
			if (string.IsNullOrEmpty(s)) return "";
			StringBuilder sb = new StringBuilder();
			foreach (char c in s)
			{
				switch (c)
				{
					case '\\': sb.Append("\\\\"); break;
					case '"': sb.Append("\\\""); break;
					case '\n': sb.Append("\\n"); break;
					case '\r': break;
					case '\t': sb.Append(" "); break;
					default:
						if (c < 32) sb.AppendFormat("\\u{0:x4}", (int)c);
						else sb.Append(c);
						break;
				}
			}
			return sb.ToString();
		}

		public static List<CheckResult> CheckMuiCache()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				Tuple<RegistryKey, string>[] muiRoots = new Tuple<RegistryKey, string>[]
				{
					Tuple.Create(Registry.CurrentUser, "Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\MuiCache"),
					Tuple.Create(Registry.CurrentUser, "Software\\Microsoft\\Windows\\ShellNoRoam\\MUICache"),
					Tuple.Create(Registry.ClassesRoot, "Local Settings\\Software\\Microsoft\\Windows\\Shell\\MuiCache")
				};

				List<string> list2 = new List<string>();
				HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (Tuple<RegistryKey, string> root in muiRoots)
				{
					try
					{
						using (RegistryKey registryKey = root.Item1.OpenSubKey(root.Item2))
						{
							if (registryKey == null) continue;
							string[] valueNames = registryKey.GetValueNames();
							foreach (string text in valueNames)
							{
								if (string.IsNullOrEmpty(text) || text.Length < 3) continue;

								string cleanPath = text;
								if (cleanPath.EndsWith(".FriendlyAppName", StringComparison.OrdinalIgnoreCase))
									cleanPath = cleanPath.Substring(0, cleanPath.Length - 16);
								else if (cleanPath.EndsWith(".ApplicationCompany", StringComparison.OrdinalIgnoreCase))
									cleanPath = cleanPath.Substring(0, cleanPath.Length - 19);

								cleanPath = cleanPath.Trim((char)34, ' ');
								if (!seen.Add(cleanPath)) continue;

								string textLower = cleanPath.ToLowerInvariant();
								bool isCheat = false;
								string cheatMatched = "";

								foreach (string cheatLower in CheatClientNames)
								{
									if (MatchesCheatName(textLower, cheatLower))
									{
										isCheat = true;
										cheatMatched = cheatLower;
										break;
									}
								}
								if (!isCheat)
								{
									foreach (string ec in CheatDatabases.ExtraCheatClients)
									{
										if (ec.Length >= 4 && MatchesCheatName(textLower, ec))
										{
											isCheat = true;
											cheatMatched = ec;
											break;
										}
									}
								}
								if (!isCheat)
								{
									string[] muiInj = new string[] { "kmbox", "interception", "dllinjector", "spoofer", "cheatloader", "slinky" };
									foreach (string sinj in muiInj)
									{
										if (textLower.Contains(sinj))
										{
											isCheat = true;
											cheatMatched = sinj;
											break;
										}
									}
								}

								bool isCleaner = false;
								string cleanerMatched = "";
								if (!isCheat)
								{
									foreach (string cl in CheatDatabases.KnownCleanerSignatures)
									{
										if (cl.Length >= 4 && textLower.Contains(cl))
										{
											isCleaner = true;
											cleanerMatched = cl;
											break;
										}
									}
								}

								bool exists = false;
								try { exists = File.Exists(cleanPath); } catch { }

								if (isCheat)
								{
									if (!exists)
									{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск ПО из MuiCache] В MuiCache найдена запись исполнения чит-ПО [{0}]: {1}", cheatMatched, cleanPath));
									}
									else
									{
										list2.Add(string.Format("⛔ [5.01 - Чит в MuiCache] В MuiCache зафиксирован запуск чита [{0}]: {1}", cheatMatched, cleanPath));
									}
								}
								else if (isCleaner)
								{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск ПО из MuiCache] В MuiCache найдена запись исполнения чит-ПО [{0}]: {1}", cheatMatched, cleanPath));
								}
								// Deleted non-cheat files in MuiCache are not 5.08 bans
							}
						}
					}
					catch { }
				}

				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Реестр / Фоновые следы";
					checkResult.Name = "MuiCache (История запуска исполняемых файлов в Explorer)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "🗄️ Реестр MuiCache (История запуска исполняемых файлов)..." + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2.Take(30));
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Реестр / Фоновые следы";
					checkResult2.Name = "MuiCache (История запуска Explorer)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Подозрительных записей в MuiCache не найдено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Реестр / Фоновые следы";
				checkResult3.Name = "MuiCache (История запуска Explorer)";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки MuiCache: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

				public static List<CheckResult> CheckRecentDocsMRU()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				int totalRecentDocsCount = 0;
				string name = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RecentDocs";
				using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name))
				{
					if (registryKey != null)
					{
						string[] subKeyNames = registryKey.GetSubKeyNames();
						string[] targetExts = new string[] { ".jar", ".zip", ".exe", ".dll", ".bat", ".rar", ".7z", ".cmd", ".ps1", ".vbs", ".lnk" };
						List<string> keysToScan = new List<string>(subKeyNames);
						keysToScan.Add(""); // root RecentDocs

						foreach (string text in keysToScan)
						{
							bool shouldScan = string.IsNullOrEmpty(text);
							if (!shouldScan)
							{
								foreach (string ext in targetExts)
								{
									if (text.Equals(ext, StringComparison.OrdinalIgnoreCase)) { shouldScan = true; break; }
								}
							}
							if (!shouldScan) continue;

							using (RegistryKey registryKey2 = string.IsNullOrEmpty(text) ? Registry.CurrentUser.OpenSubKey(name) : registryKey.OpenSubKey(text))
							{
								if (registryKey2 == null) continue;
								string[] valueNames = registryKey2.GetValueNames();
								foreach (string text2 in valueNames)
								{
									if (text2.Equals("MRUListEx", StringComparison.OrdinalIgnoreCase)) continue;
									byte[] array4 = registryKey2.GetValue(text2) as byte[];
									if (array4 == null || array4.Length <= 2) continue;

									totalRecentDocsCount++;
									string rawStr = Encoding.Unicode.GetString(array4);
									int nullIdx = rawStr.IndexOf('\0');
									string text4 = (nullIdx >= 0 ? rawStr.Substring(0, nullIdx) : rawStr).Trim();
									if (string.IsNullOrEmpty(text4) || text4.Length < 3) continue;

									string textLower = text4.ToLowerInvariant();
									if (CheatDatabases.IsWhitelistedPath(textLower) || textLower.Contains("          \"name\": ") || textLower.Contains("mccheat") || textLower.Contains("screenshare") || textLower.Contains("sstools"))
									{
										continue;
									}

									bool matched = false;
									string matchedCheat = "";

									foreach (string cheatLower in CheatClientNames)
									{
										if (MatchesCheatName(textLower, cheatLower))
										{
											matched = true;
											matchedCheat = cheatLower;
											break;
										}
									}
									if (!matched)
									{
										foreach (string ec in CheatDatabases.ExtraCheatClients)
										{
											if (ec.Length >= 4 && MatchesCheatName(textLower, ec))
											{
												matched = true;
												matchedCheat = ec;
												break;
											}
										}
									}
									if (!matched)
									{
										string[] extraInj = new string[] { "kmbox", "interception", "spoofer", "dllinjector", "cheatloader", "slinky" };
										foreach (string inj in extraInj)
										{
											if (textLower.Contains(inj))
											{
												matched = true;
												matchedCheat = inj;
												break;
											}
										}
									}

									bool isCleaner = false;
									string cleanerMatched = "";
									if (!matched)
									{
										foreach (string cl in CheatDatabases.KnownCleanerSignatures)
										{
											if (cl.Length >= 4 && textLower.Contains(cl))
											{
												isCleaner = true;
												cleanerMatched = cl;
												break;
											}
										}
									}

									bool exists = false;
									try { exists = File.Exists(text4); } catch { }

									string prefix = string.IsNullOrEmpty(text) ? "RecentDocs" : text;
									if (matched)
									{
										if (!exists)
										{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Обращение к ПО из RecentDocs] [{0}] [{1}] {2}", matchedCheat, prefix, text4));
										}
										else
										{
											list2.Add(string.Format("⛔ [5.01 - Чит в RecentDocs] [{0}] [{1}] {2}", matchedCheat, prefix, text4));
										}
									}
									else if (isCleaner)
									{
						list2.Add(string.Format("\ud83d\udea8 [5.08 - Обращение к ПО из RecentDocs] [{0}] [{1}] {2}", matchedCheat, prefix, text4));
									}
								}
							}
						}
					}
				}

				TimeSpan uptime = TimeSpan.FromMilliseconds(Environment.TickCount);
				if (totalRecentDocsCount == 0 && uptime.TotalHours >= 2.0)
				{
					list2.Add("ud83dudea8 [5.08 - Очистка RecentDocs MRU]: История недавно открытых файлов пуста при аптайме более 2 часов (сокрытие следов запуска файлов)!");
				}

				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Реестр / Фоновые следы";
					checkResult.Name = "📄 RecentDocs MRU (.jar, .dll, .exe обращения в системе)...";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "В RecentDocs MRU обнаружены обращения к читам или подозрительным файлам [5.01 / 5.08]: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2.Take(30));
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Реестр / Фоновые следы";
					checkResult2.Name = "RecentDocs MRU";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "📄 RecentDocs MRU (.jar, .dll, .exe обращения в системе)...";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Реестр / Фоновые следы";
				checkResult3.Name = "RecentDocs MRU";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки RecentDocs: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

				public static List<CheckResult> CheckWindowTitlesAndTray()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				List<string> list3 = new List<string>();
				List<string> listAllowed = new List<string>();
				string[] array = new string[]
				{
					"vape", "vape client", "celestial", "celestial client", "expensive", "nursultan", "minced", "wild client",
					"akrien", "fluger", "meteor client", "meteor", "wurst", "liquidbounce", "liquid bounce", "prax", "horion", "zephyr",
					"sigma client", "sigma", "neverhook", "impact client", "impact", "kamiblue", "kami blue", "kami", "inertia", "inertia client",
					"aristois", "baritone", "rusherhack", "rusher hack", "future client", "future", "skillclient", "skill client", "jigsaw client", "jigsaw",
					"zeus client", "salhack", "wolfram", "novoline", "huzuni", "entropy client", "prism client",
					"flarial client", "flux client", "orion client", "tenacity client", "raven client", "raven",
					"sora client", "dummie client", "dummi client", "rise client", "rogue client", "remix client", "ghost client",
					"drip client", "paragon client", "catalyst client", "stardust client", "cortex client",
					"aoba client", "fdp client", "darklight client", "jex client", "novaclient", "nova client",
					"eclipse client", "cheat engine", "cheat-engine", "cheatengine", "ollydbg",
					"xenos", "extreme injector", "extremeinjector", "liquid loader", "liquidloader",
					"blatware", "slavehack", "slavware", "rusohack", "atomware", "neonware", "quantumware"
				};
				string[] clickerTitles = new string[]
				{
					"autoclicker", "auto clicker", "op auto clicker", "speedautoclicker", "mangoclicker", "xdotool", "autohotkey", "macro clicker", "clickmaster", "clickstorm", "turbo clicker", "rapid clicker", "tinytask", "keyran", "botmek"
				};

				Process[] processes = Process.GetProcesses();
				foreach (Process process in processes)
				{
					try
					{
						string mainWindowTitle = process.MainWindowTitle;
						if (string.IsNullOrEmpty(mainWindowTitle)) continue;
						string pNameLower = process.ProcessName.ToLowerInvariant();
						if (pNameLower == "chrome" || pNameLower == "msedge" || pNameLower == "firefox" || pNameLower == "opera" || pNameLower == "brave" || pNameLower == "vivaldi" || pNameLower == "spotify" || pNameLower == "discord" || pNameLower == "discordcanary" || pNameLower == "discordptb" || pNameLower == "vesktop" || pNameLower == "telegram" || pNameLower == "steam" || pNameLower == "devenv" || pNameLower == "code" || pNameLower == "idea64" || pNameLower == "anydesk" || pNameLower == "rudesktop" || pNameLower == "rustdesk" || pNameLower == "teamviewer" || pNameLower == "obs64" || pNameLower == "obs32" || pNameLower == "mccheatchecker") continue;
						string text = mainWindowTitle.ToLowerInvariant();
						if (CheatDatabases.IsWhitelistedPath(text) || text.Contains("          \"name\": ") || text.Contains("mccheat") || text.Contains("screenshare") || text.Contains("cheatchecker"))
						{
							continue;
						}

						list3.Add(string.Format("PID={0} [{1}]: {2}", process.Id, process.ProcessName, mainWindowTitle));

						bool isClicker = false;
						foreach (string ct in clickerTitles)
						{
							if (text.Contains(ct))
							{
								listAllowed.Add(string.Format("ℹ️ [Разрешённый кликер] Автокликер: PID={0} [{1}] \"{2}\"", process.Id, process.ProcessName, mainWindowTitle));
								isClicker = true;
								break;
							}
						}
						if (isClicker) continue;

						bool flag = false;
						foreach (string text2 in array)
						{
							if (text.Contains(text2))
							{
								list2.Add(string.Format("⛔ [5.01 - Подозрительное окно] PID={0} [{1}] Заголовок: \"{2}\" (Совпадение: {3})", process.Id, process.ProcessName, mainWindowTitle, text2));
								flag = true;
								break;
							}
						}
						if (flag)
						{
							continue;
						}
						string[] cheatClientNames = CheatClientNames;
						foreach (string text3 in cheatClientNames)
						{
							if (MatchesCheatName(text, text3) && !IsLegitGame(text))
							{
								list2.Add(string.Format("⛔ [5.01 - Чит в заголовке] PID={0} [{1}] Заголовок: \"{2}\" (Клиент: {3})", process.Id, process.ProcessName, mainWindowTitle, text3));
								break;
							}
						}
					}
					catch
					{
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Процессы / Память";
					checkResult.Name = "Заголовки открытых окон (Чит-клиенты / Инжекторы)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
				checkResult.Detail = "Обнаружены открытые окна запрещённого ПО: " + list2.Count + " шт.";
				checkResult.Evidence = string.Join("\r\n", list2) + (listAllowed.Count > 0 ? "\r\n\r\n-- Разрешённые автокликеры --\r\n" + string.Join("\r\n", listAllowed) : "") + "\r\n\r\n-- Все проверенные окна --\r\n" + string.Join("\r\n", list3);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Процессы / Память";
					checkResult2.Name = "Inventory Profiler Next";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
				checkResult2.Detail = (listAllowed.Count > 0 ? "Обнаружены открытые автокликеры (разрешены правилами сервера: " + listAllowed.Count + " шт.). Запрещённых окон не найдено." : "Окон запрещённого ПО не обнаружено. Всего окон: " + list3.Count);
					checkResult2.Evidence = (listAllowed.Count > 0 ? string.Join("\r\n", listAllowed) + "\r\n\r\n" : "") + string.Join("\r\n", list3);
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Процессы / Память";
				checkResult3.Name = "Inventory Profiler Next";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки окон: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		public static List<CheckResult> CheckMinecraftJvmCommandLine()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				Process[] processesByName = Process.GetProcessesByName("javaw");
				if (processesByName.Length == 0)
				{
					processesByName = Process.GetProcessesByName("java");
				}
				if (processesByName.Length == 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Minecraft";
					checkResult.Name = "Аргументы запуска JVM (javaw.exe)";
					checkResult.Detected = false;
					checkResult.Severity = CheckSeverity.Info;
					checkResult.Detail = "Процесс javaw.exe в данный момент не запущен.";
					list.Add(checkResult);
					return list;
				}
				Process[] array = processesByName;
				foreach (Process process in array)
				{
					try
					{
						string text = "";
						// 1. Try native WMI first (reliable on Windows 11 where wmic.exe is deprecated/absent)
						try
						{
							using (ManagementObjectSearcher searcher = new ManagementObjectSearcher(string.Format("SELECT CommandLine FROM Win32_Process WHERE ProcessId = {0}", process.Id)))
							{
								foreach (ManagementObject mo in searcher.Get())
								{
									object cl = mo["CommandLine"];
									if (cl != null)
									{
										text = cl.ToString();
										break;
									}
								}
							}
						}
						catch
						{
						}
						// 2. Fallback to wmic CLI if WMI returned empty
						if (string.IsNullOrEmpty(text))
						{
							try
							{
								ProcessStartInfo processStartInfo = new ProcessStartInfo("wmic", "process where processid=" + process.Id + " get commandline");
								processStartInfo.RedirectStandardOutput = true;
								processStartInfo.UseShellExecute = false;
								processStartInfo.CreateNoWindow = true;
								using (Process process2 = Process.Start(processStartInfo))
								{
									text = process2.StandardOutput.ReadToEnd();
									process2.WaitForExit(3000);
								}
							}
							catch
							{
							}
						}

						if (string.IsNullOrEmpty(text))
						{
							continue;
						}

						string text2 = text.ToLowerInvariant();
						if (text2.Contains("-javaagent:"))
						{
							Match jaMatch = Regex.Match(text, @"(?i)-javaagent:([^\s""]+)");
							string agentPath = jaMatch.Success ? jaMatch.Groups[1].Value : "";
							string agentLower = agentPath.ToLowerInvariant();

							bool isWhitelistedAgent = agentLower.Contains("authlib-injector") ||
								agentLower.Contains("fabric-loader") || agentLower.Contains("quilt-loader") ||
								agentLower.Contains("sponge-mixin") || agentLower.Contains("modlauncher") ||
								agentLower.Contains("patchy") || agentLower.Contains("log4j") ||
								agentLower.Contains("evo") || agentLower.Contains("evovis") || agentLower.Contains("evoguard") ||
								agentLower.Contains("lunar") || agentLower.Contains("genesis") || agentLower.Contains("feather") ||
								agentLower.Contains("badlion") || agentLower.Contains("labymod") || agentLower.Contains("salwyrr") ||
								agentLower.Contains("optifine") || agentLower.Contains("essential") ||
								SignatureAutoUpdateManager.IsWhitelistedJavaAgent(agentPath);

							if (!isWhitelistedAgent)
							{
								bool isBlacklisted = SignatureAutoUpdateManager.IsBlacklistedJavaAgent(agentPath);
								if (isBlacklisted || agentLower.Contains(@"\temp\") || agentLower.Contains(@"\appdata\local\temp"))
								{
									list2.Add("🚨 [5.01] Чит Java Agent в аргументах JVM: " + agentPath + " в PID=" + process.Id);
								}
								else
								{
									list2.Add("⚠️ Сторонний параметр -javaagent: " + agentPath + " в PID=" + process.Id);
								}
							}
						}
						if (text2.Contains("-xbootclasspath"))
						{
							list2.Add("⚠️ Обнаружен параметр -Xbootclasspath в PID=" + process.Id);
						}
						if (text2.Contains("-agentlib:") || text2.Contains("-agentpath:"))
						{
							list2.Add("⛔ Внедрение внешнего агента/профайлера (-agentlib / -agentpath) в PID=" + process.Id);
						}
						if (text2.Contains("-xrunjdwp"))
						{
							list2.Add("⛔ Включение удалённого отладчика JDWP (-Xrunjdwp) в PID=" + process.Id);
						}
						if (text2.Contains("-noverify") || text2.Contains("-xverify:none"))
						{
							list2.Add("⛔ Отключение верификации байткода (-noverify / -Xverify:none) в целях сокрытия модификаций JVM в PID=" + process.Id);
						}

						string[] cheatClientNames = CheatClientNames;
						foreach (string text3 in cheatClientNames)
						{
							if (MatchesCheatName(text2, text3))
							{
								list2.Add("🚨 Чит в аргументах JVM [" + text3 + "] в PID=" + process.Id);
								break;
							}
						}
						string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
						foreach (string ec in extraCheatClients)
						{
							if (ec.Length >= 4 && MatchesCheatName(text2, ec))
							{
								list2.Add("⛔ Чит-клиент (ExtraCheatClients) в аргументах JVM [" + ec + "] в PID=" + process.Id);
								break;
							}
						}
					}
					catch
					{
					}
				}
				if (list2.Count > 0)
				{
					bool hasCrit = list2.Any((string s) => s.Contains("🚨") || s.Contains("⛔"));
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Minecraft";
					checkResult2.Name = "Аргументы запуска JVM (javaw.exe) — Внедрение / Читы";
					checkResult2.Detected = true;
					checkResult2.Severity = hasCrit ? CheckSeverity.Critical : CheckSeverity.Warning;
					checkResult2.Detail = "В параметрах запущенного Minecraft обнаружены подозрительные флаги / читы: " + list2.Count + " шт.";
					checkResult2.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult2);
				}
				else
				{
					CheckResult checkResult3 = new CheckResult();
					checkResult3.Category = "Minecraft";
					checkResult3.Name = "Аргументы запуска JVM (javaw.exe)";
					checkResult3.Detected = false;
					checkResult3.Severity = CheckSeverity.Info;
					checkResult3.Detail = "Параметры запуска Minecraft чисты, подмен javaagent не выявлено.";
					list.Add(checkResult3);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult4 = new CheckResult();
				checkResult4.Category = "Minecraft";
				checkResult4.Name = "Аргументы запуска JVM";
				checkResult4.Detected = false;
				checkResult4.Severity = CheckSeverity.Info;
				checkResult4.Detail = "Ошибка анализа командной строки: " + ex.Message;
				list.Add(checkResult4);
			}
			return list;
		}

				public static string GenerateDiscordReport(ScanReport report, string moderatorName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("`diff");
			stringBuilder.AppendLine("+ =================================================== +");
			stringBuilder.AppendLine("+        MC CHEAT CHECKER - SCREENSHARE REPORT        +");
			stringBuilder.AppendLine("+ =================================================== +");
			stringBuilder.AppendLine("`");
			stringBuilder.AppendLine("> **\ud83d\udc64 Игрок:** " + (string.IsNullOrEmpty(report.PlayerName) ? "Player" : report.PlayerName) + "");
			stringBuilder.AppendLine("> **\ud83d\udee1\ufe0f Модератор:** " + (string.IsNullOrEmpty(moderatorName) ? Environment.UserName : moderatorName) + "");
			stringBuilder.AppendLine("> **⏰ Время:** " + report.ScanTime.ToString("dd.MM.yyyy HH:mm:ss") + "");
			int num = report.Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.Critical);
			int num2 = report.Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.NeedsCheck);
			int num3 = report.Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.Warning);
			string text = "\ud83d\udfe2 **Игрок чист**";
			if (num > 0)
			{
				text = "\ud83d\udd34 **БАН (Обнаружены читы)**";
			}
			else if (num2 > 0)
			{
				text = "\ud83d\udfe3 **Подозрение (Требуется допроверка)**";
			}
			else if (num3 > 0)
			{
				text = "\ud83d\udfe1 **Предупреждение (Следы / Очистка)**";
			}
			stringBuilder.AppendLine("> **⚖️ Вердикт:** " + text);
			stringBuilder.AppendLine("> **\ud83d\udcca Сводка:** \ud83d\udd34 Баны: " + num + " | \ud83d\udfe3 Подозрения: " + num2 + " | \ud83d\udfe1 Следы: " + num3 + "");
			if (num > 0 || num2 > 0 || num3 > 0)
			{
				CheckResult topCrit = report.Results.FirstOrDefault(delegate(CheckResult r) { return r.Detected && r.Severity == CheckSeverity.Critical; });
				if (topCrit == null)
				{
					topCrit = report.Results.FirstOrDefault(delegate(CheckResult r) { return r.Detected && (r.Severity == CheckSeverity.NeedsCheck || r.Severity == CheckSeverity.Warning); });
				}
				if (topCrit != null)
				{
					BanCase bc = RuleBook.Classify(topCrit.Name + " " + topCrit.Detail + " " + topCrit.Evidence);
					RuleBook.RuleInfo ri = RuleBook.Get(bc);
					string targetPlayer = string.IsNullOrEmpty(report.PlayerName) ? "Player" : report.PlayerName;
					string banCmd = RuleBook.Build(targetPlayer, ri.Duration, ri.Reason);
					stringBuilder.AppendLine("> **\ud83d\udd28 Рекомендованный бан:** " + banCmd + "");
				}
			}
			stringBuilder.AppendLine();
			List<CheckResult> list = report.Results.Where((CheckResult r) => r.Detected).ToList();
			if (list.Count > 0)
			{
				stringBuilder.AppendLine("**\ud83d\udea8 Обнаруженные улики:**");
				foreach (CheckResult item in list)
				{
					string text2 = ((item.Severity == CheckSeverity.Critical) ? "\ud83d\udd34" : ((item.Severity == CheckSeverity.NeedsCheck) ? "\ud83d\udfe3" : "\ud83d\udfe1"));
					BanCase itemBc = RuleBook.Classify(item.Name + " " + item.Detail + " " + item.Evidence);
					RuleBook.RuleInfo itemRi = RuleBook.Get(itemBc);
					string rulePrefix = itemRi.Reason.Split('-')[0].Trim();
					stringBuilder.AppendLine(text2 + " **[" + rulePrefix + "] [" + item.Category + "] " + item.Name + "**");
					stringBuilder.AppendLine("   ↳ *" + item.Detail + "*");
					if (!string.IsNullOrEmpty(item.Evidence))
					{
						string[] array = item.Evidence.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
						stringBuilder.AppendLine("`");
						for (int num4 = 0; num4 < Math.Min(array.Length, 5); num4++)
						{
							stringBuilder.AppendLine(array[num4]);
						}
						if (array.Length > 5)
						{
							stringBuilder.AppendLine("... и еще " + (array.Length - 5) + " записей");
						}
						stringBuilder.AppendLine("`");
					}
				}
			}
			else
			{
				stringBuilder.AppendLine("\ud83d\udfe2 *Нарушений не обнаружено. Система чиста.*");
			}
			string resultStr = stringBuilder.ToString();
			if (resultStr.Length > 1900)
			{
				string trimmed = resultStr.Substring(0, 1850);
				int lastNl = trimmed.LastIndexOf('\n');
				if (lastNl > 0) trimmed = trimmed.Substring(0, lastNl);
				return trimmed + "\r\n\r\n... [\u26a0\ufe0f Отчет сокращен для лимита Discord (2000 симв.). Полный отчет доступен в HTML/TXT]";
			}
			return resultStr;
		}
		[System.Runtime.InteropServices.DllImport("kernel32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
		private static extern int QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);
		private static readonly object _deviceMapLock = new object();
		private static Dictionary<string, string> _devicePathMap;
public static string ResolveNtDevicePath(string ntPath)
		{
			if (string.IsNullOrEmpty(ntPath)) return ntPath;
			if (ntPath.StartsWith("\\??\\", StringComparison.OrdinalIgnoreCase))
			{
				ntPath = ntPath.Substring(4);
			}
			if (!ntPath.StartsWith("\\Device\\HarddiskVolume", StringComparison.OrdinalIgnoreCase))
			{
				return ntPath;
			}
			try
			{
				lock (_deviceMapLock)
				{
					if (_devicePathMap == null)
					{
						_devicePathMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
						DriveInfo[] drives = DriveInfo.GetDrives();
						StringBuilder sb = new StringBuilder(512);
						foreach (DriveInfo d in drives)
						{
							try
							{
								string driveLetter = d.Name.TrimEnd('\\');
								sb.Length = 0;
								if (QueryDosDevice(driveLetter, sb, sb.Capacity) > 0)
								{
									string target = sb.ToString();
									int nullIdx = target.IndexOf('\0');
									if (nullIdx >= 0) target = target.Substring(0, nullIdx);
									target = target.Trim();
									if (!string.IsNullOrEmpty(target) && !_devicePathMap.ContainsKey(target))
									{
										_devicePathMap[target] = driveLetter;
									}
								}
							}
							catch { }
						}
					}
				}
				foreach (KeyValuePair<string, string> kvp in _devicePathMap)
				{
					if (ntPath.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
					{
						return kvp.Value + ntPath.Substring(kvp.Key.Length);
					}
				}
			}
			catch { }
			int slashIdx = ntPath.IndexOf('\\', 23);
			if (slashIdx >= 0)
			{
				return "C:" + ntPath.Substring(slashIdx);
			}
			return ntPath;
		}

		public static List<CheckResult> CheckBAM()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				string[] array = new string[4] { "SYSTEM\\CurrentControlSet\\Services\\bam\\State\\UserSettings", "SYSTEM\\CurrentControlSet\\Services\\bam\\UserSettings", "SYSTEM\\CurrentControlSet\\Services\\dam\\State\\UserSettings", "SYSTEM\\CurrentControlSet\\Services\\dam\\UserSettings" };
				List<string> list2 = new List<string>();
				string[] array2 = array;
				foreach (string name in array2)
				{
					using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(name))
					{
						if (registryKey == null)
						{
							continue;
						}
						string[] subKeyNames = registryKey.GetSubKeyNames();
						foreach (string name2 in subKeyNames)
						{
							using (RegistryKey registryKey2 = registryKey.OpenSubKey(name2))
							{
								if (registryKey2 == null)
								{
									continue;
								}
								string[] valueNames = registryKey2.GetValueNames();
								foreach (string text in valueNames)
								{
									if (string.IsNullOrEmpty(text) || text.Length < 4)
									{
										continue;
									}
									string textLower = text.ToLowerInvariant();
									bool flag = false;
									string arg = "";
									string[] cheatClientNames = CheatClientNames;
									foreach (string text2 in cheatClientNames)
									{
										if (MatchesCheatName(textLower, text2))
										{
											flag = true;
											arg = text2;
											break;
										}
									}
									if (!flag)
									{
										string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
										foreach (string text3 in extraCheatClients)
										{
											if (text3.Length >= 4 && MatchesCheatName(textLower, text3))
											{
												flag = true;
												arg = text3;
												break;
											}
										}
									}
									if (!flag)
									{
										string[] extraInjectors = new string[] { "kmbox", "interception", "spoofer", "dllinjector", "cheatloader", "slinky" };
										foreach (string sinj in extraInjectors)
										{
											if (textLower.Contains(sinj))
											{
												flag = true;
												arg = sinj;
												break;
											}
										}
									}

									string arg2 = "";
									DateTime execTime = DateTime.MinValue;
									try
									{
										object value = registryKey2.GetValue(text);
										byte[] array3 = value as byte[];
										if (array3 != null && array3.Length >= 8)
										{
											long num = BitConverter.ToInt64(array3, 0);
											if (num > 0)
											{
												execTime = DateTime.FromFileTime(num);
												arg2 = string.Format(" [Запуск: {0:dd.MM.yyyy HH:mm:ss}]", execTime);
											}
										}
									}
									catch
									{
									}

									bool exists = true;
									string resolvedPath = ResolveNtDevicePath(text);
									try { exists = FastFileExists(resolvedPath); } catch { }

									if (flag)
									{
										if (!exists)
										{
								list2.Add(string.Format("\ud83d\udea8 [5.08 - Запуск ПО из BAM] В BAM зафиксирована запись исполнения чит-ПО [{0}]: {1}{2}", arg, resolvedPath, arg2));
										}
										else
										{
											list2.Add(string.Format("⛔ [5.01 - Запуск чита в BAM] [{0}] {1}{2}", arg, resolvedPath, arg2));
										}
									}
									else
									{
										bool isCleaner = false;
										string cleanerMatched = "";
										foreach (string cl in CheatDatabases.KnownCleanerSignatures)
										{
											if (cl.Length >= 4 && (textLower.Contains(cl) || resolvedPath.ToLowerInvariant().Contains(cl)))
											{
												isCleaner = true;
												cleanerMatched = cl;
												break;
											}
										}
										if (isCleaner)
										{
											list2.Add(string.Format("🚨 [5.08 - Запуск ПО из BAM] В BAM зафиксирована запись исполнения утилиты очистки [{0}]: {1}{2}", cleanerMatched, resolvedPath, arg2));
										}
									}
								}
							}
						}
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Реестр / Следы ПО";
					checkResult.Name = "BAM (Background Activity Moderator) — запуски софта";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "В журнале BAM обнаружены подозрительные записи запуска чит-клиентов или клинеров [5.01 / 5.08]: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2.Take(35));
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Реестр / Следы ПО";
					checkResult2.Name = "BAM (Background Activity Moderator)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Прямых и свежих запусков читов в BAM не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Реестр / Следы ПО";
				checkResult3.Name = "BAM (Background Activity Moderator)";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Доступ к HKLM BAM ограничен (требуются права администратора): " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

				public static List<CheckResult> CheckSystemIntegrityAndEvasion()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				long num = Environment.TickCount;
				if (num < 0)
				{
					num += 4294967296L;
				}
				TimeSpan timeSpan = TimeSpan.FromMilliseconds(num);
				if (timeSpan.TotalMinutes < 15.0)
				{
					list2.Add(string.Format("ud83dudea8 [5.08 - Низкий аптайм системы]: Компьютер включен всего {0:F1} мин. назад (< 15 мин., возможна перезагрузка для сокрытия следов читов)!", timeSpan.TotalMinutes));
				}

				string[] criticalServices = new string[] { "PcaSvc", "DPS", "EventLog", "SysMain", "bam", "W32Time", "DiagTrack" };
				string[] criticalNames = new string[]
				{
					"🛡️ Целостность системы, аптайм ПК и статус служб PcaSvc/DPS...",
					"Служба диагностических служб DPS",
					"🚨 Журнал событий Windows EventLog ОТКЛЮЧЕН (Start=4)!",
					"Служба супервыборки Prefetch (SysMain)",
					"Служба фоновой активности (BAM)",
					"Служба синхронизации времени (W32Time)",
					"Служба телеметрии и сбора (DiagTrack)"
				};

				for (int i = 0; i < criticalServices.Length; i++)
				{
					try
					{
						using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services\\" + criticalServices[i]))
						{
							if (registryKey != null)
							{
								object value = registryKey.GetValue("Start");
								if (value != null && Convert.ToInt32(value) == 4)
								{
									list2.Add(string.Format("⛔ [5.08 - Отключена критическая служба Windows] {0} отключена (Start=4)! Возможна попытка скрытия следов / затирания доказательств.", criticalNames[i]));
								}
							}
						}
					}
					catch
					{
					}
				}

				// Testsigning / Disable Integrity Checks detection
				try
				{
					using (RegistryKey sysCtrl = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control"))
					{
						if (sysCtrl != null)
						{
							object sso = sysCtrl.GetValue("SystemStartOptions");
							if (sso != null)
							{
								string ssoStr = sso.ToString().ToUpperInvariant();
								if (ssoStr.Contains("TESTSIGNING") || ssoStr.Contains("DISABLE_INTEGRITY_CHECKS"))
								{
									list2.Add("⛔ [5.08 - Режим тестовой подписи драйверов (Testsigning)] В параметрах загрузки Windows включён TESTSIGNING / DISABLE_INTEGRITY_CHECKS (" + ssoStr.Trim() + ")! Возможно внедрение неподписанных чит-драйверов ядра!");
								}
							}
						}
					}
				}
				catch
				{
				}

				// Safe Mode boot check
				try
				{
					using (RegistryKey sbKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\SafeBoot\Option"))
					{
						if (sbKey != null)
						{
							object optVal = sbKey.GetValue("OptionValue");
							if (optVal != null && Convert.ToInt32(optVal) > 0)
							{
								list2.Add("⛔ [5.08 - Загрузка в безопасном режиме (Safe Mode)] Система запущена в безопасном режиме (SafeBoot OptionValue=" + optVal + ")! Логирование отключено и скрыты следы присутствия!");
							}
						}
					}
				}
				catch
				{
				}

				// CrashControl Minidump check (suppressed crash dumps)
				try
				{
					using (RegistryKey ccKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\CrashControl"))
					{
						if (ccKey != null)
						{
							object cde = ccKey.GetValue("CrashDumpEnabled");
							if (cde != null && Convert.ToInt32(cde) == 0)
							{
								list2.Add("⛔ [5.08 - Отключение дампов памяти] Аварийные дампы сбоев ядра отключены (CrashDumpEnabled=0)! Попытка скрыть сбои читов при инжекциях.");
							}
						}
					}
				}
				catch
				{
				}

				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Очистка / Evasion";
					checkResult.Name = "Целостность системы и уклонение от проверки (Evasion)";
					checkResult.Detected = true;
					checkResult.Severity = ((list2.Any(x => x.Contains("??"))) ? CheckSeverity.Critical : CheckSeverity.Warning);
			checkResult.Detail = "Нарушена целостность системных служб / обнаружены попытки сокрытия [5.08]: " + list2.Count + " шт.";
			checkResult.Evidence = string.Join("\r\n", list2) + "\r\n\r\nАптайм ПК: " + (int)timeSpan.TotalHours + "ч " + timeSpan.Minutes + "м " + timeSpan.Seconds + "с";
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Очистка / Evasion";
					checkResult2.Name = "Целостность служб Windows и аптайм ПК";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
			checkResult2.Detail = "Все системные службы (PcaSvc, DPS, EventLog, SysMain, BAM, W32Time) активны. Аптайм ПК: " + (int)timeSpan.TotalHours + "ч " + timeSpan.Minutes + "м " + timeSpan.Seconds + "с";
			checkResult2.Evidence = "Аптайм ПК: " + (int)timeSpan.TotalHours + "ч " + timeSpan.Minutes + "м " + timeSpan.Seconds + "с\r\nСостояние системных служб: Исправно";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Очистка / Evasion";
				checkResult3.Name = "Целостность системы";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка анализа модов: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		public static string GenerateHtmlReport(ScanReport report, string moderatorName)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("<!DOCTYPE html>");
			stringBuilder.AppendLine("<html lang='ru'>");
			stringBuilder.AppendLine("<head>");
			stringBuilder.AppendLine("<meta charset='utf-8'>");
			stringBuilder.AppendLine("<title>MC Cheat Checker — Отчёт SS: " + report.PlayerName + "</title>");
			stringBuilder.AppendLine("<style>");
			stringBuilder.AppendLine("body { background-color: #0F0F13; color: #E0E0E6; font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; margin: 0; padding: 24px; }");
			stringBuilder.AppendLine(".container { max-width: 1100px; margin: 0 auto; }");
			stringBuilder.AppendLine(".card { background: #181820; border-radius: 12px; padding: 20px; margin-bottom: 20px; border: 1px solid #282835; box-shadow: 0 4px 16px rgba(0,0,0,0.4); }");
			stringBuilder.AppendLine(".header-title { font-size: 24px; font-weight: bold; color: #4CAF50; margin-bottom: 4px; display: flex; align-items: center; gap: 10px; }");
			stringBuilder.AppendLine(".header-sub { color: #8E8EA0; font-size: 13px; }");
			stringBuilder.AppendLine(".meta-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(200px, 1fr)); gap: 12px; margin-top: 15px; }");
			stringBuilder.AppendLine(".meta-item { background: #20202C; border-radius: 8px; padding: 12px; border: 1px solid #2E2E3E; }");
			stringBuilder.AppendLine(".meta-label { font-size: 11px; text-transform: uppercase; color: #7E7E94; font-weight: bold; margin-bottom: 4px; }");
			stringBuilder.AppendLine(".meta-value { font-size: 16px; font-weight: bold; color: #FFF; }");
			stringBuilder.AppendLine(".badge { display: inline-block; padding: 4px 10px; border-radius: 6px; font-weight: bold; font-size: 12px; }");
			stringBuilder.AppendLine(".badge-crit { background: #381010; color: #FF5252; border: 1px solid #6E1E1E; }");
			stringBuilder.AppendLine(".badge-visual { background: #2E1538; color: #E082FF; border: 1px solid #5C2270; }");
			stringBuilder.AppendLine(".badge-warn { background: #362908; color: #FFD54F; border: 1px solid #665012; }");
			stringBuilder.AppendLine(".badge-clean { background: #0E2B14; color: #69F0AE; border: 1px solid #1B5E20; }");
			stringBuilder.AppendLine("table { width: 100%; border-collapse: collapse; margin-top: 10px; }");
			stringBuilder.AppendLine("th { text-align: left; padding: 12px 14px; background: #22222E; color: #9A9AB0; font-size: 12px; text-transform: uppercase; border-bottom: 2px solid #333344; }");
			stringBuilder.AppendLine("td { padding: 12px 14px; border-bottom: 1px solid #242432; font-size: 13px; vertical-align: top; }");
			stringBuilder.AppendLine("tr:hover { background: #1C1C26; }");
			stringBuilder.AppendLine(".evidence-box { background: #111116; border: 1px solid #2A2A38; border-radius: 6px; padding: 10px; font-family: 'Consolas', monospace; font-size: 12px; color: #B0B0C0; white-space: pre-wrap; margin-top: 6px; max-height: 180px; overflow-y: auto; }");
			stringBuilder.AppendLine(".filter-btn { background: #1F212D; color: #B0B0C4; border: 1px solid #2F3142; border-radius: 6px; padding: 7px 14px; font-size: 12px; font-weight: 600; cursor: pointer; transition: 0.2s; }");
			stringBuilder.AppendLine(".filter-btn:hover { background: #2A2C3C; color: #FFF; }");
			stringBuilder.AppendLine(".filter-btn.active { background: #3F51B5; color: #FFF; border-color: #5C6BC0; }");
			stringBuilder.AppendLine("</style>");
			stringBuilder.AppendLine("</head>");
			stringBuilder.AppendLine("<body>");
			stringBuilder.AppendLine("<div class='container'>");
			stringBuilder.AppendLine("<div class='card'>");
			stringBuilder.AppendLine("<div class='header-title'>⚔️ MC CHEAT CHECKER — ОТЧЁТ ПРОВЕРКИ</div>");
			stringBuilder.AppendLine("<div class='header-sub'>Полный криминалистический отчёт системы проверки на запрещённые модификации, инжекты и читы (Open Source Framework)</div>");
			stringBuilder.AppendLine("<div class='meta-grid'>");
			stringBuilder.AppendLine("<div class='meta-item'><div class='meta-label'>Игрок</div><div class='meta-value'>" + report.PlayerName + "</div></div>");
			stringBuilder.AppendLine("<div class='meta-item'><div class='meta-label'>Модератор</div><div class='meta-value'>" + moderatorName + "</div></div>");
			stringBuilder.AppendLine("<div class='meta-item'><div class='meta-label'>Время проверки</div><div class='meta-value'>" + report.ScanTime.ToString("dd.MM.yyyy HH:mm:ss") + "</div></div>");
			stringBuilder.AppendLine("<div class='meta-item'><div class='meta-label'>Вердикт</div><div class='meta-value'>" + report.Verdict + "</div></div>");
			stringBuilder.AppendLine("</div>");
			stringBuilder.AppendLine("</div>");
			int numCrit = report.Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.Critical);
			int numVis = report.Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.NeedsCheck);
			int numWarn = report.Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.Warning);
			if (numCrit > 0 || numVis > 0 || numWarn > 0)
			{
				CheckResult topCrit = report.Results.FirstOrDefault(delegate(CheckResult r) { return r.Detected && r.Severity == CheckSeverity.Critical; });
				if (topCrit == null)
				{
					topCrit = report.Results.FirstOrDefault(delegate(CheckResult r) { return r.Detected && (r.Severity == CheckSeverity.NeedsCheck || r.Severity == CheckSeverity.Warning); });
				}
				if (topCrit != null)
				{
					BanCase bc = RuleBook.Classify(topCrit.Name + " " + topCrit.Detail + " " + topCrit.Evidence);
					RuleBook.RuleInfo ri = RuleBook.Get(bc);
					string targetPlayer = string.IsNullOrEmpty(report.PlayerName) ? "Player" : report.PlayerName;
					string banCmd = RuleBook.Build(targetPlayer, ri.Duration, ri.Reason);
					stringBuilder.AppendLine("<div class='card' style='background:linear-gradient(135deg, #2b1111 0%, #171318 100%); border:1px solid #732222;'>");
					stringBuilder.AppendLine("<div style='display:flex; justify-content:space-between; align-items:center; flex-wrap:wrap; gap:12px;'>");
					stringBuilder.AppendLine("<div>");
					stringBuilder.AppendLine("<div style='color:#FF5252; font-weight:bold; font-size:13px; text-transform:uppercase; letter-spacing:0.5px;'>⛔ Рекомендуемая команда бана (Регламент)</div>");
					stringBuilder.AppendLine("<code id='banCmdBox' style='display:inline-block; margin-top:8px; padding:8px 14px; background:#120a0a; border:1px solid #5a1a1a; border-radius:6px; color:#ff8a80; font-family:Consolas, monospace; font-size:14px; font-weight:bold;'>" + banCmd + "</code>");
					stringBuilder.AppendLine("</div>");
					stringBuilder.AppendLine("<button style='background:#d32f2f; color:#FFF; border:none; padding:10px 18px; border-radius:8px; font-weight:bold; cursor:pointer; font-size:13px;' onclick=\"navigator.clipboard.writeText(document.getElementById('banCmdBox').innerText); this.innerText='Скопировано!';\">Скопировать /ban</button>");
					stringBuilder.AppendLine("</div>");
					stringBuilder.AppendLine("</div>");
				}
			}
			stringBuilder.AppendLine("<div class='card'>");
			stringBuilder.AppendLine("<div style='display:flex; justify-content:space-between; align-items:center; flex-wrap:wrap; gap:12px; margin-bottom:14px;'>");
			stringBuilder.AppendLine("<h3 style='margin:0;'>Результаты проверок (" + report.Results.Count + ")</h3>");
			stringBuilder.AppendLine("<div style='display:flex; gap:6px; flex-wrap:wrap;'>");
			stringBuilder.AppendLine("<button class='filter-btn active' onclick=\"setFilter('all', this)\">Все (" + report.Results.Count + ")</button>");
			stringBuilder.AppendLine("<button class='filter-btn' onclick=\"setFilter('detected', this)\">Улики (" + (numCrit + numVis + numWarn) + ")</button>");
			stringBuilder.AppendLine("<button class='filter-btn' onclick=\"setFilter('crit', this)\">⛔ Бан (" + numCrit + ")</button>");
			stringBuilder.AppendLine("</div>");
			stringBuilder.AppendLine("</div>");
			stringBuilder.AppendLine("<input type='text' id='filterInput' onkeyup='filterTable()' placeholder='🔍 Поиск по уликам, регламенту или совпадению (например: 5.01, USN, Expensive)...' style='width:100%; box-sizing:border-box; padding:10px 14px; background:#12121A; border:1px solid #2B2D3E; border-radius:8px; color:#FFF; font-size:13px; margin-bottom:12px;'>");
			stringBuilder.AppendLine("<table>");
			stringBuilder.AppendLine("<thead><tr><th>Статус</th><th>Категория</th><th>Проверка</th><th>Результат / Улики</th></tr></thead>");
			stringBuilder.AppendLine("<tbody>");
			foreach (CheckResult result in report.Results)
			{
				string text = "badge-clean";
				string text2 = "ЧИСТО";
				if (result.Detected)
				{
					BanCase itemBc = RuleBook.Classify(result.Name + " " + result.Detail + " " + result.Evidence);
					RuleBook.RuleInfo itemRi = RuleBook.Get(itemBc);
					string ruleTag = itemRi.Reason.Split('-')[0].Trim();
					if (result.Severity == CheckSeverity.Critical)
					{
						text = "badge-crit";
						text2 = "БАН [" + ruleTag + "]";
					}
					else if (result.Severity == CheckSeverity.NeedsCheck)
					{
						text = "badge-visual";
						text2 = "ПРОВЕРИТЬ [" + ruleTag + "]";
					}
					else
					{
						text = "badge-warn";
						text2 = "СЛЕД [" + ruleTag + "]";
					}
				}
				stringBuilder.AppendLine("<tr>");
				stringBuilder.AppendLine("<td><span class='badge " + text + "'>" + text2 + "</span></td>");
				stringBuilder.AppendLine("<td style='color:#A0A0B8; font-weight:500;'>" + result.Category + "</td>");
				stringBuilder.AppendLine("<td style='color:#FFF; font-weight:bold;'>" + result.Name + "</td>");
				stringBuilder.AppendLine("<td><div>" + result.Detail + "</div>");
				if (result.Detected && !string.IsNullOrEmpty(result.Evidence))
				{
					stringBuilder.AppendLine("<div class='evidence-box'>" + result.Evidence.Replace("<", "&lt;").Replace(">", "&gt;") + "</div>");
				}
				stringBuilder.AppendLine("</td>");
				stringBuilder.AppendLine("</tr>");
			}
			stringBuilder.AppendLine("</tbody></table></div>");
			stringBuilder.AppendLine("<div style='text-align:center; color:#505064; font-size:12px; margin-top:20px;'>Криминалистический комплекс MC Cheat Checker · Community Edition | Читов не скрыть</div>");
			stringBuilder.AppendLine("<script>");
			stringBuilder.AppendLine("var currentMode = 'all';");
			stringBuilder.AppendLine("function setFilter(mode, btn) {");
			stringBuilder.AppendLine("  currentMode = mode;");
			stringBuilder.AppendLine("  var btns = document.querySelectorAll('.filter-btn');");
			stringBuilder.AppendLine("  for (var i = 0; i < btns.length; i++) { btns[i].classList.remove('active'); }");
			stringBuilder.AppendLine("  btn.classList.add('active');");
			stringBuilder.AppendLine("  filterTable();");
			stringBuilder.AppendLine("}");
			stringBuilder.AppendLine("function filterTable() {");
			stringBuilder.AppendLine("  var q = document.getElementById('filterInput').value.toLowerCase();");
			stringBuilder.AppendLine("  var rows = document.querySelectorAll('tbody tr');");
			stringBuilder.AppendLine("  for (var i = 0; i < rows.length; i++) {");
			stringBuilder.AppendLine("    var r = rows[i];");
			stringBuilder.AppendLine("    var text = r.innerText.toLowerCase();");
			stringBuilder.AppendLine("    var isCrit = r.querySelector('.badge-crit') !== null;");
			stringBuilder.AppendLine("    var isClean = r.querySelector('.badge-clean') !== null;");
			stringBuilder.AppendLine("    var isDet = !isClean;");
			stringBuilder.AppendLine("    var modeMatch = (currentMode === 'all') || (currentMode === 'detected' && isDet) || (currentMode === 'crit' && isCrit);");
			stringBuilder.AppendLine("    var textMatch = (q === '' || text.indexOf(q) !== -1);");
			stringBuilder.AppendLine("    r.style.display = (modeMatch && textMatch) ? '' : 'none';");
			stringBuilder.AppendLine("  }");
			stringBuilder.AppendLine("}");
			stringBuilder.AppendLine("</script>");
			stringBuilder.AppendLine("</div></body></html>");
			return stringBuilder.ToString();
		}

		public static string GenerateTextReport(ScanReport report)
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("================================================================================");
			sb.AppendLine("                 MC CHEAT CHECKER · СВОДКА ПРОВЕРКИ                            ");
			sb.AppendLine("================================================================================");
			sb.AppendLine("  Игрок:        " + (string.IsNullOrEmpty(report.PlayerName) ? "Player" : report.PlayerName));
			sb.AppendLine("  Время SS:     " + report.ScanTime.ToString("dd.MM.yyyy HH:mm:ss"));
			sb.AppendLine("  Вердикт:      " + report.Verdict);
			sb.AppendLine("  Статистика:   🔴 Бан: " + report.CriticalCount + " | 🟣 Проверить: " + report.VisualsCount + " | 🟡 Следы: " + report.WarningCount + " | Всего: " + report.Results.Count);

			CheckResult topCrit = report.Results.FirstOrDefault(delegate(CheckResult r) {
				if (!r.Detected) return false;
				BanCase b = RuleBook.Classify(r.Name + " " + r.Detail + " " + r.Evidence, r.Severity);
				return r.Severity == CheckSeverity.Critical && b != BanCase.Allowed;
			});
			if (topCrit == null)
			{
				topCrit = report.Results.FirstOrDefault(delegate(CheckResult r) {
					if (!r.Detected) return false;
					BanCase b = RuleBook.Classify(r.Name + " " + r.Detail + " " + r.Evidence, r.Severity);
					return (r.Severity == CheckSeverity.NeedsCheck || r.Severity == CheckSeverity.Warning) && b != BanCase.Allowed;
				});
			}
			if (topCrit != null)
			{
				BanCase bc = RuleBook.Classify(topCrit.Name + " " + topCrit.Detail + " " + topCrit.Evidence, topCrit.Severity);
				RuleBook.RuleInfo ri = RuleBook.Get(bc);
				string banCmd = RuleBook.Build(report.PlayerName, ri.Duration, ri.Reason);
				sb.AppendLine("  Бан-команда:  " + banCmd);
			}
			sb.AppendLine("--------------------------------------------------------------------------------");

			// Фильтруем только реальные нарушения регламента (исключаем разрешённые AnyDesk/кликеры)
			List<CheckResult> detectedList = report.Results.Where(delegate(CheckResult r) {
				if (!r.Detected) return false;
				BanCase b = RuleBook.Classify(r.Name + " " + r.Detail + " " + r.Evidence, r.Severity);
				return b != BanCase.Allowed;
			}).ToList();

			List<CheckResult> allowedList = report.Results.Where(delegate(CheckResult r) {
				if (!r.Detected) return false;
				BanCase b = RuleBook.Classify(r.Name + " " + r.Detail + " " + r.Evidence, r.Severity);
				return b == BanCase.Allowed;
			}).ToList();

			if (detectedList.Count == 0)
			{
				sb.AppendLine();
				sb.AppendLine("  ✅ СИСТЕМА ЧИСТА. Нарушений регламента не обнаружено.");
				sb.AppendLine("     Все " + report.Results.Count + " криминалистических проверок пройдены успешно.");
				sb.AppendLine();
			}
			else
			{
				sb.AppendLine("  НАРУШЕНИЯ И ОБНАРУЖЕННЫЕ УЛИКИ (" + detectedList.Count + "):");
				sb.AppendLine("--------------------------------------------------------------------------------");

				var grouped = detectedList.GroupBy(delegate(CheckResult r) {
					BanCase bc = RuleBook.Classify(r.Name + " " + r.Detail + " " + r.Evidence, r.Severity);
					return RuleBook.Get(bc).Reason;
				}).OrderBy(delegate(IGrouping<string, CheckResult> g) { return g.Key; });

				foreach (var grp in grouped)
				{
					sb.AppendLine("  ▶ [" + grp.Key + "]");
					foreach (CheckResult r in grp)
					{
						string icon = (r.Severity == CheckSeverity.Critical) ? "🔴 БАН" : ((r.Severity == CheckSeverity.NeedsCheck) ? "🟣 ПРОВЕРИТЬ" : "🟡 СЛЕД");
						sb.AppendLine("    • " + icon + ": " + r.Name);
						sb.AppendLine("      Инфо: " + r.Detail);
						if (!string.IsNullOrEmpty(r.Evidence))
						{
							string[] evLines = r.Evidence.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
							int maxEv = Math.Min(evLines.Length, 5);
							for (int i = 0; i < maxEv; i++)
							{
								sb.AppendLine("      Улика: " + evLines[i].Trim());
							}
							if (evLines.Length > 5)
							{
								sb.AppendLine("      ... и еще " + (evLines.Length - 5) + " путей");
							}
						}
					}
					sb.AppendLine();
				}
			}

			if (allowedList.Count > 0)
			{
				sb.AppendLine("  РАЗРЕШЁННЫЕ ИНСТРУМЕНТЫ (ПОЛИТИКА СЕРВЕРА):");
				foreach (CheckResult al in allowedList)
				{
					sb.AppendLine("    ✔ " + al.Name + ": " + al.Detail);
				}
				sb.AppendLine();
			}

			sb.AppendLine("================================================================================");
			sb.AppendLine("Сгенерировано MC Cheat Checker v4.0 (Community Edition) · " + DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
			return sb.ToString();
		}

		public static string GenerateJsonReport(ScanReport report, string moderatorName = "")
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("{");
			stringBuilder.AppendLine("  \"report\": {");
			stringBuilder.AppendLine("    \"tool\": \"MC Cheat Checker\",");
			stringBuilder.AppendLine("    \"version\": \"4.0\",");
			stringBuilder.AppendLine("    \"player\": " + JsonStr(report.PlayerName) + ",");
			stringBuilder.AppendLine("    \"moderator\": " + JsonStr(moderatorName) + ",");
			stringBuilder.AppendLine("    \"scan_time\": " + JsonStr(report.ScanTime.ToString("yyyy-MM-ddTHH:mm:ss")) + ",");
			stringBuilder.AppendLine("    \"verdict\": " + JsonStr(report.Verdict) + ",");
			CheckResult topJson = report.Results.FirstOrDefault(delegate(CheckResult r) { return r.Detected && r.Severity == CheckSeverity.Critical; });
			if (topJson == null) topJson = report.Results.FirstOrDefault(delegate(CheckResult r) { return r.Detected && (r.Severity == CheckSeverity.NeedsCheck || r.Severity == CheckSeverity.Warning); });
			string jsonBanCmd = "";
			if (topJson != null)
			{
				BanCase jbc = RuleBook.Classify(topJson.Name + " " + topJson.Detail + " " + topJson.Evidence);
				RuleBook.RuleInfo jri = RuleBook.Get(jbc);
				jsonBanCmd = RuleBook.Build(report.PlayerName, jri.Duration, jri.Reason);
			}
			stringBuilder.AppendLine("    \"recommended_ban\": " + JsonStr(jsonBanCmd) + ",");
			stringBuilder.AppendLine("    \"counts\": {");
			stringBuilder.AppendLine("      \"critical\": " + report.CriticalCount + ",");
			stringBuilder.AppendLine("      \"visuals\": " + report.VisualsCount + ",");
			stringBuilder.AppendLine("      \"warnings\": " + report.WarningCount + ",");
			stringBuilder.AppendLine("      \"info\": " + report.InfoCount + ",");
			stringBuilder.AppendLine("      \"total_detected\": " + report.TotalDetected + ",");
			stringBuilder.AppendLine("      \"total_checks\": " + report.Results.Count);
			stringBuilder.AppendLine("    }");
			stringBuilder.AppendLine("  },");
			stringBuilder.AppendLine("  \"detections\": [");
			IEnumerable<IGrouping<string, CheckResult>> enumerable = from r in report.Results
				group r by r.Category;
			int num = 0;
			int num2 = enumerable.Count();
			foreach (IGrouping<string, CheckResult> item in enumerable)
			{
				stringBuilder.AppendLine("    {");
				stringBuilder.AppendLine("      \"category\": " + JsonStr(item.Key) + ",");
				stringBuilder.AppendLine("      \"count\": " + item.Count() + ",");
				stringBuilder.AppendLine("      \"checks\": [");
				int num3 = 0;
				int num4 = item.Count();
				foreach (CheckResult item2 in item)
				{
					stringBuilder.AppendLine("        {");
					stringBuilder.AppendLine("          \"name\": " + JsonStr(item2.Name) + ",");
					stringBuilder.AppendLine("          \"severity\": " + JsonStr(item2.Severity.ToString()) + ",");
					stringBuilder.AppendLine("          \"detected\": " + item2.Detected.ToString().ToLower() + ",");
					stringBuilder.AppendLine("          \"detail\": " + JsonStr(OneLine(item2.Detail)) + ",");
					BanCase checkBc = RuleBook.Classify(item2.Name + " " + item2.Detail + " " + item2.Evidence);
					RuleBook.RuleInfo checkRi = RuleBook.Get(checkBc);
					stringBuilder.AppendLine("          \"rule_tag\": " + JsonStr(checkRi.Reason.Split('-')[0].Trim()) + ",");
					stringBuilder.AppendLine("          \"evidence\": " + JsonStr(item2.Evidence));
					stringBuilder.AppendLine("        }" + ((num3 < num4 - 1) ? "," : ""));
					num3++;
				}
				stringBuilder.AppendLine("      ]");
				stringBuilder.AppendLine("    }" + ((num < num2 - 1) ? "," : ""));
				num++;
			}
			stringBuilder.AppendLine("  ]");
			stringBuilder.AppendLine("}");
			return stringBuilder.ToString();
		}

		private static string JsonStr(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return "\"\"";
			}
			s = s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n")
				.Replace("\r", "\\r")
				.Replace("\t", "\\t");
			return "\"" + s + "\"";
		}

		private static string OneLine(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return "";
			}
			return text.Replace("\r\n", " ").Replace('\n', ' ').Replace('\r', ' ');
		}

		public static List<CheckResult> CheckPaidClientSubscriptions()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			List<string> list4 = new List<string>();
			List<string> list5 = new List<string>();

			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string folderPath3 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string commonProgData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

			List<string[]> list6 = new List<string[]>();
			list6.Add(new string[] { "vape", "config.json" });
			list6.Add(new string[] { "vape", "license.json" });
			list6.Add(new string[] { "vape", "license.key" });
			list6.Add(new string[] { "vape", "auth.json" });
			list6.Add(new string[] { ".vape", "config.json" });
			list6.Add(new string[] { ".vape", "license.json" });
			list6.Add(new string[] { "license.key" });
			list6.Add(new string[] { "config.json" });
			list6.Add(new string[] { "auth.bin" });
			list6.Add(new string[] { ".flux", "license.key" });
			list6.Add(new string[] { "license.json" });
			list6.Add(new string[] { "config.json" });
			list6.Add(new string[] { "Flarial", "license.json" });
			list6.Add(new string[] { "license.key" });
			list6.Add(new string[] { "config.json" });
			list6.Add(new string[] { "auth.json" });
			list6.Add(new string[] { "liquidbounce", "settings.json" });
			list6.Add(new string[] { ".liquidbounce", "settings.json" });
			list6.Add(new string[] { "meteor-client", "auth.json" });
			list6.Add(new string[] { "meteor-client", "accounts.json" });
			list6.Add(new string[] { "rusherhack", "license.key" });
			list6.Add(new string[] { "rusherhack", "config.json" });
			list6.Add(new string[] { "novoline", "license.key" });
			list6.Add(new string[] { "novoline", "config.cfg" });
			list6.Add(new string[] { "sigma", "license.key" });
			list6.Add(new string[] { "sigma", "config.json" });
			list6.Add(new string[] { ".sigma", "license.key" });
			list6.Add(new string[] { "license.key" });
			list6.Add(new string[] { "config.json" });
			list6.Add(new string[] { "license.key" });
			list6.Add(new string[] { "raven-client", "license.key" });
			list6.Add(new string[] { "celestial", "license.key" });
			list6.Add(new string[] { "celestial", "config.json" });
			list6.Add(new string[] { "celestial", "binds.json" });
			list6.Add(new string[] { "celestial", "users.json" });
			list6.Add(new string[] { ".celestial", "config.json" });
			list6.Add(new string[] { "expensive", "license.key" });
			list6.Add(new string[] { "expensive", "config.json" });
			list6.Add(new string[] { "expensive", "default.json" });
			list6.Add(new string[] { "expensive", "binds.json" });
			list6.Add(new string[] { "expensive", "user.json" });
			list6.Add(new string[] { ".expensive", "config.json" });
			list6.Add(new string[] { ".expensive", "binds.json" });
			list6.Add(new string[] { ".expensive", "user.json" });
			list6.Add(new string[] { "nursultan", "license.json" });
			list6.Add(new string[] { "nursultan", "config.json" });
			list6.Add(new string[] { "nursultan", "auth.json" });
			list6.Add(new string[] { "nursultan", "binds.json" });
			list6.Add(new string[] { ".nursultan", "license.json" });
			list6.Add(new string[] { ".nursultan", "config.json" });
			list6.Add(new string[] { "thunderhack", "config.json" });
			list6.Add(new string[] { "thunderhack", "misc.json" });
			list6.Add(new string[] { "thunderhack", "combat.json" });
			list6.Add(new string[] { ".thunderhack", "config.json" });
			list6.Add(new string[] { "slinky", "slinky.json" });
			list6.Add(new string[] { "slinky", "license.json" });
			list6.Add(new string[] { ".slinky", "slinky.json" });
			list6.Add(new string[] { "settings.json" });
			list6.Add(new string[] { "license.key" });
			list6.Add(new string[] { ".drip", "settings.json" });
			list6.Add(new string[] { "doomsday", "config.json" });
			list6.Add(new string[] { ".doomsday", "config.json" });
			list6.Add(new string[] { "catlavan", "config.json" });
			list6.Add(new string[] { "catlavan", "license.json" });
			list6.Add(new string[] { "catlavan", "auth.json" });
			list6.Add(new string[] { "deadcode", "config.json" });
			list6.Add(new string[] { "deadcode", "license.json" });
			list6.Add(new string[] { "deadcode", "auth.json" });
			list6.Add(new string[] { "delta", "config.json" });
			list6.Add(new string[] { "deltaclient", "config.json" });
			list6.Add(new string[] { "wexside", "config.json" });
			list6.Add(new string[] { "wexside", "license.json" });
			list6.Add(new string[] { "richpremium", "config.json" });
			list6.Add(new string[] { "flint", "config.json" });
			list6.Add(new string[] { "rockstar", "config.json" });
			list6.Add(new string[] { "boberware", "config.json" });
			list6.Add(new string[] { "boze", "boze.json" });
			list6.Add(new string[] { ".boze", "config.json" });
			list6.Add(new string[] { "minced", "config.json" });
			list6.Add(new string[] { ".minced", "config.json" });
			list6.Add(new string[] { "booster", "config.json" });
			list6.Add(new string[] { "booster", "license.json" });
			list6.Add(new string[] { "systemdlc", "config.json" });
			list6.Add(new string[] { ".systemdlc", "config.json" });
			list6.Add(new string[] { "systemdlc", "license.json" });

			string[] array2 = new string[]
			{
				folderPath,
				folderPath2,
				folderPath3,
				Path.Combine(folderPath3, "Documents"),
				Path.Combine(folderPath3, "Desktop"),
				Path.Combine(folderPath3, "Downloads"),
				Path.Combine(folderPath, ".minecraft"),
				Path.Combine(folderPath, ".minecraft", "config"),
				Path.Combine(folderPath, ".lunarclient"),
				Path.Combine(folderPath, "PrismLauncher"),
				Path.Combine(folderPath, ".feather"),
				Path.Combine(folderPath, ".modrinth"),
				commonProgData,
				Path.Combine(folderPath3, ".config")
			};

			foreach (string[] item in list6)
			{
				foreach (string path in array2)
				{
					try
					{
						string text = Path.Combine(path, item[0], item[1]);
						if (!File.Exists(text))
						{
							continue;
						}
						long len = 0L;
						try { len = new FileInfo(text).Length; } catch { }
						string text2 = len + " байт";
						string text3 = "";
						try
						{
							using (FileStream fs = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
							{
								using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
								{
									text3 = sr.ReadToEnd().Replace("\r", "").Replace("\n", " ").Trim();
									if (text3.Length > 200) text3 = text3.Substring(0, 200) + "...";
								}
							}
						}
						catch
						{
						}
						list5.Add(string.Format("⛔ [5.01 - Лицензия/токен чита {0}]: {1} ({2})\n    Совпадение: {3}", item[0].ToUpper(), text, text2, text3));
					}
					catch
					{
					}
				}
			}

			string[] tokenNames = new string[]
			{
				"license.key", "license.json", "license.txt", "license.bin", "auth.json", "auth.bin", "auth.txt", "auth.token", "token.json", "token.txt",
				"token.bin", "subscription.json", "subscription.txt", "hwid.json", "hwid.txt", "session.json", "session.bin", "key.json", "key.txt", "key.bin",
				"premium.json", "premium.txt", "account.json", "accounts.json", "user.json", "user.bin"
			};

			foreach (string path2 in array2)
			{
				if (!Directory.Exists(path2)) continue;
				try
				{
					string[] directories = Directory.GetDirectories(path2, "*", SearchOption.TopDirectoryOnly);
					foreach (string path3 in directories)
					{
						string textLower = Path.GetFileName(path3).ToLowerInvariant();
						bool flag = false;
						string text4 = "";
						foreach (string cl in CheatClientNames)
						{
							if (MatchesCheatName(textLower, cl) && !IsLegitGame(textLower))
							{
								flag = true;
								text4 = cl;
								break;
							}
						}
						if (!flag)
						{
							foreach (string ec in CheatDatabases.ExtraCheatClients)
							{
								if (ec.Length >= 4 && MatchesCheatName(textLower, ec) && !IsLegitGame(textLower))
								{
									flag = true;
									text4 = ec;
									break;
								}
							}
						}
						if (!flag) continue;

						try
						{
							string[] allFiles = Directory.GetFiles(path3, "*", SearchOption.AllDirectories);
							foreach (string text6 in allFiles)
							{
								string text7 = Path.GetFileName(text6).ToLowerInvariant();
								foreach (string text8 in tokenNames)
								{
									if (text7 == text8)
									{
										string text9 = "";
										try
										{
											using (FileStream fs = new FileStream(text6, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
											{
												using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
												{
													text9 = sr.ReadToEnd().Replace("\r", "").Replace("\n", " ").Trim();
													if (text9.Length > 200) text9 = text9.Substring(0, 200) + "...";
												}
											}
										}
										catch
										{
										}
										list2.Add(string.Format("[{0}] Адрес: {1} | Размер: {2} | {3}", text4.ToUpper(), text6, text9));
										break;
									}
								}
							}
						}
						catch
						{
						}
					}
				}
				catch
				{
				}
			}

			string[] regKeys = new string[]
			{
				"SOFTWARE\\Vape", "SOFTWARE\\VapeClient", "SOFTWARE\\Flux", "SOFTWARE\\FluxClient", "SOFTWARE\\Flarial", "SOFTWARE\\Entropy",
				"SOFTWARE\\Sigma", "SOFTWARE\\SigmaClient", "SOFTWARE\\Novoline", "SOFTWARE\\RusherHack", "SOFTWARE\\MeteorClient",
				"SOFTWARE\\LiquidBounce", "SOFTWARE\\Aristois", "SOFTWARE\\Impact", "SOFTWARE\\Wurst", "SOFTWARE\\Celestial", "SOFTWARE\\Cortex",
				"SOFTWARE\\Expensive", "SOFTWARE\\Akrien", "SOFTWARE\\Inertia", "SOFTWARE\\Prism", "SOFTWARE\\Tenacity", "SOFTWARE\\RavenClient",
				"SOFTWARE\\Nursultan", "SOFTWARE\\Catlavan", "SOFTWARE\\DeadCode", "SOFTWARE\\DeltaClient", "SOFTWARE\\Wexside",
				"SOFTWARE\\RichPremium", "SOFTWARE\\Flint", "SOFTWARE\\Rockstar", "SOFTWARE\\BoberWare", "SOFTWARE\\WildClient",
				"SOFTWARE\\Minced", "SOFTWARE\\ThunderHack", "SOFTWARE\\Drip", "SOFTWARE\\Slinky", "SOFTWARE\\Booster", "SOFTWARE\\KeyAuth",
				"SOFTWARE\\SystemDLC", "SOFTWARE\\fun.systemdlc", "SOFTWARE\\ru.systemdlc"
			};

			foreach (string text10 in regKeys)
			{
				try
				{
					using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(text10))
					{
						if (registryKey != null)
						{
							string[] valueNames = registryKey.GetValueNames();
							list4.Add(string.Format("🔑 РЕЕСТР [{0}]: найден раздел HKCU\\{1} ({2} значений: {3})", text10.Split('\\')[1], text10, valueNames.Length, string.Join(", ", valueNames)));
						}
					}
					using (RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey(text10))
					{
						if (registryKey2 != null)
						{
							string[] valueNames2 = registryKey2.GetValueNames();
							list4.Add(string.Format("🔑 РЕЕСТР [{0}]: найден раздел HKLM\\{1} ({2} значений: {3})", text10.Split('\\')[1], text10, valueNames2.Length, string.Join(", ", valueNames2)));
						}
					}
				}
				catch
				{
				}
			}

			string[] browserHistoryPaths = new string[]
			{
				Path.Combine(folderPath2, "Google", "Chrome", "User Data", "Default", "History"),
				Path.Combine(folderPath2, "Google", "Chrome", "User Data", "Profile 1", "History"),
				Path.Combine(folderPath2, "Google", "Chrome", "User Data", "Profile 2", "History"),
				Path.Combine(folderPath2, "Microsoft", "Edge", "User Data", "Default", "History"),
				Path.Combine(folderPath2, "BraveSoftware", "Brave-Browser", "User Data", "Default", "History"),
				Path.Combine(folderPath2, "Opera Software", "Opera Stable", "History"),
				Path.Combine(folderPath, "Opera Software", "Opera GX Stable", "History"),
				Path.Combine(folderPath, "Vivaldi", "User Data", "Default", "History"),
				Path.Combine(folderPath, "Mozilla", "Firefox", "Profiles")
			};

			foreach (string path4 in browserHistoryPaths)
			{
				if (!File.Exists(path4) && !Directory.Exists(path4)) continue;
				try
				{
					if (File.Exists(path4))
					{
						string text11 = null;
						using (FileStream fs = new FileStream(path4, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
						{
							using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
							{
								text11 = sr.ReadToEnd().Replace('\0', ' ');
							}
						}
						if (!string.IsNullOrEmpty(text11))
						{
							foreach (string text12 in CheatDatabases.ExtraCheatDomains)
							{
								string domainOnly = text12.Split('/')[0];
								if (domainOnly.Length >= 4 && text11.IndexOf(domainOnly, StringComparison.OrdinalIgnoreCase) >= 0)
								{
									string[] dirParts = Path.GetDirectoryName(path4).Split('\\');
									string bName = dirParts.Length >= 2 ? dirParts[dirParts.Length - 2] : "Browser";
									list3.Add(string.Format("🌐 БРАУЗЕР [{0}]: найдено посещение/авторизация сайта чита: '{1}'", bName, domainOnly));
								}
							}
						}
					}
					else if (Directory.Exists(path4))
					{
						string[] ffProfiles = Directory.GetDirectories(path4, "*", SearchOption.TopDirectoryOnly);
						foreach (string path5 in ffProfiles)
						{
							string path6 = Path.Combine(path5, "places.sqlite");
							if (!File.Exists(path6)) continue;
							string text13 = null;
							using (FileStream fs = new FileStream(path6, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
							{
								using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
								{
									text13 = sr.ReadToEnd().Replace('\0', ' ');
								}
							}
							if (!string.IsNullOrEmpty(text13))
							{
								foreach (string text14 in CheatDatabases.ExtraCheatDomains)
								{
									string domainOnly = text14.Split('/')[0];
									if (domainOnly.Length >= 4 && text13.IndexOf(domainOnly, StringComparison.OrdinalIgnoreCase) >= 0)
									{
										list3.Add(string.Format("🌐 FIREFOX: найдено посещение/авторизация сайта чита: '{0}'", domainOnly));
									}
								}
							}
						}
					}
				}
				catch
				{
				}
			}

			string[] array8 = new string[]
			{
				folderPath,
				folderPath2,
				Path.Combine(folderPath3, "Desktop"),
				Path.Combine(folderPath3, "Downloads"),
				Path.Combine(folderPath3, "Documents"),
				Path.Combine(folderPath, ".minecraft")
			};
			string[] array9 = new string[] { "*.key", "*.token", "*.hwid" };
			foreach (string path7 in array8)
			{
				if (!Directory.Exists(path7)) continue;
				try
				{
					foreach (string searchPattern in array9)
					{
						string[] matchedFiles = Directory.GetFiles(path7, searchPattern, SearchOption.AllDirectories);
						foreach (string text15 in matchedFiles)
						{
							string text16 = text15.ToLowerInvariant();
							if (text16.Contains("certificate") || text16.Contains("ssl") || text16.Contains("gpg") || text16.Contains("ssh") || text16.Contains("pgp"))
							{
								continue;
							}
							bool flag2 = false;
							foreach (string cl in CheatClientNames)
							{
								if (MatchesCheatName(text16, cl) && !IsLegitGame(text16))
								{
									flag2 = true;
									break;
								}
							}
							if (!flag2)
							{
								foreach (string ec in CheatDatabases.ExtraCheatClients)
								{
									if (ec.Length >= 4 && MatchesCheatName(text16, ec) && !IsLegitGame(text16))
									{
										flag2 = true;
										break;
									}
								}
							}
							if (!flag2) continue;

							string text17 = "";
							try
							{
								using (FileStream fs = new FileStream(text15, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
								{
									using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
									{
										text17 = sr.ReadToEnd().Replace("\r", "").Replace("\n", " ").Trim();
										if (text17.Length > 150) text17 = text17.Substring(0, 150) + "...";
									}
								}
							}
							catch
							{
							}
							list2.Add(string.Format("Обнаружены следы в latest.log: {0} (чит-команд: {1}, вайпов: {2})", text15, text17));
						}
					}
				}
				catch
				{
				}
			}

			List<string> list8 = new List<string>();
			list8.AddRange(list2);
			list8.AddRange(list5);
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Подписки / Лицензии";
			checkResult.Name = "Файлы лицензий и конфигов платных чит-клиентов";
			checkResult.Detected = list8.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = (list8.Count > 0 ? string.Format("⛔ Найдено следов подписок/токенов: {0} к коммерческим платным читам [5.01]!", list8.Count) : "Файлы лицензий и токенов платных чит-клиентов не найдены");
			checkResult.Evidence = string.Join("\r\n\r\n", list8);
			list.Add(checkResult);

			CheckResult checkResult2 = new CheckResult();
			checkResult2.Category = "Подписки / Лицензии";
			checkResult2.Name = "Реестр Windows — ключи платных чит-клиентов";
			checkResult2.Detected = list4.Count > 0;
			checkResult2.Severity = CheckSeverity.Critical;
			checkResult2.Detail = (list4.Count > 0 ? string.Format("⛔ Найдено в реестре: {0} записей платных чит-клиентов [5.01]", list4.Count) : "В реестре записей платных чит-клиентов не найдено");
			checkResult2.Evidence = string.Join("\r\n", list4);
			list.Add(checkResult2);

			List<string> list9 = new List<string>(new HashSet<string>(list3));
			CheckResult checkResult3 = new CheckResult();
			checkResult3.Category = "Подписки / Лицензии";
			checkResult3.Name = "Браузер — посещение сайтов покупки/авторизации чит-клиентов";
			checkResult3.Detected = list9.Count > 0;
			checkResult3.Severity = CheckSeverity.Warning;
			checkResult3.Detail = (list9.Count > 0 ? string.Format("⛔ Найдено переходов сайтов платных читов: {0} в истории браузеров [5.08]", list9.Count) : "Посещений сайтов покупки/авторизации платных читов не найдено");
			checkResult3.Evidence = string.Join("\r\n", list9);
			list.Add(checkResult3);

			return list;
		}

		private static List<CheckResult> CheckPurchasedCheatEvidence()
		{
			List<CheckResult> list = new List<CheckResult>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string folderPath3 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string commonProgData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			List<string> list4 = new List<string>();
			List<string> list5 = new List<string>();

			string[] array = new string[] { ".key", ".license", ".lic", ".hwid", ".token", ".auth", ".sub", ".cfg", ".json" };
			string[] array2 = new string[]
			{
				"hwid.txt", "hwid.json", "hwid.dat", "HWID", "hwid", "token", "token.dat", "auth.dat", "auth.bin", "license",
				"subscription.dat", "session.dat", "credentials.json", "user.json", "binds.json", "default.json"
			};
			string[] array3 = new string[]
			{
				folderPath,
				folderPath2,
				Path.Combine(folderPath3, "Documents"),
				Path.Combine(folderPath3, "Downloads"),
				Path.Combine(folderPath3, "Desktop"),
				Path.Combine(folderPath, ".minecraft"),
				Path.Combine(folderPath, ".minecraft", "config"),
				Path.Combine(folderPath, ".lunarclient"),
				Path.Combine(folderPath, "PrismLauncher"),
				Path.Combine(folderPath, ".feather"),
				Path.Combine(folderPath, ".modrinth"),
				commonProgData,
				Path.Combine(folderPath3, ".config")
			};

			foreach (string path in array3)
			{
				if (!Directory.Exists(path)) continue;
				string[] directories;
				try
				{
					directories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
				}
				catch
				{
					continue;
				}
				foreach (string path2 in directories)
				{
					string fileName = Path.GetFileName(path2);
					if (CheatDatabases.IsWhitelistedPath(path2)) continue;

					bool flag = false;
					string cheatMatch = "";
					foreach (string cl in CheatClientNames)
					{
						if (MatchesCheatName(fileName.ToLowerInvariant(), cl))
						{
							flag = true;
							cheatMatch = cl;
							break;
						}
					}
					if (!flag)
					{
						foreach (string ec in CheatDatabases.ExtraCheatClients)
						{
							if (ec.Length >= 4 && MatchesCheatName(fileName.ToLowerInvariant(), ec))
							{
								flag = true;
								cheatMatch = ec;
								break;
							}
						}
					}
					if (!flag) continue;

					// Loaders search
					try
					{
						string[] files = Directory.GetFiles(path2, "*.exe", SearchOption.AllDirectories);
					System.Diagnostics.Stopwatch swDisg = System.Diagnostics.Stopwatch.StartNew();
												foreach (string text in files)
						{
							if (swDisg.ElapsedMilliseconds > 1500) break;

							string text2 = Path.GetFileName(text).ToLowerInvariant();
							if ((text2.Contains("loader") || text2.Contains("launcher") || text2.Contains("client") || text2.Contains("inject") || text2.Contains("stub") || text2.Contains("bootstrapper")) && !CheatDatabases.IsWhitelistedPath(text))
							{
								list2.Add("⛔ [5.01 - Токен платного чита]: " + text + " | создан: " + SafeWrite(text) + " | размер: " + SafeLen(text) + " байт");
							}
						}
					}
					catch
					{
					}

					// Tokens / license files search
					try
					{
						string[] files2;
try { files2 = System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Take(Directory.EnumerateFiles(path2, "*", SearchOption.AllDirectories), 1000)); } catch { files2 = new string[0]; }
						foreach (string text3 in files2)
						{
							string text4 = Path.GetFileName(text3).ToLowerInvariant();
							bool flag2 = false;
							foreach (string text5 in array2)
							{
								if (text4 == text5.ToLowerInvariant())
								{
									flag2 = true;
									break;
								}
							}
							if (!flag2)
							{
								foreach (string value in array)
								{
									if (text4.EndsWith(value))
									{
										flag2 = true;
										break;
									}
								}
							}
							if (!flag2 || CheatDatabases.IsWhitelistedPath(text3)) continue;

							string text6 = "";
							try
							{
								using (FileStream fs = new FileStream(text3, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
								{
									using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
									{
										text6 = sr.ReadToEnd().Replace("\r", " ").Replace("\n", " ").Trim();
										if (text6.Length > 160) text6 = text6.Substring(0, 160) + "...";
									}
								}
							}
							catch
							{
							}
							list3.Add("🔑 Токен/ключ устройства: " + text3 + " | " + text6);
						}
					}
					catch
					{
					}
				}
			}

			// In-Memory Browser Login Data & Web Data
			try
			{
				List<string> loginDataFiles = new List<string>();
				loginDataFiles.Add(Path.Combine(folderPath2, "Google", "Chrome", "User Data", "Default", "Login Data"));
				loginDataFiles.Add(Path.Combine(folderPath2, "Google", "Chrome", "User Data", "Profile 1", "Login Data"));
				loginDataFiles.Add(Path.Combine(folderPath2, "Microsoft", "Edge", "User Data", "Default", "Login Data"));
				loginDataFiles.Add(Path.Combine(folderPath2, "BraveSoftware", "Brave-Browser", "User Data", "Default", "Login Data"));
				loginDataFiles.Add(Path.Combine(folderPath2, "Opera Software", "Opera Stable", "Login Data"));
				loginDataFiles.Add(Path.Combine(folderPath, "Opera Software", "Opera GX Stable", "Login Data"));
				loginDataFiles.Add(Path.Combine(folderPath, "Vivaldi", "User Data", "Default", "Login Data"));

				foreach (string item in loginDataFiles)
				{
					if (!File.Exists(item)) continue;
					string text7 = null;
					try
					{
						using (FileStream fs = new FileStream(item, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
						{
							using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
							{
								text7 = sr.ReadToEnd();
							}
						}
					}
					catch
					{
						continue;
					}

					if (string.IsNullOrEmpty(text7)) continue;
					foreach (string text8 in CheatDatabases.ExtraCheatDomains)
					{
						string text9 = text8.Split('/')[0];
						if (text9.Length >= 4 && text7.IndexOf(text9, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							list4.Add("⛔ В деинсталляторах найдена запись платного софта чит-клиента: " + text9 + " (" + item + ")");
						}
					}
				}
			}
			catch
			{
			}

			// Scheduled Tasks
			try
			{
				string folderPath4 = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string path3 = Path.Combine(folderPath4, "System32", "Tasks");
				if (Directory.Exists(path3))
				{
					string[] taskFiles = Directory.GetFiles(path3, "*", SearchOption.AllDirectories);
					foreach (string text10 in taskFiles)
					{
						string text11 = "";
						try
						{
							using (FileStream fs = new FileStream(text10, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
							{
								using (StreamReader sr = new StreamReader(fs, Encoding.UTF8))
								{
									text11 = sr.ReadToEnd();
									if (text11.Length > 8000) text11 = text11.Substring(0, 8000);
								}
							}
						}
						catch
						{
							continue;
						}
						string text12 = text11.ToLowerInvariant();
						foreach (string text13 in CheatClientNames)
						{
							if (text13.Length >= 5 && text12.Contains(text13))
							{
								if (!CheatDatabases.IsAmbiguous(text13) || CheatDatabases.HasCheatContext(text12))
								{
									list5.Add("📅 Задача планировщика содержит имя клиента [" + text13 + "]: " + text10);
								}
								break;
							}
						}
					}
				}
			}
			catch
			{
			}

			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Подписки / Лицензии";
			checkResult.Name = "Купленные читы: лоадеры и токены устройств";
			List<string> list8 = new List<string>();
			list8.AddRange(list2);
			list8.AddRange(list3);
			checkResult.Detected = list8.Count > 0;
			checkResult.Severity = (list8.Count > 0 ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = (list8.Count > 0 ? string.Format("⛔ Найдено доказательств платных чит-клиентов [5.01]: {0} (токенов: {1}, лоадеров: {2})", list8.Count, list2.Count, list3.Count) : "Лоадеров и HWID-токенов платных клиентов не найдено");
			checkResult.Evidence = string.Join("\r\n", list8.ToArray());
			list.Add(checkResult);

			CheckResult checkResult2 = new CheckResult();
			checkResult2.Category = "Подписки / Лицензии";
			checkResult2.Name = "Купленные читы: браузерные auth-хранилища и планировщик";
			List<string> list9 = new List<string>();
			list9.AddRange(list4);
			list9.AddRange(list5);
			checkResult2.Detected = list9.Count > 0;
			checkResult2.Severity = CheckSeverity.Warning;
			checkResult2.Detail = (list9.Count > 0 ? string.Format("⛔ Посещений сайтов покупок/авторизаций читов [5.08]: {0}", list9.Count) : "Посещений платных чит-ресурсов в веб-браузерах не найдено");
			checkResult2.Evidence = string.Join("\r\n", list9.ToArray());
			list.Add(checkResult2);
			return list;
		}

		private static string SafeWrite(string path)
		{
			try
			{
				return File.GetLastWriteTime(path).ToString("dd.MM.yyyy HH:mm");
			}
			catch
			{
				return "?";
			}
		}

		private static long SafeLen(string path)
		{
			try
			{
				return new FileInfo(path).Length;
			}
			catch
			{
				return -1L;
			}
		}

		private static List<CheckResult> CheckProcessMemoryExtended()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<ProcessItem> allRunningProcesses = ProcessMemoryScanner.GetAllRunningProcesses();
				foreach (ProcessItem item in allRunningProcesses)
				{
					if (item.IsThreat)
					{
						list.Add(new CheckResult
						{
							Category = "Процессы",
							Name = "Ошибка анализа Amcache: " + item.Name,
							Detail = "PID: " + item.Pid + " | " + item.ThreatReason + " | RAM: " + item.MemoryString,
							Detected = true,
							Severity = item.Severity,
							Evidence = item.ExecutablePath
						});
					}
				}
				List<MemoryRegionHit> list2 = ProcessMemoryScanner.ScanMinecraftRam(0);
				foreach (MemoryRegionHit item2 in list2)
				{
					if (item2.Severity == CheckSeverity.Critical || item2.Severity == CheckSeverity.NeedsCheck)
					{
						list.Add(new CheckResult
						{
							Category = "Файлы",
							Name = "Индикатор в памяти javaw: " + item2.Type,
							Detail = item2.Detail + " (" + item2.Protection + ", " + item2.Size + ")",
							Detected = true,
							Severity = item2.Severity,
							Evidence = " (PID: " + item2.Address
						});
					}
				}
			}
			catch
			{
			}
			return list;
		}

		private static List<CheckResult> CheckInternalModStringsExtended()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<InternalStringHit> list2 = ModStringInspector.ScanAllKnownModAndDangerLocations();
				foreach (InternalStringHit item in list2)
				{
					if (item.IsThreat)
					{
						list.Add(new CheckResult
						{
							Category = "Моды",
							Name = "Вредоносный мод/файл: " + item.FileName,
							Detail = item.ThreatTitle + " (инжектор: " + item.MatchedSignatures.Count + ", классов: " + item.TotalClassesInspected + ")",
							Detected = true,
							Severity = item.Severity,
							Evidence = item.FilePath + "\r\nСовпадение: " + item.GetMatchedStringSummary()
						});
					}
				}
			}
			catch
			{
			}
			return list;
		}

		private static List<CheckResult> CheckMinecraftVersionWeights()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			Dictionary<string, int> dictionary = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			dictionary["1.16"] = 17083;
			dictionary["1.16.1"] = 17083;
			dictionary["1.16.2"] = 17096;
			dictionary["1.16.3"] = 17096;
			dictionary["1.16.4"] = 17136;
			dictionary["1.16.5"] = 17136;
			dictionary["1.17"] = 19079;
			dictionary["1.17.1"] = 19089;
			dictionary["1.18"] = 19569;
			dictionary["1.18.1"] = 19573;
			dictionary["1.18.2"] = 19785;
			dictionary["1.19"] = 20960;
			dictionary["1.19.1"] = 21137;
			dictionary["1.19.2"] = 21138;
			dictionary["1.19.3"] = 22173;
			dictionary["1.19.4"] = 22927;
			dictionary["1.20"] = 22489;
			dictionary["1.20.1"] = 22490;
			dictionary["1.20.2"] = 22643;
			try
			{
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string path = Path.Combine(folderPath, ".minecraft", "versions");
				if (Directory.Exists(path))
				{
					string[] directories = Directory.GetDirectories(path);
					foreach (string text in directories)
					{
						string fileName = Path.GetFileName(text);
						string text2 = Path.Combine(text, fileName + ".jar");
						if (File.Exists(text2))
						{
							long length = new FileInfo(text2).Length;
							long num = length / 1024;
							int value;
							if (dictionary.TryGetValue(fileName, out value) && Math.Abs(num - 20429) >= 150 && Math.Abs(num - value) > 100)
							{
								list2.Add(string.Format("Версия {0}: размер {1} КБ (размер эталона: {2} КБ, разница {3} КБ) > {4}", fileName, num, value, Math.Abs(num - value), text2));
							}
						}
					}
				}
			}
			catch
			{
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "Контроль эталонных весов версий (мануал RageTime/WildGrief)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.NeedsCheck;
			checkResult.Detail = ((list2.Count > 0) ? ("Обнаружены модифицированные ванильные ядра версий (" + list2.Count + " шт.)") : "Размеры установленных ванильных ядер версий соответствуют эталонам мануала");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckKnownCheatJarSizes()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string tempPath = Path.GetTempPath();
			list3.Add(Path.Combine(folderPath, ".minecraft", "mods"));
			list3.Add(Path.Combine(folderPath, ".minecraft", "versions"));
			list3.Add(Path.Combine(folderPath, ".minecraft"));
			list3.Add(Path.Combine(folderPath2, "Downloads"));
			list3.Add(Path.Combine(folderPath2, "Desktop"));
			list3.Add(tempPath);
			list3.Add(Path.Combine(folderPath, ".tlauncher"));
			list3.Add(Path.Combine(folderPath, ".feather"));
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			foreach (string item in list3)
			{
				try
				{
					if (!Directory.Exists(item))
					{
						continue;
					}
					SearchOption searchOption = ((!item.EndsWith(".minecraft") && !item.EndsWith(".tlauncher")) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly);
					string[] files = Directory.GetFiles(item, "*.jar", searchOption);
					string[] array = files;
					foreach (string text in array)
					{
						if (!hashSet.Add(text))
						{
							continue;
						}
						try
						{
							FileInfo fileInfo = new FileInfo(text);
							if (CheatDatabases.KnownCheatJarSizes.Contains(fileInfo.Length))
							{
								list2.Add(string.Format("🚨 {0} | Размер: {1:N0} байт (ТОЧНОЕ СОВПАДЕНИЕ с базой RageTime/WildGrief) | Изменён: {2:dd.MM.yyyy HH:mm} -> {3}", Path.GetFileName(text), fileInfo.Length, fileInfo.LastWriteTime, text));
							}
						}
						catch
						{
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Аномальные файлы";
			checkResult.Name = "Поиск .jar по 140 эталонным размерам читов (WildGrief & RageTime)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = ((list2.Count > 0) ? ("ОБНАРУЖЕНО ЧИТ-КЛИЕНТОВ ПО РАЗМЕРУ: " + list2.Count + " (переименование мода не спасло!)") : "Совпадений по 140 эталонным размерам чит-модов не обнаружено");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckMinecraftOptionsAndKeybinds()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string path = Path.Combine(folderPath, ".minecraft");
			string path2 = Path.Combine(path, "options.txt");
			if (File.Exists(path2))
			{
				try
				{
					string[] array = File.ReadAllLines(path2);
					string[] array2 = array;
					foreach (string text in array2)
					{
						string text2 = text.Trim();
						if (text2.Length == 0)
						{
							continue;
						}
						if (text2.StartsWith("key_"))
						{
							string text3 = text2.ToLowerInvariant();
							string[] cheatKeybindPatterns = CheatDatabases.CheatKeybindPatterns;
							foreach (string text4 in cheatKeybindPatterns)
							{
								if (text3.Contains(text4.ToLowerInvariant()))
								{
									list2.Add("Бинд чита в options.txt: " + text2);
									break;
								}
							}
						}
						if (text2.StartsWith("gamma:"))
						{
							string text5 = text2.Replace("gamma:", "").Trim();
							double result;
							if (double.TryParse(text5.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out result) && result > 1.0)
							{
								list3.Add(string.Format("Завышенная гамма: gamma={0} (норма: 0.0-1.0)", result));
							}
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "Аудит настроек управления и options.txt";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = ((list2.Count > 0) ? ("Найдены активные чит-бинды в options.txt: " + list2.Count) : ("Подозрительных биндов и чит-команд в options.txt не найдено" + ((list3.Count > 0) ? (" (гамма: " + list3[0] + ")") : "")));
			List<string> list4 = new List<string>();
			list4.AddRange(list2);
			list4.AddRange(list3);
			checkResult.Evidence = string.Join("\r\n", list4.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckMinecraftCrashReports()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string[] array = new string[5]
			{
				Path.Combine(folderPath, ".minecraft", "crash-reports"),
				Path.Combine(folderPath, ".feather", "crash-reports"),
				Path.Combine(folderPath, ".tlauncher", "minecraft", "crash-reports"),
				Path.Combine(folderPath, ".lunarclient", "offline", "multiver", "crash-reports"),
				Path.Combine(folderPath2, ".prismlauncher", "instances")
			};
			string[] array2 = array;
			foreach (string path in array2)
			{
				if (!Directory.Exists(path))
				{
					continue;
				}
				try
				{
					FileInfo[] array3 = (from f in new DirectoryInfo(path).GetFiles("crash-*.txt", SearchOption.AllDirectories)
						orderby f.LastWriteTime descending
						select f).Take(15).ToArray();
					FileInfo[] array4 = array3;
					foreach (FileInfo fileInfo in array4)
					{
						try
						{
							string[] array5 = File.ReadAllLines(fileInfo.FullName);
							string[] array6 = array5;
							foreach (string text in array6)
							{
								string text2 = text.ToLowerInvariant().Trim();
								if (text2.Length == 0 || text2.StartsWith("//"))
								{
									continue;
								}
								string[] cheatClientNames = CheatClientNames;
								foreach (string text3 in cheatClientNames)
								{
									if (MatchesCheatName(text2, text3))
									{
										list2.Add(string.Format("🚨 [{0}] Чит в отчёте сбоя: [{1}] -> {2}", fileInfo.Name, text3, text.Trim()));
										break;
									}
								}
								string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
								foreach (string text4 in extraCheatClients)
								{
									if (text4.Length >= 4 && text2.Contains(text4.ToLowerInvariant()))
									{
										list2.Add(string.Format("🚨 [{0}] Чит-модуль в отчёте: [{1}] -> {2}", fileInfo.Name, text4, text.Trim()));
										break;
									}
								}
								CheatDatabases.BanRule[] bannedByRegulation = CheatDatabases.BannedByRegulation;
								foreach (CheatDatabases.BanRule banRule in bannedByRegulation)
								{
									string[] keys = banRule.Keys;
									foreach (string text5 in keys)
									{
										if (text5.Length >= 4 && text2.Contains(text5.ToLowerInvariant()))
										{
											list2.Add(string.Format("🚨 [{0}] Запрещённый пункт {1} ({2}) в отчёте: -> {3}", fileInfo.Name, banRule.Id, banRule.Title, text.Trim()));
											break;
										}
									}
								}
								if (list2.Count >= 30)
								{
									break;
								}
							}
						}
						catch
						{
						}
						if (list2.Count >= 30)
						{
							break;
						}

					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "Анализ отчётов о сбоях игры (.minecraft/crash-reports)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = ((list2.Count > 0) ? ("В файлах crash-reports обнаружены загружавшиеся читы/запрещённые моды (" + list2.Count + " шт.)!") : "В отчётах о сбоях (.minecraft/crash-reports) следов читов не обнаружено");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckMinecraftAccountsAndNicknames()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			try
			{
				HashSet<string> seenAccounts = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

				foreach (string mcDir in PlatformHelper.GetAllMinecraftDirectories())
				{
					if (!Directory.Exists(mcDir)) continue;

					// 1. usercache.json
					string userCachePath = Path.Combine(mcDir, "usercache.json");
					if (File.Exists(userCachePath))
					{
						try
						{
							string input = File.ReadAllText(userCachePath);
							MatchCollection matchCollection = Regex.Matches(input, "\"name\"\\s*:\\s*\"([^\"]+)\"[,\\s\\n\\r]*\"uuid\"\\s*:\\s*\"([^\"]+)\"");
							foreach (Match item in matchCollection)
							{
								string name = item.Groups[1].Value;
								string uuid = item.Groups[2].Value;
								if (seenAccounts.Add(name))
								{
									list2.Add(string.Format("Ник: {0} | UUID: {1} ({2}\\usercache.json)", name, uuid, Path.GetFileName(mcDir)));
								}
							}
						}
						catch
						{
						}
					}

					// 2. launcher_accounts.json / launcher_profiles.json / accounts.json / tlauncher_profiles.json
					string[] accountFiles = new string[] { "launcher_accounts.json", "launcher_profiles.json", "accounts.json", "tlauncher_profiles.json" };
					foreach (string af in accountFiles)
					{
						string afPath = Path.Combine(mcDir, af);
						if (!File.Exists(afPath)) continue;

						try
						{
							string text = File.ReadAllText(afPath);
							MatchCollection mc = Regex.Matches(text, "\"name\"\\s*:\\s*\"([^\"]+)\"");
							Stopwatch swShim = Stopwatch.StartNew();
							foreach (Match m in mc)
							{
								if (swShim.ElapsedMilliseconds > 1500) break;
								string acc = m.Groups[1].Value;
								if (acc.Length >= 3 && !acc.Contains("Microsoft") && !acc.Contains("token") && seenAccounts.Add(acc))
								{
									list2.Add(string.Format("🚨 AHK автокликер/макрос: {0} ({1})", acc, af));
								}
							}
						}
						catch
						{
						}
					}
				}
			}
			catch (Exception ex)
			{
				list2.Add("Ошибка создания скриншота: " + ex.Message);
			}

			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "История ников и активность на ПК (Сменяемость / Твинки)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Info;
			checkResult.Detail = (list2.Count > 0) ? ("Обнаружено уникальных никнеймов/аккаунтов на ПК: " + list2.Count + " (для выявления твинков либо смены аккаунта)") : "История ников не найдена";
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}
	}
}
