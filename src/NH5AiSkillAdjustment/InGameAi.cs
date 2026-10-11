using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace NH5AiSkillAdjustment
{
    internal static class InGameAi
    {
        const string Key = "hLEs8z7p45nHRqnzh4Bbxbeiwib0iubH8pldqn9uj0FbrreF8juh04Ph1n0f243nOIhf";

        public static bool TryRead(out int value)
        {
            value = AiSkillPatcher.VanillaMax;
            var path = SavePath();
            if (!File.Exists(path))
            {
                return false;
            }

            foreach (var raw in Values(ReadText(path)))
            {
                if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var setting)
                    && setting >= AiSkillPatcher.VanillaMin
                    && setting <= AiSkillPatcher.VanillaMax)
                {
                    value = setting;
                    return true;
                }
            }

            return false;
        }

        public static void Write(int value)
        {
            if (value < AiSkillPatcher.VanillaMin || value > AiSkillPatcher.VanillaMax)
            {
                throw new InvalidOperationException("In-game AI difficulty must be " + AiSkillPatcher.VanillaMin + "-" + AiSkillPatcher.VanillaMax + ".");
            }

            var path = SavePath();
            if (!File.Exists(path))
            {
                throw new InvalidOperationException("Could not find the Heat settings save.");
            }

            var json = ReadText(path);
            var spots = Spots(json);
            if (spots.Count == 0)
            {
                throw new InvalidOperationException("The Heat settings save has no AI difficulty.");
            }

            for (var i = spots.Count - 1; i >= 0; i--)
            {
                var spot = spots[i];
                json = json.Substring(0, spot.Start) + value.ToString(CultureInfo.InvariantCulture) + json.Substring(spot.Start + spot.Length);
            }

            WriteText(path, Stamp(json));
        }

        static string SavePath()
        {
            var profile = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
            return Path.Combine(profile, "NASCAR Heat 5", "SaveData", "savedata0_ag.dat");
        }

        static string ReadText(string path)
        {
            var bytes = File.ReadAllBytes(path);
            Xor(bytes);
            return Encoding.UTF8.GetString(bytes);
        }

        static void WriteText(string path, string text)
        {
            var backup = path + ".workshop.bak";
            if (!File.Exists(backup))
            {
                File.Copy(path, backup);
            }

            var bytes = Encoding.UTF8.GetBytes(text);
            Xor(bytes);
            File.WriteAllBytes(path, bytes);
        }

        static List<string> Values(string json)
        {
            var values = new List<string>();
            foreach (var spot in Spots(json))
            {
                values.Add(json.Substring(spot.Start, spot.Length));
            }

            return values;
        }

        static List<Spot> Spots(string json)
        {
            var found = new List<Spot>();
            const string needle = "\"custom_ai\"";
            var index = 0;
            while (index < json.Length)
            {
                var at = json.IndexOf(needle, index, StringComparison.Ordinal);
                if (at < 0)
                {
                    break;
                }

                var cursor = at + needle.Length;
                while (cursor < json.Length && char.IsWhiteSpace(json[cursor]))
                {
                    cursor++;
                }

                if (cursor >= json.Length || json[cursor] != ':')
                {
                    index = at + needle.Length;
                    continue;
                }

                cursor++;
                while (cursor < json.Length && char.IsWhiteSpace(json[cursor]))
                {
                    cursor++;
                }

                var start = cursor;
                while (cursor < json.Length && !char.IsWhiteSpace(json[cursor]) && json[cursor] != ',' && json[cursor] != '}' && json[cursor] != ']')
                {
                    cursor++;
                }

                if (cursor > start)
                {
                    found.Add(new Spot { Start = start, Length = cursor - start });
                }

                index = cursor;
            }

            return found;
        }

        static string Stamp(string json)
        {
            var spots = ChecksumSpots(json);
            if (spots.Count == 0)
            {
                return json;
            }

            var spot = spots[0];
            var zeroed = json.Substring(0, spot.Start) + "0" + json.Substring(spot.Start + spot.Length);
            var crc = Crc(Encoding.UTF8.GetBytes(zeroed)).ToString(CultureInfo.InvariantCulture);
            spots = ChecksumSpots(zeroed);
            if (spots.Count == 0)
            {
                return json;
            }

            spot = spots[0];
            return zeroed.Substring(0, spot.Start) + crc + zeroed.Substring(spot.Start + spot.Length);
        }

        static List<Spot> ChecksumSpots(string json)
        {
            var found = new List<Spot>();
            const string needle = "\"checksum\"";
            var index = 0;
            while (index < json.Length)
            {
                var at = json.IndexOf(needle, index, StringComparison.Ordinal);
                if (at < 0)
                {
                    break;
                }

                var cursor = at + needle.Length;
                while (cursor < json.Length && char.IsWhiteSpace(json[cursor]))
                {
                    cursor++;
                }

                if (cursor >= json.Length || json[cursor] != ':')
                {
                    index = at + needle.Length;
                    continue;
                }

                cursor++;
                while (cursor < json.Length && char.IsWhiteSpace(json[cursor]))
                {
                    cursor++;
                }

                var start = cursor;
                while (cursor < json.Length && char.IsDigit(json[cursor]))
                {
                    cursor++;
                }

                if (cursor > start)
                {
                    found.Add(new Spot { Start = start, Length = cursor - start });
                    return found;
                }

                index = cursor;
            }

            return found;
        }

        static void Xor(byte[] bytes)
        {
            for (var i = 0; i < bytes.Length; i++)
            {
                bytes[i] = (byte)(bytes[i] ^ Key[i % Key.Length]);
            }
        }

        static uint Crc(byte[] bytes)
        {
            var poly = 3988292384U;
            var table = new uint[256];
            for (var i = 0U; i < table.Length; i++)
            {
                var value = i;
                for (var bit = 0; bit < 8; bit++)
                {
                    value = (value & 1U) == 1U ? (value >> 1) ^ poly : value >> 1;
                }

                table[i] = value;
            }

            var crc = uint.MaxValue;
            foreach (var b in bytes)
            {
                crc = (crc >> 8) ^ table[(byte)((crc & 255U) ^ b)];
            }

            return ~crc;
        }

        struct Spot
        {
            public int Start;
            public int Length;
        }
    }
}
