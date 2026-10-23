using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace NH5AiSkillAdjustment
{
    internal sealed class MainForm : Form
    {
        private const int HeaderHeight = 110;
        private readonly SkillSlider _slider;
        private readonly SkillSlider _rating;
        private readonly Label _ratingValue;
        private readonly Label _value;
        private readonly Label _status;
        private readonly Label _path;
        private readonly Icon _icon;
        private readonly Image? _logo;
        private readonly Image? _banner;
        private string? _managedDir;
        private int _ceiling = AiSkillPatcher.MinCeiling;
        private readonly ComboBox _mode;
        private readonly Label _settings;
        private readonly AdvancedAiPanel _advanced;
        private bool _loading;
        private bool _dirty;

        private bool Ask(string text)
        {
            return MessageBox.Show(this, text, Text, MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
        }
        private readonly List<Control> _basic = new List<Control>();

        public MainForm()
        {
            Text = "NASCAR Heat 5 AI Skill Utility";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(520, 820);
            BackColor = Theme.Asphalt;
            ForeColor = Theme.White;
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9.5f);
            _logo = LoadImage("NH5AiSkillAdjustment.heat5-logo.png");
            _banner = LoadImage("NH5AiSkillAdjustment.banner-header.jpg");
            _icon = LoadIcon();
            Icon = _icon;

            Paint += (_, e) => DrawHeader(e.Graphics);

            _settings = new Label
            {
                Text = "Settings",
                ForeColor = Theme.Yellow,
                BackColor = Theme.Asphalt,
                Font = new Font("Segoe UI", 10f, FontStyle.Bold),
                Location = new Point(20, HeaderHeight + 22),
                Size = new Size(78, 22)
            };
            Controls.Add(_settings);
            _mode = new WheelCombo
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Location = new Point(102, HeaderHeight + 18),
                Size = new Size(140, 24)
            };
            _mode.Items.Add("Basic");
            _mode.Items.Add("Advanced");
            _mode.SelectedIndex = 0;
            _mode.SelectedIndexChanged += (_, __) => ShowMode();
            Controls.Add(_mode);
            _advanced = new AdvancedAiPanel { Visible = false };
            Controls.Add(_advanced);

            var instructionPanel = new Panel
            {
                Location = new Point(20, HeaderHeight + 58),
                Size = new Size(480, 210),
                BackColor = Theme.Asphalt,
                Padding = new Padding(12, 10, 12, 10)
            };
            instructionPanel.Paint += (_, e) => Theme.PaintLeftAccent(e.Graphics, instructionPanel.Height);
            instructionPanel.Controls.Add(new Label
            {
                Dock = DockStyle.Fill,
                AutoSize = false,
                ForeColor = Theme.White,
                BackColor = Theme.Asphalt,
                Padding = new Padding(4, 0, 0, 0),
                Text = "Close NASCAR Heat 5 before making changes.\r\n"
                     + "\r\n"
                     + "Use the slider to adjust global AI strength.\r\n"
                     + "100% = stock/vanilla strength.\r\n"
                     + "200% = double the stock AI strength.\r\n"
                     + "\r\n"
                     + "The lower slider is the AI difficulty from the Heat settings menu (85-105).\r\n"
                     + "The top slider is the multiplier. 100% leaves that setting as-is.\r\n"
                     + "\r\n"
                     + "RESTORE returns all AI values to their original stock/vanilla settings."
            });
            Controls.Add(instructionPanel);

            _value = new Label
            {
                AutoSize = false,
                Location = new Point(20, 390),
                Size = new Size(480, 48),
                Font = new Font("Impact", 36, FontStyle.Regular, GraphicsUnit.Pixel),
                ForeColor = Theme.Yellow,
                BackColor = Theme.Asphalt,
                TextAlign = ContentAlignment.MiddleCenter,
                Text = "100%"
            };
            Controls.Add(_value);

            _slider = new SkillSlider
            {
                Location = new Point(28, 442),
                Size = new Size(464, 52),
                BackColor = Theme.Asphalt,
                Minimum = AiSkillPatcher.MinStrength,
                Maximum = AiSkillPatcher.MaxStrength,
                Value = AiSkillPatcher.VanillaCustom
            };
            _slider.ValueChanged += (_, __) =>
            {
                _value.Text = _slider.Value.ToString() + "%";
                if (!_loading)
                {
                    _dirty = true;
                }
            };
            Controls.Add(_slider);

            Controls.Add(new Label
            {
                Text = "60%",
                Location = new Point(28, 498),
                Size = new Size(40, 18),
                ForeColor = Theme.Mute,
                BackColor = Theme.Asphalt
            });
            Controls.Add(new Label
            {
                Text = "200%",
                Location = new Point(452, 498),
                Size = new Size(40, 18),
                ForeColor = Theme.Mute,
                BackColor = Theme.Asphalt,
                TextAlign = ContentAlignment.TopRight
            });

            Controls.Add(new Label
            {
                Text = "In-Game Setting",
                Location = new Point(28, 526),
                Size = new Size(240, 24),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Yellow,
                BackColor = Theme.Asphalt
            });
            _ratingValue = new Label
            {
                Location = new Point(300, 526),
                Size = new Size(192, 24),
                Font = new Font("Segoe UI", 11f, FontStyle.Bold),
                ForeColor = Theme.Yellow,
                BackColor = Theme.Asphalt,
                TextAlign = ContentAlignment.MiddleRight,
                Text = AiSkillPatcher.VanillaMax.ToString()
            };
            Controls.Add(_ratingValue);
            _rating = new SkillSlider
            {
                Location = new Point(28, 554),
                Size = new Size(464, 52),
                BackColor = Theme.Asphalt,
                Minimum = AiSkillPatcher.VanillaMin,
                Maximum = AiSkillPatcher.VanillaMax,
                Value = AiSkillPatcher.VanillaMax
            };
            _rating.ValueChanged += (_, __) =>
            {
                _ratingValue.Text = _rating.Value.ToString();
                if (!_loading)
                {
                    _dirty = true;
                }
            };
            Controls.Add(_rating);
            Controls.Add(new Label
            {
                Text = "85",
                Location = new Point(28, 610),
                Size = new Size(40, 18),
                ForeColor = Theme.Mute,
                BackColor = Theme.Asphalt
            });
            Controls.Add(new Label
            {
                Text = "105",
                Location = new Point(452, 610),
                Size = new Size(40, 18),
                ForeColor = Theme.Mute,
                BackColor = Theme.Asphalt,
                TextAlign = ContentAlignment.TopRight
            });

            var apply = MakeButton("APPLY", Theme.Red, Theme.White, new Point(28, 638), new Size(220, 42), 0);
            apply.Click += (_, __) => { if (Ask("Are you sure you want to Apply?")) DoApply(); };
            var restore = MakeButton("RESTORE", Theme.Asphalt, Theme.Yellow, new Point(272, 638), new Size(220, 42), 2);
            restore.FlatAppearance.BorderColor = Theme.Yellow;
            restore.Click += (_, __) => { if (Ask("Are you sure you want to Restore?")) DoRestore(); };
            FormClosing += (_, e) =>
            {
                if (!_dirty && !_advanced.Pending)
                {
                    return;
                }

                var answer = MessageBox.Show(this, "Do you want to Apply these Changes?", Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question);
                if (answer == DialogResult.Cancel)
                {
                    e.Cancel = true;
                    return;
                }

                if (answer == DialogResult.No)
                {
                    return;
                }

                if (_advanced.Pending && !_advanced.TryApply())
                {
                    e.Cancel = true;
                    return;
                }

                if (_dirty)
                {
                    DoApply();
                    if (_dirty)
                    {
                        e.Cancel = true;
                    }
                }
            };
            Controls.Add(apply);
            Controls.Add(restore);

            var locate = new LinkLabel
            {
                Text = "Locate game…",
                Location = new Point(28, 694),
                AutoSize = true,
                LinkColor = Theme.Yellow,
                ActiveLinkColor = Theme.White,
                VisitedLinkColor = Theme.Yellow,
                BackColor = Theme.Asphalt
            };
            locate.LinkClicked += (_, __) => BrowseGame();
            Controls.Add(locate);

            _path = new Label
            {
                Location = new Point(150, 694),
                Size = new Size(342, 20),
                ForeColor = Theme.Mute,
                BackColor = Theme.Asphalt,
                AutoEllipsis = true
            };
            Controls.Add(_path);

            _status = new Label
            {
                Location = new Point(28, 722),
                Size = new Size(464, 56),
                ForeColor = Theme.Mute,
                BackColor = Theme.Asphalt
            };
            Controls.Add(_status);
            foreach (Control child in Controls)
            {
                if (child != _mode && child != _settings && child != _advanced)
                {
                    _basic.Add(child);
                }
            }

            Resize += (_, __) =>
            {
                if (_mode.SelectedIndex == 1)
                {
                    PlaceAdvanced();
                }
            };

            LoadGame(GameLocator.FindManagedDir());
        }

        private void ShowMode()
        {
            var advanced = _mode.SelectedIndex == 1;
            foreach (var child in _basic)
            {
                child.Visible = !advanced;
            }

            _advanced.Visible = advanced;
            if (advanced)
            {
                FormBorderStyle = FormBorderStyle.Sizable;
                MaximizeBox = true;
                var area = Screen.FromControl(this).WorkingArea;
                MinimumSize = new Size(900, 640);
                ClientSize = new Size(Math.Max(1100, area.Width - 48), Math.Max(760, area.Height - 64));

                var dll = string.IsNullOrWhiteSpace(_managedDir) ? null : System.IO.Path.Combine(_managedDir, "Assembly-CSharp.dll");
                _advanced.LoadFrom(dll);
                PlaceAdvanced();
            }
            else
            {
                if (WindowState != FormWindowState.Normal)
                {
                    WindowState = FormWindowState.Normal;
                }

                FormBorderStyle = FormBorderStyle.FixedSingle;
                MaximizeBox = false;
                MinimumSize = Size.Empty;
                ClientSize = new Size(520, 820);
            }

            Invalidate();
        }

        private void PlaceAdvanced()
        {
            var top = HeaderHeight + 56;
            _advanced.SetBounds(12, top, Math.Max(200, ClientSize.Width - 24), Math.Max(120, ClientSize.Height - top - 12));
        }

        private void DrawHeader(Graphics g)
        {
            var dest = new Rectangle(0, 0, ClientSize.Width, HeaderHeight);
            g.Clear(Theme.Asphalt);
            using (var header = new SolidBrush(Color.Black))
            {
                g.FillRectangle(header, dest);
            }
            g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.SmoothingMode = SmoothingMode.HighQuality;
            if (_banner != null && _banner.Width > 1 && _banner.Height > 1)
            {
                var scale = Math.Max(dest.Width / (float)_banner.Width, dest.Height / (float)_banner.Height);
                var srcW = Math.Min(_banner.Width, dest.Width / scale);
                var srcH = Math.Min(_banner.Height, dest.Height / scale);
                var srcX = Math.Max(0f, _banner.Width - srcW);
                var srcY = Math.Max(0f, (_banner.Height - srcH) / 2f);
                g.DrawImage(_banner, dest, srcX, srcY, srcW, srcH, GraphicsUnit.Pixel);
            }

            var titleX = dest.X + 16f;
            if (_logo != null && _logo.Width > 0 && _logo.Height > 0)
            {
                var lh = dest.Height * 0.62f;
                var lw = lh * _logo.Width / _logo.Height;
                var maxW = dest.Width * 0.34f;
                if (lw > maxW)
                {
                    lw = maxW;
                    lh = lw * _logo.Height / _logo.Width;
                }

                var ly = dest.Y + (dest.Height - lh) / 2f;
                using (var attr = new ImageAttributes())
                {
                    attr.SetColorKey(Color.FromArgb(0, 0, 0), Color.FromArgb(18, 18, 18));
                    g.DrawImage(
                        _logo,
                        new Rectangle((int)(dest.X + 12), (int)ly, Math.Max(1, (int)lw), Math.Max(1, (int)lh)),
                        0,
                        0,
                        _logo.Width,
                        _logo.Height,
                        GraphicsUnit.Pixel,
                        attr);
                }

                titleX = dest.X + 20f + lw;
            }

            var slotW = Math.Max(48f, dest.Right - titleX - 12f);
            var size = Math.Max(12f, dest.Height * 0.28f);
            using (var font = new Font("Segoe UI", size, FontStyle.Bold | FontStyle.Italic, GraphicsUnit.Pixel))
            using (var format = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                Trimming = StringTrimming.EllipsisCharacter,
                FormatFlags = StringFormatFlags.NoWrap
            })
            {
                Theme.DrawOutlinedText(g, "AI Skill Utility", font, new RectangleF(titleX, dest.Y, slotW, dest.Height), format);
            }
        }

        private static Image? LoadImage(string resource)
        {
            var assembly = Assembly.GetExecutingAssembly();
            using (var stream = assembly.GetManifestResourceStream(resource))
            {
                if (stream == null)
                {
                    return null;
                }

                using (var copy = new MemoryStream())
                {
                    stream.CopyTo(copy);
                    copy.Position = 0;
                    using (var loaded = Image.FromStream(copy))
                    {
                        return new Bitmap(loaded);
                    }
                }
            }
        }

        private static Button MakeButton(string text, Color back, Color fore, Point loc, Size size, int border)
        {
            var button = new Button
            {
                Text = text,
                Location = loc,
                Size = size,
                BackColor = back,
                ForeColor = fore,
                FlatStyle = FlatStyle.Flat,
                UseVisualStyleBackColor = false,
                Font = new Font("Impact", 16, FontStyle.Regular, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = border;
            button.FlatAppearance.BorderColor = border > 0 ? Theme.Yellow : back;
            button.FlatAppearance.MouseOverBackColor = back;
            button.FlatAppearance.MouseDownBackColor = back;
            return button;
        }

        private void LoadGame(string? managedDir)
        {
            _loading = true;
            _managedDir = managedDir;
            if (string.IsNullOrWhiteSpace(managedDir))
            {
                _path.Text = "Game not found — click Locate game";
                _status.ForeColor = Theme.Yellow;
                _status.Text = "Select your NASCAR Heat 5 folder, or NASCARHeat5_Data\\Managed.";
                _loading = false;
                _dirty = false;
                return;
            }

            var dll = System.IO.Path.Combine(managedDir, "Assembly-CSharp.dll");
            _path.Text = dll;
            try
            {
                var patcher = new AiSkillPatcher(dll);
                var target = patcher.Resolve();
                _ceiling = target.Ceiling;
                _rating.Value = InGameAi.TryRead(out var setting) ? setting : target.NativeRating;

                if (target.Kind == "patched" && target.Current.HasValue)
                {
                    _slider.Value = target.Current.Value;
                    _status.ForeColor = Theme.Yellow;
                    if (target.Current.Value > AiSkillPatcher.PercentBase && Math.Abs(target.Extra - 1f) < 0.001f)
                    {
                        _status.Text = "Old native-200 patch is still on (Heat reclamps it to 105). APPLY to make " + target.Current.Value + "% actually faster.";
                    }
                    else
                    {
                        _status.Text = AppliedMessage(target.Current.Value, target.Extra, _rating.Value);
                    }
                }
                else
                {
                    _slider.Value = AiSkillPatcher.VanillaCustom;
                    _status.ForeColor = Theme.Mute;
                    _status.Text = "Vanilla is on (online Auto is ~97). Move the slider, then APPLY.";
                }
            }
            catch (Exception ex)
            {
                _status.ForeColor = Theme.Yellow;
                _status.Text = ex.Message;
            }
            finally
            {
                _loading = false;
                _dirty = false;
            }
        }

        private void BrowseGame()
        {
            using (var dlg = new FolderBrowserDialog())
            {
                dlg.Description = "Select NASCAR Heat 5, or NASCARHeat5_Data\\Managed.";
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return;
                }

                var resolved = GameLocator.ResolveFromBrowse(dlg.SelectedPath);
                if (resolved == null)
                {
                    _status.ForeColor = Color.FromArgb(255, 120, 90);
                    _status.Text = "That folder does not contain Assembly-CSharp.dll.";
                    return;
                }

                LoadGame(resolved);
            }
        }

        private void DoApply()
        {
            try
            {
                var patcher = RequirePatcher();
                var value = patcher.Apply(_slider.Value, _ceiling, _rating.Value);
                InGameAi.Write(_rating.Value);
                _dirty = false;
                _status.ForeColor = Theme.Yellow;
                _status.Text = AppliedMessage(value, AiSkillPatcher.ExtraFor(value), _rating.Value);
            }
            catch (Exception ex)
            {
                _status.ForeColor = Color.FromArgb(255, 120, 90);
                _status.Text = ex.Message;
            }
        }

        private void DoRestore()
        {
            try
            {
                var patcher = RequirePatcher();
                var kind = patcher.Restore();
                LoadGame(_managedDir);
                _advanced.LoadFrom(System.IO.Path.Combine(_managedDir ?? "", "Assembly-CSharp.dll"));
                _dirty = false;
                _status.ForeColor = Theme.Yellow;
                _status.Text = kind == "already-stock"
                    ? "Already using vanilla AI."
                    : "Restored vanilla AI.";
            }
            catch (Exception ex)
            {
                _status.ForeColor = Color.FromArgb(255, 120, 90);
                _status.Text = ex.Message;
            }
        }

        private static string AppliedMessage(int value, float extra, int setting)
        {
            return "Applied " + value + "% multiplier (" + extra.ToString("0.00") + "x) and in-game setting " + setting + ". Close Heat before the next change.";
        }

        private AiSkillPatcher RequirePatcher()
        {
            if (string.IsNullOrWhiteSpace(_managedDir))
            {
                throw new InvalidOperationException("Locate the NASCAR Heat 5 Managed folder first.");
            }

            return new AiSkillPatcher(System.IO.Path.Combine(_managedDir, "Assembly-CSharp.dll"));
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _icon?.Dispose();
                _logo?.Dispose();
                _banner?.Dispose();
            }

            base.Dispose(disposing);
        }

        private static Icon LoadIcon()
        {
            var image = LoadImage("NH5AiSkillAdjustment.icon-ai.jpg");
            if (image == null)
            {
                return (Icon)SystemIcons.Application.Clone();
            }

            var sizes = new[] { 16, 32, 48, 256 };
            var pngs = new byte[sizes.Length][];
            for (var i = 0; i < sizes.Length; i++)
            {
                var size = sizes[i];
                using (var bmp = new Bitmap(size, size, PixelFormat.Format32bppArgb))
                using (var g = Graphics.FromImage(bmp))
                using (var ms = new MemoryStream())
                {
                    g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                    g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                    g.SmoothingMode = SmoothingMode.HighQuality;
                    g.Clear(Color.Transparent);
                    g.DrawImage(image, new Rectangle(0, 0, size, size));
                    bmp.Save(ms, ImageFormat.Png);
                    pngs[i] = ms.ToArray();
                }
            }

            image.Dispose();
            using (var ico = new MemoryStream())
            using (var writer = new BinaryWriter(ico))
            {
                writer.Write((ushort)0);
                writer.Write((ushort)1);
                writer.Write((ushort)sizes.Length);
                var offset = 6 + (16 * sizes.Length);
                for (var i = 0; i < sizes.Length; i++)
                {
                    var size = sizes[i];
                    writer.Write((byte)(size >= 256 ? 0 : size));
                    writer.Write((byte)(size >= 256 ? 0 : size));
                    writer.Write((byte)0);
                    writer.Write((byte)0);
                    writer.Write((ushort)1);
                    writer.Write((ushort)32);
                    writer.Write(pngs[i].Length);
                    writer.Write(offset);
                    offset += pngs[i].Length;
                }

                for (var i = 0; i < pngs.Length; i++)
                {
                    writer.Write(pngs[i]);
                }

                writer.Flush();
                using (var loaded = new Icon(new MemoryStream(ico.ToArray())))
                {
                    return (Icon)loaded.Clone();
                }
            }
        }
    }
}
