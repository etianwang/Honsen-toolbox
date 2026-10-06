# Honsen 工具箱开发记错本

这份文件记录已发生且已验证的问题。后续修改相关功能前，先检查这里，避免重复犯错。

## 界面交互

- 搜索框必须确认获得实际可用的布局宽度，并在调试窗口中验证可以点击、输入和清除；不能只看 XAML 结构判断。
- 收藏操作使用卡片右上角星标；不要再放到右键菜单。
- 收藏排序的交互约定：单击卡片打开工具，长按约 450ms 后拖动排序；不要额外显示拖动把手。
- WPF 的 `DragDrop.DoDragDrop` 不是可靠的逐帧卡片预览方案，`GiveFeedback` 的触发频率受系统控制，不能用它实现“整张卡片跟随鼠标”。
- 卡片拖动使用自定义鼠标捕获和无点击命中的 `Popup` 浮层：开始拖动时把卡片渲染为 `RenderTargetBitmap`，在 `PreviewMouseMove` 中更新浮层坐标，鼠标松开时根据命中卡片完成排序。
- 拖动浮层的坐标必须统一使用窗口本地坐标；混用卡片坐标、Adorner 坐标或屏幕坐标会导致预览偏移或不跟手。

## 主题与控件

- 主题资源可能是只读/冻结画刷。切换深色模式时不要修改已有 `SolidColorBrush.Color`；应替换资源字典中的整个画刷对象。
- 会随主题切换的颜色一律使用 `DynamicResource`，不能使用 `StaticResource` 或写死浅色值，否则深色模式会出现黑字、白底或不可读文字。
- 默认 WPF `ComboBox` 可能保留系统浅色模板，仅设置 `Background` 和 `Foreground` 不够。需要自定义下拉、弹层、选中和悬停状态的模板。
- 自定义导航按钮必须明确设置默认背景和前景色；不能依赖系统 `Button` 模板，以免出现白底白字。

## 验证流程

- 每次影响窗口或交互的修改后：先停止旧的 `HonsenToolbox` 进程，再执行 `dotnet build HonsenToolbox.csproj --configuration Debug`，最后启动 Debug 程序确认窗口存活。
- 编译通过不代表交互正确。涉及鼠标、键盘、主题、文本输入或弹层时，必须由实际运行的窗口验证。
- 项目同时引用 WinForms（托盘）和 WPF。遇到 `Point`、`Image`、`GiveFeedbackEventArgs`、`QueryContinueDragEventArgs` 等同名类型时，明确写出 `System.Windows` 或 `System.Windows.Controls` 命名空间，避免歧义。
- 工具箱代码远端必须双向同步：GitHub `https://github.com/etianwang/Honsen-toolbox.git` 与 Gitee `https://gitee.com/etianwang/honsen-toolbox.git`。每次提交后均推送两个远端的 `main` 分支。

## 应用识别协议

- 工具箱自身 appId 固定为 `honsen.toolbox`。当前运行时写入 `HKCU\Software\Honsen Program\Apps\honsen.toolbox`；正式安装器可按安装范围写入 HKLM。
- 应用目录必须随程序提供 UTF-8 的 `honsen.app.json`，并与注册表中的 appId、版本和主程序路径保持一致。
- 桌面应用安装、唯一实例、静默原目录更新、更新助手与工具箱联动的固定规范见 `docs/honsen-desktop-update-protocol.md`；后续实现不得与其冲突。
- 系统重装导致注册表丢失时，不扫描磁盘；由用户选定安装目录后校验 `honsen.app.json`、主 exe 和 Runner，再仅写入 HKCU 恢复登记。
- 工具箱首次安装 CAD 或文档翻译器时，下载后必须校验 GitHub SHA-256，使用 Inno 静默安装到工具箱所在 `Honsen Program` 父目录的固定应用子目录；UAC 不能绕过。
- 不要强制以 Shell `runas` 启动 Inno 安装器：部分 Windows 环境会对该动词报“系统找不到指定的路径”，即使安装包存在且校验无误。直接启动安装器；需要写入 `Program Files` 时由 Inno 自身请求 UAC。
- 开发目录不属于 `Honsen Program` 时，首次安装测试必须显式设置目标根目录；不得回退到 `bin\\Debug` 等构建目录。
- .NET 单文件发布不保证 `CopyToOutputDirectory` 内容进入发布目录；运行时读取的 `logo.ico`、`catalog.json`、`honsen.app.json` 必须同时设置 `CopyToPublishDirectory`，并从 ZIP 重新解压后启动验证。`ApplicationIcon` 还要用 Publish target 显式复制；托盘图标缺失时必须降级，不能让主窗口崩溃。

