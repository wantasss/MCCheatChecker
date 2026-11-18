using System;

namespace MCCheatChecker
{
	public static class AccessGate
	{
		public static bool GetIsAuthorized()
		{
			return true;
		}

		public static void SetIsAuthorized(bool auth)
		{
		}

		public static bool ShowGate()
		{
			return true;
		}
	}
}
