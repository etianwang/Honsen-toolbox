# Honsen 工具箱

Honsen 工具箱是 Honsen 面向 Windows 10/11 的内部桌面入口。它把网页服务和 Honsen 本地应用放在同一个工具库中，并统一提供收藏、搜索、安装、启动、版本检查、更新调用、卸载和安装登记修复。

> 当前为 Alpha 阶段。已实现的能力以本文为准；工具箱自身在线更新、错误日志上报和 GitHub/Gitee 自动故障切换仍是后续任务。

## 运行环境与技术栈

- Windows 10 或 Windows 11
- C#、.NET 8、WPF
- 默认界面语言为简体中文；用户可随时切换 English 或 Français
- 本地用户状态保存在 `%LOCALAPPDATA%\HonsenToolbox\state.json`
- 工具箱自己的安装登记使用 `honsen.toolbox`

## 工具库

| 工具 | 类型 | 当前入口 |
| --- | --- | --- |
| 深圳弘盛·系统终端 | 网页 | <https://www.honsen.africa/> |
| 仓库管理 | 桌面应用 | [GitHub Releases](https://github.com/etianwang/Honsen_WMS/releases) |
| 仓库数据在线查看 | 网页 | <https://wms.honsen.africa/> |
| 图纸管理 | 网页 | <https://edm.honsen.africa/login> |
| 柜号跟踪 | 网页 | <http://tracking.honsen.africa/> |
| 喀麦隆团队考勤 | 网页 | <https://kq.honsen.africa/> |
| 埃塞俄比亚团队考勤 | 网页 | <https://kq-et.honsen.africa/> |
| 企业网盘 | 网页 | <https://p.honsen.africa/> |
| 图纸翻译器 | 桌面应用，`honsen.cad-translator` | [GitHub Releases](https://github.com/etianwang/CAD_translator/releases) |
| 万能文档翻译器 | 桌面应用，`honsen.document-translator` | [GitHub Releases](https://github.com/etianwang/Honsen-Document-Translator/releases) |

WMS 的应用身份预留为 `honsen.wms`；待其按桌面应用协议接入 Runner 后，工具箱可像两个翻译器一样识别、安装、启动、检查更新和卸载它。

## 已实现功能

### 工具浏览与使用

- 四个页面：**已收藏**、**全部工具**、**可安装**、**更新中心**。
- 单击网页卡片，在系统默认浏览器中打开网站。
- 单击已接入且已安装的桌面应用卡片，通过其 `HonsenUpdateRunner.exe launch` 启动应用。
- 全局搜索：搜索中文、英文、法文名称与说明；按 `Ctrl + K` 可直接聚焦搜索框。
- 每张卡片右上角星标可收藏/取消收藏；默认收藏图纸翻译器、万能文档翻译器和系统终端。
- 在“已收藏”页按住卡片约 450 ms 后拖动，可调整收藏顺序；松开后保存到本机。
- 收藏状态、排序、最近打开时间、界面语言和开机自启选项均只存本机，不需要账号。

### 桌面应用发现与修复

- 启动时读取 `HKLM`、`HKCU` 以及 32/64 位注册表视图下的 `Software\Honsen Program\Apps\<appId>`。
- 同时校验注册表记录与 `<InstallLocation>\honsen.app.json`，不按显示名称、快捷方式或磁盘扫描猜测应用路径。
- 更新中心提供“修复已安装应用”：当系统重装导致注册表丢失时，用户可手动选择一个安装目录；工具箱读取其中的 `honsen.app.json`，校验主程序和 Runner 后在 HKCU 重建登记。
- 应用卡片显示本机“当前版本”和 GitHub `releases/latest` 的“最新版本”。无法访问更新源时保留本机信息，不把网络失败误报为已有更新。

### 安装、打开、检查更新与卸载

- “可安装”页目前可首次安装图纸翻译器和万能文档翻译器。
- 工具箱从对应 GitHub 最新稳定 Release 找到 `Setup.exe`，显示下载百分比，校验 Release 提供的 SHA-256，再静默运行安装器。
- 首次安装默认放入工具箱所在的 `Honsen Program` 目录：例如工具箱为 `D:\Program Files\Honsen Program\Honsen Toolbox`，应用会安装到 `D:\Program Files\Honsen Program\<应用目录>`。
- 卡片右键菜单按工具类型提供：
  - 已安装桌面应用：打开、检查更新、打开程序所在目录、卸载、打开 Release 发布页。
  - 未安装且已接入的桌面应用：安装、打开 Release 发布页。
  - 网页工具：打开网站。
- “检查更新”只比较本机版本与远端 Release，**不会**启动应用、下载文件或修改本机内容。
- 工具箱不直接替换 EXE 或运行 Inno 更新：真正更新由应用自己的 `HonsenUpdateRunner.exe apply` 完成。工具箱只负责下载、SHA-256 校验、传入安全参数并读取该次操作的结果 JSON。
- 卸载前会二次确认；工具箱只调用应用注册的受信任静默卸载命令，绝不自行删除应用目录或共享的 `Honsen Program` 父目录。

### 窗口、主题与托盘

- 支持浅色和深色主题，所有文字、卡片、边框和控件使用对应主题颜色。
- 默认启用开机自启；用户可以在侧栏关闭。
- 点击窗口关闭按钮仅隐藏到系统托盘，不退出工具箱。
- 托盘图标双击或菜单“打开界面”可恢复窗口；菜单“退出工具箱”才会真正退出。
- 网络状态会显示“网络可用”或离线提示；离线时网页和更新服务可能不可用。

## 桌面应用接入约定

每个 Honsen Windows 桌面应用必须具备不可变的 `appId`、安装目录根部的 `honsen.app.json` 与独立的 `HonsenUpdateRunner.exe`。安装后登记到：

```text
HKLM\Software\Honsen Program\Apps\<appId>
```

无管理员权限时可以使用同路径的 HKCU 键。注册表和 `honsen.app.json` 必须一致，至少包含版本、安装目录、主程序路径、`LauncherPath`、`UpdateRunnerPath`、`UpdateManifestUrl`。

必须遵守以下边界：

- 同一台电脑、同一个 `appId` 只允许一个安装实例。
- 首次安装可由应用决定目录；后续更新只能覆盖登记的原目录，不能迁移或建立第二份。
- 所有下载方必须先校验 SHA-256；只有 Runner 可以关闭主程序、运行 Inno、覆盖文件、验证和重启。
- Runner 的 `launch` 负责应用自行检查更新；工具箱的“检查更新”只做版本比较。
- 每次 `apply` 必须传唯一 `operationId` 与 `resultPath`；工具箱只读取自己创建的结果文件。
- LTS 应用（例如 WMS）默认不显性提示更新，必须由用户手动发起并二次确认。

完整的字段、Runner 参数、结果 JSON 和安全流程以 [桌面应用安装与更新协议](docs/honsen-desktop-update-protocol.md) 为唯一事实源。

## 给其他项目 Agent 的接入入口

需要接入工具箱的项目（CAD 翻译器、万能文档翻译器、WMS 和后续桌面应用）开始改动前，必须依次阅读：

1. [跨项目 Agent 上下文](docs/agent-context.md)
2. [桌面应用安装与更新协议](docs/honsen-desktop-update-protocol.md)
3. [工具箱接入契约](docs/toolbox-integration-contract.md)
4. [honsen.app.json Schema](docs/schemas/honsen.app.schema.json)
5. 同类已发布应用的 `docs/HONSEN_TOOLBOX_INTEGRATION.md`（如存在）

不得自行发明或改变 `appId`、注册表路径、`honsen.app.json` 字段、Runner 参数、结果 JSON、安装目录规则或卸载边界。发现冲突时先报告，再统一更新协议和关联项目。

## 开发与发布

```powershell
dotnet build
dotnet run
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true
```

- `catalog.json`：工具目录、名称、说明、网页地址和应用身份。
- `honsen.app.json`：工具箱自身身份描述。
- `Themes/DesignTokens.xaml` 与 `ui-foundation.css`：已确定的视觉设计令牌与参考样式。
- [memory.md](memory.md)：已验证错误与规避方式；它不替代跨项目协议。
- 任何提交均同步推送至 [GitHub](https://github.com/etianwang/Honsen-toolbox) 和 [Gitee](https://gitee.com/etianwang/honsen-toolbox)。

## 当前未做功能

- 工具箱自身的在线检查与静默更新
- 错误日志上传与服务端接收工具
- GitHub 访问失败时自动切换至 Gitee 或 `update.honsen.africa`
- WMS 的 Runner 接入和由工具箱完成的安装/更新/卸载
