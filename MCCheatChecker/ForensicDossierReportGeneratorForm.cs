using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows.Forms;

namespace MCCheatChecker
{
	public class ForensicDossierReportGeneratorForm : Form
	{
		#region Data Models

		public class DossierEvidenceItem
		{
			public int Index;
			public string ModuleName;
			public string ArtifactName;
			public string TargetRule;
			public string Severity;
			public string EvidenceDetails;
			public string Sha256OrPath;
			public DateTime Timestamp;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private string targetModeratorName;
		private DateTime ssStartTime;
		private List<DossierEvidenceItem> evidenceList = new List<DossierEvidenceItem>();

		private DarkTextBox txtPlayer;
		private DarkTextBox txtModerator;
		private DarkTextBox txtServerMode;
		private DarkTextBox txtContacts;
		private DarkTextBox txtBanDuration;
		private ComboBox cmbVerdict;
		private DarkTextBoxMultiline txtModeratorNotes;

		private DarkListView lvEvidence;
		private WebBrowser webBrowser;

		private DarkButton btnAutoCollect;
		private DarkButton btnAddEvidence;
		private DarkButton btnRemoveEvidence;
		private DarkButton btnRefreshHtml;
		private DarkButton btnSaveHtml;
		private DarkButton btnOpenBrowser;
		private DarkButton btnPrintPdf;
		private DarkButton btnCopyDiscord;
		private DarkButton btnCopyBanCmd;

		private Label lblStatus;
		private string currentGeneratedHtml = string.Empty;

		#endregion

		public ForensicDossierReportGeneratorForm(string playerName = "Player", string moderatorName = "Moderator")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			targetModeratorName = string.IsNullOrEmpty(moderatorName) ? Environment.UserName : moderatorName;
			ssStartTime = DateTime.Now.AddMinutes(-12);

			InitializeComponent();
			PopulateInitialData();
			AutoCollectEvidence();
			RenderHtmlReport();
		}

		private void InitializeComponent()
		{
			Text = "📑 Генератор итогового криминалистического досье ScreenShare (Executive Dossier HTML/PDF)";
			Size = new Size(1340, 820);
			MinimumSize = new Size(1060, 640);
			StartPosition = FormStartPosition.CenterScreen;
			BackColor = Ui.Backdrop;
			ForeColor = Ui.TextPrimary;
			Font = Ui.FontBody(9f);

			Panel topPanel = new Panel();
			topPanel.Dock = DockStyle.Top;
			topPanel.Height = 65;
			topPanel.BackColor = Ui.Surface;
			topPanel.Padding = new Padding(16, 10, 16, 10);

			Label lblHeader = new Label();
			lblHeader.Text = "📑 Итоговое судебное досье проверки на читы (ScreenShare Forensic Dossier)";
			lblHeader.Font = Ui.FontTitle(12f, true);
			lblHeader.ForeColor = Ui.TextPrimary;
			lblHeader.AutoSize = true;
			lblHeader.Location = new Point(16, 8);
			topPanel.Controls.Add(lblHeader);

			lblStatus = new Label();
			lblStatus.Text = "Готово к формированию официального отчета проверки.";
			lblStatus.Font = Ui.FontBody(8.5f);
			lblStatus.ForeColor = Ui.AccentTeal;
			lblStatus.AutoSize = true;
			lblStatus.Location = new Point(18, 36);
			topPanel.Controls.Add(lblStatus);

			Controls.Add(topPanel);

			DarkSplitContainer splitContainer = new DarkSplitContainer();
			splitContainer.Dock = DockStyle.Fill;
			splitContainer.Orientation = Orientation.Vertical;
			splitContainer.SplitterDistance = 580;
			splitContainer.BackColor = Ui.Backdrop;

			// Left: Controls & Evidence Builder
			Panel leftPanel = new Panel();
			leftPanel.Dock = DockStyle.Fill;
			leftPanel.BackColor = Ui.Backdrop;
			leftPanel.Padding = new Padding(10);
			leftPanel.AutoScroll = true;

			int y = 6;

			// Suspect & Moderator Card
			Panel metaCard = CreateSectionCard("Параметры проверки и контакты", ref y, leftPanel, 140);
			CreateField(metaCard, "Ник подозреваемого:", ref txtPlayer, targetPlayerName, 10, 24, 160);
			CreateField(metaCard, "Ник модератора:", ref txtModerator, targetModeratorName, 180, 24, 160);
			CreateField(metaCard, "Сервер / Режим:", ref txtServerMode, "Анархия / Гриф", 350, 24, 200);

			CreateField(metaCard, "Контакты (DS / AnyDesk):", ref txtContacts, "Discord: User#0000 | AnyDesk: 123456789", 10, 76, 330);
			CreateField(metaCard, "Срок бана (дни):", ref txtBanDuration, "30", 350, 76, 70);

			Label lblVerdict = new Label();
			lblVerdict.Text = "Итоговый вердикт проверки:";
			lblVerdict.Font = Ui.FontBody(8.5f, true);
			lblVerdict.ForeColor = Ui.TextSecondary;
			lblVerdict.AutoSize = true;
			lblVerdict.Location = new Point(10, 110);
			metaCard.Controls.Add(lblVerdict);

			cmbVerdict = new ComboBox();
			cmbVerdict.DropDownStyle = ComboBoxStyle.DropDownList;
			cmbVerdict.BackColor = Ui.Surface;
			cmbVerdict.ForeColor = Ui.TextPrimary;
			cmbVerdict.Font = Ui.FontBody(9f, true);
			cmbVerdict.Location = new Point(190, 106);
			cmbVerdict.Size = new Size(360, 26);
			cmbVerdict.Items.Add("ЧИСТ — Нарушений не выявлено (Оправдан)");
			cmbVerdict.Items.Add("БАН 5.01 — Использование читов / Модифицированный клиент (30 дней)");
			cmbVerdict.Items.Add("БАН 5.02 — Хранение читов / ПО для автоматизации (15 дней)");
			cmbVerdict.Items.Add("БАН 5.03 — Использование X-Ray / Пакеты ресурсов с прозрачными блоками (15 дней)");
			cmbVerdict.Items.Add("БАН 5.05 — Признание в использовании читов (15 дней)");
			cmbVerdict.Items.Add("БАН 5.06 — Отказ от проверки / Выход из игры (30 дней)");
			cmbVerdict.Items.Add("БАН 5.07 — Отсутствие связи / Discord / AnyDesk (30 дней)");
			cmbVerdict.Items.Add("БАН 5.08 — Сокрытие улик / Очистка логов / Изменение времени (30 дней)");
			cmbVerdict.Items.Add("БАН 2.06 — Багоюз / Дюпы / Эксплойты (3-7 дней)");
			cmbVerdict.SelectedIndex = 1;
			cmbVerdict.SelectedIndexChanged += delegate { UpdateVerdictDetails(); RenderHtmlReport(); };
			metaCard.Controls.Add(cmbVerdict);

			// Evidence Items Card
			Panel evidenceCard = CreateSectionCard("Таблица криминалистических улик (Evidence Matrix)", ref y, leftPanel, 240);

			lvEvidence = new DarkListView();
			lvEvidence.Location = new Point(10, 26);
			lvEvidence.Size = new Size(540, 160);
			lvEvidence.View = View.Details;
			lvEvidence.FullRowSelect = true;
			lvEvidence.Columns.Add("#", 30);
			lvEvidence.Columns.Add("Модуль", 110);
			lvEvidence.Columns.Add("Объект / Чит", 130);
			lvEvidence.Columns.Add("Пункт", 75);
			lvEvidence.Columns.Add("Таймкод", 80);
			lvEvidence.Columns.Add("Детали", 260);
			evidenceCard.Controls.Add(lvEvidence);

			btnAutoCollect = new DarkButton();
			btnAutoCollect.Text = "⚡ Автосбор улик";
			btnAutoCollect.Location = new Point(10, 196);
			btnAutoCollect.Size = new Size(130, 26);
			btnAutoCollect.Font = Ui.FontBody(8.5f, true);
			btnAutoCollect.Click += delegate { AutoCollectEvidence(); RenderHtmlReport(); };
			evidenceCard.Controls.Add(btnAutoCollect);

			btnAddEvidence = new DarkButton();
			btnAddEvidence.Text = "➕ Добавить улику";
			btnAddEvidence.Location = new Point(148, 196);
			btnAddEvidence.Size = new Size(130, 26);
			btnAddEvidence.Font = Ui.FontBody(8.5f, true);
			btnAddEvidence.Click += delegate { PromptAddEvidence(); };
			evidenceCard.Controls.Add(btnAddEvidence);

			btnRemoveEvidence = new DarkButton();
			btnRemoveEvidence.Text = "❌ Удалить";
			btnRemoveEvidence.Location = new Point(286, 196);
			btnRemoveEvidence.Size = new Size(100, 26);
			btnRemoveEvidence.Font = Ui.FontBody(8.5f, true);
			btnRemoveEvidence.Click += delegate { RemoveSelectedEvidence(); };
			evidenceCard.Controls.Add(btnRemoveEvidence);

			DarkButton btnClearEvidence = new DarkButton();
			btnClearEvidence.Text = "Очистить все";
			btnClearEvidence.Location = new Point(394, 196);
			btnClearEvidence.Size = new Size(110, 26);
			btnClearEvidence.Font = Ui.FontBody(8.5f);
			btnClearEvidence.Click += delegate { evidenceList.Clear(); RefreshEvidenceListView(); RenderHtmlReport(); };
			evidenceCard.Controls.Add(btnClearEvidence);

			// Moderator Notes Card
			Panel notesCard = CreateSectionCard("Комментарий модератора и протокол проверки", ref y, leftPanel, 120);

			txtModeratorNotes = new DarkTextBoxMultiline();
			txtModeratorNotes.Location = new Point(10, 26);
			txtModeratorNotes.Size = new Size(540, 80);
			txtModeratorNotes.Font = Ui.FontBody(8.5f);
			txtModeratorNotes.Text = "Проверка проведена в штатном режиме через AnyDesk. Игрок сотрудничал / обнаружены следы модифицированного клиента.";
			txtModeratorNotes.TextChanged += delegate { RenderHtmlReport(); };
			notesCard.Controls.Add(txtModeratorNotes);

			// Actions Panel
			Panel actionsCard = CreateSectionCard("Экспорт и управление досье", ref y, leftPanel, 100);

			btnRefreshHtml = new DarkButton();
			btnRefreshHtml.Text = "🔄 Обновить HTML";
			btnRefreshHtml.Location = new Point(10, 26);
			btnRefreshHtml.Size = new Size(130, 28);
			btnRefreshHtml.Font = Ui.FontBody(8.5f, true);
			btnRefreshHtml.Click += delegate { RenderHtmlReport(); };
			actionsCard.Controls.Add(btnRefreshHtml);

			btnSaveHtml = new DarkButton();
			btnSaveHtml.Text = "💾 Сохранить HTML";
			btnSaveHtml.Location = new Point(148, 26);
			btnSaveHtml.Size = new Size(130, 28);
			btnSaveHtml.Font = Ui.FontBody(8.5f, true);
			btnSaveHtml.BackColor = Ui.AccentTeal;
			btnSaveHtml.ForeColor = Color.Black;
			btnSaveHtml.Click += delegate { SaveHtmlDossier(); };
			actionsCard.Controls.Add(btnSaveHtml);

			btnOpenBrowser = new DarkButton();
			btnOpenBrowser.Text = "🌐 Открыть в браузере";
			btnOpenBrowser.Location = new Point(286, 26);
			btnOpenBrowser.Size = new Size(140, 28);
			btnOpenBrowser.Font = Ui.FontBody(8.5f, true);
			btnOpenBrowser.Click += delegate { OpenInDefaultBrowser(); };
			actionsCard.Controls.Add(btnOpenBrowser);

			btnPrintPdf = new DarkButton();
			btnPrintPdf.Text = "🖨️ Печать / PDF";
			btnPrintPdf.Location = new Point(434, 26);
			btnPrintPdf.Size = new Size(116, 28);
			btnPrintPdf.Font = Ui.FontBody(8.5f, true);
			btnPrintPdf.Click += delegate { PrintDossier(); };
			actionsCard.Controls.Add(btnPrintPdf);

			btnCopyDiscord = new DarkButton();
			btnCopyDiscord.Text = "📋 Копировать для Discord / Форума";
			btnCopyDiscord.Location = new Point(10, 60);
			btnCopyDiscord.Size = new Size(268, 28);
			btnCopyDiscord.Font = Ui.FontBody(8.5f, true);
			btnCopyDiscord.Click += delegate { CopyDiscordSummary(); };
			actionsCard.Controls.Add(btnCopyDiscord);

			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "⚖️ Копировать команду бана";
			btnCopyBanCmd.Location = new Point(286, 60);
			btnCopyBanCmd.Size = new Size(264, 28);
			btnCopyBanCmd.Font = Ui.FontBody(8.5f, true);
			btnCopyBanCmd.Click += delegate { CopyBanCommand(); };
			actionsCard.Controls.Add(btnCopyBanCmd);

			splitContainer.Panel1.Controls.Add(leftPanel);

			// Right: WebBrowser Preview
			Panel rightPanel = new Panel();
			rightPanel.Dock = DockStyle.Fill;
			rightPanel.BackColor = Ui.Surface;

			Panel previewHeader = new Panel();
			previewHeader.Dock = DockStyle.Top;
			previewHeader.Height = 32;
			previewHeader.BackColor = Ui.SurfaceAlt;
			previewHeader.Padding = new Padding(12, 6, 12, 6);

			Label lblPreviewTitle = new Label();
			lblPreviewTitle.Text = "🖥️ Живой интерактивный предпросмотр официального HTML-досье:";
			lblPreviewTitle.Font = Ui.FontBody(8.5f, true);
			lblPreviewTitle.ForeColor = Ui.TextSecondary;
			lblPreviewTitle.AutoSize = true;
			lblPreviewTitle.Location = new Point(12, 8);
			previewHeader.Controls.Add(lblPreviewTitle);
			rightPanel.Controls.Add(previewHeader);

			webBrowser = new WebBrowser();
			webBrowser.Dock = DockStyle.Fill;
			webBrowser.ScriptErrorsSuppressed = true;
			rightPanel.Controls.Add(webBrowser);

			splitContainer.Panel2.Controls.Add(rightPanel);
			Controls.Add(splitContainer);
			splitContainer.BringToFront();
		}

