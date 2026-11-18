using System;
using System.Collections.Generic;
using System.IO;
using System.Management;
using System.Net.NetworkInformation;
using System.Text;
using Microsoft.Win32;

namespace MCCheatChecker
{
	public static class HardwareChecks
	{
		private static readonly string[] VmMacPrefixes = new string[] { "00:05:69", "00:0c:29", "00:50:56", "08:00:27", "0a:00:27", "00:1c:42", "00:03:ff", "52:54:00", "00:15:5d", "b8:a3:86" };

		private static readonly string[] VmBiosSignatures = new string[] { "innotek gmbh", "virtualbox", "vmware, inc", "microsoft corporation" };

		private static readonly string[] VmModelSignatures = new string[] { "virtualbox", "vmware virtual", "kvm", "qemu", "parallels", "xen", "hyper-v", "virtual machine", "bochs", "vbox" };

		private static readonly string[] FakeSmbiosUuids = new string[] { "03000200-0400-0500-0007-00080009000a", "03000200-0400-0500-0006-000700090000", "564d0000-0000-0000-0000-000000000000", "00000000-0000-0000-0000-000000000000" };

		private static readonly string[] MacroHidSignatures = new string[]
		{
			"kmbox", "km-net", "kmbox net", "makcu", "cuda", "arduino", "arduino llc", "arduino sa", "leonardo",
			"micro", "esp32", "esp8266", "ch9329", "ch9350", "usb2uart", "teensy", "pjrc", "wch", "qinheng",
			"hid macro", "macro keyboard", "macro mouse", "auto click", "autoclick", "clicker", "fast click", "rp2040", "raspberry pi", "pico",
			"stm32", "libretiny", "b按键", "кукла", "botmaker", "mouse jiggler", "jiggler",
			"kmbox b", "kmbox-b", "kmbox pro", "ch340", "cp2102", "ft232", "pl2303", "interception", "vjoy", "vigem", "virtual bus", "usb serial", "silicon labs", "pro micro", "digispark", "attiny85", "cp210x", "ftdi"
		};

		private static readonly string[] SpooferServiceSignatures = new string[]
		{
			"hwidspoofer", "hwid_spoofer", "spoofer", "devimp", "deviceimpersonate", "diskid", "serialspoof", "macspoofer", "hidhide", "hidremapper",
			"portclone", "biosspoof", "eac_spoofer", "bec_spoofer", "sguard_spoof"
		};

		private static readonly string[] SpooferFolderSignatures = new string[]
		{
			"hwid spoofer", "hwidspoofer", "hwid-spoofer", "spoofer", "deleter", "hwid deleter", "hwid-changer", "hwid changer", "cleaner hwid", "disk cleaner",
			"ssd cleaner", "hdd serial", "serial changer", "mac changer", "macchanger", "volume id", "volumeid", "usb disk serial", "hwidlock", "hwid-lock"
		};

		public static List<CheckResult> RunHardwareChecks()
		{
			List<CheckResult> list = new List<CheckResult>();
			if (!PlatformHelper.IsWindows)
			{
				list.Add(CheckUnixCpuAndRam());
				list.Add(CheckUnixGpuAndMachine());
				return list;
			}
			list.Add(CheckCpuSpoofing());
			list.Add(CheckRamSpoofing());
			list.Add(CheckGpu());
			list.Add(CheckBiosAndMachine());
			return list;
		}

		public static List<CheckResult> RunHwidChecks()
		{
			List<CheckResult> list = new List<CheckResult>();
			if (!PlatformHelper.IsWindows)
			{
				list.Add(CheckUnixMachineIdAndMac());
				list.Add(CheckSpooferFolders());
				return list;
			}
			list.Add(CheckHwidIdentity());
			list.Add(CheckMacSpoofing());
			list.Add(CheckSpooferServices());
			list.Add(CheckSpooferFolders());
			return list;
		}

		public static List<CheckResult> RunHidChecks()
		{
			List<CheckResult> list = new List<CheckResult>();
			if (!PlatformHelper.IsWindows)
			{
				list.Add(CheckUnixHidDevices());
				return list;
			}
			list.Add(CheckHidMacroDevices());
			return list;
		}

