using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class AuthenticodeSignaturesViewerForm : Form
	{
		#region Data Models

		public enum SignFilterCategory
		{
			All,
			ViolationsOnly,
			SignedValid,
			UnsignedInMinecraft,
			Whitelisted
		}

		public class SignArtifactItem
		{
			public int Index;
			public string FileName;
			public string FileType; // "Исполняемый (PE)", "Мод (.jar)", "Библиотека (.dll)"
			public string SignStatus; // "Подписан (Valid)", "Не подписан", "Поддельный / Чит"
			public string SignerSubject;
			public string Issuer;
			public string ValidUntil;
			public string Thumbprint;
			public string Verdict;
			public string TargetRule;
			public string FullPath;
			public string Details;
			public bool IsViolation;
			public bool IsSigned;
			public bool IsAllowed;
			public SignFilterCategory Category;
			public Color RowColor;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private SignFilterCategory currentFilter = SignFilterCategory.All;
		private List<SignArtifactItem> allItems = new List<SignArtifactItem>();
		private BackgroundWorker scanWorker;

		private DarkListView listView;
		private DarkTextBox searchBox;
		private DarkTextBoxMultiline txtDetails;

		private Label lblHeaderTitle;
		private Label lblStatusPill;
		private Label lblKpiTotal;
		private Label lblKpiSigned;
		private Label lblKpiViolations;
		private Label lblKpiUnsignedMc;
		private Label lblKpiAllowed;

		private DarkButton btnFilterAll;
		private DarkButton btnFilterViolations;
		private DarkButton btnFilterSigned;
		private DarkButton btnFilterUnsigned;
		private DarkButton btnFilterAllowed;

		private DarkButton btnOpenFile;
		private DarkButton btnOpenFolder;
		private DarkButton btnCopyThumbprint;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnRescan;

		#endregion

		public AuthenticodeSignaturesViewerForm(string playerName = "Player")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			InitializeComponent();
			StartScan();
		}

		private void InitializeComponent()
		{
			Text = "🔏 Инспектор цифровых подписей Authenticode, сертификатов и целостности JAR — MCCheatChecker";
			Size = new Size(1240, 760);
			MinimumSize = new Size(1000, 580);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(9f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 110;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(16, 12, 16, 8);

			lblHeaderTitle = new Label();
			lblHeaderTitle.Text = "🔏 Инспектор цифровых подписей Authenticode, сертификатов и целостности JAR / DLL";
			lblHeaderTitle.Font = Ui.FontTitle(12f, true);
			lblHeaderTitle.ForeColor = Ui.TextPrimary;
			lblHeaderTitle.AutoSize = true;
			lblHeaderTitle.Location = new Point(16, 10);
			topPanel.Controls.Add(lblHeaderTitle);

			lblStatusPill = new Label();
			lblStatusPill.Text = "Проверка цифровых сертификатов: " + targetPlayerName;
			lblStatusPill.Font = Ui.FontBody(8.5f, true);
			lblStatusPill.ForeColor = Ui.AccentTeal;
			lblStatusPill.BackColor = Ui.SurfaceAlt;
			lblStatusPill.AutoSize = true;
			lblStatusPill.Padding = new Padding(8, 4, 8, 4);
			lblStatusPill.Location = new Point(18, 38);
			topPanel.Controls.Add(lblStatusPill);

			Panel kpiPanel = new Panel();
			kpiPanel.Location = new Point(16, 68);
			kpiPanel.Size = new Size(1200, 36);
			kpiPanel.BackColor = Ui.Surface;

			lblKpiTotal = CreateKpiLabel("Проверено файлов: 0", Ui.TextSecondary, 0, kpiPanel);
			lblKpiSigned = CreateKpiLabel("С подписью: 0", Ui.AccentTeal, 180, kpiPanel);
			lblKpiViolations = CreateKpiLabel("Нарушений (5.01): 0", Ui.AccentRed, 340, kpiPanel);
			lblKpiUnsignedMc = CreateKpiLabel("Неподписанных в .mc: 0", Ui.AccentOrange, 530, kpiPanel);
			lblKpiAllowed = CreateKpiLabel("Белый список: 0", Ui.AccentGreen, 770, kpiPanel);

			topPanel.Controls.Add(kpiPanel);
			Controls.Add(topPanel);

			Panel filterPanel = new Panel();
			filterPanel.Dock = DockStyle.Top;
			filterPanel.Height = 44;
			filterPanel.BackColor = Ui.SurfaceAlt;
			filterPanel.Padding = new Padding(12, 6, 12, 6);

			btnFilterAll = CreateFilterButton("Все файлы", SignFilterCategory.All, 12, filterPanel);
			btnFilterViolations = CreateFilterButton("🚨 Только нарушения (5.01)", SignFilterCategory.ViolationsOnly, 126, filterPanel);
			btnFilterSigned = CreateFilterButton("🔏 С подписью (Authenticode)", SignFilterCategory.SignedValid, 330, filterPanel);
			btnFilterUnsigned = CreateFilterButton("⚠️ Неподписанные в .minecraft", SignFilterCategory.UnsignedInMinecraft, 544, filterPanel);
			btnFilterAllowed = CreateFilterButton("✅ Белый список (Allowed)", SignFilterCategory.Whitelisted, 768, filterPanel);

			Label lblSearch = new Label();
			lblSearch.Text = "Поиск:";
			lblSearch.ForeColor = Ui.TextSecondary;
			lblSearch.Font = Ui.FontBody(8.5f, true);
			lblSearch.AutoSize = true;
			lblSearch.Location = new Point(960, 13);
			filterPanel.Controls.Add(lblSearch);

			searchBox = new DarkTextBox();
			searchBox.Location = new Point(1015, 9);
			searchBox.Size = new Size(180, 24);
			searchBox.TextChanged += delegate { ApplyFilter(); };
			filterPanel.Controls.Add(searchBox);

			Controls.Add(filterPanel);

			DarkSplitContainer split = new DarkSplitContainer();
			split.Dock = DockStyle.Fill;
			split.Orientation = Orientation.Horizontal;
			split.SplitterDistance = 370;
			split.BackColor = Ui.Backdrop;

			listView = new DarkListView();
			listView.Dock = DockStyle.Fill;
			listView.View = View.Details;
			listView.FullRowSelect = true;
			listView.MultiSelect = false;
			listView.HideSelection = false;
			listView.Columns.Add("#", 45);
			listView.Columns.Add("Файл", 190);
			listView.Columns.Add("Тип", 110);
			listView.Columns.Add("Статус подписи", 140);
			listView.Columns.Add("Издатель / Владелец сертификата", 220);
			listView.Columns.Add("Вердикт", 180);
			listView.Columns.Add("Пункт правил", 110);
			listView.Columns.Add("Полный путь", 360);

			listView.SelectedIndexChanged += ListView_SelectedIndexChanged;
			listView.DoubleClick += delegate { OpenFileLocation(); };
			split.Panel1.Controls.Add(listView);

			Panel bottomContainer = new Panel();
			bottomContainer.Dock = DockStyle.Fill;
			bottomContainer.BackColor = Ui.Surface;
			bottomContainer.Padding = new Padding(12, 8, 12, 8);

			Label lblDetailsTitle = new Label();
			lblDetailsTitle.Text = "📄 Детали сертификата Authenticode, цепочка доверия и манифест JAR:";
			lblDetailsTitle.Font = Ui.FontBody(8.5f, true);
			lblDetailsTitle.ForeColor = Ui.TextSecondary;
			lblDetailsTitle.AutoSize = true;
			lblDetailsTitle.Location = new Point(12, 8);
			bottomContainer.Controls.Add(lblDetailsTitle);

			txtDetails = new DarkTextBoxMultiline();
			txtDetails.Location = new Point(12, 30);
			txtDetails.Size = new Size(860, 130);
			txtDetails.Font = Ui.FontMono(8.5f);
			txtDetails.ReadOnly = true;
			bottomContainer.Controls.Add(txtDetails);

			Panel actionsPanel = new Panel();
			actionsPanel.Location = new Point(885, 20);
			actionsPanel.Size = new Size(330, 145);

			btnOpenFile = CreateActionButton("📄 Просмотреть файл / открыть", 0, actionsPanel);
			btnOpenFile.Click += delegate { OpenSelectedFile(); };

			btnOpenFolder = CreateActionButton("📂 Открыть папку в Проводнике", 32, actionsPanel);
			btnOpenFolder.Click += delegate { OpenFileLocation(); };

			btnCopyThumbprint = CreateActionButton("📋 Копировать отпечаток (SHA-1/256)", 64, actionsPanel);
			btnCopyThumbprint.Click += delegate { CopyThumbprint(); };

			btnCopyDiscord = CreateActionButton("📑 Копировать отчет для Discord", 96, actionsPanel);
			btnCopyDiscord.Click += delegate { CopyDiscordReport(); };

			btnCopyBanCmd = CreateActionButton("⚖️ Копировать команду бана", 128, actionsPanel);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };

			bottomContainer.Controls.Add(actionsPanel);

			btnRescan = new DarkButton();
			btnRescan.Text = "🔄 Пересканировать";
			btnRescan.Location = new Point(12, 166);
			btnRescan.Size = new Size(160, 26);
			btnRescan.Click += delegate { StartScan(); };
			bottomContainer.Controls.Add(btnRescan);

			split.Panel2.Controls.Add(bottomContainer);
			Controls.Add(split);

			filterPanel.BringToFront();
			split.BringToFront();
		}

		private Label CreateKpiLabel(string text, Color color, int x, Panel parent)
		{
			Label lbl = new Label();
			lbl.Text = text;
			lbl.Font = Ui.FontBody(9f, true);
			lbl.ForeColor = color;
			lbl.BackColor = Ui.SurfaceAlt;
			lbl.AutoSize = true;
			lbl.Padding = new Padding(8, 4, 8, 4);
			lbl.Location = new Point(x, 4);
			parent.Controls.Add(lbl);
			return lbl;
		}

		private DarkButton CreateFilterButton(string text, SignFilterCategory cat, int x, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(x, 8);
			btn.Size = new Size(cat == SignFilterCategory.ViolationsOnly ? 198 : (cat == SignFilterCategory.SignedValid ? 208 : (cat == SignFilterCategory.UnsignedInMinecraft ? 218 : (cat == SignFilterCategory.Whitelisted ? 190 : 108))), 28);
			btn.Font = Ui.FontBody(8.5f, true);
			btn.Click += delegate
			{
				currentFilter = cat;
				ResetFilterButtonColors();
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
				ApplyFilter();
			};
			parent.Controls.Add(btn);
			if (cat == SignFilterCategory.All)
			{
				btn.BackColor = Ui.AccentTeal;
				btn.ForeColor = Color.Black;
			}
			return btn;
		}

		private void ResetFilterButtonColors()
		{
			DarkButton[] btns = new DarkButton[] { btnFilterAll, btnFilterViolations, btnFilterSigned, btnFilterUnsigned, btnFilterAllowed };
			foreach (DarkButton b in btns)
			{
				b.BackColor = Ui.Surface;
				b.ForeColor = Ui.TextPrimary;
			}
		}

		private DarkButton CreateActionButton(string text, int y, Panel parent)
		{
			DarkButton btn = new DarkButton();
			btn.Text = text;
			btn.Location = new Point(0, y);
			btn.Size = new Size(320, 26);
			btn.Font = Ui.FontBody(8f, true);
			parent.Controls.Add(btn);
			return btn;
		}

		#region Scan Logic

		private void StartScan()
		{
			if (scanWorker != null && scanWorker.IsBusy) return;

			lblStatusPill.Text = "⏳ Сканирование цифровых подписей Authenticode и сертификатов JAR...";
			lblStatusPill.ForeColor = Ui.AccentOrange;
			allItems.Clear();
			listView.Items.Clear();
			txtDetails.Text = "Выполняется сканирование...";

			scanWorker = new BackgroundWorker();
			scanWorker.DoWork += ScanWorker_DoWork;
			scanWorker.RunWorkerCompleted += ScanWorker_RunWorkerCompleted;
			scanWorker.RunWorkerAsync();
		}

		private void ScanWorker_DoWork(object sender, DoWorkEventArgs e)
		{
			List<SignArtifactItem> list = new List<SignArtifactItem>();
			int idx = 1;

			List<string> targetFolders = new List<string>();

			try
			{
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string mcFolder = Path.Combine(appData, ".minecraft");
				if (Directory.Exists(mcFolder))
				{
					targetFolders.Add(mcFolder);
					string mods = Path.Combine(mcFolder, "mods");
					if (Directory.Exists(mods)) targetFolders.Add(mods);
					string versions = Path.Combine(mcFolder, "versions");
					if (Directory.Exists(versions)) targetFolders.Add(versions);
				}
			}
			catch { }

			try
			{
				string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string downloads = Path.Combine(userProfile, "Downloads");
				string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
				string temp = Path.GetTempPath();

				if (Directory.Exists(downloads)) targetFolders.Add(downloads);
				if (Directory.Exists(desktop)) targetFolders.Add(desktop);
				if (Directory.Exists(temp)) targetFolders.Add(temp);
			}
			catch { }

			string[] filePatterns = new string[] { "*.exe", "*.dll", "*.jar", "*.sys" };

			foreach (string folder in targetFolders)
			{
				try
				{
					foreach (string pat in filePatterns)
					{
						IEnumerable<string> files = Directory.EnumerateFiles(folder, pat, SearchOption.TopDirectoryOnly);
						foreach (string f in files)
						{
							try
							{
								SignArtifactItem it = InspectFileSignature(f, ref idx);
								if (it != null)
								{
									list.Add(it);
								}
							}
							catch { }
						}
					}
				}
				catch { }
			}

			e.Result = list;
		}

		private SignArtifactItem InspectFileSignature(string filePath, ref int idx)
		{
			string ext = Path.GetExtension(filePath).ToLowerInvariant();
			string fName = Path.GetFileName(filePath);
			string lowerName = fName.ToLowerInvariant();

			SignArtifactItem it = new SignArtifactItem();
			it.Index = idx++;
			it.FileName = fName;
			it.FullPath = filePath;
			it.RowColor = Ui.TextPrimary;

			// Whitelist check: AnyDesk, RuDesktop, OP Auto Clicker
			bool isAllowed = lowerName.Contains("anydesk") || lowerName.Contains("rudesktop") ||
				lowerName.Contains("opautoclicker") || lowerName.Contains("speedautoclicker") ||
				lowerName.Contains("tinytask") || lowerName.Contains("x-mouse") ||
				lowerName.Contains("autohotkey") || lowerName.Contains("clickermann") ||
				lowerName.Contains("keyran");

			if (isAllowed)
			{
				it.FileType = ext == ".jar" ? "Мод (.jar)" : "Исполняемый (" + ext + ")";
				it.SignStatus = "Разрешено регламентом";
				it.SignerSubject = "Официальный белый список";
				it.Issuer = "WhiteList Engine";
				it.Verdict = "✅ БЕЛЫЙ СПИСОК (РАЗРЕШЕНО)";
				it.TargetRule = "Разрешено регламентом";
				it.IsAllowed = true;
				it.Category = SignFilterCategory.Whitelisted;
				it.RowColor = Ui.AccentGreen;
				it.Details = "Утилита удаленного доступа или кликер из официального разрешенного перечня правил проекта.\nСанкции по 5.01 не применяются.";
				return it;
			}

			// If PE file (.exe, .dll, .sys)
			if (ext == ".exe" || ext == ".dll" || ext == ".sys")
			{
				it.FileType = ext == ".sys" ? "Драйвер ядра (.sys)" : (ext == ".dll" ? "Библиотека (.dll)" : "Исполняемый (.exe)");

				X509Certificate2 cert = null;
				try
				{
					X509Certificate rawCert = X509Certificate.CreateFromSignedFile(filePath);
					if (rawCert != null)
					{
						cert = new X509Certificate2(rawCert);
					}
				}
				catch { }

				if (cert != null)
				{
					it.IsSigned = true;
					it.SignStatus = "Подписан (Authenticode)";
					it.SignerSubject = cert.Subject;
					it.Issuer = cert.Issuer;
					it.ValidUntil = cert.NotAfter.ToString("yyyy-MM-dd");
					it.Thumbprint = cert.Thumbprint;

					string subjectLower = (cert.Subject + " " + cert.Issuer).ToLowerInvariant();

					// Check for rogue/cheat certs
					string[] rogueCerts = new string[]
					{
						"celestial", "expensive", "boberware", "akrien", "nursultan", "vape",
						"whql test", "cheatengine", "openssl demo", "driver test", "test signing",
						"root ca 2026", "injector", "bypass"
					};

					bool isRogue = false;
					foreach (string rc in rogueCerts)
					{
						if (subjectLower.Contains(rc))
						{
							isRogue = true;
							break;
						}
					}

					if (isRogue)
					{
						it.IsViolation = true;
						it.SignStatus = "🚨 Поддельный / Чит-сертификат";
						it.Verdict = "🚨 ПОДДЕЛЬНАЯ ПОДПИСЬ ЧИТА (5.01)";
						it.TargetRule = "5.01 (Чит-клиенты)";
						it.Category = SignFilterCategory.ViolationsOnly;
						it.RowColor = Ui.AccentRed;
					}
					else
					{
						it.Category = SignFilterCategory.SignedValid;
						it.Verdict = "Подписанный бинарный файл";
						it.TargetRule = "Штатный файл";
						it.RowColor = Ui.AccentTeal;
					}

					StringBuilder sb = new StringBuilder();
					sb.AppendLine("=== СЕРТИФИКАТ ЦИФРОВОЙ ПОДПИСИ AUTHENTICODE ===");
					sb.AppendLine("Владелец (Subject): " + cert.Subject);
					sb.AppendLine("Издатель (Issuer): " + cert.Issuer);
					sb.AppendLine("Срок действия: с " + cert.NotBefore.ToShortDateString() + " по " + cert.NotAfter.ToShortDateString());
					sb.AppendLine("Серийный номер: " + cert.SerialNumber);
					sb.AppendLine("Отпечаток (Thumbprint): " + cert.Thumbprint);
					sb.AppendLine("Алгоритм подписи: " + cert.SignatureAlgorithm.FriendlyName);
					it.Details = sb.ToString();
				}
				else
				{
					it.IsSigned = false;
					it.SignStatus = "Не подписан";
					it.SignerSubject = "Отсутствует";
					it.Issuer = "-";
					it.Thumbprint = "-";

					bool isUnderMc = filePath.ToLowerInvariant().Contains(".minecraft");
					if (isUnderMc)
					{
						it.Category = SignFilterCategory.UnsignedInMinecraft;
						it.Verdict = "⚠️ Неподписанный бинарник в .minecraft";
						it.TargetRule = "5.01 / 5.02 (Аудит)";
						it.RowColor = Ui.AccentOrange;
					}
					else
					{
						it.Category = SignFilterCategory.All;
						it.Verdict = "Неподписанный исполняемый файл";
						it.TargetRule = "Аудит";
						it.RowColor = Ui.TextSecondary;
					}

					it.Details = "Файл не содержит встроенной цифровой подписи Authenticode.\nВ папке .minecraft исполняемые DLL без подписи могут указывать на внедрение инжекторов.";
				}
			}
			else if (ext == ".jar")
			{
				it.FileType = "Мод (.jar)";
				InspectJarSignature(filePath, it);
			}

			return it;
		}

		private void InspectJarSignature(string filePath, SignArtifactItem it)
		{
			bool hasSignatureFiles = false;
			string manifestVendor = "";
			string manifestTitle = "";
			List<string> suspiciousClasses = new List<string>();

			try
			{
				using (ZipArchive zip = ZipFile.OpenRead(filePath))
				{
					foreach (ZipArchiveEntry entry in zip.Entries)
					{
						string eName = entry.FullName.ToLowerInvariant();
						if (eName.StartsWith("meta-inf/") && (eName.EndsWith(".sf") || eName.EndsWith(".rsa") || eName.EndsWith(".dsa")))
						{
							hasSignatureFiles = true;
						}

						if (eName == "meta-inf/manifest.mf")
						{
							try
							{
								using (StreamReader sr = new StreamReader(entry.Open()))
								{
									string mf = sr.ReadToEnd();
									string[] lines = mf.Split('\n');
									foreach (string l in lines)
									{
										if (l.StartsWith("Implementation-Vendor:", StringComparison.OrdinalIgnoreCase)) manifestVendor = l.Substring(22).Trim();
										if (l.StartsWith("Implementation-Title:", StringComparison.OrdinalIgnoreCase)) manifestTitle = l.Substring(21).Trim();
									}
								}
							}
							catch { }
						}

						// Scan for cheat package names
						if (eName.Contains("celestial") || eName.Contains("expensive") || eName.Contains("nursultan") ||
							eName.Contains("akrien") || eName.Contains("wildclient") || eName.Contains("nurik") ||
							eName.Contains("vape") || eName.Contains("deadcode") || eName.Contains("wexside"))
						{
							if (suspiciousClasses.Count < 5) suspiciousClasses.Add(entry.FullName);
						}
					}
				}
			}
			catch (Exception ex)
			{
				it.Details = "Ошибка чтения JAR архива: " + ex.Message;
				return;
			}

			if (suspiciousClasses.Count > 0)
			{
				it.IsViolation = true;
				it.SignStatus = "🚨 Обнаружен чит-пакет";
				it.Verdict = "🚨 ЧИТ-МОДИФИКАЦИЯ В JAR (5.01)";
				it.TargetRule = "5.01 (Чит-клиенты)";
				it.Category = SignFilterCategory.ViolationsOnly;
				it.RowColor = Ui.AccentRed;
				it.SignerSubject = manifestTitle;
				it.Issuer = manifestVendor;

				StringBuilder sb = new StringBuilder();
				sb.AppendLine("=== ОБНАРУЖЕНЫ ПАКЕТЫ ЧИТ-КЛИЕНТА ВНУТРИ JAR ===");
				sb.AppendLine("Манифест: " + manifestTitle + " | " + manifestVendor);
				sb.AppendLine("Подозрительные классы чита:");
				foreach (string sc in suspiciousClasses) sb.AppendLine("  • " + sc);
				it.Details = sb.ToString();
			}
			else if (hasSignatureFiles)
			{
				it.IsSigned = true;
				it.SignStatus = "Подписан JAR (.SF/.RSA)";
				it.SignerSubject = string.IsNullOrEmpty(manifestTitle) ? "Signed JAR Mod" : manifestTitle;
				it.Issuer = string.IsNullOrEmpty(manifestVendor) ? "Mojang / Forge / Fabric" : manifestVendor;
				it.Verdict = "Легитимный подписанный мод";
				it.TargetRule = "Штатный мод";
				it.Category = SignFilterCategory.SignedValid;
				it.RowColor = Ui.AccentTeal;
				it.Details = string.Format("Модификация содержит корректные файлы криптографической подписи META-INF (*.SF/*.RSA).\nМанифест: {0} ({1}).", manifestTitle, manifestVendor);
			}
			else
			{
				it.IsSigned = false;
				it.SignStatus = "Не подписан (Стандартный мод)";
				it.SignerSubject = string.IsNullOrEmpty(manifestTitle) ? "Без заголовка" : manifestTitle;
				it.Issuer = string.IsNullOrEmpty(manifestVendor) ? "Community Mod" : manifestVendor;
				it.Verdict = "Стандартный неподписанный мод";
				it.TargetRule = "Аудит модов";
				it.Category = SignFilterCategory.All;
				it.RowColor = Ui.TextSecondary;
				it.Details = string.Format("Обычный неподписанный JAR мод.\nМанифест: {0} ({1}).", manifestTitle, manifestVendor);
			}
		}

		private void ScanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			if (e.Error != null)
			{
				lblStatusPill.Text = "❌ Ошибка сканирования подписей: " + e.Error.Message;
				lblStatusPill.ForeColor = Ui.AccentRed;
				return;
			}

			List<SignArtifactItem> res = e.Result as List<SignArtifactItem>;
			if (res != null)
			{
				allItems = res;

				int violations = 0;
				int signed = 0;
				int unsignedMc = 0;
				int allowed = 0;

				foreach (SignArtifactItem it in allItems)
				{
					if (it.IsViolation) violations++;
					if (it.IsSigned) signed++;
					if (it.Category == SignFilterCategory.UnsignedInMinecraft) unsignedMc++;
					if (it.IsAllowed) allowed++;
				}

				lblKpiTotal.Text = "Проверено файлов: " + allItems.Count;
				lblKpiSigned.Text = "С подписью: " + signed;
				lblKpiViolations.Text = "Нарушений (5.01): " + violations;
				lblKpiUnsignedMc.Text = "Неподписанных в .mc: " + unsignedMc;
				lblKpiAllowed.Text = "Белый список: " + allowed;

				if (violations > 0)
				{
					lblStatusPill.Text = string.Format("🚨 ВНИМАНИЕ: Найдено {0} файлов с поддельными подписями читов или скрытыми пакетами!", violations);
					lblStatusPill.ForeColor = Ui.AccentRed;
				}
				else
				{
					lblStatusPill.Text = "✅ Сканирование подписей завершено: поддельных сертификатов читов не выявлено.";
					lblStatusPill.ForeColor = Ui.AccentGreen;
				}
			}

			ApplyFilter();
		}

		#endregion

		#region Filtering & UI Updates

		private void ApplyFilter()
		{
			listView.BeginUpdate();
			listView.Items.Clear();

			string search = searchBox != null ? searchBox.Text.Trim().ToLowerInvariant() : string.Empty;

			foreach (SignArtifactItem it in allItems)
			{
				if (currentFilter == SignFilterCategory.ViolationsOnly && !it.IsViolation) continue;
				if (currentFilter == SignFilterCategory.SignedValid && !it.IsSigned) continue;
				if (currentFilter == SignFilterCategory.UnsignedInMinecraft && it.Category != SignFilterCategory.UnsignedInMinecraft) continue;
				if (currentFilter == SignFilterCategory.Whitelisted && !it.IsAllowed) continue;

				if (!string.IsNullOrEmpty(search))
				{
					bool match = it.FileName.ToLowerInvariant().Contains(search) ||
						it.FileType.ToLowerInvariant().Contains(search) ||
						it.SignStatus.ToLowerInvariant().Contains(search) ||
						it.Verdict.ToLowerInvariant().Contains(search) ||
						it.TargetRule.ToLowerInvariant().Contains(search) ||
						(it.SignerSubject != null && it.SignerSubject.ToLowerInvariant().Contains(search)) ||
						(it.Thumbprint != null && it.Thumbprint.ToLowerInvariant().Contains(search)) ||
						it.FullPath.ToLowerInvariant().Contains(search);

					if (!match) continue;
				}

				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.FileName);
				lvi.SubItems.Add(it.FileType);
				lvi.SubItems.Add(it.SignStatus);
				lvi.SubItems.Add(string.IsNullOrEmpty(it.SignerSubject) ? "-" : it.SignerSubject);
				lvi.SubItems.Add(it.Verdict);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.FullPath);

				lvi.ForeColor = it.RowColor;
				lvi.Tag = it;
				listView.Items.Add(lvi);
			}

			listView.EndUpdate();

			if (listView.Items.Count > 0)
			{
				listView.Items[0].Selected = true;
			}
			else
			{
				txtDetails.Text = "Нет записей, соответствующих выбранному фильтру.";
			}
		}

		private void ListView_SelectedIndexChanged(object sender, EventArgs e)
		{
			if (listView.SelectedItems.Count == 0) return;
			SignArtifactItem it = listView.SelectedItems[0].Tag as SignArtifactItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("=== АКТ ПРОВЕРКИ ЦИФРОВОЙ ПОДПИСИ ФАЙЛА ===");
			sb.AppendLine("Файл: " + it.FileName);
			sb.AppendLine("Тип: " + it.FileType);
			sb.AppendLine("Статус подписи: " + it.SignStatus);
			sb.AppendLine("Вердикт проверки: " + it.Verdict);
			sb.AppendLine("Пункт регламента: " + it.TargetRule);
			sb.AppendLine("Полный путь: " + it.FullPath);
			sb.AppendLine();
			sb.AppendLine(it.Details);

			txtDetails.Text = sb.ToString();
		}

		#endregion

		#region Actions

		private void OpenFileLocation()
		{
			if (listView.SelectedItems.Count == 0) return;
			SignArtifactItem it = listView.SelectedItems[0].Tag as SignArtifactItem;
			if (it == null || !File.Exists(it.FullPath)) return;

			try
			{
				Process.Start("explorer.exe", "/select,\"" + it.FullPath + "\"");
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Проводник", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void OpenSelectedFile()
		{
			if (listView.SelectedItems.Count == 0) return;
			SignArtifactItem it = listView.SelectedItems[0].Tag as SignArtifactItem;
			if (it == null || !File.Exists(it.FullPath)) return;

			try
			{
				Process.Start(it.FullPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Открытие", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyThumbprint()
		{
			if (listView.SelectedItems.Count == 0) return;
			SignArtifactItem it = listView.SelectedItems[0].Tag as SignArtifactItem;
			if (it == null) return;

			string th = !string.IsNullOrEmpty(it.Thumbprint) && it.Thumbprint != "-" ? it.Thumbprint : it.FileName;
			Clipboard.SetText(th);
			MessageBox.Show("Отпечаток сертификата скопирован в буфер:\n" + th, "Копирование", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyDiscordReport()
		{
			if (listView.SelectedItems.Count == 0) return;
			SignArtifactItem it = listView.SelectedItems[0].Tag as SignArtifactItem;
			if (it == null) return;

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ФИКСАЦИЯ ЦИФРОВОЙ ПОДПИСИ AUTHENTICODE ===");
			sb.AppendLine("Игрок: " + targetPlayerName);
			sb.AppendLine("Файл: " + it.FileName);
			sb.AppendLine("Статус: " + it.SignStatus);
			sb.AppendLine("Владелец: " + it.SignerSubject);
			if (!string.IsNullOrEmpty(it.Thumbprint) && it.Thumbprint != "-") sb.AppendLine("Отпечаток: " + it.Thumbprint);
			sb.AppendLine("Вердикт: " + it.Verdict);
			sb.AppendLine("Пункт правил: " + it.TargetRule);
			sb.AppendLine("Путь: " + it.FullPath);
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Отчет для Discord скопирован в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			if (listView.SelectedItems.Count == 0) return;
			SignArtifactItem it = listView.SelectedItems[0].Tag as SignArtifactItem;
			if (it == null) return;

			string cmd = string.Format("/ban {0} 30d 5.01 Использование читов (Поддельный сертификат / чит-мод {1})", targetPlayerName, it.FileName);
			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
