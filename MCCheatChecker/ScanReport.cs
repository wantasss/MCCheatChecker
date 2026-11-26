using System;
using System.Collections.Generic;
using System.Linq;

namespace MCCheatChecker
{
	public class ScanReport
	{
		public DateTime ScanTime { get; set; }

		public string PlayerName { get; set; }

		public List<CheckResult> Results { get; set; }

		public int CriticalCount
		{
			get
			{
				return Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.Critical);
			}
		}

		public int VisualsCount
		{
			get
			{
				return Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.NeedsCheck);
			}
		}

		public int WarningCount
		{
			get
			{
				return Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.Warning);
			}
		}

		public int InfoCount
		{
			get
			{
				return Results.Count((CheckResult r) => r.Detected && r.Severity == CheckSeverity.Info);
			}
		}

		public int TotalDetected
		{
			get
			{
				return Results.Count((CheckResult r) => r.Detected);
			}
		}

		public string Verdict
		{
			get
			{
				if (CriticalCount > 0)
				{
					return "\ud83d\udd34 БАН — ОБНАРУЖЕНЫ ХАРД-ЧИТЫ / ИНЖЕКЦИИ";
				}
				if (VisualsCount > 0)
				{
					return "\ud83d\udfe3 СЛЕДУЕТ ПРОВЕРИТЬ (НАЙДЕНЫ ВИЗУАЛЫ / X-RAY)";
				}
				if (WarningCount >= 2)
				{
					return "\ud83d\udfe1 ПОДОЗРИТЕЛЬНО (УДАЛЁННЫЕ ПРОГРАММЫ / СЛЕДЫ)";
				}
				if (TotalDetected > 0)
				{
					return "\ud83d\udfe1 ТРЕБУЕТ ВНИМАНИЯ МОДЕРАТОРА";
				}
				return "\ud83d\udfe2 ЧИСТО (НАРУШЕНИЙ НЕ НАЙДЕНО)";
			}
		}

		public string VerdictColor
		{
			get
			{
				if (CriticalCount > 0)
				{
					return "#F44336";
				}
				if (VisualsCount > 0)
				{
					return "#AB47BC";
				}
				if (WarningCount >= 2)
				{
					return "#FFC107";
				}
				if (TotalDetected > 0)
				{
					return "#FFC107";
				}
				return "#4CAF50";
			}
		}

		public ScanReport()
		{
			ScanTime = DateTime.Now;
			PlayerName = "";
			Results = new List<CheckResult>();
		}
	}
}
