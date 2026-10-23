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
    }

    internal static class AiSettingValues
    {
        private static readonly (string Group, string Label, int Order)[] Names =
        {
            ("Stability", "Less Stable, bump force", 2),
            ("Stability", "Less Stable, aftershock", 3),
            ("Stability", "More Stable, bump force", 0),
            ("Stability", "More Stable, aftershock", 1),
            ("Recovery Skill", "Less Skill", 6),
            ("Recovery Skill", "Normal", 5),
            ("Recovery Skill", "More Skill", 4),
            ("Starting Spread", "Compressed", 8),
            ("Starting Spread", "Expanded", 7),
            ("Mechanical Failures", "None", 9),
            ("Mechanical Failures", "Infrequent", 10),
            ("Mechanical Failures", "Frequent", 11)
        };

        private static readonly float[] GameVanilla =
        {
            0.25f, 2f, 1.8f, 0.5f, 10f, 3.3333333f, 1.6666666f, 0.5f, 1.5f, -1f, 2f, 0.5f
        };

        public const int MinPercent = 60;
        public const int MaxPercent = 200;

        public static int PercentFor(IReadOnlyList<AiSettingValue> fields)
        {
            double sum = 0;
            var count = 0;
            foreach (var field in fields)
            {
                if (Math.Abs(field.Vanilla) < 0.0000001f)
                {
                    continue;
                }

                sum += field.Value / field.Vanilla;
                count++;
            }

            if (count == 0)
            {
                return 100;
            }

            var percent = (int)Math.Round(sum / count * 100.0);
            if (percent < MinPercent)
            {
                return MinPercent;
            }

            if (percent > MaxPercent)
            {
                return MaxPercent;
            }

            return percent;
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
                    Order = name.Order
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
    }
}
