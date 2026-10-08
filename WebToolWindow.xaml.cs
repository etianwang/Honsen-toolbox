using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;

namespace HonsenToolbox;

public partial class WebToolWindow : Window
{
    private readonly Uri _homeAddress;
    private readonly string _toolTitle;
    private readonly string _language;
    private readonly Dictionary<WebView2, TextBlock> _tabTitles = [];

    public WebToolWindow(string title, string address, string language)
    {
        InitializeComponent();
        _homeAddress = new Uri(address, UriKind.Absolute);
        _toolTitle = title;
        _language = language;
        Title = title;
        ApplyLanguage();
        if (HasWebViewRuntime()) AddTab(_homeAddress);
        else
        {
            PageTitle.Text = Text("缺少网页运行时", "Web runtime unavailable", "Moteur Web indisponible");
            AddressText.Text = Text("请安装 Microsoft Edge WebView2 Runtime，或使用默认浏览器打开。", "Install Microsoft Edge WebView2 Runtime, or use the default browser.", "Installez Microsoft Edge WebView2 Runtime ou utilisez le navigateur par défaut.");
            InstallRuntimeButton.Visibility = Visibility.Visible;
            LoadingPanel.Visibility = Visibility.Collapsed;
        }
    }

    private static bool HasWebViewRuntime()
    {
        try { return !string.IsNullOrWhiteSpace(CoreWebView2Environment.GetAvailableBrowserVersionString()); }
        catch { return false; }
    }

    private string Text(string zh, string en, string fr) => _language switch { "en" => en, "fr" => fr, _ => zh };

    private void ApplyLanguage()
    {
        DefaultBrowserButton.Content = Text("在默认浏览器打开", "Open in default browser", "Ouvrir dans le navigateur par défaut");
        DefaultBrowserButton.ToolTip = DefaultBrowserButton.Content;
        InstallRuntimeButton.Content = Text("安装网页运行时", "Install web runtime", "Installer le moteur Web");
        LoadingText.Text = Text("正在打开网页…", "Opening page…", "Ouverture de la page…");
        SetLabel(ReloadButton, Text("刷新", "Refresh", "Actualiser"));
        SetLabel(ForwardButton, Text("前进", "Forward", "Suivant"));
        SetLabel(BackButton, Text("后退", "Back", "Précédent"));
    }

    private static void SetLabel(System.Windows.Controls.Control control, string text)
    {
        control.ToolTip = text;
        System.Windows.Automation.AutomationProperties.SetName(control, text);
    }

    private WebView2? CurrentBrowser => (BrowserTabs.SelectedItem as TabItem)?.Tag as WebView2;

    private async void AddTab(Uri address)
    {
        var browser = new WebView2();
        browser.NavigationStarting += Browser_NavigationStarting;
        browser.NavigationCompleted += Browser_NavigationCompleted;
        var title = new TextBlock { Text = _toolTitle, MaxWidth = 150, TextTrimming = TextTrimming.CharacterEllipsis, VerticalAlignment = VerticalAlignment.Center };
        var tab = new TabItem { Tag = browser, Content = browser };
        var closeText = Text("关闭标签页", "Close tab", "Fermer l’onglet");
        var close = new System.Windows.Controls.Button { Content = "×", Tag = tab, Padding = new Thickness(5, 0, 5, 0), ToolTip = closeText, FocusVisualStyle = null };
        System.Windows.Automation.AutomationProperties.SetName(close, closeText);
        close.Click += CloseTab_Click;
        var header = new StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal };
        header.Children.Add(title); header.Children.Add(close); tab.Header = header;
        _tabTitles[browser] = title;
        BrowserTabs.Items.Add(tab);
        BrowserTabs.SelectedItem = tab;

        try
        {
            await browser.EnsureCoreWebView2Async();
            browser.CoreWebView2.DocumentTitleChanged += (_, _) => UpdateTabTitle(browser);
            browser.CoreWebView2.NewWindowRequested += Browser_NewWindowRequested;
            browser.CoreWebView2.Navigate(address.AbsoluteUri);
        }
        catch (Exception error)
        {
            title.Text = Text("网页引擎不可用", "Web engine unavailable", "Moteur Web indisponible");
            if (browser == CurrentBrowser) { PageTitle.Text = Text("无法打开网页", "Could not open page", "Impossible d’ouvrir la page"); AddressText.Text = error.Message; LoadingPanel.Visibility = Visibility.Collapsed; }
        }
    }

    private void Browser_NavigationStarting(object? sender, CoreWebView2NavigationStartingEventArgs e)
    {
        if (sender == CurrentBrowser) { LoadingPanel.Visibility = Visibility.Visible; AddressText.Text = e.Uri; }
    }

    private void Browser_NavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs e)
    {
        if (sender == CurrentBrowser) LoadingPanel.Visibility = Visibility.Collapsed;
    }

    private void Browser_NewWindowRequested(object? sender, CoreWebView2NewWindowRequestedEventArgs e)
    {
        e.Handled = true;
        if (Uri.TryCreate(e.Uri, UriKind.Absolute, out var address)) Dispatcher.BeginInvoke(() => AddTab(address));
    }

    private void UpdateTabTitle(WebView2 browser)
    {
        if (!_tabTitles.TryGetValue(browser, out var title)) return;
        title.Text = string.IsNullOrWhiteSpace(browser.CoreWebView2.DocumentTitle) ? _toolTitle : browser.CoreWebView2.DocumentTitle;
        if (browser == CurrentBrowser) PageTitle.Text = title.Text;
    }

    private void BrowserTabs_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (CurrentBrowser is not { } browser) return;
        PageTitle.Text = _tabTitles[browser].Text;
        AddressText.Text = browser.Source?.AbsoluteUri ?? _homeAddress.AbsoluteUri;
        LoadingPanel.Visibility = Visibility.Collapsed;
    }

    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if ((sender as FrameworkElement)?.Tag is not TabItem tab || tab.Tag is not WebView2 browser) return;
        _tabTitles.Remove(browser); browser.Dispose(); BrowserTabs.Items.Remove(tab);
        if (BrowserTabs.Items.Count == 0) Close();
    }

    private void Back_Click(object sender, RoutedEventArgs e) { if (CurrentBrowser?.CanGoBack == true) CurrentBrowser.GoBack(); }
    private void Forward_Click(object sender, RoutedEventArgs e) { if (CurrentBrowser?.CanGoForward == true) CurrentBrowser.GoForward(); }
    private void Reload_Click(object sender, RoutedEventArgs e) => CurrentBrowser?.Reload();
    private void OpenInDefaultBrowser_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo(CurrentBrowser?.Source?.AbsoluteUri ?? _homeAddress.AbsoluteUri) { UseShellExecute = true });
    private void InstallRuntime_Click(object sender, RoutedEventArgs e) => Process.Start(new ProcessStartInfo("https://developer.microsoft.com/microsoft-edge/webview2/") { UseShellExecute = true });

    protected override void OnClosed(EventArgs e)
    {
        foreach (var browser in _tabTitles.Keys) browser.Dispose();
        _tabTitles.Clear();
        base.OnClosed(e);
    }
}
