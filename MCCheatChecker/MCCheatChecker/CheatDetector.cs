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
using Microsoft.Win32;
using System.Net;
using System.Management;

namespace MCCheatChecker
{
	public static class CheatDetector
	{
		private const int DeepScanMaxDepth = 12;

		private const int DeepScanMaxDirs = 120000;

		private const int DeepScanMaxFiles = 150000;

		private const int DeepScanMaxFileDepth = 8;

		private const int DeepScanTimeBudgetSeconds = 12;

		private static readonly string[] CheatClientNames = new string[]
		{
			"kami", "kamiblue", "pyro", "impact", "wurst", "inertia", "liquidbounce", "sigma", "neverhook", "zamorozka",
			"minced", "wexside", "expensive", "wild", "fluger", "keaz", "meteor", "blackberry", "nursultan", "avalone",
			"mercu", "niobium", "kion", "akrien", "celestial", "deadcode", "external", "norules", "rage", "bebraware",
			"rynware", "neverware", "aristois", "baritone", "simply", "skillclient", "jigsaw", "zeus", "salhack", "ares",
			"wolfram", "metro", "wwe", "weepcraft", "kalkon", "advancedx", "matix", "nodus", "aurus", "neverclide",
			"wave", "pivo", "prerik", "squad", "cabaletta", "gishcode", "rename_me_please", "future", "rusherhack", "eternity",
			"richpremium", "editme", "mc100", "r3d", "destroy", "archware", "boberware", "bolshoy", "nurik", "celka",
			"stardust", "akr", "wex", "haven", "ponos", "emortalityclient", "dreampool", "invisible", "winner", "clowdy",
			"rasty", "dreampoolhack", "diamondsim", "potions", "autoattack", "kyprak", "infinity", "wintware", "konas", "jessica",
			"excellent", "vape", "bushroot", "panickattack", "hackbrand", "undetectable", "venus", "interium", "energyclient", "cortex",
			"cclient", "quickclient", "chclient", "caesar", "tofficlient", "decision", "bleach", "hider", "troxill", "recode",
			"exosware", "jelly", "viamcp", "fatal", "waterclient", "extendopvp", "hwid", "hitware", "harambaritone", "flauncher",
			"clownclient", "euphoriaclient", "vertclient", "wortexclient", "404client", "aoba", "darkproject", "mioclient", "vegaline", "wissend",
			"darklight", "catalyst", "jexclient", "fdp", "paragon", "trollhack", "catlavan", "haru", "verist", "lunaclient",
			"exloader", "evaware", "britva", "haruka", "ghost", "rise", "drip", "remix", "flex", "rogue",
			"dummie", "dummi", "dummyclient", "entropy", "hypnotic", "neura", "novoline", "huzuni", "orion", "eclipse",
			"stronk", "raven", "ambrosial", "tenacity", "reflex", "pulse", "killerware", "sora", "flux", "prism",
			"novaclient", "iceclient", "aqua", "ruin", "atom", "haste", "spark", "crystal", "ozone", "phantom",
			"specter", "spectrum", "nebula", "epoch", "axiom", "abyss", "nexus", "solstice", "titan", "cipher",
			"banjo", "nightfall", "dusk", "omega", "flarial", "acryl", "seraph", "zues", "predator", "kage",
			"nova2", "nova3", "sable", "vertex", "hyphen", "altdentifier", "blatware", "blat", "chipware", "vorona",
			"rusohack", "vortex", "amethyst", "garnet", "atomware", "quantumware", "neonware", "pixelware", "slavehack", "slavware",
			"babahack", "kolyan", "petyan", "vanyan", "sashka", "dimka", "misha", "chernila", "krasniy", "bychara",
			"boroda", "strafe", "scaffold", "velocity", "antikb", "aimassist", "triggerbot", "bhop", "fastplace", "fastbreak",
			"nuker", "criticals", "killauraplus", "multiaura", "tpaura", "antibot", "antiafk", "autofish", "autosprint", "highjump",
			"longjump", "boatfly", "spoofed", "bypass", "silentaura", "silentswing", "silentmine", "skidded", "cracked", "nulled",
			"leaked", "obfuscated", "liquidloader", "ambientloader", "hackloader", "bootject", "xenos", "coremod", "coremods", "javaagent-cheat",
			"patcher-cheat", "asmhook", "hax", "hacks", "mchack", "mccheat", "hackedclient", "cheatclient", "hackforge", "hackedforge",
			"mixinhack", "mixin-hack", "bytebuddy-hack", "nodus2", "lolclient", "mc-hack", "horion", "zephyr", "horionupdated", "oseris",
			"solaris", "onix", "nohopefully", "dragonfly", "bedrockhax", "bedrockcheat", "polar", "aurora", "astral", "cosmic",
			"stellar", "nebulaclient", "zenith", "horizon", "pinnacle", "summit", "victory", "dominion", "conquer", "legacy",
			"dynasty", "empire", "reign", "sovereign", "monarch", "prism2", "aether", "infinity2", "infinityclient2", "flaxenter",
			"flax", "flaxclient", "flaxus", "scope", "krystal", "krystall", "krystalclient", "dioxin", "dioxinclient", "diox",
			"lunarclient2", "lunar2", "lunarbypass", "maraxsis", "sunlitclient", "deceptiveclient", "permetricsclient", "epsilonclient", "epsilonhack", "epicbase",
			"fpsclients", "chemosai", "fumoclient", "brainwaveclient", "asteriaclient", "noodleclient", "noodleware", "koboldclient", "noirclient", "obsidianclient",
			"anubisclient", "yunoclient", "chaosclient", "cirrusclient", "voidclient", "radiumclient", "mochat", "deltacclient", "ccbclient", "farca",
			"seleneclient", "nyxclient", "silentclient", "valhallacheat", "asgardclient", "ragnarokclient", "fenrirclient", "skadiclient", "jormungandrclient", "medusaclient",
			"cerberusclient", "chimeraclient", "krakenclient", "leviathanclient", "basiliskclient", "golemclient", "ifritclient", "tenguclient", "raijinclient", "susanooclient",
			"osirisclient", "horusclient", "thothclient", "atumclient", "anhurclient", "montucclient", "kamosclient", "maraxsisclient", "thunderhack", "thunderhackrecode", "boze", "bozeclient", "slinky", "slinkyclient", "dreamclient", "doomsday", "doomsdayclient", "ravenb4", "ravenbplus", "expensive3", "expensive2", "expensive-recode", "nursultanrecode", "nursultan-next", "celestialrecode", "celestial-premium", "wildrecode", "wildclient", "nightx", "nightxclient", "vulcanclicker", "seedcracker", "seedcrackerx"
		};

		private static readonly string[] VisualKeywords = new string[36]
		{
			"x-ray", "xray", "xrayultimate", "x-rayultimate", "advancedxray", "advanced-xray", "chestesp", "storageesp", "playeresp", "esp",
			"nametags", "tracers", "freecam", "norender", "fullbright", "gamma", "blockesp", "itemesp", "mobesp", "wireframe",
			"chams", "radar", "trajectories", "wallhack", "glow", "search", "brightnight", "fullbright-", "gammabright", "orebfuscator-bypass",
			"cavefinder", "entityoutlines", "chunkborders", "tracer", "blockoutline", "oreesp"
		};

		private static readonly string[] InjectorNames = new string[9] { "exremeinjector", "injector", "liquidloader", "ambientinjector", "bootject", "xenos", "exloader", "processhacker", "cheatengine" };

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

