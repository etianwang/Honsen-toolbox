# Honsen 桌面应用安装与更新协议

**状态：固定规范**  
**适用对象：Honsen工具箱及所有 Honsen Windows 桌面应用**

## 1. 目标

- 每台电脑中，同一个 `appId` 只能有一个安装实例。
- 首次安装可由用户选择目录；后续更新只能静默覆盖该目录，绝不迁移或新建目录。
- 应用自身更新与 Honsen工具箱发起的更新，必须调用同一个独立更新助手。
- 工具箱不直接覆盖其他应用的文件，只负责发现、下载、校验、调用更新助手和展示结果。

## 2. 应用身份与唯一安装

每个应用必须有永久、唯一且发布后不可更改的 `appId`，例如：

```text
honsen.toolbox
honsen.cad-translator
honsen.document-translator
honsen.wms
```

应用安装完成后，按安装范围写入以下其中之一：

```text
HKLM\Software\Honsen Program\Apps\<appId>   # 全电脑安装
HKCU\Software\Honsen Program\Apps\<appId>   # 当前用户安装
```

安装器必须同时检查 HKLM、HKCU 和 32/64 位注册表视图。若发现同一 `appId` 的 `ExecutablePath` 存在，必须拒绝第二次安装，并显示已安装版本和目录。

注册表必填值：

```text
AppId
DisplayName
Version
InstallLocation
ExecutablePath
InstallScope                 # machine 或 user
Publisher                    # Honsen
UpdateManifestUrl
LauncherPath
UpdateRunnerPath
```

若注册表记录存在但主 exe 不存在，应显示“损坏安装”；只允许修复至注册表中的 `InstallLocation`，不允许改目录。

## 3. 安装目录识别文件

每个应用的主 exe 同级必须存在 UTF-8 编码的 `honsen.app.json`：

```json
{
  "schemaVersion": 1,
  "appId": "honsen.cad-translator",
  "displayName": "Honsen CAD中英法图纸翻译器",
  "version": "1.10.0",
  "executable": "Honsen DrawTranslate.exe",
  "updateRunner": "HonsenUpdateRunner.exe",
  "publisher": "Honsen",
  "updateManifestUrl": "https://example.com/update.json"
}
```

应用更新后，注册表与此文件的 `appId`、版本、主程序路径必须保持一致。

## 4. 更新助手

每个应用必须带有独立的 `HonsenUpdateRunner.exe`。主程序不得替换自身。

更新助手运行时应先复制到临时目录执行，以便安全替换应用目录中的原更新助手：

```text
%TEMP%\Honsen Program\UpdateRunner\<随机目录>\
```

更新助手必须：

1. 校验更新包 SHA-256。
2. 校验 `appId`、注册表、`honsen.app.json` 与目标目录一致。
3. 先等待指定主程序 PID 正常退出最多 30 秒；仅当该 PID 的 exe 路径严格等于注册表 `ExecutablePath` 时，才可结束该进程。路径不符、仍无法结束或超时后仍在运行均必须失败。
4. Runner 自己负责在需要权限时以 UAC 启动安装器；不得依赖主程序或工具箱已提权。
5. 等待安装器完成，检查退出码和安装日志。
6. 验证新版本注册表、`honsen.app.json` 和主 exe 文件版本。
7. 为同一 `appId` 使用命名互斥锁或锁文件，禁止并发更新。
8. 写入标准结果文件。

## 5. 目录不可变规则

首次安装可由用户指定目录。更新时必须从已有注册表读取唯一目标目录：

```text
更新前 InstallLocation = 更新参数 target-dir = 更新后 InstallLocation
```

安装器不得在升级中把应用迁移到默认目录、新目录或其他磁盘。任何安装后目录、主程序路径与目标目录不一致的情况，均判定更新失败。

Inno Setup 静默更新参数：

```text
/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP- \
/DIR="<target-dir>" \
/LOG="<本地日志绝对路径>"
```

静默安装不等于绕过 UAC：当目标目录需要管理员权限时，Windows 的 UAC 确认仍会出现。

## 6. 两种更新入口

### 6.1 应用自身更新

```text
主程序下载更新包 → 校验 SHA-256 → 启动更新助手 → 主程序正常退出
→ 更新助手静默更新原目录 → 验证 → 重启主程序
```

统一调用示例：

```text
HonsenUpdateRunner.exe apply \
  --source app \
  --app-id <appId> \
  --wait-pid <主程序 PID> \
  --installer "<已校验安装包路径>" \
  --sha256 "<SHA-256>" \
  --target-dir "<当前应用真实安装目录>" \
  --expected-version "<目标版本>" \
  --restart true \
  --operation-id "<GUID>" \
  --result-path "%LOCALAPPDATA%\\Honsen Program\\UpdateResults\\<appId>\\<GUID>.json"
```

