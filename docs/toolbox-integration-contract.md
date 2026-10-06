# Honsen 工具箱接入契约

本文件定义新 Windows 桌面应用被 Honsen 工具箱识别与调用的最小条件；更新细节以 [桌面应用安装与更新协议](honsen-desktop-update-protocol.md) 为准。

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
