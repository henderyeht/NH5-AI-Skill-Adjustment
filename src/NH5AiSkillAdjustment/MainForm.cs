using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace NH5AiSkillAdjustment
{
    internal sealed class MainForm : Form
    {
        private readonly SkillSlider _slider;
        private readonly Label _value;
        private readonly Label _status;
        private readonly Label _path;
        private readonly Icon _icon;
        private string? _managedDir;

        public MainForm()
        {
            Text = "NH5 AI Skill Adjustment Tool";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            MaximizeBox = false;
            MinimizeBox = false;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(520, 448);
            BackColor = Theme.Asphalt;
            ForeColor = Theme.White;
            DoubleBuffered = true;
            Font = new Font("Segoe UI", 9.5f);
            _icon = MakeIcon();
            Icon = _icon;

            Paint += (_, e) =>
            {
                Theme.FillCheckered(e.Graphics, new Rectangle(0, 0, Width, 58), 10);
                using (var shade = new SolidBrush(Color.FromArgb(130, 0, 0, 0)))
                {
                    e.Graphics.FillRectangle(shade, 0, 0, Width, 58);
                }

                using (var titleFont = new Font("Impact", 22, FontStyle.Regular, GraphicsUnit.Pixel))
                using (var yellow = new SolidBrush(Theme.Yellow))
                {
                    e.Graphics.DrawString("AI SKILL", titleFont, yellow, 18, 16);
                }

                using (var stripe = new SolidBrush(Theme.Yellow))
                {
                    e.Graphics.FillRectangle(stripe, 0, 58, Width, 6);
                }
            };

            var instructions = new Label
            {
                AutoSize = false,
                Location = new Point(20, 78),
                Size = new Size(480, 102),
                ForeColor = Theme.White,
                BackColor = Theme.Panel,
                Padding = new Padding(12, 10, 12, 10),
                Text = "Close NASCAR Heat 5 first.\r\n"
                     + "Online has no AI difficulty option, so APPLY forces native 105. The slider is percent of vanilla pace.\r\n"
                     + "100% = vanilla table. 200% = double pace. RESTORE or Steam Verify puts vanilla back."
            };
            Controls.Add(instructions);

            _value = new Label
            {
                AutoSize = false,
                Location = new Point(20, 188),
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
                Location = new Point(28, 240),
                Size = new Size(464, 40),
                BackColor = Theme.Asphalt,
                Minimum = AiSkillPatcher.MinStrength,
                Maximum = AiSkillPatcher.MaxStrength,
                Value = AiSkillPatcher.VanillaCustom
            };
            _slider.ValueChanged += (_, __) => { _value.Text = _slider.Value.ToString() + "%"; };
            Controls.Add(_slider);

            Controls.Add(new Label
            {
                Text = "60%",
                Location = new Point(28, 280),
                Size = new Size(40, 18),
                ForeColor = Theme.Mute,
                BackColor = Theme.Asphalt
            });
            Controls.Add(new Label
            {
                Text = "200%",
                Location = new Point(452, 280),
                Size = new Size(40, 18),
                ForeColor = Theme.Mute,
                BackColor = Theme.Asphalt,
                TextAlign = ContentAlignment.TopRight
            });

            var apply = MakeButton("APPLY", Theme.Red, Theme.White, new Point(28, 314), new Size(220, 42), 0);
            apply.Click += (_, __) => DoApply();
            var restore = MakeButton("RESTORE", Color.FromArgb(24, 24, 26), Theme.Yellow, new Point(272, 314), new Size(220, 42), 2);
            restore.FlatAppearance.BorderColor = Theme.Yellow;
            restore.Click += (_, __) => DoRestore();
            Controls.Add(apply);
            Controls.Add(restore);

            var locate = new LinkLabel
            {
                Text = "Locate game…",
                Location = new Point(28, 366),
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
                Location = new Point(150, 366),
                Size = new Size(342, 20),
                ForeColor = Theme.Mute,
                BackColor = Theme.Asphalt,
                AutoEllipsis = true
            };
            Controls.Add(_path);

            _status = new Label
            {
                Location = new Point(28, 392),
                Size = new Size(464, 40),
                ForeColor = Theme.Mute,
                BackColor = Theme.Asphalt
            };
            Controls.Add(_status);

            LoadGame(GameLocator.FindManagedDir());
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
                Font = new Font("Impact", 16, FontStyle.Regular, GraphicsUnit.Pixel),
                Cursor = Cursors.Hand
            };
            button.FlatAppearance.BorderSize = border;
            return button;
        }

        private void LoadGame(string? managedDir)
        {
            _managedDir = managedDir;
            if (string.IsNullOrWhiteSpace(managedDir))
            {
                _path.Text = "Game not found — click Locate game";
                _status.ForeColor = Theme.Yellow;
                _status.Text = "Select your NASCAR Heat 5 folder, or NASCARHeat5_Data\\Managed.";
                return;
            }

            var dll = System.IO.Path.Combine(managedDir, "Assembly-CSharp.dll");
            _path.Text = dll;
            try
            {
                var patcher = new AiSkillPatcher(dll);
                var target = patcher.Resolve();
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
                        _status.Text = AppliedMessage(target.Current.Value, target.Extra);
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
                var value = patcher.Apply(_slider.Value);
                _status.ForeColor = Theme.Yellow;
                _status.Text = AppliedMessage(value, AiSkillPatcher.ExtraFor(value));
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
                _slider.Value = AiSkillPatcher.VanillaCustom;
                _status.ForeColor = Theme.Yellow;
                _status.Text = kind == "already-stock"
                    ? "Already using vanilla AI (no table scale, no forced 105)."
                    : "Restored vanilla AI. Online will use Auto again (~97).";
            }
            catch (Exception ex)
            {
                _status.ForeColor = Color.FromArgb(255, 120, 90);
                _status.Text = ex.Message;
            }
        }

        private static string AppliedMessage(int value, float extra)
        {
            return "Applied " + value + "% (" + extra.ToString("0.00") + "x vanilla table, native 105). Close Heat before the next change.";
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
            }

            base.Dispose(disposing);
        }

        private static Icon MakeIcon()
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.None;
                Theme.FillCheckered(g, new Rectangle(0, 0, 32, 32), 8);
                using (var yellow = new Pen(Theme.Yellow, 3))
                {
                    g.DrawRectangle(yellow, 1, 1, 29, 29);
                }
            }

            var icon = Icon.FromHandle(bmp.GetHicon());
            return icon;
        }
    }
}
