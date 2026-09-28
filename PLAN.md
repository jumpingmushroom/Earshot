# Earshot — Technical Plan

**Idea:** closed captions for the Valheim sounds worth reacting to, in the spirit of Minecraft's
subtitles: `⚠ ↖ Greydwarf alerted  near`, `→ Troll stomping`, `↓ Deathsquito buzzing`. Each line
has a live direction arrow. Built for deaf and hard-of-hearing players, and for anyone playing
with the sound low.

**Target build:** Valheim 1.0.16 (`Version.CurrentVersion = new GameVersion(1, 0, 16)`), Unity 6.
The findings below come from `assembly_valheim.dll` pulled from the rig's `valheim_Data/Managed`
on 2026-09-28 and decompiled with `ilspycmd` into the gitignored `decomp/`. Where noted, they
also come from a throwaway runtime probe run on the rig the same day (§1.4). The probe was not
committed.

**Scope:** client-side only. No RPCs, nothing on the server, works on vanilla servers. Thunderstore
namespace `Jumpingmushroom`, package `Earshot`, GUID `com.jumpingmushroom.earshot`, repo
`github.com/jumpingmushroom/Earshot`. BepInEx only, no Jotunn. The repo layout, build scripts,
publicizer setup and pure-model test project are the same as Milestones'.

**Prior art:** no captions or subtitles mod exists for Valheim. None of the 12,124 Valheim
packages on Thunderstore has "caption" or "subtitle" in its name or description (checked
2026-09-28). The closest is Nexus mod 3246, "Accessibility": a threat radar ring and creature
outlines, with no text. Earshot deliberately does not draw a radar or outlines.

**Decisions agreed (2026-09-28 design review):**
- **Scope B, "sounds you'd want to react to":** bosses, raids, enemies, wildlife, and world
  events that need attention. Your own actions stay silent.
- **Direction:** an 8-way arrow on every line, relative to where the camera faces, that turns
  live as you turn. Distance shows as brightness, plus a `near` tag.
- **Position:** the caption list sits at **bottom centre**, the usual place for subtitles, and
  stacks upward above the stamina and Eitr bars. Its position can be adjusted.
- **Bursts:**
  - One line per kind of source, with a `×N` count.
  - When the list is full, priority runs boss > raid > enemy > wildlife > world > ambient.
  - 5 lines by default.
  - A line lingers about 3 s after its sound was last heard. Loops stay while they're audible.
  - Close sources that are on screen are dimmed and ranked lowest, not hidden.
- **Labels, in order of precedence:** Earshot's own table, then the name of the nearest
  creature, then vanilla's caption tokens. Text that doesn't localise cleanly is never shown.
- **Settings:** everything is in ConfigurationManager (F1) and applies live. Iron Gate's hidden
  "Closed Captions" toggle in Settings → Accessibility is unhidden and becomes Earshot's on/off
  switch. Captions are **on by default**.
- **Localisation:** the source name uses the game's own translated names. The action words are
  Earshot's, combined through a template for each language. 0.1.0 ships English only.
- **Colour:** each category has a colour that stays distinct for colour-blind players. Threat
  lines (boss, raid, enemy) are also bold with a ⚠, so colour is never the only signal.
- **If Iron Gate turns their own captions on:** Earshot hides their panel, keeps its own, and
  logs that it did so.

---

## 1. How vanilla sound works

### 1.1 `ZSFX`: every sound effect prefab

Every one-shot and most looping sound effects are prefabs carrying a `ZSFX` component next to an
`AudioSource` (`ZSFX.cs`). `ZSFX.Play()` (ZSFX.cs:294) is the single choke point:

- It runs only if `AudioMan.instance.RequestPlaySound(this)` accepts the sound. That call
  applies the game's own concurrency limit, dropping near-duplicates by time and distance
  (AudioMan.cs:743-763). Sounds the game culls never reach us, which is a free first filter.
