# KSA upgrade mechanical precheck

This standalone .NET 10 console tool resolves the **existing built plugin** against an explicit candidate game directory. It has no compile-time KSA reference and does not rebuild the plugin, relax its production version guard, launch KSA/StarMap, or call `LanguagePlugin.BeforeMain` / `Runtime.Initialize`.

Run from the project root in PowerShell, choosing a fresh report filename under `work` every time:

```powershell
.\tools\check_compatibility.ps1 `
  -GameDir 'C:\Program Files\Kitten Space Agency' `
  -ReportPath "$PWD\work\upgrade-audit\baseline-5541.json"

.\tools\check_compatibility.ps1 `
  -GameDir 'D:\Games\KSA-candidate' `
  -ReportPath "$PWD\work\upgrade-audit\candidate-build-NEW.json" `
  -BaselineReport "$PWD\work\upgrade-audit\baseline-5541.json"
```

`-PluginDir`, `-LoaderDir` and `-Dotnet` override defaults. Default plugin: `work/build/KsaUiLanguages`; default loader: `tools/StarMap-0.4.7`; preferred SDK: `tools/dotnet-sdk/dotnet.exe`. Reports are append-only: an existing output filename is refused. The wrapper requires an explicit project `work` output path. Baselines are read only and must have the same schema/fingerprint format and a successful precheck verdict. Do not overwrite or promote a baseline after an update until its source build is clearly identified.

The process first dynamically loads candidate `KSA.dll`, finds `KSA.Constants.DocumentsFolderPath` by reflection, installs only its fixture prefix, and invokes the getter to verify the redirected value. Failure aborts **before** loading the plugin or executing target selectors. Each run gets a new project-local fixture and CLI environment. .NET CLI home, first-use state, temporary files, NuGet cache, AppData and user-profile environment values are restricted to project `work`; telemetry and development certificate generation are disabled. The wrapper restores its process environment afterwards. No user files are enumerated or copied. The redirection is a precaution for reviewed target selectors, **not an OS filesystem sandbox for arbitrary third-party code**.

The tool independently invokes each patch class's `TargetMethods` / `TargetMethod` or resolves its Harmony target attributes. It retains targets yielded before an iterator failure, records null targets with selector indices, and continues to other patch classes. Unsupported selectors or Harmony method kinds fail without guessing. Current selectors inspect reflection/IL and static caption tables; this should be reviewed again if new selectors gain side effects.

JSON records game/plugin/Harmony versions and SHA-256, loaded dependency paths and hashes, group errors, every target signature, and normalized **original managed IL**. Member/type operands use assembly simple names and full signatures rather than metadata tokens; branch and exception-region offsets become instruction indices. Local variable types, pinned state, `InitLocals`, stack size, exact string literals and floating-point bits are included. Referenced field-RVA constant bytes are read directly from the original assembly PE and included as length, SHA-256 and Base64. Their addresses and metadata tokens are excluded from the fingerprint. Unsupported `calli`/InlineSig signatures or unknown RVA lengths produce a fingerprint error rather than a misleading stable hash.

With `-BaselineReport`, `Comparison.changed`, `missing`, `new` and `unchanged` identify **patch-group + target-signature** changes. An unresolved selector can cause `missing`; this does not independently prove the game removed a method. Reports preserve both plugin identities so a plugin change can be distinguished from a game-only update. All original target bodies stay available for review; no game DLL is copied into source or output.

Exit 0 means only `mechanical-precheck-passed-runtime-unverified`. Exit 2 means isolation, load, selector, fingerprint or comparison failed; inspect `Errors`, `Groups[].Errors` and `Groups[].Targets[].Error`. Every group's `Installation` is `not-attempted-resolve-only`. The tool deliberately does not initialize unknown-version game static state or install the translation patches. It cannot prove transpiler match counts, Harmony installation/composition, generic JIT behavior, native-call compatibility, visible translation coverage, control-ID stability, saved-data behavior or actual GUI compatibility. Those require subsequent controlled candidate builds, focused headless checks and GUI review before changing the production version guard.

The current source plugin's known 5541 selfcheck is recorded separately under `work/upgrade-audit`. Source code and the wrapper may be committed; all game DLLs, SDK/loader binaries, fixtures and reports remain excluded from public source.
