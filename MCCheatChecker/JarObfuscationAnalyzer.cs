// MCCheatChecker - JarObfuscationAnalyzer.cs
// Криминалистический анализатор обфускации JAR-архивов и детекции защитных пакеров (Allatori, Zelix, ProGuard, Stringer и др.)

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using System.Text.RegularExpressions;

namespace MCCheatChecker
{
	/// <summary>
	/// Результат криминалистического анализа обфускации JAR-файла
	/// </summary>
	public class JarObfuscationResult
	{
		public string JarPath { get; set; }
		public string JarName { get; set; }
		public int TotalClasses { get; set; }
		public int ObfuscatedClasses { get; set; }
		public int ObfuscationPercentage { get; set; }
		public List<string> DetectedObfuscators { get; set; }
		public string ObfuscatorDetail { get; set; }
		public bool IsObfuscated { get; set; }
		public string RiskLevel { get; set; } // SAFE, SUSPICIOUS, CRITICAL
		public string Summary { get; set; }
		public List<string> SampleObfuscatedClasses { get; set; }

		public JarObfuscationResult()
		{
			DetectedObfuscators = new List<string>();
			SampleObfuscatedClasses = new List<string>();
			RiskLevel = "SAFE";
			Summary = "Чистый мод без признаков коммерческой обфускации";
			ObfuscatorDetail = "Маркеров обфускаторов не обнаружено";
		}
	}

	/// <summary>
	/// Криминалистический движок анализа обфускации байткода Java
	/// </summary>
	public static class JarObfuscationAnalyzer
	{
		// Известные сигнатуры и водяные знаки обфускаторов
		private static readonly string[] KnownObfuscatorWatermarks = new string[]
		{
			"allatori",
			"zelix",
			"klassmaster",
			"zkm",
			"stringer",
			"proguard",
			"dasho",
			"dexguard",
			"radon",
			"superblaubeere",
			"smoke obfuscator",
			"skater .net",
			"sandmark"
		};

