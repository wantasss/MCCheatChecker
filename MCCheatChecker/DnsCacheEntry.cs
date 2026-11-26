namespace MCCheatChecker
{
	public class DnsCacheEntry
	{
		public string Domain;

		public string RecordType;

		public string Ttl;

		public string Data;

		public bool IsSuspicious;

		public string Category;

		public DnsCacheEntry()
		{
			Domain = "";
			RecordType = "A";
			Ttl = "—";
			Data = "—";
			IsSuspicious = false;
			Category = "Обычный узел";
		}
	}
}
