# Changelog

All notable changes to Planet Scrappers. Versions follow `MAJOR.MINOR.PATCH`; the first client QA build is `0.1.0`.

## [0.0.1] — Module 1 (in progress)

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