		/// <summary>
		/// Анализирует JAR файл и рассчитывает процент обфусцированных классов и наличие коммерческих обфускаторов
		/// </summary>
		public static JarObfuscationResult AnalyzeJar(string jarPath)
		{
			JarObfuscationResult result = new JarObfuscationResult();
			result.JarPath = jarPath;
			result.JarName = Path.GetFileName(jarPath);

			if (string.IsNullOrEmpty(jarPath) || !File.Exists(jarPath))
			{
				result.Summary = "Файл архива не найден";
				return result;
			}

			try
			{
				// Проверка размера
				FileInfo fi = new FileInfo(jarPath);
				if (fi.Length < 4)
				{
					result.Summary = "Поврежденный или пустой файл (0 байт)";
					return result;
				}

				// Проверка ZIP magic PK 0x03 0x04
				byte[] header = new byte[4];
				using (FileStream fsCheck = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				{
					fsCheck.Read(header, 0, 4);
				}
				if (header[0] != 0x50 || header[1] != 0x4B || header[2] != 0x03 || header[3] != 0x04)
				{
					result.Summary = "Файл не является валидным ZIP/JAR архивом";
					return result;
				}

				List<string> classEntries = new List<string>();
				List<string> shortClassNames = new List<string>();
				HashSet<string> obfuscatorsFound = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
				List<string> details = new List<string>();

				string manifestContent = null;

				using (FileStream zipStream = new FileStream(jarPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
				using (ZipArchive archive = new ZipArchive(zipStream, ZipArchiveMode.Read))
				{
					foreach (ZipArchiveEntry entry in archive.Entries)
					{
						string entryName = entry.FullName;
						string lowerName = entryName.ToLowerInvariant();

						// 1. Проверка манифеста
						if (lowerName == "meta-inf/manifest.mf")
						{
							try
							{
								using (StreamReader sr = new StreamReader(entry.Open(), Encoding.UTF8))
								{
									manifestContent = sr.ReadToEnd();
								}
							}
							catch { }
						}

						// 2. Проверка сигнатурных файлов обфускаторов
						if (lowerName.Contains("allatori.xml") || lowerName.Contains("allatoridem") || lowerName.Contains("allatori"))
						{
							obfuscatorsFound.Add("Allatori Obfuscator");
							details.Add("Файл конфигурации/метки Allatori: " + entryName);
						}
						if (lowerName == "zk.class" || lowerName.EndsWith("/zk.class") || lowerName.Contains("zelix") || lowerName.Contains("klassmaster"))
						{
							obfuscatorsFound.Add("Zelix KlassMaster (ZKM)");
							details.Add("Маркерный класс/ресурс Zelix: " + entryName);
						}
						if (lowerName.Contains("stringer") || lowerName.Contains("hideblocks"))
						{
							obfuscatorsFound.Add("Stringer Java Obfuscator");
							details.Add("Маркерный ресурс Stringer: " + entryName);
						}
						if (lowerName.Contains("proguard") || lowerName.Contains("mapping.txt"))
						{
							obfuscatorsFound.Add("ProGuard");
							details.Add("Маркер/маппинг ProGuard: " + entryName);
						}
						if (lowerName.Contains("dasho"))
						{
							obfuscatorsFound.Add("DashO");
							details.Add("Маркер DashO: " + entryName);
						}
						if (lowerName.Contains("radon"))
						{
							obfuscatorsFound.Add("Radon Obfuscator");
							details.Add("Маркер Radon: " + entryName);
						}

						// 3. Отбор классов для анализа структуры имён
						if (lowerName.EndsWith(".class"))
						{
							classEntries.Add(entryName);
							result.TotalClasses++;

							// Извлекаем чистое имя класса без пакета
							string simpleName = Path.GetFileNameWithoutExtension(entryName);

							if (IsObfuscatedClassName(simpleName, entryName))
							{
								result.ObfuscatedClasses++;
								if (shortClassNames.Count < 20)
								{
									shortClassNames.Add(entryName);
								}
							}
						}
					}

					// 4. Проверка манифеста на водяные знаки обфускаторов
					if (!string.IsNullOrEmpty(manifestContent))
					{
						string lowerManifest = manifestContent.ToLowerInvariant();
						foreach (string wm in KnownObfuscatorWatermarks)
						{
							if (lowerManifest.Contains(wm))
							{
								string name = MapWatermarkToDisplayName(wm);
								obfuscatorsFound.Add(name);
								details.Add("Водяной знак обфускатора в MANIFEST.MF: " + wm);
							}
						}
					}

					// 5. Проверка байткода первых классов на скрытые водяные знаки (если обфускатор ещё не найден)
					if (obfuscatorsFound.Count == 0 && classEntries.Count > 0)
					{
						int sampleCount = Math.Min(classEntries.Count, 25);
						for (int i = 0; i < sampleCount; i++)
						{
							ZipArchiveEntry clsEntry = archive.GetEntry(classEntries[i]);
							if (clsEntry != null && clsEntry.Length > 0 && clsEntry.Length < 1024 * 1024)
							{
								try
								{
									byte[] buf = new byte[(int)clsEntry.Length];
									using (Stream s = clsEntry.Open())
									{
										int read = 0;
										while (read < buf.Length)
										{
											int r = s.Read(buf, read, buf.Length - read);
											if (r <= 0) break;
											read += r;
										}
									}

									string asciiStr = Encoding.ASCII.GetString(buf);
									string lowerAscii = asciiStr.ToLowerInvariant();

									if (lowerAscii.Contains("allatori"))
									{
										obfuscatorsFound.Add("Allatori Obfuscator");
										details.Add("Строковая константа Allatori в классе " + clsEntry.Name);
										break;
									}
									if (lowerAscii.Contains("zelix") || lowerAscii.Contains("klassmaster"))
									{
										obfuscatorsFound.Add("Zelix KlassMaster (ZKM)");
										details.Add("Строковая константа Zelix в классе " + clsEntry.Name);
										break;
									}
									if (lowerAscii.Contains("stringer"))
									{
										obfuscatorsFound.Add("Stringer Java Obfuscator");
										details.Add("Строковая константа Stringer в классе " + clsEntry.Name);
										break;
									}
								}
								catch { }
							}
						}
					}
				}

				// Расчет процента обфускации
				if (result.TotalClasses > 0)
				{
					result.ObfuscationPercentage = (int)Math.Round((double)result.ObfuscatedClasses * 100.0 / result.TotalClasses);
				}
				else
				{
					result.ObfuscationPercentage = 0;
				}

				foreach (string ob in obfuscatorsFound)
				{
					result.DetectedObfuscators.Add(ob);
				}
				result.SampleObfuscatedClasses = shortClassNames;

				if (details.Count > 0)
				{
					result.ObfuscatorDetail = string.Join("; ", details.ToArray());
				}

				// Оценка риска
				// Если обнаружен коммерческий обфускатор (Allatori, Zelix, Stringer)
				bool hasCommercialObfuscator = false;
				foreach (string ob in result.DetectedObfuscators)
				{
					if (ob.Contains("Allatori") || ob.Contains("Zelix") || ob.Contains("Stringer"))
					{
						hasCommercialObfuscator = true;
						break;
					}
				}

				if (hasCommercialObfuscator && result.ObfuscationPercentage >= 40)
				{
					result.IsObfuscated = true;
					result.RiskLevel = "CRITICAL";
					result.Summary = string.Format("🚨 КРИТИЧЕСКАЯ ОБФУСКАЦИЯ ({0}% классов) с использованием {1}. Типично для приватных чит-клиентов!",
						result.ObfuscationPercentage, string.Join(", ", result.DetectedObfuscators.ToArray()));
				}
				else if (hasCommercialObfuscator)
				{
					result.IsObfuscated = true;
					result.RiskLevel = "CRITICAL";
					result.Summary = string.Format("⛔ Обнаружен коммерческий обфускатор {0} (обфусцировано: {1}%)",
						string.Join(", ", result.DetectedObfuscators.ToArray()), result.ObfuscationPercentage);
				}
				else if (result.ObfuscationPercentage >= 60)
				{
					result.IsObfuscated = true;
					result.RiskLevel = "CRITICAL";
					result.Summary = string.Format("⚠️ Высокий процент обфускации ({0}% классов). Классы имеют односимвольные имена (a.class, b.class)",
						result.ObfuscationPercentage);
				}
				else if (result.ObfuscationPercentage >= 30 || result.DetectedObfuscators.Count > 0)
				{
					result.IsObfuscated = true;
					result.RiskLevel = "SUSPICIOUS";
					result.Summary = string.Format("🔍 Умеренная обфускация ({0}% классов). Обфускатор: {1}",
						result.ObfuscationPercentage,
						result.DetectedObfuscators.Count > 0 ? string.Join(", ", result.DetectedObfuscators.ToArray()) : "Неизвестный / ProGuard");
				}
				else
				{
					result.IsObfuscated = false;
					result.RiskLevel = "SAFE";
					result.Summary = string.Format("Чистый мод (обфусцировано всего {0}%, {1} из {2} классов)",
						result.ObfuscationPercentage, result.ObfuscatedClasses, result.TotalClasses);
				}
			}
			catch (Exception ex)
			{
				result.Summary = "Ошибка анализа JAR: " + ex.Message;
			}

			return result;
		}

		/// <summary>
		/// Определяет, является ли имя класса обфусцированным
		/// </summary>
		private static bool IsObfuscatedClassName(string simpleName, string fullPath)
		{
			if (string.IsNullOrEmpty(simpleName)) return false;

			// Игнорируем стандартные мета-файлы пакетов
			if (simpleName.Equals("package-info", StringComparison.OrdinalIgnoreCase) ||
				simpleName.Equals("module-info", StringComparison.OrdinalIgnoreCase))
			{
				return false;
			}

			// 1. Длина имени 1 или 2 символа (a, b, c, aa, ab, 0, 1, $, _)
			if (simpleName.Length <= 2)
			{
				return true;
			}

			// 2. Длина 3 символа, состоящая из одинаковых или чередующихся букв/цифр (aaa, lll, IlI, OO0, 1l1)
			if (simpleName.Length == 3)
			{
				bool isConfusing = true;
				char first = simpleName[0];
				for (int i = 1; i < simpleName.Length; i++)
				{
					if (simpleName[i] != first && simpleName[i] != 'l' && simpleName[i] != 'I' && simpleName[i] != '1' && simpleName[i] != '0' && simpleName[i] != 'O')
					{
						isConfusing = false;
						break;
					}
				}
				if (isConfusing) return true;
			}

			// 3. Содержит непечатные, управляющие или не-ASCII символы
			for (int i = 0; i < simpleName.Length; i++)
			{
				char c = simpleName[i];
				if (c < 32 || c > 126)
				{
					// Не-ASCII символ в имени класса -> признак тяжелой обфускации (Zelix, Stringer, Allatori)
					return true;
				}
			}

			// 4. Пакетная обфускация (глубокая вложенность из 1-буквенных пакетов, e.g. a/b/c/d/e.class)
			string[] parts = fullPath.Split(new char[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
			if (parts.Length >= 4)
			{
				int singleLetterDirs = 0;
				for (int i = 0; i < parts.Length - 1; i++)
				{
					if (parts[i].Length <= 2) singleLetterDirs++;
				}
				if (singleLetterDirs >= 3) return true;
			}

			return false;
		}

		private static string MapWatermarkToDisplayName(string watermark)
		{
			switch (watermark)
			{
				case "allatori": return "Allatori Obfuscator";
				case "zelix":
				case "klassmaster":
				case "zkm": return "Zelix KlassMaster (ZKM)";
				case "stringer": return "Stringer Java Obfuscator";
				case "proguard": return "ProGuard";
				case "dasho": return "DashO";
				case "dexguard": return "DexGuard";
				case "radon": return "Radon Obfuscator";
				case "superblaubeere": return "SuperBlaubeere Obfuscator";
				case "smoke obfuscator": return "Smoke Obfuscator";
				case "skater .net": return "Skater Obfuscator";
				case "sandmark": return "SandMark";
				default: return watermark;
			}
		}
	}
}