- It picks a random clip and plays it. For `m_playOnAwake` prefabs it is called from
  `CustomUpdate` on the first frame (ZSFX.cs:174-183), so **loops go through `Play()` too**. The
  probe saw `sfx_fire_loop` there, parented to `fire_pit_iron`.
- Most sounds are spawned by `EffectList.Create` (EffectList.cs:37) **at a position with no
  parent**. `BaseAI` plays idle and alerted sounds as `m_idleSound.Create(transform.position, …)`
  (BaseAI.cs:421, 1566). So a creature's sound is not attached to the creature.
  `ZSFX.SetSoundEffectCreator(ZDOID)` exists, but only footsteps, poison and burning call it
  (FootStep.cs:308, SE_Poison.cs:46, SE_Burning.cs:55).

Earshot hooks `ZSFX.Play` with a Harmony postfix. The postfix reads the `AudioSource`
(`loop`, `maxDistance`, `minDistance`, `rolloffMode`, `spatialBlend`), `ZSFX.m_maxVol` as the base
volume, and the transform, then
hands a plain struct to the model. It must stay cheap: the probe logged about 480 Neck idles in
17 minutes, and combat is far busier.

### 1.2 Iron Gate's unfinished caption system

1.0.16 ships most of a caption system with its core stubbed out:

- `ZSFX` has `m_closedCaptionToken`, `m_secondaryCaptionToken`,
  `m_captionType : ClosedCaptions.CaptionType` (Default, Wildlife, Enemy, Boss) and
  `m_minimumCaptionVolume = 0.3f` (ZSFX.cs:11-20).
- `ZSFX.Play` calls `ClosedCaptions.Instance.RegisterCaption(this)` for **non-looping** sounds
  that have a token (ZSFX.cs:302-305). **`RegisterCaption` is empty**
  (ClosedCaptions.cs:95).
- `CaptionItem` (fade in and out, duration, importance by type) and `CaptionArrow` (a rotating
  direction arrow, blurred with distance) exist. `ClosedCaptions` holds colours for each type:
  enemy `(0.8, 0.24, 0.04)`, wildlife `(0.78, 0.43, 0.65)`, boss `(0.34, 0.24, 0.62)`.
- `Settings.ClosedCaptions` and `Settings.DirectionalSoundIndicators` are read from
  `PlatformPrefs` (Settings.cs:205-206). `AccessibilitySettings` has a toggle for each, loaded
  and saved in `LoadSettings` / `OnOkAsync` (AccessibilitySettings.cs:66, 81). Nothing else
  reads them.
- Vanilla's `CaptionType.Wildlife` is labelled "Wildlife, Enemy Idles" in the inspector: a
  Greydwarf growl counts as "Wildlife". Earshot uses its own categories (§2.3).

### 1.3 Localisation data

The English localisation CSV in `resources.assets` has **135 `caption_*` keys**, all action words
(`caption_alerted,alerted`, `caption_stomping`, `caption_breathingfire`, `caption_treefall`, …),
plus a handful of `sfx_*` keys (`sfx_arrow_hit`, `sfx_wishbone_*`, …). **Only the English column is
filled in.** Creature names such as `$enemy_greydwarf` are translated into every language.
`Localization` loads its CSVs from `LocalizationSettings` in `Resources`
(assembly_guiutils `Localization.cs:191, 506`).

### 1.4 Probe results (rig, 2026-09-28)

The throwaway probe dumped every loaded `ZSFX` prefab and logged the first play of each sound
during about 17 minutes of play: base, forest, a fight, doors, a cart. The results shape
§2.2 and §2.4:

- `ClosedCaptions` exists in the HUD, at `_GameMain/LoadingGUI/PixelFix/IngameGui/ClosedCaptions`,
  and `Valid` is true. Its caption prefab is `CC Entry`, its indicator prefab is **null**, and it
  holds 6 lines for 5 s.