### 6.2 Honsen工具箱发起更新

```text
工具箱下载更新包 → 校验 SHA-256 → 从注册表读取 UpdateRunnerPath
→ 调用目标应用更新助手 → 等待结果文件 → 刷新状态
```

工具箱绝不直接覆盖、移动或删除目标应用的文件。

统一调用示例：

```text
HonsenUpdateRunner.exe apply \
  --source toolbox \
  --app-id <appId> \
  --wait-pid <目标应用 PID，未运行则为 0> \
  --installer "<已校验安装包路径>" \
  --sha256 "<SHA-256>" \
  --target-dir "<注册表中的 InstallLocation>" \
  --expected-version "<目标版本>" \
  --restart false \
  --operation-id "<GUID>" \
  --result-path "%LOCALAPPDATA%\\Honsen Program\\UpdateResults\\<appId>\\<GUID>.json"
```

`--source` 只能记录更新来源（`app` 或 `toolbox`），不得作为权限判断或限制某一调用方的依据；两种入口的校验、替换和成功判定必须完全相同。

## 7. 更新结果回传

每次操作必须由调用方传入唯一 `operationId` 和独立 `result-path`。默认目录约定为：

```text
%LOCALAPPDATA%\Honsen Program\UpdateResults\<appId>\<operationId>.json
```

Runner 只能写入本次调用指定的 `result-path`，工具箱也只能读取自己传入的该文件；禁止依赖固定共享结果文件，避免并发操作互相覆盖。

成功示例：

```json
{
  "appId": "honsen.cad-translator",
  "operationId": "0f3b2a90-34c4-4d99-bd17-a964da43f6a6",
  "status": "success",
  "source": "toolbox",
  "fromVersion": "1.9.9",
  "toVersion": "1.10.0",
  "installLocation": "C:\\Honsen\\CAD Translate",
  "executablePath": "C:\\Honsen\\CAD Translate\\Honsen DrawTranslate.exe",
  "step": null,
  "installerExitCode": 0,
  "installerLogPath": "C:\\Users\\...\\cad-update.log",
  "message": "更新完成",
  "completedAtUtc": "2026-10-05T10:00:00Z"
}
```

失败示例：

```json
{
  "appId": "honsen.cad-translator",
  "operationId": "0f3b2a90-34c4-4d99-bd17-a964da43f6a6",
  "status": "failed",
  "source": "toolbox",
  "fromVersion": "1.9.9",
  "toVersion": "1.10.0",
  "step": "installer-exit",
  "installerExitCode": 5,
  "installerLogPath": "C:\\Users\\...\\cad-update.log",
  "message": "安装器退出码非零",
  "completedAtUtc": "2026-10-05T10:00:00Z"
}
```

字段名固定使用 `step`、`installerExitCode`、`installerLogPath`、`message`；不得改为同义字段。成功与失败均必须包含 `appId`、`operationId`、`status`、`source`、`fromVersion`、`toVersion` 和 `completedAtUtc`。

工具箱只能根据结果文件和更新后的注册表显示成功或失败，不能在启动安装器后直接假定更新成功。

## 8. 重启规则

- `--source app --restart true`：成功后从注册表的 `ExecutablePath` 启动新版本。
- `--source toolbox --restart false`：成功后不自动启动应用，工具箱显示“更新完成，可打开”。
- 工具箱明确传入 `--restart true` 时，更新助手可以在成功后启动应用。

## 9. 卸载

卸载成功后必须删除对应 Honsen Program 注册表项和安装目录中的 `honsen.app.json`，同时保留标准 Windows 卸载行为。

## 10. 重装系统后的手动恢复

工具箱不得扫描磁盘。用户可手动选择应用安装目录；工具箱只接受已知 `appId` 的 `honsen.app.json`，并验证主 exe 与 `updateRunner` 均位于该目录内后，写入 `HKCU\Software\Honsen Program\Apps\<appId>` 恢复本机登记。已有有效登记时不得覆盖或创建第二个目录记录。

## 11. 工具箱首次安装

对于已知但未安装的应用，工具箱可读取 GitHub 最新 Release，显示下载百分比，校验 GitHub API 返回的 SHA-256 digest 后调用 Inno 静默安装。安装目录固定为工具箱所在 `Honsen Program` 父目录下的应用子目录，例如工具箱位于 `D:\Program Files\Honsen Program\Honsen Toolbox` 时，CAD 翻译器安装至 `D:\Program Files\Honsen Program\Honsen DrawTranslate`。工具箱只显示“安装中”；Windows UAC 仍由系统显示。
