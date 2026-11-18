using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace MCCheatChecker
{
	public static class ModStringInspector
	{
		private static readonly string[] CheatPackagesAndClasses;

		private static readonly string[] InternalCheatModules;

		private static readonly byte[][] EncryptedApis;

		private static readonly byte[][] EncryptedAntiForensics;

		private static readonly byte[][] EncryptedStealers;

		private static string[] DangerousNativeApiStrings;

		private static string[] AntiForensicCommands;

		private static string[] StealerEndpoints;

		static ModStringInspector()
		{
			CheatPackagesAndClasses = new string[]
			{
				"celestial", "expensive", "akrien", "nurik", "deltaclient", "delta/client", "delta.client", "wildclient", "wild/client", "richclient",
				"rich/client", "fluger", "excellentclient", "wurstclient", "wurst/client", "liquidbounce", "meteorclient", "meteor/client", "aristois", "impactclient",
				"bleachhack", "futureclient", "rusherhack", "konasclient", "me/lambda/client", "lambdaclient", "phobos/client", "salhack", "kamiblue", "inertia/client",
				"aresclient", "novoline", "riseclient", "tenacity/client", "astolfo", "fluxclient", "zeroday", "fdpclient", "augustus", "ravenbplus",
				"ravenb3", "vapeclient", "vape/", "driplite", "entropyclient", "kuraclient", "doomsday", "bozeclient", "matix/client", "kronos/client",
				"wolfram", "thunderhack", "thunderhack/client", "nursultan", "nursultan/client", "slinky", "slinky/client", "seedcracker", "seedcrackerx", "dreamclient", "deadcode", "zamorozka", "minced", "wexside", "boberware", "rockstar", "avalone", "kion", "xorek", "vulcanclicker", "cattyn", "spirthack", "memesense", "neverlose", "gamesense", "sigmaclient", "sigma/client", "monolith", "richpremium", "celestial/premium", "expensive/client", "nursultan/recode", "flintclient", "flint/client", "kuraclient", "kura/client", "catlavan", "catlavan/client", "cellent", "cellent/client", "archware/client", "boberware/client", "bapeclient", "bape/client", "whiteout", "itami", "coffeeclient", "coffee/client", "thunderhack/recoded", "thunderhackplus", "exhibition/client", "pandora/client", "neverhook/client", "minced/client", "akrien/client", "zamorozka/client", "wexside/client", "wild/recode", "expensive/recode", "celestial/recode", "norules/client", "bebraware/client", "rynware/client", "neverware/client", "simplyhax", "skillclient", "jigsaw/client", "salhack/client", "weepcraft", "kalkon/client", "advancedx/client", "neverclide/client", "prerik/client", "cabaletta/client", "gishcode", "eternity/client", "emortalityclient", "dreampool", "clowdy/client", "rasty/client", "wintware/client", "interium/client", "vegaline/client", "fdp/client", "trollhack/client", "lunaclient", "exloader", "augustus/client", "novoline/client", "rise/client", "tenacity/client"
			};
			InternalCheatModules = new string[]
			{
				"killaura", "aimbot", "triggerbot", "hitbox", "autoclicker", "reachmod", "reachdistance", "velocitymod", "antiknockback", "fastbow",
				"wtap", "autototem", "autocrystal", "anchoraura", "bhop", "speedhack", "jesusmod", "waterwalk", "spiderhack", "nofallmod",
				"flighthack", "elytrafly", "tracers", "playeresp", "chestesp", "nametagmod", "fullbright", "phasehack", "fastplace", "fastbreak",
				"scaffold", "autobridge", "nuker", "autoarmor", "noslowdown", "noweb", "inventorymove", "autodisconnect", "bowaimbot", "packetfly",
				"boatfly", "targetstrafe", "criticals", "timerhack", "fastmine", "autopearl", "middleclickpearl", "silentaim", "autoanchor", "crystaloptimizer", "autobuy", "antidebuff", "autopotion", "fastladder", "airjump", "freecam", "autoheal", "autoleave", "shifttap", "hitboxes", "silentaimbot", "packetelytra", "blockesp", "xraymod"
			};
			EncryptedApis = new byte[11][]
			{
				new byte[14]
				{
					241, 82, 128, 181, 45, 184, 2, 85, 230, 41,
					220, 30, 163, 87
				},
				new byte[18]
				{
					240, 73, 155, 181, 61, 137, 28, 123, 233, 32,
					192, 14, 171, 74, 252, 163, 213, 66
				},
				new byte[18]
				{
					228, 73, 151, 160, 44, 188, 60, 113, 231, 42,
					199, 24, 178, 71, 227, 169, 198, 95
				},
				new byte[20]
				{
					233, 79, 167, 175, 53, 184, 30, 66, 227, 32,
					196, 50, 128, 124, 244, 175, 211, 82, 157, 175
				},
				new byte[12]
				{
					246, 78, 151, 180, 61, 140, 29, 113, 248, 4,
					227, 62
				},
				new byte[16]
				{
					244, 94, 134, 150, 49, 183, 10, 123, 253, 54,
					251, 18, 137, 68, 212, 180
				},
				new byte[14]
				{
					244, 94, 134, 150, 49, 183, 10, 123, 253, 54,
					251, 18, 137, 68
				},
				new byte[16]
				{
					241, 82, 128, 181, 45, 184, 2, 68, 248, 42,
					199, 24, 133, 91, 212, 180
				},
				new byte[18]
				{
					253, 76, 191, 160, 40, 143, 7, 113, 253, 10,
					213, 46, 131, 76, 229, 165, 200, 85
				},
				new byte[9] { 234, 90, 156, 180, 57, 181, 35, 117, 250 },
				new byte[16]
				{
					245, 94, 148, 173, 61, 186, 26, 125, 252, 32,
					255, 18, 135, 75, 244, 190
				}
			};
			EncryptedAntiForensics = new byte[8][]
			{
				new byte[11]
				{
					208, 94, 132, 181, 45, 173, 7, 120, 170, 38,
					223
				},
				new byte[23]
				{
					209, 72, 129, 160, 60, 180, 7, 122, 170, 33,
					214, 17, 131, 91, 244, 236, 212, 83, 147, 165,
					55, 174, 29
				},
				new byte[24]
				{
					193, 72, 135, 181, 49, 181, 78, 97, 249, 43,
					147, 25, 131, 67, 244, 184, 194, 81, 157, 180,
					42, 183, 15, 120
				},
				new byte[14]
				{
					228, 87, 151, 160, 42, 244, 43, 98, 239, 43,
					199, 49, 137, 72
				},
				new byte[12]
				{
					197, 88, 150, 164, 60, 176, 26, 52, 165, 54,
					214, 9
				},
				new byte[10] { 213, 94, 149, 225, 60, 188, 2, 113, 254, 32 },
				new byte[10] { 196, 82, 130, 169, 61, 171, 78, 59, 253, 127 },
				new byte[7] { 212, 95, 151, 173, 61, 173, 11 }
			};
			EncryptedStealers = new byte[3][]
			{
				new byte[24]
				{
					195, 82, 129, 162, 55, 171, 10, 58, 233, 42,
					222, 82, 135, 95, 248, 227, 208, 94, 144, 169,
					55, 182, 5, 103
				},
				new byte[20]
				{
					198, 75, 155, 239, 44, 188, 2, 113, 237, 55,
					210, 16, 200, 64, 227, 171, 136, 89, 157, 181
				},
				new byte[27]
				{
					195, 82, 129, 162, 55, 171, 10, 117, 250, 53,
					157, 30, 137, 66, 190, 173, 215, 82, 221, 182,
					61, 187, 6, 123, 229, 46, 192
				}
			};
			DangerousNativeApiStrings = StringShield.DecryptAll(EncryptedApis);
			AntiForensicCommands = StringShield.DecryptAll(EncryptedAntiForensics);
			StealerEndpoints = StringShield.DecryptAll(EncryptedStealers);
		}

		public static InternalStringHit ScanFile(string path)
		{
			InternalStringHit internalStringHit = new InternalStringHit();
			internalStringHit.FilePath = path;
			internalStringHit.FileName = Path.GetFileName(path);
			if (!File.Exists(path))
			{
				internalStringHit.ThreatTitle = "Файл не найден";
				return internalStringHit;
			}
			try
			{
				FileInfo fileInfo = new FileInfo(path);
				internalStringHit.FileSizeBytes = fileInfo.Length;
				internalStringHit.FileSizeString = FormatFileSize(fileInfo.Length);
				string text = fileInfo.Extension.ToLowerInvariant();
				switch (text)
				{
				case ".jar":
				case ".zip":
					internalStringHit.FileType = ((text == ".jar") ? "JAR Мод" : "ZIP Архив");
					ScanJarOrZip(path, internalStringHit);
					break;
				case ".class":
				{
					internalStringHit.FileType = "Java Class";
					using (FileStream stream = File.OpenRead(path))
					{
						HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
						ParseJavaClassStrings(stream, hashSet);
						AnalyzeExtractedStrings(hashSet, internalStringHit);
					}
					break;
				}
				case ".exe":
				case ".dll":
				case ".sys":
					internalStringHit.FileType = ((text == ".dll") ? "DLL Библиотека" : "EXE Бинарник");
					ScanBinaryOrScriptFile(path, internalStringHit);
					break;
				case ".bat":
				case ".cmd":
				case ".ps1":
				case ".vbs":
				case ".py":
					internalStringHit.FileType = text.Substring(1).ToUpperInvariant() + " Скрипт";
					ScanBinaryOrScriptFile(path, internalStringHit);
					break;
				default:
					internalStringHit.FileType = ((text.Length > 1) ? text.Substring(1).ToUpperInvariant() : "Файл");
					ScanBinaryOrScriptFile(path, internalStringHit);
					break;
				}
			}
			catch (Exception ex)
			{
				internalStringHit.ThreatTitle = "Ошибка анализа: " + ex.Message;
			}
			return internalStringHit;
		}

		private static void ScanJarOrZip(string zipPath, InternalStringHit hit)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			int num = 0;
			try
			{
				using (ZipArchive zipArchive = ZipFile.OpenRead(zipPath))
				{
					foreach (ZipArchiveEntry entry in zipArchive.Entries)
					{
						string text = entry.FullName.ToLowerInvariant().Replace("\\", "/");
						if (text.EndsWith(".class"))
						{
							num++;
							hashSet.Add(entry.FullName);
							using (Stream stream = entry.Open())
							{
								ParseJavaClassStrings(stream, hashSet);
							}
						}
						else if (text.EndsWith(".jar") && entry.Length > 0 && entry.Length < 62914560)
						{
							using (Stream stream2 = entry.Open())
							{
								using (MemoryStream memoryStream = new MemoryStream())
								{
									stream2.CopyTo(memoryStream);
									memoryStream.Position = 0L;
									try
									{
										using (ZipArchive zipArchive2 = new ZipArchive(memoryStream, ZipArchiveMode.Read))
										{
											foreach (ZipArchiveEntry entry2 in zipArchive2.Entries)
											{
												if (entry2.FullName.EndsWith(".class", StringComparison.OrdinalIgnoreCase))
												{
													num++;
													hashSet.Add(entry2.FullName);
													using (Stream stream3 = entry2.Open())
													{
														ParseJavaClassStrings(stream3, hashSet);
													}
												}
											}
										}
									}
									catch
									{
									}
								}
							}
						}
						else
						{
							if (!text.EndsWith(".json") && !text.EndsWith(".info") && !text.EndsWith(".mf") && !text.EndsWith(".mcmeta"))
							{
								continue;
							}
							try
							{
								using (Stream stream4 = entry.Open())
								{
									using (StreamReader streamReader = new StreamReader(stream4, Encoding.UTF8))
									{
										string text2;
										while ((text2 = streamReader.ReadLine()) != null)
										{
											string text3 = text2.Trim();
											if (text3.Length >= 4 && text3.Length <= 250)
											{
												hashSet.Add(text3);
											}
										}
									}
								}
							}
							catch
							{
							}
						}
					}
				}
			}
			catch
			{
			}
			hit.TotalClassesInspected = num;
			hit.TotalStringsExtracted = hashSet.Count;
			AnalyzeExtractedStrings(hashSet, hit);
		}

		private static void ScanBinaryOrScriptFile(string filePath, InternalStringHit hit)
		{
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			try
			{
				byte[] array;
				using (FileStream fileStream = File.OpenRead(filePath))
				{
					int num = (int)Math.Min(fileStream.Length, 15728640L);
					array = new byte[num];
					fileStream.Read(array, 0, num);
				}
				StringBuilder stringBuilder = new StringBuilder();
				foreach (byte b in array)
				{
					if (b >= 32 && b <= 126)
					{
						stringBuilder.Append((char)b);
						continue;
					}
					if (stringBuilder.Length >= 4 && stringBuilder.Length <= 300)
					{
						hashSet.Add(stringBuilder.ToString());
					}
					stringBuilder.Length = 0;
				}
				if (stringBuilder.Length >= 4)
				{
					hashSet.Add(stringBuilder.ToString());
				}
				stringBuilder.Length = 0;
				for (int j = 0; j < array.Length - 1; j += 2)
				{
					byte b2 = array[j];
					if (array[j + 1] == 0 && b2 >= 32 && b2 <= 126)
					{
						stringBuilder.Append((char)b2);
						continue;
					}
					if (stringBuilder.Length >= 4 && stringBuilder.Length <= 300)
					{
						hashSet.Add(stringBuilder.ToString());
					}
					stringBuilder.Length = 0;
				}
				if (stringBuilder.Length >= 4)
				{
					hashSet.Add(stringBuilder.ToString());
				}
			}
			catch
			{
			}
			hit.TotalStringsExtracted = hashSet.Count;
			AnalyzeExtractedStrings(hashSet, hit);
		}

		private static void AnalyzeExtractedStrings(HashSet<string> strings, InternalStringHit hit)
		{
			int num = 0;
			int num2 = 0;
			int num3 = 0;
			int num4 = 0;
			int num5 = 0;
			List<string> list = new List<string>();
			foreach (string @string in strings)
			{
				string text = @string.ToLowerInvariant();
				if (text.StartsWith("java/") || text.StartsWith("javax/") || text.StartsWith("sun/") || text.StartsWith("org/xml/") || text.StartsWith("com/sun/"))
				{
					continue;
				}
				string[] cheatPackagesAndClasses = CheatPackagesAndClasses;
				foreach (string text2 in cheatPackagesAndClasses)
				{
					if (text.Contains(text2))
					{
						hit.MatchedSignatures.Add("Чит-пакет/класс: " + text2);
						num++;
						if (list.Count < 50)
						{
							list.Add(@string);
						}
						break;
					}
				}
				string[] internalCheatModules = InternalCheatModules;
				foreach (string text3 in internalCheatModules)
				{
					if (text.Contains(text3) && !text.Contains("vanilla") && !text.Contains("fabric-api") && !text.Contains("jei") && !text.Contains("sodium") && !text.Contains("iris") && !text.Contains("voicechat") && !text.Contains("emotecraft") && !text.Contains("optifine"))
					{
						hit.MatchedSignatures.Add("Чит-модуль: " + text3);
						num2++;
						if (list.Count < 50)
						{
							list.Add(@string);
						}
						break;
					}
				}
				string[] dangerousNativeApiStrings = DangerousNativeApiStrings;
				foreach (string text4 in dangerousNativeApiStrings)
				{
					if (@string.IndexOf(text4, StringComparison.OrdinalIgnoreCase) >= 0)
					{
						hit.MatchedSignatures.Add("Инжекция API: " + text4);
						num4++;
						if (list.Count < 50)
						{
							list.Add(@string);
						}
						break;
					}
				}
				string[] antiForensicCommands = AntiForensicCommands;
				foreach (string text5 in antiForensicCommands)
				{
					if (text.Contains(text5))
					{
						hit.MatchedSignatures.Add("Анти-форензика: " + text5);
						num5++;
						if (list.Count < 50)
						{
							list.Add(@string);
						}
						break;
					}
				}
				string[] stealerEndpoints = StealerEndpoints;
				foreach (string text6 in stealerEndpoints)
				{
					if (text.Contains(text6))
					{
						hit.MatchedSignatures.Add("Webhook/Stealer: " + text6);
						num3++;
						if (list.Count < 50)
						{
							list.Add(@string);
						}
						break;
					}
				}
			}
			hit.InternalStringsSample = list;
			if (CheatDatabases.IsAllowedVisualMod(hit.FileName) || CheatDatabases.IsAllowedCpvpOptimizer(hit.FileName))
			{
				if (num == 0 && num3 == 0 && num4 == 0 && num5 == 0)
				{
					hit.IsThreat = false;
					hit.Severity = CheckSeverity.Info;
					hit.ThreatTitle = "🟢 ЛЕГИТИМНЫЙ ВИЗУАЛ / ОПТИМИЗАТОР (Разрешён регламентом)";
					return;
				}
			}
			if (num > 0 || num3 > 0)
			{
				hit.IsThreat = true;
				hit.Severity = CheckSeverity.Critical;
				hit.ThreatTitle = ((num > 0) ? ("\ud83d\udd34 ХАРД-ЧИТ (Внутренние чит-пакеты в байткоде: " + num + ")") : "\ud83d\udd34 СТИЛЕР / ТОКЕН-ГРАББЕР (Webhook)");
			}
			else if (num4 >= 2)
			{
				hit.IsThreat = true;
				hit.Severity = CheckSeverity.Critical;
				hit.ThreatTitle = "\ud83d\udd34 ИНЖЕКТОР ПАМЯТИ (Использует функции ручного маппинга / API: " + num4 + ")";
			}
			else if (num5 > 0)
			{
				hit.IsThreat = true;
				hit.Severity = CheckSeverity.Critical;
				hit.ThreatTitle = "\ud83d\udfe0 АНТИ-ФОРЕНЗИКА (Очистка журнала событий / корзины / логов)";
			}
			else if (num2 >= 2)
			{
				hit.IsThreat = true;
				hit.Severity = CheckSeverity.NeedsCheck;
				hit.ThreatTitle = "\ud83d\udfe0 ВЫСОКАЯ ПОДОЗРИТЕЛЬНОСТЬ (Множественные чит-модули: " + num2 + ")";
			}
			else if (num2 == 1 || num4 == 1)
			{
				hit.IsThreat = true;
				hit.Severity = CheckSeverity.Warning;
				hit.ThreatTitle = "\ud83d\udfe1 ТРЕБУЕТ ПРОВЕРКИ (Найден подозрительный метод / класс)";
			}
			else
			{
				hit.IsThreat = false;
				hit.Severity = CheckSeverity.Info;
				hit.ThreatTitle = "\ud83d\udfe2 БЕЗОПАСЕН (Опасных внутренних строк не обнаружено)";
			}
		}

		private static void ParseJavaClassStrings(Stream stream, HashSet<string> result)
		{
			using (BinaryReader binaryReader = new BinaryReader(stream))
			{
				try
				{
					uint num = ReadUInt32BE(binaryReader);
					if (num != 3405691582u)
					{
						return;
					}
					ReadUInt16BE(binaryReader);
					ReadUInt16BE(binaryReader);
					ushort num2 = ReadUInt16BE(binaryReader);
					for (int i = 1; i < num2; i++)
					{
						switch (binaryReader.ReadByte())
						{
						default:
							return;
						case 1:
						{
							ushort count = ReadUInt16BE(binaryReader);
							byte[] bytes = binaryReader.ReadBytes(count);
							string text = Encoding.UTF8.GetString(bytes);
							if (text.Length >= 3)
							{
								result.Add(text);
							}
							break;
						}
						case 3:
						case 4:
							binaryReader.BaseStream.Seek(4L, SeekOrigin.Current);
							break;
						case 5:
						case 6:
							binaryReader.BaseStream.Seek(8L, SeekOrigin.Current);
							i++;
							break;
						case 7:
						case 8:
						case 16:
						case 19:
						case 20:
							binaryReader.BaseStream.Seek(2L, SeekOrigin.Current);
							break;
						case 9:
						case 10:
						case 11:
						case 12:
						case 17:
						case 18:
							binaryReader.BaseStream.Seek(4L, SeekOrigin.Current);
							break;
						case 15:
							binaryReader.BaseStream.Seek(3L, SeekOrigin.Current);
							break;
						case 2:
						case 13:
						case 14:
							return;
						}
					}
				}
				catch
				{
				}
			}
		}

		private static ushort ReadUInt16BE(BinaryReader br)
		{
			byte b = br.ReadByte();
			byte b2 = br.ReadByte();
			return (ushort)((b << 8) | b2);
		}

		private static uint ReadUInt32BE(BinaryReader br)
		{
			byte b = br.ReadByte();
			byte b2 = br.ReadByte();
			byte b3 = br.ReadByte();
			byte b4 = br.ReadByte();
			return (uint)((b << 24) | (b2 << 16) | (b3 << 8) | b4);
		}

		public static List<InternalStringHit> ScanAllKnownModAndDangerLocations()
		{
			List<InternalStringHit> list = new List<InternalStringHit>();
			HashSet<string> hashSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
			List<string> list2 = new List<string>();
			string folderPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
			string folderPath2 = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			string[] array = new string[]
			{
				Path.Combine(folderPath, ".minecraft"),
				Path.Combine(folderPath, "Roaming", ".minecraft"),
				Path.Combine(folderPath, "PrismLauncher"),
				Path.Combine(folderPath, "MultiMC"),
				Path.Combine(folderPath, ".feather"),
				Path.Combine(folderPath2, ".lunarclient"),
				Path.Combine(folderPath2, "curseforge", "minecraft"),
				Path.Combine(folderPath, "ModrinthApp"),
				Path.Combine(folderPath, ".tlauncher"),
				Path.Combine(folderPath, ".flauncher"),
				Path.Combine(folderPath, ".klauncher"),
				Path.Combine(folderPath, "ATLauncher"),
				Path.Combine(folderPath, ".badlion"),
				Path.Combine(folderPath2, ".badlion"),
				Path.Combine(folderPath, ".labymod"),
				Path.Combine(folderPath, "Salwyrr"),
				Path.Combine(folderPath2, ".salwyrr")
			};
			string[] array2 = array;
			foreach (string text in array2)
			{
				if (!Directory.Exists(text))
				{
					continue;
				}
				string text2 = Path.Combine(text, "mods");
				if (Directory.Exists(text2))
				{
					list2.Add(text2);
				}
				string text3 = Path.Combine(text, "config");
				if (Directory.Exists(text3))
				{
					list2.Add(text3);
				}
				string text4 = Path.Combine(text, "versions");
				if (Directory.Exists(text4))
				{
					list2.Add(text4);
				}
				string text5 = Path.Combine(text, "shaderpacks");
				if (Directory.Exists(text5))
				{
					list2.Add(text5);
				}
				list2.Add(text);
				string path = Path.Combine(text, "instances");
				if (!Directory.Exists(path))
				{
					continue;
				}
				try
				{
					string[] directories = Directory.GetDirectories(path, "*", SearchOption.AllDirectories);
					foreach (string text6 in directories)
					{
						if (Path.GetFileName(text6).Equals("mods", StringComparison.OrdinalIgnoreCase))
						{
							list2.Add(text6);
						}
					}
				}
				catch
				{
				}
			}
			string text7 = Path.Combine(folderPath2, "Downloads");
			if (Directory.Exists(text7))
			{
				list2.Add(text7);
			}
			string textTg = Path.Combine(folderPath2, "Downloads", "Telegram Desktop");
			if (Directory.Exists(textTg)) list2.Add(textTg);
			string textTg2 = Path.Combine(folderPath2, "Downloads", "Telegram");
			if (Directory.Exists(textTg2)) list2.Add(textTg2);
			string textDisc = Path.Combine(folderPath2, "Downloads", "Discord");
			if (Directory.Exists(textDisc)) list2.Add(textDisc);
			string textMcBin = Path.Combine(folderPath, ".minecraft", "bin");
			if (Directory.Exists(textMcBin)) list2.Add(textMcBin);
			string folderPath3 = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
			if (Directory.Exists(folderPath3))
			{
				list2.Add(folderPath3);
			}
			string tempPath = Path.GetTempPath();
			if (Directory.Exists(tempPath))
			{
				list2.Add(tempPath);
			}
			string[] array3 = new string[8] { "*.jar", "*.exe", "*.dll", "*.bat", "*.cmd", "*.ps1", "*.json", "*.toml" };
			foreach (string item2 in list2)
			{
				if (!Directory.Exists(item2))
				{
					continue;
				}
				try
				{
					string[] array4 = array3;
					foreach (string searchPattern in array4)
					{
						SearchOption opt = (item2.EndsWith("mods", StringComparison.OrdinalIgnoreCase) || item2.EndsWith("versions", StringComparison.OrdinalIgnoreCase) || item2.EndsWith("shaderpacks", StringComparison.OrdinalIgnoreCase)) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
						string[] files = Directory.GetFiles(item2, searchPattern, opt);
						string[] array5 = files;
						foreach (string text8 in array5)
						{
							if (hashSet.Contains(text8))
							{
								continue;
							}
							hashSet.Add(text8);
							try
							{
								FileInfo fileInfo = new FileInfo(text8);
								if (fileInfo.Length > 0 && fileInfo.Length < 125829120)
								{
									InternalStringHit item = ScanFile(text8);
									list.Add(item);
								}
							}
							catch
							{
							}
						}
					}
				}
				catch
				{
				}
			}
			list.Sort(delegate(InternalStringHit a, InternalStringHit b)
			{
				int num = ((a.Severity == CheckSeverity.Critical) ? 100 : ((a.Severity == CheckSeverity.NeedsCheck) ? 70 : ((a.Severity == CheckSeverity.Warning) ? 40 : 0)));
				int num2 = ((b.Severity == CheckSeverity.Critical) ? 100 : ((b.Severity == CheckSeverity.NeedsCheck) ? 70 : ((b.Severity == CheckSeverity.Warning) ? 40 : 0)));
				return (num != num2) ? num2.CompareTo(num) : b.FileSizeBytes.CompareTo(a.FileSizeBytes);
			});
			return list;
		}

		public static string FormatFileSize(long bytes)
		{
			if (bytes >= 1073741824)
			{
				return string.Format("{0:0.0} ГБ", (double)bytes / 1024.0 / 1024.0 / 1024.0);
			}
			if (bytes >= 1048576)
			{
				return string.Format("{0:0.0} МБ", (double)bytes / 1024.0 / 1024.0);
			}
			if (bytes >= 1024)
			{
				return string.Format("{0:0} КБ", (double)bytes / 1024.0);
			}
			return bytes + " Б";
		}
	}
}
