# Honsen Windows 应用统一规范

**状态：固定规范（新应用必须遵守）**  
**适用范围：Honsen 工具箱与所有 Honsen Windows 桌面应用**  
**唯一事实源：本文件与 [桌面应用安装与更新协议](honsen-desktop-update-protocol.md)**

本规范解决“应用可以独立使用，也可以被 Honsen 工具箱发现、安装、打开、更新、卸载和修复登记”的一致性问题。新应用不得自行发明目录、注册表、Manifest、更新程序、发布资产或结果文件格式。

## 1. 术语与优先级

| 术语 | 含义 |
| --- | --- |
| `appId` | 永久应用身份，例如 `honsen.wms`。 |
| `InstallLocation` | 该应用唯一的安装根目录。 |
| Manifest | `<InstallLocation>\honsen.app.json`。 |
| Runner | `<InstallLocation>\HonsenUpdateRunner.exe`，唯一能替换应用文件的程序。 |
| 工具箱 | Honsen工具箱，只负责发现、下载、校验、调用 Runner 与展示结果。 |
| 稳定版 | GitHub Release 中非 Draft、非 Pre-release 的最新语义化版本。 |

- “必须”表示不符合即不得接入工具箱；“应”表示默认要求，需偏离时先在本仓库协议中说明。
- 本文与其他项目文档冲突时，以本文和更新协议为准；先更新规范，再同步修改所有项目。
- 现有应用的历史字段可以暂时保留以兼容，但不得替代本文规定字段。

## 2. 应用身份、产品目录与版本

### 2.1 `appId`

- 必须为全小写、永久不可变的 `honsen.<kebab-case>`，匹配：`^honsen\.[a-z0-9-]+$`。
- 一旦发布，绝不可重用或改名；显示名称、主 EXE 名称、仓库名可以变化，`appId` 不可以。
- 新应用先登记 `appId`，再开始制作安装器或 Runner。已保留：

```text
honsen.toolbox
honsen.cad-translator
honsen.document-translator
honsen.wms
```

### 2.2 产品目录和版本

- 每个应用必须有稳定的产品目录名，例如 `Honsen WMS`、`Honsen DrawTranslate`；升级不得更改该目录名。
- 版本必须使用 `major.minor.patch`，可附加第四段构建号，例如 `1.2.4` 或 `1.2.4.15`；Release tag 使用 `v<version>`。
- 安装器版本、主 EXE 文件版本、注册表 `Version`、Manifest `version`、Release tag 的版本必须相同。任一不一致即为失败发布。

## 3. 安装目录和数据边界

### 3.1 首次安装

首次安装可以允许用户选择目录。建议默认目录：

```text
全电脑安装：%ProgramFiles%\Honsen Program\<ProductDirectory>
当前用户安装：%LOCALAPPDATA%\Honsen Program\<ProductDirectory>
```

工具箱代为首次安装时，目标必须为工具箱所在 `Honsen Program` 目录下的产品子目录。例如：

```text
工具箱：D:\Program Files\Honsen Program\Honsen Toolbox
WMS：   D:\Program Files\Honsen Program\Honsen WMS
```

- 每个产品只能占用自己的产品子目录；不得写入或删除共享的 `Honsen Program` 根目录、工具箱目录或其他产品目录。
- 路径必须完整支持 Unicode、空格和非 ASCII 字符；不得依赖系统 ANSI/GBK 编码。
- 不能把用户可变数据、日志、下载包放进安装目录。

### 3.2 建议文件布局

```text
<InstallLocation>\
  <MainExecutable>.exe
  HonsenUpdateRunner.exe
  honsen.app.json
  ...应用私有运行文件...
```

应用私有可变数据应使用：

```text
%LOCALAPPDATA%\Honsen Program\<appId>\
%LOCALAPPDATA%\Honsen Program\UpdatePreferences\<appId>.json
%LOCALAPPDATA%\Honsen Program\UpdateResults\<appId>\<operationId>.json
%LOCALAPPDATA%\Honsen Program\Logs\<appId>\
```

卸载不得删除用户数据，除非用户在安装器中明确选择；不得删除共享 `Honsen Program` 父目录。

## 4. 安装描述文件：`honsen.app.json`

