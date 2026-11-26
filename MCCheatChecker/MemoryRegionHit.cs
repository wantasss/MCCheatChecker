namespace MCCheatChecker
{
	public class MemoryRegionHit
	{
		public string Address { get; set; }

		public string Size { get; set; }

		public string Protection { get; set; }

		public string Type { get; set; }

		public string Detail { get; set; }

		public CheckSeverity Severity { get; set; }
	}
}
