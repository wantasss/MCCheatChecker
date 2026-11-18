namespace MCCheatChecker
{
	public class CheckResult
	{
		public string Category { get; set; }

		public string Name { get; set; }

		public string Detail { get; set; }

		public bool Detected { get; set; }

		public CheckSeverity Severity { get; set; }

		public string Evidence { get; set; }

		public CheckResult()
		{
			Category = "";
			Name = "";
			Detail = "";
			Severity = CheckSeverity.Warning;
			Evidence = "";
		}

		public int SeveritySortKey()
		{
			if (!Detected)
			{
				return -1;
			}
			switch (Severity)
			{
			case CheckSeverity.Critical:
				return 4;
			case CheckSeverity.NeedsCheck:
				return 3;
			case CheckSeverity.Warning:
				return 2;
			default:
				return 1;
			}
		}
	}
}
