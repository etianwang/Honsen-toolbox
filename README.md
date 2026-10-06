# Honsen 工具箱

Honsen 工具箱是面向 Windows 10/11 的内部桌面入口。它统一打开网页与本地工具、安装已接入的桌面应用、显示本机/远端版本，并调用各应用自己的更新助手。

## 给其他项目 agent 的必读入口

任何需要接入 Honsen 工具箱的项目（CAD 翻译器、文档翻译器、WMS 或后续桌面应用）开始改动前，必须按顺序阅读：

1. [跨项目 Agent 上下文](docs/agent-context.md)。
2. [桌面应用安装与更新协议](docs/honsen-desktop-update-protocol.md)。
3. [工具箱接入契约](docs/toolbox-integration-contract.md)。
4. [honsen.app.json Schema](docs/schemas/honsen.app.schema.json)。
5. 与自身应用相同类型的已发布实现和其 `HONSEN_TOOLBOX_INTEGRATION.md`（如存在）。

本仓库的 `docs/honsen-desktop-update-protocol.md` 是跨项目唯一事实源。不得自行发明或修改 `appId`、注册表路径、`honsen.app.json` 字段、Runner 参数、结果 JSON、安装目录规则或卸载边界。遇到冲突先报告，再修改协议和所有相关项目。

## 项目现状

- 技术栈：C#、.NET 8、WPF。
- 产品名：Honsen工具箱。
- 支持语言：简体中文、English、Français；默认中文。
- 工具页：已收藏、全部工具、可安装、更新中心。
- 已接入桌面应用：
  - `honsen.cad-translator`
  - `honsen.document-translator`
- 预留应用身份：`honsen.wms`。

## 当前工具箱能力

- 通过注册表和安装目录中的 `honsen.app.json` 发现已安装应用。
- 首次安装 CAD 与文档翻译器：从 GitHub Release 下载、显示下载百分比、校验 SHA-256，并静默安装到工具箱所在 `Honsen Program` 父目录。
- 卡片显示“当前版本”和 GitHub 最新稳定 Release 版本。
- “检查更新”只比较本机版本与远端 Release，**不会打开应用、下载或更新**。
- 单击已安装应用的卡片会调用其 `HonsenUpdateRunner.exe launch` 打开应用。
- 已安装桌面应用卡片右键菜单：打开、检查更新、打开程序所在目录、卸载、打开 Release 发布页。
- 卸载只调用应用注册的受信任卸载器；工具箱不得自行删除应用目录。

## 跨项目固定约定

### 应用身份和安装登记

每个 Honsen Windows 桌面应用必须有不可变的 `appId`，并在安装后写入：

```text
HKLM\Software\Honsen Program\Apps\<appId>
```

没有管理员权限时可写入同路径的 HKCU。注册表与 `<InstallLocation>\honsen.app.json` 必须一致，至少包含版本、安装目录、主程序路径、`LauncherPath`、`UpdateRunnerPath` 和 GitHub `UpdateManifestUrl`。

### 更新与启动

- `HonsenUpdateRunner.exe` 是唯一可覆盖、校验和重启主程序的组件。
- 工具箱和主程序不得直接运行 Inno、删除或替换应用文件。
- `launch` 用于打开应用；Runner 的具体行为以协议文档为准。
- 工具箱只读取 GitHub `releases/latest` 用于显示版本与手动检查；不因检查而启动应用。
- LTS 应用（例如 WMS）默认不显性提示更新，必须由用户手动发起并二次确认后才能更新。

### 安装、卸载和安全边界

- 同一台电脑的同一 `appId` 只能安装一个实例。
- 首次安装可选目录；后续更新只能覆盖注册表记录的原目录，绝不迁移或新建第二份。
- 所有下载方均必须在交给 Runner 前校验 SHA-256。
- 卸载只能清理本应用目录和本应用专属注册表键，绝不能删除共享的 `Honsen Program` 父目录或其他 Honsen 应用。

## 维护规则

- 跨应用行为变更：先更新 `docs/honsen-desktop-update-protocol.md`，再同步所有应用。
- 工具箱自身的已验证问题与规避方式记录在 [memory.md](memory.md)。它是本项目开发记录，不替代跨项目协议。
- 其他项目提交接入时，应额外提供自己的 `docs/HONSEN_TOOLBOX_INTEGRATION.md`，列出真实主 EXE、Release 资产名、appId、Runner 支持的参数和测试结果。
