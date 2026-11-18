namespace MCCheatChecker
{
	public class ProcessItem
	{
		public int Pid { get; set; }

		public string Name { get; set; }

		public string WindowTitle { get; set; }

		public long MemoryBytes { get; set; }

		public string MemoryString { get; set; }

		public int ThreadCount { get; set; }

		public string StartTime { get; set; }

		public string ExecutablePath { get; set; }

		public string CommandLine { get; set; }

		public bool HasWindow { get; set; }

		public bool IsMinecraft { get; set; }

		public bool IsThreat { get; set; }

		public string ThreatReason { get; set; }

		public CheckSeverity Severity { get; set; }
	}
}
