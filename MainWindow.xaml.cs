using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using HonsenToolbox.Models;
using Microsoft.Win32;
using Forms = System.Windows.Forms;
using WpfButton = System.Windows.Controls.Button;
using WpfColor = System.Windows.Media.Color;
using WpfColorConverter = System.Windows.Media.ColorConverter;
using WpfPoint = System.Windows.Point;

namespace HonsenToolbox;

public partial class MainWindow : Window
{
    private const int HotKeyId = 9001;
    private const uint ModControl = 0x0002;
    private const uint ModAlt = 0x0001;
    private const uint VirtualKeySpace = 0x20;
    private readonly List<ToolEntry> _tools = LoadCatalog();
    private readonly string _statePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HonsenToolbox", "state.json");
    private string? _installStatus;
    private int? _installProgress;
    private readonly Forms.NotifyIcon _trayIcon;
    private string _page = "favourites";
    private string _language = "zh";
    private bool _isExiting;
    private ToolEntry? _draggedTool;
    private FrameworkElement? _dragSource;
    private readonly DispatcherTimer _dragHoldTimer = new() { Interval = TimeSpan.FromMilliseconds(450) };
    private bool _suppressNextClick;
    private readonly System.Windows.Controls.Primitives.Popup _dragPopup = new() { AllowsTransparency = true, IsHitTestVisible = false, Placement = System.Windows.Controls.Primitives.PlacementMode.Relative, StaysOpen = true };
    private bool _isCardDragging;
    private static readonly HttpClient GitHubClient = CreateGitHubClient();

    public MainWindow()
    {
        InitializeComponent();
        _dragHoldTimer.Tick += DragHoldTimer_Tick;
        RegisterToolbox();
        LoadState();
        DiscoverConnectedApps();
        _trayIcon = CreateTrayIcon();
        ApplyLanguage();
        RefreshTools();
        UpdateNetworkStatus();
        SetAutostart(AutostartBox.IsChecked == true);
    }

