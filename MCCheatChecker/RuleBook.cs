using System;
using System.Collections.Generic;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public static class RuleBook
	{
		public sealed class RuleInfo
		{
			public readonly string Duration;
			public readonly string Reason;

			public RuleInfo(string duration, string reason)
			{
				Duration = duration;
				Reason = reason;
			}
		}

		private static readonly Dictionary<BanCase, RuleInfo> Map;

		static RuleBook()
		{
			Map = new Dictionary<BanCase, RuleInfo>();

			// --- ОБНОВЛЁННЫЙ КРАТКИЙ РЕГЛАМЕНТ РАЗДЕЛА 5 ---
			Map[BanCase.Allowed] = new RuleInfo("0d", "Разрешено регламентом");
			Map[BanCase.CheatClients] = new RuleInfo("30d", "5.01 - Чит-клиенты");
			Map[BanCase.CombatAdvantage] = new RuleInfo("30d", "5.02 - Преимущество в бою");
			Map[BanCase.Automation] = new RuleInfo("30d", "5.03 - Автоматизация действий");
			Map[BanCase.MovementAndBlocks] = new RuleInfo("30d", "5.04 - Передвижение и блоки");
			Map[BanCase.ExtraInfoAndVision] = new RuleInfo("30d", "5.05 - Лишняя информация и обзор");
			Map[BanCase.BaseAndOreSearch] = new RuleInfo("30d", "5.06 - Поиск баз, руды и сида");
			Map[BanCase.InvisibleBlocksAndEffects] = new RuleInfo("30d", "5.07 - Скрытые блоки и эффекты");
			Map[BanCase.Important] = new RuleInfo("30d", "5.08 - Следы стороннего ПО");

			// Совместимость с вызовами
			Map[BanCase.CheatUsage] = Map[BanCase.CheatClients];
			Map[BanCase.CheatStorage] = new RuleInfo("30d", "5.08 - Хранение читов");
			Map[BanCase.CheatTraces] = new RuleInfo("30d", "5.08 - Следы стороннего ПО");
			Map[BanCase.Visuals] = Map[BanCase.ExtraInfoAndVision];
			Map[BanCase.Macro] = Map[BanCase.Automation];
			Map[BanCase.Confession] = new RuleInfo("15d", "5.08 - Признание");
			Map[BanCase.RefuseCheck] = new RuleInfo("30d", "5.08 - Отказ от проверки");
			Map[BanCase.ObstructCheck] = new RuleInfo("30d", "5.08 - Препятствие проверке");
			Map[BanCase.ServicesDisabled] = new RuleInfo("30d", "5.08 - Отключение служб");
			Map[BanCase.NetworkRegistry] = new RuleInfo("30d", "5.08 - Скрытые настройки сети");
			Map[BanCase.BugAbuse] = new RuleInfo("30d", "2.06 - Багоюз");
			Map[BanCase.OldWindows] = new RuleInfo("14d", "Windows < 14 дней");
		}

		public static RuleInfo Get(BanCase c)
		{
			RuleInfo value;
			if (!Map.TryGetValue(c, out value))
			{
				return Map[BanCase.CheatUsage];
			}
			return value;
		}

		public static string Reason(BanCase c)
		{
			return Get(c).Reason;
		}

		public static string Duration(BanCase c)
		{
			return Get(c).Duration;
		}

		public static string DurationAndReason(BanCase c)
		{
			return Duration(c) + " " + Reason(c);
		}

		public static string Command(BanCase c, string player)
		{
			return Build(player, Duration(c), Reason(c));
		}

		public static string Command(BanCase c, string player, string details)
		{
			string text = Reason(c);
			if (!string.IsNullOrEmpty(details))
			{
				text = text + ": " + details;
			}
			return Build(player, Duration(c), text);
		}

		public static string Build(string player, string duration, string reason)
		{
			if (duration == "0d" || (reason != null && reason.Contains("Разрешено регламентом")))
			{
				return string.Empty;
			}
			if (string.IsNullOrEmpty(player))
			{
				player = "Player";
			}
			if (string.IsNullOrEmpty(duration))
			{
				duration = "30d";
			}
			return "/ban " + player + " " + duration + " " + reason;
		}

		public static BanCase Classify(string text, CheckSeverity severity = CheckSeverity.Info)
		{
			if (string.IsNullOrEmpty(text))
			{
				return (severity == CheckSeverity.Critical) ? BanCase.CheatClients : BanCase.Allowed;
			}
			string t = text.ToLowerInvariant();

			// 1. Полностью разрешённые регламентом инструменты:
			// Автокликеры, утилиты проверки, разрешённые CPVP-оптимизаторы, AnyDesk и миникарты
			if (t.Contains("разрешен") || t.Contains("разрешён") ||
			    t.Contains("проги для проверки") || t.Contains("mccheat") ||
			    t.Contains("screenshare") || t.Contains("sstools") ||
			    t.Contains("autoclicker") || t.Contains("автокликер") ||
			    t.Contains("tinytask") || t.Contains("speedautoclicker") ||
			    t.Contains("opautoclicker") || t.Contains("packages") ||
			    t.Contains("start_windows.bat") || t.Contains("clicker") ||
			    t.Contains("x-mouse") || t.Contains("xmouse") ||
			    t.Contains("botmek") || t.Contains("keyran") ||
			    t.Contains("jnativehook") || t.Contains("autohotkey") || t.Contains("ahk") || t.Contains("gsautoclicker") || t.Contains("clickermann") ||
			    t.Contains("marlow") || t.Contains("haram") || t.Contains("end crystal optimizer") || t.Contains("endcrystaloptimizer") ||
			    t.Contains("walksy") || t.Contains("herosanchor") || t.Contains("hero's anchor") || t.Contains("cutebow") ||
			    t.Contains("journeymap") || t.Contains("voxelmap") || t.Contains("xaero") ||
			    t.Contains("anydesk") || t.Contains("удалённого доступа"))
			{
				return BanCase.Allowed;
			}

			// 5.01 - Чит-клиенты и инжекты
			if (t.Contains("5.01") || t.Contains("expensive") || t.Contains("nursultan") || t.Contains("celestial") ||
			    t.Contains("wexside") || t.Contains("akrien") || t.Contains("doomsday") || t.Contains("vape") ||
			    t.Contains("wurst") || t.Contains("meteor") || t.Contains("liquidbounce") || t.Contains("thunderhack") ||
			    t.Contains("drip") || t.Contains("catlavan") || t.Contains("catlean") || t.Contains("exloader") ||
			    t.Contains("systemdlc") || t.Contains("deadcode") || t.Contains("richpremium") || t.Contains("delta") ||
			    t.Contains("flint") || t.Contains("kura") || t.Contains("boze") || t.Contains("slinky") || t.Contains("sigma") ||
			    t.Contains("minced") || t.Contains("wild") || t.Contains("fluger") || t.Contains("zamorozka") ||
			    t.Contains("boberware") || t.Contains("celka") || t.Contains("rockstar") || t.Contains("avalone") ||
			    t.Contains("avolon") || t.Contains("norules") || t.Contains("archware") || t.Contains("future") ||
			    t.Contains("rusherhack") || t.Contains("pyro") || t.Contains("aristois") || t.Contains("impact") ||
			    t.Contains("inertia") || t.Contains("bleachhack") || t.Contains("ares") || t.Contains("salhack") ||
			    t.Contains("kami") || t.Contains("kamiblue") || t.Contains("matix") || t.Contains("nodus") ||
			    t.Contains("huzuni") || t.Contains("jigsaw") || t.Contains("itami") || t.Contains("bape") ||
			    t.Contains("whiteout") || t.Contains("dreamclient") || t.Contains("raven") || t.Contains("coffeeclient") ||
			    t.Contains("pandora") || t.Contains("horion") || t.Contains("zephyr") || t.Contains("prax") ||
			    t.Contains("dekoclient") || t.Contains("vegaline") || t.Contains("meriada") || t.Contains("zenware") ||
			    t.Contains("kwish") || t.Contains("richter") || t.Contains("deko") || t.Contains("hacks") || t.Contains("чит-клиент") || t.Contains("инжект") ||
			    t.Contains("cortex") || t.Contains("stubborn.website") || t.Contains("radioegor146") || t.Contains("by.kayn") || t.Contains("zdcoder") || t.Contains("magicthein") || t.Contains("baobab") || t.Contains("forge.commons") || t.Contains("dreampool") || t.Contains("process hacker"))
			{
				return BanCase.CheatClients;
			}

			// 5.02 - Преимущество в бою
			if (t.Contains("5.02") || t.Contains("aimbot") || t.Contains("killaura") || t.Contains("reach") ||
			    t.Contains("triggerbot") || t.Contains("hitbox") || t.Contains("velocity") || t.Contains("antikb") ||
			    t.Contains("autocrystal") || t.Contains("autocart") || t.Contains("bowboost") || t.Contains("bow boost") || t.Contains("criticals") ||
			    t.Contains("cleancut") || t.Contains("throughgrass") || t.Contains("attackthroughgrass") || t.Contains("shieldbreaker") || t.Contains("quickturn") ||
			    t.Contains("smoothaim") || t.Contains("silentaim") || t.Contains("targetstrafe") || t.Contains("clickaura") || t.Contains("nodelay") || t.Contains("pearltrajectories"))
			{
				return BanCase.CombatAdvantage;
			}

			// 5.03 - Автоматизация действий
			if (t.Contains("5.03") || t.Contains("autoarmor") || t.Contains("autoeat") || t.Contains("autototem") ||
			    t.Contains("cheststealer") || t.Contains("quickswap") || t.Contains("jsmacros") ||
			    t.Contains("inventory profiles next") || t.Contains("inventoryprofilesnext") || t.Contains("invprofilesnext") ||
			    t.Contains("itemscroller") || t.Contains("item scroller") ||
			    t.Contains("mousetweaks") || t.Contains("mouse tweaks") ||
			    t.Contains("fastbow") || t.Contains("autofish"))
			{
				return BanCase.Automation;
			}

			// 5.04 - Передвижение и работа с блоками
			if (t.Contains("5.04") || t.Contains("baritone") || t.Contains("elytrafly") || t.Contains("fastplace") ||
			    t.Contains("fly") || t.Contains("ghostblock") || t.Contains("invmove") || t.Contains("jesus") ||
			    t.Contains("nofall") || t.Contains("noslow") || t.Contains("noslowdown") || t.Contains("nuker") || t.Contains("scaffold") ||
			    t.Contains("speedhack") || t.Contains("spider") || t.Contains("timer") || t.Contains("tweakeroo") ||
			    t.Contains("bedrockbreaker") || t.Contains("airjump") || t.Contains("highjump") || t.Contains("step") || t.Contains("longjump"))
			{
				return BanCase.MovementAndBlocks;
			}

			// 5.05 - Лишняя информация и обзор
			if (t.Contains("5.05") || t.Contains("esp") || t.Contains("chams") || t.Contains("freecam") ||
			    t.Contains("tracers") || t.Contains("playerradar") || t.Contains("healthindicator") ||
			    t.Contains("armorindicator") || t.Contains("nbt tooltip") || t.Contains("nbttooltip") || t.Contains("nbt") ||
			    t.Contains("friendhighlighter") || t.Contains("piechartradar") || t.Contains("piechart") || t.Contains("nearbyalert") ||
			    t.Contains("tntcartalert") || t.Contains("nametags") || t.Contains("fullbright") || t.Contains("radar") || t.Contains("chestesp") || t.Contains("overlay") || t.Contains("оверлей"))
			{
				return BanCase.ExtraInfoAndVision;
			}

			// 5.06 - Поиск баз, руды и сида
			if (t.Contains("5.06") || t.Contains("xray") || t.Contains("x-ray") || t.Contains("seedcrackerx") || t.Contains("seedcracker") ||
			    t.Contains("seedmapper") || t.Contains("cavefinder") || t.Contains("chestlocator") ||
			    t.Contains("diamondscanner") || t.Contains("farsight") || t.Contains("item tracker") || t.Contains("itemtracker") ||
			    t.Contains("orescanner") || t.Contains("dungeonfinder"))
			{
				return BanCase.BaseAndOreSearch;
			}

			// 5.07 - Невидимые блоки и эффекты
			if (t.Contains("5.07") || t.Contains("darknessbegone") || t.Contains("darkness") || t.Contains("noblindness") || t.Contains("nodarkness") ||
			    t.Contains("icu") || t.Contains("visiblebarriers") || t.Contains("truesight") || t.Contains("true sight") ||
			    t.Contains("removewardeneffect") || t.Contains("removewarden") || t.Contains("antiblind") || t.Contains("removefog") || t.Contains("nofog"))
			{
				return BanCase.InvisibleBlocksAndEffects;
			}

			// 5.08 - Следы зачистки, удалённый софт, отключение служб, отказ от проверки
			if (t.Contains("5.08") || t.Contains("bleachbit") || t.Contains("privazer") || t.Contains("ccleaner") || t.Contains("sdelete") ||
			    t.Contains("powershell") || t.Contains("event id 400") || t.Contains("скрытая команда") || t.Contains("cleandetecting") || t.Contains("meow") || t.Contains("sstool") || t.Contains("y_check") ||
			    t.Contains("journaltrace") || t.Contains("usn deletejournal") || t.Contains("prefetch wiped") || t.Contains("journal wiped") ||
			    t.Contains("wevtutil") || t.Contains("fsutil") || t.Contains("затиран") || t.Contains("зачистк") ||
			    t.Contains("отказ от проверки") || t.Contains("скрыть улики") || t.Contains("клинер") || t.Contains("чистилк") ||
			    t.Contains("отключен") || t.Contains("остановлен") || t.Contains("pcasvc") || t.Contains("diagtrack") ||
			    t.Contains("dps") || t.Contains("sysmain") || t.Contains("eventlog") || t.Contains("служба остановлена") ||
			    t.Contains("hosts") || t.Contains("блокировк античит") || t.Contains("windivert") || t.Contains("clumsy"))
			{
				return BanCase.ObstructCheck;
			}

			if (t.Contains("$r payload") || t.Contains("тело чита") || t.Contains("эталонный размер") || t.Contains("встроенный чит"))
			{
				return BanCase.CheatClients;
			}
			if (t.Contains("trace") || t.Contains("след") || t.Contains("удален") || t.Contains("recycle") || t.Contains("trash") || t.Contains("корзин"))
			{
				return BanCase.CheatTraces;
			}

			if (severity == CheckSeverity.Critical)
			{
				return BanCase.CheatClients;
			}
			if (severity == CheckSeverity.NeedsCheck)
			{
				return BanCase.ExtraInfoAndVision;
			}
			if (severity == CheckSeverity.Warning)
			{
				return BanCase.CheatTraces;
			}

			return BanCase.Allowed;
		}

		public static string FormatBanCommand(string player, string findingText)
		{
			BanCase bc = Classify(findingText);
			if (bc == BanCase.Allowed)
			{
				return string.Empty;
			}
			RuleInfo ri = Get(bc);
			return Build(player, ri.Duration, ri.Reason);
		}

		public static double WindowsAgeDays()
		{
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows NT\\CurrentVersion"))
				{
					if (registryKey == null)
					{
						return -1.0;
					}
					object installTime = registryKey.GetValue("InstallTime");
					if (installTime != null)
					{
						try
						{
							long ft = Convert.ToInt64(installTime);
							if (ft > 10000000000000000L)
							{
								DateTime dt = DateTime.FromFileTimeUtc(ft);
								return (DateTime.UtcNow - dt).TotalDays;
							}
							DateTime dt2 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds((double)ft);
							return (DateTime.UtcNow - dt2).TotalDays;
						}
						catch
						{
						}
					}
					object installDate = registryKey.GetValue("InstallDate");
					if (installDate != null)
					{
						try
						{
							long s = Convert.ToInt64(installDate);
							DateTime dt3 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds((double)s);
							return (DateTime.UtcNow - dt3).TotalDays;
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
			return -1.0;
		}

		public static bool OldWindowsApplies()
		{
			double days = WindowsAgeDays();
			return days >= 0.0 && days < 14.0;
		}

		public static string OldWindowsNote()
		{
			double days = WindowsAgeDays();
			if (days < 0.0)
			{
				return "Не удалось определить дату установки Windows.";
			}
			if (days >= 14.0)
			{
				return string.Format("Windows установлена {0:F1} дн. назад (требуется >= 14 дней).", days);
			}
			int banDays = Math.Max(1, (int)Math.Ceiling(14.0 - days));
			return string.Format("Windows установлена {0:F1} дн. назад (< 14 дн.). Срок бана: {1}d.", days, banDays);
		}

		public static string CommandOldWindows(string player)
		{
			double days = WindowsAgeDays();
			int banDays = (days >= 0.0 && days < 14.0) ? Math.Max(1, (int)Math.Ceiling(14.0 - days)) : 14;
			if (string.IsNullOrEmpty(player)) player = "Player";
			return "/ban " + player + " " + banDays + "d 5.08 - Windows < 14 дней";
		}
	}
}
