# Honsen 工具箱：跨项目 Agent 上下文

## 必读顺序

1. [README](../README.md)
2. [桌面应用安装与更新协议](honsen-desktop-update-protocol.md)
3. [工具箱接入契约](toolbox-integration-contract.md)
4. [honsen.app.json Schema](schemas/honsen.app.schema.json)

本目录是 Honsen Windows 桌面应用的唯一跨项目规范。实现与文档冲突时，先报告；不得自行选择另一套字段、路径或参数。

## 工具箱现状

- 技术栈：C#、.NET 8、WPF；目标系统 Windows 10/11。
- 无账号系统；支持简体中文、英语、法语。
- 当前页：已收藏、全部工具、可安装、更新中心。
- 已接入：`honsen.cad-translator`、`honsen.document-translator`。
- 预留：`honsen.wms`。

## 已实现行为

- 工具箱读取 HKLM/HKCU、32/64 位视图中的 Honsen 应用登记，并验证 manifest、主 exe 与 Runner 都位于 `InstallLocation` 内。
- 工具箱卡片显示已安装版本与 GitHub 最新稳定 Release 版本。
- “检查更新”只比较本机版本与远端版本，绝不启动、下载或安装应用。
- “打开”调用应用的 `HonsenUpdateRunner.exe launch`。
- 首次自动安装当前仅支持 CAD 与文档翻译器；新应用在 Runner、安装器和发布资产通过接入验收后才能加入此列表。
- 已安装应用的右键菜单可打开、检查更新、打开目录、卸载、打开 Release 页面。卸载必须经用户确认并调用应用注册的卸载器。

## LTS 规则

WMS 是 LTS 应用：不得自动检查后提示、下载或安装更新。只有用户主动发起更新并完成两次确认，才可调用 Runner `apply`。这一规则不改变安装、注册表、manifest、Runner、卸载或结果文件协议。
