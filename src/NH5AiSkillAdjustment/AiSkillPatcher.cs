using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace NH5AiSkillAdjustment
{
    internal sealed class PatchTarget
    {
        public string Kind = "";
        public int Offset;
        public byte[] Original = Array.Empty<byte>();
        public int? Current;
    }

    internal sealed class AiSkillPatcher
    {
        public const int MinStrength = 60;
        public const int MaxStrength = 200;
        public const int VanillaCustom = 105;

        private static readonly byte[] RatingNeedle =
        {
            0x1F, 0x55, 0x02, 0x7B, 0x00, 0x00, 0x00, 0x04, 0x28, 0x00, 0x00, 0x00, 0x06, 0x58, 0x2A
        };

        private static readonly bool[] RatingWild =
        {
            false, false, false, false, true, true, false, false, false, true, true, false, false, false, false
        };

        private readonly string _dllPath;
        private readonly string _statePath;

        public AiSkillPatcher(string dllPath)
        {
            _dllPath = dllPath;
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NH5AiSkillAdjustment");
            Directory.CreateDirectory(dir);
            _statePath = Path.Combine(dir, "state.json");
        }

        public string DllPath => _dllPath;

        public PatchTarget Resolve()
        {
            var bytes = File.ReadAllBytes(_dllPath);
            return Resolve(bytes);
        }

        public int Apply(int value)
        {
            if (GameLocator.HeatIsRunning())
            {
                throw new InvalidOperationException("NASCAR Heat 5 is running. Close the game, then try again.");
            }

            if (value < MinStrength || value > MaxStrength)
            {
                throw new InvalidOperationException($"Strength must be {MinStrength}-{MaxStrength}.");
            }

            if (!File.Exists(_dllPath))
            {
                throw new InvalidOperationException("Could not find Assembly-CSharp.dll. Use Locate game.");
            }

            var bytes = File.ReadAllBytes(_dllPath);
            var target = Resolve(bytes);
            var patch = NewForcedRatingBytes(value);
            Array.Copy(patch, 0, bytes, target.Offset, 15);
            File.WriteAllBytes(_dllPath, bytes);
            SaveState(target.Offset, ToHex(target.Original), value);
            return value;
        }

        public string Restore()
        {
            if (GameLocator.HeatIsRunning())
            {
                throw new InvalidOperationException("NASCAR Heat 5 is running. Close the game, then try again.");
            }

            if (!File.Exists(_dllPath))
            {
                throw new InvalidOperationException("Could not find Assembly-CSharp.dll. Use Locate game.");
            }

            var bytes = File.ReadAllBytes(_dllPath);
            var target = Resolve(bytes);
            if (target.Kind == "stock")
            {
                return "already-stock";
            }

            if (target.Original == null || target.Original.Length != 15)
            {
                throw new InvalidOperationException("Could not recover the original AI skill code to restore.");
            }

            Array.Copy(target.Original, 0, bytes, target.Offset, 15);
            File.WriteAllBytes(_dllPath, bytes);
            if (File.Exists(_statePath))
            {
                File.Delete(_statePath);
            }

            return "restored";
        }

        private PatchTarget Resolve(byte[] bytes)
        {
            var hits = AllMask(bytes, RatingNeedle, RatingWild);
            var state = ReadState();

            if (hits.Count == 2)
            {
                var off = hits[1];
                var orig = new byte[15];
                Array.Copy(bytes, off, orig, 0, 15);
                return new PatchTarget { Kind = "stock", Offset = off, Original = orig };
            }

            if (hits.Count == 1)
            {
                var baseOff = hits[0];
                var effOff = baseOff + 16;
                var current = GetForcedRatingAt(bytes, effOff);
                if (current.HasValue)
                {
                    var orig = state != null && state.Offset == effOff ? FromHex(state.OriginalHex) : OriginalFromBase(bytes, baseOff);
                    return new PatchTarget { Kind = "patched", Offset = effOff, Original = orig, Current = current };
                }
            }

            if (state != null)
            {
                var current = GetForcedRatingAt(bytes, state.Offset);
                if (current.HasValue)
                {
                    return new PatchTarget
                    {
                        Kind = "patched",
                        Offset = state.Offset,
                        Original = FromHex(state.OriginalHex),
                        Current = current
                    };
                }

                if (state.Offset >= 0 && state.Offset + 15 <= bytes.Length)
                {
                    var slice = new byte[15];
                    Array.Copy(bytes, state.Offset, slice, 0, 15);
                    if (IndexOfMask(slice, RatingNeedle, RatingWild, 0) == 0)
                    {
                        return new PatchTarget { Kind = "stock", Offset = state.Offset, Original = slice };
                    }
                }
            }

            throw new InvalidOperationException("Could not find Heat 5 AI skill code in this DLL. Locate NASCARHeat5_Data\\Managed.");
        }

        // Unity 2017 tiny methods: keep short-form opcodes only.
        // ldc.i4 (4-byte operand) made GetEffectiveRating invalid (InvalidProgramException on race load).
        // Return 85 + sbyte(delta) so 60-200 fits in the original 15-byte body.
        private static byte[] NewForcedRatingBytes(int value)
        {
            var delta = value - 85;
            var patch = new byte[15];
            patch[0] = 0x1F;
            patch[1] = 85;
            patch[2] = 0x1F;
            patch[3] = unchecked((byte)(sbyte)delta);
            patch[4] = 0x58;
            patch[5] = 0x2A;
            return patch;
        }

        private static int? GetForcedRatingAt(byte[] bytes, int off)
        {
            if (off < 0 || off + 15 > bytes.Length)
            {
                return null;
            }

            if (bytes[off] == 0x1F && bytes[off + 1] == 85 && bytes[off + 2] == 0x1F && bytes[off + 4] == 0x58 && bytes[off + 5] == 0x2A)
            {
                for (var i = 6; i < 15; i++)
                {
                    if (bytes[off + i] != 0)
                    {
                        return null;
                    }
                }

                var n = 85 + (sbyte)bytes[off + 3];
                if (n >= MinStrength && n <= MaxStrength)
                {
                    return n;
                }

                return null;
            }

            if (bytes[off] == 0x20 && bytes[off + 5] == 0x2A)
            {
                for (var i = 6; i < 15; i++)
                {
                    if (bytes[off + i] != 0)
                    {
                        return null;
                    }
                }

                var n = BitConverter.ToInt32(bytes, off + 1);
                if (n >= MinStrength && n <= MaxStrength)
                {
                    return n;
                }

                return null;
            }

            if (bytes[off] == 0x1F && bytes[off + 2] == 0x2A)
            {
                for (var i = 3; i < 15; i++)
                {
                    if (bytes[off + i] != 0)
                    {
                        return null;
                    }
                }

                var n = bytes[off + 1];
                if (n >= MinStrength && n <= 127)
                {
                    return n;
                }
            }

            return null;
        }

        private static byte[] OriginalFromBase(byte[] bytes, int baseOff)
        {
            var orig = new byte[15];
            Array.Copy(bytes, baseOff, orig, 0, 15);
            var tok = BitConverter.ToUInt16(orig, 4);
            tok++;
            orig[4] = (byte)(tok & 0xFF);
            orig[5] = (byte)((tok >> 8) & 0xFF);
            return orig;
        }

        private static List<int> AllMask(byte[] hay, byte[] needle, bool[] wild)
        {
            var list = new List<int>();
            var p = 0;
            int i;
            while ((i = IndexOfMask(hay, needle, wild, p)) >= 0)
            {
                list.Add(i);
                p = i + 1;
            }

            return list;
        }

        private static int IndexOfMask(byte[] hay, byte[] needle, bool[] wild, int start)
        {
            var lim = hay.Length - needle.Length;
            for (var i = start; i <= lim; i++)
            {
                var j = 0;
                for (; j < needle.Length; j++)
                {
                    if (!wild[j] && hay[i + j] != needle[j])
                    {
                        break;
                    }
                }

                if (j == needle.Length)
                {
                    return i;
                }
            }

            return -1;
        }

        private sealed class State
        {
            public int Offset;
            public string OriginalHex = "";
        }

        private State? ReadState()
        {
            if (!File.Exists(_statePath))
            {
                return null;
            }

            try
            {
                var text = File.ReadAllText(_statePath);
                var off = Regex.Match(text, "\"offset\"\\s*:\\s*(-?\\d+)");
                var hex = Regex.Match(text, "\"originalHex\"\\s*:\\s*\"([^\"]+)\"");
                if (!off.Success || !hex.Success)
                {
                    return null;
                }

                return new State
                {
                    Offset = int.Parse(off.Groups[1].Value, CultureInfo.InvariantCulture),
                    OriginalHex = hex.Groups[1].Value
                };
            }
            catch
            {
                return null;
            }
        }

        private void SaveState(int offset, string originalHex, int strength)
        {
            var json = "{\"offset\":" + offset.ToString(CultureInfo.InvariantCulture)
                       + ",\"originalHex\":\"" + originalHex
                       + "\",\"strength\":" + strength.ToString(CultureInfo.InvariantCulture) + "}";
            File.WriteAllText(_statePath, json);
        }

        private static string ToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes);
        }

        private static byte[] FromHex(string hex)
        {
            var parts = hex.Split('-');
            var bytes = new byte[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                bytes[i] = Convert.ToByte(parts[i], 16);
            }

            return bytes;
        }
    }
}
