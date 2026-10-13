using System;
using System.IO;

namespace NH5AiSkillAdjustment
{
    internal static class GameBackup
    {
        public static string Keep(string gameFile, string suffix)
        {
            var legacy = gameFile + suffix;
            var stored = Stored(gameFile, suffix);
            if (!InGame(gameFile))
            {
                return File.Exists(legacy) || !File.Exists(stored) ? legacy : stored;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(stored) ?? stored);
            if (File.Exists(legacy) && !SamePath(legacy, stored))
            {
                if (!File.Exists(stored) || File.GetLastWriteTimeUtc(legacy) > File.GetLastWriteTimeUtc(stored))
                {
                    File.Copy(legacy, stored, true);
                }

                if (File.Exists(stored))
                {
                    try
                    {
                        File.Delete(legacy);
                    }
                    catch (IOException)
                    {
                    }
                    catch (UnauthorizedAccessException)
                    {
                    }
                }
            }

            return stored;
        }

        static string Stored(string gameFile, string suffix)
        {
            var dir = Path.GetDirectoryName(gameFile) ?? gameFile;
            var root = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "NH5 Command Center",
                "Restore",
                Slot(dir));
            return Path.Combine(root, Path.GetFileName(gameFile) + suffix);
        }

        static bool SamePath(string left, string right)
        {
            return string.Equals(Path.GetFullPath(left), Path.GetFullPath(right), StringComparison.OrdinalIgnoreCase);
        }

        static bool InGame(string path)
        {
            var parent = Directory.Exists(path) ? path : Path.GetDirectoryName(Path.GetFullPath(path));
            while (!string.IsNullOrEmpty(parent))
            {
                if (File.Exists(Path.Combine(parent, "NASCARHeat5.exe"))
                    || Directory.Exists(Path.Combine(parent, "NASCARHeat5_Data")))
                {
                    return true;
                }

                var next = Path.GetDirectoryName(parent);
                if (string.IsNullOrEmpty(next) || string.Equals(next, parent, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                parent = next;
            }

            return false;
        }

        static string Slot(string path)
        {
            var hash = 0;
            foreach (var ch in path.ToLowerInvariant())
            {
                hash = unchecked(hash * 31 + ch);
            }

            var name = Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
            if (string.IsNullOrEmpty(name))
            {
                name = "game";
            }

            foreach (var ch in Path.GetInvalidFileNameChars())
            {
                name = name.Replace(ch, '_');
            }

            return name + "-" + unchecked((uint)hash).ToString("x8");
        }
    }
}
