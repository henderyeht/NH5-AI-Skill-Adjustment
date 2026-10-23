using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NH5AiSkillAdjustment
{
    internal sealed class AiSettingValue
    {
        public string Group = "";
        public string Label = "";
        public float Value;
        public float Vanilla;
        public int FileOffset;
        public int Order;
        public int Side;
        public bool Shrinks;
    }

    internal static class AiSettingValues
    {
        private static readonly (string Group, string Label, int Order, int Side, bool Shrinks)[] Names =
        {
            ("Stability", "Less stable, bump", 2, -1, true),
            ("Stability", "Less stable, aftershock", 3, -1, false),
            ("Stability", "More stable, bump", 0, 1, false),
            ("Stability", "More stable, aftershock", 1, 1, true),
            ("Recovery Skill", "Slower", 6, -1, false),
            ("Recovery Skill", "Normal", 5, 0, false),
            ("Recovery Skill", "Faster", 4, 1, true),
            ("Starting Spread", "Compressed", 8, -1, true),
            ("Starting Spread", "Expanded", 7, 1, false),
            ("Mechanical Failures", "Off", 9, 0, false),
            ("Mechanical Failures", "Fewer", 10, -1, false),
            ("Mechanical Failures", "More", 11, 1, true)
        };

        private static readonly float[] GameVanilla =
        {
            0.25f, 2f, 1.8f, 0.5f, 10f, 3.3333333f, 1.6666666f, 0.5f, 1.5f, -1f, 2f, 0.5f
        };

        public const int MinPercent = -100;
        public const int MaxPercent = 100;

        public static void Ends(string group, out string title, out string left, out string right)
        {
            if (group == "Stability")
            {
                title = "Stability";
                left = "Less";
                right = "More";
                return;
            }

            if (group == "Recovery Skill")
            {
                title = "Recovery";
                left = "Slower";
                right = "Faster";
                return;
            }

            if (group == "Starting Spread")
            {
                title = "Starting spread";
                left = "Compressed";
                right = "Expanded";
                return;
            }

            title = "Mechanical failures";
            left = "Few";
            right = "More";
        }

        public static string Word(int position, string left, string right)
        {
            if (position == 0)
            {
                return "Vanilla";
            }

            return position < 0 ? left : right;
        }

        public static int PositionFor(IReadOnlyList<AiSettingValue> fields)
        {
            double left = 0;
            double right = 0;
            var leftCount = 0;
            var rightCount = 0;
            foreach (var field in fields)
            {
                if (field.Side == 0 || Math.Abs(field.Vanilla) < 0.0000001f)
                {
                    continue;
                }

                var ratio = field.Value / field.Vanilla;
                double strength;
                if (field.Shrinks)
                {
                    strength = ratio > 0.0000001f && ratio < 1f ? (1.0 / ratio) - 1.0 : 0;
                }
                else
                {
                    strength = ratio > 1f ? ratio - 1.0 : 0;
                }

                if (strength < 0)
                {
                    strength = 0;
                }

                if (strength > 1)
                {
                    strength = 1;
                }

                if (field.Side < 0)
                {
                    left += strength;
                    leftCount++;
                }
                else
                {
                    right += strength;
                    rightCount++;
                }
            }

            left = leftCount == 0 ? 0 : left / leftCount;
            right = rightCount == 0 ? 0 : right / rightCount;
            if (left < 0.02 && right < 0.02)
            {
                return 0;
            }

            if (right >= left)
            {
                return (int)Math.Round(right * 100.0);
            }

            return -(int)Math.Round(left * 100.0);
        }

        public static void Apply(IReadOnlyList<AiSettingValue> fields, int position)
        {
            var factor = 1f + (Math.Abs(position) / 100f);
            foreach (var field in fields)
            {
                var idle = position == 0 || field.Side == 0 || (position > 0 && field.Side < 0) || (position < 0 && field.Side > 0);
                if (idle)
                {
                    field.Value = field.Vanilla;
                    continue;
                }

                field.Value = field.Shrinks ? field.Vanilla / factor : field.Vanilla * factor;
            }
        }

        public static List<AiSettingValue> Read(string dllPath)
        {
            var bytes = File.ReadAllBytes(dllPath);
            using var asm = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { InMemory = true });
            MethodDefinition? method = null;
            foreach (var type in asm.MainModule.Types)
            {
                if (type.Name != "MGIProviderManager")
                {
                    continue;
                }

                foreach (var candidate in type.Methods)
                {
                    if (candidate.Name == "adjust_ai_settings" && candidate.Body != null)
                    {
                        method = candidate;
                        break;
                    }
                }
            }

            if (method == null)
            {
                throw new InvalidOperationException("Could not find the AI setting values in this DLL.");
            }

            var code = CodeFileOffset(bytes, method.RVA);
            var list = new List<AiSettingValue>();
            foreach (var ins in method.Body.Instructions)
            {
                if (ins.OpCode != OpCodes.Ldc_R4 || list.Count >= Names.Length)
                {
                    continue;
                }

                var name = Names[list.Count];
                list.Add(new AiSettingValue
                {
                    Group = name.Group,
                    Label = name.Label,
                    Value = Convert.ToSingle(ins.Operand, CultureInfo.InvariantCulture),
                    Vanilla = GameVanilla[list.Count],
                    FileOffset = code + ins.Offset + 1,
                    Order = name.Order,
                    Side = name.Side,
                    Shrinks = name.Shrinks
                });
            }

            if (list.Count != Names.Length)
            {
                throw new InvalidOperationException("Could not find the AI setting values in this DLL.");
            }

            list.Sort((a, b) => a.Order.CompareTo(b.Order));
            return list;
        }

        public static void Write(string dllPath, IReadOnlyList<AiSettingValue> fields)
        {
            if (dllPath.IndexOf("NASCARHeat5_Data", StringComparison.OrdinalIgnoreCase) >= 0 && GameLocator.HeatIsRunning())
            {
                throw new InvalidOperationException("NASCAR Heat 5 is running. Close the game, then try again.");
            }

            var bytes = File.ReadAllBytes(dllPath);
            foreach (var field in fields)
            {
                if (field.FileOffset < 0 || field.FileOffset + 4 > bytes.Length)
                {
                    throw new InvalidOperationException("AI setting value is outside the game file.");
                }

                Array.Copy(BitConverter.GetBytes(field.Value), 0, bytes, field.FileOffset, 4);
            }

            File.WriteAllBytes(dllPath, bytes);
        }

        private static int CodeFileOffset(byte[] bytes, int rva)
        {
            var header = RvaToOffset(bytes, rva);
            var word = BitConverter.ToUInt16(bytes, header);
            var size = (word & 3) == 2 ? 1 : ((word >> 12) & 0xF) * 4;
            if (size < 4)
            {
                size = 12;
            }

            return RvaToOffset(bytes, rva + size);
        }

        private static int RvaToOffset(byte[] bytes, int rva)
        {
            var pe = BitConverter.ToInt32(bytes, 0x3C);
            var sections = BitConverter.ToUInt16(bytes, pe + 6);
            var optSize = BitConverter.ToUInt16(bytes, pe + 20);
            var table = pe + 24 + optSize;
            for (var i = 0; i < sections; i++)
            {
                var at = table + (i * 40);
                var virtualSize = BitConverter.ToUInt32(bytes, at + 8);
                var virtualAddress = BitConverter.ToUInt32(bytes, at + 12);
                var rawSize = BitConverter.ToUInt32(bytes, at + 16);
                var rawPtr = BitConverter.ToUInt32(bytes, at + 20);
                var span = Math.Max(virtualSize, rawSize);
                if ((uint)rva >= virtualAddress && (uint)rva < virtualAddress + span)
                {
                    return (int)(rawPtr + ((uint)rva - virtualAddress));
                }
            }

            throw new InvalidOperationException("Could not find the AI setting values in this DLL.");
        }

        public static bool RevertOwn(byte[] bytes, string dllPath)
        {
            List<AiSettingValue> fields;
            try
            {
                fields = Read(dllPath);
            }
            catch (InvalidOperationException)
            {
                return false;
            }

            var changed = false;
            foreach (var field in fields)
            {
                if (!SettingOwned(field) || field.FileOffset < 0 || field.FileOffset + 4 > bytes.Length)
                {
                    continue;
                }

                Array.Copy(BitConverter.GetBytes(field.Vanilla), 0, bytes, field.FileOffset, 4);
                changed = true;
            }

            return changed;
        }

        private static bool SettingOwned(AiSettingValue field)
        {
            if (field.Side == 0 || Math.Abs(field.Vanilla) < 0.0000001f)
            {
                return false;
            }

            if (Math.Abs(field.Value - field.Vanilla) < 0.0001f)
            {
                return false;
            }

            var ratio = field.Value / field.Vanilla;
            float factor;
            if (field.Shrinks)
            {
                if (ratio <= 0f || ratio > 1f)
                {
                    return false;
                }

                factor = field.Vanilla / field.Value;
            }
            else
            {
                if (ratio < 1f)
                {
                    return false;
                }

                factor = ratio;
            }

            if (factor < 0.999f || factor > 2.001f)
            {
                return false;
            }

            var steps = (factor - 1f) * 100f;
            return Math.Abs(steps - (float)Math.Round(steps)) < 0.02f;
        }
    }
}