- The toggles `Settings/Panel/TabContent/Accessibility/List/ClosedCaptions` and
  `…/DirectionalIndicators` exist but are **hidden** (`activeSelf = false`). Both settings read
  false.
- **1,079 `ZSFX` prefabs. 628 have a caption token**: 314 Enemy, 141 Default, 119 Boss,
  54 Wildlife. **154 of those render broken text**, for example `[sfx_frozenking]` (40 prefabs),
  `[enemy_bear] groaning`, `[caption_roaring]` and `Wolf [caption_beingborn]`. 126 have a name
  but no action word.
- **49 prefabs loop, and only 9 of those are tagged.** Vanilla ignores loops anyway.
- **Untagged sounds that matter:** `sfx_troll_footstep` (100 m), `sfx_fire_loop`,
  `sfx_smelter_produce`, `sfx_kiln_produce`, `sfx_shieldgenerator_lowfuel_loop`,
  `sfx_lox_footstep*`, `sfx_flametalgate_door_*`, `sfx_trollfire_*`, `sfx_fader_bell` and the
  Fader spawn and meteor sounds, `sfx_leviathanlava_*`, and `sfx_distant thunder`.
- **Mislabelled:** `sfx_greydwarf_idle` says "Greydwarf **Brute** growling".
  `sfx_seal_crawl`, `[sfx_seal] [caption_death]`, sits on the Boar walking footstep
  (`fx_boar_footstep_walk`) and played 119 times.
- **Audibility matters.** First plays were logged far outside each sound's own `maxDistance`:
  Seagull at 225 m (range 100), Greydwarf at 169 m (range 50), tree fall at 129 m (range 60).
  "The game started a sound" is not the same as "you could hear it".
- **Idle volume:** in about 17 minutes the game played Neck idle 480×, Deer 246×, Seagull 226×,
  Greydwarf 175× and Boar 146×. Filtering is the main job.
- **Not `ZSFX` at all:** the cart rumble plays through bare `AudioSource`s on the `Cart`, and the
  Deathsquito buzz is expected to be the same kind of sound on the creature (to verify, §5).
  Ambient beds come from `AudioMan` (`RandomAmbientBase`, `m_ambientLoopSource`).
- The rig runs TNS Music Overhaul, which matters for any future music captions.

### 1.5 Other hooks used

- **Creatures:** `Character.s_characters` (Character.cs:612) is a static list of every live
  character. It is short, so scanning it for "nearest creature to this sound" is cheap. The
  creature's display name is `Character.m_name` (a localisable token).
- **Listener:** `AudioMan.instance.GetActiveAudioListener()` (AudioMan.cs:854). Direction and
  distance are measured from the listener, not the player. The camera's forward direction,
  flattened, is the reference for "ahead", the same maths as vanilla's
  `CaptionArrow.RotateArrow`.
- **Raids:** `RandEventSystem.instance.GetActiveEvent()` (RandEventSystem.cs:666) returns the
  event the local player is in. The client learns about it through `RPC_SetEvent`
  (RandEventSystem.cs:252). `RandomEvent.m_pos` gives the event centre and `m_name` its id.
  Vanilla already shows `m_startMessage` as a centre message (RandomEvent.cs:118). Earshot adds
  a persistent line with a direction while the event runs.
- **Console:** `new Terminal.ConsoleCommand(name, description, action)` (Terminal.cs:152).
- **HUD:** `Hud.instance` for the canvas. `m_staminaBar2Root` (Hud.cs:167) and `m_eitrBarRoot`
  (Hud.cs:191) are the bottom-centre bars the list must stay above.

---

## 2. Design

### 2.1 Pipeline

```
ZSFX.Play postfix ─┐
LoopTracker ──────┼─> SoundEvent ─> Filter ─> LabelResolver ─> CaptionBoard ─> CaptionHud
RaidWatch ─────────┘   (plain data)  (audible?  (text + category)  (merge, rank,   (draw, arrows,
                                      self?)                         linger)         fade)
```

