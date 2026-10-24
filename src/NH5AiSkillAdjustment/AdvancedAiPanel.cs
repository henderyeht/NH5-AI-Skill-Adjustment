using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;

namespace NH5AiSkillAdjustment
{
    internal sealed class AdvancedAiPanel : Panel
    {
        private readonly ComboBox _track;
        private readonly Panel _body;
        private readonly Label _status;
        private readonly List<AiTrackInfo> _tracks = new List<AiTrackInfo>();
        private readonly List<(Control editor, AiTrackField field)> _edits = new List<(Control, AiTrackField)>();
        private int _shownTrack = -2;
        private Size _shownView;
        private bool _laying;
        private string? _dll;

        private bool _trackPending;
        private bool _settingsDirty;
        public bool Pending => _trackPending || _settingsDirty;

        public bool TryApply()
        {
            ApplyTrack();
            return !Pending;
        }

        public AdvancedAiPanel()
        {
            BackColor = Theme.Asphalt;
            Controls.Add(new Label
            {
                Text = "Track",
                ForeColor = Theme.Yellow,
                BackColor = Theme.Asphalt,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(0, 4),
                Size = new Size(52, 22)
            });
            _track = new WheelCombo
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(56, 0),
                Size = new Size(260, 24)
            };
            _track.SelectedIndexChanged += (_, __) => ShowTrack();
            Controls.Add(_track);
            var apply = new Button
            {
                Text = "APPLY",
                Location = new Point(332, 0),
                Size = new Size(110, 26),
                BackColor = Theme.Red,
                ForeColor = Theme.White,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Font = new Font("Impact", 16, FontStyle.Regular, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand
            };
            apply.FlatAppearance.BorderSize = 0;
            apply.Click += (_, __) =>
            {
                if (MessageBox.Show(FindForm(), "Are you sure you want to Apply?", "NASCAR Heat 5 AI Skill Utility", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    ApplyTrack();
                }
            };
            Controls.Add(apply);
            _status = new Label
            {
                ForeColor = Theme.Mute,
                BackColor = Theme.Asphalt,
                Location = new Point(456, 4),
                Size = new Size(700, 28)
            };
            Controls.Add(_status);
            _body = new Panel { Location = new Point(0, 36), Size = new Size(960, 400), BackColor = Theme.Asphalt, AutoScroll = true };
            Controls.Add(_body);
            Resize += (_, __) => ShowTrack();
        }

        public void LoadFrom(string? dll)
        {
            _dll = dll;
            LoadSettings(dll);
            var keep = _track.SelectedItem as string;
            _shownTrack = -2;
            _tracks.Clear();
            _track.Items.Clear();
            _body.Controls.Clear();
            _edits.Clear();
            if (string.IsNullOrEmpty(dll))
            {
                _status.ForeColor = Theme.Yellow;
                _status.Text = "Locate the game before editing a track.";
                return;
            }

            try
            {
                _tracks.AddRange(AiTrackValues.Read(dll ?? ""));
                StampVanilla(dll ?? "");
                foreach (var track in _tracks)
                {
                    _track.Items.Add(track.Name);
                }

                var index = 0;
                if (!string.IsNullOrEmpty(keep))
                {
                    var found = _track.Items.IndexOf(keep);
                    if (found >= 0)
                    {
                        index = found;
                    }
                }

                if (_track.Items.Count > 0)
                {
                    _track.SelectedIndex = index;
                }

                _status.Text = "";
            }
            catch (Exception ex)
            {
                _status.ForeColor = Theme.Conflict;
                _status.Text = ex.Message;
            }
        }

        private void ShowTrack()
        {
            if (_laying)
            {
                return;
            }

            _laying = true;
            _trackPending = false;
            try
            {
                ShowTrackCore();
            }
            finally
            {
                _laying = false;
            }
        }

        private void ShowTrackCore()
        {
            var viewW = Math.Max(360, ClientSize.Width);
            var bodyTop = 36;
            var viewH = Math.Max(120, ClientSize.Height - bodyTop);
            if (_shownTrack == _track.SelectedIndex && _shownView.Width == viewW && _shownView.Height == viewH && _body.Controls.Count > 0)
            {
                return;
            }

            _body.Controls.Clear();
            _edits.Clear();
            _settingEdits.Clear();
            _trackSlides.Clear();
            if (_track.SelectedIndex < 0 || _track.SelectedIndex >= _tracks.Count)
            {
                return;
            }

            var fields = _tracks[_track.SelectedIndex].Fields;
            var skillScale = 1f;
            var dll = _dll;
            if (!string.IsNullOrEmpty(dll))
            {
                try
                {
                    var extra = new AiSkillPatcher(dll!).Resolve().Extra;
                    if (Math.Abs(extra - 1f) > 0.0005f)
                    {
                        skillScale = extra;
                    }
                }
                catch (InvalidOperationException)
                {
                }
            }

            _body.AutoScroll = true;
            _body.SetBounds(0, bodyTop, viewW, viewH);
            const int rowH = 26;
            const int boxW = 96;
            const int colW = 400;
            var blocks = new List<(string title, List<AiTrackField> fields)>();
            foreach (var field in fields)
            {
                if (blocks.Count == 0 || blocks[blocks.Count - 1].title != field.Group)
                {
                    blocks.Add((field.Group, new List<AiTrackField>()));
                }

                blocks[blocks.Count - 1].fields.Add(field);
            }

            blocks.Insert(0, ("\u0001", new List<AiTrackField>()));
            var top = 0;
            var scrollW = SystemInformation.VerticalScrollBarWidth;
            var room = Math.Max(colW, viewW - scrollW);
            var cols = Math.Min(blocks.Count, Math.Max(1, room / colW));

            var placed = new List<List<(string title, List<AiTrackField> fields)>>();
            var heights = new int[cols];
            for (var c = 0; c < cols; c++)
            {
                placed.Add(new List<(string, List<AiTrackField>)>());
            }

            var total = 0;
            foreach (var block in blocks)
            {
                total += BlockHeight(block, skillScale, rowH);
            }

            var target = Math.Max(rowH, total / cols);
            var col = 0;
            foreach (var block in blocks)
            {
                var need = BlockHeight(block, skillScale, rowH);
                if (col < cols - 1 && heights[col] > 0 && heights[col] + need > target)
                {
                    col++;
                }

                placed[col].Add(block);
                heights[col] += need;
            }

            var contentH = 0;
            var used = 0;
            for (var c = 0; c < cols; c++)
            {
                if (placed[c].Count > 0)
                {
                    used++;
                }

                if (heights[c] > contentH)
                {
                    contentH = heights[c];
                }
            }

            if (used < 1)
            {
                used = 1;
            }

            var labelFlags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            var shown = 0;
            for (var c = 0; c < placed.Count; c++)
            {
                if (placed[c].Count == 0)
                {
                    continue;
                }

                var x = shown * colW;
                shown++;
                var y = top;
                foreach (var block in placed[c])
                {
                    if (block.title == "\u0001")
                    {
                        y = AddSettingSliders(x, y, colW);
                        continue;
                    }

                    _body.Controls.Add(new Label
                    {
                        Text = block.title,
                        ForeColor = Theme.Yellow,
                        BackColor = Theme.Asphalt,
                        Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                        Location = new Point(x + 8, y + 2),
                        Size = new Size(colW - 16, 20)
                    });
                    y += rowH;
                    foreach (var row in Rows(block.fields, skillScale))
                    {
                        if (row.Adjust != null)
                        {
                            y = AddTrackSlider(row, x, y, colW);
                            continue;
                        }

                        var field = row.Field;
                        field.DisplayScale = 1f;
                        var textW = TextRenderer.MeasureText(field.Label, Font, new Size(int.MaxValue, int.MaxValue), labelFlags).Width + 2;
                        var labelW = Math.Min(textW, Math.Max(72, colW - boxW - 22));
                        _body.Controls.Add(new Label
                        {
                            Text = field.Label,
                            ForeColor = Theme.White,
                            BackColor = Theme.Asphalt,
                            AutoEllipsis = true,
                            Location = new Point(x + 8, y + 3),
                            Size = new Size(labelW, 20)
                        });
                        Control editor;
                        var editorX = x + 8 + labelW + 6;
                        if (field.Boolean)
                        {
                            var pick = new WheelCombo
                            {
                                DropDownStyle = ComboBoxStyle.DropDownList,
                                Location = new Point(editorX, y),
                                Size = new Size(boxW, 22)
                            };
                            pick.Items.Add("On");
                            pick.Items.Add("Off");
                            pick.SelectedIndex = field.BoolValue ? 0 : 1;
                            editor = pick;
                        }
                        else
                        {
                            editor = new WheelTextBox
                            {
                                Text = field.Value.ToString("0.####", CultureInfo.InvariantCulture),
                                Location = new Point(editorX, y),
                                Size = new Size(boxW, 22)
                            };
                        }

                        editor.TextChanged += (_, __) => _trackPending = true;
                        if (editor is ComboBox trackPick)
                        {
                            trackPick.SelectedIndexChanged += (_, __) => _trackPending = true;
                        }

                        _body.Controls.Add(editor);
                        _edits.Add((editor, field));
                        y += rowH;
                    }
                }
            }

            _body.AutoScrollMinSize = new Size(used * colW, top + contentH + 12);
            _shownTrack = _track.SelectedIndex;
            _shownView = new Size(viewW, viewH);
            _settingsDirty = false;
        }

        private sealed class SettingEdit
        {
            public SkillSlider Slider = null!;
            public int Shown;
            public List<AiSettingValue> Fields = null!;
        }

        private sealed class TrackRow
        {
            public AiTrackField Field = null!;
            public TrackAdjust Adjust = null!;
            public List<AiTrackField> Members = new List<AiTrackField>();
            public int Position;
            public bool Red;
            public int Height = 26;
        }

        private sealed class TrackSlide
        {
            public SkillSlider Slider = null!;
            public int Shown;
            public TrackAdjustKind Kind;
            public List<AiTrackField> Members = new List<AiTrackField>();
        }

        private List<AiSettingValue> _settingFields = new List<AiSettingValue>();
        private readonly List<SettingEdit> _settingEdits = new List<SettingEdit>();
        private readonly List<TrackSlide> _trackSlides = new List<TrackSlide>();

        private int BlockHeight((string title, List<AiTrackField> fields) block, float skillScale, int rowH)
        {
            if (block.title == "\u0001")
            {
                return SettingGroups().Count * 58;
            }

            var height = rowH;
            foreach (var row in Rows(block.fields, skillScale))
            {
                height += row.Height;
            }

            return height;
        }

        private List<List<AiSettingValue>> SettingGroups()
        {
            var groups = new List<List<AiSettingValue>>();
            foreach (var field in _settingFields)
            {
                if (groups.Count == 0 || groups[groups.Count - 1][0].Group != field.Group)
                {
                    groups.Add(new List<AiSettingValue>());
                }

                groups[groups.Count - 1].Add(field);
            }

            return groups;
        }

        private static string SharedTitle(string title, params AiTrackField?[] members)
        {
            foreach (var member in members)
            {
                if (member != null && member.Shared)
                {
                    return title + " (shared)";
                }
            }

            return title;
        }

        private int AddSettingSliders(int x, int y, int colW)
        {
            foreach (var group in SettingGroups())
            {
                string title;
                string left;
                string right;
                AiSettingValues.Ends(group[0].Group, out title, out left, out right);
                var position = AiSettingValues.PositionFor(group);
                SkillSlider slider;
                y = AddLabeledSlider(title, left, right, false, false, position, -100, 100, x, y, colW, () => _settingsDirty = true, out slider);
                _settingEdits.Add(new SettingEdit { Slider = slider, Shown = position, Fields = group });
            }

            return y;
        }

        private static List<TrackRow> Rows(List<AiTrackField> fields, float skillScale)
        {
            var rows = new List<TrackRow>();
            var used = new HashSet<string>(StringComparer.Ordinal);
            var red = Math.Abs(skillScale - 1f) > 0.0005f;
            foreach (var field in fields)
            {
                if (field.Key == "skill-max" || used.Contains(field.Key))
                {
                    continue;
                }

                if (field.Key == "skill-min")
                {
                    AiTrackField? high = null;
                    foreach (var other in fields)
                    {
                        if (other.Key == "skill-max" && other.Pair == field.Pair)
                        {
                            high = other;
                            break;
                        }
                    }

                    if (high != null)
                    {
                        rows.Add(new TrackRow
                        {
                            Adjust = new TrackAdjust
                            {
                                Title = SharedTitle(field.Pair + " skill range", field, high),
                                Left = "Tighter",
                                Right = "Wider",
                                Kind = TrackAdjustKind.Range,
                                Keys = new[] { "skill-min", "skill-max" }
                            },
                            Members = new List<AiTrackField> { field, high },
                            Position = AiTrackValues.RangePosition(field.Value, high.Value, field.Vanilla, high.Vanilla),
                            Red = red,
                            Height = 58
                        });
                        continue;
                    }
                }

                var adjust = AiTrackValues.AdjustFor(field.Key);
                if (adjust != null)
                {
                    var members = new List<AiTrackField>();
                    foreach (var other in fields)
                    {
                        if (Array.IndexOf(adjust.Keys, other.Key) >= 0)
                        {
                            members.Add(other);
                            used.Add(other.Key);
                        }
                    }

                    var position = adjust.Kind == TrackAdjustKind.Range && members.Count >= 2
                        ? AiTrackValues.RangePosition(members[0].Value, members[1].Value, members[0].Vanilla, members[1].Vanilla)
                        : adjust.Kind == TrackAdjustKind.Percent
                            ? AiTrackValues.PercentPosition(members)
                            : AiTrackValues.ScalePosition(members);
                    rows.Add(new TrackRow
                    {
                        Adjust = adjust,
                        Members = members,
                        Position = position,
                        Height = 58
                    });
                    continue;
                }

                rows.Add(new TrackRow { Field = field, Height = 26 });
            }

            return rows;
        }

        private int AddTrackSlider(TrackRow row, int x, int y, int colW)
        {
            var adjust = row.Adjust;
            var percent = adjust.Kind == TrackAdjustKind.Percent;
            SkillSlider slider;
            y = AddLabeledSlider(
                SharedTitle(adjust.Title, row.Members.ToArray()),
                adjust.Left,
                adjust.Right,
                percent,
                row.Red,
                row.Position,
                percent ? 60 : -100,
                percent ? 200 : 100,
                x,
                y,
                colW,
                () => _trackPending = true,
                out slider);
            _trackSlides.Add(new TrackSlide
            {
                Slider = slider,
                Shown = row.Position,
                Kind = adjust.Kind,
                Members = row.Members
            });
            return y;
        }

        private int AddLabeledSlider(string title, string left, string right, bool percent, bool red, int position, int minimum, int maximum, int x, int y, int colW, Action onChange, out SkillSlider slider)
        {
            const int pad = 8;
            const int gap = 6;
            var flags = TextFormatFlags.NoPadding | TextFormatFlags.SingleLine;
            var titleFont = new Font("Segoe UI", 9f, FontStyle.Bold);
            var endFont = new Font("Segoe UI", 9f);
            var titleW = TextRenderer.MeasureText(title, titleFont, new Size(int.MaxValue, int.MaxValue), flags).Width;
            var leftW = TextRenderer.MeasureText(left, endFont, new Size(int.MaxValue, int.MaxValue), flags).Width + 2;
            var rightW = TextRenderer.MeasureText(right, endFont, new Size(int.MaxValue, int.MaxValue), flags).Width + 2;
            _body.Controls.Add(new Label
            {
                Text = title,
                ForeColor = red ? Theme.Red : Theme.Yellow,
                BackColor = Theme.Asphalt,
                Font = titleFont,
                AutoEllipsis = true,
                Location = new Point(x + pad, y + 1),
                Size = new Size(Math.Max(1, titleW + 4), 16)
            });
            Label? readout = null;
            if (percent)
            {
                readout = new Label
                {
                    Text = position + "%",
                    ForeColor = red ? Theme.Red : Theme.Yellow,
                    BackColor = Theme.Asphalt,
                    Font = titleFont,
                    Location = new Point(x + pad + titleW + 10, y + 1),
                    Size = new Size(48, 16)
                };
                _body.Controls.Add(readout);
            }

            _body.Controls.Add(new Label
            {
                Text = left,
                ForeColor = Theme.White,
                BackColor = Theme.Asphalt,
                Font = endFont,
                Location = new Point(x + pad, y + 28),
                Size = new Size(leftW, 20)
            });
            const int railMax = 150;
            var railX = x + pad + leftW + gap;
            var railRoom = colW - pad - leftW - gap - rightW - gap - pad;
            var railW = Math.Max(72, Math.Min(railMax, railRoom));
            _body.Controls.Add(new Label
            {
                Text = right,
                ForeColor = Theme.White,
                BackColor = Theme.Asphalt,
                Font = endFont,
                Location = new Point(railX + railW + gap, y + 28),
                Size = new Size(rightW, 20)
            });
            slider = new SkillSlider
            {
                ThumbWidth = 17,
                ColorFromNeutral = true,
                Neutral = percent ? 100 : 0,
                FromCenter = !percent,
                CenterLabel = percent ? "" : "Vanilla",
                Minimum = minimum,
                Maximum = maximum,
                Value = position,
                Location = new Point(railX, y + 16),
                Size = new Size(railW, 40),
                BackColor = Theme.Asphalt
            };
            var box = readout;
            var changed = onChange;
            var knob = slider;
            knob.ValueChanged += (_, __) =>
            {
                if (box != null)
                {
                    box.Text = knob.Value + "%";
                }

                changed();
            };
            _body.Controls.Add(slider);
            return y + 58;
        }

        private void StampVanilla(string dll)
        {
            var folder = Path.GetDirectoryName(dll);
            if (string.IsNullOrEmpty(folder))
            {
                return;
            }

            var bak = Path.Combine(folder, Path.GetFileName(dll) + ".nh5ai-stock.bak");
            if (!File.Exists(bak))
            {
                return;
            }

            var bytes = File.ReadAllBytes(bak);
            foreach (var track in _tracks)
            {
                foreach (var field in track.Fields)
                {
                    if (field.FileOffset < 0 || field.FileOffset + 4 > bytes.Length)
                    {
                        continue;
                    }

                    field.Vanilla = BitConverter.ToSingle(bytes, field.FileOffset);
                }
            }
        }

        private void LoadSettings(string? dll)
        {
            _settingFields = new List<AiSettingValue>();
            _settingsDirty = false;
            if (string.IsNullOrEmpty(dll))
            {
                return;
            }

            try
            {
                _settingFields = AiSettingValues.Read(dll ?? "");
            }
            catch (Exception)
            {
            }
        }

        private bool SaveSettings()
        {
            if (_settingEdits.Count == 0 || string.IsNullOrEmpty(_dll))
            {
                _settingsDirty = false;
                return false;
            }

            var changed = new List<AiSettingValue>();
            foreach (var edit in _settingEdits)
            {
                if (edit.Slider.Value == edit.Shown)
                {
                    continue;
                }

                AiSettingValues.Apply(edit.Fields, edit.Slider.Value);
                changed.AddRange(edit.Fields);

                edit.Shown = edit.Slider.Value;
            }

            if (changed.Count == 0)
            {
                _settingsDirty = false;
                return false;
            }

            var dll = _dll ?? "";
            new AiSkillPatcher(dll).KeepRestoreCopy();
            AiSettingValues.Write(dll, changed);
            _settingsDirty = false;
            return true;
        }

        private void ApplyTrack()
        {
            var settingsSaved = false;
            try
            {
                settingsSaved = SaveSettings();
            }
            catch (Exception ex)
            {
                _status.ForeColor = Theme.Conflict;
                _status.Text = ex.Message;
                return;
            }

            if (string.IsNullOrEmpty(_dll))
            {
                if (settingsSaved)
                {
                    _status.ForeColor = Theme.Yellow;
                    _status.Text = "Saved AI settings.";
                }

                return;
            }

            var changed = new List<AiTrackField>();
            foreach (var slide in _trackSlides)
            {
                if (slide.Slider.Value == slide.Shown)
                {
                    continue;
                }

                if (slide.Kind == TrackAdjustKind.Percent)
                {
                    AiTrackValues.ApplyPercent(slide.Members, slide.Slider.Value);
                }
                else if (slide.Kind == TrackAdjustKind.Range && slide.Members.Count >= 2)
                {
                    AiTrackValues.ApplyRange(slide.Members[0], slide.Members[1], slide.Slider.Value);
                }
                else
                {
                    AiTrackValues.ApplyScale(slide.Members, slide.Slider.Value);
                }

                changed.AddRange(slide.Members);
                slide.Shown = slide.Slider.Value;
            }

            foreach (var edit in _edits)
            {
                if (edit.field.Boolean)
                {
                    var pick = (ComboBox)edit.editor;
                    var on = pick.SelectedIndex == 0;
                    if (on != edit.field.BoolValue)
                    {
                        changed.Add(new AiTrackField
                        {
                            Label = edit.field.Label,
                            Boolean = true,
                            BoolValue = on,
                            Value = on ? 1f : 0f,
                            FileOffset = edit.field.FileOffset,
                            WriteKind = edit.field.WriteKind,
                            ExtraOffsets = edit.field.ExtraOffsets
                        });
                    }

                    continue;
                }

                var box = (TextBox)edit.editor;
                if (!float.TryParse(box.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
                {
                    _status.ForeColor = Theme.Conflict;
                    _status.Text = "Could not read " + edit.field.Label + ".";
                    return;
                }

                var scale = edit.field.DisplayScale;
                if (scale < 0.0001f)
                {
                    scale = 1f;
                }

                var baseline = edit.field.Value * scale;
                var epsilon = Math.Abs(scale - 1f) > 0.0005f ? 0.00005f : 0.0000001f;
                if (Math.Abs(value - baseline) > epsilon)
                {
                    changed.Add(new AiTrackField
                    {
                        Label = edit.field.Label,
                        Value = value / scale,
                        FileOffset = edit.field.FileOffset,
                        WriteKind = edit.field.WriteKind,
                        ExtraOffsets = edit.field.ExtraOffsets
                    });
                }
            }

            if (changed.Count == 0)
            {
                _trackPending = false;
                if (settingsSaved)
                {
                    _status.ForeColor = Theme.Yellow;
                    _status.Text = "Saved AI settings.";
                    return;
                }

                _status.ForeColor = Theme.Mute;
                _status.Text = "No track values changed.";
                return;
            }

            try
            {
                AiTrackValues.Write(_dll ?? "", changed);
                LoadFrom(_dll);
                _status.ForeColor = Theme.Yellow;
                _status.Text = "Applied " + changed.Count + " track value" + (changed.Count == 1 ? "" : "s") + "." + (settingsSaved ? " Saved AI settings." : "");
            }
            catch (Exception ex)
            {
                _status.ForeColor = Theme.Conflict;
                _status.Text = ex.Message;
            }
        }
    }
}
