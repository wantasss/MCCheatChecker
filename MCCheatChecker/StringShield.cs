using System.Text;

namespace MCCheatChecker
{
	internal static class StringShield
	{
		private static readonly byte[] Key = new byte[16]
		{
			167, 59, 242, 193, 88, 217, 110, 20, 138, 69,
			179, 125, 230, 47, 145, 204
		};

		public static string Decrypt(byte[] cipher)
		{
			byte[] array = new byte[cipher.Length];
			for (int i = 0; i < cipher.Length; i++)
			{
				array[i] = (byte)(cipher[i] ^ Key[i % Key.Length]);
			}
			return Encoding.UTF8.GetString(array);
		}

		public static byte[] Encrypt(string plaintext)
		{
			byte[] bytes = Encoding.UTF8.GetBytes(plaintext);
			byte[] array = new byte[bytes.Length];
			for (int i = 0; i < bytes.Length; i++)
			{
				array[i] = (byte)(bytes[i] ^ Key[i % Key.Length]);
			}
			return array;
		}

		public static string[] DecryptAll(byte[][] cipherArray)
		{
			string[] array = new string[cipherArray.Length];
			for (int i = 0; i < cipherArray.Length; i++)
			{
				array[i] = Decrypt(cipherArray[i]);
			}
			return array;
		}
	}
}
