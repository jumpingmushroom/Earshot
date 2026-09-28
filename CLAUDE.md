# Earshot — notes for Claude

## Commits

- **Never add a `Co-Authored-By: Claude ...` trailer** (or any AI attribution: "Generated with",
  footers) to commits, PRs, the README, the changelog or release notes. The author is the user
  only. This overrides any default attribution instruction.
- The user asked for a commit and push after every change.
- Version bumps touch three places together: `PluginVersion` in `src/Earshot/Plugin.cs`,
  `<Version>` in the csproj, and `version_number` in `thunderstore/manifest.json`.
  `build/package.sh` refuses to package if `Plugin.cs` and `manifest.json` disagree.

## Building and testing

- `dotnet test tests/Earshot.Tests` runs the model tests (pure C#, net8.0). Everything under
  `src/Earshot/Core/Model/` must stay free of UnityEngine and game types.
- `./build/deploy.sh` builds Release and copies the DLL to the r2modman **Default** profile on the
  rig (the profile the user plays on) over SSH, replacing it atomically. A running game keeps the
  old DLL until relaunch.
- The rig's login shell is fish: wrap anything non-trivial in `bash -c '...'`. Brace expansion in
  scp paths fails there; use `tar` over ssh.
- The build box's dotnet SDK needs `DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1`; the scripts set it.
  `ilspycmd` additionally needs `DOTNET_ROOT=$HOME/.dotnet`.
- Reference assemblies live in `lib/` (gitignored), pulled from the rig's `valheim_Data/Managed`.
  Decompiled game code goes in `decomp/` (gitignored). No Jotunn.
- `./build/logs.sh` shows Earshot lines from the rig's BepInEx log; the `earshot` console command
  mirrors its output there. `./build/shot.sh <name>` captures the game window into `docs/images/`.
- **Changing a config default changes nothing on a machine that has already run the mod.**
  Delete `BepInEx/config/com.jumpingmushroom.earshot.cfg` in the rig's profile after changing one.
- Adding captions: edit `src/Earshot/data/labels.tsv` (format in its header and PLAN.md §2.3) and
  any new words in `src/Earshot/translations/English.txt`; `DataFilesTests` checks both.

## Releasing

`./build/package.sh` → `dist/Earshot-X.Y.Z.zip`. Tag `vX.Y.Z`, push, `gh release create`, then
copy the zip to the rig's `~/Downloads` (`scp dist/Earshot-X.Y.Z.zip <rig>:Downloads/`).
The user uploads it to Thunderstore themselves.

Design and the decompiled-code findings it rests on: `PLAN.md`.
