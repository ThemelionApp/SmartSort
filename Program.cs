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
    private readonly TextBox _folder = new() { Dock = DockStyle.Fill };
    private readonly Button _browse = new() { Text = "Browse" };
    private readonly Button _scan = new() { Text = "Scan" };
    private readonly Button _sort = new() { Text = "Sort" };
    private readonly Button _undo = new() { Text = "Undo Last Sort" };
    private readonly DataGridView _grid = new() { Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill };
    private readonly Label _status = new() { Dock = DockStyle.Fill, AutoEllipsis = true, TextAlign = ContentAlignment.MiddleLeft };
    private readonly ProgressBar _progress = new() { Dock = DockStyle.Fill, Minimum = 0, Maximum = 100 };
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
        Text = "SmartSort 0.2";
        Width = 1100;
        Height = 700;
        MinimumSize = new Size(900, 560);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);

        var defaultPath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        defaultPath = Path.Combine(defaultPath, "Downloads");
        if (!string.IsNullOrWhiteSpace(initialPath) && Directory.Exists(initialPath)) defaultPath = initialPath;
        _folder.Text = defaultPath;

        _grid.Columns.Add("Name", "Name");
        _grid.Columns.Add("Category", "Destination");
        _grid.Columns.Add("Confidence", "Confidence");
        _grid.Columns.Add("Reason", "Reason");

        var top = new TableLayoutPanel { Dock = DockStyle.Top, Height = 48, ColumnCount = 5, Padding = new Padding(10) };
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 55));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        top.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 90));
        top.Controls.Add(new Label { Text = "Folder", Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft }, 0, 0);
        top.Controls.Add(_folder, 1, 0);
        top.Controls.Add(_browse, 2, 0);
        top.Controls.Add(_scan, 3, 0);
        top.Controls.Add(_sort, 4, 0);

        var bottom = new TableLayoutPanel { Dock = DockStyle.Bottom, Height = 54, ColumnCount = 3, Padding = new Padding(10) };
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        bottom.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140));
        bottom.Controls.Add(_status, 0, 0);
        bottom.Controls.Add(_progress, 1, 0);
        bottom.Controls.Add(_undo, 2, 0);

        Controls.Add(_grid);
        Controls.Add(bottom);
        Controls.Add(top);

        _browse.Click += (_, _) => Browse();
        _scan.Click += async (_, _) => await ScanAsync();
        _sort.Click += async (_, _) => await SortAsync();
        _undo.Click += async (_, _) => await UndoAsync();
        _sort.Enabled = false;
        _status.Text = "Choose a folder and scan it. Nothing moves until you click Sort.";
    }

    private void Browse()
    {
        using var dialog = new FolderBrowserDialog { SelectedPath = _folder.Text, ShowNewFolderButton = false };
        if (dialog.ShowDialog(this) == DialogResult.OK) _folder.Text = dialog.SelectedPath;
    }

    private async Task ScanAsync()
    {
        var root = _folder.Text.Trim();
        if (!Directory.Exists(root)) { MessageBox.Show(this, "Folder does not exist."); return; }
        SetBusy(true, "Scanning...");
        try
        {
            _plan = await Task.Run(() => BuildPlan(root));
            RenderPlan();
            _sort.Enabled = _plan.Count > 0;
            var dupes = _plan.Count(x => x.Category.Contains("Duplicates", StringComparison.OrdinalIgnoreCase));
            var review = _plan.Count(x => x.Confidence < 0.90);
            _status.Text = $"{_plan.Count:N0} items scanned · {dupes:N0} exact duplicates · {review:N0} review items";
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, "Scan failed", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { SetBusy(false); }
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
            PlanItem item;
            if (duplicateFiles.Contains(path))
                item = new(path, Path.GetFileName(path), "Delete Candidates\\Duplicates", 0.99, "exact SHA-256 duplicate");
            else
                item = Classify(path);
            result.Add(item);
            BeginInvoke(() => _progress.Value = Math.Min(100, (int)Math.Round((i + 1) * 100.0 / Math.Max(1, paths.Count))));
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
        if (File.Exists(path) && GarbageExts.Contains(ext)) return new(path, name, "Delete Candidates\\Temporary", 0.99, "temporary/incomplete download");

        var lower = name.ToLowerInvariant();
        foreach (var rule in NameRules)
        {
            var match = rule.Value.FirstOrDefault(k => lower.Contains(k, StringComparison.OrdinalIgnoreCase));
            if (match != null) return new(path, name, rule.Key, 0.94, $"name keyword: {match}");
        }

        if (File.Exists(path))
        {
            foreach (var rule in ExtensionRules)
                if (rule.Value.Contains(ext))
                {
                    var confidence = rule.Key is "Images" or "Videos" or "Audio" or "Archives" or "Installers" or "Spreadsheets" or "3D & Rendering\\Models" or "Architecture\\CAD" or "Code" ? 0.97 : 0.88;
                    return new(path, name, rule.Key, confidence, $"extension: {ext}");
                }
        }
        if (Directory.Exists(path)) return new(path, name, "00 - Review\\Folders", 0.55, "folder needs review");
        return new(path, name, "00 - Review\\Other", 0.45, "no strong rule");
    }

    private async Task SortAsync()
    {
        if (_plan.Count == 0) return;
        if (MessageBox.Show(this, "Move the scanned items now? Nothing will be permanently deleted and the operation can be undone.", "SmartSort", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        SetBusy(true, "Sorting...");
        try
        {
            var log = await Task.Run(() => ApplyPlan(_folder.Text.Trim(), _plan));
            _status.Text = $"Sorted {_plan.Count:N0} items. Undo log: {Path.GetFileName(log)}";
            _plan.Clear();
            _grid.Rows.Clear();
            _sort.Enabled = false;
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
        SetBusy(true, "Undoing...");
        try
        {
            var restored = await Task.Run(() => Undo(latest));
            _status.Text = $"Restored {restored:N0} items from {Path.GetFileName(latest)}";
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
            _grid.Rows.Add(item.Name, item.Category, $"{item.Confidence:P0}", item.Reason);
    }

    private void SetBusy(bool busy, string? status = null)
    {
        _browse.Enabled = !busy;
        _scan.Enabled = !busy;
        _sort.Enabled = !busy && _plan.Count > 0;
        _undo.Enabled = !busy;
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
