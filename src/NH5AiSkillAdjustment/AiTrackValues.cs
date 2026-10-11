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
        public int FileOffset;
        public int WriteKind;
        public List<int> ExtraOffsets = new List<int>();
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
            new Name("draft_influence", "Draft", "Draft strength"),
            new Name("min_draft_lon_speed_mph", "Draft", "Minimum draft speed"),
            new Name("max_draft_lon_speed_mph", "Draft", "Maximum draft speed"),
            new Name("min_draft_lon_carlen", "Draft", "Draft length, close"),
            new Name("max_draft_lon_carlen", "Draft", "Draft length, far"),
            new Name("min_draft_width_carwid", "Draft", "Draft width, narrow"),
            new Name("max_draft_width_carwid", "Draft", "Draft width, wide"),
            new Name("enable_draftbuddy", "Draft", "Draft partners"),
            new Name("only_bump_buddies", "Draft", "Bump only partners"),
            new Name("draftbuddy_max_bump_speed_mph", "Draft", "Partner bump speed"),

            new Name("bump_force", "Bump", "Bump force"),
            new Name("bump_aftershock", "Bump", "Bump aftershock"),
            new Name("bump_aftershock_spin_f", "Bump", "Bump spin"),
            new Name("crash_force", "Bump", "Crash force"),
            new Name("crash_aftershock", "Bump", "Crash aftershock"),

            new Name("pit_speed", "Speed", "Pit speed"),
            new Name("pitentry_max_speed_mph", "Speed", "Pit entry speed"),
            new Name("pitexit_max_speed_mph", "Speed", "Pit exit speed"),
            new Name("max_lookahead", "Speed", "Lookahead, far"),
            new Name("min_lookahead", "Speed", "Lookahead, near"),
            new Name("min_lookahead_m", "Speed", "Lookahead, meters"),
            new Name("max_speedfactor_when_checking", "Speed", "Speed while checking"),
            new Name("max_speedfactor_blendpits", "Speed", "Speed blending into pits"),
            new Name("nearcar_speedf_at_min_curvature", "Speed", "Speed beside another car"),
            new Name("tire_wear_speed_factor", "Speed", "Tire wear speed"),
            new Name("tire_life_ghost", "Speed", "AI tire life"),
            new Name("tire_life_player", "Speed", "Player tire life"),
            new Name("tirewear_25_speed", "Speed", "Tire wear pace"),
            new Name("tirewear_25_lap", "Speed", "Tire wear lap"),
            new Name("magic_grip", "Speed", "Grip assist"),
            new Name("curvature_skill_f", "Speed", "Cornering skill"),
            new Name("curvature_min_skill_f", "Speed", "Cornering skill, minimum"),
            new Name("wall_approach_lat_grip_f", "Speed", "Wall approach grip"),

            new Name("pass_hys_min", "Passing", "Pass gap, minimum"),
            new Name("pass_hys_max", "Passing", "Pass gap, maximum"),
            new Name("pointless_pass_dist", "Passing", "Useless pass distance"),
            new Name("moveout_factor", "Passing", "Move out"),
            new Name("moveout_timer_f", "Passing", "Move out timer"),
            new Name("pushout_factor", "Passing", "Push out"),
            new Name("push_sensitivity", "Passing", "Push"),
            new Name("push_lat_sens", "Passing", "Side push"),
            new Name("checks", "Passing", "Checks"),
            new Name("merge_t", "Passing", "Merge time"),
            new Name("merge_dt", "Passing", "Merge step"),
            new Name("passout_greenflag_fadein_duration", "Passing", "Green flag settle"),
            new Name("yellow_moveout_f", "Passing", "Yellow move out"),
            new Name("ym_restart_intercar", "Passing", "Restart gap"),
            new Name("ym_restart_offset", "Passing", "Restart offset"),
            new Name("enable_threewide_nothrottle", "Passing", "Three wide without throttle"),

            new Name("fastlane_factor", "Racing line", "Fast lane"),
            new Name("fastlane_caution", "Racing line", "Fast lane on caution"),
            new Name("highline_enabled", "Racing line", "High line"),
            new Name("highline_skill_f", "Racing line", "High line skill"),
            new Name("highline_wall_margin", "Racing line", "High line wall gap"),
            new Name("highline_inhibited_after_greenflag_t", "Racing line", "High line delay after green"),
            new Name("highline_decide_on_back_straight", "Racing line", "High line on the backstretch"),
            new Name("force_lane1_entry", "Racing line", "Bottom lane at entry"),
            new Name("force_lane1_exit", "Racing line", "Bottom lane at exit"),
            new Name("leftwall", "Racing line", "Left wall margin"),
            new Name("rightwall", "Racing line", "Right wall margin"),
            new Name("prohibit_lane0_lon", "Racing line", "Bottom lane block"),
            new Name("zone_margin_factor", "Racing line", "Lane margin"),
            new Name("contact_extra_left_lanes", "Racing line", "Extra left lanes"),
            new Name("leftman_never_forces", "Racing line", "Left car never forces"),
            new Name("cl_desired_dist", "Racing line", "Preferred following distance"),
            new Name("cl_forcible_min_dist", "Racing line", "Closest follow distance"),
            new Name("cl_max_point_dist", "Racing line", "Furthest follow distance"),

            new Name("can_pit", "Pit", "Can pit"),
            new Name("fuel_per_lap", "Pit", "Fuel per lap"),
            new Name("laps_per_tank", "Pit", "Laps per tank"),
            new Name("pit_offset1", "Pit", "Pit offset"),
            new Name("pit_offset2", "Pit", "Pit offset 2"),
            new Name("pit_turnin", "Pit", "Pit turn-in"),
            new Name("pit_side", "Pit", "Pit side"),
            new Name("pit_side_2", "Pit", "Pit side 2"),
            new Name("pit_over", "Pit", "Pit overshoot"),
            new Name("pit_min_lookahead", "Pit", "Pit lookahead"),
            new Name("pitfork_edge_time", "Pit", "Pit fork edge"),
            new Name("pitfork_max_time", "Pit", "Pit fork time"),
            new Name("pitlane_cnt1", "Pit", "Pit lane count"),
            new Name("pitlane_cnt2", "Pit", "Pit lane count 2"),
            new Name("pit_lane_width_f", "Pit", "Pit lane width"),
            new Name("pit_lane_entry_lat", "Pit", "Pit lane entry"),
            new Name("pit_lane_exit_lat", "Pit", "Pit lane exit"),
            new Name("pit_lane1_max_nlon", "Pit", "Pit lane 1 length"),
            new Name("pit_lane2_max_nlon", "Pit", "Pit lane 2 length"),
            new Name("pit_lane3_max_nlon", "Pit", "Pit lane 3 length"),
            new Name("postpits_dontmind_t", "Pit", "After pit, don't block"),
            new Name("ontrack_pitting_lat_offset", "Pit", "On-track pit offset"),
            new Name("enable_pitstall_departure_jets", "Pit", "Pit stall launch"),
            new Name("pit_skill_variance", "Pit", "Pit skill spread"),
            new Name("hog_f", "Pit", "Fuel saving"),
            new Name("hog_yellow_factor", "Pit", "Fuel saving under yellow"),
            new Name("hog_dnf_factor", "Pit", "Fuel saving, out of race")
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

            var bak = dllPath + ".nh5ai-stock.bak";
            if (!File.Exists(bak))
            {
                File.Copy(dllPath, bak);
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
            return new AiTrackField
            {
                Group = "Skill",
                Label = label,
                Value = store.Value,
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
                Value = store.Value,
                Boolean = store.Boolean,
                BoolValue = on,
                FileOffset = store.FileOffset,
                WriteKind = store.WriteKind
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
    }
}
