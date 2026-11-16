# Changelog

All notable changes to Planet Scrappers. Versions follow `MAJOR.MINOR.PATCH`; the first client QA build is `0.1.0`.

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
