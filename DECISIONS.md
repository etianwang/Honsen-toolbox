# Architecture Decisions

## 2026-10-12 — Honsen Procure product identity

**Status: approved.**

Honsen Procure has the following permanent product identity. These values replace any temporary development identity and must be used consistently when Procure is later integrated.

| Field | Approved value |
| --- | --- |
| Toolbox `appId` | `honsen.procure` |
| Display name | `Honsen Procure` |
| Product directory | `Honsen Procure` |
| Main executable | `HonsenProcure.exe` |
| Tauri `identifier` | `africa.honsen.procure` |
| Default installation scope | `machine` |
| Release installer name | `Honsen-Procure-<version>-Setup.exe` |

`appId` and Tauri `identifier` are separate fields with separate responsibilities. Toolbox discovery, the Honsen registry, the Honsen manifest, Runner arguments, update-result paths, and application data use `honsen.procure`. Tauri bundle and WebView identity use `africa.honsen.procure`; it must not replace the Toolbox `appId`.

The approved mutable-data root is:

```text
%LOCALAPPDATA%\Honsen Program\honsen.procure
```

No installer, Runner, Toolbox manifest, registry entry, release asset, or Toolbox catalog entry is created by this decision. GitHub Release URL, first release version, signing certificate, and release update mechanism remain release-stage decisions.

### Integration gaps recorded at approval

The cross-application contract requires Procure to later provide a HonsenUpdateRunner, an installer, a manifest, registry registration, and a SHA-256-verified stable release. None exists yet.

The contract specifies that Toolbox performs application updates by downloading a verified installer and invoking Runner `apply`. The current Toolbox implementation performs discovery, Runner `launch`, latest-version comparison, and first-install flows for the two existing eligible applications, but does not yet contain an application `apply` invocation path. Procure must therefore not be marked installable or updateable in `catalog.json` until both sides meet the contract and pass end-to-end acceptance.

The current Toolbox `launch` invocation also supplies `--wait-pid`, `--operation-id`, and `--result-path`; the published Runner `launch` syntax lists only `--app-id` and `--source`. Resolve that compatibility gap before Procure ships a Runner, either by accepting the extra arguments or by aligning Toolbox and the protocol together.

Tauri's default NSIS installer uses a different silent-install interface from the Honsen protocol's Inno Setup command line. The Procure release implementation must select an installer strategy that honors the Honsen Runner contract before a release mechanism is approved.