每个安装根目录必须有一个 UTF-8（无 BOM 或有 BOM 均可）JSON 文件，文件名严格为 `honsen.app.json`。它必须符合 [Schema](schemas/honsen.app.schema.json)，且字段如下：

```json
{
  "schemaVersion": 1,
  "appId": "honsen.example-app",
  "displayName": "Honsen Example App",
  "version": "1.2.4",
  "executable": "HonsenExample.exe",
  "updateRunner": "HonsenUpdateRunner.exe",
  "publisher": "Honsen",
  "updateManifestUrl": "https://api.github.com/repos/<owner>/<repo>/releases/latest"
}
```

规则：

- `executable` 和 `updateRunner` 只能是文件名，不能含路径、`..` 或命令行参数。
- 两个文件必须真实位于 `InstallLocation` 内；不能使用符号链接越过安装目录。
- `schemaVersion` 当前固定为 `1`。未来新增必填字段时才升为 `2`，工具箱需保留对旧版本的明确兼容策略。
- 可添加应用私有字段，但不能覆盖或改变上述字段含义。
- 安装、更新、修复和卸载后均要让 Manifest 与注册表保持一致。

## 5. 注册表登记

安装器必须检查并写入以下键之一：

```text
HKLM\Software\Honsen Program\Apps\<appId>  # machine 安装
HKCU\Software\Honsen Program\Apps\<appId>  # user 安装
```

安装器检查重复实例时必须遍历 HKLM/HKCU 与 32/64 位注册表视图。任一视图中有同 `appId` 且 `ExecutablePath` 存在，即拒绝第二次安装，并显示当前版本和目录。

所有值均为字符串，且必须写入：

```text
AppId              = <appId>
DisplayName        = <用户可读名称>
Version            = <major.minor.patch[.build]>
InstallLocation    = <绝对目录>
ExecutablePath     = <InstallLocation>\<MainExecutable>.exe
LauncherPath       = <InstallLocation>\HonsenUpdateRunner.exe
UpdateRunnerPath   = <InstallLocation>\HonsenUpdateRunner.exe
UpdateManifestUrl  = <GitHub releases/latest API URL>
UpdateUrl          = <与 UpdateManifestUrl 相同，兼容字段>
InstallScope       = machine 或 user
Publisher          = Honsen
```

- 绝对路径必须位于 `InstallLocation` 内；工具箱和 Runner 必须拒绝目录外路径。
- `LauncherPath` 与 `UpdateRunnerPath` 当前必须相同。`LauncherPath launch` 是桌面快捷方式、开始菜单和工具箱“打开”的唯一入口。
- 安装器必须同时写入 Windows 卸载登记：`DisplayName`、`DisplayVersion`、`InstallLocation`、`UninstallString`、`QuietUninstallString`。
- 卸载只删除自身 `appId` 的 Honsen 登记；不得影响其他键。

## 6. 安装器规范

支持 Inno Setup 或 MSI，但必须实现相同行为。

- 首次安装：检测重复 `appId` 后才允许选择目录；目录可写时生成主 EXE、Runner、Manifest、Honsen 登记与 Windows 卸载登记。
- 更新/修复：只允许使用注册表中的 `InstallLocation`，不允许改目录、迁移、创建第二份实例或回退到默认目录。
- 安装器需要管理员权限时，由 Windows UAC 正常处理；不得通过静默参数绕过 UAC。
- 静默 Inno 更新参数固定为：

```text
/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /SP-
/DIR="<InstallLocation>"
/LOG="<本地绝对日志路径>"
```

- `[Run]` 中的自动启动只可用于用户手动首次安装；静默更新后只允许 Runner 决定是否重启。
- 安装器退出码为 `0` 只是必要条件，不是更新成功的充分条件。

## 7. `HonsenUpdateRunner.exe` 规范

Runner 是每个应用必备、独立、无控制台的更新执行器。主程序、工具箱、安装器之外的任何程序不得删除、移动、覆盖或直接运行安装器来更新应用。

### 7.1 命令行契约

必须支持：

