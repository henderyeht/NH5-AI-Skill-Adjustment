# NASCAR Heat 5 AI Skill Utility

A single-window tool for **NASCAR Heat 5**. The slider is **percent of stock/vanilla AI strength**.

- **100%** = stock/vanilla SkillTable
- **200%** = **double** that strength
- In-game AI difficulty still works; it now scales from the values this utility writes

## Download

`dist/NH5AiSkillAdjustment.exe` is the public drop on GitHub.

If Windows Defender quarantines it as `Trojan:Win32/Wacatac.B!ml`, that is a machine-learning false positive, not a real virus. Allow the file in Windows Security:

1. Open **Windows Security** → **Virus & threat protection**.
2. Open **Protection history**.
3. Select the blocked `NH5AiSkillAdjustment.exe`.
4. Choose **Actions** → **Allow on device** (or Restore, then Allow).

You can also add it under **Virus & threat protection** → **Manage settings** → **Exclusions** → **Add an exclusion** → **File**, and pick `NH5AiSkillAdjustment.exe`.

Close NASCAR Heat 5 before APPLY or RESTORE.

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

Build files live in `src/`. Testers only need the exe from `dist/`.

```
dotnet publish src\NH5AiSkillAdjustment\NH5AiSkillAdjustment.csproj -c Release -o dist
```

Requires .NET SDK to build. The exe runs on Windows with .NET Framework 4.8 (ships with Windows 10/11).

## Layout

```
src/     C# source (WinForms, no console)
dist/    NH5AiSkillAdjustment.exe  — the public drop
```
