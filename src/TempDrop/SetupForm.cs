using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using TempDrop.Core;

namespace TempDrop
{
    internal sealed class SetupForm : Form
    {
        private readonly TempDropService service;
        private readonly TextBox folderTextBox;
        private readonly NumericUpDown retentionDays;
        private readonly DateTimePicker cleanupTime;
        private readonly CheckBox automaticCleanup;
        private readonly CheckBox consent;
        private readonly Button finishButton;

        public SetupForm(TempDropService service, AppConfiguration existing)
        {
            this.service = service;
            AppConfiguration config = existing ?? AppConfiguration.CreateDefault();

            Text = existing == null ? "Set up TempDrop" : "TempDrop settings";
            StartPosition = FormStartPosition.CenterScreen;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            ClientSize = new Size(620, 510);
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(247, 248, 250);
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 92,
                BackColor = Color.FromArgb(38, 50, 78)
            };
            Controls.Add(header);

            Label title = new Label
            {
                Text = existing == null ? "Welcome to TempDrop" : "TempDrop settings",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(24, 16)
            };
            header.Controls.Add(title);

            Label subtitle = new Label
            {
                Text = "A private, local holding folder for files you only need temporarily.",
                ForeColor = Color.FromArgb(220, 226, 238),
                AutoSize = true,
                Location = new Point(27, 56)
            };
            header.Controls.Add(subtitle);

            Label folderLabel = CreateLabel("Managed folder", 28, 116);
            Controls.Add(folderLabel);
            folderTextBox = new TextBox
            {
                Location = new Point(28, 140),
                Size = new Size(470, 26),
                Text = config.ManagedFolder
            };
            Controls.Add(folderTextBox);
            Button browseButton = new Button
            {
                Text = "Browse...",
                Location = new Point(508, 138),
                Size = new Size(84, 30)
            };
            browseButton.Click += BrowseButtonClick;
            Controls.Add(browseButton);

            Label folderHint = CreateLabel("Only files inside this dedicated folder can be deleted by TempDrop.", 28, 171);
            folderHint.ForeColor = Color.DimGray;
            Controls.Add(folderHint);

            Label retentionLabel = CreateLabel("Default retention", 28, 210);
            Controls.Add(retentionLabel);
            retentionDays = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 365,
                Value = config.RetentionDays,
                Location = new Point(28, 234),
                Size = new Size(72, 26)
            };
            Controls.Add(retentionDays);
            Label daysLabel = CreateLabel("days", 108, 238);
            Controls.Add(daysLabel);

            automaticCleanup = new CheckBox
            {
                Text = "Run automatic cleanup every day at",
                Checked = config.AutomaticCleanupEnabled,
                AutoSize = true,
                Location = new Point(250, 237)
            };
            Controls.Add(automaticCleanup);
            cleanupTime = new DateTimePicker
            {
                Format = DateTimePickerFormat.Custom,
                CustomFormat = "HH:mm",
                ShowUpDown = true,
                Location = new Point(493, 233),
                Size = new Size(75, 26),
                Value = ParseTime(config.DailyRunTime)
            };
            Controls.Add(cleanupTime);

            Panel warning = new Panel
            {
                Location = new Point(28, 286),
                Size = new Size(564, 126),
                BackColor = Color.FromArgb(255, 241, 238),
                BorderStyle = BorderStyle.FixedSingle
            };
            Controls.Add(warning);
            Label warningTitle = new Label
            {
                Text = "Permanent deletion warning",
                ForeColor = Color.FromArgb(170, 45, 35),
                Font = new Font("Segoe UI", 10F, FontStyle.Bold),
                AutoSize = true,
                Location = new Point(14, 12)
            };
            warning.Controls.Add(warningTitle);
            Label warningText = new Label
            {
                Text = "Expired files are permanently deleted and do not go to the Recycle Bin.\r\nTempDrop does not upload files or send telemetry; everything stays on this PC.",
                AutoSize = true,
                Location = new Point(14, 39)
            };
            warning.Controls.Add(warningText);
            consent = new CheckBox
            {
                Text = "I understand that expired files cannot be restored.",
                Checked = config.ConsentAccepted,
                Enabled = !config.ConsentAccepted,
                AutoSize = true,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                Location = new Point(17, 89)
            };
            consent.CheckedChanged += delegate { UpdateFinishButton(); };
            warning.Controls.Add(consent);

            Button cancelButton = new Button
            {
                Text = "Cancel",
                DialogResult = DialogResult.Cancel,
                Location = new Point(406, 448),
                Size = new Size(88, 34)
            };
            Controls.Add(cancelButton);
            finishButton = new Button
            {
                Text = existing == null ? "Enable TempDrop" : "Save settings",
                Location = new Point(504, 448),
                Size = new Size(104, 34),
                BackColor = Color.FromArgb(57, 105, 222),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat
            };
            finishButton.FlatAppearance.BorderSize = 0;
            finishButton.Click += FinishButtonClick;
            Controls.Add(finishButton);

            AcceptButton = finishButton;
            CancelButton = cancelButton;
            UpdateFinishButton();
        }

        private void BrowseButtonClick(object sender, EventArgs eventArgs)
        {
            using (FolderBrowserDialog dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose a dedicated TempDrop folder";
                dialog.SelectedPath = folderTextBox.Text;
                dialog.ShowNewFolderButton = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    folderTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void FinishButtonClick(object sender, EventArgs eventArgs)
        {
            try
            {
                string managedFolder = ManagedFolderGuard.Normalize(folderTextBox.Text);
                if (!ManagedFolderGuard.IsSafeManagedFolder(managedFolder))
                {
                    MessageBox.Show(
                        "Choose a dedicated subfolder. Drive roots, Downloads, Desktop, Documents, profile roots, and network folders are blocked.",
                        "Unsafe folder",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                AppConfiguration config = AppConfiguration.CreateDefault();
                config.ManagedFolder = managedFolder;
                config.RetentionDays = (int)retentionDays.Value;
                config.DailyRunTime = cleanupTime.Value.ToString("HH:mm", CultureInfo.InvariantCulture);
                config.AutomaticCleanupEnabled = automaticCleanup.Checked;
                config.ConsentAccepted = consent.Checked;
                config.ConsentAcceptedAtUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
                service.SaveConfiguration(config);

                if (automaticCleanup.Checked)
                {
                    CommandResult taskResult = ScheduledTaskService.Create(Application.ExecutablePath, config.DailyRunTime);
                    if (taskResult.ExitCode != 0)
                    {
                        config.AutomaticCleanupEnabled = false;
                        service.SaveConfiguration(config);
                        MessageBox.Show(
                            "Settings were saved, but Windows could not create the daily task. You can still clean manually.\r\n\r\n" + taskResult.Output,
                            "Automatic cleanup was not enabled",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    }
                }
                else if (ScheduledTaskService.IsInstalled())
                {
                    ScheduledTaskService.Remove();
                }

                DialogResult = DialogResult.OK;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could not save settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void UpdateFinishButton()
        {
            finishButton.Enabled = consent.Checked;
        }

        private static Label CreateLabel(string text, int x, int y)
        {
            return new Label { Text = text, AutoSize = true, Location = new Point(x, y) };
        }

        private static DateTime ParseTime(string value)
        {
            DateTime parsed;
            if (DateTime.TryParseExact(value, "HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed))
            {
                return DateTime.Today.Add(parsed.TimeOfDay);
            }
            return DateTime.Today.AddHours(3);
        }
    }
}
