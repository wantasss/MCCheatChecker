namespace MCCheatChecker
{
	public class NetworkSocketItem
	{
		public int Pid { get; set; }

		public string ProcessName { get; set; }

		public string LocalEndpoint { get; set; }

		public string RemoteEndpoint { get; set; }

		public string State { get; set; }

		public string Verdict { get; set; }

		public bool IsSuspicious { get; set; }

		public NetworkSocketItem()
		{
			ProcessName = "javaw.exe";
			Verdict = "Норма";
		}
	}
}
