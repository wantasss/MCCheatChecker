using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Management;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum SystemInformerCategory
	{
		All,
		JavawStrings,
		JavawModules,
		Explorer,
		DcomLaunch,
		PcaSvc,
		Dps,
		Csrss,
		Violations
	}

	public class SystemInformerEvidenceItem
	{
		public string ProcessName { get; set; }
		public int ProcessId { get; set; }
		public SystemInformerCategory Category { get; set; }
		public string SourceMethod { get; set; }
		public string DetectedStringOrPath { get; set; }
		public string FileSizeOrWeight { get; set; }
		public string TimeInfo { get; set; }
		public string Verdict { get; set; }
		public string RuleViolation { get; set; }
		public string Description { get; set; }
		public string FullDetails { get; set; }
		public CheckSeverity Severity { get; set; }
		public bool IsViolation { get; set; }
	}

	public class SystemInformerScanSummary
	{
		public List<SystemInformerEvidenceItem> Items { get; set; }
		public int TotalArtifacts { get; set; }
		public int JavawStringHits { get; set; }
		public int SuspiciousDllCount { get; set; }
		public int ServiceAndExplorerHits { get; set; }
		public int DpsAnomalyCount { get; set; }
		public int CsrssInjectionCount { get; set; }
		public int ViolationCount { get; set; }
		public bool CsrssAccessDenied { get; set; }
		public string ScanDurationMs { get; set; }

		public SystemInformerScanSummary()
		{
			Items = new List<SystemInformerEvidenceItem>();
			ScanDurationMs = "0";
		}
	}

	public static class SystemInformerForensicsEngine
	{
		#region Win32 P/Invoke

		private const uint PROCESS_QUERY_INFORMATION = 0x0400;
		private const uint PROCESS_VM_READ = 0x0010;
		private const uint MEM_COMMIT = 0x1000;
		private const uint MEM_IMAGE = 0x1000000;
		private const uint PAGE_NOACCESS = 0x01;
		private const uint PAGE_READONLY = 0x02;
		private const uint PAGE_READWRITE = 0x04;
		private const uint PAGE_WRITECOPY = 0x08;
		private const uint PAGE_EXECUTE_READ = 0x20;
		private const uint PAGE_EXECUTE_READWRITE = 0x40;
		private const uint PAGE_EXECUTE_WRITECOPY = 0x80;
		private const uint PAGE_GUARD = 0x100;

		private const uint TOKEN_ADJUST_PRIVILEGES = 0x0020;
		private const uint TOKEN_QUERY = 0x0008;
		private const uint SE_PRIVILEGE_ENABLED = 0x00000002;

		[StructLayout(LayoutKind.Sequential)]
		private struct MEMORY_BASIC_INFORMATION64
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

		[StructLayout(LayoutKind.Sequential)]
		private struct MEMORY_BASIC_INFORMATION32
		{
			public uint BaseAddress;
			public uint AllocationBase;
			public uint AllocationProtect;
			public uint RegionSize;
			public uint State;
			public uint Protect;
			public uint Type;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct LUID
		{
			public uint LowPart;
			public int HighPart;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct TOKEN_PRIVILEGES
		{
			public uint PrivilegeCount;
			public LUID Luid;
			public uint Attributes;
		}

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern IntPtr OpenProcess(uint dwDesiredAccess, bool bInheritHandle, int dwProcessId);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool CloseHandle(IntPtr hObject);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION64 lpBuffer, uint dwLength);

		[DllImport("kernel32.dll", EntryPoint = "VirtualQueryEx", SetLastError = true)]
		private static extern int VirtualQueryEx32(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION32 lpBuffer, uint dwLength);

		[DllImport("kernel32.dll", SetLastError = true)]
		private static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool OpenProcessToken(IntPtr ProcessHandle, uint DesiredAccess, out IntPtr TokenHandle);

		[DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Auto)]
		private static extern bool LookupPrivilegeValue(string lpSystemName, string lpName, out LUID lpLuid);

		[DllImport("advapi32.dll", SetLastError = true)]
		private static extern bool AdjustTokenPrivileges(IntPtr TokenHandle, bool DisableAllPrivileges, ref TOKEN_PRIVILEGES NewState, uint BufferLength, IntPtr PreviousState, IntPtr ReturnLength);

		#endregion

		#region Signatures & Rules

		private struct CheatMemorySignature
		{
			public string Pattern;
			public string ClientOrType;
			public string Rule;
			public bool RequireWordStart;
			public bool RequireWordEnd;
			public bool RequireCheatContext;
			public bool IgnoreBiomeContext;
			public bool RequireCleanAsciiShort;
		}

		private static readonly CheatMemorySignature[] JavawManualSignatures = new CheatMemorySignature[]
		{
			new CheatMemorySignature { Pattern = "hitbox:", ClientOrType = "Vert Client / Hitbox", Rule = "5.01", RequireWordStart = true },
			new CheatMemorySignature { Pattern = "stubborn.website", ClientOrType = "Hitboxes Client URL", Rule = "5.01" },
			new CheatMemorySignature { Pattern = "reach:", ClientOrType = "Vert Client (Reach)", Rule = "5.01", RequireWordStart = true },
			new CheatMemorySignature { Pattern = "bushroot", ClientOrType = "Bushroot (Слабые хитбоксы)", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "<-8dd", ClientOrType = "Doomsday Client", Rule = "5.01", RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = ":79q", ClientOrType = "Doomsday Client", Rule = "5.01", RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "exitsave", ClientOrType = "Troxill Client (ExitSave)", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "triggerbot", ClientOrType = "Triggerbot", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = ":|:", ClientOrType = "Troxill Client Marker", Rule = "5.01", RequireCheatContext = true },
			new CheatMemorySignature { Pattern = "baobab:", ClientOrType = "BaoBab Hitbox Mod", Rule = "5.01", RequireWordStart = true },
			new CheatMemorySignature { Pattern = "baobab", ClientOrType = "BaoBab (Слабые хиты)", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true, IgnoreBiomeContext = true },
			new CheatMemorySignature { Pattern = "killaura", ClientOrType = "KillAura", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "expensiveclient", ClientOrType = "Expensive Client", Rule = "5.01" },
			new CheatMemorySignature { Pattern = "fun.expensive", ClientOrType = "Expensive Client", Rule = "5.01" },
			new CheatMemorySignature { Pattern = "im.expensive", ClientOrType = "Expensive Client", Rule = "5.01" },
			new CheatMemorySignature { Pattern = "expensive 3.", ClientOrType = "Expensive 3.x", Rule = "5.01" },
			new CheatMemorySignature { Pattern = "expensive recode", ClientOrType = "Expensive Recode", Rule = "5.01" },
			new CheatMemorySignature { Pattern = "net.minecraft.expensive", ClientOrType = "Expensive Client", Rule = "5.01" },
			new CheatMemorySignature { Pattern = "celka", ClientOrType = "Celestial (Celka)", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "allatori", ClientOrType = "Allatori Obfuscator (Чит-клиент)", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "71kd", ClientOrType = "Cortex Client", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true, RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "hntxxl", ClientOrType = "Cortex Client", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true, RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "scheduled_executables", ClientOrType = "Doomsday Client", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "lookaheadstep", ClientOrType = "Doomsday Client (lookAheadStep)", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = ".]+5+]5", ClientOrType = "Cortex Legit", Rule = "5.01", RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "5+]5]]q", ClientOrType = "Cortex Legit", Rule = "5.01", RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "10^]^]]]!!46", ClientOrType = "Cortex Legit", Rule = "5.01", RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "walvbt#:", ClientOrType = "Анапа v2", Rule = "5.01", RequireWordStart = true, RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "#hit", ClientOrType = "Анапа v2 (#Hit)", Rule = "5.01", RequireWordEnd = true, RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "zero/time", ClientOrType = "Hitbox Client (Zero/Time)", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "#thv", ClientOrType = "Vert Client (#THV)", Rule = "5.01", RequireWordEnd = true, RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "breakhitson", ClientOrType = "Hitbox Client (BreakHitsOn)", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "beackga:", ClientOrType = "Hitbox Client (BeackGa)", Rule = "5.01", RequireWordStart = true, RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "net.minecraftforge.asmeventhandler.31.wait(long, int)", ClientOrType = "Hitbox / SelfDestruct Hook", Rule = "5.08" },
			new CheatMemorySignature { Pattern = "clowdy", ClientOrType = "ClowdyClient", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "derick1337", ClientOrType = "Hitbox Client (Derick1337)", Rule = "5.01" },
			new CheatMemorySignature { Pattern = "okuma:", ClientOrType = "Hitbox Client (okuma)", Rule = "5.01", RequireWordStart = true, RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "sfxcmd", ClientOrType = "gg Client (sfxcmd)", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true, RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "az85'", ClientOrType = "Анапа V4", Rule = "5.01", RequireWordStart = true, RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = "71l;", ClientOrType = "Анапа V4", Rule = "5.01", RequireWordStart = true, RequireCleanAsciiShort = true },
			new CheatMemorySignature { Pattern = ";(ljava/lang/class<*>;ljava/lang/string;ljava/lang/object;)v", ClientOrType = "Самописный чит (Reflection Hook)", Rule = "5.01" },
			new CheatMemorySignature { Pattern = "vape4dll", ClientOrType = "Vape Client V4 (VAPE4DLL)", Rule = "5.01" },
			new CheatMemorySignature { Pattern = "invisiblehitbox", ClientOrType = "Vape / Hitbox (InvisibleHitbox)", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "hlit/unimi/dsi/fastutil/objects/object2doublemap;>;", ClientOrType = "Doomsday Client", Rule = "5.01" },
			new CheatMemorySignature { Pattern = "baritone", ClientOrType = "Baritone Auto-Bot", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "aimassist", ClientOrType = "AimAssist", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "aimbot", ClientOrType = "AimBot", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "criticals", ClientOrType = "Criticals Cheat", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "fun.systemdlc", ClientOrType = "SystemDLC Client", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "ru.systemdlc", ClientOrType = "SystemDLC Client", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "system.dlc", ClientOrType = "SystemDLC Client", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "systemdlc", ClientOrType = "SystemDLC Client", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true },
			new CheatMemorySignature { Pattern = "sysdlc", ClientOrType = "SystemDLC Client", Rule = "5.01", RequireWordStart = true, RequireWordEnd = true }
		};

		private static readonly Regex DpsPathRegex = new Regex(@"^([a-zA-Z]:\\.+)\\?$", RegexOptions.Compiled);
		private static readonly Regex DpsNoExeRegex = new Regex(@"^!![A-Za-z]((?!Exe).)*$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex DpsTimestampRegex = new Regex(@"!(\d{4}/\d{2}/\d{2}:\d{2}:\d{2}:\d{2})!", RegexOptions.Compiled);
		private static readonly Regex CsrssExeRegex = new Regex(@"^[A-Za-z]:\\.+\.exe$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex CsrssDllRegex = new Regex(@"^[A-Za-z]:\\.+\.dll$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex CsrssBroadPathRegex = new Regex(@"^(?:\\\\\?\\)?[A-Za-z]:\\.+$", RegexOptions.Compiled);
		private static readonly Regex JarPathExtractRegex = new Regex(@"(?:\\\\\?\\)?[A-Za-z]:\\[^\r\n\0""<>|]+?\.jar\b", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex WindowsPathExtractRegex = new Regex(@"(?:\\\\\?\\)?[A-Za-z]:\\[^,\r\n\0""<>|]+?\.(?:exe|dll|jar|bat|cmd|ps1)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		private static readonly HashSet<string> BenignMultiPathSystemExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"svchost.exe", "conhost.exe", "cmd.exe", "powershell.exe", "pwsh.exe", "dllhost.exe",
			"rundll32.exe", "regsvr32.exe", "backgroundtaskhost.exe", "runtimebroker.exe", "taskhostw.exe",
			"searchprotocolhost.exe", "searchindexer.exe", "msedge.exe", "msedgewebview2.exe", "chrome.exe",
			"firefox.exe", "opera.exe", "discord.exe", "update.exe", "setup.exe", "installer.exe",
			"unins000.exe", "msiexec.exe", "tiworker.exe", "trustedinstaller.exe", "wuauclt.exe",
			"usoclient.exe", "javaw.exe", "java.exe", "dotnet.exe", "explorer.exe", "notepad.exe",
			"calc.exe", "smartscreen.exe", "securityhealthsystray.exe", "audiodg.exe", "spoolsv.exe",
			"sihost.exe", "ctfmon.exe", "dwm.exe", "fontdrvhost.exe", "taskmgr.exe", "mmc.exe",
			"wscript.exe", "cscript.exe", "schtasks.exe", "reg.exe", "sc.exe", "net.exe", "net1.exe"
		};

		private static readonly HashSet<string> ScreenShareToolExes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
		{
			"anydesk.exe", "rudesktop.exe", "rustdesk.exe", "teamviewer.exe", "everything.exe"
		};

		#endregion

		#region Public Entry Points

		public static SystemInformerScanSummary RunScan(bool includeInformational = true)
		{
			Stopwatch swTotal = Stopwatch.StartNew();
			SystemInformerScanSummary summary = new SystemInformerScanSummary();

			if (!PlatformHelper.IsWindows)
			{
				summary.Items.Add(new SystemInformerEvidenceItem
				{
					ProcessName = "OS",
					ProcessId = 0,
					Category = SystemInformerCategory.All,
					SourceMethod = "System Check",
					DetectedStringOrPath = "Non-Windows",
					FileSizeOrWeight = "—",
					TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
					Verdict = "Пропущено",
					RuleViolation = "ИНФО",
					Description = "Анализ памяти System Informer поддерживается только на Windows.",
					FullDetails = "Текущая ОС не является Windows.",
					Severity = CheckSeverity.Info,
					IsViolation = false
				});
				FinalizeSummary(summary, swTotal);
				return summary;
			}

			TryEnableDebugPrivilege();

			try
			{
				ScanJavawProcesses(summary, includeInformational);
			}
			catch { }

			try
			{
				ScanProcessHackerAndSystemInformerInjectHistory(summary, includeInformational);
			}
			catch { }

			try
			{
				ScanExplorerProcesses(summary, includeInformational);
			}
			catch { }

			try
			{
				ScanDcomLaunchAndPlugPlay(summary, includeInformational);
			}
			catch { }

			try
			{
				ScanPcaSvc(summary, includeInformational);
			}
			catch { }

			try
			{
				ScanDpsService(summary, includeInformational);
			}
			catch { }

			try
			{
				ScanCsrssProcesses(summary, includeInformational);
			}
			catch { }

			FinalizeSummary(summary, swTotal);
			return summary;
		}

		public static List<CheckResult> RunDetectorChecks()
		{
			List<CheckResult> results = new List<CheckResult>();
			try
			{
				SystemInformerScanSummary summary = RunScan(false);
				if (summary.ViolationCount > 0)
				{
					CheckSeverity topSeverity = CheckSeverity.Warning;
					List<string> evidenceLines = new List<string>();
					foreach (SystemInformerEvidenceItem it in summary.Items)
					{
						if (!it.IsViolation) continue;
						if (it.Severity == CheckSeverity.Critical) topSeverity = CheckSeverity.Critical;
						if (evidenceLines.Count < 45)
						{
							evidenceLines.Add(string.Format("[{0} | {1} (PID {2}) | {3}] {4} -> {5}",
								it.RuleViolation,
								it.ProcessName,
								it.ProcessId,
								it.SourceMethod,
								it.Verdict,
								it.DetectedStringOrPath));
						}
					}

					results.Add(new CheckResult
					{
						Category = "Память / System Informer",
						Name = "Форензика System Informer / Process Hacker 2 (javaw, explorer, DcomLaunch, PcaSvc, DPS, csrss)",
						Detected = true,
						Severity = topSeverity,
						Detail = string.Format("Обнаружены улики в памяти System Informer: нарушений {0} (javaw строки: {1}, DLL: {2}, DPS: {3}, csrss: {4})",
							summary.ViolationCount, summary.JavawStringHits, summary.SuspiciousDllCount, summary.DpsAnomalyCount, summary.CsrssInjectionCount),
						Evidence = string.Join("\r\n", evidenceLines.ToArray())
					});
				}
				else
				{
					results.Add(new CheckResult
					{
						Category = "Память / System Informer",
						Name = "Форензика System Informer / Process Hacker 2 (javaw, explorer, DcomLaunch, PcaSvc, DPS, csrss)",
						Detected = false,
						Severity = CheckSeverity.Info,
						Detail = string.Format("Глубокий аудит памяти (javaw, explorer, DcomLaunch, PcaSvc, DPS, csrss) завершён за {0} мс: подозрительных строк, инжектов и подмен не выявлено.",
							summary.ScanDurationMs),
						Evidence = summary.CsrssAccessDenied
							? "Примечание: доступ к памяти системного процесса csrss.exe ограничен защитой PPL/античитом или уровнем прав (пропуск безопасен)."
							: "Все доступные процессы проанализированы чисто."
					});
				}
			}
			catch (Exception ex)
			{
				results.Add(new CheckResult
				{
					Category = "Память / System Informer",
					Name = "Форензика System Informer / Process Hacker 2",
					Detected = false,
					Severity = CheckSeverity.Info,
					Detail = "Аудит памяти завершён с исключением: " + ex.Message,
					Evidence = ""
				});
			}
			return results;
		}

		private static void FinalizeSummary(SystemInformerScanSummary summary, Stopwatch swTotal)
		{
			swTotal.Stop();
			summary.ScanDurationMs = swTotal.ElapsedMilliseconds.ToString();
			summary.TotalArtifacts = summary.Items.Count;

			int jStr = 0;
			int dlls = 0;
			int svcExp = 0;
			int dps = 0;
			int csrss = 0;
			int viol = 0;

			foreach (SystemInformerEvidenceItem item in summary.Items)
			{
				if (item.IsViolation)
				{
					viol++;
					if (item.Category == SystemInformerCategory.JavawStrings) jStr++;
					else if (item.Category == SystemInformerCategory.JavawModules) dlls++;
					else if (item.Category == SystemInformerCategory.Dps) dps++;
					else if (item.Category == SystemInformerCategory.Csrss) csrss++;
				}

				if (item.Category == SystemInformerCategory.Explorer ||
					item.Category == SystemInformerCategory.DcomLaunch ||
					item.Category == SystemInformerCategory.PcaSvc)
				{
					svcExp++;
				}
			}

			summary.JavawStringHits = jStr;
			summary.SuspiciousDllCount = dlls;
			summary.ServiceAndExplorerHits = svcExp;
			summary.DpsAnomalyCount = dps;
			summary.CsrssInjectionCount = csrss;
			summary.ViolationCount = viol;
		}

		#endregion

		#region 1. javaw.exe Memory Strings & Modules

		private static void ScanJavawProcesses(SystemInformerScanSummary summary, bool includeInformational)
		{
			List<Process> javawProcs = new List<Process>();
			try { javawProcs.AddRange(Process.GetProcessesByName("javaw")); } catch { }
			try { javawProcs.AddRange(Process.GetProcessesByName("java")); } catch { }

			if (javawProcs.Count == 0)
			{
				if (includeInformational)
				{
					summary.Items.Add(new SystemInformerEvidenceItem
					{
						ProcessName = "javaw.exe",
						ProcessId = 0,
						Category = SystemInformerCategory.JavawStrings,
						SourceMethod = "Process Scan",
						DetectedStringOrPath = "Не запущен",
						FileSizeOrWeight = "—",
						TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
						Verdict = "Minecraft не запущен",
						RuleViolation = "ИНФО",
						Description = "Активный процесс javaw.exe / java.exe в памяти не обнаружен.",
						FullDetails = "Для анализа строк памяти и модулей DLL игры процесс javaw.exe должен быть запущен.",
						Severity = CheckSeverity.Info,
						IsViolation = false
					});
				}
				return;
			}

			foreach (Process p in javawProcs)
			{
				try
				{
					AuditJavawModules(p, summary, includeInformational);
					AuditJavawMemoryStrings(p, summary, includeInformational);
				}
				catch { }
			}
		}

		private static void AuditJavawModules(Process p, SystemInformerScanSummary summary, bool includeInformational)
		{
			string javaRuntimeDir = "";
			DateTime procStart = DateTime.Now;
			try
			{
				procStart = p.StartTime;
				if (p.MainModule != null && !string.IsNullOrEmpty(p.MainModule.FileName))
				{
					javaRuntimeDir = (Path.GetDirectoryName(p.MainModule.FileName) ?? "").ToLowerInvariant();
				}
			}
			catch { }

			ProcessModuleCollection modules;
			try { modules = p.Modules; }
			catch { return; }

			foreach (ProcessModule mod in modules)
			{
				try
				{
					string modName = mod.ModuleName ?? "";
					string rawPath = mod.FileName ?? "";
					string cleanPath = CleanWin32Path(rawPath);
					string lowerPath = cleanPath.ToLowerInvariant();
					string lowerName = modName.ToLowerInvariant();

					bool isSystemDir = lowerPath.Contains(@"\windows\system32\") ||
						lowerPath.Contains(@"\windows\syswow64\") ||
						lowerPath.Contains(@"\windows\winsxs\") ||
						lowerPath.Contains(@"\system32\driverstore\");

					bool isLegitNative = ProcessMemoryScanner.IsLegitimateMinecraftNativeDll(lowerName, lowerPath);
					bool isJavaBin = !string.IsNullOrEmpty(javaRuntimeDir) && lowerPath.StartsWith(javaRuntimeDir);

					bool existsOnDisk = false;
					long diskBytes = 0;
					DateTime fileModTime = DateTime.MinValue;
					try
					{
						if (!string.IsNullOrEmpty(cleanPath) && File.Exists(cleanPath))
						{
							existsOnDisk = true;
							FileInfo fi = new FileInfo(cleanPath);
							diskBytes = fi.Length;
							fileModTime = fi.LastWriteTime;
						}
					}
					catch { }

					string fileDescription = "";
					try
					{
						if (mod.FileVersionInfo != null && !string.IsNullOrEmpty(mod.FileVersionInfo.FileDescription))
						{
							fileDescription = mod.FileVersionInfo.FileDescription.Trim();
						}
					}
					catch { }

					double diskMb = diskBytes > 0 ? (diskBytes / 1048576.0) : (mod.ModuleMemorySize / 1048576.0);
					string weightStr = diskBytes > 0
						? string.Format("{0:F2} МБ ({1} КБ в ОЗУ)", diskBytes / 1048576.0, mod.ModuleMemorySize / 1024)
						: string.Format("{0:F2} МБ (только ОЗУ)", mod.ModuleMemorySize / 1048576.0);

					// 1. Отсутствует на диске (удалённая / скрытая DLL)
					if (!existsOnDisk && !string.IsNullOrEmpty(cleanPath) && !isSystemDir)
					{
						summary.Items.Add(new SystemInformerEvidenceItem
						{
							ProcessName = p.ProcessName + ".exe",
							ProcessId = p.Id,
							Category = SystemInformerCategory.JavawModules,
							SourceMethod = "Modules -> FileExists",
							DetectedStringOrPath = cleanPath,
							FileSizeOrWeight = weightStr,
							TimeInfo = procStart.ToString("dd.MM.yyyy HH:mm:ss"),
							Verdict = "Удалённая DLL в памяти javaw",
							RuleViolation = "5.08",
							Description = "Загруженный модуль отсутствует на диске: признак удаления инжектированной DLL перед проверкой!",
							FullDetails = string.Format("Процесс: {0}.exe (PID {1})\r\nМодуль: {2}\r\nПуть: {3}\r\nРазмер в памяти: {4}\r\nFileDescription: {5}\r\nСтатус: Файл отсутствует на диске (Бан 5.08).",
								p.ProcessName, p.Id, modName, cleanPath, weightStr, string.IsNullOrEmpty(fileDescription) ? "<пусто>" : fileDescription),
							Severity = CheckSeverity.Critical,
							IsViolation = true
						});
						continue;
					}

					// Пропускаем стандартные системные DLL и легитимные нативки Minecraft (LWJGL/JNA/VoiceChat/OpenAL/EvoGuard)
					if (isLegitNative || isJavaBin) continue;

					bool isSigned = isSystemDir || IsDigitallySigned(cleanPath);

					// 2. Известное имя чита в DLL (если не системная и не легитимная нативка)
					string cheatHit = isSystemDir ? "" : ExtractMatchedCheat(lowerName, lowerPath);
					if (!string.IsNullOrEmpty(cheatHit))
					{
						summary.Items.Add(new SystemInformerEvidenceItem
						{
							ProcessName = p.ProcessName + ".exe",
							ProcessId = p.Id,
							Category = SystemInformerCategory.JavawModules,
							SourceMethod = "Modules -> Cheat Signature",
							DetectedStringOrPath = cleanPath,
							FileSizeOrWeight = weightStr,
							TimeInfo = fileModTime != DateTime.MinValue ? fileModTime.ToString("dd.MM.yyyy HH:mm:ss") : "—",
							Verdict = "Чит-DLL: " + cheatHit,
							RuleViolation = "5.01",
							Description = "В адресное пространство Minecraft внедрена библиотека с сигнатурой чита: " + cheatHit,
							FullDetails = string.Format("Модуль: {0}\r\nПуть: {1}\r\nВес: {2}\r\nОписание: {3}\r\nЦифровая подпись: {4}",
								modName, cleanPath, weightStr, string.IsNullOrEmpty(fileDescription) ? "<пусто>" : fileDescription, isSigned ? "Есть" : "ОТСУТСТВУЕТ"),
							Severity = CheckSeverity.Critical,
							IsViolation = true
						});
						continue;
					}

					// 3. Мануальный аудит System Informer: Пустое FileDescription + характерные веса (1.42, 1.43, 1.56, 1.89 МБ)
					bool isEmptyDesc = string.IsNullOrEmpty(fileDescription);
					bool matchesManualWeight = IsManualSuspiciousDllWeight(diskMb) || IsManualSuspiciousDllWeight(mod.ModuleMemorySize / 1048576.0);

					if (isEmptyDesc && matchesManualWeight && !isSigned)
					{
						summary.Items.Add(new SystemInformerEvidenceItem
						{
							ProcessName = p.ProcessName + ".exe",
							ProcessId = p.Id,
							Category = SystemInformerCategory.JavawModules,
							SourceMethod = "Modules -> Empty Description + Weight",
							DetectedStringOrPath = cleanPath,
							FileSizeOrWeight = weightStr,
							TimeInfo = fileModTime != DateTime.MinValue ? fileModTime.ToString("dd.MM.yyyy HH:mm:ss") : "—",
							Verdict = "Подозрительная DLL (вес " + string.Format("{0:F2} МБ", diskMb) + ")",
							RuleViolation = "5.01",
							Description = "Неподписанная DLL с пустым полем Description и характерным для чит-инжектов весом (1.42/1.43/1.56/1.89 МБ).",
							FullDetails = string.Format("Модуль: {0}\r\nПуть: {1}\r\nВес: {2}\r\nFileDescription: <пусто>\r\nПодпись: ОТСУТСТВУЕТ\r\nСовпадение по методичке System Informer (1.42 / 1.43 / 1.56 / 1.89 МБ).",
								modName, cleanPath, weightStr),
							Severity = CheckSeverity.Critical,
							IsViolation = true
						});
						continue;
					}

					// 4. Неподписанная DLL из Temp / Downloads / Desktop / Public
					bool isUserStagingDir = lowerPath.Contains(@"\appdata\local\temp\") ||
						lowerPath.Contains(@"\windows\temp\") ||
						lowerPath.Contains(@"\downloads\") ||
						lowerPath.Contains(@"\desktop\") ||
						lowerPath.Contains(@"\users\public\");

					if (!isSigned && isUserStagingDir && !IsKnownBenignOverlayOrGpu(lowerName, lowerPath))
					{
						summary.Items.Add(new SystemInformerEvidenceItem
						{
							ProcessName = p.ProcessName + ".exe",
							ProcessId = p.Id,
							Category = SystemInformerCategory.JavawModules,
							SourceMethod = "Modules -> Untrusted Temp/User DLL",
							DetectedStringOrPath = cleanPath,
							FileSizeOrWeight = weightStr,
							TimeInfo = fileModTime != DateTime.MinValue ? fileModTime.ToString("dd.MM.yyyy HH:mm:ss") : "—",
							Verdict = "Неподписанная DLL из Temp/User",
							RuleViolation = "5.01",
							Description = "Неподписанная библиотека из временной/пользовательской папки загружена в javaw.exe.",
							FullDetails = string.Format("Модуль: {0}\r\nПуть: {1}\r\nВес: {2}\r\nDescription: {3}\r\nПодпись: ОТСУТСТВУЕТ",
								modName, cleanPath, weightStr, string.IsNullOrEmpty(fileDescription) ? "<пусто>" : fileDescription),
							Severity = CheckSeverity.Critical,
							IsViolation = true
						});
						continue;
					}

					if (includeInformational && isEmptyDesc && !isSystemDir)
					{
						summary.Items.Add(new SystemInformerEvidenceItem
						{
							ProcessName = p.ProcessName + ".exe",
							ProcessId = p.Id,
							Category = SystemInformerCategory.JavawModules,
							SourceMethod = "Modules -> Info",
							DetectedStringOrPath = cleanPath,
							FileSizeOrWeight = weightStr,
							TimeInfo = fileModTime != DateTime.MinValue ? fileModTime.ToString("dd.MM.yyyy HH:mm:ss") : "—",
							Verdict = isSigned ? "Легитимный модуль (подписан)" : "Модуль (вне списка угроз)",
							RuleViolation = "ЧИСТО",
							Description = "Модуль без FileDescription проверен (подпись: " + (isSigned ? "Да" : "Нет") + ", вес безопасен).",
							FullDetails = "Путь: " + cleanPath + "\r\nВес: " + weightStr,
							Severity = CheckSeverity.Info,
							IsViolation = false
						});
					}
				}
				catch { }
			}
		}

		private static bool IsManualSuspiciousDllWeight(double mb)
		{
			// 1.42 МБ, 1.43 МБ, 1.56 МБ, 1.89 МБ (с допуском ±0.018 МБ, а также 1000-based MB)
			if (Math.Abs(mb - 1.42) <= 0.015 || Math.Abs(mb - 1.43) <= 0.015) return true;
			if (Math.Abs(mb - 1.56) <= 0.015) return true;
			if (Math.Abs(mb - 1.89) <= 0.015) return true;
			return false;
		}

		private static void AuditJavawMemoryStrings(Process p, SystemInformerScanSummary summary, bool includeInformational)
		{
			IntPtr hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, p.Id);
			if (hProc == IntPtr.Zero)
			{
				if (includeInformational)
				{
					summary.Items.Add(new SystemInformerEvidenceItem
					{
						ProcessName = p.ProcessName + ".exe",
						ProcessId = p.Id,
						Category = SystemInformerCategory.JavawStrings,
						SourceMethod = "OpenProcess",
						DetectedStringOrPath = "PID " + p.Id,
						FileSizeOrWeight = "—",
						TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
						Verdict = "Нет доступа к памяти",
						RuleViolation = "ИНФО",
						Description = "Не удалось открыть память javaw.exe (код " + Marshal.GetLastWin32Error() + ").",
						FullDetails = "Запустите программу от имени Администратора для чтения защищённых регионов памяти.",
						Severity = CheckSeverity.Info,
						IsViolation = false
					});
				}
				return;
			}

			try
			{
				HashSet<string> reportedSigs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				HashSet<string> discoveredMcPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				List<string> asmLongOrCountHits = new List<string>();
				int nonMixinAsmCount = 0;

				ScanProcessMemoryStrings(
					hProc,
					160L * 1024 * 1024, // 160 MB budget с семплированием больших регионов
					2200,               // 2.2s hard timeout
					false,              // Private + Mapped (без системных Image DLL)
					512 * 1024,         // До 512 КБ на каждый гигантский регион хипа
					delegate(string aLow, string uLow0, string uLow1)
					{
						if (aLow.IndexOf("asm:", StringComparison.Ordinal) >= 0 ||
							uLow0.IndexOf("asm:", StringComparison.Ordinal) >= 0 ||
							uLow1.IndexOf("asm:", StringComparison.Ordinal) >= 0)
						{
							return true;
						}
						if (includeInformational && discoveredMcPaths.Count < 8)
						{
							if (aLow.IndexOf(@"\mods\", StringComparison.Ordinal) >= 0 ||
								aLow.IndexOf(@"\versions\", StringComparison.Ordinal) >= 0 ||
								aLow.IndexOf(@"\resourcepacks\", StringComparison.Ordinal) >= 0 ||
								uLow0.IndexOf(@"\mods\", StringComparison.Ordinal) >= 0 ||
								uLow0.IndexOf(@"\versions\", StringComparison.Ordinal) >= 0 ||
								uLow0.IndexOf(@"\resourcepacks\", StringComparison.Ordinal) >= 0)
							{
								return true;
							}
						}
						for (int si = 0; si < JavawManualSignatures.Length; si++)
						{
							string pat = JavawManualSignatures[si].Pattern;
							if (aLow.IndexOf(pat, StringComparison.Ordinal) >= 0 ||
								uLow0.IndexOf(pat, StringComparison.Ordinal) >= 0 ||
								uLow1.IndexOf(pat, StringComparison.Ordinal) >= 0)
							{
								return true;
							}
						}
						return false;
					},
					delegate(string str, ulong addr)
					{
						string lower = AsciiToLowerFast(str);

						// 0. Исключаем строки самого чекера / белого списка путей
						if (lower.Contains("mccheatchecker") || lower.Contains("screenshare")) return;

						// 1. Поиск активных путей Minecraft (\mods, \versions, \resourcepacks) для контекста
						if (includeInformational && discoveredMcPaths.Count < 8)
						{
							if (lower.Contains(@"\mods\") || lower.Contains(@"\versions\") || lower.Contains(@"\resourcepacks\"))
							{
								Match pm = Regex.Match(str, @"[A-Za-z]:\\[^""\r\n\0<>|]{4,180}(?:\\mods|\\versions|\\resourcepacks)(?:\\[^""\r\n\0<>|]{0,60})?", RegexOptions.IgnoreCase);
								if (pm.Success)
								{
									string mcP = pm.Value.Trim();
									string mcPLow = mcP.ToLowerInvariant();
									if (!mcPLow.Contains(@"\roblox\") && !mcPLow.EndsWith(".exe") && discoveredMcPaths.Add(mcP))
									{
										summary.Items.Add(new SystemInformerEvidenceItem
										{
											ProcessName = p.ProcessName + ".exe",
											ProcessId = p.Id,
											Category = SystemInformerCategory.JavawStrings,
											SourceMethod = "Memory Strings -> .minecraft paths",
											DetectedStringOrPath = mcP,
											FileSizeOrWeight = str.Length + " симв.",
											TimeInfo = "0x" + addr.ToString("X"),
											Verdict = "Активный каталог Minecraft",
											RuleViolation = "ЧИСТО",
											Description = "Определена активная директория игры из памяти javaw.exe.",
											FullDetails = "Адрес региона: 0x" + addr.ToString("X") + "\r\nПолная строка: " + Truncate(str, 350),
											Severity = CheckSeverity.Info,
											IsViolation = false
										});
									}
								}
							}
						}

						// 2. Специальная проверка ASM: (Более 100 Lenght - ClownClient, много 10 - DEADCODE)
						int asmIdx = lower.IndexOf("asm:", StringComparison.Ordinal);
						if (asmIdx >= 0)
						{
							// Отфильтровываем легитимный spongepowered/asm:1 или org/objectweb/asm
							bool isMixinAsm = lower.Contains("spongepowered/asm:") || lower.Contains("objectweb/asm");
							if (!isMixinAsm && (asmIdx == 0 || !IsWordChar(lower[asmIdx - 1])))
							{
								nonMixinAsmCount++;
								if (str.Length > 100 && !reportedSigs.Contains("ASM:>100"))
								{
									reportedSigs.Add("ASM:>100");
									summary.Items.Add(new SystemInformerEvidenceItem
									{
										ProcessName = p.ProcessName + ".exe",
										ProcessId = p.Id,
										Category = SystemInformerCategory.JavawStrings,
										SourceMethod = "Memory Strings (len >= 4) -> ASM:",
										DetectedStringOrPath = Truncate(str, 180),
										FileSizeOrWeight = "Длина " + str.Length + " симв.",
										TimeInfo = "0x" + addr.ToString("X"),
										Verdict = "ClownClient (ASM: > 100 симв.)",
										RuleViolation = "5.01",
										Description = "Обнаружена длинная строка ASM: (>100 символов) — сигнатура ClownClient.",
										FullDetails = "Адрес: 0x" + addr.ToString("X") + "\r\nДлина: " + str.Length + "\r\nСтрока: " + Truncate(str, 600),
										Severity = CheckSeverity.Critical,
										IsViolation = true
									});
								}
								else if (asmLongOrCountHits.Count < 12)
								{
									asmLongOrCountHits.Add(Truncate(str, 120));
								}
							}
						}

						// 3. Проверка всех мануальных сигнатур читов с защитой границ слов и контекста
						for (int i = 0; i < JavawManualSignatures.Length; i++)
						{
							CheatMemorySignature sig = JavawManualSignatures[i];
							if (reportedSigs.Contains(sig.Pattern)) continue;

							int matchIdx = FindPatternWithBoundary(lower, sig);
							if (matchIdx >= 0)
							{
								// Проверка контекста биома/ресурса (для baobab)
								if (sig.IgnoreBiomeContext &&
									(lower.Contains("biome") || lower.Contains("baobab_") || lower.Contains("_baobab") ||
									 lower.Contains("textures/") || lower.Contains("models/") || lower.Contains("terralith") ||
									 lower.Contains("biomesoplenty")))
								{
									continue;
								}

								// Проверка контекста читов (для :|: чтобы не ловить Chrome/Java regexes)
								if (sig.RequireCheatContext &&
									!(lower.Contains("troxill") || lower.Contains("killaura") || lower.Contains("hitbox") ||
									  lower.Contains("vape") || lower.Contains("aimbot") || lower.Contains("cheat")))
								{
									continue;
								}

								// Проверка для allatori: игнорируем известные античит-описания/ресурсы проверки
								if (sig.Pattern == "allatori" && (lower.Contains("obfuscator detector") || lower.Contains("jarobfuscation")))
								{
									continue;
								}

								reportedSigs.Add(sig.Pattern);
								summary.Items.Add(new SystemInformerEvidenceItem
								{
									ProcessName = p.ProcessName + ".exe",
									ProcessId = p.Id,
									Category = SystemInformerCategory.JavawStrings,
									SourceMethod = "Memory Strings (min len 4)",
									DetectedStringOrPath = Truncate(str, 180),
									FileSizeOrWeight = str.Length + " симв.",
									TimeInfo = "0x" + addr.ToString("X"),
									Verdict = "Сигнатура: " + sig.ClientOrType,
									RuleViolation = sig.Rule,
									Description = "В памяти javaw.exe обнаружена сигнатура чита [" + sig.Pattern + "] -> " + sig.ClientOrType + ".",
									FullDetails = string.Format("Процесс: {0}.exe (PID {1})\r\nРегион: 0x{2:X}\r\nСигнатура: {3} ({4})\r\nРегламент: {5}\r\nКонтекст строки:\r\n{6}",
										p.ProcessName, p.Id, addr, sig.Pattern, sig.ClientOrType, sig.Rule, Truncate(str, 600)),
									Severity = CheckSeverity.Critical,
									IsViolation = true
								});
							}
						}
					});

				if (nonMixinAsmCount >= 10 && !reportedSigs.Contains("ASM:MULTI"))
				{
					summary.Items.Add(new SystemInformerEvidenceItem
					{
						ProcessName = p.ProcessName + ".exe",
						ProcessId = p.Id,
						Category = SystemInformerCategory.JavawStrings,
						SourceMethod = "Memory Strings -> ASM: (>= 10)",
						DetectedStringOrPath = "ASM: x" + nonMixinAsmCount + " (" + string.Join(" | ", asmLongOrCountHits.ToArray()) + ")",
						FileSizeOrWeight = nonMixinAsmCount + " совп.",
						TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
						Verdict = "DEADCODE Client (множество ASM:)",
						RuleViolation = "5.01",
						Description = "Зафиксировано " + nonMixinAsmCount + " строк ASM: (>= 10) — характерный признак чита DEADCODE.",
						FullDetails = "Примеры строк:\r\n" + string.Join("\r\n", asmLongOrCountHits.ToArray()),
						Severity = CheckSeverity.Critical,
						IsViolation = true
					});
				}
			}
			finally
			{
				CloseHandle(hProc);
			}
		}

		private static void ScanProcessHackerAndSystemInformerInjectHistory(SystemInformerScanSummary summary, bool includeInformational)
		{
			// Проверка реестра Process Hacker / System Informer InjectDllHistory (мануал п.31-33: под нижней стрелкой должно быть пусто)
			string[] regKeys = new string[]
			{
				@"Software\Process Hacker",
				@"Software\Process Hacker 2",
				@"Software\System Informer",
				@"Software\Winsider Seminars & Solutions\System Informer"
			};
			string[] valueNames = new string[] { "InjectDllHistory", "DllHistory", "InjectDllFileHistory" };

			foreach (string keyPath in regKeys)
			{
				try
				{
					using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(keyPath))
					{
						if (rk == null) continue;
						foreach (string valName in valueNames)
						{
							object val = rk.GetValue(valName);
							if (val == null) continue;

							List<string> dlls = ExtractInjectDllEntries(val);
							foreach (string dll in dlls)
							{
								summary.Items.Add(new SystemInformerEvidenceItem
								{
									ProcessName = "Registry HKCU",
									ProcessId = 0,
									Category = SystemInformerCategory.JavawModules,
									SourceMethod = "Process Hacker / System Informer -> InjectDllHistory",
									DetectedStringOrPath = dll,
									FileSizeOrWeight = "—",
									TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
									Verdict = "История инжекта DLL (Miscellaneous -> Inject DLL)",
									RuleViolation = "5.01",
									Description = "В реестре System Informer / Process Hacker 2 обнаружена сохранённая история инжекта DLL: " + dll,
									FullDetails = "Ключ: HKCU\\" + keyPath + "\\" + valName + "\r\nDLL: " + dll,
									Severity = CheckSeverity.Critical,
									IsViolation = true
								});
							}
						}
					}
				}
				catch { }
			}

			// Проверка %APPDATA%\Process Hacker 2\settings.xml и %APPDATA%\System Informer\settings.xml
			try
			{
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				if (!string.IsNullOrEmpty(appData))
				{
					string[] xmlPaths = new string[]
					{
						Path.Combine(appData, @"Process Hacker 2\settings.xml"),
						Path.Combine(appData, @"System Informer\settings.xml")
					};
					foreach (string xmlPath in xmlPaths)
					{
						if (!File.Exists(xmlPath)) continue;
						try
						{
							string content = File.ReadAllText(xmlPath, Encoding.UTF8);
							MatchCollection matches = Regex.Matches(content, @"<setting\s+name=""(?:InjectDllHistory|DllHistory|InjectDllFileHistory)"">([^<]+)</setting>", RegexOptions.IgnoreCase);
							bool anyDllFound = false;
							foreach (Match m in matches)
							{
								string inner = m.Groups[1].Value.Trim();
								if (!string.IsNullOrEmpty(inner) && inner.ToLowerInvariant().Contains(".dll"))
								{
									anyDllFound = true;
									summary.Items.Add(new SystemInformerEvidenceItem
									{
										ProcessName = Path.GetFileName(Path.GetDirectoryName(xmlPath)),
										ProcessId = 0,
										Category = SystemInformerCategory.JavawModules,
										SourceMethod = "settings.xml -> InjectDllHistory",
										DetectedStringOrPath = inner,
										FileSizeOrWeight = "—",
										TimeInfo = File.GetLastWriteTime(xmlPath).ToString("dd.MM.yyyy HH:mm:ss"),
										Verdict = "Инжект DLL в settings.xml",
										RuleViolation = "5.01",
										Description = "В файле settings.xml обнаружен список инжектированных DLL.",
										FullDetails = "Файл: " + xmlPath + "\r\nЗапись: " + inner,
										Severity = CheckSeverity.Critical,
										IsViolation = true
									});
								}
							}
							if (includeInformational && !anyDllFound)
							{
								summary.Items.Add(new SystemInformerEvidenceItem
								{
									ProcessName = Path.GetFileName(Path.GetDirectoryName(xmlPath)),
									ProcessId = 0,
									Category = SystemInformerCategory.JavawModules,
									SourceMethod = "settings.xml",
									DetectedStringOrPath = xmlPath,
									FileSizeOrWeight = new FileInfo(xmlPath).Length + " байт",
									TimeInfo = File.GetLastWriteTime(xmlPath).ToString("dd.MM.yyyy HH:mm:ss"),
									Verdict = "История Inject DLL пуста",
									RuleViolation = "ЧИСТО",
									Description = "Конфигурация System Informer/Process Hacker найдена, но список InjectDllHistory пуст.",
									FullDetails = "Файл: " + xmlPath + "\r\nПоле InjectDllHistory пустое (нарушений нет).",
									Severity = CheckSeverity.Info,
									IsViolation = false
								});
							}
						}
						catch { }
					}
				}
			}
			catch { }
		}

		private static List<string> ExtractInjectDllEntries(object regVal)
		{
			List<string> list = new List<string>();
			string[] arr = regVal as string[];
			if (arr != null)
			{
				foreach (string s in arr)
				{
					if (!string.IsNullOrWhiteSpace(s) && s.ToLowerInvariant().Contains(".dll"))
					{
						list.Add(s.Trim());
					}
				}
			}
			else
			{
				string single = (regVal.ToString() ?? "").Trim();
				if (!string.IsNullOrEmpty(single) && single.ToLowerInvariant().Contains(".dll"))
				{
					list.Add(single);
				}
			}
			return list;
		}

		#endregion

		#region 2. explorer.exe (pcaclient + file:///)

		private static void ScanExplorerProcesses(SystemInformerScanSummary summary, bool includeInformational)
		{
			Process[] explorers;
			try { explorers = Process.GetProcessesByName("explorer"); }
			catch { return; }

			foreach (Process exp in explorers)
			{
				IntPtr hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, exp.Id);
				if (hProc == IntPtr.Zero) continue;

				try
				{
					HashSet<string> seenPca = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					HashSet<string> seenFileUris = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					int infoPcaCount = 0;
					int infoUriCount = 0;

					ScanProcessMemoryStrings(
						hProc,
						96L * 1024 * 1024,
						1500,
						false,
						1024 * 1024,
						delegate(string aLow, string uLow0, string uLow1)
						{
							return aLow.IndexOf("pcaclient", StringComparison.Ordinal) >= 0 ||
								aLow.IndexOf("file:///", StringComparison.Ordinal) >= 0 ||
								uLow0.IndexOf("pcaclient", StringComparison.Ordinal) >= 0 ||
								uLow0.IndexOf("file:///", StringComparison.Ordinal) >= 0 ||
								uLow1.IndexOf("pcaclient", StringComparison.Ordinal) >= 0 ||
								uLow1.IndexOf("file:///", StringComparison.Ordinal) >= 0;
						},
						delegate(string str, ulong addr)
						{
							string lower = AsciiToLowerFast(str);

							// 1. pcaclient
							if (lower.Contains("pcaclient") && !lower.Contains("parameters too long"))
							{
								string normalized = str.Trim();
								if (normalized.Length >= 12 && seenPca.Add(normalized))
								{
									string extractedPath = ExtractFirstWindowsPath(normalized);
									bool isCheat = false;
									bool isCleaner = false;
									string matchSig = "";

									if (!string.IsNullOrEmpty(extractedPath))
									{
										EvaluatePathThreat(extractedPath, out isCheat, out isCleaner, out matchSig);
									}
									else
									{
										EvaluatePathThreat(normalized, out isCheat, out isCleaner, out matchSig);
									}

									if (isCheat || isCleaner)
									{
										summary.Items.Add(new SystemInformerEvidenceItem
										{
											ProcessName = "explorer.exe",
											ProcessId = exp.Id,
											Category = SystemInformerCategory.Explorer,
											SourceMethod = "explorer.exe -> pcaclient",
											DetectedStringOrPath = string.IsNullOrEmpty(extractedPath) ? Truncate(normalized, 200) : extractedPath,
											FileSizeOrWeight = "PcaClient Cache",
											TimeInfo = "0x" + addr.ToString("X"),
											Verdict = isCleaner ? "Утилита очистки (" + matchSig + ")" : "Чит в кэше PcaClient (" + matchSig + ")",
											RuleViolation = isCleaner ? "5.08" : "5.01",
											Description = "В памяти explorer.exe (кэш PcaClient) найден путь запуска запрещённого ПО: " + matchSig,
											FullDetails = "Полная строка памяти:\r\n" + Truncate(normalized, 500),
											Severity = CheckSeverity.Critical,
											IsViolation = true
										});
									}
									else if (includeInformational && infoPcaCount < 25)
									{
										infoPcaCount++;
										summary.Items.Add(new SystemInformerEvidenceItem
										{
											ProcessName = "explorer.exe",
											ProcessId = exp.Id,
											Category = SystemInformerCategory.Explorer,
											SourceMethod = "explorer.exe -> pcaclient",
											DetectedStringOrPath = string.IsNullOrEmpty(extractedPath) ? Truncate(normalized, 200) : extractedPath,
											FileSizeOrWeight = "PcaClient",
											TimeInfo = "0x" + addr.ToString("X"),
											Verdict = "Легитимная активность",
											RuleViolation = "ЧИСТО",
											Description = "Кэшированная строка PcaClient в explorer.exe.",
											FullDetails = Truncate(normalized, 400),
											Severity = CheckSeverity.Info,
											IsViolation = false
										});
									}
								}
							}

							// 2. file:/// (.exe, .dll, .jar)
							if (lower.Contains("file:///"))
							{
								// Отбрасываем системные шаблоны документации shdocvw (c:/test/test.htm и т.п.)
								if (lower.Contains("urls are returned to us") || lower.Contains("c:/test/test.htm")) return;

								MatchCollection uris = Regex.Matches(str, @"file:///[^\s""<>|\0\r\n]+", RegexOptions.IgnoreCase);
								foreach (Match um in uris)
								{
									string uriRaw = um.Value;
									string uriLower = uriRaw.ToLowerInvariant();
									if (!(uriLower.EndsWith(".exe") || uriLower.EndsWith(".dll") || uriLower.EndsWith(".jar") ||
										  uriLower.Contains(".exe") || uriLower.Contains(".dll") || uriLower.Contains(".jar")))
									{
										continue;
									}

									string localPath = DecodeFileUriToWindowsPath(uriRaw);
									if (string.IsNullOrEmpty(localPath) || !seenFileUris.Add(localPath)) continue;

									bool isCheat, isCleaner;
									string matchSig;
									EvaluatePathThreat(localPath, out isCheat, out isCleaner, out matchSig);

									if (isCheat || isCleaner)
									{
										summary.Items.Add(new SystemInformerEvidenceItem
										{
											ProcessName = "explorer.exe",
											ProcessId = exp.Id,
											Category = SystemInformerCategory.Explorer,
											SourceMethod = "explorer.exe -> file:///",
											DetectedStringOrPath = localPath,
											FileSizeOrWeight = FileExistsSafe(localPath) ? FormatFileSize(localPath) : "Удалён",
											TimeInfo = "0x" + addr.ToString("X"),
											Verdict = isCleaner ? "Обращение к клинеру (" + matchSig + ")" : "Обращение к читу (" + matchSig + ")",
											RuleViolation = isCleaner ? "5.08" : "5.01",
											Description = "Через оболочку explorer.exe (file:///) открывался подозрительный исполняемый файл/JAR: " + matchSig,
											FullDetails = "URI: " + uriRaw + "\r\nПуть: " + localPath,
											Severity = CheckSeverity.Critical,
											IsViolation = true
										});
									}
									else if (includeInformational && infoUriCount < 30 && !localPath.ToLowerInvariant().StartsWith(@"c:\windows\"))
									{
										infoUriCount++;
										summary.Items.Add(new SystemInformerEvidenceItem
										{
											ProcessName = "explorer.exe",
											ProcessId = exp.Id,
											Category = SystemInformerCategory.Explorer,
											SourceMethod = "explorer.exe -> file:///",
											DetectedStringOrPath = localPath,
											FileSizeOrWeight = FileExistsSafe(localPath) ? FormatFileSize(localPath) : "Отсутствует",
											TimeInfo = "0x" + addr.ToString("X"),
											Verdict = "Просмотр в Проводнике",
											RuleViolation = "ЧИСТО",
											Description = "URI оболочки Windows explorer.exe (подтверждает просмотр, но не запуск).",
											FullDetails = "URI: " + uriRaw + "\r\nЛокальный путь: " + localPath,
											Severity = CheckSeverity.Info,
											IsViolation = false
										});
									}
								}
							}
						});
				}
				finally
				{
					CloseHandle(hProc);
				}
			}
		}

		#endregion

		#region 3. DcomLaunch / PlugPlay (.jar paths)

		private static void ScanDcomLaunchAndPlugPlay(SystemInformerScanSummary summary, bool includeInformational)
		{
			Dictionary<int, List<string>> pidToServices = new Dictionary<int, List<string>>();
			foreach (string svc in new string[] { "DcomLaunch", "PlugPlay" })
			{
				int pid = ResolveServicePid(svc);
				if (pid > 0)
				{
					if (!pidToServices.ContainsKey(pid)) pidToServices[pid] = new List<string>();
					pidToServices[pid].Add(svc);
				}
			}

			if (pidToServices.Count == 0)
			{
				if (includeInformational)
				{
					summary.Items.Add(new SystemInformerEvidenceItem
					{
						ProcessName = "svchost (DcomLaunch/PlugPlay)",
						ProcessId = 0,
						Category = SystemInformerCategory.DcomLaunch,
						SourceMethod = "WMI Win32_Service",
						DetectedStringOrPath = "PID не получен",
						FileSizeOrWeight = "—",
						TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
						Verdict = "Служба не найдена",
						RuleViolation = "ИНФО",
						Description = "Не удалось определить активный PID служб DcomLaunch / PlugPlay.",
						FullDetails = "WMI Win32_Service не вернул активный ProcessId.",
						Severity = CheckSeverity.Info,
						IsViolation = false
					});
				}
				return;
			}

			foreach (KeyValuePair<int, List<string>> kv in pidToServices)
			{
				int pid = kv.Key;
				string svcLabel = string.Join("/", kv.Value.ToArray());

				IntPtr hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
				if (hProc == IntPtr.Zero)
				{
					if (includeInformational)
					{
						summary.Items.Add(new SystemInformerEvidenceItem
						{
							ProcessName = "svchost (" + svcLabel + ")",
							ProcessId = pid,
							Category = SystemInformerCategory.DcomLaunch,
							SourceMethod = "OpenProcess",
							DetectedStringOrPath = "ACCESS DENIED (требуется Admin)",
							FileSizeOrWeight = "—",
							TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
							Verdict = "Ограничение прав доступа",
							RuleViolation = "ИНФО",
							Description = "Для чтения памяти системной службы " + svcLabel + " запустите программу от имени Администратора.",
							FullDetails = "OpenProcess PID=" + pid + " вернул ошибку " + Marshal.GetLastWin32Error() + ".",
							Severity = CheckSeverity.Info,
							IsViolation = false
						});
					}
					continue;
				}

				try
				{
					HashSet<string> seenJars = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					int infoCount = 0;

					ScanProcessMemoryStrings(
						hProc,
						64L * 1024 * 1024,
						1400,
						false,
						1024 * 1024,
						delegate(string aLow, string uLow0, string uLow1)
						{
							return aLow.IndexOf(".jar", StringComparison.Ordinal) >= 0 ||
								uLow0.IndexOf(".jar", StringComparison.Ordinal) >= 0 ||
								uLow1.IndexOf(".jar", StringComparison.Ordinal) >= 0;
						},
						delegate(string str, ulong addr)
						{
							if (str.IndexOf(".jar", StringComparison.OrdinalIgnoreCase) < 0) return;
							MatchCollection matches = JarPathExtractRegex.Matches(str);
							foreach (Match m in matches)
							{
								string jarPath = CleanWin32Path(m.Value);
								if (!seenJars.Add(jarPath)) continue;

								string lower = jarPath.ToLowerInvariant();
								if (lower.Contains(@"\.minecraft\libraries\") ||
									lower.Contains(@"\instances\") ||
									CheatDatabases.IsWhitelistedPath(lower) ||
									CheatDatabases.IsAllowedCpvpOptimizer(lower) ||
									CheatDatabases.IsAllowedVisualMod(lower))
								{
									continue;
								}

								bool isCheat, isCleaner;
								string matchSig;
								EvaluatePathThreat(jarPath, out isCheat, out isCleaner, out matchSig);

								bool exists = FileExistsSafe(jarPath);
								string fileNameLower = Path.GetFileName(jarPath).ToLowerInvariant();
								bool isStandardInstaller = fileNameLower.Contains("fabric-installer") ||
									fileNameLower.Contains("forge-") || fileNameLower.Contains("neoforge-") ||
									fileNameLower.Contains("optifine") || fileNameLower.Contains("iris-installer") ||
									fileNameLower.Contains("quilt-installer");

								if (isCheat || isCleaner)
								{
									summary.Items.Add(new SystemInformerEvidenceItem
									{
										ProcessName = "svchost (" + svcLabel + ")",
										ProcessId = pid,
										Category = SystemInformerCategory.DcomLaunch,
										SourceMethod = svcLabel + " -> .jar",
										DetectedStringOrPath = jarPath,
										FileSizeOrWeight = exists ? FormatFileSize(jarPath) : "Удалён с диска",
										TimeInfo = "0x" + addr.ToString("X"),
										Verdict = exists ? "Чит-JAR (" + matchSig + ")" : "Удалённый чит-JAR (" + matchSig + ")",
										RuleViolation = exists ? "5.01" : "5.08",
										Description = "В памяти " + svcLabel + " зафиксирован путь к запрещённому JAR-файлу: " + matchSig,
										FullDetails = "Путь: " + jarPath + "\r\nСуществует на диске: " + (exists ? "Да" : "НЕТ (удалён)") + "\r\nРегион: 0x" + addr.ToString("X"),
										Severity = CheckSeverity.Critical,
										IsViolation = true
									});
								}
								else if (!exists && !isStandardInstaller && (lower.Contains(@"\temp\") || lower.Contains(@"\appdata\local\temp\")))
								{
									summary.Items.Add(new SystemInformerEvidenceItem
									{
										ProcessName = "svchost (" + svcLabel + ")",
										ProcessId = pid,
										Category = SystemInformerCategory.DcomLaunch,
										SourceMethod = svcLabel + " -> Temp Deleted .jar",
										DetectedStringOrPath = jarPath,
										FileSizeOrWeight = "Удалён с диска",
										TimeInfo = "0x" + addr.ToString("X"),
										Verdict = "Удалённый JAR из Temp",
										RuleViolation = "5.08",
										Description = "В памяти " + svcLabel + " зафиксирован JAR-файл из временной папки Temp, отсутствующий на диске.",
										FullDetails = "Путь: " + jarPath + "\r\nНа диске: ОТСУТСТВУЕТ (Temp)",
										Severity = CheckSeverity.Critical,
										IsViolation = true
									});
								}
								else if (includeInformational && infoCount < 25)
								{
									infoCount++;
									summary.Items.Add(new SystemInformerEvidenceItem
									{
										ProcessName = "svchost (" + svcLabel + ")",
										ProcessId = pid,
										Category = SystemInformerCategory.DcomLaunch,
										SourceMethod = svcLabel + " -> .jar",
										DetectedStringOrPath = jarPath,
										FileSizeOrWeight = exists ? FormatFileSize(jarPath) : "Не найден",
										TimeInfo = "0x" + addr.ToString("X"),
										Verdict = "Легитимный JAR",
										RuleViolation = "ЧИСТО",
										Description = "Путь к JAR-архиву в памяти " + svcLabel + ".",
										FullDetails = "Путь: " + jarPath + "\r\nСтатус на диске: " + (exists ? "Присутствует" : "Не найден"),
										Severity = CheckSeverity.Info,
										IsViolation = false
									});
								}
							}
						});
				}
				finally
				{
					CloseHandle(hProc);
				}
			}
		}

		#endregion

		#region 4. PcaSvc (jar command lines, fake extensions, cheat exes)

		private static void ScanPcaSvc(SystemInformerScanSummary summary, bool includeInformational)
		{
			int pid = ResolveServicePid("PcaSvc");
			if (pid <= 0)
			{
				if (includeInformational)
				{
					summary.Items.Add(new SystemInformerEvidenceItem
					{
						ProcessName = "svchost (PcaSvc)",
						ProcessId = 0,
						Category = SystemInformerCategory.PcaSvc,
						SourceMethod = "WMI Win32_Service",
						DetectedStringOrPath = "Служба остановлена",
						FileSizeOrWeight = "—",
						TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
						Verdict = "PcaSvc не активна",
						RuleViolation = "5.08",
						Description = "Служба Program Compatibility Assistant (PcaSvc) не запущена — возможен уход от проверки.",
						FullDetails = "Win32_Service Name='PcaSvc' не вернул активный ProcessId.",
						Severity = CheckSeverity.Warning,
						IsViolation = false // Отдельная проверка служб уже есть в ManualChecks.RunRequiredServicesCheck
					});
				}
				return;
			}

			IntPtr hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
			if (hProc == IntPtr.Zero)
			{
				if (includeInformational)
				{
					summary.Items.Add(new SystemInformerEvidenceItem
					{
						ProcessName = "svchost (PcaSvc)",
						ProcessId = pid,
						Category = SystemInformerCategory.PcaSvc,
						SourceMethod = "OpenProcess",
						DetectedStringOrPath = "ACCESS DENIED (требуется Admin)",
						FileSizeOrWeight = "—",
						TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
						Verdict = "Ограничение прав доступа",
						RuleViolation = "ИНФО",
						Description = "Для чтения памяти службы PcaSvc запустите программу от имени Администратора.",
						FullDetails = "OpenProcess PID=" + pid + " вернул ошибку " + Marshal.GetLastWin32Error() + ".",
						Severity = CheckSeverity.Info,
						IsViolation = false
					});
				}
				return;
			}

			try
			{
				HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				int infoCount = 0;

				ScanProcessMemoryStrings(
					hProc,
					64L * 1024 * 1024,
					1400,
					false,
					1024 * 1024,
					delegate(string aLow, string uLow0, string uLow1)
					{
						return aLow.IndexOf("jar", StringComparison.Ordinal) >= 0 ||
							aLow.IndexOf(@":\", StringComparison.Ordinal) >= 0 ||
							uLow0.IndexOf("jar", StringComparison.Ordinal) >= 0 ||
							uLow0.IndexOf(@":\", StringComparison.Ordinal) >= 0 ||
							uLow1.IndexOf("jar", StringComparison.Ordinal) >= 0 ||
							uLow1.IndexOf(@":\", StringComparison.Ordinal) >= 0;
					},
					delegate(string str, ulong addr)
					{
						string lower = AsciiToLowerFast(str);
						if (lower.Contains("mccheatchecker") || lower.Contains("screenshare")) return;

						// 1. Поиск "jar" (без точки!) — выявление "java -jar file.png" и поддельных расширений
						if (lower.Contains("jar"))
						{
							// Чтобы не ловить слова типа "jarvis" / "jaro", проверяем контекст запуска или пути
							bool isJavaJarLaunch = Regex.IsMatch(lower, @"(?:-jar\s+|javaw?\.exe[^\r\n]*\bjar\b|\.jar\b)");
							bool isPathWithJar = lower.Contains(@":\") && lower.Contains("jar") &&
								(lower.Contains("-jar") || lower.Contains(".jar") || lower.Contains(@"\java"));

							if (isJavaJarLaunch || isPathWithJar)
							{
								string cleanStr = Truncate(str.Trim(), 240);
								if (seen.Add("JAR:" + cleanStr))
								{
									// Проверка запуска java -jar с поддельным расширением (.png, .txt, .tmp, .dat, .jpg, без .jar)
									Match fakeExtMatch = Regex.Match(str, @"-jar\s+""?([^\r\n""]+\.(?:png|jpg|jpeg|txt|tmp|dat|bin|exe|bak))""?", RegexOptions.IgnoreCase);
									bool isCheat, isCleaner;
									string matchSig;
									EvaluatePathThreat(str, out isCheat, out isCleaner, out matchSig);

									if (fakeExtMatch.Success)
									{
										summary.Items.Add(new SystemInformerEvidenceItem
										{
											ProcessName = "svchost (PcaSvc)",
											ProcessId = pid,
											Category = SystemInformerCategory.PcaSvc,
											SourceMethod = "PcaSvc -> java -jar fake ext",
											DetectedStringOrPath = cleanStr,
											FileSizeOrWeight = "Командная строка",
											TimeInfo = "0x" + addr.ToString("X"),
											Verdict = "Маскировка JAR под " + Path.GetExtension(fakeExtMatch.Groups[1].Value),
											RuleViolation = "5.08",
											Description = "В памяти PcaSvc обнаружен запуск java -jar файла с поддельным расширением (сокрытие софта)!",
											FullDetails = "Строка в PcaSvc:\r\n" + Truncate(str, 500),
											Severity = CheckSeverity.Critical,
											IsViolation = true
										});
									}
									else if (isCheat || isCleaner)
									{
										summary.Items.Add(new SystemInformerEvidenceItem
										{
											ProcessName = "svchost (PcaSvc)",
											ProcessId = pid,
											Category = SystemInformerCategory.PcaSvc,
											SourceMethod = "PcaSvc -> jar signature",
											DetectedStringOrPath = cleanStr,
											FileSizeOrWeight = "PcaSvc String",
											TimeInfo = "0x" + addr.ToString("X"),
											Verdict = "Чит-JAR в PcaSvc (" + matchSig + ")",
											RuleViolation = "5.01",
											Description = "В памяти PcaSvc обнаружен запуск JAR с сигнатурой чита: " + matchSig,
											FullDetails = Truncate(str, 500),
											Severity = CheckSeverity.Critical,
											IsViolation = true
										});
									}
									else if (includeInformational && infoCount < 20)
									{
										infoCount++;
										summary.Items.Add(new SystemInformerEvidenceItem
										{
											ProcessName = "svchost (PcaSvc)",
											ProcessId = pid,
											Category = SystemInformerCategory.PcaSvc,
											SourceMethod = "PcaSvc -> jar",
											DetectedStringOrPath = cleanStr,
											FileSizeOrWeight = "PcaSvc",
											TimeInfo = "0x" + addr.ToString("X"),
											Verdict = "Запуск Java / JAR",
											RuleViolation = "ЧИСТО",
											Description = "Упоминание jar в памяти службы PcaSvc.",
											FullDetails = Truncate(str, 400),
											Severity = CheckSeverity.Info,
											IsViolation = false
										});
									}
								}
							}
						}

						// 2. Прямой поиск известных чит exe/dll в строках путей PcaSvc
						if (lower.Contains(@":\") || lower.Contains(".exe"))
						{
							bool isCheat2, isCleaner2;
							string matchSig2;
							EvaluatePathThreat(str, out isCheat2, out isCleaner2, out matchSig2);
							if (isCheat2 || isCleaner2)
							{
								string key = "PCA_SIG:" + matchSig2 + ":" + Truncate(str.Trim(), 120);
								if (seen.Add(key))
								{
									summary.Items.Add(new SystemInformerEvidenceItem
									{
										ProcessName = "svchost (PcaSvc)",
										ProcessId = pid,
										Category = SystemInformerCategory.PcaSvc,
										SourceMethod = "PcaSvc -> Executable Signature",
										DetectedStringOrPath = Truncate(str.Trim(), 220),
										FileSizeOrWeight = "PcaSvc Cache",
										TimeInfo = "0x" + addr.ToString("X"),
										Verdict = isCleaner2 ? "Клинер в PcaSvc (" + matchSig2 + ")" : "Чит в PcaSvc (" + matchSig2 + ")",
										RuleViolation = isCleaner2 ? "5.08" : "5.01",
										Description = "В памяти службы PcaSvc найдена сигнатура запрещённого ПО: " + matchSig2,
										FullDetails = Truncate(str, 500),
										Severity = CheckSeverity.Critical,
										IsViolation = true
									});
								}
							}
						}
					});
			}
			finally
			{
				CloseHandle(hProc);
			}
		}

		#endregion

		#region 5. DPS Service (Paths, !!NoExe, Timestamps, DPS Replace)

		private class DpsExecutionRecord
		{
			public string PathOrName;
			public string FileName;
			public string Timestamp;
			public ulong Address;
		}

		private static void ScanDpsService(SystemInformerScanSummary summary, bool includeInformational)
		{
			int pid = ResolveServicePid("DPS");
			if (pid <= 0)
			{
				if (includeInformational)
				{
					summary.Items.Add(new SystemInformerEvidenceItem
					{
						ProcessName = "svchost (DPS)",
						ProcessId = 0,
						Category = SystemInformerCategory.Dps,
						SourceMethod = "WMI Win32_Service",
						DetectedStringOrPath = "Служба не активна",
						FileSizeOrWeight = "—",
						TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
						Verdict = "DPS не найдена",
						RuleViolation = "ИНФО",
						Description = "Не удалось получить активный PID службы диагностики DPS.",
						FullDetails = "Win32_Service Name='DPS' не вернул активный ProcessId.",
						Severity = CheckSeverity.Info,
						IsViolation = false
					});
				}
				return;
			}

			IntPtr hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, pid);
			if (hProc == IntPtr.Zero)
			{
				if (includeInformational)
				{
					summary.Items.Add(new SystemInformerEvidenceItem
					{
						ProcessName = "svchost (DPS)",
						ProcessId = pid,
						Category = SystemInformerCategory.Dps,
						SourceMethod = "OpenProcess",
						DetectedStringOrPath = "ACCESS DENIED (требуется Admin)",
						FileSizeOrWeight = "—",
						TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
						Verdict = "Ограничение прав доступа",
						RuleViolation = "ИНФО",
						Description = "Для чтения памяти службы DPS запустите программу от имени Администратора.",
						FullDetails = "OpenProcess PID=" + pid + " вернул ошибку " + Marshal.GetLastWin32Error() + ".",
						Severity = CheckSeverity.Info,
						IsViolation = false
					});
				}
				return;
			}

			try
			{
				HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				HashSet<string> seenNoExe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				Dictionary<string, List<DpsExecutionRecord>> exeByName = new Dictionary<string, List<DpsExecutionRecord>>(StringComparer.OrdinalIgnoreCase);
				int infoCount = 0;

				ScanProcessMemoryStrings(
					hProc,
					64L * 1024 * 1024,
					1500,
					false,
					1024 * 1024,
					delegate(string aLow, string uLow0, string uLow1)
					{
						return aLow.IndexOf(@":\", StringComparison.Ordinal) >= 0 ||
							aLow.IndexOf("!!", StringComparison.Ordinal) >= 0 ||
							uLow0.IndexOf(@":\", StringComparison.Ordinal) >= 0 ||
							uLow0.IndexOf("!!", StringComparison.Ordinal) >= 0 ||
							uLow1.IndexOf(@":\", StringComparison.Ordinal) >= 0 ||
							uLow1.IndexOf("!!", StringComparison.Ordinal) >= 0;
					},
					delegate(string str, ulong addr)
					{
						string trimmed = str.Trim();
						if (trimmed.Length < 4) return;
						string lower = AsciiToLowerFast(trimmed);
						if (lower.Contains("mccheatchecker") || lower.Contains("screenshare")) return;

						// Извлекаем таймштамп !YYYY/MM/DD:HH:MM:SS! если присутствует
						string timeStr = "—";
						Match tm = DpsTimestampRegex.Match(trimmed);
						if (tm.Success) timeStr = tm.Groups[1].Value;

						// Шаблон 1: ^([a-zA-Z]:\\.+)\\?$
						if (trimmed.Length >= 6 && trimmed[1] == ':' && trimmed[2] == '\\' && DpsPathRegex.IsMatch(trimmed))
						{
							string cleanPath = CleanWin32Path(trimmed.TrimEnd('\\'));
							if (seenPaths.Add(cleanPath))
							{
								string fileName = "";
								try { fileName = Path.GetFileName(cleanPath); } catch { }

								if (!string.IsNullOrEmpty(fileName) && cleanPath.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))
								{
									if (!exeByName.ContainsKey(fileName)) exeByName[fileName] = new List<DpsExecutionRecord>();
									exeByName[fileName].Add(new DpsExecutionRecord
									{
										PathOrName = cleanPath,
										FileName = fileName,
										Timestamp = timeStr,
										Address = addr
									});
								}

								bool isCheat, isCleaner;
								string matchSig;
								EvaluatePathThreat(cleanPath, out isCheat, out isCleaner, out matchSig);
								bool exists = FileExistsSafe(cleanPath);

								if (isCheat || isCleaner)
								{
									summary.Items.Add(new SystemInformerEvidenceItem
									{
										ProcessName = "svchost (DPS)",
										ProcessId = pid,
										Category = SystemInformerCategory.Dps,
										SourceMethod = "DPS -> ^([a-zA-Z]:\\\\.+)\\\\?$",
										DetectedStringOrPath = cleanPath,
										FileSizeOrWeight = exists ? FormatFileSize(cleanPath) : "Удалён с диска",
										TimeInfo = timeStr,
										Verdict = isCleaner ? "Клинер в DPS (" + matchSig + ")" : "Чит в DPS (" + matchSig + ")",
										RuleViolation = !exists || isCleaner ? "5.08" : "5.01",
										Description = "В кэше службы DPS зафиксирован путь к запрещённому ПО: " + matchSig,
										FullDetails = string.Format("Путь: {0}\r\nТаймштамп: {1}\r\nНа диске: {2}", cleanPath, timeStr, exists ? "Да" : "Удалён"),
										Severity = CheckSeverity.Critical,
										IsViolation = true
									});
								}
								else if (includeInformational && infoCount < 30 && !cleanPath.ToLowerInvariant().StartsWith(@"c:\windows\"))
								{
									infoCount++;
									summary.Items.Add(new SystemInformerEvidenceItem
									{
										ProcessName = "svchost (DPS)",
										ProcessId = pid,
										Category = SystemInformerCategory.Dps,
										SourceMethod = "DPS -> Path",
										DetectedStringOrPath = cleanPath,
										FileSizeOrWeight = exists ? FormatFileSize(cleanPath) : "Не найден",
										TimeInfo = timeStr,
										Verdict = "Запись выполнения DPS",
										RuleViolation = "ЧИСТО",
										Description = "Путь из адресного пространства службы DPS.",
										FullDetails = "Путь: " + cleanPath + "\r\nВремя/метка: " + timeStr,
										Severity = CheckSeverity.Info,
										IsViolation = false
									});
								}
							}
						}

						// Шаблон 2: ^!![A-Z]((?!Exe).)*$ (файлы без расширения .exe / переименованные бинарники в формате DPS !!Name!YYYY/MM/DD:HH:MM:SS!)
						if (trimmed.StartsWith("!!") && trimmed.Length >= 6 && trimmed.IndexOf('!', 2) > 2 && DpsNoExeRegex.IsMatch(trimmed))
						{
							// Для исключения случайных текстовых строк требуем валидную структуру DPS: !!<ImageName>!<Date/Hex>!
							string[] parts = trimmed.Split(new char[] { '!' }, StringSplitOptions.RemoveEmptyEntries);
							if (parts.Length >= 2 && parts[0].Length >= 2 && parts[0].Length <= 120)
							{
								string imgName = parts[0].Trim();
								string imgLower = imgName.ToLowerInvariant();
								// Требуем таймштамп YYYY/MM/DD:HH:MM:SS или характерный суффикс без .exe (не системные имена типа Registry/System)
								bool hasDpsTimestamp = tm.Success;
								bool isBenignSystemNoExe = imgLower == "registry" || imgLower == "system" || imgLower == "idle" ||
									imgLower == "memcompression" || imgLower.EndsWith(".dll") || imgLower.EndsWith(".sys") ||
									imgLower.EndsWith(".com") || imgLower.EndsWith(".scr");

								if (hasDpsTimestamp && !isBenignSystemNoExe && seenNoExe.Add(trimmed))
								{
									summary.Items.Add(new SystemInformerEvidenceItem
									{
										ProcessName = "svchost (DPS)",
										ProcessId = pid,
										Category = SystemInformerCategory.Dps,
										SourceMethod = "DPS -> ^!![A-Z]((?!Exe).)*$",
										DetectedStringOrPath = trimmed,
										FileSizeOrWeight = "Без расширения .exe",
										TimeInfo = timeStr,
										Verdict = "Переименованный/без .exe бинарник: " + imgName,
										RuleViolation = "5.08",
										Description = "В службе DPS обнаружен запуск файла без расширения .exe (" + imgName + ") — признак маскировки/переименования.",
										FullDetails = "Запись DPS: " + trimmed + "\r\nИмя: " + imgName + "\r\nВремя компиляции/события: " + timeStr,
										Severity = CheckSeverity.Critical,
										IsViolation = true
									});
								}
							}
						}
					});

				// Детекция DPS Replace (один и тот же .exe из разных необычных директорий / подмена AnyDesk и др.)
				foreach (KeyValuePair<string, List<DpsExecutionRecord>> kv in exeByName)
				{
					string exeName = kv.Key;
					List<DpsExecutionRecord> recs = kv.Value;
					if (recs.Count < 2) continue;

					if (BenignMultiPathSystemExes.Contains(exeName)) continue;

					bool isSsTool = ScreenShareToolExes.Contains(exeName);
					List<string> suspiciousPaths = new List<string>();
					List<string> allDistinctPaths = new List<string>();

					foreach (DpsExecutionRecord r in recs)
					{
						if (!allDistinctPaths.Contains(r.PathOrName)) allDistinctPaths.Add(r.PathOrName);
						string lp = r.PathOrName.ToLowerInvariant();
						bool isTrustedRoot = lp.StartsWith(@"c:\windows\") || lp.StartsWith(@"c:\program files\") ||
							lp.StartsWith(@"c:\program files (x86)\") || lp.Contains(@"\windowsapps\") ||
							lp.Contains(@"\appdata\local\programs\");

						if (!isTrustedRoot && (lp.Contains(@"\temp\") || lp.Contains(@"\downloads\") || lp.Contains(@"\desktop\") || lp.Contains(@"\appdata\")))
						{
							if (!suspiciousPaths.Contains(r.PathOrName)) suspiciousPaths.Add(r.PathOrName);
						}
					}

					if (allDistinctPaths.Count >= 2 && (isSsTool && suspiciousPaths.Count > 0 || suspiciousPaths.Count >= 2))
					{
						summary.Items.Add(new SystemInformerEvidenceItem
						{
							ProcessName = "svchost (DPS)",
							ProcessId = pid,
							Category = SystemInformerCategory.Dps,
							SourceMethod = "DPS Replace Correlation",
							DetectedStringOrPath = exeName + " (" + allDistinctPaths.Count + " путей)",
							FileSizeOrWeight = allDistinctPaths.Count + " путей",
							TimeInfo = recs[0].Timestamp,
							Verdict = isSsTool ? "Подмена утилиты SS (DPS Replace): " + exeName : "DPS Replace / Мульти-путь: " + exeName,
							RuleViolation = "5.08",
							Description = "Один и тот же бинарник (" + exeName + ") зафиксирован в DPS с конфликтующими путями (включая Temp/Downloads/Desktop).",
							FullDetails = "Исполняемый файл: " + exeName + "\r\nПути из памяти DPS:\r\n - " + string.Join("\r\n - ", allDistinctPaths.ToArray()),
							Severity = CheckSeverity.Critical,
							IsViolation = true
						});
					}
				}
			}
			finally
			{
				CloseHandle(hProc);
			}
		}

		#endregion

		#region 6. csrss.exe (exe, dll, disguised extension PE check, PPL-safe)

		private static void ScanCsrssProcesses(SystemInformerScanSummary summary, bool includeInformational)
		{
			Process[] csrssProcs;
			try { csrssProcs = Process.GetProcessesByName("csrss"); }
			catch { return; }

			if (csrssProcs.Length == 0) return;

			int accessibleCount = 0;
			int deniedCount = 0;

			foreach (Process cs in csrssProcs)
			{
				IntPtr hProc = OpenProcess(PROCESS_QUERY_INFORMATION | PROCESS_VM_READ, false, cs.Id);
				if (hProc == IntPtr.Zero)
				{
					deniedCount++;
					continue;
				}

				accessibleCount++;
				try
				{
					HashSet<string> seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
					int infoCount = 0;

					ScanProcessMemoryStrings(
						hProc,
						48L * 1024 * 1024,
						1200,
						false,
						1024 * 1024,
						delegate(string aLow, string uLow0, string uLow1)
						{
							return aLow.IndexOf(@":\", StringComparison.Ordinal) >= 0 ||
								uLow0.IndexOf(@":\", StringComparison.Ordinal) >= 0 ||
								uLow1.IndexOf(@":\", StringComparison.Ordinal) >= 0;
						},
						delegate(string str, ulong addr)
						{
							string trimmed = str.Trim();
							if (trimmed.Length < 6) return;
							string lower = AsciiToLowerFast(trimmed);
							if (lower.Contains("mccheatchecker") || lower.Contains("screenshare")) return;

							if (!CsrssBroadPathRegex.IsMatch(trimmed)) return;
							string cleanPath = CleanWin32Path(trimmed);
							if (!seenPaths.Add(cleanPath)) return;

							string cleanLower = cleanPath.ToLowerInvariant();
							bool isSystemDir = cleanLower.StartsWith(@"c:\windows\system32\") ||
								cleanLower.StartsWith(@"c:\windows\syswow64\") ||
								cleanLower.StartsWith(@"c:\windows\winsxs\");

							bool isExe = CsrssExeRegex.IsMatch(cleanPath);
							bool isDll = CsrssDllRegex.IsMatch(cleanPath);
							bool exists = FileExistsSafe(cleanPath);

							bool isCheat, isCleaner;
							string matchSig;
							EvaluatePathThreat(cleanPath, out isCheat, out isCleaner, out matchSig);

							// 1. Явная сигнатура чита/клинера
							if (isCheat || isCleaner)
							{
								summary.Items.Add(new SystemInformerEvidenceItem
								{
									ProcessName = "csrss.exe",
									ProcessId = cs.Id,
									Category = SystemInformerCategory.Csrss,
									SourceMethod = isDll ? "csrss -> .dll (Cheat)" : "csrss -> .exe (Cheat)",
									DetectedStringOrPath = cleanPath,
									FileSizeOrWeight = exists ? FormatFileSize(cleanPath) : "Удалён с диска",
									TimeInfo = "0x" + addr.ToString("X"),
									Verdict = isCleaner ? "Клинер в csrss (" + matchSig + ")" : "Чит в csrss (" + matchSig + ")",
									RuleViolation = isCleaner || !exists ? "5.08" : "5.01",
									Description = "В адресном пространстве csrss.exe зафиксирован путь к запрещённому ПО: " + matchSig,
									FullDetails = "Путь: " + cleanPath + "\r\nРегион: 0x" + addr.ToString("X"),
									Severity = CheckSeverity.Critical,
									IsViolation = true
								});
								return;
							}

							// 2. Неподписанная .dll вне системных/легальных директорий
							if (isDll && exists && !isSystemDir &&
								!ProcessMemoryScanner.IsLegitimateMinecraftNativeDll(Path.GetFileName(cleanPath), cleanPath))
							{
								if (!IsDigitallySigned(cleanPath) && !IsKnownSafeAppDirectory(cleanLower))
								{
									summary.Items.Add(new SystemInformerEvidenceItem
									{
										ProcessName = "csrss.exe",
										ProcessId = cs.Id,
										Category = SystemInformerCategory.Csrss,
										SourceMethod = "csrss -> ^[A-Z]:\\\\.+.dll$ (Unsigned)",
										DetectedStringOrPath = cleanPath,
										FileSizeOrWeight = FormatFileSize(cleanPath),
										TimeInfo = "0x" + addr.ToString("X"),
										Verdict = "Неподписанная DLL в csrss",
										RuleViolation = "5.01",
										Description = "В памяти csrss.exe обнаружена неподписанная DLL вне доверенных каталогов.",
										FullDetails = "Путь: " + cleanPath + "\r\nПодпись Authenticode: ОТСУТСТВУЕТ",
										Severity = CheckSeverity.Warning,
										IsViolation = true
									});
									return;
								}
							}

							// 3. Проверка поддельных расширений (например, .tmp, .png, .txt, .dat, .bak) являющихся PE (MZ) на диске
							if (!isExe && !isDll && exists && !isSystemDir)
							{
								string ext = "";
								try { ext = (Path.GetExtension(cleanPath) ?? "").ToLowerInvariant(); } catch { }
								bool isDisguiseCandidate = string.IsNullOrEmpty(ext) ||
									ext == ".tmp" || ext == ".png" || ext == ".jpg" || ext == ".txt" || ext == ".dat" || ext == ".bak" || ext == ".bin";

								if (isDisguiseCandidate && HasPeMagicBytes(cleanPath))
								{
									summary.Items.Add(new SystemInformerEvidenceItem
									{
										ProcessName = "csrss.exe",
										ProcessId = cs.Id,
										Category = SystemInformerCategory.Csrss,
										SourceMethod = "csrss -> ^(?:\\\\\\?\\\\)?[A-Za-z]:\\\\.+$ (Disguised PE)",
										DetectedStringOrPath = cleanPath,
										FileSizeOrWeight = FormatFileSize(cleanPath),
										TimeInfo = "0x" + addr.ToString("X"),
										Verdict = "Замаскированный PE-файл (" + (string.IsNullOrEmpty(ext) ? "без расширения" : ext) + ")",
										RuleViolation = "5.08",
										Description = "В памяти csrss.exe найден файл с поддельным расширением, содержащий PE-заголовок MZ!",
										FullDetails = "Путь: " + cleanPath + "\r\nРасширение: " + (string.IsNullOrEmpty(ext) ? "<отсутствует>" : ext) + "\r\nМагические байты: 0x4D 0x5A (MZ Executable).",
										Severity = CheckSeverity.Critical,
										IsViolation = true
									});
									return;
								}
							}

							if (includeInformational && infoCount < 20 && (isExe || isDll) && !isSystemDir)
							{
								infoCount++;
								summary.Items.Add(new SystemInformerEvidenceItem
								{
									ProcessName = "csrss.exe",
									ProcessId = cs.Id,
									Category = SystemInformerCategory.Csrss,
									SourceMethod = "csrss -> Path",
									DetectedStringOrPath = cleanPath,
									FileSizeOrWeight = exists ? FormatFileSize(cleanPath) : "Не найден",
									TimeInfo = "0x" + addr.ToString("X"),
									Verdict = "Штатный путь csrss",
									RuleViolation = "ЧИСТО",
									Description = "Зафиксированный путь в памяти csrss.exe.",
									FullDetails = "Путь: " + cleanPath,
									Severity = CheckSeverity.Info,
									IsViolation = false
								});
							}
						});
				}
				finally
				{
					CloseHandle(hProc);
				}
			}

			if (accessibleCount == 0 && deniedCount > 0)
			{
				summary.CsrssAccessDenied = true;
				if (includeInformational)
				{
					summary.Items.Add(new SystemInformerEvidenceItem
					{
						ProcessName = "csrss.exe",
						ProcessId = csrssProcs[0].Id,
						Category = SystemInformerCategory.Csrss,
						SourceMethod = "OpenProcess (PPL Protection)",
						DetectedStringOrPath = "Доступ ограничен (Kernel/PPL/Admin)",
						FileSizeOrWeight = "—",
						TimeInfo = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
						Verdict = "Защита csrss.exe (пропущено)",
						RuleViolation = "ИНФО",
						Description = "Служба csrss.exe защищена PPL/ядром (Vanguard/Faceit/права Admin). По мануалу доступ пропускается безопасно.",
						FullDetails = "Все экземпляры csrss.exe (" + csrssProcs.Length + " шт.) отклонили OpenProcess. По правилам безопасности дальнейшие манипуляции не выполняются.",
						Severity = CheckSeverity.Info,
						IsViolation = false
					});
				}
			}
		}

		#endregion

		#region Low-Level Fast Memory String Scanner

		private static void ScanProcessMemoryStrings(
			IntPtr hProc,
			long maxTotalBytesBudget,
			int maxMsBudget,
			bool includeImageRegions,
			int maxBytesPerRegion,
			Func<string, string, string, bool> chunkPrefilter,
			Action<string, ulong> onStringFound)
		{
			Stopwatch sw = Stopwatch.StartNew();
			ulong addr = 0x10000UL;
			ulong maxAddr = (IntPtr.Size == 8) ? 0x7FFFFFFFFFFFUL : 0x7FFFFFFFUL;
			long totalBytesRead = 0;

			byte[] chunkBuf = new byte[65536];
			char[] asciiLowerChars = new char[65536];
			char[] uni0LowerChars = new char[32768];
			char[] uni1LowerChars = new char[32768];
			char[] scratchChars = new char[2048];
			bool is64 = (IntPtr.Size == 8);

			while (addr < maxAddr && totalBytesRead < maxTotalBytesBudget && sw.ElapsedMilliseconds < maxMsBudget)
			{
				ulong baseAddress;
				ulong regionSize;
				uint state;
				uint protect;
				uint type;

				if (is64)
				{
					MEMORY_BASIC_INFORMATION64 mbi;
					if (VirtualQueryEx(hProc, (IntPtr)(long)addr, out mbi, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION64))) == 0)
						break;
					baseAddress = mbi.BaseAddress;
					regionSize = mbi.RegionSize;
					state = mbi.State;
					protect = mbi.Protect;
					type = mbi.Type;
				}
				else
				{
					MEMORY_BASIC_INFORMATION32 mbi32;
					if (VirtualQueryEx32(hProc, (IntPtr)(int)addr, out mbi32, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION32))) == 0)
						break;
					baseAddress = mbi32.BaseAddress;
					regionSize = mbi32.RegionSize;
					state = mbi32.State;
					protect = mbi32.Protect;
					type = mbi32.Type;
				}

				bool readableProtect = (protect & PAGE_GUARD) == 0 && (protect & PAGE_NOACCESS) == 0 &&
					(protect == PAGE_READWRITE || protect == PAGE_READONLY || protect == PAGE_WRITECOPY ||
					 protect == PAGE_EXECUTE_READ || protect == PAGE_EXECUTE_READWRITE || protect == PAGE_EXECUTE_WRITECOPY);

				bool typeAllowed = includeImageRegions || (type != MEM_IMAGE);

				if (state == MEM_COMMIT && readableProtect && typeAllowed && regionSize > 0)
				{
					ulong regionCap = (maxBytesPerRegion > 0) ? Math.Min(regionSize, (ulong)maxBytesPerRegion) : regionSize;
					ulong regOff = 0;
					while (regOff < regionCap && totalBytesRead < maxTotalBytesBudget && sw.ElapsedMilliseconds < maxMsBudget)
					{
						int toRead = (int)Math.Min(65536UL, regionCap - regOff);
						IntPtr bytesReadPtr;
						if (ReadProcessMemory(hProc, (IntPtr)(long)(baseAddress + regOff), chunkBuf, toRead, out bytesReadPtr))
						{
							int bytesRead = bytesReadPtr.ToInt32();
							if (bytesRead <= 0) break;
							totalBytesRead += bytesRead;

							bool shouldExtract = true;
							if (chunkPrefilter != null)
							{
								for (int i = 0; i < bytesRead; i++)
								{
									byte b = chunkBuf[i];
									asciiLowerChars[i] = (b >= (byte)'A' && b <= (byte)'Z') ? (char)(b + 32) : (char)b;
								}
								string aLow = new string(asciiLowerChars, 0, bytesRead);

								int u0Len = bytesRead / 2;
								for (int i = 0; i < u0Len; i++)
								{
									ushort ch = (ushort)(chunkBuf[i * 2] | (chunkBuf[i * 2 + 1] << 8));
									uni0LowerChars[i] = (ch >= 'A' && ch <= 'Z') ? (char)(ch + 32) : (char)ch;
								}
								string uLow0 = new string(uni0LowerChars, 0, u0Len);

								int u1Len = (bytesRead > 1) ? ((bytesRead - 1) / 2) : 0;
								for (int i = 0; i < u1Len; i++)
								{
									ushort ch = (ushort)(chunkBuf[1 + i * 2] | (chunkBuf[2 + i * 2] << 8));
									uni1LowerChars[i] = (ch >= 'A' && ch <= 'Z') ? (char)(ch + 32) : (char)ch;
								}
								string uLow1 = (u1Len > 0) ? new string(uni1LowerChars, 0, u1Len) : "";

								shouldExtract = chunkPrefilter(aLow, uLow0, uLow1);
							}

							if (shouldExtract)
							{
								ExtractAndEmitStrings(chunkBuf, bytesRead, baseAddress + regOff, scratchChars, onStringFound);
							}
							if (bytesRead <= 256 || toRead <= 256) break;
							regOff += (ulong)(bytesRead - 256);
						}
						else
						{
							break;
						}
					}
				}

				ulong nextAddr = baseAddress + regionSize;
				addr = (nextAddr > addr) ? nextAddr : (addr + 4096UL);
			}
		}

		private static void ExtractAndEmitStrings(byte[] buf, int len, ulong regionAddr, char[] scratch, Action<string, ulong> onStringFound)
		{
			// Pass 1: ASCII strings (min length 4)
			int runLen = 0;
			int runStart = 0;
			for (int i = 0; i < len; i++)
			{
				byte b = buf[i];
				if ((b >= 0x20 && b <= 0x7E) || b == 0x09)
				{
					if (runLen == 0) runStart = i;
					if (runLen < scratch.Length)
					{
						scratch[runLen] = (char)b;
					}
					runLen++;
				}
				else
				{
					if (runLen >= 4)
					{
						int emitLen = Math.Min(runLen, scratch.Length);
						onStringFound(new string(scratch, 0, emitLen), regionAddr + (ulong)runStart);
					}
					runLen = 0;
				}
			}
			if (runLen >= 4)
			{
				int emitLen = Math.Min(runLen, scratch.Length);
				onStringFound(new string(scratch, 0, emitLen), regionAddr + (ulong)runStart);
			}

			// Pass 2: UTF-16LE Unicode strings (min length 4, ASCII + Cyrillic 0x0400..0x04FF)
			for (int parity = 0; parity <= 1; parity++)
			{
				runLen = 0;
				runStart = parity;
				for (int i = parity; i + 1 < len; i += 2)
				{
					ushort ch = (ushort)(buf[i] | (buf[i + 1] << 8));
					bool printable = (ch >= 0x20 && ch <= 0x7E) || (ch >= 0x0400 && ch <= 0x04FF) || ch == 0x0009;
					if (printable)
					{
						if (runLen == 0) runStart = i;
						if (runLen < scratch.Length)
						{
							scratch[runLen] = (char)ch;
						}
						runLen++;
					}
					else
					{
						if (runLen >= 4)
						{
							int emitLen = Math.Min(runLen, scratch.Length);
							onStringFound(new string(scratch, 0, emitLen), regionAddr + (ulong)runStart);
						}
						runLen = 0;
					}
				}
				if (runLen >= 4)
				{
					int emitLen = Math.Min(runLen, scratch.Length);
					onStringFound(new string(scratch, 0, emitLen), regionAddr + (ulong)runStart);
				}
			}
		}

		#endregion

		#region Threat Evaluation & Guards (Zero False Positives)

		private static void EvaluatePathThreat(string rawPath, out bool isCheat, out bool isCleaner, out string matchedSignature)
		{
			isCheat = false;
			isCleaner = false;
			matchedSignature = "";

			if (string.IsNullOrEmpty(rawPath)) return;
			string lower = rawPath.Replace('/', '\\').ToLowerInvariant();

			// Строгие исключения для самого чекера, средств SS и системных директорий Windows
			if (lower.Contains("mccheatchecker") || lower.Contains("screenshare") || lower.Contains("sstool") ||
				lower.Contains("systeminformer") || lower.Contains("system informer") ||
				lower.Contains("processhacker") || lower.Contains("process hacker"))
			{
				return;
			}
			if (CheatDatabases.IsWhitelistedPath(lower)) return;

			string[] cheatNames = CheatDetector.CheatClientNames;
			for (int i = 0; i < cheatNames.Length; i++)
			{
				string cl = cheatNames[i];
				if (cl.Length >= 4 && CheatDetector.MatchesCheatName(lower, cl))
				{
					isCheat = true;
					matchedSignature = cl;
					return;
				}
			}

			string[] extraCheats = CheatDatabases.ExtraCheatClients;
			for (int i = 0; i < extraCheats.Length; i++)
			{
				string ec = extraCheats[i];
				if (ec.Length >= 4 && CheatDetector.MatchesCheatName(lower, ec))
				{
					isCheat = true;
					matchedSignature = ec;
					return;
				}
			}

			bool isSystem32 = lower.StartsWith(@"c:\windows\system32\") || lower.StartsWith(@"c:\windows\syswow64\");
			string[] cleaners = CheatDatabases.KnownCleanerSignatures;
			for (int i = 0; i < cleaners.Length; i++)
			{
				string cln = cleaners[i];
				if (cln.Length < 5) continue;
				if (cln == "systeminformer") continue;
				if (isSystem32 && (cln == "cleanmgr" || cln == "wevtutil")) continue;

				if (ContainsWord(lower, cln))
				{
					isCleaner = true;
					matchedSignature = cln;
					return;
				}
			}
		}

		private static string ExtractMatchedCheat(string lowerName, string lowerPath)
		{
			string combined = lowerName + " " + lowerPath;
			string[] cheats = CheatDetector.CheatClientNames;
			for (int i = 0; i < cheats.Length; i++)
			{
				if (cheats[i].Length >= 4 && CheatDetector.MatchesCheatName(combined, cheats[i])) return cheats[i];
			}
			string[] extras = CheatDatabases.ExtraCheatClients;
			for (int i = 0; i < extras.Length; i++)
			{
				if (extras[i].Length >= 4 && CheatDetector.MatchesCheatName(combined, extras[i])) return extras[i];
			}
			return "";
		}

		private static int FindPatternWithBoundary(string lowerText, CheatMemorySignature sig)
		{
			int start = 0;
			string pat = sig.Pattern;
			while (start <= lowerText.Length - pat.Length)
			{
				int idx = lowerText.IndexOf(pat, start, StringComparison.Ordinal);
				if (idx < 0) return -1;

				bool startOk = !sig.RequireWordStart || idx == 0 || !IsWordChar(lowerText[idx - 1]);
				int endPos = idx + pat.Length;
				bool endOk = !sig.RequireWordEnd || endPos >= lowerText.Length || !IsWordChar(lowerText[endPos]);

				if (startOk && endOk && (!sig.RequireCleanAsciiShort || IsCleanShortAsciiContext(lowerText, idx, sig)))
				{
					return idx;
				}
				start = idx + 1;
			}
			return -1;
		}

		private static bool IsCleanShortAsciiContext(string rawStr, int matchIdx, CheatMemorySignature sig)
		{
			if (string.IsNullOrEmpty(rawStr) || rawStr.Length > 160) return false;
			for (int i = 0; i < rawStr.Length; i++)
			{
				char ch = rawStr[i];
				if ((ch < 0x20 || ch > 0x7E) && ch != '\t')
				{
					return false;
				}
			}
			if (matchIdx > 0)
			{
				char prev = rawStr[matchIdx - 1];
				if (prev != ' ' && prev != '\t' && prev != '(' && prev != '[' && prev != '{' &&
					prev != '\'' && prev != '"' && prev != '/' && prev != '\\' && prev != ':' &&
					prev != ';' && prev != ',' && prev != '=')
				{
					return false;
				}
			}
			int endPos = matchIdx + sig.Pattern.Length;
			if (endPos < rawStr.Length)
			{
				char next = rawStr[endPos];
				if (next != ' ' && next != '\t' && next != ')' && next != ']' && next != '}' &&
					next != '\'' && next != '"' && next != '/' && next != '\\' && next != ':' &&
					next != ';' && next != ',')
				{
					return false;
				}
			}
			return true;
		}

		private static bool ContainsWord(string textLower, string token)
		{
			int idx = 0;
			while ((idx = textLower.IndexOf(token, idx, StringComparison.Ordinal)) >= 0)
			{
				bool startOk = idx == 0 || !IsWordChar(textLower[idx - 1]);
				int end = idx + token.Length;
				bool endOk = end >= textLower.Length || !IsWordChar(textLower[end]);
				if (startOk && endOk) return true;
				idx = idx + 1;
			}
			return false;
		}

		private static bool IsWordChar(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9');
		}

		private static string AsciiToLowerFast(string s)
		{
			char[] arr = s.ToCharArray();
			bool changed = false;
			for (int i = 0; i < arr.Length; i++)
			{
				char c = arr[i];
				if (c >= 'A' && c <= 'Z')
				{
					arr[i] = (char)(c + 32);
					changed = true;
				}
			}
			return changed ? new string(arr) : s;
		}

		#endregion

		#region General Helpers

		private static void TryEnableDebugPrivilege()
		{
			try
			{
				IntPtr tokenHandle;
				if (OpenProcessToken(Process.GetCurrentProcess().Handle, TOKEN_ADJUST_PRIVILEGES | TOKEN_QUERY, out tokenHandle))
				{
					try
					{
						LUID luid;
						if (LookupPrivilegeValue(null, "SeDebugPrivilege", out luid))
						{
							TOKEN_PRIVILEGES tp = new TOKEN_PRIVILEGES();
							tp.PrivilegeCount = 1;
							tp.Luid = luid;
							tp.Attributes = SE_PRIVILEGE_ENABLED;
							AdjustTokenPrivileges(tokenHandle, false, ref tp, 0, IntPtr.Zero, IntPtr.Zero);
						}
					}
					finally
					{
						CloseHandle(tokenHandle);
					}
				}
			}
			catch { }
		}

		private static int ResolveServicePid(string serviceName)
		{
			try
			{
				using (ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT ProcessId, State FROM Win32_Service WHERE Name = '" + serviceName + "'"))
				{
					foreach (ManagementObject mo in searcher.Get())
					{
						string state = (mo["State"] ?? "").ToString();
						if (state.Equals("Running", StringComparison.OrdinalIgnoreCase))
						{
							object pidObj = mo["ProcessId"];
							if (pidObj != null)
							{
								int pid = Convert.ToInt32(pidObj);
								if (pid > 0) return pid;
							}
						}
					}
				}
			}
			catch { }
			return 0;
		}

		private static bool IsDigitallySigned(string path)
		{
			if (string.IsNullOrEmpty(path) || !FileExistsSafe(path)) return false;
			try
			{
				X509Certificate cert = X509Certificate.CreateFromSignedFile(path);
				return cert != null;
			}
			catch
			{
				return false;
			}
		}

		private static bool IsKnownBenignOverlayOrGpu(string lowerName, string lowerPath)
		{
			return lowerName.Contains("discordhook") || lowerName.Contains("gameoverlayrenderer") ||
				lowerName.Contains("rtsshooks") || lowerName.Contains("graphics-hook") ||
				lowerName.Contains("nvspcap") || lowerName.Contains("nvcuda") ||
				lowerName.Contains("aticfx") || lowerName.Contains("amdxx") ||
				lowerPath.Contains(@"\obs-studio\") || lowerPath.Contains(@"\rivatuner\") ||
				lowerPath.Contains(@"\discord\") || lowerPath.Contains(@"\steam\") ||
				lowerPath.Contains(@"\nvidia corporation\") || lowerPath.Contains(@"\overwolf\");
		}

		private static bool IsKnownSafeAppDirectory(string lowerPath)
		{
			return lowerPath.StartsWith(@"c:\program files\") ||
				lowerPath.StartsWith(@"c:\program files (x86)\") ||
				lowerPath.Contains(@"\windowsapps\") ||
				lowerPath.Contains(@"\appdata\local\programs\") ||
				lowerPath.Contains(@"\appdata\local\discord\") ||
				lowerPath.Contains(@"\appdata\roaming\evovis\");
		}

		private static bool HasPeMagicBytes(string filePath)
		{
			try
			{
				using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					if (fs.Length < 64) return false;
					byte[] hdr = new byte[2];
					if (fs.Read(hdr, 0, 2) == 2)
					{
						return hdr[0] == 0x4D && hdr[1] == 0x5A; // 'MZ'
					}
				}
			}
			catch { }
			return false;
		}

		private static string CleanWin32Path(string path)
		{
			if (string.IsNullOrEmpty(path)) return "";
			string res = path.Trim();
			if (res.StartsWith(@"\\?\")) res = res.Substring(4);
			if (res.StartsWith(@"\??\")) res = res.Substring(4);
			return res;
		}

		private static string DecodeFileUriToWindowsPath(string uriRaw)
		{
			try
			{
				string s = uriRaw;
				if (s.StartsWith("file:///", StringComparison.OrdinalIgnoreCase))
				{
					s = s.Substring(8);
				}
				s = Uri.UnescapeDataString(s);
				s = s.Replace('/', '\\').Trim();
				if (s.Length >= 3 && s[1] == ':') return s;
			}
			catch { }
			return "";
		}

		private static string ExtractFirstWindowsPath(string text)
		{
			Match m = WindowsPathExtractRegex.Match(text);
			return m.Success ? CleanWin32Path(m.Value) : "";
		}

		private static bool FileExistsSafe(string path)
		{
			try { return !string.IsNullOrEmpty(path) && File.Exists(path); }
			catch { return false; }
		}

		private static string FormatFileSize(string path)
		{
			try
			{
				FileInfo fi = new FileInfo(path);
				if (fi.Length >= 1048576L) return string.Format("{0:F2} МБ", fi.Length / 1048576.0);
				return string.Format("{0:F1} КБ", fi.Length / 1024.0);
			}
			catch { return "—"; }
		}

		private static string Truncate(string s, int maxLen)
		{
			if (string.IsNullOrEmpty(s)) return "";
			return (s.Length <= maxLen) ? s : (s.Substring(0, maxLen) + "...");
		}

		#endregion
	}
}
