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
    private readonly Color _bg = Color.FromArgb(246, 247, 249);
    private readonly Color _surface = Color.White;
    private readonly Color _border = Color.FromArgb(226, 229, 234);
    private readonly Color _text = Color.FromArgb(30, 34, 40);
    private readonly Color _muted = Color.FromArgb(105, 112, 122);
    private readonly Color _accent = Color.FromArgb(24, 111, 242);
    private readonly Color _success = Color.FromArgb(20, 145, 85);
    private readonly Color _warning = Color.FromArgb(211, 137, 0);
    private readonly Color _danger = Color.FromArgb(199, 59, 59);

    private readonly TextBox _folder = new();
    private readonly Button _browse = new();
    private readonly Button _scan = new();
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

    private static readonly Dictionary<string, string[]> NameRules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Projects\\T-Cube"] = ["t-cube", "t cube"],
        ["Projects\\T-Line"] = ["t-line", "t line"],
        ["Projects\\T-Lago"] = ["t-lago", "t lago"],
        ["Projects\\Meltemi"] = ["meltemi"],
        ["Projects\\Selene Residence"] = ["selene"],
        ["Projects\\The Paragon"] = ["the paragon", "paragon"],
        ["Projects\\Avelora"] = ["avelora"],
        ["Projects\\Erimi"] = ["erimi"],
        ["Projects\\Ypsonas"] = ["ypsonas"],
        ["Projects\\Kapsalos"] = ["kapsalos"],
        ["Terrasol\\Finance"] = ["invoice", "receipt", "statement", "payment", "vat", "budget", "forecast", "quotation", "quote"],
        ["Terrasol\\Legal"] = ["contract", "agreement", "nda", "legal", "lease", "deed", "terms"],
        ["Terrasol\\Marketing"] = ["brochure", "campaign", "social", "instagram", "facebook", "marketing"],
        ["Themelion"] = ["themelion", "crm", "proposal", "client portal", "design control", "vectorizer"],
        ["Architecture\\Drawings"] = ["architectural", "architecture", "floor plan", "elevation", "section", "drawing", "revision"],
        ["Architecture\\Specifications"] = ["specification", "specs", "schedule of finishes", "finishes"],
        ["3D & Rendering\\Models"] = ["3ds", "3dsmax", "sketchup", "model", "mesh", "scene"],
        ["3D & Rendering\\Textures"] = ["texture", "material", "hdri", "normal map", "roughness", "albedo"],
        ["3D & Rendering\\Exports"] = ["render", "export", "twinmotion", "corona"]
    };

    private static readonly Dictionary<string, HashSet<string>> ExtensionRules = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Images"] = new([".jpg", ".jpeg", ".png", ".webp", ".gif", ".bmp", ".tif", ".tiff", ".heic"]),
        ["Videos"] = new([".mp4", ".mov", ".mkv", ".avi", ".webm", ".m4v"]),
        ["Audio"] = new([".mp3", ".wav", ".flac", ".m4a", ".aac"]),
        ["Archives"] = new([".zip", ".rar", ".7z", ".tar", ".gz", ".bz2", ".xz"]),
        ["Installers"] = new([".exe", ".msi", ".msix", ".appx"]),
        ["Spreadsheets"] = new([".xlsx", ".xls", ".csv", ".tsv", ".ods"]),
        ["Documents"] = new([".doc", ".docx", ".odt", ".rtf", ".txt", ".md"]),
        ["PDFs"] = new([".pdf"]),
        ["3D & Rendering\\Models"] = new([".skp", ".max", ".3ds", ".fbx", ".obj", ".glb", ".gltf", ".blend", ".dae"]),
        ["Architecture\\CAD"] = new([".dwg", ".dxf", ".rvt", ".ifc"]),
        ["Code"] = new([".py", ".js", ".ts", ".tsx", ".jsx", ".html", ".css", ".scss", ".json", ".yaml", ".yml", ".sql", ".ps1", ".bat", ".sh"])
    };

    private static readonly HashSet<string> GarbageExts = new(StringComparer.OrdinalIgnoreCase) { ".tmp", ".crdownload", ".part", ".download" };

    public MainForm(string? initialPath)
    {
        Text = "SmartSort";
        Width = 1160;
        Height = 760;
        MinimumSize = new Size(900, 620);
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

        // Compact header: deliberately closer to the original prototype.
        var header = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, BackColor = _surface };
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        header.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 315));
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

        // One obvious folder/action bar.
        var folderBar = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 4,
            Padding = new Padding(0, 7, 0, 7),
            BackColor = _surface
        };
        folderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 54));
        folderBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
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

        _browse.Text = "Browse";
        StyleSecondaryButton(_browse);
        _browse.Dock = DockStyle.Fill;
        _browse.Margin = new Padding(0, 0, 10, 0);
        folderBar.Controls.Add(_browse, 2, 0);

        _scan.Text = "Scan folder";
        StylePrimaryButton(_scan);
        _scan.Dock = DockStyle.Fill;
        folderBar.Controls.Add(_scan, 3, 0);

        // Dense stats strip instead of dashboard cards.
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

        // The preview is the screen, not a card inside the screen.
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

        // Retain this internal label because the sort engine updates it, but keep it off-screen.
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
        _grid.Columns.Add("Category", "Destination");
        _grid.Columns.Add("Confidence", "Status");
        _grid.Columns.Add("Reason", "Reason");
        _grid.Columns[0].FillWeight = 34;
        _grid.Columns[1].FillWeight = 32;
        _grid.Columns[2].FillWeight = 12;
        _grid.Columns[3].FillWeight = 22;
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
        _scan.Click += async (_, _) => await ScanAsync();
        _sort.Click += async (_, _) => await SortAsync();
        _undo.Click += async (_, _) => await UndoAsync();
        _folder.KeyDown += async (_, e) => { if (e.KeyCode == Keys.Enter) await ScanAsync(); };
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
        if (!Directory.Exists(root)) { MessageBox.Show(this, "Folder does not exist.", "SmartSort", MessageBoxButtons.OK, MessageBoxIcon.Warning); return; }
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
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Scan failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetBusy(false); }
    }

    private void UpdateSummary()
    {
        var dupes = _plan.Count(x => x.Category.Contains("Duplicates", StringComparison.OrdinalIgnoreCase));
        var review = _plan.Count(x => x.Confidence < 0.90);
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

        var duplicateFiles = FindDuplicates(paths.Where(File.Exists).ToList());
        var result = new List<PlanItem>();
        for (var i = 0; i < paths.Count; i++)
        {
            var path = paths[i];
            PlanItem item = duplicateFiles.Contains(path)
                ? new(path, Path.GetFileName(path), "Delete Candidates\\Duplicates", 0.99, "Exact SHA-256 duplicate")
                : Classify(path);
            result.Add(item);
            var progress = Math.Min(100, (int)Math.Round((i + 1) * 100.0 / Math.Max(1, paths.Count)));
            BeginInvoke(() => _progress.Value = progress);
        }
        return result;
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

    private static PlanItem Classify(string path)
    {
        var name = Path.GetFileName(path);
        var ext = Path.GetExtension(path);
        if (File.Exists(path) && GarbageExts.Contains(ext)) return new(path, name, "Delete Candidates\\Temporary", 0.99, "Temporary/incomplete download");

        var lower = name.ToLowerInvariant();
        foreach (var rule in NameRules)
        {
            var match = rule.Value.FirstOrDefault(k => lower.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (match != null) return new(path, name, rule.Key, 0.94, $"Name keyword: {match}");
        }

        if (File.Exists(path))
        {
            foreach (var rule in ExtensionRules)
                if (rule.Value.Contains(ext))
                {
                    var confidence = rule.Key is "Images" or "Videos" or "Audio" or "Archives" or "Installers" or "Spreadsheets" or "3D & Rendering\\Models" or "Architecture\\CAD" or "Code" ? 0.97 : 0.88;
                    return new(path, name, rule.Key, confidence, $"Extension: {ext}");
                }
        }
        if (Directory.Exists(path)) return new(path, name, "00 - Review\\Folders", 0.55, "Folder needs review");
        return new(path, name, "00 - Review\\Other", 0.45, "No strong rule");
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
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Sort failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetBusy(false); }
    }

    private static string ApplyPlan(string root, List<PlanItem> plan)
    {
        var historyDir = Path.Combine(root, "SmartSort History");
        Directory.CreateDirectory(historyDir);
        var log = Path.Combine(historyDir, $"sort-history-{DateTime.Now:yyyyMMdd-HHmmss}.json");
        var actions = new List<MoveAction>();
        foreach (var item in plan)
        {
            if (!File.Exists(item.Path) && !Directory.Exists(item.Path)) continue;
            var category = item.Confidence >= 0.90 ? item.Category : Path.Combine("00 - Review", Sanitize(item.Category));
            var destination = UniqueDestination(Path.Combine(root, category), item.Name);
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            try
            {
                if (File.Exists(item.Path)) File.Move(item.Path, destination);
                else Directory.Move(item.Path, destination);
                actions.Add(new(item.Path, destination));
            }
            catch (Exception ex) { actions.Add(new(item.Path, null, ex.Message)); }
        }
        File.WriteAllText(log, JsonSerializer.Serialize(new HistoryLog(root, DateTime.Now, actions), new JsonSerializerOptions { WriteIndented = true }));
        return log;
    }

    private async Task UndoAsync()
    {
        var root = _folder.Text.Trim();
        var historyDir = Path.Combine(root, "SmartSort History");
        if (!Directory.Exists(historyDir)) { MessageBox.Show(this, "No SmartSort history found for this folder."); return; }
        var latest = Directory.GetFiles(historyDir, "sort-history-*.json").OrderByDescending(File.GetLastWriteTimeUtc).FirstOrDefault();
        if (latest == null) { MessageBox.Show(this, "No sort history found."); return; }
        SetBusy(true, "Undoing last sort…");
        try
        {
            var restored = await Task.Run(() => Undo(latest));
            _status.Text = $"Restored {restored:N0} items.";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Undo failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetBusy(false); }
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
        foreach (var item in _plan.OrderByDescending(x => x.Confidence))
        {
            var index = _grid.Rows.Add(item.Name, item.Category, item.Confidence >= 0.90 ? "Ready" : "Review", item.Reason);
            var row = _grid.Rows[index];
            row.Cells[2].Style.ForeColor = item.Confidence >= 0.90 ? _success : _warning;
            row.Cells[2].Style.Font = new Font("Segoe UI Semibold", 9F);
            if (item.Category.Contains("Duplicates", StringComparison.OrdinalIgnoreCase)) row.Cells[2].Style.ForeColor = _danger;
        }
    }

    private void SetBusy(bool busy, string? status = null)
    {
        _browse.Enabled = !busy;
        _scan.Enabled = !busy;
        _sort.Enabled = !busy && _plan.Count > 0;
        _undo.Enabled = !busy;
        _folder.Enabled = !busy;
        if (status != null) _status.Text = status;
        if (!busy) _progress.Value = 0;
        Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
    }

    private static HashSet<string> ManagedTopLevelFolders()
    {
        var roots = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "SmartSort History", "00 - Review", "Delete Candidates" };
        foreach (var key in NameRules.Keys.Concat(ExtensionRules.Keys)) roots.Add(key.Split('\\')[0]);
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

public sealed record PlanItem(string Path, string Name, string Category, double Confidence, string Reason);
public sealed record MoveAction(string From, string? To, string? Error = null);
public sealed record HistoryLog(string Root, DateTime Created, List<MoveAction> Actions);
