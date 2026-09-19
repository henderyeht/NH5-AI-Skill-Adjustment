# NH5 AI Skill Adjustment Tool

A single-window tool for **NASCAR Heat 5**. Vanilla Custom AI is 85–105. This slider writes **60–200** into your local game so the field actually uses that strength.

Testers: download **`dist/NH5AiSkillAdjustment.exe`** and run that file only. No install.

## Use

1. Close NASCAR Heat 5.
2. Run `NH5AiSkillAdjustment.exe`.
3. Set 60–200 and click **APPLY**.
4. **RESTORE** puts the original 85–105 clamp back.

The in-game Options slider still lists 85–105. This tool is what the AI uses. Steam **Verify integrity of game files** also restores vanilla.

If the game is not found automatically, click **Locate game…** and pick your NASCAR Heat 5 folder (or `NASCARHeat5_Data\Managed`).

This only changes AI skill on this PC. It is not a server setting.

## Build

Build files live in `src/`. The published exe is copied to `dist/` and is kept separate from source.

```
dotnet publish src\NH5AiSkillAdjustment\NH5AiSkillAdjustment.csproj -c Release -o dist
```

Requires .NET SDK to build. Testers only need the exe (Windows with .NET Framework 4.8, which ships with Windows 10/11).

v1.0.1: Unity 2017 rejected a 4-byte `ldc.i4` inside this tiny method (`InvalidProgramException` on race load). The patch now returns `85 + delta` with short-form opcodes only.

## Layout

```
src/     C# source (WinForms, no console)
dist/    NH5AiSkillAdjustment.exe  — the public drop
```
