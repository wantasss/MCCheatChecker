using System.Collections.Generic;
using System.Linq;

namespace MCCheatChecker
{
	public class InternalStringHit
	{
		public string FilePath { get; set; }

		public string FileName { get; set; }

		public string FileType { get; set; }

		public long FileSizeBytes { get; set; }

		public string FileSizeString { get; set; }

		public string Sha256 { get; set; }

		public string Md5 { get; set; }

		public CheckSeverity Severity { get; set; }

		public string ThreatTitle { get; set; }

		public List<string> MatchedSignatures { get; set; }

		public List<string> InternalStringsSample { get; set; }

		public int TotalStringsExtracted { get; set; }

		public int TotalClassesInspected { get; set; }

		public bool IsThreat { get; set; }

		public InternalStringHit()
		{
			MatchedSignatures = new List<string>();
			InternalStringsSample = new List<string>();
			Severity = CheckSeverity.Info;
			FileType = "Файл";
		}

		public string GetMatchedStringSummary()
		{
			if (MatchedSignatures.Count == 0)
			{
				return "Опасных внутренних строк не найдено";
			}
			return string.Join(" · ", MatchedSignatures.Distinct().Take(6).ToArray());
		}
	}
}