		private static CheckResult CheckCpuSpoofing()
		{
			CheckResult checkResult = NewResult("Аппаратная часть", "CPU: модель, ядра и частоты (сверка WMI ↔ реестр)");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			int num = -1;
			int num2 = 0;
			int num3 = 0;
			string text = "";
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Name, Manufacturer, NumberOfCores, NumberOfLogicalProcessors, CurrentClockSpeed, MaxClockSpeed FROM Win32_Processor"))
				{
					foreach (ManagementObject item in managementObjectSearcher.Get())
					{
						text = Str(item, "Name");
						string text2 = Str(item, "Manufacturer");
						num2 = Int(item, "NumberOfCores");
						num3 = Int(item, "NumberOfLogicalProcessors");
						num = Int(item, "CurrentClockSpeed");
						int num4 = Int(item, "MaxClockSpeed");
						list.Add("WMI: " + text + " | вендор: " + text2 + " | ядер: " + num2 + " | потоков: " + num3 + " | тек. частота: " + num + " МГц | макс.: " + num4 + " МГц");
						if (num2 == 0 || num3 == 0)
						{
							list2.Add("WMI отдаёт нулевое число ядер/потоков — данные CPU скрыты или подменены");
						}
						if (num2 > 0 && num3 > 0 && num3 < num2)
						{
							list2.Add("Логических процессоров МЕНЬШЕ физических ядер (" + num3 + " < " + num2 + ") — невозможная конфигурация, признак спуфинга");
						}
						if (num4 > 0 && num > 0 && num < num4 / 4)
						{
							list.Add("Частота сильно ниже заявленной (возможен троттлинг/энергосбережение — не признак чита)");
						}
					}
				}
			}
			catch (Exception ex)
			{
				list2.Add("WMI Win32_Processor недоступен: " + ex.Message);
			}
			int num5 = -1;
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("HARDWARE\\DESCRIPTION\\System\\CentralProcessor\\0"))
				{
					if (registryKey != null)
					{
						string text3 = (registryKey.GetValue("ProcessorNameString") ?? "").ToString().Trim();
						object value = registryKey.GetValue("~MHz");
						num5 = ((value == null) ? (-1) : Convert.ToInt32(value));
						list.Add("Реестр (HARDWARE\\...\\CentralProcessor\\0): " + text3 + " | " + num5 + " МГц");
						if (!string.IsNullOrEmpty(text) && !string.IsNullOrEmpty(text3))
						{
							string text4 = text.ToLowerInvariant().Replace(" ", "");
							string text5 = text3.ToLowerInvariant().Replace(" ", "");
							if (text4.Length > 4 && text5.Length > 4 && text4 != text5 && !text4.Contains(text5) && !text5.Contains(text4))
							{
								list2.Add("Имя CPU в WMI (\"" + text + "\") НЕ совпадает с реестром (\"" + text3 + "\") — классический спуфинг модели процессора");
							}
						}
					}
				}
			}
			catch
			{
			}
			if (num > 0 && num5 > 0)
			{
				double num6 = (double)Math.Abs(num - num5) / (double)Math.Max(num, num5);
				if (num6 > 0.4)
				{
					list2.Add("Частота CPU по WMI (" + num + " МГц) расходится с реестром (" + num5 + " МГц) более чем на 40%");
				}
			}
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено несоответствий: " + list2.Count + " — проверь вручную") : ("Конфигурация CPU непротиворечива (ядер: " + num2 + ", потоков: " + num3 + ")"));
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult CheckRamSpoofing()
		{
			CheckResult checkResult = NewResult("Аппаратная часть", "RAM: физический объём модулей ↔ объём, доступный системе");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			long num = 0L;
			int num2 = 0;
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Capacity, Speed, Manufacturer, PartNumber, ConfiguredClockSpeed, SMBIOSMemoryType FROM Win32_PhysicalMemory"))
				{
					foreach (ManagementObject item in managementObjectSearcher.Get())
					{
						num2++;
						long num3 = Long(item, "Capacity");
						num += num3;
						string text = Str(item, "Manufacturer");
						string text2 = Str(item, "PartNumber");
						int num4 = Int(item, "Speed");
						int num5 = Int(item, "ConfiguredClockSpeed");
						list.Add("Модуль " + num2 + ": " + num3 / 1073741824 + " ГБ | " + text + " | партномер: " + (string.IsNullOrEmpty(text2) ? "—" : text2.Trim()) + " | заявлено: " + num4 + " МГц | работает: " + num5 + " МГц");
						if (num3 == 0)
						{
							list2.Add("Модуль #" + num2 + " имеет нулевую ёмкость в WMI — данные памяти подменены");
						}
					}
				}
			}
			catch (Exception ex)
			{
				list2.Add("WMI Win32_PhysicalMemory недоступен: " + ex.Message);
			}
			long num6 = 0L;
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher2 = new ManagementObjectSearcher("SELECT TotalPhysicalMemory FROM Win32_ComputerSystem"))
				{
					foreach (ManagementObject item2 in managementObjectSearcher2.Get())
					{
						num6 = Long(item2, "TotalPhysicalMemory");
					}
				}
			}
			catch
			{
			}
			long num7 = num6 / 1073741824;
			long num8 = num / 1073741824;
			list.Add("Итого по модулям: " + num8 + " ГБ | видит ОС: " + num7 + " ГБ");
			if (num > 0 && num6 > 0)
			{
				double num9 = (double)(num - num6) / (double)num;
				if (num9 > 0.25)
				{
					list2.Add("ОС видит на " + Math.Round(num9 * 100.0, 1) + "% меньше памяти, чем установлено физически — возможная подмена объёма RAM");
				}
			}
			if (num7 > 0 && num7 < 2)
			{
				list2.Add("ОС видит менее 2 ГБ RAM — нетипично для игрового ПК (часто виртуальная машина)");
			}
			checkResult.Detail = ((list2.Count > 0) ? ("Несоответствия по памяти: " + list2.Count) : ((num2 > 0) ? ("Память: " + num8 + " ГБ в " + num2 + " модулях — расхождений нет") : ("ОС: " + num7 + " ГБ")));
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult CheckGpu()
		{
			CheckResult checkResult = NewResult("Аппаратная часть", "Видеокарта: модель, объём видеопамяти, драйвер");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			int num = 0;
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Name, AdapterRAM, DriverVersion, VideoProcessor, PNPDeviceID FROM Win32_VideoController"))
				{
					foreach (ManagementObject item in managementObjectSearcher.Get())
					{
						num++;
						string text = Str(item, "Name");
						long num2 = Long(item, "AdapterRAM");
						string text2 = Str(item, "DriverVersion");
						string text3 = Str(item, "PNPDeviceID");
						list.Add("GPU " + num + ": " + text + " | видеопамять: " + ((num2 == 0) ? "0 (скрыта)" : (num2 / 1048576 + " МБ")) + " | драйвер: " + text2);
						string text4 = text.ToLowerInvariant();
						if (text4.Contains("basic render") || text4.Contains("microsoft"))
						{
							list2.Add("Работает «Microsoft Basic Render Driver» — дискретной видеокарты нет/драйвер удалён (признак VM или скрытия GPU)");
						}
						if (text4.Contains("virtualbox") || text4.Contains("vmware") || text4.Contains("hyper-v") || text4.Contains("basic display") || text4.Contains("paravirtual"))
						{
							list2.Add("Видеоадаптер виртуальный: " + text);
						}
						if (text3.ToLowerInvariant().Contains("ven_80ee") || text3.ToLowerInvariant().Contains("ven_15ad"))
						{
							list2.Add("PCI-вендор видеоадаптера принадлежит гипервизору: " + text3);
						}
						if (num2 <= 16777216 && !text4.Contains("intel") && !text4.Contains("basic") && !text4.Contains("virtual") && !text4.Contains("microsoft"))
						{
							list2.Add("Объём видеопамяти обнулён/минимален (" + num2 + " байт) при карте \"" + text + "\" — данные GPU подменены");
						}
					}
				}
			}
			catch (Exception ex)
			{
				list2.Add("WMI Win32_VideoController недоступен: " + ex.Message);
			}
			if (num == 0)
			{
				list2.Add("Ни одного видеоадаптера не перечислено в WMI — подсистема отображения скрыта");
			}
			checkResult.Detail = ((list2.Count > 0) ? ("По видеокарте есть несоответствия: " + list2.Count) : ("Видеоадаптеров: " + num + " — данные корректны"));
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult CheckBiosAndMachine()
		{
			CheckResult checkResult = NewResult("Аппаратная часть", "BIOS, производитель машины и признак виртуальной среды");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT Manufacturer, SMBIOSBIOSVersion, SerialNumber, ReleaseDate, Version FROM Win32_BIOS"))
				{
					foreach (ManagementObject item in managementObjectSearcher.Get())
					{
						string text = Str(item, "Manufacturer");
						string text2 = Str(item, "SMBIOSBIOSVersion");
						string text3 = Str(item, "SerialNumber");
						string text4 = Str(item, "ReleaseDate");
						list.Add("BIOS: " + text + " | версия: " + text2 + " | серийный: " + (string.IsNullOrEmpty(text3) ? "(пусто)" : text3) + " | дата: " + text4);
						string text5 = text.ToLowerInvariant() + " " + text2.ToLowerInvariant();
						string[] vmBiosSignatures = VmBiosSignatures;
						foreach (string value in vmBiosSignatures)
						{
							if (text5.Contains(value))
							{
								list2.Add("🚨 [5.08 - Виртуальная машина в BIOS]: " + text + " / " + text2);
								break;
							}
						}
						if (string.IsNullOrEmpty(text3) || text3.Trim("0 ".ToCharArray()).Length == 0)
						{
							list2.Add("🚨 [5.08 - HWID-спуфер (серийный номер BIOS обнулён/пуст)]");
						}
					}
				}
			}
			catch (Exception ex)
			{
				list2.Add("Win32_BIOS: " + ex.Message);
			}
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher2 = new ManagementObjectSearcher("SELECT Manufacturer, Model, PCSystemType, TotalPhysicalMemory FROM Win32_ComputerSystem"))
				{
					foreach (ManagementObject item2 in managementObjectSearcher2.Get())
					{
						string text6 = Str(item2, "Manufacturer");
						string text7 = Str(item2, "Model");
						int num = Int(item2, "PCSystemType");
						list.Add("Машина: " + text6 + " " + text7 + " | PCSystemType=" + num);
						string text8 = (text6 + " " + text7).ToLowerInvariant();
						string[] vmModelSignatures = VmModelSignatures;
						foreach (string value2 in vmModelSignatures)
						{
							if (text8.Contains(value2))
							{
								list2.Add("🚨 [5.08 - Модель виртуальной машины]: " + text6 + " " + text7);
								break;
							}
						}
						if (num == 2)
						{
							list2.Add("🚨 [5.08 - PCSystemType = 2 (Virtual Machine)]");
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher3 = new ManagementObjectSearcher("SELECT UUID, Version FROM Win32_ComputerSystemProduct"))
				{
					foreach (ManagementObject item3 in managementObjectSearcher3.Get())
					{
						string text9 = Str(item3, "UUID");
						list.Add("SMBIOS UUID: " + text9);
						string[] fakeSmbiosUuids = FakeSmbiosUuids;
						foreach (string b in fakeSmbiosUuids)
						{
							if (string.Equals(text9, b, StringComparison.OrdinalIgnoreCase))
							{
								list2.Add("SMBIOS UUID равен заглушке гипервизора/обнулён: " + text9);
								break;
							}
						}
					}
				}
			}
			catch
			{
			}
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control"))
				{
					if (registryKey != null)
					{
						object value3 = registryKey.GetValue("SystemEnvironmentValue");
						if (value3 != null && value3.ToString().IndexOf("hypervisor", StringComparison.OrdinalIgnoreCase) >= 0)
						{
							list.Add("Под запущенным гипервизором: SystemEnvironmentValue = " + value3);
						}
					}
				}
				using (RegistryKey registryKey2 = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Virtual Machine\\Guest\\Parameters"))
				{
					if (registryKey2 != null)
					{
						list2.Add("Присутствует раздел Hyper-V гостя (HKLM\\SOFTWARE\\Microsoft\\Virtual Machine\\Guest\\Parameters) — система работает в VM");
					}
				}
			}
			catch
			{
			}
			checkResult.Detail = ((list2.Count > 0) ? ("Признаков виртуализации/подмены BIOS: " + list2.Count) : "Физическая машина, BIOS без VM-сигнатур");
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult CheckHwidIdentity()
		{
			CheckResult checkResult = NewResult("Аппаратная часть", "HWID-идентификаторы: MachineGuid, серийный диск, том C:");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			string text = "";
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Cryptography"))
				{
					text = ((registryKey == null) ? "" : (registryKey.GetValue("MachineGuid") ?? "").ToString());
				}
			}
			catch
			{
			}
			list.Add("MachineGuid: " + (string.IsNullOrEmpty(text) ? "(не читается — нужны права)" : text));
			if (!string.IsNullOrEmpty(text))
			{
				string text2 = text.Trim();
				if (text2.Trim('{', '}', '-', '0').Length == 0)
				{
					list2.Add("MachineGuid обнулён (" + text2 + ") — идентификатор машины подменён");
				}
				else if (!IsGuidFormat(text2))
				{
					list2.Add("MachineGuid не соответствует формату GUID: " + text2);
				}
			}
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher = new ManagementObjectSearcher("SELECT SerialNumber, Model, Size, InterfaceType FROM Win32_DiskDrive"))
				{
					int num = 0;
					foreach (ManagementObject item in managementObjectSearcher.Get())
					{
						num++;
						string text3 = Str(item, "SerialNumber");
						string text4 = Str(item, "Model");
						long num2 = Long(item, "Size");
						list.Add("Диск " + num + ": " + text4 + " | серийный: " + (string.IsNullOrEmpty(text3) ? "(пусто)" : text3.Trim()) + " | " + num2 / 1073741824 + " ГБ");
						if (string.IsNullOrWhiteSpace(text3))
						{
							list2.Add("У диска \"" + text4 + "\" пустой серийный номер — типично для VM или сброса/подмены HWID");
						}
					}
					if (num == 0)
					{
						list2.Add("Ни одного физического диска не перечислено в Win32_DiskDrive");
					}
				}
			}
			catch
			{
			}
			try
			{
				using (ManagementObjectSearcher managementObjectSearcher2 = new ManagementObjectSearcher("SELECT DeviceID, VolumeSerialNumber, VolumeName, FileSystem FROM Win32_LogicalDisk WHERE DeviceID='C:'"))
				{
					foreach (ManagementObject item2 in managementObjectSearcher2.Get())
					{
						string text5 = Str(item2, "VolumeSerialNumber");
						string text6 = Str(item2, "VolumeName");
						list.Add("Том C: серийный номер тома = " + text5 + " | метка: \"" + text6 + "\" | ФС: " + Str(item2, "FileSystem"));
						if (string.IsNullOrWhiteSpace(text5) || text5 == "00000000" || text5 == "0000-0000")
						{
							list2.Add("Серийный номер тома C: пуст/обнулён — том пересоздан или подменён перед проверкой");
						}
					}
				}
			}
			catch
			{
			}
			list.Add("Имя компьютера: " + Environment.MachineName + " | пользователь: " + Environment.UserName);
			checkResult.Detail = ((list2.Count > 0) ? ("Несоответствий в идентификаторах: " + list2.Count) : "Идентификаторы машины в норме");
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult CheckMacSpoofing()
		{
			CheckResult checkResult = NewResult("Аппаратная часть", "Сетевые адаптеры: подмена MAC и виртуальные интерфейсы");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
			foreach (NetworkInterface networkInterface in allNetworkInterfaces)
			{
				if (networkInterface.OperationalStatus != OperationalStatus.Up && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Ethernet && networkInterface.NetworkInterfaceType != NetworkInterfaceType.Wireless80211)
				{
					continue;
				}
				string text = "";
				try
				{
					text = networkInterface.GetPhysicalAddress().ToString();
				}
				catch
				{
				}
				if (text.Length >= 12)
				{
					text = text.Substring(0, 2) + ":" + text.Substring(2, 2) + ":" + text.Substring(4, 2) + ":" + text.Substring(6, 2) + ":" + text.Substring(8, 2) + ":" + text.Substring(10, 2);
				}
				list.Add(string.Concat(networkInterface.Name, " [", networkInterface.NetworkInterfaceType, "] MAC: ", string.IsNullOrEmpty(text) ? "(нет)" : text));
				if (string.IsNullOrEmpty(text) || text.Trim('0', ':').Length == 0)
				{
					if (networkInterface.OperationalStatus == OperationalStatus.Up)
					{
						list2.Add("У активного адаптера \"" + networkInterface.Name + "\" MAC обнулён — сетевой идентификатор подменён");
					}
					continue;
				}
				string text2 = text.Substring(0, 8).ToLowerInvariant();
				string[] vmMacPrefixes = VmMacPrefixes;
				foreach (string text3 in vmMacPrefixes)
				{
					if (text2 == text3.ToLowerInvariant())
					{
						list2.Add("MAC \"" + text + "\" имеет OUI виртуальной среды (" + text3 + ") — адаптер \"" + networkInterface.Name + "\"");
						break;
					}
				}
			}
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Control\\Class\\{4d36e972-e325-11ce-bfc1-08002be10318}"))
				{
					if (registryKey != null)
					{
						string[] subKeyNames = registryKey.GetSubKeyNames();
						foreach (string text4 in subKeyNames)
						{
							try
							{
								using (RegistryKey registryKey2 = registryKey.OpenSubKey(text4))
								{
									object obj2 = ((registryKey2 == null) ? null : registryKey2.GetValue("NetworkAddress"));
									if (obj2 != null)
									{
										string text5 = obj2.ToString().Trim();
										if (text5.Length != 0)
										{
											list2.Add("В драйвере сетевого адаптера задан принудительный MAC (NetworkAddress=" + text5 + ", ключ HKLM\\...\\Class\\{4d36e972...}\\" + text4 + ") — это программная подмена MAC-адреса");
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
			checkResult.Detail = ((list2.Count > 0) ? ("Признаков подмены MAC: " + list2.Count) : "MAC-адреса адаптеров без аномалий");
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult CheckHidMacroDevices()
		{
			CheckResult checkResult = NewResult("Аппаратная часть", "HID/USB-устройства: макрос-кликеры на микроконтроллерах");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			CollectDeviceNames("SYSTEM\\CurrentControlSet\\Enum\\HID", list);
			CollectDeviceNames("SYSTEM\\CurrentControlSet\\Enum\\USB", list);
			CollectDeviceNames("SYSTEM\\CurrentControlSet\\Enum\\BTHENUM", list);
			CollectDeviceNames("SYSTEM\\CurrentControlSet\\Enum\\FTDIBUS", list);
			CollectDeviceNames("SYSTEM\\CurrentControlSet\\Enum\\SWD", list);
			foreach (string item in list)
			{
				string text = item.ToLowerInvariant();
				string[] macroHidSignatures = MacroHidSignatures;
				foreach (string text2 in macroHidSignatures)
				{
					if (text.IndexOf(text2, StringComparison.Ordinal) >= 0)
					{
						list2.Add("🚨 [5.01 - Аппаратный макрос/кликер на контроллере]: [" + text2 + "] " + item);
						break;
					}
				}
			}
			List<string> list3 = new List<string>();
			list3.Add("Просмотрено записей устройств: " + list.Count);
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено HID/USB-устройств с сигнатурами макрокликеров: " + list2.Count + " — спроси игрока, что это за устройство") : "Макрос-устройств на микроконтроллерах не обнаружено");
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Evidence = Join(list3, list2);
			return checkResult;
		}

		private static void CollectDeviceNames(string regPath, List<string> output)
		{
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey(regPath))
				{
					if (registryKey == null)
					{
						return;
					}
					string[] subKeyNames = registryKey.GetSubKeyNames();
					foreach (string text in subKeyNames)
					{
						try
						{
							using (RegistryKey registryKey2 = registryKey.OpenSubKey(text))
							{
								if (registryKey2 == null)
								{
									continue;
								}
								string[] subKeyNames2 = registryKey2.GetSubKeyNames();
								foreach (string text2 in subKeyNames2)
								{
									try
									{
										using (RegistryKey registryKey3 = registryKey2.OpenSubKey(text2))
										{
											if (registryKey3 == null)
											{
												continue;
											}
											string text3 = regPath.Split('\\')[3] + ": " + text + " & " + text2;
											string[] subKeyNames3 = registryKey3.GetSubKeyNames();
											foreach (string name in subKeyNames3)
											{
												try
												{
													using (RegistryKey registryKey4 = registryKey3.OpenSubKey(name))
													{
														if (registryKey4 == null)
														{
															continue;
														}
														object value = registryKey4.GetValue("FriendlyName");
														object value2 = registryKey4.GetValue("DeviceDesc");
														if (value != null)
														{
															output.Add(text3 + " | FriendlyName: " + value);
														}
														if (value2 != null)
														{
															output.Add(text3 + " | DeviceDesc: " + value2);
														}
														goto IL_0133;
													}
												}
												catch
												{
													goto IL_0133;
												}
												IL_0133:
												if (output.Count > 6000)
												{
													return;
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

		private static CheckResult CheckSpooferServices()
		{
			CheckResult checkResult = NewResult("Аппаратная часть", "Службы Windows: HWID-спуферы и перехват устройств");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			try
			{
				using (RegistryKey registryKey = Registry.LocalMachine.OpenSubKey("SYSTEM\\CurrentControlSet\\Services"))
				{
					if (registryKey != null)
					{
						string[] subKeyNames = registryKey.GetSubKeyNames();
						foreach (string text in subKeyNames)
						{
							string text2 = text.ToLowerInvariant();
							string[] spooferServiceSignatures = SpooferServiceSignatures;
							foreach (string text3 in spooferServiceSignatures)
							{
								if (text2.IndexOf(text3, StringComparison.Ordinal) < 0)
								{
									continue;
								}
								string text4 = "";
								string text5 = "?";
								try
								{
									using (RegistryKey registryKey2 = registryKey.OpenSubKey(text))
									{
										if (registryKey2 != null)
										{
											text4 = (registryKey2.GetValue("ImagePath") ?? "").ToString();
											object value = registryKey2.GetValue("Start");
											text5 = ((value == null) ? "?" : Convert.ToInt32(value).ToString());
										}
									}
								}
								catch
								{
								}
								list2.Add("🚨 [5.08 - Служба HWID-спуфера]: [" + text3 + "] служба " + text + " | Start=" + text5 + " | " + text4);
								break;
							}
						}
						list.Add("Просмотрено служб: " + registryKey.GetSubKeyNames().Length);
					}
				}
			}
			catch (Exception ex)
			{
				list.Add("Службы не читается: " + ex.Message);
			}
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено служб со сигнатурами спуферов: " + list2.Count) : "Служб-спуферов не найдено");
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult CheckSpooferFolders()
		{
			CheckResult checkResult = NewResult("Аппаратная часть", "HWID-спуферы и лоадеры в папках пользователя");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			string[] array = new string[5]
			{
				Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
				Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads"),
				Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop")
			};
			string[] array2 = new string[19]
			{
				"hwid spoofer.exe", "hwidspoofer.exe", "spoofer.exe", "spoofer.dll", "deleter.exe", "hwid.exe", "hwidlock.exe", "diskid.exe", "serial changer.exe", "macchanger.exe",
				"volumeid.exe", "volume id.exe", "kmbox.exe", "makcu.exe", "cuda.exe", "spoofer.bat", "spoofer.cmd", "hwid_spoofer.exe", "cleaner hwid.exe"
			};
			string[] array3 = array;
			foreach (string text in array3)
			{
				if (string.IsNullOrEmpty(text) || !Directory.Exists(text))
				{
					continue;
				}
				try
				{
					string[] directories = Directory.GetDirectories(text, "*", SearchOption.TopDirectoryOnly);
					foreach (string text2 in directories)
					{
						string text3 = Path.GetFileName(text2).ToLowerInvariant();
						string[] spooferFolderSignatures = SpooferFolderSignatures;
						foreach (string text4 in spooferFolderSignatures)
						{
							if (!(text3 != text4) || text3.StartsWith(text4 + " ") || text3.StartsWith(text4 + "-") || text3.StartsWith(text4 + "_"))
							{
								list2.Add("\ud83d\udce6 Папка HWID-спуфера [" + text4 + "]: " + text2);
								break;
							}
						}
					}
				}
				catch
				{
				}
				try
				{
					string[] files = Directory.GetFiles(text, "*.exe", SearchOption.TopDirectoryOnly);
					foreach (string text5 in files)
					{
						string text6 = Path.GetFileName(text5).ToLowerInvariant();
						string[] array4 = array2;
						foreach (string text7 in array4)
						{
							if (!(text6 != text7))
							{
								list2.Add("🚨 [5.08 - Файл HWID-спуфера]: [" + text7 + "] " + text5 + " (изменён: " + File.GetLastWriteTime(text5).ToString("dd.MM.yyyy HH:mm") + ")");
								break;
							}
						}
					}
				}
				catch
				{
				}
			}
			list.Add("Проверено корней: " + array.Length);
			checkResult.Detail = ((list2.Count > 0) ? ("Найдено объектов спуферов: " + list2.Count) : "HWID-спуферов в папках пользователя не найдено");
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Critical : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult NewResult(string category, string name)
		{
			CheckResult checkResult = new CheckResult();
			checkResult.Category = category;
			checkResult.Name = name;
			checkResult.Detail = "";
			checkResult.Evidence = "";
			checkResult.Detected = false;
			checkResult.Severity = CheckSeverity.Info;
			return checkResult;
		}

		private static string Join(List<string> info, List<string> hits)
		{
			StringBuilder stringBuilder = new StringBuilder();
			if (hits != null && hits.Count > 0)
			{
				stringBuilder.AppendLine("=== НАЙДЕНО ===");
				for (int i = 0; i < hits.Count && i < 200; i++)
				{
					stringBuilder.AppendLine(hits[i]);
				}
				stringBuilder.AppendLine();
			}
			if (info != null && info.Count > 0)
			{
				stringBuilder.AppendLine("=== СВЕДЕНИЯ ОБ ОБОРУДОВАНИИ ===");
				for (int j = 0; j < info.Count && j < 200; j++)
				{
					stringBuilder.AppendLine(info[j]);
				}
			}
			return stringBuilder.ToString();
		}

		private static string Str(ManagementObject o, string prop)
		{
			try
			{
				object obj = o[prop];
				return (obj == null) ? "" : obj.ToString();
			}
			catch
			{
				return "";
			}
		}

		private static int Int(ManagementObject o, string prop)
		{
			try
			{
				object obj = o[prop];
				if (obj == null)
				{
					return -1;
				}
				return Convert.ToInt32(obj);
			}
			catch
			{
				return -1;
			}
		}

		private static long Long(ManagementObject o, string prop)
		{
			try
			{
				object obj = o[prop];
				if (obj == null)
				{
					return 0L;
				}
				return Convert.ToInt64(obj);
			}
			catch
			{
				return 0L;
			}
		}

		private static bool IsGuidFormat(string s)
		{
			if (string.IsNullOrEmpty(s))
			{
				return false;
			}
			string text = s.Trim('{', '}');
			if (text.Length != 36)
			{
				return false;
			}
			for (int i = 0; i < text.Length; i++)
			{
				char c = text[i];
				bool flag = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
				if (i == 8 || i == 13 || i == 18 || i == 23)
				{
					if (c != '-')
					{
						return false;
					}
				}
				else if (!flag)
				{
					return false;
				}
			}
			return true;
		}

		private static CheckResult CheckUnixCpuAndRam()
		{
			CheckResult checkResult = NewResult("Аппаратная часть", "CPU и RAM (" + PlatformHelper.OSDescription + ")");
			List<string> list = new List<string>();
			List<string> hits = new List<string>();
			if (PlatformHelper.IsLinux)
			{
				try
				{
					if (File.Exists("/proc/cpuinfo"))
					{
						string[] array = File.ReadAllLines("/proc/cpuinfo");
						string text = "";
						int num = 0;
						string[] array2 = array;
						foreach (string text2 in array2)
						{
							if (text2.StartsWith("model name") && string.IsNullOrEmpty(text))
							{
								int num2 = text2.IndexOf(':');
								if (num2 >= 0)
								{
									text = text2.Substring(num2 + 1).Trim();
								}
							}
							if (text2.StartsWith("processor"))
							{
								num++;
							}
						}
						list.Add("Linux CPU: " + (string.IsNullOrEmpty(text) ? "Standard" : text) + " | логических ядер: " + num);
					}
					if (File.Exists("/proc/meminfo"))
					{
						string[] array3 = File.ReadAllLines("/proc/meminfo");
						foreach (string text3 in array3)
						{
							if (text3.StartsWith("MemTotal:"))
							{
								list.Add("Linux RAM: " + text3.Trim());
								break;
							}
						}
					}
				}
				catch (Exception ex)
				{
					list.Add("Ошибка чтения /proc: " + ex.Message);
				}
			}
			else if (PlatformHelper.IsMacOS)
			{
				list.Add("macOS Архитектура: " + (Environment.Is64BitOperatingSystem ? "64-bit" : "32-bit") + " | Ядер: " + Environment.ProcessorCount);
			}
			checkResult.Detail = "Аппаратная конфигурация " + PlatformHelper.OSDescription + " получена";
			checkResult.Detected = false;
			checkResult.Severity = CheckSeverity.Info;
			checkResult.Evidence = Join(list, hits);
			return checkResult;
		}

		private static CheckResult CheckUnixGpuAndMachine()
		{
			CheckResult checkResult = NewResult("Аппаратная часть", "Платформа и виртуализация (" + PlatformHelper.OSDescription + ")");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			if (PlatformHelper.IsLinux)
			{
				try
				{
					string text = "";
					string text2 = "";
					if (File.Exists("/sys/class/dmi/id/sys_vendor"))
					{
						text = File.ReadAllText("/sys/class/dmi/id/sys_vendor").Trim();
					}
					if (File.Exists("/sys/class/dmi/id/product_name"))
					{
						text2 = File.ReadAllText("/sys/class/dmi/id/product_name").Trim();
					}
					list.Add("DMI Вендор: " + text + " | Модель: " + text2);
					string text3 = (text + " " + text2).ToLowerInvariant();
					string[] vmBiosSignatures = VmBiosSignatures;
					foreach (string text4 in vmBiosSignatures)
					{
						if (text3.Contains(text4))
						{
							list2.Add("Обнаружена виртуальная машина: " + text4 + " (DMI: " + text3 + ")");
							break;
						}
					}
					string[] vmModelSignatures = VmModelSignatures;
					foreach (string text5 in vmModelSignatures)
					{
						if (text3.Contains(text5))
						{
							list2.Add("Обнаружен гипервизор: " + text5 + " (DMI: " + text3 + ")");
							break;
						}
					}
				}
				catch
				{
				}
			}
			else if (PlatformHelper.IsMacOS)
			{
				list.Add("Apple Mac Hardware Platform (" + Environment.OSVersion.VersionString + ")");
			}
			checkResult.Detail = ((list2.Count > 0) ? ("Обнаружены признаки гипервизора: " + list2.Count) : ("Конфигурация платформы (" + PlatformHelper.OSDescription + ") штатная"));
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult CheckUnixMachineIdAndMac()
		{
			CheckResult checkResult = NewResult("Идентификаторы и сеть", "Machine-ID и сетевые интерфейсы (" + PlatformHelper.OSDescription + ")");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			if (PlatformHelper.IsLinux)
			{
				try
				{
					string text = "";
					if (File.Exists("/etc/machine-id"))
					{
						text = File.ReadAllText("/etc/machine-id").Trim();
					}
					else if (File.Exists("/var/lib/dbus/machine-id"))
					{
						text = File.ReadAllText("/var/lib/dbus/machine-id").Trim();
					}
					if (!string.IsNullOrEmpty(text))
					{
						list.Add("Linux Machine-ID: " + text);
						if (text == "00000000000000000000000000000000")
						{
							list2.Add("Обнулённый Machine-ID — признак спуфинга HWID");
						}
					}
				}
				catch
				{
				}
			}
			try
			{
				NetworkInterface[] allNetworkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
				foreach (NetworkInterface networkInterface in allNetworkInterfaces)
				{
					if (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Loopback)
					{
						continue;
					}
					string text2 = networkInterface.GetPhysicalAddress().ToString();
					if (string.IsNullOrEmpty(text2) || text2.Length < 6)
					{
						continue;
					}
					string text3 = string.Format("{0}:{1}:{2}:{3}:{4}:{5}", text2.Substring(0, 2), text2.Substring(2, 2), text2.Substring(4, 2), text2.Substring(6, 2), text2.Substring(8, 2), text2.Substring(10, 2)).ToLowerInvariant();
					list.Add("Интерфейс: " + networkInterface.Name + " | MAC: " + text3);
					string[] vmMacPrefixes = VmMacPrefixes;
					foreach (string text4 in vmMacPrefixes)
					{
						if (text3.StartsWith(text4.ToLowerInvariant()))
						{
							list2.Add("MAC-адрес (" + text3 + ") принадлежит гипервизору (префикс " + text4 + ")");
							break;
						}
					}
				}
			}
			catch
			{
			}
			checkResult.Detail = ((list2.Count > 0) ? ("Обнаружены аномалии сети/HWID: " + list2.Count) : ("Идентификаторы и сетевые адаптеры проверены (" + list.Count + " интерфейсов)"));
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}

		private static CheckResult CheckUnixHidDevices()
		{
			CheckResult checkResult = NewResult("Аппаратные макросы", "HID-устройства ввода и контроллеры (" + PlatformHelper.OSDescription + ")");
			List<string> list = new List<string>();
			List<string> list2 = new List<string>();
			if (PlatformHelper.IsLinux)
			{
				try
				{
					if (File.Exists("/proc/bus/input/devices"))
					{
						string text = File.ReadAllText("/proc/bus/input/devices");
						string[] array = text.Split('\n');
						foreach (string text2 in array)
						{
							if (!text2.StartsWith("N: Name="))
							{
								continue;
							}
							string text3 = text2.Substring(8).Trim('"', ' ', '\r');
							list.Add("Ввод: " + text3);
							string text4 = text3.ToLowerInvariant();
							string[] macroHidSignatures = MacroHidSignatures;
							foreach (string text5 in macroHidSignatures)
							{
								if (text4.Contains(text5))
								{
									list2.Add("Обнаружено подозрительное HID/макрос-устройство: \"" + text3 + "\" (сигнатура: " + text5 + ")");
									break;
								}
							}
						}
					}
				}
				catch
				{
				}
			}
			checkResult.Detail = ((list2.Count > 0) ? ("Обнаружены аппаратные макрос-устройства: " + list2.Count) : "Аппаратных макрос-контроллеров (KMBox, Arduino, Pico) не обнаружено");
			checkResult.Detected = list2.Count > 0;
			checkResult.Severity = ((list2.Count > 0) ? CheckSeverity.Warning : CheckSeverity.Info);
			checkResult.Evidence = Join(list, list2);
			return checkResult;
		}
	}
}