Everything from `SoundEvent` up to `CaptionBoard` is pure C#, with no Unity or game types
(§3), so it can be unit-tested. Adapters on the game side turn Unity objects into `SoundEvent`s,
and the HUD reads the board's snapshot each frame.

### 2.2 Capture and filter (`Core/SoundCapture.cs`, `Core/Model/Audibility.cs`)

For each `ZSFX.Play`:

1. **Skip the obvious:** skip if Earshot is disabled, if there's no local player, or if the
   sound has `spatialBlend < 0.5`. The last catches UI and 2D sounds like `sfx_gui_*` and
   `sfx_equip`, which are always the player's own.
2. **Audibility:** compute the attenuation at the listener from the source's `rolloffMode`,
   `minDistance` and `maxDistance`:
   - Logarithmic: `min / max(d, min)`, zero beyond max.
   - Linear: `1 - (d - min) / (max - min)`.
   - Custom: evaluate `GetCustomCurve(AudioSourceCurveType.CustomRolloff)` at `d / max`.

   Multiply by the source's base volume. Drop the sound if the result is below
   `MinimumVolume` (default 0.3, from vanilla's `m_minimumCaptionVolume`). **The player's own
   volume sliders are deliberately ignored.** A deaf player may have sound at 0, and captions
   must still work.
3. **Whose sound is it:** find the nearest `Character` within 2 m of the sound, players
   included.
   - If it's a player, the sound is "self" and dropped. That covers your own, and other
     players', hurt, swing and eat sounds.
   - If it's a creature, the sound belongs to that creature and is never "self", even when
     close.
   - With no character nearby, a sound is also "self" if its creator (`m_sfxCreator`) is the
     local player, if it's parented under a `Player`, or if **any** player is within 2.5 m.
     That last rule covers the door you just opened, your chopping and building, and a
     teammate's. It's a heuristic to confirm on the rig (§5).
4. Build a `SoundEvent`:

   | Field | Meaning |
   | --- | --- |
   | `prefabName` | Name with `(Clone)` stripped |
   | `tokens` | The vanilla caption tokens |
   | `isLoop` | Whether the sound loops |
   | `position` | Where the sound is |
   | `distance` | From the listener |
   | `bearing` | Signed degrees from the camera's forward direction |
   | `loudness` | Attenuated volume, 0 to 1 |
   | `sourceId` | Instance id of the resolved creature or object, used for ×N and loop tracking |
   | `sourceName` | Name of that creature or object, if any |
   | `onScreen` | Viewport test: z > 0 and inside the viewport, within 15 m |

### 2.3 Labels and categories (`Core/Model/LabelResolver.cs`, `data/labels.tsv`)

**Categories** (Earshot's own, not vanilla's `CaptionType`):
`Boss`, `Raid`, `Enemy`, `Wildlife`, `World`, `Ambient`, `Self`. `Self` is never shown in
0.1.0. It exists so the table can mark the player's own sounds explicitly.

**The label table** is an embedded TSV. It is data, not code, so fixes are cheap and translators
see every string:

```
# prefab                  category  source      action        flags
sfx_troll_footstep        Enemy     @creature   stomping
sfx_greydwarf_idle        Wildlife  @creature   growling      idle
sfx_fire_loop             Ambient   $earshot_fire  crackling
sfx_smelter_produce       World     @object     done
sfx_seal_crawl            -                                    mute
sfx_frozenking_*          Boss      @creature   @vanilla
```

- `source`:
  - `@creature`: the nearest `Character` within 2 m of the sound, using its localised `m_name`.
  - `@object`: the `Piece` the sound is parented under, using its `Piece.m_name`. Station
    sounds such as `sfx_smelter_produce` are spawned unparented, so their rows name the piece
    with a literal token instead (`$piece_smelter`).
  - `$token`: a game token (`$enemy_troll`, localised by the game). `$earshot_…` tokens and bare
    words are looked up in Earshot's translation file.
- `action`: a key in Earshot's translation file (`crackling`), or `@vanilla`, which uses the
  vanilla secondary token when it localises cleanly.
- `flags`:
  - `idle`: wildlife-style chatter, throttled harder (§2.4).
  - `mute`: never caption this sound.
  - `near`: the line can show the `near` tag even though it isn't a threat, for example a
    shield generator low on fuel. It still has to be within `NearDistance`.
- A trailing `*` in the prefab name matches by prefix, so one line covers a whole family. The
  longest matching prefix wins, and an exact name beats any prefix.
- Fields are separated by whitespace, and `-` means empty. The few prefab names with a space in
  them (`sfx_distant thunder`) are written with `_` in the table. Lookups treat a space and `_`
  as the same.
- Creature loops (§2.5) use rows named `creature:<prefab>`, for example `creature:Deathsquito`.

**Resolution order** (agreed):
1. The table entry.
2. No entry, but the sound has a vanilla caption token and a creature within 2 m: the creature's
   name plus the vanilla secondary token, if clean. The vanilla token is what marks a sound as
   caption-worthy, so untagged hit and footstep sounds don't turn into bare creature names.
3. No creature: the vanilla primary token, plus the secondary token if it's clean. A clean
   primary with a broken secondary shows the primary alone.
4. Otherwise, **no caption**. With `LogUnlabelled` on, the sound is
recorded for gap reports. With no table entry, the category comes from the vanilla type:
Boss → Boss, Enemy → Enemy, Wildlife → Wildlife, but an enemy creature's idle stays Wildlife
(throttled), and Default → World.

**Clean text:** a localised string is rejected if it contains `[`, `]` or `$`, or if it's empty.
Vanilla's broken tokens can never reach the screen.

**Localisation:** English strings live in `Earshot/translations/English.txt`, embedded, as
`key = text`. The file includes `format = {source} {action}`, so languages that put the verb
first can reorder the parts. At start-up Earshot loads a file matching
`Localization.instance.GetSelectedLanguage()` from `BepInEx/config/Earshot/translations/` if one
exists, and falls back to English for missing keys. Only English ships in 0.1.0. The README
tells translators how to contribute.

**Coverage target for 0.1.0:** about 60 to 100 table rows:
- **Bosses:** every boss's alert, attack and spawn sounds, including the broken
  `sfx_frozenking*` family.
- **Enemies:** alerted and attacking for each biome's regulars, plus the footsteps vanilla
  leaves untagged (Troll, Fire Troll, Lox when hostile).
- **Wildlife idles:** kept, but flagged `idle`.
- **World:**
  - Smelter, kiln and furnace finishing; food done or burning.
  - Doors, gates, and portals activating.
  - Shield generator low on fuel; a tree falling; a ship hit.
- **Mutes:** the known mislabels.

The rest falls through to vanilla's clean tokens.

### 2.4 The caption board (`Core/Model/CaptionBoard.cs`)

The board takes `(SoundEvent, Label)` pairs and the current time, and produces a snapshot of up
to `MaxLines` lines.

- **Group key:** `category + source text`. For example, all Greydwarfs share one line. The line's
  text is `source + latest action`, and the action updates as new sounds arrive. `×N` counts the
  distinct `sourceId`s heard in the last `Linger` seconds, and shows only when N ≥ 2. The plural
  is `source ×N`, so there's no grammar problem across languages.
- **Linger:** a line stays for `Linger` seconds (default 3) after its last sound, then fades over
  0.5 s. Loops refresh their line on every scan while they're still audible (§2.5).
- **Throttle:** an `idle` sound can't **create** a line if the same group was captioned less than
  `IdleCooldown` seconds ago (default 20). It can still refresh a line that's visible. This
  handles Neck ×480 and similar.
- **Rank:**
  - Category priority first: boss 6, raid 5, enemy 4, wildlife 3, world 2, ambient 1.
  - Then *not on screen* above *on screen*.
  - Then nearer above farther.
  - Then newest first.

  When a new line would exceed `MaxLines`, the lowest-ranked line is removed. A new line that
  ranks below every visible line is dropped rather than displacing anything.
- **Order on screen:** newest at the bottom, like subtitles, so the eye doesn't have to jump.
  Threats aren't pinned to the top: rank decides who *stays*, not where a line is drawn.
- **Direction:** each line keeps the position of its nearest contributing source. The bearing is
  recomputed every frame from the current camera, so the arrow turns as you turn. With
  `SnapArrows` on (the default), the arrow snaps to 8 directions. Off, it rotates smoothly.
- **Distance:** line opacity is 1.0 when close, easing to `FarOpacity` (0.55) at the edge of the
  sound's range. The `near` tag shows within `NearDistance` (10 m) for threat categories and for
  rows flagged `near`.
- **On screen:** those lines draw at `OnScreenOpacity` (0.5) and rank lowest within their category.

### 2.5 Special sources

- **Creature loops (`Core/LoopTracker.cs`):** every 0.25 s, walk `Character.s_characters`
  within 60 m of the listener. For each, check a small table of prefab name → (label, looping
  `AudioSource` path), for example `Deathsquito` → "Deathsquito buzzing". If that `AudioSource`
  is playing and passes the audibility check, refresh the line.
- **Loops through ZSFX (fire, shield generator, stations):** `ZSFX.Play` gives the first frame
  only. The board keeps a weak list of looping `ZSFX` it has seen, re-checked every 0.25 s: still
  active, still `IsPlaying()`, still audible → refresh. Destroyed or silent → let it linger out.
- **Raids (`Core/RaidWatch.cs`):** every 0.5 s, if `RandEventSystem.instance.GetActiveEvent()`
  is non-null, keep a `Raid` line: "⚠ Raid" plus an arrow toward `m_pos`. It's only the word
  "Raid", because vanilla already shows the event's own message (`m_startMessage`) in the centre
  of the screen. The line is refreshed while the event is active, so it never fades mid-raid.
- **Fires:** `sfx_fire_loop` and the hearth, brazier and torch loops are `Ambient`, which is
  **off by default**. Inside a base they're constant background, not a cue.

### 2.6 The HUD (`UI/CaptionHud.cs`)

- **Placement:** a `RectTransform` under the HUD canvas, anchored bottom-centre at `OffsetY`
  (default: just above the Eitr and stamina bars). It stacks upward and is sized to the widest
  line. The background is a translucent dark rounded plate at `BackgroundOpacity` (0.6), like
  film subtitles, so text reads in daylight. Milestones learned that outline and backing matter.
- **Each line:** `[⚠] [arrow] Text ×N  near`, in TextMeshPro using the game's own font, so it fits
  Valheim's look and supports the game's languages.
- **The arrow and ⚠ are sprites generated in code**, not font glyphs. Valheim's fonts may lack
  `↖` and `⚠`, and one sprite rotated in 45° steps renders the same everywhere.
- **Colours**, all editable in F1:

  | Category | Colour |
  | --- | --- |
  | Boss | Magenta `#E056FF` |
  | Raid | Yellow `#FFD84A` |
  | Enemy | Orange `#FF8A3D` |
  | Wildlife | Light blue `#7EC8FF` |
  | World | Warm grey-white `#E8E4DA` |
  | Ambient | Grey `#B5B0A6` |

  Threat lines are **bold** with the ⚠ sprite. The colours are checked for deuteranopia and
  protanopia contrast before release.
- **Hidden** while the game's HUD is hidden (`Hud.IsUserHidden()`), in menus, and in the
  loading screen. Not hidden in the inventory: sound still matters there.
- **Scale** follows the game's GUI scale times Earshot's `Scale`.

### 2.7 Vanilla integration (`Core/VanillaCaptions.cs`)

- **Toggle:** after `AccessibilitySettings.Initialize()` (AccessibilitySettings.cs:56),
  activate the `ClosedCaptions` toggle (`m_closedCaptionsToggle.gameObject.SetActive(true)`). Its
  state is driven from and saved to Earshot's `General.Enabled`, **not** from
  `PlatformPrefs("ClosedCaptions")`, because vanilla's pref defaults to 0 and captions must be on
  by default. A postfix on `Initialize` sets the toggle, and a postfix on `OnOkAsync` writes it
  back to our config entry. `DirectionalIndicators` stays hidden. If the toggle's label
  is missing or unlocalised, Earshot sets it to its own "Closed captions (Earshot)" string (§5).
- **If vanilla captions go live:** if the vanilla `ClosedCaptions` object ever has active child
  lines, meaning Iron Gate filled in `RegisterCaption`, Earshot deactivates that object and logs
  `Vanilla closed captions detected; hiding them in favour of Earshot`. This is checked cheaply
  once per second after a caption-worthy sound.

### 2.8 Config (BepInEx, `com.jumpingmushroom.earshot.cfg`, all live via F1)

| Section | Setting | Default | Meaning |
| --- | --- | --- | --- |
| General | Enabled | `true` | Master switch. Mirrors the Settings → Accessibility toggle. |
| General | MinimumVolume | `0.3` | How loud a sound must be at your position to caption it (0 to 1, ignores your volume sliders). |
| Categories | Boss / Raid / Enemy / Wildlife / World | `true` | Caption this category. |
| Categories | Ambient | `false` | Fires, torches, other constant background. |
| Display | MaxLines | `5` | Lines shown at once. |
| Display | Linger | `3` | Seconds a line stays after its sound stops. |
| Display | IdleCooldown | `20` | Seconds before the same creature's idle chatter can caption again. |
| Display | OffsetX / OffsetY | `0` / `0` | Nudge from the bottom-centre anchor, in HUD pixels. |
| Display | Scale | `1` | Size of the caption list. |
| Display | BackgroundOpacity | `0.6` | Opacity of the backing plate. |
| Display | DimOnScreen | `true` | Dim lines whose source is close and on screen. |
| Display | SnapArrows | `true` | 8-way arrows. Off: smooth rotation. |
| Display | NearDistance | `10` | Metres within which threats get the `near` tag. |
| Colors | Boss … Ambient | see §2.6 | Line colour for each category. |
| Debug | LogUnlabelled | `false` | Log audible sounds that got no caption, to report gaps. |
| Debug | Verbose | `false` | Log every caption decision. |

### 2.9 Console

- `earshot`: the last 20 audible sounds, each with its prefab, distance, loudness, the label it
  got and where the label came from (`table`, `creature`, `vanilla`), or why it was skipped
  (`quiet`, `self`, `unlabelled`, `category off`, `throttled`).
- `earshot unlabelled`: every distinct unlabelled audible sound this session, to paste into a
  gap report.

---

## 3. Project layout

```
Earshot/
  Directory.Build.props, Earshot.sln, .gitignore, LICENSE, CHANGELOG.md, README.md, PLAN.md
  build/        package.sh, publish.sh, make_icon.py   (deploy/logs/shot/crop.sh local only)
  thunderstore/ manifest.json, README.md, icon.png
  docs/images/
  src/Earshot/
    Plugin.cs, PluginConfig.cs, ConfigurationManagerAttributes.cs
    Core/Model/     SoundEvent, Audibility, LabelTable, LabelResolver, CaptionBoard,
                    Bearing, Translations, TextCheck         (no UnityEngine, no game types)
    Core/           Runtime, SoundCapture, WorldQuery, LoopTracker, RaidWatch, VanillaCaptions,
                    EarshotConsole   (not "Game/": an Earshot.Game namespace would shadow Valheim's Game class)
    Patches/        ZsfxPatch, AccessibilitySettingsPatch
    UI/             CaptionHud, CaptionLine, Sprites
    data/labels.tsv, translations/English.txt                 (embedded resources)
  tests/Earshot.Tests/   net8.0 xUnit, compiles Core/Model by link
```

`lib/` (gitignored) holds the 1.0.16 reference assemblies from the rig, plus
`UnityEngine.AudioModule.dll` and `UnityEngine.PhysicsModule.dll`, which Milestones didn't need.

## 4. Milestones for the mod itself

1. **Scaffold:** the empty plugin, config, build scripts, the test project, and the `Core/Model`
   types with tests: audibility maths, clean-text check, label table parsing and prefix
   matching, resolution order, board merge, ×N, throttle, ranking, eviction, linger, and bearing
   to 8 directions.
2. **Capture without UI:** the `ZSFX.Play` hook, the self and audibility filters, creature
   resolution, and the `earshot` console command. Check on the rig that the labels, skips and
   distances in the log match what's happening in game.
3. **HUD:** the bottom-centre list, sprites, live arrows, fades, colours, dimming. Screenshots in
   daylight and at night. Check it stays clear of the stamina and Eitr bars and of ship controls.
4. **Special sources:** creature loops (Deathsquito), ZSFX loops, raids, fires.
5. **Vanilla integration:** the Accessibility toggle and the check for vanilla captions going live.
6. **Fill out the table** from `earshot unlabelled` sessions across biomes. Then the README,
   icon, screenshots and CHANGELOG. Package 0.1.0 and copy the zip to the rig's `~/Downloads`
   for the Thunderstore upload.

**Testing:** as in Milestones, `Core/Model` has no Unity or game dependency and is unit-tested by a
net8.0 xUnit project (`dotnet test tests/Earshot.Tests`) on the build box. Everything that
touches the game is checked on the rig:
- the console command and `LogUnlabelled` output in the BepInEx log (`build/logs.sh`);
- the UI with `build/shot.sh` captures;
- where several visual options need comparing, one relaunch with a temporary sampler rather than
  a relaunch per value (config does not hot-reload on the rig).

**Release steps** (same as the sibling mods):
1. Bump the version in `Plugin.cs`, the csproj and `manifest.json` together, and update
   `CHANGELOG.md`.
2. Run `build/package.sh`, which validates and writes `dist/Earshot-X.Y.Z.zip`.
3. Tag `vX.Y.Z`, push, and create a GitHub release with the zip.
4. `scp` the zip to the rig's `~/Downloads`. The user uploads it to Thunderstore.

No AI attribution in commits, PRs, the README or release notes.

## 5. Open questions / to verify on the rig

- **The Deathsquito buzz:** which `AudioSource` on the `Deathsquito` prefab plays it, and whether
  other creatures have similar attached loops worth adding (Wraith, Wisp?). Check with a dump
  of the prefab's children at step 4.
- **The self heuristic** (2.5 m for world sounds): does it catch the player's own doors, chopping
  and building without swallowing a Greydwarf hitting the wall you're standing at? Creature
  sounds are exempt, so the risk is world sounds only.
- **The vanilla toggle's label:** no `settings_*` caption key turned up in `resources.assets`. Check
  what the unhidden toggle actually shows before relying on it.
- **Raids on dedicated servers:** confirm `GetActiveEvent()` is set on a client of a dedicated
  server, via `RPC_SetEvent`, and that `m_pos` is correct there.
- **Placement:** the bottom-centre list against ship controls and the Eitr bar. It may need to
  shift up while sailing.
- **`MinimumVolume` calibration:** if the game's sounds use logarithmic rolloff, 0.3 is strict.
  A Greydwarf alert with `minDistance` 5 would fall below it at about 17 m. The `earshot`
  console output shows loudness and distance, so the default gets tuned at build step 2.
- **Performance** of the `ZSFX.Play` postfix and the 0.25 s scans in a busy base and during a raid.
  The target is well under 0.1 ms per frame on average.
