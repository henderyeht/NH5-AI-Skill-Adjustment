using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
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
            const int boxW = 108;
            const int maxCols = 5;
            const int minCol = 340;
            var blocks = new List<(string title, List<AiTrackField> fields)>();
            foreach (var field in fields)
            {
                if (blocks.Count == 0 || blocks[blocks.Count - 1].title != field.Group)
                {
                    blocks.Add((field.Group, new List<AiTrackField>()));
                }

                blocks[blocks.Count - 1].fields.Add(field);
            }

            var top = AddSettingSliders(viewW);
            var scrollW = SystemInformation.VerticalScrollBarWidth;
            var cols = Math.Min(maxCols, Math.Max(1, blocks.Count));
            var fit = Math.Max(1, (viewW - scrollW) / minCol);
            if (fit < cols)
            {
                cols = fit;
            }

            var placed = new List<List<(string title, List<AiTrackField> fields)>>();
            var heights = new int[cols];
            for (var c = 0; c < cols; c++)
            {
                placed.Add(new List<(string, List<AiTrackField>)>());
            }

            var total = 0;
            foreach (var block in blocks)
            {
                total += rowH + (block.fields.Count * rowH);
            }

            var target = Math.Max(rowH, total / cols);
            var col = 0;
            foreach (var block in blocks)
            {
                var need = rowH + (block.fields.Count * rowH);
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

            var usable = contentH > viewH ? viewW - scrollW : viewW;
            var colW = Math.Max(minCol, usable / used);
            var labelW = Math.Max(80, colW - boxW - 28);
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
                    foreach (var field in block.fields)
                    {
                        var scaled = Math.Abs(skillScale - 1f) > 0.0005f
                            && field.Group == "Skill"
                            && field.Label.IndexOf("lap time", StringComparison.OrdinalIgnoreCase) < 0;
                        field.DisplayScale = scaled ? skillScale : 1f;
                        _body.Controls.Add(new Label
                        {
                            Text = field.Label,
                            ForeColor = scaled ? Theme.Red : Theme.White,
                            BackColor = Theme.Asphalt,
                            AutoEllipsis = true,
                            Location = new Point(x + 8, y + 3),
                            Size = new Size(labelW, 20)
                        });
                        Control editor;
                        if (field.Boolean)
                        {
                            var pick = new WheelCombo
                            {
                                DropDownStyle = ComboBoxStyle.DropDownList,
                                Location = new Point(x + 16 + labelW, y),
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
                                Text = (field.Value * field.DisplayScale).ToString("0.####", CultureInfo.InvariantCulture),
                                Location = new Point(x + 16 + labelW, y),
                                Size = new Size(boxW, 22),
                                ForeColor = scaled ? Theme.Red : Color.Black
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

            _body.AutoScrollMinSize = new Size(Math.Max(used * colW, usable), top + contentH + 12);
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

        private List<AiSettingValue> _settingFields = new List<AiSettingValue>();
        private readonly List<SettingEdit> _settingEdits = new List<SettingEdit>();

        private int AddSettingSliders(int viewW)
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

            const int rowH = 36;
            var y = 0;
            foreach (var group in groups)
            {
                var percent = AiSettingValues.PercentFor(group);
                var title = group[0].Group == "Recovery Skill" ? "Recovery" : group[0].Group;
                _body.Controls.Add(new Label
                {
                    Text = title,
                    ForeColor = Theme.Yellow,
                    BackColor = Theme.Asphalt,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    Location = new Point(8, y + 6),
                    Size = new Size(200, 22)
                });
                var readout = new Label
                {
                    Text = percent + "%",
                    ForeColor = Theme.Yellow,
                    BackColor = Theme.Asphalt,
                    Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                    TextAlign = ContentAlignment.MiddleRight,
                    Location = new Point(viewW - 78, y + 6),
                    Size = new Size(64, 22)
                };
                _body.Controls.Add(readout);
                var slider = new SkillSlider
                {
                    Minimum = AiSettingValues.MinPercent,
                    Maximum = AiSettingValues.MaxPercent,
                    Value = percent,
                    Location = new Point(214, y + 2),
                    Size = new Size(Math.Max(120, viewW - 300), 32),
                    BackColor = Theme.Asphalt
                };
                slider.ValueChanged += (_, __) =>
                {
                    readout.Text = slider.Value + "%";
                    _settingsDirty = true;
                };
                _body.Controls.Add(slider);
                _settingEdits.Add(new SettingEdit { Slider = slider, Shown = percent, Fields = group });
                y += rowH;
            }

            return groups.Count == 0 ? 0 : y + 8;
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

                var scale = edit.Slider.Value / 100f;
                foreach (var field in edit.Fields)
                {
                    field.Value = field.Vanilla * scale;
                    changed.Add(field);
                }

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
