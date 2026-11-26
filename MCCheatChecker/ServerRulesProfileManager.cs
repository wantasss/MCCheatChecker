using System;
using System.Collections.Generic;

namespace MCCheatChecker
{
	public enum ServerProfileType
	{
		AtlantisCpvp,
		StandardDefault,
		HolyWorld,
		ReallyWorld
	}

	public class ServerProfileInfo
	{
		public ServerProfileType ProfileType;
		public string DisplayName;
		public string Description;
		public List<string> WhitelistedCpvpMods;
		public List<string> WhitelistedMinimaps;
		public List<string> ProhibitedCheatKeywords;
		public bool AllowEndCrystalOptimizer;
		public bool AllowMarlowOptimizer;
		public bool AllowWalksyAnchor;
		public bool AllowCuteBow;
	}

	public static class ServerRulesProfileManager
	{
		private static ServerProfileType currentProfile = ServerProfileType.AtlantisCpvp;

		private static readonly List<string> atlantisCpvpWhitelist = new List<string>
		{
			"marlow",
			"marlow-crystal-optimizer",
			"crystaloptimizer",
			"haram",
			"haram-optimizer",
			"endcrystaloptimizer",
			"end-crystal-optimizer",
			"walksy",
			"walksy-anchor",
			"walksy-crystal",
			"heros",
			"hero-optimizer",
			"cutebow",
			"cutebowmod"
		};

		private static readonly List<string> allowedMinimaps = new List<string>
		{
			"xaerosminimap",
			"xaero-minimap",
			"journeymap"
		};

		private static readonly List<string> prohibitedKeywords = new List<string>
		{
			"baritone",
			"freecam",
			"autototem",
			"killaura",
			"reach",
			"hitbox",
			"autoclicker",
			"packetfly",
			"flight",
			"speed",
			"nofall",
			"tracers",
			"wallhack",
			"xray",
			"chams",
			"fastplace",
			"timer",
			"triggerbot",
			"derp",
			"scaffold"
		};

		public static ServerProfileType CurrentProfile
		{
			get { return currentProfile; }
			set { currentProfile = value; }
		}

		public static string CurrentProfileDisplayName
		{
			get
			{
				switch (currentProfile)
				{
					case ServerProfileType.AtlantisCpvp:
						return "Атлантис (Atlantis CPVP — Регламент 5.01-5.08)";
					case ServerProfileType.HolyWorld:
						return "HolyWorld (Стандартный регламент)";
					case ServerProfileType.ReallyWorld:
						return "ReallyWorld (Стандартный регламент)";
					default:
						return "Стандартный регламент проверок (Default)";
				}
			}
		}

		public static bool IsCpvpWhitelisted(string modIdOrFileName)
		{
			if (string.IsNullOrEmpty(modIdOrFileName)) return false;
			string lower = modIdOrFileName.ToLowerInvariant();

			foreach (string white in atlantisCpvpWhitelist)
			{
				if (lower.Contains(white)) return true;
			}
			return false;
		}

		public static bool IsMinimapAllowed(string modIdOrFileName)
		{
			if (string.IsNullOrEmpty(modIdOrFileName)) return false;
			string lower = modIdOrFileName.ToLowerInvariant();

			foreach (string map in allowedMinimaps)
			{
				if (lower.Contains(map)) return true;
			}
			return false;
		}

		public static bool IsProhibitedCheat(string nameOrMixin)
		{
			if (string.IsNullOrEmpty(nameOrMixin)) return false;
			string lower = nameOrMixin.ToLowerInvariant();

			foreach (string kw in prohibitedKeywords)
			{
				if (lower.Contains(kw)) return true;
			}
			return false;
		}

		public static string EvaluateModVerdict(string modId, string jarName, List<string> mixinTargets, out bool isViolation, out string ruleArticle, out int riskScore)
		{
			string lowerId = (modId ?? "").ToLowerInvariant();
			string lowerJar = (jarName ?? "").ToLowerInvariant();

			// 1. Проверка на CPVP белый список сервера «Атлантис»
			if (currentProfile == ServerProfileType.AtlantisCpvp && (IsCpvpWhitelisted(lowerId) || IsCpvpWhitelisted(lowerJar)))
			{
				isViolation = false;
				ruleArticle = "РАЗРЕШЕНО (Атлантис)";
				riskScore = 0;
				return "🟢 РАЗРЕШЕННЫЙ CPVP ОПТИМИЗАТОР («Атлантис» WhiteList: Marlow / Haram / End Crystal / Walksy / Hero's / CuteBow)";
			}

			// 2. Проверка миникарт (Fair-play)
			if (IsMinimapAllowed(lowerId) || IsMinimapAllowed(lowerJar))
			{
				isViolation = false;
				ruleArticle = "РАЗРЕШЕНО";
				riskScore = 5;
				return "🟢 РАЗРЕШЕННАЯ МИНИКАРТА (Xaero's / JourneyMap Fair-Play)";
			}

			// 3. Проверка явных запрещенных модов
			if (IsProhibitedCheat(lowerId) || IsProhibitedCheat(lowerJar))
			{
				isViolation = true;
				ruleArticle = "5.01";
				riskScore = 100;
				return "🚨 ЗАПРЕЩЕННЫЙ ЧИТ-МОД (Регламент 5.01 / Атлантис 5.01-5.07)";
			}

			// 4. Проверка подозрительных Mixin Target-классов
			if (mixinTargets != null && mixinTargets.Count > 0)
			{
				foreach (string target in mixinTargets)
				{
					string lt = target.ToLowerInvariant();
					if (lt.Contains("clientplayerentity") || lt.Contains("livingentityrenderer") ||
						lt.Contains("entityrenderer") || lt.Contains("minecraftclient") ||
						lt.Contains("worldrenderer") || lt.Contains("lightmaptexturemanager") ||
						lt.Contains("ingamehud") || lt.Contains("camera"))
					{
						// Если это не разрешенный CPVP мод
						isViolation = true;
						ruleArticle = "5.01";
						riskScore = 90;
						return "🚨 ПОДОЗРИТЕЛЬНЫЙ MIXIN В БОЕВОЙ/РЕНДЕР КЛАСС (" + target + ")";
					}
				}
			}

			// По умолчанию - легитимный мод
			isViolation = false;
			ruleArticle = "ЧИСТО";
			riskScore = 0;
			return "🟢 ЛЕГИТИМНЫЙ МОД";
		}
	}
}
