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

			// --- ОБНОВЛЁННЫЙ РЕГЛАМЕНТ РАЗДЕЛА 5 ---
			Map[BanCase.Allowed] = new RuleInfo("0d", "Разрешено регламентом (автокликеры и легитимные утилиты)");
			Map[BanCase.CheatClients] = new RuleInfo("30d", "5.01 - Чит-клиенты, инжекты и чит-модули (Catlavan, Celestial, Delta, DoomsDay, Expensive, Nursultan, Vape, Wexside, Wurst и аналоги)");
			Map[BanCase.CombatAdvantage] = new RuleInfo("30d", "5.02 - Преимущество в бою (AimBot, KillAura, Reach, TriggerBot, Хитбоксы, Velocity, AutoCrystal, AutoCart и аналоги)");
			Map[BanCase.Automation] = new RuleInfo("30d", "5.03 - Автоматизация действий (AutoArmor, AutoEat, AutoTotem, ChestStealer, InvProfilesNext, ItemScroller, MouseTweaks, макросы)");
			Map[BanCase.MovementAndBlocks] = new RuleInfo("30d", "5.04 - Передвижение и работа с блоками (Baritone, Fly, ElytraFly, Jesus, NoFall, Scaffold, Speed, Timer, Tweakeroo)");
			Map[BanCase.ExtraInfoAndVision] = new RuleInfo("15d", "5.05 - Лишняя информация и обзор (ESP, Freecam, Chams, Tracers, PlayerRadar, HealthIndicator, NBT Tooltip)");
			Map[BanCase.BaseAndOreSearch] = new RuleInfo("30d", "5.06 - Поиск баз, руды и сида (X-Ray, SeedCrackerX, SeedMapper, CaveFinder, ChestLocator, Farsight)");
			Map[BanCase.InvisibleBlocksAndEffects] = new RuleInfo("15d", "5.07 - Невидимые блоки и эффекты (DarknessBegone, ICU, NoBlindness, True Sight, VisibleBarriers, RemoveWardenEffect)");
			Map[BanCase.Important] = new RuleInfo("30d", "5.08 - Нарушение регламента модов / аналоги запрещённого ПО");

			// Совместимость с легаси вызовами
			Map[BanCase.CheatUsage] = Map[BanCase.CheatClients];
			Map[BanCase.CheatStorage] = new RuleInfo("30d", "5.01, 5.08 - Хранение чит-клиентов / стороннего ПО");
			Map[BanCase.CheatTraces] = new RuleInfo("30d", "5.01, 5.08 - Следы использования стороннего ПО");
			Map[BanCase.Visuals] = Map[BanCase.ExtraInfoAndVision];
			Map[BanCase.Macro] = Map[BanCase.Automation];
			Map[BanCase.Confession] = new RuleInfo("15d", "5.01, 5.08 - Признание в использовании стороннего ПО");
			Map[BanCase.RefuseCheck] = new RuleInfo("30d", "5.08 - Отказ от проверки / выход AnyDesk, RuDesktop, RustDesk, UltraViewer или Discord");
			Map[BanCase.ObstructCheck] = new RuleInfo("30d", "5.08 - Препятствие проверке / попытка скрыть улики");
			Map[BanCase.ServicesDisabled] = new RuleInfo("30d", "5.08 - Отключение обязательных системных служб Windows");
			Map[BanCase.NetworkRegistry] = new RuleInfo("30d", "5.08 - Скрытые настройки сети / подмена реестра");
			Map[BanCase.BugAbuse] = new RuleInfo("30d", "2.06 - Использование багов игры / дюпы");
			Map[BanCase.OldWindows] = new RuleInfo("14d", "Windows установлена менее 14 дней назад");
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

		public static BanCase Classify(string text)
		{
			if (string.IsNullOrEmpty(text))
			{
				return BanCase.Allowed;
			}
			string t = text.ToLowerInvariant();
			if (t.Contains("разрешен") || t.Contains("разрешён") ||
			    t.Contains("проги для проверки") || t.Contains("mccheat") ||
			    t.Contains("screenshare") || t.Contains("sstools") ||
			    t.Contains("autoclicker") || t.Contains("автокликер") ||
			    t.Contains("tinytask") || t.Contains("speedautoclicker") ||
			    t.Contains("opautoclicker") || t.Contains("packages") ||
			    t.Contains("start_windows.bat") || t.Contains("clicker") ||
			    t.Contains("x-mouse") || t.Contains("xmouse") ||
			    t.Contains("botmek") || t.Contains("keyran") ||
			    t.Contains("jnativehook") || t.Contains("autohotkey") || t.Contains("ahk") || t.Contains("gsautoclicker") || t.Contains("clickermann"))
			{
				return BanCase.Allowed;
			}

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
			    t.Contains("kwish") || t.Contains("richter") || t.Contains("deko") || t.Contains("avolon") || t.Contains("hacks") || t.Contains("чит-клиент") || t.Contains("инжект") || t.Contains("telegram") || t.Contains("discord вложение"))
			{
				return BanCase.CheatClients;
			}

			if (t.Contains("5.02") || t.Contains("aimbot") || t.Contains("killaura") || t.Contains("reach") ||
			    t.Contains("triggerbot") || t.Contains("hitbox") || t.Contains("velocity") || t.Contains("antikb") ||
			    t.Contains("autocrystal") || t.Contains("autocart") || t.Contains("bowboost") || t.Contains("criticals") ||
			    t.Contains("cleancut") || t.Contains("throughgrass") || t.Contains("shieldbreaker") || t.Contains("quickturn") ||
			    t.Contains("smoothaim") || t.Contains("silentaim") || t.Contains("targetstrafe") || t.Contains("clickaura"))
			{
				return BanCase.CombatAdvantage;
			}

			if (t.Contains("5.03") || t.Contains("autoarmor") || t.Contains("autoeat") || t.Contains("autototem") ||
			    t.Contains("cheststealer") || t.Contains("quickswap") || t.Contains("jsmacros") ||
			    t.Contains("itemscroller") || t.Contains("mousetweaks") ||
			    t.Contains("fastbow") || t.Contains("autofish"))
			{
				return BanCase.Automation;
			}

			if (t.Contains("5.04") || t.Contains("baritone") || t.Contains("elytrafly") || t.Contains("fastplace") ||
			    t.Contains("fly") || t.Contains("ghostblock") || t.Contains("invmove") || t.Contains("jesus") ||
			    t.Contains("nofall") || t.Contains("noslow") || t.Contains("nuker") || t.Contains("scaffold") ||
			    t.Contains("speedhack") || t.Contains("spider") || t.Contains("timer") || t.Contains("tweakeroo") ||
			    t.Contains("airjump") || t.Contains("highjump") || t.Contains("step") || t.Contains("longjump"))
			{
				return BanCase.MovementAndBlocks;
			}

			if (t.Contains("5.05") || t.Contains("esp") || t.Contains("chams") || t.Contains("freecam") ||
			    t.Contains("tracers") || t.Contains("playerradar") || t.Contains("healthindicator") ||
			    t.Contains("armorindicator") || t.Contains("nbt") || t.Contains("friendhighlighter") || t.Contains("piechart") ||
			    t.Contains("nametags") || t.Contains("fullbright") || t.Contains("radar") || t.Contains("chestesp") || t.Contains("overlay") || t.Contains("оверлей"))
			{
				return BanCase.ExtraInfoAndVision;
			}

			if (t.Contains("5.06") || t.Contains("xray") || t.Contains("x-ray") || t.Contains("seedcracker") ||
			    t.Contains("seedmapper") || t.Contains("cavefinder") || t.Contains("chestlocator") ||
			    t.Contains("diamondscanner") || t.Contains("farsight") || t.Contains("orescanner") || t.Contains("dungeonfinder"))
			{
				return BanCase.BaseAndOreSearch;
			}

			if (t.Contains("5.07") || t.Contains("darkness") || t.Contains("noblindness") || t.Contains("icu") ||
			    t.Contains("visiblebarriers") || t.Contains("truesight") || t.Contains("removewarden") ||
			    t.Contains("antiblind") || t.Contains("removefog") || t.Contains("nofog"))
			{
				return BanCase.InvisibleBlocksAndEffects;
			}

			if (t.Contains("bleachbit") || t.Contains("privazer") || t.Contains("ccleaner") || t.Contains("sdelete") ||
			    t.Contains("usn deletejournal") || t.Contains("prefetch wiped") || t.Contains("journal wiped") ||
			    t.Contains("wevtutil cl") || t.Contains("затиран") || t.Contains("зачистк") || t.Contains("отказ от проверки"))
			{
				return BanCase.ObstructCheck;
			}

			if (t.Contains("$r payload") || t.Contains("тело чита") || t.Contains("эталонный размер") || t.Contains("встроенный чит"))
			{
				return BanCase.CheatClients;
			}
			if ((t.Contains("trace") || t.Contains("след") || t.Contains("удален") || t.Contains("recycle") || t.Contains("trash")) &&
			    (t.Contains("чит") || t.Contains("cheat") || t.Contains("клиент") || t.Contains("взлом")))
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
								DateTime dt = DateTime.FromFileTimeUtc(ft).ToLocalTime();
								double days = (DateTime.Now - dt).TotalDays;
								return (days < 0.0) ? 0.0 : days;
							}
						}
						catch
						{
						}
					}
					object value = registryKey.GetValue("InstallDate");
					if (value == null)
					{
						return -1.0;
					}
					long num = Convert.ToInt64(value);
					DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(num).ToLocalTime();
					double totalDays = (DateTime.Now - dateTime).TotalDays;
					return (totalDays < 0.0) ? 0.0 : totalDays;
				}
			}
			catch
			{
				return -1.0;
			}
		}

		public static bool OldWindowsApplies()
		{
			double num = WindowsAgeDays();
			if (num >= 0.0)
			{
				return num < 14.0;
			}
			return false;
		}

		public static int OldWindowsDurationDays(double ageDays)
		{
			if (ageDays < 0.0 || ageDays >= 14.0)
			{
				return 0;
			}
			int num = (int)Math.Ceiling(14.0 - ageDays);
			if (num >= 1)
			{
				return num;
			}
			return 1;
		}

		public static string OldWindowsNote()
		{
			double num = WindowsAgeDays();
			if (num < 0.0)
			{
				return "Возраст установки Windows не удалось определить по реестру (п. 5.03).";
			}
			if (num >= 14.0)
			{
				return "П. 5.03 не применяется: Windows установлена " + Math.Round(num, 1) + " дн. назад (срок более 14 дней). Игрок чист.";
			}
			int num2 = OldWindowsDurationDays(num);
			return "П. 5.03: Windows установлена " + Math.Round(num, 1) + " дн. назад. Срок бана: " + num2 + " дн. (14 - возраст).";
		}

		public static string CommandOldWindows(string player)
		{
			double num = WindowsAgeDays();
			if (num < 0.0)
			{
				return Build(player, "14d", Reason(BanCase.OldWindows) + " (возраст не удалось определить)");
			}
			if (num >= 14.0)
			{
				return string.Empty;
			}
			int num2 = OldWindowsDurationDays(num);
			string reason = "5.03 - Windows установлена " + Math.Round(num, 1) + " дн. назад (срок = 14 - возраст)";
			return Build(player, num2 + "d", reason);
		}
	}
}
