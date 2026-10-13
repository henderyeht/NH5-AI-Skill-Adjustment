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

        public bool Pending { get; private set; }

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
            _body = new Panel { Location = new Point(0, 44), Size = new Size(960, 400), BackColor = Theme.Asphalt, AutoScroll = true };
            Controls.Add(_body);
            Resize += (_, __) => ShowTrack();
        }

        public void LoadFrom(string? dll)
        {
            _dll = dll;
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

                _status.ForeColor = Theme.Mute;
                _status.Text = "Shared means every track on that setup. On/Off is a switch. Cup, Truck, Xfinity, and Next Gen show up when they differ.";
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
            Pending = false;
            try
            {
                ShowTrackCore();
            }
            finally
            {
                _laying = false;
                Pending = false;
            }
        }

        private void ShowTrackCore()
        {
            var viewW = Math.Max(360, ClientSize.Width);
            var viewH = Math.Max(120, ClientSize.Height - 44);
            if (_shownTrack == _track.SelectedIndex && _shownView.Width == viewW && _shownView.Height == viewH && _body.Controls.Count > 0)
            {
                return;
            }

            _body.Controls.Clear();
            _edits.Clear();
            if (_track.SelectedIndex < 0 || _track.SelectedIndex >= _tracks.Count)
            {
                return;
            }

            var fields = _tracks[_track.SelectedIndex].Fields;
            _body.AutoScroll = true;
            _body.SetBounds(0, 44, viewW, viewH);
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
                var y = 0;
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
                        if (field.Boolean)
                        {
                            var pick = new WheelCombo
                            {
                                DropDownStyle = ComboBoxStyle.DropDownList,
                                Location = new Point(x + 16 + labelW, y),
                                Size = new Size(boxW, 22),
                                ForeColor = Theme.White
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
                                Location = new Point(x + 16 + labelW, y),
                                Size = new Size(boxW, 22),
                                BackColor = Theme.Asphalt,
                                ForeColor = Theme.White,
                                BorderStyle = BorderStyle.None
                            };
                        }

            editor.TextChanged += (_, __) => Pending = true;
            if (editor is ComboBox trackPick)
            {
                trackPick.SelectedIndexChanged += (_, __) => Pending = true;
            }

            _body.Controls.Add(editor);
            _edits.Add((editor, field));
                        y += rowH;
                    }
                }
            }

            _body.AutoScrollMinSize = new Size(Math.Max(used * colW, usable), contentH + 12);
            _shownTrack = _track.SelectedIndex;
            _shownView = new Size(viewW, viewH);
        }

        private void ApplyTrack()
        {
            if (string.IsNullOrEmpty(_dll))
            {
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

                if (Math.Abs(value - edit.field.Value) > 0.0000001f)
                {
                    changed.Add(new AiTrackField
                    {
                        Label = edit.field.Label,
                        Value = value,
                        FileOffset = edit.field.FileOffset,
                        WriteKind = edit.field.WriteKind,
                        ExtraOffsets = edit.field.ExtraOffsets
                    });
                }
            }

            if (changed.Count == 0)
            {
                _status.ForeColor = Theme.Mute;
                _status.Text = "No track values changed.";
                Pending = false;
                return;
            }

            try
            {
                AiTrackValues.Write(_dll ?? "", changed);
                LoadFrom(_dll);
                _status.ForeColor = Theme.Yellow;
                _status.Text = "Applied " + changed.Count + " track value" + (changed.Count == 1 ? "" : "s") + ".";
            }
            catch (Exception ex)
            {
                _status.ForeColor = Theme.Conflict;
                _status.Text = ex.Message;
            }
        }
    }
}
