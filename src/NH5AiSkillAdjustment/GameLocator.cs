using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace NH5AiSkillAdjustment
{
    internal static class GameLocator
    {
        public static string? FindManagedDir()
        {
            foreach (var candidate in Candidates())
            {
                if (File.Exists(Path.Combine(candidate, "Assembly-CSharp.dll")))
                {
                    return candidate;
                }
            }

            return null;
        }

        public static string? ResolveFromBrowse(string picked)
        {
            if (string.IsNullOrWhiteSpace(picked))
            {
                return null;
            }

            if (File.Exists(Path.Combine(picked, "Assembly-CSharp.dll")))
            {
                return picked;
            }

            var nested = Path.Combine(picked, "NASCARHeat5_Data", "Managed");
            if (File.Exists(Path.Combine(nested, "Assembly-CSharp.dll")))
            {
                return nested;
            }

            var managed = Path.Combine(picked, "Managed");
            if (File.Exists(Path.Combine(managed, "Assembly-CSharp.dll")))
            {
                return managed;
            }

            return null;
        }

        public static bool HeatIsRunning()
        {
            return Process.GetProcessesByName("NASCARHeat5").Length > 0;
        }

        private static IEnumerable<string> Candidates()
        {
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var steamRoot in SteamRoots())
            {
                var direct = Path.Combine(steamRoot, "steamapps", "common", "NASCAR Heat 5", "NASCARHeat5_Data", "Managed");
                if (seen.Add(direct))
                {
                    yield return direct;
                }

                var vdf = Path.Combine(steamRoot, "steamapps", "libraryfolders.vdf");
                if (!File.Exists(vdf))
                {
                    continue;
                }

                var text = File.ReadAllText(vdf);
                foreach (Match match in Regex.Matches(text, "\"path\"\\s*\"([^\"]+)\""))
                {
                    var lib = match.Groups[1].Value.Replace(@"\\", @"\");
                    var extra = Path.Combine(lib, "steamapps", "common", "NASCAR Heat 5", "NASCARHeat5_Data", "Managed");
                    if (seen.Add(extra))
                    {
                        yield return extra;
                    }
                }
            }

            yield return @"D:\SteamLibrary\steamapps\common\NASCAR Heat 5\NASCARHeat5_Data\Managed";
            yield return @"C:\Program Files (x86)\Steam\steamapps\common\NASCAR Heat 5\NASCARHeat5_Data\Managed";
            yield return @"C:\Program Files\Steam\steamapps\common\NASCAR Heat 5\NASCARHeat5_Data\Managed";
        }

        private static IEnumerable<string> SteamRoots()
        {
            var path = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam", "SteamPath", null) as string;
            if (!string.IsNullOrWhiteSpace(path))
            {
                yield return path!.Replace('/', '\\');
            }
        }
    }
}
