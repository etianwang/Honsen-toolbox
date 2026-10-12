# Honsen 工具箱接入契约

本文件定义新 Windows 桌面应用被 Honsen 工具箱识别与调用的最小条件；所有应用的目录、安装器、注册表、Manifest、Runner 与发布规范以 [Honsen Windows 应用统一规范](HONSEN_WINDOWS_APPLICATION_STANDARD.md) 为准，更新细节以 [桌面应用安装与更新协议](honsen-desktop-update-protocol.md) 为准。

## 应用必须交付

1. 固定且唯一的 `appId`。
2. 安装器写入完整 Honsen 注册表登记，并检查 HKLM/HKCU 与 32/64 位视图，防止同一 `appId` 的第二安装实例。
3. 安装目录内的 UTF-8 `honsen.app.json`，符合 [Schema](schemas/honsen.app.schema.json)。
4. 同目录的主程序和 `HonsenUpdateRunner.exe`。
5. 标准 Windows 卸载登记：`InstallLocation`、`UninstallString`、`QuietUninstallString`。
6. GitHub stable Release：可安装资产、版本 tag 与 SHA-256 digest。
7. 本项目中的 `docs/HONSEN_TOOLBOX_INTEGRATION.md`，记录真实主 exe、资产名、Runner 参数和端到端测试结果。

## 工具箱如何识别

工具箱只按精确 `appId` 查询：

```text
HKLM/HKCU\Software\Honsen Program\Apps\<appId>
```

它验证 `AppId`、`Version`、`InstallLocation`、`ExecutablePath`、`LauncherPath`、`UpdateRunnerPath`、`UpdateManifestUrl`，再验证 manifest 的 `appId` 和所有可执行路径都位于 `InstallLocation` 内。工具箱不会按显示名称、快捷方式、固定磁盘目录或磁盘扫描猜测应用位置。

## 工具箱动作

| 工具箱动作 | 应用必须支持的行为 |
| --- | --- |
| 打开 | `HonsenUpdateRunner.exe launch` 启动已验证的主程序。 |
| 检查更新 | 工具箱请求 `UpdateManifestUrl` 并比较 `Version` 与 stable Release tag；不会调用 Runner。 |
| 更新 | 工具箱下载并校验 SHA-256 后调用 Runner `apply`，传入唯一 `operationId` 和 `result-path`；只读取该结果文件。 |
| 卸载 | 工具箱先由用户确认，再调用受信任 Windows 卸载登记；应用卸载器仅清理自身文件和自身 appId 注册表键。 |
| 修复登记 | 用户手动选择目录；工具箱验证 manifest、主 exe、Runner 后只写 HKCU 登记。 |

## 接入流程

1. 应用项目先实现并验证本协议。
2. 向工具箱项目提供其 `HONSEN_TOOLBOX_INTEGRATION.md`。
3. 工具箱项目才在目录中登记 appId、Release 仓库、安装子目录与显示信息。
4. 在一台干净 Windows 环境验证：首次安装、打开、版本比较、更新、卸载、注册表丢失后的手动修复。

未经第 1、2 步验证，不得把应用标记为可由工具箱自动安装。

## 已批准但尚未接入的产品身份

Honsen Procure 的正式身份已获批准：Toolbox `appId` 为 `honsen.procure`，显示名和产品目录均为 `Honsen Procure`，主程序为 `HonsenProcure.exe`，默认安装范围为 `machine`，正式安装包命名为 `Honsen-Procure-<version>-Setup.exe`。Tauri `identifier` 固定为 `africa.honsen.procure`；它是 Tauri bundle/WebView 身份，不替代 Toolbox `appId`。

Procure 的可变数据根目录固定为 `%LOCALAPPDATA%\Honsen Program\honsen.procure`。该身份批准不等同于 Toolbox 接入：在 Procure 提供安装器、`HonsenUpdateRunner.exe`、`honsen.app.json`、注册表登记、稳定 Release 与端到端验收前，禁止新增 `catalog.json` 条目或启用自动安装、启动、更新与修复登记。

### 当前实现差距

协议要求 Toolbox 下载并校验安装器后调用 Runner `apply`。现有 Toolbox 代码目前只实现已登记应用的发现、Runner `launch`、版本比较，以及 CAD 翻译器和文档翻译器的首次安装；尚无第三方应用 Runner `apply` 调用路径。另，当前 `launch` 实现会附加 `--wait-pid`、`--operation-id` 与 `--result-path`，超出协议已列出的 `launch` 参数。Procure 接入前必须统一这些行为，且不得以文档存在替代实现与验收。
