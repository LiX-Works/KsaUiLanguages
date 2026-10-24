# KSA build 5638 local compatibility preview

Validated against KSA 2026.10.19.5638 with StarMap 0.4.8 on Windows x64.

Changes:
- Resolve Brutal.Numerics on newer games while retaining the older assembly reference conditionally.
- Target extracted CrewPortraitPanel.DrawCameraControls when present; keep the old Draw fallback.
- Translate the three Galactic grid UTF-8 ImString caption sites on 5638 with a strict match count.
- Add the Radiation canvas alias and Chinese/Japanese translations; retain machine IDs.
- Recognize 5638 and reject adjacent unknown builds. Report headless check exceptions without a desktop crash dialog.

Validation: 60 headless checks passed on 5638 with StarMap 0.4.8; actual Chinese startup dialog and Earth-only orbital scene visually checked. Existing 5541/5554 builds were not available and were not re-tested. Long play sessions, saves, every button, and every window were not verified. Some HUD captions remain English.

The stock installer remains restricted to its published builds. This is a source compatibility preview with a separately assembled local deployment, not an updated upstream installer/release. No game binaries are included in this source change.

Build 5638 requires StarMap 0.4.8 (0.4.7 fails before game startup because it references Brutal.Core.Numerics). Pass the loader explicitly:

```powershell
.\tools\build.ps1 -GameDir 'C:\Games\Kitten Space Agency' -LoaderDir 'C:\Tools\StarMap-0.4.8' -Check
```

This contribution was prepared with Codex assistance. Only source, translation dictionaries and this report should be submitted; local logs, settings, saves, game assemblies and loader binaries are excluded.
