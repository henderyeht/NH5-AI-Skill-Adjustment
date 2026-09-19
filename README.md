# NH5 AI Skill Adjustment Tool

A single-window tool for **NASCAR Heat 5**. The slider is **percent of vanilla AI pace**.

- **100%** = vanilla SkillTable, and native Custom **105** (Heat’s real max rating)
- **200%** = **double** that pace
- Multiplayer has **no AI difficulty option**. APPLY forces native 105 so online is not stuck on Auto (~97)

Testers: download **`dist/NH5AiSkillAdjustment.exe`** and run that file only. No install.

## Use

1. Close NASCAR Heat 5.
2. Run `NH5AiSkillAdjustment.exe`.
3. Set 60–200% and click **APPLY**.
4. **RESTORE** puts vanilla back.

Steam **Verify integrity of game files** also restores vanilla.

If the game is not found automatically, click **Locate game…** and pick your NASCAR Heat 5 folder (or `NASCARHeat5_Data\Managed`).

After APPLY 200, a race load should log `AdjustSkillTable: converting league … skf=2` and `custom(105)` in `output_log.txt`. After APPLY 100, you should see `custom(105)` and **no** converting line.

This only changes AI skill on this PC. It is not a server setting. Patch every PC that should see the same AI, or the field is split.

200% is intentionally hard. AI can get unstable. Use RESTORE if that happens.

## Build

Build files live in `src/`. The published exe is copied to `dist/` and is kept separate from source.

```
dotnet publish src\NH5AiSkillAdjustment\NH5AiSkillAdjustment.csproj -c Release -o dist
```

Requires .NET SDK to build. Testers only need the exe (Windows with .NET Framework 4.8, which ships with Windows 10/11).

v1.0.5: Locates SkillTable by ctor IL (1f / 1.05f / -1f), not the Next Gen field token. Base Steam / no-DLC copies work.
v1.0.4: Slider is percent (`value/100`). 200% = 2× table. APPLY always forces native 105 (MP has no difficulty control).
v1.0.3: Strength past 105 scaled SkillTable. Native Custom stayed 85–105. 200 was only +8%.
v1.0.2: Do not leave nops after `ret` in the tiny method. Every byte through return is live IL (`85 + delta`).
v1.0.1: Unity 2017 rejected a 4-byte `ldc.i4` inside this tiny method (`InvalidProgramException` on race load).

## Layout

```
src/     C# source (WinForms, no console)
dist/    NH5AiSkillAdjustment.exe  — the public drop
```
