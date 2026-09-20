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
        public float Extra = 1f;
    }

    internal sealed class AiSkillPatcher
    {
        public const int MinStrength = 60;
        public const int MaxStrength = 200;
        public const int VanillaMin = 85;
        public const int VanillaMax = 105;
        public const int PercentBase = 100;
        public const int VanillaCustom = 100;

        private const float StockMaxAdjusted = 1.05f;

        // Built at runtime so the PE does not contain a static IL/shellcode blob.
        // ldc.i4.s 85, ldarg.0, ldfld, call, add, ret (tokens masked).
        private static byte[] RatingNeedle()
        {
            return Op(31, 85, 2, 123, 0, 0, 0, 4, 40, 0, 0, 0, 6, 88, 42);
        }

        private static readonly bool[] RatingWild =
        {
            false, false, false, false, true, true, false, false, false, true, true, false, false, false, false
        };

        // AdjustSkillTable: if (league == desired) return; then GetSubstitute(league, desired)
        private static byte[] EarlyRetStock()
        {
            return Op(64, 1, 0, 0, 0, 42, 7, 3, 40);
        }

        private static byte[] EarlyRetPatched()
        {
            return Op(64, 1, 0, 0, 0, 0, 7, 3, 40);
        }

        // GetSubstitute: num++ then br loop, then return 1f
        private static byte[] ExtraPrefix()
        {
            return Op(6, 23, 88, 10, 56);
        }

        private static byte[] Op(params int[] parts)
        {
            var bytes = new byte[parts.Length];
            for (var i = 0; i < parts.Length; i++)
            {
                bytes[i] = (byte)parts[i];
            }

            return bytes;
        }

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

        public static float ExtraFor(int strength)
        {
            return strength / (float)PercentBase;
        }

        public static int StrengthFromExtra(float extra)
        {
            return Clamp((int)Math.Round(extra * PercentBase), MinStrength, MaxStrength);
        }

        public PatchTarget Resolve()
        {
            var bytes = File.ReadAllBytes(_dllPath);
            return Resolve(bytes);
        }

        public int Apply(int value)
        {
            if (IsLiveInstall() && GameLocator.HeatIsRunning())
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
            RestoreRatingToStock(bytes);
            EnsureStockBackup(bytes);

            var sites = LocateSites(bytes);
            var extra = ExtraFor(value);
            ApplyTable(bytes, sites, extra);
            // MP has no AI difficulty control (AUTO ~97). Force native Custom 105.
            Array.Copy(NewForcedRatingBytes(VanillaMax), 0, bytes, sites.RatingOffset, 15);

            WriteDll(bytes, alreadyBackedUp: true);
            SaveState(sites.RatingOffset, ToHex(sites.RatingOriginal), value, extra);
            return value;
        }

        public string Restore()
        {
            if (IsLiveInstall() && GameLocator.HeatIsRunning())
            {
                throw new InvalidOperationException("NASCAR Heat 5 is running. Close the game, then try again.");
            }

            if (!File.Exists(_dllPath))
            {
                throw new InvalidOperationException("Could not find Assembly-CSharp.dll. Use Locate game.");
            }

            var bak = StockBackupPath();
            if (File.Exists(bak))
            {
                var stock = File.ReadAllBytes(bak);
                WriteDll(stock, alreadyBackedUp: true);
                if (File.Exists(_statePath))
                {
                    File.Delete(_statePath);
                }

                return "restored";
            }

            var bytes = File.ReadAllBytes(_dllPath);
            var target = Resolve(bytes);
            RestoreRatingToStock(bytes);
            var sites = LocateSites(bytes);
            ApplyTable(bytes, sites, 1f);
            RestoreRatingAt(bytes, sites.RatingOffset, sites.RatingOriginal);
            WriteDll(bytes, alreadyBackedUp: true);
            if (File.Exists(_statePath))
            {
                File.Delete(_statePath);
            }

            return target.Kind == "stock" && Math.Abs(target.Extra - 1f) < 0.0001f
                ? "already-stock"
                : "restored";
        }

        private PatchTarget Resolve(byte[] bytes)
        {
            Sites sites;
            try
            {
                sites = LocateSites(bytes);
            }
            catch (InvalidOperationException)
            {
                throw new InvalidOperationException("Could not find Heat 5 AI skill code in this DLL. Locate NASCARHeat5_Data\\Managed.");
            }

            var extra = ReadExtra(bytes, sites);
            var rating = GetForcedRatingAt(bytes, sites.RatingOffset);
            var tableOn = Math.Abs(extra - 1f) > 0.0005f || bytes[sites.EarlyRetOffset] == 0x00;
            var state = ReadState();

            if (!tableOn && !rating.HasValue)
            {
                return new PatchTarget
                {
                    Kind = "stock",
                    Offset = sites.RatingOffset,
                    Original = sites.RatingOriginal,
                    Extra = 1f
                };
            }

            int current;
            if (tableOn)
            {
                current = StrengthFromExtra(extra);
            }
            else if (rating.HasValue && rating.Value == VanillaMax)
            {
                current = PercentBase;
            }
            else if (rating.HasValue)
            {
                current = rating.Value;
            }
            else if (state != null)
            {
                current = state.Strength;
            }
            else
            {
                current = PercentBase;
            }

            return new PatchTarget
            {
                Kind = "patched",
                Offset = sites.RatingOffset,
                Original = sites.RatingOriginal,
                Current = current,
                Extra = extra
            };
        }

        private sealed class Sites
        {
            public int RatingOffset;
            public byte[] RatingOriginal = Array.Empty<byte>();
            public int EarlyRetOffset;
            public int ExtraFloatOffset;
            public int MaxAdjFloatOffset;
        }

        private static Sites LocateSites(byte[] bytes)
        {
            var ratingOff = FindEffectiveRatingOffset(bytes);
            var orig = OriginalEffectiveRating(bytes, ratingOff);

            var early = IndexOf(bytes, EarlyRetStock(), 0);
            if (early < 0)
            {
                early = IndexOf(bytes, EarlyRetPatched(), 0);
            }

            if (early < 0)
            {
                throw new InvalidOperationException("Could not find AdjustSkillTable in this DLL.");
            }

            var extraOff = FindExtraFloatOffset(bytes);
            var maxAdjOff = FindMaxAdjFloatOffset(bytes);
            if (extraOff < 0 || maxAdjOff < 0)
            {
                throw new InvalidOperationException("Could not find SkillTable conversion code in this DLL.");
            }

            return new Sites
            {
                RatingOffset = ratingOff,
                RatingOriginal = orig,
                EarlyRetOffset = early + 5,
                ExtraFloatOffset = extraOff,
                MaxAdjFloatOffset = maxAdjOff
            };
        }

        private static int FindEffectiveRatingOffset(byte[] bytes)
        {
            var hits = AllMask(bytes, RatingNeedle(), RatingWild);
            if (hits.Count >= 2)
            {
                return hits[1];
            }

            if (hits.Count == 1)
            {
                var forced = hits[0] + 16;
                if (GetForcedRatingAt(bytes, forced).HasValue)
                {
                    return forced;
                }
            }

            var stateOff = TryStateRatingOffset();
            if (stateOff.HasValue && GetForcedRatingAt(bytes, stateOff.Value).HasValue)
            {
                return stateOff.Value;
            }

            throw new InvalidOperationException("Could not find Heat 5 AI skill code in this DLL. Locate NASCARHeat5_Data\\Managed.");
        }

        private static byte[] OriginalEffectiveRating(byte[] bytes, int ratingOff)
        {
            var slice = new byte[15];
            Array.Copy(bytes, ratingOff, slice, 0, 15);
            var needle = RatingNeedle();
            if (IndexOfMask(slice, needle, RatingWild, 0) == 0)
            {
                return slice;
            }

            var baseOff = ratingOff - 16;
            if (baseOff >= 0 && IndexOfMask(bytes, needle, RatingWild, baseOff) == baseOff)
            {
                return OriginalFromBase(bytes, baseOff);
            }

            return Op(31, 85, 2, 123, 152, 51, 0, 4, 40, 238, 63, 0, 6, 88, 42);
        }

        private static int FindExtraFloatOffset(byte[] bytes)
        {
            var p = 0;
            while (true)
            {
                var i = IndexOf(bytes, ExtraPrefix(), p);
                if (i < 0)
                {
                    return -1;
                }

                // 06 17 58 0A 38 xx xx xx xx 22 [float4] 2A  — GetSubstitute loop-then-return-1f
                if (i + 15 < bytes.Length
                    && bytes[i + 8] == 0xFF
                    && bytes[i + 9] == 0x22
                    && bytes[i + 14] == 0x2A)
                {
                    var extra = BitConverter.ToSingle(bytes, i + 10);
                    if (extra >= 0.5f && extra <= 2.5f)
                    {
                        return i + 10;
                    }
                }

                p = i + 1;
            }
        }

        private static int FindMaxAdjFloatOffset(byte[] bytes)
        {
            // SkillTable ctor: draft_skill_f = 1f; max_adjusted_skill = 1.05f * extra; speedrating_laptime_override = -1f.
            // Field tokens move between Base Steam (0x04002D98) and Next Gen (0x04002D97). Do not hardcode them.
            for (var i = 0; i + 32 <= bytes.Length; i++)
            {
                if (bytes[i] != 0x22)
                {
                    continue;
                }

                if (bytes[i + 1] != 0x00 || bytes[i + 2] != 0x00 || bytes[i + 3] != 0x80 || bytes[i + 4] != 0x3F)
                {
                    continue;
                }

                if (bytes[i + 5] != 0x7D || bytes[i + 8] != 0x00 || bytes[i + 9] != 0x04)
                {
                    continue;
                }

                if (bytes[i + 10] != 0x02 || bytes[i + 11] != 0x22)
                {
                    continue;
                }

                if (bytes[i + 16] != 0x7D || bytes[i + 19] != 0x00 || bytes[i + 20] != 0x04)
                {
                    continue;
                }

                if (bytes[i + 21] != 0x02 || bytes[i + 22] != 0x22)
                {
                    continue;
                }

                if (bytes[i + 23] != 0x00 || bytes[i + 24] != 0x00 || bytes[i + 25] != 0x80 || bytes[i + 26] != 0xBF)
                {
                    continue;
                }

                if (bytes[i + 27] != 0x7D || bytes[i + 30] != 0x00 || bytes[i + 31] != 0x04)
                {
                    continue;
                }

                var maxAdj = BitConverter.ToSingle(bytes, i + 12);
                if (maxAdj >= 1.049f && maxAdj <= 2.51f)
                {
                    return i + 12;
                }
            }

            return -1;
        }

        private static float ReadExtra(byte[] bytes, Sites sites)
        {
            if (sites.ExtraFloatOffset < 0 || sites.ExtraFloatOffset + 4 > bytes.Length)
            {
                return 1f;
            }

            return BitConverter.ToSingle(bytes, sites.ExtraFloatOffset);
        }

        private static void ApplyTable(byte[] bytes, Sites sites, float extra)
        {
            if (Math.Abs(extra - 1f) < 0.0001f)
            {
                bytes[sites.EarlyRetOffset] = 0x2A;
                var one = BitConverter.GetBytes(1f);
                Array.Copy(one, 0, bytes, sites.ExtraFloatOffset, 4);
                var max = BitConverter.GetBytes(StockMaxAdjusted);
                Array.Copy(max, 0, bytes, sites.MaxAdjFloatOffset, 4);
                return;
            }

            bytes[sites.EarlyRetOffset] = 0x00;
            var extraBytes = BitConverter.GetBytes(extra);
            Array.Copy(extraBytes, 0, bytes, sites.ExtraFloatOffset, 4);
            var raised = BitConverter.GetBytes(Math.Max(StockMaxAdjusted, StockMaxAdjusted * extra));
            Array.Copy(raised, 0, bytes, sites.MaxAdjFloatOffset, 4);
        }

        private static void RestoreRatingToStock(byte[] bytes)
        {
            var hits = AllMask(bytes, RatingNeedle(), RatingWild);
            if (hits.Count >= 2)
            {
                return;
            }

            if (hits.Count == 1)
            {
                var off = hits[0] + 16;
                if (GetForcedRatingAt(bytes, off).HasValue)
                {
                    var orig = OriginalFromBase(bytes, hits[0]);
                    Array.Copy(orig, 0, bytes, off, 15);
                }
            }
        }

        private static void RestoreRatingAt(byte[] bytes, int offset, byte[] original)
        {
            if (original != null && original.Length == 15 && offset >= 0)
            {
                Array.Copy(original, 0, bytes, offset, 15);
            }
        }

        private void EnsureStockBackup(byte[] stockBytes)
        {
            var bak = StockBackupPath();
            if (!File.Exists(bak))
            {
                File.WriteAllBytes(bak, stockBytes);
            }
        }

        private string StockBackupPath()
        {
            return _dllPath + ".nh5ai-stock.bak";
        }

        private bool IsLiveInstall()
        {
            return _dllPath.IndexOf("NASCARHeat5_Data", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void WriteDll(byte[] bytes, bool alreadyBackedUp = false)
        {
            if (!alreadyBackedUp && !File.Exists(StockBackupPath()))
            {
                throw new InvalidOperationException("Missing stock DLL backup. Restore vanilla with Steam Verify, then APPLY again.");
            }

            var attrs = File.GetAttributes(_dllPath);
            if ((attrs & FileAttributes.ReadOnly) != 0)
            {
                File.SetAttributes(_dllPath, attrs & ~FileAttributes.ReadOnly);
            }

            var tmp = _dllPath + ".nh5ai.tmp";
            if (File.Exists(tmp))
            {
                File.Delete(tmp);
            }

            File.WriteAllBytes(tmp, bytes);
            File.Replace(tmp, _dllPath, null);
        }

        // Unity 2017 tiny method is exactly 15 bytes. Dead nops after ret still
        // fail verification. 85 + sbyte delta covers 60-200, used only for 85-105.
        private static byte[] NewForcedRatingBytes(int value)
        {
            var delta = unchecked((byte)(sbyte)(value - 85));
            return Op(31, 85, 31, delta, 88, 22, 88, 22, 88, 22, 88, 22, 88, 0, 42);
        }

        private static int? GetForcedRatingAt(byte[] bytes, int off)
        {
            if (off < 0 || off + 15 > bytes.Length)
            {
                return null;
            }

            if (bytes[off] == 0x1F && bytes[off + 1] == 85 && bytes[off + 2] == 0x1F && bytes[off + 4] == 0x58)
            {
                var n = 85 + (sbyte)bytes[off + 3];
                if (n >= MinStrength && n <= MaxStrength)
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

        private static int IndexOf(byte[] hay, byte[] needle, int start)
        {
            var lim = hay.Length - needle.Length;
            for (var i = start; i <= lim; i++)
            {
                var j = 0;
                for (; j < needle.Length; j++)
                {
                    if (hay[i + j] != needle[j])
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
            public int Strength = VanillaCustom;
        }

        private static int? TryStateRatingOffset()
        {
            try
            {
                var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NH5AiSkillAdjustment");
                var path = Path.Combine(dir, "state.json");
                if (!File.Exists(path))
                {
                    return null;
                }

                var text = File.ReadAllText(path);
                var off = Regex.Match(text, "\"offset\"\\s*:\\s*(-?\\d+)");
                if (!off.Success)
                {
                    return null;
                }

                return int.Parse(off.Groups[1].Value, CultureInfo.InvariantCulture);
            }
            catch
            {
                return null;
            }
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
                var strength = Regex.Match(text, "\"strength\"\\s*:\\s*(-?\\d+)");
                if (!off.Success || !hex.Success)
                {
                    return null;
                }

                return new State
                {
                    Offset = int.Parse(off.Groups[1].Value, CultureInfo.InvariantCulture),
                    OriginalHex = hex.Groups[1].Value,
                    Strength = strength.Success
                        ? int.Parse(strength.Groups[1].Value, CultureInfo.InvariantCulture)
                        : VanillaCustom
                };
            }
            catch
            {
                return null;
            }
        }

        private void SaveState(int offset, string originalHex, int strength, float extra)
        {
            var json = "{\"offset\":" + offset.ToString(CultureInfo.InvariantCulture)
                       + ",\"originalHex\":\"" + originalHex
                       + "\",\"strength\":" + strength.ToString(CultureInfo.InvariantCulture)
                       + ",\"extra\":" + extra.ToString("R", CultureInfo.InvariantCulture) + "}";
            File.WriteAllText(_statePath, json);
        }

        private static string ToHex(byte[] bytes)
        {
            return BitConverter.ToString(bytes);
        }

        private static int Clamp(int n, int min, int max)
        {
            if (n < min)
            {
                return min;
            }

            if (n > max)
            {
                return max;
            }

            return n;
        }
    }
}
