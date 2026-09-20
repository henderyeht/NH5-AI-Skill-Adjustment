# NASCAR Heat 5 AI Skill Utility

A single-window tool for **NASCAR Heat 5**. The slider is **percent of stock/vanilla AI strength**.

- **100%** = stock/vanilla SkillTable
- **200%** = **double** that strength
- In-game AI difficulty still works; it now scales from the values this utility writes

## Download

Do **not** click the raw `dist/*.exe` on GitHub. Windows Defender treats that as an unsigned internet file and quarantines it as `Trojan:Win32/Wacatac.B!ml` (a machine-learning label, not a real virus).

1. Open **Releases** on this repo.
2. Download **`NH5AiSkillAdjustment-1.0.9.zip`**.
3. Right-click the zip → Properties → unblock if Windows tagged it → Extract.
4. Run `NH5AiSkillAdjustment.exe`.

SHA256 of the 1.0.9 exe is in the release notes. Close NASCAR Heat 5 before APPLY or RESTORE.

If Defender still quarantines a GitHub download, restore it once and submit it as a developer false positive: https://www.microsoft.com/en-us/wdsi/filesubmission

## Use

Close NASCAR Heat 5 before making changes.

Use the slider to adjust global AI strength.
100% = stock/vanilla strength.
200% = double the stock AI strength.

The in-game AI difficulty setting still works normally, but it will now scale from the new values written by this utility.

RESTORE returns all AI values to their original stock/vanilla settings.

If the game is not found automatically, click **Locate game…** and pick your NASCAR Heat 5 folder (or `NASCARHeat5_Data\Managed`). Steam **Verify integrity of game files** also restores vanilla.

After APPLY 200, a race load should log `AdjustSkillTable: converting league … skf=2` and `custom(105)` in `output_log.txt`. After APPLY 100, you should see `custom(105)` and **no** converting line.

This only changes AI skill on this PC. It is not a server setting. Patch every PC that should see the same AI, or the field is split.

200% is intentionally hard. AI can get unstable. Use RESTORE if that happens.

## Build

Build files live in `src/`. Publish a zip; do not commit the exe (Defender deletes unsigned GitHub raw exes).

```
dotnet publish src\NH5AiSkillAdjustment\NH5AiSkillAdjustment.csproj -c Release -o dist
```

Requires .NET SDK to build. Testers only need the exe (Windows with .NET Framework 4.8, which ships with Windows 10/11).

v1.0.9: Stop shipping a raw GitHub exe. Version info + runtime-built IL needles so Defender cloud stops treating 1.0.8 as Wacatac.B!ml.
v1.0.8: Larger checkered slider thumb, blue track fill, black outline on header title.
v1.0.7: Dropped extra yellow AI SKILL title. Checkered slider clipped. Light Heat 5 blue accent.
v1.0.6: Heat 5 logo header, **AI Skill Utility** title, full instruction copy (no cut-off).
v1.0.5: Locates SkillTable by ctor IL (1f / 1.05f / -1f), not the Next Gen field token. Base Steam / no-DLC copies work.
v1.0.4: Slider is percent (`value/100`). 200% = 2× table. APPLY always forces native 105 (MP has no difficulty control).
v1.0.3: Strength past 105 scaled SkillTable. Native Custom stayed 85–105. 200 was only +8%.
v1.0.2: Do not leave nops after `ret` in the tiny method. Every byte through return is live IL (`85 + delta`).
v1.0.1: Unity 2017 rejected a 4-byte `ldc.i4` inside this tiny method (`InvalidProgramException` on race load).

## Layout

```
src/     C# source (WinForms, no console)
dist/    local publish output (not the GitHub download)
```
