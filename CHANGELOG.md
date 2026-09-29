# Changelog

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
