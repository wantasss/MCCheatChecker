using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

namespace MCCheatChecker
{
    class ExtractAllUS
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            string exePath = @"C:\Users\Wanta\.gemini\antigravity\brain\ba212735-9fe6-4a63-b2b2-b97a96a77665\.user_uploaded\media_1794836306007_3327441d.exe";
            byte[] raw = File.ReadAllBytes(exePath);

            int bsjbOffset = 1573976;
            int usOffset = bsjbOffset + 248856;
            int usSize = 1841216;

            int ptr = usOffset + 1; // 0th byte is 0
            int end = usOffset + usSize;
            List<string> userStrings = new List<string>();

            while (ptr < end)
            {
                byte b0 = raw[ptr++];
                int strLen = 0;
                if ((b0 & 0x80) == 0)
                {
                    strLen = b0;
                }
                else if ((b0 & 0xC0) == 0x80)
                {
                    strLen = ((b0 & 0x3F) << 8) | raw[ptr++];
                }
                else if ((b0 & 0xE0) == 0xC0)
                {
                    strLen = ((b0 & 0x1F) << 24) | (raw[ptr++] << 16) | (raw[ptr++] << 8) | raw[ptr++];
                }

                if (strLen <= 0) continue;
                if (ptr + strLen > end) break;

                int charBytes = strLen - 1;
                if (charBytes > 0)
                {
                    string str = Encoding.Unicode.GetString(raw, ptr, charBytes);
                    userStrings.Add(str);
                }
                ptr += strLen;
            }

            Console.WriteLine(string.Format("Extracted {0} UserStrings!", userStrings.Count));
            File.WriteAllLines(@"C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\extracted_user_strings.txt", userStrings, Encoding.UTF8);
            Console.WriteLine("Saved to extracted_user_strings.txt");
        }
    }
}