```text
HonsenUpdateRunner.exe launch --app-id <appId> --source <app|toolbox>

HonsenUpdateRunner.exe apply \
  --source <app|toolbox> \
  --app-id <appId> \
  --wait-pid <PID 或 0> \
  --installer <已下载且已校验的安装器绝对路径> \
  --sha256 <64位十六进制 SHA-256> \
  --target-dir <InstallLocation> \
  --expected-version <目标版本> \
  --restart <true|false> \
  --operation-id <GUID> \
  --result-path <本次操作唯一的绝对 JSON 路径>
```

- `--source` 只记录来源，绝不能用作授权、拒绝或改变安全校验的条件。
- `launch` 读取并验证登记和 Manifest，再检查稳定更新；无更新启动 `ExecutablePath`，有更新向用户提供立即更新、稍后提醒、跳过此版本三个选择。
- LTS 应用可以关闭启动时显性更新提示；它们仍必须支持用户主动触发的 Runner 更新。

### 7.2 `apply` 的安全流程

Runner 必须依次：

1. 复制自己至 `%TEMP%\Honsen Program\UpdateRunner\<GUID>\` 后从副本执行。
2. 获取每个 `appId` 唯一的全局互斥锁，例如 `Global\HonsenUpdate-honsen_wms`。
3. 校验 `appId`、注册表、Manifest、`target-dir`、主 EXE、Runner 路径和安装包 SHA-256；任一不一致即失败。
4. 等待 `wait-pid` 正常退出最多 30 秒。仅当 PID 的真实 exe 路径与注册表 `ExecutablePath` 严格相等时，才允许结束它；否则失败。
5. 在需要时通过 UAC 启动安装器，传入固定静默参数和原安装目录。
6. 等待安装器退出，读取退出码和日志。
7. 重新读取注册表与 Manifest，并验证安装目录没有变、主 EXE 与 Runner 存在、三处版本（注册表、Manifest、主 EXE）都等于 `expected-version`。
8. 原子写入结果 JSON；仅在成功且 `--restart true` 时，从重新验证后的 `ExecutablePath` 启动主程序。

失败时不启动主程序，不假装成功；保留安装器日志和结果 JSON 供诊断。

## 8. 更新结果 JSON

每次 `apply` 使用调用方生成的 GUID，结果必须写入：

```text
%LOCALAPPDATA%\Honsen Program\UpdateResults\<appId>\<operationId>.json
```

不得复用固定结果文件。成功和失败都必须包含：

```json
{
  "appId": "honsen.example-app",
  "operationId": "<GUID>",
  "status": "success 或 failed",
  "source": "app 或 toolbox",
  "fromVersion": "1.2.3",
  "toVersion": "1.2.4",
  "step": null,
  "installerExitCode": 0,
  "installerLogPath": "C:\\...\\update.log",
  "message": "更新完成",
  "completedAtUtc": "2026-10-07T12:00:00Z"
}
```

成功结果额外应包含 `installLocation` 与 `executablePath`。失败结果的 `step` 必须说明阶段，例如 `identity-validation`、`hash-validation`、`wait-process`、`installer-launch`、`installer-exit`、`post-install-validation`。

## 9. GitHub Release 发布规范

每个稳定 Release：

- 使用 `v<version>` tag；不能是 Draft 或 Pre-release。
- 只发布一个可安装资产：`<ProductId>-<version>-Setup.exe`。
- 同时发布同名校验文件：`<ProductId>-<version>-Setup.exe.sha256`，内容为安装器 SHA-256。
- GitHub Release API 中安装器资产必须有 `sha256:` digest；没有 digest 时工具箱不得自动安装或更新。
- 不发布裸主程序 EXE、便携 ZIP 或单独的 `HonsenUpdateRunner.exe` 作为正式稳定版资产。这些内容容易被工具箱和用户误当作安装包。Runner 必须随安装器安装。
- GitHub 自动生成的 Source code ZIP/TAR 可保留。
- Release 标题和说明应包含版本、主要变更、最低系统要求与已知限制；不得含密钥、内部账号或个人数据。

需要便携版或调试文件时，放到预发布、私有构建产物或独立下载页，不能和工具箱消费的稳定 Release 混放。

## 10. 工具箱联动规则

工具箱只按精确 `appId` 识别，不按显示名称、快捷方式、固定磁盘目录或扫描磁盘猜测位置。

| 工具箱动作 | 固定行为 |
| --- | --- |
| 发现 | 读取 Honsen 登记，验证 Manifest、主 EXE 与 Runner 全在 `InstallLocation` 内。 |
| 打开 | 调用 `LauncherPath launch --app-id <appId> --source toolbox`。 |
| 检查更新 | 只请求 `UpdateManifestUrl`，比较本机 `Version` 与 stable Release tag；不启动、下载或安装应用。 |
| 更新 | 下载、校验 SHA-256、调用 Runner `apply`，只读取该次结果 JSON。 |
| 卸载 | 经用户确认后，仅调用 Windows 卸载登记中的受信任 `QuietUninstallString`。 |
| 修复登记 | 用户手动选择目录；验证 Manifest、主 EXE、Runner 后只写 HKCU 登记。 |

- 工具箱首次自动安装仅对通过端到端验收且在工具目录明确登记安装包仓库、目标产品目录的应用开启。
- LTS 应用（当前 WMS）不应自动提示或自动执行更新；必须由用户主动点击更新并二次确认。
- 如果注册表丢失，工具箱不得扫描磁盘；只能由用户选择实际目录后恢复登记。

## 11. 编码、网络、日志与安全

- 所有 JSON、文本配置、Release 说明和 HTTP 请求/响应按 UTF-8 处理。
- 必须使用 HTTPS 访问更新 API 和下载资产；开发阶段的例外不得进入稳定发布。
- 不得在代码、Manifest、日志、Release、注册表或结果 JSON 中写入密钥、密码、令牌、连接串或个人数据。
- 本地错误日志默认仅保留必要技术信息。将日志上传到服务器前，必须有单独的用户授权、最小化字段与服务端鉴权；该能力目前不是工具箱 P0。
- SHA-256 是完整性校验，不替代 HTTPS、安装目录验证、进程路径验证或安装后版本验证；四者都必须做。
- 任何来自网络的版本、资产名、URL、哈希、版本字符串和 Release 说明都视为不可信输入，解析失败必须安全失败。

## 12. 兼容性、迁移与验收

### 12.1 兼容性

- Windows 10 与 Windows 11 必须支持。
- 注册表读取必须覆盖 HKLM/HKCU、32/64 位视图。
- 应用必须能够在安装路径含空格、中文和非 ASCII 字符时运行、更新、卸载。
- `schemaVersion`、Runner 参数和结果 JSON 只能新增兼容能力，不能静默破坏工具箱已使用的字段。

### 12.2 新应用接入验收

以下全部通过，才可在工具箱中标记为“可自动安装”：

1. 干净 Windows 环境首次安装，确认目录、注册表、Manifest、主 EXE、Runner 与 Windows 卸载登记。
2. 再次安装同 `appId`，确认拒绝第二实例。
3. 通过桌面快捷方式、开始菜单和工具箱“打开”均经 `Runner launch` 启动。
4. 工具箱“检查更新”只显示版本结果，不启动应用。
5. 应用自行更新和工具箱 `apply` 均能升级原目录，并生成独立结果 JSON。
6. 模拟 SHA-256 错误、错误目标目录、错误 PID 路径、安装器退出码非零，均应失败且不重启应用。
7. 卸载后仅删除自身文件和自身登记；其他 Honsen 应用、共享父目录和用户数据不受影响。
8. 删除 Honsen 注册表后，用工具箱手动选择安装目录，确认可恢复 HKCU 登记。
9. 验证浅/深色 Windows、Unicode 路径和离线/更新源失败时的可理解错误提示。

每个应用仓库必须提供 `docs/HONSEN_TOOLBOX_INTEGRATION.md`，列出实际 `appId`、主 EXE、产品目录、Release 资产名、Runner 支持的命令、安装器技术、测试环境与验收结果。

## 13. 维护流程

1. 新应用先选定 `appId`、产品目录、主 EXE 名与 Release 资产名。
2. 实现安装器、Manifest、注册表、Runner 和卸载器。
3. 按本规范完成端到端验收并提交应用仓库的接入说明。
4. 工具箱仓库再增加目录卡片、`appId`、安装包仓库和显示名称。
5. 修改跨应用行为时，先修改本规范和更新协议，再在 CAD、文档翻译器、WMS、工具箱中同步实现。

任何应用在未满足第 2、3 步前，不得被工具箱标记为可自动安装或可自动更新。
