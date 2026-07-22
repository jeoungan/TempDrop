using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using TempDrop.Core;

namespace TempDrop
{
    internal sealed class MainForm : Form
    {
        private readonly TempDropService service;
        private AppConfiguration config;
        private readonly Label folderLabel;
        private readonly NumericUpDown retentionDays;
        private readonly DataGridView grid;
        private readonly Label statusLabel;

        public MainForm(TempDropService service)
        {
            this.service = service;
            config = service.LoadConfiguration();

            Text = "TempDrop - temporary file cleanup";
            StartPosition = FormStartPosition.CenterScreen;
            Size = new Size(1040, 680);
            MinimumSize = new Size(900, 560);
            Font = new Font("Segoe UI", 9F);
            BackColor = Color.FromArgb(247, 248, 250);
            AllowDrop = true;
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

            Panel header = new Panel
            {
                Dock = DockStyle.Top,
                Height = 105,
                BackColor = Color.FromArgb(38, 50, 78)
            };
            Controls.Add(header);
            Label title = new Label
            {
                Text = "TempDrop",
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 18F, FontStyle.Bold),
                Location = new Point(24, 14),
                AutoSize = true
            };
            header.Controls.Add(title);
            Label subtitle = new Label
            {
                Text = "Keep disposable files here. TempDrop permanently deletes them after their retention period.",
                ForeColor = Color.FromArgb(220, 226, 238),
                Location = new Point(27, 54),
                AutoSize = true
            };
            header.Controls.Add(subtitle);
            folderLabel = new Label
            {
                Text = "Managed folder: " + config.ManagedFolder,
                ForeColor = Color.FromArgb(177, 190, 216),
                Location = new Point(27, 77),
                AutoEllipsis = true,
                Size = new Size(960, 20)
            };
            header.Controls.Add(folderLabel);

            Panel toolbar = new Panel
            {
                Dock = DockStyle.Top,
                Height = 70,
                BackColor = Color.White
            };
            Controls.Add(toolbar);
            toolbar.BringToFront();

            Button addButton = CreateButton("Move files in", 18, 16, 112, 36);
            addButton.BackColor = Color.FromArgb(57, 105, 222);
            addButton.ForeColor = Color.White;
            addButton.FlatStyle = FlatStyle.Flat;
            addButton.FlatAppearance.BorderSize = 0;
            addButton.Click += AddButtonClick;
            toolbar.Controls.Add(addButton);

            Button openButton = CreateButton("Open folder", 140, 16, 96, 36);
            openButton.Click += delegate { OpenManagedFolder(); };
            toolbar.Controls.Add(openButton);

            Label retentionLabel = new Label
            {
                Text = "Retention",
                AutoSize = true,
                Location = new Point(260, 26)
            };
            toolbar.Controls.Add(retentionLabel);
            retentionDays = new NumericUpDown
            {
                Minimum = 1,
                Maximum = 365,
                Value = config.RetentionDays,
                Size = new Size(62, 26),
                Location = new Point(324, 22)
            };
            toolbar.Controls.Add(retentionDays);
            Label daysLabel = new Label { Text = "days", AutoSize = true, Location = new Point(392, 26) };
            toolbar.Controls.Add(daysLabel);

            Button saveDefaultButton = CreateButton("Save default", 432, 18, 100, 32);
            saveDefaultButton.Click += SaveDefaultButtonClick;
            toolbar.Controls.Add(saveDefaultButton);

            Button refreshButton = CreateButton("Refresh", 808, 16, 88, 36);
            refreshButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            refreshButton.Click += delegate { RefreshGrid(); };
            toolbar.Controls.Add(refreshButton);

            Button cleanButton = CreateButton("Clean expired", 906, 16, 116, 36);
            cleanButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            cleanButton.ForeColor = Color.FromArgb(180, 45, 45);
            cleanButton.Click += CleanButtonClick;
            toolbar.Controls.Add(cleanButton);

            Panel bottom = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 66,
                BackColor = Color.White
            };
            Controls.Add(bottom);

            Button extendButton = CreateButton("Extend selected", 18, 14, 138, 34);
            extendButton.Click += ExtendButtonClick;
            bottom.Controls.Add(extendButton);
            Button deleteButton = CreateButton("Delete selected now", 166, 14, 154, 34);
            deleteButton.ForeColor = Color.FromArgb(180, 45, 45);
            deleteButton.Click += DeleteButtonClick;
            bottom.Controls.Add(deleteButton);
            Button settingsButton = CreateButton("Settings", 330, 14, 88, 34);
            settingsButton.Click += SettingsButtonClick;
            bottom.Controls.Add(settingsButton);

