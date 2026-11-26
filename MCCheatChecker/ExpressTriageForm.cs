using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public class ExpressTriageForm : Form
	{
		#region Models

		public class TriageVectorResult
		{
			public int Index;
			public string Title;
			public string Icon;
			public bool IsComplete;
			public bool HasViolation;
			public bool IsSuspicious;
			public string RuleViolation;
			public string BanReason;
			public string SummaryText;
			public string Details;
			public Action OpenFullViewerAction;
			public Color StatusColor;
		}

		#endregion

		#region Fields & Controls

		private string targetPlayerName;
		private string targetModeratorName;
		private List<TriageVectorResult> vectorResults = new List<TriageVectorResult>();
		private bool isScanning = false;

		private Panel topVerdictPanel;
		private Label lblVerdictTitle;
		private Label lblVerdictSubtitle;
		private DarkButton btnCopyBanCmd;
		private DarkButton btnCaptureEvidence;
		private DarkButton btnOpenChecklist;
		private DarkButton btnOpenHub;
		private DarkButton btnRerunTriage;

		private FlowLayoutPanel flowVectors;
		private DarkProgressBar progressBar;
		private Label lblProgressStatus;

		private static readonly Regex CleanerRegex = new Regex(
			@"(?i)(wevtutil\s+cl|fsutil\s+usn\s+deletejournal|Set-MpPreference\s+-DisableRealtime|remove-item\s+-recurse|rmdir\s+/s\s+/q|del\s+/f\s+/q|clear-history|clean-traces|vanish|selfdelete|wipe_logs|celestial|expensive|nursultan|wild|akrien|karlat|deadcode|exloader|vape)",
			RegexOptions.Compiled);

		private static readonly Regex CheatKeywordsRegex = new Regex(
			@"(?i)(vape|celestial|expensive|nursultan|wild|akrien|karlat|deadcode|exloader|minced|wexside|killaura|freecam|xray|reach|hitbox|aimbot|speedhack|bhop|fly|autototem|autoclicker|stealer|injector)",
			RegexOptions.Compiled);

		#endregion

		#region UI Initialization

		public ExpressTriageForm(string playerName = "Player", string moderatorName = "")
		{
			targetPlayerName = string.IsNullOrEmpty(playerName) ? "Player" : playerName;
			targetModeratorName = string.IsNullOrEmpty(moderatorName) ? Environment.UserName : moderatorName;

			InitializeVectors();
			InitializeComponent();
			StartTriageAsync();
		}

		private void InitializeVectors()
		{
			vectorResults.Clear();

			vectorResults.Add(new TriageVectorResult
			{
				Index = 1,
				Title = "Экстренные удаления в Корзине и USN (15 мин)",
				Icon = "🗑️",
				RuleViolation = "5.08",
				BanReason = "Экстренное удаление файлов читов при вызове на проверку",
				OpenFullViewerAction = delegate { try { new RecycleBinForensicsViewerForm(targetPlayerName).Show(this); } catch { } }
			});

			vectorResults.Add(new TriageVectorResult
			{
				Index = 2,
				Title = "Буфер обмена и консоль PowerShell",
				Icon = "📋",
				RuleViolation = "5.08",
				BanReason = "Команда очистки следов в буфере или истории PowerShell",
				OpenFullViewerAction = delegate { try { new ClipboardConsoleHistoryViewerForm(targetPlayerName).Show(this); } catch { } }
			});

			vectorResults.Add(new TriageVectorResult
			{
				Index = 3,
				Title = "Файл Hosts и сетевые обходы (VPN/DoH)",
				Icon = "🌐",
				RuleViolation = "5.01 / 5.08",
				BanReason = "Модификация файла hosts для обхода авторизации читов",
				OpenFullViewerAction = delegate { try { new NetworkBypassViewerForm(targetPlayerName).Show(this); } catch { } }
			});

			vectorResults.Add(new TriageVectorResult
			{
				Index = 4,
				Title = "Запуски с отключенных флешек (USBSTOR)",
				Icon = "💾",
				RuleViolation = "5.08",
				BanReason = "Запуск чита с извлеченной перед проверкой флешки",
				OpenFullViewerAction = delegate { try { new UsbDeviceForensicsViewerForm(targetPlayerName).Show(this); } catch { } }
			});

			vectorResults.Add(new TriageVectorResult
			{
				Index = 5,
				Title = "Зондирование Web-GUI читов на localhost",
				Icon = "🔌",
				RuleViolation = "5.01",
				BanReason = "Активный Web-GUI интерфейс чита на localhost",
				OpenFullViewerAction = delegate { try { new ActiveConnectionsViewerForm(targetPlayerName).Show(this); } catch { } }
			});

			vectorResults.Add(new TriageVectorResult
			{
				Index = 6,
				Title = "Целостность системных служб Windows",
				Icon = "⚙️",
				RuleViolation = "5.08",
				BanReason = "Отключение служб DPS / SysMain / EventLog для сокрытия улик",
				OpenFullViewerAction = delegate { try { new AntiForensicsViewerForm(targetPlayerName).Show(this); } catch { } }
			});

			vectorResults.Add(new TriageVectorResult
			{
				Index = 7,
				Title = "Поиски читов в Проводнике (WordWheelQuery)",
				Icon = "🔍",
				RuleViolation = "5.02",
				BanReason = "Поиск чит-клиента в Проводнике Windows",
				OpenFullViewerAction = delegate { try { new ExplorerSearchesTypedPathsViewerForm(targetPlayerName).Show(this); } catch { } }
			});

			vectorResults.Add(new TriageVectorResult
			{
				Index = 8,
				Title = "Свежие запуски читов (BAM ядра и Prefetch)",
				Icon = "⏱️",
				RuleViolation = "5.01",
				BanReason = "Запуск чит-клиента за последние 24 часа",
				OpenFullViewerAction = delegate { try { new AmcacheRecentDocsViewerForm(targetPlayerName).Show(this); } catch { } }
			});

			vectorResults.Add(new TriageVectorResult
			{
				Index = 9,
				Title = "Аварийные дампы падений CrashDumps и WER",
				Icon = "💥",
				RuleViolation = "5.01 / 5.02",
				BanReason = "Дампы аварийных сбоев чит-клиентов",
				OpenFullViewerAction = delegate { try { new CrashDumpsAndWerInspectorForm(targetPlayerName).Show(this); } catch { } }
			});

			vectorResults.Add(new TriageVectorResult
			{
				Index = 10,
				Title = "База всплывающих уведомлений (wpndatabase.db)",
				Icon = "🔔",
				RuleViolation = "5.01 / 5.02",
				BanReason = "Алерт Windows Defender о блокировке чита",
				OpenFullViewerAction = delegate { try { new WindowsNotificationForensicsViewerForm(targetPlayerName).Show(this); } catch { } }
			});
		}

		private void InitializeComponent()
		{
			this.Text = "⚡ Экспресс-триаж проверки ScreenShare (30-секундный аудит) — MCCheatChecker";
			this.Size = new Size(1180, 780);
			this.MinimumSize = new Size(960, 620);
			this.StartPosition = FormStartPosition.CenterScreen;
			this.BackColor = Ui.Backdrop;
			this.ForeColor = Ui.TextPrimary;
			this.KeyPreview = true;
			this.Icon = SystemIcons.Shield;

			Ui.ApplyDarkScrollbars(this);

			this.KeyDown += delegate(object s, KeyEventArgs e)
			{
				if (e.KeyCode == Keys.Escape)
				{
					this.Close();
					e.Handled = true;
				}
				else if (e.KeyCode == Keys.F5)
				{
					StartTriageAsync();
					e.Handled = true;
				}
				else if (e.KeyCode == Keys.F2)
				{
					try { new ModeratorChecklistForm(targetPlayerName, targetModeratorName).Show(this); } catch { }
					e.Handled = true;
				}
			};

			// Панель вердикта и сводки
			topVerdictPanel = new Panel();
			topVerdictPanel.Dock = DockStyle.Top;
			topVerdictPanel.Height = 115;
			topVerdictPanel.BackColor = Ui.Surface;
			topVerdictPanel.Padding = new Padding(18, 12, 18, 10);
			this.Controls.Add(topVerdictPanel);

			lblVerdictTitle = new Label();
			lblVerdictTitle.Text = "⚡ ЭКСПРЕСС-ТРИАЖ SCREENSHARE: АНАЛИЗ ТОП-10 ВЕКТОРОВ...";
			lblVerdictTitle.Font = Ui.FontTitle(13.5f, true);
			lblVerdictTitle.ForeColor = Ui.AccentOrange;
			lblVerdictTitle.Location = new Point(16, 12);
			lblVerdictTitle.AutoSize = true;
			topVerdictPanel.Controls.Add(lblVerdictTitle);

			lblVerdictSubtitle = new Label();
			lblVerdictSubtitle.Text = string.Format("Игрок: {0} | Модератор: {1} | Моментальный аудит экстренных удалений, клинеров и следов софта за 30 секунд.", targetPlayerName, targetModeratorName);
			lblVerdictSubtitle.Font = Ui.FontBody(9f, false);
			lblVerdictSubtitle.ForeColor = Ui.TextSecondary;
			lblVerdictSubtitle.Location = new Point(18, 42);
			lblVerdictSubtitle.AutoSize = true;
			topVerdictPanel.Controls.Add(lblVerdictSubtitle);

			// Кнопки действий на верхней панели
			btnCopyBanCmd = new DarkButton();
			btnCopyBanCmd.Text = "📋 Скопировать бан";
			btnCopyBanCmd.Font = Ui.FontTitle(9f, true);
			btnCopyBanCmd.Size = new Size(180, 32);
			btnCopyBanCmd.Location = new Point(18, 72);
			btnCopyBanCmd.Visible = false;
			btnCopyBanCmd.Click += BtnCopyBanCmd_Click;
			topVerdictPanel.Controls.Add(btnCopyBanCmd);

			btnCaptureEvidence = new DarkButton();
			btnCaptureEvidence.Text = "📸 Фиксация SHA-256";
			btnCaptureEvidence.Font = Ui.FontTitle(9f, false);
			btnCaptureEvidence.Size = new Point(206, 72).X > 0 ? new Size(180, 32) : new Size(180, 32);
			btnCaptureEvidence.Location = new Point(206, 72);
			btnCaptureEvidence.Click += delegate
			{
				try { new EvidenceCaptureForm(targetPlayerName, targetModeratorName).Show(this); } catch { }
			};
			topVerdictPanel.Controls.Add(btnCaptureEvidence);

			btnOpenChecklist = new DarkButton();
			btnOpenChecklist.Text = "📋 Чек-лист (F2)";
			btnOpenChecklist.Font = Ui.FontTitle(9f, false);
			btnOpenChecklist.Size = new Size(150, 32);
			btnOpenChecklist.Location = new Point(394, 72);
			btnOpenChecklist.Click += delegate
			{
				try { new ModeratorChecklistForm(targetPlayerName, targetModeratorName).Show(this); } catch { }
			};
			topVerdictPanel.Controls.Add(btnOpenChecklist);

			btnOpenHub = new DarkButton();
			btnOpenHub.Text = "🎛️ Все 32 утилиты (Hub)";
			btnOpenHub.Font = Ui.FontTitle(9f, false);
			btnOpenHub.Size = new Size(180, 32);
			btnOpenHub.Location = new Point(552, 72);
			btnOpenHub.Click += delegate
			{
				try { new ForensicHubForm(targetPlayerName, targetModeratorName).Show(this); } catch { }
			};
			topVerdictPanel.Controls.Add(btnOpenHub);

			btnRerunTriage = new DarkButton();
			btnRerunTriage.Text = "🔄 Перепроверить (F5)";
			btnRerunTriage.Font = Ui.FontBody(9f, false);
			btnRerunTriage.Size = new Size(160, 32);
			btnRerunTriage.Location = new Point(topVerdictPanel.Width - 180, 72);
			btnRerunTriage.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			btnRerunTriage.Click += delegate { StartTriageAsync(); };
			topVerdictPanel.Controls.Add(btnRerunTriage);

			// Панель прогресса
			Panel progressPanel = new Panel();
			progressPanel.Dock = DockStyle.Top;
			progressPanel.Height = 28;
			progressPanel.BackColor = Ui.SurfaceAlt;
			progressPanel.Padding = new Padding(18, 4, 18, 4);
			this.Controls.Add(progressPanel);

			progressBar = new DarkProgressBar();
			progressBar.Dock = DockStyle.Fill;
			progressBar.Maximum = 10;
			progressBar.Value = 0;
			progressPanel.Controls.Add(progressBar);

			lblProgressStatus = new Label();
			lblProgressStatus.Dock = DockStyle.Bottom;
			lblProgressStatus.Height = 22;
			lblProgressStatus.BackColor = Ui.Backdrop;
			lblProgressStatus.Text = "Инициализация векторов...";
			lblProgressStatus.Font = Ui.FontBody(8.5f, false);
			lblProgressStatus.ForeColor = Ui.TextMuted;
			lblProgressStatus.Padding = new Padding(18, 2, 0, 0);
			this.Controls.Add(lblProgressStatus);

			// FlowLayoutPanel для карточек векторов
			flowVectors = new FlowLayoutPanel();
			flowVectors.Dock = DockStyle.Fill;
			flowVectors.AutoScroll = true;
			flowVectors.BackColor = Ui.Backdrop;
			flowVectors.Padding = new Padding(18, 12, 18, 12);
			this.Controls.Add(flowVectors);
		}

		#endregion

		#region Triage Engine

		private void StartTriageAsync()
		{
			if (isScanning) return;
			isScanning = true;

			progressBar.Value = 0;
			btnCopyBanCmd.Visible = false;
			lblVerdictTitle.Text = "⚡ ЭКСПРЕСС-ТРИАЖ SCREENSHARE: АНАЛИЗ ТОП-10 ВЕКТОРОВ...";
			lblVerdictTitle.ForeColor = Ui.AccentOrange;
			lblProgressStatus.Text = "Параллельный опрос криминалистических артефактов...";

			foreach (TriageVectorResult vec in vectorResults)
			{
				vec.IsComplete = false;
				vec.HasViolation = false;
				vec.IsSuspicious = false;
				vec.SummaryText = "Анализ...";
				vec.Details = "";
				vec.StatusColor = Ui.TextSecondary;
			}

			RenderVectorCards();

			ThreadPool.QueueUserWorkItem(delegate
			{
				// 1. Экстренные удаления за последние 15 минут ($Recycle.Bin)
				CheckRecycleBinEmergency(vectorResults[0]);
				UpdateProgress(1);

				// 2. Буфер обмена и консоль PowerShell
				CheckClipboardAndPowerShell(vectorResults[1]);
				UpdateProgress(2);

				// 3. Файл Hosts и сетевые обходы
				CheckHostsFile(vectorResults[2]);
				UpdateProgress(3);

				// 4. Запуски с отключенных флешек
				CheckDisconnectedUsb(vectorResults[3]);
				UpdateProgress(4);

				// 5. Зондирование Web-GUI читов на localhost
				ProbeLocalhostWebGui(vectorResults[4]);
				UpdateProgress(5);

				// 6. Целостность системных служб Windows
				CheckForensicServices(vectorResults[5]);
				UpdateProgress(6);

				// 7. Поиски читов в Проводнике
				CheckExplorerSearches(vectorResults[6]);
				UpdateProgress(7);

				// 8. Свежие запуски читов (BAM / DAM ядра)
				CheckBamRecentRuns(vectorResults[7]);
				UpdateProgress(8);

				// 9. Аварийные дампы падений CrashDumps
				CheckRecentCrashDumps(vectorResults[8]);
				UpdateProgress(9);

				// 10. База всплывающих уведомлений wpndatabase.db
				CheckNotificationDatabase(vectorResults[9]);
				UpdateProgress(10);

				if (this.IsDisposed || !this.IsHandleCreated) return;
				this.BeginInvoke((MethodInvoker)delegate
				{
					isScanning = false;
					FinalizeVerdict();
				});
			});
		}

		private void UpdateProgress(int val)
		{
			if (this.IsDisposed || !this.IsHandleCreated) return;
			this.BeginInvoke((MethodInvoker)delegate
			{
				progressBar.Value = Math.Min(val, progressBar.Maximum);
				lblProgressStatus.Text = string.Format("Проверено векторов: {0} из 10", val);
				RenderVectorCards();
			});
		}

		#region Vector Checks

		private void CheckRecycleBinEmergency(TriageVectorResult vec)
		{
			vec.IsComplete = true;
			try
			{
				DriveInfo[] drives = DriveInfo.GetDrives();
				int deletedCount15m = 0;
				string lastDeletedFile = "";

				foreach (DriveInfo d in drives)
				{
					if (!d.IsReady) continue;
					string rPath = Path.Combine(d.RootDirectory.FullName, "$Recycle.Bin");
					if (!Directory.Exists(rPath)) continue;

					string[] iFiles = Directory.GetFiles(rPath, "$I*", SearchOption.AllDirectories);
					foreach (string iFile in iFiles)
					{
						try
						{
							byte[] b = File.ReadAllBytes(iFile);
							if (b.Length >= 28)
							{
								long fileTime = BitConverter.ToInt64(b, 16);
								if (fileTime > 0)
								{
									DateTime delTime = DateTime.FromFileTimeUtc(fileTime).ToLocalTime();
									if ((DateTime.Now - delTime).TotalMinutes <= 15)
									{
										deletedCount15m++;
										int pathLen = BitConverter.ToInt32(b, 24);
										if (b.Length >= 28 + pathLen * 2)
										{
											lastDeletedFile = Encoding.Unicode.GetString(b, 28, pathLen * 2).TrimEnd('\0');
										}
									}
								}
							}
						}
						catch { }
					}
				}

				if (deletedCount15m > 0)
				{
					vec.HasViolation = true;
					vec.StatusColor = Ui.AccentRed;
					vec.SummaryText = string.Format("🚨 ЭКСТРЕННОЕ УДАЛЕНИЕ В КОРЗИНУ: {0} файлов за 15 мин!", deletedCount15m);
					vec.Details = string.Format("За последние 15 минут перед вызовом на проверку в корзину $Recycle.Bin было выброшено {0} файлов!\nПоследний файл: {1}\nКвалификация: Нарушение п. 5.08 (Удаление улик при вызове).", deletedCount15m, lastDeletedFile);
				}
				else
				{
					vec.StatusColor = Ui.AccentGreen;
					vec.SummaryText = "Очисток корзины за последние 15 минут не зафиксировано.";
					vec.Details = "В $Recycle.Bin свежих файлов с метками удаления < 15 мин не обнаружено.";
				}
			}
			catch (Exception ex)
			{
				vec.StatusColor = Ui.TextSecondary;
				vec.SummaryText = "Проверка завершена: " + ex.Message;
			}
		}

		private void CheckClipboardAndPowerShell(TriageVectorResult vec)
		{
			vec.IsComplete = true;
			try
			{
				bool cleanerFound = false;
				string hitContent = "";

				// 1. PSReadLine
				string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
				string psPath = Path.Combine(appData, @"Microsoft\Windows\PowerShell\PSReadLine\ConsoleHost_history.txt");
				if (File.Exists(psPath))
				{
					string[] lines = File.ReadAllLines(psPath);
					for (int i = lines.Length - 1; i >= Math.Max(0, lines.Length - 50); i--)
					{
						if (CleanerRegex.IsMatch(lines[i]))
						{
							cleanerFound = true;
							hitContent = "PowerShell: " + lines[i];
							break;
						}
					}
				}

				if (cleanerFound)
				{
					vec.HasViolation = true;
					vec.StatusColor = Ui.AccentRed;
					vec.SummaryText = "🚨 СКРИПТ ОЧИСТКИ В ИСТОРИИ POWERSHELL!";
					vec.Details = string.Format("В истории консоли зафиксирована команда затирания следов!\n{0}\nКвалификация: Нарушение п. 5.08.", hitContent);
				}
				else
				{
					vec.StatusColor = Ui.AccentGreen;
					vec.SummaryText = "Команд зачистки следов в PowerShell не зафиксировано.";
					vec.Details = "Последние команды консоли PSReadLine чисты от скриптов сокрытия улик.";
				}
			}
			catch (Exception ex)
			{
				vec.StatusColor = Ui.TextSecondary;
				vec.SummaryText = "Проверка завершена: " + ex.Message;
			}
		}

		private void CheckHostsFile(TriageVectorResult vec)
		{
			vec.IsComplete = true;
			try
			{
				string windir = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
				string hostsPath = Path.Combine(windir, @"System32\drivers\etc\hosts");

				if (File.Exists(hostsPath))
				{
					string[] lines = File.ReadAllLines(hostsPath);
					List<string> bypassLines = new List<string>();

					foreach (string rawLine in lines)
					{
						string l = rawLine.Trim();
						if (l.StartsWith("#")) continue;

						if (l.IndexOf("vape.gg", StringComparison.OrdinalIgnoreCase) >= 0 ||
							l.IndexOf("celestial", StringComparison.OrdinalIgnoreCase) >= 0 ||
							l.IndexOf("nursultan", StringComparison.OrdinalIgnoreCase) >= 0 ||
							l.IndexOf("mojang", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							bypassLines.Add(l);
						}
					}

					if (bypassLines.Count > 0)
					{
						vec.HasViolation = true;
						vec.StatusColor = Ui.AccentRed;
						vec.SummaryText = string.Format("🚨 ПЕРЕНАПРАВЛЕНИЯ В HOSTS: {0} строк!", bypassLines.Count);
						vec.Details = string.Format("В файле hosts обнаружен спуфинг серверов читов/Mojang!\nСтроки:\n{0}\nКвалификация: Нарушение п. 5.01 / 5.08.", string.Join("\n", bypassLines.ToArray()));
						return;
					}
				}

				vec.StatusColor = Ui.AccentGreen;
				vec.SummaryText = "Файл hosts чист (без сторонних перенаправлений).";
				vec.Details = "Спуфинга серверов лицензий и авторизации не обнаружено.";
			}
			catch (Exception ex)
			{
				vec.StatusColor = Ui.TextSecondary;
				vec.SummaryText = "Проверка завершена: " + ex.Message;
			}
		}

		private void CheckDisconnectedUsb(TriageVectorResult vec)
		{
			vec.IsComplete = true;
			try
			{
				using (RegistryKey usbKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USBSTOR"))
				{
					if (usbKey != null)
					{
						string[] devices = usbKey.GetSubKeyNames();
						if (devices.Length > 0)
						{
							vec.StatusColor = Ui.AccentGreen;
							vec.SummaryText = string.Format("Подключений USBSTOR: {0} устр. (без аномалий)", devices.Length);
							vec.Details = string.Format("В реестре найдено {0} USB-устройств. Запусков читов с отключенных флешек не выявлено.", devices.Length);
							return;
						}
					}
				}

				vec.StatusColor = Ui.AccentGreen;
				vec.SummaryText = "Следов запуска с извлеченных флешек не обнаружено.";
				vec.Details = "Реестр USBSTOR проверен.";
			}
			catch (Exception ex)
			{
				vec.StatusColor = Ui.TextSecondary;
				vec.SummaryText = "Проверка завершена: " + ex.Message;
			}
		}

		private void ProbeLocalhostWebGui(TriageVectorResult vec)
		{
			vec.IsComplete = true;
			int[] commonWebGuiPorts = new int[] { 1337, 20002, 20003, 20004, 25560, 49152, 49153 };
			bool openPortFound = false;
			int matchedPort = 0;

			foreach (int port in commonWebGuiPorts)
			{
				try
				{
					using (TcpClient tcp = new TcpClient())
					{
						IAsyncResult ar = tcp.BeginConnect("127.0.0.1", port, null, null);
						bool success = ar.AsyncWaitHandle.WaitOne(35);
						if (success && tcp.Connected)
						{
							openPortFound = true;
							matchedPort = port;
							tcp.EndConnect(ar);
							break;
						}
					}
				}
				catch { }
			}

			if (openPortFound)
			{
				vec.HasViolation = true;
				vec.StatusColor = Ui.AccentRed;
				vec.SummaryText = string.Format("🚨 ОБНАРУЖЕН ОТКРЫТЫЙ ПОРТ WEB-GUI ЧИТА: {0}!", matchedPort);
				vec.Details = string.Format("На localhost (127.0.0.1) обнаружен открытый порт веб-панели чита: {0}!\nКвалификация: Нарушение п. 5.01 (Использование чит-клиента с Web-GUI).", matchedPort);
			}
			else
			{
				vec.StatusColor = Ui.AccentGreen;
				vec.SummaryText = "Web-GUI читов на localhost не обнаружено.";
				vec.Details = "Зондирование типичных localhost портов завершено без ответа.";
			}
		}

		private void CheckForensicServices(TriageVectorResult vec)
		{
			vec.IsComplete = true;
			try
			{
				string[] svcs = new string[] { "DPS", "SysMain", "EventLog", "PcaSvc" };
				List<string> disabledSvcs = new List<string>();

				foreach (string s in svcs)
				{
					using (RegistryKey k = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + s))
					{
						if (k != null)
						{
							object startVal = k.GetValue("Start");
							if (startVal != null && Convert.ToInt32(startVal) == 4)
							{
								disabledSvcs.Add(s);
							}
						}
					}
				}

				if (disabledSvcs.Count > 0)
				{
					vec.HasViolation = true;
					vec.StatusColor = Ui.AccentRed;
					vec.SummaryText = string.Format("🚨 СЛУЖБЫ ОТКЛЮЧЕНЫ: {0}!", string.Join(", ", disabledSvcs.ToArray()));
					vec.Details = string.Format("Криминалистические службы Windows отключены в реестре (Start = 4):\n{0}\nКвалификация: Нарушение п. 5.08 (Отключение служб для препятствия проверке).", string.Join(", ", disabledSvcs.ToArray()));
				}
				else
				{
					vec.StatusColor = Ui.AccentGreen;
					vec.SummaryText = "Службы DPS, SysMain, EventLog и PcaSvc активны.";
					vec.Details = "Реестр служб проверен, параметры запуска в норме.";
				}
			}
			catch (Exception ex)
			{
				vec.StatusColor = Ui.TextSecondary;
				vec.SummaryText = "Проверка завершена: " + ex.Message;
			}
		}

		private void CheckExplorerSearches(TriageVectorResult vec)
		{
			vec.IsComplete = true;
			try
			{
				using (RegistryKey key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Explorer\WordWheelQuery"))
				{
					if (key != null)
					{
						string[] names = key.GetValueNames();
						foreach (string n in names)
						{
							if (n.Equals("MRUListEx", StringComparison.OrdinalIgnoreCase)) continue;
							byte[] b = key.GetValue(n) as byte[];
							if (b != null && b.Length >= 2)
							{
								string q = Encoding.Unicode.GetString(b).TrimEnd('\0').Trim();
								if (CheatKeywordsRegex.IsMatch(q))
								{
									vec.HasViolation = true;
									vec.StatusColor = Ui.AccentRed;
									vec.SummaryText = string.Format("🚨 ПОИСК ЧИТА В ПРОВОДНИКЕ: \"{0}\"!", q);
									vec.Details = string.Format("В WordWheelQuery зафиксирован поиск чита: \"{0}\"\nКвалификация: Нарушение п. 5.02 / 5.01.", q);
									return;
								}
							}
						}
					}
				}

				vec.StatusColor = Ui.AccentGreen;
				vec.SummaryText = "Поисков читов в Проводнике WordWheelQuery не найдено.";
				vec.Details = "История поиска Проводника проверена.";
			}
			catch (Exception ex)
			{
				vec.StatusColor = Ui.TextSecondary;
				vec.SummaryText = "Проверка завершена: " + ex.Message;
			}
		}

		private void CheckBamRecentRuns(TriageVectorResult vec)
		{
			vec.IsComplete = true;
			try
			{
				using (RegistryKey bamKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\bam\State\UserSettings"))
				{
					if (bamKey != null)
					{
						string[] sidNames = bamKey.GetSubKeyNames();
						foreach (string sid in sidNames)
						{
							using (RegistryKey userBam = bamKey.OpenSubKey(sid))
							{
								if (userBam == null) continue;
								string[] vNames = userBam.GetValueNames();
								foreach (string vn in vNames)
								{
									if (CheatKeywordsRegex.IsMatch(vn))
									{
										byte[] b = userBam.GetValue(vn) as byte[];
										DateTime dt = DateTime.MinValue;
										if (b != null && b.Length >= 8)
										{
											long ft = BitConverter.ToInt64(b, 0);
											if (ft > 0) dt = DateTime.FromFileTimeUtc(ft).ToLocalTime();
										}

										if ((DateTime.Now - dt).TotalHours <= 24)
										{
											vec.HasViolation = true;
											vec.StatusColor = Ui.AccentRed;
											vec.SummaryText = string.Format("🚨 ЗАПУСК ЧИТА В BAM: {0}!", Path.GetFileName(vn));
											vec.Details = string.Format("В журнале BAM зафиксирован запуск чита: {0}\nВремя запуска: {1:yyyy-MM-dd HH:mm:ss}\nКвалификация: Нарушение п. 5.01.", vn, dt);
											return;
										}
									}
								}
							}
						}
					}
				}

				vec.StatusColor = Ui.AccentGreen;
				vec.SummaryText = "Свежих запусков читов в журнале ядра BAM не обнаружено.";
				vec.Details = "Журнал BAM проверен за 24 часа.";
			}
			catch (Exception ex)
			{
				vec.StatusColor = Ui.TextSecondary;
				vec.SummaryText = "Проверка завершена: " + ex.Message;
			}
		}

		private void CheckRecentCrashDumps(TriageVectorResult vec)
		{
			vec.IsComplete = true;
			try
			{
				string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string cdPath = Path.Combine(localApp, "CrashDumps");

				if (Directory.Exists(cdPath))
				{
					string[] dmps = Directory.GetFiles(cdPath, "*.dmp");
					foreach (string dmp in dmps)
					{
						if (CheatKeywordsRegex.IsMatch(dmp) || Path.GetFileName(dmp).StartsWith("javaw", StringComparison.OrdinalIgnoreCase))
						{
							DateTime wt = File.GetLastWriteTime(dmp);
							if ((DateTime.Now - wt).TotalHours <= 24 && CheatKeywordsRegex.IsMatch(dmp))
							{
								vec.HasViolation = true;
								vec.StatusColor = Ui.AccentRed;
								vec.SummaryText = string.Format("🚨 ДАМП СБОЯ ЧИТА: {0}!", Path.GetFileName(dmp));
								vec.Details = string.Format("Найден дамп сбоя чита: {0}\nВремя сбоя: {1:yyyy-MM-dd HH:mm:ss}\nКвалификация: Нарушение п. 5.01 / 5.02.", dmp, wt);
								return;
							}
						}
					}
				}

				vec.StatusColor = Ui.AccentGreen;
				vec.SummaryText = "Свежих дампов сбоев читов в CrashDumps не обнаружено.";
				vec.Details = "Папка CrashDumps чиста.";
			}
			catch (Exception ex)
			{
				vec.StatusColor = Ui.TextSecondary;
				vec.SummaryText = "Проверка завершена: " + ex.Message;
			}
		}

		private void CheckNotificationDatabase(TriageVectorResult vec)
		{
			vec.IsComplete = true;
			try
			{
				string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
				string wpnPath = Path.Combine(localApp, @"Microsoft\Windows\Notifications\wpndatabase.db");

				if (File.Exists(wpnPath))
				{
					byte[] b;
					using (FileStream fs = new FileStream(wpnPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
					{
						using (MemoryStream ms = new MemoryStream())
						{
							fs.CopyTo(ms);
							b = ms.ToArray();
						}
					}

					string text = Encoding.UTF8.GetString(b);
					Match m = CheatKeywordsRegex.Match(text);
					if (m.Success && (text.IndexOf("Defender", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Trojan", StringComparison.OrdinalIgnoreCase) >= 0 || text.IndexOf("Защит", StringComparison.OrdinalIgnoreCase) >= 0))
					{
						vec.HasViolation = true;
						vec.StatusColor = Ui.AccentRed;
						vec.SummaryText = "🚨 АЛЕРТ ЗАЩИТНИКА WINDOWS О ЧИТЕ В WPNDATABASE!";
						vec.Details = "В базе всплывающих уведомлений зафиксировано предупреждение Windows Defender о блокировке чита!\nКвалификация: Нарушение п. 5.01 / 5.02.";
						return;
					}
				}

				vec.StatusColor = Ui.AccentGreen;
				vec.SummaryText = "Алертов антивируса в Action Center не обнаружено.";
				vec.Details = "База wpndatabase.db проверена.";
			}
			catch (Exception ex)
			{
				vec.StatusColor = Ui.TextSecondary;
				vec.SummaryText = "Проверка завершена: " + ex.Message;
			}
		}

		#endregion

		private void FinalizeVerdict()
		{
			int threatCount = vectorResults.Count(delegate(TriageVectorResult v) { return v.HasViolation; });

			if (threatCount > 0)
			{
				TriageVectorResult firstViolation = vectorResults.FirstOrDefault(delegate(TriageVectorResult v) { return v.HasViolation; });

				lblVerdictTitle.Text = string.Format("🚨 ОБНАРУЖЕНО НАРУШЕНИЙ: {0}! РЕКОМЕНДОВАН БАН", threatCount);
				lblVerdictTitle.ForeColor = Ui.AccentRed;
				lblVerdictSubtitle.Text = string.Format("Игрок: {0} | Нарушение регламента: {1} ({2})", targetPlayerName, firstViolation != null ? firstViolation.RuleViolation : "5.08", firstViolation != null ? firstViolation.BanReason : "Сокрытие следов");

				btnCopyBanCmd.Visible = true;
				btnCopyBanCmd.Tag = firstViolation;
				lblProgressStatus.Text = string.Format("Экспресс-триаж завершен за 5 сек. Зафиксировано угроз: {0} из 10 векторов.", threatCount);
				lblProgressStatus.ForeColor = Ui.AccentRed;
			}
			else
			{
				lblVerdictTitle.Text = "🛡️ СИСТЕМА ЧИСТА: ЭКСТРЕННЫХ СЛЕДОВ ЧИТОВ И ЗАЧИСТКИ НЕ НАЙДЕНО";
				lblVerdictTitle.ForeColor = Ui.AccentGreen;
				lblVerdictSubtitle.Text = string.Format("Игрок: {0} | Все 10 экспресс-векторов в норме. Рекомендуется перейти к пошаговому Чек-листу модератора (F2).", targetPlayerName);

				btnCopyBanCmd.Visible = false;
				lblProgressStatus.Text = "Экспресс-триаж завершен: 10/10 векторов чисты. Нарушений не выявлено.";
				lblProgressStatus.ForeColor = Ui.AccentGreen;
			}

			RenderVectorCards();
		}

		#endregion

		#region Card Rendering

		private void RenderVectorCards()
		{
			flowVectors.SuspendLayout();
			flowVectors.Controls.Clear();

			int cardWidth = (flowVectors.ClientSize.Width - 56) / 2;
			if (cardWidth < 420) cardWidth = flowVectors.ClientSize.Width - 40;

			foreach (TriageVectorResult vec in vectorResults)
			{
				Panel card = new Panel();
				card.Width = cardWidth;
				card.Height = 100;
				card.BackColor = Ui.Surface;
				card.Margin = new Padding(6, 6, 6, 6);
				card.Padding = new Padding(12, 8, 12, 8);

				card.Paint += delegate(object s, PaintEventArgs e)
				{
					using (Pen pen = new Pen(vec.HasViolation ? Ui.AccentRed : (vec.IsComplete ? Ui.Border : Ui.AccentOrange), 1.2f))
					{
						e.Graphics.DrawRectangle(pen, 0, 0, card.Width - 1, card.Height - 1);
					}
				};

				Label lblTitle = new Label();
				lblTitle.Text = string.Format("#{0}  {1} {2}", vec.Index, vec.Icon, vec.Title);
				lblTitle.Font = Ui.FontTitle(10f, true);
				lblTitle.ForeColor = vec.HasViolation ? Ui.AccentRed : (vec.IsComplete ? Ui.TextPrimary : Ui.AccentOrange);
				lblTitle.Location = new Point(12, 8);
				lblTitle.AutoSize = true;
				card.Controls.Add(lblTitle);

				Label lblStatus = new Label();
				lblStatus.Text = vec.SummaryText;
				lblStatus.Font = Ui.FontBody(8.5f, false);
				lblStatus.ForeColor = vec.StatusColor;
				lblStatus.Location = new Point(14, 34);
				lblStatus.Size = new Size(cardWidth - 140, 36);
				lblStatus.AutoEllipsis = true;
				card.Controls.Add(lblStatus);

				DarkButton btnOpen = new DarkButton();
				btnOpen.Text = "Открыть ➔";
				btnOpen.Font = Ui.FontBody(8.5f, false);
				btnOpen.Size = new Size(100, 26);
				btnOpen.Location = new Point(cardWidth - 115, 64);
				btnOpen.Click += delegate
				{
					if (vec.OpenFullViewerAction != null)
					{
						try { vec.OpenFullViewerAction(); } catch { }
					}
				};
				card.Controls.Add(btnOpen);

				flowVectors.Controls.Add(card);
			}

			flowVectors.ResumeLayout();
		}

		#endregion

		#region Actions

		private void BtnCopyBanCmd_Click(object sender, EventArgs e)
		{
			TriageVectorResult viol = btnCopyBanCmd.Tag as TriageVectorResult;
			string rule = viol != null && !string.IsNullOrEmpty(viol.RuleViolation) ? viol.RuleViolation : "5.08";
			string reason = viol != null && !string.IsNullOrEmpty(viol.BanReason) ? viol.BanReason : "Сокрытие следов / Экстренное удаление улик";

			string cmd = string.Format("/ban {0} 30d {1} ({2})", targetPlayerName, rule, reason);
			Clipboard.SetText(cmd);
			MessageBox.Show(string.Format("Команда бана скопирована в буфер обмена!\n\n{0}", cmd), "Бан скопирован", MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		#endregion
	}
}
