using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace NH5AiSkillAdjustment
{
    internal sealed class AiTrackField
    {
        public string Group = "";
        public string Label = "";
        public float Value;
        public bool Boolean;
        public bool BoolValue;
        public string Key = "";
        public string Pair = "";
        public float Vanilla;
        public int FileOffset;
        public int WriteKind;
        public float DisplayScale = 1f;
        public bool Shared;
        public List<int> ExtraOffsets = new List<int>();
    }

    internal enum TrackAdjustKind
    {
        Scale,
        Range,
        Percent
    }

    internal sealed class TrackAdjust
    {
        public string Title = "";
        public string Left = "";
        public string Right = "";
        public TrackAdjustKind Kind;
        public string[] Keys = Array.Empty<string>();
    }

    internal sealed class AiTrackInfo
    {
        public string Name = "";
        public List<AiTrackField> Fields = new List<AiTrackField>();
    }

    internal static class AiTrackValues
    {
        private sealed class Store
        {
            public string Field = "";
            public string Owner = "";
            public float Value;
            public bool Boolean;
            public int FileOffset;
            public int WriteKind;
            public bool Shared;
            public string Tag = "";
        }

        private struct Name
        {
            public Name(string field, string group, string label)
            {
                Field = field;
                Group = group;
                Label = label;
            }

            public string Field;
            public string Group;
            public string Label;
        }

        private static readonly Name[] Catalog =
        {
            new Name("draft_influence", "Draft", "Draft"),
            new Name("min_draft_lon_speed_mph", "Draft", "Draft speed, low"),
            new Name("max_draft_lon_speed_mph", "Draft", "Draft speed, high"),
            new Name("min_draft_lon_carlen", "Draft", "Draft length, close"),
            new Name("max_draft_lon_carlen", "Draft", "Draft length, far"),
            new Name("min_draft_width_carwid", "Draft", "Draft width, narrow"),
            new Name("max_draft_width_carwid", "Draft", "Draft width, wide"),
            new Name("enable_draftbuddy", "Draft", "Draft partners"),
            new Name("only_bump_buddies", "Draft", "Only bump a partner"),
            new Name("draftbuddy_max_bump_speed_mph", "Draft", "Partner bump speed"),

            new Name("bump_force", "Contact", "Bump strength"),
            new Name("bump_aftershock", "Contact", "Bump aftershock"),
            new Name("bump_aftershock_spin_f", "Contact", "Spin after a bump"),
            new Name("crash_force", "Contact", "Crash strength"),
            new Name("crash_aftershock", "Contact", "Crash aftershock"),

            new Name("max_lookahead", "Awareness", "Look ahead, far"),
            new Name("min_lookahead", "Awareness", "Look ahead, near"),
            new Name("min_lookahead_m", "Awareness", "Look ahead, distance"),
            new Name("max_speedfactor_when_checking", "Awareness", "Speed while looking"),
            new Name("nearcar_speedf_at_min_curvature", "Awareness", "Speed beside a car"),
            new Name("magic_grip", "Grip", "Grip help"),
            new Name("curvature_skill_f", "Cornering", "Cornering skill"),
            new Name("curvature_min_skill_f", "Cornering", "Cornering skill, low end"),
            new Name("wall_approach_lat_grip_f", "Grip", "Grip near the wall"),

            new Name("pass_hys_min", "Passing", "Pass gap, close"),
            new Name("pass_hys_max", "Passing", "Pass gap, far"),
            new Name("pointless_pass_dist", "Passing", "Distance a pass stops being worth it"),
            new Name("moveout_factor", "Passing", "Move aside"),
            new Name("moveout_timer_f", "Passing", "Time before moving aside"),
            new Name("pushout_factor", "Passing", "Push out"),
            new Name("push_sensitivity", "Passing", "Push"),
            new Name("push_lat_sens", "Passing", "Side push"),
            new Name("checks", "Passing", "How often it checks the lane"),
            new Name("merge_t", "Passing", "Merge time"),
            new Name("passout_greenflag_fadein_duration", "Passing", "Settle time after green"),
            new Name("yellow_moveout_f", "Passing", "Move aside under yellow"),
            new Name("enable_threewide_nothrottle", "Passing", "Lift when three-wide"),

            new Name("fastlane_factor", "Line", "Prefer the fast lane"),
            new Name("fastlane_caution", "Line", "Fast lane under caution"),
            new Name("highline_enabled", "Line", "High line"),
            new Name("highline_skill_f", "Line", "High line ability"),
            new Name("highline_wall_margin", "Line", "High line gap to the wall"),
            new Name("highline_inhibited_after_greenflag_t", "Line", "High line delay after green"),
            new Name("highline_decide_on_back_straight", "Line", "Pick the high line on the backstretch"),
            new Name("leftwall", "Line", "Left wall gap"),
            new Name("rightwall", "Line", "Right wall gap"),
            new Name("zone_margin_factor", "Line", "Lane margin"),
            new Name("contact_extra_left_lanes", "Line", "Extra lanes on the left"),
            new Name("leftman_never_forces", "Line", "Car on the left does not force"),

            new Name("cl_desired_dist", "Following", "Preferred following distance"),
            new Name("cl_forcible_min_dist", "Following", "Closest following distance"),
            new Name("cl_max_point_dist", "Following", "Furthest following distance"),

            new Name("ym_restart_intercar", "Restarts", "Restart spacing"),
            new Name("ym_restart_offset", "Restarts", "Restart lineup offset"),

            new Name("can_pit", "Pit", "Can pit"),
            new Name("pit_speed", "Pit", "Pit road speed"),
            new Name("pitentry_max_speed_mph", "Pit", "Pit entry speed"),
            new Name("pitexit_max_speed_mph", "Pit", "Pit exit speed"),
            new Name("max_speedfactor_blendpits", "Pit", "Speed entering the pits"),
            new Name("fuel_per_lap", "Pit", "Fuel used per lap"),
            new Name("laps_per_tank", "Pit", "Laps per tank"),
            new Name("postpits_dontmind_t", "Pit", "Don't block after a pit stop"),
            new Name("enable_pitstall_departure_jets", "Pit", "Launch leaving the stall"),
            new Name("pit_skill_variance", "Pit", "Pit stop consistency"),

            new Name("tire_wear_speed_factor", "Tires", "How much wear changes pace"),
            new Name("tire_life_ghost", "Tires", "AI tire life"),
            new Name("tire_life_player", "Tires", "Player tire life"),
            new Name("tirewear_25_speed", "Tires", "Worn-tire pace"),
            new Name("tirewear_25_lap", "Tires", "Lap the worn pace starts"),

            new Name("hog_f", "Fuel", "Fuel saving"),
            new Name("hog_yellow_factor", "Fuel", "Fuel saving under yellow"),
            new Name("hog_dnf_factor", "Fuel", "Fuel saving when out of the race")
        };

        public static List<AiTrackInfo> Read(string dllPath)
        {
            var bytes = File.ReadAllBytes(dllPath);
            var list = new List<AiTrackInfo>();
            using var asm = AssemblyDefinition.ReadAssembly(dllPath, new ReaderParameters { InMemory = true });
            TypeDefinition? ai = null;
            TypeDefinition? physics = null;
            foreach (var type in asm.MainModule.Types)
            {
                if (type.Namespace != "D2DConvertedDataWarehouse")
                {
                    continue;
                }

                if (type.Name == "AI")
                {
                    ai = type;
                }
                else if (type.Name == "Physics")
                {
                    physics = type;
                }
            }

            if (ai == null || physics == null)
            {
                throw new InvalidOperationException("This Heat 5 file has no AI track data.");
            }

            var flat = new Dictionary<string, List<Store>>(StringComparer.Ordinal);
            Index(ai, bytes, flat);
            Index(physics, bytes, flat);
            var names = new Dictionary<string, Name>(StringComparer.Ordinal);
            foreach (var name in Catalog)
            {
                names[name.Field] = name;
            }

            foreach (var type in asm.MainModule.Types)
            {
                if (type.Namespace != "D2DConvertedDataWarehouse" || type.Name.IndexOf('<') >= 0)
                {
                    continue;
                }

                if (type.Name.EndsWith("TrackData", StringComparison.Ordinal) || type.Name.EndsWith("TrackDataDLC", StringComparison.Ordinal))
                {
                    continue;
                }

                MethodDefinition? tweak = null;
                MethodDefinition? tweakPhysics = null;
                foreach (var method in type.Methods)
                {
                    if (method.Body == null)
                    {
                        continue;
                    }

                    if (method.Name.EndsWith("TweakAI", StringComparison.Ordinal))
                    {
                        tweak = method;
                    }
                    else if (method.Name.EndsWith("TweakPhysics", StringComparison.Ordinal))
                    {
                        tweakPhysics = method;
                    }
                }

                if (tweak == null)
                {
                    continue;
                }

                var map = new Dictionary<string, Store>(StringComparer.Ordinal);
                var extras = new List<Store>();
                var stack = new HashSet<string>(StringComparer.Ordinal);
                if (flat.TryGetValue(ai.FullName + "::MakeDefault", out var aiDefault))
                {
                    foreach (var store in aiDefault)
                    {
                        Take(map, extras, Copy(store, true, ""));
                    }
                }

                if (flat.TryGetValue(physics.FullName + "::MakeDefault", out var physicsDefault))
                {
                    foreach (var store in physicsDefault)
                    {
                        if (IsDraft(store.Field))
                        {
                            Take(map, extras, Copy(store, true, ""));
                        }
                    }
                }

                Run(tweak, bytes, flat, map, extras, stack, "", false, 0);
                if (tweakPhysics != null)
                {
                    Run(tweakPhysics, bytes, flat, map, extras, stack, "", false, 0);
                }

                var fields = new List<AiTrackField>();
                var seen = new HashSet<string>(StringComparer.Ordinal);
                foreach (var name in Catalog)
                {
                    seen.Add(name.Field);
                    if (map.TryGetValue(name.Field, out var store))
                    {
                        var field = ToField(name, store);
                        fields.Add(field);
                        foreach (var extra in extras)
                        {
                            if (extra.Field != name.Field)
                            {
                                continue;
                            }

                            if (Same(store, extra) && store.Shared == extra.Shared)
                            {
                                field.ExtraOffsets.Add(extra.FileOffset);
                            }
                            else
                            {
                                fields.Add(ToField(name, extra));
                            }
                        }
                    }
                    else
                    {
                        foreach (var extra in extras)
                        {
                            if (extra.Field == name.Field)
                            {
                                fields.Add(ToField(name, extra));
                            }
                        }
                    }
                }

                foreach (var pair in map)
                {
                    if (seen.Contains(pair.Key) || !Keep(pair.Value))
                    {
                        continue;
                    }

                    fields.Add(ToField(new Name(pair.Key, "Other", Nice(pair.Key)), pair.Value));
                }

                foreach (var method in type.Methods)
                {
                    if (method.Body == null || !method.Name.EndsWith("GetSkillTable", StringComparison.Ordinal))
                    {
                        continue;
                    }

                    var isBase = method.Name.EndsWith("GetBaseSkillTable", StringComparison.Ordinal);
                    var isDlc = method.Name.EndsWith("GetDLCSkillTable", StringComparison.Ordinal);
                    if (!isBase && !isDlc && CountSkill(method) == 0)
                    {
                        continue;
                    }

                    var prefix = isBase ? "Base " : isDlc ? "Next Gen " : "";
                    AddSkill(method, CodeFileOffset(bytes, method.RVA), fields, prefix);
                }

                if (fields.Count == 0)
                {
                    continue;
                }

                list.Add(new AiTrackInfo
                {
                    Name = type.Name.Replace('_', ' '),
                    Fields = fields
                });
            }

            list.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));
            return list;
        }


        public static void Write(string dllPath, IReadOnlyList<AiTrackField> fields)
        {
            if (dllPath.IndexOf("NASCARHeat5_Data", StringComparison.OrdinalIgnoreCase) >= 0 && GameLocator.HeatIsRunning())
            {
                throw new InvalidOperationException("NASCAR Heat 5 is running. Close the game, then try again.");
            }

            var bytes = File.ReadAllBytes(dllPath);
            foreach (var field in fields)
            {
                Patch(bytes, field.FileOffset, field);
                foreach (var offset in field.ExtraOffsets)
                {
                    Patch(bytes, offset, field);
                }
            }

            File.WriteAllBytes(dllPath, bytes);
        }

        private static void Index(TypeDefinition type, byte[] bytes, Dictionary<string, List<Store>> flat)
        {
            foreach (var method in type.Methods)
            {
                if (method.Body == null)
                {
                    continue;
                }

                flat[type.FullName + "::" + method.Name] = ScanStores(method, bytes);
            }
        }

        private static void Run(MethodDefinition method, byte[] bytes, Dictionary<string, List<Store>> flat, Dictionary<string, Store> map, List<Store> extras, HashSet<string> stack, string tag, bool shared, int depth)
        {
            if (method.Body == null || depth > 6)
            {
                return;
            }

            var id = method.DeclaringType.FullName + "::" + method.Name;
            if (!stack.Add(id))
            {
                return;
            }

            try
            {
                var code = CodeFileOffset(bytes, method.RVA);
                var pending = new List<Store>();
                string? league = null;
                var leagueUntil = int.MaxValue;
                int? pendingLeague = null;
                var armLeague = false;
                foreach (var ins in method.Body.Instructions)
                {
                    if (ins.Offset >= leagueUntil)
                    {
                        league = null;
                        leagueUntil = int.MaxValue;
                    }

                    if (ins.OpCode == OpCodes.Ldarg_2)
                    {
                        armLeague = true;
                        continue;
                    }

                    if (armLeague && TryInt(ins, out var leagueId))
                    {
                        pendingLeague = leagueId;
                        armLeague = false;
                        continue;
                    }

                    armLeague = false;

                    if (pendingLeague != null && ins.Operand is Instruction target && (ins.OpCode == OpCodes.Bne_Un || ins.OpCode == OpCodes.Bne_Un_S))
                    {
                        league = LeagueName(pendingLeague.Value);
                        leagueUntil = target.Offset;
                        pendingLeague = null;
                        pending.Clear();
                        continue;
                    }

                    if (ins.OpCode == OpCodes.Beq || ins.OpCode == OpCodes.Beq_S)
                    {
                        pendingLeague = null;
                        pending.Clear();
                        continue;
                    }

                    if (ins.OpCode == OpCodes.Ldc_R4)
                    {
                        pending.Add(new Store
                        {
                            Value = Convert.ToSingle(ins.Operand, CultureInfo.InvariantCulture),
                            FileOffset = code + ins.Offset + 1,
                            WriteKind = 0
                        });
                        continue;
                    }

                    if (TryInt(ins, out var number))
                    {
                        pending.Add(IntStore(ins, code, number));
                        continue;
                    }

                    if (ins.OpCode == OpCodes.Stfld && ins.Operand is FieldReference field && pending.Count > 0)
                    {
                        var last = pending[pending.Count - 1];
                        var store = new Store
                        {
                            Field = field.Name,
                            Owner = field.DeclaringType == null ? "" : field.DeclaringType.Name,
                            Value = last.Value,
                            Boolean = IsBool(field),
                            FileOffset = last.FileOffset,
                            WriteKind = last.WriteKind,
                            Shared = shared,
                            Tag = string.IsNullOrEmpty(league) ? tag : league
                        };
                        if (Keep(store))
                        {
                            Take(map, extras, store);
                        }

                        pending.Clear();
                        continue;
                    }

                    if ((ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) && ins.Operand is MethodReference called)
                    {
                        var resolved = called.Resolve();
                        if (resolved != null && resolved.Name == "SetAI_TireLifeSpeedFalloff")
                        {
                            var floats = Floats(pending);
                            if (floats.Count >= 2)
                            {
                                Take(map, extras, Arg(floats[0], "tire_life_ghost", tag, league));
                                Take(map, extras, Arg(floats[1], "tire_life_player", tag, league));
                                if (string.IsNullOrEmpty(league) && string.IsNullOrEmpty(tag))
                                {
                                    map.Remove("tire_wear_speed_factor");
                                }
                            }
                        }
                        else if (resolved != null && (resolved.DeclaringType.Name == "AI" || resolved.DeclaringType.Name == "Physics"))
                        {
                            var key = resolved.DeclaringType.FullName + "::" + resolved.Name;
                            if (flat.TryGetValue(key, out var stores))
                            {
                                foreach (var store in stores)
                                {
                                    var copy = Copy(store, true, string.IsNullOrEmpty(league) ? tag : league);
                                    if (Keep(copy))
                                    {
                                        Take(map, extras, copy);
                                    }
                                }
                            }
                        }
                        else if (resolved != null && resolved.Body != null && resolved.DeclaringType.Namespace == "D2DConvertedDataWarehouse")
                        {
                            var next = tag;
                            if (resolved.Name.IndexOf("DLC", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                next = "Next Gen";
                            }
                            else if (resolved.Name.IndexOf("Base", StringComparison.Ordinal) >= 0)
                            {
                                next = "";
                            }

                            if (!string.IsNullOrEmpty(league))
                            {
                                next = league;
                            }

                            Run(resolved, bytes, flat, map, extras, stack, next, false, depth + 1);
                        }

                        pending.Clear();
                        pendingLeague = null;
                        continue;
                    }

                    if (ins.OpCode == OpCodes.Ldarg_0 || ins.OpCode == OpCodes.Ldarg_1 || ins.OpCode == OpCodes.Ldarg_3 || ins.OpCode == OpCodes.Ldarg
                        || ins.OpCode == OpCodes.Ldloc || ins.OpCode == OpCodes.Ldloc_0 || ins.OpCode == OpCodes.Ldloc_1 || ins.OpCode == OpCodes.Ldloc_2 || ins.OpCode == OpCodes.Ldloc_3
                        || ins.OpCode == OpCodes.Ldloc_S || ins.OpCode == OpCodes.Dup || ins.OpCode == OpCodes.Nop)
                    {
                        continue;
                    }

                    pending.Clear();
                    pendingLeague = null;
                }
            }
            finally
            {
                stack.Remove(id);
            }
        }

        private static List<Store> ScanStores(MethodDefinition method, byte[] bytes)
        {
            var list = new List<Store>();
            var code = CodeFileOffset(bytes, method.RVA);
            var pending = new List<Store>();
            foreach (var ins in method.Body.Instructions)
            {
                if (ins.OpCode == OpCodes.Ldc_R4)
                {
                    pending.Add(new Store
                    {
                        Value = Convert.ToSingle(ins.Operand, CultureInfo.InvariantCulture),
                        FileOffset = code + ins.Offset + 1,
                        WriteKind = 0
                    });
                    continue;
                }

                if (TryInt(ins, out var number))
                {
                    pending.Add(IntStore(ins, code, number));
                    continue;
                }

                if (ins.OpCode == OpCodes.Stfld && ins.Operand is FieldReference field && pending.Count > 0)
                {
                    var last = pending[pending.Count - 1];
                    list.Add(new Store
                    {
                        Field = field.Name,
                        Owner = field.DeclaringType == null ? "" : field.DeclaringType.Name,
                        Value = last.Value,
                        Boolean = IsBool(field),
                        FileOffset = last.FileOffset,
                        WriteKind = last.WriteKind,
                        Shared = true
                    });
                    pending.Clear();
                    continue;
                }

                if (ins.OpCode == OpCodes.Ldarg_0 || ins.OpCode == OpCodes.Ldarg_1 || ins.OpCode == OpCodes.Ldarg_2 || ins.OpCode == OpCodes.Ldarg_3 || ins.OpCode == OpCodes.Ldarg
                    || ins.OpCode == OpCodes.Ldloc || ins.OpCode == OpCodes.Ldloc_0 || ins.OpCode == OpCodes.Ldloc_1 || ins.OpCode == OpCodes.Ldloc_2 || ins.OpCode == OpCodes.Ldloc_3
                    || ins.OpCode == OpCodes.Ldloc_S || ins.OpCode == OpCodes.Dup || ins.OpCode == OpCodes.Nop)
                {
                    continue;
                }

                pending.Clear();
            }

            return list;
        }

        private static void AddSkill(MethodDefinition method, int code, List<AiTrackField> fields, string prefix)
        {
            var floats = new List<Store>();
            var skillIndex = 0;
            foreach (var ins in method.Body.Instructions)
            {
                if (ins.OpCode == OpCodes.Ldc_R4)
                {
                    floats.Add(new Store
                    {
                        Value = Convert.ToSingle(ins.Operand, CultureInfo.InvariantCulture),
                        FileOffset = code + ins.Offset + 1
                    });
                    continue;
                }

                if ((ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) && ins.Operand is MethodReference called)
                {
                    if (called.Name.Contains("MakeMinMax") && floats.Count >= 2)
                    {
                        skillIndex++;
                        var rank = skillIndex == 1 ? "Easy" : skillIndex == 2 ? "Medium" : skillIndex == 3 ? "Hard" : "Skill " + skillIndex.ToString(CultureInfo.InvariantCulture);
                        fields.Add(Skill(prefix + rank + " minimum", floats[0]));
                        fields.Add(Skill(prefix + rank + " maximum", floats[1]));
                        if (floats.Count >= 3)
                        {
                            fields.Add(Skill(prefix + rank + " lap time", floats[2]));
                        }
                    }

                    floats.Clear();
                    continue;
                }

                if (ins.OpCode == OpCodes.Ldarg_0 || ins.OpCode == OpCodes.Ldarg_1 || ins.OpCode == OpCodes.Ldarg_2 || ins.OpCode == OpCodes.Ldarg_3 || ins.OpCode == OpCodes.Ldarg
                    || ins.OpCode == OpCodes.Ldc_I4 || ins.OpCode == OpCodes.Ldc_I4_S || ins.OpCode == OpCodes.Ldc_I4_M1
                    || ins.OpCode == OpCodes.Ldc_I4_0 || ins.OpCode == OpCodes.Ldc_I4_1 || ins.OpCode == OpCodes.Ldc_I4_2
                    || ins.OpCode == OpCodes.Ldc_I4_3 || ins.OpCode == OpCodes.Ldc_I4_4 || ins.OpCode == OpCodes.Ldc_I4_5
                    || ins.OpCode == OpCodes.Ldc_I4_6 || ins.OpCode == OpCodes.Ldc_I4_7 || ins.OpCode == OpCodes.Ldc_I4_8)
                {
                    continue;
                }

                floats.Clear();
            }
        }

        private static int CountSkill(MethodDefinition method)
        {
            var count = 0;
            foreach (var ins in method.Body.Instructions)
            {
                if ((ins.OpCode == OpCodes.Call || ins.OpCode == OpCodes.Callvirt) && ins.Operand is MethodReference called && called.Name.Contains("MakeMinMax"))
                {
                    count++;
                }
            }

            return count;
        }

        private static AiTrackField Skill(string label, Store store)
        {
            var key = "skill";
            var pair = label;
            if (label.EndsWith(" minimum", StringComparison.Ordinal))
            {
                key = "skill-min";
                pair = label.Substring(0, label.Length - " minimum".Length);
            }
            else if (label.EndsWith(" maximum", StringComparison.Ordinal))
            {
                key = "skill-max";
                pair = label.Substring(0, label.Length - " maximum".Length);
            }
            else if (label.EndsWith(" lap time", StringComparison.Ordinal))
            {
                key = "skill-lap";
                pair = label.Substring(0, label.Length - " lap time".Length);
            }

            var shown = label;
            if (store.Shared)
            {
                shown += " (shared)";
            }

            return new AiTrackField
            {
                Group = "Skill",
                Label = shown,
                Key = key,
                Pair = pair,
                Value = store.Value,
                Vanilla = store.Value,
                Shared = store.Shared,
                FileOffset = store.FileOffset,
                WriteKind = 0
            };
        }

        private static void Take(Dictionary<string, Store> map, List<Store> extras, Store store)
        {
            if (!string.IsNullOrEmpty(store.Tag))
            {
                extras.Add(store);
                return;
            }

            map[store.Field] = store;
        }

        private static void Patch(byte[] bytes, int offset, AiTrackField field)
        {
            if (offset < 0 || offset >= bytes.Length)
            {
                throw new InvalidOperationException("AI value is outside the game file.");
            }

            if (field.Boolean)
            {
                var on = field.BoolValue;
                if (field.WriteKind == 1)
                {
                    var op = bytes[offset];
                    if (op != 0x16 && op != 0x17)
                    {
                        throw new InvalidOperationException("Could not update " + field.Label + ".");
                    }

                    bytes[offset] = (byte)(on ? 0x17 : 0x16);
                }
                else if (field.WriteKind == 2)
                {
                    bytes[offset] = (byte)(on ? 1 : 0);
                }
                else
                {
                    Array.Copy(BitConverter.GetBytes(on ? 1 : 0), 0, bytes, offset, 4);
                }
            }
            else
            {
                if (offset + 4 > bytes.Length)
                {
                    throw new InvalidOperationException("AI value is outside the game file.");
                }

                Array.Copy(BitConverter.GetBytes(field.Value), 0, bytes, offset, 4);
            }
        }

        private static bool Same(Store main, Store extra)
        {
            if (extra.Boolean || main.Boolean)
            {
                return extra.Boolean == main.Boolean && (Math.Abs(extra.Value) > 0.5f) == (Math.Abs(main.Value) > 0.5f);
            }

            return Math.Abs(extra.Value - main.Value) <= 0.0000001f;
        }

        private static Store Copy(Store store, bool shared, string tag)
        {
            return new Store
            {
                Field = store.Field,
                Owner = store.Owner,
                Value = store.Value,
                Boolean = store.Boolean,
                FileOffset = store.FileOffset,
                WriteKind = store.WriteKind,
                Shared = shared,
                Tag = tag
            };
        }

        private static Store Arg(Store source, string field, string tag, string? league)
        {
            return new Store
            {
                Field = field,
                Owner = "AI",
                Value = source.Value,
                FileOffset = source.FileOffset,
                WriteKind = 0,
                Shared = false,
                Tag = string.IsNullOrEmpty(league) ? tag : league
            };
        }

        private static AiTrackField ToField(Name name, Store store)
        {
            var label = name.Label;
            if (!string.IsNullOrEmpty(store.Tag))
            {
                label += ", " + store.Tag;
            }

            if (store.Shared)
            {
                label += " (shared)";
            }

            var on = store.Boolean && Math.Abs(store.Value) > 0.5f;
            return new AiTrackField
            {
                Group = name.Group,
                Label = label,
                Key = name.Field,
                Value = store.Value,
                Vanilla = store.Value,
                Boolean = store.Boolean,
                BoolValue = on,
                FileOffset = store.FileOffset,
                WriteKind = store.WriteKind,
                Shared = store.Shared
            };
        }

        private static bool Keep(Store store)
        {
            if (store.Owner == "Physics")
            {
                return IsDraft(store.Field);
            }

            return store.Owner == "AI" || store.Field == "tire_life_ghost" || store.Field == "tire_life_player";
        }

        private static bool IsDraft(string field)
        {
            return field.IndexOf("draft", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsBool(FieldReference field)
        {
            try
            {
                var resolved = field.Resolve();
                return resolved != null && resolved.FieldType.FullName == "System.Boolean";
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static Store IntStore(Instruction ins, int code, int number)
        {
            var kind = 3;
            var offset = code + ins.Offset + 1;
            if (ins.OpCode == OpCodes.Ldc_I4_0 || ins.OpCode == OpCodes.Ldc_I4_1 || ins.OpCode == OpCodes.Ldc_I4_2
                || ins.OpCode == OpCodes.Ldc_I4_3 || ins.OpCode == OpCodes.Ldc_I4_4 || ins.OpCode == OpCodes.Ldc_I4_5
                || ins.OpCode == OpCodes.Ldc_I4_6 || ins.OpCode == OpCodes.Ldc_I4_7 || ins.OpCode == OpCodes.Ldc_I4_8
                || ins.OpCode == OpCodes.Ldc_I4_M1)
            {
                kind = 1;
                offset = code + ins.Offset;
            }
            else if (ins.OpCode == OpCodes.Ldc_I4_S)
            {
                kind = 2;
            }

            return new Store { Value = number, FileOffset = offset, WriteKind = kind };
        }

        private static bool TryInt(Instruction ins, out int number)
        {
            if (ins.OpCode == OpCodes.Ldc_I4_M1)
            {
                number = -1;
                return true;
            }

            if (ins.OpCode == OpCodes.Ldc_I4_0) { number = 0; return true; }
            if (ins.OpCode == OpCodes.Ldc_I4_1) { number = 1; return true; }
            if (ins.OpCode == OpCodes.Ldc_I4_2) { number = 2; return true; }
            if (ins.OpCode == OpCodes.Ldc_I4_3) { number = 3; return true; }
            if (ins.OpCode == OpCodes.Ldc_I4_4) { number = 4; return true; }
            if (ins.OpCode == OpCodes.Ldc_I4_5) { number = 5; return true; }
            if (ins.OpCode == OpCodes.Ldc_I4_6) { number = 6; return true; }
            if (ins.OpCode == OpCodes.Ldc_I4_7) { number = 7; return true; }
            if (ins.OpCode == OpCodes.Ldc_I4_8) { number = 8; return true; }
            if (ins.OpCode == OpCodes.Ldc_I4_S || ins.OpCode == OpCodes.Ldc_I4)
            {
                number = Convert.ToInt32(ins.Operand, CultureInfo.InvariantCulture);
                return true;
            }

            number = 0;
            return false;
        }

        private static List<Store> Floats(List<Store> pending)
        {
            var list = new List<Store>();
            foreach (var store in pending)
            {
                if (store.WriteKind == 0)
                {
                    list.Add(store);
                }
            }

            return list;
        }

        private static string LeagueName(int id)
        {
            if (id == 0) return "Cup";
            if (id == 1) return "Xfinity";
            if (id == 2) return "Truck";
            if (id == 3) return "Dirt";
            return "League " + id.ToString(CultureInfo.InvariantCulture);
        }

        private static string Nice(string raw)
        {
            raw = raw.Replace('_', ' ').Trim();
            if (raw.Length == 0)
            {
                return raw;
            }

            return char.ToUpper(raw[0], CultureInfo.InvariantCulture) + raw.Substring(1);
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

            throw new InvalidOperationException("Could not map AI code in this DLL.");
        }

        private static readonly TrackAdjust[] Adjusts =
        {
            A("Draft", "60%", "200%", TrackAdjustKind.Percent, "draft_influence"),
            A("Draft length", "Shorter", "Longer", TrackAdjustKind.Scale, "min_draft_lon_carlen", "max_draft_lon_carlen"),
            A("Draft width", "Narrower", "Wider", TrackAdjustKind.Scale, "min_draft_width_carwid", "max_draft_width_carwid"),
            A("Draft speed window", "Narrower", "Wider", TrackAdjustKind.Range, "min_draft_lon_speed_mph", "max_draft_lon_speed_mph"),
            A("Bumps", "Softer", "Harder", TrackAdjustKind.Scale, "bump_force", "bump_aftershock"),
            A("Spin off a bump", "60%", "200%", TrackAdjustKind.Percent, "bump_aftershock_spin_f"),
            A("Crashes", "Softer", "Harder", TrackAdjustKind.Scale, "crash_force", "crash_aftershock"),
            A("Look ahead", "Shorter", "Longer", TrackAdjustKind.Scale, "max_lookahead", "min_lookahead", "min_lookahead_m"),
            A("Speed while looking", "60%", "200%", TrackAdjustKind.Percent, "max_speedfactor_when_checking"),
            A("Speed beside a car", "60%", "200%", TrackAdjustKind.Percent, "nearcar_speedf_at_min_curvature"),
            A("Speed into the pits", "60%", "200%", TrackAdjustKind.Percent, "max_speedfactor_blendpits"),
            A("Grip help", "60%", "200%", TrackAdjustKind.Percent, "magic_grip"),
            A("Grip near the wall", "60%", "200%", TrackAdjustKind.Percent, "wall_approach_lat_grip_f"),
            A("Cornering", "60%", "200%", TrackAdjustKind.Percent, "curvature_skill_f", "curvature_min_skill_f"),
            A("Room to pass", "Tighter", "Wider", TrackAdjustKind.Range, "pass_hys_min", "pass_hys_max"),
            A("Move aside", "60%", "200%", TrackAdjustKind.Percent, "moveout_factor"),
            A("Push", "Softer", "Harder", TrackAdjustKind.Scale, "pushout_factor", "push_sensitivity", "push_lat_sens"),
            A("Move aside under yellow", "60%", "200%", TrackAdjustKind.Percent, "yellow_moveout_f"),
            A("Fast lane", "60%", "200%", TrackAdjustKind.Percent, "fastlane_factor"),
            A("Fast lane under caution", "60%", "200%", TrackAdjustKind.Percent, "fastlane_caution"),
            A("High line ability", "60%", "200%", TrackAdjustKind.Percent, "highline_skill_f"),
            A("Wall gap", "Tighter", "Wider", TrackAdjustKind.Scale, "leftwall", "rightwall"),
            A("Lane margin", "Tighter", "Wider", TrackAdjustKind.Scale, "zone_margin_factor"),
            A("Following distance", "Closer", "Further", TrackAdjustKind.Scale, "cl_forcible_min_dist", "cl_desired_dist", "cl_max_point_dist"),
            A("Pit stops", "More consistent", "Less consistent", TrackAdjustKind.Scale, "pit_skill_variance"),
            A("Fuel saving", "60%", "200%", TrackAdjustKind.Percent, "hog_f", "hog_yellow_factor", "hog_dnf_factor")
        };

        private static readonly Dictionary<string, TrackAdjust> AdjustByKey = BuildAdjusts();

        private static TrackAdjust A(string title, string left, string right, TrackAdjustKind kind, params string[] keys)
        {
            return new TrackAdjust { Title = title, Left = left, Right = right, Kind = kind, Keys = keys };
        }

        private static Dictionary<string, TrackAdjust> BuildAdjusts()
        {
            var map = new Dictionary<string, TrackAdjust>(StringComparer.Ordinal);
            foreach (var adjust in Adjusts)
            {
                foreach (var key in adjust.Keys)
                {
                    map[key] = adjust;
                }
            }

            return map;
        }

        public static TrackAdjust? AdjustFor(string key)
        {
            TrackAdjust adjust;
            return AdjustByKey.TryGetValue(key, out adjust) ? adjust : null;
        }

        public static float Factor(int position)
        {
            var step = 1f + (Math.Abs(position) / 100f);
            return position >= 0 ? step : 1f / step;
        }

        public static int ScalePosition(IReadOnlyList<AiTrackField> fields)
        {
            double strength = 0;
            var count = 0;
            foreach (var field in fields)
            {
                if (Math.Abs(field.Vanilla) < 0.0000001f)
                {
                    continue;
                }

                var ratio = field.Value / field.Vanilla;
                double side;
                if (ratio >= 1f)
                {
                    side = ratio - 1.0;
                }
                else if (ratio > 0.0000001f)
                {
                    side = -((1.0 / ratio) - 1.0);
                }
                else
                {
                    continue;
                }

                strength += side;
                count++;
            }

            if (count == 0)
            {
                return 0;
            }

            var position = (int)Math.Round(strength / count * 100.0);
            if (position < -100)
            {
                return -100;
            }

            if (position > 100)
            {
                return 100;
            }

            return position;
        }

        public static int RangePosition(float low, float high, float vanillaLow, float vanillaHigh)
        {
            var vanilla = Math.Abs(vanillaHigh - vanillaLow);
            var current = Math.Abs(high - low);
            if (vanilla < 0.0000001f)
            {
                return 0;
            }

            var ratio = current / vanilla;
            double side = ratio >= 1f ? ratio - 1.0 : -((1.0 / Math.Max(ratio, 0.0000001f)) - 1.0);
            var position = (int)Math.Round(side * 100.0);
            if (position < -100)
            {
                return -100;
            }

            if (position > 100)
            {
                return 100;
            }

            return position;
        }

        public static int PercentPosition(IReadOnlyList<AiTrackField> fields)
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

            var position = (int)Math.Round(sum / count * 100.0);
            if (position < 60)
            {
                return 60;
            }

            if (position > 200)
            {
                return 200;
            }

            return position;
        }

        public static void ApplyPercent(IReadOnlyList<AiTrackField> fields, int position)
        {
            var factor = position / 100f;
            foreach (var field in fields)
            {
                field.Value = field.Vanilla * factor;
            }
        }

        public static void ApplyScale(IReadOnlyList<AiTrackField> fields, int position)
        {
            var factor = Factor(position);
            foreach (var field in fields)
            {
                field.Value = field.Vanilla * factor;
            }
        }

        public static void ApplyRange(AiTrackField low, AiTrackField high, int position)
        {
            var mid = (low.Vanilla + high.Vanilla) / 2f;
            var half = Math.Abs(high.Vanilla - low.Vanilla) / 2f;
            var span = half * Factor(position);
            var first = low.Vanilla <= high.Vanilla;
            low.Value = first ? mid - span : mid + span;
            high.Value = first ? mid + span : mid - span;
        }

        public static bool RevertOwn(byte[] bytes, string dllPath, string backupPath)
        {
            if (string.IsNullOrEmpty(backupPath) || !File.Exists(backupPath))
            {
                return false;
            }

            byte[] prior;
            List<AiTrackInfo> tracks;
            try
            {
                prior = File.ReadAllBytes(backupPath);
                tracks = Read(dllPath);
            }
            catch (InvalidOperationException)
            {
                return false;
            }
            catch (IOException)
            {
                return false;
            }

            var changed = false;
            foreach (var track in tracks)
            {
                var used = new HashSet<AiTrackField>();
                foreach (var field in track.Fields)
                {
                    if (used.Contains(field))
                    {
                        continue;
                    }

                    if (field.Boolean || field.WriteKind != 0)
                    {
                        changed |= RevertCode(bytes, prior, field);
                        used.Add(field);
                        continue;
                    }

                    var adjust = AdjustFor(field.Key);
                    if (adjust == null)
                    {
                        continue;
                    }

                    var members = new List<AiTrackField>();
                    foreach (var other in track.Fields)
                    {
                        if (other.WriteKind != 0 || other.Boolean)
                        {
                            continue;
                        }

                        var keyHit = false;
                        foreach (var key in adjust.Keys)
                        {
                            if (key == other.Key)
                            {
                                keyHit = true;
                                break;
                            }
                        }

                        if (keyHit)
                        {
                            members.Add(other);
                        }
                    }

                    if (members.Count == 0)
                    {
                        members.Add(field);
                    }

                    foreach (var member in members)
                    {
                        used.Add(member);
                    }

                    if (!RoundTrip(adjust.Kind, members, prior))
                    {
                        continue;
                    }

                    foreach (var member in members)
                    {
                        if (member.FileOffset < 0 || member.FileOffset + 4 > prior.Length || member.FileOffset + 4 > bytes.Length)
                        {
                            continue;
                        }

                        var vanilla = BitConverter.ToSingle(prior, member.FileOffset);
                        Array.Copy(BitConverter.GetBytes(vanilla), 0, bytes, member.FileOffset, 4);
                        foreach (var extra in member.ExtraOffsets)
                        {
                            if (extra < 0 || extra + 4 > bytes.Length || extra + 4 > prior.Length)
                            {
                                continue;
                            }

                            if (Math.Abs(BitConverter.ToSingle(bytes, extra) - member.Value) < 0.0005f)
                            {
                                Array.Copy(BitConverter.GetBytes(BitConverter.ToSingle(prior, extra)), 0, bytes, extra, 4);
                            }
                        }

                        changed = true;
                    }
                }
            }

            return changed;
        }

        private static bool RevertCode(byte[] live, byte[] prior, AiTrackField field)
        {
            var offset = field.FileOffset;
            if (offset < 0 || offset >= live.Length || offset >= prior.Length || live[offset] == prior[offset])
            {
                return false;
            }

            if (field.WriteKind == 1)
            {
                if ((live[offset] != 0x16 && live[offset] != 0x17) || (prior[offset] != 0x16 && prior[offset] != 0x17))
                {
                    return false;
                }
            }
            else if (field.WriteKind == 2)
            {
                if (live[offset] > 1 || prior[offset] > 1)
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

            live[offset] = prior[offset];
            return true;
        }

        private static bool RoundTrip(TrackAdjustKind kind, List<AiTrackField> members, byte[] prior)
        {
            var current = new float[members.Count];
            for (var i = 0; i < members.Count; i++)
            {
                var field = members[i];
                if (field.FileOffset < 0 || field.FileOffset + 4 > prior.Length)
                {
                    return false;
                }

                current[i] = field.Value;
                field.Vanilla = BitConverter.ToSingle(prior, field.FileOffset);
                if (Math.Abs(current[i] - field.Vanilla) < 0.0005f)
                {
                    return false;
                }
            }

            if (kind == TrackAdjustKind.Percent)
            {
                var position = PercentPosition(members);
                ApplyPercent(members, position);
            }
            else if (kind == TrackAdjustKind.Range && members.Count >= 2)
            {
                var position = RangePosition(members[0].Value, members[1].Value, members[0].Vanilla, members[1].Vanilla);
                ApplyRange(members[0], members[1], position);
            }
            else
            {
                var position = ScalePosition(members);
                ApplyScale(members, position);
            }

            var match = true;
            for (var i = 0; i < members.Count; i++)
            {
                if (Math.Abs(members[i].Value - current[i]) > 0.0005f)
                {
                    match = false;
                }

                members[i].Value = current[i];
            }

            return match;
        }
    }
}