            statusLabel = new Label
            {
                Text = "Ready",
                TextAlign = ContentAlignment.MiddleRight,
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                Location = new Point(555, 18),
                Size = new Size(455, 28)
            };
            bottom.Controls.Add(statusLabel);

            Panel gridPanel = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(18, 12, 18, 12),
                BackColor = Color.FromArgb(247, 248, 250)
            };
            Controls.Add(gridPanel);
            gridPanel.BringToFront();

            grid = new DataGridView
            {
                Dock = DockStyle.Fill,
                BackgroundColor = Color.White,
                BorderStyle = BorderStyle.Fixed3D,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AllowUserToResizeRows = false,
                AutoGenerateColumns = false,
                MultiSelect = true,
                ReadOnly = true,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            grid.RowTemplate.Height = 32;
            grid.Columns.Add(CreateColumn("FileName", "File", 300, DataGridViewAutoSizeColumnMode.Fill));
            grid.Columns.Add(CreateColumn("Size", "Size", 90, DataGridViewAutoSizeColumnMode.None));
            grid.Columns.Add(CreateColumn("Created", "Added", 145, DataGridViewAutoSizeColumnMode.None));
            grid.Columns.Add(CreateColumn("Expires", "Delete on", 145, DataGridViewAutoSizeColumnMode.None));
            grid.Columns.Add(CreateColumn("Remaining", "Remaining", 100, DataGridViewAutoSizeColumnMode.None));
            DataGridViewTextBoxColumn pathColumn = CreateColumn("FullPath", "Full path", 0, DataGridViewAutoSizeColumnMode.None);
            pathColumn.Visible = false;
            grid.Columns.Add(pathColumn);
            gridPanel.Controls.Add(grid);

            DragEnter += MainFormDragEnter;
            DragDrop += MainFormDragDrop;
            Shown += delegate { RefreshGrid(); };
        }

        private void RefreshGrid()
        {
            try
            {
                IList<InventoryItem> inventory = service.GetInventory(DateTime.UtcNow);
                grid.Rows.Clear();
                long totalBytes = 0;
                int expiredCount = 0;

                foreach (InventoryItem item in inventory)
                {
                    totalBytes += item.Length;
                    if (item.IsExpired)
                    {
                        expiredCount++;
                    }

                    int rowIndex = grid.Rows.Add(
                        item.Name,
                        FormatSize(item.Length),
                        item.CreatedAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        item.ExpiresAtUtc.ToLocalTime().ToString("yyyy-MM-dd HH:mm"),
                        GetRemainingText(item),
                        item.FullName);
                    DataGridViewRow row = grid.Rows[rowIndex];
                    if (item.IsExpired)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 235, 235);
                        row.DefaultCellStyle.ForeColor = Color.FromArgb(150, 35, 35);
                    }
                    else if (item.IsPartial)
                    {
                        row.DefaultCellStyle.BackColor = Color.FromArgb(255, 249, 225);
                    }
                }

                statusLabel.Text = string.Format(
                    "{0} files  ·  {1}  ·  {2} expired",
                    inventory.Count,
                    FormatSize(totalBytes),
                    expiredCount);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could not refresh TempDrop", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void AddButtonClick(object sender, EventArgs eventArgs)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "Choose files to move into TempDrop";
                dialog.Multiselect = true;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    ImportPaths(dialog.FileNames);
                }
            }
        }

        private void ImportPaths(IEnumerable<string> paths)
        {
            string[] files = paths.Where(File.Exists).ToArray();
            if (files.Length == 0)
            {
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                string.Format(
                    "Move {0} file(s) into TempDrop?\r\n\r\nThey will disappear from their current location and be permanently deleted after {1} days.",
                    files.Length,
                    (int)retentionDays.Value),
                "Move files into TempDrop",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            List<string> errors = new List<string>();
            foreach (string path in files)
            {
                try
                {
                    service.AddFile(path, (int)retentionDays.Value, false);
                }
                catch (Exception ex)
                {
                    errors.Add(path + Environment.NewLine + ex.Message);
                }
            }
            RefreshGrid();
            if (errors.Count > 0)
            {
                MessageBox.Show(string.Join(Environment.NewLine + Environment.NewLine, errors.ToArray()), "Some files could not be moved", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void SaveDefaultButtonClick(object sender, EventArgs eventArgs)
        {
            try
            {
                config.RetentionDays = (int)retentionDays.Value;
                service.SaveConfiguration(config);
                MessageBox.Show("The default retention period was saved.", "TempDrop", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshGrid();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Could not save settings", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void CleanButtonClick(object sender, EventArgs eventArgs)
        {
            IList<InventoryItem> expired = service.GetInventory(DateTime.UtcNow).Where(delegate(InventoryItem item) { return item.IsExpired; }).ToList();
            if (expired.Count == 0)
            {
                MessageBox.Show("There are no expired files.", "TempDrop", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                string.Format("Permanently delete {0} expired file(s) now?\r\n\r\nThis cannot be undone.", expired.Count),
                "Clean expired files",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirmation == DialogResult.Yes)
            {
                CleanupResult result = service.CleanupExpired(DateTime.UtcNow, false);
                RefreshGrid();
                MessageBox.Show(
                    string.Format("Deleted {0} file(s). {1} failed.", result.DeletedCount, result.Failed.Count),
                    "Cleanup complete",
                    MessageBoxButtons.OK,
                    result.Failed.Count == 0 ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
        }

        private void ExtendButtonClick(object sender, EventArgs eventArgs)
        {
            string[] paths = GetSelectedPaths();
            if (paths.Length == 0)
            {
                MessageBox.Show("Select at least one file first.", "TempDrop", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            foreach (string path in paths)
            {
                service.SetExpiry(path, DateTime.UtcNow.AddDays((int)retentionDays.Value));
            }
            RefreshGrid();
        }

        private void DeleteButtonClick(object sender, EventArgs eventArgs)
        {
            string[] paths = GetSelectedPaths();
            if (paths.Length == 0)
            {
                MessageBox.Show("Select at least one file first.", "TempDrop", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            DialogResult confirmation = MessageBox.Show(
                string.Format("Permanently delete {0} selected file(s)?\r\n\r\nThis cannot be undone.", paths.Length),
                "Delete selected files",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Warning);
            if (confirmation != DialogResult.Yes)
            {
                return;
            }

            foreach (string path in paths)
            {
                try
                {
                    service.DeleteFileNow(path);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Could not delete a file", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            RefreshGrid();
        }

        private void SettingsButtonClick(object sender, EventArgs eventArgs)
        {
            using (SetupForm setup = new SetupForm(service, config))
            {
                if (setup.ShowDialog(this) == DialogResult.OK)
                {
                    config = service.LoadConfiguration();
                    retentionDays.Value = config.RetentionDays;
                    folderLabel.Text = "Managed folder: " + config.ManagedFolder;
                    RefreshGrid();
                }
            }
        }

        private void OpenManagedFolder()
        {
            Directory.CreateDirectory(config.ManagedFolder);
            Process.Start("explorer.exe", "\"" + config.ManagedFolder + "\"");
        }

        private string[] GetSelectedPaths()
        {
            return grid.SelectedRows
                .Cast<DataGridViewRow>()
                .Select(delegate(DataGridViewRow row) { return Convert.ToString(row.Cells["FullPath"].Value); })
                .Where(delegate(string path) { return !string.IsNullOrWhiteSpace(path); })
                .ToArray();
        }

        private void MainFormDragEnter(object sender, DragEventArgs eventArgs)
        {
            if (eventArgs.Data.GetDataPresent(DataFormats.FileDrop))
            {
                eventArgs.Effect = DragDropEffects.Move;
            }
        }

        private void MainFormDragDrop(object sender, DragEventArgs eventArgs)
        {
            string[] paths = eventArgs.Data.GetData(DataFormats.FileDrop) as string[];
            if (paths != null)
            {
                ImportPaths(paths);
            }
        }

        private static string FormatSize(long bytes)
        {
            if (bytes >= 1024L * 1024L * 1024L) return string.Format("{0:N2} GB", bytes / (1024D * 1024D * 1024D));
            if (bytes >= 1024L * 1024L) return string.Format("{0:N1} MB", bytes / (1024D * 1024D));
            if (bytes >= 1024L) return string.Format("{0:N0} KB", bytes / 1024D);
            return bytes + " B";
        }

        private static string GetRemainingText(InventoryItem item)
        {
            if (item.IsPartial) return "In progress";
            TimeSpan remaining = item.ExpiresAtUtc - DateTime.UtcNow;
            if (remaining.TotalSeconds <= 0) return "Expired";
            if (remaining.TotalHours < 24) return string.Format("About {0}h", Math.Max(1, Math.Ceiling(remaining.TotalHours)));
            return string.Format("About {0}d", Math.Ceiling(remaining.TotalDays));
        }

        private static Button CreateButton(string text, int x, int y, int width, int height)
        {
            return new Button { Text = text, Location = new Point(x, y), Size = new Size(width, height) };
        }

        private static DataGridViewTextBoxColumn CreateColumn(string name, string header, int width, DataGridViewAutoSizeColumnMode autoSizeMode)
        {
            return new DataGridViewTextBoxColumn
            {
                Name = name,
                HeaderText = header,
                Width = width,
                AutoSizeMode = autoSizeMode
            };
        }
    }
}
