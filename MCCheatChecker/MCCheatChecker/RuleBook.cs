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
			Map[BanCase.RefuseCheck] = new RuleInfo("30d", "5.08 - Отказ от проверки / выход AnyDesk, RuDesktop или Discord");
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
