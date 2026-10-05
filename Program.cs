using System.Security.Cryptography;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace SmartSort;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm(args.FirstOrDefault()));
    }
}

public sealed class MainForm : Form
{
    private readonly Color _surface = Color.White;
    private readonly Color _text = Color.FromArgb(30, 34, 40);
    private readonly Color _muted = Color.FromArgb(105, 112, 122);
    private readonly Color _accent = Color.FromArgb(24, 111, 242);
    private readonly Color _success = Color.FromArgb(20, 145, 85);
    private readonly Color _warning = Color.FromArgb(211, 137, 0);
    private readonly Color _danger = Color.FromArgb(199, 59, 59);

    private readonly TextBox _folder = new();
    private readonly Button _browse = new();
    private readonly Button _scan = new();
    private readonly Button _rules = new();
    private readonly Button _sort = new();
    private readonly Button _undo = new();
    private readonly DataGridView _grid = new();
    private readonly Label _status = new();
    private readonly ProgressBar _progress = new();
    private readonly Label _itemsValue = new();
    private readonly Label _autoValue = new();
    private readonly Label _reviewValue = new();
    private readonly Label _dupesValue = new();
    private readonly Label _folderMeta = new();

    private List<PlanItem> _plan = new();
    private SmartSortSettings _settings;

    public MainForm(string? initialPath)
    {
        _settings = SettingsStore.Load();

        Text = "SmartSort";
        Width = 1320;
        Height = 780;
        MinimumSize = new Size(1040, 640);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);
        BackColor = _surface;
        ForeColor = _text;
        AutoScaleMode = AutoScaleMode.Dpi;