		private static readonly string[] DeepScanStrongNames = new string[129]
		{
			"kamiblue", "pyro", "wurst", "liquidbounce", "neverhook", "zamorozka", "wexside", "fluger", "keaz", "meteor",
			"nursultan", "avalone", "niobium", "akrien", "celestial", "deadcode", "norules", "bebraware", "rynware", "neverware",
			"aristois", "baritone", "skillclient", "jigsaw", "salhack", "wolfram", "weepcraft", "kalkon", "advancedx", "matix",
			"nodus", "aurus", "neverclide", "cabaletta", "gishcode", "rusherhack", "eternity", "richpremium", "archware", "boberware",
			"bolshoy", "nurik", "celka", "stardust", "emortalityclient", "dreampool", "dreampoolhack", "diamondsim", "kyprak", "wintware",
			"konas", "vape", "bushroot", "panickattack", "hackbrand", "interium", "energyclient", "quickclient", "chclient", "tofficlient",
			"troxill", "exosware", "viamcp", "waterclient", "extendopvp", "hitware", "harambaritone", "flauncher", "clownclient", "euphoriaclient",
			"vertclient", "wortexclient", "aoba", "darkproject", "mioclient", "vegaline", "wissend", "darklight", "jexclient", "paragon",
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

		private static bool MatchesCheatName(string textLower, string cheatLower)
		{
			if (string.IsNullOrEmpty(textLower) || string.IsNullOrEmpty(cheatLower))
			{
				return false;
			}
			if (IsLegitGame(textLower))
			{
				return false;
			}
			if (CheatDatabases.IsWhitelistedPath(textLower))
			{
				return false;
			}
			bool flag = CheatDatabases.IsAmbiguous(cheatLower);
			if (flag && !CheatDatabases.HasCheatContext(textLower))
			{
				return false;
			}
			if (!flag && cheatLower.Length >= 7)
			{
				return textLower.Contains(cheatLower);
			}
			return ContainsToken(textLower, cheatLower);
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
			Tuple<string, Func<List<CheckResult>>>[] array = new Tuple<string, Func<List<CheckResult>>>[1077]
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
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcca База учета использования ресурсов SRUM (SRUDB.dat)...", CheckSRUMDatabase),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Детектор виртуальных машин и песочниц (Anti-VM / Sandboxie)...", CheckVirtualMachineAndSandbox),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce5 Загрузки файлов из Telegram Desktop и браузеров...", CheckTelegramAndDirectDownloads),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccb Регламент проверки: Проверка SysMain и времени запуска Minecraft...", CheckRegulationSysMainAndUptime),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfaf Регламент проверки: Сигнатуры KillAura, AutoTotem, HitBoxes в памяти...", CheckRegulationMemorySignatures),
				Tuple.Create<string, Func<List<CheckResult>>>("⏳ Регламент проверки: Сроки давности Prefetch (правило 7-14 дней)...", CheckRegulationPrefetchStorageRules),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc89 Регламент проверки: агент через переменные Java (_JAVA_OPTIONS / JAVA_TOOL_OPTIONS)...", CheckRegulationJavaAgentEnv),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc2\ufe0f Регламент проверки: состояние USN Journal на C: (затирание следов)...", CheckRegulationUsnJournal),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddec Регламент проверки: целостность версий (java-агент внутри jar)...", CheckRegulationCoreJarIntegrity),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd70\ufe0f Регламент проверки: системное время и служба W32Time (откат часов)...", CheckRegulationClockTamper),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcb3 Подписки платных чит-клиентов (лицензии, токены, конфиги, browser auth)...", CheckPaidClientSubscriptions),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcb3 Расширенная проверка купленных читов (HWID-файлы, auth-токены, лоадеры)...", CheckPurchasedCheatEvidence),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Аппаратная часть: CPU, RAM, GPU, BIOS на спуфинг...", HardwareChecks.RunHardwareChecks),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddec HWID-спуферы, подмена MAC и виртуальные машины...", HardwareChecks.RunHwidChecks),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1\ufe0f HID-устройства: макрос-кликеры на микроконтроллерах (KMBox/Arduino)...", HardwareChecks.RunHidChecks),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddcd Проверка присутствия человека (ввод, AnyDesk, джиглеры, uptime)...", HumanPresenceCheck.RunHumanPresenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc2\ufe0f ShellBag: посещённые и удалённые папки (методичка SS)...", ManualChecks.RunShellBagCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Everything: настройки индексации и история поиска...", ManualChecks.RunEverythingConfigCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Обязательные службы Windows (PcaSvc/DPS/BAM/CDPSvc/DiagTrack)...", ManualChecks.RunRequiredServicesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Prefetch: cmd / conhost / consent / javaw...", ManualChecks.RunPrefetchProcessCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc41\ufe0f X-Ray: прозрачные текстуры и структура Advanced Xray...", ManualChecks.RunXrayStructureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddfe RunMRU и автозапуск cmd (Command Processor Autorun)...", ManualChecks.RunMruAndAutorunCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddd1\ufe0f Корзина $Recycle.Bin на всех дисках (свежие изменения)...", ManualChecks.RunRecycleBinAgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf9 Утилиты очистки следов и криминалистики на ПК...", ManualChecks.RunTraceToolsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc4\ufe0f Очистка USN Journal (EventID 3079 / FSUTIL.EXE)...", ManualChecks.RunUsnJournalCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Чекер запущенных программ, окон и RAM (ProcessMemoryScanner)...", CheckProcessMemoryExtended),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Чекер внутренних строк модов и Constant Pool (ModStringInspector)...", CheckInternalModStringsExtended),
				Tuple.Create<string, Func<List<CheckResult>>>("⚖\ufe0f Контроль эталонных весов версий (мануал RageTime/WildGrief)...", CheckMinecraftVersionWeights),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcca Инвентарь программ Java (Program-Inventory EventLog)...", ManualChecks.RunProgramInventoryEventCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfaf Сигнатуры 140 эталонных размеров чит-.jar (RageTime/WildGrief)...", CheckKnownCheatJarSizes),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Аудит биндов и настроек управления (options.txt)...", CheckMinecraftOptionsAndKeybinds),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc PowerShell Operational скрипты (EventID 4104 / методичка SS)...", ManualChecks.RunPowerShellEventLogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Сбои и ошибки Java/DLL (EventID 1000/1001 Application Log)...", ManualChecks.RunJavaCrashEventCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd25 Сетевые правила брандмауэра для Java и чит-клиентов (Шаг 02)...", ManualChecks.RunFirewallAppHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\ude80 Анализ лаунчеров и профилей запусков (Шаг 01 & 02 мануалов)...", ManualChecks.RunLauncherProfilesDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Отчёты о сбоях игры на загружавшиеся читы (.minecraft/crash-reports)...", CheckMinecraftCrashReports),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc64 История ников и аккаунтов игрока (usercache.json / мультиаккаунты)...", CheckMinecraftAccountsAndNicknames),
				Tuple.Create<string, Func<List<CheckResult>>>("⏱\ufe0f Windows Timeline: активность запуска ПО (ActivitiesCache.db)...", ManualChecks.RunActivitiesCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccc Списки переходов JumpLists (Automatic & Custom Destinations)...", ManualChecks.RunJumpListsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc2 Диалоги выбора и сохранения файлов (OpenSavePidlMRU / LastVisited)...", ManualChecks.RunComDlg32MRUCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Архиваторы: история распаковки софта (WinRAR ArcHistory / 7-Zip)...", ManualChecks.RunArchiverHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfa8 Анализ шейдеров и ресурспаков (X-Ray / маскировка софта)...", ManualChecks.RunShaderpacksAndResourcepacksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1\ufe0f Драйверы перехвата ввода (Interception / vJoy / DoubleClick)...", ManualChecks.RunMouseDebounceAndDriverCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Поисковые запросы в Проводнике Windows (WordWheelQuery)...", ManualChecks.RunWordWheelQueryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Защитник Windows: блокировки читов и исключения (EventID 1116/5007)...", ManualChecks.RunWindowsDefenderEventCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccb Буфер обмена и фоновые загрузки BITS (Clipboard & Bits-Client)...", ManualChecks.RunClipboardAndBitsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udded Адресная строка Explorer (TypedPaths), OpenWith и AppCompatFlags...", ManualChecks.RunExplorerTypedPathsAndFileExtsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Установка служб/драйверов (Event 7045) и задачи Планировщика...", ManualChecks.RunTaskSchedulerAndServicesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc89 Графические Proxy DLL (OpenGL / DirectX хуки в .minecraft)...", ManualChecks.RunGraphicsHooksAndLocalProxyDllCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 NTFS Zone.Identifier: оригинальные ссылки скачивания (HostUrl)...", ManualChecks.RunZoneIdentifierForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfad Сигнатуры заголовков файлов (Magic Bytes PK/MZ маскировка)...", ManualChecks.RunDisguisedFileHeadersCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Архив отчётов об ошибках Windows Error Reporting (ReportArchive)...", ManualChecks.RunWerCrashForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd12 Системные мьютексы и именованные каналы (Mutex & Named Pipes)...", ManualChecks.RunNamedObjectsAndPipesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏱\ufe0f Аудит манипуляций со временем файлов (Timestomping в mods)...", ManualChecks.RunTimestompForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udd16 Скомпилированные AutoHotkey макросы и кликеры (Ahk2Exe)...", ManualChecks.RunCompiledAhkAndMacroProcessesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Процессы Java HotSpot и JVM Attach (hsperfdata)...", ManualChecks.RunJvmPerfDataAndAttachCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит списка серверов Minecraft (servers.dat / HvH & Anarchy)...", ManualChecks.RunMinecraftServersDatCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc65 Альт-генераторы и подмена аккаунтов (TheAltening, MCLeaks, EasyMC)...", ManualChecks.RunAltGeneratorAndBanEvasionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc89 Нативные библиотеки перехвата и хуки в Temp (JNA / JNativeHook)...", ManualChecks.RunJnaAndNativeDllHooksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0c Аудит съёмных USB-носителей (MountedDevices USBSTOR & флешки)...", ManualChecks.RunUsbStorageAndDriveCorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Аудит версий и JVM (Custom MainClass, -noverify, -javaagent)...", ManualChecks.RunMinecraftVersionsAndJvmDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Глубокий аудит команд PowerShell (зачистки следов, откат дат)...", ManualChecks.RunPowerShellDeepHistoryAudit),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Мульти-лаунчерный аудит options.txt (FullBright гамма и бинды)...", ManualChecks.RunMultiLauncherOptionsAndGammaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc27 Криминалистика Unix / macOS / Linux (Preload, History, Clickers)...", ManualChecks.RunUnixSpecificForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc41\ufe0f Сокрытие экрана и Overlay (SetWindowDisplayAffinity / Discord / OBS Bypass)...", ManualChecks.RunScreenCaptureEvasionAndOverlayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbe Теневые копии томов VSS и политики восстановления (vssadmin delete)...", ManualChecks.RunVolumeShadowCopiesAndRecoveryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc История команд Minecraft (command_history.txt / чат / Baritone)...", ManualChecks.RunMinecraftCommandHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Стеганография и скрытые хвосты в модах (ZIP EOCD Trailing Data)...", ManualChecks.RunZipEocdTrailingPayloadCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0c Точная хронология подключений USB (setupapi.dev.log таймлайны)...", ManualChecks.RunSetupApiUsbTimelineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Аварийные логи JVM HotSpot (hs_err_pid*.log / модули сбоя)...", ManualChecks.RunJvmHotspotCrashLogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfa8 Аудит шейдеров на скрытый X-Ray (GLSL discard / прозрачность)...", ManualChecks.RunDeepShaderpackXrayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1\ufe0f Макросы мыши и дебаунс (Glorious, Bloody, Razer, G HUB)...", ManualChecks.RunMouseMacroAndDebounceForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Конфиги читов и Litematica/Baritone (easyPlace/legitMine)...", ManualChecks.RunModAndClientConfigAudit),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf9 Аудит очистки журналов аудита (EventID 104 / 1102 / wevtutil)...", ManualChecks.RunAuditLogTamperingAndClearingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит истории браузеров и загрузок (Chrome, Edge, Firefox, Yandex)...", ManualChecks.RunBrowserHistoryAndDownloadCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc4\ufe0f Аудит кэша выполнения AppCompatCache (ShimCache) и BAM...", ManualChecks.RunAppCompatCacheAndBamCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd75\ufe0f Аудит реестра UserAssist (ROT13 история запуска программ)...", ManualChecks.RunUserAssistForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccd Аудит меток и вейпоинтов (Xaero, JourneyMap, Baritone X-Ray)...", ManualChecks.RunMinimapAndWaypointExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит файла hosts (редиректы античитов и авторизации Mojang)...", ManualChecks.RunHostsFileAndDnsRedirectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Аудит отчётов о сбоях клиента (crash-reports/*.txt / моды и миксины)...", ManualChecks.RunMinecraftCrashReportsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddd1\ufe0f Детальный аудит индексов корзины $Recycle.Bin ($I метаданные)...", ManualChecks.RunRecycleBinIndexDetailsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbe Анализ дампов памяти сбоев CrashDumps (javaw.exe.*.dmp / DLL)...", ManualChecks.RunJvmCrashDumpsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит сетевых сокетов Java (Local Proxy 127.0.0.1 / WebSocket)...", ManualChecks.RunActiveJavaNetworkConnectionsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\ude9d Поиск вебхуков Discord и скрытой эксфильтрации данных (RAT)...", ManualChecks.RunDiscordWebhookAndRatDetectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf8 Анализ скриншотов Minecraft и таймлайна (screenshots/*.png)...", ManualChecks.RunMinecraftScreenshotsAnalysisCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd13 Деобфускация и анализ Base64 в PowerShell (скрытые пейлоады)...", ManualChecks.RunPowerShellBase64PayloadDecoderCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Аудит уязвимых ядерных драйверов (BYOVD / DBK64 / KProcessHacker)...", ManualChecks.RunKernelVulnerableDriverCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddec Аудит дампов Mixin и артефактов байткода (.mixin.out / логи)...", ManualChecks.RunMixinDebugAndBytecodeArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uded1 Аудит изменения и остановки служб (EventID 7040 / 7036 / PcaSvc)...", ManualChecks.RunServiceTamperingEventAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc2 Аудит диалогов выбора файлов LastVisitedPidlMRU (инжекторы)...", ManualChecks.RunLastVisitedPidlMruCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Аудит скрытых альтернативных потоков NTFS (ADS стеганография)...", ManualChecks.RunNtfsHiddenAlternateStreamsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Аудит кэша интерфейса программ MuiCache (удалённые читы)...", ManualChecks.RunMuiCacheExecutionHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcca Аудит активности программ FeatureUsage (AppSwitched / AppLaunch)...", ManualChecks.RunFeatureUsageAppSwitchedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udeaa Аудит событий заморозки и аварийных выходов (latest.log)...", ManualChecks.RunMinecraftLogDisconnectionAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc2 Диалоги выбора файлов OpenSavePidlMRU (инжекторы и DLL)...", ManualChecks.RunOpenSavePidlMruCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Аудит архивов сессий игры (logs/*.log.gz)...", ManualChecks.RunMinecraftHistoricalGzLogsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Целостность JVM и политик безопасности (java.policy / ext)...", ManualChecks.RunJvmSecurityPolicyAndEndorsedAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏱\ufe0f Аудит планировщика задач и автозагрузки (schtasks / Run)...", ManualChecks.RunScheduledTasksAndStartupPersistenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce1 Аудит системного кэша DNS (ipconfig /displaydns)...", ManualChecks.RunDnsClientCacheForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc89 Инжекции AppInit_DLLs и оверлейные слои Vulkan...", ManualChecks.RunAppInitDllsAndInjectionPersistenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1\ufe0f Фильтры драйверов мыши и перехват Interception...", ManualChecks.RunMouseUpperFiltersAndInterceptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Глубокий анализ сбоев Java в WER (Report.wer FaultModule)...", ManualChecks.RunWerReportJavawCrashAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Аудит аварийных выключений и BSOD (EventID 41 / 6008)...", ManualChecks.RunSystemCrashAndRebootEvasionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf9 Аудит службы автоочистки Storage Sense (Контроль памяти)...", ManualChecks.RunStorageSenseAndAutoCleanAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc27 Аудит контейнеров WSL и процессов гипервизоров...", ManualChecks.RunWslAndHypervisorForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccb Аудит журнала буфера обмена и перехвата (Win+V)...", ManualChecks.RunClipboardHistoryAndConfigForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит системных прокси и WinHTTP (перехватчики трафика)...", ManualChecks.RunSystemProxyAndNetworkTunnelCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbe Аудит истории томов и отключённых USB (MountedDevices)...", ManualChecks.RunMountedDevicesAndVirtualEncryptedDrivesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит службы телеметрии и твикеров (DiagTrack / Debloaters)...", ManualChecks.RunDiagTrackTelemetryAndPrivacyCleanerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Аудит сторонних ярлыков Steam (shortcuts.vdf)...", ManualChecks.RunSteamNonSteamShortcutsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Кэши сборки и зависимостей модов/читов (.gradle / .m2)...", ManualChecks.RunGradleAndMavenBuildArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcac Локальная база данных Discord LevelDB (вложения и чаты)...", ManualChecks.RunDiscordLevelDbAndCacheForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce1 Активные порты прослушивания TCP (netstat Listening)...", ManualChecks.RunActiveTcpListeningEndpointsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbb История команд терминалов и проекты VS Code...", ManualChecks.RunDeveloperCliHistoryAndVscodeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("✈\ufe0f Аудит сессий и загрузок Telegram (tdata / Downloads)...", ManualChecks.RunTelegramTdataAndDownloadsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит детектов и отключений Defender (Events 1116/5001)...", ManualChecks.RunDefenderThreatDetectionsAndRealtimeTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Аудит блоков кода PowerShell (ScriptBlock EventID 4104)...", ManualChecks.RunPowerShellScriptBlock4104AuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Аудит биндов и клавиш чит-модов (options.txt)...", ManualChecks.RunMinecraftKeybindingsAndControlsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc64 Аудит аккаунтов лаунчеров и альт-токенов (TheAltening/EasyMC)...", ManualChecks.RunLauncherAccountsAndSessionTokenCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Анализ таблицы ARP и локальных шлюзов (arp -a / дубликаты MAC)...", ManualChecks.RunArpCacheAndLocalGatewayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce5 Аудит заданий BITS и скрытых загрузок (EventLog Bits-Client)...", ManualChecks.RunBitsTransferAndDeliveryOptimizationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc2\ufe0f Парсер бинарных записей RecentDocs (.jar/.exe/.dll/.zip)...", ManualChecks.RunRecentDocsBinarySubkeysParserCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc27 Глубокий аудит подсистемы Linux (WSL rootfs / bash history)...", ManualChecks.RunWslDeepFilesystemAndHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd14 Форензика центра уведомлений Windows (wpndatabase.db / Toast)...", ManualChecks.RunWindowsNotificationCenterForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Анализ аварийных дампов памяти (%LOCALAPPDATA%\\CrashDumps)...", ManualChecks.RunCrashDumpsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⛏\ufe0f Аудит статистики игрока и коэффициента добычи X-Ray (stats)...", ManualChecks.RunMinecraftPlayerStatsAndMiningRatioCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc89 Форензика Process Hacker, Sysinternals и Cheat Engine...", ManualChecks.RunProcessHackerAndSysinternalsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Аудит шрифтов ClickGUI и распакованных шрифтов в Temp...", ManualChecks.RunFontCacheAndClickGuiFontsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd12 Аудит корневых сертификатов Root CA (mitmproxy/Fiddler)...", ManualChecks.RunRootCertificatesAndProxyCAsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Глубокий аудит аварийных логов Hotspot (hs_err_pid*.log)...", ManualChecks.RunJvmHotspotHsErrCrashLogsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfa8 Форензика кэша графических шейдеров GPU (DXCache/GLCache)...", ManualChecks.RunGraphicsShaderCacheForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Аудит журнала движка PowerShell (Event ID 400 и 800)...", ManualChecks.RunClassicPowerShellEngineEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfaf Аудит оверлейных хуков и внедрения через Discord/Steam...", ManualChecks.RunGameOverlayAndDiscordHookInjectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbe Корреляция точек монтирования пользователя (MountPoints2)...", ManualChecks.RunMountPoints2UserVolumesCorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☀\ufe0f Анализ FullBright / Gamma Exploit и X-Ray ресурспаков...", ManualChecks.RunMinecraftGammaAndVisualSettingsExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc2 Проверка перенаправления системных каталогов (User Shell)...", ManualChecks.RunUserShellFoldersRedirectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1\ufe0f Аудит аппаратных макросных мышей и USB-кликеров (HID)...", ManualChecks.RunHidMacroPeripheralsAndDonglesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf9 Аудит очистки журналов событий (Event ID 1102 и 104)...", ManualChecks.RunEventLogServiceAndWevtutilAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccb Аудит истории открытия файлов в FileExts (OpenWithList)...", ManualChecks.RunFileExtsOpenWithHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Глубокий аудит ресурспаков на скрытый X-Ray (resourcepacks)...", ManualChecks.RunMinecraftResourcePacksDeepAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\ude9f Аудит активных окон и истории панели задач (Taskband)...", ManualChecks.RunActiveWindowHistoryAndForegroundForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\ude9a Форензика службы оптимизации доставки (Delivery Optimization)...", ManualChecks.RunWindowsDeliveryOptimizationAndBitsCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf0c Анализ шейдеров на встроенный GLSL X-Ray и Chams...", ManualChecks.RunMinecraftShaderpacksDeepCodeInspectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Аудит сетевых драйверов манипуляции пакетами (WinDivert/Clumsy)...", ManualChecks.RunWinDivertAndPacketCaptureDriversCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Глубокий аудит структуры модов (fabric.mod.json / маскировка)...", ManualChecks.RunMinecraftModInternalMetadataDisguiseCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdd Анализ журнала сессии latest.log (ошибки Mixin и чит-команды)...", ManualChecks.RunMinecraftLatestLogChatAndErrorsDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Аудит графического сервера WSLg (msrdc.exe / Linux GUI читы)...", ManualChecks.RunWslgDisplayServerAndRdpSocketCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbe Парсер постоянных очередей BITS (qmgr*.dat)...", ManualChecks.RunBitsPersistentJobFilesDeepParserCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf0d Аудит сохранений level.dat на GodMode и FlyHack (NBT)...", ManualChecks.RunMinecraftLevelDatNbtCapabilitiesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddbc\ufe0f Форензика IconCache.db и кэша миниатюр проводника...", ManualChecks.RunIconAndThumbnailCacheForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔\ufe0f Аудит переназначения атаки на клавиатурные кликер-макросы...", ManualChecks.RunMinecraftPvpMacroKeybindAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcec Аудит очереди сбоев ReportQueue (неотправленные крэши)...", ManualChecks.RunWerReportQueueCrashArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd10 Аудит TLS/Schannel и отключения проверки сертификатов...", ManualChecks.RunSchannelAndCertificateRevocationTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbe Анализ журнала изменений NTFS USN и проверка на скрытое удаление...", ManualChecks.RunNtfsVolumeJournalUSNAnalyticsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Аудит графических слоев Vulkan ImplicitLayers и инжектов DirectX...", ManualChecks.RunVulkanAndDirectXLayerInjectionAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Криминалистика кэша Favicons и остаточных URL браузеров...", ManualChecks.RunBrowserCacheFaviconAndHistoryAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Глубокий Regex-анализ истории консоли PowerShell (ConsoleHost_history.txt)...", ManualChecks.RunPowerShellConsoleHostHistoryRawRegexCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce1 Парсинг сохраненных серверов Minecraft (servers.dat) на скрытые прокси...", ManualChecks.RunMinecraftSavedServersPingAndMotdAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc5 Аудит планировщика заданий Windows (schtasks)...", ManualChecks.RunScheduledTasksXmlActionsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd35 Аудит сопряженных Bluetooth устройств и кликер-педалей...", ManualChecks.RunBluetoothPairedDevicesAndControllersAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит точек восстановления и теневых копий VSS...", ManualChecks.RunSystemRestorePointsAndVssShadowCopiesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0a Аудит аудиоустройств, виртуальных кабелей и Soundpad...", ManualChecks.RunDirectXDirectSoundAndAudioDeviceAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Глубокий аудит целостности библиотек Fabric Loader и Mixin...", ManualChecks.RunMinecraftFabricQuiltFabricApiAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Аудит WMI-персистентности (CommandLineEventConsumer)...", ManualChecks.RunWmiPersistenceAndEventConsumersAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf1 Аудит Android-эмуляторов (WSA, LDPlayer, BlueStacks)...", ManualChecks.RunWindowsSubsystemForAndroidAndVirtualizationArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf8 Криминалистика скриншотов Minecraft (F2)...", ManualChecks.RunMinecraftScreenshotsExifAndOcrCluesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит системных прокси WinINet, PAC-скриптов...", ManualChecks.RunNetworkTlsSslSessionCacheAndHttpProxyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udeb0 Аудит именованных каналов Named Pipes (\\\\.\\pipe\\)...", ManualChecks.RunNamedPipesAndIpcEndpointsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит исключений и политик Windows Defender...", ManualChecks.RunDefenderMpPreferenceExclusionsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddbc\ufe0f Аудит настроек Sodium / Iris и шейдерпаков на X-Ray...", ManualChecks.RunMinecraftSodiumIrisShaderSettingsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Криминалистика поисков WordWheelQuery и TypedPaths...", ManualChecks.RunFileExplorerTypedPathsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0c Аудит активных сетевых TCP-сокетов javaw.exe...", ManualChecks.RunJavawNetworkConnectionsDeepAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Глубокий аудит вложенных Jar-in-Jar зависимостей модов...", ManualChecks.RunMinecraftFabricModsDependencyTreeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⌨\ufe0f Аудит модов и конфигов KeyStrokes, CPS Mod и ToggleSneak...", ManualChecks.RunMinecraftPlayerKeyStrokesCPSAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee0\ufe0f Аудит служб DPS, DiagTrack и PcaSvc на отключение...", ManualChecks.RunDiagnosticPolicyServiceDpsAntiForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcac Аудит истории команд чата Minecraft (command_history.txt)...", ManualChecks.RunMinecraftChatMacrosAndCommandHistoryAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd12 Аудит защиты LSA (RunAsPPL) и Memory Management...", ManualChecks.RunLocalSecurityAuthorityLsaProtectionAndPplCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfa8 Криминалистика кэша шейдеров (NVIDIA/AMD DXCache)...", ManualChecks.RunDirectXShaderDecompilerCacheForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Аудит истории запуска AppCompatFlags (Store/Persisted)...", ManualChecks.RunRegistryAppCompatUserFlagsExecutionHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccd Аудит меток и баз миникарт (Xaero, JourneyMap, FTB)...", ManualChecks.RunMinecraftWaypointsAndMinimapModConfigsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Аудит шрифтов ClickGUI чит-клиентов и дропперов...", ManualChecks.RunWindowsFontCacheServiceAndWpfFontArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Аудит ресурспаков на X-Ray, Low-Fire и скрытые DLL/JAR...", ManualChecks.RunMinecraftResourcePackCustomFontAndDisguiseAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔\ufe0f Анализ сохранённых хотбаров (hotbar.nbt) на 32k оружие...", ManualChecks.RunMinecraftPlayerHotbarItemNbtAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc8e Аудит PBR, Normal-карт и светящихся руд (Luminescent X-Ray)...", ManualChecks.RunMinecraftCustomResourcePackPBRAndSpecularMapsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Криминалистика кэша сертификатов CryptnetUrlCache (SSL CRL/OCSP)...", ManualChecks.RunWindowsCryptnetUrlCacheCertificatesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит файла etc/hosts и подмены серверов авторизации Mojang...", ManualChecks.RunWindowsSystem32EtcHostsHijackAndDnsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Аудит конфигураций модов на Freecam, Tweakeroo и чит-опции...", ManualChecks.RunMinecraftModConfigurationsAndFreecamConfigsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcca Аудит истории запусков FeatureUsage (AppSwitched/AppLaunch)...", ManualChecks.RunWindowsFeatureUsageAppSwitchedAndLaunchForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfc3 Аудит статистики игрока (stats/*.json - Fly/NoFall/Jesus)...", ManualChecks.RunMinecraftPlayerMovementAndStatisticsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc1 Криминалистика ShellBags (история открытия папок читов)...", ManualChecks.RunWindowsShellBagsFolderAccessForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0c Аудит аппаратных макросов (Arduino, Pico, макро-педали)...", ManualChecks.RunHardwareUsbMacroDevicesAndFootPedalsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Криминалистика кэша шрифтов Java AWT (~JF*.tmp)...", ManualChecks.RunJavaRuntimeFontCacheAndAwtArtifactsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Аудит инжекций Minecraft Bedrock (UWP, Horion, Prax)...", ManualChecks.RunMinecraftBedrockUwpInjectedDllsAndAddonsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd14 Криминалистика базы уведомлений Windows (wpndatabase.db)...", ManualChecks.RunWindowsPushNotificationsDatabaseForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Криминалистика аварийных дампов памяти (CrashDumps)...", ManualChecks.RunWindowsCrashDumpsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☀\ufe0f Аудит options.txt на Gamma Fullbright и FOV эксплойты...", ManualChecks.RunMinecraftPlayerOptionsTxtAdvancedFovGammaAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Аудит устройств DirectInput и виртуальных контроллеров...", ManualChecks.RunWindowsDirectInputAndXInputDeviceHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddbc\ufe0f Аудит папки скриншотов на фальшивые PNG и скрытые JAR...", ManualChecks.RunMinecraftScreenshotsFolderHiddenFilesAndAdsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd2e Аудит шейдерпаков на Wallhack и GLSL Depth Hack...", ManualChecks.RunMinecraftSodiumIrisShaderPacksDepthCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd17 Аудит зарегистрированных URL-протоколов и перехватов .JAR...", ManualChecks.RunWindowsDefaultProgramAssociationsAndUrlProtocolsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Аудит логов игры на аномалии сессий и кики античита...", ManualChecks.RunMinecraftPlayerLogsSessionDurationAnomaliesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Аудит механизма автозапуска Active Setup (Installed Components)...", ManualChecks.RunWindowsActiveSetupPersistenceAndStubPathsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Аудит репозитория UWP AppX пакетов и sideloaded сборок...", ManualChecks.RunWindowsPackageRepositoryAppxSideloadedModsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc64 Аудит playerdata/*.dat на способности игрока (Fly, GodMode)...", ManualChecks.RunMinecraftPlayerWorldDatNbtPlayerAbilitiesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc2\ufe0f Криминалистика диалогов выбора файлов (OpenSavePidlMRU)...", ManualChecks.RunWindowsStorageAccessFrameworkAndOpenSavePidlCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Криминалистика кэша шрифтов DirectWrite (FNTCACHE.DAT)...", ManualChecks.RunWindowsFontCacheServiceFntcacheDatDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcac Аудит командных префиксов и логов чата (.vape, .bind)...", ManualChecks.RunMinecraftPlayerChatFilterAndRegexAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит журнала службы теневого копирования тома VSS...", ManualChecks.RunWindowsVolumeShadowCopiesAndVssWritersStatusCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca1 Аудит настроек шейдерпаков на Fullbright и твики...", ManualChecks.RunMinecraftSodiumIrisShaderPackBloomAndLightingTweaksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Аудит режимов совместимости и прав Admin (AppCompat Layers)...", ManualChecks.RunWindowsAppCompatFlagsLayersAndExecutionModesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Аудит эндер-сундука (EnderItems) на 32k оружие и краш-блоки...", ManualChecks.RunMinecraftPlayerWorldDatEnderChestInventoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcca Глубокий аудит UserAssist со счетчиками запусков и фокуса...", ManualChecks.RunWindowsUserAssistDetailedFocusTimeAndExecutionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⌨\ufe0f Криминалистика истории диалога «Выполнить» (RunMRU)...", ManualChecks.RunWindowsRunMruDialogCommandsHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔\ufe0f Анализ боевой статистики игрока (Killaura/Aimbot/CrystalAura)...", ManualChecks.RunMinecraftPlayerStatisticsCombatPvpAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce5 Аудит службы BITS и журнала Delivery Optimization на загрузку читов...", ManualChecks.RunWindowsDeliveryOptimizationAndBitsCacheDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcd6 Аудит книги рецептов (RecipeBook) и крафта на дюпы...", ManualChecks.RunMinecraftInventoryCraftingRecipeBookExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит сетевых интерфейсов, VPN-адаптеров и прокси во время SS...", ManualChecks.RunWindowsNetworkProfileInterfacesAndVpnTunnelsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Аудит серверных ресурспаков на блокировку и прозрачные X-Ray текстуры...", ManualChecks.RunMinecraftMultiplayerServerResourcePacksCacheAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Аудит истории Windows Terminal и профилей консоли...", ManualChecks.RunWindowsTerminalAndConsoleHostHistoryAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Анализ манифестов Fabric и Quilt (fabric.mod.json) на подмену модов...", ManualChecks.RunMinecraftFabricLoaderAndQuiltModMetadataCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf6 Аудит сопряжённых Bluetooth-устройств и беспроводных макро-педалей...", ManualChecks.RunWindowsConnectedDevicesAndBluetoothLePeripheralsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Аудит биндов клавиш (options.txt) на дабл-бинд и чит-меню...", ManualChecks.RunMinecraftPlayerKeybindsAndCustomOptionsDeepCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏰ Аудит планировщика задач Windows на скрытые задачи читов и RAT...", ManualChecks.RunWindowsTaskSchedulerHiddenTasksAndTriggersCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf92 Аудит слотов инвентаря и крафтинг-сетки на стаки и 32k...", ManualChecks.RunMinecraftPlayerInventoryCraftingGridStateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce5 Анализ незавершённых загрузок (.crdownload/.tmp) и скрытых PE-файлов...", ManualChecks.RunWindowsBackgroundTransferAndHttpClientTracesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит параметров JVM и javaagent мульти-лаунчеров (Prism/Lunar/Feather)...", ManualChecks.RunMinecraftLunarFeatherPrismCustomLaunchArgsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит следов уязвимых драйверов ядра (BYOVD) и удалённых служб...", ManualChecks.RunWindowsServiceTracesAndDeletedDriversRegistryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Аудит киков античитами (GrimAC/Vulcan/Polar) и крашей Netty в логах...", ManualChecks.RunMinecraftPlayerLogsDisconnectionAndKickAnalysisCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит истории сетевых подключений к серверам и альт-прокси...", ManualChecks.RunMinecraftPlayerLogsRecentServerIpsAndProxiesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc4\ufe0f Аудит баз совместимости SDB (Shim-инжекции и хуки)...", ManualChecks.RunWindowsAppCompatFlagsTelemetryAndCompatibilityDbCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc41\ufe0f Аудит OptiFine CIT/CEM текстур на подмену моделей и Chams...", ManualChecks.RunMinecraftOptifineCustomSkyAndCitPropertiesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1\ufe0f Анализ настроек мыши, кастомных кривых и сглаживания аима...", ManualChecks.RunWindowsClipCursorAndMouseRawInputHooksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Аудит конфигураций Mixin в Fabric/Quilt модах на перехват рендера...", ManualChecks.RunMinecraftFabricQuiltMixinAuditConfigsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc64 Аудит альт-аккаунтов и менеджеров (TheAltening/EasyMC)...", ManualChecks.RunMinecraftPlayerRealmsPersistenceAndMultiplayerConfigCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Аудит истории PowerShell PSReadLine и следов очистки команд...", ManualChecks.RunWindowsPowerShellConsoleReadLineHistoryDeepForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf0d Аудит правил мира GameRules и тиков в level.dat...", ManualChecks.RunMinecraftPlayerWorldSaveLevelDatGameRulesExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc3\ufe0f Аудит RecentFileCache.bcf на следы запусков удалённых программ...", ManualChecks.RunWindowsAppCompatFlagsTelemetryCompatibilityAslLogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Аудит вложенных библиотек Jar-in-Jar (META-INF/jars) на скрытые читы...", ManualChecks.RunMinecraftFabricLoaderCustomModsDependencyGraphCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Аудит отклонения и подмены серверных ресурспаков (X-Ray bypass)...", ManualChecks.RunMinecraftPlayerLogsServerResourcePackPromptsAndDeclinesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит кэша службы DNS на домены авторизации читов...", ManualChecks.RunWindowsDnsCacheServiceAndHostsResolutionAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит файлов локализации модов (lang/*.json) на скрытые чит-модули...", ManualChecks.RunMinecraftFabricLoaderCustomLanguageAndI18nCheatsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Аудит глобальных переменных Java (_JAVA_OPTIONS/JAVA_TOOL_OPTIONS)...", ManualChecks.RunWindowsSystemEnvironmentVariablesAndJavaToolOptionsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("✨ Анализ GLSL программ шейдерпаков на отключение буфера глубины (Chams)...", ManualChecks.RunMinecraftSodiumIrisShaderPackDepthBufferAndDepthMaskCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\ude9f Аудит логов GLFW, фокуса окна и сбоев захвата курсора...", ManualChecks.RunMinecraftPlayerLogsGlfwInputAndKeyboardFocusCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Аудит настроек Direct3D и инжекций внешних графических оверлеев...", ManualChecks.RunWindowsDirect3DAndDxgiPresentHookAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd75\ufe0f Аудит сокрытия модов из ModMenu (\"hidden\": true)...", ManualChecks.RunMinecraftFabricLoaderCustomManifestSignaturesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Аудит шрифтов ClickGUI в кэше Windows и Java AWT...", ManualChecks.RunWindowsFontCacheServiceWpfClickGuiFontsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcac Поиск команд с точкой (.bind/.vape) и Baritone (#goto) в логах чата...", ManualChecks.RunMinecraftPlayerLogsChatCommandHistoryAndDotPrefixCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbb Аудит истории консоли PowerShell (ConsoleHost_history) и анти-SS команд...", ManualChecks.RunWindowsPowerShellAstAndConsoleHostHistoryCommandAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Аудит дампов аварийных падений javaw и инжекторов (CrashDumps/WER)...", ManualChecks.RunWindowsErrorReportingCrashDumpsAndWerArchiveAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfad Аудит ресурспаков на скрытые X-Ray и ESP шейдеры подсветки сущностей...", ManualChecks.RunMinecraftResourcePackCustomShadersAndXRayEntityOutlineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит фоновой службы BITS на скрытые сетевые загрузки читов...", ManualChecks.RunWindowsBackgroundIntelligentTransferServiceBitsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⌨\ufe0f Аудит хуков ввода (AppInit_DLLs) и драйверов кликеров (Interception)...", ManualChecks.RunWindowsWin32kAndDirectInputLowLevelHooksAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит виртуальных сетевых адаптеров, VPN-туннелей (TAP/Wintun) и прокси...", ManualChecks.RunWindowsNetworkAdaptersAndVirtualTunnelingInterfacesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfad Аудит маскировки Java-модов под PNG/BIN/TXT (ZIP Magic Bytes)...", ManualChecks.RunMinecraftModFilesExtensionMasqueradingAndMagicBytesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd11 Аудит мастер-ключей DPAPI и криптографических аномалий Windows...", ManualChecks.RunWindowsDpapiMasterKeysAndCngSecurityAnomaliesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Аудит логов сбоев виртуальной машины Java (hs_err_pid) и аргументов JavaAgent...", ManualChecks.RunMinecraftJvmHotSpotCrashLogsAndJavaAgentArgsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbe Аудит истории подключений съёмных накопителей и флешек (USBSTOR)...", ManualChecks.RunWindowsUsbStorageHistoryAndRemovableDriveAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udea8 Аудит преднамеренной очистки журналов Windows (Event ID 104/1102)...", ManualChecks.RunWindowsEventLogClearedAndAuditTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0a Аудит библиотек OpenAL (OpenAL32.dll) на перехват звука и Sound ESP...", ManualChecks.RunMinecraftOpenAlSoundEngineAndNativesHookAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf9 Аудит виртуальных веб-камер и подмены экрана (OBS Virtual Camera)...", ManualChecks.RunWindowsDirectShowVirtualCameraAndStreamSpoofingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит launcher_profiles.json на подмену путей Java Runtime (Temp/Downloads)...", ManualChecks.RunMinecraftLauncherProfilesCustomJrePathTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏰ Аудит Планировщика задач на скрипты самоуничтожения и автозапуск...", ManualChecks.RunWindowsTaskSchedulerPersistenceAndWiperTasksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0c Аудит событий PnP на недавнее извлечение USB-флешек перед проверкой...", ManualChecks.RunWindowsKernelPnpUsbRemovalEventLogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc65 Аудит ресурспаков на кастомные CEM-модели сущностей и игроков...", ManualChecks.RunMinecraftResourcePackCustomEntityModelsCemCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd12 Аудит конфигурации DNS-over-HTTPS (DoH) и шифрованных запросов...", ManualChecks.RunWindowsDnsOverHttpsAndEncryptedResolutionAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddec Аудит логов Fabric на Mixin-инжекции в классы движения игрока...", ManualChecks.RunMinecraftFabricLoaderSyntheticMixinsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcca Аудит базы активности ресурсов SRUM (SRUDB.dat) на трафик удалённых утилит...", ManualChecks.RunWindowsSystemResourceUsageMonitorSrumAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccb Аудит истории буфера обмена (Win+V) и кэша активностей ActivitiesCache...", ManualChecks.RunWindowsClipboardHistoryAndActivityCacheAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1\ufe0f Аудит параметров мыши, акселерации и софта макросов (Razer/Bloody/Logitech)...", ManualChecks.RunMinecraftMousePollingRateAndRegistryJitterAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит параметров BCD на тестовый режим (TestSigning/nointegritychecks)...", ManualChecks.RunWindowsBcdDriverSignatureAndTestSigningAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf33 Аудит дерева процессов javaw.exe (PPID) и обнаружение осиротевших процессов...", ManualChecks.RunMinecraftJavawProcessParentageAndOrphanAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddd1\ufe0f Глубокий аудит метаданных $I в Корзине ($Recycle.Bin) на удалённые чит-файлы...", ManualChecks.RunWindowsRecycleBinDollarIMetadataForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc4\ufe0f Аудит службы теневых копий VSS и обнаружение удаления снимков восстановления...", ManualChecks.RunWindowsVolumeShadowCopiesAndVssWiperAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfa8 Аудит кэшей шейдерных конвейеров Sodium/Iris на повреждения и сбросы...", ManualChecks.RunMinecraftSodiumIrisShaderPipelineCacheAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd17 Аудит каталогов игры (.minecraft/mods) на скрытые симлинки (NTFS Junction)...", ManualChecks.RunMinecraftReparsePointsAndSymbolicLinksAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc64 Аудит профилей на сторонние серверы авторизации (Authlib-Injector/EasyMC)...", ManualChecks.RunMinecraftAuthlibInjectorAndAltDistributorsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uded1 Аудит реестра служб на уязвимые драйверы ядра (BYOVD: gdrv/mhyprot/rtcore64)...", ManualChecks.RunWindowsByovdVulnerableDriversAndRootDevicesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Аудит журнала Security на события создания процессов (Event 4688)...", ManualChecks.RunWindowsSecurityProcessCreationEvent4688AuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf19 Аудит каталогов Lunar Client на Weave-Loader и сторонние JavaAgent модули...", ManualChecks.RunMinecraftLunarClientWeaveAndAgentHooksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏱\ufe0f Аудит журнала диагностики производительности Windows (WDI)...", ManualChecks.RunWindowsDiagnosticsPerformanceWdiLogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfa5 Аудит конфигураций OBS Studio на изоляцию javaw и скрытие оверлеев...", ManualChecks.RunMinecraftObsGameCaptureAndOverlayHookAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbb Аудит профилей Windows Terminal на скрытый запуск с обходом политик...", ManualChecks.RunWindowsTerminalProfilesAndConPtyHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Расшифровка записей UserAssist (ROT13) на скрытые запуски читов...", ManualChecks.RunWindowsUserAssistRot13DecodedExecutionAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Аудит манифестов Forge на ранний запуск CoreMod и TweakClass...", ManualChecks.RunMinecraftForgeCoreModTweakerAndFmlPluginCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Аудит дампов аварийного сбоя ядра Windows (BSOD Minidump)...", ManualChecks.RunWindowsKernelMinidumpAndBsodCrashAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcac Аудит каталогов клиентов на зашифрованные IRC-каналы связи...", ManualChecks.RunMinecraftEncryptedIrcAndSecretChatForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udff7\ufe0f Аудит кэша MuiCache на сопоставление путей и имён переименованных читов...", ManualChecks.RunWindowsMuiCachePathAndFriendlyNameSpoofingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd10 Аудит ключей RSA MachineKeys на генерацию локальных MITM-прокси...", ManualChecks.RunWindowsCryptoRsaMachineKeysAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udd16 Поиск каталогов, воксельных кэшей и настроек автопилота Baritone...", ManualChecks.RunMinecraftBaritonePathfindingCacheAndSettingsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc1 Аудит ProgramData на скрытые GUID-папки распаковки инжекторов...", ManualChecks.RunWindowsProgramDataHiddenStagingFoldersCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит модов мультипротокола (ViaFabric/MultiConnect) на подмену версий...", ManualChecks.RunMinecraftViaFabricProtocolVersionSpoofingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc64 Аудит реестра BAM на запуски программ из удалённых учётных записей...", ManualChecks.RunWindowsBamDeletedUserSidExecutionForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd07 Аудит реестра SilentProcessExit на подавление отчётов сбоев javaw...", ManualChecks.RunWindowsSilentProcessExitAndWerFaultAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\ude80 Аудит версий игры на подмену главного класса запуска (MainClass Wrapper)...", ManualChecks.RunMinecraftLauncherCustomMainClassWrapperCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd20 Аудит каталога шрифтов Windows на шрифты интерфейсов читов (Comfortaa)...", ManualChecks.RunWindowsFontLinkAndClickGuiFontInstallAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf8 Аудит папки скриншотов на повреждения и следы выборочного удаления...", ManualChecks.RunMinecraftScreenshotTimestampGapsAndDeletionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Аудит журнала PowerShell на события ScriptBlock Logging (Event 4104)...", ManualChecks.RunWindowsPowerShellScriptBlockLoggingEvent4104Check),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит реестра AppCompatFlags Layers на шимы RunAsInvoker и чит-приложения...", ManualChecks.RunWindowsAppCompatFlagsLayersPrivilegeAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca1 Аудит options.txt на завышенную гамму (FullBright) и быстрые клавиши...", ManualChecks.RunMinecraftPlayerOptionsChatKeybindsAndMacrosCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc1 Аудит реестра Shellbags (BagMRU) на открытие и переименование папок читов...", ManualChecks.RunWindowsShellbagsFolderHistoryAndRenamingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc41\ufe0f Аудит настроек рендера Sodium на принудительное отключение Entity Culling...", ManualChecks.RunMinecraftSodiumVideoSettingsFullbrightAndFogCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc27 Аудит подсистемы WSL (Windows Subsystem for Linux) на сторонние туннели...", ManualChecks.RunWindowsWslVirtualSubsystemAndNetworkBridgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbd Аудит монтирования виртуальных дисков VHD/VHDX на съёмные тома...", ManualChecks.RunWindowsVirtualHardDiskVhdxAndMountAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Аудит инстансов Prism/MultiMC на сторонние патчи библиотек...", ManualChecks.RunMinecraftPrismLauncherInstancePatchesAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит подсистемы безопасности LSA (RunAsPPL) на целостность политик...", ManualChecks.RunWindowsLsaProtectionAndRunAsPplAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Аудит кэша серверных ресурспаков на блокировку и подмену хэшей...", ManualChecks.RunMinecraftServerResourcePacksCacheAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит системного файла hosts на подмену серверов авторизации Mojang...", ManualChecks.RunWindowsEtcHostsFileRedirectionAndTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит исключений антивируса Windows Defender на сокрытие папок читов...", ManualChecks.RunWindowsDefenderExclusionPathsAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd04 Поиск модификаций отвязки камеры FreeLook и 360-градусного обзора...", ManualChecks.RunMinecraftFreeLookAndPerspectiveModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚠\ufe0f Аудит журнала Security на события изменения политик аудита (Event 4719)...", ManualChecks.RunWindowsSecurityAuditPolicyChangedEvent4719Check),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Аудит шрифтов в ресурспаках на отрицательные отступы и эксплойты никнеймов...", ManualChecks.RunMinecraftResourcePackFontGlyphWidthExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Аудит недавних запусков в Prefetch за 24ч и всплесков вызовов скриптов...", ManualChecks.RunWindowsPrefetchExecutionBurstAndHashCorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udccc Аудит закреплений Taskband и кэша иконок трея TrayNotify...", ManualChecks.RunWindowsTaskbandAndTrayNotifyPinForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Поиск скрытых загрузчиков байткода Fabric/NeoForge LanguageAdapter...", ManualChecks.RunMinecraftFabricAndNeoForgeLanguageProviderTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит политик AppLocker и журналов блокировок неподписанного ПО...", ManualChecks.RunWindowsAppLockerAndSRPBypassEventLogsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⌨\ufe0f Анализ конфигураций options.txt на спаренные бинды атаки и макросы...", ManualChecks.RunMinecraftChatMacroAndKeybindTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd00 Аудит реестра на перехват и подмену команд Shell Open (.exe/.jar)...", ManualChecks.RunWindowsRegistryShellOpenCommandHijackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Аудит слоев перехвата рендеринга DirectX и Vulkan...", ManualChecks.RunWindowsDirectXAndVulkanGraphicsLayerHookCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит параметров безопасности JVM и аргументов javaagent...", ManualChecks.RunMinecraftJvmSecurityManagerAndPropertiesTamperCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbe Аудит съёмных томов, RAM-дисков и реестра MountedDevices...", ManualChecks.RunWindowsStorageSpacesAndBitLockerHiddenVolumesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Поиск модификаций перехвата сетевого конвейера Netty...", ManualChecks.RunMinecraftNettyPipelinePacketManipulationModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc3\ufe0f Аудит каталогов VirtualStore и кастомных SDB-баз AppCompat...", ManualChecks.RunWindowsVirtualStoreAndCustomSdbAppCompatCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde0 Аудит параметров Session Manager Memory Management и отключения дампов...", ManualChecks.RunWindowsMemoryManagementAndPagefileEvasionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Поиск скрытых вложенных библиотек Jar-in-Jar внутри модов...", ManualChecks.RunMinecraftFabricJarInJarNestedContainerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0c Анализ логов SetupAPI на подключение аппаратных кликеров Arduino/Pico...", ManualChecks.RunWindowsSetupApiHardwareDeviceInstallForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf1 Аудит JSON-моделей блоков в ресурспаках на модельный X-Ray...", ManualChecks.RunMinecraftResourcePackBlockModelXrayForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce1 Аудит параметров TCP/IP интерфейсов на MTU десинк и регедит-твики...", ManualChecks.RunWindowsTcpStackTuningAndDesyncTweaksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd11 Аудит времени модификации мастер-ключей DPAPI (%AppData%\\Protect)...", ManualChecks.RunWindowsDpapiMasterKeyRecentActivityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc89 Аудит конфигураций Mixin на инъекции в Reach, Hitbox и LivingEntity...", ManualChecks.RunMinecraftFabricMixinInjectorTargetAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce5 Аудит службы BITS и очереди qmgr*.dat на скрытую догрузку файлов...", ManualChecks.RunWindowsBitsTransferJobsPersistenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Поиск скрытых профилей автокликеров и макросов в .minecraft/config...", ManualChecks.RunMinecraftHiddenMacroConfigsAndProfilesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Аудит параметров Console Host на сокрытие консольных окон (буфер 1x1)...", ManualChecks.RunWindowsConsoleHostInvisibleBufferEvasionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\ude9f Аудит журнала System на установку ядерных драйверов (Event 7045)...", ManualChecks.RunWindowsKernelDriverAndServiceInstallEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Проверка модов на прямую подмену ванильных классов (Non-Mixin bypass)...", ManualChecks.RunMinecraftFabricModCoreClassIntegrityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcc2 Декодирование бинарных PIDL потоков в OpenSavePidlMRU на выбор читов...", ManualChecks.RunWindowsOpenSavePidlMruHiddenPathsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Сканирование локальных loopback портов на WebSocket/IPC мосты читов...", ManualChecks.RunMinecraftClientLocalWebSocketAndIpcBridgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⏱\ufe0f Анализ журнала на панические уходы в сон/гибернацию перед проверкой...", ManualChecks.RunWindowsSleepWakePanicTimeJumpForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udded Анализ истории ручного ввода путей в Проводнике (TypedPaths)...", ManualChecks.RunWindowsExplorerTypedPathsHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf0c Аудит конфигураций неба OptiFine и прозрачных текстур игроков...", ManualChecks.RunMinecraftOptiFineSkyAndEntityTransparencyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce1 Поиск установленных драйверов захвата пакетов Npcap/WinPcap...", ManualChecks.RunWindowsNetworkSnifferAndPromiscuousDriversCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf8 Аудит целостности скриншотов клиента и недавних временных интервалов...", ManualChecks.RunMinecraftScreenshotsMetadataAndTimestampGapsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚙\ufe0f Аудит постоянных WMI подписок на скрипты авто-зачистки следов...", ManualChecks.RunWindowsWmiEventSubscriptionPersistenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc2\ufe0f Глубокий аудит истории папок в ShellBags (BagMRU)...", ManualChecks.RunWindowsShellBagsFolderHistoryDeepAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит системных переменных _JAVA_OPTIONS и JAVA_TOOL_OPTIONS...", ManualChecks.RunMinecraftJvmGlobalEnvironmentOptionsInjectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd10 Аудит активности криптографических RSA-контейнеров за 24ч...", ManualChecks.RunWindowsRsaAndCngKeyContainerActivityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0a Поиск модификаций звукового ESP и радаров шагов в mods/...", ManualChecks.RunMinecraftDirectionalAudioAndSoundPhysicsEspModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd14 Анализ базы уведомлений Windows wpndatabase.db на алерты читов...", ManualChecks.RunWindowsNotificationToastDatabaseForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Аудит профилей PowerShell на скрытые скрипты зачистки следов...", ManualChecks.RunWindowsPowerShellProfileAndTranscriptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfad Поиск модов маскировки заголовка окна игры и фейкового Discord RPC...", ManualChecks.RunMinecraftWindowTitleAndDiscordRpcMaskingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит слоев AppCompatFlags на шимы IgnoreFreeLibrary и сокрытие DEP...", ManualChecks.RunWindowsAppCompatLayersIgnoreFreeLibraryAndShimCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Аудит шрифтов ресурспаков на zero-width символы и смещение намтегов...", ManualChecks.RunMinecraftResourcePackFontZeroWidthNametagShiftCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc64 Аудит реестра BAM на запуски читов из-под удалённых или гостевых SID...", ManualChecks.RunWindowsBamOrphanedUserSidExecutionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbe Аудит размера и целостности файлов подкачки pagefile/swapfile...", ManualChecks.RunWindowsPagefileAndSwapfileSizeAnomalyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚡ Поиск аномальных экстремальных приоритетов Mixin (priority > 10000)...", ManualChecks.RunMinecraftFabricMixinPriorityOrderHijackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd12 Аудит политик цифровой подписи драйверов и целостности кода CI...", ManualChecks.RunWindowsDriverSigningPolicyAndCiIntegrityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udea8 Поиск модификаций оповещения об администрации (Staff-Alert/Vanish)...", ManualChecks.RunMinecraftStaffAlertAndChatPacketInterceptionModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0c Аудит реестра USBSTOR на спуфнутые серийные номера и эмуляторы...", ManualChecks.RunWindowsUsbStorageSerialSpoofingAndVirtualDriveCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfc3 Аудит истории команд диалога Win+R (RunMRU) на вайперы...", ManualChecks.RunWindowsExplorerRunMruCommandHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce1 Поиск проприетарных CustomPayload каналов связи чит-клиентов...", ManualChecks.RunMinecraftCustomPayloadChannelTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd11 Аудит зарегистрированных крипто-провайдеров CSP на сторонние DLL...", ManualChecks.RunWindowsCryptographyServiceProvidersRegistryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd07 Анализ sounds.json в ресурспаках на заглушение шагов (Silent-Step)...", ManualChecks.RunMinecraftResourcePackSoundsMutingExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Поиск служб виртуальных мониторов и трансляции внешних радаров...", ManualChecks.RunWindowsVirtualDisplayAndRemoteStreamingDriverCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5\ufe0f Аудит истории подключений Remote Desktop (MSTSC MRU)...", ManualChecks.RunWindowsRdpConnectionHistoryAndMstscMruCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd2e Поиск GLSL-кода подсветки силуэтов игроков в шейдерпаках...", ManualChecks.RunMinecraftShaderPackPlayerSilhouetteEspCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce5 Аудит событий службы BITS на загрузки из Discord/GitHub...", ManualChecks.RunWindowsBitsClientOperationalEventLogsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd12 Поиск тяжелых коммерческих обфускаторов (Zelix/Allatori) в модах...", ManualChecks.RunMinecraftModClassStringObfuscationEntropyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfad Аудит хэшей Prefetch на маскировку читов под EXPLORER.EXE...", ManualChecks.RunWindowsPrefetchMasqueradingAndHashAnomalyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbe Аудит служб теневого копирования VSS и отключения System Restore...", ManualChecks.RunWindowsVssProviderAndSystemRestoreTamperCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf7 Поиск модификаций свободной камеры FreeCam в mods/ и config/...", ManualChecks.RunMinecraftFreeCamAndSpectatorPacketEvasionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf9 Аудит устройств DirectShow на виртуальные веб-камеры (OBS/ManyCam)...", ManualChecks.RunWindowsDirectShowVirtualVideoFilterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("✨ Анализ текстур частиц в ресурспаках на частицы-маяки (Crit ESP)...", ManualChecks.RunMinecraftResourcePackCustomParticleBeaconExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит реестра MountPoints2 на запуск читов с сетевых UNC-шар...", ManualChecks.RunWindowsExplorerMountPoints2NetworkSharesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Расшифровка ROT13 записей в UserAssist за последние 48 часов...", ManualChecks.RunWindowsUserAssistRot13ExecutionDeltaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит кэша шейдеров Sodium/Iris на блокировку файлов ReadOnly...", ManualChecks.RunMinecraftSodiumShaderCacheTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0c Аудит журнала Kernel-PnP на экстренное отключение флешек (Event 225)...", ManualChecks.RunWindowsKernelPnpUsbEjectRecentEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udff9 Поиск модификаций пакетного флуда FastUse и скорострельного лука...", ManualChecks.RunMinecraftFastUseAndPacketFloodModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcbb Аудит профилей Windows Terminal settings.json на скрытые скрипты...", ManualChecks.RunWindowsTerminalConPtyProfilesPersistenceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Проверка удаления теневых копий томов и событий сброса VSS...", ManualChecks.RunWindowsVolumeShadowCopyDeletionEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Поиск синтетических мостов Mixin и скрытых аксессоров читов...", ManualChecks.RunMinecraftFabricLoaderMixinSyntheticBridgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Анализ фоновых задач передачи BITS на скрытую загрузку читов...", ManualChecks.RunWindowsBackgroundTransferBITSJobsBinaryForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔ Поиск модификаций NoSlowdown и KeepSprint в клиенте...", ManualChecks.RunMinecraftNoSlowdownAndKeepSprintModCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Анализ целостности службы кэша шрифтов Windows (FontCache)...", ManualChecks.RunWindowsFontCacheServiceAnomalyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc8e Проверка PBR-текстур и подсветки руд в шейдерах сквозь стены...", ManualChecks.RunMinecraftSodiumIrisPbrNormalMapXRayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1 Анализ фильтр-драйверов клавиатуры и мыши (Interception/Clicker)...", ManualChecks.RunWindowsDirectInputKeyboardHookTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\ude80 Поиск скрытых ранних entrypoints (preLaunch) в Fabric-модах...", ManualChecks.RunMinecraftFabricLoaderEntrypointInjectionForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Аудит удаления и сброса журнала изменений NTFS USN Journal...", ManualChecks.RunWindowsNtfsChangeJournalUsnPurgeEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc41 Поиск отключения куллинга сущностей и настенных никнеймов...", ManualChecks.RunMinecraftEntityCullingBypassConfigCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf1 Проверка куллинга граней блоков Sodium (Cave Finder / X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionCullingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc3 Анализ кэша телеметрии AppCompatFlags Compatibility Assistant...", ManualChecks.RunWindowsAppCompatFlagsTelemetryCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfad Поиск стеганографических пейлоадов в текстурах скинов и плащей...", ManualChecks.RunMinecraftCustomSkinStealthCapeNametagCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddd1 Парсинг бинарных индексов $I корзины на удаление читов...", ManualChecks.RunWindowsRecycleBinOrphanedSidDeletedJarsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Проверка манифестов META-INF и целостности JAR-модов...", ManualChecks.RunMinecraftFabricLoaderJarModSignatureCertificateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит аргументов JVM и перехвата ClassPath на внешние пути...", ManualChecks.RunMinecraftFabricLoaderClassPathUrlHijackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\ude9f Поиск скрытых прозрачных оверлеев DirectComposition ESP...", ManualChecks.RunWindowsDirectCompositionOverlayForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfaf Поиск скрытых Mixin-аксессоров к приватным боевым полям...", ManualChecks.RunMinecraftFabricLoaderMixinSyntheticAccessorLeakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddc4 Аудит сброса и зачистки кэша шрифтов DirectWrite FontTable...", ManualChecks.RunWindowsFontTableCacheTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfa8 Аудит кастомных колормапов OptiFine на X-Ray прозрачность...", ManualChecks.RunMinecraftOptiFineCustomColormapXRayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Аудит реестра FontSubstitutes на сокрытие текста и оверлеи...", ManualChecks.RunWindowsFontSubstitutesRegistryHijackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd13 Поиск динамической загрузки зашифрованного байткода в память...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicClassLoadingForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf1 Аудит политик AppModel на сайдлоад UWP-макросов и кликеров...", ManualChecks.RunWindowsAppModelPolicyExecutionForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfae Проверка отключения теста глубины OpenGL Sodium (Engine Chams)...", ManualChecks.RunMinecraftSodiumCustomGlFormatTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Анализ командной истории PowerShell на следы зачистки и скриптов...", ManualChecks.RunWindowsPowerShellEngineInternalScriptCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Поиск кастомных IMixinConfigPlugin и условных инжекторов Mixin...", ManualChecks.RunMinecraftFabricLoaderMixinConfigPluginTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0a Аудит виртуальных аудиокабелей и loopback устройств (SoundClicker)...", ManualChecks.RunWindowsDirectSoundAudioOutputDeviceHookCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf1 Проверка модификаций мешера чанков Sodium на подсветку руд...", ManualChecks.RunMinecraftSodiumCustomMeshPipelineShadersCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udda5 Аудит истории удаленных подключений RDP Terminal Server Client...", ManualChecks.RunWindowsTerminalServerClientActiveXHistoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd4a Детект манипуляций способностями игрока и скоростью полета...", ManualChecks.RunMinecraftPlayerAbilitiesFlightCreativeExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddec Аудит динамических Java Agent в Temp и модулей Attach API...", ManualChecks.RunMinecraftFabricLoaderClassWeavingAgentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Анализ изоляции и недавних перезапусков Font Driver Host...", ManualChecks.RunWindowsFontDriverHostUserModeDriverCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcf7 Поиск патчей коллизии камеры и проникновения сквозь блоки...", ManualChecks.RunMinecraftSodiumCustomCameraClipFrustumCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Аудит параметров хранилища теневых копий и урезания VSS...", ManualChecks.RunWindowsVolumeSnapshotLicensingServiceForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca1 Аудит кастомных шейдеров подсветки Glowing ESP сквозь стены...", ManualChecks.RunMinecraftEntityRendererGlowingOutlineShaderCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf43 Проверка прозрачности листвы и альфа-отсечения Sodium (Cave Finder)...", ManualChecks.RunMinecraftSodiumCustomAlphaDiscardPipelineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Аудит реестра шрифтов на установку из временных каталогов Temp...", ManualChecks.RunWindowsFontRegistryHiddenTtfInstallCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔ Поиск принудительной отмены отдачи и урона в Mixin (AntiKnockback)...", ManualChecks.RunMinecraftFabricLoaderMixinInjectorCallbackReturnTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd04 Аудит событий отключения и сброса службы восстановления системы...", ManualChecks.RunWindowsVolumeRestorePointCreationFrequencyEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Поиск скрытых сетевых каналов управления читами (CustomPayload C2S)...", ManualChecks.RunMinecraftCustomPayloadNetworkC2SExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Поиск синтетических мостов @Invoker к приватным методам боя...", ManualChecks.RunMinecraftFabricLoaderMixinSyntheticInvokerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Аудит подразделов реестра DirectWrite на регистрацию чит-шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryKeyMruCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf2b Проверка отключения дистанционного тумана и слепоты Sodium...", ManualChecks.RunMinecraftSodiumCustomFogPipelineFrustumCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Аудит фильтр-драйверов стека томов на отключение драйвера volsnap...", ManualChecks.RunWindowsVolumeSnapshotFilterDriverAltitudeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf1 Проверка порядка отрисовки слоев чанков Sodium (Engine Chams)...", ManualChecks.RunMinecraftSodiumChunkRenderSortOrderCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Аудит кэша remappedJars Fabric Loader на инжект классов читов...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicClassInjectorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Аудит аварийных остановок службы кэша шрифтов FontCache (7031)...", ManualChecks.RunWindowsFontCacheServiceEventLogCrashForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf1 Проверка дескрипторов формата вершин чанков Sodium на флаги рентгена...", ManualChecks.RunMinecraftSodiumCustomVertexFormatMemoryOffsetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Аудит реестра VolSnap и SPP на блокировку лимитов теневых копий...", ManualChecks.RunWindowsVolumeShadowCopyStorageAllocationDeltaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔ Поиск модификаций дистанции атаки и расширения луча рейкаста Reach...", ManualChecks.RunMinecraftPlayerInteractionReachRaycastTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfad Поиск манипуляций приватными полями движения через @Shadow...", ManualChecks.RunMinecraftFabricLoaderMixinShadowFieldTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd12 Проверка прав доступа ACL реестра службы FontCache на блокировку...", ManualChecks.RunWindowsFontCacheServiceRegistrySecurityAclCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf1 Аудит кэша окклюзии блоков Sodium на выборочное просвечивание баз...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionCacheTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Аудит модулей записи VSS Writers на наличие сбоев и срыва снимков...", ManualChecks.RunWindowsVolumeShadowCopyWriterFailureEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfd7 Поиск модификаций автоматической установки блоков Scaffold...", ManualChecks.RunMinecraftPlayerBlockPlacementAngleDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Поиск динамических инжекторов privateLookupIn к классам игры...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicInvokerFactoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1 Аудит реестра мыши и параметров программных кликеров...", ManualChecks.RunWindowsDirectInputDeviceRawInputSuppressionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca1 Проверка экстремальной гаммы FullBright и карты освещения...", ManualChecks.RunMinecraftSodiumCustomLightmapTextureOverrideCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Аудит сброса теневых копий томов из-за переполнения хранилища (25)...", ManualChecks.RunWindowsVolumeShadowCopyStorageFragmentationEventsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Поиск модификаций автоматической подмены тотема (AutoTotem)...", ManualChecks.RunMinecraftPlayerInventoryAutoTotemSwapExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("☕ Аудит аргументов JVM на скрытые параметры Bootstrap ClassPath...", ManualChecks.RunMinecraftFabricLoaderMixinBootstrapAgentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Аудит политик проверки подписи драйверов ядра и изоляции HVCI...", ManualChecks.RunWindowsDirectInputDeviceDriverSignatureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf1 Проверка формы воксельных коллизий блоков (Phase / ReachThrough)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionVoxelShapeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf32 Анализ дерева родительских процессов при вызове утилит очистки VSS...", ManualChecks.RunWindowsVolumeShadowCopyDeletionProcessLineageCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⚔ Поиск модификаций снижения отдачи и отмены импульсов взрывов...", ManualChecks.RunMinecraftPlayerCombatKnockbackReductionExploitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\udde9 Поиск синтетических мостов к диспетчеру рендеринга и матрицам...", ManualChecks.RunMinecraftFabricLoaderMixinSyntheticAccessorBridgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd12 Аудит атрибутов файлов и эксклюзивных блокировок кэша шрифтов...", ManualChecks.RunWindowsFontTableCacheFileLockingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf1 Поиск аномалий шага вершин мешера чанков Sodium для рентгена...", ManualChecks.RunMinecraftSodiumCustomChunkMeshFormatStrideAnomalyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Аудит критического урезания максимального размера хранилища VSS...", ManualChecks.RunWindowsVolumeShadowCopyStorageMaximumSizeTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⛏ Поиск модификаций мгновенного ломания блоков FastBreak / SpeedMine...", ManualChecks.RunMinecraftPlayerFastBreakPacketTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("⌨ Поиск скрытых перехватчиков клавиатуры и клавиш вызова чит-меню...", ManualChecks.RunMinecraftFabricLoaderMixinSyntheticBridgeDispatchCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf5 Аудит потоков процесса игры javaw.exe на наличие сторонних кликеров...", ManualChecks.RunWindowsDirectInputDeviceRawInputThreadHookCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf1 Поиск переназначения породы в полупрозрачные слои рендеринга...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTranslucentPassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Аудит событий изменения типа запуска службы Volume Shadow Copy (7040)...", ManualChecks.RunWindowsVolumeShadowCopyDeletionServiceStateChangeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfaf Поиск модификаций Silent Aimbot и рассинхронизации углов обзора...", ManualChecks.RunMinecraftPlayerCombatAimbotRotationPitchDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddec Поиск динамических трансформеров байткода на подмену инструкций...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodReplacerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Аудит политик FontManagement на принудительное отключение кэша...", ManualChecks.RunWindowsFontTableCacheRegistrySecurityAclAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf0b Поиск модификаций рендеринга жидкостей и прозрачной лавы (Lava X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionFluidCullingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcdc Аудит фактов принудительной очистки системных журналов событий...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogPurgeCorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\uddb1 Поиск модов автоматического клика и обхода задержки атаки...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBurstIntervalAnomalyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddec Поиск динамических инжекторов доступа к приватным полям игры...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicFieldAccessorInjectorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc89 Аудит каталогов Java Runtime на подмену системных DLL рендеринга...", ManualChecks.RunWindowsFontTableCacheDWriteDllHijackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf1 Поиск модификаций луча рейкаста и взаимодействия сквозь стены...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionVoxelRaycastCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Аудит всплесков событий метаданных NTFS и VolSnap (Вайпинг диска)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogOverwriteCorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfaf Поиск модификаций расширения угла атаки и кругового Reach FOV...", ManualChecks.RunMinecraftPlayerCombatReachAngleFovLimitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udce6 Поиск динамических перехватчиков пакетов движения и боя...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodInterceptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd24 Аудит памяти javaw.exe на наличие потоков шрифтов в ОЗУ...", ManualChecks.RunWindowsFontTableCacheDWriteCustomFontFileStreamCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc41 Поиск принудительного отключения отсечения сущностей Frustum ESP...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionEntityFrustumCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Аудит событий внезапного отключения томов для сброса VSS (157)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeOfflineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1 Поиск модификаций предикатов рейкаста и пробития сквозь щиты...", ManualChecks.RunMinecraftPlayerCombatReachRaycastEntityFilterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic Accessor интерфейсов (Reach/Combat injection)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicAccessorInterfaceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка FontCache Registry Security DACL Wipe (NULL DACL / Access Denied)...", ManualChecks.RunWindowsFontTableCacheRegistrySecurityDaclWipeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Custom Block Occlusion & Biome Tint (X-Ray Foliage/Ore Glow)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionBiomeTintCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Shadow Copy IOCTL & DeviceControl (Event 12293/12294 Purge)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogDeviceControlCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Combat AutoClicker Click Jitter & Speed Limit Spoofing (CPS Emulation)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerClickSpeedLimitCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic Method Redirect (@Redirect detours)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodRedirectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка GDI FontLink & EUDC Registry (Инъекция символов чит-оверлеев)...", ManualChecks.RunWindowsFontTableCacheGdiFontLinkRegistryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Terrain Mesh Compiler Pipeline (Chunk Builder X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshCompilerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot TRIM & Storage Deallocation (Event 153/154/8200)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogTrimZeroingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Silent Aimbot & Angle Snap Smoothing (Rotation Pitch/Yaw Decoupling)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAngleSnapSmoothCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic LocalCapture (Перехват локальных переменных)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicLocalCaptureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Font Drivers & FontAssoc Registry (Драйверы шрифтов и инъекции)...", ManualChecks.RunWindowsFontTableCacheRegistryFontDriversCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Translucent Render Pass Pipeline (Chest / Wallhack X-Ray)...", ManualChecks.RunMinecraftSodiumChunkRenderPassAlphaBlendPipelineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot Disk Signature Clash & Purge (Event 58)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogDiskSignatureClashCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка AutoClicker Burst Mode & Micro-Pause (Butterfly / DoubleClick)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBurstJitterMacroCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic @ModifyConstant (Reach/Speed constant spoofing)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicModifyConstantCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка GDI+ PrivateFontCollection & Memory Font Streams в javaw.exe...", ManualChecks.RunWindowsFontTableCacheGdiPrivateFontCollectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Biome Blend & SkyLight Cache Tampering (FullBright/X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionBiomeBlendRadiusCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot RAW Disk Sector Access & Header Purge (Event 140)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogRawDiskSectorReadCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Combat AutoClicker CPS Distribution & Clamped Intervals...", ManualChecks.RunMinecraftPlayerCombatAutoClickerTargetCPSDistributionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic @ModifyVariable (Velocity/Damage tampering)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicModifyVariableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Font Directory Header & DSIG Signature (Аномалии структуры шрифтов)...", ManualChecks.RunWindowsFontTableCacheRegistryFontSignatureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Voxel Shape Occlusion Cache (Phase / Wallhack)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionVoxelCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot IO Flush Breakdown (Event 137/57 Logfile Purge)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogIoFlushFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка AutoClicker Debounce State Matrix & Microswitch Hold/Release...", ManualChecks.RunMinecraftPlayerCombatAutoClickerDebounceStateMatrixCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic @ModifyReturnValue (Reach/Sneak bypass)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicModifyReturnValueCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Console TrueTypeFont Registry (Сокрытие вывода консоли)...", ManualChecks.RunWindowsFontTableCacheRegistryFontAssociationOverrideCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Fluid Mesh Compiler & Lava X-Ray (Fluid Face Culling)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionFluidFaceCullingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot Forced Shrinkage & Purge (Event 257/89)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeResizeShrinkCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка AutoClicker Physical Inertia & Friction Drag Modeling...", ManualChecks.RunMinecraftPlayerCombatAutoClickerInertiaFrictionModelCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic @WrapOperation (Packet / Attack wrapping)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicWrapOperationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка FontCache Service DAT File Section Integrity (FNTCACHE.DAT)...", ManualChecks.RunWindowsFontTableCacheRegistryFontSignatureExtendedTableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Block Occlusion Cache Fast-Lookup Bypass...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionBlockOcclusionCacheLookupCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot Cluster Allocation Breakdown (Event 55/98)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogClusterBadAllocationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка AutoClicker Reaction Latency & Post-Hit Hesitation...", ManualChecks.RunMinecraftPlayerCombatAutoClickerReactionLatencyDistributionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic @Inject(cancellable=true) (NoFall/Velocity)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicCancellableInjectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Untrusted Font Mitigation Registry Policies (Обход Defender Guard)...", ManualChecks.RunWindowsFontTableCacheRegistryFontDownloadRegistryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Block Face Opacity & Quad Builder (Ore ESP X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionBlockFaceFullAndOpaqueCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot NTFS Self-Healing Breakdown (Event 98/262144)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogChkdskSelfHealingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Multi-Target Switching Hesitation & Angular Velocity Limits...", ManualChecks.RunMinecraftPlayerCombatAutoClickerTargetSwitchHesitationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric MixinExtras @Share Sugar (Скрытая передача данных инъекций)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicShareCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка OpenType fsType Font Embedding Licensing & DRM Flags...", ManualChecks.RunWindowsFontTableCacheRegistryFontEmbeddingLicensingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Terrain Section Queue Priority (Приоритет рудных чанков)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainSectionQueueCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot Exclusive Lock Starvation (Event 12297/24)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotLockLeakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка AutoClicker Sinusoidal Tremor & Lissajous Aim Tracking...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMicroDriftSinusoidalCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric MixinExtras @Local Sugar (Hitbox / Box injection)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicLocalCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка OpenType post Table Glyph Names (Шрифты ESP и радара)...", ManualChecks.RunWindowsFontTableCacheRegistryFontPostScriptGlyphNameCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Terrain Mesh Vertex Matrix Transform (Squash X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshTransformCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot Paging IO Starvation & Breakdown (Event 51)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotPagingIoCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Kalman Filter & Adaptive State Estimation Aim Smoothing...", ManualChecks.RunMinecraftPlayerCombatAutoClickerKalmanFilterSmoothingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic @Coerce (Пакеты/Обфускация)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicCoerceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка OpenType kern & GPOS Positioning Tables (ESP оверлеи)...", ManualChecks.RunWindowsFontTableCacheRegistryFontKerningTableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Terrain Mesh Slicing & Vertical Clipping (CaveFinder)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshSliceCacheCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot Driver Detach & Controller Error (Event 10/11)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotDeviceIoControlCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Cubic Bezier Spline & Hermite Curve Aim Interpolation...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBezierSplineInterpolationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic @WrapWithCondition (Packet / Sound drop)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicWrapWithConditionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка OpenType maxp Table Memory Profile (Аномалии хипа шрифтов)...", ManualChecks.RunWindowsFontTableCacheRegistryFontMaxpTableMemoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Staging Buffer Off-Heap Vertex Modification (VRAM X-Ray)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshStagingBufferCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot Harddisk IO Error Burst (Event 7/15)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotHarddiskIoErrorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка PID Controller Aim Feedback Loop (P-I-D Наводка)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPIDControllerAimCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric MixinExtras LocalRef (Мутация параметров движения/боя)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicSugarRefCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка OpenType hmtx Metric Table (Фантомные символы оверлеев)...", ManualChecks.RunWindowsFontTableCacheRegistryFontHmtxTableWidthCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Direct Index Draw & Wireframe Topology Hack...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshDirectIndexDrawCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot PnP Surprise Removal (Event 219/225)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotDevicePnpRemovalCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Adaptive Kalman Covariance Tuning & Dynamic Aim Gain...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAdaptiveKalmanCovarianceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric MixinExtras @WrapMethod (Полный детуринг метода)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicWrapMethodCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType fpgm Program Table Bytecode (Инструкции шрифтов)...", ManualChecks.RunWindowsFontTableCacheRegistryFontFpgmTableBytecodeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Depth Testing & Stencil Buffer (In-Engine Chams)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshDepthTestCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot StorAhci Bus Device Reset (Event 129)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotDeviceResetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Gauss-Markov Stochastic Process & Aim Drift Modeling...", ManualChecks.RunMinecraftPlayerCombatAutoClickerGaussMarkovDriftCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic Expressions (MVEL/Janino/SpEL)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicExpressionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType cvt Control Value Table (Инъекции оверлеев)...", ManualChecks.RunWindowsFontTableCacheRegistryFontCvtTableValueCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium glPolygonOffset Depth Bias (Ore Wallhack)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshPolygonOffsetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка VSS NTFS $MFT Record Corruption (Event 130/134/140)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotMftRecordCorruptCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Markov Chain Discrete State Transition Matrix...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMarkovChainStateTransitionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Synthetic Bridges & Hidden Classes...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicSyntheticBridgeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType gasp Table & Anti-Aliasing Overlays...", ManualChecks.RunWindowsFontTableCacheRegistryFontGaspTableForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Multi-Draw Indirect (MDI Command Buffer)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshMultiDrawIndirectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка VSS NTFS $UsnJrnl Truncation (Event 136/138/142)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotUsnJournalCorruptCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка HMM & Viterbi State Sequence Optimization...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHiddenMarkovModelViterbiCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Java Record Component Injections...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordComponentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType vmtx/VORG Vertical Metrics Tampering...", ManualChecks.RunWindowsFontTableCacheRegistryFontVmtxTableForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Indirect Count Buffer (ARB_indirect_parameters)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshMultiDrawCountBufferCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot DeviceIoControl IOCTL Failures...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotDeviceIoControlFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Recurrent Neural Network (LSTM/GRU) Click Weights...", ManualChecks.RunMinecraftPlayerCombatAutoClickerRecurrentNeuralNetworkLstmCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Sealed PermittedSubclasses...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicPermittedSubclassesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка OpenType CFF INDEX & Subroutine Payloads...", ManualChecks.RunWindowsFontTableCacheRegistryFontCffIndexDataForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Shader Storage Buffer Objects (SSBO)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshShaderStorageBufferObjectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка VSS Diff Area Starvation & Settings Tampering...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotDiffAreaMinFreeSpaceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Reinforcement Learning Q-Table & Policy Gradient...", ManualChecks.RunMinecraftPlayerCombatAutoClickerReinforcementLearningQTableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin NestHost & NestMembers Access...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicNestHostBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType COLR/CPAL Color Palettes & HUDs...", ManualChecks.RunWindowsFontTableCacheRegistryFontColrCpalPaletteForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Uniform Buffer Objects (UBO Matrix)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshUniformBufferObjectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка VSS Writer Freeze Timeout & Abort (Event 5002)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotWriterTimeoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Genetic Algorithm Click Chromosome Evolution...", ManualChecks.RunMinecraftPlayerCombatAutoClickerGeneticAlgorithmChromosomeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic Bootstrap Methods (BSM)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicBootstrapMethodCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType fvar/cvar Variable Font Tuple Deltas...", ManualChecks.RunWindowsFontTableCacheRegistryFontCvarFvarVariableFontCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Atomic Counter Buffers (Occlusion Bypass)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshAtomicCounterBufferCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка VSS Registry Writer Freeze Deadlocks (Event 8193)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotRegistryWriterFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Simulated Annealing & Metropolis Click Timing...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSimulatedAnnealingMetropolisCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin MethodHandle.invokeExact...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodHandleInvokeExactCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType HVAR/VVAR Variation Metrics...", ManualChecks.RunWindowsFontTableCacheRegistryFontHvarVvarDeltaGlyphMetricsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Transform Feedback Buffer (Vertex Hijack)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshTransformFeedbackBufferCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot Cluster Allocation Failures...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotClusterAllocationFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Geometric Brownian Motion & Wiener Process...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBrownianMotionWienerProcessCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin VarHandle & MemorySegment...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicVarHandleMemorySegmentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType MVAR Global Metrics Variations...", ManualChecks.RunWindowsFontTableCacheRegistryFontMvarMetricsVariationsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium GPU Mesh Shader Pipeline (Nvidium)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshMeshShaderCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot Disk Bad Blocks (Event 7/15)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotHarddiskBadSectorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Lévy Flight & Heavy-Tailed Click Spacing...", ManualChecks.RunMinecraftPlayerCombatAutoClickerLevyFlightHeavyTailCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic ClassDefiner & Hidden Classes...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicClassDefinerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType BASE Baseline Table Coordinate Shifts...", ManualChecks.RunWindowsFontTableCacheRegistryFontBaseTableBaselineForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Vulkan Ray Tracing Acceleration Structures...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanRayTracingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot Paging IO Starvation (Event 50/51)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotPagingIoStarvationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Non-Homogeneous Poisson Point Process Clusters...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPoissonPointProcessClusterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin constantdynamic (condy Indy)...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicConstantDynamicIndyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType JSTF Text Layout Expansion...", ManualChecks.RunWindowsFontTableCacheRegistryFontJstfJustificationForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium MeshOptimizer Vertex Decimation...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshMeshOptimizerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot Sparse Zero-Allocation...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotSparseFileAllocationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Extreme Value Theory (EVT/Fréchet) CPS...", ManualChecks.RunMinecraftPlayerCombatAutoClickerExtremeValueTheoryFrechetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin MethodParameters Obfuscation...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodParametersAttributeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType MATH Table Layout & Radar Matrices...", ManualChecks.RunWindowsFontTableCacheRegistryFontMathTableEquationForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Vulkan Bindless Descriptor Indexing...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorIndexingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot ReFS Metadata Transaction Desync...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataCorruptCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Multivariate Gaussian Copula Joint Click Delays...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMultivariateGaussianCopulaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin JPMS Module Opens & Exports...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicModuleExportOpenBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType EBLC/EBDT Embedded Bitmap Glyphs...", ManualChecks.RunWindowsFontTableCacheRegistryFontEblcEbdtBitmapGlyphForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Sampler Texture LOD Bias (Clean Chams)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshDynamicSamplerAnisotropyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка VSS BitLocker Key Rebind Errors (Event 24588)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotBitLockerMetadataRebindCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка HMM Baum-Welch Forward-Backward Smoothing...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHiddenMarkovModelForwardBackwardSmoothingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin Dynamic MethodType Lookups...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicMethodTypeLookupCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType bloc/bdat Small Bitmap Glyphs...", ManualChecks.RunWindowsFontTableCacheRegistryFontSbitSmallGlyphBitmapForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium GLSL Fragment Shader discard Hooks...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshFragmentDiscardOverrideCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot ReFS Checkpoint Errors (Event 141)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка ARMA/ARIMA Time Series Click Modeling...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAutoregressiveMovingAverageArmaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Fabric Mixin ClassLoader Constraint Violations...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicClassLoadingConstraintCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка TrueType gvar Outline Warping & Radar Vectors...", ManualChecks.RunWindowsFontTableCacheRegistryFontGvarGlyphVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Vulkan Subpass G-Buffer Depth Reading...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSubpassInputCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Volume Snapshot NVMe TRIM Starvation (Event 154)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotTrimStarvationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Stochastic Resonance & Sub-Threshold Noise...", ManualChecks.RunMinecraftPlayerCombatAutoClickerStochasticResonanceNoiseCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка хуков сериализации Java Record-пакетов...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordSerializationHookCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблиц OpenType fmtx и скрытых шрифтовых оверлеев...", ManualChecks.RunWindowsFontTableCacheRegistryFontFmtxFontMetricsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sodium Vulkan Sparse Binding декоммита мешей...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSparseBindingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка сбоев контрольных сумм ReFS метаданных VSS (Event 133)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataChecksumMismatchCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка оператора энергии Тигера-Кайзера (TKEO)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerNonlinearEnergyOperatorTeagerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка деконструкторов Record Patterns в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordPatternMatchingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка пар кернинга kern в OpenType шрифтах...", ManualChecks.RunWindowsFontTableCacheRegistryFontKernTablePairAdjustmentForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка шаблонов дескрипторов Vulkan (DescriptorUpdateTemplate)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorUpdateTemplateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка сброса снимков VSS через релокацию кластеров ReFS (Event 148)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocatedClusterDefragCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка показателя Херста кликов (R/S Rescaled Range)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHurstExponentRescaledRangeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка компактных конструкторов Record в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordCompactConstructorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы GSUB лигатурной подстановки Chams...", ManualChecks.RunWindowsFontTableCacheRegistryFontGsubLigatureChamsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Push Constants в рендерере мешей Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPushConstantWallhackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка сбоев разреженных томов ReFS/VSS (Event 156)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSparseVvolAllocationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка флуктуационного анализа без тренда (DFA)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerDetrendedFluctuationAnalysisDfaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка прокси сериализации Record-пакетов в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordCanonicalSerializationProxyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы GPOS якорного позиционирования...", ManualChecks.RunWindowsFontTableCacheRegistryFontGposAnchorPositioningForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Dynamic Rendering в рендерере мешей Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicRenderingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка транзакционных откатов ReFS/VSS (Event 161)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTransactionRollbackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка фрактальной размерности Хигучи (HFD)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHiguchiFractalDimensionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка обхода аксессоров Record через Reflection в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordReflectionAccessorBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка подтаблицы cmap Format 14 селекторов вариантов...", ManualChecks.RunWindowsFontTableCacheRegistryFontCmapFormat14UnicodeVariationSelectorsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Subgroup Operations в рендерере мешей Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSubgroupOperationsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка сбоев контрольных точек ReFS/VSS (Event 166)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointAllocationFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка рекуррентного анализа фазового пространства (RQA)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerRecurrenceQuantificationAnalysisRqaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы BootstrapMethods в Record-пакетах Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordBootstrapMethodAttributeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы VDMX квантованных вертикальных метрик...", ManualChecks.RunWindowsFontTableCacheRegistryFontVdmxVerticalDeviceMetricsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Partially Bound дескрипторов в Sodium/Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorIndexingPartiallyBoundCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка повреждений потоков метаданных ReFS/VSS (Event 171)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataStreamCorruptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка выборочной энтропии кликов (SampEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSampleEntropySampEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Type Annotations в Record-пакетах Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordTypeAnnotationInjectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка смарт-шрифтов SIL Graphite (Glat/Gloc)...", ManualChecks.RunWindowsFontTableCacheRegistryFontGlatGlocGraphiteSmartFontForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Descriptor Buffer в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка сбоев ремаппинга кластеров ReFS/VSS (Event 176)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocatedClusterRemapFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка нечеткой энтропии кликов (FuzzEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerFuzzyEntropyFuzzEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Generic Signatures в Record-пакетах Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordGenericSignatureTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы EBSC масштабирования растровых глифов...", ManualChecks.RunWindowsFontTableCacheRegistryFontSbitBitmapScalingChamsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка VkDescriptorBufferBindingInfoEXT в Sodium/Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferBindingInfoCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка рассинхронизации деревьев ReFS/VSS (Event 181)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataCheckpointTreeDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка перестановочной энтропии кликов (PermEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPermutationEntropyPermEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка исчерпываемости Record Patterns в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordPatternExhaustivenessBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы Feat пользовательских функций...", ManualChecks.RunWindowsFontTableCacheRegistryFontFeatFeatureTableForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Embedded Samplers в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferEmbeddedSamplerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка повреждений списков экстентов ReFS/VSS (Event 186)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataExtentListCorruptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка спектральной энтропии кликов (SpecEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSpectralEntropySpecEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка условий Pattern Guards в Record-пакетах Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordPatternGuardsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы Sill языкового маппинга оверлеев...", ManualChecks.RunWindowsFontTableCacheRegistryFontSillLanguageSupervisionForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Push Descriptors в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferPushDescriptorsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка битовых карт распределения блоков ReFS/VSS (Event 191)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataAllocatedBlockBitmapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка вейвлет-энтропии кликов (WavEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerWaveletEntropyWavEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка деконструкции Record Patterns в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordDeconstructionPatternAccessorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы vhea вертикального макета для трассеров...", ManualChecks.RunWindowsFontTableCacheRegistryFontVheaVerticalHeaderForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Mutable Descriptors в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferMutableDescriptorsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка потери кворума суперблоков ReFS/VSS (Event 196)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataSuperblockQuorumCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка условной энтропии кликов (CondEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerConditionalEntropyCondEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка срезания флагов ACC_SYNTHETIC в Record-пакетах...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordSyntheticAccessorStrippingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы vmtx вертикальных метрик глифов...", ManualChecks.RunWindowsFontTableCacheRegistryFontVmtxVerticalMetricsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Inline Uniform Blocks в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferInlineUniformCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка повреждений таблиц аллокаторов ReFS/VSS (Event 201)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataAllocatorTableDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка трансферной энтропии кликов (TransEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerTransferEntropyTransEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка вложенных Record Patterns в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicRecordPatternNestedComponentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы VORG вертикальных координат начала...", ManualChecks.RunWindowsFontTableCacheRegistryFontVorgVerticalOriginForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Sampler Heap в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDescriptorBufferSamplerHeapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка переполнения журналов ReFS/VSS (Event 206)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataCheckpointLogOverflowCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка многомасштабной энтропии кликов (MSE)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMultiscaleEntropyMseCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка аннотаций RecordComponent в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentAnnotationReflectionBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы BASE базовых линий шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontBaseBaselineForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка сбоев ReFS Scrubber (Event 211)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSScrubberExtentRemapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка перекрестной энтропии кликов (Cross-SampEn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCrossSampleEntropyCrossSampEnCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка канонических конструкторов Record в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinRecordCanonicalConstructorParamCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы MATH математической верстки шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontMathTypesettingForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Dedicated Allocation в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDedicatedAllocationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка сбросов транзакций ReFS MinStore (Event 216)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMinStoreTransactionAbortCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка дивергенции Кульбака-Лейблера кликов (D_KL)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerRelativeEntropyKullbackLeiblerDklCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Generic-сигнатур Record компонентов в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentGenericSignatureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы MVAR вариаций метрик шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontMvarMetricsVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Fragment Shading Rate (VRS) в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanFragmentShadingRateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка повреждений экстентов B+ дерева ReFS (Event 221)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSBTreeMultiLevelExtentCorruptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка дивергенции Дженсена-Шеннона кликов (JSD)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerJensenShannonDivergenceJsdCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка dynamic CallSite в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleInvokerCallSiteCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы HVAR горизонтальных метрик шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontHvarHorizontalMetricsVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Mesh Shaders (VK_EXT_mesh_shader)...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanMeshShaderCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка аллокатора контейнеров ReFS (Event 226)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSContainerAllocatorDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка расстояния Васерштейна кликов (W1 / EMD)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerWassersteinDistanceW1Check),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка скрытых мутабельных полей в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentHiddenFieldCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы VVAR вертикальных метрик шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontVvarVerticalMetricsVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Multi-Draw Indirect в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanMultiDrawIndirectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка поколений чекпоинтов ReFS (Event 231)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointGenerationDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка энергетического расстояния Крамера кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerEnergyDistanceCramerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка обхода VarHandle в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentVarHandleCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы STAT стилевых осей шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontStatStyleAttributesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Sampler Ycbcr Conversion в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSamplerYcbcrConversionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка больших кластеров ReFS (Event 236)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSLargeClusterExtentRelocationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка ядерного теста расхождения MMD кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMaximumMeanDiscrepancyMmdCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка LambdaMetafactory в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLambdaMetafactoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы cvar вариаций хинтинга шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontCvarCvtVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Sampler Filter Minmax в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSamplerFilterMinmaxCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка многоуровневых хранилищ ReFS (Event 241)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTieredStorageMigrationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка марковских переходов кликов (TVD)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerTotalVariationDistanceMarkovCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка ConstantDynamic (condy) в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentConstantDynamicBootstrapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы gvar кортежей дельт шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontGvarTupleDeltaForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Depth Clamp в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthClampCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблиц Allocate-On-Write ReFS (Event 246)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocateOnWriteDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка расстояния Хеллингера кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHellingerDistanceBhattacharyyaCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка unreflectSpecial в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentUnreflectSpecialCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таблицы avar нелинейных осей шрифтов...", ManualChecks.RunWindowsFontTableCacheRegistryFontAvarAxisVariationsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Color Blend Factors в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanColorBlendStateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка кворума суперблоков ReFS (Event 251)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSuperblockQuorumLossCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка расстояния Фреше кривых клика (dF)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerFrechetDistanceDiscreteCurveCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка dropArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleDropArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка отладочных таблиц TSI в шрифтах...", ManualChecks.RunWindowsFontTableCacheRegistryFontTsiDebugStreamForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Polygon Mode Wireframe в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPolygonModeWireframeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка сбоев наката журнала ReFS (Event 256)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMultiVolumeJournalCrashCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка критерия Колмогорова-Смирнова (D_KS)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerKolmogorovSmirnovDksCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка filterReturnValue в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentFilterReturnValueCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка локализованных фич GSUB locl в шрифтах...", ManualChecks.RunWindowsFontTableCacheRegistryFontLoclFeatureSpoofingForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Point Mode в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPointSizePrimitiveRestartCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка таймаутов барьера ReFS (Event 261)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCommitBarrierTimeoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка критерия Андерсона-Дарлинга (A2)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAndersonDarlingA2Check),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка foldArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleFoldArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка композиции глифов GSUB ccmp в шрифтах...", ManualChecks.RunWindowsFontTableCacheRegistryFontCcmpGlyphCompositionForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Depth Bias Slope Factor в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthBiasSlopeFactorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка коллизий стейджинга ReFS (Event 266)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSStagingAreaCollisionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка критерия Крамера-фон Мизеса (T_CvM)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCramerVonMisesTCvMCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка guardWithTest в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleGuardWithTestCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка обязательных альтернатив GSUB rclt...", ManualChecks.RunWindowsFontTableCacheRegistryFontRcltRequiredContextualAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Stencil Reference Ops в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanStencilReferenceCompareOpCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка дедлоков перемещения ReFS (Event 271)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMultiTierRelocationLogAbortCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка критерия Ватсона круговых фаз (U2)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerWatsonU2CircularDistributionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка catchException в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleCatchExceptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка контекстных росчерков GSUB cswh...", ManualChecks.RunWindowsFontTableCacheRegistryFontCswhContextualSwashForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Vulkan Depth Bounds Test в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthBoundsTestCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка дедлоков аллокатора ReFS (Event 276)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSContainerFragmentationDeadlockCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка критерия Койпера циклических фаз (V)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerKuiperTestCyclicCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка tryFinally в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleTryFinallyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка произвольных лигатур GSUB dlig...", ManualChecks.RunWindowsFontTableCacheRegistryFontDligDiscretionaryLigaturesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Conservative Rasterization в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanConservativeRasterizationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка разреженных экстентов ReFS VVol (Event 281)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSparseVVolExtentRollbackCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка индекса Джини кликов (Gini Impurity)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerGiniImpurityDecisionBoundaryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка комбинаторов loop в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleLoopCombinatorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка исторических лигатур GSUB hlig...", ManualChecks.RunWindowsFontTableCacheRegistryFontHligHistoricalLigaturesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка Sample Locations в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSampleLocationsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка корня дерева чекпоинтов ReFS (Event 286)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeRootDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0d Проверка коэффициента силуэта кликов (s)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSilhouetteCoefficientClusterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка фильтров аргументов MethodHandles.filterArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleFilterArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка контекстных альтернатив GSUB calt...", ManualChecks.RunWindowsFontTableCacheRegistryFontCaltContextualAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Conditional Rendering в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanConditionalRenderingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка рассинхронизации зеркалирования ReFS (Event 291-295)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTieredResiliencyDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка индексов кластеризации кликов (DBI & Dunn)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerDaviesBouldinClusterSeparationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка перестановки аргументов MethodHandles.permuteArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandlePermuteArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка стилистических альтернатив GSUB salt...", ManualChecks.RunWindowsFontTableCacheRegistryFontSaltStylisticAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка барицентрических координат во фрагментных шейдерах...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanBarycentricCoordinatesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка битовых карт аллокатора ReFS (Event 296-300)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocatedSpaceBitmapDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка индекса Калински-Харабаша кликов (VRC)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCalinskiHarabaszIndexCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка комбайнеров foldArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleFoldArgumentsWithCombinerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка титульных альтернатив GSUB titl...", ManualChecks.RunWindowsFontTableCacheRegistryFontTitlTitlingAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка кастомных границ сэмплеров в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanCustomBorderColorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка коллапса высоты B+ дерева ReFS (Event 301-305)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSBTreeHeightCollapseDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка индекса конкордантности кликов (C-Index)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerConcordanceIndexHazardCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка упаковки аргументов MethodHandles.collectArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleCollectArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка малых прописных GSUB smcp...", ManualChecks.RunWindowsFontTableCacheRegistryFontSmcpSmallCapitalsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка растеризации линий в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanLineRasterizationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка рассогласования эпох ReFS (Event 306-310)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSEpochSkewDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка индекса Брайера кликов (BS)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBrierScoreReliabilityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка распаковки аргументов MethodHandles.spreadInvoker в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleSpreadInvokerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка капители GSUB c2sc...", ManualChecks.RunWindowsFontTableCacheRegistryFontC2scCapitalsToSmallCapitalsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка динамических вершинных буферов в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanVertexInputDynamicStateCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка разрывов LSN журнала ReFS (Event 311-315)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSLsnWrapDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка лог-правдоподобия Вальда кликов (SPRT)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSprtLogLikelihoodRatioCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка трамплинов identity/constant в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleIdentityConstantTrampolineCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка шрифтов unicase GSUB unic...", ManualChecks.RunWindowsFontTableCacheRegistryFontUnicUnicaseForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Extended Dynamic State 2 в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanExtendedDynamicState2Check),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка счетчиков поколений ReFS (Event 316-320)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSGenerationCounterWrapDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка кумулятивных сумм CUSUM кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCusumChangePointDetectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка адаптеров zero/empty в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleZeroEmptyAdaptersCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка исторических форм GSUB hist...", ManualChecks.RunWindowsFontTableCacheRegistryFontHistHistoricalFormsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Extended Dynamic State 3 в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanExtendedDynamicState3Check),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка чексумм чекпоинтов ReFS (Event 321-325)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointChecksumMismatchCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка волатильности EMA кликов (EVR)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerEmaVolatilityRatioCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка усечения аргументов explicitCastArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleExplicitCastArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка курсивных альтернатив GSUB ital...", ManualChecks.RunWindowsFontTableCacheRegistryFontItalItalicsAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка тайминг-сайдканалов Shader Clock в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanShaderClockCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка дедлоков компактификации ReFS (Event 326-330)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCompactionDeadlockCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка спектральной чистоты Фурье кликов (SFDR)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerFftSpectralPurityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка приведения типов asType в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleAsTypeCoercionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка руби-нотации GSUB ruby...", ManualChecks.RunWindowsFontTableCacheRegistryFontRubyRubyNotationForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка перезапуска топологии примитивов в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPrimitiveTopologyRestartCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка таймаутов барьеров записи ReFS (Event 331-335)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSWriteBarrierTimeoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка скейлинга флуктуаций DFA кликов (alpha)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerDfaScalingExponentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка диспетчеров exactInvoker в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleExactInvokerDispatchCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка альтернативных аннотаций GSUB nalt...", ManualChecks.RunWindowsFontTableCacheRegistryFontNaltAlternateAnnotationForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка микрокарт прозрачности Opacity Micromap в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanOpacityMicromapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка межтомовых барьеров ReFS (Event 336-340)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMultiVolumeBarrierDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка автокорреляции Льюнга-Бокса кликов (Q)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerLjungBoxAutocorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка динамических вызовов invokeWithArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleInvokeWithArgumentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка индексов sups/subs...", ManualChecks.RunWindowsFontTableCacheRegistryFontSupsSubsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Multisampled Render в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanMultisampledRenderSingleSampledCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка зависаний флеша ReFS (Event 341-345)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSWriteStallStarvationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка ошибки прогноза MASE кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMaseForecastCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка каррирования MethodHandles.insertArguments в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentMethodHandleInsertArgumentsBindingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вертикальных таблиц OpenType vrt2...", ManualChecks.RunWindowsFontTableCacheRegistryFontVrt2VerticalAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка слияния субпассов Vulkan в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSubpassMergeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка таймаутов сброса чекпоинтов ReFS (Event 346-350)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointDrainTimeoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Энгла ARCH на волатильность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerEngleArchTestHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка динамических прокси altMetafactory в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLambdaAltMetafactoryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка таблиц порядковых форм OpenType ordn...", ManualChecks.RunWindowsFontTableCacheRegistryFontOrdnOrdinalFormsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка маскирования Color Write Enable в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanColorWriteEnableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка срывов синхронизации аллокатора ReFS (Event 351-355)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocatorSyncAbortCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка критерия Дарбина-Уотсона (DW) остатков кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerDurbinWatsonAutocorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка динамической конкатенации StringConcatFactory в Fabric...", ManualChecks.RunMinecraftFabricLoaderMixinDynamicStringConcatFactoryRecipeInjectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка таблиц табличных чисел OpenType tnum...", ManualChecks.RunWindowsFontTableCacheRegistryFontTnumTabularFiguresForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Advanced Blend Chams в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanAdvancedBlendOperationsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка сброса флеша ярусов ReFS (Event 356-360)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTierFlushDropoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста отношения дисперсий Ло-МакКинли (VR)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerLoMacKinlayVarianceRatioCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка синтеза номинальных дескрипторов Constants API...", ManualChecks.RunMinecraftFabricLoaderMixinNominalDescriptorConstantsApiCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка таблиц старостильных чисел OpenType onum...", ManualChecks.RunWindowsFontTableCacheRegistryFontOnumOldstyleFiguresForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка провоцирующей вершины Provoking Vertex в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanProvokingVertexCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка рассинхронизации эпох контейнеров ReFS (Event 361-365)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSContainerEpochSkewCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Дики-Фуллера (ADF) на стационарность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAugmentedDickeyFullerStationarityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов unreflectConstructor в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentUnreflectConstructorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка пропорциональных чисел OpenType pnum...", ManualChecks.RunWindowsFontTableCacheRegistryFontPnumProportionalFiguresForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Depth Clip Enable в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthClipEnableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка голодания пула буферов ReFS (Event 366-370)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSEvictionStarvationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка тренда утомления Тейла-Сена и Кендалла (Tau)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerTheilSenKendallTrendCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка unreflectGetter/Setter в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentUnreflectGetterSetterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка выравненных чисел OpenType lnum...", ManualChecks.RunWindowsFontTableCacheRegistryFontLnumLiningFiguresForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка делителя вершинных атрибутов в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanVertexAttributeDivisorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка тупиков дефрагментации ReFS (Event 371-375)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSDefragDeadlockCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка критерия Филлипса-Перрона (PP) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPhillipsPerronStationarityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка телепортации контекста Lookup.in в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupClassTeleportCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка полуширинных форм OpenType hwid...", ManualChecks.RunWindowsFontTableCacheRegistryFontHwidHalfWidthFormsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка подавления сброса глубины в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanLoadStoreOpNoneCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка регрессий LSN журнала ReFS (Event 376-380)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSLsnRegressionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка критерия Бреуша-Пагана (BP) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBreuschPaganHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка эскалации allowedModes в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupAllowedModesTamperingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка полноширинных форм OpenType fwid...", ManualChecks.RunWindowsFontTableCacheRegistryFontFwidFullWidthFormsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка петли обратной связи аттачментов в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanAttachmentFeedbackLoopCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка таймаутов четности ReFS (Event 381-385)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSParityResyncTimeoutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка общего теста Уайта (LM) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerWhiteGeneralHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка внедрения классов Lookup.defineClass в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка регистрозависимых форм OpenType case...", ManualChecks.RunWindowsFontTableCacheRegistryFontCaseCaseSensitiveFormsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка зон отсечения Discard Rectangles в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDiscardRectanglesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка рассинхронизации барьеров ReFS (Event 386-390)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSJournalBarrierDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка F-теста Голдфелда-Квандта (GQ) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerGoldfeldQuandtVarianceRatioCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.findSpecial в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindSpecialBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка контекстных интервалов OpenType chws...", ManualChecks.RunWindowsFontTableCacheRegistryFontChwsContextualSpacingForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Depth Clamp Zero One в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthClampZeroOneCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка рассинхронизации эпох пула ReFS (Event 391-395)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSEpochDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Брок-Дехерт-Шейнкмана (BDS) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBdsNonLinearChaosCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.findStaticSetter в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindStaticSetterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка полуширинных метрик OpenType halt...", ManualChecks.RunWindowsFontTableCacheRegistryFontHaltAlternateHalfWidthsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Shader Subgroups в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanShaderSubgroupControlFlowCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка зависаний барьеров записи ReFS (Event 396-400)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSWriteBarrierStallCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка критерия Бартлетта (B) однородности дисперсий кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBartlettHomogeneityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.findConstructor в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindConstructorCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вертикальных полуширинных метрик OpenType vhal...", ManualChecks.RunWindowsFontTableCacheRegistryFontVhalVerticalHalfMetricsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка карт плотности фрагментов в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanFragmentDensityMapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка сброса кэша ярусов ReFS (Event 401-405)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTierCacheEvictionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка робастного критерия Ливиня (W) кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerLeveneEqualityOfVariancesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.findGetter/Setter в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindGetterSetterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вертикального кернинга OpenType vkrn...", ManualChecks.RunWindowsFontTableCacheRegistryFontVkrnVerticalKerningForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка 8-битных индексов в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanIndexTypeUint8Check),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка голодания конвейера записи ReFS (Event 406-410)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSPipelineStarvationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста CUSUM of Squares на сдвиги дисперсии кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCusumOfSquaresStructuralBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.findVirtual в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindVirtualCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка математических греческих форм OpenType mgrk...", ManualChecks.RunWindowsFontTableCacheRegistryFontMgrkMathematicalGreekForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Border Color Swizzle в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanBorderColorSwizzleCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка рассинхронизации чекпоинтов ReFS (Event 411-415)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointSyncDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Парка на степенную дисперсию кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerParkPowerLawHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.unreflectVarHandle в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupUnreflectVarHandleCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка пропорциональных форм Kana OpenType pkna...", ManualChecks.RunWindowsFontTableCacheRegistryFontPknaProportionalKanaForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Image View Min LOD в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanImageViewMinLodCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка повреждений деревьев чекпоинтов ReFS (Event 416-420)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeCorruptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Глейзера на гетероскедастичность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerGlejserHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.findStaticVarHandle в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupFindStaticVarHandleCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка случайных вариантов OpenType rand...", ManualChecks.RunWindowsFontTableCacheRegistryFontRandRandomAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Graphics Pipeline Library в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanGraphicsPipelineLibraryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка повреждений аллокатора ReFS (Event 421-425)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSAllocatorRunCorruptCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Харрисона-МакКейба на гетероскедастичность...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHarrisonMcCabeHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.unreflectConstructor в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupUnreflectConstructorBypassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вариантов расчерков OpenType swsh...", ManualChecks.RunWindowsFontTableCacheRegistryFontSwshSwashAlternatesForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Dynamic Rendering Unused Attachments в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicRenderingUnusedAttachmentsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка сбоев синхронизации таблиц ReFS (Event 426-430)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataTableSyncFailureCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Рамсея (RESET) на нелинейный джиттер...", ManualChecks.RunMinecraftPlayerCombatAutoClickerRamseyRESETNonLinearHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.unreflectSetter в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupUnreflectStaticSetterCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка орнаментов OpenType ornm...", ManualChecks.RunWindowsFontTableCacheRegistryFontOrnmOrnamentsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Fragment Shader Interlock в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanFragmentShaderInterlockCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка сбоев ребалансировки деревьев ReFS (Event 431-435)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeRebalanceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Харви-Коллиера на стабильность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHarveyCollierHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.unreflectGetter в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupUnreflectGetterPrivateFinalCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка курсивных связей OpenType curs...", ManualChecks.RunWindowsFontTableCacheRegistryFontCursCursiveAttachmentForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Subgroup Size Control в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanSubgroupSizeControlCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка повреждений карты кластеров ReFS (Event 436-440)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSClusterMapCorruptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Бокса-Пирса на автокорреляцию кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBoxPierceNonParametricHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.defineHiddenClass в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineHiddenClassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка дистанционных смещений OpenType dist...", ManualChecks.RunWindowsFontTableCacheRegistryFontDistDistanceGlyphPositioningForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Shader Demote to Helper в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanShaderDemoteToHelperInvocationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка сбоев обрезки узлов ReFS (Event 441-445)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreePruneCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Бреуша-Годфри (LM) на автокорреляцию кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBreuschGodfreyLaggedAutocorrelationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.defineClass в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageTeleportCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка диакритических смещений OpenType abvm...", ManualChecks.RunWindowsFontTableCacheRegistryFontAbvmAboveBaseMarkForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Primitives Generated Query в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPrimitivesGeneratedQueryCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка контрольных сумм записей журнала ReFS (Event 446-450)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataLogRecordChecksumCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Маклеода-Ли на условную гетероскедастичность...", ManualChecks.RunMinecraftPlayerCombatAutoClickerMcLeodLiARCHHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.ensureInitialized в Fabric Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupEnsureInitializedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка подстрочных диакритик OpenType blwm...", ManualChecks.RunWindowsFontTableCacheRegistryFontBlwmBelowBaseMarkForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Non-Seamless Cube Map в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanNonSeamlessCubeMapCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка рассинхронизации листьев деревьев ReFS (Event 451-455)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeLeafDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста серий Вальда-Вольфовица на случайность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerWaldWolfowitzRunsHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.defineClass экспорта модулей...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassModuleExportCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка многоуровневых меток OpenType mkmk...", ManualChecks.RunWindowsFontTableCacheRegistryFontMkmkMarkToMarkPositioningForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Update-After-Bind дескрипторов в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanUpdateAfterBindXRayCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка рассинхронизации родительских узлов ReFS (Event 456-460)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeParentDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста рангов Бартельса на случайность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerBartelsRankVonNeumannHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка вызовов Lookup.defineClass открытия модулей...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassModuleOpensCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка пропорциональных ширин OpenType pwid...", ManualChecks.RunWindowsFontTableCacheRegistryFontPwidProportionalWidthsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Depth Bias Control в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDepthBiasControlCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка коллизий узлов ветвления ReFS (Event 461-465)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSBranchNodeCollisionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Харке-Бера на нормальность остатков...", ManualChecks.RunMinecraftPlayerCombatAutoClickerJarqueBeraResidualNormalityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass и ProtectionDomain...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassProtectionDomainCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка OpenType sinf Scientific Inferiors...", ManualChecks.RunWindowsFontTableCacheRegistryFontSinfScientificInferiorsForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Rasterization Order Attachment Access в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanRasterizationOrderAttachmentAccessCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка ReFS Metadata Stream Checksum (Event 466-470)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataStreamChecksumDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Кохрана-Армитеджа на тренд кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerCochranArmitageTrendHeteroskedasticityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass и графа чтения модулей...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassModuleReadsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss01...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs01StylisticSet1ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Pipeline Robustness в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanPipelineRobustnessCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка рассинхрона Root Dir ReFS (Event 471-475)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSRootDirDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка критерия Шапиро-Франсиа на нормальность кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerShapiroFranciaNormalityCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass регистрации SPI-сервисов...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassModuleProvideServiceCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss02...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs02StylisticSet2ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка калибровки таймингов Vulkan Timestamps...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanCalibratedTimestampsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка рассинхрона Sparse ReFS (Event 476-480)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSparseAllocClusterDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Чоу на структурный сдвиг кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerChowTestStructuralBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass динамических сервисов...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassModuleProvideDynamicTargetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss03...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs03StylisticSet3ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка сброса окклюзии Host Query Reset...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanHostQueryResetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка коллизий Object ID ReFS (Event 481-485)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSObjectIdAllocCollisionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Зивота-Эндрюса на слом ряда кликов...", ManualChecks.RunMinecraftPlayerCombatAutoClickerZivotAndrewsUnitRootBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass экспорта модулей...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportDynamicModuleCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss04...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs04StylisticSet4ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка выравнивания Texel Buffer в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanTexelBufferAlignmentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка конфликтов транзакций ReFS (Event 486-490)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTransactionConflictDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка критерия Эндрюса-Плобергера Sup-F...", ManualChecks.RunMinecraftPlayerCombatAutoClickerAndrewsPlobergerOptimalSupFCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка ServiceLoader инъекций в Mixin...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportServiceLoaderInjectionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss05...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs05StylisticSet5ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Dynamic State 3 в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3RasterizationSamplesCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Security Descriptor ReFS (Event 491-495)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSecurityDescriptorDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Плобергера Avg-F на нестабильность...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPlobergerExponentialWaldAvgFCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass загрузки агентов...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportInstrumentationAgentCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss06...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs06StylisticSet6ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Polygon Mode в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3PolygonModeCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка рассинхрона USN ReFS (Event 496-500)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSUSNJournalDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Ниблома на постоянство параметров...", ManualChecks.RunMinecraftPlayerCombatAutoClickerNyblomParameterStabilityLMCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass и Unsafe телепортации...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeTeleportCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss07...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs07StylisticSet7ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Color Blend Enable в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3ColorBlendEnableCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка экстентов ReFS (Event 501-505)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSSparseFileExtentDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Хансена на параметрическую нестабильность...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHansenInstabilityL2TestCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass и смещений Unsafe...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeFieldOffsetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss08...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs08StylisticSet8ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Color Blend Equation в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3ColorBlendEquationCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка усечения метаданных ReFS (Event 506-510)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataStreamTruncateDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка теста Эллиотта-Ротенберга-Стока DF-GLS...", ManualChecks.RunMinecraftPlayerCombatAutoClickerElliottMullerStockEfficientUnitRootCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass и Unsafe.putAddress...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeMemoryTeleportCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss09...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs09StylisticSet9ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Color Write Mask в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3ColorWriteMaskCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка сброса коммитов ReFS (Event 511-515)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSTransactionCommitAbortDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка модели аддитивных выбросов Перрона AO...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPerronAdditiveOutlierBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass и Unsafe.park...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeParkCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss10...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs10StylisticSet10ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Logic Op в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3LogicOpCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка сбоев Prune ReFS (Event 516-520)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreePruneFailureDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка модели инновационных выбросов Перрона IO...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPerronInnovationalOutlierBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass и Unsafe.throw...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeThrowExceptionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss11...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs11StylisticSet11ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Color Blend Advanced в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3ColorBlendAdvancedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка сбоев расширения ReFS (Event 521-525)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSMetadataStreamExpansionFailureDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка критерия Перрона-Фогельсанга...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPerronVogelsangInnovationalOutlierBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass и Unsafe.putVolatile...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeVolatilePutCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss12...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs12StylisticSet12ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка коллизий листьев ReFS (Event 526-530)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeLeafCollisionDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка AO-критерия Перрона-Фогельсанга...", ManualChecks.RunMinecraftPlayerCombatAutoClickerPerronVogelsangAdditiveOutlierBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Lookup.defineClass и Unsafe.getVolatile...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentLookupDefineClassPackageOpensExportUnsafeVolatileGetCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss13...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs13StylisticSet13ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Stencil Op в Sodium Vulkan...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3StencilOpAdvancedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка сбоев расщепления ReFS (Event 531-535)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeLeafSplitFailureDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка коинтеграции Хансена-Йохансена...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHansenJohansenCointegrationBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Proxy и InvocationHandler в Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentInvocationHandlerProxyCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss14...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs14StylisticSet14ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Depth Bias/Clamp в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3DepthBiasClampAdvancedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка слияния листьев ReFS (Event 536-540)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeLeafMergeFailureDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка асимметрии ECM Энгла-Грейнджера...", ManualChecks.RunMinecraftPlayerCombatAutoClickerEngleGrangerErrorCorrectionAsymmetryBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка ClassLoader.definePackage в Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentClassLoaderDefinePackageCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss15...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs15StylisticSet15ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Rasterization Stream в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3RasterizationStreamPointSizeAdvancedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка утилизации листьев ReFS (Event 541-545)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeLeafRecycleFailureDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка TVECM Хансена-Сео...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHansenSeoThresholdVectorErrorCorrectionBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка подделки Manifest SEALED в Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentClassLoaderSealedManifestSpoofCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss16...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs16StylisticSet16ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Tessellation Domain в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3TessellationDomainOriginAdvancedCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка корня дерева ReFS (Event 546-550)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeRootIndexDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка SETAR-ECM Сео...", ManualChecks.RunMinecraftPlayerCombatAutoClickerSeoSelfExcitingThresholdCointegrationBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка URLStreamHandler в Record...", ManualChecks.RunMinecraftFabricLoaderMixinRecordComponentClassLoaderUrlStreamHandlerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка наборов глифов OpenType ss17...", ManualChecks.RunWindowsFontTableCacheRegistryFontSs17StylisticSet17ForensicsCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка Winding/Cull Mode в Sodium...", ManualChecks.RunMinecraftSodiumCustomBlockOcclusionTerrainMeshVulkanDynamicState3TessellationDomainControlPointSpacingCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udd0e Проверка корневых экстентов ReFS (Event 551-555)...", ManualChecks.RunWindowsVolumeShadowCopyStorageEventLogVolumeSnapshotReFSCheckpointTreeRootExtentAllocationDesyncCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc51 Проверка коинтеграции Хансена-Филлипса-Улиариса (1000-й метод!)...", ManualChecks.RunMinecraftPlayerCombatAutoClickerHansenPhillipsOuliarisMultivariateCointegrationRankBreakCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udca5 Анализ дампов памяти CrashDumps и подавления сервиса WER...", ManualChecks.RunWindowsWerLocalDumpsAndCrashSuppressionCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee1\ufe0f Аудит конфигураций Sodium на отключение Entity Culling для Chams...", ManualChecks.RunMinecraftSodiumStencilBufferAndRenderPassCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udf10 Аудит параметров TTL кэша DNS и скрытого файла hosts.ics...", ManualChecks.RunWindowsDnsCacheTtlAndHostsIcsRedirectCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfb5 Анализ аудиофайлов .ogg в ресурспаках на триггеры Sound-Clicker...", ManualChecks.RunMinecraftAudioReactiveClickerOggTriggerCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udcca Проверка активности системной базы мониторинга ресурсов SRUM...", ManualChecks.RunWindowsSrumNetworkByteTransfersAuditCheck),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udc31 MeowDoomsdayFucker: поиск Doomsday Client (ОЗУ/Prefetch/DNS/Файлы)...", SsToolsManager.ScanMeowDoomsdayForensics),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83e\uddf9 Clean Detecting: аудит батников очистки, клинеров и затирания следов...", SsToolsManager.ScanCleanDetectingForensics),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83d\udee0\ufe0f SSTool & Discord: аудит артефактов screenshare и загрузок...", SsToolsManager.ScanSsToolArtifacts),
				Tuple.Create<string, Func<List<CheckResult>>>("\ud83c\udfaf y_check: аудит координатных логов и скриптов проверки...", SsToolsManager.ScanYCheckArtifacts)
			};
			for (int i = 0; i < array.Length; i++)
			{
				if (ct.IsCancellationRequested)
				{
					break;
				}
				ReportStep(i + 1, array.Length, array[i].Item1);
				try
				{
					List<CheckResult> collection = array[i].Item2();
					if (collection != null) { scanReport.Results.AddRange(collection); }
				}
				catch (Exception ex)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Ошибки";
					checkResult.Name = "Ошибка проверки " + array[i].Item1;
					checkResult.Detail = ex.Message;
					checkResult.Severity = CheckSeverity.Info;
					scanReport.Results.Add(checkResult);
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
						list3.Add("⚠\ufe0f Корзина на диске " + driveInfo.Name + " была изменена прямо перед проверкой (" + directoryInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")");
					}
					string[] files2 = Directory.GetFiles(path2, "*", SearchOption.AllDirectories);
					string[] array2 = files2;
					foreach (string text2 in array2)
					{
						string text3 = Path.GetFileName(text2).ToLower();
						string[] cheatClientNames = CheatClientNames;
						foreach (string text4 in cheatClientNames)
						{
							if (MatchesCheatName(text3, text4))
							{
								list2.Add("Удалённый чит-файл в Корзине [" + text4 + "]: " + text2);
								break;
							}
						}
						string[] visualKeywords = VisualKeywords;
						foreach (string text5 in visualKeywords)
						{
							if (text3.Contains(text5))
							{
								list2.Add("Удалённый визуал в Корзине [" + text5 + "]: " + text2);
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
					string[] array3 = files3;
					foreach (string text6 in array3)
					{
						string textLower = Path.GetFileNameWithoutExtension(text6).ToLower();
						string[] cheatClientNames2 = CheatClientNames;
						foreach (string text7 in cheatClientNames2)
						{
							if (MatchesCheatName(textLower, text7))
							{
								list2.Add("Ярлык удалённой программы в Recent [" + text7 + "]: " + text6 + " (создан: " + File.GetLastWriteTime(text6).ToString("dd.MM.yyyy HH:mm") + ")");
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
			checkResult.Category = "Удалённые программы";
			checkResult.Name = "Следы удалённых программ, модов и очистки корзины";
			checkResult.Detected = list2.Count > 0 || list3.Count > 0;
			checkResult.Severity = CheckSeverity.Warning;
			checkResult.Detail = ((list2.Count > 0 || list3.Count > 0) ? ("Найдено следов удалённого ПО: " + list2.Count + " | Очисток корзины: " + list3.Count) : "Следов намеренного удаления читов перед проверкой не найдено");
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
								list2.Add("\ud83d\udfe3 [X-Ray архив по названию]: " + text2 + " (совпадение: " + text4 + ")");
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
											list2.Add("\ud83d\udfe3 [Прозрачный блок X-Ray] " + text6 + " весит " + entry.Length + " байт в: " + text2);
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
								list2.Add("\ud83d\udfe3 [X-Ray папка ресурспака]: " + text7 + " (совпадение: " + text9 + ")");
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
									list2.Add("\ud83d\udfe3 [Прозрачный блок X-Ray] " + text10 + " весит " + fileInfo.Length + " байт в: " + text13);
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
			checkResult.Evidence = ((list2.Count > 0) ? (string.Join("\r\n", list2.ToArray()) + "\r\n\r\nВсе установленные ресурспаки:\r\n" + string.Join("\r\n", list3.ToArray())) : ("Папка ресурспаков: " + text));
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
								list4.Add("\ud83d\udfe3 [X-Ray мод]: " + text14 + " (ключ: " + text16 + ")");
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
								list5.Add("\ud83d\udfe3 [ESP / Tracers мод]: " + text17 + " (ключ: " + text19 + ")");
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
								list6.Add("\ud83d\udfe3 [FreeCam / Камера мод]: " + text20 + " (ключ: " + text22 + ")");
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
								list7.Add("⚠\ufe0f В файле options.txt гамма выкручена выше нормы: gamma=" + result + " (норма: 0.0 - 1.0). Игрок видит в темноте без факелов!");
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
								list7.Add("\ud83d\udfe3 [FullBright мод]: " + text26 + " (ключ: " + text28 + ")");
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
								list8.Add("\ud83d\udfe3 [Миникарта/Радар]: " + text29 + " — проверить, разрешён ли пещерный радар/игроки на сервере");
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
							list2.Add("\ud83d\udd34 Чит-клиент по имени [" + text3 + "]: " + text2);
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
									list2.Add("\ud83d\udd34 Чит-класс боевой механики (Killaura/Aimbot) внутри: " + text2);
									break;
								}
							}
							if (flag)
							{
								list2.Add("\ud83d\udd34 Инжект-аддон addon3/addon4.json внутри: " + text2);
							}
							if (flag2 && fileInfo.Length > 20000 && fileInfo.Length < 10000000)
							{
								list2.Add("\ud83d\udd34 Сигнатура Doomsday чита внутри: " + text2);
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
								list.Add("\ud83d\udce6 Подозрительный пакет [" + text3 + "] в: " + jarPath + " → " + entry.FullName);
								break;
							}
						}
						foreach (string text4 in cheatClassNames)
						{
							if (text.Contains(text4.ToLower()))
							{
								list.Add("⚠\ufe0f Чит-класс [" + text4 + "] в: " + jarPath + " → " + entry.FullName);
								break;
							}
						}
						foreach (string text5 in suspiciousManifests)
						{
							if (text.EndsWith(text5.ToLower()))
							{
								list.Add("\ud83d\udcc4 Подозрительный манифест [" + text5 + "] в: " + jarPath + " → " + entry.FullName);
								break;
							}
						}
						if (text2.Length > 10)
						{
							CheatDatabases.BanRule[] array = CheatDatabases.MatchRegulationKeys(text2);
							CheatDatabases.BanRule[] array2 = array;
							foreach (CheatDatabases.BanRule banRule in array2)
							{
								list.Add("\ud83d\udeab [Регламент " + banRule.Id + "] " + banRule.Title + " в: " + jarPath + " → " + entry.FullName);
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
										list.Add("\ud83d\udccb [Регламент " + banRule2.Id + "] " + banRule2.Title + " в манифесте: " + jarPath + " → " + entry.Name);
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
							list2.Add(process.ProcessName + " (чит: " + text2 + ")");
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
			checkResult2.Detail = ((list3.Count > 0) ? ("Обнаружено: " + list3.Count) : "Сторонних инжекторов не запущено");
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

		private static List<CheckResult> CheckCheatFolders()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string folderPath3 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string[] array = new string[3] { folderPath, folderPath2, folderPath3 };
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
						string text2 = Path.GetFileName(text).ToLower();
						string[] cheatClientNames = CheatClientNames;
						foreach (string text3 in cheatClientNames)
						{
							if ((text2 == text3 || text2 == "." + text3 || text2.Contains(text3)) && !text2.Contains("lunar"))
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
					string[] array4 = directories2;
					foreach (string text4 in array4)
					{
						string text5 = Path.GetFileName(text4).ToLower();
						string[] cheatClientNames2 = CheatClientNames;
						foreach (string text6 in cheatClientNames2)
						{
							if (text5 == text6 || text5 == "." + text6)
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
			checkResult.Category = "Файлы и папки";
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
					if ((DateTime.UtcNow - utcNow).TotalSeconds > 12.0)
					{
						flag = true;
						list3.Add("Скан остановлен по времени (" + 12 + " c) — обход неполный.");
						break;
					}
					if (num >= 120000)
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
								list2.Add("\ud83d\udcc1 Глубокая папка: " + text + "  ← сигнатура \"" + text3 + "\"");
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
									list2.Add("\ud83d\udcc4 Файл: " + text4 + "  ← сигнатура \"" + text6 + "\"");
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
			checkResult.Category = "Файлы и папки";
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
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено чит-версий: " + list2.Count) : "Папка versions проверена, всё чисто");
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
			list5.Add(Path.Combine(folderPath, ".tlauncher", "legacy", "minecraft", "logs", "latest.log"));
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
							list4.Add(string.Format("\ud83d\udea8 [{0}] Файл latest.log имеет нулевой размер (0 байт) при запущенном Minecraft! Признак затирания логов перед SS.", Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(item)))));
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
											list2.Add(string.Format("[{0}] Следы чита: [{1}] -> {2}", Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(item))), text5, text3.Trim()));
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
												list3.Add(string.Format("\ud83d\udea8 [{0}] Чит-команда: {1} -> {2}", Path.GetFileName(Path.GetDirectoryName(Path.GetDirectoryName(item))), text6, text3.Trim()));
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
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string path = Path.Combine(folderPath, ".minecraft", "launcher_profiles.json");
			if (File.Exists(path))
			{
				try
				{
					string text = File.ReadAllText(path).ToLower();
					string[] cheatClientNames = CheatClientNames;
					foreach (string text2 in cheatClientNames)
					{
						if (MatchesCheatName(text, text2))
						{
							list2.Add("Профиль лаунчера содержит: " + text2);
						}
					}
					if (text.Contains("-javaagent"))
					{
						list2.Add("Обнаружен аргумент -javaagent (инжектор)");
					}
					if (text.Contains("-xbootclasspath"))
					{
						list2.Add("Обнаружен -Xbootclasspath (перехватчик)");
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
			checkResult.Category = "Файлы и папки";
			checkResult.Name = "Файлы читов на Рабочем столе и в Загрузках";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено файлов: " + list2.Count) : "Рабочий стол и Загрузки чисты");
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
			checkResult.Category = "Файлы и папки";
			checkResult.Name = "Следы чит-клиентов в AppData";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено следов: " + list2.Count) : "Следов в AppData не обнаружено");
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
			checkResult.Category = "Файлы и папки";
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
			string[] array2 = new string[11]
			{
				"click", "lbutton", "rbutton", "cps", "autoclicker", "clicker", "trigger", "fastplace", "jitter", "loop",
				"sleep"
			};
			string[] array3 = array;
			foreach (string path in array3)
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
						bool flag = false;
						string value = "";
						try
						{
							string[] array4 = File.ReadAllLines(item);
							int num = 0;
							string[] array5 = array4;
							foreach (string text in array5)
							{
								string text2 = text.ToLowerInvariant().Trim();
								if (text2.StartsWith(";") || string.IsNullOrEmpty(text2))
								{
									continue;
								}
								string[] array6 = array2;
								foreach (string text3 in array6)
								{
									if (text2.Contains(text3))
									{
										switch (text3)
										{
										case "click":
										case "lbutton":
										case "rbutton":
											num++;
											break;
										default:
											value = text3;
											break;
										}
									}
								}
							}
							if (num >= 2 || !string.IsNullOrEmpty(value))
							{
								flag = true;
							}
						}
						catch
						{
						}
						string fileName = Path.GetFileName(item);
						if (flag)
						{
							list2.Add(string.Format("\ud83d\udea8 AHK автокликер/макрос: {0} ({1})", fileName, item));
						}
						else
						{
							list3.Add(string.Format("Обычный AHK скрипт (без кликеров): {0}", fileName));
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
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = ((list2.Count > 0) ? ("ОБНАРУЖЕНЫ AHK АВТОКЛИКЕРЫ/МАКРОСЫ: " + list2.Count + " шт.") : ((list3.Count > 0) ? "Найдены безобидные AHK скрипты (кликеров нет)" : "Макросов и AHK не найдено"));
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
					string[] array = files;
					foreach (string text in array)
					{
						if (File.GetLastWriteTime(text) < dateTime)
						{
							continue;
						}
						string textLower = Path.GetFileNameWithoutExtension(text).ToLower();
						bool flag = false;
						string text2 = "";
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
								list3.Add(string.Format("\ud83d\uddd1\ufe0f Удалённый недавний файл: {0} -> Цель: {1} (ФАЙЛ УДАЛЁН)", Path.GetFileName(text), text4));
							}
						}
						if (flag)
						{
							list2.Add(string.Format("\ud83d\udea8 [{0}] {1} (ярлык: {2:dd.MM.yyyy HH:mm}){3}", text2, Path.GetFileNameWithoutExtension(text), File.GetLastWriteTime(text), (!string.IsNullOrEmpty(text4)) ? (" -> " + text4) : ""));
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
			checkResult.Detail = ((list4.Count > 0) ? ("Найдено подозрительных записей в Recent: " + list4.Count) : "В Recent всё чисто");
			checkResult.Evidence = string.Join("\r\n", list4.ToArray());
			list.Add(checkResult);
			return list;
		}

		public static string ExtractLnkTargetPath(string lnkPath)
		{
			try
			{
				byte[] bytes = File.ReadAllBytes(lnkPath);
				string input = Encoding.Default.GetString(bytes);
				Match match = Regex.Match(input, "[a-zA-Z]:\\\\[a-zA-Z0-9_\\-\\.\\ \\\\+]+\\.[a-zA-Z0-9]+");
				if (match.Success)
				{
					return match.Value.Trim();
				}
				string input2 = Encoding.Unicode.GetString(bytes);
				Match match2 = Regex.Match(input2, "[a-zA-Z]:\\\\[a-zA-Z0-9_\\-\\.\\ \\\\+]+\\.[a-zA-Z0-9]+");
				if (match2.Success)
				{
					return match2.Value.Trim();
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
			foreach (string item2 in list6)
			{
				try
				{
					FileInfo fileInfo = new FileInfo(item2);
					if (fileInfo.Length == 0 || fileInfo.Length > 83886080)
					{
						continue;
					}
					byte[] array2 = null;
					string text7 = Path.Combine(Path.GetTempPath(), "_hist_" + Guid.NewGuid().ToString("N") + ".tmp");
					try
					{
						File.Copy(item2, text7, true);
						array2 = File.ReadAllBytes(text7);
					}
					catch
					{
						using (FileStream fileStream = new FileStream(item2, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
						{
							int num = (int)Math.Min(fileInfo.Length, 26214400L);
							array2 = new byte[num];
							fileStream.Read(array2, 0, num);
						}
					}
					finally
					{
						try
						{
							if (File.Exists(text7))
							{
								File.Delete(text7);
							}
						}
						catch
						{
						}
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
						string value = text10.ToLowerInvariant();
						if (text8.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0 || text9.IndexOf(value, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							list3.Add(string.Format("\ud83d\udea8 [Браузер] Скачивание/упоминание софта [{0}] в истории: {1}", text10, fileInfo.FullName));
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
							list2.Add(string.Format("\ud83c\udf10 [Браузер] Визит на чит-ресурс [{0}]: {1}", text11, fileInfo.FullName));
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
			checkResult.Severity = ((list3.Count > 0) ? CheckSeverity.Critical : ((list2.Count > 0) ? CheckSeverity.NeedsCheck : CheckSeverity.Info));
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
			list3.Add(Path.Combine(folderPath, ".tlauncher", "legacy", "Minecraft", "game", "screenshots"));
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
				list2.Add(string.Format("⚠\ufe0f [Свежие скриншоты во время игры/проверки: {0} шт. за последние 2 часа]", num2));
			}
			if (num > 0 && list5.Count == 0)
			{
				list2.Add("⚠\ufe0f Папка скриншотов существует, но полностью пуста (возможна недавняя очистка)");
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
			checkResult.Detected = num2 > 0;
			checkResult.Severity = ((num2 > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Detail = string.Format("Всего скриншотов: {0} (Свежих <2ч: {1}, за сутки: {2})", list5.Count, num2, num3);
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckPrefetch()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
			if (!Directory.Exists(path))
			{
				path = "C:\\Windows\\Prefetch";
			}
			if (Directory.Exists(path))
			{
				try
				{
					string[] wiperPatterns = new string[] { "FSUTIL.EXE*.pf", "WEVTUTIL.EXE*.pf", "CIPHER.EXE*.pf", "SDELETE*.pf", "BLEACHBIT*.pf", "CCLEANER*.pf", "BCWIPER*.pf", "ERASER*.pf" };
					foreach (string pattern in wiperPatterns)
					{
						string[] files = Directory.GetFiles(path, pattern);
						foreach (string wf in files)
						{
							FileInfo fileInfo = new FileInfo(wf);
							list2.Add("⚠\ufe0f " + Path.GetFileName(wf) + " в Prefetch (" + fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ") — утилита зачистки следов/журналов!");
						}
					}
					string[] files2 = Directory.GetFiles(path, "*.pf");
					foreach (string path2 in files2)
					{
						string textLower = Path.GetFileNameWithoutExtension(path2).ToLower();
						string[] cheatClientNames = CheatClientNames;
						foreach (string text in cheatClientNames)
						{
							if (MatchesCheatName(textLower, text))
							{
								list2.Add("Prefetch [" + text + "]: " + Path.GetFileName(path2));
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
			checkResult.Category = "Следы запуска";
			checkResult.Name = "Prefetch (следы запуска ПО и утилит очистки)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((!list2.Any((string f) => f.Contains("FSUTIL") || f.Contains("WEVTUTIL") || f.Contains("CIPHER") || f.Contains("SDELETE") || f.Contains("BLEACHBIT") || f.Contains("CCLEANER"))) ? CheckSeverity.Warning : CheckSeverity.Critical);
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено записей: " + list2.Count) : "Prefetch проверен, чисто");
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
							list2.Add("Application EventID 3079 @ " + eventLogEntry.TimeGenerated.ToString("dd.MM.yyyy HH:mm") + ": очистка USN Journal!");
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
							list2.Add("🚨 System EventID 104 @ " + eventLogEntry.TimeGenerated.ToString("dd.MM.yyyy HH:mm") + ": журнал System был очищен (wevtutil)!");
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
			checkResult.Name = "BAM реестр и Event Log (EventID 104/3079)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = (list2.Any((string f) => f.Contains("EventID") || f.Contains("очистка"))) ? CheckSeverity.Critical : CheckSeverity.Warning;
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено записей: " + list2.Count) : "Журналы чисты");
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
									list2.Add("🚨 Внедрённый DLL в Minecraft.Windows.exe: " + module.FileName);
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
				"ambhi", "fate", "flaxenter", "deceptive", "prestige", "solstice", "latite", "krypton"
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
								list2.Add("🚨 Активный процесс инжектора Bedrock: " + process2.ProcessName + ".exe (PID: " + process2.Id + ")");
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
						list2.Add(string.Format("🚨 [Bedrock Client Folder] Обнаружена директория чит-клиента Bedrock: {0} ({1} файлов конфигураций)", bDir, bFiles.Length));
					}
					catch
					{
						list2.Add("🚨 [Bedrock Client Folder] Обнаружена директория чит-клиента Bedrock: " + bDir);
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
						list2.Add("🚨 Подозрительный DLL в папке Bedrock com.mojang: " + text9);
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
									list3.Add(string.Format("⚠️ [Bedrock NightVision] Обнаружена завышенная гамма gfx_gamma={0} (FullBright/ночное видение без зелий) в options.txt", gammaVal));
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
							list2.Add("🚨 Запуск инжектора Bedrock в Prefetch: " + text10 + " (" + lastWriteTime.ToString("dd.MM.yyyy HH:mm:ss") + ")");
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
								list2.Add("🚨 Файл читов Bedrock в " + Path.GetFileName(path3) + ": " + text13);
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
				checkResult2.Detail = "В папке Bedrock com.mojang найдены ресурспаки X-Ray/ESP/FullBright (" + list3.Count + " шт.) — требуется ручная проверка!";
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
							string text2 = "";
							try
							{
								text2 = module.FileName;
							}
							catch
							{
							}
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
									list2.Add("⚠\ufe0f Подозрительная DLL из папки Temp внедрена в javaw.exe: " + text2);
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
			checkResult.Category = "Процессы";
			checkResult.Name = "Анализ загруженных DLL модулей в javaw.exe (Инжекции памяти)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Critical;
			checkResult.Detail = ((list2.Count > 0) ? ("Обнаружены подозрительные/инжектированные DLL в процесс Minecraft (" + list2.Count + ")!") : "Внедрённых сторонних DLL в javaw.exe не обнаружено");
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
			list3.Add(Path.Combine(folderPath, ".tlauncher", "legacy", "minecraft", "logs"));
			list3.Add(Path.Combine(folderPath2, ".lunarclient", "offline", "multiver", "logs"));
			List<string> list4 = list3;
			string[] array = new string[31]
			{
				".bind", ".killaura", ".vape", ".damage", ".friend", ".config", ".vclip", ".hclip", ".panic", ".selfdestruct",
				".teleport", ".elytra", ".target", ".spammer", ".give", "[wurst]", "[liquidbounce]", "[meteor]", "[expensive]", "[neverhook]",
				"[akrien]", "[celestial]", "[deadcode]", "[nurik]", "vape v4", "vape lite", "nursultan", "minced", "weighbar", "richpremium",
				"boberware"
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
											continue;
										}
										string text5 = "\ud83d\udea8 [UserAssist] Запуск ПО: " + text3;
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
							list2.Add("Обнаружен чит-домен (ExtraCheatDomains) в DNS кэше: " + ed);
						}
					}
					string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
					foreach (string ec in extraCheatClients)
					{
						if (ec.Length >= 4 && text.Contains(ec.ToLowerInvariant()))
						{
							list2.Add("Обнаружен чит-клиент (ExtraCheatClients) в DNS кэше: " + ec);
						}
					}}
			}
			catch
			{
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "История";
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
					string[] array = File.ReadAllLines(text);
					string[] array2 = new string[]
					{
						"fsutil usn", "deletejournal", "vape", "nursultan", "expensive", "celestial", "deadcode", "akrien", "downloadstring", "invoke-webrequest",
						"remove-item", "del *.jar", "rmdir", "wevtutil cl", "taskkill", "cipher /w", "sdelete", "bleachbit", "ccleaner", "reg delete",
						"clear-history", "remove-itemproperty", "set-service", "stop-service", "stop-process", "irm ", "iex ", "curl ", "wget ",
						"thunderhack", "slinky", "dreamclient", "zamorozka", "fluger", "minced", "excellent", "wildclient", "certutil", "bitsadmin",
						"bcdedit", "vssadmin", "del /f", "attrib -h", "net stop", "sc stop", "rundll32"
					};
					for (int i = 0; i < array.Length; i++)
					{
						string text2 = array[i].ToLower().Trim();
						string[] array3 = array2;
						foreach (string value in array3)
						{
							if (text2.Contains(value))
							{
								list2.Add("Подозрительная команда PowerShell [строка " + (i + 1) + "]: " + array[i].Trim());
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
			checkResult.Evidence = ((list2.Count > 0) ? string.Join("\r\n", list2.ToArray()) : ("Файл истории: " + text));
			list.Add(checkResult);
			return list;
		}

		private static List<CheckResult> CheckCrashDumps()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string path = Path.Combine(folderPath, "CrashDumps");
			if (Directory.Exists(path))
			{
				try
				{
					string[] files = Directory.GetFiles(path, "*.dmp", SearchOption.TopDirectoryOnly);
					string[] array = files;
					foreach (string path2 in array)
					{
						string text = Path.GetFileName(path2).ToLower();
						DateTime lastWriteTime = File.GetLastWriteTime(path2);
						bool flag = false;
						string[] cheatClientNames = CheatClientNames;
						foreach (string text2 in cheatClientNames)
						{
							if (MatchesCheatName(text, text2))
							{
								list2.Add("\ud83d\udea8 Аварийный дамп чит-программы [" + text2 + "]: " + Path.GetFileName(path2) + " (" + lastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")");
								flag = true;
								break;
							}
						}
						if (!flag && (text.StartsWith("javaw") || text.StartsWith("minecraft")))
						{
							list3.Add("Дамп Java/Minecraft: " + Path.GetFileName(path2) + " (" + lastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")");
						}
					}
				}
				catch
				{
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Следы запуска";
			checkResult.Name = "Дампы аварийного завершения (CrashDumps сбоев читов/Minecraft)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = ((list2.Count > 0) ? ("ОБНАРУЖЕНЫ АВАРИЙНЫЕ ДАМПЫ ЧИТОВ (" + list2.Count + " шт.)!") : ((list3.Count > 0) ? ("Дампов читов нет (обычных падений Java: " + list3.Count + ")") : "Дампы сбоев читов не обнаружены"));
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
											list3.Add(string.Format("⚠\ufe0f [Свежее подключение USB] Флешка была вставлена {0:dd.MM.yyyy HH:mm:ss} (менее 2 часов назад)! Проверьте на запуск читов с внешнего диска!", result));
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
			checkResult.Category = "Оборудование";
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
						"CCLEANER", "BLEACHBIT", "PRIVAZER", "ERASER", "SDELETE", "WEVTUTIL", "FSUTIL", "USN", "CLEANMGR",
						"CIPHER", "BCWIPER", "GLARY", "WISE", "ADVANCED", "REGDEL", "DEFRAG", "REVO", "IOBIT", "KERISH", "REGORG", "DBCLEAN"
					};
					string[] files = Directory.GetFiles(path, "*.pf");
					string[] array2 = files;
					foreach (string text in array2)
					{
						string text2 = Path.GetFileName(text).ToUpper();
						string[] array3 = array;
						foreach (string text3 in array3)
						{
							if (text2.StartsWith(text3))
							{
								DateTime lastWriteTime = File.GetLastWriteTime(text);
								TimeSpan timeSpan = DateTime.Now - lastWriteTime;
								if (timeSpan.TotalHours < 24.0)
								{
									list2.Add("🚨 Недавний запуск утилиты очистки/затирания [" + text3 + "]: " + text + " (" + lastWriteTime.ToString("dd.MM.yyyy HH:mm") + ", " + (int)timeSpan.TotalMinutes + " мин назад)");
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
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Следы запуска";
			checkResult.Name = "Запуски утилит скрытия/очистки за последние 24ч (Prefetch)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Any(x => x.Contains("WEVTUTIL") || x.Contains("FSUTIL") || x.Contains("CIPHER") || x.Contains("SDELETE") || x.Contains("BLEACHBIT") || x.Contains("BCWIPER") || x.Contains("ERASER"))) ? CheckSeverity.Critical : CheckSeverity.Warning);
			checkResult.Detail = ((list2.Count > 0) ? ("РћР‘РќРђР РЈР–Р•РќР« Р—РђРџРЈРЎРљР РЈРўРР›РРў РћР§РРЎРўРљР РЎР›Р•Р”РћР’ (" + list2.Count + " шт.)!") : "Недавних запусков CCleaner, BleachBit, FSUTIL, CIPHER не обнаружено");
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
								list2.Add("\ud83d\udea8 ПУНКТ 3.6 РЕГЛАМЕНТА: Служба SysMain (Prefetch) ОТКЛЮЧЕНА (Start=4)! Квалифицируется как уклонение от проверки / скрытие следов!");
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
							list2.Add("⚠\ufe0f ШАГ 1 РЕГЛАМЕНТА: Minecraft (javaw.exe PID=" + process.Id + ") запущен всего " + Math.Round(timeSpan.TotalMinutes, 1) + " мин назад (возможный перезапуск клиента в момент вызова на проверку)!");
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
						list2.Add(string.Concat("\ud83d\udea8 ШАГ 4 РЕГЛАМЕНТА: ", text, " (", environmentVariableTarget, ") содержит агент: ", text2));
					}
					else
					{
						list2.Add(string.Concat("⚠\ufe0f ШАГ 4 РЕГЛАМЕНТА: переменная ", text, " (", environmentVariableTarget, ") задана: ", text2));
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
						bool flag = text2.Contains("отказано") || text2.Contains("access is denied");
						bool flag2 = Regex.IsMatch(text ?? string.Empty, "0x0[0-9a-fA-F]{15}");
						bool flag3 = text2.Contains("не найден") || text2.Contains("not found") || text2.Contains("cannot find") || text2.Contains("отсутствует");
						if (flag)
						{
							list3.Add("Для чтения USN Journal на " + driveLetter + " нужны права администратора.");
						}
						else if (flag3 || (!flag2 && exitCode != 0))
						{
							list2.Add("🚨 ПУНКТ 3.6 РЕГЛАМЕНТА: USN Journal на диске " + driveLetter + " недоступен или удалён (fsutil код " + exitCode + ") — признак deletejournal / затирания следов!");
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
					list2.Add("🚨 ПУНКТ 3.6 РЕГЛАМЕНТА: USN Journal на диске C: недоступен или удалён!");
				}
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Регламент проверки / Очистка";
			checkResult.Name = "Состояние USN Journal на NTFS-дисках (ШАГ 3.6 регламента)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			if (list2.Count > 0)
			{
				checkResult.Detail = "USN Journal отсутствует или удалён — зафиксировано затирание следов на дисках (" + list2.Count + " шт.).";
			}
			else if (list3.Count > 0)
			{
				checkResult.Detail = "Проверка ограничена: " + string.Join(" ", list3);
			}
			else
			{
				checkResult.Detail = "USN Journal на всех проверенных NTFS-дисках (" + checkedDrives + " шт.) активен и читается — удаление через deletejournal не производилось.";
			}
			checkResult.Evidence = ((list2.Count > 0) ? string.Join("\r\n", list2) : "");
			list.Add(checkResult);
			return list;
		}

		public static List<CheckResult> CheckRegulationCoreJarIntegrity()
		{
			List<CheckResult> list = new List<CheckResult>();
			List<string> list2 = new List<string>();
			string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
			string path2 = Path.Combine(path, "versions");
			List<string> list3 = new List<string>();
			try
			{
				if (Directory.Exists(path2))
				{
					string[] directories = Directory.GetDirectories(path2);
					foreach (string text in directories)
					{
						string fileName = Path.GetFileName(text);
						string text2 = Path.Combine(text, fileName + ".jar");
						if (File.Exists(text2))
						{
							list3.Add(text2);
						}
					}
				}
			}
			catch
			{
			}
			foreach (string item in list3)
			{
				try
				{
					using (ZipArchive zipArchive = ZipFile.OpenRead(item))
					{
						bool flag = false;
						bool flag2 = false;
						bool flag3 = false;
						foreach (ZipArchiveEntry entry in zipArchive.Entries)
						{
							string text3 = entry.FullName.ToLowerInvariant();
							if (text3 == "meta-inf/services/java.lang.instrument")
							{
								flag = true;
							}
							if (text3.EndsWith(".class") && (text3.Contains("instrument/agent") || text3.Contains("agentmain")))
							{
								flag3 = true;
							}
							if (text3.StartsWith("agent/") && text3.EndsWith(".class"))
							{
								flag3 = true;
							}
							if (!(text3 == "meta-inf/manifest.mf") || entry.Length >= 200000)
							{
								continue;
							}
							using (StreamReader streamReader = new StreamReader(entry.Open()))
							{
								string text4 = streamReader.ReadToEnd().ToLowerInvariant();
								if (text4.Contains("agent-class") || text4.Contains("premain-class"))
								{
									flag2 = true;
								}
							}
						}
						if (flag || flag2 || flag3)
						{
							list2.Add("\ud83d\udea8 ШАГ 7 РЕГЛАМЕНТА: в jar версии клиента найден java-агент: " + item);
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
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = ((list2.Count > 0) ? ("Версия клиента подменена: java-агент внутри jar (" + list2.Count + " шт.). Это инъекция в обход модов.") : ("Проверено версий: " + list3.Count + ". Встроенных java-агентов в главных jar не найдено."));
			checkResult.Evidence = ((list2.Count > 0) ? string.Join("\r\n", list2) : "");
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
							list2.Add("\ud83d\udea8 ШАГ 1.2 РЕГЛАМЕНТА: служба времени W32Time ОТКЛЮЧЕНА (Start=4) — часы можно откатить незаметно.");
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
			if (text2.Contains("unspecified") || text2.Contains("не синхронизирован") || text2.Contains("unsynchronized") || text2.Contains("0x8007052e"))
			{
				list2.Add("⚠\ufe0f ШАГ 1.2 РЕГЛАМЕНТА: время не синхронизировано с сервером — возможен откат часов.");
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
				checkResult.Detail = "Проверка ограничена: " + string.Join(" ", list3);
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
					"killaura", "autototem", "hitboxes", "velocity", "freecam", "cheststealer", "aimassist", "triggerbot", "deadcode", "wexside",
					"expensive", "nursultan", "celestial", "akrien", "rich", "bleachhack", "atomic", "targetstrafe", "fastbow", "reach"
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
								list2.Add(string.Format("🚨 ШАГ 5 РЕГЛАМЕНТА: В RAM javaw (PID={0}) обнаружен чит: {1} ({2}, адрес: {3})", process.Id, hit.Type, hit.Detail, hit.Address));
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
								list2.Add(string.Format("🚨 ШАГ 5 РЕГЛАМЕНТА: Подозрительный модуль в javaw: {0} ({1})", mHit.Detail, mHit.Address));
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
					foreach (string text in files)
					{
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
							string text3 = ((timeSpan.TotalDays <= 7.0) ? ("🚨 [Активно: запуск " + Math.Round(timeSpan.TotalDays, 1) + " дн. назад (в пределах 7 дней)]") : ((!(timeSpan.TotalDays <= 14.0)) ? ("🕒 [Устарело: запуск " + Math.Round(timeSpan.TotalDays, 0) + " дн. назад (>14 дней)]") : ("📦 [Хранение: запуск " + Math.Round(timeSpan.TotalDays, 1) + " дн. назад (в пределах 7-14 дней)]")));
							list2.Add(text3 + " [" + matchedCheat + "] " + Path.GetFileName(text) + " (Дата: " + fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")");
						}
					}
				}
				if (list2.Count > 0)
				{
					bool flag = list2.Any((string h) => h.Contains("[Активно") || h.Contains("[Хранение"));
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Регламент проверки / Сроки";
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
					checkResult2.Category = "Регламент проверки / Сроки";
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
				checkResult3.Category = "Регламент проверки / Сроки";
				checkResult3.Name = "Сроки давности Prefetch";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки Prefetch: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

		public static List<CheckResult> CheckVirtualMachineAndSandbox()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				string[] array = new string[8] { "vmware", "virtualbox", "vbox", "qemu", "kvm", "xen", "bochs", "parallels" };
				try
				{
					using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\Description\\System\\BIOS"))
					{
						if (registryKey != null)
						{
							string text = (registryKey.GetValue("SystemManufacturer") ?? "").ToString().ToLowerInvariant();
							string text2 = (registryKey.GetValue("SystemProductName") ?? "").ToString().ToLowerInvariant();
							string text3 = (registryKey.GetValue("BIOSVersion") ?? "").ToString().ToLowerInvariant();
							string[] array2 = array;
							foreach (string value in array2)
							{
								if (text.Contains(value) || text2.Contains(value) || text3.Contains(value))
								{
									list2.Add("\ud83d\udea8 Обнаружена виртуальная машина в BIOS: " + text + " / " + text2);
									break;
								}
							}
						}
					}
				}
				catch
				{
				}
				string[] array3 = new string[6] { "vmtoolsd", "vboxtray", "vboxservice", "sandboxiedcomlaunch", "sbiesvc", "sbiectrl" };
				string[] array4 = array3;
				foreach (string text4 in array4)
				{
					if (Process.GetProcessesByName(text4).Length > 0)
					{
						list2.Add("\ud83d\udea8 Активный фоновый процесс виртуализации / песочницы: " + text4 + ".exe");
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Среда / Виртуализация";
					checkResult.Name = "Детектор виртуальных машин и песочниц (VMware / VBox / Sandboxie)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Warning;
					checkResult.Detail = "Обнаружены признаки запуска проверки внутри виртуальной машины или песочницы: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Среда / Виртуализация";
					checkResult2.Name = "Анализ физической среды ПК (Anti-VM)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Проверка запущена на реальном физическом компьютере (признаков VM/Sandbox не обнаружено).";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Среда / Виртуализация";
				checkResult3.Name = "Анализ физической среды";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки Anti-VM: " + ex.Message;
				list.Add(checkResult3);
			}
			return list;
		}

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
						foreach (string text in files)
						{
							string text2 = Path.GetFileName(text).ToLowerInvariant();
							if (!text2.EndsWith(".jar") && !text2.EndsWith(".exe") && !text2.EndsWith(".dll") && !text2.EndsWith(".zip") && !text2.EndsWith(".rar") && !text2.EndsWith(".7z") && !text2.EndsWith(".bat") && !text2.EndsWith(".cmd") && !text2.EndsWith(".ps1"))
							{
								continue;
							}
							string[] cheatClientNames = CheatClientNames;
							foreach (string cheatLower in cheatClientNames)
							{
								if (MatchesCheatName(text2, cheatLower) && !text2.Contains("lunar") && !text2.Contains("feather") && !text2.Contains("badlion"))
								{
									FileInfo fileInfo = new FileInfo(text);
									list2.Add("\ud83d\udea8 [" + Path.GetFileName(path) + "] Загруженный чит: " + Path.GetFileName(text) + " (" + fileInfo.Length + " байт, изменён: " + fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")");
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
				checkResult3.Name = "Загрузки мессенджеров";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки загрузок: " + ex.Message;
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
					string[] array = directories;
					foreach (string path2 in array)
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
						string[] array2 = files;
						foreach (string path3 in array2)
						{
							try
							{
								byte[] array3 = File.ReadAllBytes(path3);
								if (array3.Length >= 28)
								{
									long fileTime = BitConverter.ToInt64(array3, 16);
									DateTime dateTime = DateTime.FromFileTime(fileTime);
									string text = Encoding.Unicode.GetString(array3, 28, array3.Length - 28);
									char[] trimChars = new char[1];
									string text2 = text.TrimEnd(trimChars);
									string text3 = text2.ToLowerInvariant();
									bool flag = false;
									string text4 = "";
									string[] cheatClientNames = CheatClientNames;
									foreach (string text5 in cheatClientNames)
									{
										if (MatchesCheatName(text3, text5) && !text3.Contains("lunar"))
										{
											flag = true;
											text4 = text5;
											break;
										}
									}
									if (!flag)
									{
										string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
										foreach (string text6 in extraCheatClients)
										{
											if (text6.Length >= 4 && MatchesCheatName(text3, text6) && !text3.Contains("lunar"))
											{
												flag = true;
												text4 = text6;
												break;
											}
										}
									}
									if (!flag)
									{
										CheatDatabases.BanRule[] bannedByRegulation = CheatDatabases.BannedByRegulation;
										foreach (CheatDatabases.BanRule banRule in bannedByRegulation)
										{
											string[] keys = banRule.Keys;
											foreach (string text7 in keys)
											{
												if (text7.Length >= 4 && text3.Contains(text7.ToLowerInvariant()))
												{
													flag = true;
													text4 = banRule.Id + " (" + banRule.Title + ")";
													break;
												}
											}
											if (flag)
											{
												break;
											}
										}
									}
									TimeSpan timeSpan = DateTime.Now - dateTime;
									if (flag)
									{
										string item = string.Format("\ud83d\udea8 [Корзина $I | {0}] Удалён чит [{1}]: {2} (Дата: {3:dd.MM.yyyy HH:mm:ss})", item2.Name, text4, text2, dateTime);
										if (timeSpan.TotalHours >= 0.0 && timeSpan.TotalHours <= 2.0)
										{
											list3.Add(string.Format("⚠\ufe0f [Свежее удаление перед SS] Чит удалён в корзину {0:dd.MM.yyyy HH:mm:ss} ({1:0} мин. назад)! -> {2}", dateTime, timeSpan.TotalMinutes, text2));
										}
										list2.Add(item);
									}
									else if ((text3.EndsWith(".jar") || text3.EndsWith(".exe") || text3.EndsWith(".dll") || text3.EndsWith(".bat") || text3.EndsWith(".cmd") || text3.EndsWith(".ps1") || text3.EndsWith(".zip") || text3.EndsWith(".7z") || text3.EndsWith(".rar") || text3.EndsWith(".amc")) && timeSpan.TotalHours >= 0.0 && timeSpan.TotalHours <= 2.0)
									{
										list3.Add(string.Format("⚠\ufe0f [Свежее удаление исполняемого файла] {0} удалён {1:dd.MM.yyyy HH:mm:ss} ({2:0} мин. назад)", text2, dateTime, timeSpan.TotalMinutes));
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
						if (list2.Count >= 30)
						{
							break;
						}
					}
				}
				List<string> list4 = new List<string>();
				list4.AddRange(list3);
				list4.AddRange(list2);
				if (list4.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Корзина / Удаления";
					checkResult.Name = "Декодирование $I индексов корзины $Recycle.Bin";
					checkResult.Detected = true;
					checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.NeedsCheck);
					checkResult.Detail = string.Format("В корзине $Recycle.Bin обнаружены удалённые читы ({0} шт.) и свежие удаления ({1} шт.)!", list2.Count, list3.Count);
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
					checkResult2.Detail = "В корзине $Recycle.Bin следов читов и свежих удалений исполняемых файлов не обнаружено.";
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
								list2.Add("🚨 [ОБХОД ПРОВЕРКИ] Служба Diagnostic Policy Service (DPS) ОТКЛЮЧЕНА (Start=4)! Ведение базы учета ресурсов SRUM заблокировано.");
								isTampered = true;
							}
							else
							{
								list2.Add("✅ Служба DPS (Diagnostic Policy Service / SRUM) активна.");
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
					list2.Add("🚨 [КРИТИЧНО] База данных SRUDB.dat отсутствует на диске! Возможна намеренная очистка истории активности.");
					isTampered = true;
				}
				else
				{
					try
					{
						FileInfo fileInfo = new FileInfo(text);
						TimeSpan age = DateTime.Now - fileInfo.LastWriteTime;
						list2.Add(string.Format("[База данных SRUM] SRUDB.dat: {0:F1} МБ, обновлена: {1:dd.MM.yyyy HH:mm:ss} ({2:F0} мин назад)", (double)fileInfo.Length / 1048576.0, fileInfo.LastWriteTime, age.TotalMinutes));

						if (fileInfo.Length < 100 * 1024)
						{
							list2.Add("⚠️ Аномально маленький размер SRUDB.dat (< 100 КБ) — подозрение на недавний сброс/очистку базы.");
						}

						// 4. Memory/binary scanning of SRUDB.dat for deleted cheat executables
						if (fileInfo.Length > 0)
						{
							string[] targets = new string[]
							{
								"nursultan", "celestial", "expensive", "wexside", "akrien", "minced", "wild.exe",
								"flint.exe", "kura.exe", "deadcode", "rich.exe", "thunderhack", "slinky", "doomsday",
								"ravenb", "vape.exe", "drip.exe", "liquidbounce", "horion.exe", "zephyr.exe", "prax.exe",
								"speedautoclicker", "opautoclicker", "tinytask.exe", "keyran.exe"
							};

							using (FileStream fs = new FileStream(text, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete))
							{
								int maxScanBytes = (int)Math.Min(fs.Length, 32 * 1024 * 1024);
								byte[] buffer = new byte[2 * 1024 * 1024];
								int totalRead = 0;
								HashSet<string> detectedCheats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

								while (totalRead < maxScanBytes)
								{
									int toRead = Math.Min(buffer.Length, maxScanBytes - totalRead);
									int read = fs.Read(buffer, 0, toRead);
									if (read <= 0) break;
									totalRead += read;

									string chunkUtf8 = Encoding.UTF8.GetString(buffer, 0, read).ToLowerInvariant();
									string chunkUnicode = Encoding.Unicode.GetString(buffer, 0, read).ToLowerInvariant();

									foreach (string t in targets)
									{
										if ((chunkUtf8.Contains(t) || chunkUnicode.Contains(t)) && !detectedCheats.Contains(t))
										{
											detectedCheats.Add(t);
											list2.Add("🚨 [SRUM FORENSIC] В системной базе SRUDB.dat найдено упоминание чит-процесса: " + t);
											cheatDetected = true;
										}
									}

									if (detectedCheats.Count >= 8) break;
								}
							}
						}
					}
					catch (UnauthorizedAccessException)
					{
						list2.Add("ℹ️ Доступ к чтению SRUDB.dat ограничен правами Windows (требуются права Администратора).");
					}
					catch (Exception ex)
					{
						list2.Add("ℹ️ Ошибка сканирования содержимого SRUDB.dat: " + ex.Message);
					}
				}

				CheckResult checkResult = new CheckResult();
				checkResult.Category = "Системные базы / SRUM";
				checkResult.Name = "База учета использования ресурсов SRUM (SRUDB.dat)";
				checkResult.Detected = cheatDetected || isTampered;
				checkResult.Severity = (cheatDetected || isTampered) ? CheckSeverity.Critical : CheckSeverity.Info;
				checkResult.Detail = cheatDetected
					? "В базе SRUM обнаружены записи сетевого трафика и расхода ресурсов удалённых читов!"
					: (isTampered
						? "Обнаружены признаки отключения или сокрытия базы SRUM (DPS Start=4 / удаление базы)!"
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

		public static List<CheckResult> CheckPcaAppLaunchLogs()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string path = Path.Combine(folderPath, "appcompat\\pca");
				if (Directory.Exists(path))
				{
					string[] files = Directory.GetFiles(path, "*.txt");
					HashSet<string> seenEntries = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

					foreach (string path2 in files)
					{
						string fileName = Path.GetFileName(path2);
						try
						{
							byte[] rawBytes = File.ReadAllBytes(path2);
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

								string resolvedPath = Environment.ExpandEnvironmentVariables(rawExePath);
								string exeName = Path.GetFileName(resolvedPath).ToLowerInvariant();
								string resolvedLower = resolvedPath.ToLowerInvariant();

								if (CheatDatabases.IsWhitelistedPath(resolvedLower) || CheatDatabases.IsWhitelistedProcess(exeName.Replace(".exe", "")))
								{
									continue;
								}
								if (exeName.Contains("lunarclient") || exeName.Contains("badlion") || exeName.Contains("curseforge") || exeName.Contains("prismlauncher") || exeName.Contains("tlauncher"))
								{
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
									string[] extraKeywords = new string[] { "autoclicker", "killaura", "hitbox", "reach", "aimassist", "xray", "speedautoclicker", "tinytask", "keyran", "opautoclicker", "kmbox" };
									foreach (string kw in extraKeywords)
									{
										if (exeName.Contains(kw))
										{
											matchedCheat = kw;
											break;
										}
									}
								}

								if (matchedCheat != null)
								{
									string dedupeKey = resolvedLower + "|" + timestamp;
									if (!seenEntries.Add(dedupeKey)) continue;

									bool fileExists = File.Exists(resolvedPath);
									string status = fileExists ? "Файл сохранен на диске" : "🗑️ ФАЙЛ УДАЛЕН ПОСЛЕ ЗАПУСКА!";
									string timeStr = string.IsNullOrEmpty(timestamp) ? "" : (" | Время: " + timestamp);

									list2.Add(string.Format("🚨 [{0}] Чит: {1} | Путь: {2}{3} | Статус: {4}", fileName, matchedCheat, resolvedPath, timeStr, status));
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
					checkResult.Name = "Журналы PCA (Program Compatibility Assistant — Запуски в appcompat\\pca)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "В системных журналах PCA обнаружены записи о запусках читов: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Следы запуска";
					checkResult2.Name = "Журналы PCA (appcompat\\pca)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "В журналах PCA записей о запуске читов не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Следы запуска";
				checkResult3.Name = "Журналы PCA";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки PCA: " + ex.Message;
				list.Add(checkResult3);
			}
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

				foreach (string modsPath in modsDirs)
				{
					try
					{
						string[] files = Directory.GetFiles(modsPath, "*.jar", SearchOption.TopDirectoryOnly);
						foreach (string text in files)
						{
							try
							{
								using (ZipArchive zipArchive = ZipFile.OpenRead(text))
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
														list2.Add("\ud83d\udea8 [Подозрительный Mixin в моде " + Path.GetFileName(text) + "] Перехват сетевых/боевых пакетов: " + entry.Name);
													}
												}
											}
										}
										if (text2.Contains("/wurst/") || text2.Contains("/meteor/") || text2.Contains("/expensive/") || text2.Contains("/celestial/") || text2.Contains("/nursultan/") || text2.Contains("/liquidbounce/") || text2.Contains("/thunderhack/") || text2.Contains("/slinky/") || text2.Contains("/seedcracker/") || text2.Contains("/doomsday/") || text2.Contains("/boze/") || text2.Contains("/vape/") || text2.Contains("/akrien/") || text2.Contains("/criticals/") || text2.Contains("/targetstrafe/") || text2.Contains("/silentaim/") || text2.Contains("/packetfly/") || text2.Contains("/dreamclient/") || text2.Contains("/zamorozka/") || text2.Contains("/minced/") || text2.Contains("/wexside/") || text2.Contains("/richclient/") || text2.Contains("/fluger/") || text2.Contains("/autototem/") || text2.Contains("/anchor/") || text2.Contains("/fastmine/"))
										{
											list2.Add("\ud83d\udea8 [Скрытый чит в моде " + Path.GetFileName(text) + "] Класс: " + entry.FullName);
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
					checkResult.Detail = "Обнаружены скрытые боевые миксины или чит-пакеты в модах: " + list2.Count + " шт.";
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
				checkResult3.Detail = "Ошибка анализа модов: " + ex.Message;
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
				string programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);

				// 1. Bloody / Oscar Editor
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
							list2.Add("🚨 [Bloody/Oscar] Обнаружен макрос-скрипт мыши (.amc): " + text);
							if (list2.Count >= 10) break;
						}
						string[] bkgFiles = Directory.GetFiles(path, "*.bkg", SearchOption.AllDirectories);
						foreach (string text2 in bkgFiles)
						{
							list2.Add("🚨 [Bloody] Обнаружен файл макросов Bloody (.bkg): " + text2);
							if (list2.Count >= 15) break;
						}
					}
					catch
					{
					}
				}

				// 2. Razer Synapse
				string razerPath = Path.Combine(localAppData, "Razer\\Synapse3\\Macros");
				if (Directory.Exists(razerPath))
				{
					try
					{
						string[] files = Directory.GetFiles(razerPath, "*.xml", SearchOption.AllDirectories);
						foreach (string text3 in files)
						{
							try
							{
								string text4 = File.ReadAllText(text3);
								if (text4.Contains("<Delay>10</Delay>") || text4.Contains("<Delay>15</Delay>") || text4.Contains("<Delay>20</Delay>") || text4.Contains("MouseButton"))
								{
									list2.Add("🚨 [Razer Synapse] Обнаружен XML-макрос автокликера: " + text3);
									if (list2.Count >= 20) break;
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

				// 3. Logitech G HUB
				string lghubPath = Path.Combine(localAppData, "LGHUB\\scripts");
				if (Directory.Exists(lghubPath))
				{
					try
					{
						string[] files = Directory.GetFiles(lghubPath, "*.lua", SearchOption.AllDirectories);
						foreach (string text5 in files)
						{
							try
							{
								string text6 = File.ReadAllText(text5).ToLowerInvariant();
								if (text6.Contains("pressmousebutton") && (text6.Contains("sleep") || text6.Contains("repeat")))
								{
									list2.Add("🚨 [Logitech G HUB] Обнаружен Lua-скрипт автокликера: " + text5);
									if (list2.Count >= 25) break;
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

				// 4. Keyran Macro Software
				string[] keyranDirs = new string[]
				{
					Path.Combine(localAppData, "Keyran\\Macros"),
					Path.Combine(appData, "Keyran\\Macros"),
					"C:\\Program Files\\Keyran\\Macros",
					"C:\\Program Files (x86)\\Keyran\\Macros"
				};
				foreach (string kp in keyranDirs)
				{
					if (!Directory.Exists(kp)) continue;
					try
					{
						string[] kFiles = Directory.GetFiles(kp, "*.*", SearchOption.AllDirectories);
						foreach (string kf in kFiles)
						{
							string ext = Path.GetExtension(kf).ToLowerInvariant();
							if (ext == ".kmp" || ext == ".amc" || ext == ".macro" || ext == ".dat")
							{
								list2.Add("🚨 [Keyran] Обнаружен макрос-файл Keyran: " + kf);
								if (list2.Count >= 30) break;
							}
						}
					}
					catch
					{
					}
				}

				// 5. Corsair iCUE
				string[] icueDirs = new string[]
				{
					Path.Combine(appData, "Corsair\\CUE5\\macros"),
					Path.Combine(appData, "Corsair\\CUE4\\macros"),
					Path.Combine(localAppData, "Corsair\\CUE5\\macros")
				};
				foreach (string ip in icueDirs)
				{
					if (!Directory.Exists(ip)) continue;
					try
					{
						string[] ifiles = Directory.GetFiles(ip, "*.xml", SearchOption.AllDirectories);
						foreach (string ifile in ifiles)
						{
							list2.Add("🚨 [Corsair iCUE] Обнаружен макрос мыши iCUE: " + ifile);
							if (list2.Count >= 35) break;
						}
					}
					catch
					{
					}
				}

				// 6. TGMacro
				string[] tgDirs = new string[]
				{
					Path.Combine(localAppData, "TGMacro"),
					Path.Combine(appData, "TGMacro")
				};
				foreach (string tgp in tgDirs)
				{
					if (!Directory.Exists(tgp)) continue;
					try
					{
						string[] tgfiles = Directory.GetFiles(tgp, "*.*", SearchOption.TopDirectoryOnly);
						foreach (string tgf in tgfiles)
						{
							string ext = Path.GetExtension(tgf).ToLowerInvariant();
							if (ext == ".tjm" || ext == ".xml")
							{
								list2.Add("🚨 [TGMacro] Обнаружен скрипт TGMacro: " + tgf);
								if (list2.Count >= 40) break;
							}
						}
					}
					catch
					{
					}
				}

				// 7. X-Mouse Button Control XML profile audit
				string[] xmousePaths = new string[]
				{
					Path.Combine(appData, "Highresolution Enterprises\\X-MouseButtonControl\\XMouseButtonControl.xml"),
					Path.Combine(localAppData, "Highresolution Enterprises\\X-MouseButtonControl\\XMouseButtonControl.xml")
				};
				foreach (string xmp in xmousePaths)
				{
					if (File.Exists(xmp))
					{
						try
						{
							string xmContent = File.ReadAllText(xmp);
							if (xmContent.Contains("{CLICK}") || xmContent.Contains("{WAITMS:") || xmContent.Contains("{MBLDN}") || xmContent.Contains("Simulated Keystrokes"))
							{
								list2.Add("🚨 [X-Mouse Button Control] В конфигурации XMouseButtonControl.xml найдены симуляции быстрых кликов/макросы!");
							}
							else
							{
								list2.Add("ℹ️ [X-Mouse Button Control] Найден файл настроек: " + xmp);
							}
						}
						catch
						{
						}
					}
				}

				// 8. ReWASD Turbo/Macro Profiles
				string rewasdPath = Path.Combine(programData, "Disc-Soft\\reWASD\\Profiles");
				if (Directory.Exists(rewasdPath))
				{
					try
					{
						string[] rfiles = Directory.GetFiles(rewasdPath, "*.rewasd", SearchOption.AllDirectories);
						foreach (string rf in rfiles)
						{
							list2.Add("🚨 [reWASD] Обнаружен профиль эмулятора/макросов reWASD: " + rf);
							if (list2.Count >= 45) break;
						}
					}
					catch
					{
					}
				}

				// 9. Active Macro & Clicker Processes
				string[] macroProcesses = new string[]
				{
					"bloody7", "bloody6", "oscareditor", "xmousebuttoncontrol", "xmouse",
					"keyran", "rewasd", "tinytask", "speedautoclicker", "opautoclicker", "gsautoclicker",
					"tgmacro", "interception", "autohotkey", "ahk2exe"
				};
				foreach (string procName in macroProcesses)
				{
					try
					{
						if (Process.GetProcessesByName(procName).Length > 0)
						{
							list2.Add("⚠️ Запущен активный процесс софта мыши/макросов: " + procName + ".exe");
						}
					}
					catch
					{
					}
				}

				bool flag = list2.Any((string text8) => text8.StartsWith("🚨"));
				CheckResult checkResult = new CheckResult();
				checkResult.Category = "Мышь / Макросы";
				checkResult.Name = "Анализ макросов мыши (Bloody/Razer/Logitech/Keyran/X-Mouse/reWASD)";
				checkResult.Detected = list2.Count > 0;
				checkResult.Severity = (flag ? CheckSeverity.Critical : ((list2.Count > 0) ? CheckSeverity.NeedsCheck : CheckSeverity.Info));
				checkResult.Detail = ((list2.Count > 0) ? string.Format("Обнаружены макросы/ПО мыши: {0} шт. (критических макрос-файлов: {1})", list2.Count, list2.Count((string text8) => text8.StartsWith("🚨"))) : "Макрос-файлов автокликеров и скриптов мыши не обнаружено");
				checkResult.Evidence = string.Join("\r\n", list2);
				list.Add(checkResult);
			}
			catch (Exception ex)
			{
				CheckResult checkResult2 = new CheckResult();
				checkResult2.Category = "Мышь / Макросы";
				checkResult2.Name = "Анализ макросов мыши";
				checkResult2.Detected = false;
				checkResult2.Severity = CheckSeverity.Info;
				checkResult2.Detail = "Ошибка проверки макросов: " + ex.Message;
				list.Add(checkResult2);
			}
			return list;
		}

		public static List<CheckResult> CheckDiscordAndTelegramCaches()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string[] array = new string[3]
				{
					Path.Combine(folderPath, "Telegram Desktop\\tdata"),
					Path.Combine(folderPath, "discord"),
					Path.Combine(folderPath, "Vencord")
				};
				string[] array2 = array;
				foreach (string text in array2)
				{
					if (Directory.Exists(text))
					{
						list2.Add("[Мессенджер / Клиент] Папка: " + text);
					}
				}
				CheckResult checkResult = new CheckResult();
				checkResult.Category = "Мессенджеры / Источники";
				checkResult.Name = "Клиенты передачи файлов и конфигураций (Discord / Telegram / Vencord)";
				checkResult.Detected = false;
				checkResult.Severity = CheckSeverity.Info;
				checkResult.Detail = "Обнаружены клиенты связи: " + list2.Count + " шт.";
				checkResult.Evidence = string.Join("\r\n", list2);
				list.Add(checkResult);
			}
			catch (Exception ex)
			{
				CheckResult checkResult2 = new CheckResult();
				checkResult2.Category = "Мессенджеры / Источники";
				checkResult2.Name = "Клиенты связи";
				checkResult2.Detected = false;
				checkResult2.Severity = CheckSeverity.Info;
				checkResult2.Detail = "Ошибка проверки мессенджеров: " + ex.Message;
				list.Add(checkResult2);
			}
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
					string[] array3 = File.ReadAllLines(path);
					string[] array4 = array3;
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
							list2.Add("⚠\ufe0f История поиска / запусков Everything отключена (" + text2 + ") — возможная очистка следов перед SS!");
						}
						string[] cheatClientNames = CheatClientNames;
						foreach (string cheatLower in cheatClientNames)
						{
							if (MatchesCheatName(text3, cheatLower) && !text3.Contains("lunar"))
							{
								list2.Add("[Everything History] " + text2);
								break;
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
							string[] array5 = File.ReadAllLines(path3);
							string[] array6 = array5;
							foreach (string text5 in array6)
							{
								string text6 = text5.ToLowerInvariant();
								string[] cheatClientNames2 = CheatClientNames;
								foreach (string cheatLower2 in cheatClientNames2)
								{
									if (MatchesCheatName(text6, cheatLower2) && !text6.Contains("lunar"))
									{
										list2.Add("[Everything CSV] " + text5);
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
						list2.Add("⚠\ufe0f Файл hosts был изменён менее 24 часов назад (" + fileInfo.LastWriteTime.ToString("dd.MM.yyyy HH:mm") + ")!");
					}
					string[] array = File.ReadAllLines(text);
					string[] array2 = array;
					foreach (string text2 in array2)
					{
						string text3 = text2.Trim();
						if (!text3.StartsWith("#") && !string.IsNullOrEmpty(text3))
						{
							string text4 = text3.ToLowerInvariant();
							if (text4.Contains("mojang") || text4.Contains("minecraft") || text4.Contains("hypixel") || text4.Contains("atlantis") || text4.Contains("funtime") || text4.Contains("authserver") || text4.Contains("sessionserver") || text4.Contains("easymc") || text4.Contains("thealtening") || text4.Contains("mcleaks") || text4.Contains("polar.gg") || text4.Contains("grim.ac") || text4.Contains("authlib"))
							{
								list2.Add("\ud83d\udea8 Перенаправление игрового домена в hosts: " + text3);
							}
						}
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Сеть / DNS";
					checkResult.Name = "Файл hosts (Подмена DNS / Блокировка античитов)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Warning;
					checkResult.Detail = "Обнаружены изменения файла hosts или перенаправление игровых серверов: " + list2.Count + " шт.";
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
				string[] array = new string[2]
				{
					Path.GetTempPath(),
					Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\Temp"
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
						string[] files = Directory.GetFiles(path, "*.*", SearchOption.TopDirectoryOnly);
						foreach (string text in files)
						{
							string text2 = Path.GetFileName(text).ToLowerInvariant();
							if (text2.Contains("jnativehook") || text2.Contains("autoclicker") || text2.Contains("ghook") || text2.Contains("keyhook"))
							{
								list2.Add("Хук-библиотека автокликера в Temp: " + text);
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
					checkResult.Category = "Макросы / Кликеры";
					checkResult.Name = "JNativeHook и хук-библиотеки внешних автокликеров (Temp)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "В системных каталогах Temp найдены распакованные библиотеки внешних кликеров: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Макросы / Кликеры";
					checkResult2.Name = "JNativeHook и хук-библиотеки внешних автокликеров";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Следов библиотек JNativeHook в Temp не обнаружено.";
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Макросы / Кликеры";
				checkResult3.Name = "JNativeHook";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка проверки JNativeHook: " + ex.Message;
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
				string path = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft\\resourcepacks");
				if (Directory.Exists(path))
				{
					string[] files = Directory.GetFiles(path, "*.zip", SearchOption.TopDirectoryOnly);
					foreach (string text in files)
					{
						try
						{
							using (ZipArchive zipArchive = ZipFile.OpenRead(text))
							{
								foreach (ZipArchiveEntry entry in zipArchive.Entries)
								{
									string text2 = entry.FullName.ToLowerInvariant();
									if ((text2.Contains("textures/entity/chest/") || text2.Contains("textures/block/spawner.png") || text2.Contains("textures/entity/spawner.png")) && entry.Length > 0 && entry.Length < 700)
									{
										list2.Add("🟣 [Прозрачный Chest / Spawner ESP в архиве] " + entry.Name + " (" + entry.Length + " байт) в: " + text);
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
										list2.Add("🟣 [Прозрачный Chest / Spawner ESP в папке] " + Path.GetFileName(png) + " (" + fi.Length + " байт) в папке: " + packDir);
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
				checkResult3.Category = "Визуалы";
				checkResult3.Name = "Chest ESP";
				checkResult3.Detected = false;
				checkResult3.Severity = CheckSeverity.Info;
				checkResult3.Detail = "Ошибка анализа ресурспаков: " + ex.Message;
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
							string text2 = text.ToLowerInvariant();
							string[] cheatClientNames = CheatClientNames;
							foreach (string text3 in cheatClientNames)
							{
								int startIndex = 0;
								while ((startIndex = text2.IndexOf(text3, startIndex, StringComparison.OrdinalIgnoreCase)) != -1)
								{
									if (MatchesCheatName(text3, text3))
									{
										int num = Math.Max(0, startIndex - 40);
										int length = Math.Min(120, text.Length - num);
										string text4 = text.Substring(num, length).Replace('\0', ' ').Trim();
										Match match = Regex.Match(text4, "[a-zA-Z]:\\\\[^\\s\\0<>\"\\|]+");
										if (match.Success)
										{
											string value = match.Value;
											if (!list2.Contains(value) && !value.ToLower().Contains("lunar"))
											{
												list2.Add(value);
											}
										}
										else
										{
											string text5 = text4.Trim();
											if (!list2.Contains(text5) && !text5.ToLower().Contains("lunar") && text5.Length > 5)
											{
												list2.Add("[" + text3 + "] " + text5);
											}
										}
									}
									startIndex += text3.Length + 1;
								}
							}
							string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
							foreach (string ec in extraCheatClients)
							{
								if (ec.Length < 4) continue;
								int startIndex = 0;
								while ((startIndex = text2.IndexOf(ec.ToLowerInvariant(), startIndex, StringComparison.OrdinalIgnoreCase)) != -1)
								{
									int num = Math.Max(0, startIndex - 40);
									int length = Math.Min(120, text.Length - num);
									string text4 = text.Substring(num, length).Replace('\0', ' ').Trim();
									Match match = Regex.Match(text4, "[a-zA-Z]:\\\\[^\\s\\0<>\"\\|]+");
									if (match.Success)
									{
										string value = match.Value;
										if (!list2.Contains(value) && !value.ToLower().Contains("lunar"))
										{
											list2.Add("[ExtraCheat] " + value);
										}
									}
									else
									{
										string text5 = text4.Trim();
										if (!list2.Contains(text5) && !text5.ToLower().Contains("lunar") && text5.Length > 5)
										{
											list2.Add("[ExtraCheat " + ec + "] " + text5);
										}
									}
									startIndex += ec.Length + 1;
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
					checkResult.Detail = "В системном кэше совместимости ShimCache обнаружены следы читов: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2.Take(25));
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Реестр / Фоновые следы";
					checkResult2.Name = "ShimCache (AppCompatCache)";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Следов известных читов в ShimCache не найдено.";
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
				checkResult3.Detail = "Доступ к ShimCache ограничен: " + ex.Message;
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
						TimeSpan timeSpan = DateTime.Now - fileInfo.LastWriteTime;
						if (timeSpan.TotalMinutes < 15.0)
						{
							list2.Add("⚠️ Amcache.hve был модифицирован всего " + Math.Round(timeSpan.TotalMinutes, 1) + " мин назад (возможное затирание кэша перед проверкой)!");
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
								string text2 = (registryKey2.GetValue("DisplayName") ?? "").ToString();
								if (string.IsNullOrEmpty(text2))
								{
									continue;
								}
								string textLower = text2.ToLowerInvariant();
								bool matched = false;
								string[] cheatClientNames = CheatClientNames;
								foreach (string cheatLower in cheatClientNames)
								{
									if (MatchesCheatName(textLower, cheatLower))
									{
										list2.Add(string.Concat("[Установленное ПО] ", text2, " (Путь: ", registryKey2.GetValue("InstallLocation"), ")"));
										matched = true;
										break;
									}
								}
								if (!matched)
								{
									string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
									foreach (string ec in extraCheatClients)
									{
										if (ec.Length >= 4 && MatchesCheatName(textLower, ec))
										{
											list2.Add(string.Concat("[Установленное ПО - ExtraCheat] ", text2, " (Путь: ", registryKey2.GetValue("InstallLocation"), ")"));
											break;
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
					checkResult.Category = "Программы / Деинсталляция";
					checkResult.Name = "Amcache и записи деинсталляции Windows (Uninstall)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "Обнаружены установленные чит-программы или подозрительные изменения Amcache: " + list2.Count + " шт.";
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
					checkResult2.Detail = "Следов установки сторонних чит-лаунчеров не обнаружено.";
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

		public static List<CheckResult> CheckNetworkSocketsAndConnections()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				List<string> list2 = new List<string>();
				ProcessStartInfo processStartInfo = new ProcessStartInfo("netstat", "-ano -p tcp");
				processStartInfo.RedirectStandardOutput = true;
				processStartInfo.UseShellExecute = false;
				processStartInfo.CreateNoWindow = true;
				using (Process process = Process.Start(processStartInfo))
				{
					string text = process.StandardOutput.ReadToEnd();
					process.WaitForExit(4000);
					HashSet<int> hashSet = new HashSet<int>();
					Process[] processesByName = Process.GetProcessesByName("javaw");
					foreach (Process process2 in processesByName)
					{
						hashSet.Add(process2.Id);
					}
					Process[] processesByName2 = Process.GetProcessesByName("java");
					foreach (Process process3 in processesByName2)
					{
						hashSet.Add(process3.Id);
					}
					string[] array = text.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
					string[] array2 = array;
					foreach (string text2 in array2)
					{
						if (!text2.Contains("ESTABLISHED"))
						{
							continue;
						}
						string[] array3 = Regex.Split(text2.Trim(), "\\s+");
						int result;
						if (array3.Length >= 5 && int.TryParse(array3[array3.Length - 1], out result) && hashSet.Contains(result))
						{
							string text3 = array3[2];
							if (text3.EndsWith(":6667") || text3.EndsWith(":8088") || text3.EndsWith(":1337") || text3.EndsWith(":7777"))
							{
								list2.Add("\ud83d\udea8 javaw.exe (PID=" + result + ") активное подозрительное TCP соединение: " + text3);
							}
						}
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Сеть / Соединения";
					checkResult.Name = "Активные сетевые сокеты javaw.exe (IRC / Cloud Configs)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "Обнаружены подозрительные исходящие соединения Minecraft: " + list2.Count + " шт.";
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
					checkResult2.Detail = "Подозрительных сокетов IRC/Cloud у Minecraft не обнаружено.";
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
				checkResult3.Detail = "Ошибка проверки сокетов: " + ex.Message;
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
								list2.Add(string.Format("🚨 Чит в памяти javaw (PID={0}): {1} | {2} (Адрес: {3}, Защита: {4}, Размер: {5})", process.Id, hit.Type, hit.Detail, hit.Address, hit.Protection, hit.Size));
							}
						}

						// 2. Scan loaded DLL modules for hooking engines and unauthorized paths
						List<MemoryRegionHit> modHits = ProcessMemoryScanner.CheckJavawLoadedModules(process);
						foreach (MemoryRegionHit mHit in modHits)
						{
							if (mHit.Severity == CheckSeverity.Critical || mHit.Severity == CheckSeverity.NeedsCheck)
							{
								list2.Add(string.Format("🚨 Внедрённый модуль в javaw (PID={0}): {1} (Адрес: {2})", process.Id, mHit.Detail, mHit.Address));
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
					checkResult2.Detail = "В памяти активного процесса Minecraft обнаружены сигнатуры читов и инжекций: " + list2.Count + " шт.!";
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
					checkResult3.Detail = "Сигнатур известных читов и инжекций в памяти активного процесса Minecraft не найдено.";
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
				string text = (result.Detected ? ((result.Severity == CheckSeverity.Critical) ? "БАН" : ((result.Severity == CheckSeverity.NeedsCheck) ? "ВИЗУАЛ" : "СЛЕД")) : "ЧИСТО");
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
				string[] array = new string[2] { "Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Layers", "Software\\Microsoft\\Windows NT\\CurrentVersion\\AppCompatFlags\\Compatibility Assistant\\Store" };
				List<string> list2 = new List<string>();
				string[] array2 = array;
				foreach (string name in array2)
				{
					using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name))
					{
						if (registryKey != null)
						{
							string[] valueNames = registryKey.GetValueNames();
							foreach (string text in valueNames)
							{
								if (string.IsNullOrEmpty(text))
								{
									continue;
								}
								string textLower = text.ToLowerInvariant();
								string[] cheatClientNames = CheatClientNames;
								foreach (string cheatLower in cheatClientNames)
								{
									if (MatchesCheatName(textLower, cheatLower))
									{
										list2.Add("[HKCU Layers] " + text + " = " + registryKey.GetValue(text));
										break;
									}
								}
							}
						}
					}
					try
					{
						using (RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey(name))
						{
							if (registryKey2 == null)
							{
								continue;
							}
							string[] valueNames2 = registryKey2.GetValueNames();
							foreach (string text2 in valueNames2)
							{
								if (string.IsNullOrEmpty(text2))
								{
									continue;
								}
								string textLower2 = text2.ToLowerInvariant();
								string[] cheatClientNames2 = CheatClientNames;
								foreach (string cheatLower2 in cheatClientNames2)
								{
									if (MatchesCheatName(textLower2, cheatLower2))
									{
										list2.Add("[HKLM Layers] " + text2 + " = " + registryKey2.GetValue(text2));
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
					checkResult.Category = "Реестр / Фоновые следы";
					checkResult.Name = "AppCompatFlags Layers (Совместимость / Запуск от админа)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "Обнаружены записи запуска читов в параметрах совместимости: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Реестр / Фоновые следы";
					checkResult2.Name = "AppCompatFlags Layers";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Записей читов в AppCompatFlags Layers не обнаружено.";
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
				checkResult3.Detail = "Ошибка проверки Layers: " + ex.Message;
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
						foreach (string text2 in valueNames)
						{
							if (string.IsNullOrEmpty(text2))
							{
								continue;
							}
							string textLower = text2.ToLowerInvariant();
							string[] cheatClientNames = CheatClientNames;
							foreach (string cheatLower in cheatClientNames)
							{
								if (MatchesCheatName(textLower, cheatLower))
								{
									list2.Add("[" + text.Split('\\').Last() + "] " + text2);
									break;
								}
							}
						}
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Реестр / Фоновые следы";
					checkResult.Name = "FeatureUsage AppSwitched (Переключение окон / Запуски Windows 10/11)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "В телеметрии активности окон обнаружены читы: " + list2.Count + " шт.";
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
							string text2 = "";
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
				checkResult.Detected = list2.Any((string d) => d.Contains("[USB"));
				checkResult.Severity = (checkResult.Detected ? CheckSeverity.Warning : CheckSeverity.Info);
				checkResult.Detail = "История подключений дисков Windows: " + list2.Count + " накопителей.";
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
				sb.Append("],");
				sb.Append("\"footer\":{\"text\":\"MC Cheat Checker Community Edition \u2022 " + report.ScanTime.ToString("dd.MM.yyyy HH:mm:ss") + "\"}");
				sb.Append("}]}");

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
				string[] array = new string[] { "Software\\Classes\\Local Settings\\Software\\Microsoft\\Windows\\Shell\\MuiCache", "Software\\Microsoft\\Windows\\ShellNoRoam\\MUICache" };
				List<string> list2 = new List<string>();
				string[] array2 = array;
				foreach (string name in array2)
				{
					using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name))
					{
						if (registryKey == null)
						{
							continue;
						}
						string[] valueNames = registryKey.GetValueNames();
						foreach (string text in valueNames)
						{
							if (string.IsNullOrEmpty(text))
							{
								continue;
							}
							string textLower = text.ToLowerInvariant();
							bool matched = false;
							string[] cheatClientNames = CheatClientNames;
							foreach (string cheatLower in cheatClientNames)
							{
								if (MatchesCheatName(textLower, cheatLower))
								{
									object value = registryKey.GetValue(text);
									string text2 = ((value != null) ? (" (" + value.ToString() + ")") : "");
									list2.Add(text + text2);
									matched = true;
									break;
								}
							}
							if (!matched)
							{
								string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
								foreach (string ec in extraCheatClients)
								{
									if (ec.Length >= 4 && MatchesCheatName(textLower, ec))
									{
										object value = registryKey.GetValue(text);
										string text2 = ((value != null) ? (" (" + value.ToString() + ")") : "");
										list2.Add("[ExtraCheat] " + text + text2);
										break;
									}
								}
							}
						}
					}
				}
				if (list2.Count > 0)
				{
					CheckResult checkResult = new CheckResult();
					checkResult.Category = "Реестр / Фоновые следы";
					checkResult.Name = "MuiCache (История запуска исполняемых файлов в Explorer)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "В реестре MuiCache обнаружены записи о запуске читов: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2.Take(25));
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
				checkResult3.Name = "MuiCache";
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
				string name = "Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\RecentDocs";
				using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(name))
				{
					if (registryKey != null)
					{
						string[] subKeyNames = registryKey.GetSubKeyNames();
						string[] array = new string[] { ".jar", ".zip", ".exe", ".dll", ".bat", ".rar", ".7z", ".cmd", ".ps1", ".vbs", ".lnk" };
						string[] array2 = subKeyNames;
						foreach (string text in array2)
						{
							bool flag = false;
							string[] array3 = array;
							foreach (string value in array3)
							{
								if (text.Equals(value, StringComparison.OrdinalIgnoreCase))
								{
									flag = true;
									break;
								}
							}
							if (!flag)
							{
								continue;
							}
							using (RegistryKey registryKey2 = registryKey.OpenSubKey(text))
							{
								if (registryKey2 == null)
								{
									continue;
								}
								string[] valueNames = registryKey2.GetValueNames();
								foreach (string text2 in valueNames)
								{
									if (text2.Equals("MRUListEx", StringComparison.OrdinalIgnoreCase))
									{
										continue;
									}
									byte[] array4 = registryKey2.GetValue(text2) as byte[];
									if (array4 == null || array4.Length <= 2)
									{
										continue;
									}
									string text3 = Encoding.Unicode.GetString(array4);
									char[] trimChars = new char[1];
									string text4 = text3.TrimEnd(trimChars);
									string textLower = text4.ToLowerInvariant();
									bool matched = false;
									string[] cheatClientNames = CheatClientNames;
									foreach (string cheatLower in cheatClientNames)
									{
										if (MatchesCheatName(textLower, cheatLower))
										{
											list2.Add("[" + text + "] " + text4);
											matched = true;
											break;
										}
									}
									if (!matched)
									{
										string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
										foreach (string ec in extraCheatClients)
										{
											if (ec.Length >= 4 && MatchesCheatName(textLower, ec))
											{
												list2.Add("[" + text + " - ExtraCheat] " + text4);
												break;
											}
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
					checkResult.Category = "Реестр / Фоновые следы";
					checkResult.Name = "RecentDocs MRU (.jar, .dll, .exe обращения в системе)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "В истории недавних файлов RecentDocs найдены следы читов: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2.Take(25));
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Реестр / Фоновые следы";
					checkResult2.Name = "RecentDocs MRU";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Следов чит-файлов в RecentDocs не обнаружено.";
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
				string[] array = new string[128]
				{
					"autoclicker", "auto clicker", "op auto clicker", "speedautoclicker", "mangoclicker", "xdotool", "autohotkey", "macro clicker", "clickmaster", "clickstorm",
					"turbo clicker", "rapid clicker", "vape", "vape client", "celestial", "celestial client", "expensive", "nursultan", "minced", "wild client",
					"akrien", "fluger", "meteor client", "meteor", "wurst", "liquidbounce", "liquid bounce", "prax", "horion", "zephyr",
					"sigma client", "sigma", "neverhook", "impact client", "impact", "kamiblue", "kami blue", "kami", "inertia", "inertia client",
					"aristois", "baritone", "rusherhack", "rusher hack", "future client", "future", "skillclient", "skill client", "jigsaw client", "jigsaw",
					"zeus client", "zeus", "salhack", "wolfram", "novoline", "huzuni", "entropy client", "entropy", "prism client", "prism",
					"flarial client", "flarial", "flux client", "flux", "orion client", "orion", "tenacity", "tenacity client", "raven client", "raven",
					"sora client", "sora", "dummie client", "dummie", "dummi client", "dummi", "rise client", "rogue client", "remix client", "ghost client",
					"ghost", "drip client", "drip", "paragon client", "paragon", "catalyst client", "catalyst", "stardust client", "stardust", "cortex client",
					"cortex", "aoba client", "aoba", "fdp client", "fdp", "darklight client", "jex client", "jex", "novaclient", "nova client",
					"atomic", "eclipse client", "cheat engine", "cheat-engine", "cheatengine", "process hacker", "processhacker", "x64dbg", "x32dbg", "ollydbg",
					"windbg", "dnspy", "dotpeek", "ilspy", "justdecompile", "xenos", "extreme injector", "extremeinjector", "liquid loader", "liquidloader",
					"ambient", "blatware", "slavehack", "slavware", "rusohack", "atomware", "neonware", "quantumware"
				};
				Process[] processes = Process.GetProcesses();
				Process[] array2 = processes;
				foreach (Process process in array2)
				{
					try
					{
						string mainWindowTitle = process.MainWindowTitle;
						if (string.IsNullOrEmpty(mainWindowTitle))
						{
							continue;
						}
						string text = mainWindowTitle.ToLowerInvariant();
						list3.Add(string.Format("PID={0} [{1}]: {2}", process.Id, process.ProcessName, mainWindowTitle));
						bool flag = false;
						string[] array3 = array;
						foreach (string text2 in array3)
						{
							if (text.Contains(text2))
							{
								list2.Add(string.Format("\ud83d\udd34 [ЗАГОЛОВОК ЧИТА] PID={0} [{1}] заголовок: \"{2}\" (совпадение: {3})", process.Id, process.ProcessName, mainWindowTitle, text2));
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
								list2.Add(string.Format("\ud83d\udd34 [ЗАГОЛОВОК ЧИТА - БАЗА] PID={0} [{1}] заголовок: \"{2}\" (клиент: {3})", process.Id, process.ProcessName, mainWindowTitle, text3));
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
					checkResult.Name = "Заголовки активных окон (Чит-клиенты / Инжекторы / Кликеры)";
					checkResult.Detected = true;
					checkResult.Severity = CheckSeverity.Critical;
					checkResult.Detail = "\ud83d\udd34 Обнаружены открытые окна запрещённого ПО: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2) + "\r\n\r\n── Все открытые окна ──\r\n" + string.Join("\r\n", list3);
					list.Add(checkResult);
				}
				else
				{
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Процессы / Память";
					checkResult2.Name = "Заголовки активных окон";
					checkResult2.Detected = false;
					checkResult2.Severity = CheckSeverity.Info;
					checkResult2.Detail = "Окон запрещённого ПО и кликеров не обнаружено. Всего окон: " + list3.Count;
					checkResult2.Evidence = string.Join("\r\n", list3);
					list.Add(checkResult2);
				}
			}
			catch (Exception ex)
			{
				CheckResult checkResult3 = new CheckResult();
				checkResult3.Category = "Процессы / Память";
				checkResult3.Name = "Заголовки активных окон";
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
							list2.Add("🚨 Обнаружен параметр -javaagent: (внедрение байткода) в PID=" + process.Id);
						}
						if (text2.Contains("-xbootclasspath"))
						{
							list2.Add("⚠️ Обнаружен параметр -Xbootclasspath в PID=" + process.Id);
						}
						if (text2.Contains("-agentlib:") || text2.Contains("-agentpath:"))
						{
							list2.Add("🚨 Обнаружен агент отладки/профайлера (-agentlib / -agentpath) в PID=" + process.Id);
						}
						if (text2.Contains("-xrunjdwp"))
						{
							list2.Add("🚨 Обнаружен удалённый отладчик JDWP (-Xrunjdwp) в PID=" + process.Id);
						}
						if (text2.Contains("-noverify") || text2.Contains("-xverify:none"))
						{
							list2.Add("🚨 Отключена верификация байткода (-noverify / -Xverify:none) — обход проверок целостности JVM в PID=" + process.Id);
						}
						if (text2.Contains("-dfabric.dli.main=") || text2.Contains("-djava.system.class.loader="))
						{
							list2.Add("🚨 Подмена главного класса загрузчика / ClassLoader в аргументах JVM в PID=" + process.Id);
						}
						if (text2.Contains("-xx:+disableattachmechanism"))
						{
							list2.Add("⚠️ Запрет подключения к JVM (-XX:+DisableAttachMechanism) в PID=" + process.Id);
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
								list2.Add("🚨 Чит-клиент (ExtraCheatClients) в аргументах JVM [" + ec + "] в PID=" + process.Id);
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
					CheckResult checkResult2 = new CheckResult();
					checkResult2.Category = "Minecraft";
					checkResult2.Name = "Аргументы запуска JVM (javaw.exe) — Внедрение / Читы";
					checkResult2.Detected = true;
					checkResult2.Severity = CheckSeverity.Critical;
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
			stringBuilder.AppendLine("```diff");
			stringBuilder.AppendLine("+ =================================================== +");
			stringBuilder.AppendLine("+        MC CHEAT CHECKER - SCREENSHARE REPORT (Community Edition)   +");
			stringBuilder.AppendLine("+ =================================================== +");
			stringBuilder.AppendLine("```");
			stringBuilder.AppendLine("> **\ud83d\udc64 Игрок:** `" + report.PlayerName + "`");
			stringBuilder.AppendLine("> **\ud83d\udee1\ufe0f Модератор:** `" + (string.IsNullOrEmpty(moderatorName) ? Environment.UserName : moderatorName) + "`");
			stringBuilder.AppendLine("> **⏰ Время:** `" + report.ScanTime.ToString("dd.MM.yyyy HH:mm:ss") + "`");
			int num = report.Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.Critical);
			int num2 = report.Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.NeedsCheck);
			int num3 = report.Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.Warning);
			string text = "\ud83d\udfe2 **ИГРОК ЧИСТ**";
			if (num > 0)
			{
				text = "\ud83d\udd34 **БАН (ОБНАРУЖЕНЫ ЧИТЫ)**";
			}
			else if (num2 > 0)
			{
				text = "\ud83d\udfe3 **ВИЗУАЛЫ (ТРЕБУЕТСЯ ПРОВЕРКА)**";
			}
			else if (num3 > 0)
			{
				text = "\ud83d\udfe1 **ПОДОЗРИТЕЛЬНО (СЛЕДЫ/ОЧИСТКА)**";
			}
			stringBuilder.AppendLine("> **⚖\ufe0f Вердикт:** " + text);
			stringBuilder.AppendLine("> **\ud83d\udcca Сводка:** \ud83d\udd34 Читы: `" + num + "` | \ud83d\udfe3 Визуалы: `" + num2 + "` | \ud83d\udfe1 Следы: `" + num3 + "`");
			stringBuilder.AppendLine();
			List<CheckResult> list = report.Results.Where((CheckResult r) => r.Detected).ToList();
			if (list.Count > 0)
			{
				stringBuilder.AppendLine("**\ud83d\udea8 Обнаруженные улики:**");
				foreach (CheckResult item in list)
				{
					string text2 = ((item.Severity == CheckSeverity.Critical) ? "\ud83d\udd34" : ((item.Severity == CheckSeverity.NeedsCheck) ? "\ud83d\udfe3" : "\ud83d\udfe1"));
					stringBuilder.AppendLine(text2 + " **[" + item.Category + "] " + item.Name + "**");
					stringBuilder.AppendLine("   ⤷ *" + item.Detail + "*");
					if (!string.IsNullOrEmpty(item.Evidence))
					{
						string[] array = item.Evidence.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
						stringBuilder.AppendLine("```");
						for (int num4 = 0; num4 < Math.Min(array.Length, 5); num4++)
						{
							stringBuilder.AppendLine(array[num4]);
						}
						if (array.Length > 5)
						{
							stringBuilder.AppendLine("... и ещё " + (array.Length - 5) + " записей");
						}
						stringBuilder.AppendLine("```");
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
				return trimmed + "\r\n\r\n... [⚠️ Список улик сокращён для лимита Discord (2000 симв.). Полный отчёт сохранён в HTML/TXT]";
			}
			return resultStr;
		}

		public static List<CheckResult> CheckBAM()
		{
			List<CheckResult> list = new List<CheckResult>();
			try
			{
				string[] array = new string[2] { "SYSTEM\\CurrentControlSet\\Services\\bam\\State\\UserSettings", "SYSTEM\\CurrentControlSet\\Services\\bam\\UserSettings" };
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
										continue;
									}
									string arg2 = "";
									try
									{
										object value = registryKey2.GetValue(text);
										byte[] array3 = value as byte[];
										if (array3 != null && array3.Length >= 8)
										{
											long num = BitConverter.ToInt64(array3, 0);
											if (num > 0)
											{
												DateTime dateTime = DateTime.FromFileTime(num);
												arg2 = string.Format(" [Запуск: {0:dd.MM.yyyy HH:mm:ss}]", dateTime);
											}
										}
									}
									catch
									{
									}
									list2.Add(string.Format("\ud83d\udea8 [{0}] {1}{2}", arg, text, arg2));
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
					checkResult.Detail = "В реестре BAM обнаружены подтверждённые записи прямого запуска чит-программ: " + list2.Count + " шт.";
					checkResult.Evidence = string.Join("\r\n", list2.Take(30));
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
					list2.Add("⚠️ Перезагрузка системы выполнена всего " + Math.Round(timeSpan.TotalMinutes, 1) + " мин назад (возможный сброс RAM / Prefetch / кэша перед проверкой)!");
				}

				string[] criticalServices = new string[] { "PcaSvc", "DPS", "EventLog", "SysMain", "bam", "W32Time", "DiagTrack" };
				string[] criticalNames = new string[]
				{
					"Служба отслеживания совместимости PcaSvc",
					"Служба диагностических политик DPS",
					"Журнал событий Windows EventLog",
					"Служба кэширования Prefetch (SysMain)",
					"Служба фоновой активности (BAM)",
					"Служба системного времени (W32Time)",
					"Служба телеметрии и сбоев (DiagTrack)"
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
									list2.Add(string.Format("🚨 {0} ОТКЛЮЧЕНА (Start=4)! Прямой признак сокрытия следов / саботажа криминалистики.", criticalNames[i]));
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
					checkResult.Category = "Очистка / Evasion";
					checkResult.Name = "Целостность системы и уклонение от проверки (Evasion)";
					checkResult.Detected = true;
					checkResult.Severity = ((list2.Any(x => x.Contains("🚨"))) ? CheckSeverity.Critical : CheckSeverity.Warning);
					checkResult.Detail = "Выявлены признаки подготовки к проверке / отключения служб логирования: " + list2.Count + " шт.";
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
					checkResult2.Detail = "Все ключевые службы (PcaSvc, DPS, EventLog, SysMain, BAM, W32Time) активны. Аптайм ПК: " + (int)timeSpan.TotalHours + "ч " + timeSpan.Minutes + "м " + timeSpan.Seconds + "с";
					checkResult2.Evidence = "Аптайм ПК: " + (int)timeSpan.TotalHours + "ч " + timeSpan.Minutes + "м " + timeSpan.Seconds + "с\r\nКлючевые системные службы: Включены";
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
				checkResult3.Detail = "Ошибка анализа служб: " + ex.Message;
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
			stringBuilder.AppendLine("</style>");
			stringBuilder.AppendLine("</head>");
			stringBuilder.AppendLine("<body>");
			stringBuilder.AppendLine("<div class='container'>");
			stringBuilder.AppendLine("<div class='card'>");
			stringBuilder.AppendLine("<div class='header-title'>⚔\ufe0f MC CHEAT CHECKER — ОТЧЁТ ПРОВЕРКИ</div>");
			stringBuilder.AppendLine("<div class='header-sub'>Полный комплексный аудит системы на запрещённые модификации, визуалы и читы (Open Source Framework)</div>");
			stringBuilder.AppendLine("<div class='meta-grid'>");
			stringBuilder.AppendLine("<div class='meta-item'><div class='meta-label'>Игрок</div><div class='meta-value'>" + report.PlayerName + "</div></div>");
			stringBuilder.AppendLine("<div class='meta-item'><div class='meta-label'>Модератор</div><div class='meta-value'>" + moderatorName + "</div></div>");
			stringBuilder.AppendLine("<div class='meta-item'><div class='meta-label'>Время проверки</div><div class='meta-value'>" + report.ScanTime.ToString("dd.MM.yyyy HH:mm:ss") + "</div></div>");
			stringBuilder.AppendLine("<div class='meta-item'><div class='meta-label'>Вердикт</div><div class='meta-value'>" + report.Verdict + "</div></div>");
			stringBuilder.AppendLine("</div>");
			stringBuilder.AppendLine("</div>");
			stringBuilder.AppendLine("<div class='card'>");
			stringBuilder.AppendLine("<h3 style='margin-top:0;'>Результаты проверок (" + report.Results.Count + ")</h3>");
			stringBuilder.AppendLine("<table>");
			stringBuilder.AppendLine("<thead><tr><th>Статус</th><th>Категория</th><th>Проверка</th><th>Результат / Улики</th></tr></thead>");
			stringBuilder.AppendLine("<tbody>");
			foreach (CheckResult result in report.Results)
			{
				string text = "badge-clean";
				string text2 = "ЧИСТО";
				if (result.Detected)
				{
					if (result.Severity == CheckSeverity.Critical)
					{
						text = "badge-crit";
						text2 = "БАН";
					}
					else if (result.Severity == CheckSeverity.NeedsCheck)
					{
						text = "badge-visual";
						text2 = "ВИЗУАЛ";
					}
					else
					{
						text = "badge-warn";
						text2 = "СЛЕД";
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
			stringBuilder.AppendLine("<div style='text-align:center; color:#505064; font-size:12px; margin-top:20px;'>Сформировано утилитой MC Cheat Checker · Community Edition | Защита от читов</div>");
			stringBuilder.AppendLine("</div></body></html>");
			return stringBuilder.ToString();
		}

		public static string GenerateTextReport(ScanReport report)
		{
			StringBuilder stringBuilder = new StringBuilder();
			stringBuilder.AppendLine("╔══════════════════════════════════════════════════════════════╗");
			stringBuilder.AppendLine("║       MC CHEAT CHECKER — ПОЛНЫЙ ДЕТЕКТ (Community Edition)         ║");
			stringBuilder.AppendLine("╚══════════════════════════════════════════════════════════════╝");
			stringBuilder.AppendLine("  Ник игрока:  " + report.PlayerName);
			stringBuilder.AppendLine("  Время SS:    " + report.ScanTime.ToString("dd.MM.yyyy HH:mm:ss"));
			stringBuilder.AppendLine("  Вердикт:     " + report.Verdict);
			stringBuilder.AppendLine("  \ud83d\udd34 Хард-читы (Бан): " + report.CriticalCount + " | \ud83d\udfe3 Визуалы (Проверить): " + report.VisualsCount + " | \ud83d\udfe1 Предупреждений: " + report.WarningCount);
			stringBuilder.AppendLine(new string('─', 64));
			stringBuilder.AppendLine();
			IEnumerable<IGrouping<string, CheckResult>> enumerable = from r in report.Results
				group r by r.Category;
			foreach (IGrouping<string, CheckResult> item in enumerable)
			{
				stringBuilder.AppendLine("▶ " + item.Key.ToUpper());
				foreach (CheckResult item2 in item)
				{
					string text = "\ud83d\udfe2";
					if (item2.Detected)
					{
						text = ((item2.Severity == CheckSeverity.Critical) ? "\ud83d\udd34" : ((item2.Severity != CheckSeverity.NeedsCheck) ? "\ud83d\udfe1" : "\ud83d\udfe3"));
					}
					else if (item2.Severity == CheckSeverity.Info)
					{
						text = "ℹ\ufe0f";
					}
					stringBuilder.AppendLine("  " + text + " " + item2.Name);
					stringBuilder.AppendLine("     → " + item2.Detail);
					if (!string.IsNullOrEmpty(item2.Evidence))
					{
						string[] array = item2.Evidence.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
						for (int num = 0; num < Math.Min(array.Length, 15); num++)
						{
							stringBuilder.AppendLine("       " + array[num]);
						}
					}
				}
				stringBuilder.AppendLine();
			}
			stringBuilder.AppendLine(new string('─', 64));
			stringBuilder.AppendLine("Сгенерировано MC Cheat Checker v4.0 (Community Edition)");
			return stringBuilder.ToString();
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
			List<string[]> list6 = new List<string[]>();
			list6.Add(new string[2] { "vape", "config.json" });
			list6.Add(new string[2] { "vape", "license.json" });
			list6.Add(new string[2] { "vape", "license.key" });
			list6.Add(new string[2] { "vape", "auth.json" });
			list6.Add(new string[2] { ".vape", "config.json" });
			list6.Add(new string[2] { ".vape", "license.json" });
			list6.Add(new string[2] { "flux", "license.key" });
			list6.Add(new string[2] { "flux", "config.json" });
			list6.Add(new string[2] { "flux", "auth.bin" });
			list6.Add(new string[2] { ".flux", "license.key" });
			list6.Add(new string[2] { "flarial", "license.json" });
			list6.Add(new string[2] { "flarial", "config.json" });
			list6.Add(new string[2] { "Flarial", "license.json" });
			list6.Add(new string[2] { "entropy", "license.key" });
			list6.Add(new string[2] { "entropy", "config.json" });
			list6.Add(new string[2] { "entropy", "auth.json" });
			list6.Add(new string[2] { "liquidbounce", "settings.json" });
			list6.Add(new string[2] { ".liquidbounce", "settings.json" });
			list6.Add(new string[2] { "meteor-client", "auth.json" });
			list6.Add(new string[2] { "meteor-client", "accounts.json" });
			list6.Add(new string[2] { "rusherhack", "license.key" });
			list6.Add(new string[2] { "rusherhack", "config.json" });
			list6.Add(new string[2] { "novoline", "license.key" });
			list6.Add(new string[2] { "novoline", "config.cfg" });
			list6.Add(new string[2] { "sigma", "license.key" });
			list6.Add(new string[2] { "sigma", "config.json" });
			list6.Add(new string[2] { ".sigma", "license.key" });
			list6.Add(new string[2] { "prism", "license.key" });
			list6.Add(new string[2] { "prism-client", "license.key" });
			list6.Add(new string[2] { "tenacity", "license.key" });
			list6.Add(new string[2] { "tenacity", "config.json" });
			list6.Add(new string[2] { "orion", "license.key" });
			list6.Add(new string[2] { "orion-client", "config.json" });
			list6.Add(new string[2] { "raven", "license.key" });
			list6.Add(new string[2] { "raven-client", "license.key" });
			list6.Add(new string[2] { "dummie", "license.key" });
			list6.Add(new string[2] { "dummie", "config.json" });
			list6.Add(new string[2] { "dummi", "license.key" });
			list6.Add(new string[2] { "dummi", "config.json" });
			list6.Add(new string[2] { "cortex", "license.key" });
			list6.Add(new string[2] { "cortex", "auth.json" });
			list6.Add(new string[2] { "celestial", "license.key" });
			list6.Add(new string[2] { "celestial", "config.json" });
			list6.Add(new string[2] { "impact", "license.key" });
			list6.Add(new string[2] { "impact", "config.json" });
			list6.Add(new string[2] { "wolfram", "license.key" });
			list6.Add(new string[2] { "wolfram", "auth.json" });
			list6.Add(new string[2] { "expensive", "license.key" });
			list6.Add(new string[2] { "expensive", "config.json" });
			list6.Add(new string[2] { "akrien", "license.key" });
			list6.Add(new string[2] { "akrien", "config.json" });
			list6.Add(new string[2] { "wurst", "config.json" });
			list6.Add(new string[2] { ".wurst", "config.json" });
			list6.Add(new string[2] { "aristois", "license.key" });
			list6.Add(new string[2] { "aristois", "config.json" });
			list6.Add(new string[2] { "inertia", "license.key" });
			list6.Add(new string[2] { "inertia", "config.json" });
			list6.Add(new string[2] { "nursultan", "license.json" });
			list6.Add(new string[2] { "nursultan", "config.json" });
			list6.Add(new string[2] { "nursultan", "auth.json" });
			list6.Add(new string[2] { ".nursultan", "license.json" });
			list6.Add(new string[2] { ".nursultan", "config.json" });
			list6.Add(new string[2] { "thunderhack", "config.json" });
			list6.Add(new string[2] { ".thunderhack", "config.json" });
			list6.Add(new string[2] { "ThunderHack", "config.json" });
			list6.Add(new string[2] { "slinky", "slinky.json" });
			list6.Add(new string[2] { "slinky", "license.json" });
			list6.Add(new string[2] { ".slinky", "slinky.json" });
			list6.Add(new string[2] { "doomsday", "config.json" });
			list6.Add(new string[2] { ".doomsday", "config.json" });
			list6.Add(new string[2] { "boze", "boze.json" });
			list6.Add(new string[2] { ".boze", "config.json" });
			list6.Add(new string[2] { "seedcrackerx", "config.json" });
			list6.Add(new string[2] { ".seedcrackerx", "seedcrackerx.json" });
			list6.Add(new string[2] { "wildclient", "license.json" });
			list6.Add(new string[2] { "richclient", "license.json" });
			list6.Add(new string[2] { "excellent", "config.json" });
			list6.Add(new string[2] { "minced", "config.json" });
			list6.Add(new string[2] { "neverhook", "config.json" });
			list6.Add(new string[2] { "expensive", "default.json" });
			list6.Add(new string[2] { ".expensive", "config.json" });
			list6.Add(new string[2] { ".expensive", "binds.json" });
			list6.Add(new string[2] { ".expensive", "user.json" });
			List<string[]> list7 = list6;
			string[] array = new string[26]
			{
				"license.key", "license.json", "license.txt", "license.bin", "auth.json", "auth.bin", "auth.txt", "auth.token", "token.json", "token.txt",
				"token.bin", "subscription.json", "subscription.txt", "hwid.json", "hwid.txt", "session.json", "session.bin", "key.json", "key.txt", "key.bin",
				"premium.json", "premium.txt", "account.json", "accounts.json", "user.json", "user.bin"
			};
			string[] array2 = new string[6]
			{
				folderPath,
				folderPath2,
				folderPath3,
				Path.Combine(folderPath3, "Documents"),
				Path.Combine(folderPath3, "Desktop"),
				Path.Combine(folderPath3, "Downloads")
			};
			string[] array3;
			foreach (string[] item in list7)
			{
				array3 = array2;
				foreach (string path in array3)
				{
					try
					{
						string text = Path.Combine(path, item[0], item[1]);
						if (!File.Exists(text))
						{
							continue;
						}
						string text2 = new FileInfo(text).Length + " байт";
						string text3 = "";
						try
						{
							text3 = File.ReadAllText(text, Encoding.UTF8).Replace("\r", "").Replace("\n", " ")
								.Trim();
							if (text3.Length > 200)
							{
								text3 = text3.Substring(0, 200) + "...";
							}
						}
						catch
						{
						}
						list5.Add(string.Format("\ud83d\udcb3 [{0}] конфиг/лицензия найдена: {1} ({2})\n    Содержимое: {3}", item[0].ToUpper(), text, text2, text3));
					}
					catch
					{
					}
				}
			}
			array3 = array2;
			foreach (string path2 in array3)
			{
				if (!Directory.Exists(path2))
				{
					continue;
				}
				try
				{
					string[] directories = Directory.GetDirectories(path2, "*", SearchOption.TopDirectoryOnly);
					foreach (string path3 in directories)
					{
						string textLower = Path.GetFileName(path3).ToLower();
						bool flag = false;
						string text4 = "";
						string[] cheatClientNames = CheatClientNames;
						foreach (string text5 in cheatClientNames)
						{
							if (MatchesCheatName(textLower, text5) && !IsLegitGame(textLower))
							{
								flag = true;
								text4 = text5;
								break;
							}
						}
						if (!flag)
						{
							continue;
						}
						try
						{
							cheatClientNames = Directory.GetFiles(path3, "*", SearchOption.AllDirectories);
							foreach (string text6 in cheatClientNames)
							{
								string text7 = Path.GetFileName(text6).ToLower();
								string[] array4 = array;
								foreach (string text8 in array4)
								{
									if (!(text7 == text8))
									{
										continue;
									}
									string text9 = "";
									try
									{
										text9 = File.ReadAllText(text6, Encoding.UTF8).Replace("\r", "").Replace("\n", " ")
											.Trim();
										if (text9.Length > 200)
										{
											text9 = text9.Substring(0, 200) + "...";
										}
									}
									catch
									{
									}
									list2.Add(string.Format("\ud83d\udd34 [{0}] ФАЙЛ ЛИЦЕНЗИИ/ТОКЕНА: {1}\n    Содержимое: {2}", text4.ToUpper(), text6, text9));
									break;
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
			string[] array5 = new string[26]
			{
				"SOFTWARE\\Vape", "SOFTWARE\\VapeClient", "SOFTWARE\\Flux", "SOFTWARE\\FluxClient", "SOFTWARE\\Flarial", "SOFTWARE\\Entropy", "SOFTWARE\\Sigma", "SOFTWARE\\SigmaClient", "SOFTWARE\\Novoline", "SOFTWARE\\RusherHack",
				"SOFTWARE\\MeteorClient", "SOFTWARE\\LiquidBounce", "SOFTWARE\\Aristois", "SOFTWARE\\Impact", "SOFTWARE\\Wurst", "SOFTWARE\\Celestial", "SOFTWARE\\Cortex", "SOFTWARE\\Dummie", "SOFTWARE\\Dummi", "SOFTWARE\\Wolfram",
				"SOFTWARE\\Expensive", "SOFTWARE\\Akrien", "SOFTWARE\\Inertia", "SOFTWARE\\Prism", "SOFTWARE\\Tenacity", "SOFTWARE\\RavenClient"
			};
			array3 = array5;
			foreach (string text10 in array3)
			{
				try
				{
					using (RegistryKey registryKey = Registry.CurrentUser.OpenSubKey(text10))
					{
						if (registryKey != null)
						{
							string[] valueNames = registryKey.GetValueNames();
							list4.Add(string.Format("\ud83d\udd11 РЕЕСТР [{0}]: найден раздел HKCU\\{1} ({2} значений: {3})", text10.Split('\\')[1], text10, valueNames.Length, string.Join(", ", valueNames)));
						}
					}
					using (RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey(text10))
					{
						if (registryKey2 != null)
						{
							string[] valueNames2 = registryKey2.GetValueNames();
							list4.Add(string.Format("\ud83d\udd11 РЕЕСТР [{0}]: найден раздел HKLM\\{1} ({2} значений: {3})", text10.Split('\\')[1], text10, valueNames2.Length, string.Join(", ", valueNames2)));
						}
					}
				}
				catch
				{
				}
			}
			string[] array6 = new string[5]
			{
				Path.Combine(folderPath2, "Google", "Chrome", "User Data", "Default", "History"),
				Path.Combine(folderPath2, "Google", "Chrome", "User Data", "Profile 1", "History"),
				Path.Combine(folderPath2, "Microsoft", "Edge", "User Data", "Default", "History"),
				Path.Combine(folderPath2, "BraveSoftware", "Brave-Browser", "User Data", "Default", "History"),
				Path.Combine(folderPath, "Mozilla", "Firefox", "Profiles")
			};
			string[] array7 = new string[40]
			{
				"vape.gg", "vape.lol", "vape.pw", "liquidbounce.net", "meteorclient.com", "rusherhack.org", "aristois.net", "sigma.rip", "impact.cx", "inertia.pw",
				"future.gg", "wolfram.cf", "novoline.wtf", "entropy-client", "flux-client", "flarial.xyz", "flarial.net", "celestial-client", "cortexclient", "akrien.wtf",
				"expensive-client", "dreampoolhack", "clowdy.space", "nursultan.fun", "drip.gg", "wurst-client", "salhack", "skillclient", "jigsaw", "/login",
				"/buy", "/purchase", "/checkout", "/activate", "/license", "/auth", "/hwid", "/dashboard", "/subscription", "/premium"
			};
			array3 = array6;
			foreach (string path4 in array3)
			{
				if (!File.Exists(path4) && !Directory.Exists(path4))
				{
					continue;
				}
				try
				{
					string[] directories;
					if (File.Exists(path4))
					{
						byte[] bytes = File.ReadAllBytes(path4);
						string text11 = Encoding.UTF8.GetString(bytes).Replace('\0', ' ');
						directories = array7;
						foreach (string text12 in directories)
						{
							if (text11.IndexOf(text12, StringComparison.OrdinalIgnoreCase) >= 0)
							{
								list3.Add(string.Format("\ud83c\udf10 БРАУЗЕР [{0}]: найдено посещение/авторизация сайта чита: '{1}'", Path.GetDirectoryName(path4).Split('\\')[Path.GetDirectoryName(path4).Split('\\').Length - 2], text12));
							}
						}
					}
					if (!Directory.Exists(path4))
					{
						continue;
					}
					directories = Directory.GetDirectories(path4, "*", SearchOption.TopDirectoryOnly);
					foreach (string path5 in directories)
					{
						string path6 = Path.Combine(path5, "places.sqlite");
						if (!File.Exists(path6))
						{
							continue;
						}
						byte[] bytes2 = File.ReadAllBytes(path6);
						string text13 = Encoding.UTF8.GetString(bytes2).Replace('\0', ' ');
						string[] cheatClientNames = array7;
						foreach (string text14 in cheatClientNames)
						{
							if (text13.IndexOf(text14, StringComparison.OrdinalIgnoreCase) >= 0)
							{
								list3.Add(string.Format("\ud83c\udf10 FIREFOX: найдено посещение/авторизация сайта чита: '{0}'", text14));
							}
						}
					}
				}
				catch
				{
				}
			}
			string[] array8 = new string[5]
			{
				folderPath,
				folderPath2,
				Path.Combine(folderPath3, "Desktop"),
				Path.Combine(folderPath3, "Downloads"),
				Path.Combine(folderPath3, "Documents")
			};
			string[] array9 = new string[3] { "*.key", "*.token", "*.hwid" };
			array3 = array8;
			foreach (string path7 in array3)
			{
				if (!Directory.Exists(path7))
				{
					continue;
				}
				try
				{
					string[] directories = array9;
					foreach (string searchPattern in directories)
					{
						string[] cheatClientNames = Directory.GetFiles(path7, searchPattern, SearchOption.AllDirectories);
						foreach (string text15 in cheatClientNames)
						{
							string text16 = text15.ToLower();
							if (text16.Contains("certificate") || text16.Contains("ssl") || text16.Contains("gpg") || text16.Contains("ssh") || text16.Contains("pgp"))
							{
								continue;
							}
							bool flag2 = false;
							string[] array4 = CheatClientNames;
							foreach (string cheatLower in array4)
							{
								if (MatchesCheatName(text16, cheatLower) && !IsLegitGame(text16))
								{
									flag2 = true;
									break;
								}
							}
							if (!flag2)
							{
								continue;
							}
							string text17 = "";
							try
							{
								text17 = File.ReadAllText(text15, Encoding.UTF8).Replace("\r", "").Replace("\n", " ")
									.Trim();
								if (text17.Length > 150)
								{
									text17 = text17.Substring(0, 150) + "...";
								}
							}
							catch
							{
							}
							list2.Add(string.Format("\ud83d\udd10 КЛЮЧ/ТОКЕН ЧИТА: {0}\n    Содержимое: {1}", text15, text17));
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
			checkResult.Detail = ((list8.Count > 0) ? string.Format("\ud83d\udd34 НАЙДЕНО ФАЙЛОВ ЛИЦЕНЗИЙ/ТОКЕНОВ: {0} — ДОКАЗАТЕЛЬСТВО ПЛАТНОГО ЧИТА!", list8.Count) : "Файлы лицензий и токенов платных чит-клиентов не найдены");
			checkResult.Evidence = string.Join("\r\n\r\n", list8);
			list.Add(checkResult);
			CheckResult checkResult2 = new CheckResult();
			checkResult2.Category = "Подписки / Лицензии";
			checkResult2.Name = "Реестр Windows — ключи платных чит-клиентов";
			checkResult2.Detected = list4.Count > 0;
			checkResult2.Severity = CheckSeverity.Critical;
			checkResult2.Detail = ((list4.Count > 0) ? string.Format("\ud83d\udd34 НАЙДЕНО В РЕЕСТРЕ: {0} записей платных чит-клиентов", list4.Count) : "В реестре записей платных чит-клиентов не найдено");
			checkResult2.Evidence = string.Join("\r\n", list4);
			list.Add(checkResult2);
			List<string> list9 = new List<string>(new HashSet<string>(list3));
			CheckResult checkResult3 = new CheckResult();
			checkResult3.Category = "Подписки / Лицензии";
			checkResult3.Name = "Браузер — посещение сайтов покупки/авторизации чит-клиентов";
			checkResult3.Detected = list9.Count > 0;
			checkResult3.Severity = CheckSeverity.Warning;
			checkResult3.Detail = ((list9.Count > 0) ? string.Format("⚠\ufe0f НАЙДЕНО ПОСЕЩЕНИЙ САЙТОВ ПОКУПКИ ЧИТОВ: {0} — требует уточнения", list9.Count) : "Посещений сайтов покупки/авторизации платных читов не найдено");
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
			List<string> list2 = new List<string>();
			List<string> list3 = new List<string>();
			List<string> list4 = new List<string>();
			List<string> list5 = new List<string>();
			string[] array = new string[7] { ".key", ".license", ".lic", ".hwid", ".token", ".auth", ".sub" };
			string[] array2 = new string[13]
			{
				"hwid.txt", "hwid.json", "hwid.dat", "HWID", "hwid", "token", "token.dat", "auth.dat", "auth.bin", "license",
				"subscription.dat", "session.dat", "credentials.json"
			};
			string[] array3 = new string[5]
			{
				folderPath,
				folderPath2,
				Path.Combine(folderPath3, "Documents"),
				Path.Combine(folderPath3, "Downloads"),
				Path.Combine(folderPath3, "Desktop")
			};
			string[] array4 = array3;
			foreach (string path in array4)
			{
				if (!Directory.Exists(path))
				{
					continue;
				}
				string[] directories;
				try
				{
					directories = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
				}
				catch
				{
					continue;
				}
				string[] array5 = directories;
				foreach (string path2 in array5)
				{
					string fileName = Path.GetFileName(path2);
					if (CheatDatabases.IsWhitelistedPath(path2))
					{
						continue;
					}
					bool flag = false;
					string[] cheatClientNames = CheatClientNames;
					foreach (string cheatLower in cheatClientNames)
					{
						if (MatchesCheatName(fileName.ToLowerInvariant(), cheatLower))
						{
							flag = true;
							break;
						}
					}
					if (!flag)
					{
						continue;
					}
					try
					{
						string[] files = Directory.GetFiles(path2, "*.exe", SearchOption.AllDirectories);
						foreach (string text in files)
						{
							string text2 = Path.GetFileName(text).ToLowerInvariant();
							if ((text2.Contains("loader") || text2.Contains("launcher") || text2.Contains("client") || text2.Contains("inject") || text2.Contains("stub") || text2.Contains("bootstrapper")) && !CheatDatabases.IsWhitelistedPath(text))
							{
								list2.Add("\ud83d\udd34 Лоадер купленного клиента: " + text + " | создан: " + SafeWrite(text) + " | размер: " + SafeLen(text) + " байт");
							}
						}
					}
					catch
					{
					}
					try
					{
						string[] files2 = Directory.GetFiles(path2, "*", SearchOption.AllDirectories);
						foreach (string text3 in files2)
						{
							string text4 = Path.GetFileName(text3).ToLowerInvariant();
							bool flag2 = false;
							string[] array6 = array2;
							foreach (string text5 in array6)
							{
								if (text4 == text5.ToLowerInvariant())
								{
									flag2 = true;
									break;
								}
							}
							if (!flag2)
							{
								string[] array7 = array;
								foreach (string value in array7)
								{
									if (text4.EndsWith(value))
									{
										flag2 = true;
										break;
									}
								}
							}
							if (!flag2 || CheatDatabases.IsWhitelistedPath(text3))
							{
								continue;
							}
							string text6 = "";
							try
							{
								text6 = File.ReadAllText(text3, Encoding.UTF8).Replace("\r", " ").Replace("\n", " ")
									.Trim();
								if (text6.Length > 160)
								{
									text6 = text6.Substring(0, 160) + "...";
								}
							}
							catch
							{
							}
							list3.Add("\ud83d\udd11 Токен/ключ устройства: " + text3 + " | " + text6);
						}
					}
					catch
					{
					}
				}
			}
			try
			{
				List<string> list6 = new List<string>();
				list6.Add(Path.Combine(folderPath2, "Google", "Chrome", "User Data", "Default", "Login Data"));
				list6.Add(Path.Combine(folderPath2, "Google", "Chrome", "User Data", "Profile 1", "Login Data"));
				list6.Add(Path.Combine(folderPath2, "Microsoft", "Edge", "User Data", "Default", "Login Data"));
				list6.Add(Path.Combine(folderPath2, "BraveSoftware", "Brave-Browser", "User Data", "Default", "Login Data"));
				list6.Add(Path.Combine(folderPath2, "Opera Software", "Opera Stable", "Login Data"));
				list6.Add(Path.Combine(folderPath, "Vivaldi", "User Data", "Default", "Login Data"));
				List<string> list7 = list6;
				foreach (string item in list7)
				{
					if (!File.Exists(item))
					{
						continue;
					}
					byte[] bytes;
					try
					{
						bytes = File.ReadAllBytes(item);
					}
					catch
					{
						continue;
					}
					string text7 = Encoding.UTF8.GetString(bytes);
					array4 = CheatDatabases.ExtraCheatDomains;
					foreach (string text8 in array4)
					{
						string text9 = text8.Split('/')[0];
						if (text7.IndexOf(text9, StringComparison.OrdinalIgnoreCase) >= 0)
						{
							list4.Add("\ud83c\udf10 В сохранённых паролях браузера есть домен чит-сервиса: " + text9 + " (" + item + ")");
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				string folderPath4 = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string path3 = Path.Combine(folderPath4, "System32", "Tasks");
				if (Directory.Exists(path3))
				{
					array4 = Directory.GetFiles(path3, "*", SearchOption.AllDirectories);
					foreach (string text10 in array4)
					{
						string text11 = "";
						try
						{
							text11 = Encoding.UTF8.GetString(File.ReadAllBytes(text10));
							if (text11.Length > 6000)
							{
								text11 = text11.Substring(0, 6000);
							}
						}
						catch
						{
							continue;
						}
						string text12 = text11.ToLowerInvariant();
						string[] array5 = CheatClientNames;
						foreach (string text13 in array5)
						{
							if (text13.Length >= 6 && text12.Contains(text13))
							{
								if (!CheatDatabases.IsAmbiguous(text13) || CheatDatabases.HasCheatContext(text12))
								{
									list5.Add("\ud83d\udcc5 Задача планировщика содержит имя клиента [" + text13 + "]: " + text10);
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
			checkResult.Severity = ((list8.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Detail = ((list8.Count > 0) ? string.Format("\ud83d\udd34 НАЙДЕНО доказательств пользования купленным клиентом: {0} (лоадеров: {1}, токенов: {2})", list8.Count, list2.Count, list3.Count) : "Лоадеров и HWID-токенов платных клиентов не найдено");
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
			checkResult2.Detail = ((list9.Count > 0) ? string.Format("⚠\ufe0f Косвенные следы покупки/автозапуска клиента: {0}", list9.Count) : "Сохранённых логинов чит-сервисов и задач планировщика не найдено");
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
							Name = "Подозрительный процесс: " + item.Name,
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
							Category = "Память",
							Name = "Индикатор в памяти javaw: " + item2.Type,
							Detail = item2.Detail + " (" + item2.Protection + ", " + item2.Size + ")",
							Detected = true,
							Severity = item2.Severity,
							Evidence = "Адрес: " + item2.Address
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
							Detail = item.ThreatTitle + " (Сигнатур: " + item.MatchedSignatures.Count + ", классов: " + item.TotalClassesInspected + ")",
							Detected = true,
							Severity = item.Severity,
							Evidence = item.FilePath + "\r\nСигнатуры: " + item.GetMatchedStringSummary()
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
								list2.Add(string.Format("Версия {0}: размер {1} КБ (эталон мануала: {2} КБ, разница {3} КБ) → {4}", fileName, num, value, Math.Abs(num - value), text2));
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
								list2.Add(string.Format("\ud83d\udea8 {0} | Размер: {1:N0} байт (ТОЧНОЕ СОВПАДЕНИЕ с базой RageTime/WildGrief) | Изменён: {2:dd.MM.yyyy HH:mm} -> {3}", Path.GetFileName(text), fileInfo.Length, fileInfo.LastWriteTime, text));
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
			checkResult.Category = "Сигнатуры читов";
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
				Path.Combine(folderPath, ".tlauncher", "legacy", "minecraft", "crash-reports"),
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
										list2.Add(string.Format("\ud83d\udea8 [{0}] Чит в отчёте сбоя: [{1}] -> {2}", fileInfo.Name, text3, text.Trim()));
										break;
									}
								}
								string[] extraCheatClients = CheatDatabases.ExtraCheatClients;
								foreach (string text4 in extraCheatClients)
								{
									if (text4.Length >= 4 && text2.Contains(text4.ToLowerInvariant()))
									{
										list2.Add(string.Format("\ud83d\udea8 [{0}] Чит-модуль в отчёте: [{1}] -> {2}", fileInfo.Name, text4, text.Trim()));
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
											list2.Add(string.Format("\ud83d\udea8 [{0}] Запрещённый пункт {1} ({2}) в отчёте: -> {3}", fileInfo.Name, banRule.Id, banRule.Title, text.Trim()));
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
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string path = Path.Combine(folderPath, ".minecraft");
			try
			{
				string path2 = Path.Combine(path, "usercache.json");
				if (File.Exists(path2))
				{
					string input = File.ReadAllText(path2);
					MatchCollection matchCollection = Regex.Matches(input, "\"name\"\\s*:\\s*\"([^\"]+)\"[,\\s\\n\\r]*\"uuid\"\\s*:\\s*\"([^\"]+)\"");
					foreach (Match item in matchCollection)
					{
						string value = item.Groups[1].Value;
						string value2 = item.Groups[2].Value;
						list2.Add(string.Format("Ник: {0} | UUID: {1} (кэш профилей usercache.json)", value, value2));
					}
				}
			}
			catch
			{
			}
			try
			{
				string path3 = Path.Combine(path, "launcher_accounts.json");
				if (File.Exists(path3))
				{
					string input2 = File.ReadAllText(path3);
					MatchCollection matchCollection2 = Regex.Matches(input2, "\"name\"\\s*:\\s*\"([^\"]+)\"");
					foreach (Match item2 in matchCollection2)
					{
						string name = item2.Groups[1].Value;
						if (!list2.Any((string a) => a.Contains(name)) && name.Length > 2 && !name.Contains("Microsoft") && !name.Contains("token"))
						{
							list2.Add(string.Format("Аккаунт лаунчера: {0} (launcher_accounts.json)", name));
						}
					}
				}
			}
			catch
			{
			}
			CheckResult checkResult = new CheckResult();
			checkResult.Category = "Minecraft";
			checkResult.Name = "История ников и аккаунтов на ПК (usercache.json)";
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = CheckSeverity.Info;
			checkResult.Detail = ((list2.Count > 0) ? ("Обнаружено аккаунтов/ников на этом ПК: " + list2.Count + " (для выявления твинков и мультиаккаунтов)") : "История ников не найдена");
			checkResult.Evidence = string.Join("\r\n", list2.ToArray());
			list.Add(checkResult);
			return list;
		}
	}
}