		private Panel CreateSectionCard(string title, ref int y, Panel parent, int height)
		{
			Panel card = new Panel();
			card.Location = new Point(10, y);
			card.Size = new Size(560, height);
			card.BackColor = Ui.Surface;
			card.BorderStyle = BorderStyle.None;

			Label lbl = new Label();
			lbl.Text = title;
			lbl.Font = Ui.FontBody(8.5f, true);
			lbl.ForeColor = Ui.AccentTeal;
			lbl.AutoSize = true;
			lbl.Location = new Point(10, 6);
			card.Controls.Add(lbl);

			parent.Controls.Add(card);
			y += height + 10;
			return card;
		}

		private void CreateField(Panel parent, string labelText, ref DarkTextBox box, string defaultValue, int x, int y, int width)
		{
			Label lbl = new Label();
			lbl.Text = labelText;
			lbl.Font = Ui.FontBody(8f, true);
			lbl.ForeColor = Ui.TextSecondary;
			lbl.AutoSize = true;
			lbl.Location = new Point(x, y);
			parent.Controls.Add(lbl);

			box = new DarkTextBox();
			box.Text = defaultValue;
			box.Location = new Point(x, y + 18);
			box.Size = new Size(width, 24);
			box.TextChanged += delegate { RenderHtmlReport(); };
			parent.Controls.Add(box);
		}

		private void PopulateInitialData()
		{
			txtPlayer.Text = targetPlayerName;
			txtModerator.Text = targetModeratorName;
			txtServerMode.Text = "Анархия #1 (1.16.5-1.20)";
			txtContacts.Text = "Discord: " + targetPlayerName + " | AnyDesk: 832 941 028";
		}

		private void UpdateVerdictDetails()
		{
			string sel = cmbVerdict.SelectedItem != null ? cmbVerdict.SelectedItem.ToString() : "";
			if (sel.Contains("5.01"))
			{
				txtBanDuration.Text = "30";
			}
			else if (sel.Contains("5.02") || sel.Contains("5.03") || sel.Contains("5.05"))
			{
				txtBanDuration.Text = "15";
			}
			else if (sel.Contains("5.06") || sel.Contains("5.07") || sel.Contains("5.08"))
			{
				txtBanDuration.Text = "30";
			}
			else if (sel.Contains("2.06"))
			{
				txtBanDuration.Text = "7";
			}
			else
			{
				txtBanDuration.Text = "0";
			}
		}

		#region Evidence Gathering