        var defaultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath)) defaultPath = initialPath;

        BuildDashboard(defaultPath);
        WireEvents();
        ResetSummary();
    }

    private void BuildDashboard(string defaultPath)
    {
        SuspendLayout();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(22, 18, 22, 18),
            BackColor = _surface
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 48));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 66));

        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = _surface };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 360));
        header.Controls.Add(new Label
        {
            Text = "SmartSort",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 19F),
            ForeColor = _text,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        header.Controls.Add(new Label
        {
            Text = "Preview first  •  Nothing moves until Sort",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI", 9.5F),
            ForeColor = _muted,
            TextAlign = ContentAlignment.MiddleRight
        }, 1, 0);

        var folderBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            Padding = new Padding(0, 7, 0, 7),
            BackColor = _surface
        };
        folderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        folderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        folderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 98));
        folderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
        folderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
        folderBar.Controls.Add(new Label
        {
            Text = "Folder",
            Dock = DockStyle.Fill,
            ForeColor = _muted,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        _folder.Text = defaultPath;
        _folder.Dock = DockStyle.Fill;
        _folder.Font = new Font("Segoe UI", 10.5F);
        _folder.BorderStyle = BorderStyle.FixedSingle;
        _folder.Margin = new Padding(0, 0, 10, 0);
        folderBar.Controls.Add(_folder, 1, 0);

        _rules.Text = "Rules";
        StyleSecondaryButton(_rules);
        _rules.Dock = DockStyle.Fill;
        _rules.Margin = new Padding(0, 0, 10, 0);
        folderBar.Controls.Add(_rules, 2, 0);

        _browse.Text = "Browse";
        StyleSecondaryButton(_browse);
        _browse.Dock = DockStyle.Fill;
        _browse.Margin = new Padding(0, 0, 10, 0);
        folderBar.Controls.Add(_browse, 3, 0);

        _scan.Text = "Scan folder";
        StylePrimaryButton(_scan);
        _scan.Dock = DockStyle.Fill;
        folderBar.Controls.Add(_scan, 4, 0);

        var stats = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            BackColor = Color.FromArgb(247, 248, 250),
            Padding = new Padding(10, 5, 10, 5),
            Margin = new Padding(0, 2, 0, 10),
            CellBorderStyle = TableLayoutPanelCellBorderStyle.Single
        };
        for (var i = 0; i < 4; i++) stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25));
        stats.Controls.Add(MakeStat("Items", _itemsValue, _text), 0, 0);
        stats.Controls.Add(MakeStat("Ready", _autoValue, _success), 1, 0);
        stats.Controls.Add(MakeStat("Review", _reviewValue, _warning), 2, 0);
        stats.Controls.Add(MakeStat("Duplicates", _dupesValue, _danger), 3, 0);

        ConfigureGrid();
        _grid.Margin = new Padding(0, 0, 0, 10);

        var footer = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 5,
            BackColor = _surface,
            Padding = new Padding(0, 10, 0, 0)
        };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 14));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 150));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 172));

        _status.Text = "Choose a folder and scan it.";
        _status.Dock = DockStyle.Fill;
        _status.ForeColor = _muted;
        _status.TextAlign = ContentAlignment.MiddleLeft;
        _status.AutoEllipsis = true;
        footer.Controls.Add(_status, 0, 0);

        _progress.Dock = DockStyle.Fill;
        _progress.Minimum = 0;
        _progress.Maximum = 100;
        _progress.Margin = new Padding(8, 13, 8, 13);
        footer.Controls.Add(_progress, 1, 0);

        _undo.Text = "Undo last sort";
        StyleSecondaryButton(_undo);
        _undo.Dock = DockStyle.Fill;
        footer.Controls.Add(_undo, 3, 0);

        _sort.Text = "SORT FILES";
        StylePrimaryButton(_sort);
        _sort.Dock = DockStyle.Fill;
        _sort.Enabled = false;
        _sort.Font = new Font("Segoe UI Semibold", 10.5F);
        footer.Controls.Add(_sort, 4, 0);

        _folderMeta.Visible = false;

        root.Controls.Add(header, 0, 0);
        root.Controls.Add(folderBar, 0, 1);
        root.Controls.Add(stats, 0, 2);
        root.Controls.Add(_grid, 0, 3);
        root.Controls.Add(footer, 0, 4);
        Controls.Add(root);

        ResumeLayout(true);
    }

    private Control MakeStat(string title, Label value, Color valueColor)
    {
        var panel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            BackColor = Color.FromArgb(247, 248, 250),
            Padding = new Padding(10, 0, 10, 0)
        };
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 62));
        panel.Controls.Add(new Label
        {
            Text = title,
            Dock = DockStyle.Fill,
            ForeColor = _muted,
            Font = new Font("Segoe UI", 9.5F),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);
        value.Text = "—";
        value.Dock = DockStyle.Fill;
        value.ForeColor = valueColor;
        value.Font = new Font("Segoe UI Semibold", 12F);
        value.TextAlign = ContentAlignment.MiddleRight;
        panel.Controls.Add(value, 1, 0);
        return panel;
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.AllowUserToResizeRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.RowHeadersVisible = false;
        _grid.BackgroundColor = Color.White;
        _grid.BorderStyle = BorderStyle.FixedSingle;
        _grid.GridColor = Color.FromArgb(232, 234, 238);
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.ColumnHeadersHeight = 38;
        _grid.RowTemplate.Height = 34;
        _grid.EnableHeadersVisualStyles = false;
        _grid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(244, 246, 248);
        _grid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(82, 88, 98);
        _grid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI Semibold", 9F);
        _grid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(244, 246, 248);
        _grid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(82, 88, 98);
        _grid.DefaultCellStyle.BackColor = Color.White;
        _grid.DefaultCellStyle.ForeColor = _text;
        _grid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(230, 239, 255);
        _grid.DefaultCellStyle.SelectionForeColor = _text;
        _grid.DefaultCellStyle.Padding = new Padding(6, 0, 6, 0);
        _grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(250, 251, 252);

        _grid.Columns.Clear();
        _grid.Columns.Add("Name", "File / folder");
        _grid.Columns.Add("Extension", "Ext");
        _grid.Columns.Add("Size", "Size");
        _grid.Columns.Add("Modified", "Modified");
        _grid.Columns.Add("Category", "Destination");
        _grid.Columns.Add("Confidence", "Status");
        _grid.Columns.Add("Reason", "Reason");

        _grid.Columns[0].FillWeight = 27;
        _grid.Columns[1].FillWeight = 7;
        _grid.Columns[2].FillWeight = 9;
        _grid.Columns[3].FillWeight = 13;
        _grid.Columns[4].FillWeight = 24;
        _grid.Columns[5].FillWeight = 8;
        _grid.Columns[6].FillWeight = 18;

        _grid.Columns[1].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;
        _grid.Columns[2].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
    }

    private void StylePrimaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(17, 93, 211);
        button.FlatAppearance.MouseDownBackColor = Color.FromArgb(13, 78, 181);
        button.BackColor = _accent;
        button.ForeColor = Color.White;
        button.Font = new Font("Segoe UI Semibold", 10F);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        button.MinimumSize = new Size(90, 36);
    }

    private void StyleSecondaryButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderColor = Color.FromArgb(207, 211, 217);
        button.FlatAppearance.BorderSize = 1;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(245, 246, 248);
        button.BackColor = Color.White;
        button.ForeColor = _text;
        button.Font = new Font("Segoe UI Semibold", 9.5F);
        button.Cursor = Cursors.Hand;
        button.UseVisualStyleBackColor = false;
        button.MinimumSize = new Size(90, 36);
    }

    private void WireEvents()
    {
        _browse.Click += (_, _) => Browse();
        _rules.Click += (_, _) => EditRules();
        _scan.Click += async (_, _) => await ScanAsync();
        _sort.Click += async (_, _) => await SortAsync();
        _undo.Click += async (_, _) => await UndoAsync();
        _folder.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) await ScanAsync(); };
    }

    private void EditRules()
    {
        using var dialog = new RulesForm(_settings);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _settings = dialog.Settings;
            SettingsStore.Save(_settings);
            _status.Text = "Rules saved. Scan again to apply them.";
        }
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = _folder.Text, ShowNewFolderButton = false };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _folder.Text = dialog.SelectedPath;
            _folderMeta.Text = "Ready to scan · nothing will move yet.";
        }
    }

    private async Task ScanAsync()
    {
        var root = _folder.Text.Trim();
        if (!Directory.Exists(root))
        {
            MessageBox.Show(this, "Folder does not exist.", "SmartSort", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SetBusy(true, "Scanning folder…");
        try
        {
            _plan = await Task.Run(() => BuildPlan(root));
            RenderPlan();
            _sort.Enabled = _plan.Count > 0;
            UpdateSummary();
            _folderMeta.Text = $"{root} · review the preview, then sort when ready.";
            _status.Text = "Scan complete. Nothing has moved yet.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Scan failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private void UpdateSummary()
    {
        var dupes = _plan.Count(x => x.Category.Contains("Duplicates", StringComparison.OrdinalIgnoreCase));
        var review = _plan.Count(x => x.Confidence < _settings.ReviewThreshold);
        var auto = _plan.Count - review;
        _itemsValue.Text = _plan.Count.ToString("N0");
        _autoValue.Text = auto.ToString("N0");
        _reviewValue.Text = review.ToString("N0");
        _dupesValue.Text = dupes.ToString("N0");
    }

    private void ResetSummary()
    {
        _itemsValue.Text = "—";
        _autoValue.Text = "—";
        _reviewValue.Text = "—";
        _dupesValue.Text = "—";
    }

    private List<PlanItem> BuildPlan(string root)
    {
        var managed = ManagedTopLevelFolders();
        var paths = Directory.EnumerateFileSystemEntries(root)
            .Where(p => !Path.GetFileName(p).StartsWith('.') && !managed.Contains(Path.GetFileName(p)))
            .ToList();

        var duplicateFiles = _settings.DetectDuplicates
            ? FindDuplicates(paths.Where(File.Exists).ToList())
            : new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var result = new List<PlanItem>();
        for (var i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            var metadata = GetMetadata(path);
            PlanItem item = duplicateFiles.Contains(path)
                ? new(path, Path.GetFileName(path), "Delete Candidates\\Duplicates", 0.99, "Exact SHA-256 duplicate", metadata.Extension, metadata.SizeBytes, metadata.Modified)
                : Classify(path, metadata);
            result.Add(item);

            var progress = Math.Min(100, (int)Math.Round((i + 1) * 100.0 / Math.Max(1, paths.Count)));
            BeginInvoke(() => _progress.Value = progress);
        }
        return result;
    }

    private PlanItem Classify(string path, ItemMetadata metadata)
    {
        var name = Path.GetFileName(path);
        var ext = metadata.Extension;

        if (_settings.QuarantineTemporaryDownloads && File.Exists(path) && SmartSortSettings.GarbageExtensions.Contains(ext))
            return new(path, name, "Delete Candidates\\Temporary", 0.99, "Temporary/incomplete download", ext, metadata.SizeBytes, metadata.Modified);

        var lower = name.ToLowerInvariant();
        foreach (var rule in _settings.Rules)
        {
            var keywords = RuleText.Split(rule.Keywords);
            var keywordMatch = keywords.FirstOrDefault(k => lower.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (!string.IsNullOrWhiteSpace(keywordMatch))
                return new(path, name, rule.Destination, ClampConfidence(rule.Confidence), $"Name keyword: {keywordMatch}", ext, metadata.SizeBytes, metadata.Modified);
        }

        if (File.Exists(path))
        {
            foreach (var rule in _settings.Rules)
            {
                var extensions = RuleText.Split(rule.Extensions)
                    .Select(NormalizeExtension)
                    .Where(x => !string.IsNullOrWhiteSpace(x));

                if (extensions.Contains(ext, StringComparer.OrdinalIgnoreCase))
                    return new(path, name, rule.Destination, ClampConfidence(rule.Confidence), $"Extension: {ext}", ext, metadata.SizeBytes, metadata.Modified);
            }
        }

        if (Directory.Exists(path))
            return new(path, name, "00 - Review\\Folders", 0.55, "Folder needs review", "Folder", metadata.SizeBytes, metadata.Modified);

        return new(path, name, "00 - Review\\Other", 0.45, "No matching rule", ext, metadata.SizeBytes, metadata.Modified);
    }

    private static double ClampConfidence(double value) => Math.Max(0.01, Math.Min(0.99, value));

    private static string NormalizeExtension(string ext)
    {
        ext = ext.Trim();
        if (string.IsNullOrWhiteSpace(ext)) return string.Empty;
        if (ext == "*") return ext;
        return ext.StartsWith('.') ? ext.ToLowerInvariant() : "." + ext.ToLowerInvariant();
    }

    private static ItemMetadata GetMetadata(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                var info = new FileInfo(path);
                return new(Path.GetExtension(path).ToLowerInvariant(), info.Length, info.LastWriteTime);
            }

            if (Directory.Exists(path))
            {
                var info = new DirectoryInfo(path);
                return new("Folder", TryGetFolderSize(path), info.LastWriteTime);
            }
        }
        catch
        {
            // Metadata is helpful, but should never stop sorting.
        }

        return new(string.Empty, null, null);
    }

    private static long? TryGetFolderSize(string path)
    {
        try
        {
            long total = 0;
            var pending = new Stack<string>();
            pending.Push(path);
            while (pending.Count > 0)
            {
                var current = pending.Pop();
                try
                {
                    foreach (var file in Directory.EnumerateFiles(current))
                    {
                        try { total += new FileInfo(file).Length; } catch { }
                    }
                    foreach (var dir in Directory.EnumerateDirectories(current)) pending.Push(dir);
                }
                catch { }
            }
            return total;
        }
        catch
        {
            return null;
        }
    }

    private static HashSet<string> FindDuplicates(List<string> files)
    {
        var duplicates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sizeGroup in files.GroupBy(f => new FileInfo(f).Length).Where(g => g.Count() > 1))
        {
            var hashGroups = sizeGroup.GroupBy(HashFile);
            foreach (var group in hashGroups.Where(g => g.Count() > 1))
            {
                var keep = group.OrderBy(f => File.GetLastWriteTimeUtc(f)).First();
                foreach (var f in group.Where(f => !f.Equals(keep, StringComparison.OrdinalIgnoreCase))) duplicates.Add(f);
            }
        }
        return duplicates;
    }

    private static string HashFile(string file)
    {
        using var sha = SHA256.Create();
        using var stream = File.OpenRead(file);
        return Convert.ToHexString(sha.ComputeHash(stream));
    }

    private async Task SortAsync()
    {
        if (_plan.Count == 0) return;
        if (MessageBox.Show(this, "Move the scanned items now? Nothing will be permanently deleted and the operation can be undone.", "SmartSort", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        SetBusy(true, "Sorting files…");
        try
        {
            var log = await Task.Run(() => ApplyPlan(_folder.Text.Trim(), _plan));
            _status.Text = $"Sorted {_plan.Count:N0} items · undo is available.";
            _folderMeta.Text = $"Last sort saved in {Path.GetFileName(log)}";
            _plan.Clear();
            _grid.Rows.Clear();
            _sort.Enabled = false;
            ResetSummary();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Sort failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private string ApplyPlan(string root, List<PlanItem> plan)
    {
        var historyDir = Path.Combine(root, "SmartSort History");
        Directory.CreateDirectory(historyDir);
        var log = Path.Combine(historyDir, $"sort-history-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        var actions = new List<MoveAction>();

        foreach (var item in plan)
        {
            if (!File.Exists(item.Path) && !Directory.Exists(item.Path)) continue;
            var category = item.Confidence >= _settings.ReviewThreshold
                ? item.Category
                : Path.Combine("00 - Review", Sanitize(item.Category));
            var destination = UniqueDestination(Path.Combine(root, category), item.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            try
            {
                if (File.Exists(item.Path)) File.Move(item.Path, destination);
                else Directory.Move(item.Path, destination);
                actions.Add(new(item.Path, destination));
            }
            catch (Exception ex)
            {
                actions.Add(new(item.Path, null, ex.Message));
            }
        }

        File.WriteAllText(log, JsonSerializer.Serialize(new HistoryLog(root, DateTime.Now, actions), new JsonSerializerOptions { WriteIndented = true }));
        return log;
    }

    private async Task UndoAsync()
    {
        var root = _folder.Text.Trim();
        var historyDir = Path.Combine(root, "SmartSort History");
        if (!Directory.Exists(historyDir))
        {
            MessageBox.Show(this, "No SmartSort history found for this folder.");
            return;
        }

        var latest = Directory.GetFiles(historyDir, "sort-history-*.json").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        if (latest == null)
        {
            MessageBox.Show(this, "No sort history found.");
            return;
        }

        SetBusy(true, "Undoing last sort…");
        try
        {
            var restored = await Task.Run(() => Undo(latest));
            _status.Text = $"Restored {restored:N0} items.";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, ex.Message, "Undo failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private static int Undo(string logPath)
    {
        var log = JsonSerializer.Deserialize<HistoryLog>(File.ReadAllText(logPath)) ?? throw new InvalidDataException("Invalid history file.");
        var count = 0;
        foreach (var action in log.Actions.AsEnumerable().Reverse())
        {
            if (string.IsNullOrWhiteSpace(action.To)) continue;
            if (!File.Exists(action.To) && !Directory.Exists(action.To)) continue;
            var dest = action.From;
            if (File.Exists(dest) || Directory.Exists(dest)) dest = UniqueDestination(Path.GetDirectoryName(dest)!, Path.GetFileName(dest));
            Directory.CreateDirectory(Path.GetDirectoryName(dest)!);
            if (File.Exists(action.To)) File.Move(action.To, dest); else Directory.Move(action.To, dest);
            count++;
        }
        return count;
    }

    private void RenderPlan()
    {
        _grid.Rows.Clear();
        foreach (var item in _plan.OrderByDescending(x => x.Confidence).ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var status = item.Confidence >= _settings.ReviewThreshold ? "Ready" : "Review";
            var index = _grid.Rows.Add(
                item.Name,
                item.Extension,
                FormatBytes(item.SizeBytes),
                item.Modified?.ToString("yyyy-MM-dd HH:mm") ?? "—",
                item.Category,
                status,
                item.Reason);

            var row = _grid.Rows[index];
            row.Cells[5].Style.ForeColor = item.Confidence >= _settings.ReviewThreshold ? _success : _warning;
            row.Cells[5].Style.Font = new Font("Segoe UI Semibold", 9F);
            if (item.Category.Contains("Duplicates", StringComparison.OrdinalIgnoreCase)) row.Cells[5].Style.ForeColor = _danger;
        }
    }

    private static string FormatBytes(long? bytes)
    {
        if (bytes is null) return "—";
        double value = bytes.Value;
        string[] units = ["B", "KB", "MB", "GB", "TB"];
        var unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit == 0 ? $"{value:N0} {units[unit]}" : $"{value:N1} {units[unit]}";
    }

    private void SetBusy(bool busy, string? status = null)
    {
        _browse.Enabled = !busy;
        _rules.Enabled = !busy;
        _scan.Enabled = !busy;
        _sort.Enabled = !busy && _plan.Count > 0;
        _undo.Enabled = !busy;
        _folder.Enabled = !busy;
        if (status != null) _status.Text = status;
        if (!busy) _progress.Value = 0;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private HashSet<string> ManagedTopLevelFolders()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "SmartSort History", "00 - Review", "Delete Candidates"
        };
        foreach (var rule in _settings.Rules.Where(r => !string.IsNullOrWhiteSpace(r.Destination)))
            roots.Add(rule.Destination.Split('\\')[0]);
        return roots;
    }

    private static string UniqueDestination(string folder, string name)
    {
        Directory.CreateDirectory(folder);
        var candidate = Path.Combine(folder, name);
        if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
        var ext = Path.GetExtension(name);
        var stem = Path.GetFileNameWithoutExtension(name);
        for (var i = 2; ; i++)
        {
            candidate = Path.Combine(folder, $"{stem} ({i}){ext}");
            if (!File.Exists(candidate) && !Directory.Exists(candidate)) return candidate;
        }
    }

    private static string Sanitize(string value) => Regex.Replace(value, "[<>:\\\\|?*\"]", "_").Trim();
}

public sealed class RulesForm : Form
{
    private readonly DataGridView _grid = new();
    private readonly NumericUpDown _threshold = new();
    private readonly CheckBox _duplicates = new();
    private readonly CheckBox _temporary = new();
    private readonly BindingSource _binding = new();
    private readonly List<SortRule> _workingRules;

    public SmartSortSettings Settings { get; private set; }

    public RulesForm(SmartSortSettings current)
    {
        Settings = current.Clone();
        _workingRules = Settings.Rules.Select(r => r.Clone()).ToList();

        Text = "SmartSort Rules";
        Width = 980;
        Height = 650;
        MinimumSize = new Size(780, 520);
        StartPosition = FormStartPosition.CenterParent;
        Font = new Font("Segoe UI", 9.5F);
        AutoScaleMode = AutoScaleMode.Dpi;

        BuildUi();
    }

    private void BuildUi()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            RowCount = 4,
            ColumnCount = 1,
            Padding = new Padding(18)
        };
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 58));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, 54));

        root.Controls.Add(new Label
        {
            Text = "Sorting rules",
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 17F),
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        var options = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 9, 0, 0)
        };

        options.Controls.Add(new Label { Text = "Ready threshold", AutoSize = true, Margin = new Padding(0, 7, 8, 0) });
        _threshold.Minimum = 50;
        _threshold.Maximum = 99;
        _threshold.Value = (decimal)Math.Round(Settings.ReviewThreshold * 100);
        _threshold.Width = 62;
        options.Controls.Add(_threshold);
        options.Controls.Add(new Label { Text = "%", AutoSize = true, Margin = new Padding(3, 7, 22, 0) });

        _duplicates.Text = "Detect exact duplicates";
        _duplicates.Checked = Settings.DetectDuplicates;
        _duplicates.AutoSize = true;
        _duplicates.Margin = new Padding(0, 6, 22, 0);
        options.Controls.Add(_duplicates);

        _temporary.Text = "Quarantine temporary downloads";
        _temporary.Checked = Settings.QuarantineTemporaryDownloads;
        _temporary.AutoSize = true;
        _temporary.Margin = new Padding(0, 6, 0, 0);
        options.Controls.Add(_temporary);
        root.Controls.Add(options, 0, 1);

        ConfigureGrid();
        root.Controls.Add(_grid, 0, 2);

        var footer = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 6, Padding = new Padding(0, 10, 0, 0) };
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100));
        footer.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        var add = MakeButton("Add rule");
        var delete = MakeButton("Delete");
        var defaults = MakeButton("Restore defaults");
        var cancel = MakeButton("Cancel");
        var save = MakeButton("Save", true);

        add.Click += (_, _) => AddRule();
        delete.Click += (_, _) => DeleteRule();
        defaults.Click += (_, _) => RestoreDefaults();
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };
        save.Click += (_, _) => SaveAndClose();

        footer.Controls.Add(add, 0, 0);
        footer.Controls.Add(delete, 1, 0);
        footer.Controls.Add(defaults, 2, 0);
        footer.Controls.Add(cancel, 4, 0);
        footer.Controls.Add(save, 5, 0);
        root.Controls.Add(footer, 0, 3);

        Controls.Add(root);
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.RowHeadersVisible = false;
        _grid.AutoGenerateColumns = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.MultiSelect = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowTemplate.Height = 32;

        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Destination folder",
            DataPropertyName = nameof(SortRule.Destination),
            FillWeight = 28
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Filename keywords (separate with ;)",
            DataPropertyName = nameof(SortRule.Keywords),
            FillWeight = 38
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Extensions",
            DataPropertyName = nameof(SortRule.Extensions),
            FillWeight = 24
        });
        _grid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Confidence",
            DataPropertyName = nameof(SortRule.ConfidencePercent),
            FillWeight = 10
        });

        _binding.DataSource = _workingRules;
        _grid.DataSource = _binding;
    }

    private Button MakeButton(string text, bool primary = false)
    {
        var button = new Button
        {
            Text = text,
            Dock = DockStyle.Fill,
            FlatStyle = FlatStyle.Flat,
            BackColor = primary ? Color.FromArgb(24, 111, 242) : Color.White,
            ForeColor = primary ? Color.White : Color.FromArgb(30, 34, 40),
            Font = new Font("Segoe UI Semibold", 9F),
            Margin = new Padding(4, 0, 4, 0)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(207, 211, 217);
        button.FlatAppearance.BorderSize = primary ? 0 : 1;
        return button;
    }

    private void AddRule()
    {
        _workingRules.Add(new SortRule
        {
            Destination = "New Category",
            Keywords = string.Empty,
            Extensions = string.Empty,
            Confidence = 0.95
        });
        _binding.ResetBindings(false);
        _grid.CurrentCell = _grid.Rows[^1].Cells[0];
        _grid.BeginEdit(true);
    }

    private void DeleteRule()
    {
        if (_grid.CurrentRow?.DataBoundItem is not SortRule rule) return;
        _workingRules.Remove(rule);
        _binding.ResetBindings(false);
    }

    private void RestoreDefaults()
    {
        if (MessageBox.Show(this, "Replace all current rules with the SmartSort defaults?", "Restore defaults", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        _workingRules.Clear();
        _workingRules.AddRange(SmartSortSettings.CreateDefault().Rules.Select(r => r.Clone()));
        _binding.ResetBindings(false);
    }

    private void SaveAndClose()
    {
        _grid.EndEdit();
        _binding.EndEdit();

        if (_workingRules.Any(r => string.IsNullOrWhiteSpace(r.Destination)))
        {
            MessageBox.Show(this, "Every rule needs a destination folder.", "SmartSort", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        Settings = new SmartSortSettings
        {
            ReviewThreshold = (double)_threshold.Value / 100.0,
            DetectDuplicates = _duplicates.Checked,
            QuarantineTemporaryDownloads = _temporary.Checked,
            Rules = _workingRules.Select(r => r.Clone()).ToList()
        };
        DialogResult = DialogResult.OK;
        Close();
    }
}

public static class RuleText
{
    public static IEnumerable<string> Split(string? value) =>
        (value ?? string.Empty)
            .Split([';', ','], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(x => !string.IsNullOrWhiteSpace(x));
}

public sealed class SmartSortSettings
{
    public double ReviewThreshold { get; set; } = 0.90;
    public bool DetectDuplicates { get; set; } = true;
    public bool QuarantineTemporaryDownloads { get; set; } = true;
    public List<SortRule> Rules { get; set; } = new();

    public static readonly HashSet<string> GarbageExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".tmp", ".crdownload", ".part", ".download"
    };

    public SmartSortSettings Clone() => new()
    {
        ReviewThreshold = ReviewThreshold,
        DetectDuplicates = DetectDuplicates,
        QuarantineTemporaryDownloads = QuarantineTemporaryDownloads,
        Rules = Rules.Select(r => r.Clone()).ToList()
    };

    public static SmartSortSettings CreateDefault() => new()
    {
        ReviewThreshold = 0.90,
        DetectDuplicates = true,
        QuarantineTemporaryDownloads = true,
        Rules =
        [
            new("Projects\\T-Cube", "t-cube; t cube", "", 0.94),
            new("Projects\\T-Line", "t-line; t line", "", 0.94),
            new("Projects\\T-Lago", "t-lago; t lago", "", 0.94),
            new("Projects\\Meltemi", "meltemi", "", 0.94),
            new("Projects\\Selene Residence", "selene", "", 0.94),
            new("Projects\\The Paragon", "the paragon; paragon", "", 0.94),
            new("Projects\\Avelora", "avelora", "", 0.94),
            new("Projects\\Erimi", "erimi", "", 0.94),
            new("Projects\\Ypsonas", "ypsonas", "", 0.94),
            new("Projects\\Kapsalos", "kapsalos", "", 0.94),
            new("Terrasol\\Finance", "invoice; receipt; statement; payment; vat; budget; forecast; quotation; quote", "", 0.94),
            new("Terrasol\\Legal", "contract; agreement; nda; legal; lease; deed; terms", "", 0.94),
            new("Terrasol\\Marketing", "brochure; campaign; social; instagram; facebook; marketing", "", 0.94),
            new("Themelion", "themelion; crm; proposal; client portal; design control; vectorizer", "", 0.94),
            new("Architecture\\Drawings", "architectural; architecture; floor plan; elevation; section; drawing; revision", "", 0.94),
            new("Architecture\\Specifications", "specification; specs; schedule of finishes; finishes", "", 0.94),
            new("3D & Rendering\\Models", "3ds; 3dsmax; sketchup; model; mesh; scene", ".skp; .max; .3ds; .fbx; .obj; .glb; .gltf; .blend; .dae", 0.97),
            new("3D & Rendering\\Textures", "texture; material; hdri; normal map; roughness; albedo", "", 0.94),
            new("3D & Rendering\\Exports", "render; export; twinmotion; corona", "", 0.94),
            new("Architecture\\CAD", "", ".dwg; .dxf; .rvt; .ifc", 0.97),
            new("Images", "", ".jpg; .jpeg; .png; .webp; .gif; .bmp; .tif; .tiff; .heic", 0.97),
            new("Videos", "", ".mp4; .mov; .mkv; .avi; .webm; .m4v", 0.97),
            new("Audio", "", ".mp3; .wav; .flac; .m4a; .aac", 0.97),
            new("Archives", "", ".zip; .rar; .7z; .tar; .gz; .bz2; .xz", 0.97),
            new("Installers", "", ".exe; .msi; .msix; .appx", 0.97),
            new("Spreadsheets", "", ".xlsx; .xls; .csv; .tsv; .ods", 0.97),
            new("Documents", "", ".doc; .docx; .odt; .rtf; .txt; .md", 0.88),
            new("PDFs", "", ".pdf", 0.88),
            new("Code", "", ".py; .js; .ts; .tsx; .jsx; .html; .css; .scss; .json; .yaml; .yml; .sql; .ps1; .bat; .sh", 0.97)
        ]
    };
}

public sealed class SortRule
{
    public string Destination { get; set; } = string.Empty;
    public string Keywords { get; set; } = string.Empty;
    public string Extensions { get; set; } = string.Empty;
    public double Confidence { get; set; } = 0.95;

    public int ConfidencePercent
    {
        get => (int)Math.Round(Confidence * 100);
        set => Confidence = Math.Max(0.01, Math.Min(0.99, value / 100.0));
    }

    public SortRule() { }

    public SortRule(string destination, string keywords, string extensions, double confidence)
    {
        Destination = destination;
        Keywords = keywords;
        Extensions = extensions;
        Confidence = confidence;
    }

    public SortRule Clone() => new(Destination, Keywords, Extensions, Confidence);
}

public static class SettingsStore
{
    private static string SettingsDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "SmartSort");

    private static string SettingsPath => Path.Combine(SettingsDirectory, "settings.json");

    public static SmartSortSettings Load()
    {
        try
        {
            if (!File.Exists(SettingsPath)) return SmartSortSettings.CreateDefault();
            var settings = JsonSerializer.Deserialize<SmartSortSettings>(File.ReadAllText(SettingsPath));
            if (settings is null || settings.Rules.Count == 0) return SmartSortSettings.CreateDefault();
            return settings;
        }
        catch
        {
            return SmartSortSettings.CreateDefault();
        }
    }

    public static void Save(SmartSortSettings settings)
    {
        Directory.CreateDirectory(SettingsDirectory);
        File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true }));
    }
}

public sealed record ItemMetadata(string Extension, long? SizeBytes, DateTime? Modified);
public sealed record PlanItem(string Path, string Name, string Category, double Confidence, string Reason, string Extension, long? SizeBytes, DateTime? Modified);
public sealed record MoveAction(string From, string? To, string? Error = null);
public sealed record HistoryLog(string Root, DateTime Created, List<MoveAction> Actions);
