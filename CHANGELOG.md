# Changelog

All notable changes to Planet Scrappers. Versions follow `MAJOR.MINOR.PATCH`; the first client QA build is `0.1.0`.

## [0.3.0] — Phase 3: automation and new zones

### Added
- **Machines with running costs:** a shared machine framework (built at Nova's, switched on/off at the machine,
  charged credits per minute only while working; switches itself off if you can't pay).
- **Recycling Factory** (6,000 cr, 40 cr/min): refines warehouse scrap at 3 units/s and pays 30% more than the depot.
- **Conveyor links** (1,500 cr each, 10 cr/min): carry a collector's scrap to the warehouse; crates ride the belt.
- **Salvage Robots** (6,000 cr, up to 3): hovering drones that find tier 1-3 junk in Rust Valley and the Iron District,
  take it apart, vacuum up the scrap and unload it at the warehouse.
- **Spaceport Graveyard** (east gate, tier 4): a crashed freighter cut apart section by section (nose, wings,
  two hull sections, engine), a shuttle and cargo pods.
- **Deep Core** (west gate, tier 5): an underground mine under a rock roof, lit by work lamps, with drilling rigs,
  ore crushers, rare-alloy crystal veins and mine carts. Oxygen drains 2.5× faster underground.
- **Industrial Crusher** (tier 5, 12,000 cr).
- **Missions 15–20:** Power Up, Robot Crew, Spaceport Graveyard, Heavy Metal, Engine Room, Deep Core.
- 28 new dismantlable objects (78 in total); tool bar and hotkeys now cover tools 1–5.
- Upgrade Station redesigned into Tools and Automation columns.
- Save data now includes machines (built and on/off) and robot count.
- Automated playtest covers all 20 missions (65 checks) and ignores stray key presses while running.

### Changed
- Impact sounds fade with distance, so robots working far away stay quiet.

## [0.2.0] — Phase 2 start: salvage truck, Iron District, first automation

### Added
- **Drivable SH-01 salvage truck:** E to get in and out, W/S speed, A/D steer, Space brake. Its 150-unit bin
  vacuums loose scrap while you drive; sell the load at the Recycling Depot when parked within 40 m.
  The cabin is pressurised, so oxygen does not drain while driving.
- **Iron District:** a new zone north of Machine Row (through the gap in the fence), with two factory halls.
  New tier 3 and tier 4 machines: robot arms, hydraulic presses, assembly lines and factory cranes (19 more objects).
- **Plasma Cutter** (tier 4, 4,500 cr) now on sale at Nova's.
- **First automation:** three collector pads (Vehicle Graveyard, Container Yard, debris field). A Scrap Collector
  (800 cr) pulls in loose scrap within 15 m on its own; press E to empty it into your backpack.
- **Warehouse:** store your backpack at the warehouse drop-off (600 units) and sell the stored load at the depot.
- **Missions 9–14:** Take the Wheel, Haul Day, Set Up a Collector, Into the Iron District, Cutting Edge, Line Break.
- Saving now includes the truck's position and bin, the warehouse, collector hoppers and which collectors are built.
- Automated playtest extended to all 14 missions (47 checks).

### Changed
- Machine Row is fenced all the way to the north fence; its gate is the only way to the Iron District.
- The sky is a navy-to-blue gradient instead of near black.
- Scrap pieces now fly to the nearest scrap magnet (backpack, truck bin or collector).

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
