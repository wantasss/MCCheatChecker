namespace MCCheatChecker
{
	public class SystemRamInfo
	{
		public ulong TotalBytes;

		public ulong AvailableBytes;

		public ulong UsedBytes;

		public uint MemoryLoad;

		public double TotalGb
		{
			get
			{
				return (double)TotalBytes / 1024.0 / 1024.0 / 1024.0;
			}
		}

		public double AvailableGb
		{
			get
			{
				return (double)AvailableBytes / 1024.0 / 1024.0 / 1024.0;
			}
		}

		public double UsedGb
		{
			get
			{
				return (double)UsedBytes / 1024.0 / 1024.0 / 1024.0;
			}
		}

		public override string ToString()
		{
			return string.Format("{0:0.0} ГБ / {1:0.0} ГБ ({2}% загрузка)", UsedGb, TotalGb, MemoryLoad);
		}
	}
}
