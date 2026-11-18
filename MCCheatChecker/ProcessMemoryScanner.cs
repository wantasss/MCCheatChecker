using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

namespace MCCheatChecker
{
	public static class ProcessMemoryScanner
	{
		[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
		private class MEMORYSTATUSEX
		{
			public uint dwLength;

			public uint dwMemoryLoad;

			public ulong ullTotalPhys;

			public ulong ullAvailPhys;

			public ulong ullTotalPageFile;

			public ulong ullAvailPageFile;

			public ulong ullTotalVirtual;

			public ulong ullAvailVirtual;

			public ulong ullAvailExtendedVirtual;

			public MEMORYSTATUSEX()
			{
				dwLength = (uint)Marshal.SizeOf(typeof(MEMORYSTATUSEX));
			}
		}

		private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

		public struct MEMORY_BASIC_INFORMATION64
		{
			public ulong BaseAddress;

			public ulong AllocationBase;

			public uint AllocationProtect;

			public uint __alignment1;

			public ulong RegionSize;

			public uint State;

			public uint Protect;

			public uint Type;

			public uint __alignment2;
		}

		private const uint PROCESS_QUERY_LIMITED_INFORMATION = 4096u;

		private const uint PROCESS_VM_READ = 16u;

		private const uint PROCESS_QUERY_INFORMATION = 1024u;

		private const uint MEM_COMMIT = 4096u;

		private const uint PAGE_EXECUTE = 16u;

		private const uint PAGE_EXECUTE_READ = 32u;

		private const uint PAGE_EXECUTE_READWRITE = 64u;

		private const uint MEM_IMAGE = 16777216u;

		private static readonly string[] KnownCheatProcesses = new string[]
		{
			"cheatengine", "processhacker", "xenos", "extremeinjector", "injector",
			"wireshark", "fiddler", "dnspy", "ilspy", "ghidra", "x64dbg", "x32dbg", "ollydbg", "systeminformer",
			"reghax", "celestial", "expensive", "akrien", "nurik", "deltaclient", "wildclient", "richclient", "fluger",
			"excellent", "impact", "wurstclient", "liquidbounce", "meteorclient", "aristois", "vape", "driplite", "entropy", "kura",
			"doomsday", "bozeclient", "rusherhack", "futureclient", "nursultan", "neverhook", "zamorozka", "minced", "wexside", "deadcode",
			"bebraware", "rynware", "neverware", "jigsaw", "salhack", "ares", "wolfram", "wwe", "bleachhack", "matix",
			"pivoclient", "gish", "eternity", "richpremium", "boberware", "emortality", "dreampool", "clowdy", "rasty", "pyro",
			"flauncher", "exloader", "vegaline", "koid", "journaltrace", "usnjrnl", "dmc", "mango",
			"interception", "kmbox", "vjoy",
			"thunderhack", "slinky", "seedcracker", "seedcrackerx", "dreamclient", "rockstarclient", "avalone", "kion", "xorek",
			"raven", "ravenb4", "ravenbplus", "abyss", "whiterabbit", "flint", "catlavan", "cellent", "bape", "whiteout", "itami", "coffeeclient", "pandora", "archware", "exhibition", "cattyn", "spirthack", "memesense", "neverlose"
		};

		private static readonly string[] RamCheatSignatures = new string[]
		{
			"Celestial", "Expensive", "Akrien", "Nurik", "DeltaClient", "WildClient", "RichClient", "Fluger", "ExcellentClient", "LiquidBounce",
			"WurstClient", "MeteorClient", "Aristois", "VapeClient", "DripLite", "EntropyClient", "KuraClient", "HitBox", "KillAura",
			"TriggerBot", "ReachMod", "VelocityMod", "NoFallMod", "JesusMod", "SpeedHack", "FastPlace", "TimerMod", "ChestESP", "PlayerESP",
			"TracersMod", "Baritone", "Nursultan", "NeverHook", "Zamorozka", "Minced", "Wexside", "DeadCode", "BebraWare", "RynWare",
			"Neverware", "Jigsaw", "Salhack", "Ares", "Wolfram", "WWE", "BleachHack", "Matix", "PivoClient", "Cabaletta",
			"Gish", "Eternity", "RichPremium", "R3D", "BoberWare", "Nurik", "Celka", "Ponos", "Emortality", "DreamPool",
			"Clowdy", "Rasty", "DiamondSim", "AutoAttack", "TopkaAutoBuy", "AutoBuy", "ElytraSwap", "ArmorHotSwap", "GoProne", "Autofish",
			"PYRO", "Impact", "RusherHack", "SquadClient", "Destroy", "ArchWare", "VegaLine", "SelfDistruct", "DekoClient", "Carbonara",
			"Meriada", "Swaper", "ShiftTap", "AutoLeave", "ClickPerl", "AttackAura", "CleanCut", "FreeCam", "AimBot",
			"HitBoxes", "AutoTotem", "InvMove", "ThunderHack", "Boze", "Abyss", "RavenB4", "RavenBPlus",
			"Slinky", "DreamClient", "RockstarClient", "AvaloneClient", "KionClient", "ShitClient", "MERCU", "NIOBIUM", "XorekAutoBuy", "XorekAutoMyst",
			"TopkaAutoSell", "TopkaCasino", "DiamondGen", "AutoAuction", "AuctionHelper", "AutoPearl", "AutoArmor", "FastBow", "AntiDebuff", "AutoPotion",
			"MiddleClickPearl", "BowAimbot", "SilentAimbot", "TargetStrafe", "NoSlowDown", "FlyMod", "ThunderHack-Recode", "NursultanRecode",
			"SeedCrackerX", "DoomsdayClient", "AutoAnchor", "AnchorAura", "Criticals", "TimerHack", "FastMine", "SilentAim", "PacketFly", "BoatFly", "NursultanClient", "CelestialPremium", "FlugerClient", "WildClientPremium", "ThunderHack-Recode", "NursultanRecode", "DeadCodeClient", "ExpensiveClient", "BoberWareClient", "AkrienAntiBot", "VapeV4", "VapeLite", "DripLiteV2", "SlinkyClient", "DreamClientV2", "RockstarClientV2", "AvaloneClientV2", "KionClientV2", "AutoTotemSlot", "SilentAimbotTarget", "MiddleClickPearlAction", "CriticalsPacket", "TargetStrafeRange", "TimerHackSpeed", "FastMineSpeed", "AnchorAuraTarget", "Catlavan", "Cellent", "FlintClient", "KuraClient", "ArchWare", "BoberWare", "Whiteout", "Itami", "CoffeeClient", "Exhibition", "Pandora", "Neverlose", "GameSense", "Desync", "TargetStrafe", "CustomHitbox", "AnchorAuraTarget",
			"CatLavan", "ExLoader", "WhiteRabbit", "AimAssist", "AutoPot", "EagleMod", "InventoryMove", "FastEat", "AntiKnockback", "AutoGapple", "BedFucker", "FastBreak", "Scaffold", "SafeWalk", "NoSlowdown", "StepMod", "PhaseMod", "Trajectories", "TargetHUD", "XRay"
		};

		private static readonly string[] RamCheatSignaturesLower = InitializeRamCheatSignaturesLower();
		private static string[] InitializeRamCheatSignaturesLower()
		{
			string[] arr = new string[RamCheatSignatures.Length];
			for (int i = 0; i < RamCheatSignatures.Length; i++)
			{
				arr[i] = RamCheatSignatures[i].ToLowerInvariant();
			}
			return arr;
		}

		private static List<MemoryRegionHit> _cachedRamHits = null;
		private static DateTime _lastRamScanTime = DateTime.MinValue;
		private static readonly object _ramScanLock = new object();

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool GlobalMemoryStatusEx([In][Out] MEMORYSTATUSEX lpBuffer);

		[DllImport("user32.dll")]
		private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

		[DllImport("user32.dll")]
		private static extern bool IsWindowVisible(IntPtr hWnd);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		private static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr OpenProcess(uint processAccess, bool bInheritHandle, int processId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool QueryFullProcessImageName(IntPtr hProcess, int flags, StringBuilder text, ref int size);

		[DllImport("kernel32.dll")]
		public static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

		[DllImport("kernel32.dll")]
		public static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

		public static SystemRamInfo GetSystemRamInfo()
		{
			SystemRamInfo systemRamInfo = new SystemRamInfo();
			if (PlatformHelper.IsWindows)
			{
				try
				{
					MEMORYSTATUSEX mEMORYSTATUSEX = new MEMORYSTATUSEX();
					if (GlobalMemoryStatusEx(mEMORYSTATUSEX))
					{
						systemRamInfo.TotalBytes = mEMORYSTATUSEX.ullTotalPhys;
						systemRamInfo.AvailableBytes = mEMORYSTATUSEX.ullAvailPhys;
						systemRamInfo.UsedBytes = ((mEMORYSTATUSEX.ullTotalPhys > mEMORYSTATUSEX.ullAvailPhys) ? (mEMORYSTATUSEX.ullTotalPhys - mEMORYSTATUSEX.ullAvailPhys) : 0);
						systemRamInfo.MemoryLoad = mEMORYSTATUSEX.dwMemoryLoad;
						return systemRamInfo;
					}
				}
				catch
				{
				}
			}
			else if (PlatformHelper.IsLinux)
			{
				try
				{
					if (File.Exists("/proc/meminfo"))
					{
						ulong num = 0uL;
						ulong num2 = 0uL;
						string[] array = File.ReadAllLines("/proc/meminfo");
						foreach (string text in array)
						{
							if (text.StartsWith("MemTotal:"))
							{
								string[] array2 = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
								if (array2.Length >= 2)
								{
									num = ulong.Parse(array2[1]) * 1024;
								}
							}
							else if (text.StartsWith("MemAvailable:"))
							{
								string[] array3 = text.Split(new char[2] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
								if (array3.Length >= 2)
								{
									num2 = ulong.Parse(array3[1]) * 1024;
								}
							}
						}
						if (num != 0)
						{
							systemRamInfo.TotalBytes = num;
							systemRamInfo.AvailableBytes = num2;
							systemRamInfo.UsedBytes = ((num > num2) ? (num - num2) : 0);
							systemRamInfo.MemoryLoad = (uint)(systemRamInfo.UsedBytes * 100 / num);
							return systemRamInfo;
						}
					}
				}
				catch
				{
				}
			}
			systemRamInfo.TotalBytes = 17179869184uL;
			systemRamInfo.AvailableBytes = 8589934592uL;
			systemRamInfo.UsedBytes = 8589934592uL;
			systemRamInfo.MemoryLoad = 50u;
			return systemRamInfo;
		}

		public static Dictionary<int, string> GetOpenWindowTitles()
		{
			Dictionary<int, string> dict = new Dictionary<int, string>();
			if (PlatformHelper.IsWindows)
			{
				try
				{
					EnumWindows(delegate(IntPtr hWnd, IntPtr lParam)
					{
						if (IsWindowVisible(hWnd))
						{
							StringBuilder stringBuilder = new StringBuilder(256);
							int windowText = GetWindowText(hWnd, stringBuilder, stringBuilder.Capacity);
							if (windowText > 0)
							{
								string text = stringBuilder.ToString().Trim();
								if (!string.IsNullOrEmpty(text) && text != "Program Manager" && text != "Settings")
								{
									uint lpdwProcessId;
									GetWindowThreadProcessId(hWnd, out lpdwProcessId);
									if (!dict.ContainsKey((int)lpdwProcessId))
									{
										dict[(int)lpdwProcessId] = text;
									}
								}
							}
						}
						return true;
					}, IntPtr.Zero);
				}
				catch
				{
				}
			}
			else
			{
				try
				{
					Process[] processes = Process.GetProcesses();
					foreach (Process process in processes)
					{
						try
						{
							if (!string.IsNullOrEmpty(process.MainWindowTitle))
							{
								dict[process.Id] = process.MainWindowTitle;
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
			return dict;
		}

		public static List<ProcessItem> GetAllRunningProcesses()
		{
			List<ProcessItem> list = new List<ProcessItem>();
			Dictionary<int, string> openWindowTitles = GetOpenWindowTitles();
			Process[] processes = Process.GetProcesses();
			Process[] array = processes;
			foreach (Process process in array)
			{
				try
				{
					ProcessItem processItem = new ProcessItem();
					processItem.Pid = process.Id;
					processItem.Name = process.ProcessName;
					processItem.MemoryBytes = process.WorkingSet64;
					processItem.MemoryString = FormatMemory(process.WorkingSet64);
					processItem.ThreadCount = process.Threads.Count;
					string value;
					if (openWindowTitles.TryGetValue(process.Id, out value))
					{
						processItem.WindowTitle = value;
						processItem.HasWindow = true;
					}
					else
					{
						processItem.WindowTitle = "";
						processItem.HasWindow = false;
					}
					try
					{
						processItem.StartTime = process.StartTime.ToString("HH:mm:ss");
					}
					catch
					{
						processItem.StartTime = "—";
					}
					processItem.ExecutablePath = GetProcessPath(process);
					string text = process.ProcessName.ToLowerInvariant();
					if (text == "javaw" || text == "java" || text.Contains("minecraft") || text.Contains("bedrock"))
					{
						processItem.IsMinecraft = true;
					}
					AnalyzeThreat(processItem);
					list.Add(processItem);
				}
				catch
				{
				}
			}
			list.Sort(delegate(ProcessItem a, ProcessItem b)
			{
				int num = ((a.Severity == CheckSeverity.Critical) ? 100 : ((a.Severity == CheckSeverity.Warning) ? 50 : (a.IsMinecraft ? 25 : 0)));
				int num2 = ((b.Severity == CheckSeverity.Critical) ? 100 : ((b.Severity == CheckSeverity.Warning) ? 50 : (b.IsMinecraft ? 25 : 0)));
				return (num != num2) ? num2.CompareTo(num) : b.MemoryBytes.CompareTo(a.MemoryBytes);
			});
			return list;
		}

		private static void AnalyzeThreat(ProcessItem item)
		{
			string text = item.Name.ToLowerInvariant();
			string text2 = (item.WindowTitle ?? "").ToLowerInvariant();
			string text3 = (item.ExecutablePath ?? "").ToLowerInvariant();
			// 1. Проверка на активные утилиты затирания следов и клинеры (Регламент 5.08)
			foreach (string cleaner in CheatDatabases.KnownCleanerSignatures)
			{
				if (cleaner.Length < 3) continue;
				if (cleaner == "cleaner" && (text.Contains("ccleaner") || text.Contains("nvidia") || text.Contains("amd") || text.Contains("steam")))
					continue;
				if (text.Contains(cleaner) || text2.Contains(cleaner) || (text3.Length > 0 && text3.Contains(cleaner)))
				{
					item.IsThreat = true;
					item.Severity = CheckSeverity.Critical;
					item.ThreatReason = "🚨 [5.08 - Активный клинер/вайпер следов]: " + cleaner;
					return;
				}
			}

			// 2. Проверка по расширенной базе чит-клиентов (Регламент 5.01)
			foreach (string cheat in CheatDatabases.ExtraCheatClients)
			{
				if (cheat.Length < 3) continue;
				if (text.Contains(cheat) || text2.Contains(cheat) || (text3.Length > 0 && text3.Contains(cheat)))
				{
					item.IsThreat = true;
					item.Severity = CheckSeverity.Critical;
					item.ThreatReason = "🚨 [5.01 - Активный чит-клиент]: " + cheat;
					return;
				}
			}

			// 3. Проверка инжекторов и лоадеров
			foreach (string inj in CheatDatabases.ExtraInjectorsAndLoaders)
			{
				if (inj.Length < 4) continue;
				if (text.Contains(inj) || text2.Contains(inj) || (text3.Length > 0 && text3.Contains(inj)))
				{
					item.IsThreat = true;
					item.Severity = CheckSeverity.Critical;
					item.ThreatReason = "🚨 [5.01 - Инжектор / DLL Loader]: " + inj;
					return;
				}
			}

			string[] knownCheatProcesses = KnownCheatProcesses;
			foreach (string text4 in knownCheatProcesses)
			{
				if (text.Contains(text4) || text2.Contains(text4) || (text3.Length > 0 && text3.Contains(text4)))
				{
					item.IsThreat = true;
					item.Severity = CheckSeverity.Critical;
					item.ThreatReason = "🚨 [5.01 - Чит / инжектор / хак-утилита]: " + text4;
					return;
				}
			}
			if (text.Contains("clicker") || text2.Contains("clicker") || text.Contains("macro"))
			{
				item.IsThreat = false;
				item.Severity = CheckSeverity.Info;
				item.ThreatReason = "Автокликер (разрешён регламентом сервера)";
			}
			else if ((text3.Contains("\\appdata\\local\\temp\\") || text3.Contains("\\downloads\\")) && !CheatDatabases.IsWhitelistedProcess(item.Name) && !text.Contains("installer") && !text.Contains("setup") && !text.Contains("update") && !text.Contains("discord") && !text.Contains("chrome") && !text.Contains("telegram") && !text.Contains("spotify") && !text.Contains("steam") && !text.Contains("mccheatchecker") && !text3.Contains("mccheat") && !text3.Contains("проги для проверки") && (text.EndsWith(".tmp") || text.Contains("loader") || text.Contains("cheat") || text.Contains("inject")))
			{
				item.IsThreat = true;
				item.Severity = CheckSeverity.Warning;
				item.ThreatReason = "Запущен подозрительный файл из временной папки (Temp / Downloads)";
			}
			else if (item.IsMinecraft)
			{
				item.Severity = CheckSeverity.Info;
				item.ThreatReason = "Процесс Minecraft (" + item.Name + ")";
			}
			else
			{
				item.Severity = CheckSeverity.Info;
				item.ThreatReason = "Легитимный системный/пользовательский процесс";
			}
		}

		private static string GetProcessPath(Process p)
		{
			if (PlatformHelper.IsWindows)
			{
				try
				{
					IntPtr intPtr = OpenProcess(4096u, false, p.Id);
					if (intPtr != IntPtr.Zero)
					{
						try
						{
							StringBuilder stringBuilder = new StringBuilder(1024);
							int size = stringBuilder.Capacity;
							if (QueryFullProcessImageName(intPtr, 0, stringBuilder, ref size))
							{
								return stringBuilder.ToString();
							}
						}
						finally
						{
							CloseHandle(intPtr);
						}
					}
				}
				catch
				{
				}
			}
			else if (PlatformHelper.IsLinux)
			{
				try
				{
					string text = "/proc/" + p.Id + "/exe";
					if (File.Exists(text))
					{
						return text;
					}
				}
				catch
				{
				}
			}
			try
			{
				return (p.MainModule != null) ? p.MainModule.FileName : "";
			}
			catch
			{
				return "";
			}
		}

		public static string FormatMemory(long bytes)
		{
			if (bytes >= 1073741824)
			{
				return string.Format("{0:0.0} ГБ", (double)bytes / 1024.0 / 1024.0 / 1024.0);
			}
			if (bytes >= 1048576)
			{
				return string.Format("{0:0} МБ", (double)bytes / 1024.0 / 1024.0);
			}
			if (bytes >= 1024)
			{
				return string.Format("{0:0} КБ", (double)bytes / 1024.0);
			}
			return bytes + " Б";
		}

		public static List<MemoryRegionHit> ScanMinecraftRam(int specificPid)
		{
			lock (_ramScanLock)
			{
				if (_cachedRamHits != null && (DateTime.UtcNow - _lastRamScanTime).TotalSeconds < 30.0)
				{
					return new List<MemoryRegionHit>(_cachedRamHits);
				}
				List<MemoryRegionHit> list = new List<MemoryRegionHit>();
			List<Process> list2 = new List<Process>();
			if (specificPid > 0)
			{
				try
				{
					list2.Add(Process.GetProcessById(specificPid));
				}
				catch
				{
				}
			}
			else
			{
				list2.AddRange(Process.GetProcessesByName("javaw"));
				if (list2.Count == 0)
				{
					list2.AddRange(Process.GetProcessesByName("java"));
				}
				if (list2.Count == 0)
				{
					list2.AddRange(Process.GetProcessesByName("Minecraft.Windows"));
				}
				if (list2.Count == 0)
				{
					list2.AddRange(Process.GetProcessesByName("minecraft"));
				}
			}
			if (list2.Count == 0)
			{
				list.Add(new MemoryRegionHit
				{
					Address = "—",
					Size = "0 МБ",
					Protection = "N/A",
					Type = "Minecraft не запущен",
					Detail = "Активных процессов javaw.exe / java.exe в системе не обнаружено.",
					Severity = CheckSeverity.Info
				});
				return list;
			}
			if (!PlatformHelper.IsWindows)
			{
				if (PlatformHelper.IsLinux)
				{
					foreach (Process item in list2)
					{
						try
						{
							string path = "/proc/" + item.Id + "/maps";
							if (!File.Exists(path))
							{
								continue;
							}
							string[] array = File.ReadAllLines(path);
							string[] array2 = array;
							foreach (string text in array2)
							{
								string text2 = text.ToLowerInvariant();
								bool flag = false;
								string text3 = "";
								if (text2.Contains("/tmp/") || text2.Contains("/dev/shm/"))
								{
									flag = true;
									text3 = "Библиотека загружена из /tmp или /dev/shm";
								}
								else if (text2.Contains("(deleted)"))
								{
									flag = true;
									text3 = "Скрытая/удалённая библиотека (deleted so)";
								}
								else
								{
									string[] knownCheatProcesses = KnownCheatProcesses;
									foreach (string text4 in knownCheatProcesses)
									{
										if (text2.Contains(text4))
										{
											flag = true;
											text3 = "Чит-библиотека в памяти (" + text4 + ")";
											break;
										}
									}
								}
								if (flag)
								{
									list.Add(new MemoryRegionHit
									{
										Address = "PID " + item.Id,
										Size = "SO/MAP",
										Protection = "PROC_MAPS",
										Type = "LINUX INJECTION: " + text3,
										Detail = text.Trim(),
										Severity = CheckSeverity.Critical
									});
								}
							}
						}
						catch
						{
						}
					}
				}
				if (list.Count == 0)
				{
					list.Add(new MemoryRegionHit
					{
						Address = "—",
						Size = "0 МБ",
						Protection = "N/A",
						Type = "Мониторинг Linux/macOS",
						Detail = "Процессы Minecraft проанализированы. Сторонних библиотек/инжекций не обнаружено.",
						Severity = CheckSeverity.Info
					});
				}
				return list;
			}
			foreach (Process item2 in list2)
			{
				list.AddRange(CheckJavawLoadedModules(item2));
				IntPtr intPtr = IntPtr.Zero;
				try
				{
					intPtr = OpenProcess(1040u, false, item2.Id);
					if (intPtr == IntPtr.Zero)
					{
						list.Add(new MemoryRegionHit
						{
							Address = "PID " + item2.Id,
							Size = "—",
							Protection = "Отказано в доступе",
							Type = "Ограничение прав",
							Detail = "Не удалось открыть процесс javaw.exe для чтения памяти (запустите от Администратора).",
							Severity = CheckSeverity.Warning
						});
						continue;
					}
					Stopwatch sw = Stopwatch.StartNew();
					ulong num = 65536uL;
					ulong num2 = 140737488355327uL;
					long num3 = 0L;
					long num4 = 33554432L; // 32 MB max budget
					while (num < num2 && num3 < num4)
					{
						if (sw.ElapsedMilliseconds > 2500)
						{
							break; // 2.5s hard timeout
						}
						int dwLength = Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION64));
						MEMORY_BASIC_INFORMATION64 lpBuffer;
						if (VirtualQueryEx(intPtr, (IntPtr)(long)num, out lpBuffer, (uint)dwLength) == 0)
						{
							break;
						}
						if (lpBuffer.State == 4096 && (lpBuffer.Protect == 64 || lpBuffer.Protect == 32) && lpBuffer.Type != 16777216)
						{
							byte[] array3 = new byte[2];
							IntPtr lpNumberOfBytesRead;
							if (ReadProcessMemory(intPtr, (IntPtr)(long)lpBuffer.BaseAddress, array3, 2, out lpNumberOfBytesRead) && lpNumberOfBytesRead.ToInt32() == 2 && array3[0] == 77 && array3[1] == 90)
							{
								list.Add(new MemoryRegionHit
								{
									Address = "0x" + lpBuffer.BaseAddress.ToString("X"),
									Size = FormatMemory((long)lpBuffer.RegionSize),
									Protection = ((lpBuffer.Protect == 64) ? "PAGE_EXECUTE_READWRITE" : "PAGE_EXECUTE_READ"),
									Type = "MANUAL-MAP INJECTION",
									Detail = "Обнаружен скрытый исполняемый PE-заголовок (MZ) в приватной памяти javaw.exe!",
									Severity = CheckSeverity.Critical
								});
							}
						}
						bool isWritable = (lpBuffer.Protect == 4 || lpBuffer.Protect == 64);
						if (lpBuffer.State == 4096 && isWritable && lpBuffer.Type != 16777216 && lpBuffer.RegionSize != 0 && lpBuffer.RegionSize <= 4194304)
						{
							ulong regOffset = 0;
							ulong maxRegRead = Math.Min(lpBuffer.RegionSize, 1048576uL);
							while (regOffset < maxRegRead && num3 < num4)
							{
								if (sw.ElapsedMilliseconds > 2500) break;
								int toRead = (int)Math.Min(65536uL, maxRegRead - regOffset);
								byte[] array4 = new byte[toRead];
								IntPtr lpNumberOfBytesRead2;
								if (ReadProcessMemory(intPtr, (IntPtr)(long)(lpBuffer.BaseAddress + regOffset), array4, toRead, out lpNumberOfBytesRead2) && lpNumberOfBytesRead2.ToInt32() > 0)
								{
									int bytesRead = lpNumberOfBytesRead2.ToInt32();
									num3 += bytesRead;
									string textAscii = Encoding.ASCII.GetString(array4, 0, bytesRead).ToLowerInvariant();
									string textUnicode = Encoding.Unicode.GetString(array4, 0, bytesRead).ToLowerInvariant();
									bool hitFound = false;
									for (int sIdx = 0; sIdx < RamCheatSignaturesLower.Length; sIdx++)
									{
										string sig = RamCheatSignaturesLower[sIdx];
										if (textAscii.Contains(sig) || textUnicode.Contains(sig))
										{
											string origSig = RamCheatSignatures[sIdx];
											list.Add(new MemoryRegionHit
											{
												Address = "0x" + (lpBuffer.BaseAddress + regOffset).ToString("X"),
												Size = FormatMemory((long)lpBuffer.RegionSize),
												Protection = "PAGE_COMMIT",
												Type = "ЧИТ В ПАМЯТИ: " + origSig,
												Detail = "В адресном пространстве javaw (PID: " + item2.Id + ") найден строковый маркер чита: " + origSig,
												Severity = CheckSeverity.Critical
											});
											hitFound = true;
											break;
										}
									}
									if (hitFound) break;
								}
								if (toRead <= 128) break;
								regOffset += (ulong)(toRead - 128);
							}
						}
						num = lpBuffer.BaseAddress + lpBuffer.RegionSize;
						if (lpBuffer.RegionSize == 0)
						{
							num += 4096;
						}
					}
				}
				catch (Exception ex)
				{
					list.Add(new MemoryRegionHit
					{
						Address = "ERR",
						Size = "0",
						Protection = "ERR",
						Type = "Исключение при чтении памяти",
						Detail = ex.Message,
						Severity = CheckSeverity.Info
					});
				}
				finally
				{
					if (intPtr != IntPtr.Zero)
					{
						CloseHandle(intPtr);
					}
				}
			}
			if (list.Count == 0)
			{
				list.Add(new MemoryRegionHit
				{
					Address = "0x00000000",
					Size = "OK",
					Protection = "CLEAN",
					Type = "Память javaw.exe чиста",
					Detail = "Аномалий, manual-map инжекций и известных сигнатур читов в памяти не обнаружено.",
					Severity = CheckSeverity.Info
				});
			}
				_cachedRamHits = new List<MemoryRegionHit>(list);
				_lastRamScanTime = DateTime.UtcNow;
				return list;
			}
		}

		public static List<MemoryRegionHit> CheckJavawLoadedModules(Process p)
		{
			List<MemoryRegionHit> list = new List<MemoryRegionHit>();
			try
			{
				string[] array = new string[]
				{
					"rename_me_please", "editme", "mc100", "dauntiblyat", "slinky", "vape", "driplite", "entropy",
					"kura", "doomsday", "boze", "celestial", "expensive", "whiterabbit", "minced", "neverhook",
					"rusherhack", "liquidbounce", "injector", "koid", "cheat", "hack",
					"interception", "kmbox", "ch340", "cp2102", "minhook", "detours", "xenos", "extreme",
					"nursultan", "thunderhack", "dreamclient", "aimware", "neverlose", "drip", "catlavan",
					"wexside", "deadcode", "richpremium", "delta", "flint", "rockstar", "boberware", "wild",
					"celka", "akrien", "excellent", "fluger", "vegaline", "archware", "exloader", "cattyn",
					"spirthack", "memesense", "itami", "bape", "whiteout", "raven", "coffee", "exhibition"
				};

				foreach (ProcessModule module in p.Modules)
				{
					string text = (module.ModuleName ?? "").ToLowerInvariant();
					string text2 = "";
					try
					{
						text2 = (module.FileName ?? "").ToLowerInvariant();
					}
					catch
					{
					}
					string[] array2 = array;
					foreach (string value in array2)
					{
						if (text.Contains(value) || (!string.IsNullOrEmpty(text2) && text2.Contains(value)))
						{
							list.Add(new MemoryRegionHit
							{
								Address = "0x" + module.BaseAddress.ToString("X"),
								Size = module.ModuleMemorySize / 1024 + " КБ",
								Protection = "LOADED_MODULE",
								Type = "ИНЖЕКТ DLL: " + module.ModuleName,
								Detail = "🚨 [5.01 - Внедрение чита в javaw.exe] Библиотека: " + module.ModuleName + " (" + module.FileName + ")",
								Severity = CheckSeverity.Critical
							});
							break;
						}
					}
					bool isLegitNative = text.Contains("lwjgl") || text.Contains("openal") || text.Contains("glfw") || text.Contains("jemalloc") || text.Contains("jinput") || text.Contains("management_ext") || text.Contains("nio.dll") || text.Contains("net.dll") || text.Contains("sunmscapi.dll") || text.Contains("attach.dll") || text.Contains("management.dll") || text.Contains("instrument.dll") || text.Contains("zip.dll") || text.Contains("wndproc.dll") || text.Contains("splashscreen.dll") || text.Contains("webview2loader") || text.Contains("discord_game_sdk");
					if (!isLegitNative && (text2.Contains("\\appdata\\local\\temp\\") || text2.Contains("\\downloads\\") || text2.Contains("\\desktop\\") || text2.Contains("\\users\\public\\")))
					{
						list.Add(new MemoryRegionHit
						{
							Address = "0x" + module.BaseAddress.ToString("X"),
							Size = module.ModuleMemorySize / 1024 + " КБ",
							Protection = "LOADED_MODULE",
							Type = "ИНЖЕКТ DLL ИЗ TEMP / DESKTOP: " + module.ModuleName,
							Detail = "🚨 [5.01 - Сторонняя DLL в javaw.exe]: " + module.ModuleName + " загружена из " + module.FileName + " — признак внешней инжекции",
							Severity = CheckSeverity.Critical
						});
					}
				}
			}
			catch
			{
			}
			return list;
		}

		public static bool KillProcess(int pid, out string message)
		{
			try
			{
				Process processById = Process.GetProcessById(pid);
				string processName = processById.ProcessName;
				processById.Kill();
				message = "Процесс " + processName + " (PID " + pid + ") успешно завершён.";
				return true;
			}
			catch (Exception ex)
			{
				message = "Не удалось завершить процесс: " + ex.Message;
				return false;
			}
		}

		public static List<MemoryRegionHit> SearchProcessMemoryForKeyword(int pid, string keyword)
		{
			List<MemoryRegionHit> list = new List<MemoryRegionHit>();
			if (string.IsNullOrEmpty(keyword))
			{
				return list;
			}
			List<Process> list2 = new List<Process>();
			if (pid > 0)
			{
				try
				{
					list2.Add(Process.GetProcessById(pid));
				}
				catch
				{
				}
			}
			else
			{
				list2.AddRange(Process.GetProcessesByName("javaw"));
				if (list2.Count == 0)
				{
					list2.AddRange(Process.GetProcessesByName("java"));
				}
			}
			if (list2.Count == 0)
			{
				return list;
			}
			string value = keyword.ToLowerInvariant();
			foreach (Process item in list2)
			{
				if (!PlatformHelper.IsWindows)
				{
					try
					{
						string path = "/proc/" + item.Id + "/cmdline";
						if (File.Exists(path))
						{
							string text = File.ReadAllText(path);
							if (text.ToLowerInvariant().Contains(value))
							{
								list.Add(new MemoryRegionHit
								{
									Address = "PID " + item.Id,
									Size = "CMDLINE",
									Protection = "N/A",
									Type = "Попадание в аргументах процесса",
									Detail = ((text.Length > 200) ? (text.Substring(0, 200) + "...") : text),
									Severity = CheckSeverity.Warning
								});
							}
						}
					}
					catch
					{
					}
					continue;
				}
				IntPtr intPtr = IntPtr.Zero;
				try
				{
					intPtr = OpenProcess(1040u, false, item.Id);
					if (intPtr == IntPtr.Zero)
					{
						continue;
					}
					ulong num = 65536uL;
					ulong num2 = 140737488355327uL;
					long num3 = 0L;
					long num4 = 268435456L;
					while (num < num2 && num3 < num4 && list.Count < 50)
					{
						int dwLength = Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION64));
						MEMORY_BASIC_INFORMATION64 lpBuffer;
						if (VirtualQueryEx(intPtr, (IntPtr)(long)num, out lpBuffer, (uint)dwLength) == 0)
						{
							break;
						}
						if (lpBuffer.State == 4096 && lpBuffer.RegionSize != 0 && lpBuffer.RegionSize < 10485760)
						{
							int num5 = (int)Math.Min(65536uL, lpBuffer.RegionSize);
							byte[] array = new byte[num5];
							IntPtr lpNumberOfBytesRead;
							if (ReadProcessMemory(intPtr, (IntPtr)(long)lpBuffer.BaseAddress, array, num5, out lpNumberOfBytesRead) && lpNumberOfBytesRead.ToInt32() > 0)
							{
								int num6 = lpNumberOfBytesRead.ToInt32();
								num3 += num6;
								string text2 = Encoding.ASCII.GetString(array, 0, num6);
								string text3 = Encoding.Unicode.GetString(array, 0, num6);
								int num7 = text2.IndexOf(value, StringComparison.OrdinalIgnoreCase);
								int num8 = text3.IndexOf(value, StringComparison.OrdinalIgnoreCase);
								if (num7 >= 0 || num8 >= 0)
								{
									bool isUnicode = (num7 < 0 && num8 >= 0);
									int num9 = isUnicode ? (num8 * 2) : num7;
									ulong num10 = lpBuffer.BaseAddress + (ulong)num9;
									string text4;
									if (isUnicode)
									{
										int startByte = Math.Max(0, (num8 - 10) * 2);
										int charCount = Math.Min(35, (num6 - startByte) / 2);
										text4 = (charCount > 0) ? Encoding.Unicode.GetString(array, startByte, charCount * 2) : "";
									}
									else
									{
										int num11 = Math.Max(0, num9 - 20);
										int count = Math.Min(60, num6 - num11);
										text4 = Encoding.ASCII.GetString(array, num11, count);
									}
									char[] array2 = new char[text4.Length];
									for (int i = 0; i < text4.Length; i++)
									{
										char c = text4[i];
										array2[i] = ((c >= ' ' && c <= '~') || char.IsLetterOrDigit(c)) ? c : '.';
									}
									text4 = new string(array2);
									list.Add(new MemoryRegionHit
									{
										Address = "0x" + num10.ToString("X"),
										Size = FormatMemory((long)lpBuffer.RegionSize),
										Protection = "COMMITTED",
										Type = "Совпадение строки: " + keyword,
										Detail = "Контекст памяти: \"" + text4 + "\"",
										Severity = CheckSeverity.Warning
									});
								}
							}
						}
						num = lpBuffer.BaseAddress + lpBuffer.RegionSize;
						if (lpBuffer.RegionSize == 0)
						{
							num += 4096;
						}
					}
				}
				catch
				{
				}
				finally
				{
					if (intPtr != IntPtr.Zero)
					{
						CloseHandle(intPtr);
					}
				}
			}
			return list;
		}
	}
}
