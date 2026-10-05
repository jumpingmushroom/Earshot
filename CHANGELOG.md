# Changelog

## 0.1.1 — more creatures, fewer gaps

- The Stone Golem's second attack no longer shows a file path; the game's caption for it is an
  asset path. Any game caption that isn't a translation key is now ignored.
- Golem attacks are threats (bold, ⚠), its footsteps are captioned, and its sighing is throttled
  idle chatter.
- Boss and creature lines that lost their action word now have one: Moder roaring, breathing ice
  and flapping its wings; The Elder, Eikthyr, Yagluth, Fader and The Queen roaring; The Elder
  shooting; and more. The game ships these words without English text, so Earshot supplies it.
- Warnings before the hit: Charred Marksman aiming, Fader's bite and claw wind-ups and fire-breath
  inhale, Fenring jumping, Dvergr and Drake charging, The Elder charging, Bonemass's ground attack,
  Abomination attacks and awakening, Yagluth's meteors, nova and beam, Gjall roaring, The Queen's
  pierce.
- Fallen Valkyrie, Seeker and Abomination attacks and Charred deaths count as threats.
- New: Vile, Writhan and Kvastur sounds, Wraith moaning, Fader's firewall and idle, big skeletons,
  the lava Leviathan sinking, the windmill finishing, and the Deep North creatures (Kall
  Fimbulbringer, Gammeltroll, Krigen, Hexen, Moose, Seal, Fallen Warrior, Elaking).
- A sound that Earshot's table gives to a creature (a boss or enemy row, or a creature as its
  source) is never mistaken for your own.

## 0.1.0 — first cut

- Closed captions at the bottom of the screen for the sounds worth reacting to: bosses, enemies,
  wildlife, and world events such as a smelter finishing, food done or burning, doors you didn't
  open and a shield generator low on fuel. A Raid line points at the event while one is running
  near you.
- Every line has a live arrow pointing at the sound (8 directions, or smooth), a `near` tag for
  close threats, and dims with distance.
- One line per kind of source with a `×N` count, priority by category when the list is full, idle
  chatter throttled, sounds you can see up close dimmed.
- Looping sounds are captioned while you can hear them, such as fires when the Ambient category
  is on. A Deathsquito's buzz is captioned the same way (not yet seen in play).
- Audibility is measured from the sound itself at your position, not your volume sliders, with a
  default cutoff of 0.05 — captions work with the game's sound off.
- Your own actions stay silent, including chests and doors you open (anything within the game's
  5 m interaction range). World sounds settle for a few seconds after you arrive through a portal
  or log in, so a reloading door doesn't caption its own opening sound.
- Settings → Accessibility → "Closed captions (Earshot)" turns captions on and off; everything
  else is in F1.
- Captions come from the game's own caption data where it's clean, from the nearest creature's
  name, and from Earshot's own table, which fixes or fills in the rest. English strings;
  translations can be added as text files.
- `earshot`, `earshot unlabelled` and `earshot demo` console commands.
