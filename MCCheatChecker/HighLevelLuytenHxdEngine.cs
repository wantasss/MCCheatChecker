using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.IO.Compression;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public enum HighLevelCheckCategory
	{
		All,
		LuytenMods,
		HxdDllHex,
		BamUnsigned,
		SystemDlc,
		EventLogsAndRegistry,
		Violations
	}

	public class HighLevelEvidenceItem
	{
		public string SourceModule { get; set; }
		public HighLevelCheckCategory Category { get; set; }
		public string TargetName { get; set; }
		public string CoreOrType { get; set; }
		public string MatchedSignatures { get; set; }
		public string TimeOrSize { get; set; }
		public string Verdict { get; set; }
		public string RuleViolation { get; set; }
		public string Description { get; set; }
		public string FullDetails { get; set; }
		public CheckSeverity Severity { get; set; }
		public bool IsViolation { get; set; }
	}

	public class HighLevelScanSummary
	{
		public List<HighLevelEvidenceItem> Items { get; set; }
		public int TotalScanned { get; set; }
		public int LuytenModHits { get; set; }
		public int HxdDllHits { get; set; }
		public int BamUnsignedHits { get; set; }
		public int SystemDlcHits { get; set; }
		public int EventLogHits { get; set; }
		public int ViolationCount { get; set; }
		public string ScanDurationMs { get; set; }

		public HighLevelScanSummary()
		{
			Items = new List<HighLevelEvidenceItem>();
			ScanDurationMs = "0";
		}
	}

	public static class HighLevelLuytenHxdEngine
	{
		#region Win32 Interop (Volume Mapping Only)

		[DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
		private static extern uint QueryDosDevice(string lpDeviceName, StringBuilder lpTargetPath, int ucchMax);

		#endregion

		#region Signatures & Rules

		private static readonly string[] ForgeHitboxPrimary = new string[]
		{
			"AxisAlignedBB", "getEntitiesWithinAABB", "EntitySize",
			"setEntityBoundingBox", "func_174826_a", "func_70085_c"
		};

		private static readonly string[] ForgeHitboxSecondary = new string[]
		{
			"getWidth", "width", "height"
		};

		private static readonly string[] ForgeReachPatterns = new string[]
		{
			"getReachDistance", "attackEntity", "mc.playerController",
			"func_78757_d", "ClickMouse", "leftClick"
		};

		private static readonly string[] ForgeMovementPrimary = new string[]
		{
			"EntityLivingBase", "prevPosX", "motionX", "moveStrafing"
		};

		private static readonly string[] ForgeMovementSecondary = new string[]
		{
			"posX", "onUpdate", "isOnGround"
		};

		private static readonly string[] LabyModLiteralPatterns = new string[]
		{
			"dci", ".cD()", ".cc().b", ".cc().e", ".cH()", ".cc()"
		};

		private static readonly string[] LabyModCpTokens = new string[]
		{
			"dci", "cD", "cc", "cH"
		};

		private static readonly string[] FabricCombatPrimary = new string[]
		{
			"method_5857", "setBoundingBox",
			"method_2920", "getReachDistance",
			"method_2918", "attackEntity",
			"method_2904",
			"class_636", "ClientPlayerInteractionManager"
		};

		private static readonly string[] FabricContextTokens = new string[]
		{
			"net.minecraft.class_238", "Box",
			"method_5829", "getBoundingBox",
			"class_1297",
			"class_1657",
			"method_23317"
		};

		private static readonly string[] ExplicitMaliciousJarPackages = new string[]
		{
			"/wurst/", "/meteor/", "/expensive/", "/celestial/", "/nursultan/",
			"/liquidbounce/", "/thunderhack/", "/slinky/", "/seedcracker/",
			"/doomsday/", "/vape/", "/catlavan/", "/deadcode/", "/delta/",
			"/flint/", "/rockstar/", "/boberware/", "/tenacity/", "/novoline/",
			"/rise/", "/drip/", "/wild/", "/minced/", "/wexside/", "/hitbox/",
			"/killaura/", "/silentaim/", "/triggerbot/", "/systemdlc/", "/sysdlc/"
		};

		private static readonly string[] StandardLegitimateModIds = new string[]
		{
			"sodium", "lithium", "phosphor", "starlight", "iris", "optifine",
			"journeymap", "xaerominimap", "xaeroworldmap", "xaeros_minimap",
			"replaymod", "fabric-api", "fabricloader", "voicechat", "plasmovoice",
			"modmenu", "cloth-config", "ferritecore", "immediatelyfast",
			"entityculling", "dynamic-fps", "appleskin", "jade", "jei", "rei",
			"emi", "architectury", "geckolib", "indium", "reeses-sodium-options",
			"sodium-extra", "zoomify", "ok-zoomer", "lambdynamiclights", "continuity"
		};

		private static readonly byte[][] EncryptedHxdCheats = new byte[][]
		{
			new byte[] { 230, 67, 155, 178, 25, 181, 7, 115, 228, 32, 215, 63, 164 },
			new byte[] { 230, 67, 155, 178, 25, 181, 7, 115, 228, 32, 215 },
			new byte[] { 192, 94, 134, 131, 55, 172, 0, 112, 227, 43, 212, 63, 137, 87 },
			new byte[] { 226, 85, 134, 168, 44, 160, 62, 120, 235, 60, 214, 15 },
			new byte[] { 227, 84, 157, 172, 43, 189, 15, 109 },
			new byte[] { 244, 66, 129, 181, 61, 180, 42, 88, 201 },
			new byte[] { 239, 82, 134, 163, 55, 161 },
			new byte[] { 245, 94, 147, 162, 48 }
		};

		private static readonly byte[][] EncryptedHxdApis = new byte[][]
		{
			new byte[] { 245, 94, 147, 165, 8, 171, 1, 119, 239, 54, 192, 48, 131, 66, 254, 190, 222 },
			new byte[] { 192, 94, 134, 131, 33, 173, 11, 103 },
			new byte[] { 205, 85, 155, 239, 60, 181, 2 },
			new byte[] { 205, 77, 159, 239, 60, 181, 2 },
			new byte[] { 228, 73, 151, 160, 44, 188, 58, 123, 229, 41, 219, 24, 138, 95, 162, 254, 244, 85, 147, 177, 43, 177, 1, 96 },
			new byte[] { 205, 90, 132, 160, 47, 247, 11, 108, 239 },
			new byte[] { 224, 94, 134, 149, 48, 171, 11, 117, 238, 6, 220, 19, 146, 74, 233, 184 },
			new byte[] { 244, 94, 134, 149, 48, 171, 11, 117, 238, 6, 220, 19, 146, 74, 233, 184 },
			new byte[] { 241, 82, 128, 181, 45, 184, 2, 68, 248, 42, 199, 24, 133, 91 },
			new byte[] { 224, 94, 134, 145, 42, 182, 13, 85, 238, 33, 193, 24, 149, 92 },
			new byte[] { 224, 94, 134, 128, 43, 160, 0, 119, 193, 32, 202, 46, 146, 78, 229, 169 }
		};

		private static readonly byte[][] EncryptedWipeCmds = new byte[][]
		{
			new byte[] { 193, 72, 135, 181, 49, 181, 78, 97, 249, 43, 147, 25, 131, 67, 244, 184, 194, 81, 157, 180, 42, 183, 15, 120 },
			new byte[] { 208, 94, 132, 181, 45, 173, 7, 120, 170, 38, 223 },
			new byte[] { 209, 72, 129, 160, 60, 180, 7, 122, 170, 33, 214, 17, 131, 91, 244 }
		};

		private static readonly byte[][] EncryptedInjectors = new byte[][]
		{
			new byte[] { 223, 94, 156, 174, 43 },
			new byte[] { 194, 67, 134, 179, 61, 180, 11, 125, 228, 47, 214, 30, 146, 64, 227 },
			new byte[] { 197, 84, 157, 181, 50, 188, 13, 96 },
			new byte[] { 203, 82, 131, 180, 49, 189, 2, 123, 235, 33, 214, 15 },
			new byte[] { 204, 95, 159, 160, 40, 169, 11, 102 },
			new byte[] { 194, 67, 158, 174, 57, 189, 11, 102 }
		};

		private static readonly byte[] EncryptedMagicLabel = new byte[] { 228, 122, 210, 135, 29, 249, 44, 85, 170, 7, 246 };

		private static readonly string[] HxdCheatMarkers = StringShield.DecryptAll(EncryptedHxdCheats);
		private static readonly string[] HxdApiMarkers = StringShield.DecryptAll(EncryptedHxdApis);
		private static readonly string[] WipingCliMarkers = StringShield.DecryptAll(EncryptedWipeCmds);
		private static readonly string[] BamInjectorMarkers = StringShield.DecryptAll(EncryptedInjectors);
		private static readonly string JvmClassMagicLabel = StringShield.Decrypt(EncryptedMagicLabel);

		private static readonly string[] SystemDlcKeywords = new string[]
		{
			"fun.systemdlc", "ru.systemdlc", "system.dlc", "systemdlc", "sysdlc"
		};

		private static readonly string[] SafeTempInstallerPrefixes = new string[]
		{
			"setup", "install", "unins000", "uninstall", "vc_redist", "vcredist",
			"dotnet", "ndp4", "updater", "update", "chrome_installer", "msedge",
			"webview2", "directx", "dxsetup"
		};

		private static readonly Regex ModIdJsonRegex = new Regex(@"""id""\s*:\s*""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex ModNameJsonRegex = new Regex(@"""name""\s*:\s*""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex TomlModIdRegex = new Regex(@"modId\s*=\s*""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex TomlDisplayNameRegex = new Regex(@"displayName\s*=\s*""([^""]+)""", RegexOptions.Compiled | RegexOptions.IgnoreCase);
		private static readonly Regex TempGuidFolderRegex = new Regex(@"\\temp\\(\{[0-9a-f\-]{8,}\}|is-[a-z0-9]{4,}|vs[a-z0-9]{2,}|_mei\d+|dotnet_|chocolatey)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

		#endregion

		#region Public Entry Points

		public static HighLevelScanSummary RunScan(bool includeInformational = true)
		{
			return RunScanInternal(includeInformational, null, null, null);
		}

		public static HighLevelScanSummary RunScanCustom(
			bool includeInformational,
			string extraModsDir,
			string extraDllPath,
			string extraSystemDlcRoot)
		{
			return RunScanInternal(includeInformational, extraModsDir, extraDllPath, extraSystemDlcRoot);
		}

		public static List<CheckResult> RunDetectorChecks()
		{
			List<CheckResult> results = new List<CheckResult>();
			try
			{
				HighLevelScanSummary summary = RunScan(true);
				List<HighLevelEvidenceItem> violations = new List<HighLevelEvidenceItem>();
				for (int i = 0; i < summary.Items.Count; i++)
				{
					if (summary.Items[i].IsViolation)
					{
						violations.Add(summary.Items[i]);
					}
				}

				if (violations.Count > 0)
				{
					CheckSeverity topSev = CheckSeverity.Warning;
					List<string> lines = new List<string>();
					for (int i = 0; i < violations.Count; i++)
					{
						HighLevelEvidenceItem v = violations[i];
						if (v.Severity == CheckSeverity.Critical) topSev = CheckSeverity.Critical;
						if (lines.Count < 45)
						{
							lines.Add(string.Format("[{0} | {1} | {2}] {3} -> {4} ({5})",
								v.RuleViolation,
								v.SourceModule,
								v.TargetName,
								v.Verdict,
								v.MatchedSignatures,
								v.Description));
						}
					}

					results.Add(new CheckResult
					{
						Category = "Форензика Luyten / HxD / BAM",
						Name = "Luyten JAR / HxD DLL / BAM / EventLog / SystemDLC форензика",
						Detected = true,
						Severity = topSev,
						Detail = string.Format(
							"Выявлены нарушения высокого уровня: {0} (LuytenMods={1}, HxD_DLL={2}, BAM={3}, SystemDLC={4}, EventLog/Reg={5})",
							summary.ViolationCount,
							summary.LuytenModHits,
							summary.HxdDllHits,
							summary.BamUnsignedHits,
							summary.SystemDlcHits,
							summary.EventLogHits),
						Evidence = string.Join("\r\n", lines.ToArray())
					});
				}
				else
				{
					results.Add(new CheckResult
					{
						Category = "Форензика Luyten / HxD / BAM",
						Name = "Luyten JAR / HxD DLL / BAM / EventLog / SystemDLC форензика",
						Detected = false,
						Severity = CheckSeverity.Info,
						Detail = string.Format(
							"Высокоуровневый аудит (Luyten REGEX MODS, HxD DLL, BAM, EventLogs/Registry, SystemDLC) завершён за {0} мс: проверено {1} объектов, нарушений не обнаружено.",
							summary.ScanDurationMs, summary.TotalScanned),
						Evidence = "Проверены моды (.minecraft / Prism / Feather / Modrinth / CurseForge / Lunar / TLauncher / EvoVis), DLL в памяти/MRU, BAM, Журналы событий, Реестр и следы SystemDLC."
					});
				}
			}
			catch (Exception ex)
			{
				results.Add(new CheckResult
				{
					Category = "Форензика Luyten / HxD / BAM",
					Name = "Luyten JAR / HxD DLL / BAM / EventLog / SystemDLC форензика",
					Detected = false,
					Severity = CheckSeverity.Info,
					Detail = "Аудит завершён с исключением: " + ex.Message,
					Evidence = ""
				});
			}
			return results;
		}

		public static HighLevelEvidenceItem InspectModJar(string jarPath)
		{
			return AnalyzeModJarFile(jarPath);
		}

		public static HighLevelEvidenceItem InspectDllHex(string dllPath, string sourceLabel)
		{
			return AnalyzeDllFileHxD(dllPath, string.IsNullOrEmpty(sourceLabel) ? "HxD DLL Inspector" : sourceLabel);
		}

		public static List<HighLevelEvidenceItem> InspectSystemDlcFolder(string dirPath)
		{
			List<HighLevelEvidenceItem> items = new List<HighLevelEvidenceItem>();
			int dummyScanned = 0;
			ScanSystemDlcSingleDirectory(dirPath, items, ref dummyScanned, true);
			return items;
		}

		#endregion

		#region Orchestration

		private static HighLevelScanSummary RunScanInternal(
			bool includeInformational,
			string extraModsDir,
			string extraDllPath,
			string extraSystemDlcRoot)
		{
			Stopwatch sw = Stopwatch.StartNew();
			HighLevelScanSummary summary = new HighLevelScanSummary();
			int totalScanned = 0;

			try
			{
				ScanLuytenModJars(summary.Items, includeInformational, extraModsDir, ref totalScanned);
			}
			catch { }

			try
			{
				ScanHxdDlls(summary.Items, includeInformational, extraDllPath, ref totalScanned);
			}
			catch { }

			try
			{
				if (PlatformHelper.IsWindows)
				{
					ScanBamNotSignedOnly(summary.Items, includeInformational, ref totalScanned);
				}
			}
			catch { }

			try
			{
				if (PlatformHelper.IsWindows)
				{
					ScanEventLogs(summary.Items, includeInformational, ref totalScanned);
					ScanRegistryForensics(summary.Items, includeInformational, ref totalScanned);
				}
			}
			catch { }

			try
			{
				ScanSystemDlcDeep(summary.Items, includeInformational, extraSystemDlcRoot, ref totalScanned);
			}
			catch { }

			FinalizeSummary(summary, totalScanned, sw);
			return summary;
		}

		private static void FinalizeSummary(HighLevelScanSummary summary, int totalScanned, Stopwatch sw)
		{
			sw.Stop();
			summary.ScanDurationMs = sw.ElapsedMilliseconds.ToString();
			summary.TotalScanned = Math.Max(totalScanned, summary.Items.Count);

			int luyten = 0;
			int hxd = 0;
			int bam = 0;
			int sysdlc = 0;
			int evReg = 0;
			int violations = 0;

			for (int i = 0; i < summary.Items.Count; i++)
			{
				HighLevelEvidenceItem item = summary.Items[i];
				if (!item.IsViolation) continue;

				violations++;
				if (item.Category == HighLevelCheckCategory.LuytenMods) luyten++;
				else if (item.Category == HighLevelCheckCategory.HxdDllHex) hxd++;
				else if (item.Category == HighLevelCheckCategory.BamUnsigned) bam++;
				else if (item.Category == HighLevelCheckCategory.SystemDlc) sysdlc++;
				else if (item.Category == HighLevelCheckCategory.EventLogsAndRegistry) evReg++;
			}

			summary.LuytenModHits = luyten;
			summary.HxdDllHits = hxd;
			summary.BamUnsignedHits = bam;
			summary.SystemDlcHits = sysdlc;
			summary.EventLogHits = evReg;
			summary.ViolationCount = violations;
		}

		#endregion

		#region 1. Luyten JAR Core & REGEX MODS Analyzer

		private static void ScanLuytenModJars(
			List<HighLevelEvidenceItem> items,
			bool includeInformational,
			string extraModsDir,
			ref int totalScanned)
		{
			List<string> jarPaths = DiscoverAllModJarPaths(extraModsDir);
			int legitAdded = 0;

			for (int i = 0; i < jarPaths.Count; i++)
			{
				string jarPath = jarPaths[i];
				totalScanned++;
				HighLevelEvidenceItem item = AnalyzeModJarFile(jarPath);
				if (item == null) continue;

				if (item.IsViolation)
				{
					items.Add(item);
				}
				else if (includeInformational && legitAdded < 60)
				{
					items.Add(item);
					legitAdded++;
				}
			}

			if (includeInformational && jarPaths.Count == 0)
			{
				items.Add(new HighLevelEvidenceItem
				{
					SourceModule = "Luyten JAR / REGEX MODS",
					Category = HighLevelCheckCategory.LuytenMods,
					TargetName = "Моды не найдены",
					CoreOrType = "—",
					MatchedSignatures = "—",
					TimeOrSize = "0 файлов",
					Verdict = "Чисто (папки mods пусты)",
					RuleViolation = "ИНФО",
					Description = "В известных лаунчерах Minecraft (.minecraft, EvoVis, Prism, Feather, Modrinth, CurseForge, Lunar, TLauncher) .jar моды не обнаружены.",
					FullDetails = "Сканирование REGEX MODS выполнено без нарушений.",
					Severity = CheckSeverity.Info,
					IsViolation = false
				});
			}
		}

		private static List<string> DiscoverAllModJarPaths(string extraModsDir)
		{
			HashSet<string> seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> result = new List<string>();
			List<string> modDirs = new List<string>();

			if (!string.IsNullOrEmpty(extraModsDir) && Directory.Exists(extraModsDir))
			{
				modDirs.Add(extraModsDir);
			}

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			AddModsDirAndVersionSubdirs(Path.Combine(appData, @".minecraft\mods"), modDirs);
			AddModsDirAndVersionSubdirs(Path.Combine(appData, @"EvoVis\instance\mc\mods"), modDirs);
			AddModsDirAndVersionSubdirs(Path.Combine(appData, @".feather\mods"), modDirs);
			AddModsDirAndVersionSubdirs(Path.Combine(appData, @".tlauncher\legacy\Minecraft\game\mods"), modDirs);

			TryAddGlobModsDirs(Path.Combine(appData, @"PrismLauncher\instances"), new string[] { @"minecraft\mods", @".minecraft\mods" }, modDirs);
			TryAddGlobModsDirs(Path.Combine(appData, @"MultiMC\instances"), new string[] { @"minecraft\mods", @".minecraft\mods" }, modDirs);
			TryAddGlobModsDirs(Path.Combine(appData, @"ModrinthApp\profiles"), new string[] { "mods" }, modDirs);
			TryAddGlobModsDirs(Path.Combine(userProfile, @"curseforge\minecraft\Instances"), new string[] { "mods" }, modDirs);
			TryAddGlobModsDirs(Path.Combine(userProfile, @".lunarclient\offline"), new string[] { "mods" }, modDirs);
			TryAddGlobModsDirs(Path.Combine(appData, @".tlauncher\mcl\Minecraft\game\versions"), new string[] { "mods" }, modDirs);
			TryAddGlobModsDirs(Path.Combine(localAppData, @"Packages\Microsoft.4297127D64EC6_8wekyb3d8bbwe\LocalCache\Local"), new string[] { "mods" }, modDirs);

			for (int d = 0; d < modDirs.Count; d++)
			{
				string dir = modDirs[d];
				try
				{
					if (!Directory.Exists(dir)) continue;
					string[] jars = Directory.GetFiles(dir, "*.jar", SearchOption.TopDirectoryOnly);
					for (int j = 0; j < jars.Length; j++)
					{
						string jp = jars[j];
						if (seen.Add(jp))
						{
							result.Add(jp);
						}
					}
				}
				catch { }
			}

			return result;
		}

		private static void AddModsDirAndVersionSubdirs(string baseModsDir, List<string> targetList)
		{
			try
			{
				if (string.IsNullOrEmpty(baseModsDir) || !Directory.Exists(baseModsDir)) return;
				targetList.Add(baseModsDir);
				string[] subDirs = Directory.GetDirectories(baseModsDir);
				for (int i = 0; i < subDirs.Length; i++)
				{
					targetList.Add(subDirs[i]);
				}
			}
			catch { }
		}

		private static void TryAddGlobModsDirs(string instancesRoot, string[] relativeModSubpaths, List<string> targetList)
		{
			try
			{
				if (!Directory.Exists(instancesRoot)) return;
				string[] dirs = Directory.GetDirectories(instancesRoot);
				for (int i = 0; i < dirs.Length; i++)
				{
					for (int r = 0; r < relativeModSubpaths.Length; r++)
					{
						string candidate = Path.Combine(dirs[i], relativeModSubpaths[r]);
						AddModsDirAndVersionSubdirs(candidate, targetList);
					}
				}
			}
			catch { }
		}

		private static HighLevelEvidenceItem AnalyzeModJarFile(string jarPath)
		{
			if (string.IsNullOrEmpty(jarPath) || !File.Exists(jarPath)) return null;

			string fileName = Path.GetFileName(jarPath);
			string sizeText = FormatFileSize(jarPath);
			string modId = "";
			string modName = "";
			string modCore = "Unknown / Java";
			bool hasFabricMeta = false;
			bool hasForgeMeta = false;
			bool hasLabyMeta = false;
			bool hasJavaAgentManifest = false;

			List<string> explicitCheatEvidence = new List<string>();
			List<string> systemDlcEvidence = new List<string>();
			string bestTemplateClass = "";
			string bestTemplateKind = "";
			List<string> bestTemplateHits = new List<string>();
			HashSet<string> jarWideFabricPrimary = new HashSet<string>(StringComparer.Ordinal);
			HashSet<string> jarWideFabricContext = new HashSet<string>(StringComparer.Ordinal);
			HashSet<string> jarWideForgeHitboxPrimary = new HashSet<string>(StringComparer.Ordinal);
			HashSet<string> jarWideForgeHitboxSecondary = new HashSet<string>(StringComparer.Ordinal);
			HashSet<string> jarWideForgeReach = new HashSet<string>(StringComparer.Ordinal);
			HashSet<string> jarWideForgeMovePrimary = new HashSet<string>(StringComparer.Ordinal);
			HashSet<string> jarWideForgeMoveSecondary = new HashSet<string>(StringComparer.Ordinal);
			HashSet<string> jarWideLabyHits = new HashSet<string>(StringComparer.Ordinal);

			try
			{
				using (ZipArchive zip = ZipFile.OpenRead(jarPath))
				{
					foreach (ZipArchiveEntry entry in zip.Entries)
					{
						string entryPath = entry.FullName ?? "";
						string entryLower = entryPath.ToLowerInvariant();

						if (entryLower == "fabric.mod.json")
						{
							hasFabricMeta = true;
							string json = ReadZipEntryText(entry, 256 * 1024);
							Match mId = ModIdJsonRegex.Match(json);
							if (mId.Success) modId = mId.Groups[1].Value.Trim();
							Match mName = ModNameJsonRegex.Match(json);
							if (mName.Success) modName = mName.Groups[1].Value.Trim();
							string jsonLow = json.ToLowerInvariant();
							foreach (string sd in SystemDlcKeywords)
							{
								if (ContainsWordToken(jsonLow, sd)) systemDlcEvidence.Add("fabric.mod.json:" + sd);
							}
						}
						else if (entryLower == "meta-inf/mods.toml" || entryLower == "meta-inf/neoforge.mods.toml")
						{
							hasForgeMeta = true;
							string toml = ReadZipEntryText(entry, 256 * 1024);
							Match mId = TomlModIdRegex.Match(toml);
							if (mId.Success) modId = mId.Groups[1].Value.Trim();
							Match mName = TomlDisplayNameRegex.Match(toml);
							if (mName.Success) modName = mName.Groups[1].Value.Trim();
							if (toml.IndexOf("labymod", StringComparison.OrdinalIgnoreCase) >= 0) hasLabyMeta = true;
						}
						else if (entryLower == "mcmod.info")
						{
							hasForgeMeta = true;
							string info = ReadZipEntryText(entry, 256 * 1024);
							if (info.IndexOf("labymod", StringComparison.OrdinalIgnoreCase) >= 0) hasLabyMeta = true;
							Match mName = ModNameJsonRegex.Match(info);
							if (string.IsNullOrEmpty(modName) && mName.Success) modName = mName.Groups[1].Value.Trim();
						}
						else if (entryLower == "addon.json" || entryLower.StartsWith("net/labymod/"))
						{
							hasLabyMeta = true;
						}
						else if (entryLower == "meta-inf/manifest.mf")
						{
							string mf = ReadZipEntryText(entry, 64 * 1024).ToLowerInvariant();
							if (mf.Contains("premain-class") || mf.Contains("agent-class") || mf.Contains("can-redefine-classes: true"))
							{
								hasJavaAgentManifest = true;
							}
						}

						if (entryLower.Contains("/labymod/")) hasLabyMeta = true;

						foreach (string pkg in ExplicitMaliciousJarPackages)
						{
							if (entryLower.Contains(pkg))
							{
								if (pkg.Contains("systemdlc") || pkg.Contains("sysdlc"))
								{
									systemDlcEvidence.Add(entryPath);
								}
								else
								{
									explicitCheatEvidence.Add("Пакет чита: " + entryPath);
								}
							}
						}

						if (entryLower.EndsWith(".mixins.json") || entryLower.EndsWith(".mixin.json"))
						{
							string mxText = ReadZipEntryText(entry, 128 * 1024);
							string mxLow = mxText.ToLowerInvariant();
							foreach (string sd in SystemDlcKeywords)
							{
								if (ContainsWordToken(mxLow, sd)) systemDlcEvidence.Add("Mixin:" + sd);
							}
						}
					}

					if (hasLabyMeta) modCore = "LabyMod";
					else if (hasFabricMeta) modCore = "Fabric";
					else if (hasForgeMeta) modCore = "Forge";

					bool isCpvpWhite = ServerRulesProfileManager.IsCpvpWhitelisted(fileName) ||
						(!string.IsNullOrEmpty(modId) && ServerRulesProfileManager.IsCpvpWhitelisted(modId)) ||
						CheatDatabases.IsAllowedCpvpOptimizer(fileName) ||
						(!string.IsNullOrEmpty(modId) && CheatDatabases.IsAllowedCpvpOptimizer(modId)) ||
						(!string.IsNullOrEmpty(modName) && CheatDatabases.IsAllowedCpvpOptimizer(modName));

					bool isStandardLegitMod = IsStandardLegitMod(fileName, modId);

					// Pass 2: Extract Constant Pool & source strings from .class entries
					int classEntriesInspected = 0;
					foreach (ZipArchiveEntry entry in zip.Entries)
					{
						string entryPath = entry.FullName ?? "";
						string entryLower = entryPath.ToLowerInvariant();
						if (!entryLower.EndsWith(".class") && !entryLower.EndsWith(".java")) continue;
						if (entry.Length <= 0 || entry.Length > 2 * 1024 * 1024) continue;
						classEntriesInspected++;
						if (classEntriesInspected > 2500) break;

						byte[] bytes = ReadZipEntryBytes(entry, 1024 * 1024);
						if (bytes == null || bytes.Length == 0) continue;

						List<string> cpStrings = ExtractClassConstantPoolAndText(bytes);
						if (cpStrings.Count == 0) continue;

						if (modCore == "Unknown / Java")
						{
							for (int s = 0; s < cpStrings.Count; s++)
							{
								string tok = cpStrings[s];
								if (tok.Contains("net/labymod/") || tok.Contains("net.labymod.")) { hasLabyMeta = true; modCore = "LabyMod"; break; }
								if (tok.Contains("net/minecraftforge/") || tok.Contains("cpw/mods/fml/")) { modCore = "Forge"; break; }
								if (tok.Contains("net/fabricmc/") || tok.Contains("net/minecraft/class_")) { modCore = "Fabric"; break; }
							}
						}

						// Explicit SystemDLC inside class
						for (int s = 0; s < cpStrings.Count; s++)
						{
							string tokLow = cpStrings[s].ToLowerInvariant();
							for (int k = 0; k < SystemDlcKeywords.Length; k++)
							{
								if (ContainsWordToken(tokLow, SystemDlcKeywords[k]))
								{
									systemDlcEvidence.Add(entryPath + " -> " + SystemDlcKeywords[k]);
									break;
								}
							}
						}

						if (isCpvpWhite) continue;

						// Evaluate exact REGEX MODS templates per class
						HashSet<string> clsForgeHitboxP = new HashSet<string>(StringComparer.Ordinal);
						HashSet<string> clsForgeHitboxS = new HashSet<string>(StringComparer.Ordinal);
						HashSet<string> clsForgeReach = new HashSet<string>(StringComparer.Ordinal);
						HashSet<string> clsForgeMoveP = new HashSet<string>(StringComparer.Ordinal);
						HashSet<string> clsForgeMoveS = new HashSet<string>(StringComparer.Ordinal);
						HashSet<string> clsLaby = new HashSet<string>(StringComparer.Ordinal);
						HashSet<string> clsFabricP = new HashSet<string>(StringComparer.Ordinal);
						HashSet<string> clsFabricC = new HashSet<string>(StringComparer.Ordinal);

						bool classHasLabyContext = hasLabyMeta;
						bool classHasSingleB = false;
						bool classHasSingleE = false;

						for (int s = 0; s < cpStrings.Count; s++)
						{
							string rawTok = cpStrings[s];
							if (rawTok.Length < 1) continue;
							string dotNorm = rawTok.Replace('/', '.');

							if (rawTok == "b") classHasSingleB = true;
							if (rawTok == "e") classHasSingleE = true;
							if (dotNorm.Contains("net.labymod.") || dotNorm == "Ldci;" || dotNorm == "dci") classHasLabyContext = true;

							// Forge Hitbox
							for (int p = 0; p < ForgeHitboxPrimary.Length; p++)
							{
								if (ContainsIdentifierToken(dotNorm, ForgeHitboxPrimary[p]))
								{
									clsForgeHitboxP.Add(ForgeHitboxPrimary[p]);
									jarWideForgeHitboxPrimary.Add(ForgeHitboxPrimary[p]);
								}
							}
							for (int p = 0; p < ForgeHitboxSecondary.Length; p++)
							{
								if (IsExactOrCallIdentifier(rawTok, ForgeHitboxSecondary[p]))
								{
									clsForgeHitboxS.Add(ForgeHitboxSecondary[p]);
									jarWideForgeHitboxSecondary.Add(ForgeHitboxSecondary[p]);
								}
							}

							// Forge Reach / AutoClicker
							for (int p = 0; p < ForgeReachPatterns.Length; p++)
							{
								string pat = ForgeReachPatterns[p];
								if (pat == "mc.playerController")
								{
									if (ContainsIdentifierToken(dotNorm, "mc.playerController") ||
										ContainsIdentifierToken(dotNorm, "playerController") ||
										ContainsIdentifierToken(dotNorm, "field_71442_b"))
									{
										clsForgeReach.Add("mc.playerController");
										jarWideForgeReach.Add("mc.playerController");
									}
								}
								else if (ContainsIdentifierToken(dotNorm, pat))
								{
									clsForgeReach.Add(pat);
									jarWideForgeReach.Add(pat);
								}
							}

							// Forge Criticals / Movement
							for (int p = 0; p < ForgeMovementPrimary.Length; p++)
							{
								if (ContainsIdentifierToken(dotNorm, ForgeMovementPrimary[p]))
								{
									clsForgeMoveP.Add(ForgeMovementPrimary[p]);
									jarWideForgeMovePrimary.Add(ForgeMovementPrimary[p]);
								}
							}
							for (int p = 0; p < ForgeMovementSecondary.Length; p++)
							{
								if (IsExactOrCallIdentifier(rawTok, ForgeMovementSecondary[p]))
								{
									clsForgeMoveS.Add(ForgeMovementSecondary[p]);
									jarWideForgeMoveSecondary.Add(ForgeMovementSecondary[p]);
								}
							}

							// LabyMod literal expression check
							if (ContainsIdentifierToken(rawTok, "dci") || dotNorm == "Ldci;")
							{
								clsLaby.Add("dci");
								jarWideLabyHits.Add("dci");
							}
							bool hasCcDotField = false;
							if (rawTok.Contains(".cc().b")) { clsLaby.Add(".cc().b"); jarWideLabyHits.Add(".cc().b"); hasCcDotField = true; }
							if (rawTok.Contains(".cc().e")) { clsLaby.Add(".cc().e"); jarWideLabyHits.Add(".cc().e"); hasCcDotField = true; }
							if (rawTok.Contains(".cD()")) { clsLaby.Add(".cD()"); jarWideLabyHits.Add(".cD()"); }
							if (rawTok.Contains(".cH()")) { clsLaby.Add(".cH()"); jarWideLabyHits.Add(".cH()"); }
							if (!hasCcDotField && rawTok.Contains(".cc()")) { clsLaby.Add(".cc()"); jarWideLabyHits.Add(".cc()"); }

							// LabyMod CP tokens (only when LabyMod context exists so arbitrary ProGuard jars don't FP)
							if (classHasLabyContext)
							{
								if (rawTok == "cD") { clsLaby.Add(".cD()"); jarWideLabyHits.Add(".cD()"); }
								if (rawTok == "cH") { clsLaby.Add(".cH()"); jarWideLabyHits.Add(".cH()"); }
								if (rawTok == "cc")
								{
									if (classHasSingleB) { clsLaby.Add(".cc().b"); jarWideLabyHits.Add(".cc().b"); }
									else if (classHasSingleE) { clsLaby.Add(".cc().e"); jarWideLabyHits.Add(".cc().e"); }
									else { clsLaby.Add(".cc()"); jarWideLabyHits.Add(".cc()"); }
								}
							}

							// Fabric / Yarn Primary Combat
							for (int p = 0; p < FabricCombatPrimary.Length; p++)
							{
								string fp = FabricCombatPrimary[p];
								if (ContainsIdentifierToken(dotNorm, fp))
								{
									string canonical = CanonicalizeFabricToken(fp);
									clsFabricP.Add(canonical);
									jarWideFabricPrimary.Add(canonical);
								}
							}

							// Fabric / Yarn Context
							for (int p = 0; p < FabricContextTokens.Length; p++)
							{
								string fc = FabricContextTokens[p];
								bool matched = (fc == "Box")
									? (rawTok == "Box" || dotNorm.EndsWith(".Box") || dotNorm.Contains(".Box;") || rawTok.Contains("util/math/Box"))
									: ContainsIdentifierToken(dotNorm, fc);
								if (matched)
								{
									string canonical = CanonicalizeFabricToken(fc);
									clsFabricC.Add(canonical);
									jarWideFabricContext.Add(canonical);
								}
							}
						}

						// Check >= 2 rule in this class
						if (isStandardLegitMod)
						{
							// Standard performance/visual mods (Sodium/Iris/VoiceChat/etc.) only trigger if explicit Forge Hitbox/Reach tamper combo
							if (clsForgeHitboxP.Contains("setEntityBoundingBox") && clsForgeReach.Contains("attackEntity"))
							{
								List<string> sigs = new List<string>(clsForgeHitboxP);
								sigs.AddRange(clsForgeReach);
								if (sigs.Count > bestTemplateHits.Count)
								{
									bestTemplateHits = sigs;
									bestTemplateClass = entryPath;
									bestTemplateKind = "REGEX MODS: Инъекция Hitbox/Reach в стандартный мод";
								}
							}
							continue;
						}

						// Forge Hitbox: >= 2 distinct with >= 1 HitboxPrimary
						int hitboxTotal = clsForgeHitboxP.Count + clsForgeHitboxS.Count;
						if (clsForgeHitboxP.Count >= 1 && hitboxTotal >= 2)
						{
							List<string> combined = CombineDistinct(clsForgeHitboxP, clsForgeHitboxS);
							if (combined.Count > bestTemplateHits.Count)
							{
								bestTemplateHits = combined;
								bestTemplateClass = entryPath;
								bestTemplateKind = "REGEX MODS (Forge Hitboxes)";
							}
						}

						// Forge Reach / AutoClicker: >= 2 distinct
						if (clsForgeReach.Count >= 2 && clsForgeReach.Count > bestTemplateHits.Count)
						{
							bestTemplateHits = new List<string>(clsForgeReach);
							bestTemplateClass = entryPath;
							bestTemplateKind = "REGEX MODS (Forge Reach / AutoClicker)";
						}

						// Forge Movement / Criticals: >= 2 distinct with >= 1 MovePrimary
						int moveTotal = clsForgeMoveP.Count + clsForgeMoveS.Count;
						if (clsForgeMoveP.Count >= 1 && moveTotal >= 2)
						{
							List<string> combined = CombineDistinct(clsForgeMoveP, clsForgeMoveS);
							if (combined.Count > bestTemplateHits.Count)
							{
								bestTemplateHits = combined;
								bestTemplateClass = entryPath;
								bestTemplateKind = "REGEX MODS (Forge Criticals / Movement)";
							}
						}

						// LabyMod: >= 2 distinct
						if (clsLaby.Count >= 2 && clsLaby.Count > bestTemplateHits.Count)
						{
							bestTemplateHits = new List<string>(clsLaby);
							bestTemplateClass = entryPath;
							bestTemplateKind = "REGEX MODS (LabyMod)";
						}

						// Fabric / Yarn: >= 2 distinct with >= 1 FabricCombatPrimary
						int fabTotal = clsFabricP.Count + clsFabricC.Count;
						if (clsFabricP.Count >= 1 && fabTotal >= 2)
						{
							List<string> combined = CombineDistinct(clsFabricP, clsFabricC);
							if (combined.Count > bestTemplateHits.Count)
							{
								bestTemplateHits = combined;
								bestTemplateClass = entryPath;
								bestTemplateKind = "REGEX MODS (Fabric / Yarn)";
							}
						}
					}

					// Check mod-wide Fabric / Forge / LabyMod combo (>= 2 distinct across classes if no single class exceeded 2)
					if (!isCpvpWhite && !isStandardLegitMod && bestTemplateHits.Count < 2)
					{
						if (jarWideFabricPrimary.Count >= 1 && (jarWideFabricPrimary.Count + jarWideFabricContext.Count) >= 2)
						{
							bestTemplateHits = CombineDistinct(jarWideFabricPrimary, jarWideFabricContext);
							bestTemplateClass = "(уровень всего JAR)";
							bestTemplateKind = "REGEX MODS (Fabric / Yarn)";
						}
						else if (jarWideForgeHitboxPrimary.Count >= 1 && (jarWideForgeHitboxPrimary.Count + jarWideForgeHitboxSecondary.Count) >= 2)
						{
							bestTemplateHits = CombineDistinct(jarWideForgeHitboxPrimary, jarWideForgeHitboxSecondary);
							bestTemplateClass = "(уровень всего JAR)";
							bestTemplateKind = "REGEX MODS (Forge Hitboxes)";
						}
						else if (jarWideForgeReach.Count >= 2)
						{
							bestTemplateHits = new List<string>(jarWideForgeReach);
							bestTemplateClass = "(уровень всего JAR)";
							bestTemplateKind = "REGEX MODS (Forge Reach / AutoClicker)";
						}
						else if (jarWideForgeMovePrimary.Count >= 1 && (jarWideForgeMovePrimary.Count + jarWideForgeMoveSecondary.Count) >= 2)
						{
							bestTemplateHits = CombineDistinct(jarWideForgeMovePrimary, jarWideForgeMoveSecondary);
							bestTemplateClass = "(уровень всего JAR)";
							bestTemplateKind = "REGEX MODS (Forge Criticals / Movement)";
						}
						else if (jarWideLabyHits.Count >= 2)
						{
							bestTemplateHits = new List<string>(jarWideLabyHits);
							bestTemplateClass = "(уровень всего JAR)";
							bestTemplateKind = "REGEX MODS (LabyMod)";
						}
					}
				}
			}
			catch (Exception ex)
			{
				return new HighLevelEvidenceItem
				{
					SourceModule = "Luyten JAR / REGEX MODS",
					Category = HighLevelCheckCategory.LuytenMods,
					TargetName = fileName,
					CoreOrType = modCore,
					MatchedSignatures = "Ошибка чтения",
					TimeOrSize = sizeText,
					Verdict = "Пропущен (ошибка архива)",
					RuleViolation = "ИНФО",
					Description = "Не удалось открыть JAR: " + ex.Message,
					FullDetails = jarPath,
					Severity = CheckSeverity.Info,
					IsViolation = false
				};
			}

			// Verdict prioritization: SystemDLC -> Explicit Cheat -> REGEX MODS (>= 2) -> Clean/Info
			if (systemDlcEvidence.Count > 0)
			{
				return new HighLevelEvidenceItem
				{
					SourceModule = "Luyten JAR / SystemDLC",
					Category = HighLevelCheckCategory.SystemDlc,
					TargetName = fileName,
					CoreOrType = "SystemDLC (" + modCore + ")",
					MatchedSignatures = string.Join(", ", DeduplicateTake(systemDlcEvidence, 8)),
					TimeOrSize = sizeText,
					Verdict = "ОБНАРУЖЕН ЧИТ SYSTEMDLC В JAR",
					RuleViolation = "5.01",
					Description = "Внутри архива обнаружены классы/строки SystemDLC (" + modCore + "): " + fileName,
					FullDetails = "Путь: " + jarPath + "\r\nСовпадения: " + string.Join("; ", DeduplicateTake(systemDlcEvidence, 12)),
					Severity = CheckSeverity.Critical,
					IsViolation = true
				};
			}

			if (explicitCheatEvidence.Count > 0 || bestTemplateHits.Count >= 2)
			{
				List<string> sigList = new List<string>();
				if (bestTemplateHits.Count >= 2) sigList.AddRange(bestTemplateHits);
				if (explicitCheatEvidence.Count > 0) sigList.AddRange(DeduplicateTake(explicitCheatEvidence, 5));
				string matchedText = string.Join(", ", sigList.ToArray());

				string verdict = bestTemplateHits.Count >= 2
					? bestTemplateKind
					: "Чит-пакет внутри мода";

				return new HighLevelEvidenceItem
				{
					SourceModule = "Luyten JAR / REGEX MODS",
					Category = HighLevelCheckCategory.LuytenMods,
					TargetName = fileName,
					CoreOrType = modCore + (string.IsNullOrEmpty(modId) ? "" : (" [" + modId + "]")),
					MatchedSignatures = matchedText,
					TimeOrSize = sizeText,
					Verdict = verdict,
					RuleViolation = hasJavaAgentManifest ? "5.01 / 5.08" : "5.01",
					Description = string.Format("Обнаружена комбинация REGEX MODS ({0} совп.) в классе {1} мода {2}",
						sigList.Count, string.IsNullOrEmpty(bestTemplateClass) ? "—" : bestTemplateClass, fileName),
					FullDetails = "Файл: " + jarPath + "\r\nЯдро: " + modCore +
						(string.IsNullOrEmpty(modName) ? "" : ("\r\nИмя мода: " + modName)) +
						"\r\nКласс: " + bestTemplateClass +
						"\r\nСигнатуры: " + matchedText,
					Severity = CheckSeverity.Critical,
					IsViolation = true
				};
			}

			// Also verify filename cheat match cleanly if un-whitelisted
			if (IsExplicitCheatFileName(fileName))
			{
				return new HighLevelEvidenceItem
				{
					SourceModule = "Luyten JAR / Имя файла",
					Category = HighLevelCheckCategory.LuytenMods,
					TargetName = fileName,
					CoreOrType = modCore,
					MatchedSignatures = Path.GetFileNameWithoutExtension(fileName),
					TimeOrSize = sizeText,
					Verdict = "Подозрительное имя мода",
					RuleViolation = "5.01",
					Description = "Файл мода соответствует известной сигнатуре чита: " + fileName,
					FullDetails = jarPath,
					Severity = CheckSeverity.Critical,
					IsViolation = true
				};
			}

			return new HighLevelEvidenceItem
			{
				SourceModule = "Luyten JAR / REGEX MODS",
				Category = HighLevelCheckCategory.LuytenMods,
				TargetName = fileName,
				CoreOrType = modCore + (string.IsNullOrEmpty(modId) ? "" : (" [" + modId + "]")),
				MatchedSignatures = "Нарушений нет",
				TimeOrSize = sizeText,
				Verdict = "Легитимный мод",
				RuleViolation = "ИНФО",
				Description = "Мод прошёл Luyten REGEX MODS анализ чисто.",
				FullDetails = jarPath,
				Severity = CheckSeverity.Info,
				IsViolation = false
			};
		}

		private static List<string> ExtractClassConstantPoolAndText(byte[] data)
		{
			List<string> results = new List<string>();
			if (data == null || data.Length < 10) return results;

			bool isJavaClass = data[0] == 0xCA && data[1] == 0xFE && data[2] == 0xBA && data[3] == 0xBE;
			if (isJavaClass)
			{
				try
				{
					int cpCount = (data[8] << 8) | data[9];
					int pos = 10;
					bool cpValid = cpCount > 1 && cpCount < 65535;
					for (int idx = 1; cpValid && idx < cpCount && pos < data.Length; idx++)
					{
						byte tag = data[pos++];
						switch (tag)
						{
							case 1: // CONSTANT_Utf8
								if (pos + 2 > data.Length) { cpValid = false; break; }
								int len = (data[pos] << 8) | data[pos + 1];
								pos += 2;
								if (len < 0 || pos + len > data.Length) { cpValid = false; break; }
								if (len > 0 && len <= 4096)
								{
									string s = Encoding.UTF8.GetString(data, pos, len);
									results.Add(s);
								}
								pos += len;
								break;
							case 3: // Integer
							case 4: // Float
							case 9: // Fieldref
							case 10: // Methodref
							case 11: // InterfaceMethodref
							case 12: // NameAndType
							case 17: // Dynamic
							case 18: // InvokeDynamic
								pos += 4;
								break;
							case 5: // Long
							case 6: // Double
								pos += 8;
								idx++; // JVM spec: takes 2 CP slots
								break;
							case 7: // Class
							case 8: // String
							case 16: // MethodType
							case 19: // Module
							case 20: // Package
								pos += 2;
								break;
							case 15: // MethodHandle
								pos += 3;
								break;
							default:
								cpValid = false;
								break;
						}
					}
					if (results.Count > 0) return results;
				}
				catch { }
			}

			// Fallback: extract printable ASCII/UTF-8 runs (>= 2 chars for LabyMod/Java tokens)
			StringBuilder run = new StringBuilder();
			for (int i = 0; i < data.Length; i++)
			{
				byte b = data[i];
				if (b >= 0x20 && b <= 0x7E)
				{
					run.Append((char)b);
				}
				else
				{
					if (run.Length >= 2) results.Add(run.ToString());
					run.Length = 0;
				}
			}
			if (run.Length >= 2) results.Add(run.ToString());
			return results;
		}

		private static string CanonicalizeFabricToken(string token)
		{
			if (token == "setBoundingBox" || token == "method_5857") return "method_5857 (setBoundingBox)";
			if (token == "getReachDistance" || token == "method_2920") return "method_2920 (getReachDistance)";
			if (token == "attackEntity" || token == "method_2918") return "method_2918 (attackEntity)";
			if (token == "ClientPlayerInteractionManager" || token == "class_636") return "class_636 (InteractionManager)";
			if (token == "getBoundingBox" || token == "method_5829") return "method_5829 (getBoundingBox)";
			if (token == "Box" || token == "net.minecraft.class_238") return "net.minecraft.class_238 (Box)";
			return token;
		}

		private static bool IsStandardLegitMod(string fileName, string modId)
		{
			string fnLow = (fileName ?? "").ToLowerInvariant();
			string idLow = (modId ?? "").ToLowerInvariant();
			for (int i = 0; i < StandardLegitimateModIds.Length; i++)
			{
				string leg = StandardLegitimateModIds[i];
				if (idLow == leg || idLow.StartsWith(leg + "-") || fnLow.StartsWith(leg + "-") || fnLow.StartsWith(leg + "_") || fnLow == leg + ".jar")
				{
					return true;
				}
			}
			return false;
		}

		private static bool IsExplicitCheatFileName(string fileName)
		{
			string low = Path.GetFileNameWithoutExtension(fileName ?? "").ToLowerInvariant();
			if (string.IsNullOrEmpty(low)) return false;
			if (CheatDatabases.IsWhitelistedPath(low)) return false;
			string[] cheats = CheatDetector.CheatClientNames;
			for (int i = 0; i < cheats.Length; i++)
			{
				if (cheats[i].Length >= 4 && !CheatDatabases.IsAmbiguous(cheats[i]) && CheatDetector.MatchesCheatName(low, cheats[i]))
				{
					return true;
				}
			}
			return false;
		}

		#endregion

		#region 2. HxD Hex-Editor DLL Inspector

		private static void ScanHxdDlls(
			List<HighLevelEvidenceItem> items,
			bool includeInformational,
			string extraDllPath,
			ref int totalScanned)
		{
			Dictionary<string, string> candidateSources = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

			if (!string.IsNullOrEmpty(extraDllPath) && File.Exists(extraDllPath))
			{
				candidateSources[extraDllPath] = "Target DLL";
			}

			// 1. Loaded modules in running javaw.exe / java.exe
			try
			{
				Process[] javas = Process.GetProcessesByName("javaw");
				foreach (Process p in javas)
				{
					CollectProcessDllCandidates(p, "javaw.exe (PID " + p.Id + ")", candidateSources);
				}
				Process[] javas2 = Process.GetProcessesByName("java");
				foreach (Process p in javas2)
				{
					CollectProcessDllCandidates(p, "java.exe (PID " + p.Id + ")", candidateSources);
				}
			}
			catch { }

			// 2. OpenSavePidlMRU, RecentDocs, InjectDllHistory
			CollectMruDllPaths(candidateSources);

			// 3. Temp, Downloads, Desktop, Documents top-level DLLs
			string[] userDirs = new string[]
			{
				Path.GetTempPath(),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Documents")
			};
			for (int d = 0; d < userDirs.Length; d++)
			{
				try
				{
					if (!Directory.Exists(userDirs[d])) continue;
					string[] dlls = Directory.GetFiles(userDirs[d], "*.dll", SearchOption.TopDirectoryOnly);
					int maxTake = Math.Min(dlls.Length, 80);
					for (int i = 0; i < maxTake; i++)
					{
						if (!candidateSources.ContainsKey(dlls[i]))
						{
							candidateSources[dlls[i]] = "Пользовательская папка (" + Path.GetFileName(userDirs[d].TrimEnd('\\')) + ")";
						}
					}
				}
				catch { }
			}

			int benignShown = 0;
			foreach (KeyValuePair<string, string> kvp in candidateSources)
			{
				string dllPath = kvp.Key;
				totalScanned++;
				HighLevelEvidenceItem item = AnalyzeDllFileHxD(dllPath, kvp.Value);
				if (item == null) continue;

				if (item.IsViolation)
				{
					items.Add(item);
				}
				else if (includeInformational && benignShown < 25)
				{
					items.Add(item);
					benignShown++;
				}
			}
		}

		private static void CollectProcessDllCandidates(Process proc, string sourceLabel, Dictionary<string, string> candidateSources)
		{
			try
			{
				foreach (ProcessModule mod in proc.Modules)
				{
					try
					{
						string fn = mod.FileName;
						if (string.IsNullOrEmpty(fn)) continue;
						if (!fn.EndsWith(".dll", StringComparison.OrdinalIgnoreCase)) continue;
						if (!candidateSources.ContainsKey(fn))
						{
							candidateSources[fn] = sourceLabel;
						}
					}
					catch { }
				}
			}
			catch { }
		}

		private static void CollectMruDllPaths(Dictionary<string, string> candidateSources)
		{
			string[] regMruPaths = new string[]
			{
				@"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU\dll",
				@"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU\*",
				@"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs\.dll",
				@"Software\ProcessHacker",
				@"Software\SystemInformer"
			};

			for (int r = 0; r < regMruPaths.Length; r++)
			{
				try
				{
					using (RegistryKey key = Registry.CurrentUser.OpenSubKey(regMruPaths[r]))
					{
						if (key == null) continue;
						string[] names = key.GetValueNames();
						for (int n = 0; n < names.Length; n++)
						{
							if (names[n].Equals("MRUListEx", StringComparison.OrdinalIgnoreCase)) continue;
							object val = key.GetValue(names[n]);
							List<string> extracted = ExtractStringsFromRegistryValue(val);
							for (int e = 0; e < extracted.Count; e++)
							{
								string candidate = extracted[e];
								if (candidate.EndsWith(".dll", StringComparison.OrdinalIgnoreCase) && candidate.Length >= 7 && candidate[1] == ':')
								{
									if (File.Exists(candidate) && !candidateSources.ContainsKey(candidate))
									{
										candidateSources[candidate] = "MRU / InjectDllHistory";
									}
								}
							}
						}
					}
				}
				catch { }
			}
		}

		private static HighLevelEvidenceItem AnalyzeDllFileHxD(string dllPath, string sourceLabel)
		{
			if (string.IsNullOrEmpty(dllPath) || !File.Exists(dllPath)) return null;
			string fileName = Path.GetFileName(dllPath);
			string lowerName = fileName.ToLowerInvariant();
			string lowerPath = dllPath.Replace('/', '\\').ToLowerInvariant();

			// 1. OS / Runtime / Whitelist / Known Safe Path prefilter
			if (IsSystemOrJavaRuntimeDllPath(lowerPath) ||
				ProcessMemoryScanner.IsLegitimateMinecraftNativeDll(lowerName, lowerPath) ||
				CheatDatabases.IsWhitelistedPath(lowerPath) ||
				lowerPath.Contains("mccheatchecker") ||
				lowerPath.Contains(@"\programs\antigravity\"))
			{
				return null;
			}

			// 2. Skip valid Authenticode signed binaries unless explicit SystemDLC/Doomsday
			bool isSigned = IsFileAuthenticodeSigned(dllPath);
			if (isSigned)
			{
				return null;
			}

			long length = 0;
			try { length = new FileInfo(dllPath).Length; } catch { }
			if (length <= 0 || length > 45L * 1024L * 1024L) return null;

			byte[] data;
			try
			{
				int readCap = (int)Math.Min(length, 10L * 1024L * 1024L);
				data = new byte[readCap];
				using (FileStream fs = new FileStream(dllPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					int totalRead = 0;
					while (totalRead < readCap)
					{
						int r = fs.Read(data, totalRead, readCap - totalRead);
						if (r <= 0) break;
						totalRead += r;
					}
					if (totalRead < readCap)
					{
						Array.Resize(ref data, totalRead);
					}
				}
			}
			catch
			{
				return null;
			}

			if (data.Length < 8) return null;

			// Scan for CA FE BA BE magic bytes
			bool hasCafeBabeRaw = false;
			bool hasCafeBabeWithClassVer = false;
			for (int i = 0; i <= data.Length - 4; i++)
			{
				if (data[i] == 0xCA && data[i + 1] == 0xFE && data[i + 2] == 0xBA && data[i + 3] == 0xBE)
				{
					hasCafeBabeRaw = true;
					if (i + 7 < data.Length && data[i + 4] == 0x00 && data[i + 5] <= 0x03 && data[i + 6] == 0x00 && data[i + 7] >= 0x2D && data[i + 7] <= 0x48)
					{
						hasCafeBabeWithClassVer = true;
						break;
					}
				}
			}

			// Extract lowercased ASCII and UTF-16LE views for rapid indicator search
			string ascii = DecodeAsciiPrintableAndNullSpaced(data);
			string uniLe = DecodeUtf16LeSafe(data);

			List<string> cheatHits = new List<string>();
			List<string> apiHits = new List<string>();
			bool hasUnambiguousCheatName = false;
			bool hasSystemDlc = false;

			if (hasCafeBabeRaw)
			{
				cheatHits.Add(hasCafeBabeWithClassVer ? (JvmClassMagicLabel + " (JVM Class)") : JvmClassMagicLabel);
			}

			for (int i = 0; i < HxdCheatMarkers.Length; i++)
			{
				string marker = HxdCheatMarkers[i];
				bool matched = ContainsHxdMarker(ascii, uniLe, marker);
				if (matched)
				{
					cheatHits.Add(marker);
					if (marker == "Doomsday" || marker == "SystemDLC")
					{
						hasUnambiguousCheatName = true;
						if (marker == "SystemDLC") hasSystemDlc = true;
					}
				}
			}

			for (int i = 0; i < SystemDlcKeywords.Length; i++)
			{
				if (ContainsWordToken(ascii.ToLowerInvariant(), SystemDlcKeywords[i]) ||
					ContainsWordToken(uniLe.ToLowerInvariant(), SystemDlcKeywords[i]))
				{
					hasSystemDlc = true;
					hasUnambiguousCheatName = true;
					if (!cheatHits.Contains(SystemDlcKeywords[i])) cheatHits.Add(SystemDlcKeywords[i]);
				}
			}

			for (int i = 0; i < HxdApiMarkers.Length; i++)
			{
				string marker = HxdApiMarkers[i];
				if (ContainsHxdMarker(ascii, uniLe, marker))
				{
					apiHits.Add(marker);
				}
			}

			bool isCheatCombo = hasUnambiguousCheatName || (cheatHits.Count >= 1 && apiHits.Count >= 1);

			if (isCheatCombo)
			{
				List<string> allHits = new List<string>(cheatHits);
				allHits.AddRange(apiHits);
				string sigStr = string.Join(", ", allHits.ToArray());

				return new HighLevelEvidenceItem
				{
					SourceModule = "HxD DLL Inspector (" + sourceLabel + ")",
					Category = hasSystemDlc ? HighLevelCheckCategory.SystemDlc : HighLevelCheckCategory.HxdDllHex,
					TargetName = fileName,
					CoreOrType = hasSignedMismatch(isSigned) + (hasCafeBabeRaw ? "PE DLL + JVM Class Payload" : "Неподписанная PE DLL"),
					MatchedSignatures = sigStr,
					TimeOrSize = FormatFileSize(dllPath),
					Verdict = hasSystemDlc ? "ОБНАРУЖЕНА SYSTEMDLC DLL" : "ИНЖЕКТ / ЧИТ-DLL (HxD Hex)",
					RuleViolation = "5.01",
					Description = string.Format("Неподписанная DLL '{0}' содержит боевую/JNI комбинацию HxD: {1}", fileName, sigStr),
					FullDetails = "Путь: " + dllPath + "\r\nИсточник: " + sourceLabel + "\r\nПодпись: Отсутствует\r\nСовпадения HxD: " + sigStr,
					Severity = CheckSeverity.Critical,
					IsViolation = true
				};
			}

			return new HighLevelEvidenceItem
			{
				SourceModule = "HxD DLL Inspector (" + sourceLabel + ")",
				Category = HighLevelCheckCategory.HxdDllHex,
				TargetName = fileName,
				CoreOrType = "Неподписанная DLL",
				MatchedSignatures = "Комбинаций HxD нет",
				TimeOrSize = FormatFileSize(dllPath),
				Verdict = "Чисто",
				RuleViolation = "ИНФО",
				Description = "Неподписанная DLL проверена по hex-сигнатурам (JVM Class / JNI / AABB): чит-комбинации отсутствуют.",
				FullDetails = dllPath,
				Severity = CheckSeverity.Info,
				IsViolation = false
			};
		}

		private static string hasSignedMismatch(bool isSigned)
		{
			return isSigned ? "" : "[Unsigned] ";
		}

		private static bool ContainsHxdMarker(string ascii, string uniLe, string marker)
		{
			if (marker == "Hitbox" || marker == "Reach" || marker == "Doomsday" || marker == "SystemDLC")
			{
				return ContainsWordToken(ascii.ToLowerInvariant(), marker.ToLowerInvariant()) ||
					ContainsWordToken(uniLe.ToLowerInvariant(), marker.ToLowerInvariant());
			}
			return ascii.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0 ||
				uniLe.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0;
		}

		private static bool IsSystemOrJavaRuntimeDllPath(string lowerPath)
		{
			return lowerPath.StartsWith(@"c:\windows\system32\") ||
				lowerPath.StartsWith(@"c:\windows\syswow64\") ||
				lowerPath.StartsWith(@"c:\windows\winsxs\") ||
				lowerPath.Contains(@"\system32\driverstore\") ||
				lowerPath.Contains(@"\windowsapps\") ||
				lowerPath.Contains(@"\program files\java\") ||
				lowerPath.Contains(@"\program files (x86)\java\") ||
				lowerPath.Contains(@"\runtime\bin\") ||
				lowerPath.Contains(@"\jre\bin\") ||
				lowerPath.Contains(@"\jdk\") ||
				lowerPath.Contains(@"\microsoft.net\") ||
				lowerPath.Contains(@"\assembly\nativeimages");
		}

		#endregion

		#region 3. BAM "Not Signed Only" Audit

		private static void ScanBamNotSignedOnly(
			List<HighLevelEvidenceItem> items,
			bool includeInformational,
			ref int totalScanned)
		{
			Dictionary<string, string> volumeMap = BuildDeviceToDriveMap();
			string[] bamRoots = new string[]
			{
				@"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings",
				@"SYSTEM\CurrentControlSet\Services\bam\UserSettings"
			};

			bool accessDenied = false;
			int signedSafeCount = 0;
			int unsignedBenignShown = 0;

			for (int r = 0; r < bamRoots.Length; r++)
			{
				try
				{
					using (RegistryKey rootKey = Registry.LocalMachine.OpenSubKey(bamRoots[r], false))
					{
						if (rootKey == null) continue;
						string[] sidSubkeys = rootKey.GetSubKeyNames();
						for (int s = 0; s < sidSubkeys.Length; s++)
						{
							string sid = sidSubkeys[s];
							bool isSid1001 = sid.EndsWith("1001", StringComparison.Ordinal);
							try
							{
								using (RegistryKey sidKey = rootKey.OpenSubKey(sid, false))
								{
									if (sidKey == null) continue;
									string[] valNames = sidKey.GetValueNames();
									for (int v = 0; v < valNames.Length; v++)
									{
										string devPath = valNames[v];
										if (!devPath.StartsWith(@"\Device\", StringComparison.OrdinalIgnoreCase)) continue;
										totalScanned++;

										byte[] rawVal = sidKey.GetValue(devPath) as byte[];
										string execTimeStr = ParseBamFileTime(rawVal);
										string winPath = ResolveDevicePathToWindowsPath(devPath, volumeMap);
										string lowerWin = winPath.ToLowerInvariant();
										string exeName = Path.GetFileName(winPath);
										string lowerExe = exeName.ToLowerInvariant();

										if (lowerWin.Contains("mccheatchecker") || lowerWin.Contains("screenshare") || CheatDatabases.IsWhitelistedPath(lowerWin))
										{
											signedSafeCount++;
											continue;
										}

										bool exists = FileExistsSafe(winPath);
										bool isSigned = exists && IsFileAuthenticodeSigned(winPath);
										if (isSigned)
										{
											signedSafeCount++;
											continue;
										}

										// BAM "Not Signed Only": unsigned or deleted binary
										string cheatMatch = FindBamThreatSignature(lowerWin, lowerExe);
										bool isSystemDlc = ContainsAnySystemDlc(lowerWin);
										bool isSafeExistingDir = exists && IsSafeBamAppDirectory(lowerWin);
										bool isSuspiciousUserLoc = IsSuspiciousTempOrUserLocation(lowerWin) && !IsLegitTempInstaller(lowerWin, lowerExe);

										if (isSystemDlc || !string.IsNullOrEmpty(cheatMatch))
										{
											string tag = isSystemDlc ? "SystemDLC" : cheatMatch;
											items.Add(new HighLevelEvidenceItem
											{
												SourceModule = "BAM (" + (isSid1001 ? "*1001" : sid) + ")",
												Category = isSystemDlc ? HighLevelCheckCategory.SystemDlc : HighLevelCheckCategory.BamUnsigned,
												TargetName = exeName,
												CoreOrType = exists ? "Не подписан (На диске)" : "УДАЛЁН ПОСЛЕ ЗАПУСКА",
												MatchedSignatures = tag,
												TimeOrSize = execTimeStr,
												Verdict = isSystemDlc ? "СЛЕД SYSTEMDLC В BAM" : (exists ? "ЗАПУСК ЧИТА / СРЕДСТВА ОБХОДА" : "САМОУДАЛИВШИЙСЯ ЧИТ / ОЧИСТИТЕЛЬ"),
												RuleViolation = exists ? "5.01" : "5.08",
												Description = string.Format("BAM зафиксировал запуск '{0}' ({1}), подпись отсутствует{2}",
													winPath, tag, exists ? "" : ", файл уже удалён с диска"),
												FullDetails = "SID: " + sid + "\r\nDevice: " + devPath + "\r\nПуть: " + winPath + "\r\nВремя BAM: " + execTimeStr,
												Severity = CheckSeverity.Critical,
												IsViolation = true
											});
										}
										else if (!exists && isSuspiciousUserLoc)
										{
											items.Add(new HighLevelEvidenceItem
											{
												SourceModule = "BAM (" + (isSid1001 ? "*1001" : sid) + ")",
												Category = HighLevelCheckCategory.BamUnsigned,
												TargetName = exeName,
												CoreOrType = "Удалённый неподписанный EXE",
												MatchedSignatures = "Deleted from Temp/Downloads/Desktop",
												TimeOrSize = execTimeStr,
												Verdict = "Удалённый подозрительный EXE из Temp/Downloads",
												RuleViolation = "5.08",
												Description = string.Format("BAM: неподписанный файл '{0}' был запущен и впоследствии удалён из пользовательской папки.", winPath),
												FullDetails = "SID: " + sid + "\r\nПуть: " + winPath + "\r\nВремя BAM: " + execTimeStr,
												Severity = CheckSeverity.Critical,
												IsViolation = true
											});
										}
										else if (includeInformational && !isSafeExistingDir && unsignedBenignShown < 25)
										{
											unsignedBenignShown++;
											items.Add(new HighLevelEvidenceItem
											{
												SourceModule = "BAM (" + (isSid1001 ? "*1001" : sid) + ")",
												Category = HighLevelCheckCategory.BamUnsigned,
												TargetName = exeName,
												CoreOrType = exists ? "Not Signed (На диске)" : "Not Signed (Нет файла)",
												MatchedSignatures = "Чистый / Без чит-сигнатур",
												TimeOrSize = execTimeStr,
												Verdict = exists ? "Неподписанное ПО" : "Удалённый временный файл",
												RuleViolation = "ИНФО",
												Description = winPath,
												FullDetails = "SID: " + sid + "\r\nПуть: " + winPath + "\r\nВремя BAM: " + execTimeStr,
												Severity = CheckSeverity.Info,
												IsViolation = false
											});
										}
									}
								}
							}
							catch (SecurityException) { accessDenied = true; }
							catch (UnauthorizedAccessException) { accessDenied = true; }
							catch { }
						}
					}
				}
				catch (SecurityException) { accessDenied = true; }
				catch (UnauthorizedAccessException) { accessDenied = true; }
				catch { }
			}

			if (includeInformational)
			{
				items.Add(new HighLevelEvidenceItem
				{
					SourceModule = "BAM Not Signed Only",
					Category = HighLevelCheckCategory.BamUnsigned,
					TargetName = accessDenied ? "Доступ к BAM ограничен" : "Сводка BAM (Not Signed Only)",
					CoreOrType = "HKLM\\...\\bam",
					MatchedSignatures = "Проверено подписанных: " + signedSafeCount,
					TimeOrSize = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
					Verdict = accessDenied ? "Требуются права Администратора" : "Аудит BAM завершён",
					RuleViolation = "ИНФО",
					Description = accessDenied
						? "Ключ реестра HKLM\\SYSTEM\\CurrentControlSet\\Services\\bam требует прав Администратора."
						: string.Format("Проанализирован BAM (*1001): подписанных безопасных {0}, опасных следов не найдено.", signedSafeCount),
					FullDetails = accessDenied ? "Запустите от имени Администратора для полного чтения BAM." : "BAM UserSettings OK",
					Severity = CheckSeverity.Info,
					IsViolation = false
				});
			}
		}

		private static string FindBamThreatSignature(string lowerPath, string lowerExe)
		{
			if (CheatDatabases.IsWhitelistedPath(lowerPath)) return "";

			string[] cheats = CheatDetector.CheatClientNames;
			for (int i = 0; i < cheats.Length; i++)
			{
				if (cheats[i].Length >= 4 && !CheatDatabases.IsAmbiguous(cheats[i]) && CheatDetector.MatchesCheatName(lowerPath, cheats[i]))
				{
					return cheats[i];
				}
			}

			string[] extra = CheatDatabases.ExtraCheatClients;
			for (int i = 0; i < extra.Length; i++)
			{
				if (extra[i].Length >= 4 && !CheatDatabases.IsAmbiguous(extra[i]) && CheatDetector.MatchesCheatName(lowerPath, extra[i]))
				{
					return extra[i];
				}
			}

			string[] cleaners = CheatDatabases.KnownCleanerSignatures;
			for (int i = 0; i < cleaners.Length; i++)
			{
				string cln = cleaners[i];
				if (cln.Length < 5 || cln == "systeminformer") continue;
				if (ContainsWordToken(lowerPath, cln)) return cln;
			}

			for (int i = 0; i < BamInjectorMarkers.Length; i++)
			{
				if (ContainsWordToken(lowerExe, BamInjectorMarkers[i])) return BamInjectorMarkers[i];
			}

			return "";
		}

		private static bool IsSafeBamAppDirectory(string lowerPath)
		{
			return lowerPath.StartsWith(@"c:\windows\") ||
				lowerPath.StartsWith(@"c:\program files\") ||
				lowerPath.StartsWith(@"c:\program files (x86)\") ||
				lowerPath.Contains(@"\windowsapps\") ||
				lowerPath.Contains(@"\appdata\local\programs\") ||
				lowerPath.Contains(@"\appdata\local\discord\") ||
				lowerPath.Contains(@"\appdata\roaming\evovis\") ||
				lowerPath.Contains(@"\steamapps\") ||
				lowerPath.Contains(@"\jetbrains\") ||
				lowerPath.Contains(@"\.vscode\") ||
				lowerPath.Contains(@"\.gemini\");
		}

		private static bool IsSuspiciousTempOrUserLocation(string lowerPath)
		{
			return lowerPath.Contains(@"\appdata\local\temp\") ||
				lowerPath.Contains(@"\windows\temp\") ||
				lowerPath.Contains(@"\downloads\") ||
				lowerPath.Contains(@"\desktop\") ||
				lowerPath.Contains(@"\$recycle.bin\") ||
				lowerPath.Contains(@"\users\public\");
		}

		private static bool IsLegitTempInstaller(string lowerPath, string lowerExe)
		{
			if (TempGuidFolderRegex.IsMatch(lowerPath)) return true;
			for (int i = 0; i < SafeTempInstallerPrefixes.Length; i++)
			{
				if (lowerExe.StartsWith(SafeTempInstallerPrefixes[i])) return true;
			}
			return lowerExe.EndsWith(".tmp.exe") || lowerExe.Contains("setup") || lowerExe.Contains("update");
		}

		private static string ParseBamFileTime(byte[] raw)
		{
			try
			{
				if (raw == null || raw.Length < 8) return "—";
				long ft = BitConverter.ToInt64(raw, 0);
				if (ft <= 116444736000000000L) return "—";
				DateTime dt = DateTime.FromFileTimeUtc(ft).ToLocalTime();
				return dt.ToString("dd.MM.yyyy HH:mm:ss");
			}
			catch { return "—"; }
		}

		private static Dictionary<string, string> BuildDeviceToDriveMap()
		{
			Dictionary<string, string> map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
			try
			{
				string[] drives = Environment.GetLogicalDrives();
				StringBuilder sb = new StringBuilder(512);
				for (int i = 0; i < drives.Length; i++)
				{
					string driveRoot = drives[i].TrimEnd('\\');
					if (driveRoot.Length != 2) continue;
					sb.Length = 0;
					uint len = QueryDosDevice(driveRoot, sb, sb.Capacity);
					if (len > 0)
					{
						string dev = sb.ToString().TrimEnd('\0');
						if (!string.IsNullOrEmpty(dev))
						{
							map[dev] = driveRoot;
						}
					}
				}
			}
			catch { }
			return map;
		}

		private static string ResolveDevicePathToWindowsPath(string devPath, Dictionary<string, string> volumeMap)
		{
			foreach (KeyValuePair<string, string> kvp in volumeMap)
			{
				if (devPath.StartsWith(kvp.Key + "\\", StringComparison.OrdinalIgnoreCase))
				{
					return kvp.Value + devPath.Substring(kvp.Key.Length);
				}
			}
			Match m = Regex.Match(devPath, @"^\\Device\\HarddiskVolume\d+(\\.*)$", RegexOptions.IgnoreCase);
			if (m.Success)
			{
				return "C:" + m.Groups[1].Value;
			}
			return devPath;
		}

		#endregion

		#region 4. Event Logs (12 Key IDs) & Registry Forensics

		private static void ScanEventLogs(
			List<HighLevelEvidenceItem> items,
			bool includeInformational,
			ref int totalScanned)
		{
			// Exact 12 Event IDs: 3079, 1102, 7, 7036, 1001, 1002, 104, 1074, 4663, 4688, 4660, 4616
			QueryEventLogById("Application", 3079, 5, items, includeInformational, ref totalScanned);
			QueryEventLogById("Microsoft-Windows-Ntfs/Operational", 3079, 5, items, includeInformational, ref totalScanned);
			QueryEventLogById("Security", 1102, 5, items, includeInformational, ref totalScanned);
			QueryEventLogById("System", 104, 10, items, includeInformational, ref totalScanned);
			QueryEventLogById("Application", 104, 10, items, includeInformational, ref totalScanned);
			QueryEventLogById("Security", 4616, 15, items, includeInformational, ref totalScanned);
			QueryEventLogById("Microsoft-Windows-Sysmon/Operational", 7, 25, items, includeInformational, ref totalScanned);
			QueryEventLogById("System", 7036, 20, items, includeInformational, ref totalScanned);
			QueryEventLogById("Application", 1001, 10, items, includeInformational, ref totalScanned);
			QueryEventLogById("Application", 1002, 10, items, includeInformational, ref totalScanned);
			QueryEventLogById("System", 1074, 5, items, includeInformational, ref totalScanned);
			QueryEventLogById("Security", 4663, 20, items, includeInformational, ref totalScanned);
			QueryEventLogById("Security", 4688, 25, items, includeInformational, ref totalScanned);
			QueryEventLogById("Security", 4660, 20, items, includeInformational, ref totalScanned);
		}

		private static void QueryEventLogById(
			string logName,
			int eventId,
			int maxRecords,
			List<HighLevelEvidenceItem> items,
			bool includeInformational,
			ref int totalScanned)
		{
			totalScanned++;
			int count = 0;
			DateTime? latestTime = null;

			try
			{
				string xPath = string.Format("*[System[(EventID={0})]]", eventId);
				EventLogQuery query = new EventLogQuery(logName, PathType.LogName, xPath);
				query.ReverseDirection = true;

				using (EventLogReader reader = new EventLogReader(query))
				{
					EventRecord rec;
					while (count < maxRecords && (rec = reader.ReadEvent()) != null)
					{
						using (rec)
						{
							count++;
							DateTime evTime = rec.TimeCreated.HasValue ? rec.TimeCreated.Value.ToLocalTime() : DateTime.MinValue;
							if (!latestTime.HasValue && evTime != DateTime.MinValue) latestTime = evTime;

							string desc = "";
							try { desc = rec.FormatDescription() ?? ""; } catch { }
							if (string.IsNullOrEmpty(desc) && rec.Properties != null)
							{
								StringBuilder sb = new StringBuilder();
								for (int p = 0; p < rec.Properties.Count; p++)
								{
									object val = rec.Properties[p].Value;
									if (val != null) sb.Append(val.ToString()).Append(" | ");
								}
								desc = sb.ToString();
							}

							EvaluateSingleEventRecord(logName, eventId, evTime, desc, rec, items);
						}
					}
				}

				if (includeInformational && count > 0 && (eventId == 1001 || eventId == 1002 || eventId == 1074 || eventId == 7036))
				{
					items.Add(new HighLevelEvidenceItem
					{
						SourceModule = "EventLog (" + logName + ")",
						Category = HighLevelCheckCategory.EventLogsAndRegistry,
						TargetName = "Event ID " + eventId,
						CoreOrType = logName,
						MatchedSignatures = "Записей (выборка): " + count,
						TimeOrSize = latestTime.HasValue ? latestTime.Value.ToString("dd.MM.yyyy HH:mm:ss") : "—",
						Verdict = "Штатное событие Windows",
						RuleViolation = "ИНФО",
						Description = DescribeNormalEventId(eventId, count),
						FullDetails = logName + " EventID=" + eventId,
						Severity = CheckSeverity.Info,
						IsViolation = false
					});
				}
			}
			catch (EventLogNotFoundException)
			{
				// Sysmon or channel not installed on host — completely normal
			}
			catch (UnauthorizedAccessException)
			{
				if (includeInformational && eventId == 1102)
				{
					items.Add(new HighLevelEvidenceItem
					{
						SourceModule = "EventLog (Security)",
						Category = HighLevelCheckCategory.EventLogsAndRegistry,
						TargetName = "Журнал Security (1102/4616/4688)",
						CoreOrType = "Security Log",
						MatchedSignatures = "Требуются права Администратора",
						TimeOrSize = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
						Verdict = "Доступ ограничен",
						RuleViolation = "ИНФО",
						Description = "Чтение журнала Security (1102, 4616, 4663, 4688, 4660) требует запуска от Администратора.",
						FullDetails = "Security Event Log access requires elevation.",
						Severity = CheckSeverity.Info,
						IsViolation = false
					});
				}
			}
			catch { }
		}

		private static void EvaluateSingleEventRecord(
			string logName,
			int eventId,
			DateTime evTime,
			string desc,
			EventRecord rec,
			List<HighLevelEvidenceItem> items)
		{
			string timeStr = evTime != DateTime.MinValue ? evTime.ToString("dd.MM.yyyy HH:mm:ss") : "—";
			string descLow = (desc ?? "").ToLowerInvariant();
			bool recent14Days = evTime != DateTime.MinValue && (DateTime.Now - evTime).TotalDays <= 14.0;

			if (eventId == 3079 && recent14Days)
			{
				items.Add(new HighLevelEvidenceItem
				{
					SourceModule = "EventLog (" + logName + ")",
					Category = HighLevelCheckCategory.EventLogsAndRegistry,
					TargetName = "Event ID 3079 (Удаление USN Journal)",
					CoreOrType = "NTFS / Anti-Forensics",
					MatchedSignatures = "EventID=3079",
					TimeOrSize = timeStr,
					Verdict = "ОЧИСТКА USN ЖУРНАЛА",
					RuleViolation = "5.08",
					Description = "Обнаружено удаление USN-журнала файловой системы за последние 14 дней.",
					FullDetails = Truncate(desc, 500),
					Severity = CheckSeverity.Critical,
					IsViolation = true
				});
			}
			else if (eventId == 1102 && recent14Days)
			{
				items.Add(new HighLevelEvidenceItem
				{
					SourceModule = "EventLog (Security)",
					Category = HighLevelCheckCategory.EventLogsAndRegistry,
					TargetName = "Event ID 1102 (Очистка Security Log)",
					CoreOrType = "Security / Anti-Forensics",
					MatchedSignatures = "EventID=1102",
					TimeOrSize = timeStr,
					Verdict = "ЖУРНАЛ БЕЗОПАСНОСТИ ОЧИЩЕН",
					RuleViolation = "5.08",
					Description = "Зафиксирована очистка журнала аудита безопасности Windows (Event ID 1102).",
					FullDetails = Truncate(desc, 500),
					Severity = CheckSeverity.Critical,
					IsViolation = true
				});
			}
			else if (eventId == 104 && recent14Days)
			{
				bool importantChannel = descLow.Contains("security") || descLow.Contains("system") ||
					descLow.Contains("application") || descLow.Contains("powershell") ||
					descLow.Contains("sysmon") || descLow.Contains("ntfs");
				if (importantChannel)
				{
					items.Add(new HighLevelEvidenceItem
					{
						SourceModule = "EventLog (" + logName + ")",
						Category = HighLevelCheckCategory.EventLogsAndRegistry,
						TargetName = "Event ID 104 (Очистка журнала)",
						CoreOrType = "Eventlog Clear",
						MatchedSignatures = "EventID=104",
						TimeOrSize = timeStr,
						Verdict = "ОЧИСТКА ЖУРНАЛА СОБЫТИЙ",
						RuleViolation = "5.08",
						Description = "Зафиксирована очистка системного журнала событий (Event ID 104).",
						FullDetails = Truncate(desc, 500),
						Severity = CheckSeverity.Critical,
						IsViolation = true
					});
				}
			}
			else if (eventId == 4616 && recent14Days)
			{
				// Flag ONLY large manual clock change (>= 5 min) by non-system time sync
				bool isNtpOrSystemProc = descLow.Contains("w32time") || descLow.Contains("svchost.exe") ||
					descLow.Contains("local service") || descLow.Contains("network service");
				double deltaMin = ExtractClockJumpMinutes(rec);
				if (!isNtpOrSystemProc && deltaMin >= 5.0)
				{
					items.Add(new HighLevelEvidenceItem
					{
						SourceModule = "EventLog (Security)",
						Category = HighLevelCheckCategory.EventLogsAndRegistry,
						TargetName = "Event ID 4616 (Сдвиг системных часов)",
						CoreOrType = "Clock Tampering",
						MatchedSignatures = string.Format("Delta={0:F1} мин", deltaMin),
						TimeOrSize = timeStr,
						Verdict = "РУЧНОЙ ПЕРЕВОД ВРЕМЕНИ",
						RuleViolation = "5.08",
						Description = string.Format("Обнаружен ручной перевод системного времени на {0:F1} мин.", deltaMin),
						FullDetails = Truncate(desc, 500),
						Severity = CheckSeverity.Critical,
						IsViolation = true
					});
				}
			}
			else if (eventId == 4688 || eventId == 4663 || eventId == 4660 || eventId == 7)
			{
				if (ContainsAnySystemDlc(descLow))
				{
					items.Add(new HighLevelEvidenceItem
					{
						SourceModule = "EventLog (" + logName + ")",
						Category = HighLevelCheckCategory.SystemDlc,
						TargetName = "Event ID " + eventId,
						CoreOrType = "SystemDLC в логе",
						MatchedSignatures = "SystemDLC",
						TimeOrSize = timeStr,
						Verdict = "СЛЕД SYSTEMDLC В ЖУРНАЛЕ",
						RuleViolation = "5.01",
						Description = "В событии Windows (" + eventId + ") обнаружен след SystemDLC.",
						FullDetails = Truncate(desc, 500),
						Severity = CheckSeverity.Critical,
						IsViolation = true
					});
				}
				else if (!descLow.Contains("mccheatchecker") && !descLow.Contains("screenshare") &&
					ContainsAnyWipeCommand(descLow))
				{
					items.Add(new HighLevelEvidenceItem
					{
						SourceModule = "EventLog (" + logName + ")",
						Category = HighLevelCheckCategory.EventLogsAndRegistry,
						TargetName = "Event ID " + eventId,
						CoreOrType = "Анти-форензика в CLI",
						MatchedSignatures = "Wiping Command",
						TimeOrSize = timeStr,
						Verdict = "КОМАНДА ОЧИСТКИ ЛОГОВ",
						RuleViolation = "5.08",
						Description = "Зафиксирован запуск утилиты принудительной очистки журналов/теневых копий.",
						FullDetails = Truncate(desc, 500),
						Severity = CheckSeverity.Critical,
						IsViolation = true
					});
				}
			}
		}

		private static double ExtractClockJumpMinutes(EventRecord rec)
		{
			try
			{
				if (rec.Properties == null) return 0;
				List<DateTime> times = new List<DateTime>();
				for (int i = 0; i < rec.Properties.Count; i++)
				{
					object v = rec.Properties[i].Value;
					if (v is DateTime) times.Add((DateTime)v);
				}
				if (times.Count >= 2)
				{
					return Math.Abs((times[1] - times[0]).TotalMinutes);
				}
			}
			catch { }
			return 0;
		}

		private static string DescribeNormalEventId(int eventId, int count)
		{
			if (eventId == 1001) return "События WER / BugCheck (1001): " + count + " недавних записей без признаков читов.";
			if (eventId == 1002) return "События Application Hang (1002): " + count + " записей.";
			if (eventId == 1074) return "Штатные перезагрузки/выключения (1074): " + count + " записей.";
			if (eventId == 7036) return "Переходы служб Windows (7036): проверено " + count + " недавних событий.";
			return "Штатных событий: " + count;
		}

		private static void ScanRegistryForensics(
			List<HighLevelEvidenceItem> items,
			bool includeInformational,
			ref int totalScanned)
		{
			// 1. PCA Store / Persisted
			string[] pcaPaths = new string[]
			{
				@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store",
				@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Persisted"
			};
			int pcaCleanCount = 0;
			for (int p = 0; p < pcaPaths.Length; p++)
			{
				try
				{
					using (RegistryKey key = Registry.CurrentUser.OpenSubKey(pcaPaths[p]))
					{
						if (key == null) continue;
						string[] valNames = key.GetValueNames();
						for (int i = 0; i < valNames.Length; i++)
						{
							string path = valNames[i];
							totalScanned++;
							string low = path.ToLowerInvariant();
							if (low.Contains("mccheatchecker") || low.Contains("screenshare") || CheatDatabases.IsWhitelistedPath(low))
							{
								pcaCleanCount++;
								continue;
							}
							if (ContainsAnySystemDlc(low))
							{
								items.Add(MakeRegistryViolationItem("PCA Store/Persisted", path, "SystemDLC", "5.01", true));
								continue;
							}
							string hit = FindBamThreatSignature(low, Path.GetFileName(low));
							if (!string.IsNullOrEmpty(hit))
							{
								items.Add(MakeRegistryViolationItem("PCA Store/Persisted", path, hit, "5.01 / 5.08", false));
							}
							else
							{
								pcaCleanCount++;
							}
						}
					}
				}
				catch { }
			}

			// 2. OpenSavePidlMRU, RecentDocs, RunMRU
			int mruCleanCount = 0;
			string[] mruKeys = new string[]
			{
				@"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU",
				@"Software\Microsoft\Windows\CurrentVersion\Explorer\RecentDocs",
				@"Software\Microsoft\Windows\CurrentVersion\Explorer\RunMRU"
			};
			for (int k = 0; k < mruKeys.Length; k++)
			{
				InspectRegistryTreeStrings(Registry.CurrentUser, mruKeys[k], 0, items, ref totalScanned, ref mruCleanCount);
			}

			// 3. Command Processor\AutoRun persistence / execution hijacking
			InspectCommandProcessorAutoRun(Registry.CurrentUser, "HKCU", items, ref totalScanned);
			InspectCommandProcessorAutoRun(Registry.LocalMachine, "HKLM", items, ref totalScanned);

			if (includeInformational)
			{
				items.Add(new HighLevelEvidenceItem
				{
					SourceModule = "Реестр Windows (PCA / MRU / RunMRU / AutoRun)",
					Category = HighLevelCheckCategory.EventLogsAndRegistry,
					TargetName = "Аудит следов реестра",
					CoreOrType = "Registry Forensics",
					MatchedSignatures = string.Format("PCA: {0}, MRU: {1}", pcaCleanCount, mruCleanCount),
					TimeOrSize = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
					Verdict = "Чисто",
					RuleViolation = "ИНФО",
					Description = string.Format("Проверены ветки PCA Store/Persisted ({0}), OpenSavePidlMRU/RecentDocs/RunMRU ({1}) и Command Processor\\AutoRun: подозрительных записей нет.", pcaCleanCount, mruCleanCount),
					FullDetails = "Registry checks passed cleanly.",
					Severity = CheckSeverity.Info,
					IsViolation = false
				});
			}
		}

		private static void InspectRegistryTreeStrings(
			RegistryKey hive,
			string subKeyPath,
			int depth,
			List<HighLevelEvidenceItem> items,
			ref int totalScanned,
			ref int cleanCount)
		{
			if (depth > 2) return;
			try
			{
				using (RegistryKey key = hive.OpenSubKey(subKeyPath))
				{
					if (key == null) return;
					string[] vals = key.GetValueNames();
					for (int i = 0; i < vals.Length; i++)
					{
						if (vals[i].Equals("MRUListEx", StringComparison.OrdinalIgnoreCase) ||
							vals[i].Equals("MRUList", StringComparison.OrdinalIgnoreCase)) continue;
						totalScanned++;
						object val = key.GetValue(vals[i]);
						List<string> strings = ExtractStringsFromRegistryValue(val);
						bool flagged = false;
						for (int s = 0; s < strings.Count; s++)
						{
							string st = strings[s];
							string low = st.ToLowerInvariant();
							if (low.Contains("mccheatchecker") || low.Contains("screenshare") ||
								low.Contains("shell32.dll") || CheatDatabases.IsWhitelistedPath(low))
							{
								continue;
							}
							if (ContainsAnySystemDlc(low))
							{
								items.Add(MakeRegistryViolationItem(subKeyPath, st, "SystemDLC", "5.01", true));
								flagged = true;
								break;
							}
							string hit = FindBamThreatSignature(low, Path.GetFileName(low));
							if (!string.IsNullOrEmpty(hit))
							{
								items.Add(MakeRegistryViolationItem(subKeyPath, st, hit, "5.01 / 5.08", false));
								flagged = true;
								break;
							}
						}
						if (!flagged) cleanCount++;
					}

					string[] subs = key.GetSubKeyNames();
					for (int i = 0; i < subs.Length; i++)
					{
						InspectRegistryTreeStrings(hive, subKeyPath + "\\" + subs[i], depth + 1, items, ref totalScanned, ref cleanCount);
					}
				}
			}
			catch { }
		}

		private static void InspectCommandProcessorAutoRun(
			RegistryKey hive,
			string hiveName,
			List<HighLevelEvidenceItem> items,
			ref int totalScanned)
		{
			try
			{
				using (RegistryKey key = hive.OpenSubKey(@"Software\Microsoft\Command Processor"))
				{
					if (key == null) return;
					totalScanned++;
					object val = key.GetValue("AutoRun");
					if (val == null) return;
					string cmd = val.ToString().Trim();
					if (string.IsNullOrEmpty(cmd)) return;

					items.Add(new HighLevelEvidenceItem
					{
						SourceModule = "Реестр (" + hiveName + "\\...\\Command Processor)",
						Category = HighLevelCheckCategory.EventLogsAndRegistry,
						TargetName = "AutoRun",
						CoreOrType = "Persistence / Bypass",
						MatchedSignatures = Truncate(cmd, 80),
						TimeOrSize = "—",
						Verdict = "ОБНАРУЖЕН AUTORUN В CMD",
						RuleViolation = "5.08",
						Description = "Значение AutoRun в Command Processor подменяет/перехватывает команды cmd.exe: " + cmd,
						FullDetails = hiveName + @"\Software\Microsoft\Command Processor\AutoRun = " + cmd,
						Severity = CheckSeverity.Critical,
						IsViolation = true
					});
				}
			}
			catch { }
		}

		private static HighLevelEvidenceItem MakeRegistryViolationItem(
			string regSource,
			string detectedText,
			string matchedSig,
			string rule,
			bool isSystemDlc)
		{
			return new HighLevelEvidenceItem
			{
				SourceModule = "Реестр (" + regSource + ")",
				Category = isSystemDlc ? HighLevelCheckCategory.SystemDlc : HighLevelCheckCategory.EventLogsAndRegistry,
				TargetName = Path.GetFileName(detectedText.Trim()),
				CoreOrType = isSystemDlc ? "SystemDLC Trace" : "Registry Trace",
				MatchedSignatures = matchedSig,
				TimeOrSize = "—",
				Verdict = isSystemDlc ? "СЛЕД SYSTEMDLC В РЕЕСТРЕ" : "СЛЕД ЧИТА / ОЧИСТИТЕЛЯ В РЕЕСТРЕ",
				RuleViolation = rule,
				Description = string.Format("В реестре ({0}) обнаружена запись '{1}' (сигнатура: {2}).", regSource, Truncate(detectedText, 120), matchedSig),
				FullDetails = "Источник: " + regSource + "\r\nЗначение: " + detectedText,
				Severity = CheckSeverity.Critical,
				IsViolation = true
			};
		}

		#endregion

		#region 5. SystemDLC Deep Detector

		private static void ScanSystemDlcDeep(
			List<HighLevelEvidenceItem> items,
			bool includeInformational,
			string extraSystemDlcRoot,
			ref int totalScanned)
		{
			int initialViolations = items.Count;
			List<string> dirsToCheck = new List<string>();

			if (!string.IsNullOrEmpty(extraSystemDlcRoot))
			{
				dirsToCheck.Add(extraSystemDlcRoot);
			}

			try
			{
				DriveInfo[] drives = DriveInfo.GetDrives();
				for (int d = 0; d < drives.Length; d++)
				{
					if (!drives[d].IsReady || drives[d].DriveType != DriveType.Fixed) continue;
					string root = drives[d].RootDirectory.FullName;
					dirsToCheck.Add(Path.Combine(root, "SystemDLC"));
					dirsToCheck.Add(Path.Combine(root, ".systemdlc"));
				}
			}
			catch { }

			string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

			dirsToCheck.Add(Path.Combine(appData, "SystemDLC"));
			dirsToCheck.Add(Path.Combine(appData, ".systemdlc"));
			dirsToCheck.Add(Path.Combine(appData, "fun.systemdlc"));
			dirsToCheck.Add(Path.Combine(appData, "ru.systemdlc"));
			dirsToCheck.Add(Path.Combine(localAppData, "SystemDLC"));
			dirsToCheck.Add(Path.Combine(localAppData, ".systemdlc"));
			dirsToCheck.Add(Path.Combine(userProfile, "SystemDLC"));
			dirsToCheck.Add(Path.Combine(userProfile, ".systemdlc"));

			HashSet<string> checkedDirs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			for (int i = 0; i < dirsToCheck.Count; i++)
			{
				string dir = dirsToCheck[i];
				if (string.IsNullOrEmpty(dir) || !checkedDirs.Add(dir)) continue;
				ScanSystemDlcSingleDirectory(dir, items, ref totalScanned, false);
			}

			// User drop folders & Temp: files matching systemdlc / sysdlc
			string[] userSearchDirs = new string[]
			{
				Path.GetTempPath(),
				Path.Combine(userProfile, "Downloads"),
				Path.Combine(userProfile, "Desktop"),
				Path.Combine(userProfile, "Documents"),
				appData,
				localAppData,
				Path.Combine(appData, @".minecraft\config"),
				Path.Combine(appData, @"EvoVis\instance\mc\config")
			};
			for (int d = 0; d < userSearchDirs.Length; d++)
			{
				ScanFolderNamesForSystemDlc(userSearchDirs[d], items, ref totalScanned);
			}

			// Windows Prefetch SYSTEMDLC*.pf / *SYSDLC*.pf
			try
			{
				string pfDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Prefetch");
				if (Directory.Exists(pfDir))
				{
					totalScanned++;
					string[] pfFiles = Directory.GetFiles(pfDir, "*.pf");
					for (int p = 0; p < pfFiles.Length; p++)
					{
						string pfName = Path.GetFileName(pfFiles[p]).ToLowerInvariant();
						if (ContainsAnySystemDlc(pfName))
						{
							items.Add(new HighLevelEvidenceItem
							{
								SourceModule = "SystemDLC Prefetch",
								Category = HighLevelCheckCategory.SystemDlc,
								TargetName = Path.GetFileName(pfFiles[p]),
								CoreOrType = "Windows Prefetch (.pf)",
								MatchedSignatures = "Prefetch SYSTEMDLC",
								TimeOrSize = File.GetLastWriteTime(pfFiles[p]).ToString("dd.MM.yyyy HH:mm:ss"),
								Verdict = "ОБНАРУЖЕН СЛЕД ЗАПУСКА SYSTEMDLC",
								RuleViolation = "5.01",
								Description = "В Windows Prefetch обнаружен .pf-след запуска SystemDLC: " + pfFiles[p],
								FullDetails = pfFiles[p],
								Severity = CheckSeverity.Critical,
								IsViolation = true
							});
						}
					}
				}
			}
			catch { }

			// Running processes matching SystemDLC
			try
			{
				Process[] procs = Process.GetProcesses();
				for (int p = 0; p < procs.Length; p++)
				{
					try
					{
						string pn = (procs[p].ProcessName ?? "").ToLowerInvariant();
						if (ContainsAnySystemDlc(pn))
						{
							items.Add(new HighLevelEvidenceItem
							{
								SourceModule = "SystemDLC Process",
								Category = HighLevelCheckCategory.SystemDlc,
								TargetName = procs[p].ProcessName + " (PID " + procs[p].Id + ")",
								CoreOrType = "Running Process",
								MatchedSignatures = "systemdlc",
								TimeOrSize = DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss"),
								Verdict = "АКТИВНЫЙ ПРОЦЕСС SYSTEMDLC",
								RuleViolation = "5.01",
								Description = "В системе запущен процесс SystemDLC: " + procs[p].ProcessName,
								FullDetails = "PID: " + procs[p].Id,
								Severity = CheckSeverity.Critical,
								IsViolation = true
							});
						}
					}
					catch { }
				}
			}
			catch { }

			// SystemDLC dedicated Registry keys
			string[] sysDlcReg = new string[]
			{
				@"Software\SystemDLC",
				@"Software\fun.systemdlc",
				@"Software\ru.systemdlc"
			};
			for (int r = 0; r < sysDlcReg.Length; r++)
			{
				try
				{
					using (RegistryKey rk = Registry.CurrentUser.OpenSubKey(sysDlcReg[r]))
					{
						if (rk != null)
						{
							items.Add(MakeRegistryViolationItem("HKCU\\" + sysDlcReg[r], sysDlcReg[r], "SystemDLC", "5.01", true));
						}
					}
				}
				catch { }
			}

			int foundNew = items.Count - initialViolations;
			if (includeInformational && foundNew == 0)
			{
				items.Add(new HighLevelEvidenceItem
				{
					SourceModule = "SystemDLC Deep Detector",
					Category = HighLevelCheckCategory.SystemDlc,
					TargetName = "Проверка SystemDLC (папки / Prefetch / реестр / JAR / RAM)",
					CoreOrType = "Deep Audit",
					MatchedSignatures = "systemdlc, fun.systemdlc, ru.systemdlc, sysdlc",
					TimeOrSize = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
					Verdict = "Чисто",
					RuleViolation = "ИНФО",
					Description = "Папки C:\\SystemDLC, AppData\\SystemDLC, Prefetch SYSTEMDLC*.pf, реестр и конфиги чисты.",
					FullDetails = "SystemDLC deep check completed with 0 hits.",
					Severity = CheckSeverity.Info,
					IsViolation = false
				});
			}
		}

		private static void ScanSystemDlcSingleDirectory(
			string dirPath,
			List<HighLevelEvidenceItem> items,
			ref int totalScanned,
			bool inspectSubfiles)
		{
			try
			{
				totalScanned++;
				if (!Directory.Exists(dirPath)) return;

				string[] childFiles = new string[0];
				try { childFiles = Directory.GetFiles(dirPath, "*", SearchOption.TopDirectoryOnly); } catch { }

				items.Add(new HighLevelEvidenceItem
				{
					SourceModule = "SystemDLC Folder",
					Category = HighLevelCheckCategory.SystemDlc,
					TargetName = Path.GetFileName(dirPath.TrimEnd('\\')),
					CoreOrType = "Чит-клиент SystemDLC",
					MatchedSignatures = "Папка SystemDLC (" + childFiles.Length + " файлов)",
					TimeOrSize = Directory.GetLastWriteTime(dirPath).ToString("dd.MM.yyyy HH:mm:ss"),
					Verdict = "ОБНАРУЖЕНА ПАПКА SYSTEMDLC",
					RuleViolation = "5.01",
					Description = "На диске обнаружена директория чита SystemDLC: " + dirPath,
					FullDetails = "Путь: " + dirPath + "\r\nФайлов: " + childFiles.Length,
					Severity = CheckSeverity.Critical,
					IsViolation = true
				});

				if (inspectSubfiles)
				{
					int take = Math.Min(childFiles.Length, 10);
					for (int i = 0; i < take; i++)
					{
						totalScanned++;
					}
				}
			}
			catch { }
		}

		private static void ScanFolderNamesForSystemDlc(
			string dirPath,
			List<HighLevelEvidenceItem> items,
			ref int totalScanned)
		{
			try
			{
				if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return;

				string[] subDirs = Directory.GetDirectories(dirPath);
				for (int i = 0; i < subDirs.Length; i++)
				{
					string name = Path.GetFileName(subDirs[i]).ToLowerInvariant();
					if (ContainsAnySystemDlc(name) && !name.Contains("mccheatchecker"))
					{
						ScanSystemDlcSingleDirectory(subDirs[i], items, ref totalScanned, false);
					}
				}

				string[] files = Directory.GetFiles(dirPath, "*", SearchOption.TopDirectoryOnly);
				for (int i = 0; i < files.Length; i++)
				{
					totalScanned++;
					string fn = Path.GetFileName(files[i]);
					string low = fn.ToLowerInvariant();
					if (low.Contains("mccheatchecker") || low.Contains("verify_phase")) continue;
					if (ContainsAnySystemDlc(low))
					{
						items.Add(new HighLevelEvidenceItem
						{
							SourceModule = "SystemDLC Artifact",
							Category = HighLevelCheckCategory.SystemDlc,
							TargetName = fn,
							CoreOrType = "Файл / Архив SystemDLC",
							MatchedSignatures = "systemdlc",
							TimeOrSize = FormatFileSize(files[i]),
							Verdict = "ОБНАРУЖЕН ФАЙЛ SYSTEMDLC",
							RuleViolation = "5.01",
							Description = "Обнаружен файл с сигнатурой чита SystemDLC: " + files[i],
							FullDetails = files[i],
							Severity = CheckSeverity.Critical,
							IsViolation = true
						});
					}
				}
			}
			catch { }
		}

		#endregion

		#region Shared Helpers

		private static bool ContainsAnySystemDlc(string textLower)
		{
			if (string.IsNullOrEmpty(textLower)) return false;
			for (int i = 0; i < SystemDlcKeywords.Length; i++)
			{
				if (ContainsWordToken(textLower, SystemDlcKeywords[i])) return true;
			}
			return false;
		}

		private static bool ContainsIdentifierToken(string text, string token)
		{
			if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(token)) return false;
			int idx = 0;
			while ((idx = text.IndexOf(token, idx, StringComparison.Ordinal)) >= 0)
			{
				bool leftOk = (idx == 0) || !IsIdentChar(text[idx - 1]);
				int end = idx + token.Length;
				bool rightOk = (end >= text.Length) || !IsIdentChar(text[end]);
				if (leftOk && rightOk) return true;
				idx = idx + 1;
			}
			return false;
		}

		private static bool IsExactOrCallIdentifier(string tok, string ident)
		{
			if (tok == ident || tok == "." + ident || tok == ident + "()") return true;
			return ContainsIdentifierToken(tok, ident);
		}

		private static bool ContainsWordToken(string lowerText, string lowerToken)
		{
			if (string.IsNullOrEmpty(lowerText) || string.IsNullOrEmpty(lowerToken)) return false;
			int idx = 0;
			while ((idx = lowerText.IndexOf(lowerToken, idx, StringComparison.Ordinal)) >= 0)
			{
				bool leftOk = (idx == 0) || !IsWordChar(lowerText[idx - 1]);
				int end = idx + lowerToken.Length;
				bool rightOk = (end >= lowerText.Length) || !IsWordChar(lowerText[end]);
				if (leftOk && rightOk) return true;
				idx = idx + 1;
			}
			return false;
		}

		private static bool IsIdentChar(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '_';
		}

		private static bool IsWordChar(char c)
		{
			return (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9');
		}

		private static List<string> CombineDistinct(HashSet<string> first, HashSet<string> second)
		{
			HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
			List<string> list = new List<string>();
			foreach (string s in first)
			{
				if (seen.Add(s)) list.Add(s);
			}
			foreach (string s in second)
			{
				if (seen.Add(s)) list.Add(s);
			}
			return list;
		}

		private static string[] DeduplicateTake(List<string> src, int max)
		{
			HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
			List<string> list = new List<string>();
			for (int i = 0; i < src.Count && list.Count < max; i++)
			{
				if (seen.Add(src[i])) list.Add(src[i]);
			}
			return list.ToArray();
		}

		private static string ReadZipEntryText(ZipArchiveEntry entry, int maxBytes)
		{
			byte[] raw = ReadZipEntryBytes(entry, maxBytes);
			if (raw == null || raw.Length == 0) return "";
			return Encoding.UTF8.GetString(raw);
		}

		private static byte[] ReadZipEntryBytes(ZipArchiveEntry entry, int maxBytes)
		{
			try
			{
				using (Stream s = entry.Open())
				using (MemoryStream ms = new MemoryStream())
				{
					byte[] buf = new byte[16384];
					int total = 0;
					int read;
					while (total < maxBytes && (read = s.Read(buf, 0, Math.Min(buf.Length, maxBytes - total))) > 0)
					{
						ms.Write(buf, 0, read);
						total += read;
					}
					return ms.ToArray();
				}
			}
			catch { return null; }
		}

		private static string DecodeAsciiPrintableAndNullSpaced(byte[] data)
		{
			char[] chars = new char[data.Length];
			for (int i = 0; i < data.Length; i++)
			{
				byte b = data[i];
				chars[i] = (b >= 0x20 && b <= 0x7E) ? (char)b : '\n';
			}
			return new string(chars);
		}

		private static string DecodeUtf16LeSafe(byte[] data)
		{
			int len = data.Length / 2;
			char[] chars = new char[len];
			for (int i = 0; i < len; i++)
			{
				ushort u = (ushort)(data[i * 2] | (data[i * 2 + 1] << 8));
				chars[i] = (u >= 0x20 && u <= 0x7E) ? (char)u : '\n';
			}
			return new string(chars);
		}

		private static List<string> ExtractStringsFromRegistryValue(object val)
		{
			List<string> result = new List<string>();
			if (val == null) return result;
			if (val is string)
			{
				string s = ((string)val).Trim();
				if (s.Length >= 3) result.Add(s);
				return result;
			}
			if (val is string[])
			{
				string[] arr = (string[])val;
				for (int i = 0; i < arr.Length; i++)
				{
					if (!string.IsNullOrEmpty(arr[i])) result.Add(arr[i].Trim());
				}
				return result;
			}
			if (val is byte[])
			{
				byte[] bytes = (byte[])val;
				ExtractRunsFromBytes(bytes, result);
			}
			return result;
		}

		private static void ExtractRunsFromBytes(byte[] buf, List<string> output)
		{
			StringBuilder ascii = new StringBuilder();
			for (int i = 0; i < buf.Length; i++)
			{
				byte b = buf[i];
				if (b >= 0x20 && b <= 0x7E) ascii.Append((char)b);
				else
				{
					if (ascii.Length >= 4) output.Add(ascii.ToString());
					ascii.Length = 0;
				}
			}
			if (ascii.Length >= 4) output.Add(ascii.ToString());

			for (int parity = 0; parity <= 1; parity++)
			{
				StringBuilder uni = new StringBuilder();
				for (int i = parity; i + 1 < buf.Length; i += 2)
				{
					ushort u = (ushort)(buf[i] | (buf[i + 1] << 8));
					if ((u >= 0x20 && u <= 0x7E) || (u >= 0x0410 && u <= 0x044F)) uni.Append((char)u);
					else
					{
						if (uni.Length >= 4) output.Add(uni.ToString());
						uni.Length = 0;
					}
				}
				if (uni.Length >= 4) output.Add(uni.ToString());
			}
		}

		private static bool ContainsAnyWipeCommand(string textLower)
		{
			if (string.IsNullOrEmpty(textLower)) return false;
			for (int i = 0; i < WipingCliMarkers.Length; i++)
			{
				if (textLower.Contains(WipingCliMarkers[i])) return true;
			}
			return false;
		}

		private static bool IsFileAuthenticodeSigned(string filePath)
		{
			if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return false;
			try
			{
				X509Certificate rawCert = X509Certificate.CreateFromSignedFile(filePath);
				if (rawCert == null) return false;
				X509Certificate2 cert2 = new X509Certificate2(rawCert);
				string subj = ((cert2.Subject ?? "") + " " + (cert2.Issuer ?? "")).ToLowerInvariant();
				if (subj.Contains("test signing") || subj.Contains("whql test") || subj.Contains("cheatengine"))
				{
					return false;
				}
				X509Chain chain = new X509Chain();
				chain.ChainPolicy.RevocationMode = X509RevocationMode.NoCheck;
				chain.ChainPolicy.VerificationFlags = X509VerificationFlags.IgnoreNotTimeValid;
				return chain.Build(cert2);
			}
			catch
			{
				return false;
			}
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
				long len = new FileInfo(path).Length;
				if (len >= 1048576L) return string.Format("{0:F2} МБ", len / 1048576.0);
				return string.Format("{0:F1} КБ", len / 1024.0);
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
