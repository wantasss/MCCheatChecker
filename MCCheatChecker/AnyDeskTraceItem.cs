namespace MCCheatChecker
{
	public class AnyDeskTraceItem
	{
		public string Time { get; set; }

		public string Category { get; set; }

		public string RemoteId { get; set; }

		public string Description { get; set; }

		public bool IsWarning { get; set; }

		public AnyDeskTraceItem()
		{
			Category = "Событие";
			RemoteId = "—";
		}
	}
}