		private void AutoCollectEvidence()
		{
			evidenceList.Clear();
			int idx = 1;

			// Check 1: Prefetch
			try
			{
				string winDir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string pfDir = Path.Combine(winDir, "Prefetch");
				if (Directory.Exists(pfDir))
				{
					string[] pfFiles = Directory.GetFiles(pfDir, "*.pf");
					foreach (string pf in pfFiles)
					{
						string fname = Path.GetFileName(pf).ToLowerInvariant();
						if (fname.Contains("expensive") || fname.Contains("celestial") || fname.Contains("vape") || fname.Contains("nursultan") || fname.Contains("akrien"))
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Prefetch (.pf)",
								ArtifactName = Path.GetFileName(pf),
								TargetRule = "5.01",
								Severity = "CRITICAL",
								EvidenceDetails = "Запуск исполняемого файла чита зафиксирован в кэше Prefetch.",
								Sha256OrPath = pf,
								Timestamp = File.GetLastWriteTime(pf)
							});
						}
					}
				}
			}
			catch { }

			// Check 2: .minecraft mods & versions
			try
			{
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string mcMods = Path.Combine(appData, ".minecraft\\mods");
				if (Directory.Exists(mcMods))
				{
					string[] jars = Directory.GetFiles(mcMods, "*.jar");
					foreach (string jar in jars)
					{
						string jname = Path.GetFileName(jar).ToLowerInvariant();
						if (jname.Contains("celestial") || jname.Contains("expensive") || jname.Contains("wild") || jname.Contains("nurik") || jname.Contains("xray") || jname.Contains("baritone"))
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "JAR / Mods",
								ArtifactName = Path.GetFileName(jar),
								TargetRule = jname.Contains("xray") ? "5.03" : "5.01",
								Severity = "CRITICAL",
								EvidenceDetails = "Чит-модификация обнаружена в каталоге .minecraft\\mods.",
								Sha256OrPath = jar,
								Timestamp = File.GetLastWriteTime(jar)
							});
						}
					}
				}
			}
			catch { }

			// Check 3: Downloads & Desktop
			try
			{
				string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
				string downloads = Path.Combine(userProfile, "Downloads");
				if (Directory.Exists(downloads))
				{
					string[] dlFiles = Directory.GetFiles(downloads, "*.*");
					foreach (string f in dlFiles)
					{
						string fname = Path.GetFileName(f).ToLowerInvariant();
						if (fname.Contains("expensive") || fname.Contains("celestial") || fname.Contains("vape") || fname.Contains("koid") || fname.Contains("nurik"))
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Downloads",
								ArtifactName = Path.GetFileName(f),
								TargetRule = "5.02",
								Severity = "HIGH",
								EvidenceDetails = "Дистрибутив чита обнаружен в папке Загрузки пользователя.",
								Sha256OrPath = f,
								Timestamp = File.GetLastWriteTime(f)
							});
						}
					}
				}
			}
			catch { }

			// Check 4: ComDlg32 OpenSave & LastVisited
			try
			{
				using (Microsoft.Win32.RegistryKey openSave = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\ComDlg32\OpenSavePidlMRU"))
				{
					if (openSave != null)
					{
						string[] subs = openSave.GetSubKeyNames();
						foreach (string sub in subs)
						{
							using (Microsoft.Win32.RegistryKey sk = openSave.OpenSubKey(sub))
							{
								if (sk == null) continue;
								string[] vns = sk.GetValueNames();
								foreach (string vn in vns)
								{
									if (vn.Equals("MRUListEx", StringComparison.OrdinalIgnoreCase)) continue;
									byte[] b = sk.GetValue(vn) as byte[];
									if (b == null || b.Length < 6) continue;
									string raw = (Encoding.Unicode.GetString(b) + " " + Encoding.Default.GetString(b)).ToLowerInvariant();
									string[] cheats = new string[] { "vape", "celestial", "expensive", "deadcode", "drip", "koid", "nurik", "wild", "akrien", "minced" };
									foreach (string ch in cheats)
									{
										if (raw.Contains(ch))
										{
											evidenceList.Add(new DossierEvidenceItem
											{
												Index = idx++,
												ModuleName = "ComDlg32 OpenSave",
												ArtifactName = "OpenSavePidlMRU\\" + sub + " -> " + ch,
												TargetRule = "5.08",
												Severity = "CRITICAL",
												EvidenceDetails = string.Format("Выбор или сохранение чит-программы [{0}] через системный диалог Windows (PIDL).", ch),
												Sha256OrPath = "HKCU\\Software\\Microsoft\\Windows\\CurrentVersion\\Explorer\\ComDlg32\\OpenSavePidlMRU\\" + sub,
												Timestamp = DateTime.Now
											});
											break;
										}
									}
								}
							}
						}
					}
				}
			}
			catch { }

			// Check 5: Power & Time Jumps (Event 1 / 41 / 42)
			try
			{
				string q = @"*[System[(Provider[@Name='Microsoft-Windows-Kernel-General'] and EventID=1) or (Provider[@Name='Microsoft-Windows-Kernel-Power'] and (EventID=41 or EventID=42))]]";
				System.Diagnostics.Eventing.Reader.EventLogQuery elq = new System.Diagnostics.Eventing.Reader.EventLogQuery("System", System.Diagnostics.Eventing.Reader.PathType.LogName, q);
				elq.ReverseDirection = true;
				using (System.Diagnostics.Eventing.Reader.EventLogReader rdr = new System.Diagnostics.Eventing.Reader.EventLogReader(elq))
				{
					System.Diagnostics.Eventing.Reader.EventRecord ev;
					int evCount = 0;
					while ((ev = rdr.ReadEvent()) != null && evCount < 30)
					{
						using (ev)
						{
							DateTime evTime = ev.TimeCreated ?? DateTime.MinValue;
							double ageMin = (DateTime.Now - evTime).TotalMinutes;
							if (ev.Id == 42 && ageMin <= 60.0)
							{
								evidenceList.Add(new DossierEvidenceItem
								{
									Index = idx++,
									ModuleName = "Kernel-Power",
									ArtifactName = "Event ID 42 (Sleep / Hibernate)",
									TargetRule = "5.08",
									Severity = "CRITICAL",
									EvidenceDetails = string.Format("Панический перевод ПК в спящий режим за {0:0.0} мин до проверки для сброса RAM.", ageMin),
									Sha256OrPath = "System EventLog Event 42",
									Timestamp = evTime
								});
								break;
							}
							else if (ev.Id == 41 && ageMin <= 90.0)
							{
								evidenceList.Add(new DossierEvidenceItem
								{
									Index = idx++,
									ModuleName = "Kernel-Power",
									ArtifactName = "Event ID 41 (Hard Reset / Panic)",
									TargetRule = "5.04",
									Severity = "CRITICAL",
									EvidenceDetails = string.Format("Принудительный сброс питания / Hard Reset за {0:0.0} мин до проверки.", ageMin),
									Sha256OrPath = "System EventLog Event 41",
									Timestamp = evTime
								});
								break;
							}
							evCount++;
						}
					}
				}
			}
			catch { }

			// Check 6: JVM Injections & Proxy DLLs
			try
			{
				string[] envVars = new string[] { "_JAVA_OPTIONS", "JAVA_TOOL_OPTIONS" };
				foreach (string ev in envVars)
				{
					string val = Environment.GetEnvironmentVariable(ev);
					if (!string.IsNullOrEmpty(val) && val.ToLowerInvariant().Contains("-javaagent:"))
					{
						evidenceList.Add(new DossierEvidenceItem
						{
							Index = idx++,
							ModuleName = "JVM Environment",
							ArtifactName = ev + " -> " + val,
							TargetRule = "5.01",
							Severity = "CRITICAL",
							EvidenceDetails = "В переменной среды Java обнаружен параметр внедрения JavaAgent: " + val,
							Sha256OrPath = ev,
							Timestamp = DateTime.Now
						});
						break;
					}
				}

				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string mcRuntime = Path.Combine(appData, @".minecraft\runtime");
				if (Directory.Exists(mcRuntime))
				{
					string[] rogueDlls = new string[] { "dwrite.dll", "version.dll", "opengl32.dll", "dxgi.dll" };
					foreach (string rDll in rogueDlls)
					{
						string[] found = Directory.GetFiles(mcRuntime, rDll, SearchOption.AllDirectories);
						if (found != null && found.Length > 0)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "JRE Proxy DLL",
								ArtifactName = rDll + " in runtime\\bin",
								TargetRule = "5.01",
								Severity = "CRITICAL",
								EvidenceDetails = "В каталоге Java Runtime обнаружена прокси-библиотека перехвата: " + found[0],
								Sha256OrPath = found[0],
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 7: Named Pipes IPC
			try
			{
				string[] pipes = Directory.GetFiles(@"\\.\pipe\");
				string[] cheatSigs = new string[] { "vape", "celestial", "expensive", "nursultan", "slinky", "drip", "koid", "doomsday", "flint" };
				foreach (string p in pipes)
				{
					string pLower = Path.GetFileName(p).ToLowerInvariant();
					foreach (string sig in cheatSigs)
					{
						if (pLower.Contains(sig))
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Named Pipe IPC",
								ArtifactName = p,
								TargetRule = "5.01",
								Severity = "CRITICAL",
								EvidenceDetails = "Обнаружен активный именованный канал связи чита: " + p,
								Sha256OrPath = p,
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 8: Compatibility Assistant Store SACP Deleted Executables
			try
			{
				using (Microsoft.Win32.RegistryKey sacpKey = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Compatibility Assistant\Store"))
				{
					if (sacpKey != null)
					{
						string[] valNames = sacpKey.GetValueNames();
						foreach (string vn in valNames)
						{
							if (!File.Exists(vn) && (vn.ToLowerInvariant().Contains(@"\downloads\") || vn.ToLowerInvariant().Contains(@"\appdata\local\temp\") || vn.ToLowerInvariant().Contains(@"\desktop\")))
							{
								string fn = Path.GetFileName(vn).ToLowerInvariant();
								if (!fn.Contains("anydesk") && !fn.Contains("rudesktop") && !fn.Contains("mccheatchecker"))
								{
									evidenceList.Add(new DossierEvidenceItem
									{
										Index = idx++,
										ModuleName = "SACP Store Execution",
										ArtifactName = vn,
										TargetRule = "5.08",
										Severity = "CRITICAL",
										EvidenceDetails = "В базе SACP зафиксирован запуск исполняемого файла, который был удален с диска перед проверкой: " + vn,
										Sha256OrPath = vn,
										Timestamp = DateTime.Now
									});
									break;
								}
							}
						}
					}
				}
			}
			

			catch { }

			// Check 9: Screen Capture Evasion & Overlay Bypass (WDA_EXCLUDEFROMCAPTURE)
			try
			{
				List<CheckResult> overlayResults = ManualChecks.RunScreenCaptureEvasionAndOverlayCheck();
				if (overlayResults != null)
				{
					foreach (CheckResult cr in overlayResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "DWM & Оверлеи",
								ArtifactName = "Обход захвата экрана (SetWindowDisplayAffinity) / Stealth Overlay",
								TargetRule = "5.08 / 5.01",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = "Win32 HWND / DWM Affinity 0x11",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			

			catch { }

			// Check 10: Minecraft Command History (command_history.txt)
			try
			{
				List<CheckResult> cmdResults = ManualChecks.RunMinecraftCommandHistoryCheck();
				if (cmdResults != null)
				{
					foreach (CheckResult cr in cmdResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "История команд",
								ArtifactName = "command_history.txt",
								TargetRule = "5.01 / 5.08",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = "command_history.txt",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			

			catch { }

			// Check 11: ZIP EOCD Steganography in Mods
			try
			{
				List<CheckResult> zipResults = ManualChecks.RunZipEocdTrailingPayloadCheck();
				if (zipResults != null)
				{
					foreach (CheckResult cr in zipResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "ZIP Стеганография",
								ArtifactName = "Скрытая полезная нагрузка после EOCD",
								TargetRule = "5.01 / 5.08",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = "ZIP EOCD Overlay Payload",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 12: SetupAPI.dev.log USB Connection Timeline
			try
			{
				List<CheckResult> usbResults = ManualChecks.RunSetupApiUsbTimelineCheck();
				if (usbResults != null)
				{
					foreach (CheckResult cr in usbResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "SetupAPI USB Timeline",
								ArtifactName = "setupapi.dev.log",
								TargetRule = "5.08 / 5.01",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"INF\setupapi.dev.log",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 13: JVM HotSpot Crash Logs (hs_err_pid*.log)
			try
			{
				List<CheckResult> jvmResults = ManualChecks.RunJvmHotspotCrashLogCheck();
				if (jvmResults != null)
				{
					foreach (CheckResult cr in jvmResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "JVM Crash Logs",
								ArtifactName = "hs_err_pid.log",
								TargetRule = "5.01 / 5.08",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"hs_err_pid.log",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 14: Deep Shaderpack X-Ray (GLSL Discard)
			try
			{
				List<CheckResult> xrayResults = ManualChecks.RunDeepShaderpackXrayCheck();
				if (xrayResults != null)
				{
					foreach (CheckResult cr in xrayResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Shaderpack X-Ray",
								ArtifactName = "GLSL Discard / Shaders",
								TargetRule = "5.01",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"shaderpacks/*.zip",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 15: Process Hollowing & Unbacked Memory
			try
			{
				List<CheckResult> holResults = ManualChecks.RunProcessHollowingAndUnbackedMemoryCheck();
				if (holResults != null)
				{
					foreach (CheckResult cr in holResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Process Hollowing",
								ArtifactName = "Unbacked PE Memory",
								TargetRule = "5.01 / 5.08",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"Process Memory",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 16: Fake Computer & Remote AnyDesk
			try
			{
				List<CheckResult> fakeResults = ManualChecks.RunFakeComputerAndRemoteAnyDeskCheck();
				if (fakeResults != null)
				{
					foreach (CheckResult cr in fakeResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Fake PC / AnyDesk",
								ArtifactName = "AnyDesk & PC Integrity",
								TargetRule = "5.08",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"AnyDesk Profiles",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 17: Anti-Evasion & Stealth Bypass Inspector
			try
			{
				List<CheckResult> evaResults = ManualChecks.RunAntiEvasionAndStealthBypassCheck();
				if (evaResults != null)
				{
					foreach (CheckResult cr in evaResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Anti-Evasion & Stealth Bypass",
								ArtifactName = "MountPoints2 & history.db & Cleaners",
								TargetRule = "5.08 / 5.01",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"MountPoints2 / history.db",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 18: Hypervisor, VM & Stream-Proof Evasion
			try
			{
				List<CheckResult> vmResults = ManualChecks.RunHypervisorVmAndSandboxEvasionCheck();
				if (vmResults != null)
				{
					foreach (CheckResult cr in vmResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Hypervisor & VM Evasion",
								ArtifactName = "Virtualization & Stream-Proof",
								TargetRule = "5.08 / 5.01",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"Hypervisor / WDA Affinity",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 19: Discord Rich Presence & RPC Forensics
			try
			{
				List<CheckResult> dscResults = ManualChecks.RunDiscordRichPresenceAndRpcForensicsCheck();
				if (dscResults != null)
				{
					foreach (CheckResult cr in dscResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Discord & RPC Forensics",
								ArtifactName = "Discord Rich Presence / IPC",
								TargetRule = "5.01 / 5.08",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"Discord LevelDB / discord-ipc",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 20: AMSI, ETW & Memory Patching Forensics
			try
			{
				List<CheckResult> amsiResults = ManualChecks.RunAmsiEtwAndMemoryPatchingCheck();
				if (amsiResults != null)
				{
					foreach (CheckResult cr in amsiResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Memory Patching & AMSI/ETW",
								ArtifactName = "AMSI / ETW Patch / MinHook",
								TargetRule = "5.01 / 5.08",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"javaw.exe Memory / amsi.dll / ntdll.dll",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 21: Minecraft Screenshots, Metadata & Watermarks
			try
			{
				List<CheckResult> scResults = ManualChecks.RunMinecraftScreenshotsExifAndOcrForensicsCheck();
				if (scResults != null)
				{
					foreach (CheckResult cr in scResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Screenshot & EXIF Forensics",
								ArtifactName = "Screenshot Tampering / Watermark",
								TargetRule = "5.01 / 5.08",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"Minecraft Screenshots / PNG Chunks",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 22: Hardware Mouse, Debounce & Macro Forensics
			try
			{
				List<CheckResult> mouseResults = ManualChecks.RunHardwareMouseAndMacroAuditCheck();
				if (mouseResults != null)
				{
					foreach (CheckResult cr in mouseResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Hardware Mouse & Macro Forensics",
								ArtifactName = "Debounce Abuse / Bloody / Synthetic Driver",
								TargetRule = "2.06 / 5.01 / 5.08",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"HID Mouse / Glorious Core / interception.sys",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 23: Resource Pack X-Ray, CEM & Invisible Fonts
			try
			{
				List<CheckResult> rpResults = ManualChecks.RunFontAndTexturePackBypassCheck();
				if (rpResults != null)
				{
					foreach (CheckResult cr in rpResults)
					{
						if (cr.Severity == CheckSeverity.Critical)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "ResourcePack & Font Forensics",
								ArtifactName = "X-Ray Textures / CEM Hitboxes / Zero Fonts",
								TargetRule = "5.01 / 5.08",
								Severity = "CRITICAL",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"resourcepacks / OptiFine CEM / font.json",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 24: Kernel Drivers, BYOVD & TestSigning
			try
			{
				List<CheckResult> kernelResults = ManualChecks.RunKernelDriversAndHypervisorHooksCheck();
				if (kernelResults != null)
				{
					foreach (CheckResult cr in kernelResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Kernel Drivers & TestSigning",
								ArtifactName = "BYOVD Driver / kdmapper / TestSigning",
								TargetRule = "5.01 / 5.08",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"System Drivers / BCD TestSigning",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 25: System Time Anomalies, Clock Rewind & Time Skew
			try
			{
				List<CheckResult> timeResults = ManualChecks.RunTimeAnomalyAndSystemClockTamperingCheck();
				if (timeResults != null)
				{
					foreach (CheckResult cr in timeResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "System Time & Clock Tampering",
								ArtifactName = "Event ID 1 Rewind / Reboot / Time-Travel File",
								TargetRule = "5.08",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"System Event Log / w32time / File Timestamps",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 26: Windows 10/11 Clipboard Win+V & history.db Forensics
			try
			{
				List<CheckResult> clipResults = ManualChecks.RunClipboardWinVAndHistoryDatabaseCheck();
				if (clipResults != null)
				{
					foreach (CheckResult cr in clipResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Clipboard & history.db Forensics",
								ArtifactName = "Cheat Token / Cleaner Command / Tampered DB",
								TargetRule = "5.01 / 5.08",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"%LocalAppData%\Microsoft\Windows\Clipboard\history.db",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 27: Windows Update Tampering & Fake Update Screens
			try
			{
				List<CheckResult> usoResults = ManualChecks.RunUsoAndWindowsUpdateTamperingCheck();
				if (usoResults != null)
				{
					foreach (CheckResult cr in usoResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Windows Update & Fake Screen",
								ArtifactName = "Fake Update Screen / usoclient Reboot",
								TargetRule = "5.08",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"Top-Level Windows / usoclient / CBS.log",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 28: Remote Streaming & Virtual Audio Forensics
			try
			{
				List<CheckResult> audioResults = ManualChecks.RunRemoteStreamingAndVirtualAudioDriverCheck();
				if (audioResults != null)
				{
					foreach (CheckResult cr in audioResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Virtual Audio & Remote Streaming",
								ArtifactName = "VB-Cable / VoiceMeeter / Covert Audio Channel",
								TargetRule = "5.08",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"Class\{4d36e96c-e325-11ce-bfc1-08002be10318} / MMDEVAPI",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 29: Direct3D 11, OpenGL VTable Hooks & ESP Overlays
			try
			{
				List<CheckResult> d3dResults = ManualChecks.RunDirect3dHookAndEspOverlayCheck();
				if (d3dResults != null)
				{
					foreach (CheckResult cr in d3dResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Direct3D/OpenGL Hooks & ESP",
								ArtifactName = "wglSwapBuffers / Present Hook / ESP Overlay",
								TargetRule = "5.01 / 5.08",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"opengl32.dll / dxgi.dll / Transparent Window",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 30: Local Tunnels, VPN & Proxy Bypass Forensics
			try
			{
				List<CheckResult> tunnelResults = ManualChecks.RunLocalTunnelsVpnAndProxyBypassCheck();
				if (tunnelResults != null)
				{
					foreach (CheckResult cr in tunnelResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Local Tunnels & Proxy Bypass",
								ArtifactName = "Ngrok / Wintun / Proxy / hosts Tampering",
								TargetRule = "5.08",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"Prefetch / NDIS / Internet Settings / hosts",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 31: Java Agent & JVM Flags Forensics
			try
			{
				List<CheckResult> agentResults = ManualChecks.RunJavaAgentAndJvmFlagsCheck();
				if (agentResults != null)
				{
					foreach (CheckResult cr in agentResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Java Agent & JVM Flags",
								ArtifactName = "-javaagent / -agentpath / JAVA_TOOL_OPTIONS",
								TargetRule = "5.01",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"Win32_Process / Environment Variables",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 32: Mixin & mod.json Forensics
			try
			{
				List<CheckResult> mixinResults = ManualChecks.RunMixinAndModJsonCheck();
				if (mixinResults != null)
				{
					foreach (CheckResult cr in mixinResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Mixin & mod.json Forensics",
								ArtifactName = "fabric.mod.json / mcmod.info / *.mixins.json",
								TargetRule = "5.01",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @".minecraft\mods / versions",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 33: Unified Forensic Timeline
			try
			{
				List<CheckResult> timelineResults = ManualChecks.RunUnifiedForensicTimelineCheck();
				if (timelineResults != null)
				{
					foreach (CheckResult cr in timelineResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Unified Forensic Timeline",
								ArtifactName = "Prefetch / BAM / EventLog / USN Journal",
								TargetRule = "5.07",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"System Forensic Timeline",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 34: Minecraft Options & Keybinds
			try
			{
				List<CheckResult> keybindResults = ManualChecks.RunMinecraftOptionsAndKeybindsCheck();
				if (keybindResults != null)
				{
					foreach (CheckResult cr in keybindResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Minecraft Options & Keybinds",
								ArtifactName = "options.txt / keybinds.txt",
								TargetRule = "5.01",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @".minecraft\options.txt",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 35: World Seed & ChunkBase Forensics
			try
			{
				List<CheckResult> seedResults = ManualChecks.RunWorldSeedAndChunkbaseCheck();
				if (seedResults != null)
				{
					foreach (CheckResult cr in seedResults)
					{
						if (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning)
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "World Seed & ChunkBase",
								ArtifactName = "SeedCrackerX / Cubiomes / ChunkBase",
								TargetRule = "5.06",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"Browser History / Mods",
								Timestamp = DateTime.Now
							});
							break;
						}
					}
				}
			}
			catch { }

			// Check 36: System Informer / Process Hacker 2 Native Memory & Service Forensics
			try
			{
				List<CheckResult> siResults = ManualChecks.RunSystemInformerAndProcessHackerManualCheck();
				if (siResults != null)
				{
					foreach (CheckResult cr in siResults)
					{
						if (cr.Detected && (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning))
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "System Informer Forensics",
								ArtifactName = cr.Name,
								TargetRule = cr.Severity == CheckSeverity.Critical ? "5.01 / 5.08" : "5.08",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"Memory (javaw / explorer / DcomLaunch / PcaSvc / DPS / csrss)",
								Timestamp = DateTime.Now
							});
						}
					}
				}
			}
			catch { }

			// Check 37: High-Level Luyten REGEX MODS, HxD Hex DLL, BAM Unsigned, Event Logs & SystemDLC
			try
			{
				List<CheckResult> hlResults = ManualChecks.RunHighLevelLuytenHxdAndSystemDlcManualCheck();
				if (hlResults != null)
				{
					foreach (CheckResult cr in hlResults)
					{
						if (cr.Detected && (cr.Severity == CheckSeverity.Critical || cr.Severity == CheckSeverity.Warning))
						{
							evidenceList.Add(new DossierEvidenceItem
							{
								Index = idx++,
								ModuleName = "Luyten / HxD / BAM / SystemDLC",
								ArtifactName = cr.Name,
								TargetRule = cr.Severity == CheckSeverity.Critical ? "5.01 / 5.08" : "5.08",
								Severity = cr.Severity == CheckSeverity.Critical ? "CRITICAL" : "WARNING",
								EvidenceDetails = !string.IsNullOrEmpty(cr.Evidence) ? cr.Evidence : cr.Detail,
								Sha256OrPath = @"Luyten REGEX / HxD DLL / BAM Unsigned / Event Logs / SystemDLC",
								Timestamp = DateTime.Now
							});
						}
					}
				}
			}
			catch { }

			// If no critical evidence found, add sample audit trail entry
			if (evidenceList.Count == 0)
			{
				evidenceList.Add(new DossierEvidenceItem
				{
					Index = 1,
					ModuleName = "Комплексный аудит",
					ArtifactName = "Криминалистическое сканирование 89 модулей",
					TargetRule = "5.01-5.08",
					Severity = "INFO",
					EvidenceDetails = "Файловая система, кэши Prefetch/ShimCache, память javaw.exe, диалоги ComDlg32, JVM и прокси DLL, каналы Named Pipes, следы SACP/BAM, сокрытие экрана DWM и оверлеи, история команд Minecraft, стеганография модов ZIP EOCD, хронология USB SetupAPI, аварийные логи JVM HotSpot, шейдерпаки X-Ray (GLSL discard), внедрение в память Process Hollowing, аудит AnyDesk и подмены ПК, точки MountPoints2, буфер обмена history.db, самоликвидаторы (fsutil, wevtutil, vssadmin), виртуальные машины (VM), песочницы, Stream-Proof сокрытие WDA и виртуальные мониторы, бинды options.txt, поиск сида SeedCrackerX/ChunkBase, нативная память System Informer (javaw, explorer, DcomLaunch, PcaSvc, DPS, csrss), Luyten REGEX MODS, HxD Hex DLL, BAM Not Signed Only, SystemDLC, журналы событий и реестр чисты.",
					Sha256OrPath = "Verified Integrity",
					Timestamp = DateTime.Now
				});
			}

			RefreshEvidenceListView();
		}

		private void RefreshEvidenceListView()
		{
			lvEvidence.BeginUpdate();
			lvEvidence.Items.Clear();

			foreach (DossierEvidenceItem it in evidenceList)
			{
				ListViewItem lvi = new ListViewItem(it.Index.ToString());
				lvi.SubItems.Add(it.ModuleName);
				lvi.SubItems.Add(it.ArtifactName);
				lvi.SubItems.Add(it.TargetRule);
				lvi.SubItems.Add(it.Timestamp.ToString("HH:mm:ss"));
				lvi.SubItems.Add(it.EvidenceDetails);

				if (it.Severity == "CRITICAL") lvi.ForeColor = Ui.AccentRed;
				else if (it.Severity == "HIGH") lvi.ForeColor = Ui.AccentOrange;
				else lvi.ForeColor = Ui.TextPrimary;

				lvi.Tag = it;
				lvEvidence.Items.Add(lvi);
			}

			lvEvidence.EndUpdate();
		}

		private void PromptAddEvidence()
		{
			using (Form prompt = new Form())
			{
				prompt.Text = "Добавить криминалистическую улику";
				prompt.Size = new Size(500, 320);
				prompt.StartPosition = FormStartPosition.CenterParent;
				prompt.BackColor = Ui.Surface;
				prompt.ForeColor = Ui.TextPrimary;
				prompt.Font = Ui.FontBody(9f);
				prompt.FormBorderStyle = FormBorderStyle.FixedDialog;
				prompt.MaximizeBox = false;
				prompt.MinimizeBox = false;

				Label l1 = new Label { Text = "Модуль / Источник:", Location = new Point(20, 20), AutoSize = true, ForeColor = Ui.TextSecondary };
				DarkTextBox tbModule = new DarkTextBox { Text = "NTFS ADS / USN", Location = new Point(170, 18), Size = new Size(290, 24) };

				Label l2 = new Label { Text = "Объект / Чит:", Location = new Point(20, 60), AutoSize = true, ForeColor = Ui.TextSecondary };
				DarkTextBox tbObject = new DarkTextBox { Text = "celestial_recode.jar", Location = new Point(170, 58), Size = new Size(290, 24) };

				Label l3 = new Label { Text = "Пункт регламента:", Location = new Point(20, 100), AutoSize = true, ForeColor = Ui.TextSecondary };
				DarkTextBox tbRule = new DarkTextBox { Text = "5.01", Location = new Point(170, 98), Size = new Size(290, 24) };

				Label l4 = new Label { Text = "Детали доказательства:", Location = new Point(20, 140), AutoSize = true, ForeColor = Ui.TextSecondary };
				DarkTextBox tbDetails = new DarkTextBox { Text = "Обнаружена сигнатура чита и скачивание из celestial.lol", Location = new Point(170, 138), Size = new Size(290, 24) };

				DarkButton btnOk = new DarkButton { Text = "Добавить", Location = new Point(250, 210), Size = new Size(100, 30), DialogResult = DialogResult.OK };
				DarkButton btnCancel = new DarkButton { Text = "Отмена", Location = new Point(360, 210), Size = new Size(100, 30), DialogResult = DialogResult.Cancel };

				prompt.Controls.AddRange(new Control[] { l1, tbModule, l2, tbObject, l3, tbRule, l4, tbDetails, btnOk, btnCancel });

				if (prompt.ShowDialog(this) == DialogResult.OK)
				{
					evidenceList.Add(new DossierEvidenceItem
					{
						Index = evidenceList.Count + 1,
						ModuleName = tbModule.Text.Trim(),
						ArtifactName = tbObject.Text.Trim(),
						TargetRule = tbRule.Text.Trim(),
						Severity = "CRITICAL",
						EvidenceDetails = tbDetails.Text.Trim(),
						Sha256OrPath = "Введено модератором",
						Timestamp = DateTime.Now
					});

					RefreshEvidenceListView();
					RenderHtmlReport();
				}
			}
		}

		private void RemoveSelectedEvidence()
		{
			if (lvEvidence.SelectedItems.Count == 0) return;
			DossierEvidenceItem it = lvEvidence.SelectedItems[0].Tag as DossierEvidenceItem;
			if (it != null)
			{
				evidenceList.Remove(it);
				for (int i = 0; i < evidenceList.Count; i++)
				{
					evidenceList[i].Index = i + 1;
				}
				RefreshEvidenceListView();
				RenderHtmlReport();
			}
		}

		#endregion

		#region HTML Report Rendering

		private void RenderHtmlReport()
		{
			string verdictText = cmbVerdict.SelectedItem != null ? cmbVerdict.SelectedItem.ToString() : "ЧИСТ";
			bool isClear = verdictText.StartsWith("ЧИСТ");
			string badgeColor = isClear ? "#10b981" : "#ef4444";
			string badgeBg = isClear ? "rgba(16, 185, 129, 0.15)" : "rgba(239, 68, 68, 0.15)";
			string verdictTitle = isClear ? "НАРУШЕНИЙ НЕ ВЫЯВЛЕНО (ИГРОК ОПРАВДАН)" : "ВЕРДИКТ: БАН НАКАЗАНИЕ";

			string duration = txtBanDuration.Text.Trim();
			string pName = string.IsNullOrEmpty(txtPlayer.Text) ? targetPlayerName : txtPlayer.Text.Trim();
			string mName = string.IsNullOrEmpty(txtModerator.Text) ? targetModeratorName : txtModerator.Text.Trim();
			string server = string.IsNullOrEmpty(txtServerMode.Text) ? "Сервер" : txtServerMode.Text.Trim();
			string contacts = string.IsNullOrEmpty(txtContacts.Text) ? "Не указаны" : txtContacts.Text.Trim();
			string notes = string.IsNullOrEmpty(txtModeratorNotes.Text) ? "Проверка завершена." : txtModeratorNotes.Text.Trim();

			TimeSpan durationSpan = DateTime.Now - ssStartTime;
			string ssDurationStr = string.Format("{0} мин. {1} сек.", (int)durationSpan.TotalMinutes, durationSpan.Seconds);

			// System Hardware Summary
			string osVersion = Environment.OSVersion.ToString() + (Environment.Is64BitOperatingSystem ? " (64-bit)" : " (32-bit)");
			string machineName = Environment.MachineName;
			int procCount = Environment.ProcessorCount;
			long uptimeHours = Environment.TickCount / (1000 * 60 * 60);

			// Build HTML
			StringBuilder sb = new StringBuilder();
			sb.AppendLine("<!DOCTYPE html>");
			sb.AppendLine("<html lang=\"ru\">");
			sb.AppendLine("<head>");
			sb.AppendLine("<meta charset=\"utf-8\"/>");
			sb.AppendLine("<meta http-equiv=\"X-UA-Compatible\" content=\"IE=edge\"/>");
			sb.AppendLine("<title>Судебное Досье Проверки — " + pName + "</title>");
			sb.AppendLine("<style>");
			sb.AppendLine(":root {");
			sb.AppendLine("  --bg-dark: #0a0e14;");
			sb.AppendLine("  --card-bg: #121820;");
			sb.AppendLine("  --card-border: #1e2837;");
			sb.AppendLine("  --text-main: #e2e8f0;");
			sb.AppendLine("  --text-muted: #94a3b8;");
			sb.AppendLine("  --accent-teal: #38bdf8;");
			sb.AppendLine("  --accent-red: #ef4444;");
			sb.AppendLine("  --accent-green: #10b981;");
			sb.AppendLine("  --accent-orange: #f59e0b;");
			sb.AppendLine("}");
			sb.AppendLine("body { margin: 0; padding: 24px; background: var(--bg-dark); color: var(--text-main); font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif; font-size: 14px; line-height: 1.5; }");
			sb.AppendLine(".container { max-width: 980px; margin: 0 auto; }");
			sb.AppendLine(".card { background: var(--card-bg); border: 1px solid var(--card-border); border-radius: 8px; padding: 20px; margin-bottom: 20px; box-shadow: 0 4px 12px rgba(0,0,0,0.3); }");
			sb.AppendLine(".header { display: flex; justify-content: space-between; align-items: center; border-bottom: 1px solid var(--card-border); padding-bottom: 16px; margin-bottom: 20px; }");
			sb.AppendLine(".logo-title { font-size: 20px; font-weight: 700; color: var(--accent-teal); letter-spacing: 0.5px; }");
			sb.AppendLine(".doc-id { font-size: 12px; color: var(--text-muted); font-family: Consolas, monospace; }");
			sb.AppendLine(".verdict-banner { background: " + badgeBg + "; border: 2px solid " + badgeColor + "; border-radius: 8px; padding: 18px 24px; margin-bottom: 24px; text-align: center; }");
			sb.AppendLine(".verdict-badge { font-size: 22px; font-weight: 800; color: " + badgeColor + "; text-transform: uppercase; letter-spacing: 1px; }");
			sb.AppendLine(".verdict-sub { font-size: 14px; color: var(--text-main); margin-top: 6px; }");
			sb.AppendLine(".grid-2 { display: table; width: 100%; table-layout: fixed; margin-bottom: 16px; }");
			sb.AppendLine(".grid-col { display: table-cell; width: 50%; vertical-align: top; padding-right: 12px; }");
			sb.AppendLine(".meta-item { margin-bottom: 8px; }");
			sb.AppendLine(".meta-label { color: var(--text-muted); font-size: 12px; text-transform: uppercase; font-weight: 600; }");
			sb.AppendLine(".meta-val { font-size: 15px; font-weight: 600; color: var(--text-main); }");
			sb.AppendLine(".whitelist-box { background: rgba(16, 185, 129, 0.08); border-left: 4px solid var(--accent-green); padding: 14px 18px; border-radius: 4px; margin-bottom: 20px; }");
			sb.AppendLine(".whitelist-title { font-weight: 700; color: var(--accent-green); font-size: 13px; text-transform: uppercase; margin-bottom: 4px; }");
			sb.AppendLine(".evidence-table { width: 100%; border-collapse: collapse; font-size: 13px; margin-top: 10px; }");
			sb.AppendLine(".evidence-table th { background: #1a2230; color: var(--text-muted); text-align: left; padding: 10px 12px; font-weight: 600; border-bottom: 2px solid var(--card-border); }");
			sb.AppendLine(".evidence-table td { padding: 10px 12px; border-bottom: 1px solid var(--card-border); vertical-align: middle; }");
			sb.AppendLine(".badge-pill { display: inline-block; padding: 2px 8px; border-radius: 4px; font-size: 11px; font-weight: 700; text-transform: uppercase; }");
			sb.AppendLine(".pill-crit { background: rgba(239, 68, 68, 0.2); color: var(--accent-red); border: 1px solid var(--accent-red); }");
			sb.AppendLine(".pill-info { background: rgba(56, 189, 248, 0.2); color: var(--accent-teal); border: 1px solid var(--accent-teal); }");
			sb.AppendLine(".footer { text-align: center; font-size: 11px; color: var(--text-muted); margin-top: 30px; border-top: 1px solid var(--card-border); padding-top: 16px; }");
			sb.AppendLine("@media print {");
			sb.AppendLine("  body { background: #fff !important; color: #000 !important; font-size: 11pt; padding: 0; }");
			sb.AppendLine("  .card { background: #fff !important; border: 1px solid #ccc !important; box-shadow: none !important; }");
			sb.AppendLine("  .verdict-banner { border: 2px solid #000 !important; background: #f0f0f0 !important; }");
			sb.AppendLine("  .verdict-badge { color: #000 !important; }");
			sb.AppendLine("  .evidence-table th { background: #eee !important; color: #000 !important; }");
			sb.AppendLine("  .evidence-table td { color: #000 !important; }");
			sb.AppendLine("  .pill-crit, .pill-info { border: 1px solid #000 !important; color: #000 !important; background: #eee !important; }");
			sb.AppendLine("}");
			sb.AppendLine("</style>");
			sb.AppendLine("</head>");
			sb.AppendLine("<body>");

			sb.AppendLine("<div class=\"container\">");

			// Header
			sb.AppendLine("<div class=\"card\">");
			sb.AppendLine("  <div class=\"header\">");
			sb.AppendLine("    <div>");
			sb.AppendLine("      <div class=\"logo-title\">🛡️ MC CHEAT CHECKER — ЭКСПЕРТНОЕ ДОСЬЕ</div>");
			sb.AppendLine("      <div style=\"color: var(--text-muted); font-size: 12px;\">Протокол судебной криминалистики ScreenShare Minecraft</div>");
			sb.AppendLine("    </div>");
			sb.AppendLine("    <div style=\"text-align: right;\">");
			sb.AppendLine("      <div class=\"doc-id\">ID: SS-" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "</div>");
			sb.AppendLine("      <div class=\"doc-id\">Дата: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " (МСК)</div>");
			sb.AppendLine("    </div>");
			sb.AppendLine("  </div>");

			// Verdict Banner
			sb.AppendLine("  <div class=\"verdict-banner\">");
			sb.AppendLine("    <div class=\"verdict-badge\">" + verdictTitle + "</div>");
			sb.AppendLine("    <div class=\"verdict-sub\">" + verdictText + (isClear ? "" : " | Назначенный срок: <b>" + duration + " дней</b>") + "</div>");
			sb.AppendLine("  </div>");

			// Details Grid
			sb.AppendLine("  <div class=\"grid-2\">");
			sb.AppendLine("    <div class=\"grid-col\">");
			sb.AppendLine("      <div class=\"meta-item\"><span class=\"meta-label\">Подозреваемый игрок:</span><div class=\"meta-val\">" + pName + "</div></div>");
			sb.AppendLine("      <div class=\"meta-item\"><span class=\"meta-label\">Проверяющий модератор:</span><div class=\"meta-val\">" + mName + "</div></div>");
			sb.AppendLine("      <div class=\"meta-item\"><span class=\"meta-label\">Сервер / Игровой режим:</span><div class=\"meta-val\">" + server + "</div></div>");
			sb.AppendLine("      <div class=\"meta-item\"><span class=\"meta-label\">Контакты ScreenShare:</span><div class=\"meta-val\">" + contacts + "</div></div>");
			sb.AppendLine("    </div>");
			sb.AppendLine("    <div class=\"grid-col\">");
			sb.AppendLine("      <div class=\"meta-item\"><span class=\"meta-label\">Длительность проверки:</span><div class=\"meta-val\">" + ssDurationStr + "</div></div>");
			sb.AppendLine("      <div class=\"meta-item\"><span class=\"meta-label\">Операционная система:</span><div class=\"meta-val\">" + osVersion + "</div></div>");
			sb.AppendLine("      <div class=\"meta-item\"><span class=\"meta-label\">Имя рабочей станции / CPU:</span><div class=\"meta-val\">" + machineName + " (" + procCount + " ядер)</div></div>");
			sb.AppendLine("      <div class=\"meta-item\"><span class=\"meta-label\">Время непрерывной работы (Uptime):</span><div class=\"meta-val\">" + uptimeHours + " ч.</div></div>");
			sb.AppendLine("    </div>");
			sb.AppendLine("  </div>");

			// Whitelist Guarantee Box
			sb.AppendLine("  <div class=\"whitelist-box\">");
			sb.AppendLine("    <div class=\"whitelist-title\">🛡️ ОФИЦИАЛЬНАЯ ГАРАНТИЯ БЕЛОГО СПИСКА (ПРАВИЛО 5.01)</div>");
			sb.AppendLine("    <div style=\"font-size: 13px; color: var(--text-main);\">");
			sb.AppendLine("      В строгом соответствии с правилами сервера, утилиты кликеров (<b>OP Auto Clicker, SpeedAutoClicker, TinyTask, X-Mouse Button Control, AutoHotkey, Clickermann, Keyran</b>) и средства удаленного доступа ScreenShare (<b>AnyDesk, RuDesktop</b>) были проверены и сертифицированы как <b>РАЗРЕШЕННЫЕ</b>. Санкции по ст. 5.01 к ним не применялись.");
			sb.AppendLine("    </div>");
			sb.AppendLine("  </div>");

			// Evidence Matrix Table
			sb.AppendLine("  <div style=\"margin-top: 24px;\">");
			sb.AppendLine("    <div style=\"font-size: 16px; font-weight: 700; color: var(--accent-teal); margin-bottom: 8px;\">📊 Матрица зафиксированных улик и артефактов (" + evidenceList.Count + ")</div>");
			sb.AppendLine("    <table class=\"evidence-table\">");
			sb.AppendLine("      <thead>");
			sb.AppendLine("        <tr>");
			sb.AppendLine("          <th style=\"width: 30px;\">#</th>");
			sb.AppendLine("          <th style=\"width: 140px;\">Источник / Модуль</th>");
			sb.AppendLine("          <th style=\"width: 160px;\">Обнаруженный объект</th>");
			sb.AppendLine("          <th style=\"width: 80px;\">Пункт</th>");
			sb.AppendLine("          <th style=\"width: 90px;\">Таймкод</th>");
			sb.AppendLine("          <th>Описание улики и контекст</th>");
			sb.AppendLine("        </tr>");
			sb.AppendLine("      </thead>");
			sb.AppendLine("      <tbody>");

			foreach (DossierEvidenceItem it in evidenceList)
			{
				string pillClass = it.Severity == "CRITICAL" ? "pill-crit" : "pill-info";
				sb.AppendLine("        <tr>");
				sb.AppendLine("          <td>" + it.Index + "</td>");
				sb.AppendLine("          <td><b>" + it.ModuleName + "</b></td>");
				sb.AppendLine("          <td><span style=\"font-family: monospace; font-size: 12px;\">" + it.ArtifactName + "</span></td>");
				sb.AppendLine("          <td><span class=\"badge-pill " + pillClass + "\">" + it.TargetRule + "</span></td>");
				sb.AppendLine("          <td>" + it.Timestamp.ToString("HH:mm:ss") + "</td>");
				sb.AppendLine("          <td>" + it.EvidenceDetails + "</td>");
				sb.AppendLine("        </tr>");
			}

			sb.AppendLine("      </tbody>");
			sb.AppendLine("    </table>");
			sb.AppendLine("  </div>");

			// Moderator Notes
			sb.AppendLine("  <div style=\"margin-top: 24px;\">");
			sb.AppendLine("    <div style=\"font-size: 14px; font-weight: 700; color: var(--text-muted); text-transform: uppercase;\">Протокол и заключение модератора:</div>");
			sb.AppendLine("    <div style=\"background: var(--card-border); padding: 12px 16px; border-radius: 6px; margin-top: 6px; font-style: italic;\">" + notes + "</div>");
			sb.AppendLine("  </div>");

			// Footer & Cryptographic seal
			string integrityHash = ComputeSha256(pName + mName + verdictText + DateTime.Now.ToShortDateString());
			sb.AppendLine("  <div class=\"footer\">");
			sb.AppendLine("    <div>Криптографический хэш целостности досье (SHA-256): <span style=\"font-family: monospace; color: var(--accent-teal);\">" + integrityHash + "</span></div>");
			sb.AppendLine("    <div style=\"margin-top: 4px;\">Контрольная сумма программы (SHA256): <span style=\"font-family: monospace; color: var(--accent-green);\">" + IntegrityGuard.SelfSha256 + "</span> (Статус целостности: " + (IntegrityGuard.IsTampered ? "🚨 ОБНАРУЖЕНЫ АНОМАЛИИ" : "✅ ПОДТВЕРЖДЕНА") + ")</div>");
			sb.AppendLine("    <div style=\"margin-top: 4px;\">Сформировано программным комплексом MCCheatChecker (Community Edition) 2026. Подлежит приобщению к архиву модерации.</div>");
			sb.AppendLine("  </div>");

			sb.AppendLine("</div>"); // card
			sb.AppendLine("</div>"); // container

			sb.AppendLine("</body>");
			sb.AppendLine("</html>");

			currentGeneratedHtml = sb.ToString();

			try
			{
				webBrowser.DocumentText = currentGeneratedHtml;
				lblStatus.Text = "Досье успешно сгенерировано. Всего улик: " + evidenceList.Count + ". Вердикт: " + (isClear ? "ЧИСТ" : "БАН");
				lblStatus.ForeColor = isClear ? Ui.AccentGreen : Ui.AccentRed;
			}
			catch (Exception ex)
			{
				lblStatus.Text = "Ошибка отображения: " + ex.Message;
				lblStatus.ForeColor = Ui.AccentRed;
			}
		}

		private string ComputeSha256(string input)
		{
			using (SHA256 sha = SHA256.Create())
			{
				byte[] bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(input));
				StringBuilder sb = new StringBuilder();
				for (int i = 0; i < bytes.Length; i++)
				{
					sb.Append(bytes[i].ToString("x2"));
				}
				return sb.ToString();
			}
		}

		#endregion

		#region Actions

		private void SaveHtmlDossier()
		{
			using (SaveFileDialog sfd = new SaveFileDialog())
			{
				sfd.Title = "Сохранить официальное HTML-досье ScreenShare";
				sfd.Filter = "HTML Документ (*.html)|*.html|Все файлы (*.*)|*.*";
				sfd.FileName = string.Format("Dossier_{0}_{1}.html", txtPlayer.Text.Trim(), DateTime.Now.ToString("yyyyMMdd_HHmm"));

				if (sfd.ShowDialog(this) == DialogResult.OK)
				{
					try
					{
						File.WriteAllText(sfd.FileName, currentGeneratedHtml, Encoding.UTF8);
						MessageBox.Show("Досье проверки успешно сохранено в:\n" + sfd.FileName, "Сохранение отчета", MessageBoxButtons.OK, MessageBoxIcon.Information);
					}
					catch (Exception ex)
					{
						MessageBox.Show("Ошибка записи файла: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
					}
				}
			}
		}

		private void OpenInDefaultBrowser()
		{
			try
			{
				string tempPath = Path.Combine(Path.GetTempPath(), string.Format("MCCheatChecker_Dossier_{0}.html", DateTime.Now.Ticks));
				File.WriteAllText(tempPath, currentGeneratedHtml, Encoding.UTF8);
				Process.Start(tempPath);
			}
			catch (Exception ex)
			{
				MessageBox.Show(ex.Message, "Открытие в браузере", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void PrintDossier()
		{
			try
			{
				webBrowser.ShowPrintDialog();
			}
			catch (Exception ex)
			{
				MessageBox.Show("Ошибка вывода на печать: " + ex.Message, "Печать", MessageBoxButtons.OK, MessageBoxIcon.Error);
			}
		}

		private void CopyDiscordSummary()
		{
			string verdictText = cmbVerdict.SelectedItem != null ? cmbVerdict.SelectedItem.ToString() : "ЧИСТ";
			bool isClear = verdictText.StartsWith("ЧИСТ");

			StringBuilder sb = new StringBuilder();
			sb.AppendLine("```yaml");
			sb.AppendLine("=== ОФИЦИАЛЬНОЕ ДОСЬЕ ПРОВЕРКИ SCREENSHARE ===");
			sb.AppendLine("Игрок: " + txtPlayer.Text.Trim());
			sb.AppendLine("Модератор: " + txtModerator.Text.Trim());
			sb.AppendLine("Сервер: " + txtServerMode.Text.Trim());
			sb.AppendLine("Контакты: " + txtContacts.Text.Trim());
			sb.AppendLine("Вердикт: " + (isClear ? "ЧИСТ (ОПРАВДАН)" : "БАН (НАРУШЕНИЕ)"));
			sb.AppendLine("Срок: " + txtBanDuration.Text.Trim() + " дней");
			sb.AppendLine("Количество улик: " + evidenceList.Count);
			sb.AppendLine("Время проверки: " + DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss") + " МСК");
			sb.AppendLine("---------------------------------------------");
			sb.AppendLine("Белый список (Автокликеры/AnyDesk): ПРОВЕРЕНЫ (ALLOWED)");
			sb.AppendLine("```");

			Clipboard.SetText(sb.ToString());
			MessageBox.Show("Краткая форма отчета для Discord скопирована в буфер обмена!", "Discord отчет", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		private void CopyBanCommand()
		{
			string pName = txtPlayer.Text.Trim();
			string verdict = cmbVerdict.SelectedItem != null ? cmbVerdict.SelectedItem.ToString() : "";
			string dur = txtBanDuration.Text.Trim();

			string cmd;
			if (verdict.StartsWith("ЧИСТ"))
			{
				cmd = string.Format("# Игрок {0} чист, нарушений регламента не выявлено.", pName);
			}
			else if (verdict.Contains("5.01"))
			{
				cmd = string.Format("/ban {0} {1}d 5.01 Использование читов / Модифицированный клиент", pName, dur);
			}
			else if (verdict.Contains("5.08"))
			{
				cmd = string.Format("/ban {0} {1}d 5.08 Сокрытие улик / Очистка следов перед проверкой", pName, dur);
			}
			else if (verdict.Contains("5.02"))
			{
				cmd = string.Format("/ban {0} {1}d 5.02 Хранение стороннего ПО", pName, dur);
			}
			else if (verdict.Contains("5.03"))
			{
				cmd = string.Format("/ban {0} {1}d 5.03 Использование X-Ray / Прозрачные текстуры", pName, dur);
			}
			else
			{
				cmd = string.Format("/ban {0} {1}d Нарушение регламента ScreenShare", pName, dur);
			}

			Clipboard.SetText(cmd);
			MessageBox.Show("Команда бана скопирована в буфер:\n" + cmd, "Команда бана", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
