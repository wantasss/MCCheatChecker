using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace MCCheatChecker
{
	public static class SignatureAutoUpdateManager
	{
		private static readonly string CacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MCCheatChecker");
		private static readonly string CacheFilePath = Path.Combine(CacheDir, "signatures.json");

		public static string CurrentVersion { get; private set; }
		public static DateTime LastUpdateTime { get; private set; }
		public static bool IsLoadedFromRemote { get; private set; }

		private static readonly HashSet<string> dynamicJavaAgentBlacklist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private static readonly HashSet<string> dynamicJavaAgentWhitelist = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private static readonly HashSet<string> dynamicCheatClients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		private static readonly HashSet<long> dynamicCheatJarSizes = new HashSet<long>();
		private static readonly object syncLock = new object();

		private const string RemoteSignaturesUrl = "https://raw.githubusercontent.com/antigravity-mccheat/rules/main/signatures.json";

		static SignatureAutoUpdateManager()
		{
			CurrentVersion = "2026.11.24.1";
			LastUpdateTime = DateTime.Now;
			Initialize();
		}

		public static void Initialize()
		{
			lock (syncLock)
			{
				PopulateBuiltinDefaults();

				try
				{
					if (!Directory.Exists(CacheDir))
					{
						Directory.CreateDirectory(CacheDir);
					}

					if (File.Exists(CacheFilePath))
					{
						string json = File.ReadAllText(CacheFilePath, Encoding.UTF8);
						ParseAndApplyJson(json);
					}
					else
					{
						// Save default cache
						string defaultJson = GenerateDefaultJson();
						File.WriteAllText(CacheFilePath, defaultJson, Encoding.UTF8);
					}
				}
				catch
				{
					// Fallback to built-in defaults
				}
			}
		}

		private static void PopulateBuiltinDefaults()
		{
			dynamicJavaAgentBlacklist.Clear();
			string[] defaultBlacklist = new string[]
			{
				"vape", "vapev4", "vape-v4", "driplite", "drip-lite", "drip", "slinky", "celestial",
				"celestialrecode", "expensive", "expensiverecode", "nursultan", "nursultanrecode",
				"neverhook", "minced", "wildclient", "wild", "deadcode", "akrien", "speclient",
				"zamorozka", "wurst", "meteor", "liquidbounce", "whiteout", "itami", "coffeeclient",
				"kuraclient", "thunderhack", "boze", "doomsday", "flint", "catlavan", "bape", "norules"
			};
			for (int i = 0; i < defaultBlacklist.Length; i++)
			{
				dynamicJavaAgentBlacklist.Add(defaultBlacklist[i]);
			}

			dynamicJavaAgentWhitelist.Clear();
			string[] defaultWhitelist = new string[]
			{
				"authlib-injector", "fabric-loader", "quilt-loader", "sponge-mixin",
				"byte-buddy-agent", "byte-buddy", "intellij-coverage-agent", "jacocoagent",
				"debugger-agent", "jetbrains-agent", "jdwp"
			};
			for (int i = 0; i < defaultWhitelist.Length; i++)
			{
				dynamicJavaAgentWhitelist.Add(defaultWhitelist[i]);
			}

			dynamicCheatClients.Clear();
			if (CheatDatabases.ExtraCheatClients != null)
			{
				for (int i = 0; i < CheatDatabases.ExtraCheatClients.Length; i++)
				{
					dynamicCheatClients.Add(CheatDatabases.ExtraCheatClients[i]);
				}
			}

			dynamicCheatJarSizes.Clear();
			if (CheatDatabases.KnownCheatJarSizes != null)
			{
				foreach (long s in CheatDatabases.KnownCheatJarSizes)
				{
					dynamicCheatJarSizes.Add(s);
				}
			}
		}

		public static bool CheckForUpdates(out string statusMessage)
		{
			try
			{
				HttpWebRequest request = (HttpWebRequest)WebRequest.Create(RemoteSignaturesUrl);
				request.Timeout = 4000;
				request.ReadWriteTimeout = 4000;
				request.UserAgent = "MCCheatChecker-SignatureUpdater/1.0";

				using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
				{
					if (response.StatusCode == HttpStatusCode.OK)
					{
						using (Stream stream = response.GetResponseStream())
						using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
						{
							string json = reader.ReadToEnd();
							if (!string.IsNullOrEmpty(json) && json.IndexOf("version", StringComparison.OrdinalIgnoreCase) >= 0)
							{
								lock (syncLock)
								{
									ParseAndApplyJson(json);
									File.WriteAllText(CacheFilePath, json, Encoding.UTF8);
									IsLoadedFromRemote = true;
									LastUpdateTime = DateTime.Now;
								}
								statusMessage = string.Format("База сигнатур успешно обновлена с сервера! Версия: {0}, правил: {1}", CurrentVersion, GetTotalSignaturesCount());
								return true;
							}
						}
					}
				}
			}
			catch (Exception ex)
			{
				// Offline or remote host down -> safe fallback to local cache
				lock (syncLock)
				{
					if (File.Exists(CacheFilePath))
					{
						try
						{
							string localJson = File.ReadAllText(CacheFilePath, Encoding.UTF8);
							ParseAndApplyJson(localJson);
						}
						catch { }
					}
				}
				statusMessage = string.Format("Сервер обновлений недоступен ({0}). Загружена локальная офлайн-база сигнатур (Версия: {1}, правил: {2}).",
					ex.Message, CurrentVersion, GetTotalSignaturesCount());
				return false;
			}

			statusMessage = "Не удалось применить обновления сигнатур. Используется резервная копия.";
			return false;
		}

		public static void CheckForUpdatesAsync(Action<bool, string> callback)
		{
			ThreadPool.QueueUserWorkItem(delegate
			{
				string msg;
				bool ok = CheckForUpdates(out msg);
				if (callback != null)
				{
					callback(ok, msg);
				}
			});
		}

		private static void ParseAndApplyJson(string json)
		{
			if (string.IsNullOrEmpty(json)) return;

			// Extract version
			Match mVer = Regex.Match(json, "\"version\"\\s*:\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);
			if (mVer.Success)
			{
				CurrentVersion = mVer.Groups[1].Value;
			}

			// Extract JavaAgentBlacklist
			List<string> bl = ExtractJsonStringArray(json, "javaAgentBlacklist");
			if (bl != null && bl.Count > 0)
			{
				foreach (string s in bl)
				{
					dynamicJavaAgentBlacklist.Add(s.Trim().ToLowerInvariant());
				}
			}

			// Extract JavaAgentWhitelist
			List<string> wl = ExtractJsonStringArray(json, "javaAgentWhitelist");
			if (wl != null && wl.Count > 0)
			{
				foreach (string s in wl)
				{
					dynamicJavaAgentWhitelist.Add(s.Trim().ToLowerInvariant());
				}
			}

			// Extract ExtraCheatClients
			List<string> cc = ExtractJsonStringArray(json, "extraCheatClients");
			if (cc != null && cc.Count > 0)
			{
				foreach (string s in cc)
				{
					dynamicCheatClients.Add(s.Trim().ToLowerInvariant());
				}
			}

			// Extract KnownCheatJarSizes
			Match mSizes = Regex.Match(json, "\"knownCheatJarSizes\"\\s*:\\s*\\[([^\\]]+)\\]", RegexOptions.IgnoreCase);
			if (mSizes.Success)
			{
				string[] nums = mSizes.Groups[1].Value.Split(new char[] { ',', ' ', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
				for (int i = 0; i < nums.Length; i++)
				{
					long sz;
					if (long.TryParse(nums[i].Trim(), out sz))
					{
						dynamicCheatJarSizes.Add(sz);
					}
				}
			}
		}

		private static List<string> ExtractJsonStringArray(string json, string arrayKey)
		{
			List<string> result = new List<string>();
			string pattern = "\"" + arrayKey + "\"\\s*:\\s*\\[([^\\]]+)\\]";
			Match m = Regex.Match(json, pattern, RegexOptions.IgnoreCase);
			if (m.Success)
			{
				MatchCollection items = Regex.Matches(m.Groups[1].Value, "\"([^\"]+)\"");
				foreach (Match it in items)
				{
					result.Add(it.Groups[1].Value);
				}
			}
			return result;
		}

		private static string GenerateDefaultJson()
		{
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("{");
			sb.AppendLine("  \"version\": \"2026.11.24.1\",");
			sb.AppendLine(string.Format("  \"lastUpdated\": \"{0:yyyy-MM-dd HH:mm:ss}\",", DateTime.Now));
			sb.AppendLine("  \"description\": \"База сигнатур читов и Java-агентов MCCheatChecker (Атлантис Регламент 5.01-5.08)\",");

			sb.AppendLine("  \"javaAgentBlacklist\": [");
			int count = 0;
			foreach (string b in dynamicJavaAgentBlacklist)
			{
				count++;
				sb.AppendFormat("    \"{0}\"{1}\n", b, count == dynamicJavaAgentBlacklist.Count ? "" : ",");
			}
			sb.AppendLine("  ],");

			sb.AppendLine("  \"javaAgentWhitelist\": [");
			count = 0;
			foreach (string w in dynamicJavaAgentWhitelist)
			{
				count++;
				sb.AppendFormat("    \"{0}\"{1}\n", w, count == dynamicJavaAgentWhitelist.Count ? "" : ",");
			}
			sb.AppendLine("  ],");

			sb.AppendLine("  \"extraCheatClients\": [");
			count = 0;
			foreach (string c in dynamicCheatClients)
			{
				count++;
				sb.AppendFormat("    \"{0}\"{1}\n", c, count == dynamicCheatClients.Count ? "" : ",");
			}
			sb.AppendLine("  ]");
			sb.AppendLine("}");
			return sb.ToString();
		}

		public static bool IsBlacklistedJavaAgent(string agentPathOrName)
		{
			if (string.IsNullOrEmpty(agentPathOrName)) return false;
			string lower = agentPathOrName.ToLowerInvariant();

			lock (syncLock)
			{
				foreach (string bl in dynamicJavaAgentBlacklist)
				{
					if (lower.IndexOf(bl, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static bool IsWhitelistedJavaAgent(string agentPathOrName)
		{
			if (string.IsNullOrEmpty(agentPathOrName)) return false;
			string lower = agentPathOrName.ToLowerInvariant();

			lock (syncLock)
			{
				foreach (string wl in dynamicJavaAgentWhitelist)
				{
					if (lower.IndexOf(wl, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						return true;
					}
				}
			}
			return false;
		}

		public static int GetTotalSignaturesCount()
		{
			lock (syncLock)
			{
				return dynamicJavaAgentBlacklist.Count + dynamicJavaAgentWhitelist.Count + dynamicCheatClients.Count + dynamicCheatJarSizes.Count;
			}
		}
	}
}
