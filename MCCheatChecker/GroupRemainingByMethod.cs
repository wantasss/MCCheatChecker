using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

class GroupRemainingByMethod
{
    static void Main()
    {
        string[] lines = File.ReadAllLines(@"C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\CheatDetector.cs", Encoding.UTF8);
        string currentMethod = "TOP_LEVEL";
        var methodLines = new Dictionary<string, List<Tuple<int, string>>>();

        for (int i = 0; i < lines.Length; i++)
        {
            string l = lines[i].Trim();
            if (l.StartsWith("public static") || l.StartsWith("private static") || l.StartsWith("public ") || l.StartsWith("private "))
            {
                if (l.Contains("(") && !l.EndsWith(";"))
                {
                    currentMethod = l;
                }
            }

            if (lines[i].IndexOf('\uFFFD') >= 0)
            {
                if (!methodLines.ContainsKey(currentMethod))
                {
                    methodLines[currentMethod] = new List<Tuple<int, string>>();
                }
                methodLines[currentMethod].Add(Tuple.Create(i + 1, lines[i].Trim()));
            }
        }

        using (var sw = new StreamWriter(@"C:\Users\Wanta\Desktop\MCCheatChecker\MCCheatChecker\method_fffd_grouped.txt", false, Encoding.UTF8))
        {
            foreach (var kvp in methodLines)
            {
                sw.WriteLine("=== METHOD: {0} (count: {1}) ===", kvp.Key, kvp.Value.Count);
                foreach (var item in kvp.Value)
                {
                    sw.WriteLine("  L{0}: {1}", item.Item1, item.Item2);
                }
                sw.WriteLine();
            }
        }

        Console.WriteLine("Grouped into {0} methods. Written to method_fffd_grouped.txt", methodLines.Count);
    }
}