    private static List<ToolEntry> LoadCatalog()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "catalog.json");
            var tools = JsonSerializer.Deserialize<List<ToolEntry>>(File.ReadAllText(path));
            if (tools is { Count: > 0 }) return tools;
        }
        catch { /* The compiled fallback keeps the launcher usable if the catalog is unavailable. */ }
        return CreateDefaultTools();
    }

    private static HttpClient CreateGitHubClient()
    {
        var client = new HttpClient();
        client.DefaultRequestHeaders.UserAgent.ParseAdd("HonsenToolbox/0.1");
        return client;
    }

    private static void RegisterToolbox()
    {
        const string appId = "honsen.toolbox";
        try
        {
            var installLocation = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            var executablePath = Environment.ProcessPath ?? Path.Combine(installLocation, "HonsenToolbox.exe");
            var version = typeof(MainWindow).Assembly.GetName().Version?.ToString(3) ?? "0.1.0";
            using var key = Registry.CurrentUser.CreateSubKey($@"Software\Honsen Program\Apps\{appId}");
            key.SetValue("AppId", appId);
            key.SetValue("DisplayName", "Honsen工具箱");
            key.SetValue("Version", version);
            key.SetValue("InstallLocation", installLocation);
            key.SetValue("ExecutablePath", executablePath);
            key.SetValue("InstallScope", "user");
            key.SetValue("Publisher", "Honsen");
            key.SetValue("UpdateManifestUrl", "");
        }
        catch
        {
            // Registration must never prevent the toolbox from opening.
        }
    }

    private static List<ToolEntry> CreateDefaultTools() =>
    [
        Web("terminal", "深圳弘盛·系统终端", "Shenzhen Honsen System Terminal", "Terminal système Honsen Shenzhen", "公司内部系统入口", "Company internal systems", "Portail des systèmes internes", "https://www.honsen.africa/", "⌂", true, 2),
        Desktop("wms", "仓库管理", "Honsen WMS", "Honsen WMS", "本地 LTS 仓储管理客户端", "Local LTS warehouse client", "Client local LTS de gestion d’entrepôt", "https://github.com/etianwang/Honsen_WMS/releases", "▣", false, 5),
        Web("wms-online", "仓库数据在线查看", "WMS Online Viewer", "Consultation WMS en ligne", "在线只读查看", "Online read-only view", "Consultation en ligne", "https://wms.honsen.africa/", "◫", false, 6),
        Web("edm", "图纸管理", "Drawing Management", "Gestion des plans", "图纸与工程资料", "Drawings and engineering documents", "Plans et documents d'ingénierie", "https://edm.honsen.africa/login", "⌑", false, 7),
        Web("tracking", "柜号跟踪", "Container Tracking", "Suivi de conteneurs", "当前为网页，后续支持桌面版", "Web service; desktop version planned", "Service Web ; version bureau prévue", "http://tracking.honsen.africa/", "⌁", false, 7),
        Web("attendance-cam", "喀麦隆团队考勤", "Cameroon Team Attendance", "Présence équipe Cameroun", "CAM 团队网页", "CAM team web service", "Service Web équipe CAM", "https://kq.honsen.africa/", "◷", false, 8),
        Web("attendance-et", "埃塞俄比亚团队考勤", "Ethiopia Team Attendance", "Présence équipe Éthiopie", "ETH 团队网页", "ETH team web service", "Service Web équipe ETH", "https://kq-et.honsen.africa/", "◷", false, 9),
        Web("drive", "企业网盘", "Company Drive", "Disque d'entreprise", "公司文件存储", "Company file storage", "Stockage des fichiers d'entreprise", "https://p.honsen.africa/", "□", false, 10),
        Desktop("cad-translator", "Honsen CAD 中英法图纸翻译器", "Honsen CAD Drawing Translator", "Traducteur de plans CAD Honsen", "中英法 CAD 图纸翻译", "Chinese, English and French CAD translation", "Traduction CAD chinois, anglais et français", "https://github.com/etianwang/CAD_translator/releases", "文", true, 0),
        Desktop("document-translator", "Honsen 文档翻译器", "Honsen Document Translator", "Traducteur de documents Honsen", "中英法文档翻译", "Chinese, English and French document translation", "Traduction de documents chinois, anglais et français", "https://github.com/etianwang/Honsen-Document-Translator/releases", "▤", true, 1),
    ];

    private static ToolEntry Web(string id, string zh, string en, string fr, string zhDesc, string enDesc, string frDesc, string url, string icon, bool favourite, int order) =>
        new() { Id = id, ChineseName = zh, EnglishName = en, FrenchName = fr, ChineseDescription = zhDesc, EnglishDescription = enDesc, FrenchDescription = frDesc, Url = url, Icon = icon, IsWeb = true, IsInstallable = false, IsFavourite = favourite, SortOrder = order };

    private static ToolEntry Desktop(string id, string zh, string en, string fr, string zhDesc, string enDesc, string frDesc, string url, string icon, bool favourite, int order) =>
        new() { Id = id, ChineseName = zh, EnglishName = en, FrenchName = fr, ChineseDescription = zhDesc, EnglishDescription = enDesc, FrenchDescription = frDesc, Url = url, Icon = icon, AppId = id switch { "cad-translator" => "honsen.cad-translator", "document-translator" => "honsen.document-translator", _ => null }, IsWeb = false, IsInstallable = true, IsFavourite = favourite, SortOrder = order };

    private void DiscoverConnectedApps()
    {
        foreach (var tool in _tools) { tool.InstalledVersion = null; tool.LauncherPath = null; tool.UpdateManifestUrl = null; tool.LatestVersion = null; }
        foreach (var tool in _tools.Where(tool => !string.IsNullOrWhiteSpace(tool.AppId)))
        {
            var app = FindInstalledApp(tool.AppId!);
            if (app is not null) { tool.InstalledVersion = app.Version; tool.LauncherPath = app.LauncherPath; tool.UpdateManifestUrl = app.UpdateManifestUrl; }
        }
    }

    private static ConnectedApp? FindInstalledApp(string appId)
    {
        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var key = baseKey.OpenSubKey($@"Software\Honsen Program\Apps\{appId}");
                if (key is null || !string.Equals(key.GetValue("AppId") as string, appId, StringComparison.Ordinal)) continue;
                var location = key.GetValue("InstallLocation") as string;
                var executable = key.GetValue("ExecutablePath") as string;
                var launcher = key.GetValue("LauncherPath") as string;
                var runner = key.GetValue("UpdateRunnerPath") as string;
                var version = key.GetValue("Version") as string;
                var updateManifestUrl = key.GetValue("UpdateManifestUrl") as string;
                if (location is null || executable is null || launcher is null || runner is null || version is null ||
                    !File.Exists(executable) || !File.Exists(launcher) || !File.Exists(runner) ||
                    !IsInside(location, executable) || !IsInside(location, launcher) || !IsInside(location, runner)) continue;
                var manifest = Path.Combine(location, "honsen.app.json");
                using var json = JsonDocument.Parse(File.ReadAllText(manifest));
                if (!json.RootElement.TryGetProperty("appId", out var manifestId) || manifestId.GetString() != appId) continue;
                return new ConnectedApp(version, launcher, updateManifestUrl);
            }
            catch { /* Invalid third-party registry or manifest data is ignored. */ }
        }
        return null;
    }

    private void RepairInstall_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new Forms.FolderBrowserDialog { Description = Text("选择应用的安装目录", "Select the app installation folder", "Sélectionnez le dossier d’installation") };
        if (dialog.ShowDialog() != Forms.DialogResult.OK || string.IsNullOrWhiteSpace(dialog.SelectedPath)) return;
        if (!TryReadInstallFolder(dialog.SelectedPath, out var recovered, out var reason))
        {
            System.Windows.MessageBox.Show(reason, Text("无法修复", "Could not repair", "Réparation impossible"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (_tools.FirstOrDefault(tool => tool.AppId == recovered.AppId)?.HasConnectedRunner == true)
        {
            System.Windows.MessageBox.Show(Text("此应用已在工具箱中登记，无需修复。", "This app is already registered in the toolbox.", "Cette application est déjà enregistrée dans la boîte à outils."), Text("无需修复", "No repair needed", "Aucune réparation nécessaire"), MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }
        var prompt = Text($"恢复 {recovered.DisplayName} v{recovered.Version} 的工具箱登记？\n\n{recovered.InstallLocation}", $"Restore toolbox registration for {recovered.DisplayName} v{recovered.Version}?\n\n{recovered.InstallLocation}", $"Restaurer l’inscription de {recovered.DisplayName} v{recovered.Version} ?\n\n{recovered.InstallLocation}");
        if (System.Windows.MessageBox.Show(prompt, Text("确认恢复", "Confirm restore", "Confirmer la restauration"), MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        using var key = Registry.CurrentUser.CreateSubKey($@"Software\Honsen Program\Apps\{recovered.AppId}");
        key.SetValue("AppId", recovered.AppId); key.SetValue("DisplayName", recovered.DisplayName); key.SetValue("Version", recovered.Version);
        key.SetValue("InstallLocation", recovered.InstallLocation); key.SetValue("ExecutablePath", recovered.ExecutablePath); key.SetValue("LauncherPath", recovered.RunnerPath);
        key.SetValue("UpdateRunnerPath", recovered.RunnerPath); key.SetValue("UpdateManifestUrl", recovered.UpdateManifestUrl); key.SetValue("UpdateUrl", recovered.UpdateManifestUrl);
        key.SetValue("InstallScope", "user"); key.SetValue("Publisher", recovered.Publisher);
        DiscoverConnectedApps(); RefreshTools();
        PageDescription.Text = Text($"已恢复 {recovered.DisplayName} 的本机登记。", $"Restored local registration for {recovered.DisplayName}.", $"Inscription locale restaurée pour {recovered.DisplayName}.");
    }

    private bool TryReadInstallFolder(string directory, out RecoveredApp app, out string reason)
    {
        app = default!; reason = Text("该目录不是受支持的 Honsen 应用安装目录。", "This folder is not a supported Honsen app installation.", "Ce dossier n’est pas une installation Honsen prise en charge.");
        try
        {
            var location = Path.GetFullPath(directory);
            var manifestPath = Path.Combine(location, "honsen.app.json");
            using var json = JsonDocument.Parse(File.ReadAllText(manifestPath));
            var root = json.RootElement;
            string? Value(string name) => root.TryGetProperty(name, out var value) ? value.GetString() : null;
            var appId = Value("appId"); var executableName = Value("executable"); var runnerName = Value("updateRunner");
            if (_tools.All(tool => tool.AppId != appId) || string.IsNullOrWhiteSpace(executableName) || string.IsNullOrWhiteSpace(runnerName)) return false;
            var executable = Path.GetFullPath(Path.Combine(location, executableName));
            var runner = Path.GetFullPath(Path.Combine(location, runnerName));
            if (!IsInside(location, executable) || !IsInside(location, runner) || !File.Exists(executable) || !File.Exists(runner)) return false;
            app = new RecoveredApp(appId!, Value("displayName") ?? appId!, Value("version") ?? "0.0.0", location, executable, runner, Value("publisher") ?? "Honsen", Value("updateManifestUrl") ?? "");
            return true;
        }
        catch { return false; }
    }

    private static bool IsInside(string directory, string path)
    {
        var root = Path.TrimEndingDirectorySeparator(Path.GetFullPath(directory)) + Path.DirectorySeparatorChar;
        return Path.GetFullPath(path).StartsWith(root, StringComparison.OrdinalIgnoreCase);
    }

    private void RefreshTools()
    {
        var query = SearchBox?.Text.Trim() ?? "";
        IEnumerable<ToolEntry> displayed = _tools;
        if (string.IsNullOrWhiteSpace(query))
        {
            if (_page == "favourites") displayed = displayed.Where(tool => tool.IsFavourite);
            if (_page == "install") displayed = displayed.Where(tool => tool.IsInstallable);
            if (_page == "updates") displayed = displayed.Where(tool => tool.HasConnectedRunner);
        }
        else displayed = displayed.Where(tool => Matches(tool, query));

        foreach (var tool in displayed)
        {
            tool.DisplayName = tool.Name(_language);
            tool.DisplayDescription = tool.HasConnectedRunner ? $"{tool.Description(_language)} · {Text("已安装", "Installed", "Installé")}" : tool.Description(_language);
            tool.DisplayVersionInfo = tool.HasConnectedRunner ? $"{Text("当前版本", "Current", "Actuelle")} v{tool.InstalledVersion} · {Text("最新版本", "Latest", "Dernière")} {(tool.LatestVersion is null ? "—" : $"v{tool.LatestVersion}")}" : "";
            tool.DisplayType = tool.IsWeb ? Text("网页服务", "Web service", "Service Web") : Text("桌面应用", "Desktop app", "Application bureau");
            tool.DisplayFavouriteSymbol = tool.IsFavourite ? "★" : "☆";
            tool.DisplayActionText = _page == "updates" && tool.HasConnectedRunner ? Text("检查更新 ↗", "Check updates ↗", "Vérifier ↗") : tool.AppId is not null && !tool.HasConnectedRunner ? Text("安装 ↗", "Install ↗", "Installer ↗") : Text("打开 ↗", "Open ↗", "Ouvrir ↗");
        }

        var results = displayed.OrderBy(tool => tool.SortOrder).ThenByDescending(tool => tool.LastOpenedUtc).ToList();
        ToolsList.ItemsSource = results;
        InstallProgressPanel.Visibility = _page == "install" && _installStatus is not null ? Visibility.Visible : Visibility.Collapsed;
        SectionTitle.Text = !string.IsNullOrWhiteSpace(query) ? Text($"搜索结果 · {results.Count}", $"Search results · {results.Count}", $"Résultats · {results.Count}") : _page == "updates" ? Text($"已接入 {results.Count} 个桌面应用", $"{results.Count} desktop apps connected", $"{results.Count} applications connectées") : PageSection();
    }

    private bool Matches(ToolEntry tool, string query) =>
        new[] { tool.ChineseName, tool.EnglishName, tool.FrenchName, tool.ChineseDescription, tool.EnglishDescription, tool.FrenchDescription }
            .Any(value => value.Contains(query, StringComparison.OrdinalIgnoreCase));

    private void ApplyLanguage()
    {
        Title = Text("Honsen工具箱", "Honsen Toolbox", "Boîte à outils Honsen");
        PageTitle.Text = _page switch
        {
            "all" => Text("全部工具", "All tools", "Tous les outils"),
            "install" => Text("可安装工具", "Available tools", "Outils disponibles"),
            "updates" => Text("更新中心", "Update center", "Centre de mises à jour"),
            _ => Text("收藏的工具", "Favourite tools", "Outils favoris"),
        };
        PageDescription.Text = Text("点击卡片打开；点击右上角星标管理收藏。", "Click a card to open it; use the star to manage favourites.", "Cliquez pour ouvrir ; utilisez l’étoile pour gérer les favoris.");
        RepairInstallButton.Content = Text("修复已安装应用", "Repair installed app", "Réparer une application");
        RepairInstallButton.Visibility = _page == "updates" ? Visibility.Visible : Visibility.Collapsed;
        FavouriteNavLabel.Text = Text("已收藏", "Favourites", "Favoris");
        AllNavLabel.Text = Text("全部工具", "All tools", "Tous les outils");
        InstallNavLabel.Text = Text("可安装", "Available", "Disponibles");
        UpdatesNavLabel.Text = Text("更新中心", "Updates", "Mises à jour");
        AutostartBox.Content = Text("开机自动启动", "Start with Windows", "Démarrer avec Windows");
        RefreshTools();
    }

    private string Text(string zh, string en, string fr) => _language switch { "en" => en, "fr" => fr, _ => zh };
    private string PageSection() => _page switch { "all" => Text("全部工具", "All tools", "Tous les outils"), "install" => Text("可安装", "Available", "Disponibles"), _ => Text("已收藏", "Favourites", "Favoris") };

    private async void Tool_Click(object sender, RoutedEventArgs e)
    {
        if (_suppressNextClick) { _suppressNextClick = false; return; }
        if (((FrameworkElement)sender).Tag is not ToolEntry tool) return;
        await OpenToolAsync(tool);
    }

    private async Task OpenToolAsync(ToolEntry tool)
    {
        tool.LastOpenedUtc = DateTime.UtcNow;
        SaveState();
        if (tool.HasConnectedRunner) { if (_page == "updates") await CheckForUpdatesAsync(tool); else await LaunchConnectedToolAsync(tool); return; }
        if (tool.AppId is not null) { await InstallKnownToolAsync(tool); return; }
        Process.Start(new ProcessStartInfo(tool.Url) { UseShellExecute = true });
        RefreshTools();
    }

    private void ToolCard_ContextMenuOpened(object sender, RoutedEventArgs e)
    {
        if (sender is not ContextMenu menu || menu.PlacementTarget is not WpfButton { Tag: ToolEntry tool }) return;
        menu.Items.Clear();
        if (tool.HasConnectedRunner)
        {
            menu.Items.Add(MenuItem(Text("打开", "Open", "Ouvrir"), async () => await OpenToolAsync(tool)));
            menu.Items.Add(MenuItem(Text("检查更新", "Check for updates", "Vérifier les mises à jour"), async () => await CheckForUpdatesAsync(tool)));
            menu.Items.Add(MenuItem(Text("打开程序所在目录", "Open program folder", "Ouvrir le dossier du programme"), () => Process.Start("explorer.exe", Path.GetDirectoryName(tool.LauncherPath)!)));
            menu.Items.Add(new Separator());
            menu.Items.Add(MenuItem(Text("卸载", "Uninstall", "Désinstaller"), async () => await UninstallToolAsync(tool)));
        }
        else if (tool.AppId is not null)
        {
            menu.Items.Add(MenuItem(Text("安装", "Install", "Installer"), async () => await InstallKnownToolAsync(tool)));
        }
        menu.Items.Add(MenuItem(tool.IsWeb ? Text("打开网页", "Open website", "Ouvrir le site") : Text("打开 Release 发布页", "Open Release page", "Ouvrir la page Release"), () => Process.Start(new ProcessStartInfo(tool.Url) { UseShellExecute = true })));
    }

    private static MenuItem MenuItem(string label, Action action)
    {
        var item = new MenuItem { Header = label };
        item.Click += (_, _) => action();
        return item;
    }

    private async Task UninstallToolAsync(ToolEntry tool)
    {
        var location = Path.GetDirectoryName(tool.LauncherPath!);
        if (location is null || !TryGetUninstaller(location, out var command))
        {
            System.Windows.MessageBox.Show(Text("未找到该应用的受信任卸载器。", "No trusted uninstaller was found for this app.", "Aucun programme de désinstallation fiable n’a été trouvé."), Text("无法卸载", "Cannot uninstall", "Désinstallation impossible"), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        var prompt = Text($"确定卸载 {tool.DisplayName}？\n\n这会删除该应用及其本机安装登记。", $"Uninstall {tool.DisplayName}?\n\nThis removes the app and its local registration.", $"Désinstaller {tool.DisplayName} ?\n\nL’application et son inscription locale seront supprimées.");
        if (System.Windows.MessageBox.Show(prompt, Text("确认卸载", "Confirm uninstall", "Confirmer la désinstallation"), MessageBoxButton.YesNo, MessageBoxImage.Warning) != MessageBoxResult.Yes) return;
        try
        {
            using var process = Process.Start(new ProcessStartInfo(command.Executable, command.Arguments) { UseShellExecute = false, WorkingDirectory = location }) ?? throw new InvalidOperationException();
            await process.WaitForExitAsync();
            if (process.ExitCode != 0) throw new InvalidOperationException($"卸载器退出码：{process.ExitCode}");
            DiscoverConnectedApps(); RefreshTools();
            PageDescription.Text = Text($"{tool.DisplayName} 已卸载。", $"{tool.DisplayName} was uninstalled.", $"{tool.DisplayName} a été désinstallé.");
        }
        catch (Exception error)
        {
            System.Windows.MessageBox.Show(error.Message, Text("卸载未完成", "Uninstall did not complete", "Désinstallation incomplète"), MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private static bool TryGetUninstaller(string installLocation, out UninstallCommand command)
    {
        foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            try
            {
                using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                using var root = baseKey.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Uninstall");
                if (root is null) continue;
                foreach (var name in root.GetSubKeyNames())
                {
                    using var key = root.OpenSubKey(name);
                    if (key is null || !string.Equals(Path.TrimEndingDirectorySeparator(key.GetValue("InstallLocation") as string ?? ""), Path.TrimEndingDirectorySeparator(installLocation), StringComparison.OrdinalIgnoreCase)) continue;
                    if (SplitCommand(key.GetValue("QuietUninstallString") as string, out command) && File.Exists(command.Executable) && IsInside(installLocation, command.Executable)) return true;
                }
            }
            catch { /* Invalid uninstall registry data is ignored. */ }
        }
        command = default!;
        return false;
    }

    private static bool SplitCommand(string? value, out UninstallCommand command)
    {
        command = default!;
        if (string.IsNullOrWhiteSpace(value)) return false;
        var text = value.Trim();
        var end = text.StartsWith('"') ? text.IndexOf('"', 1) : text.IndexOf(' ');
        if (end < 1) { command = new UninstallCommand(text.Trim('"'), ""); return true; }
        command = new UninstallCommand(text[..end].Trim('"'), text[(end + 1)..].Trim());
        return true;
    }

    private async Task LaunchConnectedToolAsync(ToolEntry tool)
    {
        try
        {
            var operationId = Guid.NewGuid().ToString();
            var resultPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Honsen Program", "UpdateResults", tool.AppId!, $"{operationId}.json");
            Directory.CreateDirectory(Path.GetDirectoryName(resultPath)!);
            var start = new ProcessStartInfo(tool.LauncherPath!) { UseShellExecute = true };
            start.ArgumentList.Add("launch"); start.ArgumentList.Add("--app-id"); start.ArgumentList.Add(tool.AppId!); start.ArgumentList.Add("--source"); start.ArgumentList.Add("toolbox");
            start.ArgumentList.Add("--wait-pid"); start.ArgumentList.Add("0"); start.ArgumentList.Add("--operation-id"); start.ArgumentList.Add(operationId); start.ArgumentList.Add("--result-path"); start.ArgumentList.Add(resultPath);
            PageDescription.Text = Text($"正在通过更新服务打开 {tool.DisplayName}…", $"Opening {tool.DisplayName} through its update service…", $"Ouverture de {tool.DisplayName} via le service de mise à jour…");
            using var process = Process.Start(start) ?? throw new InvalidOperationException("无法启动更新服务。");
            await process.WaitForExitAsync();
            if (File.Exists(resultPath))
            {
                using var result = JsonDocument.Parse(File.ReadAllText(resultPath));
                var status = result.RootElement.TryGetProperty("status", out var value) ? value.GetString() : null;
                PageDescription.Text = status == "success" ? Text($"{tool.DisplayName} 已完成检查。", $"{tool.DisplayName} finished checking.", $"Vérification terminée pour {tool.DisplayName}.") : Text($"{tool.DisplayName} 更新服务未完成操作。", $"{tool.DisplayName} did not complete the update action.", $"Le service de mise à jour de {tool.DisplayName} n’a pas terminé l’opération.");
            }
        }
        catch
        {
            PageDescription.Text = Text($"无法调用 {tool.DisplayName} 的更新服务。", $"Could not call {tool.DisplayName}'s update service.", $"Impossible d’appeler le service de mise à jour de {tool.DisplayName}.");
        }
    }

    private async Task CheckForUpdatesAsync(ToolEntry tool)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(tool.UpdateManifestUrl)) throw new InvalidOperationException("应用未提供更新地址。");
            PageDescription.Text = Text($"正在检查 {tool.DisplayName} 的更新…", $"Checking updates for {tool.DisplayName}…", $"Vérification des mises à jour de {tool.DisplayName}…");
            if (!Version.TryParse(tool.InstalledVersion, out var installed)) throw new InvalidOperationException("本机版本格式无法比较。");
            var available = await GetLatestVersionAsync(tool.UpdateManifestUrl);
            tool.LatestVersion = available.ToString();
            RefreshTools();
            PageDescription.Text = available > installed
                ? Text($"发现新版本 v{available}（当前 v{installed}）。", $"Version v{available} is available (current: v{installed}).", $"La version v{available} est disponible (actuelle : v{installed}).")
                : Text($"{tool.DisplayName} 已是最新版本 v{installed}。", $"{tool.DisplayName} is up to date (v{installed}).", $"{tool.DisplayName} est à jour (v{installed}).");
        }
        catch (Exception error)
        {
            PageDescription.Text = Text($"无法检查 {tool.DisplayName} 的更新：{error.Message}", $"Could not check updates for {tool.DisplayName}: {error.Message}", $"Impossible de vérifier les mises à jour de {tool.DisplayName} : {error.Message}");
        }
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e) => await RefreshLatestVersionsAsync();

    private async Task RefreshLatestVersionsAsync()
    {
        foreach (var tool in _tools.Where(tool => tool.HasConnectedRunner && !string.IsNullOrWhiteSpace(tool.UpdateManifestUrl)))
        {
            try { tool.LatestVersion = (await GetLatestVersionAsync(tool.UpdateManifestUrl!)).ToString(); }
            catch { tool.LatestVersion = null; }
        }
        RefreshTools();
    }

    private static async Task<Version> GetLatestVersionAsync(string updateManifestUrl)
    {
        using var release = JsonDocument.Parse(await GitHubClient.GetStringAsync(updateManifestUrl));
        var latest = release.RootElement.GetProperty("tag_name").GetString()?.Trim().TrimStart('v', 'V');
        return Version.TryParse(latest, out var version) ? version : throw new InvalidOperationException("发布版本格式无法比较。");
    }

    private async Task InstallKnownToolAsync(ToolEntry tool)
    {
        var package = tool.AppId switch
        {
            "honsen.cad-translator" => new InstallPackage("etianwang/CAD_translator", "Honsen DrawTranslate"),
            "honsen.document-translator" => new InstallPackage("etianwang/Honsen-Document-Translator", "Honsen Document Translator"),
            _ => null,
        };
        if (package is null) { Process.Start(new ProcessStartInfo(tool.Url) { UseShellExecute = true }); return; }
        try
        {
            SetInstallStatus(Text($"正在查询 {tool.DisplayName} 的安装包…", $"Finding {tool.DisplayName}'s installer…", $"Recherche du programme d’installation de {tool.DisplayName}…"));
            var release = await GitHubClient.GetStringAsync($"https://api.github.com/repos/{package.Repository}/releases/latest");
            using var releaseJson = JsonDocument.Parse(release);
            var asset = releaseJson.RootElement.GetProperty("assets").EnumerateArray().FirstOrDefault(item => item.GetProperty("name").GetString()?.EndsWith("Setup.exe", StringComparison.OrdinalIgnoreCase) == true);
            var name = asset.ValueKind == JsonValueKind.Undefined ? null : asset.GetProperty("name").GetString();
            var url = asset.ValueKind == JsonValueKind.Undefined ? null : asset.GetProperty("browser_download_url").GetString();
            var digest = asset.ValueKind == JsonValueKind.Undefined ? null : asset.GetProperty("digest").GetString();
            if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(digest) || !digest.StartsWith("sha256:", StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("发布版本没有可校验的安装包。");
            var downloadDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HonsenToolbox", "Downloads", tool.AppId!);
            Directory.CreateDirectory(downloadDirectory);
            var installer = Path.Combine(downloadDirectory, name!);
            await DownloadInstallerAsync(url, installer, tool);
            string actualHash;
            await using (var installerStream = File.OpenRead(installer)) actualHash = Convert.ToHexString(await SHA256.HashDataAsync(installerStream));
            if (!string.Equals(actualHash, digest[7..], StringComparison.OrdinalIgnoreCase)) throw new InvalidOperationException("安装包 SHA-256 校验失败。");
            var target = Path.Combine(GetHonsenProgramRoot(), package.DirectoryName);
            SetInstallStatus(Text($"正在静默安装 {tool.DisplayName}…", $"Installing {tool.DisplayName} silently…", $"Installation silencieuse de {tool.DisplayName}…"));
            var start = new ProcessStartInfo(installer) { UseShellExecute = false, WorkingDirectory = Path.GetDirectoryName(installer)!, Arguments = $"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- /DIR=\"{target}\"" };
            using var process = Process.Start(start) ?? throw new InvalidOperationException("无法启动安装器。");
            await process.WaitForExitAsync();
            if (process.ExitCode != 0) throw new InvalidOperationException($"安装器退出码：{process.ExitCode}");
            DiscoverConnectedApps(); RefreshTools();
            if (!tool.HasConnectedRunner) throw new InvalidOperationException("安装完成后未检测到有效的 Honsen 应用登记。");
            SetInstallStatus(Text($"{tool.DisplayName} 安装完成。", $"{tool.DisplayName} is installed.", $"{tool.DisplayName} est installé."), 100);
        }
        catch (Exception error)
        {
            SetInstallStatus(Text($"{tool.DisplayName} 安装未完成：{error.Message}", $"{tool.DisplayName} installation did not complete: {error.Message}", $"L’installation de {tool.DisplayName} n’a pas abouti : {error.Message}"));
        }
    }

    private async Task DownloadInstallerAsync(string url, string destination, ToolEntry tool)
    {
        using var response = await GitHubClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();
        var total = response.Content.Headers.ContentLength;
        await using var source = await response.Content.ReadAsStreamAsync();
        await using var target = File.Create(destination);
        var buffer = new byte[81920]; long received = 0; int read;
        while ((read = await source.ReadAsync(buffer)) > 0)
        {
            await target.WriteAsync(buffer.AsMemory(0, read)); received += read;
            var progress = total is > 0 ? $" {received * 100 / total.Value}%" : "";
            SetInstallStatus(Text($"正在下载 {tool.DisplayName}{progress}", $"Downloading {tool.DisplayName}{progress}", $"Téléchargement de {tool.DisplayName}{progress}"), total is > 0 ? (int)(received * 100 / total.Value) : null);
        }
    }

    private void SetInstallStatus(string status, int? progress = null)
    {
        _installStatus = status;
        _installProgress = progress;
        InstallProgressText.Text = status;
        InstallProgressBar.IsIndeterminate = progress is null;
        if (progress is not null) InstallProgressBar.Value = progress.Value;
        InstallProgressPanel.Visibility = _page == "install" ? Visibility.Visible : Visibility.Collapsed;
    }

    private static string GetHonsenProgramRoot()
    {
        var overrideRoot = Environment.GetEnvironmentVariable("HONSEN_PROGRAM_ROOT");
        if (!string.IsNullOrWhiteSpace(overrideRoot)) return Path.GetFullPath(overrideRoot);
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        for (var directory = current; directory.Parent is not null; directory = directory.Parent)
            if (string.Equals(directory.Name, "Honsen Program", StringComparison.OrdinalIgnoreCase)) return directory.FullName;
        throw new InvalidOperationException("工具箱必须安装在 Honsen Program 目录中。");
    }

    private void FavouriteStar_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (((FrameworkElement)sender).Tag is not ToolEntry tool) return;
        tool.IsFavourite = !tool.IsFavourite;
        SaveState();
        RefreshTools();
        e.Handled = true;
    }

    private void ToolCard_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (_page != "favourites" || !string.IsNullOrWhiteSpace(SearchBox.Text) || ((FrameworkElement)sender).Tag is not ToolEntry tool) return;
        if (e.OriginalSource is FrameworkElement { Tag: ToolEntry }) return;
        _draggedTool = tool;
        _dragSource = (FrameworkElement)sender;
        _dragHoldTimer.Start();
    }

    private void ToolCard_PreviewMouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        _dragHoldTimer.Stop();
        if (_isCardDragging)
        {
            FinishCardDrag(e.GetPosition(this));
            e.Handled = true;
        }
        _draggedTool = null;
        _dragSource = null;
    }

    private void ToolCard_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (_isCardDragging) MoveCardDragPreview(e.GetPosition(this));
    }

    private void DragHoldTimer_Tick(object? sender, EventArgs e)
    {
        _dragHoldTimer.Stop();
        if (_draggedTool is null || _dragSource is null || Mouse.LeftButton != MouseButtonState.Pressed) return;
        _suppressNextClick = true;
        var tool = _draggedTool;
        _draggedTool = null;
        BeginCardDragPreview(_dragSource, tool);
    }

    private void BeginCardDragPreview(FrameworkElement card, ToolEntry tool)
    {
        var width = Math.Max(1, (int)Math.Ceiling(card.ActualWidth));
        var height = Math.Max(1, (int)Math.Ceiling(card.ActualHeight));
        var snapshot = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
        snapshot.Render(card);
        _dragPopup.PlacementTarget = this;
        _dragPopup.Child = new Border { Width = card.ActualWidth, Height = card.ActualHeight, Opacity = 0.92, Child = new System.Windows.Controls.Image { Source = snapshot } };
        card.Opacity = 0.35;
        _isCardDragging = true;
        Mouse.Capture(card);
        MoveCardDragPreview(Mouse.GetPosition(this));
        _dragPopup.IsOpen = true;
    }

    private void MoveCardDragPreview(System.Windows.Point cursor)
    {
        if (_dragSource is null) return;
        _dragPopup.HorizontalOffset = cursor.X - (_dragSource.ActualWidth / 2);
        _dragPopup.VerticalOffset = cursor.Y - 22;
    }

    private void FinishCardDrag(System.Windows.Point cursor)
    {
        if (_dragSource is not null)
        {
            _dragSource.Opacity = 1;
            Mouse.Capture(null);
        }
        _dragPopup.IsOpen = false;
        _isCardDragging = false;
        var hit = VisualTreeHelper.HitTest(this, cursor)?.VisualHit;
        var target = FindToolEntry(hit);
        if (_dragSource?.Tag is ToolEntry source && target is not null && target != source) ReorderFavourite(source, target);
    }

    private void ToolsList_DragOver(object sender, System.Windows.DragEventArgs e) => e.Effects = e.Data.GetDataPresent(typeof(ToolEntry)) ? System.Windows.DragDropEffects.Move : System.Windows.DragDropEffects.None;

    private void ToolsList_Drop(object sender, System.Windows.DragEventArgs e)
    {
        if (_page != "favourites" || e.Data.GetData(typeof(ToolEntry)) is not ToolEntry source) return;
        var target = FindToolEntry(e.OriginalSource as DependencyObject);
        if (target is null || target == source) return;
        ReorderFavourite(source, target);
    }

    private void ReorderFavourite(ToolEntry source, ToolEntry target)
    {
        var favourites = _tools.Where(tool => tool.IsFavourite).OrderBy(tool => tool.SortOrder).ToList();
        favourites.Remove(source);
        favourites.Insert(favourites.IndexOf(target), source);
        for (var index = 0; index < favourites.Count; index++) favourites[index].SortOrder = index;
        SaveState(); RefreshTools();
    }

    private static ToolEntry? FindToolEntry(DependencyObject? element)
    {
        while (element is not null)
        {
            if (element is FrameworkElement { DataContext: ToolEntry tool }) return tool;
            element = VisualTreeHelper.GetParent(element);
        }
        return null;
    }

    private void Nav_Click(object sender, RoutedEventArgs e)
    {
        _page = ReferenceEquals(sender, AllNav) ? "all" : ReferenceEquals(sender, InstallNav) ? "install" : ReferenceEquals(sender, UpdatesNav) ? "updates" : "favourites";
        foreach (var button in new[] { FavouriteNav, AllNav, InstallNav, UpdatesNav }) button.Tag = null;
        ((WpfButton)sender).Tag = "selected";
        ApplyLanguage();
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e) => RefreshTools();
    private void ClearSearch_Click(object sender, RoutedEventArgs e) => SearchBox.Clear();

    private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control && e.Key == Key.K)
        {
            SearchBox.Focus();
            e.Handled = true;
        }
    }

    private void LanguageBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (LanguageBox.SelectedItem is not ComboBoxItem { Tag: string language }) return;
        _language = language;
        if (PageTitle is null) return;
        ApplyLanguage();
        SaveState();
    }

    private void Theme_Click(object sender, RoutedEventArgs e)
    {
        var dark = ThemeBrush("Surface.App").Color.R < 100;
        SetBrush("Surface.App", dark ? "#F4F3EE" : "#1B211E"); SetBrush("Surface.Nav", dark ? "#E8E7E1" : "#242D28"); SetBrush("Surface.Card", dark ? "#FFFFFF" : "#2D3831");
        SetBrush("Surface.Selected", dark ? "#D1E0D6" : "#395746"); SetBrush("Surface.Hover", dark ? "#EDF4EF" : "#354137"); SetBrush("Text.Primary", dark ? "#242925" : "#F1F5F0");
        SetBrush("Text.Secondary", dark ? "#59625B" : "#CBD5CC"); SetBrush("Text.Muted", dark ? "#6C726D" : "#AAB7AD"); SetBrush("Border", dark ? "#C8CAC2" : "#4A584E"); SetBrush("Border.Subtle", dark ? "#D7D7D0" : "#3D4B42");
    }

    private SolidColorBrush ThemeBrush(string key)
    {
        var dictionary = System.Windows.Application.Current.Resources.MergedDictionaries.First(source => source.Contains(key));
        return (SolidColorBrush)dictionary[key];
    }

    private void SetBrush(string key, string hex)
    {
        var dictionary = System.Windows.Application.Current.Resources.MergedDictionaries.First(source => source.Contains(key));
        dictionary[key] = new SolidColorBrush((WpfColor)WpfColorConverter.ConvertFromString(hex));
    }

    private void Autostart_Changed(object sender, RoutedEventArgs e) => SetAutostart(AutostartBox.IsChecked == true);

    private void SetAutostart(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey("Software\\Microsoft\\Windows\\CurrentVersion\\Run");
        if (enabled) key?.SetValue("HonsenToolbox", $"\"{Environment.ProcessPath}\""); else key?.DeleteValue("HonsenToolbox", false);
    }

    private void UpdateNetworkStatus() => NetworkStatus.Text = NetworkInterface.GetIsNetworkAvailable() ? Text("● 网络可用", "● Network available", "● Réseau disponible") : Text("● 离线：网页和更新服务不可用", "● Offline: web and update services are unavailable", "● Hors ligne : les services Web et de mise à jour sont indisponibles");

    private Forms.NotifyIcon CreateTrayIcon()
    {
        var menu = new Forms.ContextMenuStrip();
        menu.Items.Add(Text("打开界面", "Open", "Ouvrir"), null, (_, _) => ShowFromTray());
        menu.Items.Add(Text("退出工具箱", "Exit Toolbox", "Quitter"), null, (_, _) => { _isExiting = true; _trayIcon.Visible = false; System.Windows.Application.Current.Shutdown(); });
        var icon = new Forms.NotifyIcon { Icon = new System.Drawing.Icon(Path.Combine(AppContext.BaseDirectory, "logo.ico")), Text = "Honsen工具箱", ContextMenuStrip = menu, Visible = true };
        icon.DoubleClick += (_, _) => ShowFromTray();
        return icon;
    }

    private void ShowFromTray() { Show(); WindowState = WindowState.Normal; Activate(); }
    private void Window_Closing(object? sender, System.ComponentModel.CancelEventArgs e) { if (!_isExiting) { e.Cancel = true; Hide(); } }

    private void Window_SourceInitialized(object? sender, EventArgs e)
    {
        var helper = new System.Windows.Interop.WindowInteropHelper(this);
        RegisterHotKey(helper.Handle, HotKeyId, ModControl | ModAlt, VirtualKeySpace);
        System.Windows.Interop.HwndSource.FromHwnd(helper.Handle)?.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == 0x0312 && wParam.ToInt32() == HotKeyId) { ShowFromTray(); SearchBox.Focus(); handled = true; }
        return IntPtr.Zero;
    }

    protected override void OnClosed(EventArgs e)
    {
        var helper = new System.Windows.Interop.WindowInteropHelper(this);
        UnregisterHotKey(helper.Handle, HotKeyId); _trayIcon.Dispose(); base.OnClosed(e);
    }

    private void LoadState()
    {
        try
        {
            if (!File.Exists(_statePath)) return;
            var state = JsonSerializer.Deserialize<UserState>(File.ReadAllText(_statePath));
            if (state is null) return;
            _language = state.Language ?? _language; LanguageBox.SelectedIndex = _language == "en" ? 1 : _language == "fr" ? 2 : 0; AutostartBox.IsChecked = state.Autostart;
            foreach (var saved in state.Tools)
                if (_tools.FirstOrDefault(tool => tool.Id == saved.Id) is { } tool) { tool.IsFavourite = saved.IsFavourite; tool.SortOrder = saved.SortOrder; tool.LastOpenedUtc = saved.LastOpenedUtc; }
        }
        catch { /* Local preferences are optional; a corrupt file is ignored. */ }
    }

    private void SaveState()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_statePath)!);
        var state = new UserState { Language = _language, Autostart = AutostartBox.IsChecked == true, Tools = _tools.Select(tool => new SavedTool(tool.Id, tool.IsFavourite, tool.SortOrder, tool.LastOpenedUtc)).ToList() };
        File.WriteAllText(_statePath, JsonSerializer.Serialize(state));
    }

    private sealed class UserState { public string? Language { get; set; } public bool Autostart { get; set; } = true; public List<SavedTool> Tools { get; set; } = []; }
    private sealed record SavedTool(string Id, bool IsFavourite, int SortOrder, DateTime? LastOpenedUtc);
    private sealed record ConnectedApp(string Version, string LauncherPath, string? UpdateManifestUrl);
    private sealed record UninstallCommand(string Executable, string Arguments);
    private sealed record RecoveredApp(string AppId, string DisplayName, string Version, string InstallLocation, string ExecutablePath, string RunnerPath, string Publisher, string UpdateManifestUrl);
    private sealed record InstallPackage(string Repository, string DirectoryName);

    [DllImport("user32.dll")] private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
    [DllImport("user32.dll")] private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
}
