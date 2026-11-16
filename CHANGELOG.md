# Changelog

All notable changes to Planet Scrappers. Versions follow `MAJOR.MINOR.PATCH`; the first client QA build is `0.1.0`.

## [0.1.0] — First client QA build: complete prototype loop (Modules 2–4)

### Added
- **Dismantling (Module 2):** pick up the rusty wrench, hold the left mouse button to swing it from the salvage arm.
  Objects lose parts at 66%, 33% and 10% durability (dented, stripped, bare frame), then break apart.
  Tool tiers: a tool bounces off anything above its tier. Sparks, debris, dust and layered hit sounds.
- 31 dismantlable objects in Rust Valley: oil drums, vending bots, hover scooters (tier 1), cargo containers,
  Mule-7 trucks (tier 2) and generator units behind the Machine Row gate (tier 3). Broken objects are
  replaced by fresh salvage after 3–6 minutes, so the valley never runs dry.
- **Scrap:** pooled scrap pieces pop out in a low-gravity arc and fly into the backpack when you are close.
  Rising pickup sound for streaks. SCRAP-BOT warns when the backpack is full.
- **Economy (Module 3):** four resources (iron, copper, electronics, rare alloy), backpack capacity (30/50/80/120),
  Recycling Depot with per-resource and sell-all, receipt and transaction log; Nova's Upgrade Station with
  Scrap Hammer (250 cr), Electric Dismantler (1,200 cr), backpack upgrades and locked later-phase tools.
- **Missions (Module 4):** the eight first-shift missions with rewards and SCRAP-BOT briefings; Mission 8 opens Machine Row.
- **Screens:** main menu (Continue with save summary, New Game with confirmation), HUD (health, oxygen,
  credits, mission tracker, target durability, interaction prompt, tool slots, backpack, SCRAP-BOT messages),
  inventory (Tab), missions (J), pause (Esc), settings, credits. Light frosted style from the approved designs.
- **Suit:** oxygen drains outside the base and refills near it; running out drains health; a suit failure returns
  you to base and drops half the backpack.
- **Save and load:** versioned JSON save with automatic backup, autosave after every sale, purchase and mission
  and every 2 minutes; settings saved separately.
- **Settings:** resolution, display mode, quality, frame-rate cap, V-sync, camera mode, mouse sensitivity,
  invert look, master volume.
- **Sound:** generated, licence-free sound effects and an ambience loop (`tools/generate_sfx.py`).
- **SCRAP-BOT scan (F):** tier, durability and contents of the object you are aiming at.
- **Automated playtest:** launch with `-autotest` to play the whole first shift automatically (29 checks, screenshots).

### Changed
- SCRAP-BOT: removed the cyan light that tinted its yellow parts green; flat screen face and rounded-box head.

## [0.0.2] — Module 1b: character revision

### Changed
- Third-person over-the-shoulder camera is now the default; the Rookie is visible on the moon surface.
- Movement is relative to the camera; the Rookie turns to face the direction of travel.
- SCRAP-BOT rebuilt to match the approved reference (white head, screen face with slanted cyan eyes,
  yellow ear discs and chest, cyan core light) and hovers at shoulder height beside the player.

### Added
- V (or right-stick click) switches between third-person and first-person; the body still casts a shadow in first person.
- Placeholder Rookie model styled after the reference: orange suit and helmet, black visor, grey ear caps,
  backpack with oxygen tanks, mechanical salvage arm on the right with a tool mount for Module 2.
- Walk, sprint, jump and landing animation (procedural, replaced by real animations in the art pass).
- Boot prints in the regolith that fade after about 25 seconds, and low-gravity dust puffs on steps and landings.
- Camera pulls in so rocks and buildings never block the view.

### Removed
- First-person-only controller (replaced by the player controller and camera rig).

## [0.0.1] — Module 1

### Added
- Unity 6 URP project structure and folder layout.
- First-person controller: walk, sprint, jump, mouse and gamepad look, low-gravity jump arc.
- Cursor lock (Esc releases, click to resume) until the pause menu arrives in Module 4.
- SCRAP-BOT placeholder that hovers beside the player and watches them.
- Rust Valley grey-box built from the approved map: base, Upgrade Station, Recycling Depot, warehouse, Vehicle Graveyard, Container Yard, locked Machine Row, debris fields, roads and perimeter fence.
- Generated starfield sky, the planet Halcyon on the horizon and tileable regolith ground.
- Version label on screen for QA reports.
- One-click Windows QA build and zip (`Planet Scrappers > Build Windows QA Zip`).
- Review screenshots from fixed viewpoints (`Planet Scrappers > Capture Review Snapshots`).
- Input devices written to the player log at start-up; `-inputdebug` logs look input for bug reports.

### Fixed during internal testing
- Signs no longer show through buildings (depth-tested sign shader).
- Building fronts were in shadow; the sun now sits in the north-east.
- SCRAP-BOT no longer drifts into the middle of the view while sprinting.
